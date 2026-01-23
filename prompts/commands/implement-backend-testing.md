# Implement Backend Testing

> **Version Comando:** 1.1.0
> **Ultima actualizacion:** 2025-01-23

---

Implementa los tests de un feature backend siguiendo el plan de testing y las guias de testing de APSYS. Ejecuta las 4 fases secuencialmente: Scenarios → Unit Tests → Repository Tests → WebAPI Tests.

## Entrada

**Plan a implementar:** $ARGUMENTS

- Si se proporciona un nombre de plan, busca en `.claude/planning/{$ARGUMENTS}-testing-plan.md`
- Si se proporciona `--phase={scenarios|unit|repository|webapi}`, ejecuta solo esa fase
- Si `$ARGUMENTS` esta vacio, pregunta al usuario que plan desea implementar

**Ejemplos:**
```bash
/implement-backend-testing gestion-proveedores
/implement-backend-testing gestion-proveedores --phase=unit
/implement-backend-testing --phase=scenarios
```

## Configuracion

**Ubicacion de planes:** `.claude/planning/`

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Guias a Consultar

Antes de implementar cada fase, lee las guias relevantes desde `{GUIDES_REPO}`:

### Convenciones Generales

| Guia | Ruta |
|------|------|
| Convenciones de Testing | `testing/fundamentals/guides/conventions.md` |

### Unit Tests

| Guia | Ruta |
|------|------|
| Domain Testing | `testing/unit/guides/domain-testing.md` |

### Integration Tests - Repository

| Guia | Ruta |
|------|------|
| Database Testing | `testing/integration/guides/database-testing.md` |
| Infrastructure Testing | `testing/unit/guides/infrastructure-testing.md` |

### Integration Tests - WebAPI

| Guia | Ruta |
|------|------|
| WebAPI Testing | `testing/integration/guides/webapi-testing.md` |

### Scenarios

| Guia | Ruta |
|------|------|
| Scenarios Creation | `testing/integration/scenarios/guides/scenarios-creation-guide.md` |

---

## Proceso de Implementacion

### Paso 0: Cargar y Validar Plan de Testing

1. Lee el archivo de plan desde `.claude/planning/{feature}-testing-plan.md`
2. Extrae informacion del plan de testing
3. Muestra resumen al usuario:

```markdown
## Testing Plan: {nombre}
**Entidad Principal:** {Entity}
**Plan de Implementacion Origen:** {ruta-al-plan-de-implementacion}
**Fases:** Scenarios → Unit Tests → Repository Tests → WebAPI Tests

### Tests a Implementar
- Unit Tests: {n} archivos
- Repository Tests: {n} archivos
- WebAPI Tests: {n} archivos
- Scenarios: {n} archivos

¿Continuar con la implementacion?
```

### Paso 1: Explorar Tests Existentes

Busca tests existentes como referencia para todas las fases:

```bash
# Unit Tests
Glob: **/*.UnitTests/**/*Tests.cs
Glob: **/*.UnitTests/**/domain/*Tests.cs

# Integration Tests - Repository
Glob: **/*.IntegrationTests/**/infrastructure/*Tests.cs
Glob: **/*.IntegrationTests/**/NH*RepositoryTests.cs

# Integration Tests - WebAPI
Glob: **/*.IntegrationTests/**/webapi/**/*Tests.cs
Glob: **/*.IntegrationTests/**/endpoints/*EndpointTests.cs
Glob: **/*.IntegrationTests/**/mappingprofiles/*MappingProfileTests.cs

# Scenarios
Glob: **/*.IntegrationTests/scenarios/**/Sc*.cs
Glob: **/*.IntegrationTests/scenarios/**/*.xml

# Test Infrastructure
Glob: **/*TestBase.cs
Glob: **/CustomWebApplicationFactory.cs
```

---

## Fase 0: Scenarios de Datos

Extrae de la seccion "Fase 0: Scenarios de Datos" del plan.

### 0.1 Identificar Dependencias de Scenarios

Antes de crear el scenario principal, verifica si hay dependencias:

```bash
# Buscar scenarios existentes que pueden ser dependencias
Grep: "class Sc.*Create{DependencyEntity}" **/*.IntegrationTests/scenarios/**/*.cs
```

