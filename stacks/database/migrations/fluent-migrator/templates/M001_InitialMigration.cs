using FluentMigrator;

namespace {ProjectName}.migrations.migrations;

/// <summary>
/// Initial migration - creates base schema.
/// This is a template migration to verify the setup works correctly.
/// </summary>
[Migration(1)]
public class M001_InitialMigration : Migration
{
    private const string SchemaName = "public";

    public override void Up()
    {
        // Example: Create a simple table to verify migrations work
        // Replace this with your actual initial schema

        // Create.Table("example")
        //     .InSchema(SchemaName)
        //     .WithColumn("id").AsGuid().PrimaryKey()
        //     .WithColumn("name").AsString(100).NotNullable()
        //     .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        // For now, just execute a simple validation query
        Execute.Sql("SELECT 1;");
    }

    public override void Down()
    {
        // Revert the changes made in Up()

        // Delete.Table("example").InSchema(SchemaName);
    }
}
