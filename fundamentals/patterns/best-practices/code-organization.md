# Code Organization

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-13

---

## Descripción

La organización consistente del código es fundamental para mantener proyectos .NET escalables y mantenibles. Esta guía establece las convenciones de organización de código utilizadas en aplicaciones .NET con Clean Architecture, abarcando desde la estructura de namespaces hasta la organización de archivos y carpetas.

Una organización de código bien definida:
- **Facilita la navegación** del proyecto
- **Mejora la legibilidad** y comprensión del código
- **Reduce el tiempo** de onboarding de nuevos desarrolladores
- **Previene inconsistencias** entre diferentes partes del proyecto
- **Facilita el mantenimiento** a largo plazo

---

## 1. File-Scoped Namespaces (C# 10+)

### ¿Qué son?

Desde C# 10, podemos declarar namespaces a nivel de archivo, eliminando un nivel de indentación innecesario. Esta característica simplifica el código y mejora la legibilidad.

### ✅ Usar File-Scoped Namespaces

```csharp
namespace Domain.Entities;

/// <summary>
/// Representa un usuario del sistema
/// </summary>
public class User
{
    public virtual Guid Id { get; protected set; }
    public virtual string Email { get; set; }
    public virtual string FullName { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual DateTime CreatedAt { get; protected set; }

    public User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}
```

### ❌ Evitar Block-Scoped Namespaces (Estilo Antiguo)

```csharp
namespace Domain.Entities
{
    /// <summary>
    /// Representa un usuario del sistema
    /// </summary>
    public class User
    {
        public virtual Guid Id { get; protected set; }
        public virtual string Email { get; set; }
        public virtual string FullName { get; set; }
        public virtual bool IsActive { get; set; }
        public virtual DateTime CreatedAt { get; protected set; }

        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }
}
// ❌ Nivel de indentación innecesario en toda la clase
```

### Ventajas de File-Scoped Namespaces

1. **Menos indentación**: Todo el código del archivo tiene un nivel menos de indentación
2. **Más espacio horizontal**: Especialmente útil con límites de caracteres por línea
3. **Más limpio**: Elimina llaves innecesarias
4. **Estándar moderno**: Es el estilo recomendado para C# 10+

---

## 2. Namespace Organization por Capa

### Convención de Namespaces

Los namespaces deben reflejar la capa de Clean Architecture y la funcionalidad específica:

```
{ProyectoRaiz}.{Capa}.{Feature/Funcionalidad}
```

### Domain Layer

**Convención**: `Domain.{Funcionalidad}`

```csharp
// Entidades
namespace Domain.Entities;
namespace Domain.Entities.Validators;

// Value Objects
namespace Domain.ValueObjects;

// Interfaces
namespace Domain.Interfaces.Repositories;
namespace Domain.Interfaces.Services;

// DAOs (Data Access Objects)
namespace Domain.Daos;

// Exceptions
namespace Domain.Exceptions;

// Enums
namespace Domain.Enums;
```

**Ejemplo completo**:

```csharp
// Domain/Entities/User.cs
namespace Domain.Entities;

public class User
{
    public virtual Guid Id { get; protected set; }
    public virtual string Email { get; set; }
    public virtual UserRole Role { get; set; }
}

// Domain/Entities/Validators/UserValidator.cs
namespace Domain.Entities.Validators;

using FluentValidation;
using Domain.Entities;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

// Domain/Enums/UserRole.cs
namespace Domain.Enums;

public enum UserRole
{
    User = 1,
    Admin = 2,
    SuperAdmin = 3
}
```

### Application Layer

**Convención**: `Application.{Funcionalidad}.{Feature}`

```csharp
// Use Cases
namespace Application.UseCases.Users;
namespace Application.UseCases.Products;
namespace Application.UseCases.Orders;

// DTOs internos
namespace Application.Dtos;

// Interfaces de servicios de aplicación
namespace Application.Interfaces;

// Mappers
namespace Application.Mappers;

// Validators de comandos
namespace Application.Validators;
```

