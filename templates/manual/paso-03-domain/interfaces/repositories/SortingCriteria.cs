namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Class representing a sorting criteria
/// </summary>
public class SortingCriteria
{

    /// <summary>
    /// Gets or sets the name of the field to sort by.
    /// This property is used to specify the field name that will be used for sorting the results
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sorting criteria type.
    /// This property indicates whether the sorting should be done in ascending or descending order.
    /// </summary>
    public SortingCriteriaType Criteria { get; set; } = SortingCriteriaType.Ascending;

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy)
    {
        this.SortBy = sortBy;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy, SortingCriteriaType criteria)
    {
        this.SortBy = sortBy;
        this.Criteria = criteria;
    }
}

/// <summary>
/// The sorting criteria type enumeration
/// </summary>
public enum SortingCriteriaType
{
    /// <summary>
    /// Sort ascending
    /// </summary>
    Ascending = 1,

    /// <summary>
    /// Sort descending
    /// </summary>
    Descending = 2
}
