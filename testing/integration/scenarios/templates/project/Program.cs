using Spectre.Console;
using System.Data;
using System.Text;
using {ProjectName}.scenarios;

Console.OutputEncoding = Encoding.UTF8;
ScenarioBuilder builder;

try
{
    // Read the command line parameters
    AnsiConsole.MarkupLine("Reading command line parameters...");
    CommandLineArgs parameter = [];

    if (!parameter.TryGetValue("cnn", out string? connectionStringValue))
        throw new ArgumentException("No [cnn] parameter received. You need pass the connection string in order to execute the scenarios");

    if (!parameter.TryGetValue("output", out string? outputValue))
        throw new ArgumentException("No [output] parameter received. You need pass the output folder path in order to save the scenarios");

    if (!Directory.Exists(outputValue))
    {
        try
        {
            Directory.CreateDirectory(outputValue);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"The output folder path [{outputValue}] is not valid. Error: {ex.Message}");
        }
    }

    // Synchronous
    AnsiConsole.Status()
    .AutoRefresh(true)
        .Spinner(Spinner.Known.Default)
        .Start("[yellow]Reading assemblies from assemblies...[/]", ctx =>
        {
            AnsiConsole.MarkupLine("[grey]LOG:[/] Loading scenarios[grey]...[/]");
            builder = new ScenarioBuilder(connectionStringValue);

            // Simulate some work
            foreach (var scenario in builder.Scenarios)
            {
                var scenarioName = scenario.ScenarioFileName;
                AnsiConsole.MarkupLine($"[grey]LOG:[/] Creating scenario {scenarioName}...");

                //Clear the database
                ctx.Status = "[yellow]Cleaning database...[/]";
                builder.NDbUnitTest.ClearDatabase();

                //Check if the scenario has a preload scenario and load it
                if (scenario.PreloadScenario != null)
                {
                    ctx.Status = "[yellow]Preloading scenario required...[/]";
                    builder.LoadXmlFile(scenario.PreloadScenario, outputValue);
                }

                //Seed the scenario and save it in a xml file
                ctx.Status = "[yellow]Creating scenario...[/]";
                scenario.SeedData().Wait();
                ctx.Status = "[yellow]Saving scenario in file system...[/]";
                string filePath = Path.Combine(outputValue, $"{scenarioName}.xml");
                DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();
                dataSet.WriteXml(filePath);
            }
        });

    AnsiConsole.MarkupLine("[green3_1]Scenario loading completed[/]");
    return (int)ExitCode.Success;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.UnknownError;
}
