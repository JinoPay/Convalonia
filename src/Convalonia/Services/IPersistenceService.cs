using System.Threading;
using System.Threading.Tasks;

namespace Convalonia.Services;

/// <summary>
/// Base interface for persistence services
/// </summary>
/// <typeparam name="T">Type of entity to persist</typeparam>
public interface IPersistenceService<T> where T : class
{
    /// <summary>
    /// Save entity to persistent storage
    /// </summary>
    /// <param name="entity">Entity to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load entity from persistent storage
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<T?> LoadAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete entity from persistent storage
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if entity exists in persistent storage
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
