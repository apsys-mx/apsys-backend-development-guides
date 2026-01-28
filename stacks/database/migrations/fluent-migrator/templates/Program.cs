using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using {ProjectName}.migrations;

const string _run = "run";
const string _rollback = "rollback";

try
{
    // Read the command line parameters
    AnsiConsole.MarkupLine("Reading command line parameters...");
    CommandLineArgs parameter = [];
    if (!parameter.TryGetValue("cnn", out string? value))
        throw new ArgumentException("No [cnn] parameter received. You need pass the connection string in order to execute the migrations");

    // Create the service provider
    AnsiConsole.MarkupLine("[bold yellow]Connecting to database...[/]");
    string connectionStringValue = value;
    var serviceProvider = CreateServices(connectionStringValue);
    using var scope = serviceProvider.CreateScope();
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    // Check if the action is passed in the command line. If not, default to run
    if (!parameter.TryGetValue("action", out string? action) && string.IsNullOrEmpty(action))
        action = _run;

    // Execute the requested action
    if (action == _run)
    {
        AnsiConsole.Status()
            .Start("Start running migrations...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                ctx.Status("Running migrations...");
                UpdateDatabase(scope.ServiceProvider);
                runner.MigrateUp();
            });
        AnsiConsole.MarkupLine("All migrations are updated");
    }
    else if (action == _rollback)
    {
        AnsiConsole.Status()
            .Start("Start rolling back the last migration...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("blue"));
                ctx.Status("Rolling back migration...");
                var lastMigration = runner.MigrationLoader.LoadMigrations().LastOrDefault();
                var rollBackToVersion = lastMigration.Value.Version - 1;
                runner.MigrateDown(rollBackToVersion);
            });
        AnsiConsole.MarkupLine("Last transaction rolled back");

    }
    else
    {
        throw new ArgumentException("Invalid action. Please use 'run' or 'rollback'");
    }
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.UnknownError;
}

/// <summary>
/// Configure the dependency injection services
/// </sumamry>
static IServiceProvider CreateServices(string? connectionString)
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
            .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations())
        .AddLogging(lb => lb.AddFluentMigratorConsole())
        .BuildServiceProvider(false);
}

static void UpdateDatabase(IServiceProvider serviceProvider)
{
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
