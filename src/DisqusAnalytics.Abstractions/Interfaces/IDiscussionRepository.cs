using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface IDiscussionRepository
{
    Task<Discussion?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as discussões de um fórum, ordenadas pela data
    /// de criação.
    /// </summary>
    /// <param name="forumId">Fórum consultado.</param>
    /// <param name="asTracking">
    /// True quando as entidades serão alteradas e salvas depois.
    /// </param>
    Task<IReadOnlyList<Discussion>> GetByForumIdAsync(
        long forumId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(
        IEnumerable<Discussion> discussions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste alterações feitas em discussões já rastreadas.
    /// </summary>
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
