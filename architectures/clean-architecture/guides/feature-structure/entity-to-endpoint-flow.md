# Entity to Endpoint Flow - Feature Structure

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Esta guía documenta el flujo completo de datos desde la entidad de dominio hasta el endpoint HTTP en aplicaciones .NET con Clean Architecture. Muestra cómo las diferentes capas interactúan, cómo se transforman los datos, y cómo fluyen las dependencias.

## Objetivo

Comprender el flujo end-to-end de operaciones comunes:
- Cómo se crean, consultan, actualizan y eliminan entidades
- Qué responsabilidad tiene cada capa
- Cómo se transforman los datos entre capas
- Cómo se manejan errores a través del flujo
- Cómo se orquestan transacciones

---

## Tabla de Contenido

1. [Arquitectura de Capas](#arquitectura-de-capas)
2. [Create Flow (Creación)](#create-flow-creación)
3. [Get Flow (Consulta Individual)](#get-flow-consulta-individual)
4. [GetManyAndCount Flow (Consulta con Paginación)](#getmanyandcount-flow-consulta-con-paginación)
5. [Update Flow (Actualización)](#update-flow-actualización)
6. [Delete Flow (Eliminación)](#delete-flow-eliminación)
7. [Dependency Flow](#dependency-flow)
8. [Data Transformation](#data-transformation)
9. [Error Handling Flow](#error-handling-flow)
10. [Transaction Management](#transaction-management)

---

## Arquitectura de Capas

### Diagrama General

```
┌─────────────────────────────────────────────────────────────┐
│                         HTTP Request                         │
│                     POST /users                              │
│                     { email, name }                          │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                     WebApi Layer                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ CreateUserEndpoint                                   │   │
│  │  • Recibe Request (Model)                           │   │
│  │  • Mapea a Command (AutoMapper)                     │   │
│  │  • Invoca UseCase.ExecuteAsync()                    │   │
│  │  • Maneja Result<T> (success/fail)                  │   │
│  │  • Mapea a Response DTO                             │   │
│  │  • Retorna HTTP Status Code + JSON                  │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │ Command
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ CreateUserUseCase.Handler                            │   │
│  │  • Orquesta lógica de negocio                       │   │
│  │  • Usa UnitOfWork para transacciones                │   │
│  │  • Llama métodos de repositorio                     │   │
│  │  • Llama servicios externos (IIdentityService)      │   │
│  │  • Maneja excepciones de dominio                    │   │
│  │  • Retorna Result<User>                             │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │ Repository calls
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                         │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ NHUserRepository                                     │   │
│  │  • Implementa IUserRepository                       │   │
│  │  • Usa NHibernate ISession                          │   │
│  │  • Valida entidad (entity.IsValid())               │   │
│  │  • Ejecuta queries SQL (via NHibernate)            │   │
│  │  • Retorna entidades de dominio                     │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ User Entity + UserValidator                          │   │
│  │  • Reglas de negocio                                │   │
│  │  • Validaciones (FluentValidation)                  │   │
│  │  • Sin dependencias externas                        │   │
│  │  • Propiedades virtual para NHibernate lazy loading │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
                      Database (PostgreSQL)
```

### Responsabilidades por Capa

| Capa | Responsabilidad | Entrada | Salida |
|------|----------------|---------|--------|
| **WebApi** | Traducción HTTP ↔ Dominio | HTTP Request | HTTP Response |
| **Application** | Orquestación de casos de uso | Command | Result<T> |
| **Infrastructure** | Persistencia y servicios externos | Domain entities | Domain entities |
| **Domain** | Reglas de negocio puras | - | Entities validadas |

---

## Create Flow (Creación)

Flujo completo para crear una nueva entidad.

### Diagrama de Secuencia

```
Client          Endpoint           UseCase          Repository         Domain
  │                │                  │                  │                │
  │─POST /users───>│                  │                  │                │
  │  {email, name} │                  │                  │                │
  │                │                  │                  │                │
  │                │──Map to Command─>│                  │                │
  │                │                  │                  │                │
  │                │                  │─BeginTransaction>│                │
  │                │                  │                  │                │
  │                │                  │─CreateAsync()───>│                │
  │                │                  │  (email, name)   │                │
  │                │                  │                  │                │
  │                │                  │                  │─new User()────>│
  │                │                  │                  │  (email, name) │
  │                │                  │                  │                │
  │                │                  │                  │<─User instance─│
  │                │                  │                  │                │
  │                │                  │                  │─IsValid()?────>│
  │                │                  │                  │                │
  │                │                  │                  │<─ValidationResult
  │                │                  │                  │                │
  │                │                  │                  │─AddAsync()─────│
  │                │                  │                  │  (user)        │
  │                │                  │                  │                │
  │                │                  │<─User────────────│                │
  │                │                  │                  │                │
  │                │                  │─Commit()────────>│                │
  │                │                  │                  │                │
  │                │                  │─IdentityService->│                │
  │                │                  │  Create()        │                │
  │                │                  │                  │                │
  │                │<─Result<User>────│                  │                │
  │                │                  │                  │                │
  │                │─Map to Response──│                  │                │
  │                │  (UserDto)       │                  │                │
  │                │                  │                  │                │
  │<─201 Created───│                  │                  │                │
  │  {UserDto}     │                  │                  │                │
```

### 1. WebApi Layer - CreateUserEndpoint.cs

```csharp
// WebApi/features/users/endpoint/CreateUserEndpoint.cs
namespace hashira.stone.backend.webapi.features.users.endpoint;

using FastEndpoints;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.users.models;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.domain.exceptions;
using FluentResults;

public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Post("/users");
        Description(b => b
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        DontThrowIfValidationFails();
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
    {
        // 1️⃣ Mapeo: Request Model → Command
        var command = _mapper.Map<CreateUserUseCase.Command>(request);

        // 2️⃣ Ejecución del caso de uso
        var result = await command.ExecuteAsync(ct);

        // 3️⃣ Manejo de errores (Result pattern)
        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();

            // InvalidDomainException = 400 Bad Request
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is InvalidDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return;
            }

            // DuplicatedDomainException = 409 Conflict
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is DuplicatedDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                return;
            }

            // Default = 500 Internal Server Error
            AddError(error?.Message ?? "Unknown error");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            return;
        }

        // 4️⃣ Mapeo: User entity → Response DTO
        var userResponse = _mapper.Map<CreateUserModel.Response>(result.Value);

        // 5️⃣ Retorno HTTP 201 Created
        await Send.CreatedAtAsync(
            $"/users/{userResponse.User.Id}",
            new[] { userResponse.User.Id },
            userResponse,
            false,
            ct);
    }
}
```

**📋 Responsabilidades del Endpoint:**
- ✅ Recibir HTTP Request
- ✅ Mapear Request → Command (AutoMapper)
- ✅ Invocar Use Case
- ✅ Traducir errores de dominio a HTTP status codes
- ✅ Mapear resultado a DTO
- ✅ Retornar HTTP Response

### 2. Application Layer - CreateUserUseCase.cs

```csharp
// Application/usecases/users/CreateUserUseCase.cs
namespace hashira.stone.backend.application.usecases.users;

using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.interfaces.services;

public abstract class CreateUserUseCase
{
    // Inner class: Command
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // Inner class: Handler
    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IIdentityService _identityService = identityService;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            // 1️⃣ Iniciar transacción
            _uoW.BeginTransaction();

            try
            {
                // 2️⃣ Crear usuario en servicio externo (Auth0)
                var password = GenerateRandomPassword();
                _identityService.Create(command.Email, command.Name, password);

                // 3️⃣ Crear usuario en base de datos
                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

                // 4️⃣ Commit transacción
                _uoW.Commit();

                // 5️⃣ Retornar resultado exitoso
                return Result.Ok(user);
            }
            catch (InvalidDomainException idex)
            {
                // Rollback en caso de validación fallida
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";
                return Result.Fail(new Error(firstErrorMessage).CausedBy(idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                // Rollback en caso de duplicado
                _uoW.Rollback();
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                // Rollback en caso de error genérico
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }

        private static string GenerateRandomPassword() { /* ... */ }
    }
}
```

**📋 Responsabilidades del Use Case:**
- ✅ Orquestar lógica de negocio
- ✅ Gestionar transacciones (BeginTransaction, Commit, Rollback)
- ✅ Llamar repositorios y servicios externos
- ✅ Manejar excepciones de dominio
- ✅ Retornar Result<T> (success o fail)

### 3. Infrastructure Layer - NHUserRepository.cs

```csharp
// Infrastructure/nhibernate/NHUserRepository.cs
namespace hashira.stone.backend.infrastructure.nhibernate;

using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        // 1️⃣ Crear instancia de la entidad
        var user = new User(email, name);

        // 2️⃣ Validar entidad (FluentValidation)
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // 3️⃣ Verificar duplicados
        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"A user with the email '{email}' already exists.");

        // 4️⃣ Agregar a la sesión de NHibernate
        await AddAsync(user);

        // 5️⃣ Flush si no hay transacción activa
        FlushWhenNotActiveTransaction();

        // 6️⃣ Retornar entidad creada
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}
```

**📋 Responsabilidades del Repository:**
- ✅ Crear instancia de entidad
- ✅ Validar entidad (entity.IsValid())
- ✅ Verificar reglas de negocio (duplicados)
- ✅ Persistir en base de datos
- ✅ Lanzar excepciones de dominio

### 4. Domain Layer - User.cs

```csharp
// Domain/entities/User.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
    public virtual string UserId { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}
```

```csharp
// Domain/entities/validators/UserValidator.cs
namespace hashira.stone.backend.domain.entities.validators;

using FluentValidation;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Email] cannot be null or empty")
            .WithErrorCode("Email")
            .EmailAddress()
            .WithMessage("The [Email] is not a valid email address")
            .WithErrorCode("Email_InvalidDomain");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name");
    }
}
```

**📋 Responsabilidades del Domain:**
- ✅ Definir propiedades de la entidad
- ✅ Definir reglas de validación (FluentValidation)
- ✅ Sin dependencias de infraestructura
- ✅ Propiedades `virtual` para NHibernate lazy loading

---

## Get Flow (Consulta Individual)

Flujo para obtener una entidad por ID o criterio.

### Diagrama de Secuencia

```
Client       Endpoint        UseCase       Repository      Database
  │             │               │               │              │
  │─GET /users/{username}──────>│               │              │
  │             │               │               │              │
  │             │──Map to Command────>│         │              │
  │             │               │               │              │
  │             │               │──GetByEmailAsync(username)──>│
  │             │               │               │              │
  │             │               │               │──SELECT * FROM users
  │             │               │               │  WHERE email = ?──>│
  │             │               │               │              │
  │             │               │               │<──User row──────────│
  │             │               │               │              │
  │             │               │<──User entity───────────────│
  │             │               │               │              │
  │             │<──Result<User>──────│         │              │
  │             │               │               │              │
  │             │──Map to UserDto─────│         │              │
  │             │               │               │              │
  │<─200 OK────│               │               │              │
  │  {UserDto} │               │               │              │
```

### Application Layer - GetUserUseCase.cs

```csharp
// Application/usecases/users/GetUserUseCase.cs
namespace hashira.stone.backend.application.usecases.users;

using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;

public class GetUserUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string UserName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                // 1️⃣ Buscar usuario por email
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);

                // 2️⃣ Retornar resultado (success o fail)
                return user == null
                    ? Result.Fail(UserErrors.UserNotFound(request.UserName))
                    : Result.Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with username: {UserName}", request.UserName);
                return Result.Fail("Error retrieving user");
            }
        }
    }
}
```

**🔑 Diferencias vs Create:**
- ✅ No requiere transacción (solo lectura)
- ✅ Retorna Result.Fail si no encuentra la entidad
- ✅ Usa método específico del repositorio (GetByEmailAsync)

---

## GetManyAndCount Flow (Consulta con Paginación)

Flujo para obtener múltiples entidades con paginación, filtrado y ordenamiento.

### Diagrama de Secuencia

```
Client       Endpoint         UseCase        Repository       Database
  │             │                │                │               │
  │─GET /users?page=1&size=10&filter=...────>│    │               │
  │             │                │                │               │
  │             │──Extract QueryString────>│       │               │
  │             │                │                │               │
  │             │                │──GetManyAndCountAsync(query)──>│
  │             │                │                │               │
  │             │                │                │──Parse filters──>│
  │             │                │                │──Build WHERE clause
  │             │                │                │──Apply sorting─>│
  │             │                │                │──Apply pagination
  │             │                │                │               │
  │             │                │                │──SELECT * FROM users
  │             │                │                │  WHERE ... ORDER BY ... LIMIT ... OFFSET ...──>│
  │             │                │                │               │
  │             │                │                │──SELECT COUNT(*) FROM users WHERE ...──>│
  │             │                │                │               │
  │             │                │                │<──[User1, User2, ...] + Count────────────│
  │             │                │                │               │
  │             │                │<──GetManyAndCountResult<User>──────│
  │             │                │  { Items: [...], Count: 42 }│               │
  │             │                │                │               │
  │             │<──GetManyAndCountResult<User>───│               │
  │             │                │                │               │
  │             │──Map to GetManyAndCountResultDto<UserDto>───│   │
  │             │                │                │               │
  │<─200 OK────│                │                │               │
  │  { items: [...], count: 42 }                │               │
```

### Application Layer - GetManyAndCountUsersUseCase.cs

```csharp
// Application/usecases/users/GetManyAndCountUsersUseCase.cs
namespace hashira.stone.backend.application.usecases.users;

using FastEndpoints;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;

public abstract class GetManyAndCountUsersUseCase
{
    public class Command : ICommand<GetManyAndCountResult<User>>
    {
        // Query string: ?page=1&size=10&filter=email~"test"&sort=email:asc
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                _logger.LogInformation("Executing GetManyAndCountUsersUseCase with query: {Query}", command.Query);

                // 1️⃣ Llamar repositorio con query string
                // El repositorio parsea: filtros, paginación, ordenamiento
                var users = await _uoW.Users.GetManyAndCountAsync(
                    command.Query,
                    nameof(User.Email),  // Default sort field
                    ct);

                _logger.LogInformation("End GetManyAndCountUsersUseCase with total users: {TotalUsers}", users.Count);

                _uoW.Commit();

                // 2️⃣ Retornar resultado { Items: [...], Count: N }
                return users;
            }
            catch
            {
                _uoW.Rollback();
                throw;
            }
        }
    }
}
```

### WebApi Layer - GetManyAndCountUsersEndPoint.cs

```csharp
// WebApi/features/users/endpoint/GetManyAndCountUsersEndPoint.cs
namespace hashira.stone.backend.webapi.features.users.endpoint;

using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.users.models;

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

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        try
        {
            // 1️⃣ Extraer query string completo
            var request = new GetManyAndCountUsersUseCase.Command
            {
                Query = HttpContext.Request.QueryString.Value
            };

            // 2️⃣ Ejecutar use case
            var getManyAndCountResult = await request.ExecuteAsync(ct);

            // 3️⃣ Mapear resultado a DTO
            // GetManyAndCountResult<User> → GetManyAndCountResultDto<UserDto>
            var response = this.mapper.Map<GetManyAndCountResultDto<UserDto>>(getManyAndCountResult);

            Logger.LogInformation("Successfully retrieved users");

            // 4️⃣ Retornar 200 OK con { items: [...], count: N }
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

**🔑 Características GetManyAndCount:**
- ✅ Recibe query string completo (`?page=1&size=10&filter=...`)
- ✅ Repository parsea filtros, paginación, ordenamiento
- ✅ Retorna `{ items: [...], count: totalCount }`
- ✅ Usado para listados con paginación en UI

**Ejemplo de Query String:**
```
GET /users?page=1&size=10&filter=email~"@example.com"&sort=name:asc

Resultado:
{
  "items": [
    { "id": "...", "email": "user1@example.com", "name": "Alice" },
    { "id": "...", "email": "user2@example.com", "name": "Bob" }
  ],
  "count": 42  // Total de registros (sin paginación)
}
```

---

## Update Flow (Actualización)

Flujo para actualizar una entidad existente.

### Diagrama de Secuencia

```
Client       Endpoint         UseCase        Repository       Domain
  │             │                │                │               │
  │─PUT /technical-standards/{id}──────>│         │               │
  │  { code, name, ... }        │                │               │
  │             │                │                │               │
  │             │──Map to Command──────>│         │               │
  │             │                │                │               │
  │             │                │──BeginTransaction──────>│       │
  │             │                │                │               │
  │             │                │──UpdateAsync(id, ...)──>│       │
  │             │                │                │               │
  │             │                │                │──GetByIdAsync(id)──>│
  │             │                │                │               │
  │             │                │                │<──Existing entity───│
  │             │                │                │               │
  │             │                │                │──Update properties──>│
  │             │                │                │               │
  │             │                │                │──IsValid()?───>│
  │             │                │                │               │
  │             │                │                │<──ValidationResult──│
  │             │                │                │               │
  │             │                │                │──SaveOrUpdateAsync()─>│
  │             │                │                │               │
  │             │                │<──Updated entity────────│       │
  │             │                │                │               │
  │             │                │──Commit()──────>│               │
  │             │                │                │               │
  │             │<──Result<TechnicalStandard>───│ │               │
  │             │                │                │               │
  │<─200 OK────│                │                │               │
  │  {TechnicalStandardDto}     │                │               │
```

### Application Layer - UpdateTechnicalStandardUseCase.cs

```csharp
// Application/usecases/technicalstandards/UpdateTechnicalStandardUseCase.cs
namespace hashira.stone.backend.application.usecases.technicalstandards;

using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;

public class UpdateTechnicalStandardUseCase
{
    public class Command : ICommand<Result<TechnicalStandard>>
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uow) : ICommandHandler<Command, Result<TechnicalStandard>>
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<Result<TechnicalStandard>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                // 1️⃣ Iniciar transacción
                _uow.BeginTransaction();

                // 2️⃣ Actualizar entidad (repository busca, actualiza y valida)
                var updated = await _uow.TechnicalStandards.UpdateAsync(
                    request.Id,
                    request.Code,
                    request.Name,
                    request.Edition,
                    request.Status,
                    request.Type);

                // 3️⃣ Commit transacción
                _uow.Commit();

                // 4️⃣ Retornar resultado exitoso
                return Result.Ok(updated);
            }
            catch (ResourceNotFoundException ex)
            {
                _uow.Rollback();
                return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
            }
            catch (InvalidDomainException ex)
            {
                _uow.Rollback();
                return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
            }
            catch (DuplicatedDomainException ex)
            {
                _uow.Rollback();
                return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
            }
            catch (Exception ex)
            {
                _uow.Rollback();
                return Result.Fail<TechnicalStandard>(new Error("Internal server error: " + ex.Message).CausedBy(ex));
            }
        }
    }
}
```

**🔑 Diferencias vs Create:**
- ✅ Requiere ID para identificar la entidad
- ✅ Repository busca entidad existente primero
- ✅ Lanza `ResourceNotFoundException` si no existe
- ✅ Actualiza propiedades y valida
- ✅ Retorna entidad actualizada

---

## Delete Flow (Eliminación)

Flujo para eliminar una entidad.

### Patrón Típico

```csharp
// Application/usecases/users/DeleteUserUseCase.cs
public class DeleteUserUseCase
{
    public class Command : ICommand<Result>
    {
        public Guid Id { get; set; }
    }

    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result>
    {
        private readonly IUnitOfWork _uoW = uoW;

        public async Task<Result> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                // Buscar entidad
                var user = await _uoW.Users.GetByIdAsync(request.Id, ct);
                if (user == null)
                    return Result.Fail(UserErrors.UserNotFound(request.Id.ToString()));

                // Eliminar
                await _uoW.Users.DeleteAsync(user, ct);

                _uoW.Commit();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail(ex.Message);
            }
        }
    }
}
```

**🔑 Características Delete:**
- ✅ Retorna `Result` (sin genérico) si no necesita devolver la entidad
- ✅ Verifica existencia antes de eliminar
- ✅ Endpoint retorna 204 No Content en caso de éxito

---

## Dependency Flow

Flujo de dependencias desde WebApi hasta Domain.

### Diagrama de Dependencias

```
┌─────────────────────────────────────────────────────────────┐
│                        WebApi Layer                          │
│  ┌────────────────────────────────────────────────────┐     │
│  │ CreateUserEndpoint                                 │     │
│  │                                                    │     │
│  │ Dependencies (Constructor Injection):              │     │
│  │  • IMapper (AutoMapper)                           │     │
│  │                                                    │     │
│  │ Uses:                                              │     │
│  │  • CreateUserUseCase.Command                      │     │
│  │  • CreateUserModel.Request/Response               │     │
│  │  • UserDto                                         │     │
│  └────────────────────────────────────────────────────┘     │
└──────────────────────────┬──────────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌────────────────────────────────────────────────────┐     │
│  │ CreateUserUseCase.Handler                          │     │
│  │                                                    │     │
│  │ Dependencies (Constructor Injection):              │     │
│  │  • IUnitOfWork (from Domain)                      │     │
│  │  • IIdentityService (from Domain)                 │     │
│  │                                                    │     │
│  │ Uses:                                              │     │
│  │  • User entity (Domain)                           │     │
│  │  • Result<T> (FluentResults)                      │     │
│  │  • Exceptions (Domain)                            │     │
│  └────────────────────────────────────────────────────┘     │
└──────────────────────────┬──────────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  ┌────────────────────────────────────────────────────┐     │
│  │ NHUnitOfWork (implements IUnitOfWork)              │     │
│  │                                                    │     │
│  │ Dependencies:                                      │     │
│  │  • ISession (NHibernate)                          │     │
│  │  • IServiceProvider (DI)                          │     │
│  │                                                    │     │
│  │ Provides:                                          │     │
│  │  • IUserRepository → NHUserRepository             │     │
│  │  • IRoleRepository → NHRoleRepository             │     │
│  └────────────────────────────────────────────────────┘     │
│  ┌────────────────────────────────────────────────────┐     │
│  │ NHUserRepository (implements IUserRepository)      │     │
│  │                                                    │     │
│  │ Dependencies:                                      │     │
│  │  • ISession (NHibernate)                          │     │
│  │                                                    │     │
│  │ Uses:                                              │     │
│  │  • User entity (Domain)                           │     │
│  │  • Exceptions (Domain)                            │     │
│  └────────────────────────────────────────────────────┘     │
└──────────────────────────┬──────────────────────────────────┘
                           │ depends on
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  ┌────────────────────────────────────────────────────┐     │
│  │ User Entity                                        │     │
│  │ UserValidator (FluentValidation)                   │     │
│  │ IUserRepository (interface)                        │     │
│  │ IUnitOfWork (interface)                            │     │
│  │ InvalidDomainException                             │     │
│  │ DuplicatedDomainException                          │     │
│  │                                                    │     │
│  │ No dependencies externas (solo .NET base)          │     │
│  └────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### Inyección de Dependencias

**Program.cs (Simplified):**

```csharp
// WebApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Registrar Infrastructure (NHibernate, Repositories)
builder.Services.AddScoped<ISession>(sp =>
    NHSessionFactory.GetSession());
builder.Services.AddScoped<IUnitOfWork, NHUnitOfWork>();
builder.Services.AddScoped<IUserRepository, NHUserRepository>();

// 2️⃣ Registrar Application (Use Cases)
// FastEndpoints auto-registra Handlers como ICommandHandler<,>

// 3️⃣ Registrar WebApi (AutoMapper)
builder.Services.AddAutoMapper(typeof(Program));

// 4️⃣ FastEndpoints
builder.Services.AddFastEndpoints();

var app = builder.Build();
app.UseFastEndpoints();
app.Run();
```

**📋 Orden de Registro:**
1. Infrastructure (repositories, session)
2. Application (handlers)
3. WebApi (AutoMapper, endpoints)

---

## Data Transformation

Cómo se transforman los datos entre capas.

### Flujo de Transformación

```
HTTP Request (JSON)
      │
      ▼
Request Model (WebApi)                  CreateUserModel.Request
      │                                 { Email, Name }
      │ AutoMapper
      ▼
Command (Application)                   CreateUserUseCase.Command
      │                                 { Email, Name }
      │ Repository
      ▼
Entity (Domain)                         User
      │                                 { Id, Email, Name, Roles, ... }
      │ NHibernate
      ▼
Database Row                            users table
      │                                 | id | email | name | ... |
      │
      ▼ (Read back)
Entity (Domain)                         User
      │                                 { Id, Email, Name, Roles, ... }
      │ AutoMapper
      ▼
Response DTO (WebApi)                   UserDto
      │                                 { Id, Email, Name, Roles }
      │
      ▼
HTTP Response (JSON)
```

### Mapeo Explícito: Request → Command

**AutoMapper Profile:**

```csharp
// WebApi/mappingprofiles/UserMappingProfile.cs
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Request → Command
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();

        // User entity → UserDto
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));

        // User entity → CreateUserModel.Response
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User,
                opt => opt.MapFrom(src => src));
    }
}
```

### Transformación sin AutoMapper (Manual)

En algunos casos, la transformación es manual:

```csharp
// GetManyAndCountUsersEndPoint.cs
var request = new GetManyAndCountUsersUseCase.Command
{
    Query = HttpContext.Request.QueryString.Value  // Manual assignment
};
```

---

## Error Handling Flow

Cómo se propagan y manejan los errores.

### Propagación de Errores

```
Domain Layer (lanza excepciones)
      │
      │ throw InvalidDomainException
      │ throw DuplicatedDomainException
      │ throw ResourceNotFoundException
      ▼
Infrastructure Layer (propaga)
      │
      │ Excepciones no capturadas se propagan
      ▼
Application Layer (captura y convierte a Result)
      │
      │ try-catch
      │ return Result.Fail(error)
      ▼
WebApi Layer (traduce a HTTP status)
      │
      │ InvalidDomainException → 400 Bad Request
      │ DuplicatedDomainException → 409 Conflict
      │ ResourceNotFoundException → 404 Not Found
      │ Exception (generic) → 500 Internal Server Error
      ▼
HTTP Response
```

### Ejemplo Completo de Error Handling

**Domain Exception:**

```csharp
// Domain/exceptions/InvalidDomainException.cs
public class InvalidDomainException : Exception
{
    public InvalidDomainException(string message) : base(message) { }
}
```

**Repository (lanza):**

```csharp
// Infrastructure/nhibernate/NHUserRepository.cs
if (!user.IsValid())
    throw new InvalidDomainException(user.Validate());
```

**Use Case (captura y convierte):**

```csharp
// Application/usecases/users/CreateUserUseCase.cs
catch (InvalidDomainException idex)
{
    _uoW.Rollback();
    return Result.Fail(new Error(idex.Message).CausedBy(idex));
}
```

**Endpoint (traduce a HTTP):**

```csharp
// WebApi/features/users/endpoint/CreateUserEndpoint.cs
if (error?.Reasons.OfType<ExceptionalError>()
    .Any(r => r.Exception is InvalidDomainException) == true)
{
    AddError(error.Message);
    await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
    return;
}
```

### Tabla de Mapeo: Exception → HTTP Status

| Domain Exception | HTTP Status Code | Descripción |
|-----------------|------------------|-------------|
| `InvalidDomainException` | 400 Bad Request | Validación de dominio fallida |
| `DuplicatedDomainException` | 409 Conflict | Recurso duplicado |
| `ResourceNotFoundException` | 404 Not Found | Recurso no encontrado |
| `Exception` (generic) | 500 Internal Server Error | Error inesperado |

---

## Transaction Management

Cómo se gestionan las transacciones.

### Unit of Work Pattern

```csharp
// Domain/interfaces/repositories/IUnitOfWork.cs
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    ITechnicalStandardRepository TechnicalStandards { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

### Implementación

```csharp
// Infrastructure/nhibernate/NHUnitOfWork.cs
public class NHUnitOfWork : IUnitOfWork
{
    private readonly ISession _session;
    private ITransaction? _transaction;

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        Users = new NHUserRepository(session, serviceProvider);
        Roles = new NHRoleRepository(session, serviceProvider);
        // ...
    }

    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }

    public void BeginTransaction()
    {
        _transaction = _session.BeginTransaction();
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _session?.Dispose();
    }
}
```

### Uso en Use Cases

```csharp
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    _uoW.BeginTransaction();  // 1️⃣ Iniciar transacción

    try
    {
        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
        _identityService.Create(command.Email, command.Name, password);

        _uoW.Commit();  // 2️⃣ Commit si todo OK
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _uoW.Rollback();  // 3️⃣ Rollback si error
        return Result.Fail(ex.Message);
    }
}
```

**📋 Reglas de Transacciones:**
- ✅ **Create/Update/Delete**: Siempre usar transacción
- ✅ **Read-only (Get)**: Opcionalmente sin transacción
- ✅ **GetManyAndCount**: Usar transacción para consistencia
- ✅ **Rollback automático** en caso de excepción

---

## Resumen de Flujos

### Comparación de Operaciones

| Operación | HTTP Method | Transacción | Validación | Retorno |
|-----------|-------------|-------------|------------|---------|
| **Create** | POST | ✅ Sí | ✅ Sí (validator) | Result<Entity> → 201 Created |
| **Get** | GET | ❌ No | ❌ No | Result<Entity> → 200 OK |
| **GetManyAndCount** | GET | ⚠️ Opcional | ❌ No | GetManyAndCountResult<Entity> → 200 OK |
| **Update** | PUT/PATCH | ✅ Sí | ✅ Sí (validator) | Result<Entity> → 200 OK |
| **Delete** | DELETE | ✅ Sí | ❌ No | Result → 204 No Content |

### Patrones Comunes

**✅ Create/Update:**
```
BeginTransaction → Validate → Save → Commit → Return Result<T>
```

**✅ Get:**
```
Query → Return Result<T> (no transaction)
```

**✅ GetManyAndCount:**
```
BeginTransaction → Parse filters → Query → Count → Commit → Return Result
```

**✅ Error Handling:**
```
try → Operation → Commit
catch → Rollback → Return Result.Fail()
```

---

## Checklist: Implementar Nuevo Flujo

### Create Flow
- [ ] Domain: Entity + Validator creados
- [ ] Domain: IRepository.CreateAsync() definido
- [ ] Infrastructure: Repository.CreateAsync() implementado con validación
- [ ] Application: CreateUseCase.Command + Handler creados
- [ ] Application: Maneja transacciones (BeginTransaction, Commit, Rollback)
- [ ] Application: Captura excepciones de dominio
- [ ] WebApi: CreateEndpoint implementado
- [ ] WebApi: Request Model con inner classes Request/Response
- [ ] WebApi: AutoMapper profile configurado
- [ ] WebApi: Traduce excepciones a HTTP status codes
- [ ] Tests: Unit tests para Handler
- [ ] Tests: Integration tests para Endpoint

### Get Flow
- [ ] Domain: IRepository.GetByIdAsync() o similar definido
- [ ] Infrastructure: Repository query implementado
- [ ] Application: GetUseCase.Command + Handler creados
- [ ] Application: Retorna Result.Fail si no encuentra
- [ ] WebApi: GetEndpoint implementado
- [ ] WebApi: Mapea entidad a DTO
- [ ] Tests: Unit tests para Handler

### GetManyAndCount Flow
- [ ] Domain: IRepository.GetManyAndCountAsync() definido
- [ ] Infrastructure: Query con filtros, paginación, ordenamiento
- [ ] Application: GetManyAndCountUseCase implementado
- [ ] WebApi: Endpoint extrae QueryString completo
- [ ] WebApi: Retorna GetManyAndCountResultDto<T>
- [ ] Tests: Integration tests con diferentes filtros

### Update Flow
- [ ] Domain: IRepository.UpdateAsync() definido
- [ ] Infrastructure: Repository busca, actualiza, valida
- [ ] Application: UpdateUseCase con transacción
- [ ] Application: Maneja ResourceNotFoundException
- [ ] WebApi: UpdateEndpoint con PUT o PATCH
- [ ] Tests: Unit tests para Handler

---

## Recursos Adicionales

### Guías Relacionadas

- [Folder Organization](./folder-organization.md) - Estructura de carpetas por capa
- [Naming Conventions](./naming-conventions.md) - Convenciones de nombres
- [Best Practices - Error Handling](../best-practices/error-handling.md) - Manejo de errores
- [Best Practices - Async/Await](../best-practices/async-await-patterns.md) - Patrones async

### Stack Tecnológico

- **FastEndpoints 7.0** - REPR pattern, Command/Handler
- **FluentResults 4.0** - Result<T> pattern
- **FluentValidation 12.0** - Validación de dominio
- **NHibernate 5.5** - ORM y persistencia
- **AutoMapper 14.0** - Mapeo de objetos

---

## Conclusión

**Principios Clave del Flujo:**

1. ✅ **Separation of Concerns** - Cada capa tiene responsabilidad clara
2. ✅ **Dependency Inversion** - Capas externas dependen de interfaces del dominio
3. ✅ **Result Pattern** - Manejo de errores sin excepciones en Application
4. ✅ **Unit of Work** - Gestión consistente de transacciones
5. ✅ **Data Transformation** - AutoMapper entre capas
6. ✅ **Command/Handler Pattern** - FastEndpoints para CQRS-lite

**Flujo Mental:**

```
HTTP → Endpoint → Command → Handler → Repository → Entity → Database
                                    ↓
                            Result<Entity>
                                    ↓
                           AutoMapper → DTO
                                    ↓
                              HTTP Response
```

Cada operación sigue este flujo con pequeñas variaciones según el tipo de operación (Create, Get, Update, Delete).

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