**Ejemplo completo**:

```csharp
// Application/UseCases/Users/CreateUserUseCase.cs
namespace Application.UseCases.Users;

using FluentResults;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public record CreateUserCommand(string Email, string FullName) : IRequest<Result<User>>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<User>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName
        };

        await _unitOfWork.Users.SaveOrUpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        return Result.Ok(user);
    }
}
```

### Infrastructure Layer

**Convención**: `Infrastructure.{Tecnología}.{Funcionalidad}`

```csharp
// NHibernate
namespace Infrastructure.NHibernate;
namespace Infrastructure.NHibernate.Mappers;
namespace Infrastructure.NHibernate.Repositories;

// Entity Framework
namespace Infrastructure.EntityFramework;
namespace Infrastructure.EntityFramework.Configurations;

// Servicios externos
namespace Infrastructure.ExternalServices.Email;
namespace Infrastructure.ExternalServices.Storage;

// Caching
namespace Infrastructure.Caching;
```

**Ejemplo completo**:

```csharp
// Infrastructure/NHibernate/NHUserRepository.cs
namespace Infrastructure.NHibernate;

using Domain.Entities;
using Domain.Interfaces.Repositories;
using NHibernate;

public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public NHUserRepository(ISession session) : base(session)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await Session.Query<User>()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(ct);
    }
}

// Infrastructure/NHibernate/Mappers/UserMapper.cs
namespace Infrastructure.NHibernate.Mappers;

using Domain.Entities;
using FluentNHibernate.Mapping;

public class UserMapper : ClassMap<User>
{
    public UserMapper()
    {
        Table("users");

        Id(x => x.Id).GeneratedBy.GuidComb().Column("id");
        Map(x => x.Email).Column("email").Not.Nullable().Length(255);
        Map(x => x.FullName).Column("full_name").Not.Nullable().Length(255);
        Map(x => x.IsActive).Column("is_active").Not.Nullable();
        Map(x => x.CreatedAt).Column("created_at").Not.Nullable();
    }
}
```

### WebApi Layer

**Convención**: `WebApi.{Funcionalidad}.{Feature}`

```csharp
// Features (Endpoints)
namespace WebApi.Features.Users.Endpoint;
namespace WebApi.Features.Products.Endpoint;

// Models (Request/Response)
namespace WebApi.Features.Users.Models;
namespace WebApi.Features.Products.Models;

// DTOs
namespace WebApi.Dtos;

// Middleware
namespace WebApi.Middleware;

// Filters
namespace WebApi.Filters;

// Extensions
namespace WebApi.Extensions;
```

**Ejemplo completo**:

```csharp
// WebApi/Features/Users/Endpoint/CreateUserEndpoint.cs
namespace WebApi.Features.Users.Endpoint;

using FastEndpoints;
using MediatR;
using Application.UseCases.Users;
using WebApi.Features.Users.Models;

public class CreateUserEndpoint : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly IMediator _mediator;

    public CreateUserEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
    {
        var command = new CreateUserCommand(req.Email, req.FullName);
        var result = await _mediator.Send(command, ct);

        if (result.IsFailed)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(new CreateUserModel.Response(result.Value.Id), ct);
    }
}

// WebApi/Features/Users/Models/CreateUserModel.cs
namespace WebApi.Features.Users.Models;

public static class CreateUserModel
{
    public record Request(string Email, string FullName);
    public record Response(Guid UserId);
}
```

---

## 3. Using Directives Order

### Convención de Orden

Los `using` deben estar organizados en el siguiente orden:

1. **System namespaces** (orden alfabético)
2. **Línea en blanco**
3. **Third-party namespaces** (orden alfabético)
4. **Línea en blanco**
5. **Project namespaces** (orden alfabético por capa)

### ✅ Orden Correcto de Usings

