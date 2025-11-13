# Dependency Injection

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenido

1. [Introducción](#introducción)
2. [Service Lifetimes](#service-lifetimes)
3. [Constructor Injection vs Property Injection](#constructor-injection-vs-property-injection)
4. [Service Registration Patterns](#service-registration-patterns)
5. [Scrutor para Registro Automático](#scrutor-para-registro-automático)
6. [Evitar Service Locator Pattern](#evitar-service-locator-pattern)
7. [Testing con DI](#testing-con-di)
8. [Anti-Patrones Comunes](#anti-patrones-comunes)
9. [Checklists](#checklists)
10. [Referencias](#referencias)

---

## Introducción

Dependency Injection (DI) es un patrón de diseño que implementa Inversión de Control (IoC) para resolver dependencias. En .NET, el contenedor de DI integrado (`Microsoft.Extensions.DependencyInjection`) proporciona:

- **Gestión automática del ciclo de vida** de los objetos
- **Desacoplamiento** entre componentes
- **Facilidad para testing** mediante inyección de mocks
- **Configuración centralizada** de dependencias

### Principios Fundamentales

1. **Constructor Injection**: Inyectar dependencias a través del constructor (preferido)
2. **Lifetimes apropiados**: Singleton, Scoped, Transient según el caso de uso
3. **Registro explícito**: Registrar servicios al inicio de la aplicación
4. **Evitar Service Locator**: No usar `IServiceProvider` directamente en lógica de negocio

---

## Service Lifetimes

.NET DI soporta tres lifetimes principales que determinan cuándo se crean y destruyen las instancias de servicios.

### Singleton

**Una única instancia** para toda la vida de la aplicación.

```csharp
namespace WebApi;

using Microsoft.Extensions.DependencyInjection;
using Application.Services;
using Domain.Interfaces.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Singleton para servicios stateless que se comparten
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IGuidGenerator, GuidGenerator>();
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

        return services;
    }
}
```

**Cuándo usar Singleton:**
- Servicios stateless (sin estado)
- Servicios costosos de crear
- Servicios thread-safe
- Configuraciones globales
- Caches en memoria

**Ejemplo de servicio Singleton:**

```csharp
namespace Application.Services;

using Domain.Interfaces.Services;

// ✅ CORRECTO: Stateless, thread-safe
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

public class GuidGenerator : IGuidGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}
```

### Scoped

**Una instancia por request** (en aplicaciones web) o por scope creado manualmente.

```csharp
namespace WebApi;

using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Users;
using Application.UseCases.Orders;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Scoped para handlers que procesan un request
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();

        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<PlaceOrderHandler>();

        return services;
    }
}
```

**Cuándo usar Scoped:**
- Handlers/Use Cases (un handler por request)
- Repositorios que usan DbContext/ISession
- Unit of Work
- Servicios que mantienen estado durante un request

**Ejemplo de servicio Scoped:**

```csharp
namespace Infrastructure.Persistence;

using NHibernate;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Scoped porque dependen de ISession (scoped)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
```

**Ejemplo de Repository Scoped:**

```csharp
namespace Infrastructure.Persistence.Repositories;

using NHibernate;
using Domain.Entities;
using Domain.Interfaces.Repositories;

// ✅ CORRECTO: Scoped - una instancia por request
public class UserRepository : IUserRepository
{
    private readonly ISession _session;

    // ISession es scoped, por lo tanto UserRepository debe ser scoped
    public UserRepository(ISession session)
    {
        _session = session;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _session.GetAsync<User>(id, ct);
    }

    public async Task SaveOrUpdateAsync(User user, CancellationToken ct)
    {
        await _session.SaveOrUpdateAsync(user, ct);
    }
}
```

### Transient

**Una nueva instancia cada vez** que se solicita el servicio.

```csharp
namespace WebApi;

using Microsoft.Extensions.DependencyInjection;
using Application.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransientServices(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Transient para servicios ligeros y stateless
        services.AddTransient<IPasswordHasher, PasswordHasher>();
        services.AddTransient<ITokenGenerator, JwtTokenGenerator>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
```

**Cuándo usar Transient:**
- Servicios ligeros (bajo costo de creación)
- Servicios stateless simples
- Servicios que no se comparten entre componentes

**Ejemplo de servicio Transient:**

```csharp
namespace Application.Services;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Domain.Interfaces.Services;

// ✅ CORRECTO: Transient - ligero y stateless
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 128 / 8;
    private const int HashSize = 256 / 8;
    private const int Iterations = 10000;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        byte[] hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);
        byte[] salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        for (int i = 0; i < HashSize; i++)
        {
            if (hashBytes[i + SaltSize] != hash[i])
                return false;
        }

        return true;
    }
}
```

### Tabla Comparativa de Lifetimes

| Lifetime | Instancias | Cuándo Usar | Ejemplo |
|----------|-----------|-------------|---------|
| **Singleton** | 1 por aplicación | Servicios stateless, costosos de crear, thread-safe | DateTimeProvider, Cache, Configuration |
| **Scoped** | 1 por request/scope | Handlers, Repositories con DbContext/Session | CreateUserHandler, UserRepository |
| **Transient** | 1 por inyección | Servicios ligeros, stateless | PasswordHasher, Validators |

### ❌ Problemas Comunes con Lifetimes

```csharp
// ❌ INCORRECTO: Singleton depende de Scoped (Captive Dependency)
public class SingletonService
{
    private readonly IUserRepository _userRepository; // IUserRepository es Scoped!

    public SingletonService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
}

// ✅ CORRECTO: Usar IServiceScopeFactory para resolver Scoped desde Singleton
public class SingletonService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SingletonService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task DoWorkAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Usar userRepository...
    }
}
```

---

## Constructor Injection vs Property Injection

### Constructor Injection (Preferido)

Constructor Injection es el patrón recomendado en .NET.

```csharp
namespace Application.UseCases.Users;

using FluentResults;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

// ✅ CORRECTO: Constructor Injection
public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<CreateUserHandler> _logger;

    // Todas las dependencias se inyectan por el constructor
    public CreateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        ILogger<CreateUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<User>> Handle(
        CreateUserCommand command,
        CancellationToken ct)
    {
        // Todas las dependencias están disponibles y no son null
        var hashedPassword = _passwordHasher.HashPassword(command.Password);

        var user = new User
        {
            Email = command.Email,
            PasswordHash = hashedPassword
        };

        await _userRepository.SaveOrUpdateAsync(user, ct);
        await _emailSender.SendWelcomeEmailAsync(user.Email, ct);

        _logger.LogInformation(
            "Usuario creado: {UserId}, Email: {Email}",
            user.Id,
            user.Email);

        return Result.Ok(user);
    }
}
```

**Ventajas de Constructor Injection:**
- ✅ Dependencias explícitas y visibles
- ✅ Inmutabilidad (las dependencias no cambian)
- ✅ Fácil de testear
- ✅ Null-safety (si la clase se construye, todas las dependencias existen)
- ✅ El contenedor de DI valida las dependencias al inicio

### Primary Constructor (C# 12+)

C# 12 introdujo Primary Constructors que simplifican la sintaxis:

```csharp
namespace Application.UseCases.Users;

using FluentResults;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

// ✅ CORRECTO: Primary Constructor (C# 12+)
public class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<CreateUserHandler> logger)
{
    // No es necesario declarar campos ni asignarlos
    // Los parámetros del constructor están disponibles en toda la clase

    public async Task<Result<User>> Handle(
        CreateUserCommand command,
        CancellationToken ct)
    {
        var hashedPassword = passwordHasher.HashPassword(command.Password);

        var user = new User
        {
            Email = command.Email,
            PasswordHash = hashedPassword
        };

        await userRepository.SaveOrUpdateAsync(user, ct);
        await emailSender.SendWelcomeEmailAsync(user.Email, ct);

        logger.LogInformation(
            "Usuario creado: {UserId}, Email: {Email}",
            user.Id,
            user.Email);

        return Result.Ok(user);
    }
}
```

### ❌ Property Injection (No Recomendado)

```csharp
// ❌ INCORRECTO: Property Injection
public class CreateUserHandler
{
    // Dependencias opcionales - pueden ser null
    public IUserRepository? UserRepository { get; set; }
    public IPasswordHasher? PasswordHasher { get; set; }
    public IEmailSender? EmailSender { get; set; }

    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // ❌ Necesitas null checks en todos lados
        if (UserRepository == null)
            throw new InvalidOperationException("UserRepository not set");

        if (PasswordHasher == null)
            throw new InvalidOperationException("PasswordHasher not set");

        // ... más null checks
    }
}
```

**Problemas con Property Injection:**
- ❌ Dependencias opcionales (pueden ser null)
- ❌ No es inmutable
- ❌ Difícil de testear
- ❌ No es obvio qué dependencias se necesitan
- ❌ Errores en runtime en lugar de compile-time

### Cuándo Usar Property Injection

Property Injection **solo** es aceptable en casos muy específicos:

```csharp
namespace Application.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ✅ ACEPTABLE: Property Injection para dependencia opcional (ILogger)
public class OptionalLoggerService
{
    // ILogger es realmente opcional - tiene un default (NullLogger)
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public void DoWork()
    {
        // Logger siempre existe (nunca es null)
        Logger.LogInformation("Doing work...");
    }
}
```

---

## Service Registration Patterns

### Registro Manual

```csharp
namespace WebApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Users;
using Application.UseCases.Orders;
using Application.Services;
using Infrastructure.Persistence.Repositories;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ✅ Registro manual - explícito y claro

        // Singleton
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();

        // Scoped
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<CreateUserHandler>();
        builder.Services.AddScoped<CreateOrderHandler>();

        // Transient
        builder.Services.AddTransient<IPasswordHasher, PasswordHasher>();
        builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

        var app = builder.Build();
        app.Run();
    }
}
```

### Extension Methods para Organización

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Users;
using Application.UseCases.Orders;
using Application.Services;
using Infrastructure.Persistence.Repositories;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;

// ✅ CORRECTO: Extension methods para agrupar registros relacionados
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationLayer(
        this IServiceCollection services)
    {
        // Use Cases
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();
        services.AddScoped<GetUserHandler>();

        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<PlaceOrderHandler>();
        services.AddScoped<CancelOrderHandler>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IPasswordHasher, PasswordHasher>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }

    public static IServiceCollection AddInfrastructureLayer(
        this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
```

**Uso en Program.cs:**

```csharp
namespace WebApi;

using Microsoft.AspNetCore.Builder;
using WebApi.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ✅ CORRECTO: Uso de extension methods
        builder.Services.AddApplicationLayer();
        builder.Services.AddInfrastructureLayer();

        var app = builder.Build();
        app.Run();
    }
}
```

### Registro con Factory

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Application.Services;
using Domain.Interfaces.Services;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ✅ CORRECTO: Factory para crear instancia con configuración
        services.AddTransient<IEmailSender>(provider =>
        {
            var smtpHost = configuration["Email:SmtpHost"]!;
            var smtpPort = int.Parse(configuration["Email:SmtpPort"]!);
            var username = configuration["Email:Username"]!;
            var password = configuration["Email:Password"]!;

            return new SmtpEmailSender(smtpHost, smtpPort, username, password);
        });

        return services;
    }
}
```

### Registro Condicional

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application.Services;
using Domain.Interfaces.Services;

public static class NotificationServiceExtensions
{
    public static IServiceCollection AddNotificationService(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        // ✅ CORRECTO: Registro condicional basado en entorno
        if (environment.IsDevelopment())
        {
            // En desarrollo, usar implementación fake
            services.AddTransient<IEmailSender, FakeEmailSender>();
        }
        else
        {
            // En producción, usar implementación real
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}
```

### TryAdd Methods

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Domain.Interfaces.Services;
using Application.Services;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddDefaultServices(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: TryAdd solo registra si no existe
        // Útil para librerías que no quieren sobrescribir registros del usuario
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.TryAddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
```

---

## Scrutor para Registro Automático

Scrutor es una librería que permite escanear assemblies y registrar servicios automáticamente basándose en convenciones.

### Instalación

```xml
<PackageReference Include="Scrutor" Version="4.2.2" />
```

### Registro por Convención

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Scrutor;

public static class ScrutorExtensions
{
    public static IServiceCollection AddApplicationLayerWithScrutor(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Escanear assembly y registrar por convención
        services.Scan(scan => scan
            // Escanear el assembly que contiene CreateUserHandler
            .FromAssemblyOf<Application.UseCases.Users.CreateUserHandler>()

            // Registrar todos los handlers (clases que terminan en "Handler")
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Handler")))
                .AsSelf() // Registrar como su propio tipo
                .WithScopedLifetime()

            // Registrar servicios que implementan interfaces específicas
            .AddClasses(classes => classes.AssignableTo<Domain.Interfaces.Services.ITransientService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()

            .AddClasses(classes => classes.AssignableTo<Domain.Interfaces.Services.ISingletonService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

        return services;
    }
}
```

### Interfaces Marker para Scrutor

```csharp
namespace Domain.Interfaces.Services;

// ✅ CORRECTO: Marker interfaces para Scrutor
public interface ITransientService { }
public interface IScopedService { }
public interface ISingletonService { }
```

**Uso en servicios:**

```csharp
namespace Application.Services;

using Domain.Interfaces.Services;

// ✅ CORRECTO: Implementar marker interface
public class PasswordHasher : IPasswordHasher, ITransientService
{
    public string HashPassword(string password)
    {
        // Implementación...
    }
}

public class DateTimeProvider : IDateTimeProvider, ISingletonService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

### Registro de Repositorios con Scrutor

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Scrutor;

public static class RepositoryRegistrationExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        // ✅ CORRECTO: Registrar todos los repositorios automáticamente
        services.Scan(scan => scan
            .FromAssemblyOf<Infrastructure.Persistence.Repositories.UserRepository>()

            // Todos los tipos que implementan IRepository (interfaz base)
            .AddClasses(classes => classes.AssignableTo<Domain.Interfaces.Repositories.IRepository>())
                .AsImplementedInterfaces() // IUserRepository, IOrderRepository, etc.
                .WithScopedLifetime());

        return services;
    }
}
```

### Decoration con Scrutor

```csharp
namespace WebApi.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Application.Decorators;
using Domain.Interfaces.Repositories;

public static class DecoratorExtensions
{
    public static IServiceCollection AddRepositoryDecorators(
        this IServiceCollection services)
    {
        // Primero registrar los repositorios base
        services.AddScoped<IUserRepository, UserRepository>();

        // ✅ CORRECTO: Decorar con logging
        services.Decorate<IUserRepository, LoggingUserRepositoryDecorator>();

        // ✅ CORRECTO: Decorar con caching
        services.Decorate<IUserRepository, CachingUserRepositoryDecorator>();

        // Resultado final: CachingUserRepositoryDecorator -> LoggingUserRepositoryDecorator -> UserRepository

        return services;
    }
}
```

**Implementación de Decorator:**

```csharp
namespace Application.Decorators;

using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

// ✅ CORRECTO: Decorator para logging
public class LoggingUserRepositoryDecorator : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ILogger<LoggingUserRepositoryDecorator> _logger;

    public LoggingUserRepositoryDecorator(
        IUserRepository inner,
        ILogger<LoggingUserRepositoryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Getting user {UserId}", id);

        var user = await _inner.GetByIdAsync(id, ct);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
        }

        return user;
    }

    public async Task SaveOrUpdateAsync(User user, CancellationToken ct)
    {
        _logger.LogInformation("Saving user {UserId}", user.Id);
        await _inner.SaveOrUpdateAsync(user, ct);
    }
}
```

---

## Evitar Service Locator Pattern

El Service Locator Pattern es un anti-patrón donde se usa `IServiceProvider` directamente para resolver dependencias.

### ❌ Service Locator (Anti-Patrón)

```csharp
namespace Application.UseCases.Users;

using System;
using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces.Repositories;

// ❌ INCORRECTO: Service Locator Pattern
public class CreateUserHandler
{
    private readonly IServiceProvider _serviceProvider;

    public CreateUserHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // ❌ INCORRECTO: Resolver dependencias manualmente
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        var passwordHasher = _serviceProvider.GetRequiredService<IPasswordHasher>();
        var emailSender = _serviceProvider.GetRequiredService<IEmailSender>();

        // ... usar los servicios
    }
}
```

**Problemas con Service Locator:**
- ❌ Oculta las dependencias (no son visibles en el constructor)
- ❌ Difícil de testear
- ❌ Viola el principio de Dependency Inversion
- ❌ Errores en runtime en lugar de compile-time
- ❌ Hace el código más difícil de entender

### ✅ Constructor Injection (Correcto)

```csharp
namespace Application.UseCases.Users;

using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;

// ✅ CORRECTO: Constructor Injection
public class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender)
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Dependencias ya disponibles
        var hashedPassword = passwordHasher.HashPassword(command.Password);
        // ...
    }
}
```

### Casos Excepcionales: IServiceScopeFactory

Hay casos legítimos donde necesitas `IServiceScopeFactory`:

```csharp
namespace Infrastructure.BackgroundServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.UseCases.Orders;

// ✅ CORRECTO: IServiceScopeFactory en BackgroundService (Singleton)
public class OrderProcessingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderProcessingBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // ✅ CORRECTO: Crear scope para resolver servicios Scoped
            using (var scope = scopeFactory.CreateScope())
            {
                // Resolver handlers que son Scoped
                var processOrderHandler = scope.ServiceProvider
                    .GetRequiredService<ProcessOrderHandler>();

                try
                {
                    await processOrderHandler.ProcessPendingOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing orders");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

---

## Testing con DI

### Testing sin Container (Preferido)

```csharp
namespace Application.Tests.UseCases.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using FluentAssertions;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

[TestFixture]
public class CreateUserHandlerTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IPasswordHasher> _passwordHasherMock;
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<ILogger<CreateUserHandler>> _loggerMock;
    private CreateUserHandler _sut;

    [SetUp]
    public void SetUp()
    {
        // ✅ CORRECTO: Crear mocks directamente
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _emailSenderMock = new Mock<IEmailSender>();
        _loggerMock = new Mock<ILogger<CreateUserHandler>>();

        // ✅ System Under Test (SUT) - inyección manual
        _sut = new CreateUserHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _emailSenderMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);

        _userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.Is<User>(u => u.Email == command.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailSenderMock.Verify(
            x => x.SendWelcomeEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_DuplicateEmail_ReturnsFail()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var existingUser = new User { Email = command.Email };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("ya está registrado"));

        _userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

### Testing con Container (Integration Tests)

```csharp
namespace Application.Tests.Integration;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using FluentAssertions;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence.InMemory;

[TestFixture]
public class CreateUserHandlerIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private CreateUserHandler _sut;

    [SetUp]
    public void SetUp()
    {
        // ✅ CORRECTO: Configurar container para integration tests
        var services = new ServiceCollection();

        // Registrar servicios reales
        services.AddScoped<CreateUserHandler>();

        // Usar implementaciones in-memory para repositorios
        services.AddScoped<IUserRepository, InMemoryUserRepository>();

        // Servicios reales
        services.AddTransient<IPasswordHasher, PasswordHasher>();
        services.AddTransient<IEmailSender, FakeEmailSender>();

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _sut = _serviceProvider.GetRequiredService<CreateUserHandler>();
    }

    [Test]
    public async Task Handle_EndToEnd_CreatesUserSuccessfully()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "integration@example.com",
            Password = "Password123!",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verificar que el usuario fue creado en el repositorio
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        var savedUser = await userRepository.GetByEmailAsync(
            command.Email,
            CancellationToken.None);

        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(command.Email);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }
}
```

### WebApplicationFactory para API Tests

```csharp
namespace WebApi.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using FluentAssertions;
using Domain.Interfaces.Services;

[TestFixture]
public class UserEndpointsTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void SetUp()
    {
        // ✅ CORRECTO: Sobrescribir servicios para testing
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Reemplazar EmailSender con implementación fake
                    services.RemoveAll<IEmailSender>();
                    services.AddTransient<IEmailSender, FakeEmailSender>();
                });
            });

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task CreateUser_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
```

---

## Anti-Patrones Comunes

### ❌ Anti-Patrón 1: Captive Dependency

```csharp
// ❌ INCORRECTO: Singleton captura dependencia Scoped
public class SingletonService
{
    private readonly IUserRepository _userRepository; // Scoped!

    public SingletonService(IUserRepository userRepository)
    {
        _userRepository = userRepository; // ❌ Problema de lifetime
    }
}
```

```csharp
// ✅ CORRECTO: Usar IServiceScopeFactory
public class SingletonService(IServiceScopeFactory scopeFactory)
{
    public async Task DoWorkAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Usar repository...
    }
}
```

### ❌ Anti-Patrón 2: Registro Circular

```csharp
// ❌ INCORRECTO: Dependencias circulares
public class ServiceA(ServiceB serviceB) { }
public class ServiceB(ServiceA serviceA) { } // ❌ Circular!

// Registro:
services.AddScoped<ServiceA>();
services.AddScoped<ServiceB>();
// Esto fallará en runtime
```

```csharp
// ✅ CORRECTO: Extraer interface o refactorizar
public interface IServiceAOperations
{
    void DoSomething();
}

public class ServiceA(ServiceB serviceB) : IServiceAOperations
{
    public void DoSomething() { /* ... */ }
}

public class ServiceB(IServiceAOperations serviceA) { }

// Ahora no hay ciclo
```

### ❌ Anti-Patrón 3: New Keyword en Lugar de DI

```csharp
namespace Application.UseCases.Users;

using Domain.Interfaces.Repositories;
using Infrastructure.Persistence.Repositories;

// ❌ INCORRECTO: Crear instancias con new
public class CreateUserHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // ❌ No usar DI
        var userRepository = new UserRepository();
        var passwordHasher = new PasswordHasher();

        // ...
    }
}
```

```csharp
// ✅ CORRECTO: Inyectar dependencias
public class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    public async Task Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Usar dependencias inyectadas
    }
}
```

### ❌ Anti-Patrón 4: Ambient Context / Singleton Stático

```csharp
// ❌ INCORRECTO: Singleton estático
public static class UserContext
{
    private static User? _currentUser;

