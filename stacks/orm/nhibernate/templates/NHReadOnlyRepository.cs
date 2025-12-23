using System.Linq.Expressions;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.infrastructure.nhibernate.filtering;
using System.Linq.Dynamic.Core;
using NHibernate;
using NHibernate.Linq;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Implementation of the read-only repository pattern using NHibernate ORM.
/// Provides methods for querying entities from a data store without modifying them.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
/// <param name="session">The NHibernate session used for database operations</param>
public class NHReadOnlyRepository<T, TKey>(ISession session) : IReadOnlyRepository<T, TKey> where T : class, new()
{
    /// <summary>
    /// The NHibernate session used for database operations.
    /// Protected to allow access from derived classes.
    /// </summary>
    protected internal readonly ISession _session = session;

    /// <summary>
    /// Counts the total number of entities in the repository.
    /// </summary>
    /// <returns>The total count of entities</returns>
    public int Count()
        => this._session.QueryOver<T>().RowCount();

    /// <summary>
    /// Counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <returns>The count of entities that match the query</returns>
    public int Count(Expression<Func<T, bool>> query)
        => this._session.Query<T>().Where(query).Count();

    /// <summary>
    /// Asynchronously counts the total number of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
    /// <returns>A task representing the asynchronous operation, with a result of the total count of entities</returns>

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => this._session.Query<T>().CountAsync(cancellationToken);

    /// <summary>
    /// Asynchronously counts the number of entities that match a specified query expression.
    /// This method allows for filtering entities based on a provided expression.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
    => this._session.Query<T>().Where(query).CountAsync(cancellationToken);

    /// <summary>
    /// Retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <returns>The entity with the specified identifier, or null if not found</returns>
    public T Get(TKey id)
        => this._session.Get<T>(id);

    /// <summary>
    /// Retrieves all entities from the repository.
    /// </summary>
    /// <returns>An enumerable collection of all entities in the repository</returns>
    public IEnumerable<T> Get()
        => this._session.Query<T>();

    /// <summary>
    /// Retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <returns>An enumerable collection of entities that match the query</returns>
    public IEnumerable<T> Get(Expression<Func<T, bool>> query)
        => this._session.Query<T>()
                .Where(query);

    /// <summary>
    /// Retrieves a paginated and sorted subset of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="page">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortingCriteria">The criteria to use for sorting the results</param>
    /// <returns>A paginated and sorted enumerable collection of entities that match the query</returns>
    public IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria)
        => this._session.Query<T>()
                .Where(query)
                .OrderBy(sortingCriteria.ToExpression())
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

    /// <summary>
    /// Asynchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity with the specified identifier, or null if not found</returns>
    public Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default)
        => this._session.GetAsync<T>(id, cancellationToken);

    /// <summary>
    /// Asynchronously retrieves all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of all entities</returns>
    public async Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default)
        => await this._session.Query<T>().ToListAsync(cancellationToken);

    /// <summary>
    /// Asynchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
        => await this._session.Query<T>()
                .Where(query)
                .ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieves a paginated result set along with the total count of items that match a string query.
    /// This method parses a string query to extract pagination, sorting, and filtering information,
    /// then applies them to retrieve the appropriate data.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A result object containing both the matching entities and the total count</returns>
    public GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute query
        var items = this.Get(expression, pageNumber, pageSize, sortingCriteria);
        var total = this.Count(expression);

        // Return results
        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    /// <summary>
    /// Asynchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// This method parses a string query to extract pagination, sorting, and filtering information,
    /// then applies them to retrieve the appropriate data.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>A task representing the asynchronous operation, containing a result object with both the matching entities and the total count</returns>
    public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute queries sequentially to avoid DataReader conversion issues
        var total = await this._session.Query<T>()
            .Where(expression)
            .CountAsync(cancellationToken);

        var items = await this._session.Query<T>()
            .OrderBy(sortingCriteria.ToExpression())
            .Where(expression)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return results
        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    /// <summary>
    /// Prepares a query by parsing the query string and extracting pagination, sorting, and filtering information.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A tuple containing the prepared expression, page number, page size, and sorting criteria</returns>
    private static (Expression<Func<T, bool>> expression, int pageNumber, int pageSize, SortingCriteria sortingCriteria) PrepareQuery(string? query, string defaultSorting)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
        QueryStringParser queryStringParser = new(queryString);

        // Get pagination info
        int pageNumber = queryStringParser.ParsePageNumber();
        int pageSize = queryStringParser.ParsePageSize();

        // Get sorting info
        Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
        SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
            ? SortingCriteriaType.Descending
            : SortingCriteriaType.Ascending;
        SortingCriteria sortingCriteria = new(sorting.By, directions);

        // Get filters
        IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
        QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
        var expression = FilterExpressionParser.ParsePredicate<T>(filters);
        if (quickSearch != null)
            expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

        return (expression, pageNumber, pageSize, sortingCriteria);
    }
}