### 0.2 Crear Scenario

**Archivo:** `{proyecto}.IntegrationTests/scenarios/Sc###Create{Entity}.cs`

**Estructura:**

```csharp
using {proyecto}.Domain.Entities;
using {proyecto}.IntegrationTests.Scenarios.Infrastructure;

namespace {proyecto}.IntegrationTests.Scenarios;

public class Sc###Create{Entity} : IScenario
{
    // Si hay dependencias
    public Type? PreloadScenario => typeof(Sc###Create{DependencyEntity});

    public IEnumerable<object> GetData()
    {
        yield return new {Entity}
        {
            Id = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"),
            // Propiedades segun el plan
        };
    }
}
```

**Convenciones:**

- Numero de scenario (###) segun convencion del proyecto
- GUIDs predecibles para tests
- Datos representativos para validaciones
- PreloadScenario si hay dependencias FK

### Verificacion Fase 0

- [ ] Scenario implementa IScenario
- [ ] PreloadScenario definido si hay dependencias
- [ ] GUIDs son estaticos y predecibles
- [ ] Datos cubren casos de test

---

### CHECKPOINT: Regenerar XMLs de Scenarios

**IMPORTANTE:** Si se crearon nuevos scenarios en esta fase, los archivos XML deben regenerarse antes de ejecutar los tests de integracion.

**Accion requerida del usuario:**

```markdown
## Scenarios Creados

Se han creado los siguientes scenarios:
- `Sc###Create{Entity}.cs`
- (otros scenarios si aplica)

## Accion Requerida

Para que los tests de integracion funcionen, debes regenerar los XMLs.

### Opcion 1: Usar script existente (Recomendado)

Si existe un script `buildscenarios.bat` en la raiz del proyecto:

```bash
.\buildscenarios.bat
```

### Opcion 2: Ejecutar manualmente

1. **Compilar el proyecto de scenarios:**
   ```bash
   cd tests/{proyecto}.scenarios
   dotnet build
   ```

2. **Ejecutar el .exe compilado con argumentos:**
   ```bash
   cd bin/Debug/net9.0
   {proyecto}.scenarios.exe /cnn:"Host=localhost;Port=5432;Database={dbname};Username=postgres;Password=root;" /output:"{ruta-salida-xmls}"
   ```

**Argumentos requeridos:**
| Argumento | Descripcion |
|-----------|-------------|
| `/cnn:` | Connection string a la BD de desarrollo/test |
| `/output:` | Ruta donde se generaran los XMLs |

**Ubicacion tipica del proyecto de scenarios:**
- `tests/{proyecto}.scenarios/`

## Verificacion

Despues de ejecutar el generador, verifica que existan los XMLs en la ruta de salida:
- `{output}/Create{Entity}.xml`

---

**¿XMLs regenerados?** Confirma para continuar con la implementacion de tests.
- "si" o "continuar" - Proceder con Fase 1 (Unit Tests)
- "solo unit" - Implementar solo Unit Tests (no requieren scenarios)
- "cancelar" - Pausar implementacion
```

**Nota:** Los Unit Tests (Fase 1) NO requieren scenarios y pueden implementarse sin esperar la regeneracion de XMLs. Los Integration Tests (Fases 2 y 3) SI requieren que los XMLs esten actualizados.

**Referencia:** Ver `{GUIDES_REPO}/testing/integration/scenarios/guides/scenarios-creation-guide.md` para mas detalles.

---

## Fase 1: Unit Tests - Domain Layer

Extrae de la seccion "Fase 1: Unit Tests - Domain Layer" del plan.

### 1.1 Entity Tests

**Archivo:** `{proyecto}.UnitTests/domain/{Entity}Tests.cs`

**Estructura:**

```csharp
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.Domain.Entities;

namespace {proyecto}.UnitTests.Domain;

[TestFixture]
public class {Entity}Tests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesEntity()
    {
        // Arrange
        var expectedName = "Test Name";

        // Act
        var entity = new {Entity}(expectedName);

        // Assert
        entity.Name.Should().Be(expectedName);
    }

    [Test]
    public void GetValidator_ReturnsValidatorInstance()
    {
        // Arrange
        var entity = new {Entity}();

        // Act
        var validator = entity.GetValidator();

        // Assert
        validator.Should().NotBeNull();
        validator.Should().BeOfType<{Entity}Validator>();
    }

    [Test]
    public void Validate_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var entity = new {Entity}
        {
            // Propiedades validas segun el plan
        };

        // Act
        var result = entity.Validate();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validate_With{InvalidField}_ReturnsError()
    {
        // Arrange
        var entity = new {Entity}
        {
            {Field} = {invalidValue}
        };

        // Act
        var result = entity.Validate();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "{Field}");
    }
}
```

### 1.2 Validator Tests

**Archivo:** `{proyecto}.UnitTests/domain/{Entity}ValidatorTests.cs`

**Estructura:**

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using {proyecto}.Domain.Entities;
using {proyecto}.Domain.Entities.Validators;

namespace {proyecto}.UnitTests.Domain;

[TestFixture]
public class {Entity}ValidatorTests
{
    private {Entity}Validator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new {Entity}Validator();
    }

    [Test]
    public void Validate_{Field}_WhenEmpty_ShouldHaveError()
    {
        // Arrange
        var entity = new {Entity} { {Field} = string.Empty };

        // Act
        var result = _validator.TestValidate(entity);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.{Field});
    }

    [Test]
    public void Validate_{Field}_WhenExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var entity = new {Entity} { {Field} = new string('x', 101) };

        // Act
        var result = _validator.TestValidate(entity);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.{Field});
    }

    [Test]
    public void Validate_WhenAllFieldsValid_ShouldNotHaveErrors()
    {
        // Arrange
        var entity = new {Entity}
        {
            // Todas las propiedades con valores validos
        };

        // Act
        var result = _validator.TestValidate(entity);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

### Verificacion Fase 1

- [ ] Entity tests cubren constructor y validacion
- [ ] Validator tests cubren cada regla de validacion
- [ ] Usa FluentAssertions para assertions
- [ ] Patron AAA (Arrange-Act-Assert) en todos los tests
- [ ] Naming convention: `Method_Condition_ExpectedResult`

---

### CHECKPOINT: Verificar XMLs Antes de Integration Tests

**IMPORTANTE:** Antes de continuar con los Integration Tests (Fases 2 y 3), verifica que los XMLs de scenarios esten actualizados.

```markdown
## Verificacion de XMLs