    public static User? CurrentUser
    {
        get => _currentUser;
        set => _currentUser = value;
    }
}

// ❌ Problemas: no thread-safe, difícil de testear, acoplamiento global
```

```csharp
// ✅ CORRECTO: Usar IHttpContextAccessor y servicios Scoped
namespace Application.Services;

using Microsoft.AspNetCore.Http;
using Domain.Entities;

public interface ICurrentUserService
{
    User? CurrentUser { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public User? CurrentUser
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User
                .FindFirst("user_id")?.Value;

            if (userId == null)
                return null;

            // Resolver User desde claims o base de datos
            return null; // Implementación completa...
        }
    }
}

// Registro como Scoped
// services.AddScoped<ICurrentUserService, CurrentUserService>();
```

### ❌ Anti-Patrón 5: Demasiadas Dependencias (God Object)

```csharp
// ❌ INCORRECTO: Demasiadas dependencias (God Object)
public class OrderService(
    IUserRepository userRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IPaymentService paymentService,
    IShippingService shippingService,
    INotificationService notificationService,
    ITaxCalculationService taxCalculationService,
    IDiscountService discountService,
    ILogger<OrderService> logger,
    IEmailSender emailSender,
    ISmsSender smsSender)
{
    // ❌ Clase hace demasiado - viola SRP
}
```

```csharp
// ✅ CORRECTO: Dividir en múltiples handlers específicos
public class CreateOrderHandler(
    IUserRepository userRepository,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    IInventoryService inventoryService)
{
    // Responsabilidad única: crear orden
}

