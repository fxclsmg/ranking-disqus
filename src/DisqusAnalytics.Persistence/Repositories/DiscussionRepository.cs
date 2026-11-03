using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DisqusAnalytics.Persistence.Repositories;

public sealed class DiscussionRepository(
    DisqusDbContext context) : IDiscussionRepository
{
    public async Task<Discussion?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await context.Discussions
            .FirstOrDefaultAsync(
                discussion => discussion.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Discussion>> GetByForumIdAsync(
        long forumId,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Discussion> query = asTracking
            ? context.Discussions
            : context.Discussions.AsNoTracking();

        var items = await query
            .Where(discussion => discussion.ForumId == forumId)
            .ToListAsync(cancellationToken);

        // O SQLite não ordena por DateTimeOffset: a ordenação é feita
        // em memória.
        return items
            .OrderBy(discussion => discussion.CreatedAt)
            .ToList();
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.Discussions
            .FirstOrDefaultAsync(
                item => item.Id == discussion.Id,
                cancellationToken);

        if (existing is null)
        {
            context.Discussions.Add(discussion);
        }
        else
        {
            UpdateEntity(existing, discussion);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertRangeAsync(
        IEnumerable<Discussion> discussions,
        CancellationToken cancellationToken = default)
    {
        var items = discussions
            .GroupBy(discussion => discussion.Id)
            .Select(group => group.Last())
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        var ids = items
            .Select(discussion => discussion.Id)
            .ToList();

        var existing = await context.Discussions
            .Where(discussion => ids.Contains(discussion.Id))
            .ToDictionaryAsync(
                discussion => discussion.Id,
                cancellationToken);

        foreach (var discussion in items)
        {
            if (existing.TryGetValue(discussion.Id, out var current))
            {
                UpdateEntity(current, discussion);
            }
            else
            {
                context.Discussions.Add(discussion);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void UpdateEntity(
        Discussion existing,
        Discussion source)
    {
        existing.ForumId = source.ForumId;
        existing.Title = source.Title;
        existing.Link = source.Link;
        existing.Slug = source.Slug;
        existing.CommentCount = source.CommentCount;
        existing.CreatedAt = source.CreatedAt;
        existing.LastPostAt = source.LastPostAt;
        existing.IsClosed = source.IsClosed;
        existing.IsDeleted = source.IsDeleted;
        existing.IsRelevant = source.IsRelevant;
    }
}
