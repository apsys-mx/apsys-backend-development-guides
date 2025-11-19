using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiProyecto.webapi.tests;

/// <summary>
/// Custom WebApplicationFactory for testing
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    /// <summary>
    /// Configure the WebHost for testing
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configurar servicios para testing
        });
        // Usar ambiente de testing
        builder.UseEnvironment("Testing");
    }
}
