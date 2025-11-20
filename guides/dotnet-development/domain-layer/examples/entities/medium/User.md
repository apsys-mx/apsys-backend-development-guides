# User Entity - Medium Complexity Example

**Complejidad:** Media
**Proyecto:** hashira-stone-backend
**Ubicación:** `domain/entities/User.cs`

## Descripción

`User` es un ejemplo de **entidad de complejidad media** con múltiples propiedades simples (`Email`, `Name`, `Locked`) y una colección (`Roles`). Demuestra el manejo de relaciones One-to-Many.

## Características

- ✅ Múltiples propiedades de tipos diferentes
- ✅ Colección `IList<Role>` (One-to-Many)
- ✅ Validación de Email format
- ✅ Boolean property (`Locked`)
- ✅ Tests de colecciones vacías vs null

---

## Código Completo

### Entity

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

### Validator

```csharp
// domain/entities/validators/UserValidator.cs
namespace hashira.stone.backend.domain.entities.validators;

using FluentValidation;
using hashira.stone.backend.domain.entities;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .WithMessage("Email is required");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email must be a valid email address");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles collection cannot be null");
    }
}
```

---

## Tests

```csharp
// tests/hashira.stone.backend.domain.tests/entities/UserTests.cs
using AutoFixture;
using FluentAssertions;
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.tests.entities;

public class UserTests : DomainTestBase
{
    private User _user;

    [SetUp]
    public void SetUp()
    {
        _user = fixture.Build<User>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Roles, new List<Role>())
            .Create();
    }

    #region Valid Instance Tests

    [Test]
    public void IsValid_WhenInstanceIsValid_ReturnsTrue()
    {
        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeTrue("User should be valid with default valid properties");
    }

    [Test]
    public void IsValid_WhenRolesIsEmpty_ReturnsTrue()
    {
        // Arrange
        _user.Roles.Clear();

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeTrue("User should be valid even with empty Roles collection");
    }
    #endregion

    #region Email Validation Tests

    [Test]
    public void IsValid_WhenEmailIsEmpty_ReturnsFalse()
    {
        // Arrange
        _user.Email = string.Empty;

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeFalse("User should be invalid when Email is empty");
    }

    [Test]
    public void IsValid_WhenEmailIsNull_ReturnsFalse()
    {
        // Arrange
        _user.Email = null!;

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeFalse("User should be invalid when Email is null");
    }

    [Test]
    [TestCase("invalid.email")]
    [TestCase("invalid@")]
    [TestCase("@invalid.com")]
    public void IsValid_WhenEmailFormatIsInvalid_ReturnsFalse(string invalidEmail)
    {
        // Arrange
        _user.Email = invalidEmail;

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeFalse($"User should be invalid when Email format is incorrect: {invalidEmail}");
    }

    #endregion

    #region Name Validation Tests

    [Test]
    public void IsValid_WhenNameIsEmpty_ReturnsFalse()
    {
        // Arrange
        _user.Name = string.Empty;

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeFalse("User should be invalid when Name is empty");
    }

    [Test]
    public void IsValid_WhenNameIsNull_ReturnsFalse()
    {
        // Arrange
        _user.Name = null!;

        // Act
        var result = _user.IsValid();

        // Assert
        result.Should().BeFalse("User should be invalid when Name is null");
    }

    #endregion
}
```

---

## Uso en Código

### Crear usuario y asignar roles

```csharp
// Crear usuario
var user = new User("john.doe@example.com", "John Doe");

// Crear roles
var adminRole = new Role("Administrator");
var userRole = new Role("User");

// Asignar roles
user.Roles.Add(adminRole);
user.Roles.Add(userRole);

// Validar
if (!user.IsValid())
{
    throw new InvalidDomainException(user.Validate());
}

// Guardar
await _userRepository.SaveAsync(user);
```

### Validar antes de operaciones

```csharp
public async Task<User> CreateUserWithRolesAsync(string email, string name, List<string> roleNames)
{
    // Crear usuario
    var user = new User(email, name);

    // Cargar roles de la BD
    foreach (var roleName in roleNames)
    {
        var role = await _roleRepository.GetByNameAsync(roleName);
        if (role != null)
        {
            user.Roles.Add(role);
        }
    }

    // Validar antes de guardar
    if (!user.IsValid())
    {
        var errors = user.Validate();
        var errorMessages = string.Join(", ", errors.Select(e => e.ErrorMessage));
        throw new InvalidOperationException($"User validation failed: {errorMessages}");
    }

    await _userRepository.SaveAsync(user);

    return user;
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Colecciones IList<T>** - Inicialización con `new List<>()`
- **Email Validation** - FluentValidation con `.EmailAddress()`
- **Boolean Properties** - `Locked` con valor default `false`
- **Collection Testing** - Empty vs null collection

### 📚 Patrones Importantes

**Inicialización de Collections:**
```csharp
// ✅ CORRECTO
public virtual IList<Role> Roles { get; set; } = new List<Role>();

// ❌ INCORRECTO
public virtual IList<Role> Roles { get; set; }  // Puede ser null
```

**Testing con AutoFixture:**
```csharp
_user = fixture.Build<User>()
    .With(x => x.Email, "test@example.com")
    .With(x => x.Roles, new List<Role>())  // Evita recursión
    .Create();
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Entity Testing Practices](../../entities-testing-practices.md)

**Ejemplos Relacionados:**
- [Role - Simple](../simple/Role.md)
- [Prototype - Complex](../complex/Prototype.md)

**Proyecto Real:**
- hashira.stone.backend: `domain/entities/User.cs`

---

**Última actualización:** 2025-01-20
