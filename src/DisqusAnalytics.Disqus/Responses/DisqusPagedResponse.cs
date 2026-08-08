namespace DisqusAnalytics.Disqus.Responses;

public sealed class DisqusPagedResponse<T>
{
    public int Code { get; set; }

    public T? Response { get; set; }

    public CursorResponse? Cursor { get; set; }
}
