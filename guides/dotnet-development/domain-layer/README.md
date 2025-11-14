# Domain Layer - Clean Architecture

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

La capa de dominio es el **núcleo** de Clean Architecture. Contiene las entidades de negocio, reglas de dominio, validaciones y definiciones de interfaces. Esta capa NO debe tener dependencias hacia otras capas y debe ser completamente independiente de frameworks externos.

## Principios Fundamentales

- **Independencia Total**: No depende de Application, Infrastructure o WebApi
- **Reglas de Negocio**: Contiene toda la lógica de negocio core
- **Persistencia Agnóstica**: No conoce cómo se persisten los datos
- **Framework Agnóstico**: No depende de NHibernate, EF, FastEndpoints, etc.
- **Interfaces en Domain**: Define contratos (IRepository, IUnitOfWork) que Infrastructure implementa

## Guías Disponibles

### 1. [Entities](./entities.md) ✅ v1.0.0

Entidades de dominio y clase base AbstractDomainObject.

**Contenido:**
- Qué es una entidad de dominio
- AbstractDomainObject pattern
- Propiedades virtuales (NHibernate requirement)
- Constructores
- Métodos de dominio (IsValid, Validate, GetValidator)
- Ejemplos: User, Role, Prototype, TechnicalStandard

**Cuándo usar:** Al crear nuevas entidades de dominio.

---

### 2. [Validators](./validators.md) ✅ v1.0.0

Validaciones de entidades con FluentValidation.

**Contenido:**
- AbstractValidator<T> pattern
- Reglas de validación (RuleFor)
- Validaciones comunes (NotNull, NotEmpty, EmailAddress)
- Validaciones custom
- Error codes y messages
- Integración con entidades
- IsValid() y Validate()
- Ejemplos: UserValidator, PrototypeValidator

**Cuándo usar:** Al agregar validaciones a entidades de dominio.

---

### 3. [Repository Interfaces](./repository-interfaces.md) ✅ v1.0.0

Interfaces de repositorios y Unit of Work.

**Contenido:**
- IRepository<T, TKey> interface
- IReadOnlyRepository<T, TKey> interface
- IEntityRepository interfaces específicas
- IUnitOfWork interface
- GetManyAndCountAsync patterns
- Métodos custom por repositorio
- Separation: Interfaces en Domain, Implementaciones en Infrastructure
- Ejemplos: IUserRepository, IPrototypeRepository

**Cuándo usar:** Al definir contratos de persistencia para entidades.

---

### 4. [DAOs](./daos.md) ✅ v1.0.0

Data Access Objects para consultas optimizadas.

**Contenido:**
- Qué es un DAO
- Cuándo usar DAO vs Entity
- Estructura de DAOs
- Propiedades read-only
- Search fields (SearchAll pattern)
- DAO Repositories (IReadOnlyRepository)
- Ejemplos: PrototypeDao, TechnicalStandardDao

**Cuándo usar:** Para listados, reportes y consultas de solo lectura.

---

### 5. [Domain Exceptions](./domain-exceptions.md) ✅ v1.0.0

Excepciones custom del dominio.

**Contenido:**
- InvalidDomainException
- DuplicatedDomainException
- ResourceNotFoundException
- InvalidFilterArgumentException
- Cuándo lanzar excepciones
- Excepciones vs FluentResults
- Error messages
- Ejemplos prácticos

**Cuándo usar:** Al definir reglas de dominio que pueden fallar.

---

### 6. [Value Objects](./value-objects.md) ✅ v1.0.0

Value Objects pattern para conceptos de dominio inmutables.

**Contenido:**
- Value Object vs Entity comparison
- Características (Immutability, Equality by value, Self-validation, No Identity)
- Implementación con C# 13 records
- Ejemplos completos: Email, Money, Address, DateRange, PhoneNumber
- Integration con NHibernate (Component mapping, IUserType)
- Validation patterns (constructor, TryParse)
- Factory methods y conversions
- DO/DON'T best practices

**Cuándo usar:** Para representar conceptos sin identidad propia (emails, money, addresses, ranges).

---

## Estructura de la Capa de Dominio

