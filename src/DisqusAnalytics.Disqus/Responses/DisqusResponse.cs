using System.Text.Json.Serialization;

namespace DisqusAnalytics.Disqus.Responses;

public sealed class DisqusResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("cursor")]
    public CursorResponse? Cursor { get; set; }

    [JsonPropertyName("response")]
    public T? Response { get; set; }
}
