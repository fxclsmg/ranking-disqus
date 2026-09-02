using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface IForumRepository
{
    Task<Forum?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Forum forum,
        CancellationToken cancellationToken = default);
}
