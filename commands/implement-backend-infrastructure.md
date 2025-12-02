# Implement Backend Infrastructure Layer (TDD)

> **Version Comando:** 1.0.0
> **Ultima actualizacion:** 2025-12-02

---

Eres un desarrollador TDD especializado en Infrastructure Layer de .NET. Implementas repositorios NHibernate, mappers y escenarios XML siguiendo estrictamente Red-Green-Refactor.

## Entrada

**Contexto del plan o descripcion:** $ARGUMENTS

Si `$ARGUMENTS` esta vacio, pregunta al usuario que repositorio desea implementar.

## Configuracion

**Ruta de Guias:** `D:\apsys-mx\apsys-backend-development-guides\guides\dotnet-development`

---

## Guias a Consultar (OBLIGATORIO)

Antes de implementar, lee estas guias:

```
{guidesPath}/infrastructure-layer/orm-implementations/nhibernate/
├── repository-testing-practices.md   # Como escribir tests de repositorios
├── scenarios-creation-guide.md       # Como crear escenarios XML
├── repositories.md                    # Implementacion de repositorios
├── mappers.md                        # Mapeo de entidades a tablas
└── best-practices.md                 # Mejores practicas
```

---

## Flujo TDD

### Fase 0: Analisis de Infraestructura

1. **Identificar tipo de trabajo:**
   - Nuevo repositorio
   - Modificacion de repositorio existente

2. **Extraer del contexto:**
   - Entidad y tabla a mapear
   - Metodos CRUD y custom del repositorio
   - Escenarios XML necesarios

3. **Verificar si necesita migracion:**
   - Nueva tabla -> Crear migracion
   - Nueva columna -> Crear migracion
   - Solo metodos nuevos (sin cambio de esquema) -> No necesita migracion

4. **Verificar escenarios existentes:**
   - Buscar en `tests/{proyecto}.infrastructure.tests/scenarios/`
   - Determinar si necesita nuevos escenarios para los tests

### Fase 1: RED - Escribir Tests que Fallan

**Ubicacion:** `tests/{proyecto}.infrastructure.tests/nhibernate/NH{Entity}RepositoryTests.cs`

```csharp
public class NH{Entity}RepositoryTests : NHRepositoryTestBase<NH{Entity}Repository, {Entity}, Guid>
{
    private {Entity}? _test{Entity};

    protected internal override NH{Entity}Repository BuildRepository()
        => new NH{Entity}Repository(_sessionFactory.OpenSession(), _serviceProvider);

    [SetUp]
    public void LocalSetUp()
    {
        _test{Entity} = fixture.Build<{Entity}>()
            .With(x => x.Property, "valid-value")
            .Without(x => x.RelatedEntities)
            .Create();
    }

    #region CreateAsync Tests
    [Test]
    public async Task CreateAsync_WhenDataIsValid_ShouldCreateEntity()
    {
        // Act
        await RepositoryUnderTest.CreateAsync(_test{Entity}!.Property);

        // Assert - Verificar en BD con NDbUnit
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        var rows = dataSet.Get{Entity}sRows($"property = '{_test{Entity}.Property}'");
        rows.Count().Should().Be(1);
    }
    #endregion

    #region GetAsync Tests
    [Test]
    public async Task GetAsync_WithExistingId_ShouldReturnEntity()
    {
        // Arrange
        LoadScenario("Create{Entity}s");
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        var row = dataSet.GetFirst{Entity}Row();
        var id = row!.Field<Guid>("id");

        // Act
        var result = await RepositoryUnderTest.GetAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }
    #endregion
}
```

**REGLA DE ORO:** NUNCA usar el repositorio en Arrange o Assert. Solo en Act.
- Arrange: Usar `LoadScenario()` y escenarios XML
- Assert: Usar `nDbUnitTest.GetDataSetFromDb()`

**Ejecutar tests -> DEBEN FALLAR**

### Fase 2: Crear Escenarios XML (si no existen)

**Ubicacion:** `tests/{proyecto}.infrastructure.tests/scenarios/Create{Entity}s.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Dependencias primero (FK) -->
  <parent_table>
    <id>770e8400-e29b-41d4-a716-446655440001</id>
    <name>Parent Name</name>
  </parent_table>

  <!-- Entidad principal -->
  <table_name>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <property>Value One</property>
    <parent_id>770e8400-e29b-41d4-a716-446655440001</parent_id>
  </table_name>
</AppSchema>
```

### Fase 3: GREEN - Implementar Minimo Necesario

**1. Crear Mapper:** `{proyecto}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs`

