using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DisqusAnalytics.Persistence.Repositories;

public sealed class ForumRepository(
    DisqusDbContext context) : IForumRepository
{
    public async Task<Forum?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await context.Forums
            .FirstOrDefaultAsync(
                forum => forum.Id == id,
                cancellationToken);
    }

    public async Task UpsertAsync(
        Forum forum,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.Forums
            .FirstOrDefaultAsync(
                item => item.Id == forum.Id,
                cancellationToken);

        if (existing is null)
        {
            context.Forums.Add(forum);
        }
        else
        {
            existing.ShortName = forum.ShortName;
            existing.Name = forum.Name;
            existing.Url = forum.Url;
            existing.CreatedAt = forum.CreatedAt;
            existing.LastSyncAt = forum.LastSyncAt;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
