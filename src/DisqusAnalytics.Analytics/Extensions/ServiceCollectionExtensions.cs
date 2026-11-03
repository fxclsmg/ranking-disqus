using DisqusAnalytics.Abstractions.Configuration;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Analytics.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DisqusAnalytics.Analytics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalytics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AnalyticsOptions>(
            configuration.GetSection("Analytics"));

        services.AddScoped<IStatisticsService, StatisticsService>();

        return services;
    }
}
