using FastEndpoints;
using FastEndpoints.Swagger;
using {ProjectName}.application.usecases;  // TODO: Update with actual use case namespace
using {ProjectName}.webapi.infrastructure;

// Load environment variables from .env file
// This is necessary to ensure that the connection string and other settings are available
DotNetEnv.Env.Load();

IConfiguration configuration;

var builder = WebApplication.CreateBuilder(args);
configuration = builder.Configuration;
var environment = builder.Environment;

// Configure dependency injection container
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)
    .ConfigureAutoMapper()
    .ConfigureValidators()
    .ConfigureDependencyInjections(environment)
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()
    .SwaggerDocument();

var app = builder.Build();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints()
    .UseSwagger()
    .UseSwaggerUI(opt =>
{
    opt.DefaultModelsExpandDepth(-1); // Hide schemas by default
    opt.DisplayRequestDuration();
    opt.EnableTryItOutByDefault();
});

// Automatically register all Commands and Handlers from the application assembly
// TODO: Update with actual use case type from your application layer
// app.Services.RegisterCommandsFromAssembly(typeof(YourUseCaseClass).Assembly);

await app.RunAsync();
