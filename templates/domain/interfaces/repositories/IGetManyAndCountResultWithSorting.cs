namespace {ProjectName}.domain.interfaces.repositories;

/// <summary>
/// Interface for objects that provide sorting capabilities in paginated results.
/// This allows implementing classes to expose sorting criteria information.
/// </summary>
public interface IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Gets the sorting criteria applied to the result set.
    /// </summary>
    SortingCriteria Sorting { get; }
}