```csharp
namespace Application.UseCases.Users;

// 1. System namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// 2. Third-party namespaces
using FluentResults;
using FluentValidation;
using MediatR;
using NHibernate;
using NHibernate.Linq;

// 3. Project namespaces (por capa)
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;

public record CreateUserCommand(string Email, string FullName) : IRequest<Result<User>>;
```

### ❌ Orden Incorrecto de Usings

```csharp
namespace Application.UseCases.Users;

// ❌ Mezclados sin orden
using Domain.Entities;
using MediatR;
using System.Threading.Tasks;
using FluentResults;
using Domain.Interfaces.Repositories;
using System;
using NHibernate;

public record CreateUserCommand(string Email, string FullName) : IRequest<Result<User>>;
```

### Global Usings (C# 10+)

Para usings que se utilizan en todo el proyecto, usar `global using`:

```csharp
// GlobalUsings.cs (en la raíz de cada proyecto)
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
```

### Implicit Usings

En el `.csproj`, habilitar implicit usings para reducir boilerplate:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## 4. Naming Conventions

### Convenciones Generales

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| **Namespaces** | PascalCase | `Domain.Entities` |
| **Classes** | PascalCase | `User`, `UserValidator` |
| **Interfaces** | IPascalCase | `IUserRepository`, `IUnitOfWork` |
| **Methods** | PascalCase | `GetUserById`, `CreateUser` |
| **Properties** | PascalCase | `Email`, `FullName` |
| **Fields (private)** | _camelCase | `_unitOfWork`, `_session` |
| **Parameters** | camelCase | `userId`, `email` |
| **Local variables** | camelCase | `user`, `result` |
| **Constants** | PascalCase | `MaxRetries`, `DefaultTimeout` |
| **Enums** | PascalCase | `UserRole`, `OrderStatus` |
| **Enum values** | PascalCase | `Active`, `Inactive` |
| **Records** | PascalCase | `CreateUserCommand` |
| **Folders** | kebab-case | `use-cases`, `users` |

### ✅ Naming Correcto

```csharp
namespace Application.UseCases.Users;

using Domain.Entities;
using Domain.Interfaces.Repositories;

// ✅ Interface con prefijo I
public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId);
}

// ✅ Clase en PascalCase
public class UserService : IUserService
{
    // ✅ Field privado con _camelCase
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    // ✅ Constante en PascalCase
    private const int MaxRetries = 3;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ✅ Método en PascalCase
    // ✅ Parámetro en camelCase
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        // ✅ Variable local en camelCase
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", userId);
            return null;
        }

        return user;
    }
}
```

### ❌ Naming Incorrecto

```csharp
namespace Application.UseCases.Users;

// ❌ Interface sin prefijo I
public interface UserService
{
    Task<User?> getUserById(Guid userId);
}

// ❌ Clase en camelCase
public class userService : UserService
{
    // ❌ Field sin underscore
    private readonly IUnitOfWork unitOfWork;

    // ❌ Constante en SCREAMING_CASE
    private const int MAX_RETRIES = 3;

    public userService(IUnitOfWork UnitOfWork)
    {
        // ❌ Parámetro en PascalCase
        unitOfWork = UnitOfWork;
    }

    // ❌ Método en camelCase
    public async Task<User?> getUserById(Guid UserId)
    {
        // ❌ Variable en PascalCase
        var User = await unitOfWork.Users.GetByIdAsync(UserId);
        return User;
    }
}
```

### Naming por Tipo de Archivo

