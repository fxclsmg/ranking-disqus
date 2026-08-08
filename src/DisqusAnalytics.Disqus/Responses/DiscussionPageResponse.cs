using DisqusAnalytics.Disqus.DTOs;

namespace DisqusAnalytics.Disqus.Responses;

public sealed class DiscussionPageResponse
{
    public List<DiscussionDto> Items { get; set; } = [];

    public DiscussionCursorDto? Cursor { get; set; }
}

public sealed class DiscussionCursorDto
{
    public string? Prev { get; set; }

    public string? Next { get; set; }

    public bool HasNext { get; set; }

    public bool HasPrev { get; set; }

    public bool More { get; set; }
}