Si se crearon scenarios nuevos en Fase 0, confirma que:

1. Se ejecuto el generador de scenarios usando una de estas opciones:

   **Opcion A - Script (si existe):**
   ```bash
   .\buildscenarios.bat
   ```

   **Opcion B - Manual:**
   ```bash
   cd tests/{proyecto}.scenarios
   dotnet build
   cd bin/Debug/net9.0
   {proyecto}.scenarios.exe /cnn:"..." /output:"..."
   ```

2. Existen los archivos XML en la ruta de salida configurada:
   - `{output}/Create{Entity}.xml`

3. Los XMLs contienen los datos esperados del scenario

---

**¿XMLs listos?** Confirma para continuar con Integration Tests.
- "si" o "continuar" - Proceder con Fase 2 (Repository Tests)
- "cancelar" - Pausar implementacion
```

**Si los XMLs no estan listos:** Los tests de integracion fallaran porque LoadScenario() no encontrara los datos en la base de datos.

---

## Fase 2: Integration Tests - Repository Layer

Extrae de la seccion "Fase 2: Integration Tests - Repository Layer" del plan.

### 2.1 Repository Tests

**Archivo:** `{proyecto}.IntegrationTests/infrastructure/NH{Entity}RepositoryTests.cs`

**Clase base:** `NHRepositoryTestBase<NH{Entity}Repository, {Entity}, Guid>`

**IMPORTANTE - Antipatron a evitar:**
> NUNCA usar el repositorio bajo test para Arrange o Assert.
> - Arrange: Usar `LoadScenario<Sc###Create{Entity}>()`
> - Assert: Usar NDbUnit para verificar estado de BD

**Estructura:**

