using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace DisqusAnalytics.Persistence.Repositories;

public sealed class AuthorRepository(
    DisqusDbContext context) : IAuthorRepository
{
    public async Task<Author?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await context.Authors
            .FirstOrDefaultAsync(
                author => author.Id == id,
                cancellationToken);
    }

    public async Task UpsertRangeAsync(
        IEnumerable<Author> authors,
        CancellationToken cancellationToken = default)
    {
        // Um mesmo autor pode comentar várias vezes na mesma página.
        var items = authors
            .GroupBy(author => author.Id)
            .Select(group => group.Last())
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        var ids = items
            .Select(author => author.Id)
            .ToList();

        var existing = await context.Authors
            .Where(author => ids.Contains(author.Id))
            .ToDictionaryAsync(
                author => author.Id,
                cancellationToken);

        foreach (var author in items)
        {
            if (existing.TryGetValue(author.Id, out var current))
            {
                current.Name = author.Name;
                current.Username = author.Username;
                current.ProfileUrl = author.ProfileUrl;
                current.AvatarUrl = author.AvatarUrl;
            }
            else
            {
                context.Authors.Add(author);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
