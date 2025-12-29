# Validators - Domain Layer

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Los **validators** son responsables de definir y aplicar reglas de validación a las entidades de dominio. En APSYS utilizamos **FluentValidation** que permite definir reglas de forma declarativa y fluida. Cada entidad tiene su propio validator que hereda de `AbstractValidator<T>`.

## Objetivo

- Definir reglas de validación declarativas con FluentValidation
- Centralizar validaciones en un solo lugar
- Proveer mensajes de error claros y útiles
- Integrar validaciones con entidades a través de `GetValidator()`
- Mantener validaciones independientes de frameworks de persistencia

---

## Tabla de Contenido

1. [¿Qué es FluentValidation?](#qué-es-fluentvalidation)
2. [AbstractValidator Pattern](#abstractvalidator-pattern)
3. [RuleFor - Definir Reglas](#rulefor---definir-reglas)
4. [Validaciones Comunes](#validaciones-comunes)
5. [Validaciones Custom](#validaciones-custom)
6. [WithMessage y WithErrorCode](#withmessage-y-witherrorcode)
7. [Validaciones con Referencias](#validaciones-con-referencias)
8. [Resources para Constantes](#resources-para-constantes)
9. [Ejemplos Reales](#ejemplos-reales)
10. [Integración con Entidades](#integración-con-entidades)
11. [Patrones y Best Practices](#patrones-y-best-practices)

---

## ¿Qué es FluentValidation?

### Definición

**FluentValidation** es una biblioteca .NET para validaciones que usa un API fluent (encadenamiento de métodos) para construir reglas de validación fuertemente tipadas.

### Ventajas

✅ **Sintaxis fluida y legible**
```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress();
```

✅ **Fuertemente tipada** - Refactoring seguro

✅ **Separación de concerns** - Validaciones separadas de entidades

✅ **Mensajes personalizables** - `WithMessage()`, `WithErrorCode()`

✅ **Validaciones complejas** - `.Must()`, `.When()`, etc.

### vs Data Annotations

```csharp
❌ Data Annotations (acoplado a atributos):
public class User
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}

✅ FluentValidation (separado):
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

---

## AbstractValidator Pattern

### Patrón Base

Todos los validators heredan de `AbstractValidator<T>`:

```csharp
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        // Reglas de validación aquí
    }
}
```

### Estructura Estándar

```csharp
public class {EntityName}Validator : AbstractValidator<{EntityName}>
{
    public {EntityName}Validator()
    {
        RuleFor(x => x.Property1)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Property2)
            .GreaterThan(0);

        // ... más reglas
    }
}
```

### Ubicación

```
domain/
└── entities/
    └── validators/
        ├── UserValidator.cs
        ├── RoleValidator.cs
        ├── PrototypeValidator.cs
        └── TechnicalStandardValidator.cs
```

---

## RuleFor - Definir Reglas

### Sintaxis Básica

```csharp
RuleFor(x => x.PropertyName)
    .Validation1()
    .Validation2()
    .WithMessage("Custom message");
```

### Selector Lambda

```csharp
// ✅ Correcto: expresión lambda
RuleFor(x => x.Email)
    .NotEmpty();

// ✅ Correcto: acceso a propiedad anidada
RuleFor(x => x.Address.City)
    .NotEmpty();

// ❌ Incorrecto: no es expresión lambda
RuleFor(GetEmail())  // NO COMPILA
```

### Múltiples Propiedades

```csharp
public UserValidator()
{
    RuleFor(x => x.Email)
        .NotEmpty()
        .EmailAddress();

    RuleFor(x => x.Name)
        .NotEmpty()
        .MaximumLength(100);

    RuleFor(x => x.Age)
        .GreaterThanOrEqualTo(18);
}
```

---

## Validaciones Comunes

### NotNull / NotEmpty

```csharp
// NotNull: no puede ser null
RuleFor(x => x.Name)
    .NotNull();

// NotEmpty: no puede ser null ni vacío
RuleFor(x => x.Email)
    .NotEmpty();  // ← Más común para strings

// Para strings, NotEmpty incluye NotNull
```

### Strings

```csharp
// Email address
RuleFor(x => x.Email)
    .EmailAddress();

// Longitud
RuleFor(x => x.Name)
    .Length(3, 50);  // Entre 3 y 50 caracteres

RuleFor(x => x.Code)
    .MinimumLength(5)
    .MaximumLength(10);

// Regex
RuleFor(x => x.PhoneNumber)
    .Matches(@"^\d{10}$")
    .WithMessage("Phone must be 10 digits");
```

### Números

```csharp
// Comparaciones
RuleFor(x => x.Age)
    .GreaterThan(0)
    .LessThan(150);

RuleFor(x => x.Price)
    .GreaterThanOrEqualTo(0)
    .LessThanOrEqualTo(1000000);

// Rango inclusivo
RuleFor(x => x.Rating)
    .InclusiveBetween(1, 5);

// Rango exclusivo
RuleFor(x => x.Score)
    .ExclusiveBetween(0, 100);
```

### Fechas

```csharp
// Comparaciones
RuleFor(x => x.BirthDate)
    .LessThan(DateTime.Today);

RuleFor(x => x.ExpirationDate)
    .GreaterThan(DateTime.Today);

// Comparar con otra propiedad (ver más abajo)
```

### Colecciones

```csharp
// No vacía
RuleFor(x => x.Roles)
    .NotEmpty()
    .WithMessage("User must have at least one role");

// Validar elementos
RuleForEach(x => x.Items)
    .SetValidator(new OrderItemValidator());
```

### Enums

```csharp
// Es un valor válido del enum
RuleFor(x => x.Status)
    .IsInEnum();
```

---

## Validaciones Custom

### Must() - Predicado Custom

```csharp
// Validación simple
RuleFor(x => x.IssueDate)
    .Must(date => date <= DateTime.Today)
    .WithMessage("IssueDate cannot be in the future");

// Con acceso a la instancia completa
RuleFor(x => x.ExpirationDate)
    .Must((prototype, expirationDate) => expirationDate > prototype.IssueDate)
    .WithMessage("ExpirationDate must be after IssueDate");

// Validar contra lista de valores
RuleFor(x => x.Status)
    .Must(status => new[] { "Active", "Inactive", "Pending" }.Contains(status))
    .WithMessage("Status must be Active, Inactive, or Pending");
```

### Custom() - Lógica Compleja

```csharp
RuleFor(x => x.Password)
    .Custom((password, context) =>
    {
        if (password.Length < 8)
        {
            context.AddFailure("Password must be at least 8 characters");
        }

        if (!password.Any(char.IsUpper))
        {
            context.AddFailure("Password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            context.AddFailure("Password must contain at least one digit");
        }
    });
```

### When() - Validación Condicional

```csharp
// Solo validar si condición se cumple
RuleFor(x => x.ShippingAddress)
    .NotEmpty()
    .When(x => x.RequiresShipping);

// Unless: inverso de When
RuleFor(x => x.Comment)
    .NotEmpty()
    .Unless(x => x.IsAutoGenerated);
```

---

## WithMessage y WithErrorCode

### WithMessage() - Mensaje Custom

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("The [Email] cannot be null or empty");

RuleFor(x => x.Age)
    .GreaterThanOrEqualTo(18)
    .WithMessage("User must be at least 18 years old");
```

### Placeholders en Mensajes

```csharp
RuleFor(x => x.Name)
    .Length(3, 50)
    .WithMessage("Name must be between {MinLength} and {MaxLength} characters. You entered {TotalLength}.");
```

### WithErrorCode() - Código de Error

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("The [Email] cannot be null or empty")
    .WithErrorCode("Email");  // ← Código para identificar el error

RuleFor(x => x.Email)
    .EmailAddress()
    .WithMessage("The [Email] is not a valid email address")
    .WithErrorCode("Email_InvalidDomain");  // ← Código más específico
```

**Uso:** Los códigos de error ayudan al frontend a identificar qué tipo de error ocurrió sin parsear el mensaje.

### Encadenar Múltiples Validaciones

```csharp
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
```

---

## Validaciones con Referencias

### Comparar con Otra Propiedad

```csharp
// ExpirationDate debe ser después de IssueDate
RuleFor(x => x.ExpirationDate)
    .GreaterThan(x => x.IssueDate)
    .WithMessage("Expiration date must be after issue date");

// Confirmación de password
RuleFor(x => x.PasswordConfirmation)
    .Equal(x => x.Password)
    .WithMessage("Passwords must match");
```

### Usar Must() con Referencias

```csharp
RuleFor(x => x.ExpirationDate)
    .Must((prototype, expirationDate) => expirationDate > prototype.IssueDate)
    .WithMessage("ExpirationDate must be after IssueDate");
```

### DependentRules() - Validar Solo si Otra Pasa

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .DependentRules(() =>
    {
        RuleFor(x => x.Email)
            .EmailAddress();  // Solo valida si NotEmpty pasó
    });
```

---

## Resources para Constantes

### Patrón de Resources

Para evitar "magic strings", usar clases de recursos:

```csharp
// domain/resources/TechnicalStandardResource.cs
namespace hashira.stone.backend.domain.resources;

/// <summary>
/// Provides constant values related to the TechnicalStandard entity.
/// </summary>
public static class TechnicalStandardResource
{
    /// <summary>
    /// Represents the "Active" status for a technical standard.
    /// </summary>
    public const string StatusActive = "Active";

    /// <summary>
    /// Represents the "Deprecated" status for a technical standard.
    /// </summary>
    public const string StatusDeprecated = "Deprecated";
}
```

### Uso en Validators

```csharp
public class TechnicalStandardValidator : AbstractValidator<TechnicalStandard>
{
    public TechnicalStandardValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status =>
                status is TechnicalStandardResource.StatusActive or
                TechnicalStandardResource.StatusDeprecated)
            .WithMessage("The [Status] must be either 'Active' or 'Deprecated'")
            .WithErrorCode("Status");
    }
}
```

### Validar con Lista de Valores

```csharp
// domain/resources/PrototypeResources.cs
public static class PrototypeResources
{
    public static readonly string[] ValidStatus = { "Active", "Expired", "Cancelled" };
}

// En validator
RuleFor(x => x.Status)
    .Must(status => PrototypeResources.ValidStatus.Contains(status))
    .WithMessage("Status must be one of: Active, Expired, Cancelled");
```

---

## Ejemplos Reales

Basados en [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

### UserValidator - Validaciones Complejas de Email

```csharp
// domain/entities/validators/UserValidator.cs
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

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

**Características:**
- Validación de email en 3 niveles: NotEmpty → EmailAddress → Regex
- Códigos de error específicos por tipo de fallo
- Mensajes descriptivos

### RoleValidator - Validación Simple

```csharp
// domain/entities/validators/RoleValidator.cs
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Validator class for the Role model
/// </summary>
public class RoleValidator : AbstractValidator<Role>
{
    /// <summary>
    /// Initialize a new instance of RoleValidator
    /// </summary>
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name");
    }
}
```

**Características:**
- Validación simple de una sola propiedad
- Documentación XML completa
- Patrón estándar

### PrototypeValidator - Validaciones de Fechas y Custom

```csharp
// domain/entities/validators/PrototypeValidator.cs
using FluentValidation;
using hashira.stone.backend.domain.resources;

namespace hashira.stone.backend.domain.entities.validators;

public class PrototypeValidator : AbstractValidator<Prototype>
{
    public PrototypeValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("Number is required");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("IssueDate is required")
            .Must(date => date <= DateTime.Today)
            .WithMessage("IssueDate cannot be in the future");

        RuleFor(x => x.ExpirationDate)
            .NotEmpty()
            .WithMessage("ExpirationDate is required")
            .Must((prototype, expirationDate) => expirationDate > prototype.IssueDate)
            .WithMessage("ExpirationDate must be after IssueDate");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => PrototypeResources.ValidStatus.Contains(status))
            .WithMessage("Status must be one of: Active, Expired, Cancelled");
    }
}
```

**Características:**
- Validación de fechas con `.Must()`
- Validación con referencia a otra propiedad (IssueDate < ExpirationDate)
- Validación contra lista de valores válidos (Resources)

### TechnicalStandardValidator - Múltiples Propiedades

```csharp
// domain/entities/validators/TechnicalStandardValidator.cs
using FluentValidation;
using hashira.stone.backend.domain.resources;

namespace hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Validator class for the TechnicalStandard model
/// </summary>
public class TechnicalStandardValidator : AbstractValidator<TechnicalStandard>
{
    /// <summary>
    /// Initialize a new instance of TechnicalStandardValidator
    /// </summary>
    public TechnicalStandardValidator()
    {
        RuleFor(x => x.Code)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Code] cannot be null or empty")
            .WithErrorCode("Code");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name");

        RuleFor(x => x.Edition)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Edition] cannot be null or empty")
            .WithErrorCode("Edition");

        RuleFor(x => x.Status)
            .NotNull()
            .NotEmpty()
            .Must(status =>
                status is TechnicalStandardResource.StatusActive or
                TechnicalStandardResource.StatusDeprecated)
            .WithMessage("The [Status] must be either 'Active' or 'Deprecated'")
            .WithErrorCode("Status");

        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Type] cannot be null or empty")
            .WithErrorCode("Type");
    }
}
```

**Características:**
- Validación de múltiples propiedades required
- Pattern matching en `.Must()` (C# 9+)
- Uso de constantes de Resources

---

## Integración con Entidades

### Patrón Completo

```csharp
// 1️⃣ Entidad sobrescribe GetValidator()
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();  // ← Retorna validator
}

// 2️⃣ Validator define reglas
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotEmpty();
    }
}

// 3️⃣ Uso en código
var user = new User("test@example.com", "Test");

if (!user.IsValid())
{
    var errors = user.Validate();
    throw new InvalidDomainException(errors);
}
```

### Flujo de Validación

```
user.IsValid()
       ↓
GetValidator() → new UserValidator()
       ↓
UserValidator.Validate(user)
       ↓
FluentValidation ejecuta todas las reglas
       ↓
ValidationResult { IsValid, Errors[] }
       ↓
Retorna true/false
```

### En Repositorios

```csharp
public async Task<User> CreateAsync(string email, string name)
{
    var user = new User(email, name);

    // Validar antes de guardar
    if (!user.IsValid())
        throw new InvalidDomainException(user.Validate());

    await AddAsync(user);
    return user;
}
```

---

## Patrones y Best Practices

### ✅ DO - Hacer

#### 1. Un Validator por Entidad

```csharp
✅ Correcto:
public class UserValidator : AbstractValidator<User>
public class RoleValidator : AbstractValidator<Role>

❌ Incorrecto:
public class AllEntitiesValidator  // ❌ Un validator para todo
```

#### 2. Constructor Único para Reglas

```csharp
✅ Correcto:
public UserValidator()
{
    RuleFor(x => x.Email).NotEmpty();
    RuleFor(x => x.Name).NotEmpty();
}

❌ Incorrecto:
public UserValidator(bool includeEmailValidation)  // ❌ Lógica condicional
```

#### 3. Usar WithMessage y WithErrorCode

```csharp
✅ Correcto:
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("Email is required")
    .WithErrorCode("Email");

❌ Incorrecto:
RuleFor(x => x.Email)
    .NotEmpty();  // Sin mensaje custom
```

#### 4. Usar Resources para Constantes

```csharp
✅ Correcto:
Must(status => status == TechnicalStandardResource.StatusActive)

❌ Incorrecto:
Must(status => status == "Active")  // Magic string
```

#### 5. Documentar Validators

```csharp
✅ Correcto:
/// <summary>
/// Validator class for the User model
/// </summary>
public class UserValidator : AbstractValidator<User>

❌ Incorrecto:
public class UserValidator : AbstractValidator<User>  // Sin docs
```

#### 6. Validar Not Null Primero

```csharp
✅ Correcto:
RuleFor(x => x.Email)
    .NotNull()
    .NotEmpty()
    .EmailAddress();  // Solo ejecuta si NotEmpty pasó

❌ Incorrecto:
RuleFor(x => x.Email)
    .EmailAddress()  // Falla con NullReferenceException
    .NotNull();
```

### ❌ DON'T - No Hacer

#### 1. No Validar en Constructor de Entidad

```csharp
❌ Incorrecto:
public User(string email, string name)
{
    if (string.IsNullOrEmpty(email))
        throw new ArgumentException("Email is required");  // ❌ NO

    Email = email;
    Name = name;
}

✅ Correcto:
// Validación va en Validator, no en constructor
```

#### 2. No Duplicar Validaciones

```csharp
❌ Incorrecto:
// En entidad
if (string.IsNullOrEmpty(Email)) throw ...

// En validator
RuleFor(x => x.Email).NotEmpty();  // ❌ Duplicado

✅ Correcto:
// Solo en Validator
```

#### 3. No Usar Data Annotations

```csharp
❌ Incorrecto:
public class User : AbstractDomainObject
{
    [Required]  // ❌ NO usar atributos
    [EmailAddress]
    public virtual string Email { get; set; }
}

✅ Correcto:
// Validación solo en UserValidator
```

#### 4. No Hacer Queries a BD

```csharp
❌ Incorrecto:
RuleFor(x => x.Email)
    .Must(email => !_dbContext.Users.Any(u => u.Email == email))  // ❌ Query en validator
    .WithMessage("Email already exists");

✅ Correcto:
// Validar existencia en Repository, no en Validator
```

---

## Checklist: Nuevo Validator

Al crear un nuevo validator:

- [ ] Clase hereda de `AbstractValidator<{Entity}>`
- [ ] Namespace: `{proyecto}.domain.entities.validators`
- [ ] Constructor único define todas las reglas
- [ ] Cada propiedad required tiene `NotNull()` o `NotEmpty()`
- [ ] Todas las reglas tienen `WithMessage()`
- [ ] Reglas importantes tienen `WithErrorCode()`
- [ ] Constantes usan clases de Resources (no magic strings)
- [ ] Validaciones custom usan `.Must()` cuando necesario
- [ ] Comparaciones de propiedades usan referencias correctas
- [ ] Documentación XML completa
- [ ] Entidad referencia validator en `GetValidator()`
- [ ] No hace queries a base de datos
- [ ] No duplica validación de constructor

---

## Conclusión

**Principios Clave para Validators:**

1. ✅ **Un validator por entidad** - Separación clara
2. ✅ **AbstractValidator<T>** - Patrón estándar de FluentValidation
3. ✅ **RuleFor() fluent API** - Sintaxis declarativa
4. ✅ **WithMessage + WithErrorCode** - Mensajes claros y códigos identificables
5. ✅ **Resources para constantes** - Evitar magic strings
6. ✅ **Validación independiente** - No queries a BD, no lógica de negocio compleja

**Flujo Mental:**

```
Entidad → GetValidator() → UserValidator
   ↓
Constructor del Validator
   ↓
RuleFor(propiedad) → Validaciones → WithMessage/WithErrorCode
   ↓
IsValid()/Validate() ejecuta todas las reglas
   ↓
ValidationResult con errores (si hay)
   ↓
Throw InvalidDomainException si no es válido
```

**Tipos de validaciones por complejidad:**

- **Simple:** NotNull, NotEmpty, Length
- **Media:** EmailAddress, Regex, GreaterThan
- **Compleja:** Must() con lógica custom, Must() con referencias a otras propiedades

---

## Recursos Adicionales

### Guías Relacionadas

- [Entities](./entities.md) - Entidades de dominio
- [Domain Exceptions](./domain-exceptions.md) - InvalidDomainException
- [Repository Interfaces](./repository-interfaces.md) - Validar antes de persistir

### Documentación Oficial

- [FluentValidation](https://docs.fluentvalidation.net/)
- [Built-in Validators](https://docs.fluentvalidation.net/en/latest/built-in-validators.html)
- [Custom Validators](https://docs.fluentvalidation.net/en/latest/custom-validators.html)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
