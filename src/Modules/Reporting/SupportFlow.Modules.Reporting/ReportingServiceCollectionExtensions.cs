using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.Modules.Reporting;

public static class ReportingServiceCollectionExtensions
{
    public static IServiceCollection AddReporting(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
