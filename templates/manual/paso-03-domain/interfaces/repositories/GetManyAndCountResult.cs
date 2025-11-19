namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Class to return the result for a paginated query with sorting capabilities.
/// Provides a container for collections of items along with pagination and sorting information.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class GetManyAndCountResult<T> : IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Default page size when no specific size is requested.
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 25;

    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the total count of records that match the query criteria.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based indexing).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the sorting criteria applied to the result set.
    /// Implements the IGetManyAndCountResultWithSorting interface.
    /// </summary>
    public SortingCriteria Sorting { get; set; }

    /// <summary>
    /// Constructor that initializes a new instance with the specified items, count, pagination, and sorting information.
    /// </summary>
    /// <param name="items">The collection of items for the current page.</param>
    /// <param name="count">The total number of records that match the query criteria.</param>
    /// <param name="pageNumber">The current page number (1-based indexing).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="sorting">The sorting criteria applied to the result set.</param>
    public GetManyAndCountResult(IEnumerable<T> items, long count, int pageNumber, int pageSize, SortingCriteria sorting)
    {
        Items = items;
        Count = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Sorting = sorting;
    }

    /// <summary>
    /// Default constructor that initializes a new instance with empty items and default values.
    /// The default values are:
    /// - Empty collection of items
    /// - Count set to 0
    /// - Page number set to 1
    /// - Page size set to DEFAULT_PAGE_SIZE (25)
    /// - Sorting criteria initialized with default values
    /// </summary>
    public GetManyAndCountResult()
    {
        Items = [];
        Count = 0;
        PageNumber = 1;
        PageSize = DEFAULT_PAGE_SIZE;
        Sorting = new SortingCriteria();
    }
}
