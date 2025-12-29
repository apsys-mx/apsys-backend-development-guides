# Read-Only Feature Implementation: Step-by-Step Guide

**Version:** 1.0.0
**Last Updated:** 2025-11-15
**Status:** ✅ Complete

## Table of Contents

1. [Introduction](#introduction)
2. [Prerequisites](#prerequisites)
3. [Overview](#overview)
4. [Phase 1: Domain Layer](#phase-1-domain-layer)
5. [Phase 2: Infrastructure Layer](#phase-2-infrastructure-layer)
6. [Phase 3: Application Layer](#phase-3-application-layer)
7. [Phase 4: WebApi Layer](#phase-4-webapi-layer)
8. [Phase 5: Testing](#phase-5-testing)
9. [Phase 6: Database Setup](#phase-6-database-setup)
10. [Verification Checklist](#verification-checklist)
11. [Common Pitfalls](#common-pitfalls)
12. [Next Steps](#next-steps)

---

## Introduction

This guide provides step-by-step instructions to implement a **Read-Only Feature** in a .NET Clean Architecture application. We'll build a **TechnicalStandards** lookup feature as a practical example, covering query operations only.

**What You'll Build:**
- Read-only operations (Get by ID, Get Many with filtering/sorting)
- 13 files across 4 layers
- Database view with pre-computed fields
- Query string parsing for filtering and pagination
- RESTful GET endpoints with Swagger documentation

**Time to Complete:** 1-2 hours

---

## Prerequisites

Before starting, ensure you have:

### Required Knowledge
- C# 13 and .NET 9.0
- Clean Architecture principles
- Basic SQL knowledge
- Understanding of CRUD patterns (recommended to complete CRUD guide first)

### Required Tools
- Visual Studio 2022 or VS Code
- .NET 9.0 SDK
- SQL Server or compatible database

### Required Packages
```xml
<PackageReference Include="FastEndpoints" Version="7.0.1" />
<PackageReference Include="NHibernate" Version="5.5.2" />
<PackageReference Include="FluentValidation" Version="12.0.0" />
<PackageReference Include="FluentResults" Version="4.0.0" />
<PackageReference Include="AutoMapper" Version="14.0.0" />
<PackageReference Include="FluentMigrator" Version="5.2.0" />
```

### Project Structure
```
YourProject/
├── src/
│   ├── YourProject.Domain/
│   ├── YourProject.Application/
│   ├── YourProject.Infrastructure/
│   ├── YourProject.WebApi/
│   └── YourProject.Migrations/
```

---

## Overview

### Implementation Phases

We'll implement the feature in 6 phases:

1. **Phase 1: Domain Layer** (1 file) - DAO only, no validation
2. **Phase 2: Infrastructure Layer** (3 files) - Mapper, base repository, migration
3. **Phase 3: Application Layer** (2 files) - Read use cases
4. **Phase 4: WebApi Layer** (5 files) - GET endpoints
5. **Phase 5: Testing** - Verify read operations
6. **Phase 6: Database Setup** - Create view

### Files to Create

| # | File | Layer | Purpose |
|---|------|-------|---------|
| 1 | TechnicalStandardDao.cs | Domain | Data access object (no validation) |
| 2 | TechnicalStandardDaoMapper.cs | Infrastructure | NHibernate DAO mapping (read-only) |
| 3 | NHReadOnlyRepository.cs | Infrastructure | Base read-only repository (if not exists) |
| 4 | M026TechnicalStandardsView.cs | Migrations | Database view migration |
| 5 | GetTechnicalStandardUseCase.cs | Application | Get single |
| 6 | GetManyAndCountTechnicalStandardsUseCase.cs | Application | Get list with pagination |
| 7 | TechnicalStandardDto.cs | WebApi | Data transfer object |
| 8 | GetTechnicalStandardModel.cs | WebApi | Get request/response |
| 9 | GetManyAndCountModel.cs | WebApi | List request |
| 10 | TechnicalStandardMappingProfile.cs | WebApi | AutoMapper config |
| 11 | GetTechnicalStandardEndpoint.cs | WebApi | GET by ID endpoint |
| 12 | GetManyAndCountTechnicalStandardsEndpoint.cs | WebApi | GET list endpoint |
| 13 | IUnitOfWork update | Domain | Register DAO repository |

**Key Difference from CRUD:** NO Create, Update, Delete files!

---

## Phase 1: Domain Layer

The domain layer for read-only features is minimal - just a DAO (no validation, no business logic).

### Step 1.1: Create TechnicalStandardDao

**Location:** `src/YourProject.Domain/daos/TechnicalStandardDao.cs`

Create the data access object for read operations:

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
- **Simple POCO** - No methods, no validation, just properties
- **No validator** - Read-only data doesn't need validation
- **All properties virtual** - Required for NHibernate lazy loading
- **SearchAll field** - Will be computed in database view for efficient searching

**What's Different from CRUD:**
- ❌ No `GetValidator()` method
- ❌ No business logic methods
- ❌ No FluentValidation validator class

**Checkpoint:** Build the project. No errors should appear.

---

## Phase 2: Infrastructure Layer

The infrastructure layer implements read-only data access.

### Step 2.1: Create or Verify IReadOnlyRepository Interface

**Location:** `src/YourProject.Domain/interfaces/repositories/IReadOnlyRepository.cs`

**Check if this file already exists in your project.** If it doesn't, create it:

```csharp
using System.Linq.Expressions;

namespace YourProject.Domain.Interfaces.Repositories;

/// <summary>
/// Defines a read-only repository for retrieving entities without modification.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
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

**Note:** This is a **base interface** that should be created once and reused for all read-only features.

**Checkpoint:** Build the project.

---

### Step 2.2: Create or Verify NHReadOnlyRepository Implementation

**Location:** `src/YourProject.Infrastructure/nhibernate/NHReadOnlyRepository.cs`

**Check if this file already exists in your project.** If it doesn't, create it:

```csharp
using System.Linq.Expressions;
using YourProject.Domain.Interfaces.Repositories;
using YourProject.Infrastructure.NHibernate.Filtering;
using System.Linq.Dynamic.Core;
using NHibernate;
using NHibernate.Linq;

namespace YourProject.Infrastructure.NHibernate;

/// <summary>
/// NHibernate implementation of read-only repository.
/// Provides only query operations without modification capabilities.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
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

    public GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        var items = this.Get(expression, pageNumber, pageSize, sortingCriteria);
        var total = this.Count(expression);

        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(
        string? query,
        string defaultSorting,
        CancellationToken ct = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute queries sequentially to avoid DataReader issues
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

**Note:** This is a **base class** that should be created once and reused for all read-only features.

**Checkpoint:** Build the project. Verify no errors.

---

### Step 2.3: Create TechnicalStandardDaoMapper

**Location:** `src/YourProject.Infrastructure/nhibernate/mappers/TechnicalStandardDaoMapper.cs`

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

        // CRITICAL: Mutable(false) prevents updates/inserts
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

**Critical Configuration:**
- **`Mutable(false)`** - Tells NHibernate this is read-only (prevents UPDATE/INSERT attempts)
- **`Table("technical_standards_view")`** - Maps to a database VIEW (not table)
- All other mappings are standard column mappings

**What's Different from CRUD:**
- ✅ `Mutable(false)` instead of `Mutable(true)`
- ✅ Maps to VIEW instead of TABLE
- ❌ No Insert/Update mappings

**Checkpoint:** Build the project.

---

### Step 2.4: Create Database View Migration

**Location:** `src/YourProject.Migrations/M026TechnicalStandardsView.cs`

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
                -- Pre-computed search field for efficient full-text search
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
- ✅ **Pre-computed `search_all` field** - No runtime concatenation needed
- ✅ **Pre-filtered** - Only Active standards (filtering in database, not application)
- ✅ **Can join multiple tables** - If needed for complex queries
- ✅ **Optimized for reads** - Database can create execution plans

**Note:** Adjust the migration number to follow your project's sequence.

**Checkpoint:** Don't run the migration yet. We'll do that in Phase 6.

---

## Phase 3: Application Layer

The application layer implements use cases for read operations.

### Step 3.1: Create Error Types (If Not Exists)

Before creating use cases, ensure you have error types defined.

**Location:** `src/YourProject.Domain/errors/TechnicalStandardErrors.cs`

```csharp
using FluentResults;

namespace YourProject.Domain.Errors;

/// <summary>
/// Technical standard-specific error types.
/// </summary>
public static class TechnicalStandardErrors
{
    public static TechnicalStandardNotFoundError TechnicalStandardNotFound(Guid id)
    {
        return new TechnicalStandardNotFoundError(id);
    }
}

/// <summary>
/// Error returned when a technical standard is not found.
/// </summary>
public class TechnicalStandardNotFoundError : Error
{
    public TechnicalStandardNotFoundError(Guid id)
        : base($"Technical standard with id '{id}' was not found.")
    {
        Metadata.Add("TechnicalStandardId", id);
    }
}
```

**Checkpoint:** Build the project.

---

### Step 3.2: Create GetTechnicalStandardUseCase

**Location:** `src/YourProject.Application/usecases/technicalstandards/GetTechnicalStandardUseCase.cs`

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
                _logger.LogInformation("Retrieving TechnicalStandard with Id: {Id}", request.Id);

                // Note: If you have a TechnicalStandard entity, use it for GetById
                // Otherwise, use the DAO repository
                var technicalStandard = await _uoW.TechnicalStandards.GetByIdAsync(request.Id);

                if (technicalStandard == null)
                {
                    _logger.LogWarning("TechnicalStandard not found with Id: {Id}", request.Id);
                    return Result.Fail(TechnicalStandardErrors.TechnicalStandardNotFound(request.Id));
                }

                _logger.LogInformation("Successfully retrieved TechnicalStandard");
                return Result.Ok(technicalStandard);
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

**Note:** This example assumes you have a `TechnicalStandard` entity in addition to the DAO. If your feature is **purely read-only** (no entity exists), modify to use the DAO:

```csharp
// Alternative: Use DAO if no entity exists
var technicalStandard = await _uoW.TechnicalStandardDaos.GetAsync(request.Id, ct);
```

**Checkpoint:** Build the project.

---

### Step 3.3: Create GetManyAndCountTechnicalStandardsUseCase

**Location:** `src/YourProject.Application/usecases/technicalstandards/GetManyAndCountTechnicalStandardsUseCase.cs`

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
    /// Command to get technical standards with pagination and filtering.
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
                    nameof(TechnicalStandard.Name), // Default sorting field
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

**Key Points:**
- Returns `GetManyAndCountResult<TechnicalStandardDao>` (uses DAO, not entity)
- Accesses via `_uoW.TechnicalStandardDaos` (read-only repository)
- Query string is parsed by repository for filtering/sorting/pagination

**Checkpoint:** Build the project. Application layer is now complete.

---

## Phase 4: WebApi Layer

The WebApi layer exposes HTTP GET endpoints.

### Step 4.1: Create TechnicalStandardDto

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

**Note:** DTO doesn't include `SearchAll` (internal optimization field).

**Checkpoint:** Build the project.

---

### Step 4.2: Create Request/Response Models

#### GetTechnicalStandardModel

**Location:** `src/YourProject.WebApi/features/technicalstandards/models/GetTechnicalStandardModel.cs`

```csharp
using YourProject.WebApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace YourProject.WebApi.Features.TechnicalStandards.Models;

/// <summary>
/// Model for getting a single technical standard.
/// </summary>
public class GetTechnicalStandardModel
{
    /// <summary>
    /// Request to get a technical standard by ID.
    /// </summary>
    public class Request
    {
        [FromRoute]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Response containing the technical standard.
    /// </summary>
    public class Response
    {
        public TechnicalStandardDto TechnicalStandard { get; set; } = new TechnicalStandardDto();
    }
}
```

#### GetManyAndCountModel

**Location:** `src/YourProject.WebApi/features/technicalstandards/models/GetManyAndCountModel.cs`

```csharp
namespace YourProject.WebApi.Features.TechnicalStandards.Models;

/// <summary>
/// Model for getting a paginated list of technical standards.
/// </summary>
public class GetManyAndCountModel
{
    /// <summary>
    /// Request for getting technical standards with filtering/sorting.
    /// </summary>
    public class Request
    {
        public string? Query { get; set; }
    }
}
```

**Checkpoint:** Build the project.

---

### Step 4.3: Create TechnicalStandardMappingProfile

**Location:** `src/YourProject.WebApi/mappingprofiles/TechnicalStandardMappingProfile.cs`

```csharp
using AutoMapper;
using YourProject.Application.UseCases.TechnicalStandards;
using YourProject.Domain.DAOs;
using YourProject.Domain.Entities;
using YourProject.WebApi.DTOs;
using YourProject.WebApi.Features.TechnicalStandards.Models;

namespace YourProject.WebApi.MappingProfiles;

public class TechnicalStandardMappingProfile : Profile
{
    public TechnicalStandardMappingProfile()
    {
        // DAO to DTO
        CreateMap<TechnicalStandardDao, TechnicalStandardDto>();

        // GetManyAndCount mappings
        CreateMap<GetManyAndCountResultDto<TechnicalStandardDao>, GetManyAndCountResultDto<TechnicalStandardDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
        CreateMap<GetManyAndCountModel.Request, GetManyAndCountTechnicalStandardsUseCase.Command>();

        // Entity to DTO (if entity exists)
        CreateMap<TechnicalStandard, TechnicalStandardDto>();

        // Get mappings
        CreateMap<GetTechnicalStandardModel.Request, GetTechnicalStandardUseCase.Command>();
        CreateMap<TechnicalStandard, GetTechnicalStandardModel.Response>()
            .ForMember(dest => dest.TechnicalStandard, opt => opt.MapFrom(src => src));
    }
}
```

**Mapping Strategy:**
- DAO → DTO (for list operations)
- Entity → DTO (for single get, if entity exists)
- Request → Use Case Command

**Checkpoint:** Build the project.

---

### Step 4.4: Create GetTechnicalStandardEndpoint

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

**Key Configuration:**
- **Route:** GET /technical-standards/{id}
- **Status Codes:** 200 (OK), 404 (Not Found), 500 (Error)
- **Authentication:** Required (MustBeApplicationUser policy)

**Checkpoint:** Build the project.

---

### Step 4.5: Create GetManyAndCountTechnicalStandardsEndpoint

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
            .WithDescription(@"Get a paginated list of technical standards with filtering and sorting.

            Query Parameters:
            - search: Search in code, name, edition
            - page: Page number (default: 1)
            - pageSize: Items per page (default: 10)
            - sortBy: Field name to sort by (default: Name)
            - sortDirection: 'asc' or 'desc' (default: asc)")
            .Produces<GetManyAndCountResultDto<TechnicalStandardDto>>(200, "application/json"));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        try
        {
            // Extract full query string
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

**Key Configuration:**
- **Route:** GET /technical-standards
- **Query String:** Supports filtering, sorting, pagination
- **Status Codes:** 200 (OK), 500 (Error)

**Checkpoint:** Build the project. WebApi layer is now complete.

---

## Phase 5: Testing

### Step 5.1: Configure Dependency Injection

Update your DI configuration (typically in `Program.cs`):

```csharp
// Register AutoMapper
builder.Services.AddAutoMapper(typeof(TechnicalStandardMappingProfile));

// FastEndpoints (if not already registered)
builder.Services.AddFastEndpoints();

// Configure authentication (if not already configured)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBeApplicationUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// In middleware pipeline
app.UseFastEndpoints();
```

**Checkpoint:** Run the application. It should start without errors.

---

### Step 5.2: Update IUnitOfWork

Add the TechnicalStandardDao repository to your `IUnitOfWork`:

**Location:** `src/YourProject.Domain/interfaces/repositories/IUnitOfWork.cs`

```csharp
public interface IUnitOfWork
{
    // ... existing repositories

    // If you have a TechnicalStandard entity:
    ITechnicalStandardRepository TechnicalStandards { get; }

    // Read-only DAO repository
    IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

And implement in your UnitOfWork class:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ISession _session;
    private IReadOnlyRepository<TechnicalStandardDao, Guid>? _technicalStandardDaos;

    public IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos =>
        _technicalStandardDaos ??= new NHReadOnlyRepository<TechnicalStandardDao, Guid>(_session);

    // ... rest of implementation
}
```

**Checkpoint:** Build and run the application.

---

### Step 5.3: Manual Testing with Swagger

Navigate to Swagger UI (typically `https://localhost:5001/swagger`).

#### Test 1: Get Technical Standard by ID

**Request:**
```http
GET /api/technical-standards/{id}
Authorization: Bearer {token}
```

**Expected Response (200 OK):**
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

#### Test 2: Get All Technical Standards (No Filters)

**Request:**
```http
GET /api/technical-standards
Authorization: Bearer {token}
```

**Expected Response (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "CFE-G0100-04",
      "name": "Diseño de Estructuras",
      "edition": "2015",
      "status": "Active",
      "type": "CFE"
    }
  ],
  "count": 1,
  "page": 1,
  "pageSize": 10,
  "sorting": {
    "by": "Name",
    "direction": "Ascending"
  }
}
```

#### Test 3: Get Technical Standards with Filtering

**Request:**
```http
GET /api/technical-standards?search=CFE&page=1&pageSize=5&sortBy=Code&sortDirection=asc
Authorization: Bearer {token}
```

**Expected Response (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "CFE-G0100-04",
      "name": "Diseño de Estructuras",
      "edition": "2015",
      "status": "Active",
      "type": "CFE"
    }
  ],
  "count": 1,
  "page": 1,
  "pageSize": 5,
  "sorting": {
    "by": "Code",
    "direction": "Ascending"
  }
}
```

#### Test 4: Not Found

**Request:**
```http
GET /api/technical-standards/00000000-0000-0000-0000-000000000000
Authorization: Bearer {token}
```

**Expected Response (404 Not Found):**
```json
{
  "errors": {
    "Id": [
      "Technical standard with id '00000000-0000-0000-0000-000000000000' was not found."
    ]
  }
}
```

**Checkpoint:** All tests should pass.

---

## Phase 6: Database Setup

### Step 6.1: Ensure Base Table Exists

Before creating the view, ensure the base `technical_standards` table exists. If it doesn't, create a migration:

```csharp
[Migration(202511140001)]
public class M025TechnicalStandardsTable : Migration
{
    public override void Up()
    {
        Create.Table("technical_standards")
            .InSchema("app")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("creation_date").AsDateTime().NotNullable()
            .WithColumn("code").AsString(50).NotNullable().Unique()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("edition").AsString(50).NotNullable()
            .WithColumn("status").AsString(50).NotNullable()
            .WithColumn("type").AsString(50).NotNullable();

        // Indexes for performance
        Create.Index("IX_TechnicalStandards_Code")
            .OnTable("technical_standards").InSchema("app")
            .OnColumn("code");

        Create.Index("IX_TechnicalStandards_Status")
            .OnTable("technical_standards").InSchema("app")
            .OnColumn("status");
    }

    public override void Down()
    {
        Delete.Table("technical_standards").InSchema("app");
    }
}
```

---

### Step 6.2: Run the View Migration

Run the migration we created in Step 2.4:

```bash
dotnet fm migrate -p YourProject.Migrations -c "YourConnectionString"
```

Or if using a migration runner tool, execute the migration.

**Verify:** Check that the `technical_standards_view` exists in your database.

---

### Step 6.3: Register NHibernate Mapper

In your NHibernate configuration setup:

```csharp
var mapper = new ModelMapper();

// Add DAO mapper
mapper.AddMapping<TechnicalStandardDaoMapper>();

// Add other mappers...

var configuration = new Configuration();
configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
```

**Checkpoint:** Run the application and verify database connection.

---

## Verification Checklist

Use this checklist to ensure your read-only feature is complete:

### Domain Layer
- [ ] TechnicalStandardDao created (simple POCO)
- [ ] No validator class (read-only doesn't need validation)
- [ ] DAO includes all necessary fields
- [ ] SearchAll field included for full-text search

### Infrastructure Layer
- [ ] Database view created via migration
- [ ] View includes pre-computed SearchAll field
- [ ] TechnicalStandardDaoMapper created
- [ ] Mapper sets `Mutable(false)`
- [ ] Mapper maps to view (not table)
- [ ] NHReadOnlyRepository base class exists
- [ ] Mapper registered in NHibernate configuration

### Application Layer
- [ ] GetTechnicalStandardUseCase created
- [ ] GetManyAndCountTechnicalStandardsUseCase created
- [ ] Error handling implemented
- [ ] Logging added
- [ ] Transactions used for session management

### WebApi Layer
- [ ] TechnicalStandardDto created
- [ ] GetTechnicalStandardModel created
- [ ] GetManyAndCountModel created
- [ ] TechnicalStandardMappingProfile created
- [ ] GetTechnicalStandardEndpoint created
- [ ] GetManyAndCountTechnicalStandardsEndpoint created
- [ ] Swagger documentation added
- [ ] Authentication policy applied

### Configuration
- [ ] TechnicalStandardDaos registered in IUnitOfWork
- [ ] AutoMapper profile registered
- [ ] FastEndpoints configured

### Testing
- [ ] Get single returns 200 OK
- [ ] Get single returns 404 for not found
- [ ] Get many returns items and count
- [ ] Filtering works correctly
- [ ] Sorting works correctly
- [ ] Pagination works correctly

### Verification
- [ ] NO Create endpoint exists
- [ ] NO Update endpoint exists
- [ ] NO Delete endpoint exists
- [ ] Application starts without errors
- [ ] Swagger UI accessible
- [ ] All GET operations work

---

## Common Pitfalls

### 1. Forgot Mutable(false) in Mapper

**Problem:** NHibernate tries to update/insert and fails

**Solution:** Always set `Mutable(false)` in DAO mappers:
```csharp
public TechnicalStandardDaoMapper()
{
    Mutable(false); // CRITICAL!
    Table("technical_standards_view");
}
```

---

### 2. View Doesn't Exist

**Problem:** `Invalid object name 'technical_standards_view'`

**Solution:** Ensure migration was run:
```bash
dotnet fm migrate -p YourProject.Migrations -c "ConnectionString"
```

---

### 3. SearchAll Field Not Computed

**Problem:** SearchAll is empty or null

**Solution:** Ensure view computes the field:
```sql
CONCAT(ts.code, ' ', ts.name, ' ', ts.edition) AS search_all
```

---

### 4. DAO Repository Not Registered

**Problem:** `NullReferenceException` when accessing `_uoW.TechnicalStandardDaos`

**Solution:** Register in IUnitOfWork:
```csharp
public IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos =>
    _technicalStandardDaos ??= new NHReadOnlyRepository<TechnicalStandardDao, Guid>(_session);
```

---

### 5. Query String Not Passed to Use Case

**Problem:** Filtering/sorting doesn't work

**Solution:** Extract full query string in endpoint:
```csharp
req.Query = HttpContext.Request.QueryString.Value;
```

---

### 6. Mapper Not Registered

**Problem:** `AutoMapperMappingException: Missing type map configuration`

**Solution:** Register profile:
```csharp
builder.Services.AddAutoMapper(typeof(TechnicalStandardMappingProfile));
```

---

### 7. Wrong Repository Interface

**Problem:** Trying to call Add/Update/Delete methods

**Solution:** Use `IReadOnlyRepository<T>` not `IRepository<T>`:
```csharp
// CORRECT
IReadOnlyRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos { get; }

// WRONG
IRepository<TechnicalStandardDao, Guid> TechnicalStandardDaos { get; }
```

---

### 8. Missing Base Table

**Problem:** View creation fails because base table doesn't exist

**Solution:** Create base table migration first (before view migration)

---

## Next Steps

Congratulations! You've implemented a complete read-only feature. Here's what to do next:

### 1. Add Unit Tests

Create tests for each layer:
- **Domain**: Test DAO structure (minimal, since no logic)
- **Application**: Test use cases with mocked repositories
- **Infrastructure**: Test repository methods with in-memory database
- **WebApi**: Test endpoint responses

### 2. Add Integration Tests

Test the complete flow:
```csharp
[Fact]
public async Task GetMany_ReturnsFilteredResults()
{
    // Act
    var response = await client.GetAsync("/technical-standards?search=CFE");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadAsAsync<GetManyAndCountResultDto<TechnicalStandardDto>>();
    Assert.All(result.Items, item => Assert.Contains("CFE", item.Code));
}
```

### 3. Optimize Database View

Add indexes to base table columns used in WHERE/JOIN:
```sql
CREATE INDEX IX_TechnicalStandards_Status
ON app.technical_standards(status);
```

### 4. Add Caching

Cache frequently accessed data:
```csharp
[Cache(60)] // Cache for 60 seconds
public override void Configure() { ... }
```

### 5. Add Advanced Filtering

Extend QueryStringParser to support field-specific filters:
```
/technical-standards?type=CFE&status=Active
```

### 6. Add Export Functionality

Add endpoint to export to CSV/Excel:
```csharp
Get("/technical-standards/export");
```

### 7. Implement Similar Read-Only Features

Apply this pattern to other lookup tables:
- Countries
- Status codes
- Categories
- User roles (for dropdowns)

### 8. Add GraphQL Support

Expose read-only data via GraphQL for flexible queries

### 9. Review Best Practices

Read related guides:
- [CRUD Feature Pattern](README.md) - Comparison with full CRUD
- [Domain Layer - DAOs](../../domain-layer/daos.md) - DAO pattern details
- [Infrastructure Layer - Repository Pattern](../../infrastructure-layer/repository-pattern.md)

---

## Summary

You've successfully implemented a read-only feature with:

✅ **13 files** across 4 architectural layers
✅ **Database view** with pre-computed fields
✅ **Read-only repository** preventing accidental modifications
✅ **Query string parsing** for flexible filtering/sorting/pagination
✅ **RESTful GET endpoints** with proper status codes
✅ **Error handling** with typed exceptions
✅ **Swagger documentation** for API consumers

**Total Implementation Time:** 1-2 hours
**Lines of Code:** ~800 lines
**Operations Supported:** Get (single), Get Many (with filtering/sorting/pagination)

This feature serves as a template for all future read-only/lookup features in your Clean Architecture application.

---

## Related Guides

- [Read-Only Feature Overview](README.md) - Conceptual overview
- [CRUD Feature Guide](../crud-feature/README.md) - Full CRUD pattern
- [Complex Feature Guide](../complex-feature/README.md) - Advanced relationships
- [WebApi Layer Guide](../../webapi-layer/README.md) - HTTP layer details
- [Application Layer Guide](../../application-layer/README.md) - Use case patterns

---

**Need Help?**
- Review error messages carefully
- Check the Common Pitfalls section
- Verify the checklist for missing steps
- Compare your code with the reference implementation

---

**Maintainer:** Equipo APSYS
**Reference Project:** hashira.stone.backend (TechnicalStandards DAO pattern)