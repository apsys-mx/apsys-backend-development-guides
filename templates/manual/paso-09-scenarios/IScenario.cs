namespace MiProyecto.scenarios;

/// <summary>
/// Defines the operations to seed the database with the data of the scenario
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Execute the operations to seed the database
    /// </summary>
    Task SeedData();

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    string ScenarioFileName { get; }

    /// <summary>
    /// If defined, the scenario will be pre-loaded before the current scenario
    /// </summary>
    Type? PreloadScenario { get; }
}
