using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using DisqusAnalytics.Abstractions.Configuration;
using DisqusAnalytics.Abstractions.Interfaces;
using DisqusAnalytics.Abstractions.Reporting;
using DisqusAnalytics.Analytics.Text;
using DisqusAnalytics.Domain.Entities;
using DisqusAnalytics.Domain.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DisqusAnalytics.Analytics.Services;

/// <summary>
/// Calcula as estatísticas dos alvos configurados a partir dos dados
/// armazenados no banco: comentários, autores, discussões, palavras,
/// bigramas e distribuição temporal.
/// </summary>
public sealed class StatisticsService(
    IForumRepository forumRepository,
    IDiscussionRepository discussionRepository,
    ICommentRepository commentRepository,
    IOptions<ForumTargetsOptions> targetsOptions,
    IOptions<AnalyticsOptions> analyticsOptions,
    ILogger<StatisticsService> logger) : IStatisticsService
{
    /// <summary>
    /// Quantidade máxima de palavras raras listadas.
    /// </summary>
    private const int RareWordLimit = 40;

    /// <summary>
    /// Quantidade máxima de novos comentaristas listados.
    /// </summary>
    private const int NewcomerLimit = 10;

    /// <summary>
    /// Detecta letras repetidas em sequência ("maravilindoooo",
    /// "episóóóó"), típicas de ênfase e não de vocabulário.
    /// </summary>
    private static readonly Regex RepeatedCharacters = new(
        @"(.)\1{2,}",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Fuso de Brasília. O relatório é sempre apresentado em -03:00,
    /// independentemente do fuso do servidor.
    /// </summary>
    private static readonly TimeZoneInfo BrasiliaTimeZone =
        ResolveBrasiliaTimeZone();

    private static TimeZoneInfo ResolveBrasiliaTimeZone()
    {
        string[] ids =
        [
            "America/Sao_Paulo",
            "E. South America Standard Time"
        ];

        foreach (var id in ids)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        // Sem base de fusos disponível: o Brasil não tem horário de
        // verão desde 2019, então -03:00 é o esperado.
        return TimeZoneInfo.CreateCustomTimeZone(
            "BRT",
            TimeSpan.FromHours(-3),
            "BRT",
            "BRT");
    }

    private static DateTimeOffset ToBrasilia(DateTimeOffset value)
    {
        return TimeZoneInfo.ConvertTime(value, BrasiliaTimeZone);
    }

    public async Task<StatisticsReport> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var options = analyticsOptions.Value;

        var targets = targetsOptions.Value.GetValidTargets();

        if (targets.Count == 0)
        {
            logger.LogWarning(
                "Nenhum alvo configurado na seção 'Forums'. " +
                "O relatório será gerado vazio.");

            return new StatisticsReport
            {
                GeneratedAt = ToBrasilia(DateTimeOffset.UtcNow)
            };
        }

        var shows = new List<ShowStatistics>();

        // Vários alvos podem compartilhar o mesmo fórum (caso do
        // Portal Deviante). As discussões são carregadas uma única vez.
        foreach (var group in targets.GroupBy(
                     target => target.Forum,
                     StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var forum = await forumRepository.GetByShortNameAsync(
                group.Key,
                cancellationToken);

            if (forum is null)
            {
                logger.LogWarning(
                    "O fórum {Forum} ainda não foi sincronizado. " +
                    "Execute o comando sync antes de gerar as estatísticas.",
                    group.Key);

                continue;
            }

            var discussions = await discussionRepository.GetByForumIdAsync(
                forum.Id,
                cancellationToken: cancellationToken);

            foreach (var target in group)
            {
                var selected = discussions
                    .Where(discussion => target.Matches(discussion.Title))
                    .ToList();

                var comments = await commentRepository.GetByDiscussionIdsAsync(
                    selected
                        .Select(discussion => discussion.Id)
                        .ToList(),
                    includeAuthor: true,
                    cancellationToken: cancellationToken);

                var show = BuildShow(
                    target,
                    selected,
                    comments,
                    options);

                shows.Add(show);

                logger.LogInformation(
                    "{Show}: {Comments} comentários de {Authors} autores " +
                    "em {Discussions} discussões.",
                    show.Name,
                    show.Totals.Comments,
                    show.Totals.Authors,
                    show.Totals.Discussions);
            }
        }

        return new StatisticsReport
        {
            GeneratedAt = ToBrasilia(DateTimeOffset.UtcNow),
            Shows = shows
        };
    }

    public async Task<string> ExportAsync(
        StatisticsReport report,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(outputPath)
            ? analyticsOptions.Value.OutputPath
            : outputPath;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "O caminho do relatório não foi configurado.");
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(report, JsonOptions);

        await File.WriteAllTextAsync(
            path,
            json,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        // A página também precisa funcionar aberta direto do disco
        // (file://), situação em que o navegador bloqueia o fetch de
        // arquivos locais. O mesmo conteúdo é gravado como script, que
        // o <script src="statistics.js"> consegue carregar.
        var scriptPath = Path.ChangeExtension(path, ".js");

        var script =
            $"window.DISQUS_STATS = {json};{Environment.NewLine}";

        await File.WriteAllTextAsync(
            scriptPath,
            script,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        logger.LogInformation(
            "Relatório exportado para {Path} (e {ScriptPath}).",
            path,
            scriptPath);

        return path;
    }

    private static ShowStatistics BuildShow(
        ForumTarget target,
        IReadOnlyList<Discussion> discussions,
        IReadOnlyList<Comment> comments,
        AnalyticsOptions options)
    {
        // Comentários removidos e spam ficam de fora das estatísticas.
        var visible = comments
            .Where(comment => !comment.IsDeleted && !comment.IsSpam)
            .ToList();

        var discussionById = discussions
            .ToDictionary(discussion => discussion.Id);

        var commentsByDiscussion = visible
            .GroupBy(comment => comment.DiscussionId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(comment => comment.CreatedAt).ToList());

        var authors = BuildAuthors(visible, discussions.Count);

        var newestYear = visible.Count == 0
            ? ToBrasilia(DateTimeOffset.UtcNow).Year
            : ToBrasilia(visible.Max(comment => comment.CreatedAt)).Year;

        var newcomers = authors
            .Where(author => author.FirstCommentAt.Year == newestYear)
            .OrderByDescending(author => author.Comments)
            .ThenBy(author => author.Name, StringComparer.OrdinalIgnoreCase)
            .Take(NewcomerLimit)
            .Select(author => new NewcomerStatistics
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Username = author.Username,
                ProfileUrl = author.ProfileUrl,
                FirstCommentAt = author.FirstCommentAt,
                Comments = author.Comments,
                Discussions = author.Discussions,
                Characters = author.Characters
            })
            .ToList();

        var text = BuildTextStatistics(visible, options);

        var longestComment = BuildRecord(
            visible.MaxBy(comment => comment.CharacterCount),
            discussionById);

        var shortestComment = BuildRecord(
            visible.MinBy(comment => comment.CharacterCount),
            discussionById);

        var firstCommenters = BuildEdgeCommenters(
            commentsByDiscussion.Values.Select(list => list[0]),
            commentsByDiscussion.Count);

        var lastCommenters = BuildEdgeCommenters(
            commentsByDiscussion.Values.Select(list => list[^1]),
            commentsByDiscussion.Count);

        var firstResponseHours = BuildFirstResponseHours(
            discussions,
            commentsByDiscussion);

        var characters = visible.Sum(comment => comment.CharacterCount);

        var singleCommentAuthors = authors.Count(author => author.Comments == 1);

        var duplicatedComments = CountDuplicatedComments(visible);

        return new ShowStatistics
        {
            Name = target.Name,
            Forum = target.Forum,
            Totals = new ShowTotals
            {
                Discussions = discussions.Count,
                DiscussionsWithComments = commentsByDiscussion.Count,
                Comments = visible.Count,
                Authors = authors.Count,
                SingleCommentAuthors = singleCommentAuthors,
                SingleCommentAuthorsShare = visible.Count == 0
                    ? 0
                    : Math.Round(
                        singleCommentAuthors * 100.0 / authors.Count,
                        1),
                TopTenAuthorsShare = Math.Round(
                    authors
                        .Take(10)
                        .Sum(author => author.Share),
                    1),
                Characters = characters,
                AverageCharacters = visible.Count == 0
                    ? 0
                    : Math.Round((double)characters / visible.Count, 1),
                CommentsPerDiscussion = discussions.Count == 0
                    ? 0
                    : Math.Round(
                        (double)visible.Count / discussions.Count,
                        1),
                MedianFirstResponseHours = Math.Round(
                    Median(firstResponseHours),
                    1),
                VocabularySize = text.VocabularySize,
                RareWordCount = text.RareWordCount,
                HashtagCount = text.HashtagCount,
                EmojiCount = text.EmojiCount,
                CommentsWithEmoji = text.CommentsWithEmoji,
                OneYearWords = text.OneYearWords,
                OneYearWordsShare = text.VocabularySize == 0
                    ? 0
                    : Math.Round(
                        text.OneYearWords * 100.0 / text.VocabularySize,
                        1),
                DuplicatedComments = duplicatedComments,
                DuplicatedCommentsShare = visible.Count == 0
                    ? 0
                    : Math.Round(
                        duplicatedComments * 100.0 / visible.Count,
                        1),
                CommentsWithLinks = visible.Count(comment =>
                    comment.Message.Contains(
                        "<a ",
                        StringComparison.OrdinalIgnoreCase)),
                CommentsWithImages = visible.Count(comment =>
                    comment.Message.Contains(
                        "<img",
                        StringComparison.OrdinalIgnoreCase)),
                FirstCommentAt = visible.Count == 0
                    ? null
                    : ToBrasilia(visible.Min(comment => comment.CreatedAt)),
                LastCommentAt = visible.Count == 0
                    ? null
                    : ToBrasilia(visible.Max(comment => comment.CreatedAt))
            },
            TopAuthors = authors.Take(options.TopAuthors).ToList(),
            TopDiscussions = BuildDiscussions(
                visible,
                discussionById,
                options),
            CommentsByMonth = BuildMonthly(visible),
            TopWords = text.TopWords,
            RareWords = text.RareWords,
            WordLengths = text.WordLengths,
            Hashtags = text.Hashtags,
            Emojis = text.Emojis,
            VocabularyByYear = text.VocabularyByYear,
            LongestComment = longestComment,
            ShortestComment = shortestComment,
            FirstCommenters = firstCommenters,
            LastCommenters = lastCommenters,
            TopBigrams = text.TopBigrams,
            NewAuthorsByYear = BuildNewAuthorsByYear(visible, authors),
            Newcomers = newcomers,
            ActivityByWeekdayHour = BuildActivity(visible)
        };
    }

    private static IReadOnlyList<AuthorStatistics> BuildAuthors(
        IReadOnlyList<Comment> comments,
        int episodeCount)
    {
        var total = comments.Count;

        var authors = comments
            .GroupBy(comment => comment.AuthorId)
            .Select(group =>
            {
                var first = group.First();
                var count = group.Count();
                var characters = group.Sum(comment => comment.CharacterCount);
                var episodes = group
                    .Select(comment => comment.DiscussionId)
                    .Distinct()
                    .Count();

                return new AuthorStatistics
                {
                    AuthorId = group.Key,
                    Name = string.IsNullOrWhiteSpace(first.Author?.Name)
                        ? "Anônimo"
                        : first.Author!.Name,
                    Username = first.Author?.Username ?? string.Empty,
                    ProfileUrl = first.Author?.ProfileUrl ?? string.Empty,
                    Comments = count,
                    Characters = characters,
                    AverageCharacters = Math.Round(
                        (double)characters / count,
                        1),
                    Discussions = episodes,
                    EpisodeShare = episodeCount == 0
                        ? 0
                        : Math.Round(episodes * 100.0 / episodeCount, 1),
                    Share = total == 0
                        ? 0
                        : Math.Round(count * 100.0 / total, 1),
                    FirstCommentAt = ToBrasilia(
                        group.Min(comment => comment.CreatedAt)),
                    LastCommentAt = ToBrasilia(
                        group.Max(comment => comment.CreatedAt))
                };
            })
            .OrderByDescending(author => author.Comments)
            .ThenBy(author => author.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return authors;
    }

    private static IReadOnlyList<DiscussionStatistics> BuildDiscussions(
        IReadOnlyList<Comment> comments,
        IReadOnlyDictionary<long, Discussion> discussions,
        AnalyticsOptions options)
    {
        return comments
            .GroupBy(comment => comment.DiscussionId)
            .Select(group =>
            {
                discussions.TryGetValue(group.Key, out var discussion);

                return new DiscussionStatistics
                {
                    Id = group.Key,
                    Title = discussion?.Title ?? string.Empty,
                    Link = discussion?.Link ?? string.Empty,
                    CreatedAt = discussion is null
                        ? DateTimeOffset.MinValue
                        : ToBrasilia(discussion.CreatedAt),
                    Comments = group.Count(),
                    Authors = group
                        .Select(comment => comment.AuthorId)
                        .Distinct()
                        .Count()
                };
            })
            .OrderByDescending(item => item.Comments)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(options.TopDiscussions)
            .ToList();
    }

    private static IReadOnlyList<PeriodStatistics> BuildMonthly(
        IReadOnlyList<Comment> comments)
    {
        return comments
            .GroupBy(comment => new
            {
                ToBrasilia(comment.CreatedAt).Year,
                ToBrasilia(comment.CreatedAt).Month
            })
            .Select(group => new PeriodStatistics
            {
                Period = $"{group.Key.Year:0000}-{group.Key.Month:00}",
                Comments = group.Count(),
                Authors = group
                    .Select(comment => comment.AuthorId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(item => item.Period, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Calcula palavras, bigramas e vocabulário em uma única passada
    /// pelo texto dos comentários.
    /// </summary>
    private static TextStatisticsResult BuildTextStatistics(
        IReadOnlyList<Comment> comments,
        AnalyticsOptions options)
    {
        var words = new Dictionary<string, WordCounter>(StringComparer.Ordinal);
        var phrases = new Dictionary<string, WordCounter>(StringComparer.Ordinal);
        var hashtags = new Dictionary<string, WordCounter>(
            StringComparer.OrdinalIgnoreCase);

        var emojis = new Dictionary<string, WordCounter>(StringComparer.Ordinal);

        // Em quais anos cada palavra apareceu (renovação do vocabulário).
        var wordYears = new Dictionary<string, HashSet<int>>(
            StringComparer.Ordinal);

        var hashtagCount = 0;
        var emojiCount = 0;
        var commentsWithEmoji = 0;

        foreach (var comment in comments)
        {
            var text = CommentText.Clean(comment.Message);
            var year = ToBrasilia(comment.CreatedAt).Year;

            var tokens = WordTokenizer
                .Tokenize(text)
                .ToList();

            var commentEmojis = EmojiText.Extract(text).ToList();

            if (commentEmojis.Count > 0)
            {
                commentsWithEmoji++;
            }

            foreach (var emoji in commentEmojis)
            {
                emojiCount++;

                if (!emojis.TryGetValue(emoji, out var emojiCounter))
                {
                    emojiCounter = new WordCounter();

                    emojis.Add(emoji, emojiCounter);
                }

                emojiCounter.Add(comment.AuthorId);
            }

            // Tokens comuns na ordem em que aparecem, usados nos bigramas.
            var plain = new List<string>(tokens.Count);

            foreach (var token in tokens)
            {
                if (token.IsHashtag)
                {
                    hashtagCount++;

                    if (!hashtags.TryGetValue(
                            token.Value,
                            out var hashtagCounter))
                    {
                        hashtagCounter = new WordCounter();

                        hashtags.Add(token.Value, hashtagCounter);
                    }

                    hashtagCounter.Add(comment.AuthorId);

                    continue;
                }

                plain.Add(token.Value);

                if (!IsCountableWord(token.Value, options))
                {
                    continue;
                }

                if (!words.TryGetValue(token.Value, out var counter))
                {
                    counter = new WordCounter();

                    words.Add(token.Value, counter);
                }

                counter.Add(comment.AuthorId);

                if (!wordYears.TryGetValue(token.Value, out var years))
                {
                    years = [];

                    wordYears.Add(token.Value, years);
                }

                years.Add(year);
            }

            for (var index = 0; index + 1 < plain.Count; index++)
            {
                var first = plain[index];
                var second = plain[index + 1];

                // Palavras de ligação não formam bigramas úteis
                // ("de que", "o episódio").
                if (first.Length < 2
                    || second.Length < 2
                    || StopWords.IsLinker(first)
                    || StopWords.IsLinker(second))
                {
                    continue;
                }

                var phrase = $"{first} {second}";

                if (!phrases.TryGetValue(phrase, out var counter))
                {
                    counter = new WordCounter();

                    phrases.Add(phrase, counter);
                }

                counter.Add(comment.AuthorId);
            }
        }

        var topWords = words
            .OrderByDescending(item => item.Value.Count)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(options.TopWords)
            .Select(ToWordStatistics)
            .ToList();

        // Palavras mais raras: apareceram uma única vez. As maiores
        // vêm primeiro porque são as mais curiosas. Hashtags não
        // entram aqui (têm painel próprio).
        var rare = words
            .Where(item => item.Value.Count == 1 && item.Key.Length >= 6)
            .OrderByDescending(item => item.Key.Length)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(RareWordLimit)
            .Select(ToWordStatistics)
            .ToList();

        var topBigrams = phrases
            .OrderByDescending(item => item.Value.Count)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(20)
            .Select(item => new PhraseStatistics
            {
                Phrase = item.Key,
                Count = item.Value.Count,
                Authors = item.Value.Authors
            })
            .ToList();

        // Distribuição por comprimento (exata: o frontend agrupa em
        // faixas como 3-5, 6-10, 11-15 e 16+).
        var lengths = words
            .GroupBy(item => item.Key.Length)
            .OrderBy(group => group.Key)
            .Select(group => new WordLengthStatistics
            {
                Length = group.Key,
                Words = group.Count(),
                Occurrences = group.Sum(item => item.Value.Count),
                RareWords = group.Count(item => item.Value.Count == 1)
            })
            .ToList();

        // Hashtags numéricas (#100) são marcação de episódio e ficam
        // de fora do painel.
        var hashtagList = hashtags
            .Where(item => item.Key.Any(char.IsLetter))
            .OrderByDescending(item => item.Value.Count)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .Select(item => new HashtagStatistics
            {
                Tag = item.Key,
                Count = item.Value.Count,
                Authors = item.Value.Authors
            })
            .ToList();

        var emojiList = emojis
            .OrderByDescending(item => item.Value.Count)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(24)
            .Select(item => new EmojiStatistics
            {
                Emoji = item.Key,
                Count = item.Value.Count,
                Authors = item.Value.Authors
            })
            .ToList();

        var vocabularyByYear = BuildVocabularyByYear(wordYears, words);

        return new TextStatisticsResult(
            topWords,
            rare,
            words.Count,
            words.Count(item => item.Value.Count == 1),
            topBigrams,
            lengths,
            hashtagList,
            hashtagCount,
            emojiList,
            emojiCount,
            commentsWithEmoji,
            vocabularyByYear,
            wordYears.Count(item => item.Value.Count == 1));
    }

    /// <summary>
    /// Calcula quantas palavras estavam em uso a cada ano, quantas
    /// estrearam e quantas desapareceram em relação ao ano anterior.
    /// </summary>
    private static IReadOnlyList<VocabularyYearStatistics> BuildVocabularyByYear(
        IReadOnlyDictionary<string, HashSet<int>> wordYears,
        IReadOnlyDictionary<string, WordCounter> words)
    {
        if (wordYears.Count == 0)
        {
            return [];
        }

        var years = wordYears
            .SelectMany(item => item.Value)
            .Distinct()
            .OrderBy(year => year)
            .ToList();

        var result = new List<VocabularyYearStatistics>(years.Count);

        for (var index = 0; index < years.Count; index++)
        {
            var year = years[index];
            var previous = index == 0 ? (int?)null : years[index - 1];

            var wordsOfYear = wordYears
                .Where(item => item.Value.Contains(year))
                .ToList();

            var lostWords = previous is null
                ? 0
                : wordYears.Count(item =>
                    item.Value.Contains(previous.Value)
                    && !item.Value.Contains(year));

            // Palavras que só existiram neste ano, as mais usadas
            // primeiro: as "gírias da temporada".
            var season = wordsOfYear
                .Where(item => item.Value.Count == 1)
                .OrderByDescending(item => words[item.Key].Count)
                .ThenBy(item => item.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(item => new WordStatistics
                {
                    Word = item.Key,
                    Count = words[item.Key].Count,
                    Authors = words[item.Key].Authors
                })
                .ToList();

            result.Add(new VocabularyYearStatistics
            {
                Year = year,
                Words = wordsOfYear.Count,
                NewWords = wordsOfYear.Count(item =>
                    item.Value.Min() == year),
                LostWords = lostWords,
                SeasonWords = season
            });
        }

        return result;
    }

    private static WordStatistics ToWordStatistics(
        KeyValuePair<string, WordCounter> item)
    {
        return new WordStatistics
        {
            Word = item.Key,
            Count = item.Value.Count,
            Authors = item.Value.Authors
        };
    }

    /// <summary>
    /// Monta a evolução anual da comunidade: quantos autores estrearam,
    /// quanto comentaram no ano de estreia, quantos voltaram depois e
    /// quantos autores estiveram ativos no ano.
    /// </summary>
    private static IReadOnlyList<NewAuthorStatistics> BuildNewAuthorsByYear(
        IReadOnlyList<Comment> comments,
        IReadOnlyList<AuthorStatistics> authors)
    {
        if (authors.Count == 0)
        {
            return [];
        }

        var commentsByYear = comments
            .GroupBy(comment => ToBrasilia(comment.CreatedAt).Year)
            .ToDictionary(group => group.Key, group => group.Count());

        var firstYearByAuthor = authors.ToDictionary(
            author => author.AuthorId,
            author => author.FirstCommentAt.Year);

        // Comentários feitos no próprio ano de estreia do autor.
        var newcomerCommentsByYear = new Dictionary<int, int>();

        foreach (var comment in comments)
        {
            var year = ToBrasilia(comment.CreatedAt).Year;

            if (!firstYearByAuthor.TryGetValue(
                    comment.AuthorId,
                    out var authorFirstYear)
                || authorFirstYear != year)
            {
                continue;
            }

            newcomerCommentsByYear[year] =
                newcomerCommentsByYear.GetValueOrDefault(year) + 1;
        }

        var cohortByYear = authors
            .GroupBy(author => author.FirstCommentAt.Year)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Autores que comentaram no ano (exato, sem aproximar por
        // intervalo entre a primeira e a última aparição).
        var activeByYear = comments
            .GroupBy(comment => ToBrasilia(comment.CreatedAt).Year)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(comment => comment.AuthorId)
                    .Distinct()
                    .Count());

        var firstYear = authors.Min(author => author.FirstCommentAt.Year);

        var lastYear = authors.Max(author => Math.Max(
            author.FirstCommentAt.Year,
            author.LastCommentAt.Year));

        var result = new List<NewAuthorStatistics>(lastYear - firstYear + 1);

        for (var year = firstYear; year <= lastYear; year++)
        {
            cohortByYear.TryGetValue(year, out var cohort);

            var newAuthors = cohort?.Count ?? 0;

            var retained = cohort?.Count(
                author => author.LastCommentAt.Year > year) ?? 0;

            result.Add(new NewAuthorStatistics
            {
                Year = year,
                NewAuthors = newAuthors,
                NewAuthorsComments =
                    newcomerCommentsByYear.GetValueOrDefault(year),
                ActiveAuthors = activeByYear.GetValueOrDefault(year),
                Comments = commentsByYear.GetValueOrDefault(year),
                RetentionShare = newAuthors == 0
                    ? 0
                    : Math.Round(retained * 100.0 / newAuthors, 1)
            });
        }

        return result;
    }

    private static IReadOnlyList<ActivityCell> BuildActivity(
        IReadOnlyList<Comment> comments)
    {
        return comments
            .GroupBy(comment => new
            {
                Weekday = (int)ToBrasilia(comment.CreatedAt).DayOfWeek,
                ToBrasilia(comment.CreatedAt).Hour
            })
            .Select(group => new ActivityCell
            {
                Weekday = group.Key.Weekday,
                Hour = group.Key.Hour,
                Comments = group.Count()
            })
            .OrderBy(cell => cell.Weekday)
            .ThenBy(cell => cell.Hour)
            .ToList();
    }

    /// <summary>
    /// Monta o recorde (mais longo ou mais curto) com autor, data e
    /// episódio.
    /// </summary>
    private static CommentRecordStatistics? BuildRecord(
        Comment? comment,
        IReadOnlyDictionary<long, Discussion> discussions)
    {
        if (comment is null)
        {
            return null;
        }

        discussions.TryGetValue(comment.DiscussionId, out var discussion);

        return new CommentRecordStatistics
        {
            AuthorName = string.IsNullOrWhiteSpace(comment.Author?.Name)
                ? "Anônimo"
                : comment.Author!.Name,
            Username = comment.Author?.Username ?? string.Empty,
            ProfileUrl = comment.Author?.ProfileUrl ?? string.Empty,
            Characters = comment.CharacterCount,
            CreatedAt = ToBrasilia(comment.CreatedAt),
            DiscussionTitle = discussion?.Title ?? string.Empty,
            DiscussionLink = discussion?.Link ?? string.Empty,
            Excerpt = BuildExcerpt(comment.Message)
        };
    }

    private static string BuildExcerpt(string? message)
    {
        const int limit = 220;

        var text = CommentText.Clean(message);

        return text.Length <= limit
            ? text
            : string.Concat(text.AsSpan(0, limit).TrimEnd(), "…");
    }

    /// <summary>
    /// Conta quantas vezes cada autor abriu (ou fechou) um episódio.
    /// </summary>
    private static IReadOnlyList<EdgeCommenterStatistics> BuildEdgeCommenters(
        IEnumerable<Comment> comments,
        int discussionsWithComments)
    {
        var counters = new Dictionary<long, (Comment Comment, int Count)>();

        foreach (var comment in comments)
        {
            counters.TryGetValue(comment.AuthorId, out var entry);

            counters[comment.AuthorId] = (entry.Comment ?? comment, entry.Count + 1);
        }

        return counters
            .Select(item => new EdgeCommenterStatistics
            {
                AuthorId = item.Key,
                Name = string.IsNullOrWhiteSpace(item.Value.Comment.Author?.Name)
                    ? "Anônimo"
                    : item.Value.Comment.Author!.Name,
                Username = item.Value.Comment.Author?.Username ?? string.Empty,
                ProfileUrl = item.Value.Comment.Author?.ProfileUrl ?? string.Empty,
                Count = item.Value.Count,
                Share = discussionsWithComments == 0
                    ? 0
                    : Math.Round(
                        item.Value.Count * 100.0 / discussionsWithComments,
                        1)
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Mediana de horas entre a criação do episódio e o primeiro
    /// comentário.
    /// </summary>
    private static IReadOnlyList<double> BuildFirstResponseHours(
        IReadOnlyList<Discussion> discussions,
        IReadOnlyDictionary<long, List<Comment>> commentsByDiscussion)
    {
        var hours = new List<double>();

        foreach (var discussion in discussions)
        {
            if (!commentsByDiscussion.TryGetValue(discussion.Id, out var list)
                || list.Count == 0)
            {
                continue;
            }

            var first = list[0].CreatedAt;

            hours.Add(Math.Max(0, (first - discussion.CreatedAt).TotalHours));
        }

        return hours;
    }

    /// <summary>
    /// Conta comentários cujo texto normalizado repete um comentário
    /// anterior (boilerplate, cópia, resposta padrão).
    /// </summary>
    private static int CountDuplicatedComments(
        IReadOnlyList<Comment> comments)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var duplicated = 0;

        foreach (var comment in comments)
        {
            var normalized = CommentText
                .Clean(comment.Message)
                .ToLowerInvariant();

            if (!seen.Add(normalized))
            {
                duplicated++;
            }
        }

        return duplicated;
    }

    private static bool IsCountableWord(
        string token,
        AnalyticsOptions options)
    {
        return token.Length >= options.MinimumWordLength
            && !StopWords.Contains(token)
            && !IsRepeatedNoise(token);
    }

    /// <summary>
    /// Descarta palavras formadas pela repetição de poucas letras,
    /// como "kkkkkk", "hahaha" e "maravilindooooo".
    /// </summary>
    private static bool IsRepeatedNoise(string token)
    {
        if (token.Length >= 5 && token.Distinct().Count() <= 2)
        {
            return true;
        }

        return RepeatedCharacters.IsMatch(token);
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values
            .OrderBy(value => value)
            .ToList();

        var middle = ordered.Count / 2;

        return ordered.Count % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2;
    }

    private sealed record TextStatisticsResult(
        IReadOnlyList<WordStatistics> TopWords,
        IReadOnlyList<WordStatistics> RareWords,
        int VocabularySize,
        int RareWordCount,
        IReadOnlyList<PhraseStatistics> TopBigrams,
        IReadOnlyList<WordLengthStatistics> WordLengths,
        IReadOnlyList<HashtagStatistics> Hashtags,
        int HashtagCount,
        IReadOnlyList<EmojiStatistics> Emojis,
        int EmojiCount,
        int CommentsWithEmoji,
        IReadOnlyList<VocabularyYearStatistics> VocabularyByYear,
        int OneYearWords);

    /// <summary>
    /// Acumula as ocorrências de uma palavra (ou bigrama) e quantos
    /// autores diferentes a utilizaram.
    /// </summary>
    private sealed class WordCounter
    {
        private readonly HashSet<long> _authors = [];

        public int Count { get; private set; }

        public int Authors => _authors.Count;

        public void Add(long authorId)
        {
            Count++;

            _authors.Add(authorId);
        }
    }
}
