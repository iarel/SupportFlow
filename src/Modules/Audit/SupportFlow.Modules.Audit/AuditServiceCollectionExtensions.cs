using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportFlow.BuildingBlocks.Inbox;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Audit.Application;
using SupportFlow.Modules.Audit.EventHandlers;
using SupportFlow.Modules.Audit.Infrastructure;
using SupportFlow.Modules.Conversations.Contracts;

namespace SupportFlow.Modules.Audit;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddAudit(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AuditDbContext>(options => AuditDbContext.Configure(
            options,
            configuration.GetConnectionString("SupportFlow")
                ?? throw new InvalidOperationException("Connection string 'SupportFlow' is not configured.")));
        services.AddDatabaseMigrator<AuditDbContext>();
        services.AddRetentionCleanup<AuditDbContext>();

        services.AddScoped<IAuditEventRepository, AuditEventRepository>();

        // IInbox and IUnitOfWork are bound to the module's DbContext, so they are not registered in the shared
        // container (see AddConversations).
        services.AddScoped(provider =>
        {
            var context = provider.GetRequiredService<AuditDbContext>();

            return new RecordAuditHandler(
                provider.GetRequiredService<IAuditEventRepository>(),
                new EfInbox(context, provider.GetRequiredService<TimeProvider>()),
                new EfUnitOfWork(context));
        });

        services.AddIntegrationEventHandler<ConversationOpened, ConversationOpenedAuditHandler>();

        return services;
    }
}
