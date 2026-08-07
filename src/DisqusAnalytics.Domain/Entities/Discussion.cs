namespace DisqusAnalytics.Domain.Entities;

/// <summary>
/// Representa uma discussão (thread) do Disqus.
/// </summary>
public class Discussion
{
    /// <summary>
    /// Identificador da discussão no Disqus.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Fórum ao qual a discussão pertence.
    /// </summary>
    public long ForumId { get; set; }

    public Forum? Forum { get; set; }

    /// <summary>
    /// Título da discussão.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL da página comentada.
    /// </summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Slug da discussão.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade de comentários informada pelo Disqus.
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Data de criação da discussão.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Data do último comentário.
    /// </summary>
    public DateTimeOffset? LastPostAt { get; set; }

    public bool IsClosed { get; set; }

    public bool IsDeleted { get; set; }

    /// <summary>
    /// Indica se a discussão passou pelos filtros configurados.
    /// </summary>
    public bool IsRelevant { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
