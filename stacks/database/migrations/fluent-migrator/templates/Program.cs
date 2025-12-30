using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using {ProjectName}.migrations;
using {ProjectName}.migrations.migrations;

const string ActionRun = "run";
const string ActionRollback = "rollback";

try
{
    AnsiConsole.MarkupLine("[bold yellow]Reading command line parameters...[/]");
    CommandLineArgs cmdArgs = new();

    if (!cmdArgs.TryGetValue("cnn", out string? connectionString))
    {
        throw new ArgumentException(
            "Missing [cnn] parameter. Usage: dotnet run cnn=\"your_connection_string\" [action=run|rollback]");
    }

    AnsiConsole.MarkupLine("[bold yellow]Connecting to database...[/]");
    var serviceProvider = CreateServices(connectionString);

    using var scope = serviceProvider.CreateScope();
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    if (!cmdArgs.TryGetValue("action", out string? action) || string.IsNullOrEmpty(action))
    {
        action = ActionRun;
    }

    if (action == ActionRun)
    {
        AnsiConsole.Status()
            .Start("Running migrations...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                runner.MigrateUp();
            });
        AnsiConsole.MarkupLine("[bold green]All migrations applied successfully![/]");
    }
    else if (action == ActionRollback)
    {
        AnsiConsole.Status()
            .Start("Rolling back last migration...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("blue"));

                var lastMigration = runner.MigrationLoader.LoadMigrations().LastOrDefault();
                if (lastMigration.Value != null)
                {
                    var rollbackToVersion = lastMigration.Value.Version - 1;
                    runner.MigrateDown(rollbackToVersion);
                }
            });
        AnsiConsole.MarkupLine("[bold blue]Last migration rolled back successfully![/]");
    }
    else
    {
        throw new ArgumentException($"Invalid action '{action}'. Use 'run' or 'rollback'.");
    }

    return 0;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return 1;
}

static IServiceProvider CreateServices(string connectionString)
{
    return new ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(rb => rb
            // ============================================================
            // CONFIGURE DATABASE PROVIDER
            // See: stacks/database/{postgresql|sqlserver}/guides/setup.md
            // ============================================================

            // For PostgreSQL:
            .AddPostgres11_0()

            // For SQL Server:
            // .AddSqlServer()

            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(M001_InitialMigration).Assembly).For.Migrations())
        .AddLogging(lb => lb.AddFluentMigratorConsole())
        .BuildServiceProvider(false);
}
