# Complex Feature with Entity Relationships - Step-by-Step Implementation

> **Version:** 1.0.0
> **Last Updated:** 2025-01-15
> **Estimated Time:** 2-3 hours
> **Difficulty:** Advanced

---

## Introduction

This guide walks you through implementing a **complex feature with entity relationships** in Clean Architecture using .NET 9.0. You'll build a complete **User-Role management system** with a **many-to-many relationship**.

### What You'll Build

A fully functional User-Role feature with:
- **2 entities** with many-to-many relationship
- **Join table** for relationship management
- **CRUD operations** for both entities
- **Relationship management** endpoints (add/remove roles)
- **Navigation properties** with lazy/eager loading
- **Cascade operations** for automatic relationship updates
- **Flattened DTOs** for API responses

### Files to Create

**Total:** ~18-20 files across 4 layers

| Layer | Files | Purpose |
|-------|-------|---------|
| **Domain** | 6 files | Entities, validators, repository interfaces |
| **Infrastructure** | 6 files | NHibernate mappers, repositories, migrations |
| **Application** | 4-6 files | Use cases for CRUD and relationship management |
| **WebApi** | 6-8 files | DTOs, models, mapping profiles, endpoints |

---

## Prerequisites

### Tools Required
- ✅ .NET 9.0 SDK
- ✅ Visual Studio 2022 / VS Code / Rider
- ✅ PostgreSQL / SQL Server
- ✅ Postman / Swagger UI (for testing)

### NuGet Packages Required
```xml
<PackageReference Include="FastEndpoints" Version="7.0.1" />
<PackageReference Include="NHibernate" Version="5.5.2" />
<PackageReference Include="FluentValidation" Version="12.0.0" />
<PackageReference Include="FluentResults" Version="4.0.0" />
<PackageReference Include="AutoMapper" Version="14.0.0" />
<PackageReference Include="FluentMigrator" Version="5.2.0" />
```

### Knowledge Required
- ✅ Understanding of [CRUD Features](../crud-feature/README.md)
- ✅ Clean Architecture concepts
- ✅ NHibernate ORM basics
- ✅ Entity relationships (one-to-many, many-to-many)
- ✅ FluentValidation
- ✅ FluentMigrator

---

## Overview

### Implementation Phases

We'll implement this feature in **6 phases**:

1. **Phase 1: Domain Layer** - Entities with navigation properties
2. **Phase 2: Infrastructure Layer** - Mappers, repositories, migrations
3. **Phase 3: Application Layer** - Use cases (CRUD + relationships)
4. **Phase 4: WebApi Layer** - DTOs, endpoints, mapping profiles
5. **Phase 5: Testing** - Manual testing via Swagger
6. **Phase 6: Database Setup** - Migrations and data

### File Creation Order

| # | File | Layer | Lines | Time |
|---|------|-------|-------|------|
| 1 | Role.cs | Domain | ~40 | 5 min |
| 2 | User.cs | Domain | ~60 | 10 min |
| 3 | RoleValidator.cs | Domain | ~25 | 5 min |
| 4 | UserValidator.cs | Domain | ~35 | 5 min |
| 5 | IRoleRepository.cs | Domain | ~20 | 5 min |
| 6 | IUserRepository.cs | Domain | ~30 | 5 min |
| 7 | RoleMapper.cs | Infrastructure | ~35 | 10 min |
| 8 | UserMapper.cs | Infrastructure | ~70 | 15 min |
| 9 | M020CreateRolesTable.cs | Infrastructure | ~45 | 10 min |
| 10 | M024CreateUsersTable.cs | Infrastructure | ~70 | 15 min |
| 11 | NHRoleRepository.cs | Infrastructure | ~50 | 10 min |
| 12 | NHUserRepository.cs | Infrastructure | ~60 | 10 min |
| 13 | CreateUserUseCase.cs | Application | ~110 | 15 min |
| 14 | GetUserUseCase.cs | Application | ~60 | 10 min |
| 15 | GetManyAndCountUsersUseCase.cs | Application | ~55 | 10 min |
| 16 | AddUsersToRoleUseCase.cs | Application | ~80 | 15 min |
| 17 | RemoveUserFromRoleUseCase.cs | Application | ~80 | 15 min |
| 18 | UserDto.cs | WebApi | ~30 | 5 min |
| 19 | RoleDto.cs | WebApi | ~20 | 5 min |
| 20 | UserMappingProfile.cs | WebApi | ~30 | 10 min |
| 21 | CreateUserModel.cs | WebApi | ~40 | 5 min |
| 22 | GetUserModel.cs | WebApi | ~35 | 5 min |
| 23 | CreateUserEndpoint.cs | WebApi | ~70 | 15 min |
| 24 | GetUserEndpoint.cs | WebApi | ~65 | 15 min |
| 25 | GetManyAndCountUsersEndpoint.cs | WebApi | ~60 | 15 min |

**Total:** ~1,185 lines of code, 2-3 hours

---

## Phase 1: Domain Layer

### Step 1.1: Create the Role Entity

**File:** `src/Domain/entities/Role.cs`

```csharp
using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

namespace hashira.stone.backend.domain.entities;

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role : AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the name of the role.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public Role()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with the specified name.
    /// </summary>
    /// <param name="name">The role name</param>
    public Role(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Get the validator for the Role entity.
    /// </summary>
    public override IValidator GetValidator()
        => new RoleValidator();
}
```

