# Best Practices - Mejores Prácticas

**Categoría:** Patrones DO (Hacer)
**Aplica a:** Todas las entidades de dominio

## Descripción

Guía de las mejores prácticas que **DEBES SEGUIR** al crear entidades de dominio en APSYS. Cada patrón incluye ejemplos correctos e incorrectos.

---

## ✅ DO #1: Siempre Heredar de AbstractDomainObject

### ✅ Correcto

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

    public override IValidator GetValidator()
        => new ProductValidator();
}
```

### ❌ Incorrecto

```csharp
// ❌ NO heredar de AbstractDomainObject
public class Product
{
    // ❌ Duplicando funcionalidad que ya existe
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // ❌ Sin validación integrada
}
```

### Por qué es importante

- ✅ Obtiene `Id` y `CreationDate` automáticamente
- ✅ Métodos `IsValid()` y `Validate()` disponibles
- ✅ Consistencia en toda la codebase
- ✅ Menos código duplicado
- ✅ Integración con FluentValidation lista

---

## ✅ DO #2: Propiedades Virtual

### ✅ Correcto

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
}
```

### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    // ❌ Falta virtual
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Locked { get; set; }
    public IList<Role> Roles { get; set; } = new List<Role>();
}
```

### Por qué es importante

- ✅ **Requerido por NHibernate** para crear proxies dinámicos
- ✅ Permite lazy loading de relaciones
- ✅ Permite change tracking
- ✅ Sin `virtual`, NHibernate NO funcionará correctamente

---

## ✅ DO #3: Dos Constructores

### ✅ Correcto

```csharp
public class User : AbstractDomainObject
{
    // 1️⃣ Constructor vacío para NHibernate
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public User()
    {
    }

    // 2️⃣ Constructor con parámetros para creación
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's full name</param>
    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
}
```

### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    // ❌ Solo un constructor - NHibernate fallará
    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
}
```

### Por qué es importante

- ✅ **Constructor vacío es OBLIGATORIO para NHibernate**
- ✅ Constructor con parámetros facilita creación en código
- ✅ Dos constructores = mejor experiencia de desarrollo
- ❌ Sin constructor vacío, NHibernate NO puede instanciar la entidad

---

## ✅ DO #4: Sobrescribir GetValidator

### ✅ Correcto

```csharp
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

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

### ❌ Incorrecto

```csharp
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

    // ❌ NO sobrescribir GetValidator
    // IsValid() y Validate() no funcionarán correctamente
}
```

### Por qué es importante

- ✅ Integra la entidad con FluentValidation
- ✅ `IsValid()` y `Validate()` funcionan automáticamente
- ✅ Validación consistente en toda la aplicación
- ❌ Sin override, no hay validación

---

## ✅ DO #5: Inicializar Colecciones

### ✅ Correcto

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    // ✅ Inicializar colecciones con new List<>()
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
    public virtual IList<Permission> Permissions { get; set; } = new List<Permission>();
}
```

### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    // ❌ Sin inicializar - puede ser null
    public virtual IList<Role> Roles { get; set; }
    public virtual IList<Permission> Permissions { get; set; }
}
```

### Por qué es importante

- ✅ Evita `NullReferenceException` al usar la colección
- ✅ Permite usar `user.Roles.Add()` sin null checks
- ✅ Constructor vacío puede ser usado sin inicializar manualmente
- ❌ Sin inicialización, el código cliente debe verificar null siempre

### Patrón Completo

```csharp
// ✅ Declaración
public virtual IList<Role> Roles { get; set; } = new List<Role>();

// ✅ Uso directo sin null check
var user = new User();
user.Roles.Add(adminRole);  // ✅ Funciona inmediatamente

// ❌ Sin inicialización requiere null check
var user = new User();
if (user.Roles == null)  // ❌ Extra boilerplate
    user.Roles = new List<Role>();
user.Roles.Add(adminRole);
```

---

## ✅ DO #6: Documentación XML

### ✅ Correcto

```csharp
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
    }

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    // ❌ Sin documentación
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }

    public User()
    {
    }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}
```

### Por qué es importante

- ✅ IntelliSense muestra documentación en el IDE
- ✅ Facilita entender el propósito de cada propiedad
- ✅ Mejor experiencia para otros desarrolladores
- ✅ Documentación automática generada (si se usa DocFX u otros)

### Template de Documentación

```csharp
/// <summary>
/// Gets or sets [descripción de la propiedad]
/// </summary>
public virtual string PropertyName { get; set; } = string.Empty;

/// <summary>
/// Initializes a new instance of the <see cref="EntityName"/> class.
/// This constructor is used by NHibernate for mapping purposes.
/// </summary>
public EntityName()
{
}

/// <summary>
/// Initializes a new instance of the <see cref="EntityName"/> class.
/// </summary>
/// <param name="paramName">The [descripción del parámetro]</param>
public EntityName(string paramName)
{
    PropertyName = paramName;
}

