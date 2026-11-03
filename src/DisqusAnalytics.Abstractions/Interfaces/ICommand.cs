namespace DisqusAnalytics.Abstractions.Interfaces;

/// <summary>
/// Representa um comando executável pela aplicação.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Nome utilizado na linha de comando.
    /// Exemplo: sync, stats, export.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Descrição do comando.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executa o comando.
    /// </summary>
    /// <param name="arguments">
    /// Argumentos informados após o nome do comando.
    /// Exemplo: sync comments -> ["comments"].
    /// </param>
    Task ExecuteAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
