# Folder Organization - Feature Structure

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Esta guía documenta la estructura estándar de carpetas y archivos para organizar **features** en aplicaciones .NET con Clean Architecture. Un feature representa un módulo de negocio completo que atraviesa todas las capas de la arquitectura.

## Objetivo

Establecer una organización consistente de carpetas que:
- Facilite encontrar código relacionado rápidamente
- Promueva la independencia entre features
- Mantenga la separación de concerns por capa
- Simplifique el onboarding de nuevos desarrolladores
- Escale bien al agregar nuevos features

---

## Principios de Organización

### 1. Vertical Slicing por Feature

Cada feature se organiza verticalmente atravesando todas las capas:

```
Domain Layer → Application Layer → Infrastructure Layer → WebApi Layer
```

**✅ Ventajas:**
- Código de un feature completo está co-localizado por capa
- Cambios a un feature no afectan otros features
- Fácil eliminar o agregar features
- Equipos pueden trabajar en features independientes

### 2. Separación de Concerns por Capa

Cada capa tiene responsabilidades específicas:

| Capa | Responsabilidad | Contenido |
|------|----------------|-----------|
| **Domain** | Lógica de negocio pura | Entities, DAOs, Validators, Repository Interfaces |
| **Application** | Casos de uso / orquestación | Use Cases (Command + Handler) |
| **Infrastructure** | Implementación técnica | Repositorios NHibernate, Mappers, Servicios externos |
| **WebApi** | Exposición HTTP | Endpoints, Models, DTOs |

### 3. Shared vs Feature-Specific

**Shared Components** (componentes compartidos):
- Interfaces base (`IRepository<T>`, `IReadOnlyRepository<T>`)
- Excepciones comunes (`InvalidDomainException`)
- Helpers y utilities
- Base classes (`AbstractDomainObject`, `BaseEndpoint`)

**Feature-Specific** (específicos del feature):
- Entities del feature
- Validators del feature
- Use cases del feature
- Endpoints del feature

---

## Estructura de Capas

### Domain Layer

Contiene la lógica de negocio pura y las reglas del dominio.

#### Estructura de Carpetas

```
hashira.stone.backend.domain/
├── entities/                                    # Entidades del dominio
│   ├── User.cs                                  # Entity: User
│   ├── Role.cs                                  # Entity: Role
│   ├── TechnicalStandard.cs                     # Entity: TechnicalStandard
│   ├── Prototype.cs                             # Entity: Prototype
│   ├── AbstractDomainObject.cs                  # Base class para entities
│   └── validators/                              # Validadores FluentValidation
│       ├── UserValidator.cs
│       ├── RoleValidator.cs
│       ├── TechnicalStandardValidator.cs
│       └── PrototypeValidator.cs
├── daos/                                        # Data Access Objects (read-only)
│   ├── TechnicalStandardDao.cs
│   └── PrototypeDao.cs
├── interfaces/                                  # Interfaces del dominio
│   ├── repositories/                            # Repository interfaces
│   │   ├── IRepository.cs                       # ✅ Shared - Base repository interface
│   │   ├── IReadOnlyRepository.cs               # ✅ Shared - Base read-only interface
│   │   ├── IUnitOfWork.cs                       # ✅ Shared - Unit of Work pattern
│   │   ├── IUserRepository.cs                   # ⚡ Feature-specific
│   │   ├── IRoleRepository.cs                   # ⚡ Feature-specific
│   │   ├── ITechnicalStandardRepository.cs      # ⚡ Feature-specific
│   │   ├── ITechnicalStandardDaoRepository.cs   # ⚡ Feature-specific (DAO)
│   │   ├── IPrototypeRepository.cs              # ⚡ Feature-specific
│   │   └── IPrototypeDaoRepository.cs           # ⚡ Feature-specific (DAO)
│   └── services/                                # Service interfaces
│       └── IIdentityService.cs                  # External service interface
├── exceptions/                                  # ✅ Shared - Domain exceptions
│   ├── InvalidDomainException.cs
│   ├── DuplicatedDomainException.cs
│   ├── ResourceNotFoundException.cs
│   └── InvalidFilterArgumentException.cs
├── errors/                                      # ✅ Shared - Error helpers
│   ├── AbstractDomainObjectErrors.cs
│   ├── UserErrors.cs
│   └── ResultBaseExtender.cs
└── resources/                                   # ✅ Shared - Constants and resources
    ├── StringResources.cs
    ├── ClaimTypeResource.cs
    ├── AppSchemaResource.cs
    ├── RolesResources.cs
    ├── TechnicalStandardResource.cs
    └── PrototypeResources.cs
```

