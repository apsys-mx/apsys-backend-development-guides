# FastEndpoints Basics - Estructura de Endpoints

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-11-15

## 📋 Tabla de Contenidos
1. [Anatomía de un Endpoint](#anatomía-de-un-endpoint)
2. [Tipos de Endpoints](#tipos-de-endpoints)
3. [Método Configure()](#método-configure)
4. [Método HandleAsync()](#método-handleasync)
5. [Route Parameters](#route-parameters)
6. [Query Parameters](#query-parameters)
7. [Verbos HTTP](#verbos-http)
8. [Autorización y Políticas](#autorización-y-políticas)
9. [Dependency Injection](#dependency-injection)
10. [Ejemplos Completos](#ejemplos-completos)
11. [Mejores Prácticas](#mejores-prácticas)
12. [Referencias](#referencias)

---

## Anatomía de un Endpoint

Un endpoint en FastEndpoints tiene **dos componentes principales**:

```csharp
using FastEndpoints;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.users.models;
using hashira.stone.backend.application.usecases.users;

namespace hashira.stone.backend.webapi.features.users.endpoint;

public class CreateUserEndpoint(AutoMapper.IMapper mapper)  // 👈 Constructor DI
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>  // 👈 Base class
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    // 1️⃣ CONFIGURACIÓN
    public override void Configure()
    {
        Post("/users");                                    // Verbo + Ruta
        Description(b => b                                // Swagger docs
            .Produces<UserDto>(201)
            .ProducesProblemDetails(400));
        Policies("MustBeApplicationAdministrator");       // Autorización
        DontThrowIfValidationFails();                     // Validación manual
    }

    // 2️⃣ LÓGICA DE NEGOCIO
    public override async Task HandleAsync(
        CreateUserModel.Request request,                 // Request DTO
        CancellationToken ct)                            // Cancellation token
    {
        // 2.1. Mapear Request → Command
        var command = _mapper.Map<CreateUserUseCase.Command>(request);

        // 2.2. Ejecutar Use Case
        var result = await command.ExecuteAsync(ct);

        // 2.3. Manejar errores
        if (result.IsFailed)
        {
            AddError(result.Errors.First().Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        // 2.4. Mapear y devolver respuesta
        var response = _mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAtAsync($"/users/{response.User.Id}", response, ct);
    }
}
```

### 🔑 Componentes Clave

| Componente | Descripción | Ubicación |
|------------|-------------|-----------|
| **Herencia** | `Endpoint<TRequest, TResponse>` | Clase base |
| **Constructor** | Dependency Injection | Constructor |
| **Configure()** | Configuración de routing, auth, swagger | Método override |
| **HandleAsync()** | Lógica del endpoint | Método override |
| **Request DTO** | Datos de entrada | Parámetro |
| **Response DTO** | Datos de salida | Return value |
| **CancellationToken** | Cancelación de operaciones | Parámetro |

---

## Tipos de Endpoints

FastEndpoints ofrece 4 tipos base según la presencia de Request/Response:

### 1. `Endpoint<TRequest, TResponse>` ✅ **Más común**

**Uso**: Cuando tienes entrada Y salida.

```csharp
public class CreateUserEndpoint
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public override void Configure()
    {
        Post("/users");
    }

    public override async Task HandleAsync(
        CreateUserModel.Request req,  // ✅ Tiene request
        CancellationToken ct)
    {
        // ... lógica
        await Send.OkAsync(new CreateUserModel.Response { ... }, ct);  // ✅ Tiene response
    }
}
```

**Ejemplos**: `CreateUserEndpoint`, `UpdateUserEndpoint`, `GetUserEndpoint`

---

### 2. `Endpoint<TRequest>` (sin Response explícito)

**Uso**: Cuando tienes entrada pero no devuelves datos específicos.

```csharp
public class DeleteUserEndpoint
    : Endpoint<DeleteUserModel.Request>
{
    public override void Configure()
    {
        Delete("/users/{Id}");
    }

    public override async Task HandleAsync(
        DeleteUserModel.Request req,  // ✅ Tiene request
        CancellationToken ct)
    {
        // ... lógica de eliminación
        await Send.NoContentAsync(ct);  // ❌ No devuelve datos (204 No Content)
    }
}
```

**Ejemplos**: Endpoints de eliminación, comandos sin respuesta

---

### 3. `EndpointWithoutRequest<TResponse>`

**Uso**: Cuando NO tienes entrada pero SÍ devuelves datos.

```csharp
public class GetCurrentUserEndpoint
    : EndpointWithoutRequest<GetCurrentUserModel.Response>
{
    public override void Configure()
    {
        Get("/users/current");
    }

    public override async Task HandleAsync(CancellationToken ct)  // ❌ No tiene request
    {
        var userId = User.Identity.Name;  // Obtener del contexto
        // ... lógica
        await Send.OkAsync(new GetCurrentUserModel.Response { ... }, ct);  // ✅ Tiene response
    }
}
```

**Ejemplos**: Obtener usuario actual, health checks con datos

---

### 4. `EndpointWithoutRequest` (sin Request ni Response)

**Uso**: Cuando NO tienes entrada NI salida (muy raro).

```csharp
public class HealthCheckEndpoint
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)  // ❌ No tiene request
    {
        await Send.OkAsync(ct);  // ❌ No devuelve datos (solo 200 OK)
    }
}
```

**Ejemplos**: Health checks simples, warmup endpoints

---

### 📌 Fluent Generics (Alternativa)

FastEndpoints también ofrece sintaxis fluent para definir tipos:

```csharp
// Equivalente a Endpoint<TRequest, TResponse>
public class CreateUserEndpoint : Ep.Req<CreateUserModel.Request>.Res<CreateUserModel.Response>
{
    // ...
}

// Equivalente a Endpoint<TRequest>
public class DeleteUserEndpoint : Ep.Req<DeleteUserModel.Request>.NoRes
{
    // ...
}

// Equivalente a EndpointWithoutRequest<TResponse>
public class GetCurrentUserEndpoint : Ep.NoReq.Res<GetCurrentUserModel.Response>
{
    // ...
}

// Equivalente a EndpointWithoutRequest
public class HealthCheckEndpoint : Ep.NoReq.NoRes
{
    // ...
}
```

**Recomendación APSYS**: Usar la sintaxis tradicional (`Endpoint<TRequest, TResponse>`) para mayor claridad.

---

## Método Configure()

El método `Configure()` se ejecuta **una vez al inicio de la aplicación** y define:

### 1. 🛣️ Verbo HTTP y Ruta

```csharp
public override void Configure()
{
    // Verbos HTTP
    Get("/users");                      // GET /users
    Post("/users");                     // POST /users
    Put("/users/{Id}");                 // PUT /users/123
    Patch("/users/{Id}");               // PATCH /users/123
    Delete("/users/{Id}");              // DELETE /users/123

    // Múltiples verbos para la misma ruta (NO recomendado)
    Verbs(Http.GET, Http.POST);
    Routes("/users");
}
```

---

### 2. 📝 Descripción y Documentación Swagger

```csharp
public override void Configure()
{
    Get("/users/{UserName}");

    // Documentación básica
    Description(d => d
        .WithTags("Users")                                    // Tag de Swagger
        .WithName("GetUser")                                  // Nombre operación
        .WithDescription("Get a user by username")            // Descripción
        .Produces<GetUserModel.Response>(200, "application/json")
        .ProducesProblemDetails(404)
        .ProducesProblemDetails(500));

    // Documentación detallada (opcional)
    Summary(s =>
    {
        s.Summary = "Get a user by username";
        s.Description = "Retrieves a user from the system using their unique username";
        s.RequestParam(r => r.UserName, "The unique username of the user");
        s.Response<GetUserModel.Response>(200, "User found and returned");
        s.Response(404, "User not found");
        s.ExampleRequest = new GetUserModel.Request { UserName = "john.doe" };
    });
}
```

---

### 3. 🔒 Autorización y Políticas

```csharp
public override void Configure()
{
    Post("/users");

    // Opción 1: Sin autenticación (público)
    AllowAnonymous();

    // Opción 2: Requiere autenticación (cualquier usuario autenticado)
    // (No se especifica nada, por defecto requiere auth)

    // Opción 3: Requiere política específica
    Policies("MustBeApplicationAdministrator");

    // Opción 4: Múltiples políticas (AND - todas deben cumplirse)
    Policies("MustBeApplicationUser", "MustBeFromTenant001");

    // Opción 5: Roles (alternativa a políticas)
    Roles("Admin", "Moderator");  // OR - cualquier rol es válido

    // Opción 6: Claims
    Claims("Permission", "users.create");
}
```

**Patrón APSYS**: Usar `Policies()` definidas en `PolicyConfiguration.cs`.

---

### 4. ✅ Validación

```csharp
public override void Configure()
{
    Post("/users");

    // Opción 1: Lanzar excepción si falla validación (default)
    // (No se especifica nada)

    // Opción 2: NO lanzar excepción, manejar manualmente
    DontThrowIfValidationFails();  // ✅ Recomendado para control fino

    // Opción 3: Desactivar validación
    DontValidate();
}
```

**Patrón APSYS**: Usar `DontThrowIfValidationFails()` y manejar errores manualmente.

---

### 5. ⚙️ Otras Configuraciones

```csharp
public override void Configure()
{
    Get("/users");

    // Throttling / Rate Limiting
    Throttle(
        hitLimit: 10,               // 10 requests
        durationSeconds: 60);       // por minuto

    // CORS personalizado
    Options(b => b.RequireCors(x => x.AllowAnyOrigin()));

    // Versioning
    Version(1);  // API v1

    // Response caching
    ResponseCache(60);  // Cache por 60 segundos

    // Pre/Post processors
    PreProcessor<SecurityPreProcessor>();
    PostProcessor<LoggingPostProcessor>();
}
```

---

## Método HandleAsync()

El método `HandleAsync()` se ejecuta **cada vez que llega una request** y contiene la lógica del endpoint.

### 📌 Estructura Típica

```csharp
public override async Task HandleAsync(
    CreateUserModel.Request request,  // 1️⃣ Request DTO
    CancellationToken ct)              // 2️⃣ Cancellation token
{
    // 1. Mapear Request → Command
    var command = _mapper.Map<CreateUserUseCase.Command>(request);

    // 2. Ejecutar Use Case
    var result = await command.ExecuteAsync(ct);

    // 3. Manejar errores
    if (result.IsFailed)
    {
        var error = result.Errors.FirstOrDefault();

        // Mapear excepciones de dominio a HTTP status codes
        if (error?.Reasons.OfType<ExceptionalError>()
            .Any(r => r.Exception is DuplicatedDomainException) == true)
        {
            AddError(error.Message);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
            return;
        }

        // ... más manejo de errores

        await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        return;
    }

    // 4. Mapear Entity → Response DTO
    var response = _mapper.Map<CreateUserModel.Response>(result.Value);

    // 5. Enviar respuesta
    await Send.CreatedAtAsync($"/users/{response.User.Id}", response, ct);
}
```

### 🔑 Métodos de Respuesta

#### ✅ Respuestas de Éxito

```csharp
// 200 OK
await Send.OkAsync(response, ct);

// 201 Created (con Location header)
await Send.CreatedAtAsync(
    uri: $"/users/{userId}",
    routeValues: new { userId },
    responseBody: response,
    generateAbsoluteUrl: false,
    cancellation: ct);

// 204 No Content
await Send.NoContentAsync(ct);

// 202 Accepted
await Send.AcceptedAsync($"/jobs/{jobId}", ct);
```

#### ❌ Respuestas de Error

```csharp
// 400 Bad Request (con errores de validación)
AddError("Email is required");
AddError(r => r.Email, "Invalid email format");
await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);

// 404 Not Found
await SendNotFoundAsync(ct);

// 401 Unauthorized
await SendUnauthorizedAsync(ct);

// 403 Forbidden
await SendForbiddenAsync(ct);

// 409 Conflict
await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

// 500 Internal Server Error
await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
```

#### 📄 Otras Respuestas

```csharp
// Archivo
await SendFileAsync(fileInfo, ct: ct);

// Stream
await SendStreamAsync(stream, fileName: "report.pdf", ct: ct);

// String (texto plano)
await SendStringAsync("Hello World", ct: ct);

// Bytes
await SendBytesAsync(bytes, ct: ct);
```

---

## Route Parameters

Los route parameters se capturan de la URL y se mapean automáticamente al Request DTO.

### 📌 Definición

```csharp
// Request DTO
public class GetUserModel
{
    public class Request
    {
        public string UserName { get; set; } = string.Empty;  // 👈 Debe coincidir con nombre
    }

    public class Response
    {
        public UserDto User { get; set; } = new UserDto();
    }
}

// Endpoint
public class GetUserEndpoint : Endpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override void Configure()
    {
        Get("/users/{UserName}");  // 👈 {UserName} se mapea a Request.UserName
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        // req.UserName contiene el valor de la URL
        // Ej: GET /users/john.doe → req.UserName = "john.doe"
    }
}
```

### 🔑 Múltiples Route Parameters

```csharp
// Request DTO
public class UpdateUserModel
{
    public class Request
    {
        public Guid Id { get; set; }             // 👈 Route parameter
        public string Name { get; set; } = string.Empty;   // Body
        public string Email { get; set; } = string.Empty;  // Body
    }
}

// Endpoint
public override void Configure()
{
    Put("/users/{Id}");  // 👈 {Id} se mapea a Request.Id
}
```

**Request**:
```http
PUT /users/550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**Mapping**:
- `Id` → De la URL: `550e8400-e29b-41d4-a716-446655440000`
- `Name` → Del body: `"John Doe"`
- `Email` → Del body: `"john@example.com"`

---

### ⚠️ Case Sensitivity

FastEndpoints es **case-insensitive** por defecto para route parameters:

```csharp
Get("/users/{UserName}");   // ✅
Get("/users/{username}");   // ✅ También funciona
Get("/users/{USERNAME}");   // ✅ También funciona

// Todos mapean a Request.UserName
```

**Recomendación APSYS**: Usar **PascalCase** en la ruta para consistencia con C#.

---

## Query Parameters

Los query parameters se capturan de la query string y se mapean al Request DTO.

### 📌 Definición

```csharp
// Request DTO
public class GetManyAndCountModel
{
    public class Request
    {
        public int PageNumber { get; set; } = 1;      // 👈 Query param
        public int PageSize { get; set; } = 25;       // 👈 Query param
        public string? SortBy { get; set; }           // 👈 Query param
        public string? SortDirection { get; set; }    // 👈 Query param
        public bool? IsActive { get; set; }           // 👈 Query param
    }
}

// Endpoint
public class GetManyAndCountUsersEndpoint
    : Endpoint<GetManyAndCountModel.Request, GetManyAndCountResultDto<UserDto>>
{
    public override void Configure()
    {
        Get("/users");  // Sin route parameters
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        // FastEndpoints mapea automáticamente los query params
        // req.PageNumber, req.PageSize, etc.
    }
}
```

**Request**:
```http
GET /users?PageNumber=2&PageSize=50&SortBy=Email&SortDirection=asc&IsActive=true
```

**Mapping**:
```csharp
req.PageNumber = 2
req.PageSize = 50
req.SortBy = "Email"
req.SortDirection = "asc"
req.IsActive = true
```

---

### 🔑 Query String Completo

En el proyecto APSYS, para filtrado avanzado se pasa el query string completo al Use Case:

```csharp
public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
{
    // Pasar TODA la query string al Use Case para parsing avanzado
    var command = new GetManyAndCountUsersUseCase.Command
    {
        Query = HttpContext.Request.QueryString.Value  // 👈 Query completo
    };

    var result = await command.ExecuteAsync(ct);
    // ...
}
```

**Query String**:
```
?pageNumber=1&pageSize=25&sortBy=Email&sortDirection=asc&IsActive=true||eq&Name=John||contains
```

El Use Case parsea el query string usando `QueryStringParser` (ver [NHibernate Queries Guide](../infrastructure-layer/orm-implementations/nhibernate/queries.md)).

---

## Verbos HTTP

FastEndpoints soporta todos los verbos HTTP estándar:

### 📌 Métodos Disponibles

```csharp
public override void Configure()
{
    // GET - Obtener recursos
    Get("/users");
    Get("/users/{Id}");

    // POST - Crear recursos
    Post("/users");

    // PUT - Actualizar completo (reemplazar)
    Put("/users/{Id}");

    // PATCH - Actualizar parcial
    Patch("/users/{Id}");

    // DELETE - Eliminar recursos
    Delete("/users/{Id}");

    // HEAD - Headers sin body (raro)
    Head("/users/{Id}");

    // OPTIONS - Descubrir opciones (CORS)
    Options("/users");

    // Múltiples verbos (NO recomendado)
    Verbs(Http.GET, Http.POST);
    Routes("/users");
}
```

### 🎯 Cuándo Usar Cada Verbo

| Verbo | Uso | Ejemplo APSYS |
|-------|-----|---------------|
| **GET** | Obtener recursos, sin side effects | `GetUserEndpoint`, `GetManyAndCountUsersEndpoint` |
| **POST** | Crear nuevos recursos | `CreateUserEndpoint` |
| **PUT** | Actualizar completo (reemplazar entidad) | `UpdateTechnicalStandardEndpoint` |
| **PATCH** | Actualizar parcial (solo campos específicos) | `UpdateUserLockEndpoint` (lock/unlock) |
| **DELETE** | Eliminar recursos | `DeleteUserEndpoint` (si existiera) |

### ⚠️ Idempotencia

| Verbo | Idempotente | Safe |
|-------|-------------|------|
| GET | ✅ Sí | ✅ Sí |
| POST | ❌ No | ❌ No |
| PUT | ✅ Sí | ❌ No |
| PATCH | ❌ No* | ❌ No |
| DELETE | ✅ Sí | ❌ No |

*PATCH puede ser idempotente dependiendo de la implementación.

---

## Autorización y Políticas

FastEndpoints integra el sistema de autorización de ASP.NET Core.

### 📌 Configuración de Políticas

Las políticas se definen en `infrastructure/PolicyConfiguration.cs`:

```csharp
public static class PolicyConfiguration
{
    public static IServiceCollection ConfigurePolicy(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Política: Debe ser Administrador de la Aplicación
            options.AddPolicy("MustBeApplicationAdministrator", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("permissions", "application:admin");
            });

            // Política: Debe ser Usuario de la Aplicación
            options.AddPolicy("MustBeApplicationUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("permissions", "application:user");
            });

            // Otras políticas...
        });

        return services;
    }
}
```

### 🔒 Uso en Endpoints

```csharp
public override void Configure()
{
    Post("/users");

    // ✅ Opción 1: Público (sin autenticación)
    AllowAnonymous();

    // ✅ Opción 2: Requiere autenticación (cualquier usuario logueado)
    // (por defecto, no especificar nada)

    // ✅ Opción 3: Requiere política específica (recomendado)
    Policies("MustBeApplicationAdministrator");

    // ✅ Opción 4: Múltiples políticas (AND)
    Policies("MustBeApplicationUser", "MustHaveWritePermission");

    // ⚠️ Opción 5: Roles (alternativa)
    Roles("Admin", "Moderator");  // OR

    // ⚠️ Opción 6: Claims directos (no recomendado, usar políticas)
    Claims("email", "admin@example.com");
}
```

### 🎯 Patrón APSYS

**Usar políticas** definidas en `PolicyConfiguration.cs`:

```csharp
// ✅ CORRECTO
Policies("MustBeApplicationAdministrator");

// ❌ EVITAR - Claims directos
Claims("permissions", "application:admin");

// ❌ EVITAR - Roles directos
Roles("Admin");
```

**Por qué**: Las políticas son reutilizables, centralizadas y fáciles de mantener.

---

### 🔍 Acceder a Usuario Actual

```csharp
public override async Task HandleAsync(GetCurrentUserModel.Request req, CancellationToken ct)
{
    // Opción 1: Desde User (ClaimsPrincipal)
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = User.FindFirst(ClaimTypes.Email)?.Value;
    var username = User.Identity?.Name;

    // Opción 2: Desde HttpContext
    var userId2 = HttpContext.User.FindFirst("sub")?.Value;

    // Opción 3: Extension method personalizado
    var tenantId = User.GetTenantId();  // Extension method
}
```

---

## Dependency Injection

FastEndpoints soporta **Constructor Injection** y **Property Injection**.

### 1️⃣ Constructor Injection ✅ **Recomendado**

```csharp
public class CreateUserEndpoint(
    AutoMapper.IMapper mapper,
    ILogger<CreateUserEndpoint> logger)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;
    private readonly ILogger<CreateUserEndpoint> _logger = logger;

    public override async Task HandleAsync(
        CreateUserModel.Request request,
        CancellationToken ct)
    {
        _logger.LogInformation("Creating user: {Email}", request.Email);
        // ...
    }
}
```

**Ventajas**:
- ✅ Type-safe
- ✅ Compile-time errors
- ✅ Más testeable
- ✅ Inmutable

---

### 2️⃣ Property Injection

```csharp
public class CreateUserEndpoint
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public AutoMapper.IMapper Mapper { get; set; } = default!;
    public ILogger<CreateUserEndpoint> Logger { get; set; } = default!;

    public override async Task HandleAsync(
        CreateUserModel.Request request,
        CancellationToken ct)
    {
        Logger.LogInformation("Creating user: {Email}", request.Email);
        // ...
    }
}
```

**Desventajas**:
- ⚠️ Menos type-safe
- ⚠️ Errores en runtime si no se inyecta
- ⚠️ Mutable

---

### 3️⃣ Resolver en Runtime

```csharp
public override async Task HandleAsync(
    CreateUserModel.Request request,
    CancellationToken ct)
{
    // Resolver servicio on-demand
    var mapper = HttpContext.Resolve<AutoMapper.IMapper>();
    var logger = HttpContext.Resolve<ILogger<CreateUserEndpoint>>();

    // Usar servicios
    logger.LogInformation("Creating user");
}
```

**Uso**: Para servicios opcionales o condicionales.

---

### 🎯 Patrón APSYS

**Usar Constructor Injection** para servicios requeridos:

```csharp
// ✅ CORRECTO
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;
}

// ⚠️ EVITAR (a menos que sea necesario)
public class CreateUserEndpoint
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public AutoMapper.IMapper Mapper { get; set; } = default!;
}
```

---

## Ejemplos Completos

### 📋 Ejemplo 1: GET con Route Parameter

```csharp
// 1. Model
public class GetUserModel
{
    public class Request
    {
        public string UserName { get; set; } = string.Empty;
    }

    public class Response
    {
        public UserDto User { get; set; } = new UserDto();
    }
}

// 2. Endpoint
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users/{UserName}");
        Description(d => d
            .WithTags("Users")
            .WithName("GetUser")
            .WithDescription("Get a user by username")
            .Produces<GetUserModel.Response>(200)
            .ProducesProblemDetails(404));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var command = new GetUserUseCase.Command { UserName = req.UserName };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case UserNotFoundError e:
                    await HandleErrorAsync(
                        r => r.UserName,
                        e.Message,
                        HttpStatusCode.NotFound,
                        ct);
                    break;
                default:
                    await HandleUnexpectedErrorAsync(error, ct);
                    break;
            }
            return;
        }

        var response = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

**Request**:
```http
GET /users/john.doe HTTP/1.1
Authorization: Bearer <token>
```

**Response** (200 OK):
```json
{
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "John Doe",
    "email": "john.doe@example.com",
    "roles": ["Admin", "User"]
  }
}
```

---

### 📋 Ejemplo 2: PUT con Route Parameter y Body

```csharp
// 1. Model
public class UpdateTechnicalStandardModel
{
    public class Request
    {
        public Guid Id { get; set; }                     // Route param
        public string Code { get; set; } = string.Empty;  // Body
        public string Title { get; set; } = string.Empty; // Body
        public string Description { get; set; } = string.Empty; // Body
    }

    public class Response
    {
        public TechnicalStandardDto TechnicalStandard { get; set; } = new();
    }
}

// 2. Endpoint
public class UpdateTechnicalStandardEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<UpdateTechnicalStandardModel.Request, UpdateTechnicalStandardModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Put("/technical-standards/{Id}");
        Policies("MustBeApplicationUser");
        Summary(s =>
        {
            s.Summary = "Update an existing technical standard";
            s.Description = "Updates a technical standard by ID.";
            s.RequestParam(r => r.Id, "Technical standard ID");
            s.Response<UpdateTechnicalStandardModel.Response>(200, "Technical standard updated");
            s.Response(404, "Technical standard not found");
            s.Response(400, "Invalid data");
            s.Response(409, "Duplicate code");
        });
    }

    public override async Task HandleAsync(
        UpdateTechnicalStandardModel.Request req,
        CancellationToken ct)
    {
        try
        {
            var command = _mapper.Map<UpdateTechnicalStandardUseCase.Command>(req);
            var result = await command.ExecuteAsync(ct);

            if (result.IsSuccess)
            {
                var response = _mapper.Map<UpdateTechnicalStandardModel.Response>(result.Value);
                await Send.OkAsync(response, ct);
                return;
            }

            var error = result.Errors.FirstOrDefault();
            var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

            switch (errorType?.Exception)
            {
                case InvalidDomainException:
                    await HandleErrorWithMessageAsync(
                        error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
                    break;
                case DuplicatedDomainException:
                    await HandleErrorWithMessageAsync(
                        error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
                    break;
                case ResourceNotFoundException:
                    await HandleErrorWithMessageAsync(
                        error, error?.Message ?? "", ct, HttpStatusCode.NotFound);
                    break;
                default:
                    await HandleErrorWithMessageAsync(
                        error, error?.Message ?? "Unknown error", ct, HttpStatusCode.InternalServerError);
                    break;
            }
        }
        catch (Exception ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        }
    }
}
```

**Request**:
```http
PUT /technical-standards/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Authorization: Bearer <token>
Content-Type: application/json

{
  "code": "ISO-9001",
  "title": "Quality Management System",
  "description": "International standard for quality management"
}
```

**Response** (200 OK):
```json
{
  "technicalStandard": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "code": "ISO-9001",
    "title": "Quality Management System",
    "description": "International standard for quality management",
    "createdAt": "2025-01-15T10:00:00Z"
  }
}
```

---

### 📋 Ejemplo 3: GET Many con Query Parameters

```csharp
// 1. Model
public class GetManyAndCountModel
{
    public class Request
    {
        // Vacío - query params se obtienen del QueryString completo
    }
}

// 2. Endpoint
public class GetManyAndCountUsersEndPoint(AutoMapper.IMapper mapper)
    : Endpoint<GetManyAndCountModel.Request, GetManyAndCountResultDto<UserDto>>
{
    private readonly AutoMapper.IMapper mapper = mapper;

    public override void Configure()
    {
        Get("/users");
        Description(d => d
            .WithTags("Users")
            .WithName("GetManyAndCountUsers")
            .WithDescription("Get a list of users with optional filtering and pagination")
            .Produces<GetManyAndCountResultDto<UserDto>>(200));
        DontThrowIfValidationFails();
        Policies("MustBeApplicationUser");
    }

    override public async Task HandleAsync(
        GetManyAndCountModel.Request req,
        CancellationToken ct)
    {
        try
        {
            var request = new GetManyAndCountUsersUseCase.Command
            {
                Query = HttpContext.Request.QueryString.Value  // Query completo
            };

            var getManyAndCountResult = await request.ExecuteAsync(ct);
            var response = mapper.Map<GetManyAndCountResultDto<UserDto>>(getManyAndCountResult);

            Logger.LogInformation("Successfully retrieved users");
            await Send.OkAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve users");
            AddError(ex.Message);
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, cancellation: ct);
        }
    }
}
```

**Request**:
```http
GET /users?pageNumber=1&pageSize=25&sortBy=Email&sortDirection=asc&IsActive=true||eq HTTP/1.1
Authorization: Bearer <token>
```

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "John Doe",
      "email": "john@example.com",
      "roles": ["Admin"]
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "Jane Smith",
      "email": "jane@example.com",
      "roles": ["User"]
    }
  ],
  "count": 42,
  "pageNumber": 1,
  "pageSize": 25,
  "sortBy": "Email",
  "sortCriteria": "asc"
}
```

---

## Mejores Prácticas

### ✅ 1. Usar BaseEndpoint para Helpers Comunes

```csharp
// ✅ CORRECTO - Usar BaseEndpoint
public class GetUserEndpoint : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        if (result.IsFailed)
        {
            await HandleErrorAsync(
                r => r.UserName,
                "User not found",
                HttpStatusCode.NotFound,
                ct);  // ✅ Helper de BaseEndpoint
            return;
        }
    }
}

