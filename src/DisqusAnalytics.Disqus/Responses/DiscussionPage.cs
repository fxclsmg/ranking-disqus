using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Disqus.Responses;

public sealed class DiscussionPage
{
    public IReadOnlyList<Discussion> Items { get; init; }
        = [];

    public string? NextCursor { get; init; }

    public bool HasNext { get; init; }
}
