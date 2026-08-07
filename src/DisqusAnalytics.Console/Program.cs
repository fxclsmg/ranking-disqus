using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Console.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DisqusAnalytics.Console.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddCommands();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Disqus Analytics");

var commands = host.Services.GetServices<ICommand>();

var commandName = args.FirstOrDefault() ?? "run";

var command = commands.FirstOrDefault(c =>
    c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

if (command is null)
{
    logger.LogError("Comando '{Command}' não encontrado.", commandName);

    logger.LogInformation("Comandos disponíveis:");

    foreach (var item in commands)
    {
        logger.LogInformation("{Name} - {Description}",
            item.Name,
            item.Description);
    }

    return;
}

await command.ExecuteAsync();
