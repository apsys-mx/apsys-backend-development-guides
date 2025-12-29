# Authentication & Authorization

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-11-15

## Descripción General

Esta guía documenta los patrones y mejores prácticas para implementar autenticación y autorización en la capa WebApi usando **FastEndpoints 7.0.1** con **JWT Bearer Authentication** y **Auth0** como Identity Provider. Cubre la configuración de ASP.NET Core Identity, custom authorization handlers, policies, claims, roles y acceso a información del usuario autenticado.

## Tabla de Contenidos

1. [Conceptos Fundamentales](#conceptos-fundamentales)
2. [Configuración de Autenticación](#configuración-de-autenticación)
3. [Configuración de Auth0](#configuración-de-auth0)
4. [Authorization Policies](#authorization-policies)
5. [Custom Authorization Handlers](#custom-authorization-handlers)
6. [Seguridad Declarativa en Endpoints](#seguridad-declarativa-en-endpoints)
7. [Claims y IPrincipal](#claims-y-iprincipal)
8. [Acceso Anónimo](#acceso-anónimo)
9. [Acceso a Información del Usuario](#acceso-a-información-del-usuario)
10. [Testing de Autenticación](#testing-de-autenticación)
11. [Mejores Prácticas](#mejores-prácticas)
12. [Anti-patrones](#anti-patrones)

---

## Conceptos Fundamentales

### Authentication vs Authorization

**Authentication (Autenticación)**:
- Verifica **quién** es el usuario
- Proceso de validar credenciales (username/password, tokens, etc.)
- Resultado: Usuario autenticado con identity (claims)

**Authorization (Autorización)**:
- Determina **qué puede hacer** el usuario
- Proceso de verificar permisos, roles, policies
- Resultado: Acceso concedido o denegado a recursos

### JWT (JSON Web Token)

El proyecto usa **JWT Bearer Authentication** para autenticar requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Componentes de un JWT**:
1. **Header**: Algoritmo y tipo de token
2. **Payload**: Claims (datos del usuario)
3. **Signature**: Firma para verificar integridad

**Claims en el JWT**:
```json
{
  "email": "user@example.com",
  "username": "user@example.com",
  "name": "John Doe",
  "sub": "auth0|123456",
  "iat": 1699999999,
  "exp": 1700086399
}
```

### Auth0 como Identity Provider

El proyecto usa **Auth0** para:
- Gestión de usuarios
- Autenticación (login/logout)
- Generación de JWT tokens
- Custom claims mediante Auth0 Actions

---

## Configuración de Autenticación

### Program.cs

La configuración se realiza en `Program.cs`:

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.infrastructure;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

// Configure dependency injection container
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()                          // ← Configure authorization policies
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration) // ← Configure Auth0/JWT
    .ConfigureUnitOfWork(configuration)
    .ConfigureAutoMapper()
    .ConfigureValidators()
    .ConfigureDependencyInjections(environment)
    .AddLogging()
    .AddAuthorization()                         // ← Add authorization services
    .AddFastEndpoints()
    .SwaggerDocument();

var app = builder.Build();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()    // ← Must come before UseAuthorization
    .UseAuthorization()     // ← Must come after UseAuthentication
    .UseFastEndpoints()
    .UseSwagger()
    .UseSwaggerUI(opt =>
    {
        opt.DefaultModelsExpandDepth(-1);
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

app.Services.RegisterCommandsFromAssembly(typeof(GetManyAndCountUsersUseCase).Assembly);

await app.RunAsync();
```

**Orden crítico del middleware**:
1. `UseAuthentication()` - Identifica al usuario
2. `UseAuthorization()` - Verifica permisos
3. `UseFastEndpoints()` - Ejecuta endpoints

### ServiceCollectionExtender - ConfigureIdentityServerClient

Configuración de JWT Bearer Authentication con Auth0:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace hashira.stone.backend.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure identity server authority for authorization
    /// </summary>
    public static IServiceCollection ConfigureIdentityServerClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get Auth0 domain from configuration
        string? identityServerUrl = configuration.GetSection("IdentityServerConfiguration:Address").Value;
        if (string.IsNullOrEmpty(identityServerUrl))
            throw new InvalidOperationException("No identityServer configuration found in the configuration file");

        services.AddAuthentication("Bearer")
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = identityServerUrl;  // Auth0 domain
                options.RequireHttpsMetadata = false;   // Allow HTTP in development
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false  // Don't validate audience (optional)
                };
            });

        return services;
    }
}
```

**Parámetros importantes**:
- `Authority`: URL del Auth0 tenant (ej. `https://your-tenant.auth0.com/`)
- `RequireHttpsMetadata`: `false` para desarrollo, `true` para producción
- `ValidateAudience`: Si se valida el claim `aud` del token

---

## Configuración de Auth0

### appsettings.json

Configuración del tenant de Auth0:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "IdentityServerConfiguration": {
    "Address": "https://hashira-stone-testing.us.auth0.com/",
    "Audience": "https://hashira-stone-backend.online/api"
  }
}
```

### Variables de Entorno (.env)

Para mayor seguridad, las credenciales deben estar en `.env`:

```env
IDENTITY_SERVER_ADDRESS=https://your-tenant.us.auth0.com/
IDENTITY_SERVER_AUDIENCE=https://your-api-identifier
```

### Auth0 Actions - Custom Claims

Auth0 permite agregar claims personalizados mediante **Actions**:

**Login Flow Action** (JavaScript):
```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://your-api.com/';

  // Add custom claims
  if (event.user.email) {
    api.idToken.setCustomClaim(`${namespace}email`, event.user.email);
    api.idToken.setCustomClaim(`${namespace}username`, event.user.email);
  }

  if (event.user.name) {
    api.idToken.setCustomClaim(`${namespace}name`, event.user.name);
  }

  // Add roles if they exist
  if (event.authorization && event.authorization.roles) {
    api.idToken.setCustomClaim(`${namespace}roles`, event.authorization.roles);
  }
};
```

**Resultado en el JWT**:
```json
{
  "https://your-api.com/email": "user@example.com",
  "https://your-api.com/username": "user@example.com",
  "https://your-api.com/name": "John Doe",
  "https://your-api.com/roles": ["PlatformAdministrator"],
  "sub": "auth0|123456",
  "iat": 1699999999,
  "exp": 1700086399
}
```

---

## Authorization Policies

### ConfigurePolicy

Configuración de policies personalizadas:

```csharp
using Microsoft.AspNetCore.Authorization;
using hashira.stone.backend.webapi.infrastructure.authorization;

namespace hashira.stone.backend.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure authorization policies
    /// </summary>
    public static IServiceCollection ConfigurePolicy(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy: Require authenticated user
            options.AddPolicy("DefaultAuthorizationPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });

            // Custom policy: Must be application user
            options.AddPolicy("MustBeApplicationUser", policy =>
            {
                policy.AddRequirements(new MustBeApplicationUser.Requirement());
            });

            // Custom policy: Must be administrator
            options.AddPolicy("MustBeApplicationAdministrator", policy =>
            {
                policy.Requirements.Add(new MustBeApplicationAdministrator.Requirement());
            });
        });

        // Register custom authorization handlers
        services.AddScoped<IAuthorizationHandler, MustBeApplicationUser.Handler>();
        services.AddScoped<IAuthorizationHandler, MustBeApplicationAdministrator.Handler>();

        return services;
    }
}
```

### Políticas Built-in de ASP.NET Core

```csharp
services.AddAuthorization(options =>
{
    // Require authenticated user
    options.AddPolicy("AuthenticatedOnly", policy =>
        policy.RequireAuthenticatedUser());

    // Require specific role
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Require multiple roles (ANY)
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireRole("Admin", "Manager"));

    // Require specific claim
    options.AddPolicy("EmployeesOnly", policy =>
        policy.RequireClaim("EmployeeNumber"));

    // Require claim with specific value
    options.AddPolicy("HROnly", policy =>
        policy.RequireClaim("Department", "HR"));

    // Complex policy with multiple requirements
    options.AddPolicy("SeniorManagers", policy =>
        policy.RequireRole("Manager")
              .RequireClaim("Seniority", "Senior"));

    // Custom assertion
    options.AddPolicy("Over18", policy =>
        policy.RequireAssertion(context =>
        {
            var ageClaim = context.User.FindFirst("Age");
            return ageClaim != null && int.Parse(ageClaim.Value) >= 18;
        }));
});
```

---

## Custom Authorization Handlers

### MustBeApplicationUser

Handler que verifica si el usuario existe en la base de datos:

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.resources;
using Microsoft.AspNetCore.Authorization;

namespace hashira.stone.backend.webapi.infrastructure.authorization
{
    public static class MustBeApplicationUser
    {
        /// <summary>
        /// Authorization requirement that checks if a user is an application user.
        /// </summary>
        public class Requirement : IAuthorizationRequirement
        {
            public Requirement() { }
        }

        /// <summary>
        /// Authorization handler that checks if the user is an application user.
        /// </summary>
        public class Handler(IUnitOfWork unitOfWork) : AuthorizationHandler<Requirement>
        {
            private readonly IUnitOfWork _unitOfWork = unitOfWork;

            protected override async Task HandleRequirementAsync(
                AuthorizationHandlerContext context,
                Requirement requirement)
            {
                // Get user name from claims
                var userName = context.User.FindFirst(ClaimTypeResource.UserName)?.Value;
                if (string.IsNullOrEmpty(userName))
                    return;

                // Check if user exists in database
                var user = await _unitOfWork.Users.GetByEmailAsync(userName);
                if (user is not null)
                    context.Succeed(requirement);  // Authorization succeeds

                return;
            }
        }
    }
}
```

**Uso en endpoints**:
```csharp
public override void Configure()
{
    Get("/users/{UserName}");
    Policies("MustBeApplicationUser");  // Aplica la policy
}
```

### MustBeApplicationAdministrator

Handler que verifica si el usuario tiene el rol de administrador:

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.resources;
using Microsoft.AspNetCore.Authorization;

namespace hashira.stone.backend.webapi.infrastructure.authorization;

public static class MustBeApplicationAdministrator
{
    const string emailClaim = "email";

    /// <summary>
    /// Authorization requirement that checks if a user is an application administrator.
    /// </summary>
    public class Requirement : IAuthorizationRequirement
    {
        public Requirement() { }
    }

    /// <summary>
    /// Authorization handler that checks if the user is an application administrator.
    /// </summary>
    public class Handler(IUnitOfWork unitOfWork) : AuthorizationHandler<Requirement>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            // Get user email from claims
            var email = context.User.FindFirst(emailClaim)?.Value;
            if (string.IsNullOrEmpty(email))
                return;

            // Get user from database
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
                return;

            // Check if user has PlatformAdministrator role
            var role = user.Roles.FirstOrDefault(r => r.Name == RolesResources.PlatformAdministrator);

            if (role is not null)
                context.Succeed(requirement);  // Authorization succeeds

            return;
        }
    }
}
```

**Uso en endpoints**:
```csharp
public override void Configure()
{
    Post("/users");
    Policies("MustBeApplicationAdministrator");  // Solo administradores
}
```

### Patrón de Custom Authorization Handler

**Estructura recomendada**:

```csharp
public static class MyCustomPolicy
{
    // 1. Define the requirement
    public class Requirement : IAuthorizationRequirement
    {
        public string MinimumLevel { get; }

        public Requirement(string minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }
    }

    // 2. Implement the handler
    public class Handler : AuthorizationHandler<Requirement>
    {
        private readonly IMyService _myService;

        public Handler(IMyService myService)
        {
            _myService = myService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            Requirement requirement)
        {
            // Get claims
            var userLevel = context.User.FindFirst("Level")?.Value;
            if (string.IsNullOrEmpty(userLevel))
                return;  // Fail silently

            // Perform authorization logic
            var authorized = await _myService.CheckLevelAsync(userLevel, requirement.MinimumLevel);

            if (authorized)
                context.Succeed(requirement);  // ✅ Success

            // Don't call context.Fail() unless you want to prevent other handlers from running
            return;
        }
    }
}

// 3. Register in ConfigurePolicy
services.AddAuthorization(options =>
{
    options.AddPolicy("RequiresLevel", policy =>
        policy.AddRequirements(new MyCustomPolicy.Requirement("Gold")));
});
services.AddScoped<IAuthorizationHandler, MyCustomPolicy.Handler>();
```

---

## Seguridad Declarativa en Endpoints

### Policies

Aplica policies registradas:

```csharp
public override void Configure()
{
    Post("/users");
    Policies("MustBeApplicationAdministrator");
}

// Múltiples policies (TODAS deben cumplirse)
public override void Configure()
{
    Post("/sensitive");
    Policies("MustBeApplicationUser", "RequiresVerifiedEmail");
}
```

### Roles

Requiere que el usuario tenga **alguno** de los roles especificados:

```csharp
public override void Configure()
{
    Post("/admin/settings");
    Roles("Admin", "SuperAdmin");  // Acceso si tiene Admin OR SuperAdmin
}
```

### Claims

Requiere que el usuario tenga **alguno** de los claims especificados:

```csharp
public override void Configure()
{
    Get("/employees/data");
    Claims("EmployeeID", "ContractorID");  // Acceso si tiene EmployeeID OR ContractorID
}
```

### ClaimsAll

Requiere que el usuario tenga **todos** los claims especificados:

```csharp
public override void Configure()
{
    Get("/restricted");
    ClaimsAll("Verified", "Active");  // Acceso solo si tiene Verified AND Active
}
```

### Permissions

Requiere que el usuario tenga **alguno** de los permisos especificados:

```csharp
public override void Configure()
{
    Put("/users/{Id}");
    Permissions("Users.Update", "Users.Manage");  // Acceso si tiene cualquiera
}
```

### PermissionsAll

Requiere que el usuario tenga **todos** los permisos especificados:

```csharp
public override void Configure()
{
    Delete("/users/{Id}");
    PermissionsAll("Users.Delete", "Users.Manage");  // Acceso solo con ambos
}
```

### Combinación de Requisitos

Todos los métodos declarativos se combinan con **AND**:

```csharp
public override void Configure()
{
    Post("/api/restricted");
    Claims("AdminID", "EmployeeID");           // Tiene AdminID OR EmployeeID
    Roles("Admin", "Manager");                 // AND tiene Admin OR Manager
    Permissions("UpdateUsers", "DeleteUsers"); // AND tiene UpdateUsers OR DeleteUsers
    Policies("VerifiedEmail");                 // AND cumple VerifiedEmail policy
}
```

**Lógica de autorización**:
```
(AdminID OR EmployeeID)
  AND
(Admin OR Manager)
  AND
(UpdateUsers OR DeleteUsers)
  AND
VerifiedEmail policy succeeds
```

### Ejemplo Completo

```csharp
public class UpdateUserEndpoint : BaseEndpoint<UpdateUserModel.Request, UpdateUserModel.Response>
{
    public override void Configure()
    {
        Put("/users/{Id}");

        // Declarative security
        Policies("MustBeApplicationUser");     // Custom policy
        Roles("Admin", "Manager");             // Role-based
        Permissions("Users.Update");           // Permission-based

        // Documentation
        Description(d => d
            .WithTags("Users")
            .Produces<UpdateUserModel.Response>(200)
            .ProducesProblemDetails(401)
            .ProducesProblemDetails(403)
            .ProducesProblemDetails(404));
    }

    public override async Task HandleAsync(UpdateUserModel.Request req, CancellationToken ct)
    {
        // User is already authorized at this point
        var command = _mapper.Map<UpdateUserUseCase.Command>(req);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            // Handle errors...
            return;
        }

        var response = _mapper.Map<UpdateUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

---

## Claims y IPrincipal

### ClaimTypeResource

Constantes para claim types:

```csharp
namespace hashira.stone.backend.domain.resources
{
    public static class ClaimTypeResource
    {
        /// <summary>
        /// The Email claim type
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// The UserName claim type
        /// </summary>
        public const string UserName = "username";
    }
}
```

### IPrincipalExtender

Extension methods para acceder a claims del usuario:

```csharp
using System.Configuration;
using System.Security.Claims;
using System.Security.Principal;

namespace hashira.stone.backend.webapi;

/// <summary>
/// IPrincipalExtender class
/// </summary>
public static class IPrincipalExtender
{
    private const string EMAILCLAIMTYPE = "email";
    private const string USERNAMECLAIMTYPE = "username";
    private const string NAMECLAIMTYPE = "name";

    /// <summary>
    /// Get a claim from IPrincipal
    /// </summary>
    public static Claim? GetClaim(this IPrincipal principal, string claimType)
    {
        ClaimsPrincipal? claims = principal as ClaimsPrincipal;
        return claims?.FindFirst(claimType);
    }

    /// <summary>
    /// Get a claim value
    /// </summary>
    public static string GetClaimValue(this IPrincipal principal, string claimType)
    {
        var claim = principal.GetClaim(claimType);
        return claim == null ? string.Empty : claim.Value;
    }

    /// <summary>
    /// Get the username from the IPrincipal claims
    /// </summary>
    public static string GetUserName(this IPrincipal principal)
    {
        var userName = principal.GetClaimValue(USERNAMECLAIMTYPE);
        if (string.IsNullOrEmpty(userName))
            throw new ConfigurationErrorsException(
                "No username claim found in the user's principal. " +
                "If the authority is Auth0, verify that a custom action is adding " +
                "the additional claims in the login flow");
        return userName;
    }

    /// <summary>
    /// Get the user email from the IPrincipal claims
    /// </summary>
    public static string GetUserEmail(this IPrincipal principal)
    {
        var email = principal.GetClaimValue(EMAILCLAIMTYPE);
        if (string.IsNullOrEmpty(email))
            throw new ConfigurationErrorsException(
                "No email claim found in the user's principal. " +
                "If the authority is Auth0, verify that a custom action is adding " +
                "the additional claims in the login flow");
        return email;
    }

    /// <summary>
    /// Get the name from the IPrincipal claims
    /// </summary>
    public static string GetName(this IPrincipal principal)
        => principal.GetClaimValue(NAMECLAIMTYPE);
}
```

### Uso en Endpoints

```csharp
public class GetCurrentUserEndpoint : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override void Configure()
    {
        Get("/users/current");
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        // Acceder a claims del usuario autenticado
        var userEmail = User.GetUserName();  // ← Extension method
        if (string.IsNullOrEmpty(userEmail))
        {
            AddError("User is not authenticated or user claim is missing.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
            return;
        }

        var command = new GetUserUseCase.Command { UserName = userEmail };
        var result = await command.ExecuteAsync(ct);

        // ... rest of the handler
    }
}
```

### Acceso Directo a Claims

Alternativa sin extension methods:

```csharp
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    // Acceder al ClaimsPrincipal
    var claimsPrincipal = User as ClaimsPrincipal;

    // Obtener claim específico
    var emailClaim = claimsPrincipal?.FindFirst("email");
    var email = emailClaim?.Value;

    // Obtener todos los claims
    var allClaims = claimsPrincipal?.Claims.ToList();

    // Verificar si tiene claim
    var hasEmployeeId = claimsPrincipal?.HasClaim("EmployeeID", "12345") ?? false;

    // Obtener roles
    var roles = claimsPrincipal?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList();
}
```

---

## Acceso Anónimo

### AllowAnonymous()

Permite acceso sin autenticación:

```csharp
public class HelloEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/hello");
        AllowAnonymous();  // ← No requiere autenticación
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync("Hello World!", ct);
    }
}
```

### AllowAnonymous para Verbos Específicos

Permite acceso anónimo solo para ciertos HTTP verbs:

```csharp
public class UserEndpoint : Endpoint<UserRequest, UserResponse>
{
    public override void Configure()
    {
        Verbs(Http.GET, Http.POST, Http.PUT);
        Routes("/users");

        AllowAnonymous(Http.POST);  // ← Solo POST es anónimo
        // GET y PUT requieren autenticación
    }
}
```

### Ejemplo: Login Endpoint

```csharp
public class LoginEndpoint : Endpoint<LoginModel.Request, LoginModel.Response>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();  // Login debe ser accesible sin token
        Description(d => d
            .WithTags("Authentication")
            .Produces<LoginModel.Response>(200)
            .ProducesProblemDetails(401));
    }

    public override async Task HandleAsync(LoginModel.Request req, CancellationToken ct)
    {
        // Validar credenciales con Auth0
        // Retornar token JWT
        // ...
    }
}
```

---

## Acceso a Información del Usuario

### Información Disponible en Endpoints

FastEndpoints proporciona acceso al usuario autenticado mediante `User`:

```csharp
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    // 1. Identity
    var identity = User.Identity;
    var isAuthenticated = identity?.IsAuthenticated ?? false;
    var authenticationType = identity?.AuthenticationType;
    var userName = identity?.Name;

    // 2. Claims
    var emailClaim = User.FindFirst("email");
    var email = emailClaim?.Value;

    // 3. Roles
    var isAdmin = User.IsInRole("Admin");

    // 4. Extension methods (si están disponibles)
    var userEmail = User.GetUserEmail();
    var fullName = User.GetName();
}
```

### Ejemplo Completo: GetCurrentUserEndpoint

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.webapi.features.users.models;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

/// <summary>
/// Endpoint for retrieving current Application User
/// </summary>
public class GetCurrentUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users/current");
        Description(b => b
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        DontThrowIfValidationFails();
        // No se especifica policy, usa el default (requiere autenticación)
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        // Obtener email del usuario autenticado
        var userEmail = User.GetUserName();
        if (string.IsNullOrEmpty(userEmail))
        {
            AddError("User is not authenticated or user claim is missing.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);
            return;
        }

        // Buscar usuario en base de datos
        var command = new GetUserUseCase.Command { UserName = userEmail };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case UserNotFoundError e:
                    await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
                    break;
                default:
                    await HandleUnexpectedErrorAsync(error, ct);
                    break;
            }
            return;
        }

        var userResponse = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(userResponse, ct);
    }
}
```

---

## Testing de Autenticación

### Mock Authentication en Tests

Para testing, se puede crear un middleware de autenticación falsa:

```csharp
public class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("email", "test@example.com"),
            new Claim("username", "test@example.com"),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

**Configuración en tests**:

```csharp
public class IntegrationTestBase
{
    protected WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace real authentication with fake
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(
                            "Test", options => { });
                });
            });
    }
}
```

### Mock de Auth0Service

El proyecto usa un mock para desarrollo/testing:

```csharp
public static IServiceCollection ConfigureDependencyInjections(
    this IServiceCollection services,
    IWebHostEnvironment environment)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        services.AddScoped<IIdentityService, Auth0ServiceMock>();  // ← Mock
    }
    else
    {
        services.AddScoped<IIdentityService, Auth0Service>();      // ← Real
    }
    return services;
}
```

---

## Mejores Prácticas

### 1. Usar Policies para Lógica Compleja

✅ **Hacer**:
```csharp
// Define policy con lógica compleja
services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeApplicationUser", policy =>
        policy.AddRequirements(new MustBeApplicationUser.Requirement()));
});

// Usa policy en endpoint
public override void Configure()
{
    Get("/users");
    Policies("MustBeApplicationUser");
}
```

❌ **No hacer**:
```csharp
// Lógica de autorización en el handler
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    var email = User.GetUserEmail();
    var user = await _repository.GetByEmailAsync(email);
    if (user == null)
    {
        await Send.ErrorsAsync(403, ct);
        return;
    }
    // ... rest
}
```

### 2. Centralizar Claim Types

✅ **Hacer**:
```csharp
public static class ClaimTypeResource
{
    public const string Email = "email";
    public const string UserName = "username";
    public const string EmployeeId = "employee_id";
}

// Uso
var email = context.User.FindFirst(ClaimTypeResource.Email)?.Value;
```

❌ **No hacer**:
```csharp
var email = context.User.FindFirst("email")?.Value;  // Magic string
var email2 = context.User.FindFirst("Email")?.Value; // Inconsistente
```

### 3. Usar Extension Methods para Claims Comunes

✅ **Hacer**:
```csharp
public static class IPrincipalExtender
{
    public static string GetUserEmail(this IPrincipal principal)
    {
        var email = principal.GetClaimValue("email");
        if (string.IsNullOrEmpty(email))
            throw new ConfigurationErrorsException("No email claim found");
        return email;
    }
}

// Uso limpio
var email = User.GetUserEmail();
```

❌ **No hacer**:
```csharp
// Repetir lógica en cada endpoint
var emailClaim = User.FindFirst("email");
var email = emailClaim?.Value ?? string.Empty;
if (string.IsNullOrEmpty(email))
    throw new Exception("No email");
```

### 4. Documentar Requisitos de Seguridad en Swagger

✅ **Hacer**:
```csharp
public override void Configure()
{
    Post("/users");
    Policies("MustBeApplicationAdministrator");
    Description(d => d
        .WithTags("Users")
        .Produces<UserDto>(201)
        .ProducesProblemDetails(401, "Unauthorized - No token provided")
        .ProducesProblemDetails(403, "Forbidden - Not an administrator"));
}
```

### 5. No Mezclar Authentication y Authorization Logic

✅ **Hacer**:
```csharp
// Authentication: Configuración separada
services.AddAuthentication("Bearer")
    .AddJwtBearer(options => { ... });

// Authorization: Policies separadas
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
```

### 6. Usar HTTPS en Producción

✅ **Hacer**:
```csharp
services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = identityServerUrl;
        options.RequireHttpsMetadata = environment.IsProduction();  // ← True en producción
    });
```

### 7. Validar Claims Críticos

✅ **Hacer**:
```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    Requirement requirement)
{
    var email = context.User.FindFirst("email")?.Value;

    // Validar que el claim existe y es válido
    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
    {
        // Fail silently (no llames context.Fail())
        return;
    }

    // ... rest of logic
}
```

### 8. Fail Silently en Authorization Handlers

✅ **Hacer**:
```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    Requirement requirement)
{
    // Si falla, simplemente return (no context.Fail())
    if (someCondition)
        return;  // ← Fail silently

    // Si tiene éxito
    context.Succeed(requirement);
}
```

❌ **No hacer**:
```csharp
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    Requirement requirement)
{
    if (someCondition)
        context.Fail();  // ❌ Previene que otros handlers corran
}
```

### 9. Usar Roles Constants

✅ **Hacer**:
```csharp
public static class RolesResources
{
    public const string PlatformAdministrator = "PlatformAdministrator";
    public const string Manager = "Manager";
    public const string User = "User";
}

// Uso
var isAdmin = user.Roles.Any(r => r.Name == RolesResources.PlatformAdministrator);
```

### 10. Configurar CORS Adecuadamente

✅ **Hacer**:
```csharp
services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
        .WithOrigins(allowedOrigins)  // Orígenes específicos
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());  // Si usas cookies
});
```

---

## Anti-patrones

### 1. Hardcodear Secrets en Código

❌ **Evitar**:
```csharp
services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = "https://my-tenant.auth0.com/";  // ❌ Hardcoded
    });
```

✅ **Mejor**:
```csharp
var authority = configuration["IdentityServerConfiguration:Address"];
services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = authority;  // ✅ Desde configuración
    });
```

### 2. Autorización en el Handler en Lugar de Policies

❌ **Evitar**:
```csharp
public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    // ❌ Lógica de autorización mezclada con lógica de negocio
    if (!User.IsInRole("Admin"))
    {
        await Send.ErrorsAsync(403, ct);
        return;
    }

    // Business logic...
}
```

✅ **Mejor**:
```csharp
public override void Configure()
{
    Post("/admin/action");
    Roles("Admin");  // ✅ Declarativo, separado
}

public override async Task HandleAsync(MyRequest req, CancellationToken ct)
{
    // Solo lógica de negocio
}
```

### 3. No Validar Claims del Token

❌ **Evitar**:
```csharp
var email = User.FindFirst("email")?.Value;
// Usar email sin validar si existe o es válido
var user = await _repository.GetByEmailAsync(email);
```

✅ **Mejor**:
```csharp
var email = User.FindFirst("email")?.Value;
if (string.IsNullOrEmpty(email))
{
    AddError("Email claim is missing");
    await Send.ErrorsAsync(401, ct);
    return;
}

var user = await _repository.GetByEmailAsync(email);
```

### 4. Usar AllowAnonymous sin Validación

❌ **Evitar**:
```csharp
public override void Configure()
{
    Post("/users/{Id}/delete");
    AllowAnonymous();  // ❌ Operación sensible sin autenticación
}
```

### 5. No Usar Extension Methods para Claims Repetidos

❌ **Evitar**:
```csharp
// Repetir en cada endpoint
var emailClaim = User.FindFirst("email");
var email = emailClaim?.Value ?? string.Empty;
if (string.IsNullOrEmpty(email))
    throw new Exception("No email");
```

✅ **Mejor**:
```csharp
// Una vez en IPrincipalExtender
public static string GetUserEmail(this IPrincipal principal) { ... }

// Usar en todos los endpoints
var email = User.GetUserEmail();
```

### 6. Mezclar Múltiples Esquemas sin Claridad

❌ **Evitar**:
```csharp
services.AddAuthentication()
    .AddJwtBearer(...)
    .AddCookie(...);
// ¿Cuál es el default? No está claro
```

✅ **Mejor**:
```csharp
services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;  // ✅ Explícito
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(...)
.AddCookie(...);
```

### 7. Exponer Información Sensible en Claims

❌ **Evitar**:
```csharp
// En Auth0 Action
api.idToken.setCustomClaim("password", user.password);  // ❌ NUNCA
api.idToken.setCustomClaim("ssn", user.socialSecurity);  // ❌ NUNCA
```

✅ **Mejor**:
```csharp
// Solo información no sensible
api.idToken.setCustomClaim("email", user.email);
api.idToken.setCustomClaim("name", user.name);
api.idToken.setCustomClaim("roles", user.roles);
```

---

## Referencias

### Documentación Oficial

- [FastEndpoints - Security](https://fast-endpoints.com/docs/security)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
- [Auth0 Documentation](https://auth0.com/docs)
- [JWT.io](https://jwt.io/)
- [OAuth 2.0](https://oauth.net/2/)
- [OpenID Connect](https://openid.net/connect/)

### Guías Relacionadas

- [error-responses.md](error-responses.md) - Manejo de errores 401/403
- [fastendpoints-basics.md](fastendpoints-basics.md) - Estructura de endpoints
- [../infrastructure-layer/external-services/identity-providers/auth0.md](../infrastructure-layer/external-services/identity-providers/auth0.md) - Integración con Auth0
- [../best-practices/dependency-injection.md](../best-practices/dependency-injection.md) - DI patterns

---

**Versión:** 1.0.0
**Última actualización:** 2025-11-15
**Maintainers:** Equipo APSYS