**Key Points:**
- ✅ Inherits from `AbstractDomainObject` (Id, CreationDate, etc.)
- ✅ `virtual` keyword on properties for NHibernate lazy loading
- ✅ Parameterless constructor for NHibernate
- ✅ Business constructor with required parameters
- ✅ Returns validator via `GetValidator()`

**Checkpoint:** Verify the file compiles without errors.

---

### Step 1.2: Create the User Entity with Navigation Property

**File:** `src/Domain/entities/User.cs`

```csharp
using FluentValidation;
using hashira.stone.backend.domain.entities.validators;

namespace hashira.stone.backend.domain.entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : AbstractDomainObject
{
    /// <summary>
    /// Gets or sets the email of the user.
    /// </summary>
    public virtual string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles assigned to the user.
    /// </summary>
    /// <remarks>
    /// Navigation property for many-to-many relationship with Role entity.
    /// This collection is lazy-loaded by default.
    /// </remarks>
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    /// <summary>
    /// The user identifier from the identity provider (e.g., Auth0).
    /// </summary>
    /// <remarks>
    /// Used to get the user from the Auth0 identity repository.
    /// Not mapped to the database.
    /// </remarks>
    public virtual string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    public User()
    {
        Roles = new List<Role>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="name">User full name</param>
    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Roles = new List<Role>();
    }

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    /// <returns>Validator instance</returns>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

**Key Points:**
- ✅ **Navigation property** `IList<Role> Roles` for many-to-many relationship
- ✅ **Initialized to empty list** in constructors to prevent null reference exceptions
- ✅ **All properties marked as virtual** for NHibernate
- ✅ `UserId` for Auth0 integration (not mapped to database)

**Checkpoint:** Verify navigation property is `IList<Role>` and initialized.

---

### Step 1.3: Create the RoleValidator

**File:** `src/Domain/entities/validators/RoleValidator.cs`

```csharp
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Validator for Role entity.
/// </summary>
public class RoleValidator : AbstractValidator<Role>
{
    public RoleValidator()
    {
        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name")
            .MaximumLength(100)
            .WithMessage("The [Name] cannot exceed 100 characters")
            .WithErrorCode("Name_TooLong");
    }
}
```

**Key Points:**
- ✅ Simple validation for Role's own properties
- ✅ No complex rules needed for roles

**Checkpoint:** Build the project to verify validator compiles.

---

### Step 1.4: Create the UserValidator

**File:** `src/Domain/entities/validators/UserValidator.cs`

```csharp
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

/// <summary>
/// Validator for User entity.
/// </summary>
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Email] cannot be null or empty")
            .WithErrorCode("Email")
            .EmailAddress()
            .WithMessage("The [Email] is not a valid email address")
            .WithErrorCode("Email_InvalidDomain")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithMessage("The [Email] must contain a valid domain format (e.g. user@domain.com)")
            .WithErrorCode("Email_InvalidFormat");

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("The [Name] cannot be null or empty")
            .WithErrorCode("Name")
            .MaximumLength(200)
            .WithMessage("The [Name] cannot exceed 200 characters")
            .WithErrorCode("Name_TooLong");

        // IMPORTANT: DO NOT validate navigation properties
        // Roles collection is managed through dedicated use cases
    }
}
```

**Key Points:**
- ✅ Validates only User's own properties
- ✅ **Does NOT validate Roles collection** - relationships are managed separately
- ✅ Email validation with format check

**Checkpoint:** Ensure no validation rules for `Roles` property.

---

### Step 1.5: Create the IRoleRepository Interface

**File:** `src/Domain/interfaces/repositories/IRoleRepository.cs`

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="Role"/> entities.
/// </summary>
public interface IRoleRepository : IRepository<Role, Guid>
{
    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    /// <param name="name">The role name to search for.</param>
    /// <returns>The role entity if found; otherwise, null.</returns>
    Task<Role?> GetByNameAsync(string name);
}
```

**Key Points:**
- ✅ Extends `IRepository<Role, Guid>` for base CRUD operations
- ✅ Custom method `GetByNameAsync` for business-specific queries
- ✅ No methods for managing relationships

**Checkpoint:** Verify interface extends IRepository correctly.

---

### Step 1.6: Create the IUserRepository Interface

**File:** `src/Domain/interfaces/repositories/IUserRepository.cs`

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="User"/> entities.
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Creates a new user with the specified email.
    /// </summary>
    /// <param name="email">The email address for the new user.</param>
    /// <param name="name">The name of the new user.</param>
    /// <returns>The newly created user entity.</returns>
    Task<User> CreateAsync(string email, string name);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user entity if found; otherwise, null.</returns>
    Task<User?> GetByEmailAsync(string email);
}
```

**Key Points:**
- ✅ Custom `CreateAsync` method for user creation
- ✅ Custom `GetByEmailAsync` for queries
- ✅ No methods for managing relationships (handled through entity manipulation)

**Checkpoint:** Domain layer complete. Build the project to verify all files compile.

---

## Phase 2: Infrastructure Layer

### Step 2.1: Create the RoleMapper

**File:** `src/Infrastructure/nhibernate/mappers/RoleMapper.cs`

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="Role"/> entity.
/// </summary>
public class RoleMapper : ClassMapping<Role>
{
    public RoleMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("roles");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        // Note: No back-reference to Users
        // This is a unidirectional relationship from User → Role
    }
}
```

**Key Points:**
- ✅ Simple mapper with no relationship configuration
- ✅ `Name` marked as unique
- ✅ Unidirectional relationship (User → Role, not Role → Users)

**Checkpoint:** Verify mapper compiles and table name is "roles".

---

### Step 2.2: Create the UserMapper with Relationship Configuration

