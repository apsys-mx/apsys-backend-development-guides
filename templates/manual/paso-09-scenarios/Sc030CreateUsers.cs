using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.scenarios;

public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";

    /// <summary>
    /// Pre-load the roles scenario (which includes sandbox)
    /// </summary>
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        await _uoW.Users.CreateAsync("usuario1@example.com", "Usuario Uno");
        await _uoW.Users.CreateAsync("usuario2@example.com", "Usuario Dos");
    }
}