| Tipo de Archivo | Patrón | Ejemplo |
|-----------------|--------|---------|
| **Entity** | `{Entity}.cs` | `User.cs`, `Product.cs` |
| **Validator** | `{Entity}Validator.cs` | `UserValidator.cs` |
| **Repository Interface** | `I{Entity}Repository.cs` | `IUserRepository.cs` |
| **Repository Impl** | `NH{Entity}Repository.cs` | `NHUserRepository.cs` |
| **Mapper** | `{Entity}Mapper.cs` | `UserMapper.cs` |
| **DAO** | `{Entity}Dao.cs` | `UserDao.cs` |
| **UseCase** | `{Action}{Entity}UseCase.cs` | `CreateUserUseCase.cs` |
| **Endpoint** | `{Action}{Entity}Endpoint.cs` | `CreateUserEndpoint.cs` |
| **Model** | `{Action}{Entity}Model.cs` | `CreateUserModel.cs` |
| **DTO** | `{Entity}Dto.cs` | `UserDto.cs` |

---

## 5. File Organization

### Un Archivo, Una Responsabilidad

Cada archivo debe contener **una sola clase, interface, record o enum principal**.

### ✅ Organización Correcta de Archivos

```
Domain/
├── Entities/
│   ├── User.cs                    # Solo clase User
│   ├── Product.cs                 # Solo clase Product
│   └── Validators/
│       ├── UserValidator.cs       # Solo UserValidator
│       └── ProductValidator.cs    # Solo ProductValidator
├── Enums/
│   ├── UserRole.cs               # Solo enum UserRole
│   └── OrderStatus.cs            # Solo enum OrderStatus
└── Interfaces/
    └── Repositories/
        ├── IUserRepository.cs     # Solo IUserRepository
        └── IProductRepository.cs  # Solo IProductRepository
```

### ❌ Múltiples Clases en un Archivo

```csharp
// ❌ NO hacer esto - múltiples entidades en un archivo
namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Excepción: Clases Anidadas Relacionadas

Es aceptable tener clases anidadas si están **fuertemente relacionadas**:

```csharp
// ✅ Aceptable - Request/Response en el mismo archivo
namespace WebApi.Features.Users.Models;

public static class CreateUserModel
{
    public record Request(string Email, string FullName);
    public record Response(Guid UserId);
}
```

```csharp
// ✅ Aceptable - Command/Handler en el mismo archivo
namespace Application.UseCases.Users;

public record CreateUserCommand(string Email) : IRequest<Result<User>>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<User>>
{
    // Implementation...
}
```

---

## 6. Folder Organization por Feature

### Estructura Vertical por Feature

Organizar archivos agrupados por **feature/funcionalidad** (vertical slicing), no por tipo técnico.

### ✅ Organización por Feature (Recomendado)

```
Application/
└── UseCases/
    ├── Users/                              # Feature: Users
    │   ├── CreateUserUseCase.cs
    │   ├── GetUserUseCase.cs
    │   ├── GetManyAndCountUsersUseCase.cs
    │   └── UpdateUserUseCase.cs
    ├── Products/                           # Feature: Products
    │   ├── CreateProductUseCase.cs
    │   ├── GetProductUseCase.cs
    │   └── UpdateProductUseCase.cs
    └── Orders/                             # Feature: Orders
        ├── CreateOrderUseCase.cs
        ├── GetOrderUseCase.cs
        └── CancelOrderUseCase.cs
```

**Ventajas**:
- Todo relacionado con "Users" está junto
- Fácil encontrar código relacionado con un feature
- Facilita agregar/eliminar features completos
- Mejor para equipos trabajando en features diferentes

### ❌ Organización por Tipo Técnico (Horizontal)

```
Application/
└── UseCases/
    ├── Create/                            # ❌ Agrupado por acción
    │   ├── CreateUserUseCase.cs
    │   ├── CreateProductUseCase.cs
    │   └── CreateOrderUseCase.cs
    ├── Get/
    │   ├── GetUserUseCase.cs
    │   ├── GetProductUseCase.cs
    │   └── GetOrderUseCase.cs
    └── Update/
        ├── UpdateUserUseCase.cs
        ├── UpdateProductUseCase.cs
        └── UpdateOrderUseCase.cs
