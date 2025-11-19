using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.scenarios;

public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    public string ScenarioFileName => "CreateRoles";

    /// <summary>
    /// Pre-load the sandbox scenario
    /// </summary>
    public Type? PreloadScenario => typeof(Sc010CreateSandBox);

    /// <summary>
    /// Seed data using the repository
    /// </summary>
    public Task SeedData()
    {
        return _uoW.Roles.CreateDefaultRoles();
    }
}