**File:** `src/Infrastructure/nhibernate/mappers/UserMapper.cs`

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="User"/> entity.
/// </summary>
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        // IMPORTANT: UserId is NOT mapped to database
        // It's used only for Auth0 integration

        // Many-to-Many Relationship Configuration
        Bag(x => x.Roles, collectionMap =>
        {
            collectionMap.Schema(AppSchemaResource.SchemaName);
            collectionMap.Table("user_in_roles");          // Join table name
            collectionMap.Key(k => k.Column("user_id"));    // FK to users table
            collectionMap.Cascade(Cascade.All);             // Cascade all operations
            collectionMap.Inverse(false);                   // User owns the relationship
        },
        relationMap => relationMap.ManyToMany(m =>
        {
            m.Column("role_id");                            // FK to roles table
            m.Class(typeof(Role));                          // Related entity type
        }));
    }
}
```

**Key Points:**
- ✅ **Bag()** method for `IList<T>` collections
- ✅ **Table("user_in_roles")** - join table name
- ✅ **Key("user_id")** - foreign key to users table
- ✅ **ManyToMany("role_id")** - foreign key to roles table
- ✅ **Cascade.All** - enables automatic cascade operations
- ✅ **Inverse(false)** - User owns the relationship (changes saved from User side)
- ✅ **UserId NOT mapped** to database

**Checkpoint:** Verify `Bag()` configuration with correct table and column names.

---

### Step 2.3: Create Roles Table Migration

**File:** `src/Infrastructure/migrations/M020CreateRolesTable.cs`

```csharp
using FluentMigrator;

namespace hashira.stone.backend.migrations;

[Migration(20)]
public class M020CreateRolesTable : Migration
{
    private readonly string _rolesTableName = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Down()
    {
        Delete.Table(_rolesTableName)
            .InSchema(_schemaName);
    }

    public override void Up()
    {
        // Create roles table
        Create.Table(_rolesTableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("name").AsString(100).NotNullable().Unique();

        // Add index for name (performance optimization)
        Create.Index($"ix_{_rolesTableName}_name")
              .OnTable(_rolesTableName)
              .InSchema(_schemaName)
              .OnColumn("name")
              .Ascending()
              .WithOptions().Unique();

        // Insert default roles
        Insert.IntoTable(_rolesTableName)
              .InSchema(_schemaName)
              .Row(new { id = Guid.NewGuid(), name = "Admin" })
              .Row(new { id = Guid.NewGuid(), name = "Inspector" })
              .Row(new { id = Guid.NewGuid(), name = "Viewer" });
    }
}
```

**Key Points:**
- ✅ Simple table with id and name columns
- ✅ Unique constraint on name
- ✅ Unique index for performance
- ✅ Inserts default roles (Admin, Inspector, Viewer)

**Checkpoint:** Verify migration number (20) and table structure.

---

### Step 2.4: Create Users Table and Join Table Migration

**File:** `src/Infrastructure/migrations/M024CreateUsersTable.cs`

```csharp
using FluentMigrator;

namespace hashira.stone.backend.migrations;

[Migration(24)]
public class M024CreateUsersTable : Migration
{
    private readonly string _usersTableName = "users";
    private readonly string _userInRolesTableName = "user_in_roles";
    private readonly string _rolesTableName = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Down()
    {
        // IMPORTANT: Delete join table first (foreign key constraints)
        Delete.Table(_userInRolesTableName)
            .InSchema(_schemaName);

        Delete.Table(_usersTableName)
            .InSchema(_schemaName);
    }

    public override void Up()
    {
        // Create users table
        Create.Table(_usersTableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("email").AsString(255).NotNullable().Unique()
              .WithColumn("name").AsString(200).NotNullable();

        // Create user_in_roles junction table for many-to-many relationship
        Create.Table(_userInRolesTableName)
              .InSchema(_schemaName)
              .WithColumn("user_id").AsGuid().NotNullable()
              .WithColumn("role_id").AsGuid().NotNullable();

        // Add composite primary key (ensures uniqueness of user-role pairs)
        Create.PrimaryKey($"pk_{_userInRolesTableName}")
              .OnTable(_userInRolesTableName)
              .WithSchema(_schemaName)
              .Columns("user_id", "role_id");

        // Add foreign key to users table
        Create.ForeignKey($"fk_{_userInRolesTableName}_user_id")
              .FromTable(_userInRolesTableName)
              .InSchema(_schemaName)
              .ForeignColumn("user_id")
              .ToTable(_usersTableName)
              .InSchema(_schemaName)
              .PrimaryColumn("id")
              .OnDelete(System.Data.Rule.Cascade);  // Delete relationships when user is deleted

        // Add foreign key to roles table
        Create.ForeignKey($"fk_{_userInRolesTableName}_role_id")
              .FromTable(_userInRolesTableName)
              .InSchema(_schemaName)
              .ForeignColumn("role_id")
              .ToTable(_rolesTableName)
              .InSchema(_schemaName)
              .PrimaryColumn("id")
              .OnDelete(System.Data.Rule.Cascade);  // Delete relationships when role is deleted

        // Add index for email (performance optimization)
        Create.Index($"ix_{_usersTableName}_email")
              .OnTable(_usersTableName)
              .InSchema(_schemaName)
              .OnColumn("email")
              .Ascending()
              .WithOptions().Unique();
    }
}
```

**Key Points:**
- ✅ **Users table** with id, email, name
- ✅ **Join table** `user_in_roles` with user_id and role_id
- ✅ **Composite primary key** on (user_id, role_id) ensures uniqueness
- ✅ **Two foreign keys** pointing to users and roles tables
- ✅ **Cascade delete** - relationships deleted when user or role is deleted
- ✅ **Delete order** in Down() - join table first, then users table
- ✅ **Unique index** on email

**Checkpoint:** Verify composite primary key and foreign key configurations.

---

### Step 2.5: Create NHRoleRepository

**File:** `src/Infrastructure/nhibernate/NHRoleRepository.cs`

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementation of the <see cref="IRoleRepository"/> using NHibernate.
/// </summary>
public class NHRoleRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<Role, Guid>(session, serviceProvider), IRoleRepository
{
    /// <summary>
    /// Get a role by its name
    /// </summary>
    /// <param name="name">The role name</param>
    /// <returns>The role with the specified name, or null if not found</returns>
    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _session.Query<Role>()
            .Where(r => r.Name == name)
            .SingleOrDefaultAsync();
    }
}
```

**Key Points:**
- ✅ Inherits from `NHRepository<Role, Guid>` for base operations
- ✅ Implements `GetByNameAsync` for custom query

**Checkpoint:** Build and verify repository compiles.

---

### Step 2.6: Create NHUserRepository

**File:** `src/Infrastructure/nhibernate/NHUserRepository.cs`

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementation of the <see cref="IUserRepository"/> using NHibernate.
/// </summary>
public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Create a new user with the specified email
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="name">User name</param>
    /// <returns>The created user</returns>
    public async Task<User> CreateAsync(string email, string name)
    {
        // Create new user entity
        var user = new User(email, name);

        // Validate entity
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // Check for duplicates
        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"A user with the email '{email}' already exists.");