```

**Desventajas**:
- Código relacionado con "Users" está esparcido
- Difícil seguir el flujo de un feature
- Acoplamiento accidental entre features

---

## 7. Project Structure Template

### Estructura Completa de un Proyecto

```
solution/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Product.cs
│   │   │   └── Validators/
│   │   │       ├── UserValidator.cs
│   │   │       └── ProductValidator.cs
│   │   ├── Daos/
│   │   │   ├── UserDao.cs
│   │   │   └── ProductDao.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   └── ProductStatus.cs
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   └── Money.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   └── ValidationException.cs
│   │   └── Interfaces/
│   │       ├── Repositories/
│   │       │   ├── IUserRepository.cs
│   │       │   ├── IProductRepository.cs
│   │       │   └── IUnitOfWork.cs
│   │       └── Services/
│   │           └── IDomainEventService.cs
│   │
│   ├── Application/
│   │   ├── UseCases/
│   │   │   ├── Users/
│   │   │   │   ├── CreateUserUseCase.cs
│   │   │   │   ├── GetUserUseCase.cs
│   │   │   │   ├── GetManyAndCountUsersUseCase.cs
│   │   │   │   └── UpdateUserUseCase.cs
│   │   │   └── Products/
│   │   │       ├── CreateProductUseCase.cs
│   │   │       └── GetProductUseCase.cs
│   │   ├── Dtos/
│   │   │   └── InternalDto.cs
│   │   ├── Interfaces/
│   │   │   └── IEmailService.cs
│   │   └── Mappers/
│   │       └── ApplicationMappingProfile.cs
│   │
│   ├── Infrastructure/
│   │   ├── NHibernate/
│   │   │   ├── NHUserRepository.cs
│   │   │   ├── NHProductRepository.cs
│   │   │   ├── NHUnitOfWork.cs
│   │   │   ├── SessionFactoryBuilder.cs
│   │   │   └── Mappers/
│   │   │       ├── UserMapper.cs
│   │   │       ├── ProductMapper.cs
│   │   │       ├── UserDaoMapper.cs
│   │   │       └── ProductDaoMapper.cs
│   │   ├── ExternalServices/
│   │   │   ├── Email/
│   │   │   │   └── SendGridEmailService.cs
│   │   │   └── Storage/
│   │   │       └── S3StorageService.cs
│   │   └── Caching/
│   │       └── RedisCacheService.cs
│   │
│   └── WebApi/
│       ├── Features/
│       │   ├── users/
│       │   │   ├── endpoint/
│       │   │   │   ├── CreateUserEndpoint.cs
│       │   │   │   ├── GetUserEndpoint.cs
│       │   │   │   ├── GetManyAndCountUsersEndpoint.cs
│       │   │   │   └── UpdateUserEndpoint.cs
│       │   │   └── models/
│       │   │       ├── CreateUserModel.cs
│       │   │       ├── GetUserModel.cs
│       │   │       ├── GetManyAndCountModel.cs
│       │   │       └── UpdateUserModel.cs
│       │   └── products/
│       │       ├── endpoint/
│       │       │   ├── CreateProductEndpoint.cs
│       │       │   └── GetProductEndpoint.cs
│       │       └── models/
│       │           ├── CreateProductModel.cs
│       │           └── GetProductModel.cs
│       ├── Dtos/
│       │   ├── UserDto.cs
│       │   └── ProductDto.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Filters/
│       │   └── ValidationFilter.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs
│       │   └── WebApplicationExtensions.cs
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   ├── Infrastructure.Tests/
│   └── WebApi.Tests/
│
└── solution.sln
```

---

## 8. Naming Conventions por Capa

### Domain Layer

```
Domain/
├── Entities/               → PascalCase singular (User, Product)
├── Daos/                   → PascalCase con sufijo Dao (UserDao)
├── Enums/                  → PascalCase singular (UserRole)
├── ValueObjects/           → PascalCase (Email, Money)
├── Exceptions/             → PascalCase con sufijo Exception
└── Interfaces/
    └── Repositories/       → I + PascalCase + Repository
