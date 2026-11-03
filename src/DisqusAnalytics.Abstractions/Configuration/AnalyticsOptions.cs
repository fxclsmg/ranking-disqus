namespace DisqusAnalytics.Abstractions.Configuration;

/// <summary>
/// Configurações de geração do relatório estatístico.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>
    /// Caminho do arquivo JSON gerado.
    /// A pasta docs é a publicada pelo GitHub Pages, por isso o JSON
    /// fica ao lado do index.html e pode ser lido pela página.
    /// </summary>
    public string OutputPath { get; set; } = "docs/statistics.json";

    /// <summary>
    /// Quantidade de autores exibidos no ranking.
    /// </summary>
    public int TopAuthors { get; set; } = 25;

    /// <summary>
    /// Quantidade de discussões exibidas no ranking.
    /// </summary>
    public int TopDiscussions { get; set; } = 25;

    /// <summary>
    /// Quantidade de palavras exibidas no ranking.
    /// </summary>
    public int TopWords { get; set; } = 100;

    /// <summary>
    /// Tamanho mínimo de uma palavra para entrar nas estatísticas.
    /// </summary>
    public int MinimumWordLength { get; set; } = 3;
}
