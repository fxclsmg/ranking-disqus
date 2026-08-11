namespace DisqusAnalytics.Disqus.Responses;

using DisqusAnalytics.Disqus.DTOs;

public sealed class CommentPageResponse
{
    public List<CommentDto> Items { get; set; } = [];

    public CursorResponse? Cursor { get; set; }
}
