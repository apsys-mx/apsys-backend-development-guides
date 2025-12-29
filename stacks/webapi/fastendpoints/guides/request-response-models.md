# Request/Response Models

**Version:** 1.0.0
**Last Updated:** 2025-01-15
**Status:** ✅ Complete

---

## Table of Contents

1. [Introducción](#introducción)
2. [¿Qué son los Request/Response Models?](#qué-son-los-requestresponse-models)
3. [Arquitectura del Patrón](#arquitectura-del-patrón)
4. [Convenciones de Nomenclatura](#convenciones-de-nomenclatura)
5. [Estructura de Clases Anidadas](#estructura-de-clases-anidadas)
6. [Tipos de Datos Comunes](#tipos-de-datos-comunes)
7. [Documentación XML](#documentación-xml)
8. [Integración con FastEndpoints](#integración-con-fastendpoints)
9. [Integración con Validación](#integración-con-validación)
10. [Mapeo a Commands y Queries](#mapeo-a-commands-y-queries)
11. [Ejemplos Completos](#ejemplos-completos)
12. [Best Practices](#best-practices)
13. [Errores Comunes](#errores-comunes)
14. [Referencias](#referencias)

---

## Introducción

Los **Request/Response Models** son Data Transfer Objects (DTOs) específicos de la capa WebApi que definen la estructura de datos que entra y sale de los endpoints. En nuestra arquitectura Clean Architecture con FastEndpoints, estos modelos actúan como contratos de API explícitos y type-safe.

### ¿Por qué usar Request/Response Models?

1. **Separación de responsabilidades**: Los modelos de API no dependen de la lógica de negocio
2. **Contratos de API claros**: Documentación automática en Swagger
3. **Type Safety**: Validación en tiempo de compilación
4. **Versionado de API**: Fácil evolución de la API sin afectar la lógica de negocio
5. **Mapeo explícito**: Transformación controlada entre API y Application Layer

---

## ¿Qué son los Request/Response Models?

Los Request/Response Models son clases que encapsulan los datos de entrada (Request) y salida (Response) de un endpoint específico.

### Ubicación en la Arquitectura

```
src/
└── hashira.stone.backend.webapi/
    └── features/
        └── {feature}/
            └── models/
                ├── CreateUserModel.cs
                ├── GetUserModel.cs
                ├── UpdateUserModel.cs
                └── GetManyAndCountModel.cs
```

### Responsabilidades

- **Request Model**:
  - Definir la estructura de datos que el cliente envía
  - Recibir datos del HTTP request (body, route, query parameters)
  - Servir como entrada para la validación
  - Mapearse a Commands/Queries de Application Layer

- **Response Model**:
  - Definir la estructura de datos que el servidor devuelve
  - Encapsular DTOs de respuesta
  - Servir como contrato de salida para Swagger

---

## Arquitectura del Patrón

### Flujo de Datos

```
┌──────────────┐
│   Cliente    │
│   (HTTP)     │
└──────┬───────┘
       │ JSON
       ▼
┌──────────────────────────────┐
│   Request Model              │
│   (FastEndpoints Binding)    │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│   FluentValidation           │
│   Validator<Request>         │
└──────┬───────────────────────┘
       │ Valid
       ▼
┌──────────────────────────────┐
│   AutoMapper                 │
│   Request → Command/Query    │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│   Application Layer          │
│   UseCase.ExecuteAsync()     │
└──────┬───────────────────────┘
       │ Result<T>
       ▼
┌──────────────────────────────┐
│   AutoMapper                 │
│   Entity/DTO → Response      │
└──────┬───────────────────────┘
       │
       ▼
┌──────────────────────────────┐
│   Response Model             │
│   (FastEndpoints Serializes) │
└──────┬───────────────────────┘
       │ JSON
       ▼
┌──────────────┐
│   Cliente    │
│   (HTTP)     │
└──────────────┘
```

---

## Convenciones de Nomenclatura

### Patrón de Naming

```
{Verbo}{Entidad}Model
```

### Verbos Estándar

| Verbo | Operación | HTTP Method | Ejemplo |
|-------|-----------|-------------|---------|
| `Create` | Crear un nuevo recurso | POST | `CreateUserModel` |
| `Get` | Obtener un recurso específico | GET | `GetUserModel` |
| `GetManyAndCount` | Obtener lista paginada | GET | `GetManyAndCountUsersModel` |
| `Update` | Actualizar recurso completo | PUT | `UpdateUserModel` |
| `Patch` | Actualizar recurso parcial | PATCH | `PatchUserEmailModel` |
| `Delete` | Eliminar un recurso | DELETE | `DeleteUserModel` |
| Acción específica | Operación de negocio | POST/PUT | `UpdateUserLockModel` |

### Ejemplos del Proyecto

```
✅ CreateUserModel
✅ GetUserModel
✅ GetManyAndCountModel
✅ UpdateUserLockModel
✅ CreateTechnicalStandardModel
✅ UpdatePrototypeModel
```

### Naming para Request y Response

```csharp
public class CreateUserModel
{
    public class Request { }
    public class Response { }
}
```

- **Siempre usar** `Request` y `Response` como nombres de clases anidadas
- **No usar** variaciones como `Req`, `Res`, `Input`, `Output`

---

## Estructura de Clases Anidadas

### Patrón Base

```csharp
namespace hashira.stone.backend.webapi.features.{feature}.models;

/// <summary>
/// Data model for {operation} {entity}
/// </summary>
public class {Verb}{Entity}Model
{
    /// <summary>
    /// Represents the request data used to {operation} {entity}
    /// </summary>
    public class Request
    {
        // Propiedades de entrada
    }

    /// <summary>
    /// Represents the response data returned after {operation} {entity}
    /// </summary>
    public class Response
    {
        // Propiedades de salida
    }
}
```

### ¿Por qué Clases Anidadas?

1. **Organización lógica**: Request y Response están naturalmente agrupados
2. **Namespace limpio**: No contamina el namespace con múltiples clases
3. **Discoverability**: Fácil encontrar Request y Response relacionados
4. **FastEndpoints convention**: Patrón recomendado por la librería

### Ejemplo Completo

```csharp
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
    /// Represents the response data returned after creating a new user.
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

---

## Tipos de Datos Comunes

### Primitivos

```csharp
public class Request
{
    // Strings - siempre inicializar con string.Empty
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Números
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long Count { get; set; }
    public decimal Amount { get; set; }

    // Booleanos
    public bool IsActive { get; set; }
    public bool Lock { get; set; }

    // Fechas
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Identificadores
    public Guid Id { get; set; }
}
```

### Tipos Nullable

```csharp
public class Request
{
    // Query parameters opcionales
    public string? Query { get; set; }
    public string? SortBy { get; set; }
    public int? PageNumber { get; set; }

    // Fechas opcionales
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // IDs opcionales
    public Guid? ParentId { get; set; }
}
```

### DTOs Personalizados

```csharp
public class Response
{
    // DTO único
    public UserDto User { get; set; } = new UserDto();
    public TechnicalStandardDto TechnicalStandard { get; set; } = new();

    // Colección de DTOs
    public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();
}
```

### DTOs Genéricos

```csharp
// Herencia de DTO base genérico
public class Response : GetManyAndCountResultDto<UserDto>
{
    // Hereda:
    // - IEnumerable<UserDto> Items
    // - long Count
    // - int PageNumber
    // - int PageSize
    // - string SortBy
    // - string SortCriteria
}
```

### Enumeraciones

```csharp
public class Request
{
    // Como string (más flexible para API)
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    // Como enum (type-safe, menos flexible)
    public UserStatus Status { get; set; }
    public TechnicalStandardType Type { get; set; }
}
```

**💡 Recomendación**: Preferir `string` en Request/Response models para mayor flexibilidad de API. Convertir a `enum` en Application Layer si es necesario.

---

## Documentación XML

### Estructura Completa

```csharp
/// <summary>
/// Data model for {operation description}
/// </summary>
public class CreateUserModel
{
    /// <summary>
    /// Represents the request data used to {operation}
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the {property description}.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response data returned after {operation}
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the {property description}.
        /// </summary>
        public UserDto User { get; set; } = new UserDto();
    }
}
```

### Templates de Documentación

#### Para Clase Principal

```csharp
/// <summary>
/// Data model for creating a new {entity}
/// </summary>

/// <summary>
/// Data model for retrieving a {entity} by {identifier}
/// </summary>

/// <summary>
/// Data model for updating an existing {entity}
/// </summary>

/// <summary>
/// Model for {specific operation description}
/// </summary>
```

#### Para Request

```csharp
/// <summary>
/// Represents the request data used to create a new {entity}
/// </summary>

/// <summary>
/// Represents the request data used to get {entity} by {identifier}
/// </summary>

/// <summary>
/// Represents the request data used to update an existing {entity}
/// </summary>

/// <summary>
/// Request model for {specific operation}
/// </summary>
```

#### Para Response

```csharp
/// <summary>
/// Represents the response data returned after creating a new {entity}
/// </summary>

/// <summary>
/// Response containing the requested {entity}
/// </summary>

/// <summary>
/// Represents a paginated list of {entities} along with the total count.
/// </summary>
```

#### Para Propiedades

```csharp
/// <summary>
/// Gets or sets the {property description}.
/// </summary>

/// <summary>
/// Gets or sets the unique identifier of the {entity} to {operation}.
/// This value is extracted from the route parameter.
/// </summary>

/// <summary>
/// Gets or sets the {property description}.
/// Optional. Defaults to {default value}.
/// </summary>
```

### Ejemplo Completo con Documentación Avanzada

```csharp
/// <summary>
/// Data model for updating the lock state of a user
/// </summary>
public class UpdateUserLockModel
{
    /// <summary>
    /// Request model for updating the lock state of a user
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the username (email) of the user to lock/unlock.
        /// This value is extracted from the route parameter.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to lock (true) or unlock (false) the user.
        /// When true, the user will be prevented from accessing the system.
        /// </summary>
        public bool Lock { get; set; }
    }

    /// <summary>
    /// Response model for changing the lock state of a user
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
```

---

## Integración con FastEndpoints

### Binding Automático

FastEndpoints realiza binding automático basado en:

1. **Route parameters**: Propiedades que coinciden con `{NombreParametro}` en la ruta
2. **Query parameters**: Propiedades con tipos simples en GET requests
3. **Body**: Todo el objeto Request en POST/PUT/PATCH

### Ejemplo de Binding

```csharp
// Model
public class GetUserModel
{
    public class Request
    {
        public string UserName { get; set; } = string.Empty;
    }
}

// Endpoint
public override void Configure()
{
    Get("/users/{UserName}");  // ← UserName se mapea automáticamente
}

// HTTP Request
GET /users/john.doe@example.com
// Request.UserName = "john.doe@example.com"
```

### Binding de Body

```csharp
// Model
public class CreateUserModel
{
    public class Request
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

// HTTP Request
POST /users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john.doe@example.com"
}

// Request.Name = "John Doe"
// Request.Email = "john.doe@example.com"
```

### Binding de Query Parameters

```csharp
// Model
public class GetManyAndCountModel
{
    public class Request
    {
        public string? Query { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
    }
}

// HTTP Request
GET /users?query=john&pageNumber=2&pageSize=20&sortBy=email

// Request.Query = "john"
// Request.PageNumber = 2
// Request.PageSize = 20
// Request.SortBy = "email"
```

### Binding Mixto (Route + Body)

```csharp
// Model
public class UpdateUserLockModel
{
    public class Request
    {
        public string UserName { get; set; } = string.Empty;  // ← Route
        public bool Lock { get; set; }                        // ← Body
    }
}

// Endpoint
public override void Configure()
{
    Put("/users/{UserName}/lock");
}

// HTTP Request
PUT /users/john.doe@example.com/lock
Content-Type: application/json

{
  "lock": true
}

// Request.UserName = "john.doe@example.com" (route)
// Request.Lock = true (body)
```

### Atributos Opcionales (No Recomendado)

```csharp
using Microsoft.AspNetCore.Mvc;

public class Request
{
    [FromRoute]
    public string UserName { get; set; } = string.Empty;

    [FromBody]
    public bool Lock { get; set; }
}
```

**⚠️ Nota**: FastEndpoints NO requiere `[FromRoute]` ni `[FromBody]`. El binding es automático basado en convención. Solo usar estos atributos si necesitas override del comportamiento por defecto.

---

## Integración con Validación

### Validador de Request

```csharp
using FastEndpoints;
using FluentValidation;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.features.users.validators;

/// <summary>
/// Validator for <see cref="GetUserModel.Request"/>
/// </summary>
public class GetUserRequestValidator : Validator<GetUserModel.Request>
{
    public GetUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotNull().NotEmpty()
            .WithMessage("The [Email] cannot be null or empty")
            .WithErrorCode("Email_Empty")
            .EmailAddress()
            .WithMessage("The [Email] is not a valid email address")
            .WithErrorCode("Email_Invalid");
    }
}
```

### Registro Automático

FastEndpoints automáticamente descubre y registra validadores que heredan de `Validator<T>`.

### Validación en el Endpoint

```csharp
public class GetUserEndpoint : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override void Configure()
    {
        Get("/users/{UserName}");
        // Validación automática antes de HandleAsync()
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        // Si llegamos aquí, req ya fue validado
        var command = new GetUserUseCase.Command { UserName = req.UserName };
        // ...
    }
}
```

### Convención de Ubicación

```
features/
└── users/
    ├── models/
    │   └── GetUserModel.cs
    └── validators/
        └── GetUserRequestValidator.cs
```

### Ejemplo de Validador Completo

```csharp
public class CreateUserRequestValidator : Validator<CreateUserModel.Request>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .WithErrorCode("Name_Required")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters")
            .WithErrorCode("Name_TooLong");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .WithErrorCode("Email_Required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .WithErrorCode("Email_Invalid")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters")
            .WithErrorCode("Email_TooLong");
    }
}
```

---

## Mapeo a Commands y Queries

### AutoMapper Profile

```csharp
using AutoMapper;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Request → Command/Query
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
        CreateMap<GetUserModel.Request, GetUserUseCase.Command>();

        // Entity/Result → Response
        CreateMap<User, UserDto>();
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<User, GetUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
    }
}
```

### Uso en Endpoint

```csharp
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override async Task HandleAsync(
        CreateUserModel.Request request,
        CancellationToken ct)
    {
        // Request → Command
        var command = _mapper.Map<CreateUserUseCase.Command>(request);

        // Ejecutar Use Case
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await HandleErrorAsync(result.Errors[0], ct);
            return;
        }

        // Entity → Response
        var response = _mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAsync(response, ct);
    }
}
```

### Mapeo con Lógica Personalizada

```csharp
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UpdatePrototypeModel.Request, UpdatePrototypeUseCase.Command>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Number.Trim()))
            .ForMember(dest => dest.IssueDate, opt => opt.MapFrom(src => src.IssueDate.ToUniversalTime()))
            .ForMember(dest => dest.ExpirationDate, opt => opt.MapFrom(src => src.ExpirationDate.ToUniversalTime()));
    }
}
```

---

## Ejemplos Completos

### 1. Create Model (POST)

```csharp
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
    /// Represents the response data returned after creating a new user.
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

**Uso en Endpoint**:

```csharp
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Post("/users");
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(
        CreateUserModel.Request request,
        CancellationToken ct)
    {
        var command = _mapper.Map<CreateUserUseCase.Command>(request);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await HandleErrorAsync(result.Errors[0], ct);
            return;
        }

        var response = _mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAsync(response, ct);
    }
}
```

---

### 2. Get by ID Model (GET)

```csharp
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.users.models;

/// <summary>
/// Data model for retrieving a User by username
/// </summary>
public class GetUserModel
{
    /// <summary>
    /// Represents the request data used to get a user by username
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the username (email) of the user to retrieve.
        /// This value is extracted from the route parameter.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing the requested user
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the retrieved user.
        /// </summary>
        public UserDto User { get; set; } = new UserDto();
    }
}
```

**Uso en Endpoint**:

```csharp
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users/{UserName}");  // ← UserName mapea automáticamente
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

---

### 3. GetManyAndCount Model (GET con Paginación)

```csharp
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.users.models;

/// <summary>
/// Data model for retrieving many users with count
/// </summary>
public class GetManyAndCountModel
{
    /// <summary>
    /// Represents the request data used to get many users with count
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the search query to filter users.
        /// Optional. If not provided, all users are returned.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Gets or sets the page number (1-based).
        /// Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size.
        /// Defaults to 10. Maximum 100.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the field to sort by.
        /// Optional. Defaults to "Name".
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// Valid values: "asc", "desc". Defaults to "asc".
        /// </summary>
        public string SortCriteria { get; set; } = "asc";
    }

    /// <summary>
    /// Represents a paginated list of users along with the total count.
    /// </summary>
    public class Response : GetManyAndCountResultDto<UserDto>
    {
        // Hereda:
        // - IEnumerable<UserDto> Items
        // - long Count
        // - int PageNumber
        // - int PageSize
        // - string SortBy
        // - string SortCriteria
    }
}
```

**Uso en Endpoint**:

```csharp
public class GetManyAndCountUsersEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetManyAndCountModel.Request, GetManyAndCountModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users");
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        var command = _mapper.Map<GetManyAndCountUsersUseCase.Command>(req);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await HandleUnexpectedErrorAsync(result.Errors[0], ct);
            return;
        }

        var response = _mapper.Map<GetManyAndCountModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

