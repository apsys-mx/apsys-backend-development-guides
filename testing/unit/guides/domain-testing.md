# Entity Testing Practices - Domain Layer

**Version:** 1.0.0
**Estado:** ✅ Completo
**Última actualización:** 2025-01-20

## Descripción

Esta guía documenta las prácticas y patrones estándar para el testing de entidades del dominio en proyectos backend .NET. Las prácticas aquí descritas están basadas en los proyectos de referencia **hashira-stone-backend** y **hollow-soulmaster-backend**.

## Tabla de Contenido

1. [Configuración Inicial](#1-configuración-inicial)
   - 1.1. DomainTestBase
   - 1.2. Librerías y Frameworks
   - 1.3. AutoFixture Configuration

2. [Estructura de Test Class](#2-estructura-de-test-class)
   - 2.1. Naming Conventions
   - 2.2. Herencia y Setup
   - 2.3. Organización por Regiones
   - 2.4. Fields y Properties

3. [Tests de Constructores](#3-tests-de-constructores)
   - 3.1. Constructor Vacío
   - 3.2. Constructores Parametrizados
   - 3.3. Inicialización de Propiedades Heredadas

4. [Tests de Validación](#4-tests-de-validación)
   - 4.1. Tests de IsValid() - Happy Path
   - 4.2. Tests de IsValid() - Casos Negativos
   - 4.3. Tests de Validate() - Error Details
   - 4.4. Tests de GetValidator()

5. [Tests de Propiedades](#5-tests-de-propiedades)
   - 5.1. String Properties (null, empty, length)
   - 5.2. DateTime Properties
   - 5.3. Enum Properties
   - 5.4. Nullable Properties
   - 5.5. Collection Properties

6. [Tests de Reglas de Negocio](#6-tests-de-reglas-de-negocio)
   - 6.1. Validaciones Cross-Property
   - 6.2. Email Validation
   - 6.3. Date Range Validation
   - 6.4. Allowed Values Validation

7. [Patrones de Arranque de Datos](#7-patrones-de-arranque-de-datos)
   - 7.1. Uso de AutoFixture
   - 7.2. fixture.Create vs fixture.Build
   - 7.3. Instanciación Manual vs AutoFixture
   - 7.4. Manejo de Collections
   - 7.5. Manejo de Recursión

8. [Assertions y Mensajes](#8-assertions-y-mensajes)
   - 8.1. FluentAssertions Best Practices
   - 8.2. Mensajes Descriptivos
   - 8.3. Validación de Error Messages

9. [Edge Cases y Boundary Testing](#9-edge-cases-y-boundary-testing)
   - 9.1. Valores Límite
   - 9.2. Valores Default
   - 9.3. Null vs Empty

10. [Casos Especiales](#10-casos-especiales)
    - 10.1. AbstractDomainObject Properties
    - 10.2. Entidades con Relaciones
    - 10.3. Entidades con Business Logic Compleja

11. [Anti-Patterns a Evitar](#11-anti-patterns-a-evitar)
    - 11.1. Tests que No Usan AAA
    - 11.2. Múltiples Assertions No Relacionadas
    - 11.3. Tests Sin Mensajes Descriptivos

12. [Checklist de Testing](#12-checklist-de-testing)

13. [Referencias y Ejemplos](#13-referencias-y-ejemplos)

---

## 1. Configuración Inicial

### 1.1. DomainTestBase

Todas las clases de test de entidades deben heredar de `DomainTestBase`. Esta clase base proporciona configuración común para todos los tests de dominio, especialmente la configuración de AutoFixture.

**Implementación:**

```csharp
using AutoFixture;

namespace your.project.domain.tests.entities;

/// <summary>
/// Base class for domain entity tests that provides common test setup functionality
/// </summary>
public abstract class DomainTestBase
{
    protected internal IFixture fixture;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        fixture = new Fixture();

        // Configurar AutoFixture para evitar errores de recursión infinita
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
```

**Propósito:**
- Centralizar configuración común de AutoFixture
- Evitar repetición de código en cada test class
- Manejar recursión en entidades con relaciones circulares
- Proporcionar acceso a `fixture` para todas las clases derivadas

### 1.2. Librerías y Frameworks

**Librerías Requeridas:**

```xml
<ItemGroup>
  <!-- Test Framework -->
  <PackageReference Include="NUnit" Version="3.13.3" />
  <PackageReference Include="NUnit3TestAdapter" Version="4.2.1" />

  <!-- Assertions -->
  <PackageReference Include="FluentAssertions" Version="6.11.0" />

  <!-- Test Data Generation -->
  <PackageReference Include="AutoFixture" Version="4.18.0" />

  <!-- Test Runner -->
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.6.0" />
</ItemGroup>
```

**Propósito de cada librería:**

- **NUnit**: Framework de testing - provee `[Test]`, `[SetUp]`, `[TestCase]`, etc.
- **FluentAssertions**: Assertions legibles y expresivas con mensajes de error claros
- **AutoFixture**: Generación automática de datos de prueba, reduce boilerplate
- **Microsoft.NET.Test.Sdk**: SDK necesario para ejecutar tests en .NET

### 1.3. AutoFixture Configuration

**OmitOnRecursionBehavior** es crucial para entidades con relaciones:

```csharp
// SIN esta configuración:
var user = fixture.Create<User>();
// ❌ Error: User -> Roles -> Users -> Roles -> ... (recursión infinita)

// CON OmitOnRecursionBehavior:
var user = fixture.Create<User>();
// ✅ User con Roles = lista vacía (evita recursión)
```

**Por qué es necesario:**
- Las entidades del dominio frecuentemente tienen relaciones bidireccionales
- `User` tiene `List<Role>`, `Role` podría tener `List<User>`
- AutoFixture por defecto lanza excepción al detectar recursión
- `OmitOnRecursionBehavior` omite propiedades recursivas (las deja null/empty)

---

## 2. Estructura de Test Class

### 2.1. Naming Conventions

**Convención:**
- Archivo: `{EntityName}Tests.cs`
- Clase: `public class {EntityName}Tests`
- Método de test: `{Method}_{Scenario}_{ExpectedResult}`

**Ejemplos:**

```csharp
// ✅ CORRECTO
public class RoleTests : DomainTestBase { }
public class UserTests : DomainTestBase { }

[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse() { }

[Test]
public void Constructor_WithValidParameters_ShouldSetAllProperties() { }

// ❌ INCORRECTO
public class RoleTest { } // Falta 's' al final
public class TestRole { } // Test no debe ir al inicio

[Test]
public void Test1() { } // Nombre no descriptivo

[Test]
public void ValidateRole() { } // No sigue convención
```

### 2.2. Herencia y Setup

**Estructura estándar:**

```csharp
using AutoFixture;
using FluentAssertions;
using your.project.domain.entities;

namespace your.project.domain.tests.entities;

public class UserTests : DomainTestBase
{
    private User _user;

    [SetUp]
    public void SetUp()
    {
        // Crear instancia fresca antes de cada test
        _user = fixture.Build<User>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Roles, new List<Role>())
            .Create();
    }

    [Test]
    public void IsValid_WhenInstanceIsValid_ReturnsTrue()
    {
        // Test implementation...
    }
}
```

**Puntos clave:**
- ✅ Heredar de `DomainTestBase`
- ✅ Usar `[SetUp]` para crear instancia fresca en cada test
- ✅ Campo privado `_user` para la entidad bajo test
- ✅ Configurar datos válidos por defecto en SetUp
- ✅ Usar `fixture` heredado de DomainTestBase

### 2.3. Organización por Regiones

Organiza tests en regiones lógicas para mejor legibilidad:

```csharp
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
        // ...
    }

    #endregion

    #region Number Validation Tests

    [Test]
    public void IsValid_WhenNumberIsEmpty_ReturnsFalse()
    {
        // ...
    }

    [Test]
    public void IsValid_WhenNumberIsNull_ReturnsFalse()
    {
        // ...
    }

    #endregion

    #region IssueDate Validation Tests

    [Test]
    public void IsValid_WhenIssueDateIsNull_ReturnsFalse()
    {
        // ...
    }

    [Test]
    public void IsValid_WhenIssueDateIsFuture_ReturnsFalse()
    {
        // ...
    }

    #endregion

    #region Status Validation Tests

    [Test]
    public void IsValid_WhenStatusIsNull_ReturnsFalse()
    {
        // ...
    }

    [TestCase("Active")]
    [TestCase("Expired")]
    public void IsValid_WhenStatusIsValid_ReturnsTrue(string validStatus)
    {
        // ...
    }

    #endregion
}
```

**Regiones recomendadas:**
- `#region Constructor Tests` - Tests de constructores
- `#region Valid Instance Tests` - Happy paths
- `#region {Property} Validation Tests` - Tests por propiedad
- `#region Validate Tests` - Tests del método Validate()
- `#region GetValidator Tests` - Tests de GetValidator()
- `#region Property Setter Tests` - Tests de setters (opcional)

### 2.4. Fields y Properties

**Fields privados:**

```csharp
public class ModuleUserTests : DomainTestBase
{
    // Campo para la entidad bajo test
    private ModuleUser _moduleUser;

    // Fixture local (opcional, si necesitas configuración específica)
    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        // Opción 1: Usar fixture heredado
        _moduleUser = fixture.Create<ModuleUser>();

        // Opción 2: Usar fixture local
        _fixture = new Fixture();
        _moduleUser = _fixture.Create<ModuleUser>();
    }
}
```

**Convenciones:**
- Usar `_entityName` (con guión bajo) para la entidad bajo test
- Campo privado, no público
- Reinicializar en `[SetUp]` para garantizar aislamiento entre tests
- Opcionalmente, campo `_fixture` local si necesitas configuración específica

---

## 3. Tests de Constructores

### 3.1. Constructor Vacío

**Propósito:** Verificar que el constructor sin parámetros inicializa correctamente las propiedades.

```csharp
[Test]
public void Constructor_Empty_ShouldCreateWithDefaults()
{
    // Arrange & Act
    var role = new ModuleRole();

    // Assert
    role.Id.Should().NotBeEmpty();
    role.Name.Should().BeEmpty();
    role.CreationDate.Should().NotBe(default);
}
```

**Qué verificar:**
- ✅ `Id` se genera (no es `Guid.Empty`)
- ✅ `CreationDate` se inicializa (no es `default(DateTime)`)
- ✅ Propiedades string son empty (no null)
- ✅ Collections están inicializadas (no null)

### 3.2. Constructores Parametrizados

**Propósito:** Verificar que constructores con parámetros setean todas las propiedades correctamente.

```csharp
[Test]
public void Constructor_WithParameters_ShouldSetAllProperties()
{
    // Arrange
    var id = _fixture.Create<Guid>();
    var activeModule = _fixture.Create<ActivedModule>();
    var accessGrantedDate = _fixture.Create<DateTime>();
    var grantedByUserId = _fixture.Create<Guid>();
    var status = _fixture.Create<UserStatus>();

    // Act
    var moduleUser = new ModuleUser(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        status);

    // Assert
    moduleUser.Id.Should().Be(id);
    moduleUser.ActiveModule.Should().Be(activeModule);
    moduleUser.AccessGrantedDate.Should().Be(accessGrantedDate);
    moduleUser.GrantedByUserId.Should().Be(grantedByUserId);
    moduleUser.Status.Should().Be(status);
}
```

**Para parámetros nullable:**

```csharp
[Test]
public void Constructor_WithNullGrantedByUserId_ShouldSetPropertyToNull()
{
    // Arrange
    var id = _fixture.Create<Guid>();
    var activeModule = _fixture.Create<ActivedModule>();
    var accessGrantedDate = _fixture.Create<DateTime>();
    Guid? grantedByUserId = null; // Explícitamente null

    // Act
    var moduleUser = new ModuleUser(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        UserStatus.Active);

    // Assert
    moduleUser.GrantedByUserId.Should().BeNull();
}
```

### 3.3. Inicialización de Propiedades Heredadas

**Propósito:** Verificar que propiedades de `AbstractDomainObject` se inicializan correctamente.

```csharp
[Test]
public void Constructor_Empty_ShouldInitializeAbstractDomainObjectProperties()
{
    // Arrange & Act
    var role = new ModuleRole();

    // Assert - Propiedades de AbstractDomainObject
    role.Id.Should().NotBeEmpty("Id should be generated automatically");
    role.CreationDate.Should().NotBe(default, "CreationDate should be set to current time");
    role.CreationDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
}

[Test]
public void Constructor_WithName_ShouldInitializeAbstractDomainObjectPropertiesAndName()
{
    // Arrange
    var name = "Administrator";

    // Act
    var role = new ModuleRole(name);

    // Assert
    role.Name.Should().Be(name);
    role.Id.Should().NotBeEmpty();
    role.CreationDate.Should().NotBe(default);
}
```

---

## 4. Tests de Validación

### 4.1. Tests de IsValid() - Happy Path

**Propósito:** Verificar que instancias válidas pasan la validación.

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Arrange
    var role = new ModuleRole("Viewer");

    // Act
    var result = role.IsValid();

    // Assert
    result.Should().BeTrue("Role with valid name should pass validation");
}
```

**Para entidades complejas:**

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Arrange
    _prototype.Number = "PROTO-001";
    _prototype.Status = "Active";
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeTrue("Prototype should be valid with all valid properties");
}
```

**Con AutoFixture configurado:**

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Arrange - fixture.Build configura instancia válida
    _user = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .With(x => x.Name, "John Doe")
        .With(x => x.Roles, new List<Role>())
        .Create();

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeTrue("User should be valid with default valid properties");
}
```

### 4.2. Tests de IsValid() - Casos Negativos

**Propósito:** Verificar que instancias inválidas NO pasan la validación.

**String Properties - Null:**

```csharp
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
```

**String Properties - Empty:**

```csharp
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
```

**Email Format:**

```csharp
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
```

**DateTime Properties:**

```csharp
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
```

**Guid Properties:**

```csharp
[Test]
public void IsValid_WithEmptyId_ShouldReturnFalse()
{
    // Arrange - Guid.Empty is the specific value being tested
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.Id, Guid.Empty)
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse("ModuleUser should be invalid when Id is empty");
}
```

### 4.3. Tests de Validate() - Error Details

**Propósito:** Verificar que `Validate()` retorna errores específicos con el `PropertyName` correcto.

```csharp
[Test]
public void Validate_WithEmptyName_ShouldReturnErrors()
{
    // Arrange
    var role = new ModuleRole();
    role.Name = string.Empty;

    // Act
    var errors = role.Validate().ToList();

    // Assert
    errors.Should().NotBeEmpty("Validate should return errors for empty Name");
    errors.Should().Contain(e => e.PropertyName == "Name",
        "Error should be associated with Name property");
}
```

**Multiple Property Errors:**

```csharp
[Test]
public void Validate_WithMultipleInvalidProperties_ShouldReturnAllErrors()
{
    // Arrange
    _moduleUser.Id = Guid.Empty;
    _moduleUser.ActiveModule = null!;
    _moduleUser.AccessGrantedDate = default;

    // Act
    var errors = _moduleUser.Validate().ToList();

    // Assert
    errors.Should().NotBeEmpty();
    errors.Should().Contain(e => e.PropertyName == "Id");
    errors.Should().Contain(e => e.PropertyName == "ActiveModule");
    errors.Should().Contain(e => e.PropertyName == "AccessGrantedDate");
}
```

**Happy Path:**

```csharp
[Test]
public void Validate_WithValidData_ShouldReturnNoErrors()
{
    // Arrange
    var role = new ModuleRole("Editor");

    // Act
    var errors = role.Validate().ToList();

    // Assert
    errors.Should().BeEmpty("Validate should return no errors for valid instance");
}
```

### 4.4. Tests de GetValidator()

**Propósito:** Verificar que `GetValidator()` retorna el validator correcto.

```csharp
[Test]
public void GetValidator_ShouldReturnModuleRoleValidator()
{
    // Arrange
    var role = new ModuleRole();

    // Act
    var validator = role.GetValidator();

    // Assert
    validator.Should().NotBeNull("GetValidator should return a validator instance");
    validator.GetType().Name.Should().Be("ModuleRoleValidator",
        "Should return the correct validator type");
}
```

---

## 5. Tests de Propiedades

### 5.1. String Properties (null, empty, length)

**Null:**

```csharp
[Test]
public void IsValid_WhenNameIsNull_ReturnsFalse()
{
    // Arrange
    _role.Name = null!;

    // Act
    var isValid = _role.IsValid();

    // Assert
    isValid.Should().BeFalse("Entity should be invalid when required string is null");
}
```

**Empty:**

```csharp
[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse()
{
    // Arrange
    _role.Name = string.Empty;

    // Act
    var isValid = _role.IsValid();

    // Assert
    isValid.Should().BeFalse("Entity should be invalid when required string is empty");
}
```

**Max Length:**

```csharp
[Test]
public void IsValid_WithNameExceeding100Characters_ShouldReturnFalse()
{
    // Arrange
    var role = new ModuleRole(new string('A', 101));

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeFalse("Name exceeding 100 characters should be invalid");
}

[Test]
public void IsValid_WithNameAt100Characters_ShouldReturnTrue()
{
    // Arrange
    var role = new ModuleRole(new string('A', 100));

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeTrue("Name with exactly 100 characters should be valid");
}
```

### 5.2. DateTime Properties

**Default DateTime:**

```csharp
[Test]
public void IsValid_WithDefaultAccessGrantedDate_ShouldReturnFalse()
{
    // Arrange - Default DateTime is the specific value being tested
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.AccessGrantedDate, default(DateTime))
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse("AccessGrantedDate should not be default value");
}
```

**Future Dates:**

```csharp
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
```

**Date Ranges:**

```csharp
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
```

### 5.3. Enum Properties

**Valid Values:**

```csharp
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
    result.Should().BeTrue($"Prototype should be valid when Status is '{validStatus}'");
}
```

**Invalid Values:**

```csharp
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
    result.Should().BeFalse($"Prototype should be invalid when Status is '{invalidStatus}'");
}
```

**Null Enum:**

```csharp
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
```

### 5.4. Nullable Properties

**Null is Valid:**

```csharp
[Test]
public void IsValid_WithNullGrantedByUserId_ShouldReturnTrue()
{
    // Arrange - Null GrantedByUserId is valid for administrators
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.GrantedByUserId, (Guid?)null)
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeTrue("GrantedByUserId can be null for administrators");
}
```

**Setter to Null:**

```csharp
[Test]
public void GrantedByUserId_Property_ShouldBeSettableToNull()
{
    // Arrange
    var moduleUser = new ModuleUser();
    moduleUser.GrantedByUserId = _fixture.Create<Guid>();

    // Act
    moduleUser.GrantedByUserId = null;

    // Assert
    moduleUser.GrantedByUserId.Should().BeNull();
}
```

### 5.5. Collection Properties

**Empty Collection is Valid:**

```csharp
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
```

**Null Collection is Invalid:**

```csharp
[Test]
public void IsValid_WhenRolesIsNull_ReturnsFalse()
{
    // Arrange
    _user.Roles = null!;

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeFalse("User should be invalid when Roles collection is null");
}
```

---

## 6. Tests de Reglas de Negocio

### 6.1. Validaciones Cross-Property

**Ejemplo: ExpirationDate debe ser mayor que IssueDate**

```csharp
[Test]
public void IsValid_WhenExpirationDateIsBeforeIssueDate_ReturnsFalse()
{
    // Arrange
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddDays(-1);

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeFalse(
        "Prototype should be invalid when ExpirationDate is before IssueDate");
}

[Test]
public void IsValid_WhenExpirationDateIsAfterIssueDate_ReturnsTrue()
{
    // Arrange
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
    _prototype.Status = "Active";

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeTrue(
        "Prototype should be valid when ExpirationDate is after IssueDate");
}
```

### 6.2. Email Validation

```csharp
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
[TestCase("invalid.email")]
[TestCase("invalid@")]
[TestCase("@invalid.com")]
[TestCase("invalid@domain")]
[TestCase("spaces in@email.com")]
public void IsValid_WhenEmailFormatIsInvalid_ReturnsFalse(string invalidEmail)
{
    // Arrange
    _user.Email = invalidEmail;

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeFalse(
        $"User should be invalid when Email format is incorrect: {invalidEmail}");
}

[Test]
[TestCase("valid@example.com")]
[TestCase("user.name@domain.com")]
[TestCase("user+tag@example.co.uk")]
public void IsValid_WhenEmailFormatIsValid_ReturnsTrue(string validEmail)
{
    // Arrange
    _user.Email = validEmail;

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeTrue(
        $"User should be valid when Email format is correct: {validEmail}");
}
```

### 6.3. Date Range Validation

```csharp
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
    result.Should().BeFalse(
        "Prototype should be invalid when IssueDate is in the future");
}

[Test]
public void IsValid_WhenIssueDateIsTodayOrPast_ReturnsTrue()
{
    // Arrange
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
    _prototype.Status = "Active";

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeTrue(
        "Prototype should be valid when IssueDate is today or in the past");
}
```

### 6.4. Allowed Values Validation

```csharp
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
[TestCase("Draft")]
public void IsValid_WhenStatusIsNotAllowed_ReturnsFalse(string invalidStatus)
{
    // Arrange
    _prototype.Status = invalidStatus;

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeFalse(
        $"Prototype should be invalid when Status is not in allowed list: {invalidStatus}");
}

[TestCase("Active")]
[TestCase("Expired")]
[TestCase("Cancelled")]
public void IsValid_WhenStatusIsAllowed_ReturnsTrue(string validStatus)
{
    // Arrange
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
    _prototype.Status = validStatus;

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeTrue(
        $"Prototype should be valid when Status is in allowed list: {validStatus}");
}
```

---

## 7. Patrones de Arranque de Datos

### 7.1. Uso de AutoFixture

**Cuándo usar AutoFixture:**

✅ **USAR AutoFixture cuando:**
- Necesitas una instancia válida genérica
- Propiedades específicas no importan para el test
- Quieres reducir boilerplate
- Estás testeando comportamiento, no valores específicos

❌ **NO usar AutoFixture cuando:**
- Valores específicos son importantes para el test
- El test es sobre construcción del objeto
- Necesitas control fino sobre valores

**Ejemplo - Usar AutoFixture:**

```csharp
[Test]
public void IsValid_WithValidInstance_ReturnsTrue()
{
    // AutoFixture genera valores válidos automáticamente
    var role = fixture.Create<Role>();

    var result = role.IsValid();

    result.Should().BeTrue();
}
```

**Ejemplo - NO usar AutoFixture:**

```csharp
[Test]
public void Constructor_WithName_ShouldSetName()
{
    // Valor específico es importante, usar construcción manual
    var expectedName = "Administrator";

    var role = new ModuleRole(expectedName);

    role.Name.Should().Be(expectedName);
}
```

### 7.2. fixture.Create vs fixture.Build

**fixture.Create<T>():**
- Creación rápida con valores aleatorios
- No hay control sobre valores generados
- Útil cuando valores no importan

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Crea Role con Name aleatorio
    var role = fixture.Create<Role>();

    var result = role.IsValid();

    result.Should().BeTrue();
}
```

**fixture.Build<T>():**
- Control fino sobre propiedades específicas
- Configura valores importantes, AutoFixture genera el resto
- Útil para setear propiedades problemáticas (collections, emails, etc.)

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Controla Email y Roles, AutoFixture genera Name, etc.
    var user = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .With(x => x.Roles, new List<Role>())
        .Create();

    var result = user.IsValid();

    result.Should().BeTrue();
}
```

**Comparación:**

```csharp
// ❌ PROBLEMA: Email puede ser inválido, Roles puede causar recursión
var user = fixture.Create<User>();

// ✅ SOLUCIÓN: Controla propiedades problemáticas
var user = fixture.Build<User>()
    .With(x => x.Email, "valid@example.com")
    .With(x => x.Roles, new List<Role>())
    .Create();
```

### 7.3. Instanciación Manual vs AutoFixture

**Instanciación Manual:**

Úsala cuando:
- Estás testeando constructores
- Valores específicos son parte del test
- Necesitas null/empty/valores especiales

```csharp
[Test]
public void Constructor_WithName_ShouldSetName()
{
    // Manual: valor específico es importante
    var name = "Administrator";

    var role = new ModuleRole(name);

    role.Name.Should().Be(name);
}

[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse()
{
    // Manual: necesitas valor específico (empty)
    var role = new ModuleRole();
    role.Name = string.Empty;

    var result = role.IsValid();

    result.Should().BeFalse();
}
```

**AutoFixture:**

Úsala cuando:
- Valores específicos no importan
- Quieres reducir código
- Estás testeando comportamiento general

```csharp
[SetUp]
public void SetUp()
{
    // AutoFixture: valores válidos genéricos
    _prototype = fixture.Create<Prototype>();
}

[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Usa instancia de SetUp, no importan valores específicos
    _prototype.Status = "Active";
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);

    var result = _prototype.IsValid();

    result.Should().BeTrue();
}
```

### 7.4. Manejo de Collections

**Problema: AutoFixture puede crear collections con items que causan recursión**

```csharp
// ❌ PROBLEMA: Puede causar recursión si Role tiene referencia a User
var user = fixture.Create<User>();
// user.Roles podría tener items con referencia circular
```

**Solución: Especificar collection vacía**

```csharp
// ✅ SOLUCIÓN: Collection vacía, sin recursión
var user = fixture.Build<User>()
    .With(x => x.Roles, new List<Role>())
    .Create();
```

**En SetUp:**

```csharp
[SetUp]
public void SetUp()
{
    _user = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .With(x => x.Roles, new List<Role>()) // Evita recursión
        .Create();
}
```

**Test de collection vacía:**

```csharp
[Test]
public void IsValid_WhenRolesIsEmpty_ReturnsTrue()
{
    // Arrange
    _user.Roles.Clear();

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeTrue(
        "User should be valid even with empty Roles collection");
}
```

### 7.5. Manejo de Recursión

**Problema: Entidades con relaciones circulares**

```
User -> Roles -> Users -> Roles -> ... (infinito)
```

**Solución 1: OmitOnRecursionBehavior en DomainTestBase**

```csharp
public abstract class DomainTestBase
{
    protected internal IFixture fixture;

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        fixture = new Fixture();

        // Configurar para omitir propiedades recursivas
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
```

**Solución 2: Especificar collections vacías con .With()**

```csharp
var user = fixture.Build<User>()
    .With(x => x.Roles, new List<Role>()) // Evita recursión
    .Create();
```

**Solución 3: .Without() para omitir propiedad**

```csharp
var user = fixture.Build<User>()
    .Without(x => x.Roles) // Roles será null
    .Create();

// Luego inicializar manualmente si es necesario
user.Roles = new List<Role>();
```

---

## 8. Assertions y Mensajes

### 8.1. FluentAssertions Best Practices

**Boolean Assertions:**

```csharp
// ✅ CORRECTO
result.Should().BeTrue();
result.Should().BeFalse();

// ❌ EVITAR
Assert.IsTrue(result);
Assert.IsFalse(result);
```

**Equality:**

```csharp
// ✅ CORRECTO
role.Name.Should().Be("Administrator");
moduleUser.Id.Should().Be(expectedId);

// ❌ EVITAR
Assert.AreEqual("Administrator", role.Name);
```

**Null Checks:**

```csharp
// ✅ CORRECTO
role.Name.Should().NotBeNull();
role.Name.Should().BeNull();

// ❌ EVITAR
Assert.IsNotNull(role.Name);
Assert.IsNull(role.Name);
```

**Empty Checks:**

```csharp
// ✅ CORRECTO - Strings
role.Name.Should().BeEmpty();
role.Name.Should().NotBeEmpty();

// ✅ CORRECTO - Collections
errors.Should().BeEmpty();
errors.Should().NotBeEmpty();

// ❌ EVITAR
Assert.AreEqual(string.Empty, role.Name);
Assert.AreEqual(0, errors.Count);
```

**Guid Checks:**

```csharp
// ✅ CORRECTO
role.Id.Should().NotBeEmpty();

// ❌ EVITAR
Assert.AreNotEqual(Guid.Empty, role.Id);
```

**DateTime Checks:**

```csharp
// ✅ CORRECTO - Para tiempo actual
role.CreationDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));

// ✅ CORRECTO - Para comparación exacta
role.IssueDate.Should().Be(expectedDate);

// ✅ CORRECTO - Para default
role.IssueDate.Should().NotBe(default);
```

**Collection Assertions:**

```csharp
// ✅ CORRECTO - Contiene elemento
errors.Should().Contain(e => e.PropertyName == "Name");

// ✅ CORRECTO - Múltiples contains
errors.Should().Contain(e => e.PropertyName == "Id");
errors.Should().Contain(e => e.PropertyName == "Name");

// ✅ CORRECTO - Not empty
errors.Should().NotBeEmpty();
```

### 8.2. Mensajes Descriptivos

**Siempre incluye el parámetro "because":**

```csharp
// ✅ CORRECTO - Con mensaje descriptivo
result.Should().BeTrue("Role should be valid with a non-empty Name");
result.Should().BeFalse("Role should be invalid when Name is null");

// ❌ EVITAR - Sin mensaje
result.Should().BeTrue();
result.Should().BeFalse();
```

**Mensajes deben explicar el "por qué":**

```csharp
// ✅ CORRECTO - Explica por qué debe ser true/false
role.Id.Should().NotBeEmpty("Id should be generated automatically");
role.CreationDate.Should().NotBe(default, "CreationDate should be set to current time");

// ❌ EVITAR - Mensaje no agrega valor
role.Id.Should().NotBeEmpty("Id is not empty");
```

**Para TestCase, incluye el valor en el mensaje:**

```csharp
[TestCase("invalid.email")]
[TestCase("invalid@")]
[TestCase("@invalid.com")]
public void IsValid_WhenEmailFormatIsInvalid_ReturnsFalse(string invalidEmail)
{
    _user.Email = invalidEmail;

    var result = _user.IsValid();

    // ✅ CORRECTO - Incluye el valor que causó el problema
    result.Should().BeFalse(
        $"User should be invalid when Email format is incorrect: {invalidEmail}");
}
```

**Para cross-property validations:**

```csharp
// ✅ CORRECTO - Explica la regla de negocio
result.Should().BeFalse(
    "Prototype should be invalid when ExpirationDate is before IssueDate");

result.Should().BeTrue(
    "User should be valid even with empty Roles collection");
```

### 8.3. Validación de Error Messages

**Verificar PropertyName:**

```csharp
[Test]
public void Validate_WithEmptyName_ShouldReturnErrors()
{
    // Arrange
    var role = new ModuleRole();

    // Act
    var errors = role.Validate().ToList();

    // Assert
    errors.Should().NotBeEmpty("Validate should return errors for empty Name");
    errors.Should().Contain(e => e.PropertyName == "Name",
        "Error should be associated with Name property");
}
```

**Multiple Property Errors:**

```csharp
[Test]
public void Validate_WithMultipleInvalidProperties_ShouldReturnAllErrors()
{
    // Arrange
    _moduleUser.Id = Guid.Empty;
    _moduleUser.ActiveModule = null!;
    _moduleUser.AccessGrantedDate = default;

    // Act
    var errors = _moduleUser.Validate().ToList();

    // Assert
    errors.Should().NotBeEmpty();
    errors.Should().Contain(e => e.PropertyName == "Id",
        "Should have error for invalid Id");
    errors.Should().Contain(e => e.PropertyName == "ActiveModule",
        "Should have error for null ActiveModule");
    errors.Should().Contain(e => e.PropertyName == "AccessGrantedDate",
        "Should have error for default AccessGrantedDate");
}
```

**Error Message Content (opcional):**

```csharp
[Test]
public void Validate_WithEmptyName_ShouldReturnSpecificErrorMessage()
{
    // Arrange
    var role = new ModuleRole();

    // Act
    var errors = role.Validate().ToList();

    // Assert
    var nameError = errors.FirstOrDefault(e => e.PropertyName == "Name");
    nameError.Should().NotBeNull();
    nameError!.ErrorMessage.Should().Contain("required",
        "Error message should indicate Name is required");
}
```

---

## 9. Edge Cases y Boundary Testing

### 9.1. Valores Límite

**String Length - Exactamente en el límite:**

```csharp
[Test]
public void IsValid_WithNameAt100Characters_ShouldReturnTrue()
{
    // Arrange - Exactamente 100 caracteres
    var role = new ModuleRole(new string('A', 100));

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeTrue(
        "Name with exactly 100 characters should be valid");
}
```

**String Length - Un carácter sobre el límite:**

```csharp
[Test]
public void IsValid_WithNameExceeding100Characters_ShouldReturnFalse()
{
    // Arrange - 101 caracteres
    var role = new ModuleRole(new string('A', 101));

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "Name exceeding 100 characters should be invalid");
}
```

**String Length - Un carácter bajo el límite:**

```csharp
[Test]
public void IsValid_WithNameAt99Characters_ShouldReturnTrue()
{
    // Arrange - 99 caracteres
    var role = new ModuleRole(new string('A', 99));

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeTrue(
        "Name with 99 characters should be valid");
}
```

**Numeric Ranges:**

```csharp
[TestCase(0)]   // Límite inferior
[TestCase(1)]   // Mínimo válido
[TestCase(100)] // Máximo válido
[TestCase(101)] // Sobre límite superior
public void IsValid_WithAgeBoundaryValues_ShouldValidateCorrectly(int age)
{
    // Test implementación depende de reglas de negocio...
}
```

### 9.2. Valores Default

**Guid.Empty:**

```csharp
[Test]
public void IsValid_WithEmptyId_ShouldReturnFalse()
{
    // Arrange - Guid.Empty is the specific value being tested
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.Id, Guid.Empty)
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "ModuleUser should be invalid when Id is Guid.Empty");
}
```

**default(DateTime):**

```csharp
[Test]
public void IsValid_WithDefaultAccessGrantedDate_ShouldReturnFalse()
{
    // Arrange - Default DateTime (01/01/0001 00:00:00)
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.AccessGrantedDate, default(DateTime))
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "ModuleUser should be invalid when AccessGrantedDate is default");
}
```

**default(Enum):**

```csharp
[Test]
public void IsValid_WithDefaultStatus_ShouldReturnFalse()
{
    // Arrange - default(UserStatus) = 0
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.Status, default(UserStatus))
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "ModuleUser should be invalid when Status is default value");
}
```

### 9.3. Null vs Empty

**Para Strings:**

```csharp
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
public void IsValid_WhenNameIsWhitespace_ReturnsFalse()
{
    // Arrange
    _role.Name = "   ";

    // Act
    var result = _role.IsValid();

    // Assert
    result.Should().BeFalse("Role should be invalid when Name is whitespace");
}
```

**Para Collections:**

```csharp
[Test]
public void IsValid_WhenRolesIsNull_ReturnsFalse()
{
    // Arrange
    _user.Roles = null!;

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeFalse("User should be invalid when Roles is null");
}

