using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.Modules.Conversations;

public static class ConversationsServiceCollectionExtensions
{
    public static IServiceCollection AddConversations(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