---

### 4. Update Model (PUT)

```csharp
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.prototypes.models;

/// <summary>
/// Model for updating an existing prototype.
/// </summary>
public class UpdatePrototypeModel
{
    /// <summary>
    /// Represents the request data used to update an existing prototype.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the unique identifier of the prototype to update.
        /// This value is extracted from the route parameter.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the prototype number as a string.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the prototype was created or recorded.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the prototype.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the prototype entity.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response data returned after updating a prototype.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the data of the updated prototype.
        /// </summary>
        public PrototypeDto Prototype { get; set; } = new PrototypeDto();
    }
}
```

**Uso en Endpoint**:

```csharp
public class UpdatePrototypeEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<UpdatePrototypeModel.Request, UpdatePrototypeModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Put("/prototypes/{Id}");
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(
        UpdatePrototypeModel.Request req,
        CancellationToken ct)
    {
        var command = _mapper.Map<UpdatePrototypeUseCase.Command>(req);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await HandleErrorAsync(result.Errors[0], ct);
            return;
        }

        var response = _mapper.Map<UpdatePrototypeModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

---

### 5. Patch Model (PATCH - Actualización Parcial)

```csharp
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.users.models;

/// <summary>
/// Data model for updating the lock state of a user
/// </summary>
public class UpdateUserLockModel
{
    /// <summary>
    /// Request model for updating the lock state of a user
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the username (email) of the user to lock/unlock.
        /// This value is extracted from the route parameter.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to lock (true) or unlock (false) the user.
        /// </summary>
        public bool Lock { get; set; }
    }

    /// <summary>
    /// Response model for changing the lock state of a user
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
```

**Uso en Endpoint**:

```csharp
public class UpdateUserLockEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<UpdateUserLockModel.Request, UpdateUserLockModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Put("/users/{UserName}/lock");
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(
        UpdateUserLockModel.Request req,
        CancellationToken ct)
    {
        var command = _mapper.Map<UpdateUserLockUseCase.Command>(req);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            await HandleErrorAsync(result.Errors[0], ct);
            return;
        }

        var response = new UpdateUserLockModel.Response { Success = result.IsSuccess };
        await Send.OkAsync(response, ct);
    }
}
```

---

### 6. Delete Model (DELETE)

```csharp
namespace hashira.stone.backend.webapi.features.users.models;