```

### Application Layer

```
Application/
├── UseCases/
│   └── {feature}/         → kebab-case plural (users, products)
│       └── {Action}{Entity}UseCase.cs → PascalCase
├── Dtos/                  → PascalCase con sufijo Dto
└── Interfaces/            → I + PascalCase
```

### Infrastructure Layer

```
Infrastructure/
├── NHibernate/
│   ├── NH{Entity}Repository.cs  → NH prefix + PascalCase
│   └── Mappers/
│       └── {Entity}Mapper.cs    → PascalCase
└── ExternalServices/
    └── {Service}/
        └── {Provider}{Service}Service.cs
```

### WebApi Layer

```
WebApi/
├── Features/
│   └── {feature}/         → kebab-case plural (users, products)
│       ├── endpoint/      → kebab-case
│       │   └── {Action}{Entity}Endpoint.cs
│       └── models/        → kebab-case
│           └── {Action}{Entity}Model.cs
└── Dtos/                  → PascalCase con sufijo Dto
```

---

## 9. Code Formatting Standards

### Indentación y Espaciado

```csharp
namespace Application.UseCases.Users;

using Domain.Entities;
using Domain.Interfaces.Repositories;

// ✅ 4 espacios de indentación (no tabs)
// ✅ Línea en blanco después de usings
// ✅ Línea en blanco entre métodos

public class UserService
{
    private readonly IUnitOfWork _unitOfWork;

    // ✅ Línea en blanco entre fields y constructor
    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ✅ Línea en blanco entre métodos
    public async Task<User?> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, ct);

        // ✅ Línea en blanco antes de return
        return user;
    }

    public async Task CreateUserAsync(User user, CancellationToken ct)
    {
        await _unitOfWork.Users.SaveOrUpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);
    }
}
```

### Llaves (Braces)

```csharp
// ✅ Llaves en nueva línea (Allman style)
public class User
{
    public Guid Id { get; set; }

    public void Activate()
    {
        IsActive = true;
    }
}

// ✅ Para bloques de una línea, siempre usar llaves
if (user == null)
{
    throw new ArgumentNullException(nameof(user));
}

// ❌ Evitar omitir llaves
if (user == null)
    throw new ArgumentNullException(nameof(user));
```

### Longitud de Línea

```csharp
// ✅ Máximo 120 caracteres por línea
public async Task<Result<User>> CreateUserAsync(
    string email,
    string fullName,
    UserRole role,
    CancellationToken ct)
{
    // Implementation...
}

// ❌ Evitar líneas muy largas
public async Task<Result<User>> CreateUserAsync(string email, string fullName, UserRole role, CancellationToken ct)
{
    // Implementation...
}
```

---

## 10. EditorConfig

Configurar `.editorconfig` en la raíz de la solución para consistencia:

```ini
# EditorConfig is awesome: https://EditorConfig.org

root = true

