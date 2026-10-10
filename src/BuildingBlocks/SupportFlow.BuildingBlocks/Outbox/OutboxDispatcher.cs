using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Persistence;

namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Delivers the outbox of the module that owns <typeparamref name="TContext"/> to the handlers of all modules
/// (ADR-0003): leases a batch with <c>FOR UPDATE SKIP LOCKED</c>, calls the handlers with no transaction open, then
/// marks each event processed or schedules a retry. Delivery is at least once; handlers deduplicate by inbox.
/// </summary>
public sealed class OutboxDispatcher<TContext>(
    IServiceScopeFactory scopeFactory,
    IntegrationEventTypes types,
    OutboxDispatcherOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher<TContext>> logger)
    : IOutboxDispatcher
    where TContext : DbContext
{
    private const int MaxErrorLength = 2000;

    private static readonly MethodInfo _deliverToHandlersMethod = typeof(OutboxDispatcher<TContext>)
        .GetMethod(nameof(DeliverToHandlersAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, DeliverToHandlers> _deliverers = new();

    private delegate Task DeliverToHandlers(
        IServiceProvider services,
        object integrationEvent,
        IntegrationEventContext context,
        CancellationToken cancellationToken);

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        var batch = await ClaimBatchAsync(cancellationToken);

        foreach (var message in batch)
        {
            await DeliverAsync(message, cancellationToken);
        }

        return batch.Count;
    }

    private static async Task DeliverToHandlersAsync<TEvent>(
        IServiceProvider services,
        object integrationEvent,
        IntegrationEventContext context,
        CancellationToken cancellationToken)
        where TEvent : class
    {
        // A failing handler fails the whole event; on retry the handlers that already succeeded skip it by inbox.
        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
        {
            await handler.HandleAsync((TEvent)integrationEvent, context, cancellationToken);
        }
    }

    private static DeliverToHandlers DelivererFor(Type eventType)
    {
        return _deliverers.GetOrAdd(
            eventType,
            type => _deliverToHandlersMethod.MakeGenericMethod(type).CreateDelegate<DeliverToHandlers>());
    }

    private async Task<List<OutboxMessage>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var now = timeProvider.GetUtcNow();

        return await context.Set<OutboxMessage>()
            .FromSqlRaw(context.Model.ClaimOutboxBatchSql(), now, now + options.LockDuration, options.BatchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private async Task DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (!types.TryGetType(message.Type, out var eventType))
            {
                throw new InvalidOperationException($"Unknown integration event '{message.Type}'.");
            }

            var integrationEvent = IntegrationEventSerializer.Deserialize(message.Payload, eventType);

            // A fresh scope per event: each handler works with new module contexts.
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                await DelivererFor(eventType)(
                    scope.ServiceProvider,
                    integrationEvent,
                    new IntegrationEventContext(message.Id),
                    cancellationToken);
            }

            await CompleteAsync(message, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await FailAsync(message, exception, cancellationToken);
        }
    }

    private async Task CompleteAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var updated = await UpdateLeasedAsync(
            message,
            setters => setters
                .SetProperty(m => m.ProcessedAt, now)
                .SetProperty(m => m.LockedUntil, (DateTimeOffset?)null),
            cancellationToken);

        if (!updated)
        {
            OutboxLog.LeaseLost(logger, message.Id, message.Type);
        }
    }

    private async Task FailAsync(OutboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var error = $"{exception.GetType().Name}: {exception.Message}";
        error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;

        if (message.Attempts >= options.MaxAttempts)
        {
            OutboxLog.Parked(logger, exception, message.Id, message.Type, message.Attempts);

            await UpdateLeasedAsync(
                message,
                setters => setters
                    .SetProperty(m => m.FailedAt, now)
                    .SetProperty(m => m.LastError, error)
                    .SetProperty(m => m.LockedUntil, (DateTimeOffset?)null),
                cancellationToken);

            return;
        }

        var nextAttemptAt = now + RetryDelay(message.Attempts);
        OutboxLog.WillRetry(logger, exception, message.Id, message.Type, message.Attempts, nextAttemptAt);

        await UpdateLeasedAsync(
            message,
            setters => setters
                .SetProperty(m => m.NextAttemptAt, nextAttemptAt)
                .SetProperty(m => m.LastError, error)
                .SetProperty(m => m.LockedUntil, (DateTimeOffset?)null),
            cancellationToken);
    }

    /// <summary>
    /// Updates the event only while this instance still holds its lease: if the lease expired and another instance
    /// claimed the event, <see cref="OutboxMessage.Attempts"/> has changed and the late result is dropped.
    /// </summary>
    private async Task<bool> UpdateLeasedAsync(
        OutboxMessage message,
        Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<OutboxMessage>> setters,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var updated = await context.Set<OutboxMessage>()
            .Where(m => m.Id == message.Id && m.Attempts == message.Attempts)
            .ExecuteUpdateAsync(setters, cancellationToken);

        return updated == 1;
    }

    /// <summary>
    /// Exponential backoff with jitter: half of the delay is fixed, half random.
    /// </summary>
    private TimeSpan RetryDelay(int attempts)
    {
        var exponential = options.InitialRetryDelay * Math.Pow(2, Math.Min(attempts - 1, 30));
        var delay = exponential < options.MaxRetryDelay ? exponential : options.MaxRetryDelay;
        var jitter = RandomNumberGenerator.GetInt32(0, 1001) / 1000.0;

        return delay * (0.5 + (0.5 * jitter));
    }
}
