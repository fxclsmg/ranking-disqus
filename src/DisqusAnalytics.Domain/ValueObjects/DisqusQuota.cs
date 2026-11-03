namespace DisqusAnalytics.Domain.ValueObjects;

/// <summary>
/// Cota de requisições da API do Disqus.
/// A chave pública permite 1000 requisições por janela de uma hora.
/// </summary>
public sealed record DisqusQuota
{
    /// <summary>
    /// Limite de requisições da janela.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Requisições que ainda podem ser feitas na janela atual.
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    /// Momento em que a janela reinicia.
    /// </summary>
    public DateTimeOffset? ResetAt { get; init; }
}
