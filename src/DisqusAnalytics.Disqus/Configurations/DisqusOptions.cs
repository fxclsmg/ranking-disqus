namespace DisqusAnalytics.Disqus.Configurations;

public sealed class DisqusOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string Forum { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://disqus.com/api/3.0/";
}
