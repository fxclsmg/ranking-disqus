using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Abstractions.Interfaces;

public interface ICommentRepository
{
    /// <summary>
    /// Obtém os comentários das discussões informadas.
    /// </summary>
    /// <param name="discussionIds">Discussões consultadas.</param>
    /// <param name="includeAuthor">
    /// Carrega os dados do autor junto com o comentário.
    /// </param>
    Task<IReadOnlyList<Comment>> GetByDiscussionIdsAsync(
        IReadOnlyCollection<long> discussionIds,
        bool includeAuthor = false,
        CancellationToken cancellationToken = default);

    Task UpsertRangeAsync(
        IEnumerable<Comment> comments,
        CancellationToken cancellationToken = default);
}