[Test]
public void IsValid_WhenRolesIsEmpty_ReturnsTrue()
{
    // Arrange
    _user.Roles = new List<Role>(); // Empty pero no null

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeTrue(
        "User should be valid with empty Roles collection");
}
```

**Para Nullable Properties:**

```csharp
[Test]
public void IsValid_WhenOptionalPropertyIsNull_ReturnsTrue()
{
    // Arrange
    _moduleUser.GrantedByUserId = null; // Null es válido

    // Act
    var result = _moduleUser.IsValid();

    // Assert
    result.Should().BeTrue(
        "ModuleUser should be valid when optional GrantedByUserId is null");
}
```

---

## 10. Casos Especiales

### 10.1. AbstractDomainObject Properties

Todas las entidades heredan de `AbstractDomainObject` que provee:
- `Id` (Guid)
- `CreationDate` (DateTime)
- `UpdateDate` (DateTime?)

**Test Constructor - Inicialización:**

```csharp
[Test]
public void Constructor_Empty_ShouldInitializeAbstractDomainObjectProperties()
{
    // Arrange & Act
    var role = new ModuleRole();

    // Assert
    // Id debe generarse automáticamente
    role.Id.Should().NotBeEmpty("Id should be generated automatically");

    // CreationDate debe setearse a tiempo actual
    role.CreationDate.Should().NotBe(default,
        "CreationDate should be set to current time");
    role.CreationDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1),
        "CreationDate should be approximately now");

    // UpdateDate debe ser null inicialmente
    role.UpdateDate.Should().BeNull(
        "UpdateDate should be null for newly created entity");
}
```

**Test Validation - Id no puede ser Guid.Empty:**

```csharp
[Test]
public void IsValid_WithEmptyId_ShouldReturnFalse()
{
    // Arrange
    var role = new ModuleRole("Admin");
    role.Id = Guid.Empty; // Forzar Id inválido

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "Entity should be invalid when Id is Guid.Empty");
}
```

**Test Validation - CreationDate no puede ser default:**

```csharp
[Test]
public void IsValid_WithDefaultCreationDate_ShouldReturnFalse()
{
    // Arrange
    var role = new ModuleRole("Admin");
    role.CreationDate = default; // Forzar fecha inválida

    // Act
    var isValid = role.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "Entity should be invalid when CreationDate is default");
}
```

### 10.2. Entidades con Relaciones

**Navigation Properties - Collection:**

```csharp
public class User : AbstractDomainObject
{
    public string Name { get; set; }
    public string Email { get; set; }
    public virtual ICollection<Role> Roles { get; set; } // Navigation property
}
```

**Test - Collection no debe ser null:**

```csharp
[Test]
public void IsValid_WhenRolesIsNull_ReturnsFalse()
{
    // Arrange
    _user.Roles = null!;

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeFalse(
        "User should be invalid when Roles collection is null");
}
```

**Test - Collection vacía es válida:**

```csharp
[Test]
public void IsValid_WhenRolesIsEmpty_ReturnsTrue()
{
    // Arrange
    _user.Roles.Clear();

    // Act
    var result = _user.IsValid();

    // Assert
    result.Should().BeTrue(
        "User should be valid with empty Roles collection");
}
```

**SetUp con Collections:**

```csharp
[SetUp]
public void SetUp()
{
    _user = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .With(x => x.Roles, new List<Role>()) // Evita recursión
        .Create();
}
```

**Navigation Properties - Single Reference:**

```csharp
public class ModuleUser : AbstractDomainObject
{
    public virtual ActivedModule ActiveModule { get; set; } // Navigation property
}
```

**Test - Reference no debe ser null:**

```csharp
[Test]
public void IsValid_WithNullActiveModule_ShouldReturnFalse()
{
    // Arrange
    var moduleUser = fixture.Build<ModuleUser>()
        .With(mu => mu.ActiveModule, (ActivedModule)null!)
        .Create();

    // Act
    var isValid = moduleUser.IsValid();

    // Assert
    isValid.Should().BeFalse(
        "ModuleUser should be invalid when ActiveModule is null");
}
```

### 10.3. Entidades con Business Logic Compleja

**Ejemplo: Prototype con múltiples validaciones interdependientes**

```csharp
public class Prototype : AbstractDomainObject
{
    public string Number { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Status { get; set; }
}
```

**Validaciones:**
1. Number: requerido, no vacío
2. IssueDate: requerido, no puede ser futuro
3. ExpirationDate: requerido, debe ser mayor que IssueDate
4. Status: debe ser uno de ["Active", "Expired", "Cancelled"]

**Tests:**

```csharp
[Test]
public void IsValid_WhenAllPropertiesAreValid_ReturnsTrue()
{
    // Arrange - Configurar TODAS las propiedades correctamente
    _prototype.Number = "PROTO-001";
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
    _prototype.Status = "Active";

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeTrue(
        "Prototype should be valid when all properties meet requirements");
}

[Test]
public void IsValid_WhenExpirationDateIsBeforeIssueDate_ReturnsFalse()
{
    // Arrange - Cross-property validation
    _prototype.Number = "PROTO-001";
    _prototype.IssueDate = DateTime.Today;
    _prototype.ExpirationDate = DateTime.Today.AddDays(-1); // Antes de IssueDate
    _prototype.Status = "Active";

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeFalse(
        "Prototype should be invalid when ExpirationDate is before IssueDate");
}

[Test]
public void IsValid_WhenIssueDateIsFuture_ReturnsFalse()
{
    // Arrange - Business rule: IssueDate no puede ser futuro
    _prototype.Number = "PROTO-001";
    _prototype.IssueDate = DateTime.Today.AddDays(1); // Futuro
    _prototype.ExpirationDate = DateTime.Today.AddMonths(1);
    _prototype.Status = "Active";

    // Act
    var result = _prototype.IsValid();

    // Assert
    result.Should().BeFalse(
        "Prototype should be invalid when IssueDate is in the future");
}
```

**Tip:** Para entidades complejas, organiza tests por categorías usando regiones:

```csharp
#region Valid Instance Tests
// Happy path
#endregion

#region Number Validation Tests
// Tests específicos de Number
#endregion

#region IssueDate Validation Tests
// Tests específicos de IssueDate
#endregion

#region ExpirationDate Validation Tests
// Tests específicos de ExpirationDate
// Incluyendo cross-property con IssueDate
#endregion

#region Status Validation Tests
// Tests de valores permitidos
#endregion
```

---

## 11. Anti-Patterns a Evitar

### 11.1. Tests que No Usan AAA

**❌ ANTI-PATTERN:**

```csharp
[Test]
public void TestRoleValidation()
{
    var role = new Role();
    role.Name = null!;
    Assert.IsFalse(role.IsValid());

    role.Name = "Admin";
    Assert.IsTrue(role.IsValid());

    role.Name = "";
    Assert.IsFalse(role.IsValid());
}
```

**Problemas:**
- Múltiples escenarios en un test
- No sigue patrón AAA
- Difícil identificar qué falló si el test falla
- Mezcla assertions de diferentes conceptos

**✅ CORRECTO:**

```csharp
[Test]
public void IsValid_WhenNameIsNull_ReturnsFalse()
{
    // Arrange
    var role = new Role();
    role.Name = null!;

    // Act
    var result = role.IsValid();

    // Assert
    result.Should().BeFalse("Role should be invalid when Name is null");
}

[Test]
public void IsValid_WhenNameIsValid_ReturnsTrue()
{
    // Arrange
    var role = new Role();
    role.Name = "Admin";

    // Act
    var result = role.IsValid();

    // Assert
    result.Should().BeTrue("Role should be valid with a valid Name");
}

[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse()
{
    // Arrange
    var role = new Role();
    role.Name = string.Empty;

    // Act
    var result = role.IsValid();

    // Assert
    result.Should().BeFalse("Role should be invalid when Name is empty");
}
```

### 11.2. Múltiples Assertions No Relacionadas

**❌ ANTI-PATTERN:**

```csharp
[Test]
public void TestUserProperties()
{
    // Arrange
    var user = new User();

    // Assert
    user.Id.Should().NotBeEmpty();
    user.Email.Should().NotBeNull();
    user.Roles.Should().NotBeNull();

    user.Email = "invalid";
    user.IsValid().Should().BeFalse();

    user.Email = "valid@test.com";
    user.IsValid().Should().BeTrue();
}
```

**Problemas:**
- Mezcla tests de inicialización con validación
- Si primera assertion falla, no se ejecutan las demás
- No queda claro qué exactamente está siendo testeado

**✅ CORRECTO:**

```csharp
[Test]
public void Constructor_Empty_ShouldInitializeProperties()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Id.Should().NotBeEmpty();
    user.CreationDate.Should().NotBe(default);
}

[Test]
public void Constructor_Empty_ShouldInitializeCollections()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Roles.Should().NotBeNull();
    user.Roles.Should().BeEmpty();
}

