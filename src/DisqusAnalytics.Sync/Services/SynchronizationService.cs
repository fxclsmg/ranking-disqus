using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Sync.Services;

/// <summary>
/// Serviço responsável por coordenar a sincronização dos dados.
/// </summary>
public sealed class SynchronizationService(
    IDisqusClient disqusClient,
    ILogger<SynchronizationService> logger) : ISynchronizationService
{
    public async Task SynchronizeAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string forum = "scicastpodcast";

        logger.LogInformation(
            "Iniciando sincronização do fórum {Forum}.",
            forum);

        var forumResult = await disqusClient.GetForumAsync(
            forum,
            cancellationToken);

        if (forumResult is null)
        {
            logger.LogWarning(
                "O fórum {Forum} não foi encontrado.",
                forum);

            return;
        }

        logger.LogInformation(
            "Fórum encontrado: {Name} ({ShortName}).",
            forumResult.Name,
            forumResult.ShortName);

        logger.LogInformation(
            "URL: {Url}",
            forumResult.Url);

        logger.LogInformation(
            "ID: {Id}",
            forumResult.Id);

        logger.LogInformation(
            "Consultando primeira página de threads...");

        var page = await disqusClient.GetDiscussionsAsync(
            forum,
            limit: 100,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Threads recebidas: {Count}",
            page.Items.Count);

        logger.LogInformation(
            "Existe próxima página: {HasNextPage}",
            page.HasNextPage);

        logger.LogInformation(
            "Próximo cursor: {Cursor}",
            page.NextCursor ?? "(nenhum)");

        foreach (var discussion in page.Items.Take(5))
        {
            logger.LogInformation(
                "Thread: {Id} | {Title} | Comentários: {Comments}",
                discussion.Id,
                discussion.Title,
                discussion.CommentCount);
        }

        logger.LogInformation(
            "Sincronização do fórum concluída.");
    }

    /*
    public async Task SynchronizeAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string forum = "scicastpodcast";

        logger.LogInformation(
            "Iniciando sincronização do fórum {Forum}.",
            forum);

        var result = await disqusClient.GetForumAsync(
            forum,
            cancellationToken);

        if (result is null)
        {
            logger.LogWarning(
                "O fórum {Forum} não foi encontrado.",
                forum);

            return;
        }

        logger.LogInformation(
            "Fórum encontrado: {Name} ({ShortName}).",
            result.Name,
            result.ShortName);

        logger.LogInformation(
            "URL: {Url}",
            result.Url);

        logger.LogInformation(
            "ID: {Id}",
            result.Id);

        logger.LogInformation($"Sincronização do fórum {forum} concluída.");
    }
    */
}
