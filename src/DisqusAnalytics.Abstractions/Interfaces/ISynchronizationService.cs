namespace DisqusAnalytics.Abstractions.Interfaces;

/// <summary>
/// Responsável por sincronizar os dados de um fórum
/// com a fonte de dados configurada.
/// </summary>
public interface ISynchronizationService
{
    /// <summary>
    /// Executa a sincronização dos dados.
    /// </summary>
    Task SynchronizeAsync(
        CancellationToken cancellationToken = default);
}
