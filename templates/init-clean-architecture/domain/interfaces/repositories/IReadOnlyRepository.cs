using System.Linq.Expressions;

namespace {ProjectName}.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for retrieving entities from a data store.
/// This interface provides both synchronous and asynchronous methods for querying data without modification capabilities.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
public interface IReadOnlyRepository<T, TKey> where T : class, new()
{
    #region Synchronous Methods

    /// <summary>
    /// Synchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <returns>The entity with the specified identifier, or null if not found</returns>
    T Get(TKey id);

    /// <summary>
    /// Synchronously retrieves all entities from the repository.
    /// </summary>
    /// <returns>An enumerable collection of all entities in the repository</returns>
    IEnumerable<T> Get();

    /// <summary>
    /// Synchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <returns>An enumerable collection of entities that match the query</returns>
    IEnumerable<T> Get(Expression<Func<T, bool>> query);

    /// <summary>
    /// Retrieves a paginated subset of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="page">The 0-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A paginated enumerable collection of entities that match the query</returns>
    IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria);

    /// <summary>
    /// Synchronously counts the total number of entities in the repository.
    /// </summary>
    /// <returns>The total count of entities</returns>
    int Count();

    /// <summary>
    /// Synchronously counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <returns>The count of entities that match the query</returns>
    int Count(Expression<Func<T, bool>> query);

    /// <summary>
    /// Synchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query to filter the entities</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A result object containing both the entities and the total count</returns>
    GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting);

    #endregion

    #region Asynchronous Methods

    /// <summary>
    /// Asynchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity with the specified identifier, or null if not found</returns>
    Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of all entities</returns>
    Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of entities that match the query</returns>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the total number of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count of entities that match the query</returns>
    Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query to filter the entities</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a result object with both the entities and the total count</returns>
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default);

    #endregion

}
