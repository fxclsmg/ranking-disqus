using System.Text.RegularExpressions;

namespace DisqusAnalytics.Analytics.Text;

/// <summary>
/// Divide o texto de um comentário em palavras e hashtags.
/// </summary>
public static partial class WordTokenizer
{
    /// <summary>
    /// Palavras comuns e hashtags são alternativas do mesmo padrão:
    /// o "#" só inicia hashtag quando aparece no texto.
    /// </summary>
    private const string Pattern =
        @"(?<word>[\p{L}]+)|(?<tag>#[\p{L}\p{N}_]+)";

    public static IEnumerable<WordToken> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (Match match in TokenRegex().Matches(text))
        {
            var hashtag = match.Groups["tag"];

            if (hashtag.Success)
            {
                yield return new WordToken(hashtag.Value, IsHashtag: true);

                continue;
            }

            var word = match.Groups["word"].Value.ToLowerInvariant();

            if (IsUrl(word))
            {
                continue;
            }

            yield return new WordToken(word, IsHashtag: false);
        }
    }

    private static bool IsUrl(string token)
    {
        return token.StartsWith("http", StringComparison.Ordinal)
            || token.StartsWith("www", StringComparison.Ordinal);
    }

    [GeneratedRegex(Pattern)]
    private static partial Regex TokenRegex();
}
