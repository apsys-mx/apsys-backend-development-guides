# Repository Interfaces

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenidos
- [Introducción](#introducción)
- [Principio de Inversión de Dependencias](#principio-de-inversión-de-dependencias)
- [IReadOnlyRepository&lt;T, TKey&gt;](#ireadonlyrepositoryt-tkey)
- [IRepository&lt;T, TKey&gt;](#irepositoryt-tkey)
- [IUnitOfWork](#iunitofwork)
- [Interfaces de Repositorios Específicos](#interfaces-de-repositorios-específicos)
- [Clases de Soporte](#clases-de-soporte)
- [Ejemplos Reales del Proyecto](#ejemplos-reales-del-proyecto)
- [Patrones y Best Practices](#patrones-y-best-practices)
- [Checklist para Nuevos Repositorios](#checklist-para-nuevos-repositorios)

---

## Introducción

Los **Repository Interfaces** son contratos que definen las operaciones de acceso a datos sin especificar la implementación. En APSYS, seguimos el **Repository Pattern** para:

- **Abstracción de persistencia**: La lógica de dominio no conoce detalles de NHibernate
- **Testabilidad**: Podemos mockear repositorios fácilmente en tests
- **Inversión de dependencias**: Las interfaces están en el Domain Layer, las implementaciones en Infrastructure
- **Separación de concerns**: Operaciones de lectura vs escritura claramente separadas

### Estructura de Capas

```
Domain Layer (Interfaces)
    └── interfaces/repositories/
        ├── IReadOnlyRepository.cs        ← Base para lectura
        ├── IRepository.cs                 ← Base para CRUD
        ├── IUserRepository.cs             ← Específico por entidad
        ├── IRoleRepository.cs
        ├── IPrototypeRepository.cs
        ├── ITechnicalStandardRepository.cs
        └── IUnitOfWork.cs                 ← Agrega todos los repos

Infrastructure Layer (Implementaciones)
    └── persistence/repositories/
        ├── UserRepository.cs              ← Implementación con NHibernate
        ├── RoleRepository.cs
        ├── PrototypeRepository.cs
        ├── TechnicalStandardRepository.cs
        └── UnitOfWork.cs
```

---

## Principio de Inversión de Dependencias

El **Dependency Inversion Principle (DIP)** es clave en la arquitectura APSYS:

```
╔═══════════════════════════════════════════════════════════════╗
║                        DOMAIN LAYER                           ║
║  ┌─────────────────────────────────────────────────────────┐  ║
║  │  IUserRepository (Interface)                            │  ║
║  │  + Task<User> CreateAsync(string email, string name)   │  ║
║  │  + Task<User?> GetByEmailAsync(string email)           │  ║
║  └─────────────────────────────────────────────────────────┘  ║
║                             ▲                                 ║
╚═════════════════════════════║═════════════════════════════════╝
                              ║ implements
╔═════════════════════════════║═════════════════════════════════╗
║                    INFRASTRUCTURE LAYER                       ║
║  ┌─────────────────────────────────────────────────────────┐  ║
║  │  UserRepository : IUserRepository                       │  ║
║  │  {                                                      │  ║
║  │      private readonly ISession _session;               │  ║
║  │                                                         │  ║
║  │      public async Task<User> CreateAsync(...)          │  ║
║  │      {                                                  │  ║
║  │          // NHibernate implementation                  │  ║
║  │      }                                                  │  ║
║  │  }                                                      │  ║
║  └─────────────────────────────────────────────────────────┘  ║
╚═══════════════════════════════════════════════════════════════╝
```

**Ventajas:**
- El Domain Layer no depende de Infrastructure
- Podemos cambiar ORM sin afectar el dominio
- Los tests del dominio no necesitan base de datos real
- La inyección de dependencias resuelve las implementaciones

---

## IReadOnlyRepository&lt;T, TKey&gt;

La interfaz base para **operaciones de solo lectura**. Proporciona métodos para consultar datos sin capacidad de modificación.

### Definición Completa

```csharp
using System.Linq.Expressions;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for retrieving entities from a data store.
/// This interface provides both synchronous and asynchronous methods for querying data without modification capabilities.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
public interface IReadOnlyRepository<T, TKey> where T : class, new()
{
    #region Synchronous Methods

    /// <summary>
    /// Synchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <returns>The entity with the specified identifier, or null if not found</returns>
    T Get(TKey id);

    /// <summary>
    /// Synchronously retrieves all entities from the repository.
    /// </summary>
    /// <returns>An enumerable collection of all entities in the repository</returns>
    IEnumerable<T> Get();

    /// <summary>
    /// Synchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <returns>An enumerable collection of entities that match the query</returns>
    IEnumerable<T> Get(Expression<Func<T, bool>> query);

    /// <summary>
    /// Retrieves a paginated subset of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="page">The 0-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>A paginated enumerable collection of entities that match the query</returns>
    IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria);

    /// <summary>
    /// Synchronously counts the total number of entities in the repository.
    /// </summary>
    /// <returns>The total count of entities</returns>
    int Count();

    /// <summary>
    /// Synchronously counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <returns>The count of entities that match the query</returns>
    int Count(Expression<Func<T, bool>> query);

    /// <summary>
    /// Synchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query to filter the entities</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A result object containing both the entities and the total count</returns>
    GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting);

    #endregion

    #region Asynchronous Methods

    /// <summary>
    /// Asynchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity with the specified identifier, or null if not found</returns>
    Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of all entities</returns>
    Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of entities that match the query</returns>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the total number of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of entities</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the count of entities that match the query</returns>
    Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query to filter the entities</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a result object with both the entities and the total count</returns>
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default);

    #endregion
}
```

### Características Clave

1. **Operaciones síncronas y asíncronas**: Cada método tiene su versión async
2. **LINQ Expressions**: `Expression<Func<T, bool>>` para queries type-safe
3. **Paginación integrada**: `GetManyAndCount()` devuelve datos + count total
4. **CancellationToken**: Soporte para cancelación de operaciones async
5. **Generic constraints**: `where T : class, new()` para entidades con constructor vacío

### Casos de Uso

- **DAOs (Data Access Objects)**: Para queries de solo lectura complejas
- **Reportes**: Consultas que no modifican datos
- **APIs de consulta**: Endpoints GET que solo leen información

---

## IRepository&lt;T, TKey&gt;

La interfaz para **repositorios completos con CRUD**. Extiende `IReadOnlyRepository<T, TKey>` agregando capacidades de escritura.

### Definición Completa

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a full repository implementation with both read and write operations.
/// This interface extends the read-only repository functionality by adding
/// methods for creating, updating, and deleting entities.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class, new()
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>The added entity, possibly with updated properties (like generated IDs)</returns>
    T Add(T item);

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddAsync(T item);

    /// <summary>
    /// Saves or updates an existing entity in the repository.
    /// If the entity has an ID that exists in the repository, it is updated;
    /// otherwise, a new entity is created.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>The saved entity, possibly with updated properties</returns>
    T Save(T item);

    /// <summary>
    /// Asynchronously saves or updates an existing entity in the repository.
    /// If the entity has an ID that exists in the repository, it is updated;
    /// otherwise, a new entity is created.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveAsync(T item);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    void Delete(T item);

    /// <summary>
    /// Asynchronously deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DeleteAsync(T item);
}
```

### Diferencia: Add() vs Save()

**En NHibernate:**

- **`Add()`**: Inserta una nueva entidad. Si el ID ya existe, lanza excepción
- **`Save()`**: Guarda o actualiza (upsert). Si el ID existe, actualiza; si no, inserta

**Uso recomendado:**

```csharp
// Para CREAR una nueva entidad
var newUser = new User("john@example.com", "John Doe");
await _unitOfWork.Users.AddAsync(newUser);

// Para ACTUALIZAR una entidad existente
var existingUser = await _unitOfWork.Users.GetAsync(userId);
existingUser.Name = "Jane Doe";
await _unitOfWork.Users.SaveAsync(existingUser);
```

---

## IUnitOfWork

La interfaz que **agrega todos los repositorios** y proporciona **gestión de transacciones**.

### Definición Completa

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines the unit of work for the application
/// </summary>
public interface IUnitOfWork : IDisposable
{
    #region crud Repositories

    /**
     * Define the repositories for managing entities in this region
     * These repositories are used to create, update, delete, and retrieve entities from the database.
     */

    /// <summary>
    /// Repository for managing roles
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Repository for managing users
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the repository that provides access to prototype objects.
    /// </summary>
    IPrototypeRepository Prototypes { get; }

    /// <summary>
    /// Repository for managing technical standards
    /// </summary>
    ITechnicalStandardRepository TechnicalStandards { get; }

    #endregion

    #region read-only Repositories

    /**
     * Define the read-only repositories for retrieving entities in this region
     * These repositories are used to retrieve entities from the database without modifying them.
     */

    /// <summary>
    /// Read-only repository for managing technical standard DAOs
    /// </summary>
    ITechnicalStandardDaoRepository TechnicalStandardDaos { get; }

    /// <summary>
    /// Read-only repository for managing prototype DAOs
    /// </summary>
    IPrototypeDaoRepository PrototypeDaos { get; }

    #endregion

    #region transactions management

    /// <summary>
    /// Commits all changes made during the transaction to the database.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back all changes made during the transaction.
    /// </summary>
    void Rollback();

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    void BeginTransaction();

    /// <summary>
    /// Resets the current transaction, to clear any existing transaction state.
    /// </summary>
    void ResetTransaction();

    /// <summary>
    /// Determines whether there is an active transaction.
    /// </summary>
    /// <returns></returns>
    bool IsActiveTransaction();

    #endregion
}
```

### Pattern Unit of Work

El **Unit of Work** pattern:

1. **Agrega repositorios**: Un punto de acceso único para todos los repositorios
2. **Gestiona transacciones**: Coordina múltiples operaciones en una sola transacción
3. **Implementa IDisposable**: Libera recursos de NHibernate (Session)

**Uso típico:**

```csharp
public class CreateUserCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User> HandleAsync(CreateUserCommand command)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // Crear usuario
            var user = await _unitOfWork.Users.CreateAsync(
                command.Email,
                command.Name
            );

            // Asignar rol
            await _unitOfWork.Roles.AddUserToRoleAsync("User", command.Email);

            _unitOfWork.Commit();
            return user;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
```

---

## Interfaces de Repositorios Específicos

Cada entidad tiene su **propia interfaz de repositorio** que hereda de `IRepository<T, TKey>` y agrega métodos específicos del dominio.

### Patrón General

```csharp
public interface I{Entity}Repository : IRepository<{Entity}, Guid>
{
    // Métodos custom específicos del dominio
    Task<{Entity}> CreateAsync(/* parámetros */);
    Task<{Entity}?> GetBy{CriterioÚnico}Async(/* criterio */);
    Task<{Entity}> UpdateAsync(Guid id, /* parámetros */);
    // Otros métodos de negocio específicos...
}
```

### Ejemplo 1: IUserRepository

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

**Métodos custom:**
- `CreateAsync(string email, string name)`: Factory method con validaciones de negocio
- `GetByEmailAsync(string email)`: Query por índice único de negocio (email)

### Ejemplo 2: IPrototypeRepository

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="Prototype"/> entities, identified by a <see cref="Guid"/>.
/// </summary>
/// <remarks>This interface extends <see cref="IRepository{TEntity, TKey}"/> to provide functionality
/// specific to  <see cref="Prototype"/> entities. It serves as a contract for implementing data access operations
/// such as retrieving, adding, updating, and deleting <see cref="Prototype"/> instances.</remarks>
public interface IPrototypeRepository : IRepository<Prototype, Guid>
{
    /// <summary>
    /// Asynchronously creates a new <see cref="Prototype"/> instance with the specified details.
    /// </summary>
    /// <param name="number">The unique identifier for the prototype. Cannot be null or empty.</param>
    /// <param name="issueDate">The date and time when the prototype was issued.</param>
    /// <param name="expirationDate">The date and time when the prototype expires. Must be later than <paramref name="issueDate"/>.</param>
    /// <param name="status">The current status of the prototype. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Prototype"/>
    /// instance.</returns>
    Task<Prototype> CreateAsync(string number, DateTime issueDate, DateTime expirationDate, string status);

    /// <summary>
    /// Retrieves a prototype by its number.
    /// </summary>
    /// <param name="number">The prototype number to search for.</param>
    /// <returns>The prototype with the specified number, or null if not found.</returns>
    Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default);
}
```

**Métodos custom:**
- `CreateAsync(...)`: Factory method con todos los parámetros requeridos
- `GetByNumberAsync(string number)`: Query por código de negocio único

### Ejemplo 3: ITechnicalStandardRepository

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="TechnicalStandard"/> entities.
/// This interface extends the <see cref="IRepository{T, TKey}"/> to provide CRUD operations.
/// </summary>
public interface ITechnicalStandardRepository : IRepository<TechnicalStandard, Guid>
{
    /// <summary>
    /// Creates a new technical standard with the specified details.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="name"></param>
    /// <param name="edition"></param>
    /// <param name="status"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    Task<TechnicalStandard> CreateAsync(string code, string name, string edition, string status, string type);

    Task<TechnicalStandard?> GetByCodeAsync(string code);

    /// <summary>
    /// Updates an existing technical standard with the specified details.
    /// </summary>
    /// <param name="id">The ID of the technical standard to update.</param>
    /// <param name="code">The new code.</param>
    /// <param name="name">The new name.</param>
    /// <param name="edition">The new edition.</param>
    /// <param name="status">The new status.</param>
    /// <param name="type">The new type.</param>
    /// <returns>The updated <see cref="TechnicalStandard"/> entity.</returns>
    Task<TechnicalStandard> UpdateAsync(Guid id, string code, string name, string edition, string status, string type);
}
```

**Métodos custom:**
- `CreateAsync(...)`: Factory method con validaciones
- `GetByCodeAsync(string code)`: Query por código único
- `UpdateAsync(Guid id, ...)`: Update method con todos los parámetros

### Ejemplo 4: IRoleRepository

```csharp
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="Role"/> entities.
/// This interface extends the <see cref="IRepository{T, TKey}"/> to provide CRUD operations.
/// </summary>
public interface IRoleRepository : IRepository<Role, Guid>
{
    /// <summary>
    /// Creates default roles in the system if they do not already exist.
    /// </summary>
    /// <returns></returns>
    Task CreateDefaultRoles();

    /// <summary>
    /// Creates a new role with the specified name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>
    /// The newly created role entity.
    Task<Role> CreateAsync(string name);

    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>
    /// The role entity if found; otherwise, null.
    /// </returns>
    Task<Role?> GetByNameAsync(string name);

    /// <summary>
    /// Assigns a user to a role based on the role name and user email.
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    Task AddUserToRoleAsync(string roleName, string email);

    /// <summary>
    /// Removes a user from a role based on the role name and user email.
    /// </summary>
    /// <param name="roleName"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    Task RemoveUserFromRoleAsync(string roleName, string email);
}
```

**Métodos custom:**
- `CreateDefaultRoles()`: Seed de datos iniciales
- `CreateAsync(string name)`: Factory method simple
- `GetByNameAsync(string name)`: Query por nombre único
- `AddUserToRoleAsync()`: Operación de negocio compleja (relación many-to-many)
- `RemoveUserFromRoleAsync()`: Operación de negocio compleja

---

## Clases de Soporte

### GetManyAndCountResult&lt;T&gt;

Clase para **resultados paginados** con información completa de paginación y ordenamiento.

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Class to return the result for a paginated query with sorting capabilities.
/// Provides a container for collections of items along with pagination and sorting information.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class GetManyAndCountResult<T> : IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Default page size when no specific size is requested.
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 25;

    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the total count of records that match the query criteria.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based indexing).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the sorting criteria applied to the result set.
    /// Implements the IGetManyAndCountResultWithSorting interface.
    /// </summary>
    public SortingCriteria Sorting { get; set; }

    /// <summary>
    /// Constructor that initializes a new instance with the specified items, count, pagination, and sorting information.
    /// </summary>
    /// <param name="items">The collection of items for the current page.</param>
    /// <param name="count">The total number of records that match the query criteria.</param>
    /// <param name="pageNumber">The current page number (1-based indexing).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="sorting">The sorting criteria applied to the result set.</param>
    public GetManyAndCountResult(IEnumerable<T> items, long count, int pageNumber, int pageSize, SortingCriteria sorting)
    {
        Items = items;
        Count = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Sorting = sorting;
    }

    /// <summary>
    /// Default constructor that initializes a new instance with empty items and default values.
    /// The default values are:
    /// - Empty collection of items
    /// - Count set to 0
    /// - Page number set to 1
    /// - Page size set to DEFAULT_PAGE_SIZE (25)
    /// - Sorting criteria initialized with default values
    /// </summary>
    public GetManyAndCountResult()
    {
        Items = [];
        Count = 0;
        PageNumber = 1;
        PageSize = DEFAULT_PAGE_SIZE;
        Sorting = new SortingCriteria();
    }
}
```

**Propiedades clave:**
- `Items`: Los elementos de la página actual
- `Count`: Total de registros que cumplen el filtro (no solo la página)
- `PageNumber`: Número de página actual (1-based)
- `PageSize`: Tamaño de página
- `Sorting`: Criterios de ordenamiento aplicados

### SortingCriteria

Clase para definir **criterios de ordenamiento**.

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Class representing a sorting criteria
/// </summary>
public class SortingCriteria
{
    /// <summary>
    /// Gets or sets the name of the field to sort by.
    /// This property is used to specify the field name that will be used for sorting the results
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sorting criteria type.
    /// This property indicates whether the sorting should be done in ascending or descending order.
    /// </summary>
    public SortingCriteriaType Criteria { get; set; } = SortingCriteriaType.Ascending;

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy)
    {
        this.SortBy = sortBy;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy, SortingCriteriaType criteria)
    {
        this.SortBy = sortBy;
        this.Criteria = criteria;
    }
}

/// <summary>
/// The sorting criteria type enumeration
/// </summary>
public enum SortingCriteriaType
{
    /// <summary>
    /// Sort ascending
    /// </summary>
    Ascending = 1,

    /// <summary>
    /// Sort descending
    /// </summary>
    Descending = 2
}
```

**Uso:**

```csharp
// Ordenar por nombre ascendente
var sorting = new SortingCriteria("Name", SortingCriteriaType.Ascending);

// Ordenar por fecha de creación descendente
var sorting = new SortingCriteria("CreationDate", SortingCriteriaType.Descending);
```

### IGetManyAndCountResultWithSorting

Interfaz para exponer sorting en resultados paginados.

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Interface for objects that provide sorting capabilities in paginated results.
/// This allows implementing classes to expose sorting criteria information.
/// </summary>
public interface IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Gets the sorting criteria applied to the result set.
    /// </summary>
    SortingCriteria Sorting { get; }
}
```

---

## Ejemplos Reales del Proyecto

### Ejemplo 1: Uso en Endpoint (Crear Usuario)

```csharp
public class CreateUserEndpoint : Endpoint<CreateUserRequest, UserResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // Validar que no exista usuario con mismo email
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(req.Email);
            if (existingUser != null)
            {
                await SendAsync(new UserResponse
                {
                    Success = false,
                    Message = "User with this email already exists"
                }, 409, ct);
                return;
            }

            // Crear usuario usando el método del repositorio
            var user = await _unitOfWork.Users.CreateAsync(req.Email, req.Name);

            // Asignar rol por defecto
            await _unitOfWork.Roles.AddUserToRoleAsync("User", req.Email);

            _unitOfWork.Commit();

            await SendAsync(new UserResponse
            {
                Success = true,
                Data = user,
                Message = "User created successfully"
            }, 201, ct);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new UserResponse
            {
                Success = false,
                Message = ex.Message
            }, 500, ct);
        }
    }
}
```

### Ejemplo 2: Query con Paginación

```csharp
public class GetUsersEndpoint : Endpoint<GetUsersRequest, GetManyAndCountResult<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUsersEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Get("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetUsersRequest req, CancellationToken ct)
    {
        // GetManyAndCountAsync maneja paginación y sorting automáticamente
        var result = await _unitOfWork.Users.GetManyAndCountAsync(
            req.Query,           // Filtro de búsqueda (opcional)
            "Name",              // Ordenamiento por defecto
            ct
        );

        // Mapear entidades a DTOs
        var dtoResult = new GetManyAndCountResult<UserDto>
        {
            Items = result.Items.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Locked = u.Locked
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

### Ejemplo 3: LINQ Expressions para Queries

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
        // Query con LINQ Expression
        var activePrototypes = await _unitOfWork.Prototypes.GetAsync(
            p => p.Status == "Active" && p.ExpirationDate > DateTime.UtcNow,
            ct
        );

        var dtos = activePrototypes.Select(p => new PrototypeDto
        {
            Id = p.Id,
            Number = p.Number,
            IssueDate = p.IssueDate,
            ExpirationDate = p.ExpirationDate,
            Status = p.Status
        }).ToList();

        await SendOkAsync(dtos, ct);
    }
}
```

### Ejemplo 4: Transaction Management

```csharp
public class UpdateTechnicalStandardEndpoint : Endpoint<UpdateTechnicalStandardRequest, TechnicalStandardResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTechnicalStandardEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("/api/technical-standards/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateTechnicalStandardRequest req, CancellationToken ct)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            // Validar que el código no esté en uso por otra norma
            var existingByCode = await _unitOfWork.TechnicalStandards.GetByCodeAsync(req.Code);
            if (existingByCode != null && existingByCode.Id != req.Id)
            {
                await SendAsync(new TechnicalStandardResponse
                {
                    Success = false,
                    Message = "Another technical standard with this code already exists"
                }, 409, ct);
                return;
            }

            // Actualizar usando método del repositorio
            var updated = await _unitOfWork.TechnicalStandards.UpdateAsync(
                req.Id,
                req.Code,
                req.Name,
                req.Edition,
                req.Status,
                req.Type
            );

            _unitOfWork.Commit();

            await SendAsync(new TechnicalStandardResponse
            {
                Success = true,
                Data = updated,
                Message = "Technical standard updated successfully"
            }, 200, ct);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new TechnicalStandardResponse
            {
                Success = false,
                Message = ex.Message
            }, 500, ct);
        }
    }
}
```

---

## Patrones y Best Practices

### ✅ DO: Definir Métodos Custom Significativos

```csharp
// ✅ BIEN: Métodos que expresan intención de negocio
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateAsync(string email, string name);
    Task<User?> GetByEmailAsync(string email);
}

