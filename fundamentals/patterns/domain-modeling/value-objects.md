# Value Objects

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenidos
- [Introducción](#introducción)
- [Value Object vs Entity](#value-object-vs-entity)
- [Características de Value Objects](#características-de-value-objects)
- [Implementación en C# 13](#implementación-en-c-13)
- [Ejemplos Comunes](#ejemplos-comunes)
- [Integration con NHibernate](#integration-con-nhibernate)
- [Validation de Value Objects](#validation-de-value-objects)
- [Cuándo Usar Value Objects](#cuándo-usar-value-objects)
- [Patrones y Best Practices](#patrones-y-best-practices)
- [Checklist para Nuevos Value Objects](#checklist-para-nuevos-value-objects)

---

## Introducción

Los **Value Objects** son objetos del dominio que **representan conceptos mediante su valor**, no mediante identidad. Son uno de los building blocks fundamentales de Domain-Driven Design (DDD).

En APSYS, los Value Objects se usan para:

- **Encapsular conceptos del dominio**: Email, Money, Address, DateRange
- **Garantizar validez**: Un Email siempre es válido, un Money siempre tiene currency
- **Immutability**: No cambian después de crearse
- **Equality by value**: Dos emails con el mismo valor son iguales
- **Cohesión**: Lógica relacionada agrupada (ej: Money con currency y amount)

### Conceptos Clave

```
╔═══════════════════════════════════════════════════════════════╗
║                      ENTITY vs VALUE OBJECT                    ║
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║  ENTITY (User)                    VALUE OBJECT (Email)        ║
║  ┌─────────────────────┐         ┌──────────────────────┐    ║
║  │ Id: Guid            │         │ Value: string        │    ║
║  │ Email: Email ──────────────►  │                      │    ║
║  │ Name: string        │         │ (No identity)        │    ║
║  │ CreationDate        │         │ (Immutable)          │    ║
║  │                     │         │ (Equality by value)  │    ║
║  │ + IsValid()         │         └──────────────────────┘    ║
║  └─────────────────────┘                                      ║
║                                                               ║
║  user1.Id != user2.Id              email1 == email2          ║
║  (Different identity)              (Same value)              ║
╚═══════════════════════════════════════════════════════════════╝
```

---

## Value Object vs Entity

| Aspecto | Entity | Value Object |
|---------|--------|--------------|
| **Identidad** | Sí (Id único) | No (solo valor) |
| **Equality** | Por Id | Por valor de todas sus propiedades |
| **Mutabilidad** | Mutable | **Immutable** |
| **Lifecycle** | Tracked (CreationDate, UpdateDate) | No lifecycle |
| **Validación** | FluentValidation en GetValidator() | Validación en constructor |
| **Herencia** | AbstractDomainObject | Ninguna (o ValueObject base) |
| **Ejemplo** | User, Role, TechnicalStandard | Email, Money, Address, DateRange |

### Ejemplo Comparativo

```csharp
// ENTITY: Tiene identidad, mutable
public class User : AbstractDomainObject
{
    public virtual Guid Id { get; set; }
    public virtual Email Email { get; set; } // Value Object
    public virtual string Name { get; set; }

    // Constructor vacío para NHibernate
    protected User() { }

    // Constructor con parámetros
    public User(Email email, string name)
    {
        Email = email;
        Name = name;
    }
}

// VALUE OBJECT: Sin identidad, immutable
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }

    public override string ToString() => Value;
}
```

---

## Características de Value Objects

### 1. Immutability (Inmutabilidad)

**Los Value Objects NO cambian después de crearse.**

```csharp
// ✅ BIEN: Immutable Value Object
public sealed record Email
{
    public string Value { get; init; } // init solo se asigna en constructor

    public Email(string value)
    {
        Value = value.ToLowerInvariant();
    }
}

var email = new Email("john@example.com");
// email.Value = "new@example.com"; // ¡ERROR! init-only property

// Para "cambiar", crear uno nuevo
var newEmail = new Email("new@example.com");


// ❌ MAL: Mutable Value Object
public class Email
{
    public string Value { get; set; } // ¡NO! set permite cambios

    public Email(string value)
    {
        Value = value;
    }
}
```

### 2. Equality by Value (Igualdad por Valor)

**Dos Value Objects con el mismo valor son iguales.**

```csharp
// ✅ C# 13 records tienen equality by value automático
public sealed record Email(string Value);

var email1 = new Email("john@example.com");
var email2 = new Email("john@example.com");

Console.WriteLine(email1 == email2); // true (mismo valor)
Console.WriteLine(email1.Equals(email2)); // true


// Para classes, debes implementar manualmente
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    public Email(string value)
    {
        Value = value;
    }

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Email);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Email? left, Email? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !Equals(left, right);
    }
}
```

### 3. No Identity (Sin Identidad)

**No tienen Id, solo valor.**

```csharp
// ✅ BIEN: Value Object sin Id
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}

// ❌ MAL: Value Object con Id (entonces es Entity)
public sealed record Money
{
    public Guid Id { get; init; } // ¡NO! Los Value Objects no tienen Id
    public decimal Amount { get; init; }
    public string Currency { get; init; }
}
```

### 4. Self-Validation (Validación en Constructor)

**Siempre válidos al ser creados.**

```csharp
// ✅ BIEN: Validación en constructor
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }
}

// Uso: Si se crea, es válido
try
{
    var email = new Email("john@example.com"); // ✅ Válido
    var invalid = new Email("not-an-email"); // ❌ Lanza ArgumentException
}
catch (ArgumentException ex)
{
    // Manejo de error
}
```

---

## Implementación en C# 13

En C# 13, usa **records** para Value Objects:

### Pattern Básico

```csharp
public sealed record {ValueObject}
{
    // Propiedades init-only
    public {Type} {Property} { get; init; }

    // Constructor con validación
    public {ValueObject}({Type} {property})
    {
        // Validaciones
        if (/* invalid */)
            throw new ArgumentException("...");

        {Property} = {property};
    }

    // Métodos de negocio (si aplica)
    public {Type} SomeOperation()
    {
        // Lógica
    }
}
```

### Ejemplo: Email

```csharp
namespace {proyecto}.domain.valueobjects;

/// <summary>
/// Represents an email address value object.
/// </summary>
public sealed record Email
{
    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="value">The email address</param>
    /// <exception cref="ArgumentException">Thrown when the email is invalid</exception>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    /// <summary>
    /// Returns the email address as a string.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion from string to Email.
    /// </summary>
    public static implicit operator string(Email email) => email.Value;
}
```

---

## Ejemplos Comunes

### 1. Email

```csharp
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}

// Uso
var email = new Email("john@example.com");
Console.WriteLine(email.Value); // "john@example.com"
```

### 2. Money

```csharp
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be 3 characters (ISO 4217)", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    // Operaciones de negocio
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

// Uso
var price = new Money(100.50m, "USD");
var tax = new Money(15.00m, "USD");
var total = price.Add(tax); // 115.50 USD

var discounted = price.Multiply(0.9m); // 90.45 USD (10% descuento)
```

### 3. Address

```csharp
public sealed record Address
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public Address(string street, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public override string ToString() =>
        $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

// Uso
var address = new Address(
    "123 Main St",
    "Springfield",
    "IL",
    "62701",
    "USA"
);
```

### 4. DateRange

```csharp
public sealed record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));

        StartDate = startDate;
        EndDate = endDate;
    }

    // Métodos de negocio
    public int DurationInDays()
    {
        return (EndDate - StartDate).Days;
    }

    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    public bool Overlaps(DateRange other)
    {
        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }

    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}

// Uso
var range = new DateRange(
    new DateTime(2025, 1, 1),
    new DateTime(2025, 12, 31)
);

Console.WriteLine(range.DurationInDays()); // 364
Console.WriteLine(range.Contains(new DateTime(2025, 6, 15))); // true
```

### 5. PhoneNumber

```csharp
public sealed record PhoneNumber
{
    public string CountryCode { get; init; }
    public string Number { get; init; }

    public PhoneNumber(string countryCode, string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Country code cannot be empty", nameof(countryCode));

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Number cannot be empty", nameof(number));

        // Eliminar espacios y caracteres especiales
        var cleanNumber = new string(number.Where(char.IsDigit).ToArray());

        if (cleanNumber.Length < 7)
            throw new ArgumentException("Phone number too short", nameof(number));

        CountryCode = countryCode;
        Number = cleanNumber;
    }

    public override string ToString() => $"+{CountryCode} {Number}";

    // Factory method
    public static PhoneNumber Parse(string phoneNumber)
    {
        // Ejemplo simple: +52 1234567890
        if (phoneNumber.StartsWith("+"))
        {
            var parts = phoneNumber.TrimStart('+').Split(' ', 2);
            if (parts.Length == 2)
            {
                return new PhoneNumber(parts[0], parts[1]);
            }
        }

        throw new ArgumentException($"Invalid phone number format: {phoneNumber}");
    }
}

// Uso
var phone = new PhoneNumber("52", "1234567890");
var parsed = PhoneNumber.Parse("+52 1234567890");
```

---

## Integration con NHibernate

### Component Mapping

En NHibernate, los Value Objects se mapean como **Components** (no como entidades separadas).

#### Entity con Value Object

```csharp
// Entity
public class User : AbstractDomainObject
{
    public virtual Guid Id { get; set; }
    public virtual Email Email { get; set; } // Value Object
    public virtual string Name { get; set; }

    protected User() { }

    public User(Email email, string name)
    {
        Id = Guid.NewGuid();
        Email = email;
        Name = name;
        CreationDate = DateTime.UtcNow;
    }
}
```

#### NHibernate Mapping (FluentNHibernate)

```csharp
// Infrastructure/persistence/mappings/UserMap.cs
public class UserMap : ClassMap<User>
{
    public UserMap()
    {
        Table("Users");

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.CreationDate);
        Map(x => x.Name);

        // Component mapping para Value Object
        Component(x => x.Email, email =>
        {
            email.Map(e => e.Value).Column("Email"); // Columna en tabla Users
        });
    }
}
```

**SQL Result:**

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    CreationDate DATETIME NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL  -- Column from Email Value Object
);
```

### Múltiples Propiedades en Value Object

```csharp
// Entity con Money Value Object
public class Order : AbstractDomainObject
{
    public virtual Guid Id { get; set; }
    public virtual Money TotalPrice { get; set; }

    protected Order() { }

    public Order(Money totalPrice)
    {
        Id = Guid.NewGuid();
        TotalPrice = totalPrice;
    }
}

// Mapping
public class OrderMap : ClassMap<Order>
{
    public OrderMap()
    {
        Table("Orders");

        Id(x => x.Id).GeneratedBy.Assigned();

        // Component con múltiples columnas
        Component(x => x.TotalPrice, money =>
        {
            money.Map(m => m.Amount).Column("TotalAmount");
            money.Map(m => m.Currency).Column("TotalCurrency");
        });
    }
}
```

**SQL Result:**

```sql
CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TotalAmount DECIMAL(18,2) NOT NULL,
    TotalCurrency NVARCHAR(3) NOT NULL
);
```

### Custom User Type (Alternativa)

Para Value Objects simples (una propiedad), puedes usar **IUserType**:

```csharp
// Infrastructure/persistence/types/EmailType.cs
public class EmailType : IUserType
{
    public Type ReturnedType => typeof(Email);

    public SqlType[] SqlTypes => new[] { SqlTypeFactory.GetString(255) };

    public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
    {
        var value = NHibernateUtil.String.NullSafeGet(rs, names[0], session) as string;
        return value != null ? new Email(value) : null;
    }

    public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
    {
        var email = value as Email;
        NHibernateUtil.String.NullSafeSet(cmd, email?.Value, index, session);
    }

    // ... otros métodos de IUserType
}

// Mapping
public class UserMap : ClassMap<User>
{
    public UserMap()
    {
        Table("Users");

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.Name);

        // Custom type para Email
        Map(x => x.Email).CustomType<EmailType>().Column("Email");
    }
}
```

---

## Validation de Value Objects

### Validación en Constructor (Recomendado)

```csharp
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        // Validación directa en constructor
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }
}
```

### FluentValidation para Entities con Value Objects

```csharp
// Entity
public class User : AbstractDomainObject
{
    public virtual Guid Id { get; set; }
    public virtual Email Email { get; set; }
    public virtual string Name { get; set; }

    protected User() { }

    public User(Email email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator() => new UserValidator();
}

// Validator
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        // Email ya es válido si existe (validado en constructor)
        RuleFor(x => x.Email)
            .NotNull()
            .WithMessage("Email is required")
            .WithErrorCode("Email");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .WithErrorCode("Name");
    }
}
```

### TryParse Pattern (Alternativa)

```csharp
public sealed record Email
{
    public string Value { get; init; }

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    // Factory method que lanza excepción
    public static Email Create(string value)
    {
        if (!TryCreate(value, out var email, out var error))
            throw new ArgumentException(error, nameof(value));

        return email;
    }

    // TryParse pattern para casos donde no quieres excepción
    public static bool TryCreate(string value, out Email? email, out string? error)
    {
        email = null;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Email cannot be empty";
            return false;
        }

        if (!IsValidEmail(value))
        {
            error = $"Invalid email format: {value}";
            return false;
        }

        email = new Email(value);
        return true;
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }
}

// Uso
// Con excepción
var email = Email.Create("john@example.com");

// Sin excepción
if (Email.TryCreate(req.Email, out var email, out var error))
{
    // Usar email
}
else
{
    // Manejo de error
    return Result.Fail(error);
}
```

---

## Cuándo Usar Value Objects

### ✅ Usa Value Objects cuando:

1. **El concepto se define por su valor, no por identidad**
   ```csharp
   // ✅ Email: Se define por su valor "john@example.com"
   var email = new Email("john@example.com");
   ```

2. **El objeto debe ser immutable**
   ```csharp
   // ✅ Money: No debe cambiar, crear uno nuevo
   var price = new Money(100m, "USD");
   var discounted = price.Multiply(0.9m); // Nuevo Money
   ```

3. **Equality debe ser por valor**
   ```csharp
   // ✅ Dos emails con mismo valor son iguales
   var email1 = new Email("john@example.com");
   var email2 = new Email("john@example.com");
   Console.WriteLine(email1 == email2); // true
   ```

4. **Agrupa propiedades relacionadas**
   ```csharp
   // ✅ Address: Street, City, State, PostalCode juntos
   var address = new Address("123 Main St", "Springfield", "IL", "62701", "USA");
   ```

5. **Encapsula validaciones y lógica**
   ```csharp
   // ✅ Money: Validaciones + operaciones (Add, Multiply)
   var price = new Money(100m, "USD");
   var tax = new Money(15m, "USD");
   var total = price.Add(tax); // Valida mismo currency
   ```

### ❌ NO uses Value Objects cuando:

1. **El objeto necesita identidad única**
   ```csharp
   // ❌ User: Necesita Id, CreationDate, lifecycle
   // ✅ Usa Entity
   public class User : AbstractDomainObject
   {
       public virtual Guid Id { get; set; }
       public virtual Email Email { get; set; } // Value Object dentro
   }
   ```

2. **El objeto debe mutar después de crearse**
   ```csharp
   // ❌ Si necesitas cambiar propiedades, no es Value Object
   // ✅ Usa Entity mutable
   ```

3. **El objeto tiene relaciones con otras entidades**
   ```csharp
   // ❌ User tiene Roles (IList<Role>)
   // ✅ Usa Entity con navigation properties
   ```

4. **El objeto tiene lifecycle (Created, Updated, Deleted)**
   ```csharp
   // ❌ Si tiene CreationDate, UpdateDate, DeletedDate
   // ✅ Usa Entity
   ```

---

## Patrones y Best Practices

### ✅ DO: Usar Records en C# 13

```csharp
// ✅ BIEN: Record con init-only properties
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty");

        Value = value.ToLowerInvariant();
    }
}

// Equality by value automático con records
```

### ❌ DON'T: Usar Classes Mutables

```csharp
// ❌ MAL: Class con set properties
public class Email
{
    public string Value { get; set; } // ¡NO! set permite mutabilidad

    public Email(string value)
    {
        Value = value;
    }
}

var email = new Email("john@example.com");
email.Value = "changed@example.com"; // ¡Mutó! Value Objects deben ser immutable
```

### ✅ DO: Validar en Constructor

```csharp
// ✅ BIEN: Validación en constructor
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Invalid currency code");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}

// Si se crea, es válido
```

### ❌ DON'T: Permitir Value Objects Inválidos

```csharp
// ❌ MAL: Sin validación
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount; // ¿Puede ser negativo?
        Currency = currency; // ¿Puede ser null? ¿"ABCD"?
    }
}
```

### ✅ DO: Sealed Records

```csharp
// ✅ BIEN: sealed para evitar herencia
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        Value = value;
    }
}

