using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.Modules.AIAssistance;

public static class AIAssistanceServiceCollectionExtensions
{
    public static IServiceCollection AddAIAssistance(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