#### Ejemplo Real: User Entity

```csharp
// Domain/entities/User.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the email of the user.
    /// </summary>
    public virtual string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles assigned to the user.
    /// </summary>
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    /// <summary>
    /// The user identifier from the identity provider (e.g., Auth0).
    /// </summary>
    public virtual string UserId { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

#### Ejemplo Real: UserValidator

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
            .WithErrorCode("Email_InvalidDomain")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithMessage("The [Email] must contain a valid domain format (e.g. user@domain.com)")
            .WithErrorCode("Email_InvalidFormat");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name");
    }
}
```

#### Ejemplo Real: IUserRepository

```csharp
// Domain/interfaces/repositories/IUserRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

using hashira.stone.backend.domain.entities;

/// <summary>
/// Defines a repository for managing <see cref="User"/> entities.
/// This interface extends the <see cref="IRepository{T, TKey}"/> to provide CRUD operations.
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Creates a new user with the specified email.
    /// </summary>
    Task<User> CreateAsync(string email, string name);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
}
```

**📋 Convenciones Domain Layer:**
- ✅ Entities en carpeta `entities/` (PascalCase, singular)
- ✅ Validators en carpeta `entities/validators/` con sufijo `Validator`
- ✅ DAOs en carpeta `daos/` con sufijo `Dao`
- ✅ Repository interfaces en `interfaces/repositories/` con prefijo `I`
- ✅ Excepciones en `exceptions/` con sufijo `Exception`
- ✅ Shared components separados de feature-specific

---

### Application Layer

Contiene los casos de uso (use cases) que orquestan la lógica de negocio.

#### Estructura de Carpetas

```
hashira.stone.backend.application/
├── usecases/                                    # Casos de uso organizados por feature
│   ├── users/                                   # ⚡ Feature: Users
│   │   ├── CreateUserUseCase.cs                 # CRUD: Create
│   │   ├── GetUserUseCase.cs                    # CRUD: Get
│   │   ├── GetManyAndCountUsersUseCase.cs       # CRUD: GetManyAndCount (list + pagination)
│   │   └── UpdateUserLockUseCase.cs             # CRUD: Update (parcial)
│   ├── roles/                                   # ⚡ Feature: Roles
│   │   ├── AddUsersToRoleUseCase.cs
│   │   └── RemoveUserFromRoleUseCase.cs
│   ├── technicalstandards/                      # ⚡ Feature: TechnicalStandards
│   │   ├── CreateTechnicalStandardUseCase.cs
│   │   ├── UpdateTechnicalStandardUseCase.cs
│   │   └── GetManyAndCountTechnicalStandardsUseCase.cs
│   └── prototypes/                              # ⚡ Feature: Prototypes
│       ├── CreatePrototypeUseCase.cs
│       └── GetManyAndCountPrototypesUseCase.cs
└── common/                                      # ✅ Shared - Componentes comunes
    └── ValidationError.cs
```

