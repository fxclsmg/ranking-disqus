using DisqusAnalytics.Abstractions.Configuration;
using DisqusAnalytics.Abstractions.Exceptions;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisqusAnalytics.Sync.Services;

/// <summary>
/// Serviço responsável por coordenar a sincronização dos dados.
/// Fluxo:
/// 1. Um fórum do Disqus é sincronizado uma única vez, mesmo que
///    possua vários alvos (podcasts).
/// 2. As discussões são marcadas como relevantes quando o título
///    atende aos filtros de algum alvo.
/// 3. Somente as discussões relevantes têm os comentários baixados,
///    economizando a cota de requisições da API.
/// </summary>
public sealed class SynchronizationService(
    IDisqusClient disqusClient,
    IForumRepository forumRepository,
    IDiscussionRepository discussionRepository,
    ICommentRepository commentRepository,
    IAuthorRepository authorRepository,
    IOptions<ForumTargetsOptions> targetsOptions,
    ILogger<SynchronizationService> logger) : ISynchronizationService
{
    private const int PageSize = 100;

    public async Task SynchronizeAsync(
        SyncScope scope = SyncScope.All,
        CancellationToken cancellationToken = default)
    {
        var targets = targetsOptions.Value.GetValidTargets();

        if (targets.Count == 0)
        {
            logger.LogWarning(
                "Nenhum alvo configurado na seção 'Forums'. " +
                "A sincronização não será executada.");

            return;
        }

        logger.LogInformation(
            "Iniciando sincronização. Escopo: {Scope}. Alvos: {Targets}.",
            scope,
            string.Join(", ", targets.Select(target => target.Name)));

        var groups = targets.GroupBy(
            target => target.Forum,
            StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await SynchronizeForumAsync(
                group.Key,
                group.ToList(),
                scope,
                cancellationToken);
        }

        logger.LogInformation(
            "Sincronização concluída.");
    }

    private async Task SynchronizeForumAsync(
        string shortName,
        IReadOnlyList<ForumTarget> targets,
        SyncScope scope,
        CancellationToken cancellationToken)
    {
        var forum = await GetOrCreateForumAsync(
            shortName,
            cancellationToken);

        if (forum is null)
        {
            return;
        }

        if (scope is SyncScope.All or SyncScope.Discussions)
        {
            await SynchronizeDiscussionsAsync(
                forum,
                targets,
                cancellationToken);
        }

        if (scope is SyncScope.All or SyncScope.Comments)
        {
            await SynchronizeCommentsAsync(
                forum,
                targets,
                cancellationToken);
        }

        forum.LastSyncAt = DateTimeOffset.UtcNow;

        await forumRepository.UpsertAsync(
            forum,
            cancellationToken);
    }

    /// <summary>
    /// Recupera o fórum do banco. A API só é consultada quando o
    /// fórum ainda não foi armazenado, economizando requisições.
    /// </summary>
    private async Task<Forum?> GetOrCreateForumAsync(
        string shortName,
        CancellationToken cancellationToken)
    {
        var stored = await forumRepository.GetByShortNameAsync(
            shortName,
            cancellationToken);

        if (stored is not null)
        {
            return stored;
        }

        var forum = await disqusClient.GetForumAsync(
            shortName,
            cancellationToken);

        if (forum is null)
        {
            logger.LogWarning(
                "O fórum {Forum} não foi encontrado no Disqus.",
                shortName);

            return null;
        }

        await forumRepository.UpsertAsync(
            forum,
            cancellationToken);

        logger.LogInformation(
            "Fórum encontrado: {Name} ({ShortName}).",
            forum.Name,
            forum.ShortName);

        return forum;
    }

    private async Task SynchronizeDiscussionsAsync(
        Forum forum,
        IReadOnlyList<ForumTarget> targets,
        CancellationToken cancellationToken)
    {
        string? cursor = null;
        var pageNumber = 0;
        var total = 0;
        var relevant = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            pageNumber++;

            logger.LogInformation(
                "Consultando página {Page} de discussões. Cursor: {Cursor}",
                pageNumber,
                cursor ?? "(inicial)");

            var page = await disqusClient.GetDiscussionsAsync(
                forum.ShortName,
                cursor,
                PageSize,
                cancellationToken);

            logger.LogInformation(
                "Página {Page}: {Count} discussões recebidas.",
                pageNumber,
                page.Items.Count);

            if (page.Items.Count > 0)
            {
                foreach (var discussion in page.Items)
                {
                    discussion.ForumId = forum.Id;

                    discussion.IsRelevant = IsRelevant(
                        targets,
                        discussion);
                }

                await discussionRepository.UpsertRangeAsync(
                    page.Items,
                    cancellationToken);

                total += page.Items.Count;

                relevant += page.Items.Count(
                    discussion => discussion.IsRelevant);

                logger.LogInformation(
                    "Página {Page}: {Count} discussões salvas. " +
                    "Relevantes até aqui: {Relevant}.",
                    pageNumber,
                    page.Items.Count,
                    relevant);
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
        }

        logger.LogInformation(
            "Discussões sincronizadas: {Total}. " +
            "Pertencem aos alvos: {Relevant}.",
            total,
            relevant);
    }

    private async Task SynchronizeCommentsAsync(
        Forum forum,
        IReadOnlyList<ForumTarget> targets,
        CancellationToken cancellationToken)
    {
        var discussions = await discussionRepository.GetByForumIdAsync(
            forum.Id,
            asTracking: true,
            cancellationToken);

        var relevanceChanged = false;

        foreach (var discussion in discussions)
        {
            var relevant = IsRelevant(targets, discussion);

            if (discussion.IsRelevant != relevant)
            {
                discussion.IsRelevant = relevant;

                relevanceChanged = true;
            }
        }

        if (relevanceChanged)
        {
            await discussionRepository.SaveChangesAsync(
                cancellationToken);
        }

        var selected = discussions
            .Where(discussion => discussion.IsRelevant)
            .ToList();

        var pending = selected
            .Where(NeedsCommentSync)
            .ToList();

        logger.LogInformation(
            "Discussões armazenadas: {Total}. Dos alvos: {Selected}. " +
            "Com comentários pendentes: {Pending}.",
            discussions.Count,
            selected.Count,
            pending.Count);

        var totalComments = 0;
        var syncedDiscussions = 0;

        try
        {
            foreach (var discussion in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = await SynchronizeDiscussionCommentsAsync(
                    forum,
                    discussion,
                    cancellationToken);

                totalComments += count;
                syncedDiscussions++;

                if (syncedDiscussions % 25 == 0)
                {
                    logger.LogInformation(
                        "Progresso: {Discussions}/{Pending} discussões, " +
                        "{Comments} comentários baixados.",
                        syncedDiscussions,
                        pending.Count,
                        totalComments);
                }
            }
        }
        catch (DisqusRateLimitException exception)
        {
            logger.LogWarning(
                "{Message} Progresso desta execução: " +
                "{Discussions} discussões e {Comments} comentários.",
                exception.Message,
                syncedDiscussions,
                totalComments);

            return;
        }

        logger.LogInformation(
            "Comentários sincronizados: {Comments} em {Discussions} " +
            "discussões.",
            totalComments,
            syncedDiscussions);
    }

    private async Task<int> SynchronizeDiscussionCommentsAsync(
        Forum forum,
        Discussion discussion,
        CancellationToken cancellationToken)
    {
        // Discussões sem comentários não precisam de requisição.
        if (discussion.CommentCount == 0)
        {
            MarkAsSynced(discussion);

            await discussionRepository.SaveChangesAsync(
                cancellationToken);

            return 0;
        }

        string? cursor = null;
        var fetched = 0;

        while (true)
        {
            var page = await disqusClient.GetCommentsAsync(
                forum.ShortName,
                discussion.Id,
                cursor,
                PageSize,
                cancellationToken);

            if (page.Items.Count > 0)
            {
                var authors = page.Items
                    .Where(comment => comment.Author is not null)
                    .Select(comment => comment.Author!)
                    .ToList();

                await authorRepository.UpsertRangeAsync(
                    authors,
                    cancellationToken);

                await commentRepository.UpsertRangeAsync(
                    page.Items,
                    cancellationToken);

                fetched += page.Items.Count;
            }

            if (!page.HasNextPage)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(page.NextCursor))
            {
                throw new InvalidOperationException(
                    $"A API informou que existem mais comentários na " +
                    $"discussão {discussion.Id}, mas não retornou um cursor.");
            }

            cursor = page.NextCursor;
        }

        // A data e a quantidade só são gravadas depois que a discussão
        // foi baixada por completo. Se a execução for interrompida,
        // ela será baixada novamente na próxima vez.
        MarkAsSynced(discussion);

        await discussionRepository.SaveChangesAsync(
            cancellationToken);

        logger.LogDebug(
            "Discussão {DiscussionId} ({Title}): {Count} comentários.",
            discussion.Id,
            discussion.Title,
            fetched);

        return fetched;
    }

    private static bool IsRelevant(
        IReadOnlyList<ForumTarget> targets,
        Discussion discussion)
    {
        return targets.Any(target => target.Matches(discussion.Title));
    }

    /// <summary>
    /// Indica se os comentários da discussão precisam ser consultados.
    /// A quantidade de comentários informada pelo Disqus é usada como
    /// sinal: se não mudou desde a última sincronização, nada a fazer.
    /// </summary>
    private static bool NeedsCommentSync(Discussion discussion)
    {
        if (discussion.CommentsSyncedAt is null)
        {
            return true;
        }

        return discussion.SyncedCommentCount != discussion.CommentCount;
    }

    private static void MarkAsSynced(Discussion discussion)
    {
        discussion.SyncedCommentCount = discussion.CommentCount;

        discussion.CommentsSyncedAt = DateTimeOffset.UtcNow;
    }
}
