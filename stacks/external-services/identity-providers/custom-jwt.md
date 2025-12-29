# Custom JWT Implementation Guide

**Version:** 1.0.0
**Status:** ✅ Completed
**Last Updated:** 2025-01-14

## Tabla de Contenidos

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Cuándo Usar Custom JWT](#cuándo-usar-custom-jwt)
3. [Arquitectura de JWT](#arquitectura-de-jwt)
4. [Configuración](#configuración)
5. [Token Service Implementation](#token-service-implementation)
6. [Authentication Middleware](#authentication-middleware)
7. [Refresh Tokens](#refresh-tokens)
8. [Claims-Based Authorization](#claims-based-authorization)
9. [Best Practices](#best-practices)
10. [Errores Comunes](#errores-comunes)

---

## 1. Resumen Ejecutivo

Esta guía documenta la implementación de **Custom JWT (JSON Web Tokens)** en ASP.NET Core usando **Microsoft.IdentityModel.Tokens.Jwt**, basada en el proyecto **hashira.stone.backend**.

**Puntos clave:**

- **Cuándo usar:** Full control sobre autenticación, no vendor lock-in, costo cero
- **Stack:** Microsoft.IdentityModel.Tokens.Jwt + ASP.NET Core Identity
- **Componentes:** Token Service (generación/validación) + JwtBearer middleware
- **Seguridad:** Symmetric (HMAC-SHA256) o Asymmetric (RSA) signing
- **Features:** Access tokens + Refresh tokens + Claims customizados

**Referencias del proyecto:**
- [ServiceCollectionExtender.cs:92-109](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs#L92-L109) - JWT Bearer configuration

---

## 2. Cuándo Usar Custom JWT

### ✅ Usar Custom JWT cuando:

1. **Full Control:** Necesitas control total sobre generación y validación de tokens
2. **No Vendor Lock-in:** No quieres depender de Auth0, Azure AD, etc.
3. **Costo Cero:** Presupuesto limitado (solo costos de infraestructura)
4. **Requerimientos Custom:** Claims personalizados, lógica de autenticación compleja
5. **Microservicios:** Sistema distribuido con autenticación centralizada
6. **Learning:** Aprendizaje educativo de JWT y seguridad

### ❌ NO usar Custom JWT cuando:

1. **Compliance Crítico:** Requieres SOC2, HIPAA, PCI-DSS (usar provider especializado)
2. **Social Login:** Necesitas Google, Facebook, GitHub login (Auth0 más simple)
3. **MFA:** Multi-factor authentication out-of-the-box (Auth0, Azure AD)
4. **Team Pequeño:** Sin expertise en seguridad (riesgo de vulnerabilidades)
5. **Time-to-Market:** Necesitas lanzar rápido (Auth0 SDK más rápido)

### Comparación

| Criterio | Custom JWT | Auth0 | Azure AD |
|----------|------------|-------|----------|
| **Costo** | Gratis | $23/mes+ | $6/usuario/mes |
| **Setup Time** | 2-3 días | 1 día | 1-2 días |
| **Mantenimiento** | Alto (tu equipo) | Bajo (Auth0) | Bajo (Microsoft) |
| **Control** | Total | Limitado | Limitado |
| **Social Login** | Manual | Built-in | Built-in |
| **MFA** | Manual | Built-in | Built-in |
| **Compliance** | Manual | SOC2, HIPAA | SOC2, ISO 27001 |
| **Learning Curve** | Alto | Medio | Medio |

---

## 3. Arquitectura de JWT

### 3.1. Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                      Cliente (SPA/Mobile)                    │
│  1. Login → POST /api/auth/login                            │
│  2. Recibe { accessToken, refreshToken }                    │
│  3. Requests con header: Authorization: Bearer {token}      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core API (Backend)                      │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 1. AuthController                                       │ │
│  │    - POST /login → ITokenService.GenerateToken()       │ │
│  │    - POST /refresh → ITokenService.RefreshToken()      │ │
│  └────────────────────────────────────────────────────────┘ │
│                              ↓                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 2. ITokenService (Infrastructure)                      │ │
│  │    - GenerateAccessToken(user, claims)                 │ │
│  │    - GenerateRefreshToken()                            │ │
│  │    - ValidateToken(token)                              │ │
│  └────────────────────────────────────────────────────────┘ │
│                              ↓                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 3. JwtBearer Middleware (Auto validación)              │ │
│  │    - Valida firma                                      │ │
│  │    - Valida expiration, issuer, audience               │ │
│  │    - Crea ClaimsPrincipal                              │ │
│  └────────────────────────────────────────────────────────┘ │
│                              ↓                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 4. Authorization Policies                              │ │
│  │    - [Authorize(Policy = "MustBeApplicationUser")]     │ │
│  │    - Custom Requirements + Handlers                    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 3.2. Flujo de Autenticación

```
1. LOGIN
   Cliente → POST /api/auth/login { email, password }
   API → Valida credenciales
   API → TokenService.GenerateAccessToken(user)
   API → TokenService.GenerateRefreshToken(user)
   API ← { accessToken: "eyJ...", refreshToken: "xyz...", expiresIn: 3600 }

2. AUTHENTICATED REQUEST
   Cliente → GET /api/users/me
            Header: Authorization: Bearer eyJ...
   API → JwtBearer Middleware valida token automáticamente
   API → Si válido, crea HttpContext.User con claims
   API → Controller accede a User.FindFirst("sub")
   API ← { id: "123", email: "user@example.com" }

3. REFRESH TOKEN
   Cliente → POST /api/auth/refresh { refreshToken: "xyz..." }
   API → Valida refresh token (DB lookup, expiration check)
   API → Genera nuevo access token
   API ← { accessToken: "eyJ...", refreshToken: "abc...", expiresIn: 3600 }
```

---

## 4. Configuración

### 4.1. appsettings.json

```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-keep-this-super-secret-and-in-env-vars",
    "Issuer": "https://api.yourapp.com",
    "Audience": "https://yourapp.com",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

**⚠️ Seguridad en producción:**

```json
// appsettings.Production.json (NO commitear)
{
  "JwtSettings": {
    "SecretKey": "${JWT_SECRET_KEY}",  // ← Variable de entorno
    "Issuer": "https://api.production.com",
    "Audience": "https://production.com"
  }
}
```

**Generar secret key segura:**

```bash
# PowerShell
$key = [System.Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }))
Write-Host $key

# Bash
openssl rand -base64 32
```

### 4.2. Options Pattern

```csharp
using System.ComponentModel.DataAnnotations;

namespace YourApp.Infrastructure.Options;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    [Url]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    [Range(1, 30)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = true;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
}
```

---

## 5. Token Service Implementation

### 5.1. Instalación

```bash
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 5.2. ITokenService Interface

```csharp
namespace YourApp.Domain.Interfaces.Services;

public interface ITokenService
{
    /// <summary>
    /// Genera un access token JWT para un usuario
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<Claim> additionalClaims = null);

    /// <summary>
    /// Genera un refresh token
    /// </summary>
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Valida un access token y retorna ClaimsPrincipal
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Valida un refresh token
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Revoca un refresh token
    /// </summary>
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
}
```

### 5.3. TokenService Implementation

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace YourApp.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        ILogger<TokenService> logger,
        IUnitOfWork unitOfWork)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(
        User user,
        IEnumerable<Claim> additionalClaims = null)
    {
        // ✅ Claims estándar
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name ?? user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // ✅ Claims personalizados
        if (user.Roles != null && user.Roles.Any())
        {
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        // ✅ Additional claims opcionales
        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        // ✅ Signing credentials (HMAC-SHA256)
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        // ✅ Token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                _jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        // ✅ Crear token
        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = _tokenHandler.WriteToken(token);

        _logger.LogInformation(
            "Access token generado para usuario: {UserId}",
            user.Id);

        return tokenString;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(
        string userId,
        CancellationToken ct = default)
    {
        // ✅ Generar token criptográficamente seguro
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(
                _jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // ✅ Guardar en DB
        await _unitOfWork.RefreshTokens.AddAsync(refreshToken, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation(
            "Refresh token generado para usuario: {UserId}",
            userId);

        return refreshToken;
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero // ← Sin clock skew (default 5 min)
            };

            // ✅ Validar token
            var principal = _tokenHandler.ValidateToken(
                token,
                validationParameters,
                out var validatedToken);

            // ✅ Verificar que es JWT (no otro tipo de token)
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Token inválido: algoritmo no soportado");
                return null;
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expirado");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token inválido");
            return null;
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var refreshToken = await _unitOfWork.RefreshTokens
            .FindAsync(rt => rt.Token == token, ct);

        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token no encontrado");
            return false;
        }

        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("Refresh token revocado: {Token}", token);
            return false;
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expirado: {Token}", token);
            return false;
        }

        return true;
    }

    public async Task RevokeRefreshTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var refreshToken = await _unitOfWork.RefreshTokens
            .FindAsync(rt => rt.Token == token, ct);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("Refresh token revocado: {Token}", token);
        }
    }
}
```

### 5.4. RefreshToken Entity

```csharp
namespace YourApp.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    // Navigation
    public virtual User User { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

---

## 6. Authentication Middleware

### 6.1. JWT Bearer Configuration

**Basado en:** [ServiceCollectionExtender.cs:92-109](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs#L92-L109)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ✅ 1. Configurar Options Pattern con validación
    services.AddOptions<JwtSettings>()
        .Bind(configuration.GetSection(JwtSettings.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var jwtSettings = configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>();

    if (jwtSettings == null)
        throw new InvalidOperationException("JWT configuration missing");

    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

    // ✅ 2. Configurar Authentication con JwtBearer
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = true; // ← HTTPS en producción
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = jwtSettings.ValidateIssuer,
            ValidateAudience = jwtSettings.ValidateAudience,
            ValidateLifetime = jwtSettings.ValidateLifetime,
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // ← Sin tolerancia de tiempo
        };

        // ✅ 3. Event handlers para logging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "JWT authentication failed: {Error}",
                    context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                logger.LogInformation("JWT validated for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

    // ✅ 4. Registrar TokenService
    services.AddScoped<ITokenService, TokenService>();

    return services;
}
```

### 6.2. Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Agregar JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// ✅ 2. Agregar Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// ✅ 3. Middleware order (MUY IMPORTANTE)
app.UseRouting();
app.UseAuthentication();  // ← ANTES de UseAuthorization
app.UseAuthorization();   // ← DESPUÉS de UseAuthentication
app.MapControllers();

app.Run();
```

---

## 7. Refresh Tokens

### 7.1. AuthController

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITokenService tokenService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        // ✅ 1. Validar credenciales
        var user = await _userService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            ct);

        if (user == null)
            return Unauthorized(new { error = "Invalid credentials" });

        // ✅ 2. Generar access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        // ✅ 3. Generar refresh token
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(
            user.Id.ToString(),
            ct);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            expiresIn = 3600,
            tokenType = "Bearer"
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        // ✅ 1. Validar refresh token
        var isValid = await _tokenService.ValidateRefreshTokenAsync(
            request.RefreshToken,
            ct);

        if (!isValid)
            return Unauthorized(new { error = "Invalid refresh token" });

        // ✅ 2. Obtener usuario del refresh token
        var refreshToken = await _unitOfWork.RefreshTokens
            .FindAsync(rt => rt.Token == request.RefreshToken, ct);

        var user = await _userService.GetByIdAsync(refreshToken.UserId, ct);

        if (user == null)
            return Unauthorized(new { error = "User not found" });

        // ✅ 3. Revocar token anterior
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, ct);

        // ✅ 4. Generar nuevos tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(
            user.Id.ToString(),
            ct);

        _logger.LogInformation("Tokens refreshed for user: {UserId}", user.Id);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token,
            expiresIn = 3600,
            tokenType = "Bearer"
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken ct)
    {
        // ✅ Revocar refresh token
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, ct);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User logged out: {UserId}", userId);

        return NoContent();
    }
}

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
```

---

## 8. Claims-Based Authorization

### 8.1. Custom Authorization Policies

**Basado en:** [ServiceCollectionExtender.cs:24-44](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs#L24-L44)

```csharp
public static IServiceCollection AddCustomAuthorization(
    this IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        // ✅ Policy básico: Usuario autenticado
        options.AddPolicy("DefaultAuthorizationPolicy", policy =>
        {
            policy.RequireAuthenticatedUser();
        });

        // ✅ Policy con role
        options.AddPolicy("MustBeAdministrator", policy =>
        {
            policy.RequireRole("Administrator");
        });

        // ✅ Policy con custom requirement
        options.AddPolicy("MustBeApplicationUser", policy =>
        {
            policy.AddRequirements(new MustBeApplicationUserRequirement());
        });

        // ✅ Policy con claim específico
        options.AddPolicy("MustHaveEmailVerified", policy =>
        {
            policy.RequireClaim("email_verified", "true");
        });

        // ✅ Policy combinado
        options.AddPolicy("MustBeVerifiedAdministrator", policy =>
        {
            policy.RequireRole("Administrator");
            policy.RequireClaim("email_verified", "true");
        });
    });

    // ✅ Registrar custom handlers
    services.AddScoped<IAuthorizationHandler, MustBeApplicationUserHandler>();

    return services;
}
```

### 8.2. Custom Authorization Handler

```csharp
using Microsoft.AspNetCore.Authorization;

