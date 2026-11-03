namespace DisqusAnalytics.Analytics.Text;

/// <summary>
/// Palavra extraída do texto de um comentário.
/// </summary>
/// <param name="Value">
/// Texto do token. Hashtags são mantidas com o "#" e no formato em que
/// foram escritas ("#BobaFree"); palavras comuns ficam em minúsculas.
/// </param>
/// <param name="IsHashtag">
/// True quando o token é uma hashtag (#palavra).
/// </param>
public readonly record struct WordToken(string Value, bool IsHashtag);