// ✅ BIEN: Operaciones complejas de negocio
public interface IRoleRepository : IRepository<Role, Guid>
{
    Task AddUserToRoleAsync(string roleName, string email);
    Task RemoveUserFromRoleAsync(string roleName, string email);
    Task CreateDefaultRoles();
}
```

### ❌ DON'T: Exponer Detalles de Implementación

```csharp
// ❌ MAL: Exponer Session de NHibernate
public interface IUserRepository : IRepository<User, Guid>
{
    ISession GetSession(); // ¡NO! Expone implementación
    IQueryable<User> GetQueryable(); // ¡NO! Expone IQueryable de NHibernate
}

// ✅ BIEN: Usar Expression<Func<T, bool>> en su lugar
public interface IUserRepository : IRepository<User, Guid>
{
    Task<IEnumerable<User>> GetAsync(Expression<Func<User, bool>> query, CancellationToken ct = default);
}
```

### ✅ DO: Usar CancellationToken en Métodos Async

```csharp
// ✅ BIEN: CancellationToken para cancelación cooperativa
public interface IPrototypeRepository : IRepository<Prototype, Guid>
{
    Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default);
}
```

### ❌ DON'T: Mezclar Queries y Commands en Métodos

```csharp
// ❌ MAL: Método que consulta Y modifica
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> GetOrCreateAsync(string email); // ¡NO! Side effect oculto
}

