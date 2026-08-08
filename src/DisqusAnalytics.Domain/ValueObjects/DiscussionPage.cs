using DisqusAnalytics.Domain.Entities;

namespace DisqusAnalytics.Domain.ValueObjects;

/// <summary>
/// Representa uma página de discussões retornada pela fonte de dados.
/// </summary>
public sealed class DiscussionPage
{
    public IReadOnlyList<Discussion> Items { get; }

    public string? NextCursor { get; }

    public bool HasNextPage { get; }

    public DiscussionPage(
        IReadOnlyList<Discussion> items,
        string? nextCursor,
        bool hasNextPage)
    {
        Items = items;
        NextCursor = nextCursor;
        HasNextPage = hasNextPage;
    }
}