/// <summary>
/// Data model for deleting a user
/// </summary>
public class DeleteUserModel
{
    /// <summary>
    /// Represents the request data used to delete a user
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the username (email) of the user to delete.
        /// This value is extracted from the route parameter.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response indicating the result of the delete operation
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets a value indicating whether the deletion was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
```

---

## Best Practices

### ✅ DO

1. **Usar clases anidadas para Request y Response**
   ```csharp
   ✅ public class CreateUserModel
   {
       public class Request { }
       public class Response { }
   }
   ```

2. **Inicializar strings con string.Empty**
   ```csharp
   ✅ public string Name { get; set; } = string.Empty;
   ```

3. **Inicializar objetos complejos con new()**
   ```csharp
   ✅ public UserDto User { get; set; } = new UserDto();
   ✅ public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();
   ```

4. **Usar nullable para propiedades opcionales**
   ```csharp
   ✅ public string? Query { get; set; }
   ✅ public int? PageNumber { get; set; }
   ```

5. **Documentar cada clase y propiedad con XML comments**
   ```csharp
   ✅ /// <summary>
   /// Gets or sets the Name of the user.
   /// </summary>
   public string Name { get; set; } = string.Empty;
   ```

6. **Usar DTOs en Response, no Entities**
   ```csharp
   ✅ public UserDto User { get; set; }
   ```

7. **Seguir convención de naming {Verb}{Entity}Model**
   ```csharp
   ✅ CreateUserModel
   ✅ GetUserModel
   ✅ UpdatePrototypeModel
   ```

8. **Ubicar validadores en carpeta validators/**
   ```
   ✅ features/users/models/GetUserModel.cs
   ✅ features/users/validators/GetUserRequestValidator.cs
   ```

9. **Usar herencia para DTOs genéricos**
   ```csharp
   ✅ public class Response : GetManyAndCountResultDto<UserDto> { }
   ```

10. **Crear AutoMapper profiles para mapeo explícito**
    ```csharp
    ✅ CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
    ```

---

### ❌ DON'T

1. **No usar clases separadas para Request y Response**
   ```csharp
   ❌ public class CreateUserRequest { }
   ❌ public class CreateUserResponse { }
   ```

2. **No dejar strings sin inicializar**
   ```csharp
   ❌ public string Name { get; set; }  // null por defecto
   ```

3. **No usar null! para inicialización**
   ```csharp
   ❌ public string Name { get; set; } = null!;
   ```

4. **No devolver entities directamente en Response**
   ```csharp
   ❌ public User User { get; set; }  // Usar UserDto
   ```

5. **No usar atributos [FromRoute], [FromBody] innecesariamente**
   ```csharp
   ❌ [FromRoute]
   public string UserName { get; set; }  // FastEndpoints lo hace automáticamente
   ```

6. **No mezclar lógica de negocio en Models**
   ```csharp
   ❌ public class Request
   {
       public string Email { get; set; }

       public bool IsValidEmail()  // ❌ Esto va en validator
       {
           return Email.Contains("@");
       }
   }
   ```

7. **No usar nombres genéricos**
   ```csharp
   ❌ public class UserRequest { }
   ❌ public class UserResponse { }
   ❌ public class UserModel { }
   ```

8. **No omitir documentación XML**
   ```csharp
   ❌ public class CreateUserModel  // Sin /// <summary>
   {
       public class Request { }
   }
   ```

9. **No abusar de propiedades anidadas**
   ```csharp
   ❌ public class Request
   {
       public Address Address { get; set; }  // Mejor: aplanar propiedades
   }