Basada en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```
domain/
├── entities/                                # Entidades de dominio
│   ├── AbstractDomainObject.cs             # Clase base
│   ├── User.cs
│   ├── Role.cs
│   ├── Prototype.cs
│   ├── TechnicalStandard.cs
│   └── validators/                          # Validadores
│       ├── UserValidator.cs
│       ├── RoleValidator.cs
│       ├── PrototypeValidator.cs
│       └── TechnicalStandardValidator.cs
│
├── valueobjects/                            # Value Objects (opcional)
│   ├── Email.cs                            # Email value object
│   ├── Money.cs                            # Money value object
│   ├── Address.cs                          # Address value object
│   └── DateRange.cs                        # DateRange value object
│
├── daos/                                    # Data Access Objects
│   ├── PrototypeDao.cs
│   └── TechnicalStandardDao.cs
│
├── interfaces/                              # Interfaces
│   ├── repositories/                        # Interfaces de repositorios
│   │   ├── IRepository.cs                  # Interface genérica
│   │   ├── IReadOnlyRepository.cs          # Interface read-only
│   │   ├── IUserRepository.cs              # Interface específica
│   │   ├── IPrototypeRepository.cs
│   │   ├── IPrototypeDaoRepository.cs
│   │   ├── ITechnicalStandardRepository.cs
│   │   ├── ITechnicalStandardDaoRepository.cs
│   │   ├── IUnitOfWork.cs                  # Unit of Work
│   │   ├── GetManyAndCountResult.cs        # DTOs de resultado
│   │   ├── SortingCriteria.cs
│   │   └── IGetManyAndCountResultWithSorting.cs
│   └── services/                            # Interfaces de servicios
│       └── IIdentityService.cs              # Ej: Auth0 service
│
├── exceptions/                              # Excepciones de dominio
│   ├── InvalidDomainException.cs
│   ├── DuplicatedDomainException.cs
│   ├── ResourceNotFoundException.cs
│   └── InvalidFilterArgumentException.cs
│
└── errors/                                  # FluentResults errors
    ├── AbstractDomainObjectErrors.cs
    ├── UserErrors.cs
    └── ResultBaseExtender.cs
```

## Flujo de Trabajo

### Crear Nueva Entidad de Dominio

1. **Definir entidad** → [Entities](./entities.md)
   ```csharp
   public class Product : AbstractDomainObject
   {
       public virtual string Name { get; set; } = string.Empty;
       public virtual decimal Price { get; set; }

       public Product() { }

       public Product(string name, decimal price)
       {
           Name = name;
           Price = price;
       }

       public override IValidator GetValidator() => new ProductValidator();
   }
   ```

2. **Crear validador** → [Validators](./validators.md)
   ```csharp
   public class ProductValidator : AbstractValidator<Product>
   {
       public ProductValidator()
       {
           RuleFor(x => x.Name)
               .NotNull()
               .NotEmpty();

           RuleFor(x => x.Price)
               .GreaterThan(0);
       }
   }
   ```

3. **Definir repositorio** → [Repository Interfaces](./repository-interfaces.md)
   ```csharp
   public interface IProductRepository : IRepository<Product, Guid>
   {
       Task<Product?> GetByNameAsync(string name, CancellationToken ct);
   }
   ```

4. **Agregar a Unit of Work**
   ```csharp
   public interface IUnitOfWork
   {
       IProductRepository Products { get; }
       // ... otros repositorios

       void BeginTransaction();
       void Commit();
       void Rollback();
   }
   ```

### Crear DAO para Consultas

1. **Definir DAO** → [DAOs](./daos.md)
   ```csharp
   public class ProductDao
   {
       public virtual Guid Id { get; set; }
       public virtual string Name { get; set; } = string.Empty;
       public virtual decimal Price { get; set; }
       public virtual string SearchAll { get; set; } = string.Empty;
   }
   ```

2. **Definir repositorio read-only**
   ```csharp
   public interface IProductDaoRepository : IReadOnlyRepository<ProductDao, Guid>
   {
   }
   ```

3. **Agregar a Unit of Work**
   ```csharp
   public interface IUnitOfWork
   {
       IProductDaoRepository ProductsDao { get; }
       // ... otros repositorios
   }
   ```

### Crear Value Object

1. **Definir Value Object** → [Value Objects](./value-objects.md)
   ```csharp
   public sealed record Email
   {
       public string Value { get; init; }

       public Email(string value)
       {
           if (string.IsNullOrWhiteSpace(value))
               throw new ArgumentException("Email cannot be empty");

           if (!IsValidEmail(value))
               throw new ArgumentException($"Invalid email format: {value}");

           Value = value.ToLowerInvariant();
       }

       private static bool IsValidEmail(string email) =>
           Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

       public override string ToString() => Value;
       public static implicit operator string(Email email) => email.Value;
   }
   ```

