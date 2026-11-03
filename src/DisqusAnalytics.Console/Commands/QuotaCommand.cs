using DisqusAnalytics.Abstractions.Configuration;
using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisqusAnalytics.Console.Commands;

/// <summary>
/// Consulta quantas requisições ainda podem ser feitas na janela
/// atual da API do Disqus (1000 por hora na chave pública).
/// </summary>
public sealed class QuotaCommand(
    IDisqusClient disqusClient,
    IOptions<ForumTargetsOptions> targetsOptions,
    ILogger<QuotaCommand> logger) : ICommand
{
    public string Name => "quota";

    public string Description =>
        "Consulta a cota de requisições restante do Disqus. " +
        "Uso: quota [forum].";

    public async Task ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var forum = arguments.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(forum))
        {
            forum = targetsOptions.Value
                .GetValidTargets()
                .FirstOrDefault()?
                .Forum;
        }

        if (string.IsNullOrWhiteSpace(forum))
        {
            throw new InvalidOperationException(
                "Informe o fórum. Exemplo: quota scicastpodcast.");
        }

        var quota = await disqusClient.GetQuotaAsync(
            forum,
            cancellationToken);

        if (quota is null)
        {
            logger.LogWarning(
                "A API do Disqus não retornou os cabeçalhos de cota.");

            return;
        }

        logger.LogInformation(
            "Cota do Disqus ({Forum}): {Remaining} de {Limit} " +
            "requisições restantes nesta janela.",
            forum,
            quota.Remaining,
            quota.Limit);

        if (quota.ResetAt is not null)
        {
            var reset = quota.ResetAt.Value.ToLocalTime();

            logger.LogInformation(
                "A janela reinicia às {Reset:HH:mm} " +
                "(faltam {Minutes:0} minutos).",
                reset,
                (reset - DateTimeOffset.Now).TotalMinutes);
        }

        if (quota.Remaining < 100)
        {
            logger.LogWarning(
                "Restam poucas requisições: a sincronização para ao " +
                "atingir o limite e continua na próxima execução.");
        }
    }
}