[Test]
public void IsValid_WhenEmailIsInvalid_ReturnsFalse()
{
    // Arrange
    var user = new User();
    user.Email = "invalid";

    // Act
    var result = user.IsValid();

    // Assert
    result.Should().BeFalse();
}

[Test]
public void IsValid_WhenEmailIsValid_ReturnsTrue()
{
    // Arrange
    var user = new User();
    user.Email = "valid@test.com";

    // Act
    var result = user.IsValid();

    // Assert
    result.Should().BeTrue();
}
```

### 11.3. Tests Sin Mensajes Descriptivos

**❌ ANTI-PATTERN:**

```csharp
[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse()
{
    // Arrange
    _role.Name = string.Empty;

    // Act
    var result = _role.IsValid();

    // Assert
    result.Should().BeFalse(); // ❌ Sin mensaje
}
```

**Cuando el test falla:**
```
Expected result to be false, but found true.
```
No da contexto de POR QUÉ debería ser false.

**✅ CORRECTO:**

```csharp
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
```

**Cuando el test falla:**
```
Expected result to be false because Role should be invalid when Name is empty, but found true.
```
Mensaje claro que explica la expectativa.

**Más ejemplos correctos:**

```csharp
// ✅ CORRECTO
role.Id.Should().NotBeEmpty("Id should be generated automatically");

// ✅ CORRECTO
errors.Should().Contain(e => e.PropertyName == "Name",
    "Error should be associated with Name property");

// ✅ CORRECTO
result.Should().BeFalse(
    $"User should be invalid when Email format is incorrect: {invalidEmail}");

// ✅ CORRECTO
moduleUser.GrantedByUserId.Should().BeNull(
    "GrantedByUserId can be null for administrators");
```

---

## 12. Checklist de Testing

Cuando crees tests para una nueva entidad, asegúrate de cubrir:

### Constructor(es)
- [ ] Constructor vacío inicializa correctamente
  - [ ] Id no es Guid.Empty
  - [ ] CreationDate no es default
  - [ ] String properties son empty (no null)
  - [ ] Collections están inicializadas (no null)
- [ ] Constructores parametrizados setean todas las propiedades
  - [ ] Parámetros required se asignan correctamente
  - [ ] Parámetros nullable pueden ser null
- [ ] Propiedades heredadas (Id, CreationDate) se inicializan
  - [ ] Id se genera automáticamente
  - [ ] CreationDate se setea a tiempo actual

### IsValid() Method
- [ ] Happy path con instancia válida retorna true
- [ ] Casos negativos para cada regla de validación:
  - [ ] String properties: null, empty, whitespace
  - [ ] DateTime properties: default, futuros/pasados según regla
  - [ ] Guid properties: Guid.Empty
  - [ ] Enum properties: valores fuera del rango permitido
  - [ ] Nullable properties: null cuando no es permitido
  - [ ] Collections: null cuando no es permitido
  - [ ] Cross-property validations

### Validate() Method
- [ ] Retorna errores con PropertyName correcto para cada propiedad inválida
- [ ] Happy path retorna lista vacía
- [ ] Múltiples propiedades inválidas retornan múltiples errores

### GetValidator() Method
- [ ] Retorna el validator correcto (tipo y no null)

### Property Validation

**String Properties:**
- [ ] Null retorna false (si es required)
- [ ] Empty retorna false (si es required)
- [ ] Whitespace retorna false (si aplica)
- [ ] Max length: valor exacto en límite es válido
- [ ] Max length: valor sobre límite es inválido

**DateTime Properties:**
- [ ] default(DateTime) es inválido (si es required)
- [ ] Fecha futura es válida/inválida según regla de negocio
- [ ] Fecha pasada es válida/inválida según regla de negocio

**Enum Properties:**
- [ ] Valores dentro del rango permitido son válidos
- [ ] Valores fuera del rango permitido son inválidos
- [ ] default(Enum) es inválido (si es required)

**Nullable Properties:**
- [ ] null es válido/inválido según regla de negocio
- [ ] Valor no-null válido pasa validación

**Collections:**
- [ ] null es inválido (si es required)
- [ ] Empty collection es válida (usualmente)
- [ ] Collection con items válidos pasa validación

### Business Rules
- [ ] Validaciones cross-property funcionan correctamente
  - [ ] ExpirationDate > IssueDate
  - [ ] EndDate > StartDate
  - [ ] etc.
- [ ] Formatos específicos son validados
  - [ ] Email format
  - [ ] Phone format
  - [ ] etc.
- [ ] Rangos permitidos son validados
  - [ ] Age >= 18
  - [ ] Quantity > 0
  - [ ] etc.
- [ ] Allowed values son validados
  - [ ] Status in ["Active", "Inactive"]
  - [ ] Type in ["TypeA", "TypeB", "TypeC"]
  - [ ] etc.

### Property Setters (opcional)
- [ ] Propiedades son settables
- [ ] Nullable properties pueden setearse a null
- [ ] Valores seteados se persisten correctamente

### Organización y Calidad
- [ ] Tests organizados por regiones lógicas
- [ ] Nombres de tests siguen convención {Method}_{Scenario}_{ExpectedResult}
- [ ] Todos los tests siguen patrón AAA (Arrange-Act-Assert)
- [ ] Todos los assertions tienen mensajes descriptivos ("because" parameter)
- [ ] SetUp crea instancia fresca antes de cada test
- [ ] AutoFixture configurado correctamente (OmitOnRecursionBehavior)
- [ ] Collections configuradas para evitar recursión (.With(x => x.Roles, new List<Role>()))

---

## 13. Referencias y Ejemplos

Ver ejemplos completos en:

- [Simple Entity Example - Role](../examples/entities/simple/Role.md)
- [Medium Complexity - User](../examples/entities/medium/User.md)
- [Complex Entity - Prototype](../examples/entities/complex/Prototype.md)
- [hashira-stone Project Examples](../examples/entities/by-project/hashira-stone/)

**Guías Relacionadas:**
- [Entity Guidelines](entities.md)
- [Testing Conventions](../../testing-conventions.md)
- [Validators](validators.md)

---

**Proyectos de Referencia:**
- hashira.stone.backend - `tests/hashira.stone.backend.domain.tests/entities/`
- hollow.soulmaster.backend - `tests/hollow.soulmaster.backend.domain.tests/entities/`

---

## Resumen de Convenciones

### Naming
- Test class: `{EntityName}Tests.cs`
- Test method: `{Method}_{Scenario}_{ExpectedResult}`

### Estructura
- Heredar de `DomainTestBase`
- Campo privado `_entity` para entidad bajo test
- `[SetUp]` para crear instancia fresca
- Organizar con `#region` por tipo de test

### AutoFixture
- `fixture.Create<T>()` para valores genéricos
- `fixture.Build<T>().With(...).Create()` para control fino
- Siempre configurar collections: `.With(x => x.Roles, new List<Role>())`
- `OmitOnRecursionBehavior` en DomainTestBase

### Assertions
- Usar FluentAssertions: `.Should().BeTrue()`, `.Should().BeFalse()`, etc.
- Siempre incluir mensaje "because"
- Mensaje debe explicar el "por qué"

### AAA Pattern
```csharp
// Arrange
_entity.Property = value;

// Act
var result = _entity.IsValid();

// Assert
result.Should().BeTrue("porque...");
```

### Tests Mínimos por Entidad
1. Constructor(es)
2. IsValid() - Happy path
3. IsValid() - Negative cases (una por cada regla de validación)
4. Validate() - Retorna errores con PropertyName
5. GetValidator() - Retorna validator correcto

---

**Última revisión:** 2025-01-20