namespace YourApp.WebApi.Authorization;

public class MustBeApplicationUserRequirement : IAuthorizationRequirement
{
    // Requirement vacío (solo marca)
}

public class MustBeApplicationUserHandler
    : AuthorizationHandler<MustBeApplicationUserRequirement>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MustBeApplicationUserHandler> _logger;

    public MustBeApplicationUserHandler(
        IUnitOfWork unitOfWork,
        ILogger<MustBeApplicationUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBeApplicationUserRequirement requirement)
    {
        // ✅ 1. Obtener user ID del token
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            _logger.LogWarning("User ID claim not found");
            return; // ← Requirement NOT satisfied
        }

        // ✅ 2. Verificar que usuario existe en DB
        var userId = userIdClaim.Value;
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found in database: {UserId}", userId);
            return; // ← Requirement NOT satisfied
        }

        // ✅ 3. Verificar que usuario está activo
        if (!user.IsActive)
        {
            _logger.LogWarning("User is inactive: {UserId}", userId);
            return; // ← Requirement NOT satisfied
        }

        // ✅ 4. Requirement satisfied
        _logger.LogInformation("Authorization succeeded for user: {UserId}", userId);
        context.Succeed(requirement);
    }
}
```

### 8.3. Uso en Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // ✅ Solo usuarios autenticados
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        return Ok(new { userId, email, name });
    }

    // ✅ Solo administradores
    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public IActionResult GetAllUsers()
    {
        // ...
    }

    // ✅ Custom policy
    [Authorize(Policy = "MustBeApplicationUser")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        // ...
    }

    // ✅ Multiple policies
    [Authorize(Policy = "MustBeVerifiedAdministrator")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        // ...
    }
}
```