#### Ejemplo Real: CreateUserUseCase

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
    /// <summary>
    /// Command to create a new user.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IIdentityService _identityService = identityService;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                var password = GenerateRandomPassword();
                var auth0User = _identityService.Create(command.Email, command.Name, password);

                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
                _uoW.Commit();
                return Result.Ok(user);
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";
                return Result.Fail(new Error(firstErrorMessage).CausedBy(idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }

    private static string GenerateRandomPassword() { /* ... */ }
}
```

**📋 Convenciones Application Layer:**
- ✅ Use cases en carpeta `usecases/{feature}/` (feature en plural, lowercase)
- ✅ Nombres descriptivos con sufijo `UseCase`: `Create{Entity}UseCase`, `Get{Entity}UseCase`
- ✅ Cada use case contiene inner classes: `Command` + `Handler`
- ✅ Handler implementa `ICommandHandler<Command, Result<T>>`
- ✅ Usa `Result<T>` de FluentResults para manejo de errores
- ✅ Transacciones manejadas con `IUnitOfWork`

---

### Infrastructure Layer

Implementa las interfaces del dominio usando tecnologías específicas (NHibernate, servicios externos, etc.)

#### Estructura de Carpetas

```
hashira.stone.backend.infrastructure/
├── nhibernate/                                  # Implementación NHibernate
│   ├── NHRepository.cs                          # ✅ Shared - Base repository implementation
│   ├── NHReadOnlyRepository.cs                  # ✅ Shared - Base read-only implementation
│   ├── NHUnitOfWork.cs                          # ✅ Shared - UnitOfWork implementation
│   ├── NHSessionFactory.cs                      # ✅ Shared - Session factory
│   ├── ConnectionStringBuilder.cs               # ✅ Shared - Helper
│   ├── SortingCriteriaExtender.cs               # ✅ Shared - Helper
│   ├── NHUserRepository.cs                      # ⚡ Feature-specific: Users
│   ├── NHRoleRepository.cs                      # ⚡ Feature-specific: Roles
│   ├── NHTechnicalStandardRepository.cs         # ⚡ Feature-specific: TechnicalStandards
│   ├── NHTechnicalStandardDaoRepository.cs      # ⚡ Feature-specific: TechnicalStandards DAO
│   ├── NHPrototypeRepository.cs                 # ⚡ Feature-specific: Prototypes
│   ├── NHPrototypeDaoRepository.cs              # ⚡ Feature-specific: Prototypes DAO
│   ├── mappers/                                 # NHibernate mappings
│   │   ├── UserMapper.cs                        # ⚡ Feature-specific
│   │   ├── RoleMapper.cs                        # ⚡ Feature-specific
│   │   ├── TechnicalStandardMapper.cs           # ⚡ Feature-specific
│   │   ├── TechnicalStandardDaoMapper.cs        # ⚡ Feature-specific
│   │   ├── PrototypeMapper.cs                   # ⚡ Feature-specific
│   │   └── PrototypeDaoMapper.cs                # ⚡ Feature-specific
│   └── filtering/                               # ✅ Shared - Query filtering utilities
│       ├── FilterExpressionParser.cs
│       ├── QueryOperations.cs
│       ├── QueryStringParser.cs
│       ├── FilterOperator.cs
│       ├── RelationalOperator.cs
│       ├── QuickSearch.cs
│       ├── Sorting.cs
│       ├── StringExtender.cs
│       └── InvalidQueryStringArgumentException.cs
└── services/                                    # Servicios externos
    ├── Auth0Service.cs                          # ✅ Shared - External identity service
    └── Auth0ServiceMock.cs                      # ✅ Shared - Mock para testing
```

#### Ejemplo Real: NHUserRepository

```csharp
// Infrastructure/nhibernate/NHUserRepository.cs
namespace hashira.stone.backend.infrastructure.nhibernate;

using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

