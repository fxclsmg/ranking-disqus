using DisqusAnalytics.Domain.Enums;

namespace DisqusAnalytics.Abstractions.Interfaces;

/// <summary>
/// Responsável por sincronizar os dados de um fórum
/// com a fonte de dados configurada.
/// </summary>
public interface ISynchronizationService
{
    /// <summary>
    /// Executa a sincronização dos dados dos alvos configurados.
    /// </summary>
    /// <param name="scope">
    /// Define se serão sincronizadas discussões, comentários ou ambos.
    /// </param>
    Task SynchronizeAsync(
        SyncScope scope = SyncScope.All,
        CancellationToken cancellationToken = default);
}