        // Add to session
        await AddAsync(user);

        // Flush the session to ensure the user is saved
        FlushWhenNotActiveTransaction();

        return user;
    }

    /// <summary>
    /// Get a user by their email address
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>The user with the specified email, or null if not found</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();

        // Note: Roles are lazy-loaded by default
        // To eager load roles, use: .Fetch(u => u.Roles).SingleOrDefaultAsync();
    }
}
```

**Key Points:**
- ✅ Custom `CreateAsync` with validation and duplicate check
- ✅ `GetByEmailAsync` for business queries
- ✅ **Roles lazy-loaded** by default (can be changed to eager loading)

**Checkpoint:** Infrastructure layer complete. Build the project.

---

## Phase 3: Application Layer

### Step 3.1: Create Error Types (if not exists)

**File:** `src/Domain/errors/UserErrors.cs`

```csharp
using FluentResults;

namespace hashira.stone.backend.domain.errors;

/// <summary>
/// User-specific error types.
/// </summary>
public static class UserErrors
{
    public static IError UserNotFound(string email) =>
        new UserNotFoundError($"User with email '{email}' was not found.");

    public static IError UserAlreadyExists(string email) =>
        new Error($"User with email '{email}' already exists.");
}

/// <summary>
/// Error type for user not found scenarios.
/// </summary>
public class UserNotFoundError : Error
{
    public UserNotFoundError(string message) : base(message) { }
}
```

**File:** `src/Domain/errors/RoleErrors.cs`

```csharp
using FluentResults;

namespace hashira.stone.backend.domain.errors;

/// <summary>
/// Role-specific error types.
/// </summary>
public static class RoleErrors
{
    public static IError RoleNotFound(string name) =>
        new RoleNotFoundError($"Role '{name}' was not found.");
}

/// <summary>
/// Error type for role not found scenarios.
/// </summary>
public class RoleNotFoundError : Error
{
    public RoleNotFoundError(string message) : base(message) { }
}
```

**Checkpoint:** Verify error classes compile.

---

### Step 3.2: Create CreateUserUseCase

**File:** `src/Application/usecases/users/CreateUserUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.application.common;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.interfaces.services;
using System.Security.Cryptography;
using System.Text.Json;

namespace hashira.stone.backend.application.usecases.users;

public abstract class CreateUserUseCase
{
    /// <summary>
    /// Command to create a new user.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IIdentityService _identityService = identityService;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // Create user in Auth0 identity provider
                var password = GenerateRandomPassword();
                var auth0User = _identityService.Create(command.Email, command.Name, password);

                // Create user in database
                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

                // IMPORTANT: User.Roles collection is empty initially
                // Roles are assigned through separate use cases (AddUsersToRoleUseCase)

                _uoW.Commit();
                return Result.Ok(user);
            }
            catch (HttpRequestException httpEx)
            {
                _uoW.Rollback();
                return Result.Fail(new Error($"Error creating user in authentication service")
                    .CausedBy(httpEx));
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstError = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";
                return Result.Fail(new Error(firstError).CausedBy(idex));
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

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        const int length = 12;
        var passwordChars = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        for (int i = 0; i < length; i++)
            passwordChars[i] = chars[bytes[i] % chars.Length];
        return new string(passwordChars);
    }
}
```

**Key Points:**
- ✅ Creates user in both Auth0 and database
- ✅ **Roles NOT assigned here** - separate use cases handle relationship management
- ✅ Transaction management with rollback
- ✅ Multiple exception types handled

**Checkpoint:** Verify Auth0 integration and error handling.

---

### Step 3.3: Create GetUserUseCase

**File:** `src/Application/usecases/users/GetUserUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.users;

