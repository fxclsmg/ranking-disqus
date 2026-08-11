namespace DisqusAnalytics.Disqus.DTOs;

public sealed class CommentDto
{
    public string Id { get; set; } = string.Empty;

    public string Thread { get; set; } = string.Empty;

    public string Forum { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string RawMessage { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsSpam { get; set; }

    public CommentAuthorDto? Author { get; set; }
}

public sealed class CommentAuthorDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string ProfileUrl { get; set; } = string.Empty;

    public CommentAvatarDto? Avatar { get; set; }
}

public sealed class CommentAvatarDto
{
    public string Permalink { get; set; } = string.Empty;
}