// No se puede heredar de Email
```

### ✅ DO: Operaciones que Retornan Nuevos Value Objects

```csharp
// ✅ BIEN: Métodos que retornan nuevos Value Objects
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(Amount + other.Amount, Currency); // Nuevo objeto
    }
}
```

### ❌ DON'T: Mutar Value Objects

```csharp
// ❌ MAL: Método que muta
public class Money // class, no record
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }

    public void Add(Money other) // void, muta this
    {
        Amount += other.Amount; // ¡Mutó!
    }
}
```

### ✅ DO: Implicit/Explicit Conversions

```csharp
// ✅ BIEN: Conversiones para facilitar uso
public sealed record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        Value = value;
    }

    // Implicit conversion a string
    public static implicit operator string(Email email) => email.Value;

    // Explicit conversion desde string (con validación)
    public static explicit operator Email(string value) => new Email(value);
}

// Uso
var email = new Email("john@example.com");
string emailString = email; // Implicit
Email emailFromString = (Email)"jane@example.com"; // Explicit
```

### ✅ DO: Override ToString()

```csharp
// ✅ BIEN: ToString para debugging y logging
public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

// Uso
var price = new Money(100.50m, "USD");
Console.WriteLine(price); // "100.50 USD"
```

### ✅ DO: Factory Methods para Casos Complejos

```csharp
// ✅ BIEN: Factory methods para construcción compleja
public sealed record DateRange
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    // Factory methods
    public static DateRange FromDates(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date");

        return new DateRange(startDate, endDate);
    }

    public static DateRange ForYear(int year)
    {
        return new DateRange(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31)
        );
    }

    public static DateRange ForMonth(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return new DateRange(startDate, endDate);
    }
}