/// <summary>
/// Use case for retrieving a user by their email.
/// </summary>
public class GetUserUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string UserName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);

                // Roles are lazy-loaded when accessed (user.Roles)
                // If eager loading is configured in repository, roles are already loaded

                return user == null
                    ? Result.Fail(UserErrors.UserNotFound(request.UserName))
                    : Result.Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user: {Email}", request.UserName);
                return Result.Fail("Error retrieving user");
            }
        }
    }
}
```

**Key Points:**
- ✅ Simple retrieval use case
- ✅ Roles lazy-loaded by default

**Checkpoint:** Verify error handling with UserErrors.

---

### Step 3.4: Create GetManyAndCountUsersUseCase

**File:** `src/Application/usecases/users/GetManyAndCountUsersUseCase.cs`

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.users;

public abstract class GetManyAndCountUsersUseCase
{
    public class Command : ICommand<GetManyAndCountResult<User>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation("Getting users with query: {Query}", command.Query);

                var result = await _uoW.Users.GetManyAndCountAsync(
                    command.Query,
                    nameof(User.Name),
                    ct);

                _logger.LogInformation("Found {Count} users", result.Count);
                _uoW.Commit();
                return result;
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

**Checkpoint:** Verify GetManyAndCountAsync method exists in IRepository.

---

### Step 3.5: Create AddUsersToRoleUseCase (Relationship Management)

**File:** `src/Application/usecases/roles/AddUsersToRoleUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.roles;

public class AddUsersToRoleUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // Get user by email
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);
                if (user == null)
                    return Result.Fail(UserErrors.UserNotFound(request.UserName));

                // Get role by name
                var role = await _uoW.Roles.GetByNameAsync(request.RoleName);
                if (role == null)
                    return Result.Fail(RoleErrors.RoleNotFound(request.RoleName));

                // Check if user already has this role
                if (user.Roles.Any(r => r.Id == role.Id))
                {
                    _logger.LogWarning("User {Email} already has role {Role}",
                        request.UserName, request.RoleName);
                    return Result.Ok(user);
                }

                // IMPORTANT: Add role to user's collection
                user.Roles.Add(role);

                // Update user - cascade will save the relationship to join table
                await _uoW.Users.UpdateAsync(user);

                _uoW.Commit();

                _logger.LogInformation("Added role {Role} to user {Email}",
                    request.RoleName, request.UserName);

                return Result.Ok(user);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error adding role {Role} to user {Email}",
                    request.RoleName, request.UserName);
                return Result.Fail($"Error adding role to user: {ex.Message}");
            }
        }
    }
}
```

**Key Points:**
- ✅ Retrieves both user and role
- ✅ **Adds role to user.Roles collection**
- ✅ **Updates user** - cascade automatically saves relationship to join table
- ✅ Checks for duplicates
- ✅ Transaction management

**Checkpoint:** Verify `user.Roles.Add(role)` and cascade operations.

---

### Step 3.6: Create RemoveUserFromRoleUseCase (Relationship Management)

**File:** `src/Application/usecases/roles/RemoveUserFromRoleUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.roles;

public class RemoveUserFromRoleUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // Get user by email
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);
                if (user == null)
                    return Result.Fail(UserErrors.UserNotFound(request.UserName));

                // Get role by name
                var role = await _uoW.Roles.GetByNameAsync(request.RoleName);
                if (role == null)
                    return Result.Fail(RoleErrors.RoleNotFound(request.RoleName));

                // Find role in user's collection
                var roleToRemove = user.Roles.FirstOrDefault(r => r.Id == role.Id);
                if (roleToRemove == null)
                {
                    _logger.LogWarning("User {Email} does not have role {Role}",
                        request.UserName, request.RoleName);
                    return Result.Ok(user);
                }

                // IMPORTANT: Remove role from user's collection
                user.Roles.Remove(roleToRemove);

                // Update user - cascade will delete relationship from join table
                await _uoW.Users.UpdateAsync(user);

                _uoW.Commit();

                _logger.LogInformation("Removed role {Role} from user {Email}",
                    request.RoleName, request.UserName);

                return Result.Ok(user);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error removing role {Role} from user {Email}",
                    request.RoleName, request.UserName);
                return Result.Fail($"Error removing role from user: {ex.Message}");
            }
        }
    }
}
```

**Key Points:**
- ✅ Finds role in collection
- ✅ **Removes role from collection**
- ✅ **Updates user** - cascade deletes relationship from join table

**Checkpoint:** Application layer complete. Build the project.

---

## Phase 4: WebApi Layer

### Step 4.1: Create UserDto with Flattened Related Data

**File:** `src/WebApi/dtos/UserDto.cs`

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user.
    /// Contains role names (not full Role objects).
    /// This is a flattened representation for API responses.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

**Key Points:**
- ✅ **Flattened** `Roles` property - contains role names, not full objects
- ✅ Simplified API response

**Checkpoint:** Verify Roles is `IEnumerable<string>`, not `IEnumerable<Role>`.

---

### Step 4.2: Create RoleDto

**File:** `src/WebApi/dtos/RoleDto.cs`

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for Role information
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**Checkpoint:** Simple DTO with Id and Name.

---

### Step 4.3: Create UserMappingProfile

**File:** `src/WebApi/mappingprofiles/UserMappingProfile.cs`

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO mapping with flattened Roles
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));

        // Entity → Response models
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        CreateMap<User, GetUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        // Request → Command mapping
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
        CreateMap<GetUserModel.Request, GetUserUseCase.Command>();
    }
}
```

**Key Points:**
- ✅ **Custom mapping** for Roles: `src => src.Roles.Select(r => r.Name)`
- ✅ Maps `IList<Role>` to `IEnumerable<string>`

**Checkpoint:** Verify custom ForMember for Roles property.

