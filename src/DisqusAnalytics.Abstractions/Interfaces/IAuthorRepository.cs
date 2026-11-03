using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface IAuthorRepository
{
    Task<Author?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(
        IEnumerable<Author> authors,
        CancellationToken cancellationToken = default);
}