public class ProcessPaymentHandler(
    IOrderRepository orderRepository,
    IPaymentService paymentService,
    INotificationService notificationService)
{
    // Responsabilidad única: procesar pago
}

public class ShipOrderHandler(
    IOrderRepository orderRepository,
    IShippingService shippingService,
    IEmailSender emailSender)
{
    // Responsabilidad única: enviar orden
}
```

### ❌ Anti-Patrón 6: Registrar Concrete Types

```csharp
// ❌ INCORRECTO: Registrar tipos concretos en lugar de interfaces
services.AddScoped<UserRepository>(); // ❌ No hay abstracción
```

```csharp
// ✅ CORRECTO: Registrar interfaces
services.AddScoped<IUserRepository, UserRepository>();
```

---

## Checklists

### Checklist: Configurar DI en Nueva Aplicación

- [ ] **Instalación de paquetes**
  - [ ] `Microsoft.Extensions.DependencyInjection` (incluido en ASP.NET Core)
  - [ ] `Scrutor` (opcional, para registro automático)

- [ ] **Estructura de registro**
  - [ ] Crear extension methods para cada capa (Application, Infrastructure)
  - [ ] Agrupar registros por funcionalidad
  - [ ] Usar lifetimes apropiados

- [ ] **Application Layer**
  - [ ] Registrar todos los handlers como Scoped
  - [ ] Registrar servicios de aplicación (Singleton o Transient según caso)
  - [ ] Registrar validators como Scoped

- [ ] **Infrastructure Layer**
  - [ ] Registrar repositorios como Scoped
  - [ ] Registrar DbContext/ISession como Scoped
  - [ ] Registrar servicios externos (Email, SMS, etc.)

- [ ] **Validación**
  - [ ] Verificar que no hay Captive Dependencies
  - [ ] Verificar que no hay dependencias circulares
  - [ ] Ejecutar aplicación y verificar que DI resuelve correctamente

### Checklist: Agregar Nuevo Servicio

- [ ] **Definir interface**
  - [ ] Crear interface en Domain o Application
  - [ ] Documentar el propósito de la interface

- [ ] **Implementar servicio**
  - [ ] Crear implementación en Infrastructure o Application
  - [ ] Usar Constructor Injection para dependencias
  - [ ] Evitar dependencias de otras capas incorrectas

- [ ] **Determinar lifetime**
  - [ ] ¿Es stateless? → Considerar Singleton o Transient
  - [ ] ¿Mantiene estado por request? → Scoped
  - [ ] ¿Depende de servicios Scoped (DbContext)? → Scoped
  - [ ] ¿Es ligero de crear? → Transient
  - [ ] ¿Es costoso de crear? → Singleton o Scoped

- [ ] **Registrar servicio**
  - [ ] Agregar registro en extension method apropiado
  - [ ] Usar lifetime correcto
  - [ ] Si usa Scrutor, implementar marker interface

- [ ] **Testing**
  - [ ] Crear unit tests con mocks
  - [ ] Verificar que el servicio se resuelve correctamente
  - [ ] Verificar comportamiento esperado

### Checklist: Code Review - DI

- [ ] **Constructor Injection**
  - [ ] Todas las dependencias se inyectan por constructor
  - [ ] No se usa Property Injection (excepto casos justificados)
  - [ ] No se usa Service Locator Pattern

- [ ] **Lifetimes**
  - [ ] Lifetimes son apropiados para cada servicio
  - [ ] No hay Captive Dependencies (Singleton → Scoped)
  - [ ] Servicios Singleton son thread-safe

- [ ] **Registros**
  - [ ] Todos los servicios están registrados
  - [ ] Se usan interfaces en lugar de tipos concretos
  - [ ] Registros están organizados en extension methods

- [ ] **Dependencias**
  - [ ] No hay dependencias circulares
  - [ ] Número de dependencias por clase es razonable (<5)
  - [ ] No se usa `new` para crear servicios que deberían ser inyectados

- [ ] **Testing**
  - [ ] Servicios son fáciles de mockear
  - [ ] Tests no dependen del container (unit tests)
  - [ ] Integration tests usan container real

### Checklist: Migrar a Scrutor

- [ ] **Preparación**
  - [ ] Instalar Scrutor
  - [ ] Crear marker interfaces (ITransientService, IScopedService, ISingletonService)
  - [ ] Identificar convenciones de nombres (ej: *Handler, *Repository)

- [ ] **Implementación**
  - [ ] Implementar marker interfaces en servicios
  - [ ] Configurar escaneo de assemblies
  - [ ] Configurar filtros para cada tipo de servicio

- [ ] **Validación**
  - [ ] Ejecutar aplicación y verificar registros
  - [ ] Verificar que todos los servicios se resuelven
  - [ ] Ejecutar tests para verificar comportamiento

- [ ] **Limpieza**
  - [ ] Eliminar registros manuales redundantes
  - [ ] Documentar convenciones de Scrutor
  - [ ] Actualizar guías de desarrollo del equipo

---

## Referencias

### Documentación Oficial Microsoft

- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Service Lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Dependency Injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [Testing with Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

### Librerías

- [Scrutor](https://github.com/khellang/Scrutor) - Assembly scanning and decoration extensions
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/) - Built-in DI container

### Artículos y Recursos

- [Dependency Injection Principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles#dependency-inversion)
- [SOLID Principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Inversion of Control Containers and the Dependency Injection pattern](https://martinfowler.com/articles/injection.html) - Martin Fowler

---

**Última actualización:** 2025-11-13
**Versión del documento:** 1.0.0
