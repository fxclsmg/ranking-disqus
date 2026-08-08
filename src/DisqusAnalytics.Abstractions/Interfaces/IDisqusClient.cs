using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Domain.ValueObjects;

namespace DisqusAnalytics.Abstractions.Interfaces;

/// <summary>
/// Define a comunicação necessária com a API do Disqus.
/// </summary>
public interface IDisqusClient
{
    /// <summary>
    /// Obtém informações sobre um fórum.
    /// </summary>
    Task<Forum?> GetForumAsync(
        string forum,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma página de discussões de um fórum.
    /// </summary>
    /// <param name="forum">Nome curto do fórum no Disqus.</param>
    /// <param name="cursor">
    /// Cursor retornado pela página anterior.
    /// Null indica que a consulta deve começar do início.
    /// </param>
    /// <param name="limit">Quantidade máxima de discussões solicitadas.</param>
    Task<DiscussionPage> GetDiscussionsAsync(
        string forum,
        string? cursor = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma página de comentários de uma discussão.
    /// </summary>
    /// <param name="forum">Nome curto do fórum no Disqus.</param>
    /// <param name="discussionId">Identificador da discussão.</param>
    /// <param name="cursor">
    /// Cursor retornado pela página anterior.
    /// Null indica que a consulta deve começar do início.
    /// </param>
    /// <param name="limit">Quantidade máxima de comentários solicitados.</param>
    Task<CommentPage> GetCommentsAsync(
        string forum,
        long discussionId,
        string? cursor = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}
