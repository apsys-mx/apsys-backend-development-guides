using FluentMigrator;

namespace {ProjectName}.migrations;

[Migration(1)]
public class M001Sandbox : Migration
{
    public override void Down()
    {
        Execute.Sql("DROP EXTENSION IF EXISTS unaccent;");
    }

    public override void Up()
    {
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
    }
}
