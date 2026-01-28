using Spectre.Console;
using System.Data;
using System.Text;
using {ProjectName}.scenarios;

Console.OutputEncoding = Encoding.UTF8;

AnsiConsole.MarkupLine("[blue]========================================[/]");
AnsiConsole.MarkupLine("[blue]  Scenario Builder - Starting...[/]");
AnsiConsole.MarkupLine("[blue]========================================[/]");

// Step 1: Validate command line parameters
AnsiConsole.MarkupLine("\n[yellow]Step 1:[/] Reading command line parameters...");
CommandLineArgs parameter = [];

string? connectionStringValue = null;
string? outputValue = null;

try
{
    if (!parameter.TryGetValue("cnn", out connectionStringValue) || string.IsNullOrWhiteSpace(connectionStringValue))
    {
        AnsiConsole.MarkupLine("[red]ERROR:[/] Missing or empty [bold]/cnn[/] parameter.");
        AnsiConsole.MarkupLine("[grey]Usage: dotnet <assembly>.dll /cnn:\"<connection-string>\" /output:\"<output-folder>\"[/]");
        return (int)ExitCode.InvalidParameters;
    }

    if (!parameter.TryGetValue("output", out outputValue) || string.IsNullOrWhiteSpace(outputValue))
    {
        AnsiConsole.MarkupLine("[red]ERROR:[/] Missing or empty [bold]/output[/] parameter.");
        AnsiConsole.MarkupLine("[grey]Usage: dotnet <assembly>.dll /cnn:\"<connection-string>\" /output:\"<output-folder>\"[/]");
        return (int)ExitCode.InvalidParameters;
    }

    // Log parameter values for debugging (mask connection string for security)
    var maskedCnn = connectionStringValue.Length > 20
        ? connectionStringValue.Substring(0, 20) + "..."
        : "[masked]";
    AnsiConsole.MarkupLine($"[green]OK:[/] Connection string received: {maskedCnn}");
    AnsiConsole.MarkupLine($"[green]OK:[/] Output folder: {outputValue}");
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to parse command line parameters: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.InvalidParameters;
}

// Step 2: Validate and create output folder
AnsiConsole.MarkupLine("\n[yellow]Step 2:[/] Validating output folder...");

try
{
    // Check if path is valid
    var fullPath = Path.GetFullPath(outputValue);
    AnsiConsole.MarkupLine($"[grey]LOG:[/] Resolved full path: {fullPath}");

    if (!Directory.Exists(fullPath))
    {
        AnsiConsole.MarkupLine($"[grey]LOG:[/] Directory does not exist, attempting to create...");

        // Check if parent directory exists and is accessible
        var parentDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            AnsiConsole.MarkupLine($"[grey]LOG:[/] Parent directory does not exist: {parentDir}");
        }

        Directory.CreateDirectory(fullPath);
        AnsiConsole.MarkupLine($"[green]OK:[/] Directory created successfully: {fullPath}");
    }
    else
    {
        AnsiConsole.MarkupLine($"[green]OK:[/] Directory already exists: {fullPath}");
    }

    // Verify write permissions by creating a test file
    var testFile = Path.Combine(fullPath, ".write_test");
    try
    {
        File.WriteAllText(testFile, "test");
        File.Delete(testFile);
        AnsiConsole.MarkupLine($"[green]OK:[/] Write permissions verified");
    }
    catch (UnauthorizedAccessException)
    {
        AnsiConsole.MarkupLine($"[red]ERROR:[/] No write permission to folder: {fullPath}");
        return (int)ExitCode.OutputFolderError;
    }

    // Update outputValue to the resolved full path
    outputValue = fullPath;
}
catch (ArgumentException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Invalid output folder path: {ex.Message}");
    return (int)ExitCode.OutputFolderError;
}
catch (PathTooLongException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Output folder path is too long: {ex.Message}");
    return (int)ExitCode.OutputFolderError;
}
catch (IOException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] I/O error creating output folder: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.OutputFolderError;
}
catch (UnauthorizedAccessException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Access denied creating output folder: {ex.Message}");
    return (int)ExitCode.OutputFolderError;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Unexpected error with output folder: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.OutputFolderError;
}

// Step 3: Initialize ScenarioBuilder and connect to database
AnsiConsole.MarkupLine("\n[yellow]Step 3:[/] Initializing scenario builder and database connection...");

