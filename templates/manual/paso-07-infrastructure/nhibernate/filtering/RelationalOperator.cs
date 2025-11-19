namespace MiProyecto.infrastructure.nhibernate.filtering;

/// <summary>
/// Relational operator class
/// </summary>
static class RelationalOperator
{
    public const string Equal = "equal";
    public const string NotEqual = "not_equal";
    public const string Contains = "contains";
    public const string StartsWith = "starts_with";
    public const string EndsWith = "ends_with";
    public const string Between = "between";
    public const string GreaterThan = "greater_than";
    public const string GreaterThanOrEqual = "greater_or_equal_than";
    public const string LessThan = "less_than";
    public const string LessThanOrEqual = "less_or_equal_than";
}
