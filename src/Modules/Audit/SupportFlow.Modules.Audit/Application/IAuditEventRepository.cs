using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Application;

internal interface IAuditEventRepository
{
    void Add(AuditEvent auditEvent);
}
