using FastEndpoints;

namespace {ProjectName}.webapi.features.hello;

/// <summary>
/// Example endpoint that demonstrates basic FastEndpoints usage
/// </summary>
public class HelloEndpoint : EndpointWithoutRequest
{
    private readonly IWebHostEnvironment environment;

    /// <summary>
    /// Configures the endpoint.
    /// </summary>
    public override void Configure()
    {
        Get("/hello");
        AllowAnonymous();
    }

    public HelloEndpoint(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var helloWorld = $"Hello from {ProjectName} API - Environment: {environment.EnvironmentName}";
        Logger.LogInformation(helloWorld);

        LogConfigValue("IdentityServerConfiguration:Address");
        LogConfigValue("IdentityServerConfiguration:Audience");

        await SendOkAsync(helloWorld, cancellation: ct);
    }

    private void LogConfigValue(string configKey)
    {
        var configValue = Config.GetSection(configKey).Value;
        if (string.IsNullOrEmpty(configValue))
            Logger.LogError("{ConfigKey} is null", configKey);
        else
            Logger.LogInformation("{ConfigKey} value is [{ConfigValue}]", configKey, configValue);
    }
}
