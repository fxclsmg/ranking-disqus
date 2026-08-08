namespace DisqusAnalytics.Disqus.DTOs;

public sealed class DiscussionDto
{
    public long Id { get; set; }

    public string? Ident { get; set; }

    public string? Forum { get; set; }

    public string? Title { get; set; }

    public string? Link { get; set; }

    public int Posts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public bool Closed { get; set; }

    public bool IsDeleted { get; set; }
}
