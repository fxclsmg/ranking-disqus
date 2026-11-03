namespace DisqusAnalytics.Abstractions.Reporting;

/// <summary>
/// Relatório estatístico completo, exportado em JSON para o frontend.
/// </summary>
public sealed class StatisticsReport
{
    /// <summary>
    /// Data/hora de geração do relatório.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Estatísticas por alvo (podcast).
    /// </summary>
    public IReadOnlyList<ShowStatistics> Shows { get; init; } = [];
}

/// <summary>
/// Estatísticas de um alvo (podcast).
/// </summary>
public sealed class ShowStatistics
{
    public string Name { get; init; } = string.Empty;

    public string Forum { get; init; } = string.Empty;

    public ShowTotals Totals { get; init; } = new();

    public IReadOnlyList<AuthorStatistics> TopAuthors { get; init; } = [];

    public IReadOnlyList<DiscussionStatistics> TopDiscussions { get; init; } = [];

    public IReadOnlyList<PeriodStatistics> CommentsByMonth { get; init; } = [];

    public IReadOnlyList<WordStatistics> TopWords { get; init; } = [];

    /// <summary>
    /// Palavras que apareceram uma única vez (mais raras), com as
    /// maiores primeiro.
    /// </summary>
    public IReadOnlyList<WordStatistics> RareWords { get; init; } = [];

    /// <summary>
    /// Distribuição do vocabulário por quantidade de letras.
    /// </summary>
    public IReadOnlyList<WordLengthStatistics> WordLengths { get; init; } = [];

    /// <summary>
    /// Hashtags usadas nos comentários ("#bobafree").
    /// </summary>
    public IReadOnlyList<HashtagStatistics> Hashtags { get; init; } = [];

    /// <summary>
    /// Emojis mais usados nos comentários.
    /// </summary>
    public IReadOnlyList<EmojiStatistics> Emojis { get; init; } = [];

    /// <summary>
    /// Renovação do vocabulário a cada ano.
    /// </summary>
    public IReadOnlyList<VocabularyYearStatistics> VocabularyByYear { get; init; } = [];

    /// <summary>
    /// Comentário mais longo do podcast.
    /// </summary>
    public CommentRecordStatistics? LongestComment { get; init; }

    /// <summary>
    /// Comentário mais curto do podcast.
    /// </summary>
    public CommentRecordStatistics? ShortestComment { get; init; }

    /// <summary>
    /// Autores que mais chegaram primeiro em um episódio.
    /// </summary>
    public IReadOnlyList<EdgeCommenterStatistics> FirstCommenters { get; init; } = [];

    /// <summary>
    /// Autores que mais fizeram o último comentário de um episódio.
    /// </summary>
    public IReadOnlyList<EdgeCommenterStatistics> LastCommenters { get; init; } = [];

    /// <summary>
    /// Pares de palavras mais usados ("bom dia", "melhor episódio").
    /// </summary>
    public IReadOnlyList<PhraseStatistics> TopBigrams { get; init; } = [];

    /// <summary>
    /// Quantidade de autores novos a cada ano, com retenção.
    /// </summary>
    public IReadOnlyList<NewAuthorStatistics> NewAuthorsByYear { get; init; } = [];

    /// <summary>
    /// Comentaristas que apareceram no ano mais recente, ordenados
    /// por quantidade de comentários.
    /// </summary>
    public IReadOnlyList<NewcomerStatistics> Newcomers { get; init; } = [];

    /// <summary>
    /// Comentários por dia da semana e hora.
    /// </summary>
    public IReadOnlyList<ActivityCell> ActivityByWeekdayHour { get; init; } = [];
}

/// <summary>
/// Números gerais de um alvo.
/// </summary>
public sealed class ShowTotals
{
    public int Discussions { get; init; }

    public int DiscussionsWithComments { get; init; }

    public int Comments { get; init; }

    public int Authors { get; init; }

    /// <summary>
    /// Autores que comentaram uma única vez.
    /// </summary>
    public int SingleCommentAuthors { get; init; }

    /// <summary>
    /// Participação percentual dos autores que comentaram uma vez.
    /// </summary>
    public double SingleCommentAuthorsShare { get; init; }

