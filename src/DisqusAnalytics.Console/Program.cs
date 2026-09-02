using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Console.Extensions;
using DisqusAnalytics.Disqus.Extensions;
using DisqusAnalytics.Disqus.Configurations;
using DisqusAnalytics.Sync.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DisqusAnalytics.Persistence;
using DisqusAnalytics.Persistence.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(
        "appsettings.json",
        optional: false,
        reloadOnChange: true)
    .AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDisqus(builder.Configuration);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddCommands();

builder.Services.AddSynchronization();

builder.Services.AddPersistence(builder.Configuration);


using var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<DisqusDbContext>();

    await dbContext.Database.EnsureCreatedAsync();
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Disqus Analytics");

var commands = host.Services.GetServices<ICommand>();

var commandName = args.FirstOrDefault() ?? "run";

var command = commands.FirstOrDefault(command =>
    command.Name.Equals(
        commandName,
        StringComparison.OrdinalIgnoreCase));

if (command is null)
{
    logger.LogError(
        "Comando '{Command}' não encontrado.",
        commandName);

    logger.LogInformation("Comandos disponíveis:");

    foreach (var item in commands)
    {
        logger.LogInformation(
            "{Name} - {Description}",
            item.Name,
            item.Description);
    }

    return;
}

await command.ExecuteAsync();