   ✅ public class Request
   {
       public string Street { get; set; }
       public string City { get; set; }
       public string ZipCode { get; set; }
   }
   ```

10. **No usar enums directamente en API**
    ```csharp
    ❌ public UserStatus Status { get; set; }  // Mejor: string

    ✅ public string Status { get; set; }  // Convertir a enum en Application Layer
    ```

---

## Errores Comunes

### Error 1: Request sin Response

```csharp
❌ public class CreateUserModel
{
    public class Request
    {
        public string Name { get; set; } = string.Empty;
    }
    // Falta Response
}
```

**Solución**: Siempre definir ambas clases, incluso si Response está vacía:

```csharp
✅ public class CreateUserModel
{
    public class Request
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Response
    {
        public UserDto User { get; set; } = new UserDto();
    }
}
```

---

### Error 2: Nombres de propiedades no coinciden con ruta

```csharp
// Model
public class Request
{
    public string Id { get; set; } = string.Empty;  // ❌
}

// Endpoint
Get("/users/{UserName}");  // ❌ No coincide
```

**Solución**: Los nombres deben coincidir exactamente:

```csharp
✅ public class Request
{
    public string UserName { get; set; } = string.Empty;
}

Get("/users/{UserName}");
```

---

### Error 3: Usar Entity en Response

```csharp
❌ public class Response
{
    public User User { get; set; }  // Entity de domain
}
```

**Solución**: Usar DTO:

```csharp
✅ public class Response
{
    public UserDto User { get; set; } = new UserDto();
}
```

---

### Error 4: No inicializar colecciones

```csharp
❌ public IEnumerable<UserDto> Users { get; set; }  // null
```

**Solución**: Inicializar con lista vacía:

```csharp
✅ public IEnumerable<UserDto> Users { get; set; } = new List<UserDto>();
```

---

### Error 5: Validación en Request Model

```csharp
❌ public class Request
{
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set
        {
            if (!value.Contains("@"))  // ❌ Validación aquí
                throw new ArgumentException("Invalid email");
            _email = value;
        }
    }
}
```

**Solución**: Usar FluentValidation:

```csharp
✅ public class Request
{
    public string Email { get; set; } = string.Empty;
}

