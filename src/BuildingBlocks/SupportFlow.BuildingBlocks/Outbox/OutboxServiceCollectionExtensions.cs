using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Persistence;

namespace SupportFlow.BuildingBlocks.Outbox;

public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the outbox of a publishing module for delivery. The Worker polls it; other hosts ignore it.
    /// </summary>
    public static IServiceCollection AddOutbox<TContext>(this IServiceCollection services, IntegrationEventTypes types)
        where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new OutboxDispatcherOptions());

        return services.AddSingleton<IOutboxDispatcher>(provider => new OutboxDispatcher<TContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            types,
            provider.GetRequiredService<OutboxDispatcherOptions>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<OutboxDispatcher<TContext>>>()));
    }

    /// <summary>
    /// Subscribes a handler of the consuming module to an integration event of another module.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        return services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
    }

    /// <summary>
    /// Registers retention cleanup of the module's outbox, inbox and idempotency keys, whichever it has.
    /// </summary>
    public static IServiceCollection AddRetentionCleanup<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(new RetentionOptions());

        return services.AddSingleton<IRetentionCleanup, EfRetentionCleanup<TContext>>();
    }
}
