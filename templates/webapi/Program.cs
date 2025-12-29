using FastEndpoints;
using FastEndpoints.Swagger;
using {ProjectName}.webapi.infrastructure;

// Load environment variables from .env file
DotNetEnv.Env.Load();

IConfiguration configuration;

var builder = WebApplication.CreateBuilder(args);
configuration = builder.Configuration;
var environment = builder.Environment;

// Configure dependency injection container
builder.Services
    .AddSwaggerGen(c =>
    {
        // Use full type name for nested classes to avoid schema ID conflicts
        c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    })
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)
    .ConfigureAutoMapper()
    .ConfigureValidators()
    .ConfigureUseCases()
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

await app.RunAsync();

// Make Program accessible to tests
public partial class Program { }
