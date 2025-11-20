# Repository Testing Practices - NHibernate

**Version:** 1.0.0
**Estado:** ✅ Completo
**Última actualización:** 2025-01-20

## Descripción

Esta guía documenta las prácticas y patrones estándar para el testing de repositorios NHibernate en proyectos backend .NET. Las prácticas aquí descritas están basadas en los proyectos de referencia **hashira-stone-backend** y **hollow-soulmaster-backend**.

Esta guía complementa [integration-tests.md](./integration-tests.md), que cubre la infraestructura y configuración. Aquí nos enfocamos en **prácticas diarias** y **patrones de testing**.

## ⚠️ REGLA CRÍTICA

**NUNCA usar el repositorio bajo prueba en Arrange o Assert**

✅ **Arrange:** Usar `LoadScenario()` con archivos XML
✅ **Act:** Usar `RepositoryUnderTest.Method()`
✅ **Assert:** Usar `nDbUnitTest.GetDataSetFromDb()` para verificar persistencia

❌ **NO usar** el repositorio para preparar datos ni para verificar resultados

**Por qué:** Si usas el repositorio en Arrange/Assert, un bug en `CreateAsync` hará fallar tests de `UpdateAsync`, y un bug en `GetAsync` hará pasar tests aunque `CreateAsync` falle. Los tests se vuelven interdependientes y no confiables.

