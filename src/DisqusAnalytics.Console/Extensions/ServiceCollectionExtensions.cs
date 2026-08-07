using System.Reflection;
using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DisqusAnalytics.Console.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra automaticamente todos os comandos que implementam ICommand.
    /// </summary>
    public static IServiceCollection AddCommands(this IServiceCollection services)
    {
        var commandType = typeof(ICommand);

        var commandImplementations = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is
                {
                    IsClass: true,
                    IsAbstract: false
                }
                && commandType.IsAssignableFrom(type));

        foreach (var implementation in commandImplementations)
        {
            services.AddTransient(commandType, implementation);
        }

        return services;
    }
}
