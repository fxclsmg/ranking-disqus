using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class SyncCommand(
    ISynchronizationService synchronizationService,
    ILogger<SyncCommand> logger) : ICommand
{
    public string Name => "sync";

    public string Description =>
        "Sincroniza os alvos configurados com o Disqus. " +
        "Uso: sync [discussions|comments].";

    public async Task ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var scope = ParseScope(arguments.FirstOrDefault());

        logger.LogInformation(
            "Comando SYNC iniciado. Escopo: {Scope}.",
            scope);

        await synchronizationService.SynchronizeAsync(
            scope,
            cancellationToken);

        logger.LogInformation(
            "Comando SYNC finalizado.");
    }

    private static SyncScope ParseScope(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "all" => SyncScope.All,
            "discussions" or "threads" or "discussoes" => SyncScope.Discussions,
            "comments" or "comentarios" => SyncScope.Comments,
            _ => throw new ArgumentException(
                $"Escopo '{value}' inválido. " +
                "Utilize: discussions, comments ou all.")
        };
    }
}
