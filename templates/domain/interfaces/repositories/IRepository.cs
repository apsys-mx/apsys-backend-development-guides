namespace {ProjectName}.domain.interfaces.repositories;

/// <summary>
/// Defines a full repository implementation with both read and write operations.
/// This interface extends the read-only repository functionality by adding
/// methods for creating, updating, and deleting entities.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class, new()
{

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>The added entity, possibly with updated properties (like generated IDs)</returns>
    T Add(T item);

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddAsync(T item);

    /// <summary>
    /// Saves or updates an existing entity in the repository.
    /// If the entity has an ID that exists in the repository, it is updated;
    /// otherwise, a new entity is created.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>The saved entity, possibly with updated properties</returns>
    T Save(T item);

    /// <summary>
    /// Asynchronously saves or updates an existing entity in the repository.
    /// If the entity has an ID that exists in the repository, it is updated;
    /// otherwise, a new entity is created.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveAsync(T item);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    void Delete(T item);

    /// <summary>
    /// Asynchronously deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteAsync(T item);
}