# All files
[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
tab_width = 4

# C# files
[*.cs]

# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true

# Naming conventions

# Interfaces must start with I
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = *

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Private fields must start with _
dotnet_naming_rule.private_field_should_be_begins_with_underscore.severity = warning
dotnet_naming_rule.private_field_should_be_begins_with_underscore.symbols = private_field
dotnet_naming_rule.private_field_should_be_begins_with_underscore.style = begins_with_underscore

dotnet_naming_symbols.private_field.applicable_kinds = field
dotnet_naming_symbols.private_field.applicable_accessibilities = private

dotnet_naming_style.begins_with_underscore.required_prefix = _
dotnet_naming_style.begins_with_underscore.capitalization = camel_case

# Use file-scoped namespaces
csharp_style_namespace_declarations = file_scoped:warning

# JSON files
[*.json]
indent_size = 2

# XML files
[*.{xml,csproj,props,targets}]
indent_size = 2
```

---

## 11. Best Practices Summary

### ✅ DO

1. **Usar file-scoped namespaces** (C# 10+)
2. **Organizar usings** en el orden: System → Third-party → Project
3. **Un archivo, una clase** (excepto clases anidadas relacionadas)
4. **Organizar por feature**, no por tipo técnico
5. **Usar PascalCase** para clases, interfaces, métodos, propiedades
6. **Usar camelCase** para parámetros, variables locales
7. **Usar _camelCase** para fields privados
8. **Usar kebab-case** para carpetas
9. **Prefijo I** para interfaces
10. **Configurar EditorConfig** para consistencia del equipo

### ❌ DON'T

1. **No usar block-scoped namespaces** (`namespace X { }`)
2. **No mezclar usings** sin orden
3. **No poner múltiples clases** no relacionadas en un archivo
4. **No organizar por tipo técnico** (horizontal slicing)
5. **No usar snake_case** en C# (es para Python)
6. **No usar SCREAMING_CASE** para constantes
7. **No omitir llaves** en if/for/while
8. **No usar tabs**, usar espacios
9. **No ignorar warnings** de naming conventions
10. **No mezclar estilos** de formateo

---

## 12. Checklists

### Checklist: Nuevo Archivo

Cuando creas un nuevo archivo de código:

- [ ] El archivo tiene **file-scoped namespace** (C# 10+)
- [ ] Los **usings están ordenados**: System → Third-party → Project
- [ ] Hay **líneas en blanco** entre grupos de usings
- [ ] El archivo contiene **una sola clase/interface/record** principal
- [ ] El **nombre del archivo** coincide con la clase principal
- [ ] La clase usa **PascalCase**
- [ ] Las interfaces tienen **prefijo I**
- [ ] Los fields privados usan **_camelCase**
- [ ] Los parámetros y variables usan **camelCase**
- [ ] La **indentación es de 4 espacios**, no tabs
- [ ] Hay **líneas en blanco** entre métodos
- [ ] Las llaves usan **Allman style** (nueva línea)
- [ ] El namespace refleja la **capa de Clean Architecture**
- [ ] El archivo está en la **carpeta correcta** por feature

### Checklist: Code Review

Al revisar código:

- [ ] Los namespaces son **consistentes** con la estructura del proyecto
- [ ] Los usings están **correctamente ordenados**
- [ ] No hay **usings innecesarios**
- [ ] Las clases tienen **nombres descriptivos**
- [ ] Los métodos tienen **nombres que describen su acción**
- [ ] Los parámetros tienen **nombres significativos**
- [ ] No hay **archivos con múltiples clases** no relacionadas
- [ ] La organización es **por feature**, no por tipo técnico
- [ ] Los fields privados tienen **prefijo underscore**
- [ ] Las constantes usan **PascalCase**
- [ ] El código sigue el **EditorConfig** del proyecto
- [ ] No hay **warnings de naming conventions**

### Checklist: Refactoring de Organización

Al reorganizar código existente:

- [ ] Migrar a **file-scoped namespaces**
- [ ] Reorganizar archivos **por feature**
- [ ] Renombrar archivos para que **coincidan con la clase**
- [ ] Actualizar namespaces para reflejar **nueva estructura**
- [ ] Ordenar usings según **convención**
- [ ] Renombrar fields privados con **prefijo underscore**
- [ ] Separar clases no relacionadas en **archivos individuales**
- [ ] Crear carpetas con **kebab-case**
- [ ] Actualizar referencias en **toda la solución**
- [ ] Verificar que **compile sin warnings**
- [ ] Ejecutar **tests** para verificar que nada se rompió
- [ ] Actualizar **EditorConfig** si es necesario

---

## 13. Ejemplo Completo: Feature "Users"

### Estructura de Archivos

```
src/
├── Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   └── Validators/
│   │       └── UserValidator.cs
│   ├── Enums/
│   │   └── UserRole.cs
│   └── Interfaces/
│       └── Repositories/
│           ├── IUserRepository.cs
│           └── IUnitOfWork.cs
│
├── Application/
│   └── UseCases/
│       └── users/
│           ├── CreateUserUseCase.cs
│           ├── GetUserUseCase.cs
│           └── UpdateUserUseCase.cs
│
├── Infrastructure/
│   └── NHibernate/
│       ├── NHUserRepository.cs
│       └── Mappers/
│           └── UserMapper.cs
│
└── WebApi/
    ├── Features/
    │   └── users/
    │       ├── endpoint/
    │       │   ├── CreateUserEndpoint.cs
    │       │   ├── GetUserEndpoint.cs
    │       │   └── UpdateUserEndpoint.cs
    │       └── models/
    │           ├── CreateUserModel.cs
    │           ├── GetUserModel.cs
    │           └── UpdateUserModel.cs
    └── Dtos/
        └── UserDto.cs
```

### Domain/Entities/User.cs

```csharp
namespace Domain.Entities;

public class User
{
    public virtual Guid Id { get; protected set; }
    public virtual string Email { get; set; } = string.Empty;
    public virtual string FullName { get; set; } = string.Empty;
    public virtual UserRole Role { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual DateTime CreatedAt { get; protected set; }

    public User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        Role = UserRole.User;
    }
}
```

### Domain/Enums/UserRole.cs

```csharp
namespace Domain.Enums;

public enum UserRole
{
    User = 1,
    Admin = 2,
    SuperAdmin = 3
}
```

### Domain/Entities/Validators/UserValidator.cs

```csharp
namespace Domain.Entities.Validators;

using FluentValidation;
using Domain.Entities;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(255);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role");
    }
}
```

### Application/UseCases/users/CreateUserUseCase.cs

```csharp
namespace Application.UseCases.Users;

