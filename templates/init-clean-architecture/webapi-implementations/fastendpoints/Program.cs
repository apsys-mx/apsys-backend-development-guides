using DotNetEnv;
using FastEndpoints;
using FastEndpoints.Swagger;
using {ProjectName}.application.usecases;  // TODO: Update with actual use case namespace
using {ProjectName}.webapi.infrastructure;

// Cargar variables de entorno desde .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

// Configurar servicios (Dependency Injection)
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()                              // Políticas de autorización
    .ConfigureCors(configuration)                   // CORS
    .ConfigureIdentityServerClient(configuration)   // JWT Bearer
    .ConfigureUnitOfWork(configuration)             // UnitOfWork (TODO)
    .ConfigureAutoMapper()                          // AutoMapper
    .ConfigureValidators()                          // FluentValidation
    .ConfigureDependencyInjections(environment)     // DI custom
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()                             // FastEndpoints
    .SwaggerDocument();                              // Swagger

var app = builder.Build();

// Middleware pipeline
app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints(config =>
    {
        config.Endpoints.RoutePrefix = "api";
    })
    .UseSwagger()
    .UseSwaggerUI(opt =>
    {
        opt.DefaultModelsExpandDepth(-1);  // Ocultar schemas por defecto
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

// Registrar Commands/Handlers automáticamente
// TODO: Update with actual use case type from your application layer
// app.Services.RegisterCommandsFromAssembly(typeof(UseCaseExample).Assembly);

app.Run();

// Hacer Program accesible para tests de integración
public partial class Program { }
