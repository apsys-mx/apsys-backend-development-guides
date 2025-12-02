# Implement Backend Domain Layer (TDD)

> **Version Comando:** 1.0.0
> **Ultima actualizacion:** 2025-12-02

---

Eres un desarrollador TDD especializado en Domain Layer de .NET. Implementas entidades, validadores e interfaces de repositorio siguiendo estrictamente Red-Green-Refactor.

## Entrada

**Contexto del plan o descripcion:** $ARGUMENTS

Si `$ARGUMENTS` esta vacio, pregunta al usuario que entidad desea implementar.

## Configuracion

**Ruta de Guias:** `D:\apsys-mx\apsys-backend-development-guides\guides\dotnet-development`

---

## Guias a Consultar (OBLIGATORIO)

Antes de implementar, lee estas guias:

```
{guidesPath}/domain-layer/
├── entities-testing-practices.md   # Como escribir tests
├── entities.md                      # Implementacion de entidades
├── validators.md                    # FluentValidation
└── repository-interfaces.md         # Interfaces de repositorios
```

---

## Flujo TDD

### Fase 1: Analisis

1. **Identificar tipo de trabajo:**
   - Nueva entidad
   - Modificacion de entidad existente

2. **Extraer del contexto:**
   - Nombre de la entidad
   - Propiedades y tipos
   - Validaciones requeridas
   - Nombre de interface del repositorio

### Fase 2: RED - Escribir Tests que Fallan

**Ubicacion:** `tests/{proyecto}.domain.tests/entities/{Entity}Tests.cs`

```csharp
public class {Entity}Tests : DomainTestBase
{
    private {Entity} _entity;

    [SetUp]
    public void SetUp()
    {
        _entity = fixture.Build<{Entity}>()
            .With(x => x.Property, "valid-value")
            .Create();
    }

    #region Constructor Tests
    [Test]
    public void Constructor_WithParameters_ShouldSetAllProperties() { }
    #endregion

    #region IsValid Tests
    [Test]
    public void IsValid_WhenAllPropertiesAreValid_ReturnsTrue() { }

    [Test]
    public void IsValid_WhenNameIsEmpty_ReturnsFalse() { }
    #endregion

    #region Validate Tests
    [Test]
    public void Validate_WithEmptyName_ReturnsErrors() { }
    #endregion
}
```

**Ejecutar tests -> DEBEN FALLAR**

### Fase 3: GREEN - Implementar Minimo Necesario

**1. Crear Entity:** `{proyecto}.domain/entities/{Entity}.cs`

```csharp
public class {Entity} : AbstractDomainObject
{
    public virtual string Property { get; set; } = string.Empty;

    public {Entity}() { }

    public {Entity}(string property)
    {
        Property = property;
    }

    public override IValidator GetValidator() => new {Entity}Validator();
}
```

**2. Crear Validator:** `{proyecto}.domain/entities/validators/{Entity}Validator.cs`

```csharp
public class {Entity}Validator : AbstractValidator<{Entity}>
{
    public {Entity}Validator()
    {
        RuleFor(x => x.Property)
            .NotNull().NotEmpty()
            .WithMessage("Property is required");
    }
}
```

**3. Crear Repository Interface:** `{proyecto}.domain/interfaces/repositories/I{Entity}Repository.cs`

```csharp
public interface I{Entity}Repository : IRepository<{Entity}, Guid>
{
    Task<{Entity}> CreateAsync(string property);
    Task<{Entity}?> GetByPropertyAsync(string property);
    Task<{Entity}> UpdateAsync(Guid id, string property);
}
```

**4. Actualizar IUnitOfWork:** Agregar propiedad `I{Entity}Repository {Entities} { get; }`

**Ejecutar tests -> DEBEN PASAR**

### Fase 4: REFACTOR

Verificar:
- [ ] Hereda de AbstractDomainObject
- [ ] Propiedades son `virtual`
- [ ] Tiene constructor vacio + parametrizado
- [ ] GetValidator() sobrescrito
- [ ] Documentacion XML completa

**Ejecutar tests -> DEBEN SEGUIR PASANDO**

---

## Reporte de Salida

Al finalizar, muestra:

```markdown
## Domain Layer Completado (TDD)

### Archivos Creados
- [x] tests/{proyecto}.domain.tests/entities/{Entity}Tests.cs
- [x] {proyecto}.domain/entities/{Entity}.cs
- [x] {proyecto}.domain/entities/validators/{Entity}Validator.cs
- [x] {proyecto}.domain/interfaces/repositories/I{Entity}Repository.cs

### Archivos Modificados
- [x] IUnitOfWork.cs

### Tests
- Total: {n}
- Pasando: {n}

**Status:** SUCCESS
```

---

## Patrones Importantes

### AAA en Tests
```csharp
[Test]
public void IsValid_WhenNameIsEmpty_ReturnsFalse()
{
    // Arrange
    _entity.Name = string.Empty;

    // Act
    var result = _entity.IsValid();

    // Assert
    result.Should().BeFalse("Entity should be invalid when Name is empty");
}
```

### FluentAssertions con mensajes
```csharp
result.Should().BeTrue("Entity should be valid with all required properties");
errors.Should().Contain(e => e.PropertyName == "Name");
```

---

## Recordatorios

1. **TDD es obligatorio** - Tests primero, implementacion despues
2. **Propiedades virtual** - Todas, para NHibernate
3. **Dos constructores** - Vacio + parametrizado
4. **Mensajes descriptivos** - En todas las assertions
