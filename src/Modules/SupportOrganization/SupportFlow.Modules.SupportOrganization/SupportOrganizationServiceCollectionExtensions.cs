using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.Modules.SupportOrganization;

public static class SupportOrganizationServiceCollectionExtensions
{
    public static IServiceCollection AddSupportOrganization(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
