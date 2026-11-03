using System.Net.Http.Json;
using System.Text.Json;
using DisqusAnalytics.Abstractions.Exceptions;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Disqus.Configurations;
using DisqusAnalytics.Disqus.DTOs;
using DisqusAnalytics.Disqus.Responses;
using DisqusAnalytics.Disqus.Serialization;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Domain.Text;
using DisqusAnalytics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisqusAnalytics.Disqus.Client;

public sealed class DisqusClient(
    HttpClient httpClient,
    IOptions<DisqusOptions> options,
    ILogger<DisqusClient> logger) : IDisqusClient
{
    /// <summary>
    /// Opções de leitura com o conversor que trata os horários sem
    /// fuso como UTC (padrão do Disqus).
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters = { new UtcDateTimeOffsetConverter() }
        };

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

    public async Task<Domain.ValueObjects.DiscussionPage> GetDiscussionsAsync(
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
            return new Domain.ValueObjects.DiscussionPage(
                [],
                response?.Cursor?.Next,
                response?.Cursor?.HasNext
                    ?? response?.Cursor?.More
                    ?? false);
        }

        var discussions = response.Response
            .Select(MapDiscussion)
            .ToList();

        return new Domain.ValueObjects.DiscussionPage(
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
            $"&order=asc" +
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
        var author = MapAuthor(dto.Author);

        var message = dto.Message ?? string.Empty;

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

            AuthorId = author.Id,

            Author = author,

            Message = message,

            CreatedAt = dto.CreatedAt,

            IsDeleted = dto.IsDeleted,

            IsSpam = dto.IsSpam,

            // O texto do comentário vem em HTML. O tamanho é calculado
            // sobre o texto puro para as estatísticas não contarem tags.
            CharacterCount = CommentText.Clean(message).Length
        };
    }

    private static Author MapAuthor(CommentAuthorDto? dto)
    {
        if (dto is null
            || !long.TryParse(dto.Id, out var authorId)
            || authorId == 0)
        {
            return new Author
            {
                Id = Author.AnonymousId,
                Name = "Anônimo",
                Username = "anonimo"
            };
        }

        return new Author
        {
            Id = authorId,
            Name = dto.Name ?? string.Empty,
            Username = dto.Username ?? string.Empty,
            ProfileUrl = dto.ProfileUrl ?? string.Empty,
            AvatarUrl = dto.Avatar?.Permalink ?? string.Empty
        };
    }

    /// <summary>
    /// Executa a requisição respeitando o intervalo configurado e
    /// tratando o limite de requisições da API (HTTP 429).
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        if (_options.RequestDelayMilliseconds > 0)
        {
            await Task.Delay(
                _options.RequestDelayMilliseconds,
                cancellationToken);
        }

        var response = await httpClient.GetAsync(
            relativeUrl,
            cancellationToken);

        if ((int)response.StatusCode == 429)
        {
            response.Dispose();

            throw new DisqusRateLimitException(
                "A cota de requisições por hora da API do Disqus foi " +
                "atingida. Execute a sincronização novamente mais tarde: " +
                "o progresso já foi salvo.");
        }

        response.EnsureSuccessStatusCode();

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Requisições restantes na cota do Disqus: {Remaining}.",
                ReadQuota(response).Remaining);
        }

        return response;
    }

    /// <summary>
    /// Consulta a cota restante. Também confirma que a chave da API
    /// continua válida.
    /// </summary>
    public async Task<DisqusQuota?> GetQuotaAsync(
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

        using var response = await SendAsync(
            url,
            cancellationToken);

        return ReadQuota(response);
    }

    private static DisqusQuota ReadQuota(
        HttpResponseMessage response)
    {
        var limit = ReadIntHeader(
            response,
            "X-Ratelimit-Limit");

        var remaining = ReadIntHeader(
            response,
            "X-Ratelimit-Remaining");

        var reset = ReadLongHeader(
            response,
            "X-Ratelimit-Reset");

        return new DisqusQuota
        {
            Limit = limit ?? 0,
            Remaining = remaining ?? 0,
            ResetAt = reset is null
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(reset.Value)
        };
    }

    private static int? ReadIntHeader(
        HttpResponseMessage response,
        string name)
    {
        return int.TryParse(ReadHeader(response, name), out var value)
            ? value
            : null;
    }

    private static long? ReadLongHeader(
        HttpResponseMessage response,
        string name)
    {
        return long.TryParse(ReadHeader(response, name), out var value)
            ? value
            : null;
    }

    private static string ReadHeader(
        HttpResponseMessage response,
        string name)
    {
        return response.Headers.TryGetValues(name, out var values)
            ? string.Join(",", values)
            : string.Empty;
    }

    private async Task<T?> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            relativeUrl,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                DisqusResponse<T>>(
                JsonOptions,
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
        using var response = await SendAsync(
            relativeUrl,
            cancellationToken);

        var result =
            await response.Content.ReadFromJsonAsync<
                DisqusPagedResponse<T>>(
                JsonOptions,
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
