# AbstractDomainObject - Clase Base

**Categoría:** Patrón Base
**Ubicación:** `domain/entities/AbstractDomainObject.cs`
**Proyecto de referencia:** hashira-stone-backend

## Descripción

`AbstractDomainObject` es la **clase base abstracta** de la que heredan todas las entidades de dominio en APSYS. Provee funcionalidad común como identidad única (`Id`), fecha de creación (`CreationDate`), y métodos de validación integrados con FluentValidation.

---

## Código Completo

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

---

## Propiedades Heredadas

Todas las entidades que heredan de `AbstractDomainObject` obtienen automáticamente:

| Propiedad | Tipo | Descripción | Valor por Defecto |
|-----------|------|-------------|-------------------|
| `Id` | `Guid` | Identificador único de la entidad | `Guid.NewGuid()` |
| `CreationDate` | `DateTime` | Fecha y hora de creación (UTC) | `DateTime.UtcNow` |

---

## Métodos Heredados

| Método | Retorno | Descripción |
|--------|---------|-------------|
| `IsValid()` | `bool` | Verifica si la entidad cumple todas sus validaciones |
| `Validate()` | `IEnumerable<ValidationFailure>` | Retorna lista detallada de errores de validación |
| `GetValidator()` | `IValidator?` | Método virtual que debe ser sobrescrito en entidades derivadas |

---

## Uso en Entidades

### Herencia Básica

```csharp
public class User : AbstractDomainObject
{
    // Automáticamente hereda:
    // - public virtual Guid Id
    // - public virtual DateTime CreationDate
    // - public virtual bool IsValid()
    // - public virtual IEnumerable<ValidationFailure> Validate()
    // - public virtual IValidator? GetValidator()

    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

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

### Acceso a Propiedades Heredadas

```csharp
var user = new User("test@example.com", "Test User");

// Propiedades heredadas disponibles
Console.WriteLine($"ID: {user.Id}");                    // Guid único
Console.WriteLine($"Created: {user.CreationDate}");     // DateTime UTC
```

---

## Constructores

### Constructor Vacío (Protected)

```csharp
protected AbstractDomainObject()
{
    this.CreationDate = DateTime.UtcNow;
}
```

**Propósito:**
- Asigna automáticamente `CreationDate` al momento de creación
- `Id` se genera automáticamente por el inicializador de propiedad
- Usado indirectamente por constructores de entidades derivadas

### Constructor con Parámetros (Protected)

```csharp
protected AbstractDomainObject(Guid id, DateTime creationDate)
{
    Id = id;
    CreationDate = creationDate;
}
```

**Propósito:**
- Permite especificar `Id` y `CreationDate` manualmente
- Útil para testing o casos especiales
- Raramente usado en código de aplicación

---

## Validación con FluentValidation

### Flujo de Validación

```
┌──────────────────────────┐
│  Entidad                 │
│  user.IsValid()          │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  AbstractDomainObject    │
│  IsValid()               │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  GetValidator()          │  ← Sobrescrito en entidad
│  return new UserValidator() │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  FluentValidation        │
│  Ejecuta reglas          │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  ValidationResult        │
│  IsValid, Errors[]       │
└──────────────────────────┘
```

### Ejemplo de Uso: IsValid()

```csharp
var user = new User("test@example.com", "Test User");

if (!user.IsValid())
{
    Console.WriteLine("User is invalid");
}
```

### Ejemplo de Uso: Validate()

```csharp
var user = new User("", "");  // Email y Name vacíos

var errors = user.Validate();
foreach (var error in errors)
{
    Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
// Output:
// Email: Email is required
// Name: Name is required
```

---

## Propiedades Virtual

Todas las propiedades en `AbstractDomainObject` son `virtual`:

```csharp
public virtual Guid Id { get; set; } = Guid.NewGuid();
public virtual DateTime CreationDate { get; set; }
```

**Razón:** Permite que NHibernate cree proxies dinámicos para:
- Lazy loading
- Change tracking
- Interceptación de propiedades

**Las entidades derivadas también deben usar `virtual`:**

```csharp
✅ Correcto:
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
}

❌ Incorrecto:
public class User : AbstractDomainObject
{
    public string Email { get; set; } = string.Empty;  // Falta virtual
}
```

---

## Re-asignación de CreationDate

En algunos casos, las entidades pueden querer re-asignar `CreationDate` en su constructor:

```csharp
public class TechnicalStandard : AbstractDomainObject
{
    public TechnicalStandard(string code, string name)
    {
        this.CreationDate = DateTime.UtcNow;  // Re-asignar explícitamente
        Code = code;
        Name = name;
    }
}
```

**Cuándo hacer esto:**
- Si el constructor de la entidad toma tiempo en ejecutarse
- Si quieres asegurar que `CreationDate` es exactamente el momento de construcción completa
- En la mayoría de casos **NO es necesario** porque el constructor base ya lo asigna

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Herencia de funcionalidad común** - Evita duplicación en todas las entidades
- **Integración con FluentValidation** - Métodos `IsValid()` y `Validate()` listos para usar
- **Identidad única automática** - `Id` generado automáticamente
- **Timestamp de creación** - `CreationDate` asignado en UTC
- **Propiedades virtual** - Compatibilidad con NHibernate
- **Protected constructors** - Solo accesibles desde entidades derivadas

### 📚 Patrones Importantes

**Sobrescribir GetValidator:**
```csharp
public override IValidator GetValidator()
    => new UserValidator();
```

**Usar propiedades heredadas:**
```csharp
var user = new User("test@example.com", "Test");
Console.WriteLine(user.Id);            // Guid único
Console.WriteLine(user.CreationDate);  // DateTime UTC
```

**Validación antes de persistir:**
```csharp
if (!entity.IsValid())
{
    throw new InvalidDomainException(entity.Validate());
}
await repository.SaveAsync(entity);
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Validators](../../validators.md)
- [Entity Testing Practices](../../entities-testing-practices.md)

**Patrones Relacionados:**
- [Property Types](02-properties.md)
- [Constructors](03-constructors.md)
- [Validation](04-validation.md)

**Proyecto Real:**
- hashira.stone.backend: `domain/entities/AbstractDomainObject.cs`

---

**Última actualización:** 2025-01-20
