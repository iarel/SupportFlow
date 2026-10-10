using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.SupportOrganization.Contracts;
using SupportFlow.Modules.SupportOrganization.Infrastructure;

namespace SupportFlow.Modules.SupportOrganization;

public static class SupportOrganizationServiceCollectionExtensions
{
    public static IServiceCollection AddSupportOrganization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SupportOrganizationDbContext>(options => SupportOrganizationDbContext.Configure(
            options,
            configuration.GetConnectionString("SupportFlow")
                ?? throw new InvalidOperationException("Connection string 'SupportFlow' is not configured.")));
        services.AddDatabaseMigrator<SupportOrganizationDbContext>();

        services.AddScoped<ITeamQueries, TeamQueries>();

        return services;
    }
}