// Uso
var year2025 = DateRange.ForYear(2025);
var january = DateRange.ForMonth(2025, 1);
```

### ✅ DO: Namespace Específico

```csharp
// ✅ BIEN: Namespace específico para Value Objects
namespace {proyecto}.domain.valueobjects;

public sealed record Email { ... }
public sealed record Money { ... }
public sealed record Address { ... }
```

---

## Checklist para Nuevos Value Objects

Cuando crees un nuevo Value Object en APSYS, sigue esta checklist:

### 1. Evaluar Necesidad

- [ ] ¿Se define por su valor, no por identidad?
- [ ] ¿Debe ser immutable?
- [ ] ¿Equality debe ser por valor?
- [ ] ¿Agrupa propiedades relacionadas?
- [ ] ¿Tiene validaciones complejas?

### 2. Crear Value Object

- [ ] Crear archivo en `Domain/valueobjects/{ValueObject}.cs`
- [ ] Usar `sealed record` para immutability y equality
- [ ] Namespace: `{proyecto}.domain.valueobjects`
- [ ] Documentar con XML comments

### 3. Definir Propiedades

- [ ] Todas las propiedades con `{ get; init; }`
- [ ] No agregar `set` (rompe immutability)
- [ ] No agregar `Id` (Value Objects no tienen identidad)

### 4. Constructor con Validación

- [ ] Constructor con todos los parámetros requeridos
- [ ] Validaciones en constructor (throw ArgumentException si inválido)
- [ ] Normalizar valores (ej: ToLowerInvariant, ToUpperInvariant)

### 5. Métodos de Negocio (si aplica)

- [ ] Métodos que retornan nuevos Value Objects (no mutar)
- [ ] Validaciones en métodos (ej: Money.Add valida mismo currency)

### 6. Conversions y Helpers

- [ ] Override `ToString()` para debugging
- [ ] `implicit operator` si aplica (para conversión automática)
- [ ] Factory methods si construcción es compleja

### 7. Integración con NHibernate

- [ ] Mapping como `Component` en Entity map
- [ ] Mapear propiedades a columnas en tabla de Entity
- [ ] O usar `IUserType` para Value Objects simples

### 8. Tests

- [ ] Validar que constructor lanza excepción con valores inválidos
- [ ] Validar equality by value (dos instancias con mismo valor son iguales)
- [ ] Validar immutability (no se puede cambiar después de crear)
- [ ] Validar métodos de negocio

### Ejemplo Completo: Email Value Object

```csharp
// 1. Archivo: Domain/valueobjects/Email.cs
namespace {proyecto}.domain.valueobjects;