---

## 9. Best Practices

### ✅ 1. Secret Key Segura

```csharp
// ✅ RECOMENDADO: Mínimo 256 bits (32 caracteres)
"SecretKey": "your-very-long-secret-key-at-least-32-characters-long"

// ❌ EVITAR: Keys cortas (inseguras)
"SecretKey": "secret123"
```

### ✅ 2. Token Expiration Apropiado

```csharp
// ✅ RECOMENDADO
Access Token: 15-60 minutos
Refresh Token: 7-30 días

// ❌ EVITAR
Access Token: 24 horas (demasiado largo)
Refresh Token: No expira (riesgo de seguridad)
```

### ✅ 3. HTTPS en Producción

```csharp
// ✅ RECOMENDADO
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

// ❌ EVITAR en producción
options.RequireHttpsMetadata = false;
```

### ✅ 4. ClockSkew = Zero

```csharp
// ✅ RECOMENDADO: Sin tolerancia de tiempo
ClockSkew = TimeSpan.Zero

// ❌ DEFAULT: 5 minutos de tolerancia (menos seguro)
ClockSkew = TimeSpan.FromMinutes(5)
```

### ✅ 5. Refresh Token Rotation

```csharp
// ✅ RECOMENDADO: Revocar token anterior al refrescar
await _tokenService.RevokeRefreshTokenAsync(oldToken);
var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(userId);

// ❌ EVITAR: Reutilizar mismo refresh token
```