2. **Usar en Entity**
   ```csharp
   public class User : AbstractDomainObject
   {
       public virtual Email Email { get; set; }
       public virtual string Name { get; set; }

       public User(Email email, string name)
       {
           Email = email; // Email ya está validado
           Name = name;
       }
   }
   ```

3. **Mapear en NHibernate (Infrastructure)**
   ```csharp
   public class UserMap : ClassMap<User>
   {
       public UserMap()
       {
           Component(x => x.Email, email =>
           {
               email.Map(e => e.Value).Column("Email");
           });
       }
   }
   ```

### Usar Excepciones de Dominio

```csharp
// En un repositorio o método de entidad
public async Task<User> CreateAsync(string email, string name)
{
    var user = new User(email, name);

    // Validar entidad
    if (!user.IsValid())
        throw new InvalidDomainException(user.Validate());

    // Validar regla de negocio
    if (await GetByEmailAsync(email) != null)
        throw new DuplicatedDomainException($"A user with the email '{email}' already exists.");

    await AddAsync(user);
    return user;
}
```

---

## Ejemplos Reales del Proyecto

### AbstractDomainObject

```csharp
// domain/entities/AbstractDomainObject.cs
public abstract class AbstractDomainObject
{
    public virtual Guid Id { get; set; } = Guid.NewGuid();
    public virtual DateTime CreationDate { get; set; }

    protected AbstractDomainObject()
    {
        this.CreationDate = DateTime.UtcNow;
    }

    protected AbstractDomainObject(Guid id, DateTime creationDate)
    {
        Id = id;
        CreationDate = creationDate;
    }

    // Valida y retorna bool
    public virtual bool IsValid()
    {
        IValidator? validator = GetValidator();
        if (validator == null) return true;

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.IsValid;
    }

    // Valida y retorna errores
    public virtual IEnumerable<ValidationFailure> Validate()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return new List<ValidationFailure>();

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.Errors;
    }

    // Debe ser sobrescrito en entidades
    public virtual IValidator? GetValidator() => null;
}
```

### User Entity

```csharp
// domain/entities/User.cs
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Locked = false;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}
```

### UserValidator

```csharp
// domain/entities/validators/UserValidator.cs
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotNull().WithMessage("Email is required")
            .NotEmpty().WithMessage("Email cannot be empty")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Name)
            .NotNull().WithMessage("Name is required")
            .NotEmpty().WithMessage("Name cannot be empty");
    }
}
```

### InvalidDomainException

```csharp
// domain/exceptions/InvalidDomainException.cs
public class InvalidDomainException : Exception
{
    public readonly IEnumerable<ValidationFailure> ValidationFailures;

    // Constructor con lista de errores
    public InvalidDomainException(IEnumerable<ValidationFailure> validationFailures)
    {
        this.ValidationFailures = validationFailures;
    }

    // Constructor con un solo error
    public InvalidDomainException(string property, string errorCode, string errorMessage)
    {
        var validationResults = new List<ValidationFailure>
        {
            new()
            {
                ErrorCode = errorCode,
                PropertyName = property,
                ErrorMessage = errorMessage
            }
        };
        this.ValidationFailures = validationResults.AsEnumerable();
    }

    public override string Message
    {
        get
        {
            var messages = from error in this.ValidationFailures
                           select new { error.ErrorMessage, error.ErrorCode, error.PropertyName };
            return JsonSerializer.Serialize(messages);
        }
    }
}
```

### DuplicatedDomainException

```csharp
// domain/exceptions/DuplicatedDomainException.cs
public class DuplicatedDomainException : Exception
{
    public DuplicatedDomainException(string message) : base(message)
    {
    }
}
```

---

## Checklists Rápidas

### Nueva Entidad CRUD

- [ ] Clase `{Entity}.cs` hereda de `AbstractDomainObject`
- [ ] Propiedades son `virtual` (para NHibernate)
- [ ] Constructor por defecto existe
- [ ] Constructor con parámetros para creación
- [ ] `GetValidator()` implementado
- [ ] `{Entity}Validator.cs` creado en `validators/`
- [ ] Reglas de validación definidas
- [ ] `I{Entity}Repository.cs` creado en `interfaces/repositories/`
- [ ] Hereda de `IRepository<{Entity}, Guid>`
- [ ] Métodos custom definidos si necesarios
- [ ] Agregado a `IUnitOfWork`

