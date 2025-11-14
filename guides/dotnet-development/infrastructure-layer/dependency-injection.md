# Inyección de Dependencias en Infrastructure Layer
**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [¿Qué es la Inyección de Dependencias?](#qué-es-la-inyección-de-dependencias)
2. [Lifetimes de Servicios](#lifetimes-de-servicios)
3. [Configuración de NHibernate](#configuración-de-nhibernate)
4. [Configuración de Repositorios](#configuración-de-repositorios)
5. [Métodos de Extensión](#métodos-de-extensión)
6. [Configuración Completa](#configuración-completa)
7. [Patrón Service Provider en Repositorios](#patrón-service-provider-en-repositorios)
8. [Configuración por Ambiente](#configuración-por-ambiente)
9. [Mejores Prácticas](#mejores-prácticas)
10. [Antipatrones](#antipatrones)
11. [Checklist de Implementación](#checklist-de-implementación)
12. [Ejemplos Completos](#ejemplos-completos)

---

## ¿Qué es la Inyección de Dependencias?

La **Inyección de Dependencias (DI)** es un patrón de diseño que permite invertir el control de la creación de objetos, delegando esta responsabilidad a un contenedor de DI. En el Infrastructure Layer, DI es fundamental para:

- ✅ **Gestionar el ciclo de vida** de ISessionFactory e ISession
- ✅ **Desacoplar** la infraestructura de la aplicación
- ✅ **Facilitar testing** mediante la inyección de mocks
- ✅ **Centralizar configuración** de servicios externos
- ✅ **Garantizar Session Per Request** (patrón recomendado para NHibernate)

### 🎯 Analogía Visual

```
┌─────────────────────────────────────────────────┐
│          ASP.NET Core DI Container              │
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │  ISessionFactory (SINGLETON)             │  │
│  │  • Creado al iniciar la app              │  │
│  │  • Una sola instancia para toda la app   │  │
│  │  • Thread-safe y costoso de crear        │  │
│  └──────────────────────────────────────────┘  │
│                       │                         │
│                       │ OpenSession()           │
│                       ↓                         │
│  ┌──────────────────────────────────────────┐  │
│  │  ISession (SCOPED)                       │  │
│  │  • Una instancia por request HTTP       │  │
│  │  • Se crea al inicio del request         │  │
│  │  • Se dispone al final del request       │  │
│  └──────────────────────────────────────────┘  │
│                       │                         │
│                       │ Constructor Injection   │
│                       ↓                         │
│  ┌──────────────────────────────────────────┐  │
│  │  IUnitOfWork (SCOPED)                    │  │
│  │  • Misma instancia que ISession          │  │
│  │  • Se dispone al final del request       │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

## Lifetimes de Servicios

ASP.NET Core ofrece **tres lifetimes** para servicios en DI:

### 1. **Singleton** - Una instancia para toda la aplicación

```csharp
services.AddSingleton<ISessionFactory>(sessionFactory);
```

**Características**:
- ✅ Se crea **una sola vez** al iniciar la aplicación
- ✅ **Thread-safe** por naturaleza (debe serlo)
- ✅ Vive durante **toda la vida** de la aplicación
- ❌ **NO** debe mantener estado mutable

**Casos de uso**:
- ✅ `ISessionFactory` (NHibernate)
- ✅ Configuración inmutable
- ✅ Servicios sin estado

---

### 2. **Scoped** - Una instancia por request HTTP

```csharp
services.AddScoped<ISession>(factory => sessionFactory.OpenSession());
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

**Características**:
- ✅ Se crea **una vez por request** HTTP
- ✅ Compartida por **todos los servicios** del mismo request
- ✅ Se **dispone automáticamente** al final del request
- ✅ Ideal para **Session Per Request**

**Casos de uso**:
- ✅ `ISession` (NHibernate)
- ✅ `IUnitOfWork`
- ✅ Validators (FluentValidation)
- ✅ Servicios que necesitan estado temporal

---

### 3. **Transient** - Una instancia nueva cada vez

```csharp
services.AddTransient<IEmailService, EmailService>();
```

**Características**:
- ✅ Se crea **cada vez** que se solicita
- ✅ **NO se reutiliza** entre servicios
- ✅ Se dispone cuando el **contenedor padre** se dispone
- ❌ **NO** recomendado para servicios costosos

**Casos de uso**:
- ✅ Servicios ligeros sin estado
- ✅ Servicios que deben ser independientes

---

## Configuración de NHibernate

### 🔧 ISessionFactory - Configuración Singleton

El `ISessionFactory` es **costoso de crear** y debe ser **Singleton**:

```csharp
public static IServiceCollection ConfigureUnitOfWork(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 1. Construir connection string desde variables de entorno
    string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();

    // 2. Crear NHSessionFactory
    var factory = new NHSessionFactory(connectionString);

    // 3. Construir ISessionFactory (COSTOSO - hacer solo una vez)
    var sessionFactory = factory.BuildNHibernateSessionFactory();

    // 4. Registrar como SINGLETON (una sola instancia)
    services.AddSingleton(sessionFactory);

    // 5. Registrar ISession como SCOPED (una por request)
    services.AddScoped(factory => sessionFactory.OpenSession());

    // 6. Registrar IUnitOfWork como SCOPED
    services.AddScoped<IUnitOfWork, NHUnitOfWork>();

    return services;
}
```

### 📦 Implementación de NHSessionFactory

```csharp
public class NHSessionFactory
{
    public string ConnectionString { get; }

    public NHSessionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();

        // Agregar todos los mappers del assembly
        mapper.AddMappings(typeof(RoleMapper).Assembly.ExportedTypes);

        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            // c.SchemaAction = SchemaAutoAction.Validate; // Solo en desarrollo
        });

        cfg.AddMapping(domainMapping);

        return cfg.BuildSessionFactory();
    }
}
```

### 🔐 ConnectionStringBuilder

```csharp
public static class ConnectionStringBuilder
{
    public static string BuildPostgresConnectionString()
    {
        var requiredVars = new[]
        {
            "POSTGRES_HOST",
            "POSTGRES_PORT",
            "POSTGRES_DATABASE",
            "POSTGRES_USER",
            "POSTGRES_PASSWORD"
        };

        var missingVars = requiredVars
            .Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)))
            .ToList();

        if (missingVars.Any())
        {
            throw new ConfigurationErrorsException(
                $"Missing required environment variables: {string.Join(", ", missingVars)}");
        }

        return $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST")};" +
               $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT")};" +
               $"Database={Environment.GetEnvironmentVariable("POSTGRES_DATABASE")};" +
               $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")};" +
               $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")};";
    }
}
```

---

## Configuración de Repositorios

### ❌ INCORRECTO - Registrar repositorios específicos

```csharp
// ❌ NO hacer esto
services.AddScoped<IUserRepository, NHUserRepository>();
services.AddScoped<IPrototypeRepository, NHPrototypeRepository>();
```

**Problemas**:
- ❌ Los repositorios se crean **fuera del UnitOfWork**
- ❌ **NO comparten** el mismo ISession
- ❌ Transacciones **NO funcionarán** correctamente
- ❌ Viola el patrón **Unit of Work**

---

### ✅ CORRECTO - Repositorios creados por UnitOfWork

```csharp
// ✅ SOLO registrar IUnitOfWork
services.AddScoped<IUnitOfWork, NHUnitOfWork>();

// Los repositorios se crean en las propiedades de NHUnitOfWork
public class NHUnitOfWork : IUnitOfWork
{
    private readonly ISession _session;
    private readonly IServiceProvider _serviceProvider;

    // Repositorios creados on-demand
    public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
    public IPrototypeRepository Prototypes => new NHPrototypeRepository(_session, _serviceProvider);

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }
}
```

**Ventajas**:
- ✅ Todos los repositorios **comparten el mismo ISession**
- ✅ Las transacciones **funcionan correctamente**
- ✅ Respeta el patrón **Unit of Work**
- ✅ Repositorios se crean **solo cuando se necesitan** (lazy)

---

## Métodos de Extensión

Los **métodos de extensión** organizan la configuración de DI en módulos reutilizables:

### 📁 Estructura de ServiceCollectionExtender.cs

```csharp
public static class ServiceCollectionExtender
{
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración de NHibernate
    }

    public static IServiceCollection ConfigureValidators(
        this IServiceCollection services)
    {
        // Configuración de FluentValidation
    }

    public static IServiceCollection ConfigureDependencyInjections(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        // Configuración de servicios externos
    }

    public static IServiceCollection ConfigureAutoMapper(
        this IServiceCollection services)
    {
        // Configuración de AutoMapper
    }
}
```

---

### 🧩 ConfigureValidators

```csharp
public static IServiceCollection ConfigureValidators(this IServiceCollection services)
{
    services.AddScoped<AbstractValidator<User>, UserValidator>();
    services.AddScoped<AbstractValidator<Prototype>, PrototypeValidator>();
    services.AddScoped<AbstractValidator<TechnicalStandard>, TechnicalStandardValidator>();

    return services;
}
```

**Por qué Scoped**:
- ✅ Los validators pueden **inyectar ISession** si es necesario
- ✅ Se **reutilizan** durante el mismo request
- ✅ Se **disponen** automáticamente al final del request

---

### 🌍 ConfigureDependencyInjections (por ambiente)

```csharp
public static IServiceCollection ConfigureDependencyInjections(
    this IServiceCollection services,
    IWebHostEnvironment environment)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        // Mock para desarrollo/testing
        services.AddScoped<IIdentityService, Auth0ServiceMock>();
    }
    else
    {
        // Servicio real para producción
        services.AddScoped<IIdentityService, Auth0Service>();
    }

    return services;
}
```

**Ventajas**:
- ✅ **Mocks** en desarrollo sin cambiar código
- ✅ **Servicios reales** en producción
- ✅ Tests **más rápidos** sin llamadas externas

---

## Configuración Completa

### 🚀 Program.cs

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.infrastructure;

// Cargar variables de entorno desde .env
DotNetEnv.Env.Load();

IConfiguration configuration;
var builder = WebApplication.CreateBuilder(args);
configuration = builder.Configuration;
var environment = builder.Environment;

// Configurar contenedor de DI
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)          // 🔥 NHibernate
    .ConfigureAutoMapper()                       // 🔥 AutoMapper
    .ConfigureValidators()                       // 🔥 FluentValidation
    .ConfigureDependencyInjections(environment)  // 🔥 Servicios externos
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
        opt.DefaultModelsExpandDepth(-1);
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

// Registro automático de Commands y Handlers
app.Services.RegisterCommandsFromAssembly(typeof(GetManyAndCountUsersUseCase).Assembly);

await app.RunAsync();
```

---

## Patrón Service Provider en Repositorios

Los repositorios necesitan acceso al `IServiceProvider` para resolver validators dinámicamente:

### 🔍 Constructor de NHUnitOfWork

```csharp
public class NHUnitOfWork : IUnitOfWork
{
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    // Repositorios reciben ISession y IServiceProvider
    public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
}
```

### 🔍 Uso en Repositorio

```csharp
public class NHUserRepository : NHRepository<User>, IUserRepository
{
    private readonly IServiceProvider _serviceProvider;

    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<User> CreateAsync(string email, string name)
    {
        var user = new User { Email = email, Name = name };

        // Resolver validator dinámicamente
        var validator = _serviceProvider.GetRequiredService<AbstractValidator<User>>();

        // Validar antes de guardar
        var validationResult = await validator.ValidateAsync(user);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();

            throw new InvalidDomainException(JsonSerializer.Serialize(errors));
        }

        await this.Session.SaveAsync(user);
        return user;
    }
}
```

**¿Por qué IServiceProvider?**
- ✅ Los repositorios **NO están registrados** en DI
- ✅ Se crean **manualmente** en UnitOfWork
- ✅ Necesitan **resolver validators** en tiempo de ejecución
- ✅ Evita **dependencias circulares**

---

## Configuración por Ambiente

### 🔧 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "http://localhost:3000,http://localhost:5173",
  "IdentityServerConfiguration": {
    "Address": "https://dev-example.auth0.com"
  }
}
```

### 🔧 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "NHibernate": "Debug"
    }
  },
  "AllowedHosts": "http://localhost:3000,http://localhost:5173,http://localhost:8080"
}
```

### 🔧 .env (Variables de entorno)

```bash
# PostgreSQL
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DATABASE=hashira_stone_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# Auth0
IDENTITY_SERVER_ADDRESS=https://dev-example.auth0.com
```

### 📖 Cargar .env en Program.cs

```csharp
// Cargar variables de entorno desde .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
```

---

## Mejores Prácticas

### ✅ 1. Session Per Request

```csharp
// ✅ CORRECTO - ISession Scoped
services.AddScoped(factory => sessionFactory.OpenSession());
```

**Ventajas**:
- ✅ Una sesión por request HTTP
- ✅ Evita **LazyInitializationException**
- ✅ Dispose automático al final del request
- ✅ Patrón recomendado por NHibernate

---

### ✅ 2. ISessionFactory como Singleton

```csharp
// ✅ CORRECTO - ISessionFactory Singleton
services.AddSingleton(sessionFactory);
```

**Ventajas**:
- ✅ Se crea **una sola vez**
- ✅ Thread-safe
- ✅ Evita **overhead** de creación

---

### ✅ 3. Métodos de Extensión para Organización

```csharp
// ✅ CORRECTO - Organizado y reutilizable
builder.Services
    .ConfigureUnitOfWork(configuration)
    .ConfigureValidators()
    .ConfigureDependencyInjections(environment);
```

**Ventajas**:
- ✅ **Modular** y fácil de mantener
- ✅ **Reutilizable** entre proyectos
- ✅ **Testeable** de forma independiente

---

### ✅ 4. Validación de Variables de Entorno

```csharp
public static string BuildPostgresConnectionString()
{
    var requiredVars = new[] { "POSTGRES_HOST", "POSTGRES_PORT", ... };
    var missingVars = requiredVars
        .Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)))
        .ToList();

    if (missingVars.Any())
    {
        throw new ConfigurationErrorsException(
            $"Missing required environment variables: {string.Join(", ", missingVars)}");
    }

    // ...
}
```

**Ventajas**:
- ✅ **Fail-fast** al iniciar la aplicación
- ✅ Mensaje de error **claro** y **accionable**
- ✅ Evita errores en runtime

---

### ✅ 5. Configuración por Ambiente

```csharp
if (environment.IsDevelopment())
{
    services.AddScoped<IIdentityService, Auth0ServiceMock>();
}
else
{
    services.AddScoped<IIdentityService, Auth0Service>();
}
```

**Ventajas**:
- ✅ **Mocks** para desarrollo/testing
- ✅ **Servicios reales** para producción
- ✅ Sin cambios de código

---

## Antipatrones

### ❌ 1. Registrar Repositorios Directamente

```csharp
// ❌ INCORRECTO
services.AddScoped<IUserRepository, NHUserRepository>();
services.AddScoped<IPrototypeRepository, NHPrototypeRepository>();

// Problema: Los repositorios NO comparten el mismo ISession
// Las transacciones NO funcionarán correctamente
```

**Solución**:
```csharp
// ✅ CORRECTO - Solo registrar IUnitOfWork
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

---

### ❌ 2. ISession como Singleton

```csharp
// ❌ INCORRECTO - ISession Singleton
services.AddSingleton(sessionFactory.OpenSession());

// Problemas:
// ❌ NO es thread-safe
// ❌ Puede causar data corruption
// ❌ Viola Session Per Request
```

**Solución**:
```csharp
// ✅ CORRECTO - ISession Scoped
services.AddScoped(factory => sessionFactory.OpenSession());
```

---

### ❌ 3. Hard-Coded Connection Strings

```csharp
// ❌ INCORRECTO - Hard-coded
var connectionString = "Host=localhost;Database=mydb;Username=user;Password=pass";

// Problemas:
// ❌ Credenciales en el código fuente
// ❌ Difícil de cambiar entre ambientes
// ❌ Riesgo de seguridad
```

**Solución**:
```csharp
// ✅ CORRECTO - Variables de entorno
string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();
```

---

### ❌ 4. Service Locator en Repositorios

```csharp
// ❌ INCORRECTO - Service Locator directo
public class NHUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        // ❌ Acceso global al service locator
        var validator = ServiceLocator.GetService<AbstractValidator<User>>();
    }
}

// Problemas:
// ❌ Acoplamiento fuerte
// ❌ Difícil de testear
// ❌ Oculta dependencias
```

**Solución**:
```csharp
// ✅ CORRECTO - IServiceProvider inyectado
public class NHUserRepository
{
    private readonly IServiceProvider _serviceProvider;

    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<User> CreateAsync(string email, string name)
    {
        var validator = _serviceProvider.GetRequiredService<AbstractValidator<User>>();
    }
}
```

---

### ❌ 5. No Validar Variables de Entorno

```csharp
// ❌ INCORRECTO - Sin validación
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
var connectionString = $"Host={host};..."; // ❌ host puede ser null

// Problema: NullReferenceException en runtime
```

**Solución**:
```csharp
// ✅ CORRECTO - Validación al inicio
var requiredVars = new[] { "POSTGRES_HOST", ... };
var missingVars = requiredVars
    .Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)))
    .ToList();

if (missingVars.Any())
{
    throw new ConfigurationErrorsException($"Missing: {string.Join(", ", missingVars)}");
}
```

---

## Checklist de Implementación

### ✅ Antes de Empezar
- [ ] Variables de entorno definidas en `.env`
- [ ] `DotNetEnv` package instalado
- [ ] NHibernate packages instalados
- [ ] Mappers de entidades creados

### ✅ Durante la Configuración
- [ ] `ISessionFactory` registrado como **Singleton**
- [ ] `ISession` registrado como **Scoped**
- [ ] `IUnitOfWork` registrado como **Scoped**
- [ ] Validators registrados como **Scoped**
- [ ] Métodos de extensión creados
- [ ] Connection string construido desde variables de entorno
- [ ] Validación de variables de entorno implementada
- [ ] Configuración por ambiente implementada

### ✅ Después de la Configuración
- [ ] Program.cs llama a todos los métodos de extensión
- [ ] .env cargado con `DotNetEnv.Env.Load()`
- [ ] Tests unitarios de configuración
- [ ] Verificar Session Per Request funciona
- [ ] Verificar transacciones funcionan correctamente

---

## Ejemplos Completos

### 📋 Ejemplo 1: Configuración Completa de DI

**ServiceCollectionExtender.cs**:
```csharp
using FluentValidation;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.infrastructure.nhibernate;
using hashira.stone.backend.domain.interfaces.services;
using hashira.stone.backend.infrastructure.services;
using hashira.stone.backend.domain.entities.validators;

namespace hashira.stone.backend.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure the unit of work dependency injection
    /// </summary>
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Construir connection string desde variables de entorno
        string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();

        // 2. Crear NHSessionFactory
        var factory = new NHSessionFactory(connectionString);

        // 3. Construir ISessionFactory
        var sessionFactory = factory.BuildNHibernateSessionFactory();

        // 4. Registrar ISessionFactory como Singleton
        services.AddSingleton(sessionFactory);

        // 5. Registrar ISession como Scoped (Session Per Request)
        services.AddScoped(factory => sessionFactory.OpenSession());

        // 6. Registrar IUnitOfWork como Scoped
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Configure fluent validators
    /// </summary>
    public static IServiceCollection ConfigureValidators(this IServiceCollection services)
    {
        services.AddScoped<AbstractValidator<User>, UserValidator>();
        services.AddScoped<AbstractValidator<Prototype>, PrototypeValidator>();
        services.AddScoped<AbstractValidator<TechnicalStandard>, TechnicalStandardValidator>();

        return services;
    }

    /// <summary>
    /// Configures dependency injections based on environment
    /// </summary>
    public static IServiceCollection ConfigureDependencyInjections(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            // Mock para desarrollo/testing
            services.AddScoped<IIdentityService, Auth0ServiceMock>();
        }
        else
        {
            // Servicio real para producción
            services.AddScoped<IIdentityService, Auth0Service>();
        }

        return services;
    }

    /// <summary>
    /// Configure AutoMapper
    /// </summary>
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
            cfg.AddProfile(new UserMappingProfile());
            cfg.AddProfile(new TechnicalStandardMappingProfile());
            cfg.AddProfile(new PrototypeMappingProfile());
        });

        return services;
    }
}
```

---

### 📋 Ejemplo 2: NHUnitOfWork con IServiceProvider

**NHUnitOfWork.cs**:
```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    #region CRUD Repositories
    public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
    public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
    public IPrototypeRepository Prototypes => new NHPrototypeRepository(_session, _serviceProvider);
    public ITechnicalStandardRepository TechnicalStandards => new NHTechnicalStandardRepository(_session, _serviceProvider);
    #endregion

    #region Read-Only Repositories
    public ITechnicalStandardDaoRepository TechnicalStandardDaos => new NHTechnicalStandardDaoRepository(_session);
    public IPrototypeDaoRepository PrototypeDaos => new NHPrototypeDaoRepository(_session);
    #endregion

    /// <summary>
    /// Constructor for NHUnitOfWork
    /// </summary>
    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    public void BeginTransaction()
    {
        this._transaction = this._session.BeginTransaction();
    }

    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
        else
            throw new TransactionException("The actual transaction is not longer active");
    }

    public bool IsActiveTransaction()
        => _transaction != null && _transaction.IsActive;

    public void Rollback()
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
        }
        else
            throw new ArgumentNullException(
                $"No active exception found for session {_session.Connection.ConnectionString}");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (this._transaction != null)
                    this._transaction.Dispose();
                this._session.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NHUnitOfWork()
    {
        Dispose(false);
    }
}
```

---

### 📋 Ejemplo 3: Program.cs Completo

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.infrastructure;

// Cargar variables de entorno desde .env
DotNetEnv.Env.Load();

IConfiguration configuration;

var builder = WebApplication.CreateBuilder(args);
configuration = builder.Configuration;
var environment = builder.Environment;

// Configurar contenedor de DI
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)          // NHibernate
    .ConfigureAutoMapper()                       // AutoMapper
    .ConfigureValidators()                       // FluentValidation
    .ConfigureDependencyInjections(environment)  // Servicios externos
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
        opt.DefaultModelsExpandDepth(-1);
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

// Registro automático de Commands y Handlers
app.Services.RegisterCommandsFromAssembly(typeof(GetManyAndCountUsersUseCase).Assembly);

await app.RunAsync();
```

---

## 📚 Referencias

- [ASP.NET Core Dependency Injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [NHibernate Session Management](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html)
- [Service Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                      |
|---------|------------|----------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de Dependency Injection     |

---

**Siguiente**: [NHibernate Configuration](./nhibernate-configuration.md) →
