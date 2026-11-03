namespace DisqusAnalytics.Domain.Enums;

/// <summary>
/// Define quais dados devem ser sincronizados com o Disqus.
/// </summary>
public enum SyncScope
{
    /// <summary>
    /// Sincroniza discussões e comentários.
    /// </summary>
    All = 0,

    /// <summary>
    /// Sincroniza apenas as discussões (threads) do fórum.
    /// </summary>
    Discussions = 1,

    /// <summary>
    /// Sincroniza apenas os comentários das discussões relevantes
    /// que já estão armazenadas no banco.
    /// </summary>
    Comments = 2
}
