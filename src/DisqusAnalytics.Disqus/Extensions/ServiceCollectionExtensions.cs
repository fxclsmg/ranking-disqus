using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Disqus.Client;
using DisqusAnalytics.Disqus.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DisqusAnalytics.Disqus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDisqus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DisqusOptions>(
            configuration.GetSection("Disqus"));

        services.AddHttpClient<IDisqusClient, DisqusClient>(
            client =>
            {
                client.BaseAddress = new Uri(
                    "https://disqus.com/api/3.0/");

                client.Timeout = TimeSpan.FromSeconds(30);

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "DisqusAnalytics/1.0");
            });

        return services;
    }
}
