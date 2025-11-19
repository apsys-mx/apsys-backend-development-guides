namespace MiProyecto.infrastructure.nhibernate.filtering;

/// <summary>
/// Sorting criteria
/// </summary>
public class Sorting
{
    public Sorting() { }

    public Sorting(string by, string direction)
    {
        By = by;
        Direction = direction;
    }

    /// <summary>
    /// Sorting criteria by
    /// </summary>
    public string By
    {
        get => string.IsNullOrEmpty(_by) ? _by : _by.ToPascalCase();
        set => _by = value;
    }
    private string _by = string.Empty;

    /// <summary>
    /// Sorting direction
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Determine if the sorting criteria is valid
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    => !string.IsNullOrEmpty(By) && !string.IsNullOrEmpty(Direction);

}
