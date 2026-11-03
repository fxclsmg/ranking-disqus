namespace DisqusAnalytics.Abstractions.Exceptions;

/// <summary>
/// Lançada quando a API do Disqus informa que a cota de requisições
/// por hora foi atingida (HTTP 429).
/// A sincronização é interrompida e pode ser retomada depois, pois o
/// progresso é salvo discussão por discussão.
/// </summary>
public sealed class DisqusRateLimitException(string message)
    : Exception(message);
