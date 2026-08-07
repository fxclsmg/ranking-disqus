using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class SyncCommand(
    ILogger<SyncCommand> logger) : ICommand
{
    public string Name => "sync";

    public string Description => "Sincroniza o fórum com o Disqus.";

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Comando SYNC iniciado.");

        return Task.CompletedTask;
    }
}
