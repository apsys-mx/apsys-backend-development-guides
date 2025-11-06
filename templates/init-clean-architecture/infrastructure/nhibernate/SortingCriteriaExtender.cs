using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Sorting expression extender
/// </summary>
public static class SortingCriteriaExtender
{

    /// <summary>
    /// Converts a SortingCriteria object to a string expression for sorting.
    /// This method generates a string representation of the sorting criteria,
    /// </summary>
    /// <param name="sort"></param>
    /// <returns></returns>
    public static string ToExpression(this SortingCriteria sort)
    {
        string orderExpression = sort.Criteria == SortingCriteriaType.Ascending ? $"{sort.SortBy}" : $"{sort.SortBy} descending";
        return orderExpression;
    }
}
