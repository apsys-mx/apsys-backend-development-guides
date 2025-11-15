# WebApi Layer - Clean Architecture

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-11-15

## 📋 Tabla de Contenidos
1. [¿Qué es la WebApi Layer?](#qué-es-la-webapi-layer)
2. [Responsabilidades](#responsabilidades)
3. [Stack Tecnológico](#stack-tecnológico)
4. [Arquitectura de Implementación](#arquitectura-de-implementación)
5. [Patrones Principales](#patrones-principales)
6. [Flujo de Datos](#flujo-de-datos)
7. [Guías Disponibles](#guías-disponibles)
8. [Configuración Inicial](#configuración-inicial)
9. [Ejemplos Rápidos](#ejemplos-rápidos)
10. [Mejores Prácticas](#mejores-prácticas)
11. [Referencias](#referencias)

---

## ¿Qué es la WebApi Layer?

La **WebApi Layer** es la **capa de presentación** en Clean Architecture. Es el punto de entrada HTTP de la aplicación y la única capa expuesta a los clientes externos (web, mobile, etc.).

### 🎯 Rol en Clean Architecture

```
┌─────────────────────────────────────────────┐
│          WebApi Layer (Presentación)        │  ← Endpoints HTTP
│  - FastEndpoints                            │
│  - Request/Response Models                  │
│  - DTOs                                     │
│  - AutoMapper Profiles                      │
│  - Error Handling                           │
└──────────────────┬──────────────────────────┘
                   │ Llama
┌──────────────────▼──────────────────────────┐
│        Application Layer (Use Cases)        │
│  - Commands/Handlers                        │
│  - Business Logic                           │
└──────────────────┬──────────────────────────┘
                   │ Usa
┌──────────────────▼──────────────────────────┐
│         Domain Layer (Entidades)            │
└─────────────────────────────────────────────┘
```

### ✅ Principio de Dependencia

**La WebApi Layer depende de Application Layer, pero Application NO depende de WebApi**.

```csharp
// ✅ CORRECTO - WebApi usa Application
using hashira.stone.backend.application.usecases.users;

public class CreateUserEndpoint : Endpoint<CreateUserModel.Request>
{
    public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
    {
        var command = new CreateUserUseCase.Command { ... };
        var result = await command.ExecuteAsync(ct);
    }
}

// ❌ INCORRECTO - Application no debe usar WebApi
// Application/UseCases/CreateUserUseCase.cs
using hashira.stone.backend.webapi.features.users.models; // ❌ NUNCA!
```

---

## Responsabilidades

La WebApi Layer tiene responsabilidades muy específicas y limitadas:

### ✅ Responsabilidades Permitidas

1. **Definir Endpoints HTTP**
   - Rutas (routes)
   - Verbos HTTP (GET, POST, PUT, DELETE)
   - Autenticación y autorización

2. **Request/Response Models**
   - Validación de entrada (FluentValidation)
   - Transformación de datos HTTP a Commands/Queries

3. **DTOs (Data Transfer Objects)**
   - Serialización/deserialización JSON
   - Estructura de respuestas al cliente

4. **Mapeo (AutoMapper)**
   - Models → Commands (entrada)
   - Entities → DTOs (salida)

5. **Manejo de Errores HTTP**
   - Traducción de errores de dominio a HTTP status codes
   - Respuestas de error estructuradas

6. **Documentación OpenAPI**
   - Swagger/OpenAPI configuration
   - Ejemplos de request/response

### ❌ Responsabilidades NO Permitidas

1. **Lógica de Negocio** → Application Layer
2. **Reglas de Dominio** → Domain Layer
3. **Acceso a Base de Datos** → Infrastructure Layer
4. **Validación de Entidades** → Domain Layer

---

## Stack Tecnológico

### 📦 Proyecto de Referencia: hashira.stone.backend

```xml
<ItemGroup>
  <!-- FastEndpoints - Framework de endpoints -->
  <PackageReference Include="FastEndpoints" Version="7.0.1" />
  <PackageReference Include="FastEndpoints.Swagger" Version="7.0.1" />

  <!-- AutoMapper - Mapeo de objetos -->
  <PackageReference Include="AutoMapper" Version="14.0.0" />

  <!-- FluentValidation - Validación de modelos -->
  <PackageReference Include="FluentValidation" Version="12.0.0" />

  <!-- FluentResults - Manejo de resultados -->
  <PackageReference Include="FluentResults" Version="4.0.0" />

  <!-- Authentication -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />

  <!-- Swagger/OpenAPI -->
  <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.3" />
</ItemGroup>
```

### 🔧 Framework: FastEndpoints

**¿Por qué FastEndpoints y no Minimal APIs o Controllers?**

| Característica | FastEndpoints | Minimal APIs | Controllers |
|----------------|---------------|--------------|-------------|
| **Performance** | ✅ Muy rápido | ✅ Muy rápido | ⚠️ Más lento |
| **Type Safety** | ✅ Fuerte | ⚠️ Débil | ✅ Fuerte |
| **Validación** | ✅ Integrada | ❌ Manual | ⚠️ Atributos |
| **Organización** | ✅ Por feature | ⚠️ Manual | ⚠️ Por controller |
| **Swagger** | ✅ Auto-generado | ⚠️ Manual | ✅ Auto-generado |
| **DI** | ✅ Constructor + Property | ✅ Constructor | ✅ Constructor |
| **Testing** | ✅ Fácil | ⚠️ Medio | ✅ Fácil |

**Conclusión**: FastEndpoints combina lo mejor de Minimal APIs (performance) con lo mejor de Controllers (estructura y validación).

---

## Arquitectura de Implementación

### 📁 Estructura de Carpetas

```
webapi/
├── Program.cs                          # Entry point y configuración
│
├── infrastructure/                      # Configuración de infraestructura
│   ├── DependencyInjection.cs          # Registro de servicios
│   ├── PolicyConfiguration.cs          # Políticas de autorización
│   ├── CorsConfiguration.cs            # CORS configuration
│   └── IdentityServerConfig.cs         # Auth0/JWT configuration
│
├── features/                            # Features organizados verticalmente
│   ├── BaseEndpoint.cs                 # Endpoint base con helpers
│   │
│   ├── users/                          # Feature: Users
│   │   ├── endpoint/                   # Endpoints
│   │   │   ├── CreateUserEndpoint.cs
│   │   │   ├── GetUserEndpoint.cs
│   │   │   ├── GetManyAndCountUsersEndpoint.cs
│   │   │   └── UpdateUserEndpoint.cs
│   │   ├── models/                     # Request/Response Models
│   │   │   ├── CreateUserModel.cs
│   │   │   ├── GetUserModel.cs
│   │   │   ├── GetManyAndCountModel.cs
│   │   │   └── UpdateUserModel.cs
│   │   └── validators/                 # Validadores (opcional)
│   │       └── GetUserRequestValidator.cs
│   │
│   └── technicalstandards/             # Feature: TechnicalStandards
│       ├── endpoint/
│       ├── models/
│       └── validators/
│
├── dtos/                                # Data Transfer Objects (compartidos)
│   ├── UserDto.cs
│   ├── TechnicalStandardDto.cs
│   ├── PrototypeDto.cs
│   └── GetManyAndCountResultDto.cs
│
└── mappingprofiles/                     # AutoMapper Profiles
    ├── UserMappingProfile.cs
    ├── TechnicalStandardMappingProfile.cs
    └── PrototypeMappingProfile.cs
```

### 🔑 Convenciones de Nomenclatura

| Concepto | Patrón | Ejemplo |
|----------|--------|---------|
| **Endpoint** | `{Action}{Entity}Endpoint` | `CreateUserEndpoint.cs` |
| **Request Model** | `{Action}{Entity}Model.Request` | `CreateUserModel.Request` |
| **Response Model** | `{Action}{Entity}Model.Response` | `CreateUserModel.Response` |
| **DTO** | `{Entity}Dto` | `UserDto.cs` |
| **Mapping Profile** | `{Entity}MappingProfile` | `UserMappingProfile.cs` |
| **Validator** | `{Model}Validator` | `CreateUserRequestValidator.cs` |

---

## Patrones Principales

### 1. 🎯 FastEndpoints Pattern

Cada endpoint es una clase independiente que hereda de `Endpoint<TRequest, TResponse>`:

```csharp
using FastEndpoints;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.users.models;
using hashira.stone.backend.application.usecases.users;

namespace hashira.stone.backend.webapi.features.users.endpoint;

public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    // 1. Configuración del endpoint
    public override void Configure()
    {
        Post("/users");  // Ruta y verbo HTTP

        // Documentación Swagger
        Description(b => b
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));

        DontThrowIfValidationFails();  // Manejo manual de errores
        Policies("MustBeApplicationAdministrator");  // Autorización
    }

    // 2. Manejo de la request
    public override async Task HandleAsync(
        CreateUserModel.Request request,
        CancellationToken ct)
    {
        // 2.1. Mapear Request → Command
        var command = _mapper.Map<CreateUserUseCase.Command>(request);

        // 2.2. Ejecutar Use Case
        var result = await command.ExecuteAsync(ct);

        // 2.3. Manejar resultado
        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();

            // Mapear errores de dominio a HTTP status codes
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is DuplicatedDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                return;
            }

            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is InvalidDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return;
            }

            AddError(error?.Message ?? "Unknown error");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            return;
        }

        // 2.4. Mapear Entity → Response DTO
        var userResponse = _mapper.Map<CreateUserModel.Response>(result.Value);

        // 2.5. Enviar respuesta 201 Created con Location header
        await Send.CreatedAtAsync(
            $"/users/{userResponse.User.Id}",
            new[] { userResponse.User.Id },
            userResponse,
            false,
            ct);
    }
}
```

**Ventajas**:
- ✅ Clase independiente por endpoint (SRP - Single Responsibility Principle)
- ✅ Type-safe request/response
- ✅ Validación integrada
- ✅ Swagger auto-generado
- ✅ Fácil de testear

---

### 2. 📋 Request/Response Models Pattern

Los Models representan la estructura de las HTTP requests y responses:

```csharp
using FastEndpoints;
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.users.models;

/// <summary>
/// Data model for creating a new Application user
/// </summary>
public class CreateUserModel
{
    /// <summary>
    /// Represents the request data used to create a new Application user
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the Name of the user.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Email of the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing the newly created user
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the newly created user.
        /// </summary>
        public UserDto User { get; set; } = new UserDto();
    }
}
```

**Patrón**: Nested classes `Request` y `Response` dentro del Model.

**Ventajas**:
- ✅ Agrupación lógica (Request + Response juntos)
- ✅ Namespace limpio
- ✅ Fácil navegación en IDE

---

### 3. 📦 DTOs (Data Transfer Objects)

Los DTOs son objetos simples para serialización JSON:

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The full name of the user
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

**Características**:
- ✅ Solo propiedades públicas
- ✅ Sin lógica de negocio
- ✅ Serializables a JSON
- ✅ Versionables (cambios no rompen contrato)

**DTO vs Model**:
```
Models (Request/Response):
- Específicos de cada endpoint
- Pueden incluir validación
- Mapean a Commands/Queries

DTOs:
- Compartidos entre endpoints
- Solo datos
- Mapean desde Entities
```

---

### 4. 🗺️ AutoMapper Profiles

Los Profiles definen mapeos entre objetos:

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for User entity and UserDto.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO
        CreateMap<User, UserDto>();

        // Entity → Response Model
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        CreateMap<User, GetUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        // Request Model → Command
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
        CreateMap<GetUserModel.Request, GetUserUseCase.Command>();
    }
}
```

**Convenciones**:
- Un Profile por entidad
- Mapeos bidireccionales cuando sea necesario
- Mapeos explícitos para propiedades complejas

---

### 5. 🛡️ BaseEndpoint Pattern

Clase base con helpers comunes:

```csharp
using FastEndpoints;
using FluentResults;
using System.Linq.Expressions;
using System.Net;

namespace hashira.stone.backend.webapi.features;

/// <summary>
/// Base endpoint with helpers for error handling.
/// </summary>
public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    /// <summary>
    /// Helper for property-based error handling.
    /// </summary>
    protected async Task HandleErrorAsync(
        Expression<Func<TRequest, object?>> property,
        string message,
        HttpStatusCode status,
        CancellationToken ct)
    {
        this.Logger.LogWarning(message);
        AddError(property, message);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }

    /// <summary>
    /// Helper for unexpected error handling.
    /// </summary>
    protected async Task HandleUnexpectedErrorAsync(
        IError? error,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        if (error != null && error.Metadata != null &&
            error.Metadata.TryGetValue("Exception", out var exObj))
        {
            if (exObj is Exception ex)
                this.Logger.LogError(ex, UnexpectedErrorMessage);
            else
                this.Logger.LogError(UnexpectedErrorMessage);
        }
        else
            this.Logger.LogError(UnexpectedErrorMessage);

        AddError(UnexpectedErrorMessage);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }
}
```

**Uso**:
```csharp
public class MyEndpoint : BaseEndpoint<MyRequest, MyResponse>
{
    public override async Task HandleAsync(MyRequest req, CancellationToken ct)
    {
        if (someCondition)
        {
            await HandleErrorAsync(
                r => r.PropertyName,
                "Invalid value",
                HttpStatusCode.BadRequest,
                ct);
            return;
        }

        // ... lógica normal
    }
}
```

---

### 6. 📄 GetManyAndCount Pattern (Paginación)

Patrón estándar para endpoints de listado con paginación:

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.features.users.endpoint;

/// <summary>
/// Endpoint for retrieving many users with count
/// </summary>
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
            .Produces<GetManyAndCountResultDto<UserDto>>(200, "application/json"));
        DontThrowIfValidationFails();
        Policies("MustBeApplicationUser");
    }

    override public async Task HandleAsync(
        GetManyAndCountModel.Request req,
        CancellationToken ct)
    {
        try
        {
            // Pasar toda la query string al Use Case
            var request = new GetManyAndCountUsersUseCase.Command
            {
                Query = HttpContext.Request.QueryString.Value
            };

            var getManyAndCountResult = await request.ExecuteAsync(ct);

            // Mapear resultado a DTO
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

**GetManyAndCountResultDto**:
```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data transfer object for GetManyAndCountResult<T> class
/// </summary>
public class GetManyAndCountResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public long Count { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortBy { get; set; } = string.Empty;
    public string SortCriteria { get; set; } = string.Empty;
}
```

**Query String Example**:
```
GET /users?pageNumber=1&pageSize=25&sortBy=Email&sortDirection=asc&IsActive=true||eq
```

---

## Flujo de Datos

### 📊 Request → Response Flow

```
1. Cliente HTTP
   ↓ POST /users { "name": "John", "email": "john@example.com" }

2. FastEndpoints Routing
   ↓ Encuentra CreateUserEndpoint

3. Model Binding
   ↓ CreateUserModel.Request { Name = "John", Email = "john@example.com" }

4. Validation (FluentValidation)
   ↓ Valida Request

5. Endpoint.HandleAsync()
   ↓ Ejecuta lógica del endpoint

6. AutoMapper: Request → Command
   ↓ CreateUserUseCase.Command { Name = "John", Email = "john@example.com" }

7. Application Layer (Use Case)
   ↓ await command.ExecuteAsync(ct)

8. Domain Layer (Entity creada)
   ↓ User { Id = Guid, Name = "John", Email = "john@example.com" }

9. AutoMapper: Entity → DTO
   ↓ UserDto { Id = Guid, Name = "John", Email = "john@example.com" }

10. Response Model
    ↓ CreateUserModel.Response { User = UserDto }

11. FastEndpoints Serialization
    ↓ JSON

12. Cliente HTTP
    ← 201 Created + { "user": { "id": "...", "name": "John", ... } }
```

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de WebApi Layer |
| [fastendpoints-basics.md](./fastendpoints-basics.md) | ⏳ Pendiente | Estructura de endpoints |
| [request-response-models.md](./request-response-models.md) | ⏳ Pendiente | Patrones de Models |
| [dtos.md](./dtos.md) | ⏳ Pendiente | DTOs vs Models |
| [automapper-profiles.md](./automapper-profiles.md) | ⏳ Pendiente | Configuración de mapeos |
| [error-responses.md](./error-responses.md) | ⏳ Pendiente | Status codes, ProblemDetails |
| [authentication.md](./authentication.md) | ⏳ Pendiente | JWT, Auth0, políticas |
| [swagger-configuration.md](./swagger-configuration.md) | ⏳ Pendiente | Swagger/OpenAPI |

---

## Configuración Inicial

### 1. Instalar Paquetes NuGet

```bash
dotnet add package FastEndpoints
dotnet add package FastEndpoints.Swagger
dotnet add package AutoMapper
dotnet add package FluentValidation
dotnet add package FluentResults
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2. Configurar Program.cs

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services
    .AddEndpointsApiExplorer()
    .ConfigureAutoMapper()              // Extension method personalizado
    .ConfigureUnitOfWork(configuration) // Infrastructure Layer
    .AddAuthorization()
    .AddFastEndpoints()                 // ← FastEndpoints
    .SwaggerDocument();                 // ← Swagger

var app = builder.Build();

// Configurar pipeline HTTP
app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints()                 // ← FastEndpoints middleware
    .UseSwagger()
    .UseSwaggerUI(opt =>
    {
        opt.DefaultModelsExpandDepth(-1);
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

await app.RunAsync();
```

### 3. Registrar AutoMapper

```csharp
// infrastructure/DependencyInjection.cs
public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
{
    services.AddAutoMapper(typeof(Program).Assembly);
    return services;
}
```

---

## Ejemplos Rápidos

### 📋 Ejemplo 1: Endpoint Simple (POST)

```csharp
// 1. Definir Model
public class CreateUserModel
{
    public class Request
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Response
    {
        public UserDto User { get; set; } = new UserDto();
    }
}

// 2. Definir Endpoint
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public override void Configure()
    {
        Post("/users");
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
    {
        var command = mapper.Map<CreateUserUseCase.Command>(request);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            AddError(result.Errors.First().Message);
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        var response = mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAtAsync($"/users/{response.User.Id}", response, ct);
    }
}
```

### 📋 Ejemplo 2: Endpoint GET con ID

```csharp
// 1. Model
public class GetUserModel
{
    public class Request
    {
        public Guid Id { get; set; }
    }

    public class Response
    {
        public UserDto User { get; set; } = new UserDto();
    }
}

// 2. Endpoint
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override void Configure()
    {
        Get("/users/{id}");  // Route parameter
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetUserModel.Request request, CancellationToken ct)
    {
        var command = new GetUserUseCase.Command { Id = request.Id };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var response = mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

### 📋 Ejemplo 3: Endpoint GET Many (Paginación)

```csharp
public class GetManyAndCountUsersEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<GetManyAndCountModel.Request, GetManyAndCountResultDto<UserDto>>
{
    public override void Configure()
    {
        Get("/users");
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        var command = new GetManyAndCountUsersUseCase.Command
        {
            Query = HttpContext.Request.QueryString.Value
        };

        var result = await command.ExecuteAsync(ct);
        var response = mapper.Map<GetManyAndCountResultDto<UserDto>>(result);

        await Send.OkAsync(response, ct);
    }
}
```

---

## Mejores Prácticas

### ✅ 1. Un Endpoint por Clase

```csharp
// ✅ CORRECTO - Clase dedicada
public class CreateUserEndpoint : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    // ...
}

// ❌ INCORRECTO - Múltiples acciones en una clase (estilo Controller)
public class UsersController : Controller
{
    public IActionResult Create() { }
    public IActionResult Get() { }
    public IActionResult Update() { }
    public IActionResult Delete() { }
}
```

**Por qué**: Single Responsibility Principle, fácil testing, mejor organización.

---

### ✅ 2. Mapear Request → Command, Entity → DTO

```csharp
// ✅ CORRECTO
public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
{
    // Request → Command
    var command = _mapper.Map<CreateUserUseCase.Command>(request);
    var result = await command.ExecuteAsync(ct);

    // Entity → DTO
    var response = _mapper.Map<CreateUserModel.Response>(result.Value);
    await Send.OkAsync(response, ct);
}

// ❌ INCORRECTO - Mapeo manual
public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
{
    var command = new CreateUserUseCase.Command
    {
        Name = request.Name,        // ❌ Manual
        Email = request.Email,      // ❌ Manual
        // ... 20 propiedades más
    };
}
```

**Por qué**: AutoMapper reduce boilerplate, mantiene consistencia, facilita refactoring.

---

### ✅ 3. Validación de Entrada en WebApi

```csharp
// ✅ CORRECTO - Validador en WebApi
public class CreateUserRequestValidator : Validator<CreateUserModel.Request>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);
    }
}

// ❌ INCORRECTO - Validación de entrada en Domain
// Domain debe validar reglas de negocio, no formato de entrada HTTP
```

**Por qué**: Separación de responsabilidades. WebApi valida formato, Domain valida reglas de negocio.

---

### ✅ 4. DTOs para Respuestas, No Entidades

```csharp
// ✅ CORRECTO - Devolver DTO
public class Response
{
    public UserDto User { get; set; }
}

// ❌ INCORRECTO - Devolver entidad directamente
public class Response
{
    public User User { get; set; }  // ❌ Entidad de dominio expuesta
}
```

**Por qué**:
- Evita exponer detalles internos del dominio
- Permite versionado de API sin cambiar dominio
- Evita lazy loading exceptions (NHibernate)

---

### ✅ 5. Manejo de Errores Consistente

```csharp
// ✅ CORRECTO - Mapear excepciones de dominio a HTTP status
if (result.IsFailed)
{
    var error = result.Errors.FirstOrDefault();

    if (error?.Reasons.OfType<ExceptionalError>()
        .Any(r => r.Exception is DuplicatedDomainException) == true)
    {
        await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
        return;
    }

    if (error?.Reasons.OfType<ExceptionalError>()
        .Any(r => r.Exception is InvalidDomainException) == true)
    {
        await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
        return;
    }

    await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
}
```

**Mapeo estándar**:
| Excepción de Dominio | HTTP Status Code |
|---------------------|------------------|
| `InvalidDomainException` | 400 Bad Request |
| `DuplicatedDomainException` | 409 Conflict |
| `NotFoundException` | 404 Not Found |
| `UnauthorizedException` | 401 Unauthorized |
| `ForbiddenException` | 403 Forbidden |
| Exception genérica | 500 Internal Server Error |

---

### ✅ 6. Usar Dependency Injection (Constructor o Property)

```csharp
// ✅ CORRECTO - Constructor injection
public class CreateUserEndpoint(AutoMapper.IMapper mapper, ILogger<CreateUserEndpoint> logger)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;
    private readonly ILogger _logger = logger;
}

// ✅ TAMBIÉN CORRECTO - Property injection (FastEndpoints style)
public class CreateUserEndpoint : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public AutoMapper.IMapper Mapper { get; set; }
    public ILogger<CreateUserEndpoint> Logger { get; set; }
}
```

---

### ✅ 7. Documentar con Swagger

```csharp
public override void Configure()
{
    Post("/users");

    Description(b => b
        .WithTags("Users")
        .WithName("CreateUser")
        .WithDescription("Creates a new user in the system")
        .Produces<UserDto>(StatusCodes.Status201Created)
        .ProducesProblemDetails(StatusCodes.Status400BadRequest)
        .ProducesProblemDetails(StatusCodes.Status409Conflict));

    Summary(s => {
        s.Summary = "Create a new user";
        s.Description = "Creates a new user with the provided name and email";
        s.ExampleRequest = new CreateUserModel.Request
        {
            Name = "John Doe",
            Email = "john@example.com"
        };
    });
}
```

---

## Referencias

### 📚 FastEndpoints

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [FastEndpoints GitHub](https://github.com/FastEndpoints/FastEndpoints)
- [FastEndpoints Examples](https://github.com/FastEndpoints/Documentation/tree/main/examples)

### 📚 AutoMapper

- [AutoMapper Documentation](https://docs.automapper.org/)
- [AutoMapper Getting Started](https://docs.automapper.org/en/stable/Getting-started.html)

### 📚 FluentValidation

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [FluentValidation with ASP.NET Core](https://docs.fluentvalidation.net/en/latest/aspnet.html)

### 🔗 Guías Relacionadas

- [Best Practices](../best-practices/README.md) - Prácticas generales de desarrollo
- [Application Layer](../application-layer/README.md) - Use Cases y Commands
- [Domain Layer](../domain-layer/README.md) - Entidades y reglas de negocio

---

## 🔄 Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2025-11-15 | Versión inicial de WebApi Layer README |

---

**Siguiente**: [FastEndpoints Basics](./fastendpoints-basics.md) - Estructura detallada de endpoints →

**Mantenedor**: Equipo APSYS
**Proyecto de referencia**: hashira.stone.backend (FastEndpoints 7.0.1)
