namespace DisqusAnalytics.Disqus.Configurations;

public sealed class DisqusOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string Forum { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://disqus.com/api/3.0/";

    /// <summary>
    /// Intervalo entre requisições.
    /// A chave pública do Disqus aceita 1000 requisições por hora;
    /// o intervalo evita atingir o limite em uma execução longa.
    /// </summary>
    public int RequestDelayMilliseconds { get; set; } = 250;
}
