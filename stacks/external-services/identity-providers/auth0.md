# Auth0 Integration Guide

**Version:** 1.0.0
**Status:** ✅ Completed
**Last Updated:** 2025-01-14

## Tabla de Contenidos

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Contexto del Proyecto](#contexto-del-proyecto)
3. [Anti-Patrones Identificados](#anti-patrones-identificados)
4. [Arquitectura Recomendada](#arquitectura-recomendada)
5. [Configuración de Auth0](#configuración-de-auth0)
6. [Implementación con Auth0.NET SDK](#implementación-con-auth0net-sdk)
7. [Mock para Desarrollo/Testing](#mock-para-desarrollotesting)
8. [Dependency Injection](#dependency-injection)
9. [Best Practices](#best-practices)
10. [Errores Comunes](#errores-comunes)

---

## 1. Resumen Ejecutivo

Esta guía documenta la integración con **Auth0** como Identity Provider en el proyecto **hashira.stone.backend**, identificando anti-patrones en la implementación actual y proporcionando soluciones basadas en el SDK oficial **Auth0.NET** y mejores prácticas de .NET.

**Puntos clave:**

- **Anti-patrón crítico:** Uso de `new HttpClient()` que causa **socket exhaustion**
- **Anti-patrón crítico:** Uso de `.Result` que causa **thread starvation** y deadlocks
- **Solución:** Migrar a **Auth0.NET SDK** con Management API Client
- **Solución alternativa:** Refactorizar con **IHttpClientFactory** + Typed HttpClient
- **Mock Pattern:** Implementar mocks para desarrollo/testing sin Auth0 real

**Referencias del proyecto:**
- [Auth0Service.cs:195-199](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\services\Auth0Service.cs#L195-L199) - Anti-patrón `new HttpClient()`
- [Auth0Service.cs:147-151](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\services\Auth0Service.cs#L147-L151) - Deserialización con tipos anónimos
- [IIdentityService.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\services\IIdentityService.cs) - Interface en Domain Layer

---

## 2. Contexto del Proyecto

### 2.1. Stack Tecnológico

**Proyecto de referencia:** hashira.stone.backend

**Estructura:**

```
hashira.stone.backend/
├── src/
│   ├── hashira.stone.backend.domain/
│   │   └── interfaces/services/
│   │       └── IIdentityService.cs        ← Interface en Domain
│   ├── hashira.stone.backend.infrastructure/
│   │   └── services/
│   │       ├── Auth0Service.cs            ← Implementación (anti-patrones)
│   │       └── Auth0ServiceMock.cs        ← Mock para testing
│   └── hashira.stone.backend.webapi/
│       └── infrastructure/
│           └── ServiceCollectionExtender.cs ← DI registration
```

### 2.2. Interface IIdentityService

**Archivo:** [IIdentityService.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\services\IIdentityService.cs)

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.services;

public interface IIdentityService
{
    User Create(string username, string name, string password);
    User? GetByUserName(string userName);
    User? GetByEmail(string userName);
    User? ChangePassword(string userName, string newPassword);
}
```

⚠️ **Nota:** Métodos síncronos = anti-patrón para I/O. Debe usar async/await.

---

## 3. Anti-Patrones Identificados

### 3.1. ❌ Socket Exhaustion con `new HttpClient()`

[Auth0Service.cs:195-199](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\services\Auth0Service.cs#L195-L199)

```csharp
// ❌ ANTI-PATTERN
private static HttpResponseMessage PostAsync(string requestUri, object? body)
{
    using (var httpClient = new HttpClient())  // ← Nueva instancia cada vez
    {
        var content = new StringContent(
            JsonConvert.SerializeObject(body),
            Encoding.UTF8,
            "application/json");
        return httpClient.PostAsync(requestUri, content).Result;
    }
}
```

**Impacto:** Con 100 req/seg → 24,000 sockets `TIME_WAIT` en 4 min → SocketException

### 3.2. ❌ Thread Starvation con `.Result`

```csharp
// ❌ ANTI-PATTERN
return httpClient.PostAsync(requestUri, content).Result;  // ← Bloquea thread
```

**Problema:** Deadlocks en ASP.NET Framework con SynchronizationContext

### 3.3. ❌ Validación Tardía

```csharp
// ❌ ANTI-PATTERN: Valida en cada request
if (string.IsNullOrEmpty(clientId))
    throw new ConfigurationErrorsException("ClientId not set");
```

✅ **Solución:** ValidateOnStart

---

## 4. Arquitectura Recomendada

### Opción 1: Auth0.NET SDK (✅ Recomendado)

✅ SDK oficial mantenido
✅ HttpClient pooling automático
✅ Type-safe con tipos fuertemente tipados
✅ Async/await nativo

### Opción 2: IHttpClientFactory

✅ Control total sobre HTTP
✅ Integración con Polly
⚠️ Más boilerplate

---

## 5. Configuración

### 5.1. appsettings.json

```json
{
  "Auth0ManagementSettings": {
    "Domain": "https://tu-tenant.us.auth0.com",
    "ClientId": "abc123...",
    "ClientSecret": "xyz789...",
    "Audience": "https://tu-tenant.us.auth0.com/api/v2/",
    "GrantType": "client_credentials",
    "Connection": "Username-Password-Authentication"
  }
}
```

### 5.2. Options Pattern

```csharp
public class Auth0ManagementOptions
{
    public const string SectionName = "Auth0ManagementSettings";

    [Required, Url]
    public string Domain { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    [Required, Url]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string GrantType { get; set; } = "client_credentials";

    [Required]
    public string Connection { get; set; } = string.Empty;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
```

---

## 6. Implementación con Auth0.NET SDK

### 6.1. Instalación

```bash
dotnet add package Auth0.ManagementApi
dotnet add package Auth0.AuthenticationApi
```

### 6.2. Interface Async

```csharp
public interface IIdentityService
{
    Task<User> CreateAsync(string username, string name, string password,
        CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
    Task<User?> ChangePasswordAsync(string userName, string newPassword,
        CancellationToken ct = default);
}
```

### 6.3. Auth0Service con SDK

```csharp
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

public class Auth0Service : IIdentityService
{
    private readonly Auth0ManagementOptions _options;
    private readonly ILogger<Auth0Service> _logger;
    private readonly IAuthenticationApiClient _authClient;

    public Auth0Service(
        IOptions<Auth0ManagementOptions> options,
        ILogger<Auth0Service> logger)
    {
        _options = options.Value;
        _logger = logger;
        _authClient = new AuthenticationApiClient(new Uri(_options.Domain));
    }

    private async Task<IManagementApiClient> GetManagementClientAsync(
        CancellationToken ct = default)
    {
        var tokenResponse = await _authClient.GetTokenAsync(
            new ClientCredentialsTokenRequest
            {
                ClientId = _options.ClientId,
                ClientSecret = _options.ClientSecret,
                Audience = _options.Audience
            }, ct);

        return new ManagementApiClient(
            tokenResponse.AccessToken,
            new Uri(_options.Domain));
    }

    public async Task<User> CreateAsync(
        string username,
        string name,
        string password,
        CancellationToken ct = default)
    {
        try
        {
            var client = await GetManagementClientAsync(ct);

            var auth0User = await client.Users.CreateAsync(
                new UserCreateRequest
                {
                    Email = username,
                    Name = name,
                    Password = password,
                    Connection = _options.Connection,
                    EmailVerified = false
                }, ct);

            _logger.LogInformation(
                "Usuario creado: {UserId} - {Email}",
                auth0User.UserId,
                auth0User.Email);

            return MapToDomainUser(auth0User);
        }
        catch (Auth0.Core.Exceptions.ApiException ex)
            when (ex.Message.Contains("user already exists"))
        {
            _logger.LogWarning("Usuario duplicado: {Email}", username);
            throw new ArgumentException("errors.userEmail.alreadyExist");
        }
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        var client = await GetManagementClientAsync(ct);

        var users = await client.Users.GetAllAsync(
            new GetUsersRequest { Query = $"email:\"{email}\"" },
            new PaginationInfo(0, 1, false),
            ct);

        if (users == null || !users.Any())
            return null;

        return MapToDomainUser(users.First());
    }

    public async Task<User?> ChangePasswordAsync(
        string userName,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await GetByUserNameAsync(userName, ct);
        if (user == null)
            throw new ResourceNotFoundException($"User not found: {userName}");

        var client = await GetManagementClientAsync(ct);

        var auth0User = await client.Users.UpdateAsync(
            user.UserId,
            new UserUpdateRequest
            {
                Password = newPassword,
                Connection = _options.Connection
            }, ct);

        _logger.LogInformation("Password actualizado: {UserId}", auth0User.UserId);
        return MapToDomainUser(auth0User);
    }

    private static User MapToDomainUser(Auth0.ManagementApi.Models.User auth0User) =>
        new User
        {
            UserId = auth0User.UserId,
            Email = auth0User.Email,
            Name = auth0User.FullName ?? auth0User.NickName ?? auth0User.Email,
            CreationDate = auth0User.CreatedAt ?? DateTime.UtcNow
        };
}
```

---

## 7. Mock para Desarrollo/Testing

```csharp
public class Auth0ServiceMock : IIdentityService
{
    private readonly ILogger<Auth0ServiceMock> _logger;
    private readonly Dictionary<string, User> _users = new();
    private int _userCounter = 1;

    public Auth0ServiceMock(ILogger<Auth0ServiceMock> logger)
    {
        _logger = logger;
        _users["test@example.com"] = new User
        {
            UserId = "auth0|mock-001",
            Email = "test@example.com",
            Name = "Test User",
            CreationDate = DateTime.UtcNow.AddDays(-30)
        };
    }

    public Task<User> CreateAsync(
        string username,
        string name,
        string password,
        CancellationToken ct = default)
    {
        _logger.LogInformation("MOCK: Creando usuario: {Email}", username);

        if (_users.ContainsKey(username))
            throw new ArgumentException("errors.userEmail.alreadyExist");

        var user = new User
        {
            UserId = $"auth0|mock-{_userCounter++:D3}",
            Email = username,
            Name = name,
            CreationDate = DateTime.UtcNow
        };

        _users[username] = user;
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        _users.TryGetValue(email, out var user);
        _logger.LogInformation("MOCK: Usuario {Status}: {Email}",
            user != null ? "encontrado" : "no encontrado",
            email);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default) =>
        GetByEmailAsync(userName, ct);

    public async Task<User?> ChangePasswordAsync(
        string userName,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await GetByUserNameAsync(userName, ct);
        if (user == null)
            throw new ResourceNotFoundException($"User not found: {userName}");

        _logger.LogInformation("MOCK: Password actualizado: {UserId}", user.UserId);
        return user;
    }
}
```

---

## 8. Dependency Injection

```csharp
public static IServiceCollection AddAuth0IdentityService(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    // Configuración con validación en startup
    services.AddOptions<Auth0ManagementOptions>()
        .Bind(configuration.GetSection(Auth0ManagementOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();  // ← Falla al iniciar si inválida

    // Registro según entorno
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        services.AddScoped<IIdentityService, Auth0ServiceMock>();
    else
        services.AddScoped<IIdentityService, Auth0Service>();

    return services;
}
```

**Uso en Program.cs:**

```csharp
builder.Services.AddAuth0IdentityService(builder.Configuration, builder.Environment);
```

---

## 9. Best Practices

### ✅ 1. Usar Auth0.NET SDK

```csharp
// ✅ RECOMENDADO
var authClient = new AuthenticationApiClient(new Uri(domain));
var token = await authClient.GetTokenAsync(...);
```

### ✅ 2. Async/Await Everywhere

```csharp
// ✅ RECOMENDADO
public async Task<User> CreateAsync(..., CancellationToken ct = default)
{
    var auth0User = await client.Users.CreateAsync(request, ct);
    return MapToDomainUser(auth0User);
}

// ❌ EVITAR
public User Create(...)
{
    var auth0User = client.Users.CreateAsync(request).Result; // ← Deadlock
    return MapToDomainUser(auth0User);
}
```

### ✅ 3. Validar Configuración en Startup

```csharp
// ✅ RECOMENDADO
services.AddOptions<Auth0ManagementOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();  // ← Falla al iniciar

// ❌ EVITAR: Validación en cada request
if (string.IsNullOrEmpty(clientId))
    throw new ConfigurationErrorsException(...);
```

### ✅ 4. Mock Pattern

```csharp
// ✅ RECOMENDADO
if (environment.IsDevelopment())
    services.AddScoped<IIdentityService, Auth0ServiceMock>();
else
    services.AddScoped<IIdentityService, Auth0Service>();
```

### ✅ 5. Logging Estructurado

```csharp
// ✅ RECOMENDADO
_logger.LogInformation("Usuario creado: {UserId} - {Email}", userId, email);

// ❌ EVITAR
_logger.LogInformation($"Usuario creado: {userId} - {email}");
```

### ✅ 6. Exception Handling

```csharp
// ✅ RECOMENDADO: Convertir excepciones de infraestructura a dominio
catch (Auth0.Core.Exceptions.ApiException ex)
    when (ex.Message.Contains("user already exists"))
{
    throw new ArgumentException("errors.userEmail.alreadyExist");
}
```

### ✅ 7. Token Caching (opcional)

```csharp
// ✅ Para alto volumen
if (_cache.TryGetValue("Auth0Token", out string token))
    return token;

var newToken = await _authClient.GetTokenAsync(...);
_cache.Set("Auth0Token", newToken, TimeSpan.FromHours(23)); // Expira en 24h
```

---

## 10. Errores Comunes

### ❌ Socket Exhaustion

**Causa:**
```csharp
using (var httpClient = new HttpClient()) { ... }  // ← Crea socket cada vez
```

**Solución:**
```csharp
// Opción 1: Auth0.NET SDK (recomendado)
var authClient = new AuthenticationApiClient(new Uri(domain));

// Opción 2: IHttpClientFactory
var client = _httpClientFactory.CreateClient("Auth0");
```

### ❌ Deadlock

**Causa:**
```csharp
var result = asyncMethod().Result;  // ← Bloquea thread
```

**Solución:**
```csharp
var result = await asyncMethod();  // ← Async/await
```

### ❌ Rate Limit (429)

**Solución:**
```csharp
services.AddHttpClient("Auth0")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

### ❌ Token Expired

**Solución:**
```csharp
// Obtener nuevo token en cada operación
private async Task<IManagementApiClient> GetManagementClientAsync(...)
{
    var tokenResponse = await _authClient.GetTokenAsync(...);
    return new ManagementApiClient(tokenResponse.AccessToken, ...);
}
```

---

## Conclusión

Esta guía documenta la integración con Auth0 en hashira.stone.backend, identificando anti-patrones críticos y proporcionando soluciones con Auth0.NET SDK.

**Key Takeaways:**

1. ✅ **Evitar `new HttpClient()`** → Auth0.NET SDK o IHttpClientFactory
2. ✅ **Evitar `.Result`** → async/await everywhere
3. ✅ **Validar configuración en startup** → ValidateOnStart
4. ✅ **Mock Pattern** → Testing sin Auth0 real
5. ✅ **Logging estructurado** → Mejor observability
6. ✅ **Exception handling** → Convertir a domain exceptions

**Referencias:**

- [Auth0.NET SDK](https://auth0.github.io/auth0.net/)
- [Auth0 Management API](https://auth0.com/docs/api/management/v2)
- [IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)

---

**Versión:** 1.0.0
**Proyecto de Referencia:** hashira.stone.backend
**Última Actualización:** 2025-01-14