### Nuevo DAO

- [ ] Clase `{Entity}Dao.cs` creada en `daos/`
- [ ] Propiedades son `virtual`
- [ ] NO hereda de AbstractDomainObject
- [ ] NO tiene validaciones
- [ ] `I{Entity}DaoRepository.cs` creado
- [ ] Hereda de `IReadOnlyRepository<{Entity}Dao, Guid>`
- [ ] Agregado a `IUnitOfWork`

### Nueva Excepción de Dominio

- [ ] Clase hereda de `Exception`
- [ ] Constructor con mensaje
- [ ] Constructor con mensaje y inner exception (opcional)
- [ ] Usado en métodos de entidad o repositorio
- [ ] Documentado con XML comments

### Nuevo Value Object

- [ ] Clase `{ValueObject}.cs` es `sealed record`
- [ ] Ubicado en `valueobjects/`
- [ ] Propiedades son `{ get; init; }`
- [ ] NO tiene Id (no tiene identidad)
- [ ] Constructor con validación (throw ArgumentException si inválido)
- [ ] Normalización de valores (ToLower, ToUpper, etc.)
- [ ] Override `ToString()` implementado
- [ ] Conversion operators (implicit/explicit) si aplica
- [ ] Usado como propiedad en Entity
- [ ] Mapeado como Component en NHibernate

---

## Patrones Clave

### 1. Dependency Inversion

Domain define interfaces, Infrastructure las implementa:
- ✅ `IUserRepository` en Domain
- ✅ `NHUserRepository` en Infrastructure
- ✅ Domain NO conoce NHibernate
- ✅ Domain NO conoce PostgreSQL

```
┌─────────────────┐
│  Infrastructure │  implements
│  NHUserRepo     │───────────────┐
└─────────────────┘               │
                                  ▼
                         ┌─────────────────┐
                         │     Domain      │
                         │  IUserRepository│
                         └─────────────────┘
                                  ▲
┌─────────────────┐               │
│   Application   │  depends on   │
│   CreateUserUC  │───────────────┘
└─────────────────┘
```

### 2. Entity Validation

Entidades auto-validables:
- ✅ `GetValidator()` retorna `IValidator`
- ✅ `IsValid()` verifica validación
- ✅ `Validate()` retorna errores
- ✅ Validación antes de persistir

```csharp
var user = new User("test@example.com", "Test User");

// Validar antes de guardar
if (!user.IsValid())
{
    var errors = user.Validate();
    throw new InvalidDomainException(errors);
}

await _repository.AddAsync(user);
```

### 3. DAO Pattern

Separación entre escritura y lectura:
- ✅ **Entity**: Para operaciones CRUD (User)
- ✅ **DAO**: Para consultas optimizadas (UserDao)
- ✅ DAO no tiene validaciones
- ✅ DAO es más ligero

```
CRUD Operations          Read-Only Queries
      ↓                         ↓
  ┌──────┐               ┌──────────┐
  │ User │               │ UserDao  │
  └──────┘               └──────────┘
      ↓                         ↓
┌──────────────┐        ┌────────────────────┐
│IUserRepository│        │IUserDaoRepository  │
└──────────────┘        └────────────────────┘
```

### 4. Rich Domain Model

Entidades con comportamiento:
- ✅ No son simples DTOs
- ✅ Tienen métodos de negocio
- ✅ Encapsulan reglas
- ✅ Validan su estado

```csharp
public class Order : AbstractDomainObject
{
    public virtual decimal Total { get; set; }
    public virtual OrderStatus Status { get; set; }

    // Comportamiento de dominio
    public void Ship()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidDomainException("Cannot ship unpaid order");

        Status = OrderStatus.Shipped;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidDomainException("Cannot cancel shipped order");

        Status = OrderStatus.Cancelled;
    }
}
```

---

## Reglas de Oro

### ✅ SÍ hacer en Domain

- Definir entidades con reglas de negocio
- Crear validadores con FluentValidation
- Definir interfaces de repositorios
- Lanzar excepciones de dominio
- Usar Value Objects para conceptos
- Documentar con XML comments
- Validar entidades antes de persistir
- Encapsular lógica de negocio en métodos

