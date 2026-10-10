using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Infrastructure;
using SupportFlow.Modules.SupportOrganization.Contracts;

namespace SupportFlow.Modules.Conversations;

public static class ConversationsServiceCollectionExtensions
{
    public static IServiceCollection AddConversations(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<ConversationsDbContext>(options => ConversationsDbContext.Configure(
            options,
            configuration.GetConnectionString("SupportFlow")
                ?? throw new InvalidOperationException("Connection string 'SupportFlow' is not configured.")));
        services.AddDatabaseMigrator<ConversationsDbContext>();

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationQueries, ConversationQueries>();
        services.AddScoped<IMessageQueries, MessageQueries>();
        services.AddScoped<GetConversationHandler>();
        services.AddScoped<GetMessagesHandler>();

        // IUnitOfWork, IIdempotencyStore and IIntegrationEventOutbox are shared BuildingBlocks ports, but each
        // module binds them to its own DbContext. Registered in the shared container, the modules would override
        // each other, so handlers get the module's instances explicitly.
        services.AddScoped(provider =>
        {
            var context = provider.GetRequiredService<ConversationsDbContext>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();

            return new OpenConversationHandler(
                provider.GetRequiredService<IConversationRepository>(),
                provider.GetRequiredService<IMessageRepository>(),
                new EfIdempotencyStore(context, timeProvider),
                new EfIntegrationEventOutbox(context, ConversationsIntegrationEventTypes.All, timeProvider),
                provider.GetRequiredService<ITeamQueries>(),
                new EfUnitOfWork(context),
                timeProvider);
        });

        return services;
    }
}
