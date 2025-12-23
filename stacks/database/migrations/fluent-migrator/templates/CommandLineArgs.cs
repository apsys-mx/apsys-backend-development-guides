namespace {ProjectName}.migrations;

/// <summary>
/// Parses command line arguments in key=value format.
/// Example: dotnet run cnn="Host=localhost" action=rollback
/// </summary>
public class CommandLineArgs : Dictionary<string, string>
{
    public CommandLineArgs()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            var parts = arg.Split('=', 2);
            if (parts.Length == 2)
            {
                this[parts[0]] = parts[1];
            }
        }
    }
}
