using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class SyncCommand(
    ISynchronizationService synchronizationService,
    ILogger<SyncCommand> logger) : ICommand
{
    public string Name => "sync";

    public string Description =>
        "Sincroniza os dados do fórum com o Disqus.";

    public async Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Comando SYNC iniciado.");

        await synchronizationService.SynchronizeAsync(
            cancellationToken);

        logger.LogInformation(
            "Comando SYNC finalizado.");
    }
}