// ✅ BIEN: Separar query y command
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(string email, string name);
}
```

### ✅ DO: Usar Nullable Reference Types Correctamente

```csharp
// ✅ BIEN: Indicar explícitamente si puede retornar null
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateAsync(string email, string name);        // Nunca null
    Task<User?> GetByEmailAsync(string email);                // Puede ser null
}
```

### ❌ DON'T: Crear Interfaces Genéricas Vacías

```csharp
// ❌ MAL: Interfaz sin métodos custom
public interface IRoleRepository : IRepository<Role, Guid>
{
    // Vacío - ¿para qué existe entonces?
}

// ✅ BIEN: Agregar al menos métodos de negocio
public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role> CreateAsync(string name);
    Task<Role?> GetByNameAsync(string name);
}
```

### ✅ DO: Agrupar Repositorios en IUnitOfWork

```csharp
// ✅ BIEN: Todos los repositorios accesibles desde UnitOfWork
public interface IUnitOfWork : IDisposable
{
    IRoleRepository Roles { get; }
    IUserRepository Users { get; }
    IPrototypeRepository Prototypes { get; }
    ITechnicalStandardRepository TechnicalStandards { get; }

    void Commit();
    void Rollback();
    void BeginTransaction();
}
```

### ❌ DON'T: Inyectar Múltiples Repositorios Individualmente

```csharp
// ❌ MAL: Inyectar cada repositorio
public class CreateUserCommandHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    // ¿Y cómo manejo la transacción entre ambos?
}

