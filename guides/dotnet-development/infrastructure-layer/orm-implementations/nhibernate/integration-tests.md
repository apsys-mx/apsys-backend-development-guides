# NHibernate Integration Tests
**Versión**: 1.0.0
**Última actualización**: 2025-01-18

## Tabla de Contenidos
1. [Introducción](#introducción)
2. [Arquitectura de Tests](#arquitectura-de-tests)
3. [Clases Base](#clases-base)
4. [Configuración del Entorno](#configuración-del-entorno)
5. [Crear Tests de Repositorio](#crear-tests-de-repositorio)
6. [Patrones de Testing](#patrones-de-testing)
7. [Verificación de Datos](#verificación-de-datos)
8. [Ejemplos Completos](#ejemplos-completos)
9. [Mejores Prácticas](#mejores-prácticas)
10. [Antipatrones](#antipatrones)
11. [Checklist](#checklist)

---

## Introducción

Esta guía documenta cómo crear **pruebas de integración** para repositorios NHibernate. Estas pruebas verifican que los repositorios interactúan correctamente con la base de datos real, validando:

- Operaciones CRUD (Create, Read, Update, Delete)
- Validaciones de dominio
- Manejo de duplicados
- Queries específicos (GetByEmail, GetByCode, etc.)

### Qué NO probamos

- La sesión de NHibernate directamente
- El ORM en sí mismo
- Comportamiento interno de NHibernate

### Qué SÍ probamos

- Los **repositorios** y su lógica de negocio
- Validaciones de entidades
- Manejo de excepciones de dominio
- Persistencia correcta en base de datos

---

## Arquitectura de Tests

### Jerarquía de Clases

```
┌─────────────────────────────────────────┐
│  NHRepositoryTestInfrastructureBase     │ ← Infraestructura compartida
│  • ISessionFactory                      │
│  • IConfiguration                       │
│  • INDbUnit                             │
│  • IFixture (AutoFixture)               │
│  • ServiceProvider (Validators)         │
└─────────────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────────────┐
│  NHRepositoryTestBase<TRepo, T, TKey>   │ ← Para repositorios CRUD
│  • RepositoryUnderTest                  │
│  • BuildRepository() abstract           │
│  • Setup() - Clear database             │
└─────────────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────────────┐
│  NHUserRepositoryTests                  │ ← Tests específicos
│  • BuildRepository() override           │
│  • Test methods                         │
└─────────────────────────────────────────┘
```

### Para Repositorios Read-Only

```
┌─────────────────────────────────────────┐
│  NHRepositoryTestInfrastructureBase     │
└─────────────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────────────────┐
│  NHReadOnlyRepositoryTestBase<TRepo, T, TKey> │ ← Para repositorios solo lectura
│  • RepositoryUnderTest                      │
│  • BuildRepository() abstract               │
└─────────────────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────────────┐
│  NHTechnicalStandardDaoRepositoryTests  │
└─────────────────────────────────────────┘
```

---

## Clases Base

### NHRepositoryTestInfrastructureBase

Clase base que configura toda la infraestructura necesaria para las pruebas.

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using DotNetEnv;
using hashira.stone.backend.ndbunit;
using hashira.stone.backend.common.tests;
using hashira.stone.backend.domain.entities;
using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Base infrastructure for NHibernate repository tests.
/// Contains shared setup, teardown, and utility methods.
/// </summary>
public abstract class NHRepositoryTestInfrastructureBase
{
    protected internal ISessionFactory _sessionFactory;
    protected internal IConfiguration configuration;
    protected internal INDbUnit nDbUnitTest;
    protected internal IFixture fixture;
    protected internal ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // 1. Cargar variables de entorno
        Env.Load();
        string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        // 2. Cargar configuración
        this.configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", true)
            .Build();

        // 3. Crear SessionFactory
        string connectionStringValue = ConnectionStringBuilder.BuildPostgresConnectionString();
        var nHSessionFactory = new NHSessionFactory(connectionStringValue);
        this._sessionFactory = nHSessionFactory.BuildNHibernateSessionFactory();

        // 4. Configurar AutoFixture
        this.fixture = new Fixture().Customize(new AutoMoqCustomization());
        this.fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => this.fixture.Behaviors.Remove(b));
        this.fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // 5. Configurar NDbUnit
        var schema = new AppSchema();
        this.nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionStringValue);

        // 6. Configurar ServiceProvider con Validators
        var services = new ServiceCollection();
        LoadValidators(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void LoadValidators(ServiceCollection services)
    {
        services.AddTransient<AbstractValidator<Role>, RoleValidator>();
        services.AddTransient<AbstractValidator<User>, UserValidator>();
        services.AddTransient<AbstractValidator<Prototype>, PrototypeValidator>();
        services.AddTransient<AbstractValidator<TechnicalStandard>, TechnicalStandardValidator>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        this._sessionFactory.Dispose();
        this._serviceProvider.Dispose();
    }

    /// <summary>
    /// Load a test scenario from XML file using NDbUnit.
    /// </summary>
    protected internal void LoadScenario(string scenarioName)
    {
        var scenariosFolderPath = Environment.GetEnvironmentVariable("SCENARIOS_FOLDER_PATH");
        if (string.IsNullOrEmpty(scenariosFolderPath))
            throw new ConfigurationErrorsException("No [SCENARIOS_FOLDER_PATH] value found in the .env file");

        var xmlFilePath = Path.Combine(scenariosFolderPath, $"{scenarioName}.xml");
        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException($"No scenario file found in [{xmlFilePath}]");

        this.nDbUnitTest.ClearDatabase();
        var schema = new AppSchema();
        schema.ReadXml(xmlFilePath);
        this.nDbUnitTest.SeedDatabase(schema);
    }
}
```

#### Componentes Clave

| Componente | Propósito |
|------------|-----------|
| `ISessionFactory` | Factory de NHibernate para crear sesiones |
| `IConfiguration` | Configuración desde appsettings.json |
| `INDbUnit` | Herramienta para cargar/limpiar datos de prueba |
| `IFixture` | AutoFixture para generar datos de prueba |
| `ServiceProvider` | Contenedor de DI para validators |

---

### NHRepositoryTestBase<TRepo, T, TKey>

Clase base genérica para tests de repositorios con operaciones CRUD.

```csharp
namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Base class for NHibernate repository tests.
/// </summary>
public abstract class NHRepositoryTestBase<TRepo, T, TKey> : NHRepositoryTestInfrastructureBase
    where T : class, new()
    where TRepo : NHRepository<T, TKey>
{
    /// <summary>
    /// Repository instance under test.
    /// </summary>
    public TRepo RepositoryUnderTest { get; protected set; }

    /// <summary>
    /// Setup method for each test.
    /// Initializes repository and clears database.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        this.RepositoryUnderTest = this.BuildRepository();
        this.nDbUnitTest.ClearDatabase();
    }

    /// <summary>
    /// Builds the repository instance for testing.
    /// Must be implemented in derived classes.
    /// </summary>
    abstract protected internal TRepo BuildRepository();
}
```

---

### NHReadOnlyRepositoryTestBase<TRepo, T, TKey>

Clase base para tests de repositorios de solo lectura.

```csharp
namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Base class for NHibernate read-only repository tests.
/// </summary>
public abstract class NHReadOnlyRepositoryTestBase<TRepo, T, TKey> : NHRepositoryTestInfrastructureBase
    where T : class, new()
    where TRepo : NHReadOnlyRepository<T, TKey>
{
    /// <summary>
    /// Repository instance under test.
    /// </summary>
    public TRepo RepositoryUnderTest { get; protected set; }

    /// <summary>
    /// Setup method for each test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        this.RepositoryUnderTest = this.BuildRepository();
        this.nDbUnitTest.ClearDatabase();
    }

    /// <summary>
    /// Builds the repository instance for testing.
    /// </summary>
    abstract protected internal TRepo BuildRepository();
}
```

---

## Configuración del Entorno

### Variables de Entorno (.env)

```env
# Ambiente
DOTNET_ENVIRONMENT=Testing

# Connection String
POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=hashira_test;Username=postgres;Password=secret

# Ruta a escenarios de prueba
SCENARIOS_FOLDER_PATH=D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\tests\scenarios
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=hashira_test"
  }
}
```

### Estructura de Escenarios XML

Los escenarios XML definen datos de prueba para NDbUnit:

```
tests/
├── scenarios/
│   ├── CreateUsers.xml
│   ├── CreateRoles.xml
│   ├── CreateAdminUser.xml
│   ├── CreatePrototypes.xml
│   ├── CreateTechnicalStandards.xml
│   └── CreateSandBox.xml
```

#### Ejemplo de Escenario XML

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <name>Usuario Uno</name>
  </users>
  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>usuario2@example.com</email>
    <name>Usuario Dos</name>
  </users>
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
  </roles>
</AppSchema>
```

---

## Crear Tests de Repositorio

### Paso 1: Crear Clase de Test

```csharp
using FluentAssertions;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.infrastructure.nhibernate;

namespace hashira.stone.backend.infrastructure.tests.nhibernate;

public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    // Test data
    private User? _testUser;

    // 1. Implementar BuildRepository()
    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    // 2. Setup local para datos de prueba
    [SetUp]
    public void LocalSetUp()
    {
        _testUser = fixture.Build<User>()
            .With(x => x.Email, "test@example.com")
            .Without(x => x.Roles)
            .Create();
    }

    // 3. Implementar tests...
}
```

### Paso 2: Implementar BuildRepository()

El método `BuildRepository()` crea una nueva instancia del repositorio con:
- Una sesión nueva de NHibernate
- El ServiceProvider con los validators

```csharp
protected internal override NHUserRepository BuildRepository()
    => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);
```

### Paso 3: Setup Local (Opcional)

Usar AutoFixture para crear datos de prueba:

```csharp
[SetUp]
public void LocalSetUp()
{
    _testUser = fixture.Build<User>()
        .With(x => x.Email, "test@example.com")
        .Without(x => x.Roles)  // Excluir relaciones
        .Create();
}
```

---

## Patrones de Testing

### Patrón AAA (Arrange-Act-Assert)

Todos los tests siguen el patrón AAA:

```csharp
[Test]
public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
{
    // Arrange - Preparar datos y condiciones
    // (En este caso, _testUser ya está creado en LocalSetUp)

    // Act - Ejecutar la acción a probar
    await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

    // Assert - Verificar resultados
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{_testUser.Email}'");
    userRows.Count().Should().Be(1);
}
```

### LoadScenario() - Cargar Datos de Prueba

Usar `LoadScenario()` para cargar datos predefinidos antes de un test:

```csharp
[Test]
public async Task GetByEmailAsync_WhenEmailExists_ShouldReturnUser()
{
    // Arrange - Cargar escenario con usuarios existentes
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    var result = await this.RepositoryUnderTest.GetByEmailAsync(existingEmail);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be(existingEmail);
}
```

### Tests de Excepciones

Usar FluentAssertions para verificar excepciones:

```csharp
[Test]
public async Task CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException()
{
    // Arrange
    this.LoadScenario("CreateUsers");
    var existingEmail = GetFirstUserEmailFromDb();

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest
        .CreateAsync(existingEmail!, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>()
        .WithMessage($"A user with the email '{existingEmail}' already exists.");
}
```

### TestCase - Múltiples Escenarios

Usar `[TestCase]` para probar múltiples inputs:

```csharp
[Test]
[TestCase(null)]
[TestCase("")]
[TestCase("   ")]
public async Task CreateAsync_WhenEmailIsNullOrEmpty_ShouldThrowInvalidDomainException(string? email)
{
    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest
        .CreateAsync(email!, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}

[TestCase("invalid-email-format")]
[TestCase("user@.com")]
[TestCase("user@com")]
public async Task CreateAsync_WhenEmailIsWrongFormat_ShouldThrowInvalidDomainException(string wrongEmail)
{
    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest
        .CreateAsync(wrongEmail, _testUser!.Name);

    // Assert
    await act.Should().ThrowAsync<InvalidDomainException>();
}
```

---

## Verificación de Datos

### GetDataSetFromDb() - Obtener Datos de BD

Usar NDbUnit para verificar que los datos se persistieron correctamente:

```csharp
[Test]
public async Task CreateAsync_WhenRoleNameIsValid_ShouldCreateRole()
{
    // Arrange
    var role = this.fixture.Create<Role>();

    // Act
    await this.RepositoryUnderTest.CreateAsync(role.Name);

    // Assert - Verificar en base de datos
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var roles = dataSet.GetRolesTable();
    roles.Should().NotBeNull();
    roles.Rows.Count.Should().Be(1);
}
```

### Métodos Helper de AppSchema

El `AppSchema` (DataSet tipado) proporciona métodos helper para acceder a datos:

```csharp
// Obtener filas filtradas
var userRows = dataSet.GetUsersRows($"email = '{email}'");

// Obtener primera fila
var userRow = dataSet.GetFirstUserRow();

// Obtener tabla completa
var rolesTable = dataSet.GetRolesTable();

// Acceder a campos específicos
var userId = userRow.Field<Guid>("id");
var email = userRow.Field<string>("email");
```

### Verificar Campos Específicos

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
    prototypeRow.Field<string>("number").Should().Be(_testPrototype.Number);
    prototypeRow.Field<DateTime>("issue_date").Date.Should().Be(_testPrototype.IssueDate.Date);
    prototypeRow.Field<DateTime>("expiration_date").Date.Should().Be(_testPrototype.ExpirationDate.Date);
    prototypeRow.Field<string>("status").Should().Be(_testPrototype.Status);
}
```

### Verificar Relaciones Many-to-Many

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

    // Assert
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRole = dataSet.GetFirstUserInRolesRow();
    userRole.Should().NotBeNull();
    userRole.Field<Guid>("user_id").Should().NotBeEmpty();
    userRole.Field<Guid>("role_id").Should().NotBeEmpty();
}
```

---

## Ejemplos Completos

### Ejemplo 1: Tests de UserRepository

```csharp
using AutoFixture;
using FluentAssertions;
using hashira.stone.backend.common.tests;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.infrastructure.nhibernate;

namespace hashira.stone.backend.infrastructure.tests.nhibernate;

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
    public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
    {
        // Act
        await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

        // Assert
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var userRows = dataSet.GetUsersRows($"email = '{_testUser.Email}'");
        userRows.Count().Should().Be(1);
        var firstUser = userRows.First();
        firstUser["email"].Should().Be(_testUser.Email);
    }

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
    public async Task CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException()
    {
        // Arrange
        this.LoadScenario("CreateUsers");
        var existingEmail = GetFirstUserEmailFromDb();

        // Act
        Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(existingEmail!, _testUser!.Name);

        // Assert
        await act.Should().ThrowAsync<DuplicatedDomainException>()
           .WithMessage($"A user with the email '{existingEmail}' already exists.");
    }

    #endregion

    #region GetByEmailAsync Tests

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

    [Test]
    public async Task GetByEmailAsync_WhenEmailDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await this.RepositoryUnderTest.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper methods

    private string GetFirstUserEmailFromDb()
    {
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var userRow = dataSet.GetFirstUserRow();
        userRow.Should().NotBeNull("Precondition: There should be at least one user in the dataset");
        return userRow!["email"].ToString()!;
    }

    #endregion
}
```

### Ejemplo 2: Tests de Read-Only Repository

```csharp
using FluentAssertions;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.infrastructure.nhibernate;

namespace hashira.stone.backend.infrastructure.tests.nhibernate;

/// <summary>
/// Tests for NHTechnicalStandardDaoRepository (read-only)
/// </summary>
public class NHTechnicalStandardDaoRepositoryTests
    : NHReadOnlyRepositoryTestBase<NHTechnicalStandardDaoRepository, TechnicalStandardDao, Guid>
{
    protected internal override NHTechnicalStandardDaoRepository BuildRepository()
        => new NHTechnicalStandardDaoRepository(_sessionFactory.OpenSession());

    #region Get tests

    [Test]
    public async Task Get_WhenTechnicalStandardDaoExists_ShouldReturnAll()
    {
        // Arrange
        this.LoadScenario("CreateTechnicalStandards");

        // Act
        var allTech = await this.RepositoryUnderTest.GetAsync();

        // Assert
        allTech.Should().HaveCount(30);
    }

    #endregion
}
```

### Ejemplo 3: Tests con TestCase Parametrizados

```csharp
[TestCase("PR-001", nameof(PrototypeResources.Active))]
[TestCase("PR-002", nameof(PrototypeResources.Active))]
[TestCase("PR-003", nameof(PrototypeResources.Expired))]
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

[TestCase("pR-001")]
[TestCase("Pr-001")]
[TestCase("pr-001")]
public async Task CreateAsync_WithDuplicateNumberCaseVariation_ThrowsDuplicatedDomainException(string number)
{
    // Arrange
    LoadScenario("CreateSandBox");
    string baseNumber = "PR-001";
    await this.RepositoryUnderTest.CreateAsync(
        baseNumber,
        _testPrototype!.IssueDate,
        _testPrototype.ExpirationDate,
        _testPrototype.Status);

    // Act
    Func<Task> act = async () => await this.RepositoryUnderTest.CreateAsync(
        number,
        _testPrototype!.IssueDate,
        _testPrototype.ExpirationDate,
        _testPrototype.Status);

    // Assert
    await act.Should().ThrowAsync<DuplicatedDomainException>();
}
```

---

## Mejores Prácticas

### 1. Usar Regiones para Organizar Tests

```csharp
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    #region CreateAsync Tests
    // Tests de creación
    #endregion

    #region GetByEmailAsync Tests
    // Tests de búsqueda
    #endregion

    #region UpdateAsync Tests
    // Tests de actualización
    #endregion

    #region Helper methods
    // Métodos auxiliares
    #endregion
}
```

### 2. Nombres Descriptivos de Tests

Seguir el patrón: `Method_Condition_ExpectedResult`

```csharp
// CORRECTO
CreateAsync_WhenEmailIsValid_ShouldCreateUser
CreateAsync_WhenEmailIsDuplicated_ShouldThrowDuplicatedDomainException
GetByEmailAsync_WhenEmailDoesNotExist_ShouldReturnNull

// INCORRECTO
TestCreate
CreateTest
Test1
```

### 3. Un Assert Lógico por Test

```csharp
// CORRECTO - Un concepto por test
[Test]
public async Task CreateAsync_WhenEmailIsValid_ShouldCreateUser()
{
    await this.RepositoryUnderTest.CreateAsync(_testUser!.Email, _testUser!.Name);

    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{_testUser.Email}'");
    userRows.Count().Should().Be(1);
}

// EVITAR - Múltiples conceptos
[Test]
public async Task CreateAsync_Tests()
{
    // Test 1: crear usuario
    // Test 2: validar duplicado
    // Test 3: validar email inválido
}
```

### 4. Verificar Precondiciones en Helpers

```csharp
private string GetFirstUserEmailFromDb()
{
    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();

    // Precondición explícita
    userRow.Should().NotBeNull("Precondition: There should be at least one user in the dataset");

    return userRow!["email"].ToString()!;
}
```

### 5. Usar AutoFixture para Datos Complejos

```csharp
[SetUp]
public void LocalSetUp()
{
    _testPrototype = fixture.Build<Prototype>()
        .With(x => x.Number, "PR-001")
        .With(x => x.IssueDate, DateTime.UtcNow.AddDays(-1))
        .With(x => x.ExpirationDate, DateTime.UtcNow.AddDays(2))
        .With(x => x.Status, PrototypeResources.Active)
        .Create();
}
```

### 6. Limpiar Base de Datos en Cada Test

El `Setup()` de la clase base ya hace esto:

```csharp
[SetUp]
public void Setup()
{
    this.RepositoryUnderTest = this.BuildRepository();
    this.nDbUnitTest.ClearDatabase(); // Limpia antes de cada test
}
```

---

## Antipatrones

### 1. No Limpiar la Base de Datos

```csharp
// INCORRECTO - Tests dependen del orden de ejecución
[Test]
public async Task Test1_CreateUser() { /* crea usuario */ }

[Test]
public async Task Test2_GetUser() { /* asume que Test1 se ejecutó antes */ }
```

### 2. Hardcodear IDs

```csharp
// INCORRECTO
var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

// CORRECTO - Obtener de la base de datos
var dataSet = this.nDbUnitTest.GetDataSetFromDb();
var userId = dataSet.GetFirstUserRow().Field<Guid>("id");
```

### 3. Tests que Dependen de Otros Tests

```csharp
// INCORRECTO - Test2 depende de Test1
[Test, Order(1)]
public async Task Test1() { /* crea datos */ }

[Test, Order(2)]
public async Task Test2() { /* usa datos de Test1 */ }

// CORRECTO - Cada test es independiente
[Test]
public async Task Test2()
{
    this.LoadScenario("CreateUsers"); // Carga sus propios datos
    // ...
}
```

### 4. No Verificar en Base de Datos

```csharp
// INCORRECTO - Solo verifica el retorno
[Test]
public async Task CreateAsync_ShouldCreateUser()
{
    var result = await this.RepositoryUnderTest.CreateAsync(email, name);
    result.Should().NotBeNull(); // No verifica que se persistió
}

// CORRECTO - Verifica en base de datos
[Test]
public async Task CreateAsync_ShouldCreateUser()
{
    await this.RepositoryUnderTest.CreateAsync(email, name);

    var dataSet = this.nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{email}'");
    userRows.Count().Should().Be(1);
}
```

### 5. Ignorar Case-Sensitivity

```csharp
// INCORRECTO - No prueba variaciones de case
[Test]
public async Task GetByCode_ReturnsResult()
{
    var result = await repo.GetByCodeAsync("TS-001");
}

// CORRECTO - Prueba múltiples cases
[TestCase("TS-001")]
[TestCase("ts-001")]
[TestCase("Ts-001")]
public async Task GetByCode_WithDifferentCase_ReturnsResult(string code)
{
    var result = await repo.GetByCodeAsync(code);
    result.Should().NotBeNull();
}
```

---

## Checklist

### Antes de Crear Tests

- [ ] Escenario XML creado con datos de prueba
- [ ] Validators registrados en `LoadValidators()`
- [ ] Métodos helper en AppSchema (GetXXXRows, GetFirstXXXRow)

### Durante la Implementación

- [ ] Clase hereda de `NHRepositoryTestBase` o `NHReadOnlyRepositoryTestBase`
- [ ] Método `BuildRepository()` implementado
- [ ] LocalSetUp con AutoFixture para datos de prueba
- [ ] Tests organizados en regiones
- [ ] Nombres de tests siguen patrón `Method_Condition_ExpectedResult`
- [ ] Patrón AAA en cada test

### Tests Mínimos por Método

Para `CreateAsync`:
- [ ] Caso exitoso (datos válidos)
- [ ] Validación de campos requeridos (null, empty, whitespace)
- [ ] Validación de duplicados
- [ ] Validación de formato (si aplica)

Para `GetByXXXAsync`:
- [ ] Retorna entidad cuando existe
- [ ] Retorna null cuando no existe
- [ ] Case-insensitive (si aplica)

Para `UpdateAsync`:
- [ ] Actualización exitosa
- [ ] Entidad no existe (ResourceNotFoundException)
- [ ] Duplicados con otra entidad
- [ ] Mismo valor que el actual (sin error)
- [ ] Validación de campos

### Después de la Implementación

- [ ] Todos los tests pasan
- [ ] Cobertura de casos edge
- [ ] Tests son independientes entre sí
- [ ] Sin warnings de NUnit

---

## Referencias

- [NHibernate README](./README.md) - Overview de NHibernate
- [Repositories](./repositories.md) - Implementación de repositorios
- [NDbUnit Documentation](https://github.com/NDbUnit/NDbUnit) - Herramienta de testing
- [FluentAssertions](https://fluentassertions.com/) - Librería de assertions
- [AutoFixture](https://github.com/AutoFixture/AutoFixture) - Generación de datos

---

## Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-01-18 | Versión inicial de Integration Tests guide |

---

**Anterior**: [Best Practices](./best-practices.md) ←