// ❌ EVITAR - Código repetido
public class GetUserEndpoint : Endpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        if (result.IsFailed)
        {
            Logger.LogWarning("User not found");  // ❌ Repetido
            AddError(r => r.UserName, "User not found");
            await Send.ErrorsAsync(404, ct);
            return;
        }
    }
}
```

---

### ✅ 2. Un Archivo por Endpoint

```
features/users/endpoint/
├── CreateUserEndpoint.cs     ✅
├── GetUserEndpoint.cs         ✅
├── GetManyAndCountUsersEndpoint.cs  ✅
└── UpdateUserEndpoint.cs      ✅

// ❌ EVITAR
features/users/
└── UserEndpoints.cs  (con múltiples clases)
```

---

### ✅ 3. Usar Constructor Injection

```csharp
// ✅ CORRECTO
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;
}

// ⚠️ EVITAR
public class CreateUserEndpoint
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public AutoMapper.IMapper Mapper { get; set; } = default!;
}
```

---

### ✅ 4. DontThrowIfValidationFails para Control Fino

```csharp
public override void Configure()
{
    Post("/users");
    DontThrowIfValidationFails();  // ✅ Recomendado
}

public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    // Manejar errores de validación manualmente
    if (ValidationFailed)
    {
        await Send.ErrorsAsync(cancellation: ct);
        return;
    }
}
```

---

### ✅ 5. Documentar con Description y Summary

```csharp
public override void Configure()
{
    Get("/users/{UserName}");

    // ✅ Documentación completa
    Description(d => d
        .WithTags("Users")
        .WithName("GetUser")
        .Produces<GetUserModel.Response>(200));

    Summary(s =>
    {
        s.Summary = "Get a user by username";
        s.RequestParam(r => r.UserName, "Unique username");
    });
}
```

---

### ✅ 6. Mapear Excepciones de Dominio a HTTP Status

```csharp
// ✅ CORRECTO - Mapeo consistente
if (result.IsFailed)
{
    var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

    switch (errorType?.Exception)
    {
        case InvalidDomainException:
            await Send.ErrorsAsync(400, ct);  // Bad Request
            break;
        case DuplicatedDomainException:
            await Send.ErrorsAsync(409, ct);  // Conflict
            break;
        case ResourceNotFoundException:
            await Send.ErrorsAsync(404, ct);  // Not Found
            break;
        default:
            await Send.ErrorsAsync(500, ct);  // Internal Error
            break;
    }
}
```

---

### ✅ 7. Usar Políticas en Lugar de Roles/Claims

```csharp
// ✅ CORRECTO
Policies("MustBeApplicationAdministrator");

// ❌ EVITAR
Roles("Admin");
Claims("permissions", "application:admin");
```

---

## Referencias

### 📚 FastEndpoints

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [FastEndpoints Configuration](https://fast-endpoints.com/docs/configuration)
- [FastEndpoints Endpoint Definition](https://fast-endpoints.com/docs/endpoint-definition)
- [FastEndpoints Model Binding](https://fast-endpoints.com/docs/model-binding)

### 🔗 Guías Relacionadas

- [WebApi Layer README](./README.md) - Overview de WebApi Layer
- [Request/Response Models](./request-response-models.md) - Patrones de Models
- [Error Responses](./error-responses.md) - Manejo de errores HTTP
- [Authentication](./authentication.md) - Autenticación y autorización

---

## 🔄 Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2025-11-15 | Versión inicial de FastEndpoints Basics |

---

**Anterior**: [WebApi Layer README](./README.md) ← Overview de WebApi Layer

**Siguiente**: [Request/Response Models](./request-response-models.md) → Patrones de Models

**Mantenedor**: Equipo APSYS
**Proyecto de referencia**: hashira.stone.backend (FastEndpoints 7.0.1)
