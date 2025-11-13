# Entities - Domain Layer

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Las **entidades** son el corazón del Domain Layer. Representan conceptos de negocio con identidad única y encapsulan reglas de dominio, validaciones y comportamiento. En APSYS, todas las entidades heredan de `AbstractDomainObject` que provee funcionalidad común.

## Objetivo

- Definir entidades de dominio con identidad única
- Encapsular reglas de negocio en las entidades
- Integrar validaciones con FluentValidation
- Mantener entidades independientes de frameworks de persistencia
- Seguir patrones consistentes en toda la codebase

---

## Tabla de Contenido

1. [¿Qué es una Entidad?](#qué-es-una-entidad)
2. [AbstractDomainObject](#abstractdomainobject)
3. [Propiedades Virtual](#propiedades-virtual)
4. [Constructores](#constructores)
5. [Métodos de Dominio](#métodos-de-dominio)
6. [GetValidator Integration](#getvalidator-integration)
7. [Ejemplos Reales](#ejemplos-reales)
8. [Patrones y Best Practices](#patrones-y-best-practices)

---

## ¿Qué es una Entidad?

### Definición

Una **entidad** es un objeto que:
- Tiene **identidad única** (normalmente un `Id`)
- Su identidad permanece constante a través del tiempo
- Dos entidades son iguales si tienen el mismo `Id`
- Encapsula **reglas de negocio** y comportamiento

### Entity vs DTO vs DAO

```
┌──────────────────┬──────────────────┬──────────────────┐
│     Entity       │       DTO        │       DAO        │
├──────────────────┼──────────────────┼──────────────────┤
│ Domain Layer     │ WebApi Layer     │ Domain Layer     │
│ Identidad única  │ Sin identidad    │ Sin identidad    │
│ Tiene validación │ Sin validación   │ Sin validación   │
│ Tiene métodos    │ Solo propiedades │ Solo propiedades │
│ Read/Write       │ Transferencia    │ Read-only        │
│ Hereda de Base   │ POCO             │ POCO             │
└──────────────────┴──────────────────┴──────────────────┘
```

### Características de Entidades en APSYS

✅ **Herencia de AbstractDomainObject**
```csharp
public class User : AbstractDomainObject
{
    // Hereda: Id, CreationDate, IsValid(), Validate(), GetValidator()
}
```

✅ **Propiedades virtuales (NHibernate)**
```csharp
public virtual string Name { get; set; } = string.Empty;
```

✅ **Constructores múltiples**
```csharp
public User() { }  // Para NHibernate
public User(string email, string name) { }  // Para creación
```

✅ **Validaciones integradas**
```csharp
public override IValidator GetValidator() => new UserValidator();
```

---

## AbstractDomainObject

### Definición Completa

Basada en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```csharp
// domain/entities/AbstractDomainObject.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using FluentValidation.Results;

/// <summary>
/// Clase base abstracta para objetos de dominio.
/// </summary>
public abstract class AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the domain object.
    /// This identifier is automatically generated if not provided.
    /// </summary>
    public virtual Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the creation date of the domain object.
    /// This property is automatically set to the current date and time when the object is created.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    protected AbstractDomainObject()
    {
        this.CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="creationDate"></param>
    protected AbstractDomainObject(Guid id, DateTime creationDate)
    {
        Id = id;
        CreationDate = creationDate;
    }

    /// <summary>
    /// Validates the current instance of the domain object.
    /// This method uses FluentValidation to check if the object meets its validation rules.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsValid()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return true;

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.IsValid;
    }

    /// <summary>
    /// Validates the current instance of the domain object and returns any validation failures.
    /// This method uses FluentValidation to check if the object meets its validation rules and returns a collection of validation failures if any exist.
    /// If no validator is defined, it returns an empty list of validation failures.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<ValidationFailure> Validate()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return new List<ValidationFailure>();
        else
        {
            var context = new ValidationContext<object>(this);
            ValidationResult result = validator.Validate(context);
            return result.Errors;
        }
    }

    /// <summary>
    /// Gets the validator for the domain object.
    /// This method should be overridden in derived classes to provide a specific validator for the entity.
    /// </summary>
    /// <returns></returns>
    public virtual IValidator? GetValidator()
         => null;
}
```

### Propiedades Heredadas

Todas las entidades heredan automáticamente:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único, generado automáticamente |
| `CreationDate` | `DateTime` | Fecha de creación, asignada en UTC |

### Métodos Heredados

| Método | Retorno | Descripción |
|--------|---------|-------------|
| `IsValid()` | `bool` | Verifica si la entidad cumple validaciones |
| `Validate()` | `IEnumerable<ValidationFailure>` | Retorna lista de errores de validación |
| `GetValidator()` | `IValidator?` | Debe ser sobrescrito para retornar validator |

---

## Propiedades Virtual

### ¿Por qué virtual?

NHibernate requiere que todas las propiedades sean `virtual` para poder crear **proxies dinámicos** para lazy loading y change tracking.

### Patrón Obligatorio

```csharp
✅ Correcto:
public virtual string Name { get; set; } = string.Empty;
public virtual DateTime IssueDate { get; set; }
public virtual IList<Role> Roles { get; set; } = new List<Role>();

❌ Incorrecto:
public string Name { get; set; }  // Falta virtual
```

### Tipos de Propiedades

#### Propiedades Simples

```csharp
// Strings
public virtual string Email { get; set; } = string.Empty;
public virtual string Name { get; set; } = string.Empty;

// Números
public virtual int Age { get; set; }
public virtual decimal Price { get; set; }

// Booleanos
public virtual bool Locked { get; set; }
public virtual bool IsActive { get; set; }

// Fechas
public virtual DateTime IssueDate { get; set; }
public virtual DateTime ExpirationDate { get; set; }

// Nullable
public virtual string? OptionalField { get; set; }
public virtual int? OptionalNumber { get; set; }
```

#### Colecciones (Relaciones)

```csharp
// One-to-Many
public virtual IList<Role> Roles { get; set; } = new List<Role>();
public virtual IList<Order> Orders { get; set; } = new List<Order>();

// Many-to-One
public virtual Category Category { get; set; } = null!;
public virtual User Owner { get; set; } = null!;
```

**Importante:**
- Usar `IList<T>` en lugar de `List<T>`
- Inicializar colecciones para evitar null
- Marcar referencias con `= null!` si son required

---

## Constructores

### Patrón: Dos Constructores

Todas las entidades deben tener **dos constructores**:

#### 1. Constructor Vacío (para NHibernate)

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="User"/> class.
/// This constructor is used by NHibernate for mapping purposes.
/// </summary>
public User()
{
}
```

**Propósito:** NHibernate lo usa para crear instancias al cargar desde BD.

#### 2. Constructor con Parámetros (para Creación)

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="User"/> class with the specified email and name.
/// </summary>
/// <param name="email">The user's email address</param>
/// <param name="name">The user's full name</param>
public User(string email, string name)
{
    Email = email;
    Name = name;
    Locked = false;  // Valores por defecto
}
```

**Propósito:** Usado en código de aplicación para crear nuevas entidades.

### Ejemplo Completo

```csharp
public class TechnicalStandard : AbstractDomainObject
{
    // Constructor vacío para NHibernate
    public TechnicalStandard()
    {
    }

    // Constructor con parámetros para creación
    public TechnicalStandard(string code, string name, string edition, string status, string type)
    {
        this.CreationDate = DateTime.UtcNow;  // Opcional: re-asignar si necesario
        Code = code;
        Name = name;
        Edition = edition;
        Status = status;
        Type = type;
    }

    // Propiedades...
    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    // ...
}
```

---

## Métodos de Dominio

### IsValid() - Verificar Validación

```csharp
var user = new User("test@example.com", "Test User");

// Verificar si es válido
if (!user.IsValid())
{
    // No es válido
    Console.WriteLine("User is invalid");
}
```

**Retorna:** `bool` - `true` si pasa todas las validaciones.

### Validate() - Obtener Errores

```csharp
var user = new User("", "");  // Email y Name vacíos

// Obtener errores
var errors = user.Validate();

foreach (var error in errors)
{
    Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
```

**Retorna:** `IEnumerable<ValidationFailure>` - Lista de errores.

### GetValidator() - Retornar Validator

```csharp
public class User : AbstractDomainObject
{
    // ... propiedades y constructores

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

**Debe ser sobrescrito** en cada entidad para retornar su validator específico.

---

## GetValidator Integration

### Patrón de Integración

```csharp
// 1️⃣ Entidad sobrescribe GetValidator()
public class Prototype : AbstractDomainObject
{
    public virtual string Number { get; set; } = string.Empty;
    public virtual DateTime IssueDate { get; set; }
    // ...

    public override IValidator GetValidator()
    {
        return new PrototypeValidator();
    }
}

// 2️⃣ Validator define reglas
public class PrototypeValidator : AbstractValidator<Prototype>
{
    public PrototypeValidator()
    {
        RuleFor(x => x.Number)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.IssueDate)
            .LessThan(x => x.ExpirationDate)
            .WithMessage("Issue date must be before expiration date");
    }
}

// 3️⃣ Uso en código
var prototype = new Prototype("P-001", DateTime.Now, DateTime.Now.AddDays(30), "Active");

if (!prototype.IsValid())
{
    throw new InvalidDomainException(prototype.Validate());
}
```

### Flujo de Validación

```
┌──────────────┐
│  Entidad     │
│  user.IsValid() │
└──────┬───────┘
       │
       ▼
┌──────────────────┐
│ GetValidator()   │  ← Retorna UserValidator
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ FluentValidation │  ← Ejecuta reglas
│ Validate()       │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ ValidationResult │  ← IsValid, Errors[]
└──────────────────┘
```

---

## Ejemplos Reales

Basados en [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

### User - Entidad con Colección

```csharp
// domain/entities/User.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

public class User : AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    public virtual string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's full name
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user account is locked
    /// </summary>
    public virtual bool Locked { get; set; }

    /// <summary>
    /// Gets or sets the roles assigned to this user
    /// </summary>
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public User()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's full name</param>
    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Locked = false;
    }

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

**Características:**
- Propiedades simples: `Email`, `Name`, `Locked`
- Colección: `IList<Role>`
- Dos constructores
- GetValidator sobrescrito

### Role - Entidad Simple

```csharp
// domain/entities/Role.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role : AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public Role()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with the specified name.
    /// </summary>
    /// <param name="name">The role name</param>
    public Role(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Get the validator for the Role entity.
    /// </summary>
    public override IValidator GetValidator()
        => new RoleValidator();
}
```

**Características:**
- Entidad muy simple con una sola propiedad
- Patrón completo sigue aplicando

### Prototype - Entidad con Fechas

```csharp
// domain/entities/Prototype.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Represents a prototype domain object with properties for tracking its number, issue date, expiration date, and
/// status.
/// </summary>
public class Prototype : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Prototype"/> class.
    /// </summary>
    public Prototype()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prototype"/> class with the specified details.
    /// </summary>
    /// <param name="number">The unique identifier for the prototype.</param>
    /// <param name="issueDate">The date when the prototype was issued.</param>
    /// <param name="expirationDate">The date when the prototype expires.</param>
    /// <param name="status">The current status of the prototype.</param>
    public Prototype(string number, DateTime issueDate, DateTime expirationDate, string status)
    {
        Number = number;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Status = status;
    }

    /// <summary>
    /// Gets or sets the number as a string.
    /// </summary>
    public virtual string Number { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the issue was created or recorded.
    /// </summary>
    public virtual DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the item.
    /// </summary>
    public virtual DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the operation.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Retrieves the validator associated with the current instance.
    /// </summary>
    public override IValidator GetValidator()
    {
        return new PrototypeValidator();
    }
}
```

**Características:**
- Múltiples tipos de datos: `string`, `DateTime`
- Constructor con 4 parámetros
- Documentación XML completa

### TechnicalStandard - Entidad Completa

```csharp
// domain/entities/TechnicalStandard.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Represents a technical standard in the system.
/// A technical standard defines a set of criteria, guidelines, or characteristics for processes, products, or services.
/// </summary>
public class TechnicalStandard : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TechnicalStandard"/> class.
    /// </summary>
    public TechnicalStandard()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TechnicalStandard"/> class.
    /// </summary>
    public TechnicalStandard(string code, string name, string edition, string status, string type)
    {
        this.CreationDate = DateTime.UtcNow;  // Re-asignar CreationDate
        Code = code;
        Name = name;
        Edition = edition;
        Status = status;
        Type = type;
    }

    /// <summary>
    /// Gets or sets the unique code of the technical standard.
    /// This code is required and must be unique within the system.
    /// </summary>
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the technical standard.
    /// This is a descriptive name and is required.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edition or version of the technical standard.
    /// This property is required and typically indicates the publication or revision version.
    /// </summary>
    public virtual string Edition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the technical standard.
    /// Typical values are "Active" or "Deprecated".
    /// This property is required.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the technical standard.
    /// Typical values are "CFE" or "Externa".
    /// This property is required.
    /// </summary>
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets the validator for the <see cref="TechnicalStandard"/> entity.
    /// </summary>
    public override IValidator GetValidator()
        => new TechnicalStandardValidator();
}
```

**Características:**
- Múltiples propiedades string
- Constructor con 5 parámetros
- Re-asigna `CreationDate` en constructor personalizado
- Documentación detallada de valores típicos

---

## Patrones y Best Practices

### ✅ DO - Hacer

#### 1. Siempre Heredar de AbstractDomainObject

```csharp
✅ Correcto:
public class Product : AbstractDomainObject
{
    // ...
}

❌ Incorrecto:
public class Product  // No hereda
{
    public Guid Id { get; set; }  // Duplica lógica
    public DateTime CreationDate { get; set; }
}
```

#### 2. Propiedades Virtual

```csharp
✅ Correcto:
public virtual string Name { get; set; } = string.Empty;

❌ Incorrecto:
public string Name { get; set; } = string.Empty;  // Falta virtual
```

#### 3. Dos Constructores

```csharp
✅ Correcto:
public User() { }  // Para NHibernate
public User(string email, string name) { }  // Para creación

❌ Incorrecto:
// Solo un constructor
public User(string email, string name) { }
```

#### 4. Sobrescribir GetValidator

```csharp
✅ Correcto:
public override IValidator GetValidator()
    => new UserValidator();

❌ Incorrecto:
// No sobrescribir GetValidator
```

#### 5. Inicializar Colecciones

```csharp
✅ Correcto:
public virtual IList<Role> Roles { get; set; } = new List<Role>();

❌ Incorrecto:
public virtual IList<Role> Roles { get; set; }  // Puede ser null
```

#### 6. Documentación XML

```csharp
✅ Correcto:
/// <summary>
/// Gets or sets the user's email address
/// </summary>
public virtual string Email { get; set; } = string.Empty;

❌ Incorrecto:
public virtual string Email { get; set; } = string.Empty;  // Sin docs
```

### ❌ DON'T - No Hacer

#### 1. No Agregar Lógica de Persistencia

```csharp
❌ Incorrecto:
public class User : AbstractDomainObject
{
    public void SaveToDatabase()  // ❌ NO!
    {
        // Lógica de base de datos
    }
}

✅ Correcto:
// Persistencia va en Infrastructure Layer (Repositories)
```

#### 2. No Usar Atributos de ORM

```csharp
❌ Incorrecto:
[Table("users")]  // ❌ Atributos de NHibernate/EF
public class User : AbstractDomainObject
{
    [Column("user_email")]  // ❌ NO!
    public virtual string Email { get; set; }
}

✅ Correcto:
// Mapeo va en Infrastructure Layer (Mappers)
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; }
}
```

#### 3. No Depender de Frameworks Externos

```csharp
❌ Incorrecto:
public class User : AbstractDomainObject
{
    [JsonProperty("email")]  // ❌ Atributo de Newtonsoft
    public virtual string Email { get; set; }
}

✅ Correcto:
// Serialización va en WebApi Layer (DTOs)
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; }
}
```

#### 4. No Exponer Propiedades Internas como Públicas

```csharp
❌ Incorrecto:
public class Order : AbstractDomainObject
{
    public virtual List<OrderItem> _items { get; set; }  // ❌ Público
}

✅ Correcto:
public class Order : AbstractDomainObject
{
    public virtual IList<OrderItem> Items { get; set; } = new List<OrderItem>();
}
```

---

## Checklist: Nueva Entidad

Al crear una nueva entidad de dominio:

- [ ] Clase hereda de `AbstractDomainObject`
- [ ] Namespace: `{proyecto}.domain.entities`
- [ ] Propiedades son `virtual`
- [ ] Propiedades tienen valores por defecto (`= string.Empty`, `= new List<>()`)
- [ ] Constructor vacío existe (para NHibernate)
- [ ] Constructor con parámetros existe (para creación)
- [ ] `GetValidator()` está sobrescrito
- [ ] Validator correspondiente existe en `validators/`
- [ ] Documentación XML completa en todas las propiedades
- [ ] Documentación XML en constructores
- [ ] Documentación XML en `GetValidator()`
- [ ] No tiene atributos de ORM
- [ ] No tiene lógica de persistencia
- [ ] No depende de frameworks externos

---

## Conclusión

**Principios Clave para Entidades:**

1. ✅ **Heredar de AbstractDomainObject** - Funcionalidad común
2. ✅ **Propiedades virtual** - Requerido para NHibernate
3. ✅ **Dos constructores** - Vacío para ORM, con parámetros para creación
4. ✅ **GetValidator sobrescrito** - Integración con FluentValidation
5. ✅ **Documentación completa** - XML comments en todo
6. ✅ **Independencia** - No depender de frameworks de persistencia

**Flujo Mental:**

```
Entidad hereda AbstractDomainObject
   ↓
Propiedades virtual + valores por defecto
   ↓
Constructor vacío + Constructor con parámetros
   ↓
GetValidator() → retorna Validator
   ↓
IsValid() / Validate() disponibles
   ↓
Entidad lista para usar en Application/Infrastructure
```

**Ejemplos de entidades por complejidad:**

- **Simple:** `Role` (1 propiedad)
- **Media:** `User` (propiedades + colección)
- **Compleja:** `TechnicalStandard` (múltiples propiedades + lógica)

---

## Recursos Adicionales

### Guías Relacionadas

- [Validators](./validators.md) - Validaciones con FluentValidation
- [Repository Interfaces](./repository-interfaces.md) - Contratos de persistencia
- [DAOs](./daos.md) - Objetos de solo lectura
- [Domain Exceptions](./domain-exceptions.md) - Excepciones de dominio

### Documentación Oficial

- [FluentValidation](https://docs.fluentvalidation.net/)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
