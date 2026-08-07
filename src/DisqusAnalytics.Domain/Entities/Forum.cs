namespace DisqusAnalytics.Domain.Entities;

/// <summary>
/// Representa um fórum do Disqus.
/// Exemplo:
/// scicastpodcast
/// </summary>
public class Forum : Entity
{
    /// <summary>
    /// Identificador do fórum no Disqus.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nome curto do fórum.
    /// Ex.: scicastpodcast
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Nome amigável.
    /// Ex.: Portal Deviante
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL principal do fórum.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação do fórum.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Data da última sincronização.
    /// </summary>
    public DateTimeOffset? LastSyncAt { get; set; }

    /// <summary>
    /// Discussões pertencentes ao fórum.
    /// </summary>
    public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
}
