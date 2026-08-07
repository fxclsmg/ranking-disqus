namespace DisqusAnalytics.Domain.Entities;

/// <summary>
/// Representa um comentário realizado em uma discussão.
/// </summary>
public class Comment
{
    /// <summary>
    /// Identificador do comentário no Disqus.
    /// </summary>
    public long Id { get; set; }

    public long DiscussionId { get; set; }

    public Discussion? Discussion { get; set; }

    public long AuthorId { get; set; }

    public Author? Author { get; set; }

    /// <summary>
    /// Conteúdo do comentário.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Data de publicação.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Quantidade de caracteres do comentário.
    /// Será calculada durante a sincronização.
    /// </summary>
    public int CharacterCount { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsSpam { get; set; }
}
