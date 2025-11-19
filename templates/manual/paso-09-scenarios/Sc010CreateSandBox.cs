namespace MiProyecto.scenarios;

public class Sc010CreateSandBox : IScenario
{
    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    public string ScenarioFileName => "CreateSandBox";

    /// <summary>
    /// No pre-load scenario for this scenario
    /// </summary>
    public Type? PreloadScenario => null;

    /// <summary>
    /// Seed data - Empty scenario for sandbox
    /// </summary>
    public Task SeedData()
        => Task.CompletedTask;
}
