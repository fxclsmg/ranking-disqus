using DisqusAnalytics.Abstractions.Reporting;

namespace DisqusAnalytics.Abstractions.Interfaces;

/// <summary>
/// Gera as estatísticas a partir dos dados armazenados e exporta
/// o resultado em JSON.
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Calcula as estatísticas de todos os alvos configurados.
    /// </summary>
    Task<StatisticsReport> GenerateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grava o relatório em disco e retorna o caminho absoluto.
    /// </summary>
    /// <param name="report">Relatório gerado.</param>
    /// <param name="outputPath">
    /// Caminho de saída. Quando nulo, usa o valor configurado.
    /// </param>
    Task<string> ExportAsync(
        StatisticsReport report,
        string? outputPath = null,
        CancellationToken cancellationToken = default);
}