---

### Step 4.4: Create Request/Response Models

**File:** `src/WebApi/features/users/models/CreateUserModel.cs`

```csharp
namespace hashira.stone.backend.webapi.features.users.models;

public static class CreateUserModel
{
    public class Request
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Response
    {
        public dtos.UserDto User { get; set; } = null!;
    }
}
```

**File:** `src/WebApi/features/users/models/GetUserModel.cs`

```csharp
namespace hashira.stone.backend.webapi.features.users.models;

public static class GetUserModel
{
    public class Request
    {
        public string UserName { get; set; } = string.Empty;
    }

    public class Response
    {
        public dtos.UserDto User { get; set; } = null!;
    }
}
```

**File:** `src/WebApi/features/users/models/GetManyAndCountModel.cs`

```csharp
namespace hashira.stone.backend.webapi.features.users.models;

public static class GetManyAndCountModel
{
    public class Request
    {
        public string? Query { get; set; }
    }
}
```

**Checkpoint:** Verify Request/Response classes.

---

### Step 4.5: Create CreateUserEndpoint

**File:** `src/WebApi/features/users/endpoint/CreateUserEndpoint.cs`

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Post("/users");
        Description(d => d
            .WithTags("Users")
            .WithName("CreateUser")
            .WithDescription("Create a new user")
            .Accepts<CreateUserModel.Request>("application/json")
            .Produces<CreateUserModel.Response>(201, "application/json")
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
    {
        var command = _mapper.Map<CreateUserUseCase.Command>(req);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            await HandleErrorAsync(r => r.Email, error.Message, HttpStatusCode.BadRequest, ct);
            return;
        }

        var response = _mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAtAsync<GetUserEndpoint>(
            new { UserName = result.Value.Email },
            response,
            cancellation: ct);
    }
}
```

**Key Points:**
- ✅ POST /users endpoint
- ✅ Returns 201 Created with Location header
- ✅ AutoMapper for Request → Command and Entity → Response

**Checkpoint:** Verify endpoint route and HTTP method.

---

### Step 4.6: Create GetUserEndpoint

**File:** `src/WebApi/features/users/endpoint/GetUserEndpoint.cs`

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.webapi.features.users.models;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users/{UserName}");
        Description(d => d
            .WithTags("Users")
            .WithName("GetUser")
            .WithDescription("Get a user by email")
            .Produces<GetUserModel.Response>(200, "application/json")
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var command = new GetUserUseCase.Command { UserName = req.UserName };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case UserNotFoundError e:
                    await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
                    break;
                default:
                    await HandleUnexpectedErrorAsync(error, ct);
                    break;
            }
            return;
        }

        // AutoMapper handles flattening Roles collection
        var response = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

**Key Points:**
- ✅ GET /users/{UserName} endpoint
- ✅ AutoMapper flattens Roles collection automatically
- ✅ Error handling with UserNotFoundError

**Checkpoint:** Verify route parameter and error handling.

---

### Step 4.7: Create GetManyAndCountUsersEndpoint

**File:** `src/WebApi/features/users/endpoint/GetManyAndCountUsersEndpoint.cs`

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.features.users.endpoint;

public class GetManyAndCountUsersEndpoint
    : BaseEndpoint<GetManyAndCountModel.Request, object>
{
    public override void Configure()
    {
        Get("/users");
        Description(d => d
            .WithTags("Users")
            .WithName("GetManyAndCountUsers")
            .WithDescription("Get a paginated list of users")
            .Produces(200, "application/json")
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetManyAndCountModel.Request req, CancellationToken ct)
    {
        var command = new GetManyAndCountUsersUseCase.Command { Query = req.Query };
        var result = await command.ExecuteAsync(ct);

        await Send.OkAsync(new
        {
            data = result.Data,
            count = result.Count
        }, ct);
    }
}
```

**Checkpoint:** WebApi layer complete. Build the project.

---

## Phase 5: Testing

### Step 5.1: Update Dependency Injection

**File:** `src/WebApi/Program.cs` or DI configuration

```csharp
// Register repositories
services.AddScoped<IRoleRepository, NHRoleRepository>();
services.AddScoped<IUserRepository, NHUserRepository>();

// AutoMapper will automatically discover UserMappingProfile
```

### Step 5.2: Update Unit of Work

**File:** `src/Domain/interfaces/repositories/IUnitOfWork.cs`

```csharp
public interface IUnitOfWork
{
    // ... existing repositories ...

    IRoleRepository Roles { get; }
    IUserRepository Users { get; }
}
```

**File:** `src/Infrastructure/nhibernate/NHUnitOfWork.cs`

```csharp
public class NHUnitOfWork : IUnitOfWork
{
    // ... existing code ...

    public IRoleRepository Roles { get; }
    public IUserRepository Users { get; }

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        // ... existing initializations ...

        Roles = new NHRoleRepository(session, serviceProvider);
        Users = new NHUserRepository(session, serviceProvider);
    }
}
```

**Checkpoint:** Build and verify DI configuration.

---

### Step 5.3: Manual Testing via Swagger

1. **Run migrations:**
   ```bash
   dotnet fm:up
   ```

2. **Start the application:**
   ```bash
   dotnet run
   ```

3. **Open Swagger UI:**
   Navigate to `https://localhost:5001/swagger`

4. **Test scenarios:**

#### Test 1: Create User
```http
POST /users
{
  "email": "john.doe@example.com",
  "name": "John Doe"
}
```

**Expected:** 201 Created with user data

