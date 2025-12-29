using FastEndpoints;

namespace {ProjectName}.webapi.features.hello;

/// <summary>
/// Hello endpoint for testing the API is working
/// </summary>
public class HelloEndpoint : EndpointWithoutRequest
{
    private readonly IWebHostEnvironment environment;

    /// <summary>
    /// Constructor
    /// </summary>
    public HelloEndpoint(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    /// <summary>
    /// Configures the endpoint
    /// </summary>
    public override void Configure()
    {
        Get("/api/hello");
        AllowAnonymous();
    }

    /// <summary>
    /// Handles the request
    /// </summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var message = $"Hello from {ProjectName} API - Environment: {environment.EnvironmentName}";
        Logger.LogInformation(message);
        await SendOkAsync(message, cancellation: ct);
    }
}
