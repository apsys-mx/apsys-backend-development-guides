# Read-Only Feature Pattern: Complete Guide

**Version:** 1.0.0
**Last Updated:** 2025-11-15
**Status:** ✅ Complete

## Table of Contents

1. [What is a Read-Only Feature?](#what-is-a-read-only-feature)
2. [When to Use This Pattern](#when-to-use-this-pattern)
3. [Anatomy of a Read-Only Feature](#anatomy-of-a-read-only-feature)
4. [Example: TechnicalStandards (Read Operations)](#example-technicalstandards-read-operations)
5. [Components by Layer](#components-by-layer)
6. [Data Flow Diagrams](#data-flow-diagrams)
7. [Code Examples](#code-examples)
8. [Best Practices](#best-practices)
9. [Implementation Checklist](#implementation-checklist)
10. [Related Guides](#related-guides)

---

## What is a Read-Only Feature?

A **Read-Only Feature** is a feature that provides **only query/read operations** without any modification capabilities. In Clean Architecture, this pattern is implemented using:

- **DAOs (Data Access Objects)** instead of full entities
- **IReadOnlyRepository<T>** instead of IRepository<T>
- **NHReadOnlyRepository<T>** implementation
- **Read-only database views** instead of tables
- **Only GET endpoints** (no POST/PUT/DELETE)

### Key Characteristics

| Aspect | Read-Only Feature | CRUD Feature |
|--------|------------------|--------------|
| **Operations** | GET, GetManyAndCount only | Create, Read, Update, Delete |
| **Domain Model** | DAO (simple POCO) | Entity with business logic |
| **Validation** | None (read-only) | FluentValidation validators |
| **Repository** | IReadOnlyRepository<T> | IRepository<T> |
| **Database** | View (optimized for reads) | Table |
| **NHibernate Mapping** | Mutable(false) | Mutable(true) |
| **Use Cases** | Get, GetManyAndCount | Create, Get, Update, Delete |
| **Endpoints** | GET only | POST, GET, PUT, DELETE |
| **Transactions** | Read-only transactions | Read-write transactions |

---

## When to Use This Pattern

### Use Read-Only Features When:

✅ **Lookup/Catalog Tables**
- Status lists, countries, categories
- Reference data that changes infrequently
- Data managed outside your application

✅ **Reporting/Analytics**
- Dashboard data
- Statistical summaries
- Aggregated views

✅ **Complex Queries**
- Multi-table joins
- Computed/calculated fields
- Denormalized data for performance

✅ **Third-Party Data**
- Data from external systems
- Synchronized data (read-only sync)
- Integration views

### Don't Use Read-Only When:

❌ **Users need to create/modify** data through your application
❌ **Real-time updates** are required (use CRUD with optimistic locking)
❌ **Simple entity** without complex queries (use standard CRUD)

---

## Anatomy of a Read-Only Feature

Based on the TechnicalStandards feature from the reference project, here's the complete structure:

```
read-only-feature/
├── Domain Layer (3 files)
│   ├── daos/
│   │   └── TechnicalStandardDao.cs              # DAO - simple POCO
│   └── interfaces/
│       └── repositories/
│           ├── IReadOnlyRepository.cs           # Base read-only interface
│           └── ITechnicalStandardDaoRepository.cs (optional - if custom methods needed)
│
├── Infrastructure Layer (3 files)
│   ├── nhibernate/
│   │   ├── NHReadOnlyRepository.cs              # Base read-only implementation
│   │   ├── NHTechnicalStandardDaoRepository.cs  # Optional - only if custom methods
│   │   └── mappers/
│   │       └── TechnicalStandardDaoMapper.cs    # Maps to database view
│   └── migrations/
│       └── M026TechnicalStandardsView.cs        # Creates database view
│
├── Application Layer (2 files)
│   └── usecases/
│       └── technicalstandards/
│           ├── GetTechnicalStandardUseCase.cs   # Get single record
│           └── GetManyAndCountTechnicalStandardsUseCase.cs  # Get list with pagination
│
└── WebApi Layer (5 files)
    ├── features/
    │   └── technicalstandards/
    │       ├── endpoint/
    │       │   ├── GetTechnicalStandardEndpoint.cs
    │       │   └── GetManyAndCountTechnicalStandardsEndpoint.cs
    │       └── models/
    │           ├── GetTechnicalStandardModel.cs
    │           └── GetManyAndCountModel.cs
    └── dtos/
        └── TechnicalStandardDto.cs

Total: 13 files
```

---

## Example: TechnicalStandards (Read Operations)

### Business Context

**TechnicalStandards** represents standardization codes (e.g., ISO, IEC, CFE standards) used for validation and reference. This data is:
- **Read-only** in the application (managed by administrators via database scripts)
- **Frequently queried** for validation
- **Rarely updated** (stable reference data)

### Data Model

```csharp
public class TechnicalStandardDao
{
    public virtual Guid Id { get; set; }
    public virtual DateTime CreationDate { get; set; }
    public virtual string Code { get; set; } = string.Empty;      // e.g., "CFE-G0100-04"
    public virtual string Name { get; set; } = string.Empty;      // e.g., "Diseño de Estructuras"
    public virtual string Edition { get; set; } = string.Empty;   // e.g., "2015"
    public virtual string Status { get; set; } = string.Empty;    // "Active" | "Deprecated"
    public virtual string Type { get; set; } = string.Empty;      // "CFE" | "Externa"
    public virtual string SearchAll { get; set; } = string.Empty; // For full-text search
}
```

### Supported Operations

| Operation | HTTP Method | Route | Description |
|-----------|-------------|-------|-------------|
| Get Single | GET | /technical-standards/{id} | Retrieve one standard by ID |
| Get Many | GET | /technical-standards | List with filtering, sorting, pagination |

**Note:** NO Create, Update, or Delete endpoints exist for this feature.

---

## Components by Layer

### 1. Domain Layer

#### 1.1 TechnicalStandardDao (DAO)

**Location:** `src/YourProject.Domain/daos/TechnicalStandardDao.cs`

**Purpose:** Simple data container for read operations.

```csharp
namespace YourProject.Domain.DAOs;

/// <summary>
/// Data Access Object for TechnicalStandard.
/// Used for read-only queries with optimized fields.
/// </summary>
public class TechnicalStandardDao
{
    /// <summary>
    /// Unique identifier for the technical standard.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Date when the technical standard was created.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Unique code of the technical standard (e.g., "CFE-G0100-04").
    /// </summary>
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// Name/description of the technical standard.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Edition or version of the technical standard.
    /// </summary>
    public virtual string Edition { get; set; } = string.Empty;

    /// <summary>
    /// Current status: "Active" or "Deprecated".
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Type of standard: "CFE" or "Externa".
    /// </summary>
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>
    /// Concatenated search field for full-text search.
    /// </summary>
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

**Key Points:**
- No validation logic (read-only)
- No business methods (pure data)
- All properties `virtual` for NHibernate
- Includes `SearchAll` for efficient searching

#### 1.2 IReadOnlyRepository<T, TKey> (Base Interface)

**Location:** `src/YourProject.Domain/interfaces/repositories/IReadOnlyRepository.cs`

**Purpose:** Defines contract for read-only operations.

```csharp
namespace YourProject.Domain.Interfaces.Repositories;

/// <summary>
/// Defines a read-only repository for retrieving entities without modification.
/// </summary>
public interface IReadOnlyRepository<T, TKey> where T : class, new()
{
    // Synchronous Methods
    T Get(TKey id);
    IEnumerable<T> Get();
    IEnumerable<T> Get(Expression<Func<T, bool>> query);
    IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria);
    int Count();
    int Count(Expression<Func<T, bool>> query);
    GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting);

    // Asynchronous Methods
    Task<T> GetAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAsync(CancellationToken ct = default);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken ct = default);
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken ct = default);
}
```

**Methods Available:**
- ✅ `Get()` - Retrieve single or multiple entities
- ✅ `Count()` - Count entities
- ✅ `GetManyAndCount()` - Paginated list with total count
- ❌ `Add()` - NOT available
- ❌ `Update()` - NOT available
- ❌ `Delete()` - NOT available

---

### 2. Infrastructure Layer

#### 2.1 TechnicalStandardDaoMapper (NHibernate Mapping)

**Location:** `src/YourProject.Infrastructure/nhibernate/mappers/TechnicalStandardDaoMapper.cs`

**Purpose:** Maps DAO to database view.

```csharp
using YourProject.Domain.DAOs;
using YourProject.Domain.Resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace YourProject.Infrastructure.NHibernate.Mappers;

public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);

        // IMPORTANT: Mutable(false) prevents updates
        Mutable(false);

        // Maps to a VIEW, not a table
        Table("technical_standards_view");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.CreationDate, map =>
        {
            map.Column("creation_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Code, map =>
        {
            map.Column("code");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Edition, map =>
        {
            map.Column("edition");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Status, map =>
        {
            map.Column("status");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Type, map =>
        {
            map.Column("type");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.SearchAll, map =>
        {
            map.Column("search_all");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

**Key Configuration:**
- `Mutable(false)` - Prevents NHibernate from attempting updates
- `Table("technical_standards_view")` - Maps to database view
- No `Insert()` or `Update()` method mappings

#### 2.2 NHReadOnlyRepository<T, TKey> (Base Implementation)

**Location:** `src/YourProject.Infrastructure/nhibernate/NHReadOnlyRepository.cs`

**Purpose:** Base class for all read-only repositories.

```csharp
using System.Linq.Expressions;
using YourProject.Domain.Interfaces.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace YourProject.Infrastructure.NHibernate;

/// <summary>
/// NHibernate implementation of read-only repository.
/// Provides only query operations without modification capabilities.
/// </summary>
public class NHReadOnlyRepository<T, TKey>(ISession session) : IReadOnlyRepository<T, TKey>
    where T : class, new()
{
    protected internal readonly ISession _session = session;

    public T Get(TKey id)
        => this._session.Get<T>(id);

    public IEnumerable<T> Get()
        => this._session.Query<T>();

    public IEnumerable<T> Get(Expression<Func<T, bool>> query)
        => this._session.Query<T>().Where(query);

    public IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria)
        => this._session.Query<T>()
                .Where(query)
                .OrderBy(sortingCriteria.ToExpression())
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

    public int Count()
        => this._session.QueryOver<T>().RowCount();

    public int Count(Expression<Func<T, bool>> query)
        => this._session.Query<T>().Where(query).Count();

    public async Task<T> GetAsync(TKey id, CancellationToken ct = default)
        => await this._session.GetAsync<T>(id, ct);

    public async Task<IEnumerable<T>> GetAsync(CancellationToken ct = default)
        => await this._session.Query<T>().ToListAsync(ct);

    public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken ct = default)
        => await this._session.Query<T>()
                .Where(query)
                .ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await this._session.Query<T>().CountAsync(ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken ct = default)
        => await this._session.Query<T>().Where(query).CountAsync(ct);

    public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(
        string? query,
        string defaultSorting,
        CancellationToken ct = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute queries
        var total = await this._session.Query<T>()
            .Where(expression)
            .CountAsync(ct);

        var items = await this._session.Query<T>()
            .OrderBy(sortingCriteria.ToExpression())
            .Where(expression)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    // Helper method for query parsing
    private static (Expression<Func<T, bool>> expression, int pageNumber, int pageSize, SortingCriteria sortingCriteria)
        PrepareQuery(string? query, string defaultSorting)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
        QueryStringParser queryStringParser = new(queryString);

        int pageNumber = queryStringParser.ParsePageNumber();
        int pageSize = queryStringParser.ParsePageSize();

        Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
        SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
            ? SortingCriteriaType.Descending
            : SortingCriteriaType.Ascending;
        SortingCriteria sortingCriteria = new(sorting.By, directions);

        IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
        QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
        var expression = FilterExpressionParser.ParsePredicate<T>(filters);
        if (quickSearch != null)
            expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

        return (expression, pageNumber, pageSize, sortingCriteria);
    }
}
```

**Methods Implemented:**
- All query methods from `IReadOnlyRepository<T>`
- Query string parsing for filtering/sorting/pagination
- No modification methods (Add, Update, Delete)

#### 2.3 Database Migration (Create View)

**Location:** `src/YourProject.Migrations/M026TechnicalStandardsView.cs`

**Purpose:** Creates database view for optimized reads.

```csharp
using FluentMigrator;

namespace YourProject.Migrations;

[Migration(202511150001)]
public class M026TechnicalStandardsView : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            CREATE VIEW app.technical_standards_view AS
            SELECT
                ts.id,
                ts.creation_date,
                ts.code,
                ts.name,
                ts.edition,
                ts.status,
                ts.type,
                CONCAT(ts.code, ' ', ts.name, ' ', ts.edition) AS search_all
            FROM app.technical_standards ts
            WHERE ts.status = 'Active'
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP VIEW IF EXISTS app.technical_standards_view");
    }
}
```

**View Benefits:**
- ✅ Pre-computed `search_all` field
- ✅ Filtered (only Active standards)
- ✅ Optimized for read performance
- ✅ Can join multiple tables if needed

---

### 3. Application Layer

#### 3.1 GetTechnicalStandardUseCase (Get Single)

**Location:** `src/YourProject.Application/usecases/technicalstandards/GetTechnicalStandardUseCase.cs`

**Purpose:** Retrieve a single technical standard by ID.

```csharp
using FastEndpoints;
using FluentResults;
using YourProject.Domain.Entities;
using YourProject.Domain.Errors;
using YourProject.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace YourProject.Application.UseCases.TechnicalStandards;

/// <summary>
/// Use case for retrieving a single technical standard by ID.
/// </summary>
public class GetTechnicalStandardUseCase
{
    /// <summary>
    /// Command to get a technical standard by ID.
    /// </summary>
    public class Command : ICommand<Result<TechnicalStandard>>
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Handler for executing the use case.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<TechnicalStandard>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<TechnicalStandard>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                // Note: Uses TechnicalStandard entity, not DAO
                var technicalStandard = await _uoW.TechnicalStandards.GetByIdAsync(request.Id);

                return technicalStandard == null
                    ? Result.Fail(TechnicalStandardErrors.TechnicalStandardNotFound(request.Id))
                    : Result.Ok(technicalStandard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving TechnicalStandard with Id: {Id}", request.Id);
                return Result.Fail("Error retrieving technical standard");
            }
        }
    }
}
```

**Note:** GetById uses the **entity** (not DAO) for consistency with the CRUD pattern when available.

#### 3.2 GetManyAndCountTechnicalStandardsUseCase (Get List)

**Location:** `src/YourProject.Application/usecases/technicalstandards/GetManyAndCountTechnicalStandardsUseCase.cs`

**Purpose:** Retrieve paginated list of technical standards.

```csharp
using FastEndpoints;
using YourProject.Domain.DAOs;
using YourProject.Domain.Entities;
using YourProject.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace YourProject.Application.UseCases.TechnicalStandards;

/// <summary>
/// Use case for retrieving a paginated list of technical standards.
/// </summary>
public abstract class GetManyAndCountTechnicalStandardsUseCase
{
    /// <summary>
    /// Command to get technical standards with pagination.
    /// </summary>
    public class Command : ICommand<GetManyAndCountResult<TechnicalStandardDao>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for executing the use case.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<TechnicalStandardDao>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<TechnicalStandardDao>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation(
                    "Executing GetManyAndCountTechnicalStandardsUseCase with query: {Query}",
                    command.Query);

                // Uses TechnicalStandardDao via read-only repository
                var technicalStandards = await _uoW.TechnicalStandardDaos.GetManyAndCountAsync(
                    command.Query,
                    nameof(TechnicalStandard.Name),
                    ct);

                _logger.LogInformation(
                    "End GetManyAndCountTechnicalStandardsUseCase with total: {Total}",
                    technicalStandards.Count);

                _uoW.Commit();
                return technicalStandards;
            }
            catch
            {
                _uoW.Rollback();
                throw;
            }
        }
    }
}
```

**Key Point:** Uses `TechnicalStandardDao` via `_uoW.TechnicalStandardDaos` (read-only repository).

---

### 4. WebApi Layer

#### 4.1 GetTechnicalStandardEndpoint (GET by ID)

**Location:** `src/YourProject.WebApi/features/technicalstandards/endpoint/GetTechnicalStandardEndpoint.cs`

```csharp
using FastEndpoints;
using YourProject.Application.UseCases.TechnicalStandards;
using YourProject.Domain.Errors;
using YourProject.WebApi.Features.TechnicalStandards.Models;
using System.Net;

namespace YourProject.WebApi.Features.TechnicalStandards.Endpoint;

/// <summary>
/// Endpoint to get a technical standard by ID.
/// </summary>
public class GetTechnicalStandardEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetTechnicalStandardModel.Request, GetTechnicalStandardModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/technical-standards/{id}");
        Description(d => d
            .WithTags("Technical Standards")
            .WithName("GetTechnicalStandard")
            .WithDescription("Get a technical standard by its ID")
            .Produces<GetTechnicalStandardModel.Response>(200, "application/json")
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetTechnicalStandardModel.Request req, CancellationToken ct)
    {
        var command = new GetTechnicalStandardUseCase.Command { Id = req.Id };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case TechnicalStandardNotFoundError e:
                    await HandleErrorAsync(r => r.Id, e.Message, HttpStatusCode.NotFound, ct);
                    break;
                default:
                    await HandleUnexpectedErrorAsync(error, ct);
                    break;
            }
            return;
        }

        var response = _mapper.Map<GetTechnicalStandardModel.Response>(result.Value);
        await SendOkAsync(response, ct);
    }
}
```

#### 4.2 GetManyAndCountTechnicalStandardsEndpoint (GET List)

**Location:** `src/YourProject.WebApi/features/technicalstandards/endpoint/GetManyAndCountTechnicalStandardsEndpoint.cs`

```csharp
using FastEndpoints;
using YourProject.Application.UseCases.TechnicalStandards;
using YourProject.WebApi.DTOs;
using YourProject.WebApi.Features.TechnicalStandards.Models;

namespace YourProject.WebApi.Features.TechnicalStandards.Endpoint;

/// <summary>
/// Endpoint to get a paginated list of technical standards.
/// </summary>
public class GetManyAndCountTechnicalStandardsEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<GetManyAndCountModel.Request, GetManyAndCountResultDto<TechnicalStandardDto>>
{
    private readonly AutoMapper.IMapper mapper = mapper;

    public override void Configure()
    {
        Get("/technical-standards");
        Description(d => d
            .WithTags("Technical Standards")
            .WithName("GetManyAndCountTechnicalStandards")
            .WithDescription("Get a paginated list of technical standards with filtering and sorting")
            .Produces<GetManyAndCountResultDto<TechnicalStandardDto>>(200, "application/json"));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        try
        {
            req.Query = HttpContext.Request.QueryString.Value;
            var command = this.mapper.Map<GetManyAndCountTechnicalStandardsUseCase.Command>(req);

            var result = await command.ExecuteAsync(ct);
            var response = this.mapper.Map<GetManyAndCountResultDto<TechnicalStandardDto>>(result);

            Logger.LogInformation("Successfully retrieved technical standards");
            await SendOkAsync(response, cancellation: ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve technical standards");
            AddError($"An unexpected error occurred: {ex.Message}");
            await SendErrorsAsync(StatusCodes.Status500InternalServerError, cancellation: ct);
        }
    }
}
```

#### 4.3 TechnicalStandardDto (Data Transfer Object)

**Location:** `src/YourProject.WebApi/dtos/TechnicalStandardDto.cs`

```csharp
namespace YourProject.WebApi.DTOs;

/// <summary>
/// Data Transfer Object for TechnicalStandard.
/// </summary>
public class TechnicalStandardDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
```

---

## Data Flow Diagrams

### Get Single Technical Standard Flow

```
[Client]
   ↓ GET /technical-standards/{id}
[GetTechnicalStandardEndpoint]
   ↓ Maps Request → Command
[GetTechnicalStandardUseCase.Handler]
   ↓ uoW.TechnicalStandards.GetByIdAsync(id)
[ITechnicalStandardRepository] (uses Entity, not DAO)
   ↓ NHibernate session.GetAsync()
[Database Table: technical_standards]
   ↓ Returns TechnicalStandard entity
[Use Case]
   ↓ Returns Result<TechnicalStandard>
[Endpoint]
   ↓ Maps TechnicalStandard → TechnicalStandardDto
[Client]
   ← 200 OK with TechnicalStandardDto
```

### Get Many Technical Standards Flow

```
[Client]
   ↓ GET /technical-standards?query=...
[GetManyAndCountTechnicalStandardsEndpoint]
   ↓ Extracts query string
   ↓ Maps Request → Command
[GetManyAndCountTechnicalStandardsUseCase.Handler]
   ↓ uoW.TechnicalStandardDaos.GetManyAndCountAsync(query, sorting)
[IReadOnlyRepository<TechnicalStandardDao>]
   ↓ Parses query string (filters, sorting, pagination)
   ↓ Builds LINQ expression
   ↓ NHibernate LINQ query
[Database View: technical_standards_view]
   ↓ Returns IEnumerable<TechnicalStandardDao> + Count
[Use Case]
   ↓ Returns GetManyAndCountResult<TechnicalStandardDao>
[Endpoint]
   ↓ Maps TechnicalStandardDao → TechnicalStandardDto
   ↓ Wraps in GetManyAndCountResultDto
[Client]
   ← 200 OK with { items: [], count: N, page: 1, pageSize: 10 }
```

---

## Code Examples

### Example 1: Get Single Technical Standard

**HTTP Request:**
```http
GET /api/technical-standards/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**HTTP Response (200 OK):**
```json
{
  "technicalStandard": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "CFE-G0100-04",
    "name": "Diseño de Estructuras para Líneas de Transmisión",
    "edition": "2015",
    "status": "Active",
    "type": "CFE"
  }
}
```

---

### Example 2: Get Technical Standards List (No Filters)

**HTTP Request:**
```http
GET /api/technical-standards
Authorization: Bearer {token}
```

**HTTP Response (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "CFE-G0100-04",
      "name": "Diseño de Estructuras para Líneas de Transmisión",
      "edition": "2015",
      "status": "Active",
      "type": "CFE"
    },
    {
      "id": "7cb45f64-8921-4562-b3fc-2c963f66afb7",
      "code": "ISO-9001",
      "name": "Quality Management Systems",
      "edition": "2015",
      "status": "Active",
      "type": "Externa"
    }
  ],
  "count": 2,
  "page": 1,
  "pageSize": 10,
  "sorting": {
    "by": "Name",
    "direction": "Ascending"
  }
}
```

---

### Example 3: Get Technical Standards with Filtering

**HTTP Request:**
```http
GET /api/technical-standards?search=CFE&page=1&pageSize=5&sortBy=Code&sortDirection=asc
Authorization: Bearer {token}
```

**Query String Parameters:**
- `search=CFE` - Search in `SearchAll` field
- `page=1` - First page
- `pageSize=5` - 5 items per page
- `sortBy=Code` - Sort by Code field
- `sortDirection=asc` - Ascending order

**HTTP Response (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "CFE-G0100-04",
      "name": "Diseño de Estructuras para Líneas de Transmisión",
      "edition": "2015",
      "status": "Active",
      "type": "CFE"
    },
    {
      "id": "8db45f64-9021-4562-b3fc-2c963f66afc8",
      "code": "CFE-G0200-05",
      "name": "Construcción de Líneas de Transmisión",
      "edition": "2016",
      "status": "Active",
      "type": "CFE"
    }
  ],
  "count": 2,
  "page": 1,
  "pageSize": 5,
  "sorting": {
    "by": "Code",
    "direction": "Ascending"
  }
}
```

---

### Example 4: Technical Standard Not Found

**HTTP Request:**
```http
GET /api/technical-standards/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```

**HTTP Response (404 Not Found):**
```json
{
  "errors": {
    "Id": [
      "Technical standard with id '00000000-0000-0000-0000-000000000000' was not found."
    ]
  },
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

---

## Best Practices

### 1. Use Database Views for Complex Queries

✅ **DO:**
```sql
CREATE VIEW app.technical_standards_view AS
SELECT
    ts.id,
    ts.code,
    ts.name,
    ts.edition,
    ts.status,
    ts.type,
    CONCAT(ts.code, ' ', ts.name, ' ', ts.edition) AS search_all  -- Pre-computed
FROM app.technical_standards ts
WHERE ts.status = 'Active';  -- Pre-filtered
```

❌ **DON'T:** Compute fields in application code on every query.

**Why:** Pre-computed fields improve query performance dramatically.

---

### 2. Mark Mappings as Read-Only

✅ **DO:**
```csharp
public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Mutable(false);  // Prevents accidental updates
        Table("technical_standards_view");
    }
}
```

❌ **DON'T:** Leave mappings mutable for read-only data.

**Why:** Prevents accidental modifications and clarifies intent.

---

### 3. Use IReadOnlyRepository for Read-Only Features

✅ **DO:**
```csharp
public interface IUnitOfWork
{
    IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos { get; }
}
```

❌ **DON'T:** Use full `IRepository<T>` for read-only data.

**Why:** Type system prevents accidental writes.

---

### 4. Don't Validate Read-Only DAOs

✅ **DO:**
```csharp
public class TechnicalStandardDao
{
    public virtual string Code { get; set; } = string.Empty;
    // No validator - it's read-only!
}
```

❌ **DON'T:**
```csharp
public class TechnicalStandardDao
{
    public virtual string Code { get; set; } = string.Empty;
    public override IValidator GetValidator() => new TechnicalStandardDaoValidator(); // Unnecessary!
}
```

**Why:** Read-only data doesn't need validation.

---

### 5. Use Logging for Observability

✅ **DO:**
```csharp
public async Task<Result<TechnicalStandard>> ExecuteAsync(Command request, CancellationToken ct)
{
    try
    {
        _logger.LogInformation("Retrieving TechnicalStandard with Id: {Id}", request.Id);
        var result = await _uoW.TechnicalStandards.GetByIdAsync(request.Id);
        _logger.LogInformation("Successfully retrieved TechnicalStandard");
        return Result.Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving TechnicalStandard with Id: {Id}", request.Id);
        return Result.Fail("Error retrieving technical standard");
    }
}
```

❌ **DON'T:** Skip logging for read operations.

**Why:** Helps diagnose performance issues and track usage.

---

### 6. Implement Proper Error Handling

✅ **DO:**
```csharp
if (result.IsFailed)
{
    var error = result.Errors[0];
    switch (error)
    {
        case TechnicalStandardNotFoundError e:
            await HandleErrorAsync(r => r.Id, e.Message, HttpStatusCode.NotFound, ct);
            break;
        default:
            await HandleUnexpectedErrorAsync(error, ct);
            break;
    }
    return;
}
```

❌ **DON'T:** Return generic 500 errors for not found.

**Why:** Proper HTTP status codes improve API usability.

---

### 7. Use DTOs Consistently

✅ **DO:**
```csharp
var response = _mapper.Map<GetTechnicalStandardModel.Response>(result.Value);
await SendOkAsync(response, ct);
```

❌ **DON'T:** Return entities directly to clients.

**Why:** DTOs decouple API contract from domain model.

---

### 8. Document Query String Parameters

✅ **DO:**
```csharp
Description(d => d
    .WithTags("Technical Standards")
    .WithName("GetManyAndCountTechnicalStandards")
    .WithDescription(@"Get a paginated list of technical standards.

        Query Parameters:
        - search: Search in code, name, edition
        - page: Page number (default: 1)
        - pageSize: Items per page (default: 10)
        - sortBy: Field name to sort by
        - sortDirection: 'asc' or 'desc'")
    .Produces<GetManyAndCountResultDto<TechnicalStandardDto>>(200, "application/json"));
```

❌ **DON'T:** Leave query parameters undocumented.

**Why:** Helps API consumers understand how to use endpoints.

---

### 9. Register Read-Only Repositories Separately

✅ **DO:**
```csharp
public class UnitOfWork : IUnitOfWork
{
    // Write repositories
    private ITechnicalStandardRepository? _technicalStandards;
    public ITechnicalStandardRepository TechnicalStandards =>
        _technicalStandards ??= new NHTechnicalStandardRepository(_session, _serviceProvider);

    // Read-only repositories
    private IReadOnlyRepository<TechnicalStandardDao, Guid>? _technicalStandardDaos;
    public IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos =>
        _technicalStandardDaos ??= new NHReadOnlyRepository<TechnicalStandardDao, Guid>(_session);
}
```

❌ **DON'T:** Mix read-only and write repositories.

**Why:** Clear separation of concerns and prevents confusion.

---

### 10. Use Transactions for Consistency

✅ **DO:**
```csharp
public async Task<GetManyAndCountResult<TechnicalStandardDao>> ExecuteAsync(
    Command command,
    CancellationToken ct)
{
    try
    {
        _uoW.BeginTransaction();  // Read transaction
        var result = await _uoW.TechnicalStandardDaos.GetManyAndCountAsync(command.Query, sorting, ct);
        _uoW.Commit();
        return result;
    }
    catch
    {
        _uoW.Rollback();
        throw;
    }
}
```

❌ **DON'T:** Skip transactions for read operations.

**Why:** Ensures consistent reads and proper session management.

---

## Implementation Checklist

Use this checklist to ensure your read-only feature is complete:

### Domain Layer
- [ ] DAO class created (simple POCO, no validation)
- [ ] IReadOnlyRepository<T, TKey> interface exists (or use base interface)
- [ ] DAO includes all necessary fields for queries
- [ ] SearchAll field included for full-text search (if applicable)

### Infrastructure Layer
- [ ] Database view created via migration
- [ ] View includes pre-computed fields (e.g., SearchAll)
- [ ] NHibernate DAO mapper created
- [ ] Mapper sets `Mutable(false)`
- [ ] Mapper maps to view (not table)
- [ ] NHReadOnlyRepository<T> base class available
- [ ] Custom repository created (if custom methods needed)

### Application Layer
- [ ] GetUseCase created
- [ ] GetManyAndCountUseCase created
- [ ] Use cases use read-only repository
- [ ] Error handling implemented
- [ ] Logging added for observability
- [ ] Transactions used for session management

### WebApi Layer
- [ ] GetEndpoint created (GET /resource/{id})
- [ ] GetManyAndCountEndpoint created (GET /resource)
- [ ] Request/Response models created
- [ ] DTO created
- [ ] AutoMapper profile configured
- [ ] Swagger documentation added
- [ ] Authentication policy applied
- [ ] Error responses properly mapped

### Configuration
- [ ] DAO repository registered in IUnitOfWork
- [ ] NHibernate mapper registered
- [ ] Migration applied to database

### Testing
- [ ] Get single returns 200 OK
- [ ] Get single returns 404 for not found
- [ ] Get many returns items and count
- [ ] Filtering works correctly
- [ ] Sorting works correctly
- [ ] Pagination works correctly
- [ ] No CREATE/UPDATE/DELETE endpoints exist

---

## Related Guides

**Foundation Guides:**
- [CRUD Feature Pattern](../crud-feature/README.md) - Full CRUD implementation
- [Domain Layer - DAOs](../../domain-layer/daos.md) - DAO pattern details
- [Infrastructure Layer - Repository Pattern](../../infrastructure-layer/repository-pattern.md) - Repository concepts
- [WebApi Layer - FastEndpoints Basics](../../webapi-layer/fastendpoints-basics.md) - Endpoint structure

**Next Steps:**
- [Read-Only Feature Step-by-Step](step-by-step.md) - Practical implementation guide
- [Complex Feature Pattern](../complex-feature/README.md) - Features with relationships

---

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-11-15 | Initial version based on TechnicalStandards reference |

---

**Maintainer:** Equipo APSYS
**Reference Project:** hashira.stone.backend (TechnicalStandards DAO pattern)