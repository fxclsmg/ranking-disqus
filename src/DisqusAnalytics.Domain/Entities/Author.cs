namespace DisqusAnalytics.Domain.Entities;

/// <summary>
/// Representa um usuário que realizou comentários.
/// </summary>
public class Author
{
    /// <summary>
    /// Identificador do usuário no Disqus.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nome de exibição.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome de usuário.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Perfil público.
    /// </summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL do avatar.
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