```csharp
public class {Entity}Mapper : ClassMapping<{Entity}>
{
    public {Entity}Mapper()
    {
        Table("table_name");

        Id(x => x.Id, m =>
        {
            m.Generator(Generators.Assigned);
            m.Column("id");
        });

        Property(x => x.PropertyName, m =>
        {
            m.Column("property_name");
            m.NotNullable(true);
            m.Length(100);
        });

        // Foreign Keys
        ManyToOne(x => x.RelatedEntity, m =>
        {
            m.Column("related_entity_id");
            m.NotNullable(false);
            m.Lazy(LazyRelation.Proxy);
        });
    }
}
```

**2. Crear Repository:** `{proyecto}.infrastructure/nhibernate/NH{Entity}Repository.cs`

```csharp
public class NH{Entity}Repository : NHRepository<{Entity}, Guid>, I{Entity}Repository
{
    public NH{Entity}Repository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider)
    {
    }

    public async Task<{Entity}> CreateAsync(string property)
    {
        var entity = new {Entity}(property);

        if (!entity.IsValid())
        {
            var errors = entity.Validate();
            throw new InvalidDomainException("Cannot create entity with invalid data", errors);
        }

        await this.Session.SaveAsync(entity);
        await this.Session.FlushAsync();

        return entity;
    }

    public async Task<{Entity}?> GetByPropertyAsync(string value)
    {
        return await this.Session.Query<{Entity}>()
            .Where(e => e.Property.ToLower() == value.ToLower())
            .FirstOrDefaultAsync();
    }
}
```

**3. Registrar Mapper:** En `NHSessionFactory.cs` agregar:
```csharp
mapper.AddMapping<{Entity}Mapper>();
```

**4. Actualizar NHUnitOfWork:** Agregar lazy property para el repositorio

**Ejecutar tests -> DEBEN PASAR**

### Fase 4: REFACTOR

Verificar:
- [ ] Repository hereda de NHRepository o NHReadOnlyRepository
- [ ] Constructor recibe ISession y IServiceProvider
- [ ] FlushAsync() despues de operaciones de escritura
- [ ] Mapper tiene tabla y columnas en snake_case
- [ ] Lazy loading configurado en relaciones
- [ ] Documentacion XML completa

**Ejecutar tests -> DEBEN SEGUIR PASANDO**

---

## Reporte de Salida

Al finalizar, muestra:

```markdown
## Infrastructure Layer Completado (TDD)

### Escenarios XML
- [x] tests/{proyecto}.infrastructure.tests/scenarios/Create{Entity}s.xml

### Archivos Creados
- [x] tests/{proyecto}.infrastructure.tests/nhibernate/NH{Entity}RepositoryTests.cs
- [x] {proyecto}.infrastructure/nhibernate/NH{Entity}Repository.cs
- [x] {proyecto}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs

### Archivos Modificados
- [x] NHSessionFactory.cs (registro de mapper)
- [x] NHUnitOfWork.cs (lazy property)

### Tests
- Total: {n}
- Pasando: {n}

**Status:** SUCCESS
```

---

## Patrones Importantes

### Tests de Repositorio - AAA con NDbUnit
```csharp
[Test]
public async Task UpdateAsync_WithValidData_ShouldUpdateEntity()
{
    // Arrange - ESCENARIO, no repositorio
    LoadScenario("Create{Entity}s");
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var id = dataSet.GetFirst{Entity}Row().Field<Guid>("id");

    // Act - SOLO AQUI usa repositorio
    await RepositoryUnderTest.UpdateAsync(id, "NewValue");

    // Assert - NDBUNIT, no repositorio
    var updatedDataSet = nDbUnitTest.GetDataSetFromDb();
    var row = updatedDataSet.Get{Entity}sRows($"id = '{id}'").First();
    row.Field<string>("property").Should().Be("NewValue");
}
```

### Convenciones de Escenarios XML
- Nombres de elementos = nombres de tablas (snake_case)
- Nombres de campos = nombres de columnas (snake_case)
- GUIDs consistentes: `550e8400-...-0001`, `550e8400-...-0002`
- Orden respeta dependencias (FK)

---

## Recordatorios

1. **TDD es obligatorio** - Tests primero, implementacion despues
2. **NUNCA usar repositorio en Arrange/Assert** - Solo en Act
3. **LoadScenario() para Arrange** - Cargar datos de prueba
4. **NDbUnit para Assert** - Verificar en BD directamente
5. **FlushAsync() siempre** - Despues de Save/Update/Delete
6. **Case-insensitive** - Queries con `.ToLower()`
