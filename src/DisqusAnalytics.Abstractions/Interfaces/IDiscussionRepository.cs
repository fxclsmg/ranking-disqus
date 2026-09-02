using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface IDiscussionRepository
{
    Task<Discussion?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(
        IEnumerable<Discussion> discussions,
        CancellationToken cancellationToken = default);
}