#### Test 2: Get User
```http
GET /users/john.doe@example.com
```

**Expected:** 200 OK with user data and empty roles array

#### Test 3: Add Role to User
```http
POST /roles/{roleId}/users
{
  "userName": "john.doe@example.com",
  "roleName": "Admin"
}
```

**Expected:** 200 OK with user data including "Admin" role

#### Test 4: Get User (verify role assignment)
```http
GET /users/john.doe@example.com
```

**Expected:** 200 OK with user data and roles: ["Admin"]

#### Test 5: Remove Role from User
```http
DELETE /roles/{roleId}/users/{userId}
```

**Expected:** 200 OK with user data without "Admin" role

#### Test 6: Get All Users
```http
GET /users?query=
```

**Expected:** 200 OK with array of users

**Checkpoint:** All tests pass successfully.

---

## Phase 6: Database Setup

### Step 6.1: Verify Migrations

Check migrations executed:
```bash
dotnet fm:list
```

**Expected output:**
```
20: M020CreateRolesTable - Applied
24: M024CreateUsersTable - Applied
```

### Step 6.2: Verify Database Schema

Connect to database and verify:

**Tables created:**
- ✅ `app.roles` with columns: id, name
- ✅ `app.users` with columns: id, email, name
- ✅ `app.user_in_roles` with columns: user_id, role_id

**Foreign keys:**
- ✅ `fk_user_in_roles_user_id` → users.id
- ✅ `fk_user_in_roles_role_id` → roles.id

**Indexes:**
- ✅ `ix_roles_name` (unique)
- ✅ `ix_users_email` (unique)
- ✅ `pk_user_in_roles` (composite primary key on user_id, role_id)

**Sample data:**
- ✅ 3 default roles: Admin, Inspector, Viewer

### Step 6.3: Verify Cascade Operations

Test cascade delete in database:

```sql
-- Insert test user and role assignment
INSERT INTO app.users (id, email, name)
VALUES ('...', 'test@example.com', 'Test User');

INSERT INTO app.user_in_roles (user_id, role_id)
VALUES ('...', '...');

-- Delete user - should cascade delete relationship
DELETE FROM app.users WHERE email = 'test@example.com';

-- Verify relationship deleted
SELECT * FROM app.user_in_roles WHERE user_id = '...';
-- Expected: 0 rows
```

**Checkpoint:** All database structures verified.

---

## Verification Checklist

Use this checklist to verify your implementation:

### Domain Layer
- [ ] Role entity created with virtual properties
- [ ] User entity created with `IList<Role> Roles` navigation property
- [ ] Roles collection initialized in constructors
- [ ] RoleValidator created (validates only Name)
- [ ] UserValidator created (does NOT validate Roles)
- [ ] IRoleRepository interface with GetByNameAsync
- [ ] IUserRepository interface with CreateAsync and GetByEmailAsync

### Infrastructure Layer
- [ ] RoleMapper created (simple, no relationships)
- [ ] UserMapper created with Bag() configuration for many-to-many
- [ ] Join table name: "user_in_roles"
- [ ] Cascade.All configured
- [ ] Inverse(false) - User owns relationship
- [ ] M020CreateRolesTable migration created
- [ ] M024CreateUsersTable migration created with join table
- [ ] Composite primary key on join table
- [ ] Two foreign keys on join table
- [ ] NHRoleRepository implements GetByNameAsync
- [ ] NHUserRepository implements CreateAsync and GetByEmailAsync

### Application Layer
- [ ] UserErrors and RoleErrors defined
- [ ] CreateUserUseCase created (does NOT assign roles)
- [ ] GetUserUseCase created
- [ ] GetManyAndCountUsersUseCase created
- [ ] AddUsersToRoleUseCase created (adds to collection, updates user)
- [ ] RemoveUserFromRoleUseCase created (removes from collection, updates user)
- [ ] All use cases have transaction management

### WebApi Layer
- [ ] UserDto created with `IEnumerable<string> Roles`
- [ ] RoleDto created
- [ ] UserMappingProfile created with custom Roles mapping
- [ ] CreateUserModel created
- [ ] GetUserModel created
- [ ] GetManyAndCountModel created
- [ ] CreateUserEndpoint created (POST /users)
- [ ] GetUserEndpoint created (GET /users/{email})
- [ ] GetManyAndCountUsersEndpoint created (GET /users)
- [ ] Swagger documentation configured for all endpoints

### Configuration & Testing
- [ ] Repositories registered in DI
- [ ] AutoMapper profiles registered
- [ ] IUnitOfWork updated with Roles and Users properties
- [ ] Migrations executed successfully
- [ ] All tables created in database
- [ ] Foreign keys verified
- [ ] Default roles inserted
- [ ] Create user endpoint works
- [ ] Get user endpoint returns user with roles
- [ ] Add role to user works
- [ ] Remove role from user works
- [ ] Cascade operations work (deleting user deletes relationships)

---

## Common Pitfalls

### 1. Forgetting to Initialize Navigation Properties

**Problem:**
```csharp
public virtual IList<Role> Roles { get; set; }  // No initialization!
```

**Error:** `NullReferenceException` when trying to add roles

**Solution:**
```csharp
public virtual IList<Role> Roles { get; set; } = new List<Role>();

public User()
{
    Roles = new List<Role>();
}
```

---

### 2. Using Non-Virtual Properties

**Problem:**
```csharp
public IList<Role> Roles { get; set; }  // Not virtual!
```

**Error:** NHibernate lazy loading doesn't work, proxies fail

**Solution:**
```csharp
public virtual IList<Role> Roles { get; set; }  // Always virtual!
```

