namespace {ProjectName}.infrastructure.nhibernate.filtering;

/// <summary>
/// Filter operator class
/// </summary>
public class FilterOperator
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FilterOperator()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="fileldName"></param>
    /// <param name="values"></param>
    /// <param name="relationalOperatorType"></param>
    public FilterOperator(string fileldName, IEnumerable<string> values, string relationalOperatorType)
    {
        FieldName = fileldName;
        RelationalOperatorType = relationalOperatorType;
        Values = values.ToList();
    }

    /// <summary>
    /// Gets or sets the field name
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relational operator type
    /// </summary>
    public string RelationalOperatorType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the values
    /// </summary>
    public IList<string> Values { get; set; } = new List<string>();

}
