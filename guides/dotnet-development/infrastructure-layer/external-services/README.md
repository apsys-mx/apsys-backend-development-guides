# External Services - Infrastructure Layer

**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [¿Qué son los External Services?](#qué-son-los-external-services)
2. [Responsabilidades](#responsabilidades)
3. [Patrones Principales](#patrones-principales)
4. [Stack Tecnológico](#stack-tecnológico)
5. [Guías Disponibles](#guías-disponibles)
6. [Arquitectura de Implementación](#arquitectura-de-implementación)
7. [Ejemplos Completos](#ejemplos-completos)
8. [Mejores Prácticas](#mejores-prácticas)
9. [Referencias](#referencias)

---

## ¿Qué son los External Services?

Los **External Services** son implementaciones de Infrastructure Layer que **conectan tu aplicación con servicios externos de terceros**: proveedores de identidad (Auth0, IdentityServer), APIs REST, servicios de email, caching, storage, etc.

Estos servicios son **adaptadores** que traducen entre las interfaces definidas en **Domain** y las APIs externas concretas.

### 🎯 Características Clave

- ✅ **Abstracción**: Domain define interfaces, Infrastructure implementa contra APIs externas
- ✅ **Intercambiabilidad**: Cambiar de Auth0 a IdentityServer solo requiere nueva implementación
- ✅ **Testabilidad**: Mocks para Development/Testing, implementaciones reales para Production
- ✅ **Configuración externa**: Settings desde IConfiguration/Environment Variables
- ✅ **HttpClient correcto**: Uso de IHttpClientFactory (no `new HttpClient()`)
- ✅ **Manejo de errores**: Excepciones de dominio apropiadas

---

## Responsabilidades

### ✅ SÍ hace External Services

- **Implementar interfaces de servicios**: IIdentityService, IEmailService, ICacheService
- **Llamadas HTTP**: Integrar con APIs REST de terceros
- **Autenticación/Autorización externa**: Auth0, OAuth2, JWT
- **Caching**: Memory cache, Redis, distributed cache
- **Envío de emails/SMS**: SendGrid, Twilio, SMTP
- **Storage externo**: S3, Azure Blob, Google Cloud Storage
- **Logging externo**: Sentry, Application Insights
- **Manejo de rate limiting**: Retry policies, circuit breakers

### ❌ NO hace External Services

- **Lógica de negocio**: Esta va en Domain o Application
- **Definir interfaces**: Las interfaces se definen en Domain
- **Validación de dominio**: Esto va en Domain con FluentValidation
- **Orquestación de casos de uso**: Esto va en Application
- **Exponer detalles de implementación**: La API externa no debe filtrarse

---

## Patrones Principales

### 1. 🔌 Service Adapter Pattern

Adaptador entre interfaz de Domain y API externa:

```
Domain Layer                Infrastructure Layer                 External API
    ↓                              ↓                                  ↓
IIdentityService   →   Auth0Service implements IIdentityService  →  Auth0 API
                       - GetByEmail()
                       - Create()
                       - ChangePassword()
```

**Flujo**:
```
Application UseCase
    ↓
_identityService.Create(email, name, password)  ← Interface de Domain
    ↓
Auth0Service.Create()  ← Implementación en Infrastructure
    ↓
HTTP POST https://YOUR_DOMAIN.auth0.com/api/v2/users  ← API externa
```

---

### 2. 🧪 Mock Pattern para Development/Testing

Registro condicional basado en ambiente:

```csharp
public static IServiceCollection ConfigureIdentityService(
    this IServiceCollection services,
    IWebHostEnvironment environment)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        // ✅ Mock: sin llamadas reales a API externa
        services.AddScoped<IIdentityService, Auth0ServiceMock>();
    }
    else
    {
        // ✅ Real: conecta con Auth0
        services.AddScoped<IIdentityService, Auth0Service>();
    }
    return services;
}
```

**Ventajas**:
- ✅ **Desarrollo sin dependencias externas**: No necesitas cuenta de Auth0 para dev
- ✅ **Tests más rápidos**: Sin latencia de red
- ✅ **Sin costos**: No consumes cuota de APIs externas
- ✅ **Determinístico**: Respuestas predecibles en tests

---

### 3. 🌐 HttpClient con IHttpClientFactory

**❌ ANTI-PATTERN** (proyecto de referencia):
```csharp
// ❌ INCORRECTO: Crea nuevo HttpClient cada vez
private static HttpResponseMessage PostAsync(string requestUri, object? body)
{
    using (var httpClient = new HttpClient())  // ← ANTI-PATTERN
    {
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        return httpClient.PostAsync(requestUri, content).Result;  // ← Blocking call
    }
}
```

**Problemas**:
- ❌ **Socket Exhaustion**: Cada `new HttpClient()` abre nuevas conexiones
- ❌ **No respeta DNS changes**: HttpClient cachea DNS indefinidamente
- ❌ **Blocking I/O**: `.Result` bloquea el thread

**✅ CORRECTO** (best practice):
```csharp
public class Auth0Service : IIdentityService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public Auth0Service(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        var client = _httpClientFactory.CreateClient("Auth0");  // ✅ Pooled HttpClient
        var authToken = await GetTokenAccessValueAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var body = new { email = username, name, password, connection = GetConnection() };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v2/users", content);  // ✅ Async
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var userInfo = JsonConvert.DeserializeObject<Auth0Response>(result);

        return new User { Email = username, Name = name };
    }
}
```

**Registro en DI**:
```csharp
services.AddHttpClient("Auth0", client =>
{
    client.BaseAddress = new Uri(configuration["Auth0ManagementSettings:Domain"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**Ventajas**:
- ✅ **Connection pooling**: Reutiliza conexiones HTTP
- ✅ **DNS refresh**: Respeta cambios de DNS
- ✅ **Configuración centralizada**: Base address, timeouts, policies
- ✅ **Async/Await**: No bloquea threads

---

### 4. 📝 Configuration desde IConfiguration

**Nunca hardcodear settings**:

```csharp
public class Auth0Service : IIdentityService
{
    private readonly IConfiguration _configuration;
    private const string Auth0DomainKey = "Auth0ManagementSettings:Domain";
    private const string Auth0ConnectionKey = "Auth0ManagementSettings:Connection";

    public Auth0Service(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetDomain()
    {
        var domain = _configuration.GetSection(Auth0DomainKey).Value;
        if (string.IsNullOrEmpty(domain))
            throw new ConfigurationErrorsException($"No [{Auth0DomainKey}] value set in configuration file");
        return domain;
    }
}
```

**appsettings.json**:
```json
{
  "Auth0ManagementSettings": {
    "Domain": "https://your-tenant.auth0.com",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Audience": "https://your-tenant.auth0.com/api/v2/",
    "GrantType": "client_credentials",
    "Connection": "Username-Password-Authentication"
  }
}
```

**Environment Variables (Production)**:
```bash
Auth0ManagementSettings__Domain=https://prod-tenant.auth0.com
Auth0ManagementSettings__ClientSecret=prod-secret-from-vault
```

---

## Stack Tecnológico

### Proyecto de Referencia: hashira.stone.backend

**Servicios Externos Implementados**:
```xml
<ItemGroup>
  <!-- HTTP Client -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="9.0+" />

  <!-- JSON Serialization -->
  <PackageReference Include="Newtonsoft.Json" Version="13.0+" />

  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0+" />
</ItemGroup>
```

**Servicios Disponibles**:
- ✅ **Auth0Service** - Identity provider (Create, Get, ChangePassword)
- ✅ **Auth0ServiceMock** - Mock para dev/testing

**No implementado en proyecto de referencia** (documentar best practices):
- ⏳ **Caching** - IMemoryCache, IDistributedCache
- ⏳ **Email** - SendGrid, SMTP
- ⏳ **Storage** - S3, Azure Blob

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de External Services |
| [http-clients.md](./http-clients.md) | ⏳ Pendiente | IHttpClientFactory patterns |
| [identity-providers/README.md](./identity-providers/README.md) | ⏳ Pendiente | Auth providers overview |
| [identity-providers/auth0.md](./identity-providers/auth0.md) | ⏳ Pendiente | Auth0 integration |
| [identity-providers/custom-jwt.md](./identity-providers/custom-jwt.md) | ⏳ Pendiente | Custom JWT implementation |
| [caching/README.md](./caching/README.md) | ⏳ Pendiente | Caching overview |
| [caching/memory-cache.md](./caching/memory-cache.md) | ⏳ Pendiente | IMemoryCache implementation |
| [caching/redis.md](./caching/redis.md) | ⏳ Pendiente | Redis distributed cache |

---

## Arquitectura de Implementación

### 📁 Estructura de Carpetas

Basada en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```
infrastructure/services/
├── Auth0Service.cs                     # ✅ Implementación real de Auth0
├── Auth0ServiceMock.cs                 # ✅ Mock para dev/testing
├── (futuro) EmailService.cs            # ⏳ Email service
└── (futuro) CacheService.cs            # ⏳ Cache service
```

**Registro en DI** (ServiceCollectionExtender.cs):
```
webapi/infrastructure/
└── ServiceCollectionExtender.cs        # ✅ Registro condicional de servicios
```

---

## Ejemplos Completos

### 📋 Ejemplo 1: Interface en Domain

**Domain Layer** define la interface:

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.services;

/// <summary>
/// Defines methods for interacting with an identity service like Auth0 or IdentityServer.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Create a new user in the identity server
    /// </summary>
    User Create(string username, string name, string password);

    /// <summary>
    /// Get a user by username
    /// </summary>
    User? GetByUserName(string userName);

    /// <summary>
    /// Get a user by Email
    /// </summary>
    User? GetByEmail(string userName);

    /// <summary>
    /// Change password
    /// </summary>
    User? ChangePassword(string userName, string newPassword);
}
```

**Ventajas**:
- ✅ **Domain no conoce Auth0**: Puede ser Auth0, IdentityServer, custom JWT
- ✅ **Testeable**: Fácil de mockear en tests
- ✅ **Intercambiable**: Cambiar proveedor sin tocar Application o Domain

---

### 📋 Ejemplo 2: Auth0ServiceMock para Dev/Testing

**Mock sin llamadas reales**:

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.services;

namespace hashira.stone.backend.infrastructure.services;

public class Auth0ServiceMock : IIdentityService
{
    public User Create(string username, string name, string password)
    {
        // ✅ Mock: Solo retorna el usuario sin llamar Auth0
        return new User() { Email = username, Name = name };
    }

    public User? GetByUserName(string userName)
    {
        throw new NotImplementedException();
    }

    public User? GetByEmail(string userName)
    {
        throw new NotImplementedException();
    }

    public User? ChangePassword(string userName, string newPassword)
    {
        throw new NotImplementedException();
    }
}
```

**Ventajas**:
- ✅ **Sin dependencias externas**: No necesita Auth0 configurado
- ✅ **Rápido**: Sin latencia de red
- ✅ **Determinístico**: Siempre retorna el mismo resultado

---

### 📋 Ejemplo 3: Registro Condicional en DI

**ServiceCollectionExtender.cs**:

```csharp
/// <summary>
/// Configure Identity Service (Auth0)
/// </summary>
public static IServiceCollection ConfigureIdentityService(
    this IServiceCollection services,
    IWebHostEnvironment environment)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        // ✅ Development/Testing: usa Mock
        services.AddScoped<IIdentityService, Auth0ServiceMock>();
    }
    else
    {
        // ✅ Production/Staging: usa servicio real
        services.AddScoped<IIdentityService, Auth0Service>();
    }
    return services;
}
```

**Uso en Program.cs**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configurar Identity Service (condicional por ambiente)
builder.Services.ConfigureIdentityService(builder.Environment);

var app = builder.Build();
```

**Resultado**:
- **Development**: `IIdentityService` → `Auth0ServiceMock` (sin Auth0)
- **Testing**: `IIdentityService` → `Auth0ServiceMock` (sin Auth0)
- **Production**: `IIdentityService` → `Auth0Service` (con Auth0)

---

### 📋 Ejemplo 4: Uso en Application Layer

**UseCase usando IIdentityService**:

```csharp
public class CreateUserUseCase
{
    public class Handler : ICommandHandler<Command, Result<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdentityService _identityService;  // ← Interface de Domain

        public Handler(IUnitOfWork unitOfWork, IIdentityService identityService)
        {
            _unitOfWork = unitOfWork;
            _identityService = identityService;
        }

        public async Task<Result<UserDto>> Handle(Command command)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                // ✅ Crear usuario en Auth0 (o Mock en dev)
                var identityUser = _identityService.Create(
                    command.Email,
                    command.Name,
                    command.Password
                );

                // ✅ Crear usuario en BD local
                var user = await _unitOfWork.Users.CreateAsync(
                    command.Email,
                    command.Name
                );

                _unitOfWork.Commit();
                return Result.Ok(new UserDto(user));
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error(ex.Message));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error("An error occurred while creating the user"));
            }
        }
    }
}
```

**Ventajas**:
- ✅ **Application no conoce Auth0**: Solo conoce IIdentityService
- ✅ **Testeable**: Fácil de mockear IIdentityService en tests
- ✅ **Transaccional**: Rollback si falla cualquier operación

---

## Mejores Prácticas

### ✅ 1. Usar IHttpClientFactory (NO `new HttpClient()`)

```csharp
// ❌ INCORRECTO
using (var client = new HttpClient())  // Socket exhaustion
{
    return client.PostAsync(url, content).Result;
}

// ✅ CORRECTO
public class Auth0Service : IIdentityService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public Auth0Service(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<User> CreateAsync(string email)
    {
        var client = _httpClientFactory.CreateClient("Auth0");
        // ...
    }
}
```

**Registro en DI**:
```csharp
services.AddHttpClient("Auth0", client =>
{
    client.BaseAddress = new Uri(configuration["Auth0:Domain"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

### ✅ 2. Async/Await (NO Blocking Calls)

```csharp
// ❌ INCORRECTO
var result = httpClient.PostAsync(url, content).Result;  // Blocking

// ✅ CORRECTO
var result = await httpClient.PostAsync(url, content);  // Non-blocking
```

---

### ✅ 3. Configuration desde IConfiguration

```csharp
// ❌ INCORRECTO
var domain = "https://your-tenant.auth0.com";  // Hardcoded

// ✅ CORRECTO
var domain = _configuration["Auth0ManagementSettings:Domain"];
if (string.IsNullOrEmpty(domain))
    throw new ConfigurationErrorsException("Auth0 domain not configured");
```

---

### ✅ 4. Excepciones de Dominio

```csharp
// ❌ INCORRECTO
throw new Exception("User already exists");  // Generic exception

// ✅ CORRECTO
if (content.Contains("user already exists"))
    throw new DuplicatedDomainException($"A user with email '{email}' already exists");
```

---

### ✅ 5. Mock para Development/Testing

```csharp
// ✅ CORRECTO
if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
{
    services.AddScoped<IIdentityService, Auth0ServiceMock>();
}
else
{
    services.AddScoped<IIdentityService, Auth0Service>();
}
```

---

### ✅ 6. Separación de Responsabilidades

```csharp
// ✅ CORRECTO: Métodos privados para concerns específicos
private string GetTokenAccessValue()  // ← Solo obtiene token
{
    var getTokenContent = this.GetTokenResponse();
    var authToken = JsonConvert.DeserializeAnonymousType(getTokenContent, new { access_token = "" });
    return authToken.access_token;
}

private string GetTokenResponse()  // ← Solo hace HTTP request
{
    // ... configuración ...
    var response = PostAsync(url, body);
    if (response.IsSuccessStatusCode)
        return response.Content.ReadAsStringAsync().Result;
    throw new HttpRequestException($"Error getting Auth0 access token");
}
```

---

### ✅ 7. Uso de Typed HttpClients

**Mejor aún que Named HttpClients**:

```csharp
// ✅ Typed HttpClient
public class Auth0HttpClient
{
    private readonly HttpClient _httpClient;

    public Auth0HttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["Auth0:Domain"]);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret)
    {
        var body = new { client_id = clientId, client_secret = clientSecret, /* ... */ };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/oauth/token", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

**Registro en DI**:
```csharp
services.AddHttpClient<Auth0HttpClient>();
```

---

## Referencias

### 📚 Documentación Oficial

- [IHttpClientFactory - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Auth0.NET SDK](https://github.com/auth0/auth0.net)
- [Auth0 Management API](https://auth0.com/docs/api/management/v2)
- [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http/)

### 🔗 Guías Relacionadas

- [Core Concepts](../core-concepts.md) - Conceptos fundamentales de Infrastructure
- [Repository Pattern](../repository-pattern.md) - Patrón Repository
- [Dependency Injection](../dependency-injection.md) - Configuración de DI
- [Best Practices](../../best-practices/README.md) - Prácticas generales

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de External Services README |

---

**Siguiente**: [HttpClients](./http-clients.md) - IHttpClientFactory patterns →