    /// <summary>
    /// Percentual dos comentários concentrado nos 10 maiores autores.
    /// </summary>
    public double TopTenAuthorsShare { get; init; }

    public int Characters { get; init; }

    public double AverageCharacters { get; init; }

    public double CommentsPerDiscussion { get; init; }

    /// <summary>
    /// Mediana de horas entre a publicação do episódio e o primeiro
    /// comentário.
    /// </summary>
    public double MedianFirstResponseHours { get; init; }

    /// <summary>
    /// Quantidade de palavras distintas usadas nos comentários.
    /// </summary>
    public int VocabularySize { get; init; }

    /// <summary>
    /// Quantidade de palavras usadas uma única vez.
    /// </summary>
    public int RareWordCount { get; init; }

    /// <summary>
    /// Quantidade de hashtags usadas nos comentários.
    /// </summary>
    public int HashtagCount { get; init; }

    /// <summary>
    /// Quantidade de emojis usados nos comentários.
    /// </summary>
    public int EmojiCount { get; init; }

    /// <summary>
    /// Comentários que usam pelo menos um emoji.
    /// </summary>
    public int CommentsWithEmoji { get; init; }

    /// <summary>
    /// Palavras que apareceram em um único ano.
    /// </summary>
    public int OneYearWords { get; init; }

    public double OneYearWordsShare { get; init; }

    /// <summary>
    /// Comentários cujo texto repete exatamente outro comentário.
    /// </summary>
    public int DuplicatedComments { get; init; }

    public double DuplicatedCommentsShare { get; init; }

    public int CommentsWithLinks { get; init; }

    public int CommentsWithImages { get; init; }

    public DateTimeOffset? FirstCommentAt { get; init; }

    public DateTimeOffset? LastCommentAt { get; init; }
}

/// <summary>
/// Ranking de autores.
/// </summary>
public sealed class AuthorStatistics
{
    public long AuthorId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string ProfileUrl { get; init; } = string.Empty;

    public int Comments { get; init; }

    public int Characters { get; init; }

    public double AverageCharacters { get; init; }

    public int Discussions { get; init; }

    /// <summary>
    /// Percentual dos episódios do podcast em que o autor comentou.
    /// </summary>
    public double EpisodeShare { get; init; }

    /// <summary>
    /// Participação percentual nos comentários do alvo.
    /// </summary>
    public double Share { get; init; }

    public DateTimeOffset FirstCommentAt { get; init; }

    public DateTimeOffset LastCommentAt { get; init; }
}

/// <summary>
/// Ranking de discussões (episódios) com mais comentários.
/// </summary>
public sealed class DiscussionStatistics
{
    public long Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Link { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public int Comments { get; init; }

    public int Authors { get; init; }
}

/// <summary>
/// Comentários agrupados por mês.
/// </summary>
public sealed class PeriodStatistics
{
    /// <summary>
    /// Período no formato yyyy-MM.
    /// </summary>
    public string Period { get; init; } = string.Empty;

    public int Comments { get; init; }

    public int Authors { get; init; }
}

/// <summary>
/// Ranking de palavras utilizadas nos comentários.
/// </summary>
public sealed class WordStatistics
{
    public string Word { get; init; } = string.Empty;

    public int Count { get; init; }

    /// <summary>
    /// Quantidade de autores diferentes que utilizaram a palavra.
    /// </summary>
    public int Authors { get; init; }
}

/// <summary>
/// Par de palavras consecutivas utilizado nos comentários.
/// </summary>
public sealed class PhraseStatistics
{
    public string Phrase { get; init; } = string.Empty;

    public int Count { get; init; }

    public int Authors { get; init; }
}

/// <summary>
/// Quantidade de palavras de um determinado comprimento.
/// </summary>
public sealed class WordLengthStatistics
{
    /// <summary>
    /// Quantidade de letras.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Palavras distintas com esse comprimento.
    /// </summary>
    public int Words { get; init; }

    /// <summary>
    /// Quantas vezes essas palavras apareceram no texto.
    /// </summary>
    public int Occurrences { get; init; }

