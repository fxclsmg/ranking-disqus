using System.Net.Http.Json;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Disqus.Configurations;
using DisqusAnalytics.Disqus.DTOs;
using DisqusAnalytics.Disqus.Responses;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisqusAnalytics.Disqus.Client;

public sealed class DisqusClient(
    HttpClient httpClient,
    IOptions<DisqusOptions> options,
    ILogger<DisqusClient> logger) : IDisqusClient
{
    private readonly DisqusOptions _options = options.Value;

    public async Task<Forum?> GetForumAsync(
        string forum,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(forum);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "A API Key do Disqus não foi configurada.");
        }

        var url =
            $"forums/details.json" +
            $"?forum={Uri.EscapeDataString(forum)}" +
            $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

        logger.LogInformation(
            "Consultando fórum {Forum}...",
            forum);

        var response =
            await GetAsync<ForumResponseDto>(
                url,
                cancellationToken);

        if (response is null)
        {
            return null;
        }

        return new Forum
        {
            Id = long.TryParse(response.PrimaryKey, out var id)
                ? id
                : 0,

            ShortName = response.Id ?? forum,

            Name = response.Name ?? string.Empty,

            Url = response.Url ?? string.Empty,

            CreatedAt = response.CreatedAt
        };
    }

    public async Task<DisqusAnalytics.Domain.ValueObjects.DiscussionPage>
        GetDiscussionsAsync(
            string forum,
            string? cursor = null,
            int limit = 100,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(forum);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "A API Key do Disqus não foi configurada.");
        }

        if (limit <= 0 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "O limite deve estar entre 1 e 100.");
        }

        var url =
            $"forums/listThreads.json" +
            $"?forum={Uri.EscapeDataString(forum)}" +
            $"&limit={limit}" +
            $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            url +=
                $"&cursor={Uri.EscapeDataString(cursor)}";
        }

        logger.LogInformation(
            "Consultando threads do fórum {Forum}. Cursor: {Cursor}",
            forum,
            cursor ?? "(inicial)");

        var response =
            await GetPagedAsync<List<DiscussionDto>>(
                url,
                cancellationToken);

        if (response?.Response is null)
        {
            return new DisqusAnalytics.Domain.ValueObjects.DiscussionPage(
                [],
                response?.Cursor?.Next,
                response?.Cursor?.HasNext
                    ?? response?.Cursor?.More
                    ?? false);
        }

        var discussions = response.Response
            .Select(MapDiscussion)
            .ToList();

        return new DisqusAnalytics.Domain.ValueObjects.DiscussionPage(
            discussions,
            response.Cursor?.Next,
            response.Cursor?.HasNext
                ?? response.Cursor?.More
                ?? false);
    }
        

    private static Discussion MapDiscussion(
        DiscussionDto dto)
    {
        return new Discussion
        {
            Id = dto.Id,

            ForumId =
                long.TryParse(dto.Forum, out var forumId)
                    ? forumId
                    : 0,

            Title = dto.Title ?? string.Empty,

            Link = dto.Link ?? string.Empty,

            Slug = dto.Ident ?? string.Empty,

            CommentCount = dto.Posts,

            CreatedAt = dto.CreatedAt,

            LastPostAt = dto.ModifiedAt,

            IsClosed = dto.Closed,

            IsDeleted = dto.IsDeleted
        };
    }

    public async Task<CommentPage> GetCommentsAsync(
        string forum,
        long discussionId,
        string? cursor = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(forum);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "A API Key do Disqus não foi configurada.");
        }

        if (limit <= 0 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "O limite deve estar entre 1 e 100.");
        }

        var url =
            $"threads/listPosts.json" +
            $"?forum={Uri.EscapeDataString(forum)}" +
            $"&thread={discussionId}" +
            $"&limit={limit}" +
            $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            url +=
                $"&cursor={Uri.EscapeDataString(cursor)}";
        }

        logger.LogInformation(
            "Consultando comentários da thread {DiscussionId}. Cursor: {Cursor}",
            discussionId,
            cursor ?? "(inicial)");

        var response =
            await GetPagedAsync<List<CommentDto>>(
                url,
                cancellationToken);

        var comments = (response?.Response ?? [])
            .Select(MapComment)
            .ToList();

        return new CommentPage(
            comments,
            response?.Cursor?.Next,
            response?.Cursor?.HasNext
                ?? response?.Cursor?.More
                ?? false);
    }

    private static Comment MapComment(CommentDto dto)
    {
        var authorId =
            long.TryParse(dto.Author?.Id, out var id)
                ? id
                : 0;

        return new Comment
        {
            Id =
                long.TryParse(dto.Id, out var commentId)
                    ? commentId
                    : 0,

            DiscussionId =
                long.TryParse(dto.Thread, out var discussionId)
                    ? discussionId
                    : 0,

            AuthorId = authorId,

            Message = dto.Message ?? string.Empty,

            CreatedAt = dto.CreatedAt,

            IsDeleted = dto.IsDeleted,

            IsSpam = dto.IsSpam,

            CharacterCount =
                dto.Message?.Length ?? 0
        };
    }

    private async Task<T?> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            relativeUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                DisqusResponse<T>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "A API do Disqus retornou uma resposta vazia.");
        }

        if (result.Code != 0)
        {
            throw new InvalidOperationException(
                $"A API do Disqus retornou o código {result.Code}.");
        }

        return result.Response;
    }

    private async Task<DisqusPagedResponse<T>?> GetPagedAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            relativeUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<
                DisqusPagedResponse<T>>(
                cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException(
                "A API do Disqus retornou uma resposta vazia.");
        }

        if (result.Code != 0)
        {
            throw new InvalidOperationException(
                $"A API do Disqus retornou o código {result.Code}.");
        }

        return result;
    }
}
