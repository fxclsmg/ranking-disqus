using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Sync.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DisqusAnalytics.Sync.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSynchronization(
        this IServiceCollection services)
    {
        services.AddScoped<ISynchronizationService, SynchronizationService>();

        return services;
    }
}
