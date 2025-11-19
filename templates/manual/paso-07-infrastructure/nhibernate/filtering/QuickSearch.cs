namespace MiProyecto.infrastructure.nhibernate.filtering;

/// <summary>
/// Quick search filter
/// </summary>
public class QuickSearch
{
    /// <summary>
    /// Gets or sets the quick search value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Get or sets the field names
    /// </summary>
    public IList<string> FieldNames { get; set; } = new List<string>();
}