---

### 3. Wrong Cascade Setting

**Problem:**
```csharp
collectionMap.Cascade(Cascade.AllDeleteOrphan);  // Wrong for many-to-many!
```

**Error:** Deleting user deletes all roles (not just relationships)

**Solution:**
```csharp
collectionMap.Cascade(Cascade.All);  // Correct for many-to-many
// or
collectionMap.Cascade(Cascade.SaveUpdate);  // Even safer
```

---

### 4. Forgetting Join Table in Migration

**Problem:** Created users and roles tables but forgot join table

**Error:** NHibernate can't save relationships

**Solution:** Always create join table with composite primary key:
```csharp
Create.Table("user_in_roles")
      .WithColumn("user_id").AsGuid().NotNullable()
      .WithColumn("role_id").AsGuid().NotNullable();

Create.PrimaryKey("pk_user_in_roles")
      .OnTable("user_in_roles")
      .Columns("user_id", "role_id");
```

---

### 5. Validating Navigation Properties

**Problem:**
```csharp
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Roles).NotEmpty();  // DON'T DO THIS!
    }
}
```

**Error:** Validation fails when creating user (before roles are assigned)

**Solution:** Don't validate navigation properties - manage relationships separately.

---

### 6. Not Using Composite Primary Key on Join Table

**Problem:**
```csharp
Create.Table("user_in_roles")
      .WithColumn("id").AsGuid().PrimaryKey()  // Wrong!
      .WithColumn("user_id").AsGuid()
      .WithColumn("role_id").AsGuid();
```

**Error:** Allows duplicate user-role pairs

**Solution:**
```csharp
Create.PrimaryKey("pk_user_in_roles")
      .OnTable("user_in_roles")
      .Columns("user_id", "role_id");  // Composite key!
```

---

### 7. Wrong Delete Order in Migration Down()

**Problem:**
```csharp
public override void Down()
{
    Delete.Table("users");  // Delete parent first!
    Delete.Table("user_in_roles");
}
```

**Error:** Foreign key constraint violation

**Solution:**
```csharp
public override void Down()
{
    Delete.Table("user_in_roles");  // Delete join table first!
    Delete.Table("users");
}
```

---

### 8. Forgetting to Flatten DTOs

**Problem:**
```csharp
public class UserDto
{
    public IEnumerable<RoleDto> Roles { get; set; }  // Full objects!
}
```

**Issue:** Over-fetching, complex API responses

**Solution:**
```csharp
public class UserDto
{
    public IEnumerable<string> Roles { get; set; }  // Just names!
}

// In mapping profile:
.ForMember(dest => dest.Roles,
    opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)))
```

---

## Next Steps

Congratulations! You've successfully implemented a complex feature with entity relationships. Here are some ways to extend it:

### 1. **Add More Relationship Types**
- Implement one-to-many (User → Orders)
- Implement one-to-one (User → UserProfile)

### 2. **Optimize Performance**
- Configure eager loading for specific endpoints:
  ```csharp
  return await _session.Query<User>()
      .Where(u => u.Email == email)
      .Fetch(u => u.Roles)  // Eager load
      .SingleOrDefaultAsync();
  ```

### 3. **Add Relationship Management Endpoints**
- POST /roles/{roleId}/users - Add user to role
- DELETE /roles/{roleId}/users/{userId} - Remove user from role
- GET /roles/{roleId}/users - Get all users with a specific role

### 4. **Implement Bidirectional Relationships**
- Add `IList<User> Users` to Role entity
- Configure back-reference in RoleMapper

### 5. **Add Role Hierarchy**
- Create RoleHierarchy entity for parent-child role relationships
- Implement permission inheritance

### 6. **Add Bulk Operations**
- Assign multiple roles to user at once
- Assign role to multiple users at once

### 7. **Add Auditing**
- Track when roles were assigned/removed
- Create RoleAssignment entity with timestamp and assigned_by fields

### 8. **Implement Authorization**
- Use roles for endpoint authorization
- Create role-based policies

### 9. **Add Unit Tests**
- Test relationship management use cases
- Test cascade operations
- Mock repositories and test business logic

---

## Summary

### What You Built

In this guide, you created a complete User-Role feature with:
- ✅ **2 entities** with many-to-many relationship
- ✅ **25 files** across 4 layers
- ✅ **~1,185 lines** of production code
- ✅ **Join table** with composite primary key
- ✅ **Cascade operations** for automatic updates
- ✅ **Relationship management** use cases
- ✅ **Flattened DTOs** for clean API responses

### Key Concepts Learned

1. **Navigation Properties** - `IList<T>` for collections
2. **Many-to-Many Relationships** - Join tables and Bag() configuration
3. **Cascade Operations** - Automatic relationship management
4. **Lazy vs Eager Loading** - Performance optimization
5. **Flattening DTOs** - Simplifying API responses
6. **Relationship Management** - Separate use cases for add/remove
7. **Composite Primary Keys** - Ensuring uniqueness
8. **Foreign Key Constraints** - Maintaining referential integrity

### Time Spent

- **Planning:** 15 minutes
- **Domain Layer:** 30 minutes
- **Infrastructure Layer:** 45 minutes
- **Application Layer:** 40 minutes
- **WebApi Layer:** 40 minutes
- **Testing:** 20 minutes
- **Total:** ~2.5 hours

---

**Need help?** Refer back to the [README guide](./README.md) for detailed explanations of each concept.

**Questions or issues?** Review the [Common Pitfalls](#common-pitfalls) section above.