// ✅ BIEN: Inyectar solo UnitOfWork
public class CreateUserCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    // Transacciones manejadas centralizadamente
}
```

### ✅ DO: Usar Expression&lt;Func&lt;T, bool&gt;&gt; para Queries Type-Safe

```csharp
// ✅ BIEN: Type-safe query con IntelliSense
var activeUsers = await _unitOfWork.Users.GetAsync(
    u => u.Locked == false && u.Roles.Any(r => r.Name == "Admin"),
    ct
);

// ❌ MAL: String-based query (propenso a errores)
var activeUsers = await _unitOfWork.Users.GetByQuery("Locked = 0 AND ...");
```

### ✅ DO: Implementar BeginTransaction, Commit, Rollback

```csharp
// ✅ BIEN: Manejo explícito de transacciones
try
{
    _unitOfWork.BeginTransaction();

    var user = await _unitOfWork.Users.CreateAsync(email, name);
    await _unitOfWork.Roles.AddUserToRoleAsync("User", email);

    _unitOfWork.Commit();
}
catch
{
    _unitOfWork.Rollback();
    throw;
}
```

### ❌ DON'T: Hacer Queries en Constructores o Propiedades

```csharp
// ❌ MAL: Query en propiedad
public interface IUserRepository : IRepository<User, Guid>
{
    User CurrentUser { get; } // ¡NO! Side effect oculto
}

