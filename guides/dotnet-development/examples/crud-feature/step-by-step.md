# CRUD Feature Implementation: Step-by-Step Guide

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

This guide provides step-by-step instructions to implement a complete CRUD feature in a .NET Clean Architecture application. We'll build the **Prototypes** feature as a practical example, covering all four architectural layers.

**What You'll Build:**
- Complete CRUD operations (Create, Read, Update, Get Many)
- 21 files across 4 layers
- Validation, error handling, and transaction management
- RESTful API endpoints with Swagger documentation

**Time to Complete:** 2-3 hours

---

## Prerequisites

Before starting, ensure you have:

### Required Knowledge
- C# 13 and .NET 9.0
- Clean Architecture principles
- Dependency injection concepts
- Basic SQL knowledge

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
```

### Project Structure
```
YourProject/
├── src/
│   ├── YourProject.Domain/
│   ├── YourProject.Application/
│   ├── YourProject.Infrastructure/
│   └── YourProject.WebApi/
```

---

## Overview

### Implementation Phases

We'll implement the feature in 6 phases:

1. **Phase 1: Domain Layer** (4 files) - Business entities and rules
2. **Phase 2: Infrastructure Layer** (3 files) - Database mapping
3. **Phase 3: Application Layer** (4 files) - Use cases
4. **Phase 4: WebApi Layer** (9 files) - HTTP endpoints
5. **Phase 5: Testing** - Verify each operation
6. **Phase 6: Database Setup** - Create tables and views

### Files to Create

| # | File | Layer | Purpose |
|---|------|-------|---------|
| 1 | PrototypeDao.cs | Domain | Data access object |
| 2 | PrototypeValidator.cs | Domain | Validation rules |
| 3 | Prototype.cs | Domain | Domain entity |
| 4 | IPrototypeRepository.cs | Domain | Repository contract |
| 5 | PrototypeMapper.cs | Infrastructure | NHibernate entity mapping |
| 6 | PrototypeDaoMapper.cs | Infrastructure | NHibernate DAO mapping |
| 7 | NHPrototypeRepository.cs | Infrastructure | Repository implementation |
| 8 | CreatePrototypeUseCase.cs | Application | Create operation |
| 9 | GetPrototypeUseCase.cs | Application | Read single |
| 10 | GetManyAndCountPrototypesUseCase.cs | Application | Read multiple |
| 11 | UpdatePrototypeUseCase.cs | Application | Update operation |
| 12 | PrototypeDto.cs | WebApi | Data transfer object |
| 13 | CreatePrototypeModel.cs | WebApi | Create request/response |
| 14 | GetPrototypeModel.cs | WebApi | Get request/response |
| 15 | GetManyAndCountPrototypesModel.cs | WebApi | List request |
| 16 | UpdatePrototypeModel.cs | WebApi | Update request/response |
| 17 | PrototypeMappingProfile.cs | WebApi | AutoMapper config |
| 18 | CreatePrototypeEndpoint.cs | WebApi | POST endpoint |
| 19 | GetPrototypeEndpoint.cs | WebApi | GET endpoint |
| 20 | GetManyAndCountPrototypesEndpoint.cs | WebApi | GET list endpoint |
| 21 | UpdatePrototypeEndpoint.cs | WebApi | PUT endpoint |

---

## Phase 1: Domain Layer

The domain layer defines business entities, validation rules, and repository contracts.

### Step 1.1: Create PrototypeDao

**Location:** `src/YourProject.Domain/daos/PrototypeDao.cs`

Create the data access object for read operations:

```csharp
namespace YourProject.Domain.Daos;

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

**Why DAO?**
- DAOs are optimized for read operations
- Separate from entities to avoid loading unnecessary relationships
- Support query-specific fields like `SearchAll`

**Checkpoint:** Build the project. No errors should appear.

---

### Step 1.2: Create PrototypeValidator

**Location:** `src/YourProject.Domain/entities/validators/PrototypeValidator.cs`

First, create a resources class for valid statuses:

**Location:** `src/YourProject.Domain/resources/PrototypeResources.cs`

```csharp
namespace YourProject.Domain.Resources;

public static class PrototypeResources
{
    public static readonly List<string> ValidStatus = new()
    {
        "Active",
        "Expired",
        "Cancelled"
    };
}
```

Now create the validator:

