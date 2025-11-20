# Role Entity - Simple Example

**Complejidad:** Simple
**Proyecto:** hashira-stone-backend
**Ubicación:** `domain/entities/Role.cs`

## Descripción

`Role` es un ejemplo de **entidad simple** con una única propiedad de negocio (`Name`). Demuestra el patrón básico de entidades en APSYS.

## Características

- ✅ Una sola propiedad de negocio
- ✅ Hereda de `AbstractDomainObject`
- ✅ Dos constructores (vacío + con parámetros)
- ✅ Propiedades `virtual`
- ✅ Validator integrado
- ✅ Documentación XML completa

---

## Código Completo

### Entity

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

### Validator

```csharp
// domain/entities/validators/RoleValidator.cs
namespace hashira.stone.backend.domain.entities.validators;

using FluentValidation;
using hashira.stone.backend.domain.entities;

public class RoleValidator : AbstractValidator<Role>
{
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Role name is required");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters");
    }
}
```

---

## Tests

### RoleTests.cs

```csharp
// tests/hashira.stone.backend.domain.tests/entities/RoleTests.cs
using AutoFixture;
using FluentAssertions;
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.tests.entities;

public class RoleTests : DomainTestBase
{
    private Role _role;

    [SetUp]
    public void SetUp()
    {
        _role = fixture.Create<Role>();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_Empty_ShouldCreateWithDefaults()
    {
        // Arrange & Act
        var role = new Role();

        // Assert
        role.Id.Should().NotBeEmpty();
        role.Name.Should().BeEmpty();
        role.CreationDate.Should().NotBe(default);
    }

    [Test]
    public void Constructor_WithName_ShouldSetName()
    {
        // Arrange
        var name = "Administrator";

        // Act
        var role = new Role(name);

        // Assert
        role.Name.Should().Be(name);
        role.Id.Should().NotBeEmpty();
    }

    #endregion

    #region IsValid Tests

    [Test]
    public void IsValid_WhenInstanceIsValid_ReturnsTrue()
    {
        // Act
        var result = _role.IsValid();

        // Assert
        result.Should().BeTrue("Role should be valid with a non-empty Name");
    }

    [Test]
    public void IsValid_WhenNameIsEmpty_ReturnsFalse()
    {
        // Arrange
        _role.Name = string.Empty;

        // Act
        var result = _role.IsValid();

        // Assert
        result.Should().BeFalse("Role should be invalid when Name is empty");
    }

    [Test]
    public void IsValid_WhenNameIsNull_ReturnsFalse()
    {
        // Arrange
        _role.Name = null!;

        // Act
        var result = _role.IsValid();

        // Assert
        result.Should().BeFalse("Role should be invalid when Name is null");
    }

    [Test]
    public void IsValid_WithNameExceeding100Characters_ShouldReturnFalse()
    {
        // Arrange
        _role.Name = new string('A', 101);

        // Act
        var result = _role.IsValid();

        // Assert
        result.Should().BeFalse("Role name exceeding 100 characters should be invalid");
    }

    #endregion

    #region Validate Tests

    [Test]
    public void Validate_WithEmptyName_ShouldReturnErrors()
    {
        // Arrange
        var role = new Role();

        // Act
        var errors = role.Validate().ToList();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Test]
    public void Validate_WithValidName_ShouldReturnNoErrors()
    {
        // Arrange
        var role = new Role("Admin");

        // Act
        var errors = role.Validate().ToList();

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region GetValidator Tests

    [Test]
    public void GetValidator_ShouldReturnRoleValidator()
    {
        // Arrange
        var role = new Role();

        // Act
        var validator = role.GetValidator();

        // Assert
        validator.Should().NotBeNull();
        validator.GetType().Name.Should().Be("RoleValidator");
    }

    #endregion
}
```

---

## Uso en Código

### Crear una nueva Role

```csharp
// Crear role con constructor
var adminRole = new Role("Administrator");

// Verificar que es válido
if (!adminRole.IsValid())
{
    var errors = adminRole.Validate();
    throw new InvalidDomainException(errors);
}

// Usar en repository
await roleRepository.SaveAsync(adminRole);
```

### Validación antes de guardar

```csharp
public async Task<Role> CreateRoleAsync(string roleName)
{
    // Crear entidad
    var role = new Role(roleName);

    // Validar
    if (!role.IsValid())
    {
        var errors = role.Validate();
        var errorMessages = string.Join(", ", errors.Select(e => e.ErrorMessage));
        throw new InvalidOperationException($"Role validation failed: {errorMessages}");
    }

    // Persistir
    await _roleRepository.SaveAsync(role);

    return role;
}
```

---

## Lecciones Clave

### ✅ Por qué es un buen ejemplo

1. **Simplicidad** - Una sola propiedad, fácil de entender
2. **Patrón completo** - Aunque simple, sigue todos los patrones requeridos
3. **Base para aprender** - Demuestra estructura básica antes de complejidades

### 📚 Conceptos Demostrados

- **Herencia de AbstractDomainObject** - `Id`, `CreationDate` heredados
- **Propiedades virtual** - Requerido para NHibernate
- **Dos constructores** - Vacío para ORM, parametrizado para creación
- **GetValidator()** - Integración con FluentValidation
- **Inicialización de strings** - `= string.Empty` para evitar nulls

### 🔄 Escalabilidad

Aunque `Role` es simple, puede evolucionar:

```csharp
// Agregar descripción
public virtual string Description { get; set; } = string.Empty;

// Agregar permisos
public virtual IList<Permission> Permissions { get; set; } = new List<Permission>();

// Agregar método de dominio
public void GrantPermission(Permission permission)
{
    if (!Permissions.Contains(permission))
    {
        Permissions.Add(permission);
    }
}
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Entity Testing Practices](../../entities-testing-practices.md)
- [Validators](../../validators.md)

**Ejemplos Relacionados:**
- [User - Medium Complexity](../medium/User.md)
- [Prototype - Complex](../complex/Prototype.md)

**Proyecto Real:**
- hashira.stone.backend: `domain/entities/Role.cs`
- Tests: `tests/hashira.stone.backend.domain.tests/entities/RoleTests.cs`

---

**Última actualización:** 2025-01-20