// ✅ BIEN: Query explícito en método
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetCurrentUserAsync(CancellationToken ct = default);
}
```

### ✅ DO: Usar GetManyAndCount para Paginación Eficiente

```csharp
// ✅ BIEN: Una sola query para datos + count
var result = await _unitOfWork.Users.GetManyAndCountAsync(
    "john",     // Filtro
    "Name",     // Ordenamiento por defecto
    ct
);
// result.Items contiene los usuarios de la página
// result.Count contiene el total (para paginación en UI)

// ❌ MAL: Dos queries separadas
var users = await _unitOfWork.Users.GetAsync(u => u.Name.Contains("john"), ct);
var count = await _unitOfWork.Users.CountAsync(u => u.Name.Contains("john"), ct);
// ¡Dos round trips a la BD!
```

### ✅ DO: Documentar Métodos Custom con XML Comments

```csharp
// ✅ BIEN: Documentación completa
/// <summary>
/// Retrieves a user by their email address.
/// </summary>
/// <param name="email">The email address to search for.</param>
/// <returns>
/// The user entity if found; otherwise, null.
/// </returns>
Task<User?> GetByEmailAsync(string email);
```

---

## Checklist para Nuevos Repositorios

Cuando crees un nuevo repositorio en APSYS, sigue esta checklist:

### 1. Definir la Interfaz del Repositorio

- [ ] Crear interfaz en `Domain/interfaces/repositories/I{Entity}Repository.cs`
- [ ] Heredar de `IRepository<{Entity}, Guid>` (o `IReadOnlyRepository` si es solo lectura)
- [ ] Usar namespace `{proyecto}.domain.interfaces.repositories`

### 2. Agregar Métodos Custom

- [ ] `CreateAsync(...)` con todos los parámetros requeridos
- [ ] `GetBy{CriterioÚnico}Async(...)` para índices únicos de negocio (email, code, number, etc.)
- [ ] `UpdateAsync(Guid id, ...)` si la entidad tiene updates complejos
- [ ] Métodos de negocio específicos (ej: `AddUserToRoleAsync`, `CreateDefaultRoles`)

### 3. Documentar con XML Comments

- [ ] `<summary>` describiendo propósito del repositorio
- [ ] `<param>` para cada parámetro de métodos
- [ ] `<returns>` describiendo qué retorna (incluyendo null si aplica)

### 4. Usar Tipos Correctos

- [ ] `Task<{Entity}>` para métodos que nunca retornan null
- [ ] `Task<{Entity}?>` para métodos que pueden retornar null
- [ ] `CancellationToken ct = default` en todos los métodos async
- [ ] `Expression<Func<{Entity}, bool>>` para queries complejas (heredado de base)

### 5. Agregar al IUnitOfWork

- [ ] Agregar propiedad en `IUnitOfWork.cs`: `I{Entity}Repository {Entities} { get; }`
- [ ] Colocar en región `#region crud Repositories` o `#region read-only Repositories`
- [ ] Documentar con `<summary>`

