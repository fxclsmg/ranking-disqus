namespace DisqusAnalytics.Disqus.Responses;

public sealed class CursorResponse
{
    public string? Prev { get; set; }

    public bool HasNext { get; set; }

    public string? Next { get; set; }

    public bool HasPrev { get; set; }

    public int? Total { get; set; }

    public string? Id { get; set; }

    public bool More { get; set; }
}
