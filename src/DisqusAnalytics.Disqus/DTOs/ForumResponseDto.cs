using System.Text.Json.Serialization;

namespace DisqusAnalytics.Disqus.DTOs;

public sealed class ForumResponseDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("pk")]
    public string? PrimaryKey { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}