/// <summary>
/// Implementation of the <see cref="IUserRepository"/> using NHibernate.
/// </summary>
public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Create a new user with the specified email
    /// </summary>
    public async Task<User> CreateAsync(string email, string name)
    {
        var user = new User(email, name);

        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"A user with the email '{email}' already exists.");

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }

    /// <summary>
    /// Get a user by their email address
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}
```

**📋 Convenciones Infrastructure Layer:**
- ✅ Repositorios en carpeta raíz `nhibernate/` con prefijo `NH`
- ✅ Mappers en subcarpeta `nhibernate/mappers/` con sufijo `Mapper`
- ✅ Shared utilities en subcarpetas (ej: `filtering/`)
- ✅ Servicios externos en carpeta `services/`
- ✅ Naming: `NH{Entity}Repository` para repositories
- ✅ Naming: `{Entity}Mapper` para NHibernate class mappings

---

### WebApi Layer

Expone la funcionalidad a través de HTTP usando FastEndpoints.

#### Estructura de Carpetas

```
hashira.stone.backend.webapi/
├── features/                                    # Features organizados por entidad
│   ├── users/                                   # ⚡ Feature: Users
│   │   ├── endpoint/                            # Endpoints del feature
│   │   │   ├── CreateUserEndpoint.cs
│   │   │   ├── GetUserEndpoint.cs
│   │   │   ├── GetCurrentUserEndpoint.cs
│   │   │   ├── GetManyAndCountUsersEndPoint.cs
│   │   │   └── UpdateUserLockEndpoint.cs
│   │   └── models/                              # Request/Response models
│   │       ├── CreateUserModel.cs
│   │       ├── GetUserModel.cs
│   │       ├── GetManyAndCountModel.cs
│   │       └── UpdateUserLockModel.cs
│   ├── roles/                                   # ⚡ Feature: Roles
│   │   ├── endpoint/
│   │   │   ├── AddUsersToRoleEndpoint.cs
│   │   │   └── RemoveUserFromRoleEndpoint.cs
│   │   └── models/
│   │       └── UserRoleAssignmentModel.cs
│   ├── technicalstandards/                      # ⚡ Feature: TechnicalStandards
│   │   ├── endpoint/
│   │   │   ├── CreateTechnicalStandardEndpoint.cs
│   │   │   ├── UpdateTechnicalStandardEndpoint.cs
│   │   │   └── GetManyAndCountTechnicalStandardsEndPoint.cs
│   │   └── models/
│   │       ├── CreateTechnicalStandardModel.cs
│   │       ├── UpdateTechnicalStandardModel.cs
│   │       └── GetManyAndCountModel.cs
│   ├── prototypes/                              # ⚡ Feature: Prototypes
│   │   ├── endpoint/
│   │   │   ├── CreatePrototypeEndpoint.cs
│   │   │   └── GetManyAndCountPrototypesEndpoint.cs
│   │   └── models/
│   │       ├── CreatePrototypeModel.cs
│   │       └── GetManyAndCountPrototypesModel.cs
│   ├── hello/                                   # ⚡ Feature: Hello (simple example)
│   │   └── HelloEndpoint.cs
│   └── BaseEndpoint.cs                          # ✅ Shared - Base class para endpoints
├── dtos/                                        # ✅ Shared - DTOs para respuestas
│   ├── UserDto.cs
│   ├── TechnicalStandardDto.cs
│   ├── PrototypeDto.cs
│   └── GetManyAndCountResultDto.cs
├── mappingprofiles/                             # ✅ Shared - AutoMapper profiles
│   ├── MappingProfile.cs
│   ├── UserMappingProfile.cs
│   ├── TechnicalStandardMappingProfile.cs
│   └── PrototypeMappingProfile.cs
├── infrastructure/                              # ✅ Shared - Infraestructura del WebApi
│   ├── authorization/                           # Políticas de autorización
│   │   ├── MustBeApplicationAdministrator.cs
│   │   └── MustBeApplicationUser.cs
│   └── ServiceCollectionExtender.cs             # DI registration extensions
├── Properties/
│   └── InternalsVisibleTo.cs                    # Testing visibility
├── IPrincipalExtender.cs                        # ✅ Shared - Extension methods
└── Program.cs                                   # ✅ Entry point
```

#### Ejemplo Real: CreateUserEndpoint

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
        var command = _mapper.Map<CreateUserUseCase.Command>(request);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();

            // Check for InvalidDomainException
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is InvalidDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return;
            }

            // Check for DuplicatedDomainException
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is DuplicatedDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                return;
            }

            AddError(error?.Message ?? "Unknown error");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            return;
        }

        var userResponse = _mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAtAsync(
            $"/users/{userResponse.User.Id}",
            new[] { userResponse.User.Id },
            userResponse,
            false,
            ct);
    }
}
```

