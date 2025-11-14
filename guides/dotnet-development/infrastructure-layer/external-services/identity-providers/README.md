# Identity Providers - External Services

**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [¿Qué es un Identity Provider?](#qué-es-un-identity-provider)
2. [Opciones Disponibles](#opciones-disponibles)
3. [Comparación de Providers](#comparación-de-providers)
4. [Interface de Dominio](#interface-de-dominio)
5. [Patrones de Implementación](#patrones-de-implementación)
6. [Guías Disponibles](#guías-disponibles)
7. [Casos de Uso](#casos-de-uso)
8. [Best Practices](#best-practices)
9. [Referencias](#referencias)

---

## ¿Qué es un Identity Provider?

Un **Identity Provider (IdP)** es un servicio que **almacena y gestiona identidades digitales** de usuarios. Proporciona autenticación (verificar quién eres) y autorización (qué puedes hacer).

### 🎯 Responsabilidades del IdP

- **Autenticación**: Verificar credenciales (email/password, OAuth, MFA)
- **Gestión de usuarios**: Crear, actualizar, eliminar usuarios
- **Single Sign-On (SSO)**: Login único para múltiples aplicaciones
- **Multi-Factor Authentication (MFA)**: 2FA, SMS, TOTP
- **Social Login**: Login con Google, Facebook, Microsoft, etc.
- **Password Management**: Reset, cambio, políticas de complejidad
- **Tokens**: Emitir JWT, access tokens, refresh tokens
- **Autorización**: Roles, permisos, claims

---

## Opciones Disponibles

### 1. 🔐 Auth0 (SaaS - Recomendado)

**Provider externo administrado** (usado en proyecto de referencia)

**Ventajas**:
- ✅ **Managed Service**: No mantienes infraestructura
- ✅ **Feature-rich**: MFA, social login, passwordless, etc.
- ✅ **Escalable**: Maneja millones de usuarios
- ✅ **Compliance**: SOC 2, GDPR, HIPAA
- ✅ **SDKs oficiales**: .NET, Node.js, Python, etc.
- ✅ **Customizable**: Rules, hooks, custom domains

**Desventajas**:
- ⚠️ **Costo**: Pricing basado en usuarios activos
- ⚠️ **Vendor lock-in**: Dependencia de proveedor externo
- ⚠️ **Latencia**: Llamadas a API externa

**Cuándo usar**:
- Aplicaciones B2C con muchos usuarios
- Necesitas social login o MFA
- No quieres mantener infraestructura de autenticación

**Proyecto de referencia**: ✅ hashira.stone.backend usa Auth0

---

### 2. 🏢 Azure Active Directory (Microsoft)

**Provider de Microsoft** para aplicaciones empresariales

**Ventajas**:
- ✅ **Integración con Microsoft**: Office 365, Teams, Azure
- ✅ **Enterprise features**: Conditional Access, Identity Protection
- ✅ **B2B/B2C**: Soporte para escenarios empresariales y consumidores

**Desventajas**:
- ⚠️ **Complejidad**: Curva de aprendizaje alta
- ⚠️ **Ecosistema Microsoft**: Mejor si ya usas Azure

**Cuándo usar**:
- Aplicaciones empresariales B2B
- Ya usas ecosistema Microsoft/Azure
- Necesitas integración con AD on-premises

---

### 3. 🔓 IdentityServer (Self-hosted)

**Framework open-source** de Duende Software (formerly IdentityServer4)

**Ventajas**:
- ✅ **Control total**: Self-hosted, no dependencias externas
- ✅ **OpenID Connect/OAuth2**: Estándares completos
- ✅ **Customizable**: Total control sobre flujos

**Desventajas**:
- ⚠️ **Self-hosted**: Debes mantener infraestructura
- ⚠️ **Costo**: Licencia comercial para producción
- ⚠️ **Complejidad**: Más complejo de configurar

**Cuándo usar**:
- Necesitas control total sobre autenticación
- Aplicaciones internas/privadas
- No puedes usar servicios externos por compliance

---

### 4. 🛠️ Custom JWT (DIY)

**Implementación propia** con JWT tokens

**Ventajas**:
- ✅ **Sin costos**: No pagas por servicio externo
- ✅ **Control total**: Implementas exactamente lo que necesitas
- ✅ **Sin latencia**: Todo local

**Desventajas**:
- ⚠️ **Complejidad**: Debes implementar todo (MFA, password reset, etc.)
- ⚠️ **Seguridad**: Fácil cometer errores críticos
- ⚠️ **Mantenimiento**: Más código que mantener

**Cuándo usar**:
- Aplicaciones muy simples (pocos usuarios)
- Necesitas control absoluto
- Presupuesto muy limitado

---

### 5. 🌐 Keycloak (Open Source)

**Identity provider open-source** de RedHat

**Ventajas**:
- ✅ **Open source**: Gratis, self-hosted
- ✅ **Feature-rich**: SSO, MFA, social login
- ✅ **Standards-compliant**: OpenID Connect, SAML

**Desventajas**:
- ⚠️ **Self-hosted**: Debes mantener infraestructura
- ⚠️ **Complejidad**: Configuración puede ser compleja

**Cuándo usar**:
- Necesitas features avanzados sin costo de SaaS
- Aplicaciones internas/on-premises
- Open source es requisito

---

## Comparación de Providers

| Característica | Auth0 | Azure AD | IdentityServer | Custom JWT | Keycloak |
|----------------|-------|----------|----------------|------------|----------|
| **Tipo** | SaaS | SaaS | Self-hosted | DIY | Self-hosted |
| **Costo** | $$$ (por usuario) | $$$ (por usuario) | $$$ (licencia) | Gratis | Gratis |
| **Setup** | ⚡ Rápido | ⚡ Rápido | ⏰ Medio | ⏰⏰ Lento | ⏰ Medio |
| **Mantenimiento** | ✅ Mínimo | ✅ Mínimo | ⚠️ Alto | ⚠️ Muy alto | ⚠️ Alto |
| **Social Login** | ✅ | ✅ | ✅ | ❌ | ✅ |
| **MFA** | ✅ | ✅ | ✅ | ❌ | ✅ |
| **Compliance** | ✅ SOC2, GDPR | ✅ SOC2, GDPR | ⚠️ Self-managed | ❌ | ⚠️ Self-managed |
| **Customización** | ✅ Alta | ⚠️ Media | ✅ Total | ✅ Total | ✅ Alta |
| **Escalabilidad** | ✅ Ilimitada | ✅ Ilimitada | ⚠️ Self-managed | ⚠️ Self-managed | ⚠️ Self-managed |
| **Vendor Lock-in** | ⚠️ Sí | ⚠️ Sí | ✅ No | ✅ No | ✅ No |

---

## Interface de Dominio

### IIdentityService del Proyecto de Referencia

**Domain Layer** define la interface (agnóstica de provider):

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
    /// <param name="username">Email del usuario</param>
    /// <param name="name">Nombre completo</param>
    /// <param name="password">Password inicial</param>
    /// <returns>Usuario creado</returns>
    User Create(string username, string name, string password);

    /// <summary>
    /// Get a user by username
    /// </summary>
    /// <param name="userName">Username (email)</param>
    /// <returns>Usuario o null si no existe</returns>
    User? GetByUserName(string userName);

    /// <summary>
    /// Get a user by Email
    /// </summary>
    /// <param name="userName">Email del usuario</param>
    /// <returns>Usuario o null si no existe</returns>
    User? GetByEmail(string userName);

    /// <summary>
    /// Change password
    /// </summary>
    /// <param name="userName">Username del usuario</param>
    /// <param name="newPassword">Nueva contraseña</param>
    /// <returns>Usuario actualizado</returns>
    User? ChangePassword(string userName, string newPassword);
}
```

**Ventajas**:
- ✅ **Provider-agnostic**: No menciona Auth0, IdentityServer, etc.
- ✅ **Testeable**: Fácil de mockear
- ✅ **Intercambiable**: Cambiar provider sin tocar Application/Domain

---

## Patrones de Implementación

### 1. 🎭 Adapter Pattern

Cada provider tiene su propia implementación:

```
Domain Layer                 Infrastructure Layer             External Service
     ↓                               ↓                              ↓
IIdentityService      →    Auth0Service                    →    Auth0 API
                           IdentityServerService            →    IdentityServer
                           CustomJwtService                 →    Local JWT
```

**Ejemplo**:

```csharp
// Auth0Service implementa IIdentityService
public class Auth0Service : IIdentityService
{
    private readonly Auth0HttpClient _auth0Client;

    public Auth0Service(Auth0HttpClient auth0Client)
    {
        _auth0Client = auth0Client;
    }

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        var accessToken = await _auth0Client.GetAccessTokenAsync();
        var auth0User = await _auth0Client.CreateUserAsync(username, name, password, accessToken);

        return new User
        {
            UserId = auth0User.user_id,
            Email = auth0User.email,
            Name = auth0User.name
        };
    }
}
```

```csharp
// CustomJwtService implementa la MISMA interface
public class CustomJwtService : IIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        var hashedPassword = _passwordHasher.Hash(password);
        var user = new User(username, name, hashedPassword);
        await _userRepository.AddAsync(user);
        return user;
    }
}
```

**Application Layer** usa `IIdentityService` sin saber qué provider es:

```csharp
public class CreateUserUseCase
{
    private readonly IIdentityService _identityService;  // ← No sabe si es Auth0 o Custom

    public async Task<Result<UserDto>> Handle(Command command)
    {
        var user = await _identityService.CreateAsync(command.Email, command.Name, command.Password);
        return Result.Ok(new UserDto(user));
    }
}
```

---

### 2. 🧪 Mock Pattern (Dev/Testing)

**Registro condicional** en DI:

```csharp
public static IServiceCollection ConfigureIdentityService(
    this IServiceCollection services,
    IWebHostEnvironment environment,
    IConfiguration configuration)
{
    var provider = configuration["IdentityProvider"];  // "Auth0", "CustomJWT", "Mock"

    switch (provider)
    {
        case "Auth0":
            if (environment.IsDevelopment())
                services.AddScoped<IIdentityService, Auth0ServiceMock>();
            else
                services.AddScoped<IIdentityService, Auth0Service>();
            break;

        case "CustomJWT":
            services.AddScoped<IIdentityService, CustomJwtService>();
            break;

        case "Mock":
            services.AddScoped<IIdentityService, Auth0ServiceMock>();
            break;

        default:
            throw new InvalidOperationException($"Unknown identity provider: {provider}");
    }

    return services;
}
```

**Mock Service**:

```csharp
public class Auth0ServiceMock : IIdentityService
{
    public User Create(string username, string name, string password)
    {
        // ✅ Mock: No llama API externa, solo retorna usuario fake
        return new User
        {
            UserId = Guid.NewGuid().ToString(),
            Email = username,
            Name = name,
            CreationDate = DateTime.UtcNow
        };
    }

    public User? GetByEmail(string email)
    {
        // ✅ Mock: Retorna usuario fake si email es válido
        if (string.IsNullOrEmpty(email))
            return null;

        return new User
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            Name = "Mock User",
            CreationDate = DateTime.UtcNow
        };
    }

    public User? ChangePassword(string userName, string newPassword)
    {
        return new User { Email = userName };
    }
}
```

**Ventajas**:
- ✅ **Sin dependencias**: No necesitas Auth0 configurado en dev
- ✅ **Rápido**: Sin latencia de red
- ✅ **Determinístico**: Siempre funciona igual

---

### 3. 🔄 Strategy Pattern

Selección dinámica de provider en runtime:

```csharp
public class IdentityServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public IdentityServiceFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public IIdentityService GetService()
    {
        var provider = _configuration["IdentityProvider"];

        return provider switch
        {
            "Auth0" => _serviceProvider.GetRequiredService<Auth0Service>(),
            "CustomJWT" => _serviceProvider.GetRequiredService<CustomJwtService>(),
            "IdentityServer" => _serviceProvider.GetRequiredService<IdentityServerService>(),
            _ => throw new InvalidOperationException($"Unknown provider: {provider}")
        };
    }
}
```

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de Identity Providers |
| [auth0.md](./auth0.md) | ⏳ Pendiente | Auth0 integration completa |
| [custom-jwt.md](./custom-jwt.md) | ⏳ Pendiente | Custom JWT implementation |

---

## Casos de Uso

### Caso 1: B2C con Social Login (Auth0)

**Escenario**: E-commerce con miles de usuarios, necesita login con Google/Facebook

**Solución**: Auth0
- ✅ Social login out-of-the-box
- ✅ MFA para seguridad
- ✅ Escalable a millones de usuarios
- ✅ Compliance GDPR

**Implementación**:
```csharp
services.AddScoped<IIdentityService, Auth0Service>();
```

---

### Caso 2: Aplicación Interna (Custom JWT)

**Escenario**: App interna con 50 usuarios conocidos, sin presupuesto para SaaS

**Solución**: Custom JWT
- ✅ Sin costos de SaaS
- ✅ Control total
- ✅ Usuarios gestionados en BD local

**Implementación**:
```csharp
services.AddScoped<IIdentityService, CustomJwtService>();
```

---

### Caso 3: Enterprise B2B (Azure AD)

**Escenario**: App empresarial que integra con Office 365

**Solución**: Azure AD
- ✅ SSO con Office 365
- ✅ Conditional Access policies
- ✅ Integration con AD on-premises

**Implementación**:
```csharp
services.AddScoped<IIdentityService, AzureAdService>();
```

---

### Caso 4: Microservicios On-Premises (Keycloak)

**Escenario**: Arquitectura de microservicios on-premises, open source requerido

**Solución**: Keycloak
- ✅ Open source, self-hosted
- ✅ SSO entre microservicios
- ✅ Feature-rich sin costos de licencia

**Implementación**:
```csharp
services.AddScoped<IIdentityService, KeycloakService>();
```

---

## Best Practices

### ✅ 1. Definir Interface en Domain

```csharp
// ✅ CORRECTO: Interface en Domain
namespace MyApp.Domain.Interfaces.Services;
public interface IIdentityService { /* ... */ }

// ❌ INCORRECTO: Interface en Infrastructure
namespace MyApp.Infrastructure.Services;
public interface IIdentityService { /* ... */ }
```

---

### ✅ 2. Implementación en Infrastructure

```csharp
// ✅ CORRECTO: Implementación en Infrastructure
namespace MyApp.Infrastructure.Services;
public class Auth0Service : IIdentityService { /* ... */ }
```

---

### ✅ 3. Registro Condicional por Ambiente

```csharp
// ✅ CORRECTO
if (environment.IsDevelopment())
    services.AddScoped<IIdentityService, Auth0ServiceMock>();
else
    services.AddScoped<IIdentityService, Auth0Service>();
```

---

### ✅ 4. Configuración desde IConfiguration

```csharp
// ✅ CORRECTO
var domain = _configuration["Auth0:Domain"];

// ❌ INCORRECTO
var domain = "https://my-tenant.auth0.com";  // Hardcoded
```

---

### ✅ 5. Mapeo a Entidades de Dominio

```csharp
// ✅ CORRECTO: Mapea Auth0Response a User de dominio
var auth0User = await _auth0Client.GetUserAsync(userId);
return new User
{
    UserId = auth0User.user_id,
    Email = auth0User.email,
    Name = auth0User.name
};

// ❌ INCORRECTO: Expone Auth0Response directamente
return auth0User;  // Expone detalles de implementación
```

---

### ✅ 6. Excepciones de Dominio

```csharp
// ✅ CORRECTO
if (errorContent.Contains("user already exists"))
    throw new DuplicatedDomainException($"User '{email}' already exists");

// ❌ INCORRECTO
throw new HttpRequestException("Auth0 error: user exists");  // Excepción de infraestructura
```

---

### ✅ 7. Async/Await

```csharp
// ✅ CORRECTO
public async Task<User> CreateAsync(string email, string name, string password)
{
    var user = await _identityService.CreateUserAsync(email, name, password);
    return user;
}

// ❌ INCORRECTO
public User Create(string email, string name, string password)
{
    var user = _identityService.CreateUserAsync(email, name, password).Result;  // Blocking
    return user;
}
```

---

### ✅ 8. Mock para Testing

```csharp
// ✅ CORRECTO: Mock simple y predecible
public class IdentityServiceMock : IIdentityService
{
    public User Create(string email, string name, string password)
    {
        return new User { Email = email, Name = name };
    }
}

// ❌ INCORRECTO: Mock que llama API real
public class IdentityServiceMock : IIdentityService
{
    public User Create(string email, string name, string password)
    {
        return CallRealAuth0Api(email, name, password);  // No es mock
    }
}
```

---

## Referencias

### 📚 Documentación Oficial

- [Auth0.NET SDK](https://github.com/auth0/auth0.net)
- [Auth0 Management API](https://auth0.com/docs/api/management/v2)
- [Azure Active Directory](https://learn.microsoft.com/en-us/azure/active-directory/)
- [IdentityServer (Duende)](https://duendesoftware.com/products/identityserver)
- [Keycloak](https://www.keycloak.org/)
- [OpenID Connect](https://openid.net/connect/)
- [OAuth 2.0](https://oauth.net/2/)

### 🔗 Guías Relacionadas

- [External Services Overview](../README.md) - Overview de servicios externos
- [HttpClient Patterns](../http-clients.md) - IHttpClientFactory best practices
- [Auth0 Integration](./auth0.md) - Implementación completa de Auth0
- [Custom JWT](./custom-jwt.md) - Implementación DIY de JWT
- [Best Practices](../../best-practices/README.md) - Prácticas generales

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                    |
|---------|------------|--------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de Identity Providers README |

---

**Siguiente**: [Auth0 Integration](./auth0.md) - Auth0 implementation completa →
