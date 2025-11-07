# Configuration - Dependency Injection y Setup

## Propósito

Esta carpeta contiene la **configuración de Dependency Injection** y el setup de la capa de infraestructura para ser utilizada por la capa de presentación (WebApi).

## Responsabilidades

1. ✅ Registrar implementaciones de repositorios
2. ✅ Registrar servicios externos
3. ✅ Configurar ORM y acceso a datos
4. ✅ Configurar Unit of Work
5. ✅ Organizar la configuración en métodos de extensión

## Estructura Recomendada

```
configuration/
├── InfrastructureServiceCollectionExtensions.cs   # Extension methods principales
├── PersistenceConfiguration.cs                    # Configuración de persistencia
└── ExternalServicesConfiguration.cs               # Configuración de servicios externos
```

## Patrón de Extension Methods

### Archivo Principal

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace {ProjectName}.infrastructure.configuration;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddRepositories()
            .AddExternalServices(configuration);

        return services;
    }
}
```

### Configuración de Persistencia

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NHibernate;
using {ProjectName}.infrastructure.persistence;

namespace {ProjectName}.infrastructure.configuration;

public static class PersistenceConfiguration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        // Registrar SessionFactory (NHibernate)
        var sessionFactory = SessionFactoryBuilder.Build(connectionString);
        services.AddSingleton<ISessionFactory>(sessionFactory);

        // Registrar Session con scope por request
        services.AddScoped<ISession>(provider =>
        {
            var sf = provider.GetRequiredService<ISessionFactory>();
            return sf.OpenSession();
        });

        // Registrar Unit of Work
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        return services;
    }
}
```

### Configuración de Repositorios

```csharp
using Microsoft.Extensions.DependencyInjection;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.infrastructure.repositories;

namespace {ProjectName}.infrastructure.configuration;

public static class RepositoriesConfiguration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repositorios CRUD
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Repositorios de solo lectura (DAOs)
        services.AddScoped<IUserDaoRepository, UserDaoRepository>();
        services.AddScoped<IProductDaoRepository, ProductDaoRepository>();

        return services;
    }
}
```

### Configuración de Servicios Externos

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using {ProjectName}.domain.interfaces.services;
using {ProjectName}.infrastructure.services.email;
using {ProjectName}.infrastructure.services.storage;

namespace {ProjectName}.infrastructure.configuration;

public static class ExternalServicesConfiguration
{
    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Email
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Storage
        services.AddScoped<IStorageService, S3StorageService>();

        // External APIs
        services.AddHttpClient<IWeatherService, WeatherApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WeatherApi:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        return services;
    }
}
```

## Uso en Program.cs (WebApi)

```csharp
using {ProjectName}.infrastructure.configuration;

var builder = WebApplication.CreateBuilder(args);

// Agregar toda la configuración de infraestructura con un solo método
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();
```

## Ejemplo con Entity Framework

```csharp
using Microsoft.EntityFrameworkCore;
using {ProjectName}.infrastructure.persistence;

public static class PersistenceConfiguration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        // Registrar DbContext
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        // Registrar Unit of Work
        services.AddScoped<IUnitOfWork, EFUnitOfWork>();

        return services;
    }
}
```

## Principios

### 1. Un Método por Responsabilidad

```csharp
// ✅ CORRECTO - Métodos específicos
services.AddPersistence(configuration);
services.AddRepositories();
services.AddExternalServices(configuration);

// ❌ INCORRECTO - Todo en un método gigante
services.AddInfrastructure(); // hace todo dentro
```

### 2. Configuración desde IConfiguration

```csharp
// ✅ CORRECTO - Lee desde appsettings.json o .env
var connectionString = configuration.GetConnectionString("DefaultConnection");
var apiKey = configuration["ExternalApi:ApiKey"];

// ❌ INCORRECTO - Hardcoded
var connectionString = "Server=localhost;Database=mydb;...";
```

### 3. Scopes Apropiados

```csharp
// Singleton - Una instancia para toda la aplicación
services.AddSingleton<ISessionFactory>(sessionFactory);

// Scoped - Una instancia por request HTTP
services.AddScoped<ISession>();
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
services.AddScoped<IUserRepository, UserRepository>();

// Transient - Nueva instancia cada vez que se inyecta
services.AddTransient<IEmailService, SmtpEmailService>();
```

### 4. Extension Methods Fluent

```csharp
// ✅ CORRECTO - Permite chaining
public static IServiceCollection AddInfrastructure(this IServiceCollection services)
{
    services
        .AddPersistence()
        .AddRepositories()
        .AddExternalServices();

    return services; // Importante: retornar IServiceCollection
}
```

### 5. Validación de Configuración

```csharp
// ✅ CORRECTO - Valida que la configuración existe
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

var apiKey = configuration["ExternalApi:ApiKey"]
    ?? throw new InvalidOperationException("External API key not configured");
```

## Ejemplo Completo

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NHibernate;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.domain.interfaces.services;
using {ProjectName}.infrastructure.persistence;
using {ProjectName}.infrastructure.repositories;
using {ProjectName}.infrastructure.services.email;

namespace {ProjectName}.infrastructure.configuration;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistencia
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        var sessionFactory = SessionFactoryBuilder.Build(connectionString);
        services.AddSingleton<ISessionFactory>(sessionFactory);

        services.AddScoped<ISession>(provider =>
        {
            var sf = provider.GetRequiredService<ISessionFactory>();
            return sf.OpenSession();
        });

        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Servicios externos
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
```

## Testing

Para tests, puedes crear un método similar que use mocks:

```csharp
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        // Usar In-Memory database para tests
        services.AddScoped<IUserRepository, InMemoryUserRepository>();

        // Usar mock de email service
        services.AddScoped<IEmailService, FakeEmailService>();

        return services;
    }
}
```

## Next Steps

Para configurar infraestructura con una tecnología específica:

- **NHibernate**: Ver `guides/stack-implementations/nhibernate/03-configuration.md`
- **Entity Framework**: Ver `guides/stack-implementations/entityframework/03-configuration.md`
- **Dapper**: Ver `guides/stack-implementations/dapper/03-configuration.md`
