namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// Events already handled by the consuming module's handlers, stored in its own schema (ADR-0008).
/// </summary>
public interface IInbox
{
    /// <summary>
    /// Records the event for the handler inside the current unit of work, so it is committed together with the
    /// handler's effect. Returns false when the handler already handled the event; the caller then does nothing.
    /// A concurrent delivery of the same event waits here until the first one ends.
    /// </summary>
    Task<bool> TryRegisterAsync(string handlerName, Guid eventId, CancellationToken cancellationToken);
}
