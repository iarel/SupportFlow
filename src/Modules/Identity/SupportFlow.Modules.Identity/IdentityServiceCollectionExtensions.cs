using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Identity.Application;
using SupportFlow.Modules.Identity.Contracts;
using SupportFlow.Modules.Identity.Domain;
using SupportFlow.Modules.Identity.Endpoints;
using SupportFlow.Modules.Identity.Infrastructure;

namespace SupportFlow.Modules.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        services.AddDbContext<IdentityDbContext>(options => IdentityDbContext.Configure(
            options,
            configuration.GetConnectionString("SupportFlow")
                ?? throw new InvalidOperationException("Connection string 'SupportFlow' is not configured.")));
        services.AddDatabaseMigrator<IdentityDbContext>();

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<ResolveUserAccountHandler>();
        services.AddScoped<IClaimsTransformation, UserAccountClaimsTransformation>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddAuthorizationBuilder()
            .AddPolicy(IdentityPolicies.Customer, policy => policy.RequireClaim(
                UserAccountClaimTypes.AccountType,
                nameof(AccountType.Customer)));

        return services;
    }
}
