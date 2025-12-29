# Prototype Entity - Complex Example

**Complejidad:** Compleja
**Proyecto:** hashira-stone-backend
**Ubicación:** `domain/entities/Prototype.cs`

## Descripción

`Prototype` es un ejemplo de **entidad compleja** con validaciones cross-property, DateTime properties, y reglas de negocio interdependientes. Demuestra validaciones avanzadas con FluentValidation.

## Características

- ✅ DateTime properties con validaciones de rango
- ✅ Cross-property validation (ExpirationDate > IssueDate)
- ✅ Allowed values validation (Status)
- ✅ Business rules (IssueDate no puede ser futuro)
- ✅ Múltiples tipos de validación combinados

---

## Código Completo

### Entity

```csharp
// domain/entities/Prototype.cs
namespace hashira.stone.backend.domain.entities;

using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Represents a prototype domain object with properties for tracking its number, issue date, expiration date, and status.
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

### Validator

```csharp
// domain/entities/validators/PrototypeValidator.cs
namespace hashira.stone.backend.domain.entities.validators;

using FluentValidation;
using hashira.stone.backend.domain.entities;

public class PrototypeValidator : AbstractValidator<Prototype>
{
    public PrototypeValidator()
    {
        RuleFor(x => x.Number)
            .NotNull()
            .NotEmpty()
            .WithMessage("Prototype number is required");

        RuleFor(x => x.IssueDate)
            .NotEqual(default(DateTime))
            .WithMessage("Issue date is required");

        RuleFor(x => x.IssueDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Issue date cannot be in the future");

        RuleFor(x => x.ExpirationDate)
            .NotEqual(default(DateTime))
            .WithMessage("Expiration date is required");

        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.IssueDate)
            .WithMessage("Expiration date must be after issue date");

        RuleFor(x => x.Status)
            .NotNull()
            .NotEmpty()
            .WithMessage("Status is required");

        RuleFor(x => x.Status)
            .Must(status => new[] { "Active", "Expired", "Cancelled" }.Contains(status))
            .WithMessage("Status must be one of: Active, Expired, Cancelled");
    }
}
```

---

## Tests

```csharp
// tests/hashira.stone.backend.domain.tests/entities/PrototypeTests.cs
using AutoFixture;
using FluentAssertions;
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.tests.entities;

public class PrototypeTests : DomainTestBase
{
    private Prototype _prototype;

    [SetUp]
    public void SetUp()
    {
        _prototype = fixture.Create<Prototype>();
    }

    #region Valid Instance Tests

    [Test]
    public void IsValid_WhenInstanceIsValid_ReturnsTrue()
    {
        // Arrange
        _prototype.Status = "Active";
        _prototype.IssueDate = DateTime.Today;
        _prototype.ExpirationDate = DateTime.Today.AddMonths(1);

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeTrue("Prototype should be valid with default valid properties");
    }

    #endregion

    #region Number Validation Tests

    [Test]
    public void IsValid_WhenNumberIsEmpty_ReturnsFalse()
    {
        // Arrange
        _prototype.Number = string.Empty;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when Number is empty");
    }

    [Test]
    public void IsValid_WhenNumberIsNull_ReturnsFalse()
    {
        // Arrange
        _prototype.Number = null!;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when Number is null");
    }

    #endregion

    #region IssueDate Validation Tests

    [Test]
    public void IsValid_WhenIssueDateIsNull_ReturnsFalse()
    {
        // Arrange
        _prototype.IssueDate = default;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when IssueDate is null");
    }

    [Test]
    public void IsValid_WhenIssueDateIsFuture_ReturnsFalse()
    {
        // Arrange
        _prototype.IssueDate = DateTime.Today.AddDays(1);

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when IssueDate is in the future");
    }

    #endregion

    #region ExpirationDate Validation Tests

    [Test]
    public void IsValid_WhenExpirationDateIsNull_ReturnsFalse()
    {
        // Arrange
        _prototype.ExpirationDate = default;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when ExpirationDate is null");
    }

    [Test]
    public void IsValid_WhenExpirationDateIsBeforeIssueDate_ReturnsFalse()
    {
        // Arrange
        _prototype.IssueDate = DateTime.Today;
        _prototype.ExpirationDate = DateTime.Today.AddDays(-1);

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when ExpirationDate is before IssueDate");
    }

    #endregion

    #region Status Validation Tests

    [Test]
    public void IsValid_WhenStatusIsNull_ReturnsFalse()
    {
        // Arrange
        _prototype.Status = null!;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse("Prototype should be invalid when Status is null");
    }

    [TestCase("")]
    [TestCase("Invalid")]
    [TestCase("Pending")]
    public void IsValid_WhenStatusIsNotValid_ReturnsFalse(string invalidStatus)
    {
        // Arrange
        _prototype.Status = invalidStatus;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeFalse($"Prototype should be invalid when Status is not a valid value: {invalidStatus}");
    }

    [TestCase("Active")]
    [TestCase("Expired")]
    [TestCase("Cancelled")]
    public void IsValid_WhenStatusIsValid_ReturnsTrue(string validStatus)
    {
        // Arrange
        _prototype.IssueDate = DateTime.Today;
        _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
        _prototype.Status = validStatus;

        // Act
        var result = _prototype.IsValid();

        // Assert
        result.Should().BeTrue($"Prototype should be valid when Status is a valid value: {validStatus}");
    }

    #endregion
}
```

---

## Uso en Código

### Crear y validar Prototype

```csharp
// Crear prototype
var prototype = new Prototype(
    "PROTO-2025-001",
    DateTime.Today,
    DateTime.Today.AddMonths(6),
    "Active"
);

// Validar antes de guardar
if (!prototype.IsValid())
{
    var errors = prototype.Validate();
    foreach (var error in errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
    throw new InvalidDomainException(errors);
}

await _prototypeRepository.SaveAsync(prototype);
```

### Business Logic: Expirar Prototype

```csharp
public void ExpirePrototype(Prototype prototype)
{
    // Verificar que está activo
    if (prototype.Status != "Active")
    {
        throw new InvalidOperationException("Only active prototypes can be expired");
    }

    // Cambiar status
    prototype.Status = "Expired";

    // Validar después del cambio
    if (!prototype.IsValid())
    {
        throw new InvalidDomainException(prototype.Validate());
    }
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Cross-Property Validation** - `ExpirationDate > IssueDate`
- **DateTime Validation** - No default, no futuro
- **Allowed Values** - Status con valores específicos
- **Business Rules** - IssueDate no puede ser futuro
- **TestCase Parametrizado** - Múltiples valores en un test

### 📚 Validaciones Importantes

**Cross-Property:**
```csharp
RuleFor(x => x.ExpirationDate)
    .GreaterThan(x => x.IssueDate)
    .WithMessage("Expiration date must be after issue date");
```

**Date Range:**
```csharp
RuleFor(x => x.IssueDate)
    .LessThanOrEqualTo(DateTime.Today)
    .WithMessage("Issue date cannot be in the future");
```

**Allowed Values:**
```csharp
RuleFor(x => x.Status)
    .Must(status => new[] { "Active", "Expired", "Cancelled" }.Contains(status))
    .WithMessage("Status must be one of: Active, Expired, Cancelled");
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Entity Testing Practices](../../entities-testing-practices.md)
- [Validators](../../validators.md)

**Ejemplos Relacionados:**
- [Role - Simple](../simple/Role.md)
- [User - Medium](../medium/User.md)

**Proyecto Real:**
- hashira.stone.backend: `domain/entities/Prototype.cs`

---

**Última actualización:** 2025-01-20
