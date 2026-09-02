using DisqusAnalytics.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Sync.Services;

/// <summary>
/// Serviço responsável por coordenar a sincronização dos dados.
/// </summary>
public sealed class SynchronizationService(
    IDisqusClient disqusClient,
    IForumRepository forumRepository,
    IDiscussionRepository discussionRepository,
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
            "ID do fórum: {Id}",
            forumResult.Id);

        await forumRepository.UpsertAsync(
            forumResult,
            cancellationToken);

        logger.LogInformation(
            "Fórum salvo no banco de dados.");

        string? cursor = null;
        var pageNumber = 0;
        var totalThreads = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            pageNumber++;

            logger.LogInformation(
                "Consultando página {Page}. Cursor: {Cursor}",
                pageNumber,
                cursor ?? "(inicial)");

            var page = await disqusClient.GetDiscussionsAsync(
                forum,
                cursor,
                limit: 100,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Página {Page}: {Count} threads recebidas.",
                pageNumber,
                page.Items.Count);

            if (page.Items.Count > 0)
            {
                foreach (var discussion in page.Items)
                {
                    discussion.ForumId = forumResult.Id;
                }

                await discussionRepository.UpsertRangeAsync(
                    page.Items,
                    cancellationToken);

                totalThreads += page.Items.Count;

                logger.LogInformation(
                    "Página {Page}: {Count} threads salvas.",
                    pageNumber,
                    page.Items.Count);
            }

            if (!page.HasNextPage)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(page.NextCursor))
            {
                throw new InvalidOperationException(
                    "A API informou que existe uma próxima página, " +
                    "mas não retornou um cursor.");
            }

            cursor = page.NextCursor;

            logger.LogInformation(
                "Existe próxima página. Próximo cursor: {Cursor}",
                cursor);
        }

        forumResult.LastSyncAt = DateTimeOffset.UtcNow;

        await forumRepository.UpsertAsync(
            forumResult,
            cancellationToken);

        logger.LogInformation(
            "Sincronização concluída. {TotalThreads} threads processadas.",
            totalThreads);
    }
}
