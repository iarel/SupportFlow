using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Application;

internal sealed class RecordAuditHandler(
    IAuditEventRepository auditEvents,
    IInbox inbox,
    IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Records the event once: a repeated delivery finds it in the inbox and changes nothing. Returns false for a
    /// repeat.
    /// </summary>
    public Task<bool> HandleAsync(RecordAuditCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return unitOfWork.ExecuteAsync(
            async ct =>
            {
                if (!await inbox.TryRegisterAsync(command.HandlerName, command.SourceEventId, ct))
                {
                    return false;
                }

                auditEvents.Add(AuditEvent.Record(
                    command.SourceEventId,
                    command.OccurredAt,
                    command.ActorType,
                    command.ActorId,
                    command.Action,
                    command.TargetType,
                    command.TargetId,
                    command.Details,
                    command.CorrelationId));

                return true;
            },
            cancellationToken);
    }
}
