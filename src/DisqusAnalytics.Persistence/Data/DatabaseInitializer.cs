using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DisqusAnalytics.Persistence.Data;

/// <summary>
/// Cria o banco na primeira execução e aplica as alterações de
/// esquema simples feitas depois que o banco já existia.
/// O projeto utiliza EnsureCreated (e não migrations), por isso
/// colunas novas e correções de dados precisam ser aplicadas aqui,
/// controladas pela versão gravada em PRAGMA user_version.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Versão atual do esquema/dados.
    /// 1: horários do Disqus corrigidos para UTC.
    /// </summary>
    private const int CurrentVersion = 1;

    public static async Task InitializeAsync(
        DisqusDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        await AddDiscussionColumnsAsync(
            context,
            cancellationToken);

        await FixDisqusTimestampsAsync(
            context,
            logger,
            cancellationToken);
    }

    private static async Task AddDiscussionColumnsAsync(
        DisqusDbContext context,
        CancellationToken cancellationToken)
    {
        var columns = await context.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('Discussions')")
            .ToListAsync(cancellationToken);

        var existing = new HashSet<string>(
            columns,
            StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains("CommentsSyncedAt"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Discussions\" " +
                "ADD COLUMN \"CommentsSyncedAt\" TEXT NULL;",
                cancellationToken);
        }

        if (!existing.Contains("SyncedCommentCount"))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Discussions\" " +
                "ADD COLUMN \"SyncedCommentCount\" INTEGER NOT NULL DEFAULT 0;",
                cancellationToken);
        }
    }

    /// <summary>
    /// As primeiras versões liam os horários do Disqus (que são UTC,
    /// porém sem fuso) como hora local. Isso gravava tudo 3 horas
    /// adiantado no horário de Brasília. Esta correção desloca os
    /// horários vindos da API uma única vez.
    /// </summary>
    private static async Task FixDisqusTimestampsAsync(
        DisqusDbContext context,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var version = await context.Database
            .SqlQueryRaw<int>(
                "SELECT user_version AS Value FROM pragma_user_version")
            .SingleAsync(cancellationToken);

        if (version >= CurrentVersion)
        {
            return;
        }

        var correction = TimeSpan.FromHours(-3);

        var forums = await context.Forums
            .ToListAsync(cancellationToken);

        foreach (var forum in forums)
        {
            forum.CreatedAt = forum.CreatedAt.Add(correction);
        }

        var discussions = await context.Discussions
            .ToListAsync(cancellationToken);

        foreach (var discussion in discussions)
        {
            discussion.CreatedAt = discussion.CreatedAt.Add(correction);

            if (discussion.LastPostAt is not null)
            {
                discussion.LastPostAt =
                    discussion.LastPostAt.Value.Add(correction);
            }
        }

        var comments = await context.Comments
            .ToListAsync(cancellationToken);

        foreach (var comment in comments)
        {
            comment.CreatedAt = comment.CreatedAt.Add(correction);
        }

        await context.SaveChangesAsync(cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            $"PRAGMA user_version = {CurrentVersion};",
            cancellationToken);

        logger.LogInformation(
            "Horários da API corrigidos para UTC: {Comments} comentários, " +
            "{Discussions} discussões e {Forums} fóruns atualizados.",
            comments.Count,
            discussions.Count,
            forums.Count);
    }
}