using FluentResults;
using FluentValidation;
using MediatR;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public record CreateUserCommand(string Email, string FullName) : IRequest<Result<User>>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<User>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<User> _validator;

    public CreateUserHandler(IUnitOfWork unitOfWork, IValidator<User> validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<User>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName
        };

        var validationResult = await _validator.ValidateAsync(user, ct);
        if (!validationResult.IsValid)
        {
            return Result.Fail(string.Join(", ", validationResult.Errors));
        }

        await _unitOfWork.Users.SaveOrUpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        return Result.Ok(user);
    }
}
```

### Infrastructure/NHibernate/NHUserRepository.cs

```csharp
namespace Infrastructure.NHibernate;

using Domain.Entities;
using Domain.Interfaces.Repositories;
using NHibernate;
using NHibernate.Linq;

public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public NHUserRepository(ISession session) : base(session)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await Session.Query<User>()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync(ct);
    }
}
```

### WebApi/Features/users/endpoint/CreateUserEndpoint.cs

```csharp
namespace WebApi.Features.Users.Endpoint;

using FastEndpoints;
using MediatR;
using Application.UseCases.Users;
using WebApi.Features.Users.Models;

public class CreateUserEndpoint : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly IMediator _mediator;

    public CreateUserEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
    {
        var command = new CreateUserCommand(req.Email, req.FullName);
        var result = await _mediator.Send(command, ct);

        if (result.IsFailed)
        {
            AddError(result.Errors.First().Message);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendOkAsync(new CreateUserModel.Response(result.Value.Id), ct);
    }
}
```

### WebApi/Features/users/models/CreateUserModel.cs

```csharp
namespace WebApi.Features.Users.Models;

public static class CreateUserModel
{
    public record Request(string Email, string FullName);
    public record Response(Guid UserId);
}
```

---

## Recursos Adicionales

- [C# Coding Conventions (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [EditorConfig Documentation](https://editorconfig.org/)
- [Clean Code by Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
