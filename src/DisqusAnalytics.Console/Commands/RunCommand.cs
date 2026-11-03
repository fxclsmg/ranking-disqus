using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class RunCommand(
    ISynchronizationService synchronizationService,
    IStatisticsService statisticsService,
    ILogger<RunCommand> logger) : ICommand
{
    public string Name => "run";

    public string Description =>
        "Executa sincronização, processamento e exportação.";

    public async Task ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Comando RUN iniciado.");

        await synchronizationService.SynchronizeAsync(
            SyncScope.All,
            cancellationToken);

        var report = await statisticsService.GenerateAsync(
            cancellationToken);

        await statisticsService.ExportAsync(
            report,
            arguments.FirstOrDefault(),
            cancellationToken);

        logger.LogInformation(
            "Comando RUN finalizado.");
    }
}