### 6. Implementar en Infrastructure

- [ ] Crear clase `{Entity}Repository : BaseRepository<{Entity}, Guid>, I{Entity}Repository`
- [ ] Implementar todos los métodos custom
- [ ] Agregar validaciones de negocio (duplicados, referencias, etc.)
- [ ] Implementar en `UnitOfWork.cs` la propiedad del repositorio

### 7. Validar Integración

- [ ] El repositorio se inyecta correctamente vía DI
- [ ] Los métodos custom funcionan correctamente
- [ ] Las transacciones se manejan apropiadamente
- [ ] Los tests unitarios pasan (mockear repositorio)
- [ ] Los tests de integración pasan (base de datos real)

### Ejemplo Completo: IPrototypeRepository

```csharp
// 1. Archivo: Domain/interfaces/repositories/IPrototypeRepository.cs
using {proyecto}.domain.entities;

namespace {proyecto}.domain.interfaces.repositories;

// 2. Heredar de IRepository<T, TKey>
/// <summary>
/// Defines a repository for managing <see cref="Prototype"/> entities, identified by a <see cref="Guid"/>.
/// </summary>
public interface IPrototypeRepository : IRepository<Prototype, Guid>
{
    // 3. Métodos custom documentados
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
    /// <returns>The prototype with the specified number, or null if not found.</returns>
    Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default);
}

// 4. Agregar a IUnitOfWork
public interface IUnitOfWork : IDisposable
{
    // ...otros repositorios...

    /// <summary>
    /// Gets the repository that provides access to prototype objects.
    /// </summary>
    IPrototypeRepository Prototypes { get; }

    // ...métodos de transacción...
}
```

---

## Recursos Adicionales

- **FluentValidation**: https://docs.fluentvalidation.net/
- **NHibernate Documentation**: https://nhibernate.info/doc/
- **Repository Pattern**: Martin Fowler - Patterns of Enterprise Application Architecture
- **Unit of Work Pattern**: Martin Fowler - Patterns of Enterprise Application Architecture

---

**Guía creada para APSYS Backend Development**
Basada en el proyecto: `hashira.stone.backend`
Stack: .NET 9.0, C# 13, NHibernate 5.5, FastEndpoints 7.0
