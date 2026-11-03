using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Abstractions.Reporting;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Console.Commands;

public sealed class StatsCommand(
    IStatisticsService statisticsService,
    ILogger<StatsCommand> logger) : ICommand
{
    private const int TopItems = 5;

    public string Name => "stats";

    public string Description =>
        "Gera as estatísticas dos alvos configurados e exporta em JSON. " +
        "Uso: stats [caminho-do-arquivo].";

    public async Task ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Comando STATS iniciado.");

        var report = await statisticsService.GenerateAsync(
            cancellationToken);

        if (report.Shows.Count == 0)
        {
            logger.LogWarning(
                "Nenhuma estatística foi gerada. Verifique a seção " +
                "'Forums' e execute o comando sync.");

            return;
        }

        var path = await statisticsService.ExportAsync(
            report,
            arguments.FirstOrDefault(),
            cancellationToken);

        logger.LogInformation(
            "Relatório salvo em {Path}.",
            path);

        foreach (var show in report.Shows)
        {
            LogSummary(show);
        }

        logger.LogInformation(
            "Comando STATS finalizado.");
    }

    private void LogSummary(
        ShowStatistics show)
    {
        logger.LogInformation(
            "{Show}: {Comments} comentários de {Authors} autores " +
            "em {Discussions} discussões. " +
            "Média de {Average:0.0} caracteres por comentário.",
            show.Name,
            show.Totals.Comments,
            show.Totals.Authors,
            show.Totals.Discussions,
            show.Totals.AverageCharacters);

        if (show.TopWords.Count > 0)
        {
            logger.LogInformation(
                "  Palavras mais usadas: {Words}.",
                string.Join(
                    ", ",
                    show.TopWords
                        .Take(TopItems)
                        .Select(word => $"{word.Word} ({word.Count})")));
        }

        if (show.TopAuthors.Count > 0)
        {
            logger.LogInformation(
                "  Principais autores: {Authors}.",
                string.Join(
                    ", ",
                    show.TopAuthors
                        .Take(TopItems)
                        .Select(author =>
                            $"{author.Name} ({author.Comments})")));
        }
    }
}