// 2. Sealed record
/// <summary>
/// Represents an email address value object.
/// </summary>
public sealed record Email
{
    // 3. Init-only property
    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; init; }

    // 4. Constructor con validación
    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="value">The email address</param>
    /// <exception cref="ArgumentException">Thrown when the email is invalid</exception>
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value.ToLowerInvariant(); // Normalizar
    }

    private static bool IsValidEmail(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    // 6. Helpers
    /// <summary>
    /// Returns the email address as a string.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Implicit conversion from Email to string.
    /// </summary>
    public static implicit operator string(Email email) => email.Value;
}

// 7. Uso en Entity
public class User : AbstractDomainObject
{
    public virtual Guid Id { get; set; }
    public virtual Email Email { get; set; }
    public virtual string Name { get; set; }

    protected User() { }

    public User(Email email, string name)
    {
        Id = Guid.NewGuid();
        Email = email;
        Name = name;
        CreationDate = DateTime.UtcNow;
    }
}

// 7. NHibernate Mapping
public class UserMap : ClassMap<User>
{
    public UserMap()
    {
        Table("Users");

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.CreationDate);
        Map(x => x.Name);

        Component(x => x.Email, email =>
        {
            email.Map(e => e.Value).Column("Email");
        });
    }
}
```

---

## Recursos Adicionales

- **Domain-Driven Design**: Eric Evans - "Domain-Driven Design: Tackling Complexity in the Heart of Software"
- **Value Objects**: Martin Fowler - https://martinfowler.com/bliki/ValueObject.html
- **C# Records**: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
- **NHibernate Components**: https://nhibernate.info/doc/nhibernate-reference/components.html

---

**Guía creada para APSYS Backend Development**
Stack: .NET 9.0, C# 13, NHibernate 5.5
Pattern: Domain-Driven Design (DDD)