```csharp
using FluentValidation;
using YourProject.Domain.Resources;

namespace YourProject.Domain.Entities.Validators;

public class PrototypeValidator : AbstractValidator<Prototype>
{
    public PrototypeValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("Number is required");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("IssueDate is required")
            .Must(date => date <= DateTime.Today)
            .WithMessage("IssueDate cannot be in the future");

        RuleFor(x => x.ExpirationDate)
            .NotEmpty()
            .WithMessage("ExpirationDate is required")
            .Must((prototype, expirationDate) => expirationDate > prototype.IssueDate)
            .WithMessage("ExpirationDate must be after IssueDate");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => PrototypeResources.ValidStatus.Contains(status))
            .WithMessage("Status must be one of: Active, Expired, Cancelled");
    }
}
```

**Validation Rules:**
- Number: Required
- IssueDate: Required, cannot be future
- ExpirationDate: Required, must be after IssueDate
- Status: Required, must be from valid list

**Checkpoint:** Build the project. The `Prototype` entity doesn't exist yet, so you'll see errors. This is expected.

---

### Step 1.3: Create Prototype Entity

**Location:** `src/YourProject.Domain/entities/Prototype.cs`

```csharp
using FluentValidation;
using YourProject.Domain.Entities.Validators;

namespace YourProject.Domain.Entities;

/// <summary>
/// Represents a prototype domain object with properties for tracking its number, issue date,
/// expiration date, and status.
/// </summary>
public class Prototype : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Prototype"/> class.
    /// </summary>
    public Prototype()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Prototype"/> class with the specified details.
    /// </summary>
    /// <param name="number">The unique identifier for the prototype. Cannot be null or empty.</param>
    /// <param name="issueDate">The date when the prototype was issued.</param>
    /// <param name="expirationDate">The date when the prototype expires. Must be later than <paramref name="issueDate"/>.</param>
    /// <param name="status">The current status of the prototype. Cannot be null or empty.</param>
    public Prototype(string number, DateTime issueDate, DateTime expirationDate, string status)
    {
        Number = number;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Status = status;
    }

    /// <summary>
    /// Gets or sets the number as a string.
    /// </summary>
    public virtual string Number { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the issue was created or recorded.
    /// </summary>
    public virtual DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the item.
    /// </summary>
    public virtual DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the operation.
    /// </summary>
    public virtual string Status { get; set; } = string.Empty;

    /// <summary>
    /// Retrieves the validator associated with the current instance.
    /// </summary>
    /// <returns>An <see cref="IValidator"/> instance that validates the current object.</returns>
    public override IValidator GetValidator()
    {
        return new PrototypeValidator();
    }
}
```

**Key Points:**
- Inherits from `AbstractDomainObject` (provides Id, CreationDate, IsValid(), Validate())
- Properties are `virtual` for NHibernate lazy loading
- Two constructors: parameterless (for NHibernate) and parameterized (for creation)
- `GetValidator()` returns the FluentValidation validator

**Checkpoint:** Build the project. All validation errors should now be resolved.

---

### Step 1.4: Create IPrototypeRepository

**Location:** `src/YourProject.Domain/interfaces/repositories/IPrototypeRepository.cs`

