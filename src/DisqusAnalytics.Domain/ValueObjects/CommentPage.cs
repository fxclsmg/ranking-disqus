using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Domain.ValueObjects;

/// <summary>
/// Representa uma página de comentários retornada pela fonte de dados.
/// </summary>
public sealed class CommentPage
{
    public IReadOnlyList<Comment> Items { get; }

    public string? NextCursor { get; }

    public bool HasNextPage { get; }

    public CommentPage(
        IReadOnlyList<Comment> items,
        string? nextCursor,
        bool hasNextPage)
    {
        Items = items;
        NextCursor = nextCursor;
        HasNextPage = hasNextPage;
    }
}
