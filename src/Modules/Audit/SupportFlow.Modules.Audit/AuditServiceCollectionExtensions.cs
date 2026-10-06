using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.Modules.Audit;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAudit(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
