using FluentMigrator.Runner.VersionTableInfo;

namespace {ProjectName}.migrations;

/// <summary>
/// Configures the table that FluentMigrator uses to track applied migrations.
/// </summary>
public class CustomVersionTableMetaData : IVersionTableMetaData
{
    public static string SchemaNameValue => "public";

    public required object ApplicationContext { get; set; }

    /// <summary>
    /// Whether FluentMigrator should create the schema if it doesn't exist.
    /// </summary>
    public bool OwnsSchema => true;

    /// <summary>
    /// Schema name where the version table will be created.
    /// </summary>
    public string SchemaName => SchemaNameValue;

    /// <summary>
    /// Name of the table that tracks applied migrations.
    /// </summary>
    public string TableName => "versioninfo";

    /// <summary>
    /// Name of the column that stores the version number.
    /// </summary>
    public string ColumnName => "version";

    /// <summary>
    /// Name of the unique index on the version column.
    /// </summary>
    public string UniqueIndexName => "uc_version";

    /// <summary>
    /// Name of the column that stores the application date.
    /// </summary>
    public string AppliedOnColumnName => "appliedon";

    /// <summary>
    /// Name of the column that stores the migration description.
    /// </summary>
    public string DescriptionColumnName => "description";

    /// <summary>
    /// Whether the table should have a primary key.
    /// false = only unique index (more efficient for this use case)
    /// </summary>
    public bool CreateWithPrimaryKey => false;
}
