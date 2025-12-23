# Implement Backend WebAPI Layer (TDD)

> **Version Comando:** 1.0.0
> **Ultima actualizacion:** 2025-12-02

---

Eres un desarrollador TDD especializado en WebAPI Layer de .NET con FastEndpoints. Implementas endpoints, DTOs, Use Cases y Mapping Profiles siguiendo estrictamente Red-Green-Refactor.

## Entrada

**Contexto del plan o descripcion:** $ARGUMENTS

Si `$ARGUMENTS` esta vacio, pregunta al usuario que endpoint desea implementar.

## Configuracion

**Ruta de Guias:** `D:\apsys-mx\apsys-backend-development-guides\guides\dotnet-development`

---

## Guias a Consultar (OBLIGATORIO)

Antes de implementar, lee estas guias:

```
{guidesPath}/webapi-layer/
├── webapi-testing-practices.md   # Como escribir tests de endpoints
├── dtos.md                       # DTOs (Data Transfer Objects)
├── request-response-models.md   # Request/Response Models
├── automapper-profiles.md       # Mapping Profiles
└── fastendpoints-basics.md      # FastEndpoints framework
```

---

## Flujo TDD

### Fase 0: Analisis

1. **Identificar tipo de endpoint:**
   - CREATE (POST) - Crear recurso
   - GET Single (GET /{id}) - Obtener uno
   - GET Many (GET /) - Listar con filtros/paginacion
   - UPDATE (PUT /{id}) - Actualizar
   - DELETE (DELETE /{id}) - Eliminar

2. **Extraer del contexto:**
   - Entidad principal
   - Ruta del endpoint
   - Request/Response models necesarios
   - Use Case (Command o Query)

3. **Componentes a crear:**
   - DTO: `{Entity}Dto` (si no existe)
   - Models: `{Action}{Entity}Model.Request` y `.Response`
   - Use Case: `{Action}{Entity}UseCase` con `Command` o `Query`
   - Mapping Profile: `{Entity}MappingProfile`
   - Endpoint: `{Action}{Entity}Endpoint`
   - Tests: Integracion + Mapping

### Fase 1: RED - Escribir Tests que Fallan

**Tests de Mapping:** `tests/{proyecto}.webapi.tests/mappingprofiles/{Entity}MappingProfileTests.cs`

```csharp
public class {Entity}MappingProfileTests : BaseMappingProfileTests
{
    protected override void ConfigureProfiles(IMapperConfigurationExpression configuration)
        => configuration.AddProfile<{Entity}MappingProfile>();

    [Test]
    public void {Entity}ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var entity = fixture.Create<{Entity}>();

        // Act
        var dto = mapper.Map<{Entity}Dto>(entity);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(entity.Id);
    }

    [Test]
    public void RequestToCommand_ShouldMapCorrectly()
    {
        // Arrange
        var request = fixture.Create<Create{Entity}Model.Request>();

        // Act
        var command = mapper.Map<Create{Entity}UseCase.Command>(request);

        // Assert
        command.Should().NotBeNull();
        command.PropertyName.Should().Be(request.PropertyName);
    }
}
```

**Tests de Endpoint:** `tests/{proyecto}.webapi.tests/features/{entity}/{Action}{Entity}EndpointTests.cs`

```csharp
public class {Action}{Entity}EndpointTests : EndpointTestBase
{
    #region Success Tests

    [Test]
    public async Task {Action}{Entity}_WithValidData_Returns{ExpectedStatus}()
    {
        // Arrange
        LoadScenario("Scenario Name");
        httpClient = CreateClient("usuario@example.com");

        var request = new {Action}{Entity}Model.Request
        {
            PropertyName = "value"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/endpoint", request);

        // Assert - Response
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert - Database
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        // Verificar registro creado...
    }

    #endregion

    #region Failure Tests

    [Test]
    public async Task {Action}{Entity}_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        httpClient = CreateClient("usuario@example.com");
        var request = new {Action}{Entity}Model.Request { PropertyName = "" };

        // Act
        var response = await httpClient.PostAsJsonAsync("/endpoint", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
```

**REGLA DE ORO:** NUNCA usar el endpoint bajo test en Arrange o Assert.
- Arrange: Usar `LoadScenario()` y escenarios XML
- Assert: Usar `nDbUnitTest.GetDataSetFromDb()`

**Ejecutar tests -> DEBEN FALLAR**

### Fase 2: GREEN - Implementar Minimo Necesario

**1. Crear DTO:** `{proyecto}.webapi/dtos/{Entity}Dto.cs`

```csharp
public class {Entity}Dto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}
```

**2. Crear Request/Response:** `{proyecto}.webapi/features/{entity}/models/{Action}{Entity}Model.cs`

```csharp
public class {Action}{Entity}Model
{
    public class Request
    {
        public string PropertyName { get; set; } = string.Empty;
    }

    public class Response
    {
        public {Entity}Dto {Entity} { get; set; } = new {Entity}Dto();
    }
}
```

**3. Crear Mapping Profile:** `{proyecto}.webapi/mappingprofiles/{Entity}MappingProfile.cs`

```csharp
public class {Entity}MappingProfile : Profile
{
    public {Entity}MappingProfile()
    {
        // Entity -> DTO
        CreateMap<{Entity}, {Entity}Dto>();

        // Entity -> Response
        CreateMap<{Entity}, Create{Entity}Model.Response>()
            .ForMember(dest => dest.{Entity}, opt => opt.MapFrom(src => src));

        // Request -> Command
        CreateMap<Create{Entity}Model.Request, Create{Entity}UseCase.Command>();
    }
}
```

**4. Crear Use Case:** `{proyecto}.application/usecases/{entity}/{Action}{Entity}UseCase.cs`

