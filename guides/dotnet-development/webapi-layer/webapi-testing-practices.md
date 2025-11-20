# WebAPI Testing Practices

**Version:** 1.0.0
**Last Updated:** 2025-01-20
**Status:** ✅ Complete

---

## Table of Contents

1. [Introducción](#introducción)
2. [Tipos de Tests](#tipos-de-tests)
3. [Infraestructura de Testing](#infraestructura-de-testing)
4. [Tests de Endpoints](#tests-de-endpoints)
5. [Tests de Mapping Profiles](#tests-de-mapping-profiles)
6. [Assertions y Verificaciones](#assertions-y-verificaciones)
7. [Uso de Escenarios](#uso-de-escenarios)
8. [Convenciones](#convenciones)
9. [Anti-Patrones](#anti-patrones)
10. [Checklist](#checklist)
11. [Referencias](#referencias)

---

## Introducción

Esta guía documenta las **prácticas de testing para la capa WebAPI** en aplicaciones .NET con Fast Endpoints. Los tests de esta capa son **tests de integración** que verifican el comportamiento end-to-end de los endpoints, incluyendo:

- 🔄 Serialización/deserialización JSON
- 🔐 Autenticación y autorización
- ✅ Validación de requests
- 💾 Persistencia en base de datos
- 📤 Formato de responses
- 🗺️ Mapeo con AutoMapper

### ¿Por qué son Importantes?

1. **Validación End-to-End**: Prueban el flujo completo desde HTTP hasta BD
2. **Contrato de API**: Verifican que la API cumple su contrato público
3. **Integración Real**: Usan BD real, no mocks (excepto servicios externos)
4. **Regresión**: Detectan breaking changes en la API
5. **Documentación Viva**: Los tests documentan el comportamiento esperado

---

## Tipos de Tests

### 1. Tests de Endpoints (Integration Tests)

**Propósito**: Verificar comportamiento completo de un endpoint

**Características**:
- ✅ Usa WebApplicationFactory (test server real)
- ✅ HttpClient real para requests HTTP
- ✅ Base de datos real (PostgreSQL)
- ✅ Escenarios XML para datos iniciales
- ✅ NDbUnit para verificar estado de BD
- ✅ Autenticación simulada con TestAuthHandler

**Ubicación**: `tests/{proyecto}.webapi.tests/features/{entity}/`

**Ejemplo**: `CreateUserEndpointTests.cs`, `GetPrototypeEndpointTests.cs`

---

### 2. Tests de Mapping Profiles (Unit Tests)

**Propósito**: Verificar configuración de AutoMapper

**Características**:
- ✅ Tests unitarios (no requieren BD)
- ✅ Usan AutoFixture para generar datos
- ✅ Verifican mapeos Entity → DTO
- ✅ Verifican mapeos Request → Command
- ✅ Validan configuración de AutoMapper

**Ubicación**: `tests/{proyecto}.webapi.tests/mappingprofiles/`

**Ejemplo**: `UserMappingProfileTests.cs`, `PrototypeMappingProfileTests.cs`

---

## Infraestructura de Testing

### 1. EndpointTestBase

Clase base para todos los tests de endpoints.

**Ubicación**: `tests/{proyecto}.webapi.tests/EndpointTestBase.cs`

**Responsabilidades**:
- 🔧 Configurar entorno de testing
- 🗄️ Inicializar NDbUnit
- 🧹 Limpiar BD entre tests
- 📝 Cargar escenarios XML
- 🔐 Crear HttpClient con/sin autenticación
- 🔨 Builders de contenido HTTP

**Estructura**:

```csharp
public abstract class EndpointTestBase
{
    protected internal INDbUnit nDbUnitTest;
    protected internal IConfiguration configuration;
    protected internal IFixture fixture;
    protected internal HttpClient httpClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Cargar .env
        // Configurar AutoFixture
        // Evitar recursión
    }

    [SetUp]
    public void Setup()
    {
        // Limpiar BD
        // Inicializar NDbUnit
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose HttpClient
    }

    // Helper methods
    protected internal void LoadScenario(string scenarioName) { }
    protected static internal HttpClient CreateClient(string authorizedUserName) { }
    protected static internal HttpContent BuildHttpStringContent(object content) { }
}
```

**Uso en Tests**:

```csharp
public class CreateUserEndpointTests : EndpointTestBase
{
    [Test]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        LoadScenario("CreateAdminUser");  // ← De EndpointTestBase
        httpClient = CreateClient("usuario1@example.com");  // ← De EndpointTestBase

        var request = new CreateUserModel.Request
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();  // ← De EndpointTestBase
    }
}
```

---

### 2. CustomWebApplicationFactory<T>

Factory para levantar la aplicación en modo Testing.

**Ubicación**: `tests/{proyecto}.webapi.tests/CustomWebApplicationFactory.cs`

**Implementación**:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace hashira.stone.backend.webapi;

/// <summary>
/// Custom WebApplicationFactory for testing
/// </summary>
/// <typeparam name="TProgram"></typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    /// <summary>
    /// Configure the WebHost for testing
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configuración de servicios para testing
        });

        // Usar ambiente de testing
        builder.UseEnvironment("Testing");
    }
}
```

**Uso**: Se usa internamente en `EndpointTestBase.CreateClient()`.

---

### 3. TestAuthHandler

Handler para simular autenticación en tests.

**Ubicación**: `tests/{proyecto}.webapi.tests/TestAuthHandler.cs`

**Implementación**:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string EMAILCLAIMTYPE = "email";
    private const string USERNAMECLAIMTYPE = "username";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(EMAILCLAIMTYPE, this.ClaimsIssuer),
            new Claim(USERNAMECLAIMTYPE, this.ClaimsIssuer),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}
```

**Uso**: El username se pasa en `CreateClient(username)` y se convierte en Claims.

---

### 4. BaseMappingProfileTests

Clase base para tests de mapping profiles.

**Ubicación**: `tests/{proyecto}.webapi.tests/mappingprofiles/BaseMappingProfileTests.cs`

**Implementación**:

```csharp
using AutoFixture;
using AutoMapper;

public abstract class BaseMappingProfileTests
{
    protected internal IFixture fixture;
    protected internal MapperConfiguration configuration;
    protected internal IMapper mapper;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.fixture = new Fixture()
            .WithAutoMoq()
            .WithoutRecursion();

        this.configuration = new MapperConfiguration(ConfigureProfiles);
        this.mapper = this.configuration.CreateMapper();
    }

    [Test]
    public void MappingConfiguration_ShouldBeValid()
    {
        // Assert
        configuration.AssertConfigurationIsValid();
    }

    /// <summary>
    /// Configure the AutoMapper profiles for this test class
    /// </summary>
    protected abstract void ConfigureProfiles(IMapperConfigurationExpression configuration);
}
```

**Uso en Tests**:

```csharp
public class UserMappingProfileTests : BaseMappingProfileTests
{
    protected override void ConfigureProfiles(IMapperConfigurationExpression configuration)
        => configuration.AddProfile<UserMappingProfile>();

    [Test]
    public void UserDaoToUserDto_WhenAllPropertiesAreSet_ShouldMapCorrectly()
    {
        // Arrange
        var userDao = fixture.Create<User>();

        // Act
        var userDto = mapper.Map<UserDto>(userDao);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Id.Should().Be(userDao.Id);
        userDto.Name.Should().Be(userDao.Name);
        userDto.Email.Should().Be(userDao.Email);
    }
}
```

---

## Tests de Endpoints

### Organización

**Estructura de Carpetas**:

```
tests/{proyecto}.webapi.tests/
├── features/
│   ├── users/
│   │   ├── CreateUserEndpointTests.cs
│   │   ├── GetUserEndpointTests.cs
│   │   ├── UpdateUserEndpointTests.cs
│   │   ├── GetManyUsersEndpointTests.cs
│   │   └── DeleteUserEndpointTests.cs
│   ├── prototypes/
│   │   ├── CreatePrototypeEndpointTests.cs
│   │   ├── GetPrototypeEndpointTests.cs
│   │   ├── UpdatePrototypeEndpointTests.cs
│   │   └── GetManyAndCountPrototypesEndpointTests.cs
│   └── technicalstandards/
│       └── ...
├── mappingprofiles/
│   ├── BaseMappingProfileTests.cs
│   ├── UserMappingProfileTests.cs
│   └── PrototypeMappingProfileTests.cs
├── EndpointTestBase.cs
├── CustomWebApplicationFactory.cs
└── TestAuthHandler.cs
```

**Naming Convention**:

```
{Action}{Entity}EndpointTests.cs

Ejemplos:
✅ CreateUserEndpointTests.cs
✅ GetPrototypeEndpointTests.cs
✅ UpdateTechnicalStandardEndpointTests.cs
✅ GetManyAndCountUsersEndpointTests.cs
✅ DeletePrototypeEndpointTests.cs

❌ UserTests.cs (muy genérico)
❌ TestUserEndpoint.cs (orden incorrecto)
❌ UserEndpoint.cs (no indica que es test)
```

---

### Estructura de un Test de Endpoint

**Template Básico**:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json;
using {proyecto}.webapi.features.{entity}.models;
using {proyecto}.common.tests;

namespace {proyecto}.webapi.tests.features.{entity};

public class {Action}{Entity}EndpointTests : EndpointTestBase
{
    #region Success Tests

    [Test]
    public async Task {Action}{Entity}_With{Condition}_Returns{ExpectedResult}()
    {
        // Arrange
        LoadScenario("Scenario Name");
        httpClient = CreateClient("usuario@example.com");

        var request = new {Action}{Entity}Model.Request { /* ... */ };

        // Act
        var response = await httpClient.{HttpMethod}AsJsonAsync("/endpoint", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.{Expected});
        // Verificar response body
        // Verificar BD con NDbUnit
    }

    #endregion

    #region Failure Tests

    [Test]
    public async Task {Action}{Entity}_With{InvalidCondition}_Returns{ErrorStatus}()
    {
        // Test de caso de error
    }

    #endregion

    #region Helper Methods

    private static void Assert{Entity}{Action}InDatabase(DataSet dataSet, /* params */)
    {
        // Helper para verificar BD
    }

    #endregion
}
```

---

### Patrones por Tipo de Endpoint

#### 1. CREATE Endpoints

**Casos a Probar**:

1. ✅ **Happy Path** - Datos válidos → Created (201)
2. ❌ **Validaciones** - Datos inválidos → BadRequest (400)
3. ❌ **Duplicados** - Registro existente → Conflict (409)
4. ❌ **Autenticación** - Sin auth → Unauthorized (401)
5. ❌ **Autorización** - Sin permisos → Forbidden (403)

**Ejemplo Completo**:

```csharp
public class CreateUserEndpointTests : EndpointTestBase
{
    #region Success Tests

    [Test]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        LoadScenario("CreateAdminUser");  // Usuario admin con permisos
        httpClient = CreateClient("usuario1@example.com");

        var request = new CreateUserModel.Request
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);

        // Assert - Response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Response Body
        var content = await response.Content.ReadAsStringAsync();
        var userResponse = JsonConvert.DeserializeObject<CreateUserModel.Response>(content);
        userResponse.Should().NotBeNull();
        userResponse.User.Email.Should().Be(request.Email);
        userResponse.User.Name.Should().Be(request.Name);

        // Assert - Database
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        AssertUserCreatedInDatabase(dataSet, request.Email, request.Name);
    }

    #endregion

    #region Failure Tests

    [TestCase("test@example.com", "", "The [Name] cannot be null or empty")]
    [TestCase("", "Test User", "The [Email] cannot be null or empty")]
    [TestCase("invalid-email", "Test User", "The [Email] is not a valid email address")]
    public async Task CreateUser_WithInvalidData_ReturnsBadRequest(
        string email,
        string name,
        string expectedErrorMessage)
    {
        // Arrange
        LoadScenario("CreateAdminUser");
        httpClient = CreateClient("usuario1@example.com");

        var request = new CreateUserModel.Request
        {
            Email = email,
            Name = name
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Be(expectedErrorMessage);
    }

    [Test]
    public async Task CreateUser_WithDuplicatedEmail_ReturnsConflict()
    {
        // Arrange
        LoadScenario("CreateAdminUser");  // Contiene usuario1@example.com
        httpClient = CreateClient("usuario1@example.com");

        var request = new CreateUserModel.Request
        {
            Email = "usuario1@example.com",  // Duplicado
            Name = "Test User"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Be("A user with the email 'usuario1@example.com' already exists.");
    }

    [Test]
    public async Task CreateUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        LoadScenario("CreateAdminUser");
        httpClient = CreateClient();  // Sin autenticación

        var request = new CreateUserModel.Request
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateUser_WithoutPermissions_ReturnsForbidden()
    {
        // Arrange
        LoadScenario("CreateUsers");  // Usuario sin permisos de admin
        httpClient = CreateClient("usuario2@example.com");  // No es admin

        var request = new CreateUserModel.Request();

        // Act
        var response = await httpClient.PostAsJsonAsync("/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Helper Methods

    private static void AssertUserCreatedInDatabase(DataSet dataSet, string email, string name)
    {
        var userRows = dataSet.GetUsersRows($"email = '{email}' AND name = '{name}'").ToList();
        userRows.Should().HaveCount(1,
            "User should be created in the database with the correct email and name.");
    }

    #endregion
}
```

---

#### 2. GET Single Endpoints

**Casos a Probar**:

1. ✅ **Existente** - ID/Email válido → OK (200)
2. ❌ **No Existe** - ID/Email inválido → NotFound (404)
3. ❌ **Formato Inválido** - Parámetro malformado → BadRequest (400)
4. ❌ **Sin Autenticación** → Unauthorized (401)
5. ✅ **Con Relaciones** - Incluye datos relacionados

**Ejemplo Completo**:

```csharp
public class GetUserEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GetUser_WithExistingUserName_ReturnsOk()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);
        var targetUserName = "usuario2@example.com";

        // Act
        var response = await httpClient.GetAsync($"/users/{targetUserName}");

        // Assert - Status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Response Body
        var result = await response.Content.ReadFromJsonAsync<GetUserModel.Response>();
        result.Should().NotBeNull();
        result.User.Email.Should().Be(targetUserName);
        result.User.Roles.Should().NotBeNull();
        result.User.Name.Should().NotBeNull();
    }

    [Test]
    public async Task GetUser_WithNonExistentUserName_ReturnsNotFound()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users/nonexistent@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUser_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users/invalid-email-format");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        LoadScenario("CreateUsers");
        this.httpClient = CreateClient();  // Sin autenticación

        // Act
        var response = await httpClient.GetAsync("/users/usuario1@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetUser_WithMultipleRoles_ReturnsOkWithAllRoles()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);
        var targetUserName = "usuario1@example.com";

        // Act
        var response = await httpClient.GetAsync($"/users/{targetUserName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetUserModel.Response>();
        result.Should().NotBeNull();
        result.User.Email.Should().Be(targetUserName);
        result.User.Roles.Should().NotBeNull();
        result.User.Name.Should().NotBeEmpty();
    }
}
```

---

#### 3. UPDATE Endpoints

**Casos a Probar**:

1. ✅ **Happy Path** - Datos válidos → OK (200)
2. ❌ **No Existe** - ID no encontrado → NotFound (404)
3. ❌ **Validaciones** - Datos inválidos → BadRequest (400)
4. ❌ **Duplicados** - Conflicto con otro registro → Conflict (409)
5. ✅ **Mismo Valor** - Actualizar con mismo valor → OK (200) (no-op)
6. ❌ **Sin Autorización** → Forbidden (403)

**Ejemplo Completo**:

```csharp
public class UpdatePrototypeEndpointTests : EndpointTestBase
{
    [Test]
    public async Task UpdatePrototype_WithValidData_ReturnsOk()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        var request = new UpdatePrototypeModel.Request
        {
            Number = "PR-001-UPDATED",
            IssueDate = DateTime.UtcNow.AddDays(-5),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = PrototypeResources.Active
        };

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{prototypeId}", request);

        // Assert - Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Response Body
        var content = await response.Content.ReadAsStringAsync();
        var prototypeResponse = JsonConvert.DeserializeObject<UpdatePrototypeModel.Response>(content);
        AssertPrototypeVerifiedResponse(prototypeResponse, prototypeId, request.Number,
            request.IssueDate, request.ExpirationDate, request.Status);

        // Assert - Database
        var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
        AssertPrototypeUpdatedInDatabase(updatedDataSet, prototypeId, request.Number,
            request.IssueDate, request.ExpirationDate, request.Status);
    }

    [Test]
    public async Task UpdatePrototype_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");

        var nonExistentId = Guid.NewGuid();
        var request = new UpdatePrototypeModel.Request
        {
            Number = "PR-999",
            IssueDate = DateTime.UtcNow.AddDays(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = PrototypeResources.Active
        };

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{nonExistentId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Contain("does not exist");

        // Verify no new prototype was created
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows($"number = '{request.Number}'");
        prototypeRows.Count().Should().Be(0);
    }

    [TestCase("", "Number is required")]
    [TestCase(null, "Number is required")]
    public async Task UpdatePrototype_WithEmptyNumber_ReturnsBadRequest(
        string? number,
        string expectedErrorMessage)
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        var request = new UpdatePrototypeModel.Request
        {
            Number = number ?? string.Empty,
            IssueDate = DateTime.UtcNow.AddDays(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = PrototypeResources.Active
        };

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{prototypeId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Be(expectedErrorMessage);
    }

    [Test]
    public async Task UpdatePrototype_WithDuplicateNumber_ReturnsConflict()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        // Try to update PR-001 with PR-002's number (which already exists)
        var request = new UpdatePrototypeModel.Request
        {
            Number = "PR-002",
            IssueDate = DateTime.UtcNow.AddDays(-1),
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = PrototypeResources.Active
        };

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{prototypeId}", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Contain("already exists");

        // Verify original prototype was not modified
        var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
        var originalPrototypeRows = updatedDataSet.GetPrototypesRows($"id = '{prototypeId}'");
        originalPrototypeRows.First().Field<string>("number").Should().Be("PR-001");
    }

    [Test]
    public async Task UpdatePrototype_WithSameNumber_UpdatesSuccessfully()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        // Update PR-001 keeping the same number but changing other fields
        var request = new UpdatePrototypeModel.Request
        {
            Number = "PR-001",
            IssueDate = DateTime.UtcNow.AddDays(-3),
            ExpirationDate = DateTime.UtcNow.AddDays(45),
            Status = PrototypeResources.Expired
        };

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{prototypeId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
        AssertPrototypeUpdatedInDatabase(updatedDataSet, prototypeId, request.Number,
            request.IssueDate, request.ExpirationDate, request.Status);
    }

    [Test]
    public async Task UpdatePrototype_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuarioInvalido@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        var request = new UpdatePrototypeModel.Request();

        // Act
        var response = await this.httpClient.PutAsJsonAsync($"/prototypes/{prototypeId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #region Helper Methods

    private static void AssertPrototypeUpdatedInDatabase(
        DataSet dataSet, Guid id, string number, DateTime issueDate,
        DateTime expirationDate, string status)
    {
        var prototypeRows = dataSet.GetPrototypesRows($"id = '{id}'");
        prototypeRows.Count().Should().Be(1);
        var prototypeRow = prototypeRows.First();
        prototypeRow.Field<string>("number").Should().Be(number);
        prototypeRow.Field<DateTime>("issue_date").Date.Should().Be(issueDate.Date);
        prototypeRow.Field<DateTime>("expiration_date").Date.Should().Be(expirationDate.Date);
        prototypeRow.Field<string>("status").Should().Be(status);
    }

    private static void AssertPrototypeVerifiedResponse(
        UpdatePrototypeModel.Response? response, Guid id, string number,
        DateTime issueDate, DateTime expirationDate, string status)
    {
        response?.Prototype.Should().NotBeNull();
        response?.Prototype.Id.Should().Be(id);
        response?.Prototype.Number.Should().Be(number);
        response?.Prototype.IssueDate.Date.Should().Be(issueDate.Date);
        response?.Prototype.ExpirationDate.Date.Should().Be(expirationDate.Date);
        response?.Prototype.Status.Should().Be(status);
    }

    #endregion
}
```

---

#### 4. GET Many (Listado) Endpoints

**Casos a Probar**:

1. ✅ **Sin Filtros** - Todos los registros → OK (200)
2. ✅ **Con Query Filter** - Filtrado por query → OK (200)
3. ✅ **Case Insensitive** - Query en diferentes casos
4. ✅ **Paginación** - pageNumber, pageSize
5. ✅ **Sorting** - sortBy, sortDirection
6. ✅ **Sin Resultados** - Lista vacía → OK (200)
7. ❌ **Sin Autorización** → Forbidden (403)

**Ejemplo Completo**:

```csharp
public class GetManyAndCountPrototypesEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GetPrototypesWithoutFilters_ReturnAllPrototypesInPage()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes");

        // Assert - Status
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - Response Body
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.Count.Should().Be(5);
        result.Items.Select(x => x.Number).Should().BeEquivalentTo(
            new[] { "PR-001", "PR-002", "PR-003", "PR-004", "PR-005" });
    }

    [Test]
    public async Task GetPrototypes_WithQueryFilter_ReturnsFilteredPrototypes()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes?query=PR-003");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(p => p.Number == "PR-003");
        result.Count.Should().Be(1);
    }

    [TestCase("PR-003")]
    [TestCase("pr-003")]
    [TestCase("Pr-003")]
    [TestCase("pR-003")]
    [TestCase("PR-003")]
    public async Task GetPrototypes_WithQueryFilter_IsCaseInsensitive(string query)
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync($"/prototypes?query={query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(p => p.Number == "PR-003");
        result.Count.Should().Be(1);
    }

    [Test]
    public async Task GetPrototypes_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes?pageNumber=2&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Number).Should().BeEquivalentTo(new[] { "PR-003", "PR-004" });
    }

    [Test]
    public async Task GetPrototypes_WithSorting_ReturnsSortedPrototypes()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes?sortBy=Number&sortDirection=desc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.Items.Select(x => x.Number).Should().BeInDescendingOrder();
    }

    [Test]
    public async Task GetPrototypes_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var authenticatedUserName = "usuario1@example.com";
        this.httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes?query=NonExistentNumber");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<PrototypeDto>>(content);
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Test]
    public async Task GetPrototypes_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        var unauthorizedUser = "unauthorized@example.com";
        this.httpClient = CreateClient(unauthorizedUser);

        // Act
        var response = await this.httpClient.GetAsync("/prototypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

---

#### 5. DELETE Endpoints

**Casos a Probar** (inferidos, no implementados en proyectos aún):

1. ✅ **Happy Path** - ID existente → NoContent (204) o OK (200)
2. ❌ **No Existe** - ID no encontrado → NotFound (404)
3. ❌ **Con Dependencias** - Registro con relaciones → BadRequest (400) o Conflict (409)
4. ❌ **Sin Autorización** → Forbidden (403)

**Ejemplo (Inferido)**:

```csharp
public class DeletePrototypeEndpointTests : EndpointTestBase
{
    [Test]
    public async Task DeletePrototype_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        // Act
        var response = await this.httpClient.DeleteAsync($"/prototypes/{prototypeId}");

        // Assert - Response
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Database (verificar que se eliminó)
        var updatedDataSet = this.nDbUnitTest.GetDataSetFromDb();
        var deletedRows = updatedDataSet.GetPrototypesRows($"id = '{prototypeId}'");
        deletedRows.Should().BeEmpty();
    }

    [Test]
    public async Task DeletePrototype_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        LoadScenario("CreatePrototypes");
        httpClient = CreateClient("usuario1@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await this.httpClient.DeleteAsync($"/prototypes/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeletePrototype_WithRelatedData_ReturnsConflict()
    {
        // Arrange
        LoadScenario("CreatePrototypesWithInspections");  // Prototypes con inspecciones
        httpClient = CreateClient("usuario1@example.com");

        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var prototypeRows = dataSet.GetPrototypesRows("number = 'PR-001'");
        var prototypeId = prototypeRows.First().Field<Guid>("id");

        // Act
        var response = await this.httpClient.DeleteAsync($"/prototypes/{prototypeId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);
        string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
        errorMessage.Should().Contain("Cannot delete prototype with existing inspections");
    }
}
```

---

## Tests de Mapping Profiles

### Propósito

Verificar que AutoMapper está configurado correctamente y que los mapeos funcionan como se espera.

### Estructura de un Test de Mapping Profile

```csharp
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using {proyecto}.domain.entities;
using {proyecto}.webapi.dtos;
using {proyecto}.webapi.features.{entity}.models;
using {proyecto}.webapi.mappingprofiles;

namespace {proyecto}.webapi.tests.mappingprofiles;

public class {Entity}MappingProfileTests : BaseMappingProfileTests
{
    protected override void ConfigureProfiles(IMapperConfigurationExpression configuration)
        => configuration.AddProfile<{Entity}MappingProfile>();

    [Test]
    public void {Entity}ToDto_WhenAllPropertiesAreSet_ShouldMapCorrectly()
    {
        // Arrange
        var entity = fixture.Create<{Entity}>();

        // Act
        var dto = mapper.Map<{Entity}Dto>(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
        dto.Property.Should().Be(entity.Property);
    }
}
```

### Casos a Probar

#### 1. Mapeo Entity → DTO

**Propósito**: Verificar que la entidad se mapea correctamente al DTO.

```csharp
[Test]
public void UserDaoToUserDto_WhenAllPropertiesAreSet_ShouldMapCorrectly()
{
    // Arrange
    var userDao = fixture.Create<User>();

    // Act
    var userDto = mapper.Map<UserDto>(userDao);

    // Assert
    userDto.Should().NotBeNull();
    userDto.Id.Should().Be(userDao.Id);
    userDto.Name.Should().Be(userDao.Name);
    userDto.Email.Should().Be(userDao.Email);
}
```

#### 2. Mapeo Entity → Response Model

**Propósito**: Verificar que la entidad se mapea correctamente al Response que contiene el DTO.

```csharp
[Test]
public void UserDaoToGetUserModelResponse_WhenAllPropertiesAreSet_ShouldMapCorrectly()
{
    // Arrange
    var userDao = fixture.Create<User>();

    // Act
    var userResponse = mapper.Map<GetUserModel.Response>(userDao);

    // Assert
    userResponse.Should().NotBeNull();
    userResponse.User.Should().NotBeNull();
    userResponse.User.Id.Should().Be(userDao.Id);
    userResponse.User.Name.Should().Be(userDao.Name);
    userResponse.User.Email.Should().Be(userDao.Email);
}
```

#### 3. Mapeo Request Model → Command

**Propósito**: Verificar que el Request del endpoint se mapea correctamente al Command del use case.

```csharp
[Test]
public void GetUserRequestToGetUserUseCaseCommand_WhenAllPropertiesAreSet_ShouldMapCorrectly()
{
    // Arrange
    var userRequest = fixture.Create<GetUserModel.Request>();

    // Act
    var userCommand = mapper.Map<GetUserUseCase.Command>(userRequest);

    // Assert
    userCommand.Should().NotBeNull();
    userCommand.UserName.Should().Be(userRequest.UserName);
}
```

#### 4. Mapeos Complejos (Transformaciones)

**Propósito**: Verificar transformaciones personalizadas.

```csharp
[Test]
public void User_WithMultipleRoles_MapsToRoleNames()
{
    // Arrange
    var user = fixture.Build<User>()
        .With(u => u.Roles, new List<Role>
        {
            new Role("Admin"),
            new Role("Manager")
        })
        .Create();

    // Act
    var dto = mapper.Map<UserDto>(user);

    // Assert
    dto.Roles.Should().BeEquivalentTo(new[] { "Admin", "Manager" });
}
```

#### 5. Test de Configuración

**Propósito**: Verificar que AutoMapper está configurado correctamente.

**Nota**: Este test se hereda automáticamente de `BaseMappingProfileTests`.

```csharp
[Test]
public void MappingConfiguration_ShouldBeValid()
{
    // Assert
    configuration.AssertConfigurationIsValid();
}
```

---

## Assertions y Verificaciones

### 1. Assertions de Response Status

```csharp
// Status Codes comunes
response.StatusCode.Should().Be(HttpStatusCode.OK);           // 200
response.StatusCode.Should().Be(HttpStatusCode.Created);      // 201
response.StatusCode.Should().Be(HttpStatusCode.NoContent);    // 204
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);   // 400
response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // 401
response.StatusCode.Should().Be(HttpStatusCode.Forbidden);    // 403
response.StatusCode.Should().Be(HttpStatusCode.NotFound);     // 404
response.StatusCode.Should().Be(HttpStatusCode.Conflict);     // 409
```

### 2. Assertions de Response Body

**Deserialización y verificación**:

```csharp
// Con System.Net.Http.Json
var result = await response.Content.ReadFromJsonAsync<GetUserModel.Response>();
result.Should().NotBeNull();
result.User.Email.Should().Be("expected@example.com");

// Con Newtonsoft.Json
var content = await response.Content.ReadAsStringAsync();
var result = JsonConvert.DeserializeObject<CreateUserModel.Response>(content);
result.Should().NotBeNull();
result.User.Name.Should().Be("Expected Name");
```

**Verificación de errores**:

```csharp
var content = await response.Content.ReadAsStringAsync();
var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);

// Estructura actual (a mejorar)
string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
errorMessage.Should().Be("Expected error message");
errorMessage.Should().Contain("keyword");
```

### 3. Assertions de Base de Datos

**Usando NDbUnit**:

```csharp
// Obtener DataSet
var dataSet = this.nDbUnitTest.GetDataSetFromDb();

// Verificar existencia
var userRows = dataSet.GetUsersRows($"email = '{email}'");
userRows.Should().HaveCount(1);

// Verificar propiedades
var userRow = userRows.First();
userRow.Field<string>("name").Should().Be("Expected Name");
userRow.Field<Guid>("id").Should().NotBeEmpty();
userRow.Field<DateTime>("creation_date").Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

// Verificar eliminación
var deletedRows = dataSet.GetUsersRows($"id = '{userId}'");
deletedRows.Should().BeEmpty();

// Verificar no creación
var rows = dataSet.GetUsersRows($"email = 'nonexistent@example.com'");
rows.Count().Should().Be(0);
```

### 4. Helper Methods

**Cuándo crear helpers**:
- ✅ Código repetido 3+ veces
- ✅ Assertions complejas de BD
- ✅ Assertions complejas de Response
- ✅ Verificación multi-propiedad

**Patrón de Helper Method**:

```csharp
#region Helper Methods

private static void AssertUserCreatedInDatabase(DataSet dataSet, string email, string name)
{
    var userRows = dataSet.GetUsersRows($"email = '{email}' AND name = '{name}'").ToList();
    userRows.Should().HaveCount(1,
        "User should be created in the database with the correct email and name.");

    var userRow = userRows.First();
    userRow.Field<Guid>("id").Should().NotBeEmpty();
    userRow.Field<string>("email").Should().Be(email);
    userRow.Field<string>("name").Should().Be(name);
    userRow.Field<DateTime>("creation_date").Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
}

private static void AssertPrototypeVerifiedResponse(
    CreatePrototypeModel.Response response, string number, DateTime issueDate,
    DateTime expirationDate, string status)
{
    response.Prototype.Should().NotBeNull();
    response.Prototype.Number.Should().Be(number);
    response.Prototype.IssueDate.Date.Should().Be(issueDate.Date);
    response.Prototype.ExpirationDate.Date.Should().Be(expirationDate.Date);
    response.Prototype.Status.Should().Be(status);
}

#endregion
```

---

## Uso de Escenarios

### Escenarios Compartidos

**Importante**: Los escenarios XML son los **mismos archivos** usados en tests de repositorios.

**Ubicación**: `tests/{proyecto}.infrastructure.tests/scenarios/`

**Configuración**: Variable de entorno `SCENARIOS_FOLDER_PATH` apunta a esta carpeta.

### Cargar Escenarios

```csharp
[Test]
public async Task CreateUser_Test()
{
    // Arrange
    LoadScenario("CreateAdminUser");  // Carga CreateAdminUser.xml

    // El escenario carga:
    // - roles (PlatformAdministrator)
    // - users (usuario1@example.com con rol admin)
    // - users_in_roles (relación)

    httpClient = CreateClient("usuario1@example.com");  // Usuario del escenario

    // Rest of test...
}
```

### Convenciones de Escenarios

1. **Naming**: `Create{Entity}s.xml`, `Create{Entity}WithRelations.xml`

2. **Contenido mínimo para WebAPI tests**:
   - Al menos 1 usuario con rol para autenticación
   - Datos necesarios para el test específico

3. **Ejemplos**:
   ```xml
   <!-- CreateAdminUser.xml -->
   <AppSchema>
     <roles>
       <id>660e8400-e29b-41d4-a716-446655440001</id>
       <name>PlatformAdministrator</name>
     </roles>

     <users>
       <id>550e8400-e29b-41d4-a716-446655440001</id>
       <email>usuario1@example.com</email>
       <name>Admin User</name>
     </users>

     <users_in_roles>
       <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
       <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
     </users_in_roles>
   </AppSchema>
   ```

### Reutilización

```csharp
// El mismo escenario puede usarse en múltiples tests
[Test]
public async Task GetUser_Test()
{
    LoadScenario("CreateUsers");  // Reutiliza escenario
    // ...
}

[Test]
public async Task UpdateUser_Test()
{
    LoadScenario("CreateUsers");  // Mismo escenario
    // ...
}
```

**Ver**: [Scenarios Creation Guide](../infrastructure-layer/orm-implementations/nhibernate/scenarios-creation-guide.md)

---

## Convenciones

### 1. Naming Conventions

**Tests de Endpoints**:

```
{Action}{Entity}EndpointTests.cs

Pattern de métodos de test:
{Action}{Entity}_{With/Without}{Condition}_{Returns/ShouldReturn}{ExpectedResult}

Ejemplos:
✅ CreateUser_WithValidData_ReturnsCreated
✅ GetPrototype_WithExistingId_ReturnsOk
✅ UpdateUser_WithInvalidEmail_ReturnsBadRequest
✅ GetManyUsers_WithoutFilters_ReturnsAllUsers
✅ DeletePrototype_WithNonExistentId_ReturnsNotFound
```

**Tests de Mapping Profiles**:

```
{Entity}MappingProfileTests.cs

Pattern de métodos de test:
{Source}To{Destination}_When{Condition}_Should{ExpectedResult}

Ejemplos:
✅ UserDaoToUserDto_WhenAllPropertiesAreSet_ShouldMapCorrectly
✅ RequestToCommand_WhenValidData_ShouldMapAllProperties
✅ EntityToResponse_WithNullableProperties_ShouldHandleNulls
```

### 2. Organización con Regiones

```csharp
public class CreateUserEndpointTests : EndpointTestBase
{
    #region Success Tests

    [Test]
    public async Task CreateUser_WithValidData_ReturnsCreated() { }

    #endregion

    #region Failure Tests

    [Test]
    public async Task CreateUser_WithInvalidData_ReturnsBadRequest() { }

    [Test]
    public async Task CreateUser_WithDuplicate_ReturnsConflict() { }

    #endregion

    #region Helper Methods

    private static void AssertUserCreatedInDatabase() { }

    #endregion
}
```

### 3. TestCase Parametrización

**Usar TestCase para**:
- ✅ Múltiples valores inválidos
- ✅ Diferentes formatos (case-insensitive)
- ✅ Boundary values
- ✅ Variaciones del mismo escenario

```csharp
[TestCase("test@example.com", "", "The [Name] cannot be null or empty")]
[TestCase("", "Test User", "The [Email] cannot be null or empty")]
[TestCase("invalid-email", "Test User", "The [Email] is not a valid email address")]
public async Task CreateUser_WithInvalidData_ReturnsBadRequest(
    string email, string name, string expectedErrorMessage)
{
    // Test implementation
}
```

**NO usar TestCase para**:
- ❌ Lógicas completamente diferentes
- ❌ Cuando el Arrange es muy diferente
- ❌ Cuando el Assert es muy diferente

### 4. Orden de Assertions

**Patrón recomendado**:

```csharp
// 1. Assert Response Status
response.StatusCode.Should().Be(HttpStatusCode.Created);

// 2. Assert Response Body (si aplica)
var content = await response.Content.ReadAsStringAsync();
var result = JsonConvert.DeserializeObject<Response>(content);
result.Should().NotBeNull();
result.User.Email.Should().Be(expectedEmail);

// 3. Assert Database State
var dataSet = this.nDbUnitTest.GetDataSetFromDb();
var rows = dataSet.GetUsersRows($"email = '{expectedEmail}'");
rows.Should().HaveCount(1);
```

### 5. Response y Database Verification

**¿Cuándo verificar Response?**
- ✅ Siempre verificar status code
- ✅ Verificar body cuando es parte del contrato (GET, CREATE con 201)
- ⚠️ Opcional verificar body en UPDATE/DELETE si no retorna datos relevantes

**¿Cuándo verificar BD?**
- ✅ Siempre en CREATE (verificar que se creó)
- ✅ Siempre en UPDATE (verificar que se modificó)
- ✅ Siempre en DELETE (verificar que se eliminó)
- ✅ En validaciones de duplicados (verificar que NO se creó)
- ❌ No necesario en GET (solo verificar response)

**Patrón pragmático**:

```csharp
// CREATE: Verificar response Y database
[Test]
public async Task CreateUser_Test()
{
    // Act
    var response = await httpClient.PostAsJsonAsync("/users", request);

    // Assert Response
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var result = await response.Content.ReadFromJsonAsync<Response>();
    result.User.Email.Should().Be(request.Email);

    // Assert Database
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    AssertUserCreatedInDatabase(dataSet, request.Email, request.Name);
}

// GET: Solo verificar response
[Test]
public async Task GetUser_Test()
{
    // Act
    var response = await httpClient.GetAsync("/users/email@example.com");

    // Assert Response (suficiente)
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<Response>();
    result.User.Email.Should().Be("email@example.com");
}

// UPDATE: Verificar database primariamente
[Test]
public async Task UpdateUser_Test()
{
    // Act
    var response = await httpClient.PutAsJsonAsync($"/users/{userId}", request);

    // Assert Response
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Assert Database (más importante)
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var row = dataSet.GetUsersRows($"id = '{userId}'").First();
    row.Field<string>("name").Should().Be(request.NewName);
}
```

---

## Anti-Patrones

### ❌ 1. Usar Repositorio en Arrange o Assert

**Problema**: Igual que en tests de repositorios, NO usar el código bajo test para preparar datos o verificar resultados.

```csharp
❌ INCORRECTO:

[Test]
public async Task UpdateUser_Test()
{
    // Arrange - USA ENDPOINT BAJO TEST (MAL)
    var createResponse = await httpClient.PostAsJsonAsync("/users", createRequest);
    var createdUser = await createResponse.Content.ReadFromJsonAsync<Response>();

    // Act
    await httpClient.PutAsJsonAsync($"/users/{createdUser.Id}", updateRequest);

    // Assert - USA ENDPOINT BAJO TEST (MAL)
    var getResponse = await httpClient.GetAsync($"/users/{createdUser.Id}");
    var result = await getResponse.Content.ReadFromJsonAsync<Response>();
}
```

```csharp
✅ CORRECTO:

[Test]
public async Task UpdateUser_Test()
{
    // Arrange - USA ESCENARIO
    LoadScenario("CreateUsers");
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var userId = dataSet.GetFirstUserRow().Field<Guid>("id");

    httpClient = CreateClient("usuario1@example.com");

    // Act - SOLO AQUÍ USA ENDPOINT
    await httpClient.PutAsJsonAsync($"/users/{userId}", updateRequest);

    // Assert - USA NDBUNIT
    var updatedDataSet = nDbUnitTest.GetDataSetFromDb();
    var row = updatedDataSet.GetUsersRows($"id = '{userId}'").First();
    row.Field<string>("name").Should().Be(updateRequest.Name);
}
```

---

### ❌ 2. No Mockear Servicios Externos

**Problema**: Tests ejecutan servicios reales (email, SMS, APIs externas) que:
- Son lentos
- Pueden fallar por razones externas
- Generan side effects no deseados

```csharp
❌ INCORRECTO:

[Test]
public async Task CreateUser_Test()
{
    // Arrange
    LoadScenario("CreateAdminUser");
    httpClient = CreateClient("usuario1@example.com");

    var request = new CreateUserModel.Request
    {
        Email = "test@example.com",
        Name = "Test User"
    };

    // Act
    var response = await httpClient.PostAsJsonAsync("/users", request);

    // Problema: Se ejecuta EmailService.SendWelcomeEmail() real
    // - Envía email real
    // - Puede fallar si servicio email está caído
    // - Es lento
}
```

```csharp
✅ CORRECTO:

[Test]
public async Task CreateUser_Test()
{
    // Arrange
    LoadScenario("CreateAdminUser");

    // Mock del servicio externo
    var mockEmailService = new Mock<IEmailService>();
    mockEmailService
        .Setup(s => s.SendWelcomeEmailAsync(It.IsAny<string>()))
        .Returns(Task.CompletedTask);

    // Crear cliente con mock
    httpClient = CreateClient("usuario1@example.com", services =>
    {
        services.AddSingleton(mockEmailService.Object);
    });

    var request = new CreateUserModel.Request
    {
        Email = "test@example.com",
        Name = "Test User"
    };

    // Act
    var response = await httpClient.PostAsJsonAsync("/users", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    // Verify el mock fue llamado
    mockEmailService.Verify(
        s => s.SendWelcomeEmailAsync("test@example.com"),
        Times.Once);
}
```

**Servicios que típicamente se deben mockear**:
- 📧 Email services (SendGrid, SMTP)
- 📱 SMS services (Twilio)
- ☁️ Cloud storage (S3, Azure Blob)
- 🔔 Notification services (Push notifications)
- 💳 Payment gateways (Stripe, PayPal)
- 📊 Analytics services (Google Analytics, Mixpanel)
- 🔍 Search services (Elasticsearch)

**Servicios que NO se mockean (tests de integración)**:
- ✅ Base de datos (PostgreSQL)
- ✅ NHibernate / ORM
- ✅ AutoMapper
- ✅ FluentValidation
- ✅ Domain entities
- ✅ Repositories

---

### ❌ 3. Parsing de Errores con `generalErrors`

**Problema**: La estructura actual de errores es frágil y poco tipada.

```csharp
❌ INCORRECTO (estructura actual, pero mala práctica):

var content = await response.Content.ReadAsStringAsync();
var errorResponse = JsonConvert.DeserializeObject<dynamic>(content);

// Frágil, no tipado, propenso a errores
string errorMessage = errorResponse!.errors.generalErrors[0].ToString();
errorMessage.Should().Be("Expected error message");
```

**Problema con esta estructura**:
- No es type-safe
- `dynamic` oculta errores en compile-time
- Difícil de mantener
- No sigue estándares (RFC 7807 Problem Details)

**💡 Recomendación Futura**: Implementar estructura tipada de errores:

```csharp
✅ MEJOR (a implementar en el futuro):

// 1. Definir modelo de error tipado
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
}

// 2. Usar en tests
var content = await response.Content.ReadAsStringAsync();
var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);

errorResponse.Should().NotBeNull();
errorResponse.Status.Should().Be(400);
errorResponse.Detail.Should().Contain("Name is required");
errorResponse.Errors.Should().ContainKey("Name");
```

**Por ahora**: Continuar usando `generalErrors` pero documentarlo como mala práctica a mejorar.

---

### ❌ 4. No Limpiar BD Entre Tests

**Problema**: State contamination entre tests.

```csharp
❌ INCORRECTO:

public class CreateUserEndpointTests : EndpointTestBase
{
    // No sobrescribe Setup

    [Test]
    public async Task Test1()
    {
        LoadScenario("CreateUsers");
        // Crea datos
    }

    [Test]
    public async Task Test2()
    {
        // Asume BD vacía pero tiene datos de Test1
        // Test falla de forma inconsistente
    }
}
```

```csharp
✅ CORRECTO:

public class CreateUserEndpointTests : EndpointTestBase
{
    // EndpointTestBase.Setup() limpia BD automáticamente

    [Test]
    public async Task Test1()
    {
        LoadScenario("CreateUsers");
        // Cada test empieza con BD limpia
    }

    [Test]
    public async Task Test2()
    {
        // BD está limpia, test es independiente
    }
}
```

**Nota**: `EndpointTestBase.Setup()` ya hace `nDbUnitTest.ClearDatabase()`. No lo sobrescribas sin llamar a `base.Setup()`.

---

### ❌ 5. Tests Dependientes

**Problema**: Tests que dependen del orden de ejecución.

```csharp
❌ INCORRECTO:

private static Guid _createdUserId;  // ← State compartido entre tests

[Test]
public async Task Step1_CreateUser()
{
    var response = await httpClient.PostAsJsonAsync("/users", request);
    var result = await response.Content.ReadFromJsonAsync<Response>();
    _createdUserId = result.User.Id;  // ← Guardando state
}

[Test]
public async Task Step2_UpdateUser()
{
    // Depende de que Step1 se ejecute primero
    await httpClient.PutAsJsonAsync($"/users/{_createdUserId}", updateRequest);
}
```

```csharp
✅ CORRECTO:

[Test]
public async Task CreateUser_Test()
{
    // Test independiente
    LoadScenario("CreateAdminUser");
    var response = await httpClient.PostAsJsonAsync("/users", request);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Test]
public async Task UpdateUser_Test()
{
    // Test independiente con su propio setup
    LoadScenario("CreateUsers");
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var userId = dataSet.GetFirstUserRow().Field<Guid>("id");

    await httpClient.PutAsJsonAsync($"/users/{userId}", updateRequest);
}
```

**Regla**: Cada test debe ser completamente independiente y poder ejecutarse en cualquier orden.

---

### ❌ 6. Ignorar Verificación de BD

**Problema**: Solo verificar response sin verificar persistencia.

```csharp
❌ INCORRECTO (incompleto):

[Test]
public async Task CreateUser_Test()
{
    // Act
    var response = await httpClient.PostAsJsonAsync("/users", request);

    // Assert - Solo verifica response
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    // ❌ No verifica que realmente se guardó en BD
}
```

```csharp
✅ CORRECTO:

[Test]
public async Task CreateUser_Test()
{
    // Act
    var response = await httpClient.PostAsJsonAsync("/users", request);

    // Assert Response
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    // Assert Database ← IMPORTANTE
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var userRows = dataSet.GetUsersRows($"email = '{request.Email}'");
    userRows.Should().HaveCount(1);
    userRows.First().Field<string>("name").Should().Be(request.Name);
}
```

**Excepciones**: GET endpoints pueden omitir verificación de BD ya que no modifican datos.

---

### ❌ 7. No Usar AutoFixture en Mapping Tests

**Problema**: Crear datos manualmente en tests de mappings.

```csharp
❌ INCORRECTO:

[Test]
public void UserToDto_Test()
{
    // Arrange - Manual
    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        Name = "Test",
        // ... 20 propiedades más
    };

    // ...
}
```

```csharp
✅ CORRECTO:

[Test]
public void UserToDto_Test()
{
    // Arrange - AutoFixture
    var user = fixture.Create<User>();

    // Act
    var dto = mapper.Map<UserDto>(user);

    // Assert
    dto.Id.Should().Be(user.Id);
    dto.Email.Should().Be(user.Email);
}
```

**Ventaja**: AutoFixture genera datos realistas automáticamente.

---

## Checklist

### Checklist de Endpoint Tests

Para cada endpoint, verificar:

#### CREATE Endpoint

- [ ] **Happy Path**
  - [ ] Request con datos válidos → 201 Created
  - [ ] Response body contiene el DTO creado
  - [ ] Registro existe en BD con datos correctos

- [ ] **Validaciones**
  - [ ] Campos requeridos vacíos → 400 BadRequest
  - [ ] Formatos inválidos → 400 BadRequest
  - [ ] Mensajes de error son claros

- [ ] **Duplicados**
  - [ ] Registro duplicado → 409 Conflict
  - [ ] Mensaje de error específico
  - [ ] No se crea registro duplicado en BD

- [ ] **Autenticación/Autorización**
  - [ ] Sin autenticación → 401 Unauthorized
  - [ ] Sin permisos → 403 Forbidden

---

#### GET Single Endpoint

- [ ] **Happy Path**
  - [ ] ID/Email existente → 200 OK
  - [ ] Response body contiene datos correctos
  - [ ] Relaciones se cargan correctamente (si aplica)

- [ ] **Casos de Error**
  - [ ] ID/Email no existe → 404 NotFound
  - [ ] Formato inválido → 400 BadRequest

- [ ] **Autenticación**
  - [ ] Sin autenticación → 401 Unauthorized

---

#### UPDATE Endpoint

- [ ] **Happy Path**
  - [ ] Datos válidos → 200 OK
  - [ ] Response body contiene datos actualizados
  - [ ] BD refleja los cambios

- [ ] **Casos de Error**
  - [ ] ID no existe → 404 NotFound
  - [ ] Validaciones fallan → 400 BadRequest
  - [ ] Duplicado con otro registro → 409 Conflict

- [ ] **Edge Cases**
  - [ ] Actualizar con mismo valor → 200 OK (no error)
  - [ ] Actualizar solo algunos campos → 200 OK

- [ ] **Autenticación/Autorización**
  - [ ] Sin autenticación → 401 Unauthorized
  - [ ] Sin permisos → 403 Forbidden

---

#### DELETE Endpoint

- [ ] **Happy Path**
  - [ ] ID existente → 204 NoContent (o 200 OK)
  - [ ] Registro eliminado de BD

- [ ] **Casos de Error**
  - [ ] ID no existe → 404 NotFound
  - [ ] Registro con dependencias → 409 Conflict

- [ ] **Autenticación/Autorización**
  - [ ] Sin autenticación → 401 Unauthorized
  - [ ] Sin permisos → 403 Forbidden

---

#### GET Many Endpoint

- [ ] **Listado Básico**
  - [ ] Sin filtros → 200 OK con todos los registros
  - [ ] Response contiene Items, Count, PageNumber, PageSize

- [ ] **Filtros**
  - [ ] Con query filter → solo registros que coinciden
  - [ ] Query es case-insensitive
  - [ ] Sin resultados → 200 OK con lista vacía

- [ ] **Paginación**
  - [ ] pageNumber y pageSize funcionan correctamente
  - [ ] Response tiene metadata de paginación

- [ ] **Sorting**
  - [ ] sortBy y sortDirection funcionan
  - [ ] Orden ascendente/descendente correcto

- [ ] **Autenticación/Autorización**
  - [ ] Sin autenticación → 401 Unauthorized
  - [ ] Sin permisos → 403 Forbidden

---

### Checklist de Mapping Profile Tests

Para cada Mapping Profile, verificar:

- [ ] **Configuración**
  - [ ] `MappingConfiguration_ShouldBeValid()` pasa
  - [ ] Profile hereda de `BaseMappingProfileTests`

- [ ] **Mapeos Entity → DTO**
  - [ ] Todas las propiedades públicas se mapean
  - [ ] Propiedades calculadas se transforman correctamente
  - [ ] Relaciones complejas se aplanan (IList<Role> → IEnumerable<string>)

- [ ] **Mapeos Entity → Response**
  - [ ] Response.{Dto} contiene el DTO correcto

- [ ] **Mapeos Request → Command**
  - [ ] Todas las propiedades del request se mapean al command

- [ ] **Edge Cases**
  - [ ] Propiedades nullable se manejan correctamente
  - [ ] Colecciones vacías no causan errores
  - [ ] Valores default se mapean correctamente

---

### Checklist General

- [ ] **Organización**
  - [ ] Tests en carpeta correcta: `features/{entity}/`
  - [ ] Naming convention correcta
  - [ ] Regiones organizadas (#region Success Tests, etc.)

- [ ] **Setup/Teardown**
  - [ ] Hereda de `EndpointTestBase`
  - [ ] No sobrescribe Setup sin llamar a `base.Setup()`
  - [ ] Dispose HttpClient en TearDown (hecho por base)

- [ ] **Escenarios**
  - [ ] LoadScenario() se usa para Arrange
  - [ ] Escenarios contienen usuario con permisos para auth
  - [ ] Escenarios son reutilizables

- [ ] **Assertions**
  - [ ] FluentAssertions en todos los asserts
  - [ ] Mensajes descriptivos con "because"
  - [ ] NDbUnit para verificar BD (en CREATE/UPDATE/DELETE)
  - [ ] Helper methods para assertions complejas

- [ ] **Anti-Patrones Evitados**
  - [ ] No usar endpoint bajo test en Arrange/Assert
  - [ ] Servicios externos mockeados
  - [ ] No usar `generalErrors` (o documentar como temporal)
  - [ ] Tests independientes (no orden de ejecución)
  - [ ] BD limpia entre tests

---

## Referencias

### Guías Relacionadas

**Testing**:
- [Repository Testing Practices](../infrastructure-layer/orm-implementations/nhibernate/repository-testing-practices.md)
- [Scenarios Creation Guide](../infrastructure-layer/orm-implementations/nhibernate/scenarios-creation-guide.md)
- [Integration Tests](../infrastructure-layer/orm-implementations/nhibernate/integration-tests.md)
- [Testing Conventions](../testing-conventions.md)

**WebAPI Layer**:
- [DTOs](./dtos.md)
- [Request/Response Models](./request-response-models.md)
- [AutoMapper Profiles](./automapper-profiles.md)
- [FastEndpoints Basics](./fastendpoints-basics.md)
- [Error Responses](./error-responses.md)

**Domain Layer**:
- [Entities](../domain-layer/entities.md)
- [Domain Exceptions](../domain-layer/domain-exceptions.md)

### Frameworks y Herramientas

- **NUnit**: https://docs.nunit.org/
- **FluentAssertions**: https://fluentassertions.com/
- **AutoFixture**: https://github.com/AutoFixture/AutoFixture
- **WebApplicationFactory**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **NDbUnit**: Testing framework para BD
- **Newtonsoft.Json**: https://www.newtonsoft.com/json

### Archivos de Referencia del Proyecto

**Tests de Endpoints**:
- [CreateUserEndpointTests.cs](../../tests/hashira.stone.backend.webapi.tests/users/CreateUserEndpointTests.cs)
- [GetUserEndpointTests.cs](../../tests/hashira.stone.backend.webapi.tests/features/users/GetUserEndPointTest.cs)
- [CreatePrototypeEndpointTests.cs](../../tests/hashira.stone.backend.webapi.tests/features/prototypes/CreatePrototypeEndpointTests.cs)
- [UpdatePrototypeEndpointTests.cs](../../tests/hashira.stone.backend.webapi.tests/features/prototypes/UpdatePrototypeEndpointTests.cs)
- [GetManyAndCountPrototypesEndpointTests.cs](../../tests/hashira.stone.backend.webapi.tests/features/prototypes/GetManyAndCountPrototypesEndpointTests.cs)

**Tests de Mapping Profiles**:
- [UserMappingProfileTests.cs](../../tests/hashira.stone.backend.webapi.tests/mappingprofiles/UserMappingProfileTests.cs)
- [BaseMappingProfileTests.cs](../../tests/hashira.stone.backend.webapi.tests/mappingprofiles/BaseMappingProfileTests.cs)

**Infraestructura**:
- [EndpointTestBase.cs](../../tests/hashira.stone.backend.webapi.tests/EndpointTestBase.cs)
- [CustomWebApplicationFactory.cs](../../tests/hashira.stone.backend.webapi.tests/CustomWebApplicationFactory.cs)
- [TestAuthHandler.cs](../../tests/hashira.stone.backend.webapi.tests/TestAuthHandler.cs)

---

## Changelog

### Version 1.0.0 (2025-01-20)
- ✅ Initial release
- ✅ Complete documentation of WebAPI testing patterns
- ✅ Integration tests for all endpoint types (CREATE, GET, UPDATE, DELETE, GET Many)
- ✅ Mapping profile tests
- ✅ Infrastructure classes (EndpointTestBase, CustomWebApplicationFactory, TestAuthHandler)
- ✅ Assertions and verifications (Response + Database)
- ✅ Usage of XML scenarios (shared with repository tests)
- ✅ Naming conventions and organization
- ✅ Anti-patterns and best practices
- ✅ Complete checklists for all endpoint types
- ✅ Examples from reference projects

---

**Siguiente Guía**: [Endpoints Implementation Guide](./endpoints-implementation.md) (pendiente)

[◀️ Volver al WebApi Layer](./README.md) | [🏠 Inicio](../README.md)