```csharp
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.Domain.Entities;
using {proyecto}.Domain.Exceptions;
using {proyecto}.Infrastructure.NHibernate;
using {proyecto}.IntegrationTests.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.Infrastructure;

[TestFixture]
public class NH{Entity}RepositoryTests
    : NHRepositoryTestBase<NH{Entity}Repository, {Entity}, Guid>
{
    #region CreateAsync

    [Test]
    public async Task CreateAsync_WithValidData_ShouldInsertAndReturnEntity()
    {
        // Arrange - NO usar LoadScenario, crear datos nuevos
        var name = "New Entity Name";

        // Act
        var result = await Repository.CreateAsync(name);

        // Assert - Verificar con NDbUnit, NO con Repository
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        // Verificar en BD con NDbUnit
        var dbEntity = NDbUnit.GetById<{Entity}>(result.Id);
        dbEntity.Should().NotBeNull();
        dbEntity!.Name.Should().Be(name);
    }

    [Test]
    public async Task CreateAsync_WithInvalidData_ShouldThrowInvalidDomainException()
    {
        // Arrange
        var invalidName = string.Empty;

        // Act
        var act = async () => await Repository.CreateAsync(invalidName);

        // Assert
        await act.Should().ThrowAsync<InvalidDomainException>();
    }

    [Test]
    public async Task CreateAsync_WithDuplicate{UniqueField}_ShouldThrowDuplicatedDomainException()
    {
        // Arrange - Cargar scenario con entidad existente
        LoadScenario<Sc###Create{Entity}>();
        var existingValue = "{valor-del-scenario}";

        // Act
        var act = async () => await Repository.CreateAsync(existingValue);

        // Assert
        await act.Should().ThrowAsync<DuplicatedDomainException>();
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEntity()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");

        // Act
        var result = await Repository.GetByIdAsync(existingId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingId);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await Repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateAsync

    [Test]
    public async Task UpdateAsync_WithValidData_ShouldUpdateInDatabase()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");
        var newName = "Updated Name";

        // Act
        var result = await Repository.UpdateAsync(existingId, newName);

        // Assert - Verificar con NDbUnit
        var dbEntity = NDbUnit.GetById<{Entity}>(existingId);
        dbEntity!.Name.Should().Be(newName);
    }

    [Test]
    public async Task UpdateAsync_WithNonExistingId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var act = async () => await Repository.UpdateAsync(nonExistingId, "New Name");

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFromDatabase()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");

        // Act
        await Repository.DeleteAsync(existingId);

        // Assert - Verificar con NDbUnit que no existe
        var dbEntity = NDbUnit.GetById<{Entity}>(existingId);
        dbEntity.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistingId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var act = async () => await Repository.DeleteAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    #endregion
}
```

### Verificacion Fase 2

- [ ] Tests heredan de NHRepositoryTestBase
- [ ] LoadScenario() usado para Arrange (NO Repository)
- [ ] NDbUnit usado para Assert de estado de BD
- [ ] Cubren happy path y casos de error
- [ ] Excepciones de dominio testeadas

---

## Fase 3: Integration Tests - WebAPI Layer

Extrae de la seccion "Fase 3: Integration Tests - WebAPI Layer" del plan.

### 3.1 MappingProfile Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/mappingprofiles/{Entity}MappingProfileTests.cs`

**Estructura:**

```csharp
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.Domain.Entities;
using {proyecto}.WebApi.Dtos;
using {proyecto}.WebApi.Features.{Entity}.Models;
using {proyecto}.WebApi.MappingProfiles;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;

namespace {proyecto}.IntegrationTests.WebApi.MappingProfiles;

[TestFixture]
public class {Entity}MappingProfileTests
    : BaseMappingProfileTests<{Entity}MappingProfile>
{
    [Test]
    public void Configuration_ShouldBeValid()
    {
        AssertConfigurationIsValid();
    }

    [Test]
    public void Map_{Entity}_To_{Entity}Dto_ShouldMapCorrectly()
    {
        // Arrange
        var entity = Fixture.Create<{Entity}>();

        // Act
        var dto = Mapper.Map<{Entity}Dto>(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        // Verificar todas las propiedades mapeadas
    }

    [Test]
    public void Map_Create{Entity}Request_To_Command_ShouldMapCorrectly()
    {
        // Arrange
        var request = Fixture.Create<Create{Entity}Model.Request>();

        // Act
        var command = Mapper.Map<Create{Entity}UseCase.Command>(request);

        // Assert
        command.Name.Should().Be(request.Name);
        // Verificar todas las propiedades mapeadas
    }

    [Test]
    public void Map_{Entity}_To_Create{Entity}Response_ShouldMapCorrectly()
    {
        // Arrange
        var entity = Fixture.Create<{Entity}>();

        // Act
        var response = Mapper.Map<Create{Entity}Model.Response>(entity);

        // Assert
        response.{Entity}.Should().NotBeNull();
        response.{Entity}.Id.Should().Be(entity.Id);
    }
}
```

