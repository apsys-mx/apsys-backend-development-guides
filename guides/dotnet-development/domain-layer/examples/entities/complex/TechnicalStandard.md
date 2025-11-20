# TechnicalStandard Entity - Complex Example

**Complejidad:** Compleja
**Proyecto:** hashira-stone-backend
**Ubicación:** `domain/entities/TechnicalStandard.cs`

## Descripción

`TechnicalStandard` es un ejemplo de **entidad compleja** con múltiples propiedades string y validaciones de allowed values. Demuestra cómo manejar entidades con muchas propiedades y reglas de negocio específicas.

## Características

- ✅ Múltiples propiedades string (5+)
- ✅ Constructor con 5 parámetros
- ✅ Re-asignación de `CreationDate` en constructor
- ✅ Allowed values para Status y Type
- ✅ Documentación detallada de valores típicos

---

## Código Completo

### Entity

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

### Validator

```csharp
// domain/entities/validators/TechnicalStandardValidator.cs
namespace hashira.stone.backend.domain.entities.validators;

using FluentValidation;
using hashira.stone.backend.domain.entities;

public class TechnicalStandardValidator : AbstractValidator<TechnicalStandard>
{
    public TechnicalStandardValidator()
    {
        RuleFor(x => x.Code)
            .NotNull()
            .NotEmpty()
            .WithMessage("Code is required");

        RuleFor(x => x.Code)
            .MaximumLength(50)
            .WithMessage("Code cannot exceed 50 characters");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Edition)
            .NotNull()
            .NotEmpty()
            .WithMessage("Edition is required");

        RuleFor(x => x.Status)
            .NotNull()
            .NotEmpty()
            .WithMessage("Status is required");

        RuleFor(x => x.Status)
            .Must(status => new[] { "Active", "Deprecated" }.Contains(status))
            .WithMessage("Status must be either 'Active' or 'Deprecated'");

        RuleFor(x => x.Type)
            .NotNull()
            .NotEmpty()
            .WithMessage("Type is required");

        RuleFor(x => x.Type)
            .Must(type => new[] { "CFE", "Externa" }.Contains(type))
            .WithMessage("Type must be either 'CFE' or 'Externa'");
    }
}
```

---

## Tests

```csharp
// tests/hashira.stone.backend.domain.tests/entities/TechnicalStandardTests.cs
using AutoFixture;
using FluentAssertions;
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.tests.entities;

public class TechnicalStandardTests : DomainTestBase
{
    private TechnicalStandard _standard;

    [SetUp]
    public void SetUp()
    {
        _standard = fixture.Build<TechnicalStandard>()
            .With(x => x.Status, "Active")
            .With(x => x.Type, "CFE")
            .Create();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_Empty_ShouldCreateWithDefaults()
    {
        // Arrange & Act
        var standard = new TechnicalStandard();

        // Assert
        standard.Id.Should().NotBeEmpty();
        standard.Code.Should().BeEmpty();
        standard.CreationDate.Should().NotBe(default);
    }

    [Test]
    public void Constructor_WithParameters_ShouldSetAllProperties()
    {
        // Arrange
        var code = "CFE-001";
        var name = "Standard Name";
        var edition = "2025";
        var status = "Active";
        var type = "CFE";

        // Act
        var standard = new TechnicalStandard(code, name, edition, status, type);

        // Assert
        standard.Code.Should().Be(code);
        standard.Name.Should().Be(name);
        standard.Edition.Should().Be(edition);
        standard.Status.Should().Be(status);
        standard.Type.Should().Be(type);
        standard.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region IsValid Tests

    [Test]
    public void IsValid_WhenInstanceIsValid_ReturnsTrue()
    {
        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeTrue("TechnicalStandard should be valid with all required properties");
    }

    [Test]
    public void IsValid_WhenCodeIsEmpty_ReturnsFalse()
    {
        // Arrange
        _standard.Code = string.Empty;

        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeFalse("TechnicalStandard should be invalid when Code is empty");
    }

    [TestCase("Active")]
    [TestCase("Deprecated")]
    public void IsValid_WhenStatusIsValid_ReturnsTrue(string validStatus)
    {
        // Arrange
        _standard.Status = validStatus;

        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeTrue($"TechnicalStandard should be valid when Status is '{validStatus}'");
    }

    [TestCase("")]
    [TestCase("Invalid")]
    [TestCase("Pending")]
    public void IsValid_WhenStatusIsInvalid_ReturnsFalse(string invalidStatus)
    {
        // Arrange
        _standard.Status = invalidStatus;

        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeFalse($"TechnicalStandard should be invalid when Status is '{invalidStatus}'");
    }

    [TestCase("CFE")]
    [TestCase("Externa")]
    public void IsValid_WhenTypeIsValid_ReturnsTrue(string validType)
    {
        // Arrange
        _standard.Type = validType;

        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeTrue($"TechnicalStandard should be valid when Type is '{validType}'");
    }

    [TestCase("")]
    [TestCase("Invalid")]
    [TestCase("Internal")]
    public void IsValid_WhenTypeIsInvalid_ReturnsFalse(string invalidType)
    {
        // Arrange
        _standard.Type = invalidType;

        // Act
        var result = _standard.IsValid();

        // Assert
        result.Should().BeFalse($"TechnicalStandard should be invalid when Type is '{invalidType}'");
    }

    #endregion
}
```

---

## Uso en Código

### Crear TechnicalStandard

```csharp
// Crear technical standard
var standard = new TechnicalStandard(
    "CFE-G0100-04",
    "Coordinación de aislamiento",
    "2019",
    "Active",
    "CFE"
);

// Validar
if (!standard.IsValid())
{
    var errors = standard.Validate();
    throw new InvalidDomainException(errors);
}

await _technicalStandardRepository.SaveAsync(standard);
```

### Deprecar un Standard

```csharp
public async Task DeprecateStandardAsync(Guid standardId)
{
    // Cargar standard
    var standard = await _technicalStandardRepository.GetByIdAsync(standardId);
    if (standard == null)
    {
        throw new NotFoundException("Technical standard not found");
    }

    // Cambiar status
    standard.Status = "Deprecated";

    // Validar
    if (!standard.IsValid())
    {
        throw new InvalidDomainException(standard.Validate());
    }

    await _technicalStandardRepository.UpdateAsync(standard);
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Múltiples Propiedades** - 5 propiedades string con validaciones
- **Allowed Values** - Status y Type con valores específicos
- **Constructor Complejo** - 5 parámetros
- **Re-asignación CreationDate** - Explícitamente en constructor
- **Documentación Rica** - XML comments con valores típicos

### 📚 Patrones Importantes

**Allowed Values en Validator:**
```csharp
RuleFor(x => x.Status)
    .Must(status => new[] { "Active", "Deprecated" }.Contains(status))
    .WithMessage("Status must be either 'Active' or 'Deprecated'");
```

**Testing con TestCase:**
```csharp
[TestCase("Active")]
[TestCase("Deprecated")]
public void IsValid_WhenStatusIsValid_ReturnsTrue(string validStatus)
{
    // Test implementation...
}
```

**Constructor con muchos parámetros:**
```csharp
public TechnicalStandard(string code, string name, string edition, string status, string type)
{
    this.CreationDate = DateTime.UtcNow;  // Re-asignar
    Code = code;
    Name = name;
    Edition = edition;
    Status = status;
    Type = type;
}
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
- [Prototype - Complex](Prototype.md)

**Proyecto Real:**
- hashira.stone.backend: `domain/entities/TechnicalStandard.cs`

---

**Última actualización:** 2025-01-20