/// <summary>
/// Get the validator for the [EntityName] entity.
/// </summary>
public override IValidator GetValidator()
    => new EntityNameValidator();
```

---

## Resumen de Best Practices

### Checklist Completo

Al crear una entidad, asegúrate de:

- [ ] ✅ **DO #1:** Hereda de `AbstractDomainObject`
- [ ] ✅ **DO #2:** Todas las propiedades son `virtual`
- [ ] ✅ **DO #3:** Tiene constructor vacío Y constructor con parámetros
- [ ] ✅ **DO #4:** Sobrescribe `GetValidator()`
- [ ] ✅ **DO #5:** Colecciones inicializadas con `= new List<>()`
- [ ] ✅ **DO #6:** Documentación XML en propiedades y constructores

### Entidad Completa Ejemplo

```csharp
// ✅ Entidad perfecta siguiendo todas las best practices
namespace myproject.domain.entities;

using FluentValidation;
using myproject.domain.entities.validators;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AbstractDomainObject  // ✅ DO #1
{
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    public virtual string Email { get; set; } = string.Empty;  // ✅ DO #2, #6

    /// <summary>
    /// Gets or sets the user's full name
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;  // ✅ DO #2, #6

    /// <summary>
    /// Gets or sets whether the user account is locked
    /// </summary>
    public virtual bool Locked { get; set; }  // ✅ DO #2, #6

    /// <summary>
    /// Gets or sets the roles assigned to this user
    /// </summary>
    public virtual IList<Role> Roles { get; set; } = new List<Role>();  // ✅ DO #2, #5, #6

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public User()  // ✅ DO #3
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's full name</param>
    public User(string email, string name)  // ✅ DO #3
    {
        Email = email;
        Name = name;
        Locked = false;
    }

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()  // ✅ DO #4
        => new UserValidator();
}
```

---

## Testing de Best Practices

### Test: Hereda de AbstractDomainObject

```csharp
[Test]
public void User_ShouldInheritFromAbstractDomainObject()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Should().BeAssignableTo<AbstractDomainObject>();
    user.Id.Should().NotBeEmpty();
    user.CreationDate.Should().NotBe(default);
}
```

### Test: Propiedades son Virtual

```csharp
[Test]
public void EmailProperty_ShouldBeVirtual()
{
    // Arrange
    var propertyInfo = typeof(User).GetProperty("Email");

    // Act
    var isVirtual = propertyInfo?.GetGetMethod()?.IsVirtual;

    // Assert
    isVirtual.Should().BeTrue("Email property must be virtual for NHibernate");
}
```

### Test: Tiene Dos Constructores

```csharp
[Test]
public void User_ShouldHaveEmptyConstructor()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Should().NotBeNull();
    user.Id.Should().NotBeEmpty();
}

[Test]
public void User_ShouldHaveParameterizedConstructor()
{
    // Arrange
    var email = "test@example.com";
    var name = "Test User";

    // Act
    var user = new User(email, name);

    // Assert
    user.Email.Should().Be(email);
    user.Name.Should().Be(name);
}
```

### Test: GetValidator Sobrescrito

```csharp
[Test]
public void GetValidator_ShouldReturnUserValidator()
{
    // Arrange
    var user = new User();

    // Act
    var validator = user.GetValidator();

    // Assert
    validator.Should().NotBeNull();
    validator.Should().BeOfType<UserValidator>();
}
```

### Test: Colecciones Inicializadas

```csharp
[Test]
public void Roles_ShouldBeInitialized()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Roles.Should().NotBeNull();
    user.Roles.Should().BeEmpty();
}
```

---

## Lecciones Clave

### ✅ Las 6 Reglas de Oro

1. **Herencia** - Siempre heredar de `AbstractDomainObject`
2. **Virtual** - Todas las propiedades deben ser `virtual`
3. **Dos Constructores** - Vacío (NHibernate) + Parametrizado (Creación)
4. **Validator** - Sobrescribir `GetValidator()`
5. **Colecciones** - Inicializar con `= new List<>()`
6. **Documentación** - XML comments en todo

### 📚 Beneficios

- ✅ Código consistente en toda la codebase
- ✅ Compatible con NHibernate
- ✅ Validación integrada funcionando
- ✅ Menos bugs por null references
- ✅ Mejor experiencia de desarrollo
- ✅ Tests más fáciles de escribir

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Entity Testing Practices](../../entities-testing-practices.md)
- [Validators](../../validators.md)

**Patrones Relacionados:**
- [Base Class](01-base-class.md)
- [Properties](02-properties.md)
- [Constructors](03-constructors.md)
- [Validation](04-validation.md)
- [Anti-Patterns](06-anti-patterns.md) ⚠️

**Ejemplos Prácticos:**
- [Role - Simple](../simple/Role.md)
- [User - Medium](../medium/User.md)
- [Prototype - Complex](../complex/Prototype.md)

---

**Última actualización:** 2025-01-20