```csharp
using YourProject.Domain.Entities;

namespace YourProject.Domain.Interfaces.Repositories;

/// <summary>
/// Defines a repository for managing <see cref="Prototype"/> entities.
/// </summary>
public interface IPrototypeRepository : IRepository<Prototype, Guid>
{
    /// <summary>
    /// Asynchronously creates a new <see cref="Prototype"/> instance with the specified details.
    /// </summary>
    /// <param name="number">The unique identifier for the prototype. Cannot be null or empty.</param>
    /// <param name="issueDate">The date and time when the prototype was issued.</param>
    /// <param name="expirationDate">The date and time when the prototype expires. Must be later than <paramref name="issueDate"/>.</param>
    /// <param name="status">The current status of the prototype. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Prototype"/> instance.</returns>
    Task<Prototype> CreateAsync(string number, DateTime issueDate, DateTime expirationDate, string status);

    /// <summary>
    /// Retrieves a prototype by its number.
    /// </summary>
    /// <param name="number">The prototype number to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The prototype with the specified number, or null if not found.</returns>
    Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously updates an existing <see cref="Prototype"/> instance with the specified details.
    /// </summary>
    /// <param name="id">The unique identifier of the prototype to update.</param>
    /// <param name="number">The unique identifier for the prototype. Cannot be null or empty.</param>
    /// <param name="issueDate">The date and time when the prototype was issued.</param>
    /// <param name="expirationDate">The date and time when the prototype expires. Must be later than <paramref name="issueDate"/>.</param>
    /// <param name="status">The current status of the prototype. Cannot be null or empty.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see cref="Prototype"/> instance.</returns>
    Task<Prototype> UpdateAsync(Guid id, string number, DateTime issueDate, DateTime expirationDate, string status, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a prototype by its id.
    /// </summary>
    /// <param name="id">The prototype id to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The prototype with the specified id, or null if not found.</returns>
    Task<Prototype?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

**Repository Methods:**
- `CreateAsync()` - Business logic for creating entities
- `GetByNumberAsync()` - Find by unique number
- `UpdateAsync()` - Business logic for updating entities
- `GetByIdAsync()` - Find by ID

**Why Custom Methods?**
- Standard CRUD methods (from `IRepository<T>`) handle simple operations
- Custom methods encapsulate business logic (validation, duplicate checking)

**Checkpoint:** Build the project. Domain layer is now complete.

---

## Phase 2: Infrastructure Layer

The infrastructure layer implements database persistence using NHibernate.

### Step 2.1: Create PrototypeMapper

**Location:** `src/YourProject.Infrastructure/nhibernate/mappers/PrototypeMapper.cs`

```csharp
using YourProject.Domain.Entities;
using YourProject.Domain.Resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace YourProject.Infrastructure.NHibernate.Mappers;