### ✅ 6. Almacenar Refresh Tokens en DB

```csharp
// ✅ RECOMENDADO: DB con índice en Token
CREATE INDEX ix_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens(user_id);

// ❌ EVITAR: In-memory (se pierden al reiniciar)
```

### ✅ 7. Logging de Security Events

```csharp
// ✅ RECOMENDADO
_logger.LogWarning("Failed login attempt: {Email}", email);
_logger.LogWarning("Token validation failed: {Error}", ex.Message);
_logger.LogInformation("User logged out: {UserId}", userId);
```

### ✅ 8. Rate Limiting en Login

```csharp
// ✅ RECOMENDADO: Limitar intentos de login
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
    });
});

[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<IActionResult> Login(...)
```

---

## 10. Errores Comunes

### ❌ Secret Key Corta

**Causa:**
```json
"SecretKey": "secret"  // ← Solo 6 caracteres
```

**Error:**
```
System.ArgumentOutOfRangeException: IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'HS256',
the key size must be greater than: '256' bits, key has '48' bits.
```

**Solución:**
```json
"SecretKey": "your-very-long-secret-key-at-least-32-characters-long"
```

### ❌ Middleware Order Incorrecto

**Causa:**
```csharp
app.UseAuthorization();   // ← ORDEN INCORRECTO
app.UseAuthentication();
```