### 3.2 Create Endpoint Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/endpoints/Create{Entity}EndpointTests.cs`

**Estructura:**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.WebApi.Dtos;
using {proyecto}.WebApi.Features.{Entity}.Models;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.WebApi.Endpoints;

[TestFixture]
public class Create{Entity}EndpointTests : EndpointTestBase
{
    private const string Endpoint = "/api/{entities}";

    [Test]
    public async Task Create_WithValidRequest_ShouldReturn201AndEntity()
    {
        // Arrange
        var request = new Create{Entity}Model.Request
        {
            Name = "New Entity"
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<Create{Entity}Model.Response>();
        result.Should().NotBeNull();
        result!.{Entity}.Name.Should().Be(request.Name);
    }

    [Test]
    public async Task Create_WithInvalidRequest_ShouldReturn400()
    {
        // Arrange
        var request = new Create{Entity}Model.Request
        {
            Name = string.Empty // Invalid
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_WithDuplicate_ShouldReturn409()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var request = new Create{Entity}Model.Request
        {
            Name = "{valor-existente-en-scenario}"
        };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        RemoveAuthentication();
        var request = new Create{Entity}Model.Request { Name = "Test" };

        // Act
        var response = await Client.PostAsJsonAsync(Endpoint, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### 3.3 Get Endpoint Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/endpoints/Get{Entity}EndpointTests.cs`

**Estructura:**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.WebApi.Features.{Entity}.Models;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.WebApi.Endpoints;

[TestFixture]
public class Get{Entity}EndpointTests : EndpointTestBase
{
    private const string Endpoint = "/api/{entities}";

    [Test]
    public async Task Get_WithExistingId_ShouldReturn200AndEntity()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");

        // Act
        var response = await Client.GetAsync($"{Endpoint}/{existingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Get{Entity}Model.Response>();
        result.Should().NotBeNull();
        result!.{Entity}.Id.Should().Be(existingId);
    }

    [Test]
    public async Task Get_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{Endpoint}/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Get_WithInvalidIdFormat_ShouldReturn400()
    {
        // Arrange
        var invalidId = "not-a-guid";

        // Act
        var response = await Client.GetAsync($"{Endpoint}/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### 3.4 GetManyAndCount Endpoint Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/endpoints/GetManyAndCount{Entities}EndpointTests.cs`

**Estructura:**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.WebApi.Features.{Entity}.Models;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.WebApi.Endpoints;

[TestFixture]
public class GetManyAndCount{Entities}EndpointTests : EndpointTestBase
{
    private const string Endpoint = "/api/{entities}";

    [Test]
    public async Task GetMany_WithoutFilters_ShouldReturnAllEntities()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();

        // Act
        var response = await Client.GetAsync(Endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetManyAndCount{Entities}Model.Response>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetMany_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        LoadScenario<Sc###CreateMultiple{Entities}>();
        var pageSize = 2;
        var pageNumber = 1;

        // Act
        var response = await Client.GetAsync($"{Endpoint}?pageSize={pageSize}&pageNumber={pageNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetManyAndCount{Entities}Model.Response>();
        result!.Items.Should().HaveCountLessOrEqualTo(pageSize);
    }

    [Test]
    public async Task GetMany_WithFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var filterValue = "{valor-a-filtrar}";

        // Act
        var response = await Client.GetAsync($"{Endpoint}?name={filterValue}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetManyAndCount{Entities}Model.Response>();
        result!.Items.Should().OnlyContain(x => x.Name.Contains(filterValue));
    }
}
```

### 3.5 Update Endpoint Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/endpoints/Update{Entity}EndpointTests.cs`

**Estructura:**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.WebApi.Features.{Entity}.Models;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.WebApi.Endpoints;

[TestFixture]
public class Update{Entity}EndpointTests : EndpointTestBase
{
    private const string Endpoint = "/api/{entities}";

    [Test]
    public async Task Update_WithValidRequest_ShouldReturn200AndUpdatedEntity()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");
        var request = new Update{Entity}Model.Request
        {
            Name = "Updated Name"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"{Endpoint}/{existingId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Update{Entity}Model.Response>();
        result!.{Entity}.Name.Should().Be(request.Name);
    }

    [Test]
    public async Task Update_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var request = new Update{Entity}Model.Request { Name = "Test" };

        // Act
        var response = await Client.PutAsJsonAsync($"{Endpoint}/{nonExistingId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Update_WithInvalidRequest_ShouldReturn400()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");
        var request = new Update{Entity}Model.Request { Name = string.Empty };

        // Act
        var response = await Client.PutAsJsonAsync($"{Endpoint}/{existingId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### 3.6 Delete Endpoint Tests

**Archivo:** `{proyecto}.IntegrationTests/webapi/endpoints/Delete{Entity}EndpointTests.cs`

**Estructura:**

```csharp
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using {proyecto}.IntegrationTests.WebApi.Infrastructure;
using {proyecto}.IntegrationTests.Scenarios;

namespace {proyecto}.IntegrationTests.WebApi.Endpoints;

[TestFixture]
public class Delete{Entity}EndpointTests : EndpointTestBase
{
    private const string Endpoint = "/api/{entities}";

    [Test]
    public async Task Delete_WithExistingId_ShouldReturn200()
    {
        // Arrange
        LoadScenario<Sc###Create{Entity}>();
        var existingId = new Guid("{guid-del-scenario}");

        // Act
        var response = await Client.DeleteAsync($"{Endpoint}/{existingId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Delete_WithNonExistingId_ShouldReturn404()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"{Endpoint}/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Verificacion Fase 3

- [ ] MappingProfile tests verifican configuracion valida
- [ ] Endpoint tests cubren happy path y errores
- [ ] LoadScenario() usado para datos de prueba
- [ ] Codigos HTTP correctos verificados
- [ ] Tests de autenticacion incluidos

---

## Formato de Salida

Al finalizar todas las fases, genera el reporte con la siguiente estructura.

> **Nota:** `{VERSION_COMANDO}` debe sustituirse por la version declarada en el encabezado de este prompt (campo "Version Comando").

```markdown
# Testing Implementation Complete

> **Generado con:** implement-backend-testing v{VERSION_COMANDO}
> **Fecha:** {fecha de generacion}

---

**Feature:** {nombre}
**Entidad:** {Entity}
**Plan de Testing:** `.claude/planning/{feature}-testing-plan.md`

## Scenarios

| Archivo | Descripcion |
|---------|-------------|
| `Sc###Create{Entity}.cs` | Scenario principal con {n} registros |

## Unit Tests

| Archivo | Tests |
|---------|-------|
| `{Entity}Tests.cs` | {n} tests |
| `{Entity}ValidatorTests.cs` | {n} tests |

## Integration Tests - Repository

| Archivo | Tests |
|---------|-------|
| `NH{Entity}RepositoryTests.cs` | {n} tests |

## Integration Tests - WebAPI

| Archivo | Tests |
|---------|-------|
| `{Entity}MappingProfileTests.cs` | {n} tests |
| `Create{Entity}EndpointTests.cs` | {n} tests |
| `Get{Entity}EndpointTests.cs` | {n} tests |
| `GetManyAndCount{Entities}EndpointTests.cs` | {n} tests |
| `Update{Entity}EndpointTests.cs` | {n} tests |
| `Delete{Entity}EndpointTests.cs` | {n} tests |

## Resumen

| Tipo | Archivos | Tests |
|------|----------|-------|
| Scenarios | {n} | - |
| Unit Tests | {n} | {n} |
| Repository Tests | {n} | {n} |
| WebAPI Tests | {n} | {n} |
| **Total** | **{n}** | **{n}** |

## Ejecucion de Tests

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar solo unit tests
dotnet test --filter "FullyQualifiedName~UnitTests"

# Ejecutar solo integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

**Status:** SUCCESS
```

---

## Manejo de Errores

Si alguna fase falla:

```markdown
# Testing Implementation PAUSED

> **Generado con:** implement-backend-testing v{VERSION_COMANDO}
> **Fecha:** {fecha de generacion}

---

**Fase:** {0: Scenarios | 1: Unit Tests | 2: Repository Tests | 3: WebAPI Tests}
**Error:** {descripcion}

## Progreso

| Fase | Status |
|------|--------|
| Scenarios | {completado/fallido/pendiente} |
| Unit Tests | {completado/fallido/pendiente} |
| Repository Tests | {completado/fallido/pendiente} |
| WebAPI Tests | {completado/fallido/pendiente} |

## Opciones
1. "reintentar" - Volver a intentar la fase fallida
2. "continuar" - Saltar y seguir con la siguiente fase
3. "cancelar" - Abortar implementacion
```

---

## Restricciones

### NO debes:
- Inventar tests no especificados en el plan
- Usar el repositorio bajo test para Arrange o Assert
- Crear tests sin seguir el patron AAA
- Ignorar convenciones de naming de tests
- Crear tests que dependan del orden de ejecucion
- **Ejecutar integration tests sin que el usuario confirme que los XMLs estan regenerados**
- Continuar automaticamente despues de crear scenarios nuevos

### DEBES:
- Seguir estrictamente las guias de testing
- Usar implementaciones existentes como referencia de patrones
- Implementar TODOS los tests listados en el plan
- Verificar que los tests compilan al final de cada fase
- Usar LoadScenario() para Arrange en integration tests
- Usar NDbUnit para Assert de estado de BD en repository tests
- Mostrar progreso claro al usuario
- **Esperar confirmacion del usuario despues de crear scenarios nuevos** (para que regenere XMLs)
- **Verificar que XMLs existen antes de proceder con integration tests**

---

## Anti-Patterns a Evitar

### Scenarios

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| GUIDs aleatorios | Tests no reproducibles | Usar GUIDs estaticos |
| Datos incompletos | Tests fallan por FK | Definir PreloadScenario |
| Scenario monolitico | Dificil mantenimiento | Un scenario por entidad |
| Sin XML generado | Tests no tienen datos | Ejecutar generador |

### Unit Tests

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Multiples asserts sin relacion | Test hace demasiado | Un concepto por test |
| Setup complejo | Dificil entender | Arrange simple y claro |
| Tests interdependientes | Orden afecta resultado | Tests aislados |
| Sin FluentAssertions | Mensajes poco claros | Usar .Should() |

### Repository Tests

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Usar Repository para Arrange | Test no es unitario | Usar LoadScenario() |
| Usar Repository para Assert | No verifica BD real | Usar NDbUnit |
| No limpiar BD entre tests | Tests contaminados | TestBase limpia automaticamente |
| Hardcodear connection string | No portable | Usar configuracion de test |

### Endpoint Tests

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| No verificar status code | Ignora errores HTTP | Siempre verificar StatusCode |
| Deserializar sin verificar | Null reference | Verificar response != null |
| Tests sin autenticacion | No prueba seguridad | Incluir test de 401 |
| Hardcodear URLs | Fragil ante cambios | Usar constantes |

### General

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Test names poco descriptivos | Dificil entender fallo | `Method_Condition_Expected` |
| Magic strings/numbers | Dificil mantener | Usar constantes |
| Ignorar tests fallidos | Deuda tecnica | Arreglar o eliminar |
| Copy-paste de tests | Duplicacion | Extraer metodos helper |

---

## Referencias

### Convenciones
- [Testing Conventions]({GUIDES_REPO}/testing/fundamentals/guides/conventions.md)

### Unit Tests
- [Domain Testing]({GUIDES_REPO}/testing/unit/guides/domain-testing.md)

### Integration Tests
- [Database Testing]({GUIDES_REPO}/testing/integration/guides/database-testing.md)
- [WebAPI Testing]({GUIDES_REPO}/testing/integration/guides/webapi-testing.md)
- [Infrastructure Testing]({GUIDES_REPO}/testing/unit/guides/infrastructure-testing.md)

### Scenarios
- [Scenarios Creation]({GUIDES_REPO}/testing/integration/scenarios/guides/scenarios-creation-guide.md)