    /// <summary>
    /// Quantas dessas palavras apareceram uma única vez.
    /// </summary>
    public int RareWords { get; init; }
}

/// <summary>
/// Hashtag utilizada nos comentários.
/// </summary>
public sealed class HashtagStatistics
{
    /// <summary>
    /// Hashtag com o "#", no formato em que foi escrita.
    /// </summary>
    public string Tag { get; init; } = string.Empty;

    public int Count { get; init; }

    public int Authors { get; init; }
}

/// <summary>
/// Emoji utilizado nos comentários.
/// </summary>
public sealed class EmojiStatistics
{
    public string Emoji { get; init; } = string.Empty;

    public int Count { get; init; }

    public int Authors { get; init; }
}

/// <summary>
/// Renovação do vocabulário em um ano.
/// </summary>
public sealed class VocabularyYearStatistics
{
    public int Year { get; init; }

    /// <summary>
    /// Palavras distintas usadas no ano.
    /// </summary>
    public int Words { get; init; }

    /// <summary>
    /// Palavras usadas pela primeira vez no ano.
    /// </summary>
    public int NewWords { get; init; }

    /// <summary>
    /// Palavras que apareceram no ano anterior e não apareceram neste.
    /// </summary>
    public int LostWords { get; init; }

    /// <summary>
    /// Palavras usadas apenas neste ano, as mais frequentes primeiro.
    /// </summary>
    public IReadOnlyList<WordStatistics> SeasonWords { get; init; } = [];
}

/// <summary>
/// Resumo de um ano: quantos autores chegaram, quantos voltaram e
/// quanto movimento o ano teve.
/// </summary>
public sealed class NewAuthorStatistics
{
    public int Year { get; init; }

    /// <summary>
    /// Autores que comentaram pela primeira vez no ano.
    /// </summary>
    public int NewAuthors { get; init; }

    /// <summary>
    /// Comentários feitos por esses autores no próprio ano de estreia.
    /// </summary>
    public int NewAuthorsComments { get; init; }

    /// <summary>
    /// Autores distintos que comentaram no ano (novos ou antigos).
    /// </summary>
    public int ActiveAuthors { get; init; }

    /// <summary>
    /// Comentários publicados no ano (de todos os autores).
    /// </summary>
    public int Comments { get; init; }

    /// <summary>
    /// Percentual dos autores que estrearam no ano e voltaram a
    /// comentar em algum ano posterior.
    /// </summary>
    public double RetentionShare { get; init; }
}

/// <summary>
/// Comentarista que apareceu no ano mais recente do podcast.
/// </summary>
public sealed class NewcomerStatistics
{
    public long AuthorId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string ProfileUrl { get; init; } = string.Empty;

    public DateTimeOffset FirstCommentAt { get; init; }

    public int Comments { get; init; }

    public int Discussions { get; init; }

    public int Characters { get; init; }
}

/// <summary>
/// Comentários em um dia da semana e hora do dia.
/// </summary>
public sealed class ActivityCell
{
    /// <summary>
    /// Dia da semana (0 = domingo ... 6 = sábado).
    /// </summary>
    public int Weekday { get; init; }

    /// <summary>
    /// Hora do dia (0 a 23).
    /// </summary>
    public int Hour { get; init; }

    public int Comments { get; init; }
}

/// <summary>
/// Recorde de comentário (mais longo ou mais curto).
/// </summary>
public sealed class CommentRecordStatistics
{
    public string AuthorName { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string ProfileUrl { get; init; } = string.Empty;

    /// <summary>
    /// Tamanho do comentário em caracteres de texto.
    /// </summary>
    public int Characters { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string DiscussionTitle { get; init; } = string.Empty;

    public string DiscussionLink { get; init; } = string.Empty;

    /// <summary>
    /// Início do texto do comentário.
    /// </summary>
    public string Excerpt { get; init; } = string.Empty;
}

/// <summary>
/// Autor que costuma abrir ou fechar as conversas de um episódio.
/// </summary>
public sealed class EdgeCommenterStatistics
{
    public long AuthorId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string ProfileUrl { get; init; } = string.Empty;

    /// <summary>
    /// Quantos episódios o autor abriu (ou fechou).
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Percentual dos episódios com comentários.
    /// </summary>
    public double Share { get; init; }
}