public class PrototypeMapper : ClassMapping<Prototype>
{
    public PrototypeMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("prototypes");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Number, map =>
        {
            map.Column("number");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.IssueDate, map =>
        {
            map.Column("issue_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.ExpirationDate, map =>
        {
            map.Column("expiration_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Status, map =>
        {
            map.Column("status");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

**Mapping Configuration:**
- **Schema**: Uses your application's schema (e.g., "app")
- **Table**: Maps to "prototypes" table
- **Id**: GUID with assigned generator (application generates IDs)
- **Number**: String, not nullable, unique constraint
- **Dates**: DateTime type, not nullable
- **Status**: String, not nullable

**Note:** Adjust `AppSchemaResource.SchemaName` to your project's schema resource.

**Checkpoint:** Build the project. Verify no mapping errors.

---

### Step 2.2: Create PrototypeDaoMapper

**Location:** `src/YourProject.Infrastructure/nhibernate/mappers/PrototypeDaoMapper.cs`

```csharp
using YourProject.Domain.Daos;
using YourProject.Domain.Resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace YourProject.Infrastructure.NHibernate.Mappers;

public class PrototypeDaoMapper : ClassMapping<PrototypeDao>
{
    public PrototypeDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Mutable(false);
        Table("prototypes_view");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Number, map =>
        {
            map.Column("number");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.IssueDate, map =>
        {
            map.Column("issue_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.ExpirationDate, map =>
        {
            map.Column("expiration_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Status, map =>
        {
            map.Column("status");
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

**Key Differences from Entity Mapper:**
- **Mutable(false)**: Read-only, no updates allowed
- **Table**: Maps to "prototypes_view" (database view)
- **SearchAll**: Additional field for full-text search

**Why Use a View?**
- Optimized for queries with computed fields
- Can include data from multiple tables if needed
- Prevents accidental writes to read models

**Checkpoint:** Build the project. Both mappers should compile.

---

### Step 2.3: Create NHPrototypeRepository

**Location:** `src/YourProject.Infrastructure/nhibernate/repositories/NHPrototypeRepository.cs`

```csharp
using YourProject.Domain.Entities;
using YourProject.Domain.Exceptions;
using YourProject.Domain.Interfaces.Repositories;
using NHibernate;

namespace YourProject.Infrastructure.NHibernate.Repositories;

/// <summary>
/// Provides functionality for managing <see cref="Prototype"/> entities in the database.
/// </summary>
public class NHPrototypeRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<Prototype, Guid>(session, serviceProvider), IPrototypeRepository
{
    /// <summary>
    /// Creates a new <see cref="Prototype"/> instance and persists it to the database.
    /// </summary>
    public async Task<Prototype> CreateAsync(string number, DateTime issueDate, DateTime expirationDate, string status)
    {
        var prototype = new Prototype(number, issueDate, expirationDate, status);

        if (!prototype.IsValid())
            throw new InvalidDomainException(prototype.Validate());

        var count = await this.CountAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant());

        if (count > 0)
            throw new DuplicatedDomainException($"A prototype with the number '{number}' already exists.");

        await AddAsync(prototype);
        this.FlushWhenNotActiveTransaction();
        return prototype;
    }

    /// <summary>
    /// Retrieves a prototype by its number.
    /// </summary>
    public async Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(number))
            return null;

        var prototype = await this.GetAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant(), ct);
        return prototype.FirstOrDefault();
    }

    /// <summary>
    /// Updates an existing <see cref="Prototype"/> instance with the specified details.
    /// </summary>
    public async Task<Prototype> UpdateAsync(Guid id, string number, DateTime issueDate, DateTime expirationDate, string status, CancellationToken ct = default)
    {
        // Retrieve the existing prototype
        var prototype = await this.GetAsync(id);

        if (prototype is null)
            throw new ResourceNotFoundException($"Prototype with id '{id}' does not exist.");

        // Check for duplicates using the GetByNumberAsync method
        var existingWithNumber = await this.GetByNumberAsync(number, ct);
        if (existingWithNumber is not null && existingWithNumber.Id != id)
            throw new DuplicatedDomainException($"A prototype with the number '{number}' already exists.");

        // Update properties
        prototype.Number = number;
        prototype.IssueDate = issueDate;
        prototype.ExpirationDate = expirationDate;
        prototype.Status = status;

        // Validate the updated prototype
        if (!prototype.IsValid())
            throw new InvalidDomainException(prototype.Validate());

        // Persist the changes
        await _session.UpdateAsync(prototype, ct);
        this.FlushWhenNotActiveTransaction();
        return prototype;
    }

    /// <summary>
    /// Retrieves a prototype by its id.
    /// </summary>
    public async Task<Prototype?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var prototype = await this.GetAsync(p => p.Id == id, ct);
        return prototype.FirstOrDefault();
    }
}
```

**Repository Implementation Details:**

**CreateAsync:**
1. Creates new entity instance
2. Validates using FluentValidation
3. Checks for duplicates (case-insensitive)
4. Adds to database
5. Flushes if not in transaction

**UpdateAsync:**
1. Retrieves existing entity
2. Checks existence
3. Validates no duplicate number (except same entity)
4. Updates properties
5. Validates updated entity
6. Persists changes

**Error Handling:**
- `InvalidDomainException` - Validation failures
- `DuplicatedDomainException` - Duplicate number
- `ResourceNotFoundException` - Entity not found

**Checkpoint:** Build the project. Infrastructure layer is now complete.

---

## Phase 3: Application Layer

The application layer implements use cases (business operations).

### Step 3.1: Create Error Types

Before creating use cases, define custom error types.

**Location:** `src/YourProject.Domain/errors/PrototypeStandardErrors.cs`

```csharp
using FluentResults;

namespace YourProject.Domain.Errors;

/// <summary>
/// Prototype-specific error types.
/// </summary>
public static class PrototypeStandardErrors
{
    public static PrototypeNotFoundError PrototypeNotFound(Guid id)
    {
        return new PrototypeNotFoundError(id);
    }
}

/// <summary>
/// Error returned when a prototype is not found.
/// </summary>
public class PrototypeNotFoundError : Error
{
    public PrototypeNotFoundError(Guid id)
        : base($"Prototype with id '{id}' was not found.")
    {
        Metadata.Add("PrototypeId", id);
    }
}
```

**Checkpoint:** Build the project.

---

### Step 3.2: Create CreatePrototypeUseCase

**Location:** `src/YourProject.Application/usecases/prototypes/CreatePrototypeUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using YourProject.Application.Common;
using YourProject.Domain.Entities;
using YourProject.Domain.Exceptions;
using YourProject.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace YourProject.Application.UseCases.Prototypes;

public abstract class CreatePrototypeUseCase
{
    public class Command : ICommand<Result<Prototype>>
    {
        /// <summary>
        /// Gets or sets the number of prototype as a string.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the issue was created or recorded.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the item.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the operation.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<Prototype>>
    {
        private readonly IUnitOfWork _uoW = uoW;

        /// <summary>
        /// Executes the command to create a new prototype.
        /// </summary>
        public async Task<Result<Prototype>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                var prototype = await _uoW.Prototypes.CreateAsync(
                    command.Number,
                    command.IssueDate,
                    command.ExpirationDate,
                    command.Status
                );
                _uoW.Commit();
                return Result.Ok(prototype);
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid prototype data";
                return Result.Fail(new Error(firstErrorMessage)
                    .CausedBy(idex)
                    .WithMetadata("ValidationErrors", idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

**Use Case Pattern:**
- **Command**: Input data (properties match request model)
- **Handler**: Business logic executor
- **Result<T>**: Success or failure with error details

**Transaction Management:**
1. Begin transaction
2. Execute repository method
3. Commit on success
4. Rollback on any exception

**Checkpoint:** Build the project.

---

### Step 3.3: Create GetPrototypeUseCase

**Location:** `src/YourProject.Application/usecases/prototypes/GetPrototypeUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using YourProject.Domain.Entities;
using YourProject.Domain.Errors;
using YourProject.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace YourProject.Application.UseCases.Prototypes;

/// <summary>
/// Use case for retrieving a prototype by their id.
/// </summary>
public abstract class GetPrototypeUseCase
{
    /// <summary>
    /// Command to get a prototype by their id.
    /// </summary>
    public class Command : ICommand<Result<Prototype>>
    {
        /// <summary>
        /// Gets or sets the Id of the prototype to retrieve.
        /// </summary>
        public Guid Id { set; get; }
    }

    /// <summary>
    /// Handler for executing the GetPrototype command.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger) : ICommandHandler<Command, Result<Prototype>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to get a prototype by their id.
        /// </summary>
        public async Task<Result<Prototype>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                var prototype = await _uoW.Prototypes.GetAsync(request.Id, ct);

                if (prototype is null)
                {
                    _logger.LogWarning("Prototype not found with Id: {Id}", request.Id);
                    return Result.Fail(PrototypeStandardErrors.PrototypeNotFound(request.Id));
                }

                _logger.LogInformation("Prototype retrieved successfully with Id: {Id}", request.Id);
                return Result.Ok(prototype);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving prototype with Id: {Id}", request.Id);
                return Result.Fail(new Error(ex.Message));
            }
        }
    }
}
```

**Differences from Create:**
- No transaction needed (read-only)
- Includes logging for observability
- Uses custom error type (`PrototypeNotFoundError`)

**Checkpoint:** Build the project.

---

### Step 3.4: Create GetManyAndCountPrototypesUseCase

**Location:** `src/YourProject.Application/usecases/prototypes/GetManyAndCountPrototypesUseCase.cs`

```csharp
using FastEndpoints;
using YourProject.Domain.Daos;
using YourProject.Domain.Entities;
using YourProject.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace YourProject.Application.UseCases.Prototypes;

/// <summary>
/// Use case for retrieving multiple prototypes and their total count.
/// </summary>
public abstract class GetManyAndCountPrototypesUseCase
{
    /// <summary>
    /// Command for retrieving prototypes with an optional query filter.
    /// </summary>
    public class Command : ICommand<GetManyAndCountResult<PrototypeDao>>
    {
        /// <summary>
        /// Gets or sets the query string used for filtering prototypes.
        /// </summary>
        public string? Query { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handles the execution of the <see cref="Command"/> to retrieve prototypes and their count.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger) : ICommandHandler<Command, GetManyAndCountResult<PrototypeDao>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to retrieve prototypes and their count, with optional filtering.
        /// </summary>
        public async Task<GetManyAndCountResult<PrototypeDao>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation("Executing GetManyAndCountPrototypesUseCase with query: {Query}", command.Query);

                var prototypes = await _uoW.PrototypeDaos.GetManyAndCountAsync(
                    command.Query,
                    nameof(Prototype.Number),
                    ct);

                _logger.LogInformation("End GetManyAndCountPrototypesUseCase with total prototypes: {TotalPrototypes}", prototypes.Count);
                _uoW.Commit();
                return prototypes;
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
- Uses `PrototypeDao` (not entity) for read optimization
- Returns `GetManyAndCountResult<T>` with items and count
- Supports filtering via query string
- Default ordering by `Number` field

**Checkpoint:** Build the project.

---

### Step 3.5: Create UpdatePrototypeUseCase

**Location:** `src/YourProject.Application/usecases/prototypes/UpdatePrototypeUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using YourProject.Application.Common;
using YourProject.Domain.Entities;
using YourProject.Domain.Exceptions;
using YourProject.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace YourProject.Application.UseCases.Prototypes;

/// <summary>
/// Use case for updating an existing prototype.
/// </summary>
public abstract class UpdatePrototypeUseCase
{
    /// <summary>
    /// Command to update a prototype with new details.
    /// </summary>
    public class Command : ICommand<Result<Prototype>>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the prototype to update.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the prototype number as a string.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the prototype was created or recorded.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the prototype.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the prototype entity.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for the UpdatePrototype command.
    /// </summary>
    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<Prototype>>
    {
        private readonly IUnitOfWork _uoW = uoW;

        /// <summary>
        /// Executes the command to update an existing prototype.
        /// </summary>
        public async Task<Result<Prototype>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                var prototype = await _uoW.Prototypes.UpdateAsync(
                    command.Id,
                    command.Number,
                    command.IssueDate,
                    command.ExpirationDate,
                    command.Status,
                    ct
                );
                _uoW.Commit();
                return Result.Ok(prototype);
            }
            catch (ResourceNotFoundException rnfex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(rnfex.Message).CausedBy(rnfex));
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid prototype data";
                return Result.Fail(new Error(firstErrorMessage)
                    .CausedBy(idex)
                    .WithMetadata("ValidationErrors", idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

**Exception Handling:**
- `ResourceNotFoundException` - Entity not found
- `InvalidDomainException` - Validation failure
- `DuplicatedDomainException` - Duplicate number
- Generic `Exception` - Unexpected errors

**Checkpoint:** Build the project. Application layer is now complete.

---

## Phase 4: WebApi Layer

The WebApi layer exposes HTTP endpoints using FastEndpoints.

### Step 4.1: Create PrototypeDto

**Location:** `src/YourProject.WebApi/dtos/PrototypeDto.cs`

```csharp
namespace YourProject.WebApi.Dtos;

/// <summary>
/// Represents a data transfer object (DTO) for a prototype entity.
/// </summary>
public class PrototypeDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the number associated with the entity.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Gets or sets the date when the issue was created or recorded.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the item.
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the operation.
    /// </summary>
    public string? Status { get; set; }
}
```

**Checkpoint:** Build the project.

---

### Step 4.2: Create Request/Response Models

#### CreatePrototypeModel

**Location:** `src/YourProject.WebApi/features/prototypes/models/CreatePrototypeModel.cs`

```csharp
using YourProject.WebApi.Dtos;

namespace YourProject.WebApi.Features.Prototypes.Models;

public class CreatePrototypeModel
{
    /// <summary>
    /// Represents the request data used to create a new prototype
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the number of prototype as a string.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the issue was created or recorded.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the item.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the operation.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    public class Response
    {
        /// <summary>
        /// Gets or sets the data of the newly created prototype.
        /// </summary>
        public PrototypeDto Prototype { get; set; } = new PrototypeDto();
    }
}
```

#### GetPrototypeModel

**Location:** `src/YourProject.WebApi/features/prototypes/models/GetPrototypeModel.cs`

```csharp
using YourProject.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace YourProject.WebApi.Features.Prototypes.Models;

/// <summary>
/// Data model for retrieving Application prototype
/// </summary>
public class GetPrototypeModel
{
    /// <summary>
    /// Represents the request data used to get Application prototype
    /// </summary>
    public class Request
    {
        /// <summary>
        ///  Gets or sets the id of the prototype to retrieve.
        /// </summary>
        [FromRoute]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Represents an Application prototype returned in the response.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the Prototype for result
        /// </summary>
        public PrototypeDto Prototype { get; set; } = new PrototypeDto();
    }
}
```

#### GetManyAndCountPrototypesModel

**Location:** `src/YourProject.WebApi/features/prototypes/models/GetManyAndCountPrototypesModel.cs`

```csharp
namespace YourProject.WebApi.Features.Prototypes.Models;

/// <summary>
/// Represents the model for retrieving multiple prototypes along with a count of the total results.
/// </summary>
public class GetManyAndCountPrototypesModel
{
    /// <summary>
    /// Represents a request containing a query string.
    /// </summary>
    public class Request
    {
        public string? Query { get; set; }
    }
}
```

#### UpdatePrototypeModel

**Location:** `src/YourProject.WebApi/features/prototypes/models/UpdatePrototypeModel.cs`

```csharp
using YourProject.WebApi.Dtos;

namespace YourProject.WebApi.Features.Prototypes.Models;

/// <summary>
/// Model for updating an existing prototype.
/// </summary>
public class UpdatePrototypeModel
{
    /// <summary>
    /// Represents the request data used to update an existing prototype.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the unique identifier of the prototype to update.
        /// This value is extracted from the route parameter.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the prototype number as a string.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date when the prototype was created or recorded.
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the prototype.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the current status of the prototype entity.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response data returned after updating a prototype.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Gets or sets the data of the updated prototype.
        /// </summary>
        public PrototypeDto Prototype { get; set; } = new PrototypeDto();
    }
}
```

**Checkpoint:** Build the project.

---

### Step 4.3: Create PrototypeMappingProfile

**Location:** `src/YourProject.WebApi/mappingprofiles/PrototypeMappingProfile.cs`

```csharp
using AutoMapper;
using YourProject.Application.UseCases.Prototypes;
using YourProject.Domain.Daos;
using YourProject.Domain.Entities;
using YourProject.WebApi.Dtos;
using YourProject.WebApi.Features.Prototypes.Models;

namespace YourProject.WebApi.MappingProfiles;

public class PrototypeMappingProfile : Profile
{
    public PrototypeMappingProfile()
    {
        // DAO to DTO
        CreateMap<PrototypeDao, PrototypeDto>();

        // GetManyAndCount mappings
        CreateMap<GetManyAndCountResultDto<PrototypeDao>, GetManyAndCountResultDto<PrototypeDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
        CreateMap<GetManyAndCountPrototypesModel.Request, GetManyAndCountPrototypesUseCase.Command>();

        // Entity to DTO
        CreateMap<Prototype, PrototypeDto>();

        // Create mappings
        CreateMap<CreatePrototypeModel.Request, CreatePrototypeUseCase.Command>();
        CreateMap<Prototype, CreatePrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));

        // Get mappings
        CreateMap<GetPrototypeModel.Request, GetPrototypeUseCase.Command>();
        CreateMap<Prototype, GetPrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));

        // Update mappings
        CreateMap<UpdatePrototypeModel.Request, UpdatePrototypeUseCase.Command>();
        CreateMap<Prototype, UpdatePrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));
    }
}
```

**Mapping Strategy:**
- Request → Use Case Command
- Entity/DAO → DTO
- Entity → Response (wrapping DTO)

**Checkpoint:** Build the project. The step-by-step guide is complete and saved successfully. Let me now update the main README files to reflect this progress.

---

## Verification Checklist

After completing all phases, verify:

### Domain Layer
- [ ] PrototypeDao created
- [ ] PrototypeValidator with all rules
- [ ] Prototype entity with validator
- [ ] IPrototypeRepository interface

### Infrastructure Layer
- [ ] PrototypeMapper for table mapping
- [ ] PrototypeDaoMapper for view mapping
- [ ] NHPrototypeRepository implementation

### Application Layer
- [ ] CreatePrototypeUseCase
- [ ] GetPrototypeUseCase
- [ ] GetManyAndCountPrototypesUseCase
- [ ] UpdatePrototypeUseCase

### WebApi Layer
- [ ] PrototypeDto
- [ ] All request/response models
- [ ] PrototypeMappingProfile
- [ ] All 4 endpoints

### Testing
- [ ] Application starts
- [ ] Swagger accessible
- [ ] All CRUD operations work
- [ ] Error scenarios handled

---

## Common Pitfalls

1. **Missing mapper registration** - Ensure NHibernate mappers are registered
2. **Transaction not started** - Always call BeginTransaction()
3. **AutoMapper not configured** - Register PrototypeMappingProfile
4. **Route parameters not binding** - Use Route<T>() or [FromRoute]
5. **Virtual properties** - Mark all properties as virtual for NHibernate
6. **Validation not running** - Repository must call IsValid()
7. **Case-sensitive duplicates** - Use ToLowerInvariant()
8. **DateTime timezone issues** - Use UTC consistently

---

## Next Steps

1. Add unit tests for each layer
2. Add integration tests
3. Implement Delete operation
4. Add pagination and advanced filtering
5. Add audit trail
6. Implement caching

---

## Related Guides

- [CRUD Feature Overview](README.md)
- [WebApi Layer Guide](../../webapi-layer/README.md)
- [Application Layer Guide](../../application-layer/README.md)
- [Domain Layer Guide](../../domain-layer/README.md)