#### Ejemplo Real: CreateUserModel

```csharp
// WebApi/features/users/models/CreateUserModel.cs
namespace hashira.stone.backend.webapi.features.users.models;

using FastEndpoints;
using hashira.stone.backend.webapi.dtos;

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

    public class Response
    {
        /// <summary>
        /// Gets or sets the UserId of the newly created user.
        /// </summary>
        public UserDto User { get; set; } = new UserDto();
    }
}
```

#### Ejemplo Real: UserDto

```csharp
// WebApi/dtos/UserDto.cs
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

    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

**📋 Convenciones WebApi Layer:**
- ✅ Features en carpeta `features/{feature}/` (feature en plural, lowercase)
- ✅ Endpoints en subcarpeta `endpoint/` con sufijo `Endpoint`
- ✅ Models en subcarpeta `models/` con sufijo `Model`
- ✅ DTOs compartidos en carpeta `dtos/` con sufijo `Dto`
- ✅ Cada Model contiene inner classes: `Request` y `Response`
- ✅ Naming consistente: `Create{Entity}Endpoint`, `Get{Entity}Endpoint`
- ✅ AutoMapper profiles en `mappingprofiles/`

---

## Tipos de Features

### Feature CRUD Completo

Feature con operaciones completas: Create, Read, Update, Delete (o parciales).

**Ejemplo: Users**

```
Domain:
  entities/User.cs
  entities/validators/UserValidator.cs
  interfaces/repositories/IUserRepository.cs

Application:
  usecases/users/CreateUserUseCase.cs
  usecases/users/GetUserUseCase.cs
  usecases/users/GetManyAndCountUsersUseCase.cs
  usecases/users/UpdateUserLockUseCase.cs

Infrastructure:
  nhibernate/NHUserRepository.cs
  nhibernate/mappers/UserMapper.cs

WebApi:
  features/users/endpoint/CreateUserEndpoint.cs
  features/users/endpoint/GetUserEndpoint.cs
  features/users/endpoint/GetManyAndCountUsersEndPoint.cs
  features/users/endpoint/UpdateUserLockEndpoint.cs
  features/users/models/CreateUserModel.cs
  features/users/models/GetUserModel.cs
  features/users/models/GetManyAndCountModel.cs
  features/users/models/UpdateUserLockModel.cs
  dtos/UserDto.cs
```

### Feature Read-Only (DAO Pattern)

Feature con solo operaciones de lectura usando DAOs.

**Ejemplo: TechnicalStandards (DAO)**

```
Domain:
  daos/TechnicalStandardDao.cs                      # DAO (no Entity)
  interfaces/repositories/ITechnicalStandardDaoRepository.cs

Application:
  usecases/technicalstandards/GetManyAndCountTechnicalStandardsUseCase.cs

Infrastructure:
  nhibernate/NHTechnicalStandardDaoRepository.cs    # Extends NHReadOnlyRepository
  nhibernate/mappers/TechnicalStandardDaoMapper.cs

WebApi:
  features/technicalstandards/endpoint/GetManyAndCountTechnicalStandardsEndPoint.cs
  features/technicalstandards/models/GetManyAndCountModel.cs
  dtos/TechnicalStandardDto.cs
```

**📋 Características DAO Pattern:**
- ✅ Solo lectura (no Create, Update, Delete)
- ✅ DAO en lugar de Entity (sin validaciones de dominio)
- ✅ Repository extiende `IReadOnlyRepository<TDao, TKey>`
- ✅ Implementación extiende `NHReadOnlyRepository<TDao, TKey>`
- ✅ Optimizado para queries complejas

### Feature Simple (Sin Persistencia)

Feature que no requiere persistencia en BD.

**Ejemplo: Hello**

```
WebApi:
  features/hello/HelloEndpoint.cs                   # Solo endpoint, sin models
