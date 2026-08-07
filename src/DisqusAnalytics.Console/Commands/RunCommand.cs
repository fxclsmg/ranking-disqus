using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class RunCommand(
    ILogger<RunCommand> logger) : ICommand
{
    public string Name => "run";

    public string Description =>
        "Executa sincronização, processamento e exportação.";

    public Task ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Comando RUN iniciado.");

        return Task.CompletedTask;
    }
}
