using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface IForumRepository
{
    Task<Forum?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um fórum pelo nome curto utilizado no Disqus.
    /// Ex.: scicastpodcast.
    /// </summary>
    Task<Forum?> GetByShortNameAsync(
        string shortName,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Forum forum,
        CancellationToken cancellationToken = default);
}