```

**📋 Características Feature Simple:**
- ✅ No tiene Domain layer (no entity)
- ✅ No tiene Application layer (no use case)
- ✅ No tiene Infrastructure layer (no repository)
- ✅ Solo WebApi layer (endpoint directo)
- ✅ Útil para health checks, info endpoints, etc.

---

## Feature vs Shared Components

### ¿Cuándo es Shared?

Un componente es **Shared** cuando:
- ✅ Es reutilizable por múltiples features
- ✅ Define contratos/interfaces base
- ✅ Contiene lógica común/helpers
- ✅ Es parte de la infraestructura de la app

**Ejemplos de Shared Components:**

| Componente | Ubicación | Descripción |
|-----------|-----------|-------------|
| `IRepository<T, TKey>` | Domain/interfaces/repositories/ | Interface base para repositorios |
| `IReadOnlyRepository<T, TKey>` | Domain/interfaces/repositories/ | Interface base para read-only |
| `IUnitOfWork` | Domain/interfaces/repositories/ | Unit of Work pattern |
| `AbstractDomainObject` | Domain/entities/ | Base class para entities |
| `InvalidDomainException` | Domain/exceptions/ | Excepción de validación |
| `NHRepository<T, TKey>` | Infrastructure/nhibernate/ | Implementación base repositorio |
| `NHReadOnlyRepository<T, TKey>` | Infrastructure/nhibernate/ | Implementación base read-only |
| `FilterExpressionParser` | Infrastructure/nhibernate/filtering/ | Parser de filtros query string |
| `BaseEndpoint` | WebApi/features/ | Base class para endpoints |
| `GetManyAndCountResultDto<T>` | WebApi/dtos/ | DTO genérico para paginación |

### ¿Cuándo es Feature-Specific?

Un componente es **Feature-Specific** cuando:
- ✅ Es específico de una entidad/módulo de negocio
- ✅ No se reutiliza en otros features
- ✅ Contiene lógica particular del feature

**Ejemplos de Feature-Specific Components:**

| Componente | Ubicación | Descripción |
|-----------|-----------|-------------|
| `User` | Domain/entities/ | Entity del feature Users |
| `UserValidator` | Domain/entities/validators/ | Validador específico User |
| `IUserRepository` | Domain/interfaces/repositories/ | Repository interface específica |
| `CreateUserUseCase` | Application/usecases/users/ | Use case específico |
| `NHUserRepository` | Infrastructure/nhibernate/ | Implementación específica |
| `CreateUserEndpoint` | WebApi/features/users/endpoint/ | Endpoint específico |
| `UserDto` | WebApi/dtos/ | DTO específico para User |

---

## Naming Conventions por Capa

### Domain Layer

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| **Entity** | PascalCase, singular | `User`, `Role`, `TechnicalStandard` |
| **Validator** | `{Entity}Validator` | `UserValidator`, `RoleValidator` |
| **DAO** | `{Entity}Dao` | `TechnicalStandardDao`, `PrototypeDao` |
| **Repository Interface** | `I{Entity}Repository` | `IUserRepository`, `IRoleRepository` |
| **DAO Repository Interface** | `I{Entity}DaoRepository` | `ITechnicalStandardDaoRepository` |
| **Exception** | `{Description}Exception` | `InvalidDomainException`, `DuplicatedDomainException` |

### Application Layer

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| **Use Case** | `{Verb}{Entity}UseCase` | `CreateUserUseCase`, `GetUserUseCase` |
| **Folder** | Plural, lowercase | `users/`, `roles/`, `technicalstandards/` |
| **Command** | Inner class `Command` | `CreateUserUseCase.Command` |
| **Handler** | Inner class `Handler` | `CreateUserUseCase.Handler` |

### Infrastructure Layer

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| **Repository** | `NH{Entity}Repository` | `NHUserRepository`, `NHRoleRepository` |
| **DAO Repository** | `NH{Entity}DaoRepository` | `NHTechnicalStandardDaoRepository` |
| **Mapper** | `{Entity}Mapper` | `UserMapper`, `RoleMapper` |
| **DAO Mapper** | `{Entity}DaoMapper` | `TechnicalStandardDaoMapper` |

### WebApi Layer

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| **Endpoint** | `{Verb}{Entity}Endpoint` | `CreateUserEndpoint`, `GetUserEndpoint` |
| **Model** | `{Verb}{Entity}Model` | `CreateUserModel`, `GetUserModel` |
| **DTO** | `{Entity}Dto` | `UserDto`, `TechnicalStandardDto` |
| **Folder** | Plural, lowercase | `users/`, `roles/`, `prototypes/` |
| **Request** | Inner class `Request` | `CreateUserModel.Request` |
| **Response** | Inner class `Response` | `CreateUserModel.Response` |

---

## Checklist: Nuevo Feature CRUD

Usa este checklist al crear un nuevo feature CRUD completo:

### Domain Layer
- [ ] `entities/{Entity}.cs` creado (PascalCase, singular)
- [ ] `entities/validators/{Entity}Validator.cs` creado
- [ ] Validator implementa `AbstractValidator<{Entity}>`
- [ ] `interfaces/repositories/I{Entity}Repository.cs` creado
- [ ] Repository interface extiende `IRepository<{Entity}, TKey>`
- [ ] `IUnitOfWork.cs` actualizado con propiedad del repositorio

### Application Layer
- [ ] Carpeta `usecases/{entities}/` creada (plural, lowercase)
- [ ] `usecases/{entities}/Create{Entity}UseCase.cs` creado
- [ ] `usecases/{entities}/Get{Entity}UseCase.cs` creado
- [ ] `usecases/{entities}/GetManyAndCount{Entities}UseCase.cs` creado
- [ ] `usecases/{entities}/Update{Entity}UseCase.cs` creado (si aplica)
- [ ] Cada UseCase contiene inner classes: `Command` + `Handler`
- [ ] Handler retorna `Result<T>` de FluentResults

### Infrastructure Layer
- [ ] `nhibernate/NH{Entity}Repository.cs` creado
- [ ] Repository extiende `NHRepository<{Entity}, TKey>`
- [ ] Repository implementa `I{Entity}Repository`
- [ ] `nhibernate/mappers/{Entity}Mapper.cs` creado
- [ ] Mapper extiende `ClassMapping<{Entity}>`
- [ ] UnitOfWork implementación actualizada

### WebApi Layer
- [ ] Carpeta `features/{entities}/endpoint/` creada (plural, lowercase)
- [ ] Carpeta `features/{entities}/models/` creada
- [ ] `features/{entities}/endpoint/Create{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/Get{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/GetManyAndCount{Entities}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/Update{Entity}Endpoint.cs` creado (si aplica)
- [ ] `features/{entities}/models/Create{Entity}Model.cs` creado
- [ ] `features/{entities}/models/Get{Entity}Model.cs` creado
- [ ] `features/{entities}/models/GetManyAndCountModel.cs` creado
- [ ] `features/{entities}/models/Update{Entity}Model.cs` creado (si aplica)
- [ ] Cada Model contiene inner classes: `Request` + `Response`
- [ ] `dtos/{Entity}Dto.cs` creado
- [ ] AutoMapper profile creado o actualizado

### Testing
- [ ] Tests de dominio creados (validators, entity logic)
- [ ] Tests de application creados (use cases)
- [ ] Tests de infrastructure creados (repositories)
- [ ] Tests de webapi creados (endpoints)

---

## Checklist: Nuevo Feature Read-Only (DAO)

Usa este checklist al crear un feature de solo lectura:

### Domain Layer
- [ ] `daos/{Entity}Dao.cs` creado (no validator)
- [ ] `interfaces/repositories/I{Entity}DaoRepository.cs` creado
- [ ] Repository interface extiende `IReadOnlyRepository<{Entity}Dao, TKey>`
- [ ] `IUnitOfWork.cs` actualizado con propiedad del repositorio

### Application Layer
- [ ] Carpeta `usecases/{entities}/` creada
- [ ] `usecases/{entities}/Get{Entity}UseCase.cs` creado
- [ ] `usecases/{entities}/GetManyAndCount{Entities}UseCase.cs` creado (si aplica)

### Infrastructure Layer
- [ ] `nhibernate/NH{Entity}DaoRepository.cs` creado
- [ ] Repository extiende `NHReadOnlyRepository<{Entity}Dao, TKey>`
- [ ] Repository implementa `I{Entity}DaoRepository`
- [ ] `nhibernate/mappers/{Entity}DaoMapper.cs` creado
- [ ] Mapper extiende `ClassMapping<{Entity}Dao>`

### WebApi Layer
- [ ] Carpeta `features/{entities}/endpoint/` creada
- [ ] Carpeta `features/{entities}/models/` creada
- [ ] `features/{entities}/endpoint/Get{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/GetManyAndCount{Entities}Endpoint.cs` creado (si aplica)
- [ ] `features/{entities}/models/Get{Entity}Model.cs` creado
- [ ] `features/{entities}/models/GetManyAndCountModel.cs` creado (si aplica)
- [ ] `dtos/{Entity}Dto.cs` creado

---

## Anti-Patrones a Evitar

### ❌ Mezclar Features en una Carpeta

**Incorrecto:**
```
application/
└── usecases/
    ├── CreateUserUseCase.cs
    ├── CreateRoleUseCase.cs
    ├── GetUserUseCase.cs
    └── GetRoleUseCase.cs
```

**Correcto:**
```
application/
└── usecases/
    ├── users/
    │   ├── CreateUserUseCase.cs
    │   └── GetUserUseCase.cs
    └── roles/
        ├── CreateRoleUseCase.cs
        └── GetRoleUseCase.cs
```

### ❌ Repository en Carpeta de Feature

**Incorrecto:**
```
infrastructure/
└── users/
    └── NHUserRepository.cs
```

**Correcto:**
```
infrastructure/
└── nhibernate/
    ├── NHUserRepository.cs
    └── NHRoleRepository.cs
```

**Razón:** En Infrastructure, los repositorios se agrupan por tecnología (NHibernate), no por feature.

### ❌ DTOs Dentro del Feature

**Incorrecto:**
```
webapi/
└── features/
    └── users/
        ├── endpoint/
        ├── models/
        └── UserDto.cs    # ❌ DTO dentro del feature
```

**Correcto:**
```
webapi/
├── features/
│   └── users/
│       ├── endpoint/
│       └── models/
└── dtos/
    └── UserDto.cs        # ✅ DTOs compartidos
```

**Razón:** DTOs son compartidos entre endpoints y deben estar en carpeta `dtos/`.

### ❌ Validators Fuera de entities/validators

**Incorrecto:**
```
domain/
├── entities/
│   └── User.cs
└── validators/
    └── UserValidator.cs
```

**Correcto:**
```
domain/
└── entities/
    ├── User.cs
    └── validators/
        └── UserValidator.cs
```

**Razón:** Validators están estrechamente ligados a entities y deben estar co-localizados.

---

## Recursos Adicionales

### Guías Relacionadas

- [Naming Conventions](./naming-conventions.md) - Convenciones de nombres detalladas
- [Entity to Endpoint Flow](./entity-to-endpoint-flow.md) - Flujo completo de datos
- [Best Practices](../best-practices/README.md) - Mejores prácticas generales

### Referencias

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/)
- [Feature Folders in ASP.NET Core](https://ardalis.com/feature-folders-in-asp-net-core/)

---

## Resumen

**Principios Clave:**
1. ✅ **Vertical Slicing** - Features atraviesan todas las capas
2. ✅ **Separation by Feature** - Agrupar por feature, no por tipo técnico
3. ✅ **Consistent Naming** - Nombres predecibles y consistentes
4. ✅ **Shared vs Feature-Specific** - Separar componentes compartidos
5. ✅ **Technology Grouping in Infrastructure** - Agrupar por tecnología (NHibernate, etc.)

**Estructura Mental:**

```
Feature = Domain Entity → Use Cases → Repository → Endpoints
        (entities/)   (usecases/)  (nhibernate/)  (features/)
```

Cada feature es auto-contenido y fácilmente localizable siguiendo esta estructura consistente.

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
