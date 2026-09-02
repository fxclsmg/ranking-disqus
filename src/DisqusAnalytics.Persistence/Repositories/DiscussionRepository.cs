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
        foreach (var discussion in discussions)
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
