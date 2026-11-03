using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DisqusAnalytics.Persistence.Repositories;

public sealed class CommentRepository(
    DisqusDbContext context) : ICommentRepository
{
    /// <summary>
    /// O SQLite limita a quantidade de parâmetros por comando,
    /// por isso as consultas são feitas em lotes de discussões.
    /// </summary>
    private const int MaxIdsPerQuery = 500;

    public async Task<IReadOnlyList<Comment>> GetByDiscussionIdsAsync(
        IReadOnlyCollection<long> discussionIds,
        bool includeAuthor = false,
        CancellationToken cancellationToken = default)
    {
        if (discussionIds.Count == 0)
        {
            return [];
        }

        var result = new List<Comment>();

        foreach (var chunk in discussionIds.Chunk(MaxIdsPerQuery))
        {
            IQueryable<Comment> query =
                context.Comments.AsNoTracking();

            if (includeAuthor)
            {
                query = query.Include(comment => comment.Author);
            }

            var comments = await query
                .Where(comment => chunk.Contains(comment.DiscussionId))
                .ToListAsync(cancellationToken);

            result.AddRange(comments);
        }

        // O SQLite não ordena por DateTimeOffset: a ordenação é feita
        // em memória.
        return result
            .OrderBy(comment => comment.CreatedAt)
            .ToList();
    }

    public async Task UpsertRangeAsync(
        IEnumerable<Comment> comments,
        CancellationToken cancellationToken = default)
    {
        var items = comments
            .GroupBy(comment => comment.Id)
            .Select(group => group.Last())
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        var ids = items
            .Select(comment => comment.Id)
            .ToList();

        var existing = await context.Comments
            .Where(comment => ids.Contains(comment.Id))
            .ToDictionaryAsync(
                comment => comment.Id,
                cancellationToken);

        foreach (var comment in items)
        {
            // O autor é gravado pelo IAuthorRepository. A navegação é
            // removida para evitar que o EF tente inserir o autor
            // novamente.
            comment.Author = null;

            if (existing.TryGetValue(comment.Id, out var current))
            {
                UpdateEntity(current, comment);
            }
            else
            {
                context.Comments.Add(comment);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void UpdateEntity(
        Comment existing,
        Comment source)
    {
        existing.DiscussionId = source.DiscussionId;
        existing.AuthorId = source.AuthorId;
        existing.Message = source.Message;
        existing.CreatedAt = source.CreatedAt;
        existing.CharacterCount = source.CharacterCount;
        existing.IsDeleted = source.IsDeleted;
        existing.IsSpam = source.IsSpam;
    }
}
