using SupportFlow.Modules.Audit.Application;
using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Infrastructure;

internal sealed class AuditEventRepository(AuditDbContext context) : IAuditEventRepository
{
    public void Add(AuditEvent auditEvent)
    {
        context.AuditEvents.Add(auditEvent);
    }
}
