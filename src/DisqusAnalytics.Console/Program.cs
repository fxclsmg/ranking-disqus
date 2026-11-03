using DisqusAnalytics.Abstractions.Configuration;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Analytics.Extensions;
using DisqusAnalytics.Console.Extensions;
using DisqusAnalytics.Disqus.Extensions;
using DisqusAnalytics.Persistence;
using DisqusAnalytics.Persistence.Data;
using DisqusAnalytics.Sync.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

// Alvos de análise (podcasts) definidos na seção "Forums".
builder.Services.Configure<ForumTargetsOptions>(
    builder.Configuration);

builder.Services.AddSynchronization();

builder.Services.AddAnalytics(builder.Configuration);

builder.Services.AddPersistence(builder.Configuration);

using var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<DisqusDbContext>();

    var initializerLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseInitializer");

    await DatabaseInitializer.InitializeAsync(
        dbContext,
        initializerLogger);
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

await command.ExecuteAsync(args.Skip(1).ToList());