**Solución:**
```csharp
app.UseAuthentication();  // ← PRIMERO
app.UseAuthorization();   // ← DESPUÉS
```

### ❌ ClockSkew Default

**Problema:** Tokens válidos 5 minutos después de expirar (default)

**Solución:**
```csharp
ClockSkew = TimeSpan.Zero  // ← Sin tolerancia
```

### ❌ Refresh Token No Revocado

**Causa:**
```csharp
// ❌ Generar nuevo token sin revocar anterior
var newToken = await _tokenService.GenerateRefreshTokenAsync(userId);
```

**Solución:**
```csharp
// ✅ Revocar primero
await _tokenService.RevokeRefreshTokenAsync(oldToken);
var newToken = await _tokenService.GenerateRefreshTokenAsync(userId);
```

---

## Conclusión

Esta guía documenta la implementación de **Custom JWT** en ASP.NET Core usando Microsoft.IdentityModel.Tokens.Jwt.

**Key Takeaways:**

1. ✅ **Secret key mínimo 256 bits** → Seguridad criptográfica
2. ✅ **Access tokens cortos (15-60 min)** → Minimizar exposición
3. ✅ **Refresh token rotation** → Invalidar tokens anteriores
4. ✅ **HTTPS en producción** → Prevenir MITM
5. ✅ **ClockSkew = Zero** → Expiración precisa
6. ✅ **Claims-based authorization** → Granular access control
7. ✅ **Logging de security events** → Auditoría y debugging

**Referencias:**

- [Microsoft.IdentityModel.Tokens.Jwt](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [JWT.io](https://jwt.io/) - JWT debugger

---

**Versión:** 1.0.0
**Proyecto de Referencia:** hashira.stone.backend
**Última Actualización:** 2025-01-14