📖 Ver detalles en [Anti-Pattern Crítico](#121-usar-repositorio-en-arrange-y-assert)

---

## Tabla de Contenido

1. [Configuración Inicial](#1-configuración-inicial)
   - 1.1. Herencia de Clases Base
   - 1.2. BuildRepository Pattern
   - 1.3. LocalSetUp con AutoFixture

2. [Escenarios XML - Fundamento del Testing](#2-escenarios-xml---fundamento-del-testing)
   - 2.1. ¿Qué son los Escenarios?
   - 2.2. Estructura de un Escenario XML
   - 2.3. Cuándo y Cómo Usar Escenarios
   - 2.4. Ubicación y Nomenclatura

3. [Estructura de Test Class](#3-estructura-de-test-class)
   - 3.1. Naming Conventions
   - 3.2. Organización por Regiones
   - 3.3. Helper Methods

4. [Tests de CreateAsync](#4-tests-de-createasync)
   - 4.1. Happy Path - Creación Exitosa
   - 4.2. Validación de Campos Required
   - 4.3. Validación de Duplicados
   - 4.4. Validación de Formato
   - 4.5. Validación de Default Values

5. [Tests de GetAsync / GetByXXXAsync](#5-tests-de-getasync--getbyxxxasync)
   - 5.1. GetAsync - Por ID
   - 5.2. GetByXXXAsync - Métodos Custom
   - 5.3. GetAsync con Expression
   - 5.4. Get con Filtros Complejos

6. [Tests de UpdateAsync](#6-tests-de-updateasync)
   - 6.1. Actualización Exitosa
   - 6.2. Entidad No Existe
   - 6.3. Duplicados con Otra Entidad
   - 6.4. Mismo Valor (No-Op)

7. [Tests de DeleteAsync](#7-tests-de-deleteasync)
   - 7.1. Eliminación Exitosa
   - 7.2. Verificación en Base de Datos

8. [Tests de Métodos Custom](#8-tests-de-métodos-custom)
   - 8.1. Métodos de Relaciones (AddUserToRole)
   - 8.2. Métodos de Consulta Específica
   - 8.3. Métodos de Negocio

9. [Patrones de Arranque de Datos](#9-patrones-de-arranque-de-datos)
   - 9.1. LoadScenario() - Datos Predefinidos
   - 9.2. GetDataSetFromDb() - Obtener IDs
   - 9.3. AutoFixture para Valores
   - 9.4. Crear Dependencias con Otros Repositorios

10. [Verificación de Datos](#10-verificación-de-datos)
    - 10.1. Verificar Inserción
    - 10.2. Verificar Actualización
    - 10.3. Verificar Eliminación
    - 10.4. Verificar Campos Específicos
    - 10.5. Verificar Relaciones

11. [Assertions y Mensajes](#11-assertions-y-mensajes)
    - 11.1. FluentAssertions Best Practices
    - 11.2. Mensajes Descriptivos
    - 11.3. Assertions en Arrange (Preconditions)

12. [Edge Cases y Boundary Testing](#12-edge-cases-y-boundary-testing)
    - 12.1. IDs Vacíos o Nulos
    - 12.2. Fechas Default
    - 12.3. Collections Vacías vs Null
    - 12.4. Case Sensitivity

13. [Anti-Patterns a Evitar](#13-anti-patterns-a-evitar)
    - 13.1. Usar Repositorio en Arrange y Assert
    - 13.2. Tests que Dependen de Orden
    - 13.3. No Verificar en Base de Datos
    - 13.4. Hardcodear IDs

14. [Checklist de Testing](#14-checklist-de-testing)

15. [Referencias y Ejemplos](#15-referencias-y-ejemplos)

---

## 1. Configuración Inicial

> **📖 Prerequisito:** Esta guía asume que ya tienes configurada la infraestructura de testing. Ver [integration-tests.md](./integration-tests.md) para configuración de clases base, NDbUnit y ServiceProvider.

### 1.1. Herencia de Clases Base

Todos los tests de repositorios deben heredar de una de las clases base según el tipo:

**Para Repositorios CRUD (NHRepository):**

```csharp
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    // Tests...
}
```

**Para Repositorios Read-Only (NHReadOnlyRepository):**

```csharp
public class NHTechnicalStandardDaoRepositoryTests
    : NHReadOnlyRepositoryTestBase<NHTechnicalStandardDaoRepository, TechnicalStandardDao, Guid>
{
    // Tests...
}
```

**Beneficios de la herencia:**
- ✅ Acceso a `RepositoryUnderTest` - La instancia del repositorio bajo prueba
- ✅ Acceso a `_sessionFactory` - Para crear sesiones de NHibernate
- ✅ Acceso a `_serviceProvider` - Contenedor con validators
- ✅ Acceso a `nDbUnitTest` - Para cargar y verificar datos
- ✅ Acceso a `fixture` - AutoFixture configurado
- ✅ `Setup()` automático - Limpia DB antes de cada test

### 1.2. BuildRepository Pattern

Cada clase de test **debe implementar** `BuildRepository()`:

```csharp
protected internal override NHUserRepository BuildRepository()
    => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);
```

**Elementos clave:**
- `_sessionFactory.OpenSession()` - Nueva sesión para cada test
- `_serviceProvider` - Contenedor de DI con validators registrados

**Ejemplo con dependencias adicionales:**

```csharp
protected internal override NHModuleUserRepository BuildRepository()
{
    var session = _sessionFactory.OpenSession();
    return new NHModuleUserRepository(session, _serviceProvider);
}
```

### 1.3. LocalSetUp con AutoFixture

Usar `[SetUp]` local para crear datos de prueba con AutoFixture:

```csharp
private User? _testUser;

[SetUp]
public void LocalSetUp()
{
    _testUser = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .Without(x => x.Roles)  // Excluir navigation properties
        .Create();
}
```

**Cuándo usar LocalSetUp:**
- Cuando necesitas datos de prueba consistentes en múltiples tests
- Para configurar valores específicos con AutoFixture
- Para evitar repetir código de creación de objetos

**Qué configurar con .With() y .Without():**
- `.With(x => x.Email, "valid@example.com")` - Valor específico válido
- `.Without(x => x.Roles)` - Excluir collections (evita recursión)
- `.With(x => x.IssueDate, DateTime.Today)` - Fechas válidas

---

## 2. Escenarios XML - Fundamento del Testing

### 2.1. ¿Qué son los Escenarios?

Los **escenarios** son archivos XML que contienen datos de prueba predefinidos que se cargan en la base de datos antes de ejecutar un test. Son el mecanismo fundamental para preparar el estado inicial (Arrange) sin usar el repositorio bajo prueba.

**Propósito de los Escenarios:**

1. **Aislar el repositorio bajo prueba** - No usar métodos del repositorio para preparar datos
2. **Estado conocido y reproducible** - Mismos datos en cada ejecución del test
3. **Tests independientes** - Cada test carga solo los datos que necesita
4. **Facilitar mantenimiento** - Cambios en datos de prueba se hacen en un solo lugar

### 2.2. Estructura de un Escenario XML

**Ejemplo de escenario:** `CreateUsers.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Primero: Roles (sin dependencias) -->
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </roles>

  <!-- Segundo: Users (sin relaciones) -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <name>Usuario Uno</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>
  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>usuario2@example.com</email>
    <name>Usuario Dos</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <!-- Tercero: Relaciones (tablas de join) -->
  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
  </users_in_roles>
</AppSchema>
```

**Características:**
- ✅ Nombres de elementos corresponden a tablas de base de datos
- ✅ Nombres de campos corresponden a columnas (snake_case)
- ✅ IDs son GUIDs específicos (no auto-generados)
- ✅ Incluye todos los campos requeridos
- ✅ Orden de inserción respeta dependencias (roles antes de users)

### 2.3. Cuándo y Cómo Usar Escenarios

**Cuándo usar LoadScenario:**

| Método bajo prueba | ¿Usar LoadScenario? | Propósito |
|-------------------|-------------------|-----------|
| `CreateAsync` | ✅ Para tests de duplicados | Cargar entidad existente para verificar error |
| `CreateAsync` | ❌ Para happy path | No necesita datos existentes |
| `GetAsync` / `GetByXXXAsync` | ✅ Siempre | Necesita datos existentes para buscar |
| `UpdateAsync` | ✅ Siempre | Necesita entidad existente para actualizar |
| `DeleteAsync` | ✅ Siempre | Necesita entidad existente para eliminar |
| Métodos custom | ✅ Según necesidad | Depende de la lógica del método |

**Cómo usar LoadScenario:**

```csharp
[Test]
public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser()
{
    // Arrange - Cargar escenario con usuarios predefinidos
    this.LoadScenario("CreateUsers");

    // Obtener datos del escenario (NO del repositorio)
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    var existingEmail = userRow.Field<string>("email");

    // Act - ÚNICO lugar donde se usa el repositorio
    var result = await this.RepositoryUnderTest.GetByEmailAsync(existingEmail);

    // Assert - Verificar resultado
    result.Should().NotBeNull();
    result!.Email.Should().Be(existingEmail);
}
```

**Patrón estándar:**

```csharp
// Arrange
this.LoadScenario("ScenarioName");  // 1. Cargar datos
var dataSet = nDbUnitTest.GetDataSetFromDb();  // 2. Obtener datos
var id = dataSet.GetFirstRow().Field<Guid>("id");  // 3. Extraer IDs

// Act
await RepositoryUnderTest.Method(id);  // 4. Ejecutar método

// Assert
var updatedDataSet = nDbUnitTest.GetDataSetFromDb();  // 5. Verificar en DB
var row = updatedDataSet.GetRows($"id = '{id}'").First();  // 6. Obtener fila
row.Field<string>("field").Should().Be(expected);  // 7. Assert
```

### 2.4. Ubicación y Nomenclatura

**Ubicación estándar:**

```
tests/
└── {proyecto}.infrastructure.tests/
    ├── scenarios/
    │   ├── CreateUsers.xml
    │   ├── CreateRoles.xml
    │   ├── CreateAdminUser.xml
    │   ├── CreatePrototypes.xml
    │   ├── 030_ActivedModules.xml
    │   ├── 040_ModuleUsers.xml
    │   └── CreateSandBox.xml
    └── nhibernate/
        └── NH*RepositoryTests.cs
```

**Convenciones de nomenclatura:**

| Patrón | Uso | Ejemplo |
|--------|-----|---------|
| `Create{Entity}s.xml` | Entidades individuales | CreateUsers.xml |
| `Create{Scenario}.xml` | Escenario específico | CreateAdminUser.xml |
| `###_{Entity}.xml` | Con numeración (orden) | 030_ActivedModules.xml |
| `CreateSandBox.xml` | Escenario vacío | CreateSandBox.xml |

**Variable de entorno:**

```env
# .env file
SCENARIOS_FOLDER_PATH=D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\tests\scenarios
```

---

## 3. Estructura de Test Class

### 3.1. Naming Conventions

**Convención para archivos y clases:**
- Archivo: `NH{EntityName}RepositoryTests.cs`
- Clase: `public class NH{EntityName}RepositoryTests`

**Convención para métodos de test:**
```
{Method}_{Condition}_{ExpectedResult}
```

**Ejemplos correctos:**

```csharp
// ✅ CORRECTO
CreateAsync_WhenEmailIsValid_ShouldCreateUser
CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException
GetByEmailAsync_WhenEmailExists_ShouldReturnUser
GetByEmailAsync_WhenEmailDoesNotExist_ShouldReturnNull
UpdateAsync_WithValidParameters_ShouldUpdateModuleUser
UpdateAsync_WithNonExistingId_ShouldThrowResourceNotFoundException

// ❌ INCORRECTO
TestCreate
CreateTest
Test1
ValidateUser
```

### 2.2. Organización por Regiones

Organiza tests en regiones lógicas por método del repositorio:

```csharp
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    private User? _testUser;

    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    [SetUp]
    public void LocalSetUp()
    {
        _testUser = fixture.Build<User>()
            .With(x => x.Email, "test@example.com")
            .Without(x => x.Roles)
            .Create();
    }

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser() { }

    [Test]
    public async Task CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException() { }

    #endregion

    #region GetByEmailAsync Tests

    [Test]
    public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser() { }

    [Test]
    public async Task GetByEmailAsync_WhenEmailDoesNotExist_ShouldReturnNull() { }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_WithValidParameters_ShouldUpdateUser() { }

    #endregion

    #region Helper Methods

    private string GetFirstUserEmailFromDb() { }

    #endregion
}
```

**Regiones recomendadas:**
- `#region CreateAsync Tests` - Tests de creación
- `#region GetAsync Tests` - Tests de GetAsync base
- `#region GetBy{Property}Async Tests` - Tests de métodos custom de búsqueda
- `#region UpdateAsync Tests` - Tests de actualización
- `#region DeleteAsync Tests` - Tests de eliminación
- `#region {CustomMethod} Tests` - Tests de métodos específicos del repositorio
- `#region Base Repository Methods Tests` - Tests de métodos heredados
- `#region Helper Methods` - Métodos auxiliares

### 2.3. Helper Methods

Crear métodos helper para operaciones comunes:

```csharp
#region Helper Methods

/// <summary>
/// Gets the email of the first user from the database.
/// </summary>
private string GetFirstUserEmailFromDb()
{
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    userRow.Should().NotBeNull("Precondition: There should be at least one user in the dataset");
    return userRow!["email"].ToString()!;
}

/// <summary>
/// Gets the first module user row from the database for a specific granter.
/// </summary>
private DataRow? GetFirstModuleUserRowByGranter(Guid grantedByUserId)
{
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var rows = dataSet.GetModuleUsersRows($"granted_by_user_id = '{grantedByUserId}'");
    return rows.FirstOrDefault();
}

#endregion
```

**Beneficios:**
- Reduce duplicación de código
- Mejora legibilidad de tests
- Facilita mantenimiento
- Documenta precondiciones claramente

---

## 3. Tests de CreateAsync

### 3.1. Happy Path - Creación Exitosa

**Propósito:** Verificar que CreateAsync crea correctamente la entidad con datos válidos.

```csharp
[Test]
public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
{
    // Arrange
    // (_testUser ya está configurado en LocalSetUp)

    // Act
    await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

    // Assert - Verificar en base de datos
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{_testUser.Email}'");
    userRows.Count().Should().Be(1);

    var firstUser = userRows.First();
    firstUser["email"].Should().Be(_testUser.Email);
    firstUser["name"].Should().Be(_testUser.Name);
}
```

**Qué verificar:**
- ✅ Exactamente 1 fila insertada
- ✅ Valores de campos coinciden con los parámetros
- ✅ ID se generó automáticamente (no es Guid.Empty)
- ✅ CreationDate se asignó correctamente

**Ejemplo con verificación de campos adicionales:**

```csharp
[Test]
public async Task CreateAsync_WithValidParameters_ShouldCreateModuleUser()
{
    // Arrange - Load scenario to get ActivedModule
    this.LoadScenario("030_ActivedModules");

    var activedModuleRepository = new NHActivedModuleRepository(_sessionFactory.OpenSession(), _serviceProvider);
    var activedModules = await activedModuleRepository.GetActiveByOrganizationIdAsync(ApsysmxOrgId);
    var activeModule = activedModules.First();

    var id = Guid.NewGuid();
    var accessGrantedDate = DateTime.UtcNow;
    var grantedByUserId = Guid.NewGuid();
    var status = UserStatus.Active;

    // Act
    await RepositoryUnderTest.CreateAsync(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        status);

    // Assert - Verify in database using NDbUnit
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var moduleUserRows = dataSet.GetModuleUsersRows($"id = '{id}'");
    moduleUserRows.Count().Should().Be(1);

    var moduleUserRow = moduleUserRows.First();
    moduleUserRow.Field<Guid>("id").Should().Be(id);
    moduleUserRow.Field<Guid>("active_module_id").Should().Be(activeModule.Id);
    moduleUserRow.Field<Guid?>("granted_by_user_id").Should().Be(grantedByUserId);
    moduleUserRow.Field<short>("status").Should().Be((short)status);
    moduleUserRow.Field<DateTime>("creation_date").Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
}
```

### 3.2. Validación de Campos Required

**Propósito:** Verificar que campos requeridos lanzan excepción cuando son null, empty o whitespace.

**Patrón con [TestCase]:**

```csharp
[Test]
[TestCase(null)]
[TestCase("")]
[TestCase("   ")]
public async Task CreateAsync_WhenEmailIsNullOrEmpty_ShouldThrowInvalidDomainException(string? email)
{
    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(email!, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}

[Test]
[TestCase(null)]
[TestCase("")]
[TestCase("   ")]
public async Task CreateAsync_WhenNameIsNullOrEmpty_ShouldThrowInvalidDomainException(string? name)
{
    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, name!);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}
```

**Excepciones esperadas por tipo de validación:**
- `InvalidDomainException` - Validación de FluentValidation
- `ArgumentNullException` - Validación de argumentos null (menos común)

### 3.3. Validación de Duplicados

**Propósito:** Verificar que no se pueden crear entidades duplicadas.

```csharp
[Test]
public async Task CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException()
{
    // Arrange - Cargar escenario con usuarios existentes
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(existingEmail!, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>()
       .WithMessage($"A user with the email '{existingEmail}' already exists.");
}
```

**Patrón con case variations:**

```csharp
[TestCase("PR-001")]
[TestCase("pr-001")]
[TestCase("Pr-001")]
[TestCase("pR-001")]
public async Task CreateAsync_WithDuplicateNumberCaseVariation_ThrowsDuplicatedDomainException(string number)
{
    // Arrange - Crear primer registro
    LoadScenario("CreateSandBox");
    string baseNumber = "PR-001";
    await this.RepositoryUnderTest.CreateAsync(
        baseNumber,
        _testPrototype!.IssueDate,
        _testPrototype.ExpirationDate,
        _testPrototype.Status);

    // Act - Intentar crear con variación de case
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(
        number,
        _testPrototype!.IssueDate,
        _testPrototype.ExpirationDate,
        _testPrototype.Status);

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>();
}
```

### 3.4. Validación de Formato

**Propósito:** Verificar que campos con formato específico (email, phone, etc.) son validados.

```csharp
[TestCase("invalid-email-format")]
[TestCase("user@.com")]
[TestCase("user@com")]
[TestCase("user.com")]
[TestCase("@example.com")]
public async Task CreateAsync_WhenEmailIsWrongFormat_ShouldThrowInvalidDomainException(string wrongEmail)
{
    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(wrongEmail, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}
```

### 3.5. Validación de Default Values

**Propósito:** Verificar que propiedades con valores default (Guid.Empty, default(DateTime)) son rechazadas.

```csharp
[Test]
public async Task CreateAsync_WithEmptyId_ShouldThrowInvalidDomainException()
{
    // Arrange - Load scenario to get ActivedModule
    this.LoadScenario("030_ActivedModules");

    var activedModuleRepository = new NHActivedModuleRepository(_sessionFactory.OpenSession(), _serviceProvider);
    var activedModules = await activedModuleRepository.GetActiveByOrganizationIdAsync(ApsysmxOrgId);
    var activeModule = activedModules.First();

    var id = Guid.Empty; // ❌ Invalid
    var accessGrantedDate = DateTime.UtcNow;
    var grantedByUserId = Guid.NewGuid();
    var status = UserStatus.Active;

    // Act
    Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        status);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}

[Test]
public async Task CreateAsync_WithDefaultAccessGrantedDate_ShouldThrowInvalidDomainException()
{
    // Arrange
    this.LoadScenario("030_ActivedModules");

    var activedModuleRepository = new NHActivedModuleRepository(_sessionFactory.OpenSession(), _serviceProvider);
    var activedModules = await activedModuleRepository.GetActiveByOrganizationIdAsync(ApsysmxOrgId);
    var activeModule = activedModules.First();

    var id = Guid.NewGuid();
    var accessGrantedDate = default(DateTime); // ❌ Invalid
    var grantedByUserId = Guid.NewGuid();
    var status = UserStatus.Active;

    // Act
    Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        status);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}
```

---

## 4. Tests de GetAsync / GetByXXXAsync

### 4.1. GetAsync - Por ID

**Propósito:** Verificar que GetAsync retorna la entidad correcta por su ID.

**Caso exitoso:**

```csharp
[Test]
public async Task GetAsync_WithExistingId_ShouldReturnModuleUser()
{
    // Arrange - Load scenario and get existing module user ID
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(ErikaMorenoId);
    moduleUserRow.Should().NotBeNull("Precondition: There should be at least one module user granted by Erika Moreno");
    var moduleUserId = moduleUserRow!.Field<Guid>("id");
    var expectedGrantedByUserId = moduleUserRow.Field<Guid?>("granted_by_user_id");

    // Act
    var result = await RepositoryUnderTest.GetAsync(moduleUserId);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(moduleUserId);
    result.GrantedByUserId.Should().Be(expectedGrantedByUserId);
}
```

**Caso no existente:**

```csharp
[Test]
public async Task GetAsync_WithNonExistingId_ShouldReturnNull()
{
    // Arrange
    var nonExistingId = Guid.NewGuid();

    // Act
    var result = await RepositoryUnderTest.GetAsync(nonExistingId);

    // Assert
    result.Should().BeNull();
}
```

### 4.2. GetByXXXAsync - Métodos Custom

**Propósito:** Verificar métodos de búsqueda custom (GetByEmail, GetByCode, etc.)

**Caso exitoso:**

```csharp
[Test]
public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    var result = await this.RepositoryUnderTest.GetByEmailAsync(existingEmail);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be(existingEmail);
}
```

**Caso no existente:**

```csharp
[Test]
public async Task GetByEmailAsync_WhenEmailDoesNotExist_ShouldReturnNull()
{
    // Act
    var result = await this.RepositoryUnderTest.GetByEmailAsync("nonexistent@example.com");

    // Assert
    result.Should().BeNull();
}
```

**Con TestCase parametrizado:**

```csharp
[TestCase("PR-001", "Active")]
[TestCase("PR-002", "Active")]
[TestCase("PR-003", "Expired")]
public async Task GetByNumberAsync_WithExistingNumber_ReturnsPrototype(string number, string status)
{
    // Arrange
    LoadScenario("CreatePrototypes");

    // Act
    var response = await this.RepositoryUnderTest.GetByNumberAsync(number);

    // Assert
    response.Should().NotBeNull();
    response.Number.Should().Be(number);
    response.Status.Should().Be(status);
}
```

### 4.3. GetAsync con Expression

**Propósito:** Verificar que GetAsync con expresión lambda filtra correctamente.

```csharp
[Test]
public async Task GetAsync_WithExpression_ShouldFilterCorrectly()
{
    // Arrange - Load scenario with pre-defined data
    this.LoadScenario("040_ModuleUsers");

    // Act - Filter by status
    var activeResults = await RepositoryUnderTest.GetAsync(mu => mu.Status == UserStatus.Active);
    var inactiveResults = await RepositoryUnderTest.GetAsync(mu => mu.Status == UserStatus.Inactive);

    // Assert - Verify counts based on scenario data
    activeResults.Should().HaveCount(4);
    activeResults.Should().OnlyContain(mu => mu.Status == UserStatus.Active);

    inactiveResults.Should().HaveCount(1);
    inactiveResults.Should().OnlyContain(mu => mu.Status == UserStatus.Inactive);
}
```

### 4.4. Get con Filtros Complejos

**Propósito:** Verificar métodos que retornan colecciones filtradas.

```csharp
[Test]
public async Task GetByGrantedByUserIdAsync_WithExistingGranter_ShouldReturnModuleUsers()
{
    // Arrange - Load scenario with pre-defined data
    this.LoadScenario("040_ModuleUsers");
    // Carlos Almanza granted access to 2 users in the scenario

    // Act
    var results = await RepositoryUnderTest.GetByGrantedByUserIdAsync(CarlosAlmanzaId);

    // Assert
    results.Should().HaveCount(2);
    results.Should().OnlyContain(mu => mu.GrantedByUserId == CarlosAlmanzaId);
}

[Test]
public async Task GetByGrantedByUserIdAsync_WithNonExistingGranter_ShouldReturnEmpty()
{
    // Arrange
    var nonExistingGranterId = Guid.NewGuid();

    // Act
    var results = await RepositoryUnderTest.GetByGrantedByUserIdAsync(nonExistingGranterId);

    // Assert
    results.Should().BeEmpty();
}

[Test]
public async Task GetByStatusAsync_WithActiveStatus_ShouldReturnActiveUsers()
{
    // Arrange - Load scenario with pre-defined data
    this.LoadScenario("040_ModuleUsers");

    // Act
    var results = await RepositoryUnderTest.GetByStatusAsync(UserStatus.Active);

    // Assert - Should return only active users
    results.Should().HaveCount(4);
    results.Should().OnlyContain(mu => mu.Status == UserStatus.Active);
}
```

---

## 5. Tests de UpdateAsync

### 5.1. Actualización Exitosa

**Propósito:** Verificar que UpdateAsync actualiza correctamente los campos.

```csharp
[Test]
public async Task UpdateAsync_WithValidParameters_ShouldUpdateModuleUser()
{
    // Arrange - Load scenario and get existing module user ID
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(CarlosAlmanzaId);
    moduleUserRow.Should().NotBeNull("Precondition: There should be at least one module user granted by Carlos Almanza");
    var moduleUserId = moduleUserRow!.Field<Guid>("id");

    var newStatus = UserStatus.Inactive;

    // Act
    await RepositoryUnderTest.UpdateAsync(moduleUserId, newStatus);

    // Assert - Verify in database using NDbUnit
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var updatedRows = dataSet.GetModuleUsersRows($"id = '{moduleUserId}'");
    updatedRows.Count().Should().Be(1);
    updatedRows.First().Field<short>("status").Should().Be((short)newStatus);
}
```

**Qué verificar:**
- ✅ Campos actualizados tienen los nuevos valores
- ✅ Campos no actualizados mantienen sus valores originales
- ✅ UpdateDate se actualiza (si aplica)
- ✅ Exactamente 1 fila afectada

### 5.2. Entidad No Existe

**Propósito:** Verificar que UpdateAsync lanza excepción cuando la entidad no existe.

```csharp
[Test]
public async Task UpdateAsync_WithNonExistingId_ShouldThrowResourceNotFoundException()
{
    // Arrange
    var nonExistingId = Guid.NewGuid();
    var status = UserStatus.Active;

    // Act
    Func<Task> act = async () => await RepositoryUnderTest.UpdateAsync(
        nonExistingId,
        status);

    // Assert
    await act.Should().ThrowAsync<ResourceNotFoundException>()
        .WithMessage($"Module user with id '{nonExistingId}' does not exist.");
}
```

### 5.3. Duplicados con Otra Entidad

**Propósito:** Verificar que no se puede actualizar con un valor único que ya tiene otra entidad.

```csharp
[Test]
public async Task UpdateAsync_WithDuplicateEmail_ShouldThrowDuplicatedDomainException()
{
    // Arrange - Load scenario with multiple users
    this.LoadScenario("CreateUsers");

    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var users = dataSet.GetUsersRows("");
    users.Should().HaveCountGreaterThan(1, "Precondition: Need at least 2 users");

    var firstUserId = users[0].Field<Guid>("id");
    var secondUserEmail = users[1].Field<string>("email");

    // Act - Try to update first user with second user's email
    Func<Task> act = async () => await RepositoryUnderTest.UpdateAsync(
        firstUserId,
        secondUserEmail,
        users[0].Field<string>("name"));

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>()
        .WithMessage($"A user with the email '{secondUserEmail}' already exists.");
}
```

### 5.4. Mismo Valor (No-Op)

**Propósito:** Verificar que actualizar con el mismo valor no causa error.

```csharp
[Test]
public async Task UpdateAsync_WithSameEmail_ShouldSucceed()
{
    // Arrange - Load scenario and get existing user
    this.LoadScenario("CreateUsers");
    var userRow = dataSet.GetFirstUserRow();
    var userId = userRow.Field<Guid>("id");
    var currentEmail = userRow.Field<string>("email");
    var currentName = userRow.Field<string>("name");

    // Act - Update with same email
    await RepositoryUnderTest.UpdateAsync(userId, currentEmail, currentName);

    // Assert - Verify no error and data unchanged
    var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
    var updatedRow = updatedDataSet.GetUsersRows($"id = '{userId}'").First();
    updatedRow.Field<string>("email").Should().Be(currentEmail);
}
```

---

## 7. Tests de DeleteAsync

### 7.1. Eliminación Exitosa

**Propósito:** Verificar que DeleteAsync elimina correctamente la entidad.

> **⚠️ Nota sobre DeleteAsync:** Si tu repositorio tiene un método `DeleteAsync(Guid id)` que acepta ID, úsalo. Si solo tiene `DeleteAsync(TEntity entity)`, es aceptable usar `GetAsync` en Arrange porque DeleteAsync **requiere** el objeto entidad. En ese caso, documenta claramente esta excepción.

**Opción 1: DeleteAsync por ID (Preferido)**

```csharp
[Test]
public async Task DeleteAsync_ShouldRemoveUser()
{
    // Arrange - Load scenario and get existing user ID
    this.LoadScenario("CreateUsers");
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    userRow.Should().NotBeNull("Precondition: There should be at least one user");
    var userId = userRow!.Field<Guid>("id");

    // Act - Delete by ID
    await RepositoryUnderTest.DeleteAsync(userId);

    // Assert - Verify deletion in database using NDbUnit
    var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
    var deletedRows = updatedDataSet.GetUsersRows($"id = '{userId}'");
    deletedRows.Should().BeEmpty();
}
```

**Opción 2: DeleteAsync requiere entidad (Excepción documentada)**

```csharp
[Test]
public async Task DeleteAsync_ShouldRemoveModuleUser()
{
    // Arrange - Load scenario and get existing module user
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(ErikaMorenoId);
    moduleUserRow.Should().NotBeNull("Precondition: There should be at least one module user granted by Erika Moreno");
    var moduleUserId = moduleUserRow!.Field<Guid>("id");

    // Get entity to delete - EXCEPCIÓN: DeleteAsync requiere objeto entidad
    // Esta es una limitación del API del repositorio, no un anti-pattern
    var entity = await RepositoryUnderTest.GetAsync(moduleUserId);
    entity.Should().NotBeNull();

    // Act
    await RepositoryUnderTest.DeleteAsync(entity!);

    // Assert - Verify deletion in database using NDbUnit
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var deletedRows = dataSet.GetModuleUsersRows($"id = '{moduleUserId}'");
    deletedRows.Should().BeEmpty();
}
```

**Recomendación:** Preferir métodos que acepten ID en lugar de entidad completa para facilitar testing.

### 7.2. Verificación en Base de Datos

**IMPORTANTE:** Siempre verificar que la fila fue eliminada de la base de datos:

```csharp
// ✅ CORRECTO - Verifica en DB
await RepositoryUnderTest.DeleteAsync(entity);

var dataSet = this.nDbUnitTest.GetDataSetFromDb();
var deletedRows = dataSet.GetModuleUsersRows($"id = '{entityId}'");
deletedRows.Should().BeEmpty();

// ❌ INCORRECTO - No verifica en DB
await RepositoryUnderTest.DeleteAsync(entity);
// No assertion - test incompleto
```

---

## 7. Tests de Métodos Custom

### 7.1. Métodos de Relaciones (AddUserToRole)

**Propósito:** Verificar métodos que gestionan relaciones many-to-many.

**Caso exitoso:**

```csharp
[Test]
public async Task AddUserToRoleAsync_WhenUserAndRoleExist_ShouldAddUserToRole()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var roleName = RolesResources.PlatformAdministrator;
    string userEmail = "usuario1@example.com";

    // Act
    await this.RepositoryUnderTest.AddUserToRoleAsync(roleName, userEmail);

    // Assert - Verify relationship in join table
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRole = dataSet.GetFirstUserInRolesRow();
    userRole.Should().NotBeNull();
    userRole.Field<Guid>("user_id").Should().NotBeEmpty();
    userRole.Field<Guid>("role_id").Should().NotBeEmpty();
}
```

**Caso duplicado:**

```csharp
[Test]
public async Task AddUserToRoleAsync_WhenUserHaveRole_DuplicateException()
{
    // Arrange - Scenario already has user with role
    this.LoadScenario("CreateAdminUser");
    var roleName = RolesResources.PlatformAdministrator;
    string userEmail = "usuario1@example.com";

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.AddUserToRoleAsync(roleName, userEmail);

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>();
}
```

**Caso de eliminación:**

```csharp
[Test]
public async Task RemoveUserFromRoleAsync_WhenUserHaveRole_RemoveUserFromRole()
{
    // Arrange
    this.LoadScenario("CreateAdminUser");
    var roleName = RolesResources.PlatformAdministrator;
    string userEmail = "usuario1@example.com";

    // Act
    await this.RepositoryUnderTest.RemoveUserFromRoleAsync(roleName, userEmail);

    // Assert - Verify relationship removed
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRole = dataSet.GetFirstUserInRolesRow();
    userRole.Should().BeNull();
}
```

**Casos de error:**

```csharp
[Test]
public async Task AddUserToRoleAsync_WhenUserDoesNotExist_ArgumentException()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var roleName = RolesResources.PlatformAdministrator;
    string userEmail = "nonexistent@example.com";

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.AddUserToRoleAsync(roleName, userEmail);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
}

[Test]
public async Task AddUserToRoleAsync_WhenRoleDoesNotExist_ArgumentException()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var roleName = "InvalidRole";
    string userEmail = "usuario1@example.com";

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.AddUserToRoleAsync(roleName, userEmail);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>();
}
```

### 7.2. Métodos de Consulta Específica

**Propósito:** Verificar métodos de consulta con lógica de negocio específica.

```csharp
[Test]
public async Task GetActiveByOrganizationIdAsync_WithExistingOrganization_ReturnsActiveModules()
{
    // Arrange
    this.LoadScenario("030_ActivedModules");
    var organizationId = ApsysmxOrgId;

    // Act
    var results = await RepositoryUnderTest.GetActiveByOrganizationIdAsync(organizationId);

    // Assert
    results.Should().NotBeEmpty();
    results.Should().OnlyContain(am => am.Organization.Id == organizationId);
    results.Should().OnlyContain(am => am.Status == ModuleStatus.Active);
}
```

### 7.3. Métodos de Negocio

**Propósito:** Verificar métodos que implementan lógica de negocio compleja.

```csharp
[Test]
public async Task CountAsync_ShouldReturnCorrectNumber()
{
    // Arrange - Load scenario with pre-defined data
    this.LoadScenario("040_ModuleUsers");
    // Scenario has 5 module users total

    // Act
    var count = await RepositoryUnderTest.CountAsync();

    // Assert
    count.Should().Be(5);
}

[Test]
public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var userRow = dataSet.GetFirstUserRow();
    var userId = userRow.Field<Guid>("id");

    // Act
    var exists = await RepositoryUnderTest.ExistsAsync(userId);

    // Assert
    exists.Should().BeTrue();
}

[Test]
public async Task ExistsAsync_WithNonExistingEntity_ReturnsFalse()
{
    // Arrange
    var nonExistingId = Guid.NewGuid();

    // Act
    var exists = await RepositoryUnderTest.ExistsAsync(nonExistingId);

    // Assert
    exists.Should().BeFalse();
}
```

---

## 8. Patrones de Arranque de Datos

### 8.1. LoadScenario() - Datos Predefinidos

**Propósito:** Cargar datos de prueba desde archivos XML usando NDbUnit.

```csharp
[Test]
public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser()
{
    // Arrange - Cargar escenario con usuarios predefinidos
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    var result = await this.RepositoryUnderTest.GetByEmailAsync(existingEmail);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be(existingEmail);
}
```

**Cuándo usar LoadScenario:**
- ✅ Para tests de GetByXXX (necesitas datos existentes)
- ✅ Para tests de Update (necesitas entidad existente)
- ✅ Para tests de Delete (necesitas entidad existente)
- ✅ Para tests de duplicados (necesitas registro previo)
- ✅ Para tests de relaciones (necesitas entidades relacionadas)

### 8.2. GetDataSetFromDb() - Obtener IDs

**Propósito:** Obtener IDs y datos de la base de datos para usar en Arrange.

```csharp
[Test]
public async Task UpdateAsync_WithValidParameters_ShouldUpdateModuleUser()
{
    // Arrange - Load scenario and GET ID from DB
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(CarlosAlmanzaId);
    moduleUserRow.Should().NotBeNull("Precondition: There should be at least one module user");

    // GET ID from database
    var moduleUserId = moduleUserRow!.Field<Guid>("id");
    var newStatus = UserStatus.Inactive;

    // Act
    await RepositoryUnderTest.UpdateAsync(moduleUserId, newStatus);

    // Assert...
}
```

**Métodos helper comunes:**

```csharp
// Get first row
var userRow = dataSet.GetFirstUserRow();

// Get rows with filter
var userRows = dataSet.GetUsersRows($"email = '{email}'");

// Get specific table
var rolesTable = dataSet.GetRolesTable();

// Access fields
var userId = userRow.Field<Guid>("id");
var email = userRow.Field<string>("email");
```

### 8.3. AutoFixture para Valores

**Propósito:** Usar AutoFixture para generar valores de prueba, NO para crear entidades completas.

```csharp
[SetUp]
public void LocalSetUp()
{
    // ✅ CORRECTO - Usar AutoFixture para valores
    _testUser = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")  // Valor específico
        .Without(x => x.Roles)  // Excluir collections
        .Create();
}

[Test]
public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
{
    // Act - Usar valores de _testUser
    await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

    // Assert...
}
```

**Qué NO hacer:**

```csharp
// ❌ INCORRECTO - No usar fixture.Create directamente en Arrange de test
[Test]
public async Task UpdateAsync_Test()
{
    // ❌ NO hacer esto
    var user = fixture.Create<User>();
    await RepositoryUnderTest.UpdateAsync(user.Id, user.Email, user.Name);
}
```

### 8.4. Crear Dependencias con Otros Repositorios

**Propósito:** Cuando una entidad depende de otra, usar repositorio para crear la dependencia.

```csharp
[Test]
public async Task CreateAsync_WithValidParameters_ShouldCreateModuleUser()
{
    // Arrange - Load scenario to get ActivedModule
    this.LoadScenario("030_ActivedModules");

    // Create dependency repository to get required entity
    var activedModuleRepository = new NHActivedModuleRepository(
        _sessionFactory.OpenSession(),
        _serviceProvider);
    var activedModules = await activedModuleRepository.GetActiveByOrganizationIdAsync(ApsysmxOrgId);
    var activeModule = activedModules.First();

    var id = Guid.NewGuid();
    var accessGrantedDate = DateTime.UtcNow;
    var status = UserStatus.Active;

    // Act - Use dependency
    await RepositoryUnderTest.CreateAsync(
        id,
        activeModule,  // ← Dependency obtained from another repository
        accessGrantedDate,
        null,
        status);

    // Assert...
}
```

---

## 9. Verificación de Datos

### 9.1. Verificar Inserción

**Patrón estándar para verificar que se insertó correctamente:**

```csharp
[Test]
public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
{
    // Act
    await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

    // Assert - Verificar en base de datos
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{_testUser.Email}'");

    // 1. Verificar que se insertó exactamente 1 fila
    userRows.Count().Should().Be(1);

    // 2. Verificar valores de campos
    var firstUser = userRows.First();
    firstUser["email"].Should().Be(_testUser.Email);
    firstUser["name"].Should().Be(_testUser.Name);

    // 3. Verificar que ID se generó
    firstUser.Field<Guid>("id").Should().NotBeEmpty();

    // 4. Verificar CreationDate
    firstUser.Field<DateTime>("creation_date").Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
}
```

### 9.2. Verificar Actualización

**Patrón estándar para verificar que se actualizó correctamente:**

```csharp
[Test]
public async Task UpdateAsync_WithValidParameters_ShouldUpdateModuleUser()
{
    // Arrange
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(CarlosAlmanzaId);
    var moduleUserId = moduleUserRow!.Field<Guid>("id");
    var newStatus = UserStatus.Inactive;

    // Act
    await RepositoryUnderTest.UpdateAsync(moduleUserId, newStatus);

    // Assert - Verificar actualización en base de datos
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var updatedRows = dataSet.GetModuleUsersRows($"id = '{moduleUserId}'");

    // 1. Verificar que sigue existiendo 1 fila
    updatedRows.Count().Should().Be(1);

    // 2. Verificar que el campo se actualizó
    updatedRows.First().Field<short>("status").Should().Be((short)newStatus);
}
```

### 9.3. Verificar Eliminación

**Patrón estándar para verificar que se eliminó correctamente:**

```csharp
[Test]
public async Task DeleteAsync_ShouldRemoveModuleUser()
{
    // Arrange
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(ErikaMorenoId);
    var moduleUserId = moduleUserRow!.Field<Guid>("id");
    var entity = await RepositoryUnderTest.GetAsync(moduleUserId);

    // Act
    await RepositoryUnderTest.DeleteAsync(entity!);

    // Assert - Verificar eliminación en base de datos
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var deletedRows = dataSet.GetModuleUsersRows($"id = '{moduleUserId}'");

    // Verificar que la fila no existe
    deletedRows.Should().BeEmpty();
}
```

### 9.4. Verificar Campos Específicos

**Verificar múltiples campos con assertions individuales:**

```csharp
[Test]
public async Task CreateAsync_WithValidData_CreatesPrototype()
{
    // Act
    await this.RepositoryUnderTest.CreateAsync(
        _testPrototype!.Number,
        _testPrototype.IssueDate,
        _testPrototype.ExpirationDate,
        _testPrototype.Status);

    // Assert
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var prototypeRows = dataSet.GetPrototypesRows($"number = '{_testPrototype.Number}'");
    prototypeRows.Count().Should().Be(1);

    var prototypeRow = prototypeRows.First();

    // Verificar cada campo individualmente
    prototypeRow.Field<string>("number").Should().Be(_testPrototype.Number);
    prototypeRow.Field<DateTime>("issue_date").Date.Should().Be(_testPrototype.IssueDate.Date);
    prototypeRow.Field<DateTime>("expiration_date").Date.Should().Be(_testPrototype.ExpirationDate.Date);
    prototypeRow.Field<string>("status").Should().Be(_testPrototype.Status);
}
```

**Verificar campos con tipos específicos:**

```csharp
// Guid
row.Field<Guid>("id").Should().Be(expectedId);
row.Field<Guid>("id").Should().NotBeEmpty();

// Guid nullable
row.Field<Guid?>("granted_by_user_id").Should().Be(expectedId);
row.Field<Guid?>("granted_by_user_id").Should().BeNull();

// DateTime
row.Field<DateTime>("creation_date").Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
row.Field<DateTime>("issue_date").Date.Should().Be(expectedDate.Date);

// Enum (stored as short/int)
row.Field<short>("status").Should().Be((short)UserStatus.Active);

// String
row.Field<string>("email").Should().Be(expectedEmail);
row["email"].ToString().Should().Be(expectedEmail);

// Boolean
row.Field<bool>("locked").Should().BeTrue();
```

### 9.5. Verificar Relaciones

**Verificar tablas de join para relaciones many-to-many:**

```csharp
[Test]
public async Task AddUserToRoleAsync_WhenUserAndRoleExist_ShouldAddUserToRole()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var roleName = RolesResources.PlatformAdministrator;
    string userEmail = "usuario1@example.com";

    // Act
    await this.RepositoryUnderTest.AddUserToRoleAsync(roleName, userEmail);

    // Assert - Verificar en tabla de join
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRole = dataSet.GetFirstUserInRolesRow();

    userRole.Should().NotBeNull();
    userRole.Field<Guid>("user_id").Should().NotBeEmpty();
    userRole.Field<Guid>("role_id").Should().NotBeEmpty();
}
```

---

## 10. Assertions y Mensajes

### 10.1. FluentAssertions Best Practices

**Para excepciones:**

```csharp
// ✅ CORRECTO - Con ThrowAsync
Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(null!, name);
await act.Should().ThrowAsync<InvalidDomainException>();

// ✅ CORRECTO - Con mensaje específico
await act.Should().ThrowAsync<DuplicatedDomainException>()
    .WithMessage($"A user with the email '{email}' already exists.");

// ❌ INCORRECTO
try
{
    await RepositoryUnderTest.CreateAsync(null!, name);
    Assert.Fail("Should have thrown exception");
}
catch (InvalidDomainException) { }
```

**Para colecciones:**

```csharp
// ✅ CORRECTO - Verificar count
results.Should().HaveCount(4);
results.Should().BeEmpty();
results.Should().NotBeEmpty();

// ✅ CORRECTO - Verificar contenido
results.Should().OnlyContain(mu => mu.Status == UserStatus.Active);
results.Should().Contain(u => u.Email == "test@example.com");

// ❌ INCORRECTO
Assert.AreEqual(4, results.Count());
```

**Para nullability:**

```csharp
// ✅ CORRECTO
result.Should().NotBeNull();
result.Should().BeNull();

// ❌ INCORRECTO
Assert.IsNotNull(result);
Assert.IsNull(result);
```

**Para valores:**

```csharp
// ✅ CORRECTO
result.Email.Should().Be(expectedEmail);
result.Id.Should().NotBeEmpty();
result.CreationDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));

// ❌ INCORRECTO
Assert.AreEqual(expectedEmail, result.Email);
Assert.AreNotEqual(Guid.Empty, result.Id);
```

### 10.2. Mensajes Descriptivos

**Siempre incluir mensaje "because" en preconditions:**

```csharp
// ✅ CORRECTO - Con mensaje descriptivo en precondition
var userRow = dataSet.GetFirstUserRow();
userRow.Should().NotBeNull("Precondition: There should be at least one user in the dataset");

var userId = userRow!.Field<Guid>("id");

// ❌ INCORRECTO - Sin mensaje
var userRow = dataSet.GetFirstUserRow();
userRow.Should().NotBeNull();
```

**Mensajes en assertions principales son opcionales pero recomendados:**

```csharp
// ✅ CORRECTO - Con mensaje (recomendado)
result.Should().NotBeNull("GetByEmailAsync should return user when email exists");
result!.Email.Should().Be(expectedEmail, "Email should match the queried email");

// ✅ ACEPTABLE - Sin mensaje (cuando es obvio)
result.Should().NotBeNull();
result!.Email.Should().Be(expectedEmail);
```

### 10.3. Assertions en Arrange (Preconditions)

**Verificar precondiciones en Arrange:**

```csharp
[Test]
public async Task UpdateAsync_WithValidParameters_ShouldUpdateModuleUser()
{
    // Arrange - Load scenario and get existing module user ID
    this.LoadScenario("040_ModuleUsers");
    var moduleUserRow = GetFirstModuleUserRowByGranter(CarlosAlmanzaId);

    // ✅ Assertion de precondición
    moduleUserRow.Should().NotBeNull(
        "Precondition: There should be at least one module user granted by Carlos Almanza");

    var moduleUserId = moduleUserRow!.Field<Guid>("id");
    var newStatus = UserStatus.Inactive;

    // Act
    await RepositoryUnderTest.UpdateAsync(moduleUserId, newStatus);

    // Assert
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var updatedRows = dataSet.GetModuleUsersRows($"id = '{moduleUserId}'");
    updatedRows.Count().Should().Be(1);
    updatedRows.First().Field<short>("status").Should().Be((short)newStatus);
}
```

**Por qué es importante:**
- Clarifica qué esperamos del escenario cargado
- Falla rápido si el escenario no tiene los datos esperados
- Documenta las precondiciones del test

---

## 11. Edge Cases y Boundary Testing

### 11.1. IDs Vacíos o Nulos

**Verificar que IDs vacíos son rechazados:**

```csharp
[Test]
public async Task CreateAsync_WithEmptyId_ShouldThrowInvalidDomainException()
{
    // Arrange
    var id = Guid.Empty; // ❌ Invalid
    // ... other parameters

    // Act
    Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(id, ...);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}

[Test]
public async Task GetAsync_WithEmptyGuid_ShouldReturnNull()
{
    // Arrange
    var emptyId = Guid.Empty;

    // Act
    var result = await RepositoryUnderTest.GetAsync(emptyId);

    // Assert
    result.Should().BeNull();
}
```

### 11.2. Fechas Default

**Verificar que fechas default son rechazadas:**

```csharp
[Test]
public async Task CreateAsync_WithDefaultAccessGrantedDate_ShouldThrowInvalidDomainException()
{
    // Arrange
    var id = Guid.NewGuid();
    var accessGrantedDate = default(DateTime); // ❌ Invalid (01/01/0001)
    // ... other parameters

    // Act
    Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(
        id,
        activeModule,
        accessGrantedDate,
        grantedByUserId,
        status);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}
```

### 11.3. Collections Vacías vs Null

**Verificar comportamiento con collections vacías:**

```csharp
[Test]
public async Task GetByGrantedByUserIdAsync_WithNonExistingGranter_ShouldReturnEmpty()
{
    // Arrange
    var nonExistingGranterId = Guid.NewGuid();

    // Act
    var results = await RepositoryUnderTest.GetByGrantedByUserIdAsync(nonExistingGranterId);

    // Assert - Retorna collection vacía, NO null
    results.Should().BeEmpty();
    results.Should().NotBeNull();
}
```

### 11.4. Case Sensitivity

**Verificar case-insensitivity en búsquedas:**

```csharp
[TestCase("PR-001")]
[TestCase("pr-001")]
[TestCase("Pr-001")]
[TestCase("pR-001")]
public async Task GetByNumberAsync_WithDifferentCase_ReturnsPrototype(string number)
{
    // Arrange
    LoadScenario("CreatePrototypes");

    // Act
    var result = await this.RepositoryUnderTest.GetByNumberAsync(number);

    // Assert - Should find regardless of case
    result.Should().NotBeNull();
    result.Number.Should().BeEquivalentTo("PR-001"); // Case-insensitive comparison
}
```

---

## 12. Anti-Patterns a Evitar

### 12.1. Usar Repositorio en Arrange y Assert

**ANTI-PATTERN CRÍTICO:** Usar el repositorio bajo prueba para preparar datos o verificar resultados.

```csharp
// ❌ INCORRECTO - Usa el repositorio en Arrange y Assert
[Test]
public async Task UpdateAsync_ShouldUpdateUser()
{
    // Arrange - USA EL REPOSITORIO (mal)
    var user = await this.RepositoryUnderTest.CreateAsync("test@example.com", "Test User");

    // Act
    await this.RepositoryUnderTest.UpdateAsync(user.Id, "new@example.com", "New Name");

    // Assert - USA EL REPOSITORIO (mal)
    var result = await this.RepositoryUnderTest.GetByIdAsync(user.Id);
    result.Email.Should().Be("new@example.com");
}

// ✅ CORRECTO - Usa LoadScenario para Arrange y NDbUnit para Assert
[Test]
public async Task UpdateAsync_ShouldUpdateUser()
{
    // Arrange - ESCENARIO PREDEFINIDO
    this.LoadScenario("CreateUsers");
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    var userId = userRow.Field<Guid>("id");

    // Act - SOLO AQUÍ USA EL REPOSITORIO
    await this.RepositoryUnderTest.UpdateAsync(userId, "new@example.com", "New Name");

    // Assert - VERIFICA CON NDBUNIT
    var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
    var updatedRow = updatedDataSet.GetUsersRows($"id = '{userId}'").First();
    updatedRow.Field<string>("email").Should().Be("new@example.com");
    updatedRow.Field<string>("name").Should().Be("New Name");
}
```

**Por qué es crítico:**
- Si `CreateAsync` tiene un bug, los tests de `UpdateAsync` fallan por la razón incorrecta
- Si `GetByIdAsync` tiene un bug, el Assert pasa aunque `UpdateAsync` no funcione
- Tests se vuelven interdependientes y frágiles
- Pérdida total de confianza en la suite de tests

**Tabla de consecuencias:**

| Scenario | Consecuencia |
|----------|--------------|
| Bug en CreateAsync usado en Arrange | Tests de Update/Delete fallan aunque esos métodos funcionen |
| Bug en GetAsync usado en Assert | Tests de Create/Update pasan aunque fallen |
| Bug en método bajo prueba Y en método auxiliar | No sabes cuál tiene el bug |
| Refactor de CreateAsync | Rompe 20 tests que no prueban CreateAsync |

**Solución: Usar escenarios XML**

Los escenarios XML evitan este problema completamente:
- ✅ Datos insertados directamente en DB (sin usar repositorio)
- ✅ Verificación directa en DB con NDbUnit (sin usar repositorio)
- ✅ Tests completamente aislados
- ✅ Un bug afecta solo a SUS propios tests

### 12.2. Tests que Dependen de Orden

**ANTI-PATTERN:** Tests que dependen de que otros tests se ejecuten primero.

```csharp
// ❌ INCORRECTO - Test2 depende de Test1
[Test, Order(1)]
public async Task Test1_CreateUser()
{
    await RepositoryUnderTest.CreateAsync("test@example.com", "Test");
}

[Test, Order(2)]
public async Task Test2_GetUser()
{
    // Asume que Test1 ya se ejecutó
    var result = await RepositoryUnderTest.GetByEmailAsync("test@example.com");
    result.Should().NotBeNull();
}

// ✅ CORRECTO - Cada test es independiente
[Test]
public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser()
{
    // Arrange - Carga sus propios datos
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    var result = await RepositoryUnderTest.GetByEmailAsync(existingEmail);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be(existingEmail);
}
```

### 12.3. No Verificar en Base de Datos

**ANTI-PATTERN:** Solo verificar el objeto retornado, no la persistencia.

```csharp
// ❌ INCORRECTO - No verifica que se persistió
[Test]
public async Task CreateAsync_ShouldCreateUser()
{
    var result = await RepositoryUnderTest.CreateAsync(email, name);
    result.Should().NotBeNull(); // Solo verifica retorno
}

// ✅ CORRECTO - Verifica en base de datos
[Test]
public async Task CreateAsync_ShouldCreateUser()
{
    await RepositoryUnderTest.CreateAsync(email, name);

    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{email}'");
    userRows.Count().Should().Be(1);
}
```

### 12.4. Hardcodear IDs

**ANTI-PATTERN:** Hardcodear GUIDs en lugar de obtenerlos de la base de datos.

```csharp
// ❌ INCORRECTO - GUID hardcodeado
[Test]
public async Task GetAsync_ShouldReturnUser()
{
    this.LoadScenario("CreateUsers");
    var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"); // ❌ Hardcoded

    var result = await RepositoryUnderTest.GetAsync(userId);
    result.Should().NotBeNull();
}

// ✅ CORRECTO - Obtiene ID de la base de datos
[Test]
public async Task GetAsync_ShouldReturnUser()
{
    this.LoadScenario("CreateUsers");

    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    var userId = userRow.Field<Guid>("id"); // ✅ Obtenido dinámicamente

    var result = await RepositoryUnderTest.GetAsync(userId);
    result.Should().NotBeNull();
}
```

---

## 13. Checklist de Testing

### Antes de Crear Tests

- [ ] Escenarios XML creados en carpeta `scenarios/`
- [ ] Validators registrados en `LoadValidators()` de `NHRepositoryTestInfrastructureBase`
- [ ] Métodos helper en AppSchema (GetXXXRows, GetFirstXXXRow, GetXXXTable)

### Estructura de la Clase

- [ ] Clase hereda de `NHRepositoryTestBase<TRepo, T, TKey>` o `NHReadOnlyRepositoryTestBase<TRepo, T, TKey>`
- [ ] Método `BuildRepository()` implementado
- [ ] LocalSetUp con AutoFixture para datos de prueba (si aplica)
- [ ] Tests organizados en regiones por método
- [ ] Helper methods en región `#region Helper Methods`

### Tests Mínimos por Método CreateAsync

- [ ] Happy path - Creación exitosa con datos válidos
- [ ] Validación de campos required (null, empty, whitespace)
- [ ] Validación de duplicados
- [ ] Validación de formato (email, phone, etc.)
- [ ] Validación de default values (Guid.Empty, default(DateTime))

### Tests Mínimos por Método GetAsync / GetByXXXAsync

- [ ] Retorna entidad cuando existe
- [ ] Retorna null cuando no existe
- [ ] Case-insensitive (si aplica)
- [ ] Filtros complejos funcionan correctamente

### Tests Mínimos por Método UpdateAsync

- [ ] Actualización exitosa con datos válidos
- [ ] Entidad no existe → ResourceNotFoundException
- [ ] Duplicados con otra entidad → DuplicatedDomainException
- [ ] Mismo valor actual (no-op) → Sin error

### Tests Mínimos por Método DeleteAsync

- [ ] Eliminación exitosa
- [ ] Verificación en base de datos (fila eliminada)

### Tests de Métodos Custom

- [ ] Happy path con datos válidos
- [ ] Casos de error (entidad no existe, validaciones)
- [ ] Edge cases específicos del método

### Verificación y Calidad

- [ ] Todos los tests usan patrón AAA (Arrange-Act-Assert)
- [ ] Nombres siguen convención `{Method}_{Condition}_{ExpectedResult}`
- [ ] LoadScenario usado en Arrange (NO usar repositorio)
- [ ] NDbUnit usado en Assert (NO usar repositorio)
- [ ] Preconditions tienen assertions con mensajes descriptivos
- [ ] Todos los tests son independientes (no dependen de orden)
- [ ] Sin GUIDs hardcodeados
- [ ] Sin warnings de NUnit

---

## 14. Referencias y Ejemplos

### Proyectos de Referencia

**hashira-stone-backend:**
- `tests/hashira.stone.backend.infrastructure.tests/nhibernate/`
  - NHUserRepositoryTests.cs
  - NHRoleRepositoryTests.cs
  - NHPrototypeRepositoryTests.cs
  - NHTechnicalStandardRepositoryTests.cs
  - NHTechnicalStandardDaoRepositoryTests.cs

**hollow-soulmaster-backend:**
- `tests/hollow.soulmaster.backend.infrastructure.tests/nhibernate/`
  - NHModuleUserRepositoryTests.cs
  - NHModuleRoleRepositoryTests.cs
  - NHActivedModuleRepositoryTests.cs
  - NHOrganizationDaoRepositoryTests.cs

### Guías Relacionadas

- [Integration Tests](./integration-tests.md) - Infraestructura y configuración de tests
- [Repositories](./repositories.md) - Implementación de repositorios
- [Entity Testing Practices](../../../domain-layer/entities-testing-practices.md) - Testing de entidades
- [Testing Conventions](../../../best-practices/testing-conventions.md) - Convenciones generales

### Documentación Externa

- [FluentAssertions](https://fluentassertions.com/) - Librería de assertions
- [AutoFixture](https://github.com/AutoFixture/AutoFixture) - Generación de datos de prueba
- [NDbUnit](https://github.com/NDbUnit/NDbUnit) - Herramienta de testing con datos
- [NUnit](https://nunit.org/) - Framework de testing

---

## Resumen de Convenciones

### Naming

- Test class: `NH{EntityName}RepositoryTests.cs`
- Test method: `{Method}_{Condition}_{ExpectedResult}`
- Helper method: Descriptive name with purpose

### Estructura

- Heredar de `NHRepositoryTestBase` o `NHReadOnlyRepositoryTestBase`
- Implementar `BuildRepository()`
- LocalSetUp con AutoFixture (opcional)
- Organizar con `#region` por método del repositorio

### Arranque de Datos

- **Arrange:** `LoadScenario()` + `GetDataSetFromDb()` para obtener IDs
- **Act:** `RepositoryUnderTest.MethodUnderTest()`
- **Assert:** `nDbUnitTest.GetDataSetFromDb()` para verificar persistencia

### Assertions

- Usar FluentAssertions: `.Should().Be()`, `.Should().NotBeNull()`, etc.
- Preconditions con mensajes: `.Should().NotBeNull("Precondition: ...")`
- Siempre verificar en base de datos, NO usar repositorio en Assert

### AAA Pattern

```csharp
// Arrange - LoadScenario + GetDataSetFromDb
this.LoadScenario("CreateUsers");
var userId = GetFirstUserIdFromDb();

// Act - RepositoryUnderTest solamente
await RepositoryUnderTest.UpdateAsync(userId, newValue);

// Assert - GetDataSetFromDb para verificar
var dataSet = this.nDbUnitTest.GetDataSetFromDb();
var row = dataSet.GetUsersRows($"id = '{userId}'").First();
row.Field<string>("field").Should().Be(newValue);
```

### Regla de Oro

**NUNCA usar RepositoryUnderTest en Arrange o Assert**
- ✅ Arrange: `LoadScenario()` + `GetDataSetFromDb()`
- ✅ Act: `RepositoryUnderTest.Method()`
- ✅ Assert: `GetDataSetFromDb()` para verificar

---

**Última actualización:** 2025-01-20
**Mantenedor:** Equipo APSYS
**Versión:** 1.0.0