### ❌ NO hacer en Domain

- Referenciar Infrastructure
- Referenciar Application
- Referenciar WebApi
- Usar NHibernate directamente
- Usar Entity Framework directamente
- Usar HttpClient o servicios externos
- Depender de FastEndpoints
- Hacer queries a BD
- Depender de frameworks de logging
- Usar atributos de serialización (JSON, XML)

---

## Excepciones vs FluentResults

El proyecto usa **ambos** patrones según el contexto:

### Excepciones (domain/exceptions/)

Usadas para **errores irrecuperables** en el dominio:

```csharp
// Validación falló
throw new InvalidDomainException(user.Validate());

// Regla de negocio violada
throw new DuplicatedDomainException("User already exists");

// Recurso no encontrado
throw new ResourceNotFoundException("User not found");
```

### FluentResults (domain/errors/)

Usadas para **resultados de operaciones** en Application:

```csharp
// domain/errors/UserErrors.cs
public static class UserErrors
{
    public static Error UserNotFound(Guid id)
        => new Error($"User with ID {id} not found");

    public static Error UserAlreadyExists(string email)
        => new Error($"User with email {email} already exists");
}

// En un Use Case
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    var user = await _uoW.Users.GetByIdAsync(command.Id, ct);
    if (user == null)
        return Result.Fail(UserErrors.UserNotFound(command.Id));

    return Result.Ok(user);
}
```

**Criterio de decisión:**
- **Excepción**: Error crítico en el dominio (validación, reglas de negocio)
- **FluentResults**: Control de flujo en Application (encontrado/no encontrado, éxito/fallo)

---

## Stack Tecnológico

- **FluentValidation 12.0** - Validaciones
- **FluentResults 4.0** - Result pattern (opcional)
- **C# 13** - Lenguaje
- **.NET 9.0** - Framework base

---

## Recursos Adicionales

### Documentación Oficial

- [FluentValidation Docs](https://docs.fluentvalidation.net/)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [FluentResults GitHub](https://github.com/altmann/FluentResults)

### Otras Secciones de Guías

- [Best Practices](../best-practices/README.md)
- [Feature Structure](../feature-structure/README.md)
- [Application Layer](../application-layer/README.md)
- [Infrastructure Layer](../infrastructure-layer/README.md)

---

## Conclusión

**Principios Clave del Domain Layer:**

1. ✅ **Independencia total** - No depende de ninguna otra capa
2. ✅ **Reglas de negocio centralizadas** - Toda la lógica core está aquí
3. ✅ **Validaciones en entidades** - Entidades auto-validables
4. ✅ **Interfaces definen contratos** - Infrastructure implementa
5. ✅ **Excepciones de dominio** - Errores específicos del negocio
6. ✅ **DAO para consultas** - Separación read/write

**Flujo Mental:**

```
Entidad → Validator → Repository Interface → Unit of Work
   ↓
AbstractDomainObject (Id, CreationDate, IsValid, Validate, GetValidator)
   ↓
Infrastructure implementa repositories
   ↓
Application usa interfaces del Domain
```

---

**Última actualización:** 2025-01-14
**Mantenedor:** Equipo APSYS

## Resumen de Guías Completadas

| Guía | Estado | Versión | Líneas | Contenido Principal |
|------|--------|---------|--------|---------------------|
| [Entities](./entities.md) | ✅ | v1.0.0 | 947 | AbstractDomainObject, Virtual properties, Constructors, IsValid/Validate |
| [Validators](./validators.md) | ✅ | v1.0.0 | 970 | FluentValidation, RuleFor, WithMessage/WithErrorCode, Resources pattern |
| [Repository Interfaces](./repository-interfaces.md) | ✅ | v1.0.0 | 1,402 | IRepository, IReadOnlyRepository, IUnitOfWork, GetManyAndCount |
| [DAOs](./daos.md) | ✅ | v1.0.0 | 1,051 | DAO vs Entity, SearchAll pattern, Read-only optimization |
| [Domain Exceptions](./domain-exceptions.md) | ✅ | v1.0.0 | 1,271 | InvalidDomainException, Excepciones vs Results, HTTP status mapping |
| [Value Objects](./value-objects.md) | ✅ | v1.0.0 | 1,378 | Immutability, Equality by value, C# records, NHibernate Component |
| **TOTAL** | **6 guías** | **v1.0.0** | **7,019** | **Domain Layer completo** |
