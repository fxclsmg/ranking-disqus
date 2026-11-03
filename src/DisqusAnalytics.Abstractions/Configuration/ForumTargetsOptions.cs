using System.Globalization;

namespace DisqusAnalytics.Abstractions.Configuration;

/// <summary>
/// Define um alvo de análise: um podcast/show específico dentro de um
/// fórum do Disqus.
/// O Disqus não separa os programas em fóruns diferentes: o Portal
/// Deviante, por exemplo, possui um único fórum (scicastpodcast) com
/// todos os episódios. Por isso o alvo é identificado por regras
/// aplicadas ao título da discussão.
/// Exemplo:
/// RPGuaxa =&gt; fórum scicastpodcast, títulos que contenham "RPGuaxa"
/// e que não contenham "Guaxaverso".
/// </summary>
public sealed class ForumTarget
{
    private static readonly CompareInfo Comparison =
        CultureInfo.InvariantCulture.CompareInfo;

    private static readonly CompareOptions MatchingOptions =
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Nome utilizado nos relatórios.
    /// Ex.: RPGuaxa.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome curto do fórum no Disqus.
    /// Ex.: scicastpodcast.
    /// </summary>
    public string Forum { get; set; } = string.Empty;

    /// <summary>
    /// Regras aplicadas ao título das discussões.
    /// </summary>
    public ForumTargetFilters Filters { get; set; } = new();

    /// <summary>
    /// Indica se uma discussão pertence a este alvo.
    /// </summary>
    public bool Matches(string? title)
    {
        var value = title ?? string.Empty;

        var hasFilter = Filters.TitleContains
            .Any(term => Contains(value, term));

        if (!hasFilter)
        {
            return false;
        }

        return !Filters.TitleExclude
            .Any(term => Contains(value, term));
    }

    private static bool Contains(string value, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        return Comparison.IndexOf(
            value,
            term.Trim(),
            MatchingOptions) >= 0;
    }
}

/// <summary>
/// Regras de seleção de um alvo.
/// </summary>
public sealed class ForumTargetFilters
{
    /// <summary>
    /// O título precisa conter pelo menos um destes termos.
    /// Lista vazia faz o alvo não selecionar nenhuma discussão.
    /// </summary>
    public List<string> TitleContains { get; set; } = [];

    /// <summary>
    /// Termos que excluem a discussão do alvo.
    /// Utilizado, por exemplo, para impedir que episódios do
    /// Guaxaverso (que citam "RPGuaxa" no título) entrem no
    /// ranking do RPGuaxa.
    /// </summary>
    public List<string> TitleExclude { get; set; } = [];
}

/// <summary>
/// Lista de alvos configurada na seção "Forums" do appsettings.
/// </summary>
public sealed class ForumTargetsOptions
{
    public List<ForumTarget> Forums { get; set; } = [];

    /// <summary>
    /// Alvos válidos, ignorando entradas sem fórum configurado.
    /// </summary>
    public IReadOnlyList<ForumTarget> GetValidTargets()
    {
        return Forums
            .Where(target => !string.IsNullOrWhiteSpace(target.Forum))
            .ToList();
    }
}
