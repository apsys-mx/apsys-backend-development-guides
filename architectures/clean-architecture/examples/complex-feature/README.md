# Complex Feature with Entity Relationships - Complete Guide

> **Version:** 1.0.0
> **Last Updated:** 2025-01-15
> **Prerequisites:** Understanding of [CRUD Features](../crud-feature/README.md), Clean Architecture, Entity Framework/NHibernate

---

## Table of Contents

1. [What is a Complex Feature?](#what-is-a-complex-feature)
2. [When to Use This Pattern](#when-to-use-this-pattern)
3. [Types of Entity Relationships](#types-of-entity-relationships)
4. [Anatomy of a Complex Feature](#anatomy-of-a-complex-feature)
5. [Example: User-Role Management](#example-user-role-management)
6. [Components by Layer](#components-by-layer)
   - [Domain Layer](#domain-layer)
   - [Infrastructure Layer](#infrastructure-layer)
   - [Application Layer](#application-layer)
   - [WebApi Layer](#webapi-layer)
7. [NHibernate Relationship Mapping](#nhibernate-relationship-mapping)
8. [Cascade Operations](#cascade-operations)
9. [Lazy vs Eager Loading](#lazy-vs-eager-loading)
10. [Best Practices](#best-practices)
11. [Implementation Checklist](#implementation-checklist)
12. [Related Guides](#related-guides)

---

## What is a Complex Feature?

A **Complex Feature** is an API feature that manages entities with **relationships to other entities**. Unlike simple CRUD features that work with standalone entities, complex features handle:

- **Navigation properties** between entities
- **Foreign key relationships** in the database
- **Cascade operations** (save, update, delete)
- **Join tables** for many-to-many relationships
- **Eager or lazy loading** of related data

### Comparison: CRUD vs Complex Feature

| Aspect | CRUD Feature | Complex Feature |
|--------|--------------|-----------------|
| **Entities** | Single, standalone entity | Multiple related entities |
| **Database Tables** | Single table | Multiple tables + join tables |
| **Navigation Properties** | None | `IList<T>`, `ISet<T>`, or single references |
| **Foreign Keys** | None | One or more foreign keys |
| **Cascade Operations** | Not applicable | Required (Cascade.All, Cascade.SaveUpdate, etc.) |
| **Loading Strategy** | Not applicable | Lazy or Eager loading configuration |
| **Mapping Complexity** | Simple property mapping | Relationship mapping (Bag, Set, ManyToMany) |
| **Join Tables** | None | Required for many-to-many |
| **DTO Mapping** | Direct property mapping | Complex mapping for related entities |
| **Use Cases** | CRUD operations | CRUD + relationship management |

---

## When to Use This Pattern

Use complex features with entity relationships when:

### ✅ **You Should Use This Pattern When:**

1. **Parent-Child Relationships**
   - Example: Order → OrderItems, Customer → Orders
   - A parent entity contains or owns child entities

2. **Many-to-Many Relationships**
   - Example: User ↔ Roles, Student ↔ Courses, Product ↔ Categories
   - Entities have bidirectional associations

3. **Lookup/Reference Data**
   - Example: Invoice → Customer, Product → Category
   - Entities reference other entities for additional data

4. **Aggregate Roots**
   - Example: ShoppingCart → CartItems
   - Following DDD patterns with aggregate boundaries

5. **Hierarchical Data**
   - Example: Employee → Manager (self-referencing)
   - Entities have parent-child relationships to themselves

### ❌ **You Should NOT Use This Pattern When:**

1. The entity is completely independent with no relationships
2. You only need to store IDs without loading related entities
3. The relationship is purely for reporting (use DAOs/Views instead)

---

## Types of Entity Relationships

### 1. **One-to-Many (1:N)**

A single parent entity relates to multiple child entities.

**Example:** `Order` → `OrderItems`

```csharp
public class Order : AbstractDomainObject
{
    public virtual IList<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem : AbstractDomainObject
{
    public virtual Order Order { get; set; } = null!;
}
```

**Database:**
- `orders` table
- `order_items` table with `order_id` foreign key

### 2. **Many-to-Many (M:N)**

Multiple entities on both sides relate to each other.

**Example:** `User` ↔ `Role`

```csharp
public class User : AbstractDomainObject
{
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
}

public class Role : AbstractDomainObject
{
    public virtual string Name { get; set; } = string.Empty;
}
```

**Database:**
- `users` table
- `roles` table
- `user_in_roles` join table with `user_id` + `role_id` composite primary key

### 3. **One-to-One (1:1)**

A single entity relates to exactly one other entity.

**Example:** `User` → `UserProfile`

```csharp
public class User : AbstractDomainObject
{
    public virtual UserProfile Profile { get; set; } = null!;
}

public class UserProfile : AbstractDomainObject
{
    public virtual User User { get; set; } = null!;
}
```

**Database:**
- `users` table
- `user_profiles` table with `user_id` foreign key (unique)

---

## Anatomy of a Complex Feature

A complex feature with entity relationships consists of **multiple related entities** across all layers:

```
src/
├── Domain/
│   ├── entities/
│   │   ├── User.cs                          # Main entity with navigation properties
│   │   ├── Role.cs                          # Related entity
│   │   └── validators/
│   │       ├── UserValidator.cs             # FluentValidation for User
│   │       └── RoleValidator.cs             # FluentValidation for Role
│   └── interfaces/
│       └── repositories/
│           ├── IUserRepository.cs           # Repository interface
│           └── IRoleRepository.cs
│
├── Infrastructure/
│   ├── nhibernate/
│   │   ├── mappers/
│   │   │   ├── UserMapper.cs                # NHibernate mapping with relationships
│   │   │   └── RoleMapper.cs
│   │   ├── NHUserRepository.cs              # Repository implementation
│   │   └── NHRoleRepository.cs
│   └── migrations/
│       ├── M020CreateRolesTable.cs          # Migration for roles table
│       └── M024CreateUsersTable.cs          # Migration for users + join table
│
├── Application/
│   └── usecases/
│       ├── users/
│       │   ├── CreateUserUseCase.cs         # Create user
│       │   ├── GetUserUseCase.cs            # Get user (with roles loaded)
│       │   ├── GetManyAndCountUsersUseCase.cs
│       │   └── UpdateUserUseCase.cs
│       └── roles/
│           ├── AddUsersToRoleUseCase.cs     # Manage relationship
│           └── RemoveUserFromRoleUseCase.cs # Manage relationship
│
└── WebApi/
    ├── dtos/
    │   ├── UserDto.cs                        # DTO with related data
    │   └── RoleDto.cs
    ├── mappingprofiles/
    │   ├── UserMappingProfile.cs             # AutoMapper for entities → DTOs
    │   └── RoleMappingProfile.cs
    └── features/
        ├── users/
        │   ├── models/
        │   │   ├── CreateUserModel.cs
        │   │   ├── GetUserModel.cs
        │   │   └── GetManyAndCountModel.cs
        │   └── endpoint/
        │       ├── CreateUserEndpoint.cs     # POST /users
        │       ├── GetUserEndpoint.cs        # GET /users/{email}
        │       └── GetManyAndCountUsersEndPoint.cs # GET /users
        └── roles/
            ├── models/
            │   ├── AddUserToRoleModel.cs
            │   └── RemoveUserFromRoleModel.cs
            └── endpoint/
                ├── AddUserToRoleEndpoint.cs  # POST /roles/{id}/users
                └── RemoveUserFromRoleEndpoint.cs # DELETE /roles/{id}/users/{userId}
```

**Total Files:** ~18-20 files (depending on relationship complexity)

---

## Example: User-Role Management

### Business Context

In the **Hashira Stone** inspection system, **Users** are assigned **Roles** for authorization purposes. This is a classic **many-to-many** relationship:

- A **User** can have multiple **Roles** (e.g., Admin, Inspector, Viewer)
- A **Role** can be assigned to multiple **Users**

### Key Features

- Users are created with email and name
- Roles are pre-defined in the system
- Users can be assigned/removed from roles
- User data includes their assigned roles when retrieved
- Integration with Auth0 for identity management

### Entities

**User:**
- Id (Guid)
- Email (string, unique)
- Name (string)
- **Roles (IList\<Role\>)** ← Navigation property

**Role:**
- Id (Guid)
- Name (string, e.g., "Admin", "Inspector")

### Relationship

- **Type:** Many-to-Many
- **Join Table:** `user_in_roles` with composite primary key (user_id, role_id)
- **Cascade:** Cascade.All (relationships are managed automatically)

---

## Components by Layer

### Domain Layer

#### 1. Main Entity with Navigation Property

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
- **Virtual properties** required for NHibernate lazy loading
- **IList\<Role\>** navigation property for many-to-many relationship
- **Default initialization** to empty list to prevent null reference exceptions
- **UserId** is not mapped to database (used for Auth0 integration)

#### 2. Related Entity

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
- Simple entity with only Name property
- No back-reference to Users (unidirectional relationship from User perspective)
- Virtual properties for NHibernate

#### 3. Validator for Main Entity

**File:** `src/Domain/entities/validators/UserValidator.cs`

```csharp
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

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
            .WithErrorCode("Name");

        // Note: Roles collection is NOT validated here
        // Role assignment is handled separately through dedicated use cases
    }
}
```

**Key Points:**
- **DO NOT validate navigation properties** in the entity validator
- Navigation properties are managed through dedicated use cases
- Focus on validating only the entity's own properties

#### 4. Repository Interface

**File:** `src/Domain/interfaces/repositories/IUserRepository.cs`

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="User"/> entities.
/// This interface extends the <see cref="IRepository{T, TKey}"/> to provide CRUD operations.
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Creates a new user with the specified email.
    /// </summary>
    /// <param name="email">The email address for the new user.</param>
    /// <param name="name">The name of the new user.</param>
    /// <returns>
    /// The newly created user entity.
    /// </returns>
    Task<User> CreateAsync(string email, string name);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>
    /// The user entity if found; otherwise, null.
    /// </returns>
    Task<User?> GetByEmailAsync(string email);
}
```

**Key Points:**
- Extends `IRepository<User, Guid>` for base CRUD operations
- Custom methods for business-specific queries (GetByEmailAsync)
- No methods for managing relationships (handled through entity manipulation)

---

### Infrastructure Layer

#### 1. NHibernate Mapper with Relationship Configuration

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

        // Many-to-Many Relationship Configuration
        Bag(x => x.Roles, map =>
        {
            map.Schema(AppSchemaResource.SchemaName);
            map.Table("user_in_roles");          // Join table name
            map.Key(k => k.Column("user_id"));    // Foreign key to users table
            map.Cascade(Cascade.All);             // Cascade operations
            map.Inverse(false);                   // User is the owner of the relationship
        },
        map => map.ManyToMany(m =>
        {
            m.Column("role_id");                  // Foreign key to roles table
            m.Class(typeof(Role));                // Related entity type
        }));
    }
}
```

**Key Points:**
- **Bag()** method for `IList<T>` collections
- **Table("user_in_roles")** specifies the join table
- **Key** specifies the foreign key column for the current entity (user_id)
- **ManyToMany** specifies the foreign key column for the related entity (role_id)
- **Cascade.All** enables automatic cascade operations
- **Inverse(false)** means User owns the relationship (changes are saved from User side)

#### 2. Related Entity Mapper

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
        });

        // No back-reference to Users (unidirectional relationship)
    }
}
```

**Key Points:**
- Simple mapper with no relationship configuration
- Unidirectional relationship (User → Role, not Role → Users)

#### 3. Migration with Join Table

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
        // Delete join table first (foreign key constraints)
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
              .WithColumn("email").AsString().NotNullable().Unique()
              .WithColumn("name").AsString().NotNullable();

        // Create user_in_roles junction table for many-to-many relationship
        Create.Table(_userInRolesTableName)
              .InSchema(_schemaName)
              .WithColumn("user_id").AsGuid().NotNullable()
              .WithColumn("role_id").AsGuid().NotNullable();

        // Add composite primary key
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
              .PrimaryColumn("id");

        // Add foreign key to roles table
        Create.ForeignKey($"fk_{_userInRolesTableName}_role_id")
              .FromTable(_userInRolesTableName)
              .InSchema(_schemaName)
              .ForeignColumn("role_id")
              .ToTable(_rolesTableName)
              .InSchema(_schemaName)
              .PrimaryColumn("id");

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
- **Join table** with composite primary key (user_id, role_id)
- **Two foreign keys** pointing to both tables
- **Delete order matters:** Join table must be deleted before parent tables
- **Unique index** on email for performance and uniqueness constraint

#### 4. Repository Implementation

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
/// <param name="session"></param>
/// <param name="serviceProvider"></param>
public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Create a new user with the specified email
    /// </summary>
    /// <param name="email"></param>
    /// <param name="name"></param>
    /// <returns></returns>
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
    /// <param name="email"></param>
    /// <returns>
    /// The user with the specified email, or null if not found
    /// </returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();

        // Note: Roles are lazy-loaded by default
        // To eager load: .Fetch(u => u.Roles).SingleOrDefaultAsync();
    }
}
```

**Key Points:**
- Inherits from `NHRepository<User, Guid>` for base operations
- Custom `CreateAsync` method with validation and duplicate check
- `GetByEmailAsync` for business-specific queries
- **Roles are lazy-loaded** by default (see [Lazy vs Eager Loading](#lazy-vs-eager-loading))

---

### Application Layer

#### 1. Create Use Case

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
        /// <summary>
        /// Gets or sets the email address for the new user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the new user.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IIdentityService _identityService = identityService;

        /// <summary>
        /// Executes the command to create a new user.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
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

                // Note: User.Roles collection is empty initially
                // Roles are assigned through separate use cases

                _uoW.Commit();
                return Result.Ok(user);
            }
            catch (HttpRequestException httpEx)
            {
                _uoW.Rollback();
                return Result.Fail(new Error($"Error creating new user {command.Email} on authentication service")
                    .CausedBy(httpEx));
            }
            catch (ArgumentException aex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(aex.Message).CausedBy(aex));
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";
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

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        const int length = 12;
        var passwordChars = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];

        rng.GetBytes(bytes);

        for (int i = 0; i < length; i++)
        {
            passwordChars[i] = chars[bytes[i] % chars.Length];
        }

        return new string(passwordChars);
    }
}
```

**Key Points:**
- Creates user in both Auth0 (identity provider) and database
- **Roles are NOT assigned here** - separate use cases handle relationship management
- Transaction management with rollback on errors
- FluentResults pattern for error handling

#### 2. Get Use Case (Loading Related Entities)

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
/// Use case for retrieving a user by their username.
/// </summary>
public class GetUserUseCase
{
    /// <summary>
    /// Command to get a user by their username.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        /// <summary>
        /// Gets or sets the username of the user to retrieve.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for executing the GetUser command.
    /// </summary>
    /// <param name="uoW"></param>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to get a user by their username.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
        {
            try
            {
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);

                // Roles are lazy-loaded when accessed (user.Roles)
                // If eager loading is configured, roles are already loaded

                return user == null
                    ? Result.Fail(UserErrors.UserNotFound(request.UserName))
                    : Result.Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with username: {UserName}", request.UserName);
                return Result.Fail("Error retrieving user");
            }
        }
    }
}
```

**Key Points:**
- Simple retrieval use case
- **Roles are lazy-loaded** by default (loaded when accessed)
- Can be configured for eager loading in repository

#### 3. Managing Relationships: Add Role to User

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
    /// <summary>
    /// Command to add a user to a role
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        /// <summary>
        /// The username of the user to add to the role
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The role name to add the user to
        /// </summary>
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for the AddUsersToRole command
    /// </summary>
    /// <param name="uoW"></param>
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
                    _logger.LogWarning("User {UserName} already has role {RoleName}",
                        request.UserName, request.RoleName);
                    return Result.Ok(user);
                }

                // Add role to user's collection
                user.Roles.Add(role);

                // Update user (cascade will save the relationship)
                await _uoW.Users.UpdateAsync(user);

                _uoW.Commit();

                _logger.LogInformation("Added role {RoleName} to user {UserName}",
                    request.RoleName, request.UserName);

                return Result.Ok(user);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error adding role {RoleName} to user {UserName}",
                    request.RoleName, request.UserName);
                return Result.Fail($"Error adding role to user: {ex.Message}");
            }
        }
    }
}
```

**Key Points:**
- Retrieves both entities from database
- **Adds role to user's Roles collection**
- **Updates user** - cascade operations automatically save the relationship to join table
- Checks for duplicates before adding
- Transaction management with proper rollback

#### 4. Managing Relationships: Remove Role from User

**File:** `src/Application/usecases/roles/RemoveUserFromRoleUseCase.cs`

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.roles;

/// <summary>
/// Use case for removing a user from a role in an organization.
/// Implements the command pattern to handle the operation.
/// </summary>
public class RemoveUserFromRoleUseCase
{
    /// <summary>
    /// Command object that encapsulates the request parameters for removing a user from a role.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        /// <summary>
        /// The username of the user to remove from the role
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The role name to remove the user from
        /// </summary>
        public string RoleName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler class that processes the command to remove a user from a role.
    /// Manages the transaction and performs the operation in the database.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to remove a user from a role.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
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

                // Find and remove role from user's collection
                var roleToRemove = user.Roles.FirstOrDefault(r => r.Id == role.Id);
                if (roleToRemove == null)
                {
                    _logger.LogWarning("User {UserName} does not have role {RoleName}",
                        request.UserName, request.RoleName);
                    return Result.Ok(user);
                }

                // Remove role from user's collection
                user.Roles.Remove(roleToRemove);

                // Update user (cascade will delete the relationship from join table)
                await _uoW.Users.UpdateAsync(user);

                _uoW.Commit();

                _logger.LogInformation("Removed role {RoleName} from user {UserName}",
                    request.RoleName, request.UserName);

                return Result.Ok(user);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error removing role {RoleName} from user {UserName}",
                    request.RoleName, request.UserName);
                return Result.Fail($"Error removing role from user: {ex.Message}");
            }
        }
    }
}
```

**Key Points:**
- Finds role in user's collection
- **Removes role from collection**
- **Updates user** - cascade operations automatically delete the relationship from join table
- Handles case where user doesn't have the role

---

### WebApi Layer

#### 1. DTO with Related Data

**File:** `src/WebApi/dtos/UserDto.cs`

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The full name of the user
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    /// <remarks>
    /// Contains role names (not full Role objects).
    /// This is a flattened representation for API responses.
    /// </remarks>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

**Key Points:**
- **Flattened representation** of related entities (role names instead of full Role objects)
- Simplifies API responses
- Initialized to empty collection to prevent null

#### 2. AutoMapper Configuration for Related Entities

**File:** `src/WebApi/mappingprofiles/UserMappingProfile.cs`

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for User entity and UserDto.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO mapping
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
- **Custom mapping** for Roles collection: extracts role names from Role objects
- Maps `IList<Role>` to `IEnumerable<string>`
- Handles nested object mapping

#### 3. Endpoint

**File:** `src/WebApi/features/users/endpoint/GetUserEndpoint.cs`

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.webapi.features.users.models;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

/// <summary>
/// Endpoint for retrieving a User by username
/// </summary>
/// <param name="mapper"></param>
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    /// <summary>
    /// Configure the endpoint
    /// </summary>
    public override void Configure()
    {
        Get("/users/{UserName}");
        Description(d => d
            .WithTags("Users")
            .WithName("GetUser")
            .WithDescription("Get a user by username")
            .Produces<GetUserModel.Response>(200, "application/json")
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    /// <summary>
    /// Handle the request to get a user by username
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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

        // Map entity to DTO (includes flattening Roles collection)
        var response = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

**Key Points:**
- Standard FastEndpoints pattern
- AutoMapper handles complex mapping of related entities
- Response includes flattened roles

---

## NHibernate Relationship Mapping

### Mapping Collections

NHibernate provides several ways to map collections:

| Collection Type | NHibernate Method | .NET Type | Use Case |
|----------------|-------------------|-----------|----------|
| **Bag** | `Bag()` | `IList<T>` | General collections, allows duplicates |
| **Set** | `Set()` | `ISet<T>` | Unique collections, no duplicates |
| **List** | `List()` | `IList<T>` | Ordered collections with index column |
| **Map** | `Map()` | `IDictionary<K,V>` | Key-value pairs |

### Many-to-Many Mapping Pattern

```csharp
Bag(x => x.RelatedEntities, collectionMap =>
{
    collectionMap.Schema("schema_name");
    collectionMap.Table("join_table_name");
    collectionMap.Key(k => k.Column("this_entity_id"));
    collectionMap.Cascade(Cascade.All);  // or Cascade.SaveUpdate, etc.
    collectionMap.Inverse(false);        // true = other side owns relationship
},
relationMap => relationMap.ManyToMany(m =>
{
    m.Column("other_entity_id");
    m.Class(typeof(OtherEntity));
}));
```

### One-to-Many Mapping Pattern

```csharp
// In Parent entity mapper
Bag(x => x.Children, collectionMap =>
{
    collectionMap.Schema("schema_name");
    collectionMap.Key(k => k.Column("parent_id"));  // Foreign key in child table
    collectionMap.Cascade(Cascade.AllDeleteOrphan);  // Delete children when removed from collection
},
relationMap => relationMap.OneToMany());

// In Child entity mapper
ManyToOne(x => x.Parent, map =>
{
    map.Column("parent_id");
    map.NotNullable(true);
    map.Cascade(Cascade.None);  // Parent controls cascade
});
```

### One-to-One Mapping Pattern

```csharp
// In main entity mapper
OneToOne(x => x.RelatedEntity, map =>
{
    map.Cascade(Cascade.All);
    map.Constrained(false);  // This entity owns the relationship
});

// In related entity mapper
ManyToOne(x => x.MainEntity, map =>
{
    map.Column("main_entity_id");
    map.Unique(true);  // Enforces one-to-one constraint
    map.NotNullable(true);
});
```

---

## Cascade Operations

Cascade operations define how operations on a parent entity affect related entities.

### Cascade Options

| Cascade Type | Description | Use Case |
|-------------|-------------|----------|
| **None** | No cascade operations | Default, manual control required |
| **SaveUpdate** | Cascade save and update only | Most common for many-to-many |
| **Delete** | Cascade delete only | Rare, usually not desired |
| **All** | Cascade all operations (save, update, delete) | Parent fully controls children |
| **AllDeleteOrphan** | All + delete orphaned entities | Parent owns children (one-to-many) |
| **Refresh** | Cascade refresh operations | Re-load from database |
| **Merge** | Cascade merge operations | Detached entity scenarios |

### Example Scenarios

#### Many-to-Many (User ↔ Role)

```csharp
map.Cascade(Cascade.SaveUpdate);
```

**Behavior:**
- Adding role to user.Roles → Saves relationship to join table
- Removing role from user.Roles → Deletes relationship from join table
- Deleting user → Does NOT delete roles (roles can exist independently)

#### One-to-Many (Order → OrderItems)

```csharp
map.Cascade(Cascade.AllDeleteOrphan);
```

**Behavior:**
- Adding item to order.Items → Saves item to database
- Removing item from order.Items → **Deletes item from database**
- Deleting order → Deletes all order items

#### One-to-One (User → UserProfile)

```csharp
map.Cascade(Cascade.All);
```

**Behavior:**
- Saving user → Saves user profile
- Updating user → Updates user profile
- Deleting user → Deletes user profile

---

## Lazy vs Eager Loading

### Lazy Loading (Default)

**Definition:** Related entities are loaded **only when accessed**.

```csharp
var user = await _uoW.Users.GetByIdAsync(userId);
// Roles are NOT loaded yet

var roleNames = user.Roles.Select(r => r.Name).ToList();
// NOW roles are loaded (additional database query)
```

**Pros:**
- Faster initial queries
- Less memory usage
- Only loads what you need

**Cons:**
- Multiple database queries (N+1 problem)
- Can cause issues if session is closed before accessing navigation properties

### Eager Loading

**Definition:** Related entities are loaded **immediately** with the parent entity.

```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .Fetch(u => u.Roles)  // Eager load roles
        .SingleOrDefaultAsync();
}
```

**Pros:**
- Single database query (JOIN)
- No N+1 problem
- Guaranteed data availability

**Cons:**
- Slower queries if relationships not needed
- More memory usage

### When to Use Each

| Use Lazy Loading When: | Use Eager Loading When: |
|------------------------|-------------------------|
| Related data is rarely needed | Related data is always needed |
| Performance is not critical | Performance is critical (avoid N+1) |
| Working with large collections | Working with small, predictable collections |
| API endpoints don't expose related data | API endpoints always expose related data |

### Best Practice

**In Hashira Stone project:**
- **Default:** Lazy loading
- **For API endpoints that expose related data:** Eager load in repository method
- **For internal logic:** Use lazy loading to avoid unnecessary queries

---

## Best Practices

### 1. **Always Use Virtual Properties for Navigation Properties**

```csharp
// ✅ Correct
public virtual IList<Role> Roles { get; set; } = new List<Role>();

// ❌ Wrong
public IList<Role> Roles { get; set; } = new List<Role>();
```

**Why:** NHibernate requires virtual properties for lazy loading and proxy generation.

### 2. **Initialize Collections in Entity Constructor**

```csharp
public class User : AbstractDomainObject
{
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User()
    {
        Roles = new List<Role>();  // Or initialize inline as shown above
    }
}
```

**Why:** Prevents null reference exceptions when adding items before entity is loaded from database.

### 3. **Use Appropriate Cascade Settings**

```csharp
// Many-to-Many: Use SaveUpdate (don't delete related entities)
map.Cascade(Cascade.SaveUpdate);

// One-to-Many (children owned by parent): Use AllDeleteOrphan
map.Cascade(Cascade.AllDeleteOrphan);

// One-to-One: Use All
map.Cascade(Cascade.All);
```

**Why:** Prevents accidental deletion of shared entities or orphaned records.

### 4. **Don't Validate Navigation Properties in Entity Validators**

```csharp
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();

        // ❌ Don't do this
        // RuleFor(x => x.Roles).NotEmpty();
    }
}
```

**Why:** Relationships are managed through dedicated use cases, not during entity creation/validation.

### 5. **Use Composite Primary Keys for Join Tables**

```csharp
Create.PrimaryKey($"pk_{_userInRolesTableName}")
      .OnTable(_userInRolesTableName)
      .WithSchema(_schemaName)
      .Columns("user_id", "role_id");
```

**Why:** Ensures uniqueness of relationships and improves query performance.

### 6. **Add Foreign Key Constraints in Migrations**

```csharp
Create.ForeignKey($"fk_{_userInRolesTableName}_user_id")
      .FromTable(_userInRolesTableName)
      .InSchema(_schemaName)
      .ForeignColumn("user_id")
      .ToTable(_usersTableName)
      .InSchema(_schemaName)
      .PrimaryColumn("id");
```

**Why:** Maintains referential integrity at the database level.

### 7. **Flatten Related Data in DTOs**

```csharp
// ✅ Good: Flatten to simple types
public class UserDto
{
    public IEnumerable<string> Roles { get; set; }  // Role names only
}

// ❌ Avoid: Exposing full entity objects
public class UserDto
{
    public IEnumerable<RoleDto> Roles { get; set; }  // Full objects
}
```

**Why:** Simpler API responses, prevents over-fetching, easier to consume.

### 8. **Configure Eager Loading for API Endpoints**

```csharp
// If endpoint always returns user with roles
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .Fetch(u => u.Roles)  // Eager load
        .SingleOrDefaultAsync();
}
```

**Why:** Avoids N+1 queries when related data is always needed.

### 9. **Use Unidirectional Relationships When Possible**

```csharp
// User has Roles, but Role doesn't have Users collection
public class User : AbstractDomainObject
{
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
}

public class Role : AbstractDomainObject
{
    // No Users collection
}
```

**Why:** Simpler mapping, less memory overhead, avoids circular references.

### 10. **Separate Use Cases for Relationship Management**

```csharp
// ✅ Good: Dedicated use cases
AddUsersToRoleUseCase
RemoveUserFromRoleUseCase

// ❌ Avoid: Managing relationships in create/update use cases
CreateUserUseCase // Don't assign roles here
```

**Why:** Clear separation of concerns, easier to test, better security control.

---

## Implementation Checklist

Use this checklist when implementing complex features with entity relationships:

### Domain Layer

- [ ] **Entities Created**
  - [ ] Main entity class with navigation properties
  - [ ] Related entity class(es)
  - [ ] All properties marked as `virtual`
  - [ ] Collections initialized to prevent null references
  - [ ] Parameterless constructors for NHibernate
  - [ ] Business constructors with required parameters

- [ ] **Validators Created**
  - [ ] FluentValidation validator for main entity
  - [ ] FluentValidation validator for related entities
  - [ ] NO validation rules for navigation properties
  - [ ] Validation focuses on entity's own properties only

- [ ] **Repository Interfaces Created**
  - [ ] IRepository interface for main entity
  - [ ] IRepository interface for related entities
  - [ ] Custom query methods defined (e.g., GetByEmailAsync)
  - [ ] NO methods for managing relationships directly

### Infrastructure Layer

- [ ] **NHibernate Mappers Created**
  - [ ] Mapper for main entity with relationship configuration
  - [ ] Mapper for related entities
  - [ ] Correct relationship type used (Bag, Set, etc.)
  - [ ] Join table name specified (for many-to-many)
  - [ ] Foreign key columns specified correctly
  - [ ] Cascade settings configured appropriately
  - [ ] Inverse() setting correct (who owns the relationship)

- [ ] **Repository Implementations Created**
  - [ ] Repository implementation for main entity
  - [ ] Repository implementation for related entities
  - [ ] Custom query methods implemented
  - [ ] Eager loading configured for endpoints that need it
  - [ ] Validation and duplicate checks in create methods

- [ ] **Database Migrations Created**
  - [ ] Migration for main entity table
  - [ ] Migration for related entity table
  - [ ] Migration for join table (if many-to-many)
  - [ ] Composite primary key on join table
  - [ ] Foreign key constraints added
  - [ ] Indexes added for performance (email, foreign keys, etc.)
  - [ ] Down() method deletes tables in correct order

### Application Layer

- [ ] **Use Cases Created**
  - [ ] CreateUseCase for main entity
  - [ ] GetUseCase for main entity (single)
  - [ ] GetManyAndCountUseCase for main entity (list)
  - [ ] UpdateUseCase for main entity (if needed)
  - [ ] DeleteUseCase for main entity (if needed)
  - [ ] Use cases for managing relationships (Add/Remove)
  - [ ] Transaction management (BeginTransaction, Commit, Rollback)
  - [ ] Error handling with FluentResults
  - [ ] Logging for important operations

### WebApi Layer

- [ ] **DTOs Created**
  - [ ] DTO for main entity with flattened related data
  - [ ] DTO for related entities (if exposed separately)
  - [ ] Collections initialized to prevent null

- [ ] **AutoMapper Profiles Created**
  - [ ] Mapping profile for main entity → DTO
  - [ ] Custom mapping for navigation properties (flattening)
  - [ ] Mapping for Request → Command
  - [ ] Mapping for Entity → Response

- [ ] **Request/Response Models Created**
  - [ ] CreateModel (Request/Response)
  - [ ] GetModel (Request/Response)
  - [ ] GetManyAndCountModel (Request)
  - [ ] UpdateModel (Request/Response) if needed
  - [ ] Models for relationship management

- [ ] **Endpoints Created**
  - [ ] CreateEndpoint (POST)
  - [ ] GetEndpoint (GET /{id})
  - [ ] GetManyAndCountEndpoint (GET /)
  - [ ] UpdateEndpoint (PUT /{id}) if needed
  - [ ] DeleteEndpoint (DELETE /{id}) if needed
  - [ ] Endpoints for relationship management
  - [ ] Swagger documentation configured
  - [ ] Authorization policies applied
  - [ ] Error handling for all error types

### Configuration & Testing

- [ ] **Dependency Injection Configured**
  - [ ] Repositories registered in DI container
  - [ ] AutoMapper profiles registered
  - [ ] Endpoints registered with FastEndpoints

- [ ] **Unit of Work Updated**
  - [ ] Properties added for new repositories
  - [ ] Repositories initialized in constructor

- [ ] **Database Setup Complete**
  - [ ] Migrations executed (dotnet fm:up)
  - [ ] Tables created successfully
  - [ ] Foreign keys verified
  - [ ] Sample data inserted for testing

- [ ] **Manual Testing Complete**
  - [ ] Swagger UI accessible
  - [ ] Create endpoint works
  - [ ] Get single endpoint returns entity with related data
  - [ ] Get list endpoint works with filtering/sorting
  - [ ] Relationship management endpoints work
  - [ ] Error cases handled correctly (not found, validation, etc.)

---

## Related Guides

- **[CRUD Feature](../crud-feature/README.md)** - Foundation for simple features
- **[Read-Only Feature](../read-only-feature/README.md)** - Optimized read operations
- **[WebApi Layer - Request/Response Models](../../webapi-layer/request-response-models.md)** - Model design patterns
- **[ORM - NHibernate Mappings](../../infrastructure-layer/orm-nhibernate.md)** - Advanced mapping configurations
- **[Data Migrations](../../infrastructure-layer/data-migrations.md)** - Database migration best practices

---

## Summary

Complex features with entity relationships add significant power to your API but require careful design:

### Key Takeaways

1. **Use virtual properties** for all navigation properties
2. **Initialize collections** to prevent null reference exceptions
3. **Choose the right cascade settings** for your relationship type
4. **Flatten related data in DTOs** for simpler API responses
5. **Separate relationship management** into dedicated use cases
6. **Configure eager loading** for API endpoints that always need related data
7. **Use composite primary keys** for join tables
8. **Add foreign key constraints** in database migrations
9. **Don't validate navigation properties** in entity validators
10. **Test thoroughly** - relationships add complexity and edge cases

### What We Built

In this guide, we built a complete User-Role management feature with:
- **18-20 files** across 4 layers
- **Many-to-many relationship** with join table
- **CRUD operations** for both entities
- **Relationship management** use cases
- **Proper cascade** and loading configurations
- **Flattened DTO** representation

### Next Steps

- Read the [step-by-step implementation guide](./step-by-step.md) for detailed instructions
- Review the [NHibernate mappings guide](../../infrastructure-layer/orm-nhibernate.md) for advanced scenarios
- Study the [CRUD feature guide](../crud-feature/README.md) if you haven't already
- Explore different relationship types (one-to-many, one-to-one) in your own features

---

**Questions or issues?** Check the [troubleshooting section](./step-by-step.md#common-pitfalls) in the step-by-step guide.