ScenarioBuilder? builder = null;
try
{
    builder = new ScenarioBuilder(connectionStringValue);
    AnsiConsole.MarkupLine($"[green]OK:[/] Database connection established");
    AnsiConsole.MarkupLine($"[green]OK:[/] Found {builder.Scenarios.Count} scenarios to process");
}
catch (Npgsql.NpgsqlException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Database connection failed: {ex.Message}");
    AnsiConsole.MarkupLine("[grey]Please verify:[/]");
    AnsiConsole.MarkupLine("  - The connection string is correct");
    AnsiConsole.MarkupLine("  - The database server is running and accessible");
    AnsiConsole.MarkupLine("  - The database exists and user has proper permissions");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.DatabaseConnectionError;
}
catch (TypeLoadException ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to load scenario types: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.ScenarioExecutionError;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to initialize scenario builder: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.DatabaseConnectionError;
}

// Step 4: Process scenarios
AnsiConsole.MarkupLine("\n[yellow]Step 4:[/] Processing scenarios...");

try
{
    int processedCount = 0;
    int totalScenarios = builder.Scenarios.Count;

    AnsiConsole.Status()
        .AutoRefresh(true)
        .Spinner(Spinner.Known.Default)
        .Start("[yellow]Processing scenarios...[/]", ctx =>
        {
            foreach (var scenario in builder.Scenarios)
            {
                var scenarioName = scenario.ScenarioFileName;
                processedCount++;
                AnsiConsole.MarkupLine($"\n[blue]Processing scenario {processedCount}/{totalScenarios}:[/] {scenarioName}");

                try
                {
                    // Clear the database
                    ctx.Status = $"[yellow]Cleaning database for {scenarioName}...[/]";
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] Clearing database...");
                    builder.NDbUnitTest.ClearDatabase();
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] Database cleared");

                    // Check if the scenario has a preload scenario and load it
                    if (scenario.PreloadScenario != null)
                    {
                        ctx.Status = $"[yellow]Preloading scenario for {scenarioName}...[/]";
                        AnsiConsole.MarkupLine($"[grey]LOG:[/] Loading preload scenario: {scenario.PreloadScenario.Name}");

                        try
                        {
                            builder.LoadXmlFile(scenario.PreloadScenario, outputValue);
                            AnsiConsole.MarkupLine($"[grey]LOG:[/] Preload scenario loaded successfully");
                        }
                        catch (FileNotFoundException ex)
                        {
                            AnsiConsole.MarkupLine($"[red]ERROR:[/] Preload scenario file not found: {ex.Message}");
                            throw;
                        }
                        catch (TypeLoadException ex)
                        {
                            AnsiConsole.MarkupLine($"[red]ERROR:[/] Preload scenario type not found: {ex.Message}");
                            throw;
                        }
                    }

                    // Seed the scenario and save it in a xml file
                    ctx.Status = $"[yellow]Seeding data for {scenarioName}...[/]";
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] Seeding scenario data...");
                    scenario.SeedData().Wait();
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] Scenario data seeded successfully");

                    // Save to file
                    ctx.Status = $"[yellow]Saving {scenarioName} to file...[/]";
                    string filePath = Path.Combine(outputValue, $"{scenarioName}.xml");
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] Saving to file: {filePath}");
                    DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();
                    dataSet.WriteXml(filePath);
                    AnsiConsole.MarkupLine($"[green]OK:[/] Scenario {scenarioName} saved successfully");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to process scenario {scenarioName}: {ex.Message}");
                    AnsiConsole.WriteException(ex);
                    throw new InvalidOperationException($"Scenario '{scenarioName}' failed: {ex.Message}", ex);
                }
            }
        });

    AnsiConsole.MarkupLine("\n[blue]========================================[/]");
    AnsiConsole.MarkupLine($"[green3_1]SUCCESS: All {totalScenarios} scenarios processed![/]");
    AnsiConsole.MarkupLine("[blue]========================================[/]");
    return (int)ExitCode.Success;
}
catch (InvalidOperationException ex)
{
    AnsiConsole.MarkupLine($"\n[red]FAILED:[/] Scenario processing failed: {ex.Message}");
    return (int)ExitCode.ScenarioExecutionError;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"\n[red]ERROR:[/] Unexpected error during scenario processing: {ex.Message}");
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.UnknownError;
}