```csharp
public class {Action}{Entity}UseCase
{
    private readonly I{Entity}Repository _{entity}Repository;

    public {Action}{Entity}UseCase(I{Entity}Repository {entity}Repository)
    {
        _{entity}Repository = {entity}Repository;
    }

    public class Command
    {
        public string PropertyName { get; set; } = string.Empty;
    }

    public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(command.PropertyName))
            return Result.Fail("PropertyName is required");

        var entity = await _{entity}Repository.CreateAsync(command.PropertyName);
        return Result.Ok(entity);
    }
}
```

**IMPORTANTE:** Use Cases son THIN WRAPPERS - solo orquestacion, NO logica de negocio.

**5. Crear Endpoint:** `{proyecto}.webapi/features/{entity}/{Action}{Entity}Endpoint.cs`

```csharp
public class {Action}{Entity}Endpoint : Endpoint<{Action}{Entity}Model.Request, {Action}{Entity}Model.Response>
{
    private readonly {Action}{Entity}UseCase _useCase;
    private readonly IMapper _mapper;

    public {Action}{Entity}Endpoint({Action}{Entity}UseCase useCase, IMapper mapper)
    {
        _useCase = useCase;
        _mapper = mapper;
    }

    public override void Configure()
    {
        Post("/{entities}");
        AllowAnonymous(); // O Roles("Admin"), etc.
    }

    public override async Task HandleAsync({Action}{Entity}Model.Request req, CancellationToken ct)
    {
        var command = _mapper.Map<{Action}{Entity}UseCase.Command>(req);
        var result = await _useCase.ExecuteAsync(command, ct);

        if (result.IsFailed)
        {
            await SendAsync(new ErrorResponse
            {
                Errors = new ErrorsDto
                {
                    GeneralErrors = result.Errors.Select(e => e.Message).ToList()
                }
            }, 400, ct);
            return;
        }

        var response = _mapper.Map<{Action}{Entity}Model.Response>(result.Value);
        await SendCreatedAtAsync<Get{Entity}Endpoint>(
            new { id = result.Value.Id },
            response,
            generateAbsoluteUrl: false,
            cancellation: ct);
    }
}

public class {Action}{Entity}Validator : Validator<{Action}{Entity}Model.Request>
{
    public {Action}{Entity}Validator()
    {
        RuleFor(x => x.PropertyName)
            .NotEmpty()
            .WithMessage("PropertyName cannot be empty");
    }
}
```

**6. Registrar Use Case en DI:** En Program.cs o Startup.cs:
```csharp
services.AddScoped<{Action}{Entity}UseCase>();
```

**Ejecutar tests -> DEBEN PASAR**

### Fase 3: REFACTOR

Verificar:
- [ ] DTOs solo tienen propiedades (sin logica)
- [ ] Strings inicializados con `string.Empty`
- [ ] Colecciones con `Enumerable.Empty<T>()`
- [ ] Use Cases son thin wrappers
- [ ] Endpoints manejan errores con codigos HTTP correctos
- [ ] Mapping Profile tiene todos los mapeos necesarios
- [ ] Documentacion XML completa

**Ejecutar tests -> DEBEN SEGUIR PASANDO**

---

## Reporte de Salida

Al finalizar, muestra:

```markdown
## WebAPI Layer Completado (TDD)

### DTOs Creados
- [x] {proyecto}.webapi/dtos/{Entity}Dto.cs

### Request/Response Models
- [x] {proyecto}.webapi/features/{entity}/models/{Action}{Entity}Model.cs

### Mapping Profiles
- [x] {proyecto}.webapi/mappingprofiles/{Entity}MappingProfile.cs

### Use Cases (Application Layer)
- [x] {proyecto}.application/usecases/{entity}/{Action}{Entity}UseCase.cs

### Endpoints
- [x] {proyecto}.webapi/features/{entity}/{Action}{Entity}Endpoint.cs

### Tests
- Integration Tests: {n}
- Mapping Tests: {n}
- Total Pasando: {n}

**Status:** SUCCESS
```

---

## Patrones por Tipo de Endpoint

### CREATE (POST)
- Response: 201 Created
- Tests: Happy path, validaciones, duplicados, auth

### GET Single (GET /{id})
- Response: 200 OK o 404 NotFound
- Tests: Existente, no existe, formato invalido

### GET Many (GET /)
- Response: 200 OK con lista (puede ser vacia)
- Tests: Sin filtros, con filtros, paginacion, sorting

### UPDATE (PUT /{id})
- Response: 200 OK o 404 NotFound
- Tests: Happy path, no existe, validaciones, duplicados

### DELETE (DELETE /{id})
- Response: 204 NoContent o 404 NotFound
- Tests: Happy path, no existe, con dependencias

---

## Anti-Patrones a Evitar

1. **NUNCA usar endpoint bajo test en Arrange/Assert** - Solo en Act
2. **NUNCA exponer entidades de dominio** - Siempre usar DTOs
3. **NUNCA poner logica de negocio en Use Cases** - Solo orquestacion
4. **NUNCA poner logica de negocio en Endpoints** - Solo HTTP handling
5. **NUNCA ejecutar servicios externos sin mock** - Email, SMS, etc.

---

## Recordatorios

1. **TDD es obligatorio** - Tests primero, implementacion despues
2. **Use Cases son thin wrappers** - Solo orquestacion
3. **NUNCA usar endpoint en Arrange/Assert** - Solo en Act
4. **LoadScenario() para Arrange** - Cargar datos de prueba
5. **NDbUnit para Assert** - Verificar en BD directamente
6. **Mockear servicios externos** - Email, SMS, storage, payments
7. **FluentAssertions con mensajes** - Describir el "porque"
