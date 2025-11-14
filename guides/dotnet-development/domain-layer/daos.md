# DAOs (Data Access Objects)

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenidos
- [Introducción](#introducción)
- [DAO vs Entity vs DTO](#dao-vs-entity-vs-dto)
- [Cuándo Usar DAOs](#cuándo-usar-daos)
- [Estructura de un DAO](#estructura-de-un-DAO)
- [Search Fields Pattern](#search-fields-pattern)
- [DAO Repository Interfaces](#dao-repository-interfaces)
- [Mapping: Entity → DAO](#mapping-entity--dao)
- [Ejemplos Reales del Proyecto](#ejemplos-reales-del-proyecto)
- [Patrones y Best Practices](#patrones-y-best-practices)
- [Checklist para Nuevos DAOs](#checklist-para-nuevos-daos)

---

## Introducción

Los **DAOs (Data Access Objects)** son objetos especializados para **queries de solo lectura** optimizadas. En APSYS, los DAOs se usan para:

- **Queries optimizadas**: Sin overhead de lazy loading ni proxies de NHibernate
- **Flattened data**: Sin navigation properties, solo datos planos
- **Search optimization**: Propiedades calculadas para búsqueda (SearchAll)
- **Read-only operations**: No tienen validaciones ni lógica de negocio
- **Performance**: Queries más rápidas al no cargar relaciones

### Arquitectura

```
╔═══════════════════════════════════════════════════════════════╗
║                        DOMAIN LAYER                           ║
║                                                               ║
║  ┌─────────────────────┐         ┌──────────────────────┐    ║
║  │  Entity (Write)     │         │  DAO (Read)          │    ║
║  ├─────────────────────┤         ├──────────────────────┤    ║
║  │ + Id                │         │ + Id                 │    ║
║  │ + CreationDate      │         │ + CreationDate       │    ║
║  │ + BusinessProps     │         │ + FlattenedProps     │    ║
║  │ + NavigationProps   │         │ + SearchAll          │    ║
║  │                     │         │                      │    ║
║  │ + IsValid()         │         │ (No methods)         │    ║
║  │ + GetValidator()    │         │                      │    ║
║  └─────────────────────┘         └──────────────────────┘    ║
║           │                                 │                 ║
║           │                                 │                 ║
║           ▼                                 ▼                 ║
║  ┌─────────────────────┐         ┌──────────────────────┐    ║
║  │ IRepository<T, Guid>│         │IReadOnlyRepository   │    ║
║  │ (CRUD)              │         │<T, Guid> (Queries)   │    ║
║  └─────────────────────┘         └──────────────────────┘    ║
╚═══════════════════════════════════════════════════════════════╝

        CREATE, UPDATE, DELETE              READ ONLY
```

---

## DAO vs Entity vs DTO

| Aspecto | Entity | DAO | DTO |
|---------|--------|-----|-----|
| **Ubicación** | Domain Layer | Domain Layer | Endpoints Layer |
| **Herencia** | `AbstractDomainObject` | Ninguna | Ninguna |
| **Propósito** | Lógica de dominio | Queries de lectura | Transferencia HTTP |
| **Validación** | Sí (`GetValidator()`) | No | No (validada en request) |
| **Navigation Props** | Sí (`IList<Role>`) | No | No |
| **Virtual Props** | Sí (NHibernate) | Sí (NHibernate) | No |
| **Métodos** | `IsValid()`, `Validate()` | Ninguno | Ninguno |
| **SearchAll** | No | Sí | No |
| **Repositorio** | `IRepository<T, Guid>` | `IReadOnlyRepository<T, Guid>` | N/A |
| **Uso** | CRUD operations | Read-only queries | API requests/responses |

**Flujo típico:**

```
POST /api/users (Create)
    Request DTO → Validator → Entity → Repository.Add() → Database

GET /api/users (Query)
    Database → DAO Repository → DAO → Response DTO
```

---

## Cuándo Usar DAOs

### ✅ Usa DAOs cuando:

1. **Queries de solo lectura complejas**
   ```csharp
   // Endpoint GET que lista entidades con filtros y paginación
   var result = await _unitOfWork.TechnicalStandardDaos.GetManyAndCountAsync(
       "CFE",  // Busca en SearchAll
       "Code", // Ordena por Code
       ct
   );
   ```

2. **Necesitas optimizar performance**
   ```csharp
   // DAO: Una query, sin lazy loading
   var daos = await _daoRepository.GetAsync(ct); // Rápido

   // Entity: Múltiples queries si hay navigation properties
   var entities = await _repository.GetAsync(ct); // Más lento si hay lazy loading
   ```

3. **Búsquedas de texto en múltiples campos**
   ```csharp
   // SearchAll permite buscar en Code, Name, Edition simultáneamente
   var result = await _daoRepo.GetManyAndCountAsync("CFE-001", "Code", ct);
   ```

4. **Listados para UI (grids, tablas, selects)**
   ```csharp
   // Listar normas técnicas para un dropdown
   var standards = await _unitOfWork.TechnicalStandardDaos.GetAsync(
       dao => dao.Status == "Active",
       ct
   );
   ```

### ❌ NO uses DAOs cuando:

1. **Necesitas crear, actualizar o eliminar**
   ```csharp
   // ❌ MAL: DAOs no tienen repositorios de escritura
   // ✅ BIEN: Usa Entity
   var standard = await _unitOfWork.TechnicalStandards.CreateAsync(...);
   ```

2. **Necesitas validaciones de negocio**
   ```csharp
   // ❌ MAL: DAO no tiene IsValid()
   // ✅ BIEN: Usa Entity con validator
   var user = new User(email, name);
   if (!user.IsValid()) { /* error */ }
   ```

3. **Necesitas navigation properties**
   ```csharp
   // ❌ MAL: DAO no tiene Roles navigation property
   // ✅ BIEN: Usa Entity
   var user = await _unitOfWork.Users.GetAsync(id, ct);
   var roles = user.Roles; // IList<Role>
   ```

4. **La query es simple y performance no es crítico**
   ```csharp
   // Para queries simples, Entity puede ser suficiente
   var user = await _unitOfWork.Users.GetByEmailAsync("john@example.com");
   ```

---

## Estructura de un DAO

### Patrón General

```csharp
namespace {proyecto}.domain.daos;

/// <summary>
/// Data access object for {Entity} entities, used for read-only database operations.
/// </summary>
public class {Entity}Dao
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    // Propiedades de negocio (sin navigation properties)
    public virtual string SomeProperty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search string for full-text search operations.
    /// Combines multiple fields for simplified searching.
    /// </summary>
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

### Características Clave

1. **No hereda de AbstractDomainObject**
   - No tiene métodos `IsValid()`, `Validate()`, `GetValidator()`
   - Solo tiene propiedades

2. **Propiedades virtuales**
   - Todas las propiedades son `virtual` para NHibernate proxies
   - NHibernate puede mapear y lazy load si es necesario

3. **Sin navigation properties**
   - No tiene `IList<Role>`, `IList<Prototype>`, etc.
   - Solo propiedades primitivas y strings

4. **SearchAll property**
   - Campo calculado que combina múltiples propiedades
   - Usado para búsquedas full-text en UI

5. **Namespace: `{proyecto}.domain.daos`**
   - Separado de `{proyecto}.domain.entities`

---

## Search Fields Pattern

El patrón **SearchAll** permite búsquedas eficientes en múltiples campos simultáneamente.

### ¿Qué es SearchAll?

`SearchAll` es una propiedad calculada que **concatena múltiples campos** para facilitar búsquedas.

**Ejemplo:**

```csharp
public class TechnicalStandardDao
{
    public virtual string Code { get; set; } = "CFE-001";
    public virtual string Name { get; set; } = "Conductores";
    public virtual string Edition { get; set; } = "2023";

    // SearchAll = "CFE-001 Conductores 2023"
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

### ¿Cómo se Calcula SearchAll?

SearchAll se calcula en la **capa de Infrastructure** (en el mapping de NHibernate o en el repositorio).

**En NHibernate Mapping (Infrastructure):**

```csharp
// Infrastructure/persistence/mappings/TechnicalStandardDaoMap.cs
public class TechnicalStandardDaoMap : ClassMap<TechnicalStandardDao>
{
    public TechnicalStandardDaoMap()
    {
        Table("TechnicalStandards");

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.CreationDate);
        Map(x => x.Code);
        Map(x => x.Name);
        Map(x => x.Edition);
        Map(x => x.Status);
        Map(x => x.Type);

        // Formula calculada en SQL
        Map(x => x.SearchAll).Formula("CONCAT(Code, ' ', Name, ' ', Edition)");
    }
}
```

**Alternativamente, en el Repository (Infrastructure):**

```csharp
// Infrastructure/persistence/repositories/TechnicalStandardDaoRepository.cs
public class TechnicalStandardDaoRepository : BaseReadOnlyRepository<TechnicalStandardDao, Guid>
{
    public override async Task<IEnumerable<TechnicalStandardDao>> GetAsync(CancellationToken ct = default)
    {
        var daos = await base.GetAsync(ct);

        // Calcular SearchAll en memoria
        foreach (var dao in daos)
        {
            dao.SearchAll = $"{dao.Code} {dao.Name} {dao.Edition}";
        }

        return daos;
    }
}
```

### Uso de SearchAll en Queries

```csharp
// En FastEndpoints
public override async Task HandleAsync(GetTechnicalStandardsRequest req, CancellationToken ct)
{
    // GetManyAndCountAsync busca en SearchAll automáticamente
    var result = await _unitOfWork.TechnicalStandardDaos.GetManyAndCountAsync(
        req.SearchTerm,  // "CFE" busca en "CFE-001 Conductores 2023"
        "Code",          // Ordenamiento por defecto
        ct
    );

    // result.Items contiene todos los TechnicalStandardDao que cumplen el filtro
}
```

**Beneficios:**
- **Una sola query** en lugar de múltiples `OR` clauses
- **Performance**: Índice en SearchAll puede mejorar velocidad
- **Simplicidad**: UI solo envía un string de búsqueda

---

## DAO Repository Interfaces

Los DAOs usan **`IReadOnlyRepository<T, Guid>`** porque solo necesitan operaciones de lectura.

### Patrón General

```csharp
using {proyecto}.domain.daos;

namespace {proyecto}.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for managing <see cref="{Entity}Dao"/> entities.
/// This interface extends the <see cref="IReadOnlyRepository{T, TKey}"/> to provide read-only operations.
/// </summary>
public interface I{Entity}DaoRepository : IReadOnlyRepository<{Entity}Dao, Guid>
{
    // Normalmente vacío - usa métodos heredados de IReadOnlyRepository
}
```

### Ejemplo Real: ITechnicalStandardDaoRepository

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for managing <see cref="TechnicalStandardDao"/> entities.
/// This interface extends the <see cref="IReadOnlyRepository{T, TKey}"/> to provide read-only operations.
/// </summary>
public interface ITechnicalStandardDaoRepository : IReadOnlyRepository<TechnicalStandardDao, Guid>
{
}
```

### Ejemplo Real: IPrototypeDaoRepository

```csharp
using hashira.stone.backend.domain.daos;

namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IPrototypeDaoRepository : IReadOnlyRepository<PrototypeDao, Guid>
{
}
```

### Métodos Heredados de IReadOnlyRepository

Los DAO repositories heredan estos métodos:

```csharp
// Get by ID
Task<TechnicalStandardDao> GetAsync(Guid id, CancellationToken ct = default);

// Get all
Task<IEnumerable<TechnicalStandardDao>> GetAsync(CancellationToken ct = default);

// Get with query
Task<IEnumerable<TechnicalStandardDao>> GetAsync(
    Expression<Func<TechnicalStandardDao, bool>> query,
    CancellationToken ct = default
);

// Count
Task<int> CountAsync(CancellationToken ct = default);
Task<int> CountAsync(
    Expression<Func<TechnicalStandardDao, bool>> query,
    CancellationToken ct = default
);

// Get many with pagination and count
Task<GetManyAndCountResult<TechnicalStandardDao>> GetManyAndCountAsync(
    string? query,
    string defaultSorting,
    CancellationToken ct = default
);
```

### Agregando al IUnitOfWork

```csharp
public interface IUnitOfWork : IDisposable
{
    #region crud Repositories
    // Repositorios de escritura (Entities)
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IPrototypeRepository Prototypes { get; }
    ITechnicalStandardRepository TechnicalStandards { get; }
    #endregion

    #region read-only Repositories
    // Repositorios de solo lectura (DAOs)
    ITechnicalStandardDaoRepository TechnicalStandardDaos { get; }
    IPrototypeDaoRepository PrototypeDaos { get; }
    #endregion

    #region transactions management
    void Commit();
    void Rollback();
    void BeginTransaction();
    #endregion
}
```

---

## Mapping: Entity → DAO

En NHibernate, el **DAO puede mapear a la misma tabla** que la Entity, pero con una configuración diferente.

### Entity Mapping (con navigation properties)

```csharp
// Infrastructure/persistence/mappings/TechnicalStandardMap.cs
public class TechnicalStandardMap : ClassMap<TechnicalStandard>
{
    public TechnicalStandardMap()
    {
        Table("TechnicalStandards");

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.CreationDate);
        Map(x => x.Code);
        Map(x => x.Name);
        Map(x => x.Edition);
        Map(x => x.Status);
        Map(x => x.Type);

        // Navigation properties (lazy loading)
        HasMany(x => x.Inspections)
            .KeyColumn("TechnicalStandardId")
            .Cascade.None()
            .LazyLoad();
    }
}
```

### DAO Mapping (sin navigation properties, con SearchAll)

```csharp
// Infrastructure/persistence/mappings/TechnicalStandardDaoMap.cs
public class TechnicalStandardDaoMap : ClassMap<TechnicalStandardDao>
{
    public TechnicalStandardDaoMap()
    {
        Table("TechnicalStandards"); // Misma tabla

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.CreationDate);
        Map(x => x.Code);
        Map(x => x.Name);
        Map(x => x.Edition);
        Map(x => x.Status);
        Map(x => x.Type);

        // SearchAll calculado en SQL
        Map(x => x.SearchAll).Formula("CONCAT(Code, ' ', Name, ' ', Edition)");

        // NO navigation properties - más rápido
    }
}
```

**Ventajas:**
- Misma tabla, diferentes configuraciones
- DAO: Sin lazy loading → queries más rápidas
- Entity: Con lazy loading → flexibilidad en CRUD

---

## Ejemplos Reales del Proyecto

### Ejemplo 1: TechnicalStandardDao

```csharp
namespace hashira.stone.backend.domain.entities;

/// <summary>
/// Represents a technical standard in the system.
/// A technical standard defines a set of criteria, guidelines, or characteristics for processes, products, or services.
/// </summary>
public class TechnicalStandardDao
{
    /// <summary>
    /// Gets or sets the unique identifier for the technical standard.
    /// This is the primary key for the entity.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the technical standard was created.
    /// This property is set when the entity is instantiated.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the unique code of the technical standard.
    /// This code is required and must be unique within the system.
    /// </summary>
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the technical standard.
    /// This is a descriptive name and is required.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edition or version of the technical standard.
    /// This property is required and typically indicates the publication or revision version.
    /// </summary>
    public virtual string Edition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the technical standard.
    /// Typical values are "Active" or "Deprecated".
    /// This property is required.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the technical standard.
    /// Typical values are "CFE" or "Externa".
    /// This property is required.
    /// </summary>
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a search string that combines multiple code, name and edition for simplified searching.
    /// </summary>
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

**Características:**
- No hereda de `AbstractDomainObject`
- Todas las propiedades son `virtual`
- Tiene `SearchAll` para búsqueda combinada
- No tiene métodos (`IsValid()`, `GetValidator()`, etc.)
- No tiene navigation properties

### Ejemplo 2: PrototypeDao

```csharp
namespace hashira.stone.backend.domain.daos;

/// <summary>
/// Data access object for prototype entities, used for database operations.
/// </summary>
public class PrototypeDao
{
    /// <summary>
    /// Gets or sets the unique identifier for the prototype.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the prototype.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the number associated with the prototype.
    /// </summary>
    public virtual string Number { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the prototype was issued.
    /// </summary>
    public virtual DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the prototype.
    /// </summary>
    public virtual DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the prototype.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search string for full-text search operations.
    /// </summary>
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

**Características:**
- Namespace `{proyecto}.domain.daos`
- Propiedades de negocio flattened (IssueDate, ExpirationDate, etc.)
- `SearchAll` para búsqueda en Number, Status
- No tiene métodos ni validaciones

### Ejemplo 3: Uso en Endpoint (GET con paginación)

```csharp
public class GetTechnicalStandardsEndpoint : Endpoint<GetTechnicalStandardsRequest, GetManyAndCountResult<TechnicalStandardDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTechnicalStandardsEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Get("/api/technical-standards");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetTechnicalStandardsRequest req, CancellationToken ct)
    {
        // Usar DAO repository para queries optimizadas
        var result = await _unitOfWork.TechnicalStandardDaos.GetManyAndCountAsync(
            req.SearchTerm,  // Busca en SearchAll (Code + Name + Edition)
            "Code",          // Ordenamiento por defecto
            ct
        );

        // Mapear DAO → DTO para respuesta
        var dtoResult = new GetManyAndCountResult<TechnicalStandardDto>
        {
            Items = result.Items.Select(dao => new TechnicalStandardDto
            {
                Id = dao.Id,
                Code = dao.Code,
                Name = dao.Name,
                Edition = dao.Edition,
                Status = dao.Status,
                Type = dao.Type
            }),
            Count = result.Count,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            Sorting = result.Sorting
        };

        await SendOkAsync(dtoResult, ct);
    }
}
```

### Ejemplo 4: Uso en Endpoint (GET con filtro)

```csharp
public class GetActivePrototypesEndpoint : Endpoint<EmptyRequest, List<PrototypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActivePrototypesEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Get("/api/prototypes/active");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        // Query con Expression<Func<T, bool>>
        var activePrototypes = await _unitOfWork.PrototypeDaos.GetAsync(
            dao => dao.Status == "Active" && dao.ExpirationDate > DateTime.UtcNow,
            ct
        );

        // Mapear DAO → DTO
        var dtos = activePrototypes.Select(dao => new PrototypeDto
        {
            Id = dao.Id,
            Number = dao.Number,
            IssueDate = dao.IssueDate,
            ExpirationDate = dao.ExpirationDate,
            Status = dao.Status
        }).ToList();

        await SendOkAsync(dtos, ct);
    }
}
```

---

## Patrones y Best Practices

### ✅ DO: Usar DAOs para Queries de Solo Lectura

```csharp
// ✅ BIEN: DAO para listados y búsquedas
var standards = await _unitOfWork.TechnicalStandardDaos.GetManyAndCountAsync(
    "CFE",
    "Code",
    ct
);

// Performance: Sin lazy loading, una query rápida
```

### ❌ DON'T: Intentar Modificar DAOs

```csharp
// ❌ MAL: DAO no tiene métodos de escritura
var dao = await _unitOfWork.TechnicalStandardDaos.GetAsync(id, ct);
dao.Name = "New Name";
await _unitOfWork.TechnicalStandardDaos.SaveAsync(dao); // ¡NO EXISTE!

// ✅ BIEN: Usa Entity para modificaciones
var standard = await _unitOfWork.TechnicalStandards.GetAsync(id, ct);
standard.Name = "New Name";
await _unitOfWork.TechnicalStandards.SaveAsync(standard);
_unitOfWork.Commit();
```

### ✅ DO: Incluir SearchAll en DAOs

```csharp
// ✅ BIEN: SearchAll para búsquedas combinadas
public class TechnicalStandardDao
{
    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual string Edition { get; set; } = string.Empty;

    // Combina Code + Name + Edition
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

### ❌ DON'T: Agregar Navigation Properties a DAOs

```csharp
// ❌ MAL: DAO con navigation properties
public class TechnicalStandardDao
{
    public virtual Guid Id { get; set; }
    public virtual string Code { get; set; } = string.Empty;

    public virtual IList<InspectionDao> Inspections { get; set; } // ¡NO!
}

// ✅ BIEN: DAO sin navigation properties
public class TechnicalStandardDao
{
    public virtual Guid Id { get; set; }
    public virtual string Code { get; set; } = string.Empty;
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

### ✅ DO: Usar Virtual Properties

```csharp
// ✅ BIEN: Todas las propiedades virtuales para NHibernate
public class PrototypeDao
{
    public virtual Guid Id { get; set; }
    public virtual DateTime CreationDate { get; set; }
    public virtual string Number { get; set; } = string.Empty;
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

### ❌ DON'T: Agregar Métodos a DAOs

```csharp
// ❌ MAL: DAO con métodos
public class TechnicalStandardDao
{
    public virtual string Code { get; set; } = string.Empty;

    public bool IsActive() // ¡NO! DAOs solo tienen propiedades
    {
        return Status == "Active";
    }
}

// ✅ BIEN: Lógica en endpoint o service
var activeStandards = await _unitOfWork.TechnicalStandardDaos.GetAsync(
    dao => dao.Status == "Active",
    ct
);
```

### ✅ DO: Heredar de IReadOnlyRepository en DAO Repositories

```csharp
// ✅ BIEN: Solo lectura
public interface ITechnicalStandardDaoRepository : IReadOnlyRepository<TechnicalStandardDao, Guid>
{
}

// ❌ MAL: DAO con repositorio de escritura
public interface ITechnicalStandardDaoRepository : IRepository<TechnicalStandardDao, Guid>
{
    // ¡NO! DAOs no deben tener Add, Save, Delete
}
```

### ✅ DO: Mapear DAO → DTO en Endpoints

```csharp
// ✅ BIEN: DAO → DTO en endpoint
var daos = await _unitOfWork.TechnicalStandardDaos.GetAsync(ct);
var dtos = daos.Select(dao => new TechnicalStandardDto
{
    Id = dao.Id,
    Code = dao.Code,
    Name = dao.Name
}).ToList();

await SendOkAsync(dtos, ct);
```

### ❌ DON'T: Devolver DAOs Directamente en API

```csharp
// ❌ MAL: Exponer DAO en API
var daos = await _unitOfWork.TechnicalStandardDaos.GetAsync(ct);
await SendOkAsync(daos, ct); // ¡NO! Puede exponer detalles internos

// ✅ BIEN: Mapear DAO → DTO primero
var dtos = daos.Select(dao => new TechnicalStandardDto { ... }).ToList();
await SendOkAsync(dtos, ct);
```

### ✅ DO: Usar DAOs para Dropdowns y Listas en UI

```csharp
// ✅ BIEN: DAO optimizado para dropdown
var standards = await _unitOfWork.TechnicalStandardDaos.GetAsync(
    dao => dao.Status == "Active",
    ct
);

var options = standards.Select(dao => new SelectOption
{
    Value = dao.Id.ToString(),
    Label = $"{dao.Code} - {dao.Name}"
}).ToList();

await SendOkAsync(options, ct);
```

### ✅ DO: Usar GetManyAndCountAsync para Paginación

```csharp
// ✅ BIEN: Paginación eficiente con DAO
var result = await _unitOfWork.TechnicalStandardDaos.GetManyAndCountAsync(
    req.SearchTerm,
    "Code",
    ct
);

// result.Items: Datos de la página
// result.Count: Total de registros para paginación
```

### ❌ DON'T: Duplicar Lógica de Negocio en DAOs

```csharp
// ❌ MAL: Validación en DAO
public class PrototypeDao
{
    public virtual DateTime ExpirationDate { get; set; }

    public bool IsExpired() // ¡NO!
    {
        return ExpirationDate < DateTime.UtcNow;
    }
}

// ✅ BIEN: Lógica en Entity o Service
public class Prototype : AbstractDomainObject
{
    public virtual DateTime ExpirationDate { get; set; }

    public bool IsExpired()
    {
        return ExpirationDate < DateTime.UtcNow;
    }
}
```

---

## Checklist para Nuevos DAOs

Cuando crees un nuevo DAO en APSYS, sigue esta checklist:

### 1. Crear la Clase DAO

- [ ] Crear clase en `Domain/daos/{Entity}Dao.cs`
- [ ] Usar namespace `{proyecto}.domain.daos`
- [ ] **NO** heredar de `AbstractDomainObject`
- [ ] Documentar con XML comments

### 2. Definir Propiedades

- [ ] Agregar `public virtual Guid Id { get; set; }`
- [ ] Agregar `public virtual DateTime CreationDate { get; set; }`
- [ ] Agregar propiedades de negocio como `virtual`
- [ ] **NO** agregar navigation properties (`IList<>`, etc.)
- [ ] Agregar `public virtual string SearchAll { get; set; } = string.Empty;`

### 3. Documentar Propiedades

- [ ] `<summary>` para cada propiedad
- [ ] Describir propósito de `SearchAll`

### 4. Crear Interface del DAO Repository

- [ ] Crear interfaz en `Domain/interfaces/repositories/I{Entity}DaoRepository.cs`
- [ ] Heredar de `IReadOnlyRepository<{Entity}Dao, Guid>`
- [ ] Documentar con XML comments
- [ ] Normalmente vacía (usa métodos heredados)

### 5. Agregar al IUnitOfWork

- [ ] Agregar propiedad en `IUnitOfWork.cs`: `I{Entity}DaoRepository {Entity}Daos { get; }`
- [ ] Colocar en región `#region read-only Repositories`
- [ ] Documentar con `<summary>`

### 6. Implementar en Infrastructure

- [ ] Crear mapping: `{Entity}DaoMap.cs` en Infrastructure
- [ ] Mapear a la misma tabla que Entity
- [ ] Configurar `SearchAll` con `.Formula()` o calcularlo en repositorio
- [ ] **NO** mapear navigation properties
- [ ] Crear repositorio: `{Entity}DaoRepository : BaseReadOnlyRepository<{Entity}Dao, Guid>`
- [ ] Implementar en `UnitOfWork.cs` la propiedad del DAO repository

### 7. Configurar SearchAll

- [ ] Decidir qué campos incluir en SearchAll (códigos, nombres, descripciones)
- [ ] Implementar cálculo en mapping con `.Formula()` o en repositorio
- [ ] Validar que funciona con `GetManyAndCountAsync()`

### 8. Validar Integración

- [ ] El DAO repository se inyecta correctamente vía DI
- [ ] `GetManyAndCountAsync()` funciona con SearchAll
- [ ] Queries filtradas retornan datos correctos
- [ ] Performance es mejor que usar Entity para listados
- [ ] Los tests de integración pasan

### Ejemplo Completo: PrototypeDao

```csharp
// 1. Archivo: Domain/daos/PrototypeDao.cs
namespace {proyecto}.domain.daos;

// 2. No hereda de AbstractDomainObject
/// <summary>
/// Data access object for prototype entities, used for read-only database operations.
/// </summary>
public class PrototypeDao
{
    // 3. Propiedades virtuales sin navigation properties
    /// <summary>
    /// Gets or sets the unique identifier for the prototype.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the prototype.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the number associated with the prototype.
    /// </summary>
    public virtual string Number { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the prototype was issued.
    /// </summary>
    public virtual DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the prototype.
    /// </summary>
    public virtual DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the prototype.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search string for full-text search operations.
    /// Combines Number and Status for simplified searching.
    /// </summary>
    public virtual string SearchAll { get; set; } = string.Empty;
}

// 4. Archivo: Domain/interfaces/repositories/IPrototypeDaoRepository.cs
namespace {proyecto}.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for managing <see cref="PrototypeDao"/> entities.
/// </summary>
public interface IPrototypeDaoRepository : IReadOnlyRepository<PrototypeDao, Guid>
{
    // Normalmente vacío - usa métodos heredados
}

// 5. Agregar a IUnitOfWork
public interface IUnitOfWork : IDisposable
{
    #region read-only Repositories

    /// <summary>
    /// Read-only repository for managing prototype DAOs
    /// </summary>
    IPrototypeDaoRepository PrototypeDaos { get; }

    #endregion
}
```

---

## Recursos Adicionales

- **NHibernate Formula Mapping**: https://nhibernate.info/doc/nhibernate-reference/mapping.html#mapping-declaration-property
- **Repository Pattern**: Martin Fowler - Patterns of Enterprise Application Architecture
- **DAO Pattern**: https://www.oracle.com/java/technologies/dataaccessobject.html

---

**Guía creada para APSYS Backend Development**
Basada en el proyecto: `hashira.stone.backend`
Stack: .NET 9.0, C# 13, NHibernate 5.5, FastEndpoints 7.0
