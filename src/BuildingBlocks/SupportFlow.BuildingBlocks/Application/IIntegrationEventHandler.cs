using System.Diagnostics.CodeAnalysis;

namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// Handles an integration event of another module, delivered at least once by the outbox dispatcher (ADR-0003).
/// Implementations are inbound adapters of the consuming module and must be idempotent through
/// <see cref="IInbox"/> (ADR-0008).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "\"Integration event handler\" is the term of the architecture documents, not a .NET event delegate.")]
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    /// <summary>
    /// Stable name stored in the inbox. Never change it: a new name would handle already handled events again.
    /// </summary>
    string Name { get; }

    Task HandleAsync(TEvent integrationEvent, IntegrationEventContext context, CancellationToken cancellationToken);
}
