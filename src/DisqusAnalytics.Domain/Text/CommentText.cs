using System.Net;
using System.Text.RegularExpressions;

namespace DisqusAnalytics.Domain.Text;

/// <summary>
/// Converte o HTML retornado pelo Disqus em texto simples.
/// A API devolve o comentário como HTML (parágrafos, links, quebras),
/// e as estatísticas de palavras precisam do texto puro.
/// </summary>
public static partial class CommentText
{
    /// <summary>
    /// Remove as tags, decodifica as entidades HTML (ex.: &amp;amp;)
    /// e normaliza os espaços em branco.
    /// </summary>
    public static string Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        // As tags são substituídas por um espaço para não unir
        // palavras de parágrafos diferentes ("<p>a</p><p>b</p>").
        var text = TagRegex().Replace(html, " ");

        text = WebUtility.HtmlDecode(text);

        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
