# Naming Conventions - Feature Structure

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Esta guía establece las convenciones de nombres para features a través de todas las capas de la aplicación. Define patrones consistentes para entidades, use cases, repositorios, endpoints, DTOs, modelos, y estructura de carpetas.

## Objetivo

Mantener consistencia en nombres de:
- Entidades de dominio y sus validadores
- Use cases (Create, Get, Update, Delete patterns)
- Repositorios e interfaces
- Endpoints y rutas HTTP
- DTOs y Request/Response Models
- Archivos y carpetas
- Plural vs Singular

---

## Tabla de Contenido

1. [Principios Generales](#principios-generales)
2. [Domain Layer](#domain-layer)
3. [Application Layer](#application-layer)
4. [Infrastructure Layer](#infrastructure-layer)
5. [WebApi Layer](#webapi-layer)
6. [Plural vs Singular](#plural-vs-singular)
7. [Consistencia entre Capas](#consistencia-entre-capas)
8. [Casos Especiales](#casos-especiales)

---

## Principios Generales

### Reglas Fundamentales

1. **PascalCase** para clases, interfaces, métodos, propiedades
2. **camelCase** para parámetros, variables locales, campos privados
3. **Singular** para entidades, DTOs, modelos
4. **Plural** para carpetas de features, repositorios en UoW
5. **Nombres descriptivos** que reflejen la responsabilidad
6. **Consistencia** del nombre de entidad a través de todas las capas
7. **Idioma inglés** para todo el código y documentación

### Idioma del Código

**IMPORTANTE:** Todo el código fuente debe estar escrito en **inglés**, incluyendo:

| Elemento | Idioma | Ejemplo Correcto | Ejemplo Incorrecto |
|----------|--------|------------------|-------------------|
| Nombres de clases | English | `User`, `Invoice` | `Usuario`, `Factura` |
| Nombres de métodos | English | `GetByEmail`, `Create` | `ObtenerPorEmail`, `Crear` |
| Nombres de propiedades | English | `Name`, `CreationDate` | `Nombre`, `FechaCreacion` |
| Variables | English | `user`, `totalCount` | `usuario`, `totalConteo` |
| XML Documentation | English | `/// <summary>Gets user by ID</summary>` | `/// <summary>Obtiene usuario por ID</summary>` |
| Comentarios de código | English | `// Validate before save` | `// Validar antes de guardar` |
| Mensajes de error (código) | English | `"User not found"` | `"Usuario no encontrado"` |
| Nombres de archivos | English | `UserValidator.cs` | `ValidadorUsuario.cs` |

**Razones:**

1. **Consistencia** con el ecosistema .NET y librerías externas
2. **Colaboración** - Facilita trabajo con desarrolladores internacionales
3. **Documentación** - IntelliSense y Swagger se generan correctamente
4. **Mantenibilidad** - Código uniforme en todo el proyecto

> **Nota:** Las guías de desarrollo pueden estar en español para facilitar su comprensión, pero el código fuente siempre debe estar en inglés.

### Convención de Sufijos

| Tipo | Sufijo | Ejemplo |
|------|--------|---------|
| Entidad | (ninguno) | `User`, `TechnicalStandard` |
| Validator | `Validator` | `UserValidator` |
| Interface de repositorio | `IRepository` | `IUserRepository` |
| Implementación de repositorio | `NHRepository` | `NHUserRepository` |
| DAO (Read-only) | `Dao` | `PrototypeDao` |
| DAO Repository | `DaoRepository` | `IPrototypeDaoRepository` |
| Use Case | `UseCase` | `CreateUserUseCase` |
| Endpoint | `Endpoint` | `CreateUserEndpoint` |
| DTO | `Dto` | `UserDto` |
| Request/Response Model | `Model` | `CreateUserModel` |
| Exception | `Exception` | `InvalidDomainException` |

---

## Domain Layer

### Entidades

**Patrón:** `{EntityName}` (PascalCase, singular)

```
✅ Correcto:
User
Role
TechnicalStandard
Prototype

❌ Incorrecto:
Users (plural)
user (lowercase)
UserEntity (sufijo redundante)
User_Entity (snake_case)
```

**Ejemplos reales del proyecto:**

```csharp
// Domain/entities/User.cs
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    // ...
}

// Domain/entities/TechnicalStandard.cs
public class TechnicalStandard : AbstractDomainObject
{
    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    // ...
}
```

### Validators

**Patrón:** `{EntityName}Validator` (PascalCase)

```
✅ Correcto:
UserValidator
RoleValidator
TechnicalStandardValidator
PrototypeValidator

❌ Incorrecto:
ValidateUser
UserValidation
Validator_User
```

**Ejemplo real:**

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
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty();
    }
}
```

### DAOs (Data Access Objects)

**Patrón:** `{EntityName}Dao` (PascalCase, singular)

Los DAOs son objetos de solo lectura optimizados para consultas.

```
✅ Correcto:
PrototypeDao
TechnicalStandardDao

❌ Incorrecto:
PrototypesDao (plural)
PrototypeDTO (confusión con DTO)
```

**Ejemplo real:**

```csharp
// Domain/daos/PrototypeDao.cs
namespace hashira.stone.backend.domain.daos;

public class PrototypeDao
{
    public virtual Guid Id { get; set; }
    public virtual string? Number { get; set; }
    public virtual DateTime IssueDate { get; set; }
    public virtual DateTime ExpirationDate { get; set; }
    public virtual string? Status { get; set; }
}
```

### Interfaces de Repositorio

**Patrón:** `I{EntityName}Repository` (PascalCase)

```
✅ Correcto:
IUserRepository
IRoleRepository
ITechnicalStandardRepository
IPrototypeRepository

❌ Incorrecto:
UserRepository (sin I prefix)
IUsersRepository (plural)
IRepositoryUser (orden invertido)
```

**Patrón DAO Repository:** `I{EntityName}DaoRepository`

```
✅ Correcto:
IPrototypeDaoRepository
ITechnicalStandardDaoRepository

❌ Incorrecto:
IPrototypeDaoRepo (abreviado)
IDAOPrototypeRepository (orden invertido)
```

**Ejemplos reales:**

```csharp
// Domain/interfaces/repositories/IUserRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateAsync(string email, string name);
    Task<User?> GetByEmailAsync(string email);
}

// Domain/interfaces/repositories/IPrototypeDaoRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IPrototypeDaoRepository : IReadOnlyRepository<PrototypeDao, Guid>
{
    // Solo lectura, hereda métodos de IReadOnlyRepository
}
```

### Excepciones de Dominio

**Patrón:** `{Purpose}Exception` (PascalCase)

```
✅ Correcto:
InvalidDomainException
DuplicatedDomainException
ResourceNotFoundException

❌ Incorrecto:
DomainInvalidException (orden invertido)
InvalidException (no específico)
```

### Carpetas en Domain

```
Domain/
├── entities/               ✅ Plural
│   ├── User.cs            ✅ Singular
│   ├── Role.cs
│   └── validators/        ✅ Plural
│       ├── UserValidator.cs
│       └── RoleValidator.cs
├── daos/                  ✅ Plural
│   ├── PrototypeDao.cs    ✅ Singular
│   └── TechnicalStandardDao.cs
├── interfaces/            ✅ Plural
│   ├── repositories/      ✅ Plural
│   │   ├── IUserRepository.cs
│   │   └── IPrototypeDaoRepository.cs
│   └── services/          ✅ Plural
│       └── IIdentityService.cs
└── exceptions/            ✅ Plural
    ├── InvalidDomainException.cs
    └── DuplicatedDomainException.cs
```

---

## Application Layer

### Use Cases

**Patrón:** `{Verb}{EntityName}UseCase` (PascalCase)

Verbos comunes:
- `Create` - Crear una nueva entidad
- `Get` - Obtener una entidad por ID o criterio
- `GetManyAndCount` - Obtener múltiples con paginación
- `Update` - Actualizar una entidad existente
- `Delete` - Eliminar una entidad
- `Add` - Agregar relación (ej. AddUsersToRole)
- `Remove` - Remover relación (ej. RemoveUserFromRole)

```
✅ Correcto:
CreateUserUseCase
GetUserUseCase
GetManyAndCountUsersUseCase
UpdateUserLockUseCase
UpdateTechnicalStandardUseCase
AddUsersToRoleUseCase
RemoveUserFromRoleUseCase

❌ Incorrecto:
UserCreateUseCase (orden invertido)
CreateUseCase (no específico)
CreateUserCase (falta UseCase)
Create_User_UseCase (snake_case)
```

**Ejemplos reales:**

```csharp
// Application/usecases/users/CreateUserUseCase.cs
namespace hashira.stone.backend.application.usecases.users;

using FastEndpoints;
using FluentResults;

public abstract class CreateUserUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        // Implementation...
    }
}

// Application/usecases/users/GetManyAndCountUsersUseCase.cs
public abstract class GetManyAndCountUsersUseCase
{
    public class Command : ICommand<GetManyAndCountResult<User>>
    {
        public string? Query { get; set; }
    }

    public class Handler : ICommandHandler<Command, GetManyAndCountResult<User>>
    {
        // Implementation...
    }
}
```

### Inner Classes en Use Cases

**Patrón:** `Command` y `Handler` (nombres fijos)

```csharp
✅ Correcto:
public abstract class CreateUserUseCase
{
    public class Command : ICommand<Result<User>> { }
    public class Handler : ICommandHandler<Command, Result<User>> { }
}

❌ Incorrecto:
public class CreateUserCommand { }  // No como clase separada
public class CreateUserHandler { }  // No como clase separada
```

### Carpetas en Application

```
Application/
└── usecases/                      ✅ Plural
    ├── users/                     ✅ Plural (feature)
    │   ├── CreateUserUseCase.cs   ✅ Singular entity
    │   ├── GetUserUseCase.cs
    │   ├── GetManyAndCountUsersUseCase.cs  ✅ Plural cuando aplica
    │   └── UpdateUserLockUseCase.cs
    ├── roles/                     ✅ Plural (feature)
    │   ├── AddUsersToRoleUseCase.cs
    │   └── RemoveUserFromRoleUseCase.cs
    └── technicalstandards/        ✅ Plural (feature)
        ├── CreateTechnicalStandardUseCase.cs
        └── UpdateTechnicalStandardUseCase.cs
```

**Nota:** Las carpetas de feature van en **plural minúscula** (ej. `users/`, `roles/`, `technicalstandards/`).

---

## Infrastructure Layer

### Implementación de Repositorios

**Patrón:** `NH{EntityName}Repository` (PascalCase)

El prefijo `NH` indica que es implementación de NHibernate.

```
✅ Correcto:
NHUserRepository
NHRoleRepository
NHTechnicalStandardRepository
NHPrototypeRepository

❌ Incorrecto:
UserRepository (sin prefijo de tecnología)
NhibernateUserRepository (nombre completo)
UserNHRepository (orden invertido)
```

**Patrón DAO Repository:** `NH{EntityName}DaoRepository`

```
✅ Correcto:
NHPrototypeDaoRepository
NHTechnicalStandardDaoRepository

❌ Incorrecto:
NHPrototypeDAORepository (DAO en mayúsculas)
PrototypeDaoRepository (sin prefijo NH)
```

**Ejemplos reales:**

```csharp
// Infrastructure/nhibernate/NHUserRepository.cs
namespace hashira.stone.backend.infrastructure.nhibernate;

using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;

public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
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

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}

// Infrastructure/nhibernate/NHPrototypeDaoRepository.cs
public class NHPrototypeDaoRepository(ISession session, IServiceProvider serviceProvider)
    : NHReadOnlyRepository<PrototypeDao, Guid>(session, serviceProvider), IPrototypeDaoRepository
{
    // Solo lectura, no hay métodos adicionales
}
```

### Repositorios Base

**Patrón:** `NH{Purpose}Repository` (PascalCase)

```
✅ Correcto:
NHRepository<TEntity, TId>          // Repositorio base con CRUD
NHReadOnlyRepository<TEntity, TId>  // Repositorio base de solo lectura

❌ Incorrecto:
NHBaseRepository
NHGenericRepository
```

### Unit of Work

**Patrón:** `NH{Pattern}` (PascalCase)

```
✅ Correcto:
NHUnitOfWork
NHSessionFactory

❌ Incorrecto:
UnitOfWork (sin prefijo)
NHibernateUnitOfWork (nombre completo)
```

**Propiedades en UnitOfWork (plural):**

```csharp
// Infrastructure/nhibernate/NHUnitOfWork.cs
public class NHUnitOfWork : IUnitOfWork
{
    public IUserRepository Users { get; }              // ✅ Plural
    public IRoleRepository Roles { get; }              // ✅ Plural
    public ITechnicalStandardRepository TechnicalStandards { get; }  // ✅ Plural
    public IPrototypeDaoRepository PrototypesDao { get; }            // ✅ Plural
}
```

### Carpetas en Infrastructure

```
Infrastructure/
└── nhibernate/                              ✅ Lowercase
    ├── NHRepository.cs                      ✅ Base repository
    ├── NHReadOnlyRepository.cs              ✅ Base read-only
    ├── NHUserRepository.cs                  ✅ Implementación
    ├── NHPrototypeDaoRepository.cs          ✅ DAO repository
    ├── NHUnitOfWork.cs                      ✅ Unit of Work
    └── mappings/                            ✅ Plural
        ├── UserMapping.cs
        └── PrototypeMapping.cs
```

---

## WebApi Layer

### Endpoints

**Patrón:** `{Verb}{EntityName}Endpoint` (PascalCase)

```
✅ Correcto:
CreateUserEndpoint
GetUserEndpoint
GetManyAndCountUsersEndpoint
UpdateUserLockEndpoint
UpdateTechnicalStandardEndpoint
AddUsersToRoleEndpoint
RemoveUserFromRoleEndpoint

❌ Incorrecto:
UserCreateEndpoint (orden invertido)
CreateEndpoint (no específico)
CreateUserAPI (sufijo incorrecto)
Create_User_Endpoint (snake_case)
```

**Ejemplos reales:**

```csharp
// WebApi/features/users/endpoint/CreateUserEndpoint.cs
namespace hashira.stone.backend.webapi.features.users.endpoint;

using FastEndpoints;
using hashira.stone.backend.webapi.features.users.models;
using hashira.stone.backend.application.usecases.users;

public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    public override void Configure()
    {
        Post("/users");  // ✅ Ruta en plural
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
    {
        var command = _mapper.Map<CreateUserUseCase.Command>(request);
        var result = await command.ExecuteAsync(ct);
        // ...
    }
}
```

### Rutas HTTP

**Patrón:** `/resource` en **plural minúscula**

```
✅ Correcto:
POST   /users
GET    /users/{id}
GET    /users
PUT    /users/{id}
DELETE /users/{id}

POST   /roles
GET    /technical-standards    ✅ kebab-case para múltiples palabras

❌ Incorrecto:
POST /user (singular)
POST /Users (mayúscula)
POST /create-user (verbo en ruta)
GET  /technicalStandards (camelCase)
GET  /technical_standards (snake_case)
```

**Casos especiales:**

```
✅ Correcto:
POST /roles/{roleName}/users/{userName}     ✅ Rutas anidadas en plural
GET  /users/current                          ✅ Acción especial al final

❌ Incorrecto:
POST /role/{roleName}/user/{userName} (singular)
GET  /current-user (verbo al inicio)
```

### Request/Response Models

**Patrón:** `{Verb}{EntityName}Model` (PascalCase)

```
✅ Correcto:
CreateUserModel
GetUserModel
GetManyAndCountModel
UpdateUserLockModel
UpdateTechnicalStandardModel
UserRoleAssignmentModel  ✅ Nombre descriptivo para casos especiales

❌ Incorrecto:
UserCreateModel (orden invertido)
CreateModel (no específico)
CreateUserRequest (no usar Request como sufijo de clase)
```

**Inner classes:** `Request` y `Response` (nombres fijos)

```csharp
// WebApi/features/users/models/CreateUserModel.cs
namespace hashira.stone.backend.webapi.features.users.models;

public class CreateUserModel
{
    public class Request  // ✅ Siempre "Request"
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Response  // ✅ Siempre "Response"
    {
        public UserDto User { get; set; } = new UserDto();
    }
}

// WebApi/features/roles/models/UserRoleAssignmentModel.cs
public class UserRoleAssignmentModel  // ✅ Nombre descriptivo
{
    public class Request
    {
        [FromRoute]
        public string RoleName { get; set; } = string.Empty;

        [FromRoute]
        public string UserName { get; set; } = string.Empty;
    }

    public class Response
    {
        public bool Success { get; set; }
    }
}
```

### DTOs (Data Transfer Objects)

**Patrón:** `{EntityName}Dto` (PascalCase, singular)

```
✅ Correcto:
UserDto
RoleDto
PrototypeDto
TechnicalStandardDto
GetManyAndCountResultDto<T>  ✅ DTO genérico

❌ Incorrecto:
UsersDto (plural)
UserDTO (DTO en mayúsculas)
DtoUser (orden invertido)
UserDataTransferObject (nombre completo)
```

**Ejemplos reales:**

```csharp
// WebApi/dtos/UserDto.cs
namespace hashira.stone.backend.webapi.dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

// WebApi/dtos/GetManyAndCountResultDto.cs
public class GetManyAndCountResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Count { get; set; }
}
```

### AutoMapper Profiles

**Patrón:** `{EntityName}MappingProfile` (PascalCase)

```
✅ Correcto:
UserMappingProfile
TechnicalStandardMappingProfile

❌ Incorrecto:
UserProfile (no específico)
MappingProfileUser (orden invertido)
UserMapper (confusión con IMapper)
```

### Carpetas en WebApi

```
WebApi/
├── features/                                  ✅ Plural
│   ├── users/                                 ✅ Plural (feature)
│   │   ├── endpoint/                          ✅ Singular
│   │   │   ├── CreateUserEndpoint.cs
│   │   │   ├── GetUserEndpoint.cs
│   │   │   └── UpdateUserLockEndpoint.cs
│   │   └── models/                            ✅ Plural
│   │       ├── CreateUserModel.cs
│   │       └── GetUserModel.cs
│   ├── roles/                                 ✅ Plural (feature)
│   │   ├── endpoint/
│   │   │   ├── AddUsersToRoleEndpoint.cs
│   │   │   └── RemoveUserFromRoleEndpoint.cs
│   │   └── models/
│   │       └── UserRoleAssignmentModel.cs
│   └── technicalstandards/                    ✅ Plural lowercase
│       ├── endpoint/
│       └── models/
├── dtos/                                      ✅ Plural
│   ├── UserDto.cs
│   ├── PrototypeDto.cs
│   └── GetManyAndCountResultDto.cs
└── mappingprofiles/                           ✅ Plural lowercase
    ├── UserMappingProfile.cs
    └── TechnicalStandardMappingProfile.cs
```

---

## Plural vs Singular

### Tabla de Referencia

| Elemento | Singular / Plural | Ejemplo |
|----------|-------------------|---------|
| **Clase de entidad** | Singular | `User`, `TechnicalStandard` |
| **Validator** | Singular | `UserValidator` |
| **DAO** | Singular | `PrototypeDao` |
| **Interface de repositorio** | Singular | `IUserRepository` |
| **Implementación de repositorio** | Singular | `NHUserRepository` |
| **Use Case** | Singular (excepto GetManyAndCount) | `CreateUserUseCase` |
| **Endpoint** | Singular (excepto GetManyAndCount) | `CreateUserEndpoint` |
| **Model** | Singular | `CreateUserModel` |
| **DTO** | Singular | `UserDto` |
| **Carpeta de feature** | Plural | `users/`, `roles/` |
| **Carpeta de validators** | Plural | `validators/` |
| **Carpeta de endpoints** | Singular | `endpoint/` |
| **Carpeta de models** | Plural | `models/` |
| **Carpeta de DTOs** | Plural | `dtos/` |
| **Propiedades en UnitOfWork** | Plural | `Users`, `Roles` |
| **Rutas HTTP** | Plural | `/users`, `/roles` |

### GetManyAndCount - Caso Especial

Cuando el use case o endpoint obtiene **múltiples** entidades, usar plural:

```
✅ Correcto:
GetManyAndCountUsersUseCase     ✅ Plural: retorna múltiples Users
GetManyAndCountUsersEndpoint
GetManyAndCountPrototypesUseCase

❌ Incorrecto:
GetManyAndCountUserUseCase (singular)
GetUsersUseCase (no sigue patrón GetManyAndCount)
```

### Relaciones - Casos Especiales

Cuando un use case opera sobre **relaciones**, usar plural para colecciones:

```
✅ Correcto:
AddUsersToRoleUseCase          ✅ Plural: múltiples Users
AddUsersToRoleEndpoint
RemoveUserFromRoleUseCase      ✅ Singular: un User

❌ Incorrecto:
AddUserToRoleUseCase (si agrega múltiples)
RemoveUsersFromRoleUseCase (si elimina uno solo)
```

---

## Consistencia entre Capas

### Flujo de Nombre: Entity → Endpoint

Para una entidad `User`:

```
Domain Layer:
├── User.cs                          ✅ Entidad
├── UserValidator.cs                 ✅ Validator
├── IUserRepository.cs               ✅ Interface

Application Layer:
├── CreateUserUseCase.cs             ✅ Use case
├── GetUserUseCase.cs
└── GetManyAndCountUsersUseCase.cs   ✅ Plural cuando aplica

Infrastructure Layer:
├── NHUserRepository.cs              ✅ Implementación

WebApi Layer:
├── CreateUserEndpoint.cs            ✅ Endpoint
├── CreateUserModel.cs               ✅ Model
└── UserDto.cs                       ✅ DTO

HTTP:
POST /users                          ✅ Ruta plural
```

### Flujo para TechnicalStandard

```
Domain Layer:
├── TechnicalStandard.cs
├── TechnicalStandardValidator.cs
├── ITechnicalStandardRepository.cs
├── TechnicalStandardDao.cs          ✅ DAO para lectura
├── ITechnicalStandardDaoRepository.cs

Application Layer:
├── CreateTechnicalStandardUseCase.cs
├── UpdateTechnicalStandardUseCase.cs
└── GetManyAndCountTechnicalStandardsUseCase.cs  ✅ Plural

Infrastructure Layer:
├── NHTechnicalStandardRepository.cs
└── NHTechnicalStandardDaoRepository.cs  ✅ DAO repository

WebApi Layer:
├── CreateTechnicalStandardEndpoint.cs
├── UpdateTechnicalStandardEndpoint.cs
├── CreateTechnicalStandardModel.cs
└── TechnicalStandardDto.cs

HTTP:
POST /technical-standards            ✅ kebab-case plural
```

---

## Casos Especiales

### Entidades con Múltiples Palabras

**Patrón:** PascalCase sin separadores

```
✅ Correcto:
TechnicalStandard
TechnicalStandardValidator
ITechnicalStandardRepository
NHTechnicalStandardRepository
CreateTechnicalStandardUseCase
TechnicalStandardDto

❌ Incorrecto:
Technical_Standard (snake_case)
Technicalstandard (no distinguir palabras)
TechnicalStandards (plural en clase)
```

**Carpetas y rutas:** lowercase con separador

```
✅ Correcto:
Carpeta: technicalstandards/            ✅ Plural lowercase sin separador
Ruta:    /technical-standards           ✅ Plural kebab-case

❌ Incorrecto:
Carpeta: TechnicalStandards/
Carpeta: technical-standards/
Ruta:    /technicalStandards
Ruta:    /technical_standards
```

### DAOs (Data Access Objects)

Los DAOs se usan para **consultas de solo lectura** optimizadas:

```
✅ Correcto:
PrototypeDao                     ✅ Entidad DAO
IPrototypeDaoRepository          ✅ Interface
NHPrototypeDaoRepository         ✅ Implementación

❌ Incorrecto:
PrototypeDTO (confusión con DTO de WebApi)
PrototypeReadOnly
PrototypeView
```

### Acciones Especiales (No CRUD)

Para use cases que no son CRUD estándar, usar verbo descriptivo:

```
✅ Correcto:
UpdateUserLockUseCase           ✅ Acción específica: lock user
AddUsersToRoleUseCase           ✅ Acción de relación
RemoveUserFromRoleUseCase
ActivatePrototypeUseCase
DeactivateTechnicalStandardUseCase

❌ Incorrecto:
LockUserUseCase (verbo sin Update/Set/Add/Remove)
UserLockUpdateUseCase (orden invertido)
UpdateLockUserUseCase (orden confuso)
```

### Endpoints Especiales

```
✅ Correcto:
GetCurrentUserEndpoint          ✅ Acción especial
GET /users/current

RefreshTokenEndpoint
POST /auth/refresh-token

❌ Incorrecto:
CurrentUserEndpoint (sin verbo)
GetUserCurrentEndpoint (orden confuso)
```

### Genéricos

Usar `T` para tipos genéricos:

```csharp
✅ Correcto:
public interface IRepository<TEntity, TId> { }
public class NHRepository<TEntity, TId> { }
public class GetManyAndCountResultDto<T> { }

❌ Incorrecto:
public interface IRepository<Entity, Id> { }
public class GetManyAndCountResultDto<TDto> { }  // Redundante
```

---

## Checklist: Verificar Naming Conventions

### Al crear nueva entidad

- [ ] Entidad en singular PascalCase (ej. `User`)
- [ ] Validator con sufijo `Validator` (ej. `UserValidator`)
- [ ] Interface `I{Entity}Repository`
- [ ] Implementación `NH{Entity}Repository`
- [ ] Carpeta de feature en plural lowercase (ej. `users/`)
- [ ] Propiedad en UnitOfWork en plural (ej. `Users`)

### Al crear nuevo Use Case

- [ ] Patrón `{Verb}{Entity}UseCase`
- [ ] Verbo describe acción: Create, Get, Update, Delete, Add, Remove
- [ ] Plural solo para GetManyAndCount o colecciones
- [ ] Inner classes `Command` y `Handler`
- [ ] Carpeta `usecases/{feature}/` en plural lowercase

### Al crear nuevo Endpoint

- [ ] Patrón `{Verb}{Entity}Endpoint`
- [ ] Nombre coincide con Use Case (ej. `CreateUserUseCase` → `CreateUserEndpoint`)
- [ ] Ruta HTTP en plural kebab-case (ej. `/users`, `/technical-standards`)
- [ ] Model con patrón `{Verb}{Entity}Model`
- [ ] Inner classes `Request` y `Response`
- [ ] Carpeta `features/{feature}/endpoint/`

### Al crear nuevo DTO

- [ ] Patrón `{Entity}Dto` (singular)
- [ ] Sufijo `Dto` (no `DTO`)
- [ ] Ubicado en `WebApi/dtos/`
- [ ] AutoMapper profile con patrón `{Entity}MappingProfile`

### Consistencia general

- [ ] Mismo nombre de entidad a través de todas las capas
- [ ] PascalCase para clases, interfaces, métodos
- [ ] camelCase para parámetros y variables
- [ ] Plural para carpetas de features y rutas HTTP
- [ ] Singular para clases de entidad, DTO, Model

---

## Recursos Adicionales

### Guías Relacionadas

- [Folder Organization](./folder-organization.md) - Estructura de carpetas
- [Entity to Endpoint Flow](./entity-to-endpoint-flow.md) - Flujo completo
- [Best Practices - Code Organization](../best-practices/code-organization.md) - Organización de código

### Stack Tecnológico

- **.NET 9.0** - Framework
- **C# 13** - Lenguaje (file-scoped namespaces)
- **FastEndpoints 7.0** - REPR pattern
- **FluentResults 4.0** - Result pattern
- **NHibernate 5.5** - ORM

---

## Conclusión

**Principios Clave:**

1. ✅ **Consistencia** - Mismo nombre de entidad a través de todas las capas
2. ✅ **Singular para clases** - Entidades, DTOs, Models siempre singular
3. ✅ **Plural para colecciones** - Carpetas de features, rutas HTTP, propiedades UoW
4. ✅ **Sufijos descriptivos** - Validator, Repository, UseCase, Endpoint, Dto
5. ✅ **PascalCase** - Clases, interfaces, métodos, propiedades
6. ✅ **kebab-case** - Rutas HTTP con múltiples palabras

**Flujo Mental para Naming:**

```
Entidad: User (singular PascalCase)
           ↓
Domain:    User, UserValidator, IUserRepository
           ↓
Application: CreateUserUseCase (singular)
           ↓
Infrastructure: NHUserRepository (singular)
           ↓
WebApi:    CreateUserEndpoint, CreateUserModel, UserDto (singular)
           ↓
HTTP:      POST /users (plural kebab-case)
```

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