public class RequestValidator : Validator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
```

---

### Error 6: Propiedades requeridas nullable

```csharp
❌ public class Request
{
    public string? Name { get; set; }  // Debería ser requerida
}
```

**Solución**: No usar nullable para propiedades requeridas:

```csharp
✅ public class Request
{
    public string Name { get; set; } = string.Empty;
}

// Y en validator:
RuleFor(x => x.Name).NotEmpty();
```

---

### Error 7: Mapeo manual en endpoint

```csharp
❌ public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var command = new CreateUserUseCase.Command
    {
        Name = req.Name,
        Email = req.Email
    };
    // ...
}
```

**Solución**: Usar AutoMapper:

```csharp
✅ public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var command = _mapper.Map<CreateUserUseCase.Command>(req);
    // ...
}
```

---

## Referencias

### Documentación Oficial

- **FastEndpoints Request/Response**: https://fast-endpoints.com/docs/request-response-models
- **FluentValidation**: https://docs.fluentvalidation.net/
- **AutoMapper**: https://docs.automapper.org/

### Guías Relacionadas

- [FastEndpoints Basics](./fastendpoints-basics.md)
- [DTOs](./dtos.md)
- [AutoMapper Profiles](./automapper-profiles.md)
- [Error Responses](./error-responses.md)
- [Validation](../application-layer/validation/README.md)

### Archivos de Referencia del Proyecto

**Request/Response Models**:
- [CreateUserModel.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\models\CreateUserModel.cs)
- [GetUserModel.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\models\GetUserModel.cs)
- [GetManyAndCountModel.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\models\GetManyAndCountModel.cs)
- [UpdateUserLockModel.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\models\UpdateUserLockModel.cs)
- [UpdatePrototypeModel.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\prototypes\models\UpdatePrototypeModel.cs)

**DTOs**:
- [UserDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\UserDto.cs)
- [GetManyAndCountResultDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\GetManyAndCountResultDto.cs)

**Validators**:
- [GetUserRequestValidator.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\validators\GetUserRequestValidator.cs)

**Mapping Profiles**:
- [UserMappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\UserMappingProfile.cs)

---

## Changelog

### Version 1.0.0 (2025-01-15)
- ✅ Initial release
- ✅ Complete documentation of Request/Response Models pattern
- ✅ Naming conventions and nested class structure
- ✅ Integration with FastEndpoints, FluentValidation, and AutoMapper
- ✅ 6 complete working examples from reference project
- ✅ Best practices and common errors
- ✅ XML documentation templates

---

**Siguiente Guía**: [DTOs](./dtos.md)

[◀️ Volver al WebApi Layer](./README.md) | [🏠 Inicio](../README.md)
