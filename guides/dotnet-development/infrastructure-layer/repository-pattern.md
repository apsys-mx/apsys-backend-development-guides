# Repository Pattern

**Versión:** 1.0.0
**Última actualización:** 2025-01-14

## Tabla de Contenidos
- [¿Qué es el Repository Pattern?](#qué-es-el-repository-pattern)
- [¿Por qué usar Repository Pattern?](#por-qué-usar-repository-pattern)
- [Estructura de Capas](#estructura-de-capas)
- [Tipos de Repositorios](#tipos-de-repositorios)
- [Implementación Paso a Paso](#implementación-paso-a-paso)
- [Validación en Repositorios](#validación-en-repositorios)
- [Session Management](#session-management)
- [Mejores Prácticas](#mejores-prácticas)
- [Antipatrones Comunes](#antipatrones-comunes)
- [Checklist de Implementación](#checklist-de-implementación)
- [Ejemplos Completos](#ejemplos-completos)

---

## ¿Qué es el Repository Pattern?

El **Repository Pattern** es un patrón de diseño que **abstrae el acceso a datos** detrás de una interfaz, proporcionando una colección de objetos en memoria.

```
┌─────────────────┐
│ Application     │
│ Layer           │ ──► Usa IUserRepository (abstracción)
└─────────────────┘
         │
         ▼
┌─────────────────┐
│ Domain Layer    │ ──► Define IUserRepository (interfaz)
└─────────────────┘
         ▲
         │ Implementa
┌─────────────────┐
│ Infrastructure  │ ──► NHUserRepository (implementación con NHibernate)
│ Layer           │
└─────────────────┘
```

### Analogía del Mundo Real

Piensa en una **biblioteca**:
- **Usuario** (Application Layer): pide un libro al bibliotecario
- **Bibliotecario** (Repository): sabe dónde están los libros y cómo buscarlos
- **Estantería** (Base de Datos): almacenamiento físico de libros

El usuario **NO necesita saber** dónde están los libros ni cómo están organizados. Solo interactúa con el bibliotecario (repositorio).

---

## ¿Por qué usar Repository Pattern?

### ✅ Beneficios

| Beneficio | Descripción |
|-----------|-------------|
| **Abstracción** | Application Layer no conoce el ORM (NHibernate, EF Core, etc.) |
| **Testabilidad** | Fácil crear mocks/stubs de `IRepository` para tests unitarios |
| **Centralización** | Lógica de acceso a datos en un solo lugar |
| **Reutilización** | Queries complejas se escriben una vez y se reusan |
| **Cambio de ORM** | Cambiar de NHibernate a EF Core solo afecta Infrastructure Layer |

### 📊 Comparación: Con vs Sin Repository Pattern

**❌ SIN Repository Pattern**
```csharp
// Application Layer acoplado a NHibernate
public class CreateUserUseCase(ISession session) // ❌ Depende de ISession
{
    public async Task<User> ExecuteAsync(string email, string name)
    {
        var user = new User(email, name);

        // ❌ Application Layer conoce detalles de NHibernate
        await session.SaveAsync(user);
        await session.FlushAsync();

        return user;
    }
}
```

**✅ CON Repository Pattern**
```csharp
// Application Layer usa abstracción
public class CreateUserUseCase(IUserRepository userRepository) // ✅ Depende de abstracción
{
    public async Task<User> ExecuteAsync(string email, string name)
    {
        // ✅ Application Layer solo llama métodos de negocio
        return await userRepository.CreateAsync(email, name);
    }
}
```

---

## Estructura de Capas

### 📂 Organización de Archivos

```
src/
├── hashira.stone.backend.domain/
│   └── interfaces/
│       └── repositories/
│           ├── IReadOnlyRepository.cs        ← Define contrato de lectura
│           ├── IRepository.cs                ← Define contrato CRUD completo
│           ├── IUserRepository.cs            ← Define métodos específicos de User
│           └── IRoleRepository.cs
│
└── hashira.stone.backend.infrastructure/
    └── nhibernate/
        ├── NHReadOnlyRepository.cs           ← Implementa IReadOnlyRepository con NHibernate
        ├── NHRepository.cs                   ← Implementa IRepository con validación
        ├── NHUserRepository.cs               ← Implementa IUserRepository
        └── NHRoleRepository.cs
```

### 🔑 Principio Fundamental

> **Las interfaces se definen en Domain Layer, las implementaciones en Infrastructure Layer**

**¿Por qué?**
- Domain Layer **no debe conocer** detalles de infraestructura (NHibernate, SQL, etc.)
- Infrastructure Layer **depende de** Domain Layer (Dependency Inversion Principle)
- Application Layer **solo depende de** interfaces del Domain Layer

---

## Tipos de Repositorios

### 1️⃣ Read-Only Repository

**Propósito:** Operaciones de **solo lectura** (consultas, conteo, paginación).

```csharp
// Domain Layer: hashira.stone.backend.domain/interfaces/repositories/IReadOnlyRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IReadOnlyRepository<T, TKey> where T : class, new()
{
    // ─────────────────────────────────────────────────────────────
    // Métodos Síncronos
    // ─────────────────────────────────────────────────────────────

    /// <summary>Obtiene entidad por ID</summary>
    T Get(TKey id);

    /// <summary>Obtiene todas las entidades</summary>
    IEnumerable<T> Get();

    /// <summary>Obtiene entidades que cumplen condición</summary>
    IEnumerable<T> Get(Expression<Func<T, bool>> query);

    /// <summary>Obtiene entidades paginadas y ordenadas</summary>
    IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria);

    /// <summary>Cuenta total de entidades</summary>
    int Count();

    /// <summary>Cuenta entidades que cumplen condición</summary>
    int Count(Expression<Func<T, bool>> query);

    /// <summary>Obtiene entidades paginadas con total count</summary>
    GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting);

    // ─────────────────────────────────────────────────────────────
    // Métodos Asíncronos
    // ─────────────────────────────────────────────────────────────

    Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default);
}
```

**✅ Cuándo usar:**
- Casos de uso que **solo consultan** datos (queries, reports)
- DTOs que necesitan datos de solo lectura
- Vistas/pantallas que muestran información

### 2️⃣ Full Repository (CRUD Completo)

**Propósito:** Operaciones de **lectura y escritura** (Create, Read, Update, Delete).

```csharp
// Domain Layer: hashira.stone.backend.domain/interfaces/repositories/IRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class, new()
{
    // ─────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Agrega una nueva entidad</summary>
    T Add(T item);

    /// <summary>Agrega una nueva entidad (async)</summary>
    Task AddAsync(T item);

    // ─────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Actualiza una entidad existente</summary>
    T Save(T item);

    /// <summary>Actualiza una entidad existente (async)</summary>
    Task SaveAsync(T item);

    // ─────────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Elimina una entidad</summary>
    void Delete(T item);

    /// <summary>Elimina una entidad (async)</summary>
    Task DeleteAsync(T item);
}
```

**✅ Cuándo usar:**
- Casos de uso que **modifican datos** (Create, Update, Delete)
- Operaciones transaccionales con Unit of Work

### 3️⃣ Specific Repository (Repositorio Específico)

**Propósito:** Operaciones de **negocio específicas** de una entidad.

```csharp
// Domain Layer: hashira.stone.backend.domain/interfaces/repositories/IUserRepository.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Crea un nuevo usuario con validaciones de negocio
    /// </summary>
    Task<User> CreateAsync(string email, string name);

    /// <summary>
    /// Obtiene un usuario por email
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
}
```

**✅ Cuándo usar:**
- Operaciones específicas que **no encajan** en CRUD genérico
- Validaciones de negocio complejas
- Queries específicas por campos únicos (email, username, etc.)

---

## Implementación Paso a Paso

### Paso 1: Crear Base Read-Only Repository

```csharp
// Infrastructure Layer: hashira.stone.backend.infrastructure/nhibernate/NHReadOnlyRepository.cs
using System.Linq.Expressions;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.infrastructure.nhibernate.filtering;
using System.Linq.Dynamic.Core;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementación base de solo lectura usando NHibernate
/// </summary>
public class NHReadOnlyRepository<T, TKey>(ISession session) : IReadOnlyRepository<T, TKey>
    where T : class, new()
{
    /// <summary>
    /// Session de NHibernate - protected para acceso desde clases derivadas
    /// </summary>
    protected internal readonly ISession _session = session;

    // ─────────────────────────────────────────────────────────────
    // Implementación de métodos síncronos
    // ─────────────────────────────────────────────────────────────

    public int Count()
        => this._session.QueryOver<T>().RowCount();

    public int Count(Expression<Func<T, bool>> query)
        => this._session.Query<T>().Where(query).Count();

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

    // ─────────────────────────────────────────────────────────────
    // Implementación de métodos asíncronos
    // ─────────────────────────────────────────────────────────────

    public Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default)
        => this._session.GetAsync<T>(id, cancellationToken);

    public async Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default)
        => await this._session.Query<T>().ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
        => await this._session.Query<T>()
                .Where(query)
                .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => this._session.Query<T>().CountAsync(cancellationToken);

    public Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
        => this._session.Query<T>().Where(query).CountAsync(cancellationToken);

    // ─────────────────────────────────────────────────────────────
    // GetManyAndCount: Paginación con total count
    // ─────────────────────────────────────────────────────────────

    public GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        var items = this.Get(expression, pageNumber, pageSize, sortingCriteria);
        var total = this.Count(expression);

        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Ejecutar queries secuencialmente para evitar problemas de DataReader
        var total = await this._session.Query<T>()
            .Where(expression)
            .CountAsync(cancellationToken);

        var items = await this._session.Query<T>()
            .OrderBy(sortingCriteria.ToExpression())
            .Where(expression)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper method: Parsear query string
    // ─────────────────────────────────────────────────────────────

    private static (Expression<Func<T, bool>> expression, int pageNumber, int pageSize, SortingCriteria sortingCriteria)
        PrepareQuery(string? query, string defaultSorting)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
        QueryStringParser queryStringParser = new(queryString);

        // Paginación
        int pageNumber = queryStringParser.ParsePageNumber();
        int pageSize = queryStringParser.ParsePageSize();

        // Ordenamiento
        Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
        SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
            ? SortingCriteriaType.Descending
            : SortingCriteriaType.Ascending;
        SortingCriteria sortingCriteria = new(sorting.By, directions);

        // Filtros
        IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
        QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
        var expression = FilterExpressionParser.ParsePredicate<T>(filters);
        if (quickSearch != null)
            expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

        return (expression, pageNumber, pageSize, sortingCriteria);
    }
}
```

### Paso 2: Crear Base Full Repository con Validación

```csharp
// Infrastructure Layer: hashira.stone.backend.infrastructure/nhibernate/NHRepository.cs
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using FluentValidation;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementación base de repositorio completo con validación usando NHibernate
/// </summary>
public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey>
    where T : class, new()
{
    /// <summary>
    /// Validador de FluentValidation para la entidad T
    /// </summary>
    private readonly AbstractValidator<T> validator;

    /// <summary>
    /// Constructor: Resuelve el validador desde IServiceProvider
    /// </summary>
    protected NHRepository(ISession session, IServiceProvider serviceProvider)
        : base(session)
    {
        Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
        this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T>
            ?? throw new InvalidOperationException($"The validator for {typeof(T)} type could not be created");
    }

    // ─────────────────────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Agrega una nueva entidad con validación síncrona
    /// </summary>
    public T Add(T item)
    {
        // Validar entidad
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        // Guardar en NHibernate
        this._session.Save(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Agrega una nueva entidad asíncronamente (sin validación)
    /// </summary>
    public Task AddAsync(T item)
        => this._session.SaveAsync(item);

    // ─────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza una entidad existente con validación
    /// </summary>
    public T Save(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Update(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Actualiza una entidad asíncronamente (sin validación)
    /// </summary>
    public Task SaveAsync(T item)
        => this._session.UpdateAsync(item);

    // ─────────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────────

    public void Delete(T item)
    {
        this._session.Delete(item);
        this.FlushWhenNotActiveTransaction();
    }

    public Task DeleteAsync(T item)
        => this._session.DeleteAsync(item);

    // ─────────────────────────────────────────────────────────────
    // Session Management Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica si hay una transacción activa
    /// </summary>
    protected internal bool IsTransactionActive()
        => this._session.GetCurrentTransaction() != null
           && this._session.GetCurrentTransaction().IsActive;

    /// <summary>
    /// Flush SOLO si NO hay transacción activa
    /// Si hay transacción, el Commit de UnitOfWork hará el Flush
    /// </summary>
    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

**🔑 Puntos Clave:**
1. **Validación automática**: Usa FluentValidation en `Add()` y `Save()`
2. **Flush condicional**: Solo hace `Flush()` si NO hay transacción activa
3. **IServiceProvider**: Resuelve validadores dinámicamente

### Paso 3: Crear Repositorio Específico

```csharp
// Infrastructure Layer: hashira.stone.backend.infrastructure/nhibernate/NHUserRepository.cs
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementación específica de IUserRepository usando NHibernate
/// </summary>
public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Crea un nuevo usuario con validaciones de negocio
    /// </summary>
    public async Task<User> CreateAsync(string email, string name)
    {
        // 1. Crear entidad
        var user = new User(email, name);

        // 2. Validar entidad (usa validador de FluentValidation)
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // 3. Validar regla de negocio: email único
        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"A user with the email '{email}' already exists.");

        // 4. Agregar a base de datos
        await AddAsync(user);

        // 5. Flush si no hay transacción activa
        FlushWhenNotActiveTransaction();

        return user;
    }

    /// <summary>
    /// Obtiene un usuario por email
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}
```

**🔑 Puntos Clave:**
1. **Lógica de negocio**: Valida que el email sea único
2. **Reutiliza base class**: Hereda `Add()`, `Save()`, `Get()`, etc.
3. **Métodos específicos**: `CreateAsync()`, `GetByEmailAsync()`

---

## Validación en Repositorios

### 🎯 Estrategia de Validación

```
┌─────────────────────────────────────────────────────────────┐
│ VALIDACIÓN EN REPOSITORIOS                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. FluentValidation (NHRepository base class)              │
│     ✓ Validaciones de formato (email, teléfono, etc.)      │
│     ✓ Validaciones de rango (longitud, valores mínimos)    │
│     ✓ Validaciones de requerido                            │
│                                                             │
│  2. Validaciones de Negocio (Repositorio específico)       │
│     ✓ Unicidad (email, username, etc.)                     │
│     ✓ Reglas de negocio complejas                          │
│     ✓ Validaciones con queries a BD                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Ejemplo Completo

```csharp
// 1. Validador de FluentValidation
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
    }
}

// 2. Repositorio específico con validación de negocio
public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        var user = new User(email, name);

        // ✅ Validación FluentValidation (automática en AddAsync)
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // ✅ Validación de negocio: email único
        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"Email '{email}' already exists.");

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }
}
```

---

## Session Management

### 🔄 FlushWhenNotActiveTransaction Pattern

**Problema:** ¿Cuándo hacer `Flush()` de cambios a la base de datos?

**Solución:**
- **CON Transacción activa**: El `Commit()` del UnitOfWork hará el `Flush()`
- **SIN Transacción activa**: El repositorio hace `Flush()` inmediatamente

```csharp
protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        this._session.Flush(); // ✅ Flush solo si NO hay transacción
}
```

### 📊 Comparación: Con vs Sin Transacción

**Escenario 1: CON Transacción (Caso de Uso Complejo)**

```csharp
// Application Layer: CreateUserWithRoleUseCase
public async Task<User> ExecuteAsync(string email, string name, string roleName)
{
    using var transaction = _unitOfWork.BeginTransaction(); // ✅ Inicia transacción

    try
    {
        var user = await _userRepository.CreateAsync(email, name);
        // ✅ NO hace Flush() porque hay transacción activa

        var role = await _roleRepository.GetByNameAsync(roleName);
        user.AddRole(role);
        await _userRepository.SaveAsync(user);
        // ✅ NO hace Flush() porque hay transacción activa

        await _unitOfWork.CommitAsync(); // ✅ Commit hace Flush() de TODO
        return user;
    }
    catch
    {
        await _unitOfWork.RollbackAsync(); // ✅ Rollback deshace cambios
        throw;
    }
}
```

**Escenario 2: SIN Transacción (Operación Simple)**

```csharp
// Application Layer: GetUserByEmailUseCase
public async Task<User?> ExecuteAsync(string email)
{
    // ✅ NO hay transacción para consultas simples
    return await _userRepository.GetByEmailAsync(email);
}

// Application Layer: CreateSimpleUserUseCase
public async Task<User> ExecuteAsync(string email, string name)
{
    // ✅ NO hay transacción, el repositorio hace Flush() automáticamente
    return await _userRepository.CreateAsync(email, name);
    // FlushWhenNotActiveTransaction() se ejecuta dentro de CreateAsync()
}
```

---

## Mejores Prácticas

### ✅ DO: Buenas Prácticas

#### 1. Segregar Interfaces (Read-Only vs Full)

```csharp
// ✅ CORRECTO: Caso de uso de consulta usa IReadOnlyRepository
public class GetUsersUseCase(IReadOnlyRepository<User, Guid> userRepository)
{
    public async Task<IEnumerable<User>> ExecuteAsync()
        => await userRepository.GetAsync();
}

// ✅ CORRECTO: Caso de uso de escritura usa IRepository
public class UpdateUserUseCase(IRepository<User, Guid> userRepository)
{
    public async Task<User> ExecuteAsync(Guid id, string newName)
    {
        var user = await userRepository.GetAsync(id);
        user.UpdateName(newName);
        await userRepository.SaveAsync(user);
        return user;
    }
}
```

#### 2. Métodos Específicos en Repositorios Específicos

```csharp
// ✅ CORRECTO: Método específico para query común
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<bool> EmailExistsAsync(string email);
}
```

#### 3. Validar en Métodos Específicos de Negocio

```csharp
// ✅ CORRECTO: Validación en CreateAsync (método de negocio)
public async Task<User> CreateAsync(string email, string name)
{
    var user = new User(email, name);

    if (!user.IsValid())
        throw new InvalidDomainException(user.Validate());

    if (await EmailExistsAsync(email))
        throw new DuplicatedDomainException($"Email '{email}' already exists.");

    await AddAsync(user);
    FlushWhenNotActiveTransaction();
    return user;
}
```

#### 4. Usar Expression<Func<T, bool>> para Queries Flexibles

```csharp
// ✅ CORRECTO: Expression permite queries complejas
var activeUsers = await userRepository.GetAsync(u => u.IsActive && u.CreatedAt > DateTime.Now.AddDays(-30));
```

#### 5. Retornar IEnumerable en lugar de List

```csharp
// ✅ CORRECTO: IEnumerable permite lazy evaluation
public async Task<IEnumerable<User>> GetAsync(CancellationToken cancellationToken = default)
    => await this._session.Query<User>().ToListAsync(cancellationToken);
```

### ❌ DON'T: Antipatrones

#### 1. NO poner lógica de negocio en base repositories

```csharp
// ❌ INCORRECTO: Validación de negocio en NHRepository
public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>
{
    public T Add(T item)
    {
        // ❌ NO validar reglas de negocio aquí (esto es genérico)
        if (item is User user && user.Email.Contains("@test.com"))
            throw new Exception("Test emails not allowed");

        this._session.Save(item);
        return item;
    }
}

// ✅ CORRECTO: Validación en repositorio específico
public class NHUserRepository : NHRepository<User, Guid>
{
    public async Task<User> CreateAsync(string email, string name)
    {
        if (email.Contains("@test.com"))
            throw new InvalidDomainException("Test emails not allowed");

        var user = new User(email, name);
        await AddAsync(user);
        return user;
    }
}
```

#### 2. NO retornar IQueryable desde repositorio

```csharp
// ❌ INCORRECTO: IQueryable expone detalles del ORM
public IQueryable<User> GetQuery()
    => this._session.Query<User>(); // ❌ Application Layer puede modificar query

// ✅ CORRECTO: Retornar IEnumerable o entidades materializadas
public async Task<IEnumerable<User>> GetAsync()
    => await this._session.Query<User>().ToListAsync();
```

#### 3. NO crear repositorios para TODAS las entidades

```csharp
// ❌ INCORRECTO: Repositorio innecesario
public interface IAddressRepository : IRepository<Address, Guid> { }

// ✅ CORRECTO: Acceder a Address a través de User (agregado)
var user = await userRepository.GetAsync(userId);
var addresses = user.Addresses; // ✅ Address es parte del agregado User
```

#### 4. NO hacer Flush() manual en Application Layer

```csharp
// ❌ INCORRECTO: Application Layer conoce detalles de NHibernate
public class CreateUserUseCase(ISession session) // ❌ Depende de ISession
{
    public async Task ExecuteAsync(string email)
    {
        var user = new User(email);
        await session.SaveAsync(user);
        await session.FlushAsync(); // ❌ Application Layer hace Flush
    }
}

// ✅ CORRECTO: Repositorio maneja Flush internamente
public class CreateUserUseCase(IUserRepository userRepository)
{
    public async Task ExecuteAsync(string email)
    {
        await userRepository.CreateAsync(email); // ✅ Repositorio hace Flush
    }
}
```

---

## Antipatrones Comunes

### ❌ 1. Generic Repository Overuse (Abuso de Repositorio Genérico)

**Problema:** Usar solo `IRepository<T, TKey>` para TODO sin crear repositorios específicos.

```csharp
// ❌ INCORRECTO
public class CreateUserUseCase(IRepository<User, Guid> userRepository)
{
    public async Task ExecuteAsync(string email, string name)
    {
        // ❌ Validación de negocio en Application Layer
        var existingUser = (await userRepository.GetAsync(u => u.Email == email)).FirstOrDefault();
        if (existingUser != null)
            throw new Exception("Email exists");

        var user = new User(email, name);
        await userRepository.AddAsync(user);
    }
}

// ✅ CORRECTO
public class CreateUserUseCase(IUserRepository userRepository)
{
    public async Task ExecuteAsync(string email, string name)
    {
        // ✅ Lógica de negocio encapsulada en repositorio
        await userRepository.CreateAsync(email, name);
    }
}
```

### ❌ 2. Leaky Abstraction (Abstracción que Filtra)

**Problema:** Exponer detalles del ORM (NHibernate) a través del repositorio.

```csharp
// ❌ INCORRECTO
public interface IUserRepository
{
    IQueryable<User> GetQuery(); // ❌ Expone IQueryable de NHibernate
    void Flush(); // ❌ Expone método de NHibernate
}

// ✅ CORRECTO
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAsync(Expression<Func<User, bool>> predicate);
    Task<User?> GetByEmailAsync(string email);
}
```

### ❌ 3. Repository por Tabla (Table-per-Repository)

**Problema:** Crear repositorio para CADA tabla sin considerar agregados.

```csharp
// ❌ INCORRECTO: Muchos repositorios pequeños
public interface IUserRepository : IRepository<User, Guid> { }
public interface IAddressRepository : IRepository<Address, Guid> { }
public interface IPhoneRepository : IRepository<Phone, Guid> { }

// ✅ CORRECTO: Agregado User maneja Address y Phone
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateWithAddressAsync(string email, string address);
}

public class User
{
    public ICollection<Address> Addresses { get; set; } // ✅ Parte del agregado
    public ICollection<Phone> Phones { get; set; }
}
```

### ❌ 4. CRUD Genérico en Domain Layer

**Problema:** Interfaces genéricas que no representan lenguaje de negocio.

```csharp
// ❌ INCORRECTO: Nombres técnicos
public interface IUserRepository
{
    Task Insert(User user);
    Task Update(User user);
    Task Delete(Guid id);
}

// ✅ CORRECTO: Lenguaje de negocio
public interface IUserRepository
{
    Task<User> RegisterUserAsync(string email, string name);
    Task<User> ActivateAccountAsync(Guid userId);
    Task DeactivateAccountAsync(Guid userId);
}
```

---

## Checklist de Implementación

### 📋 Interfaces (Domain Layer)

- [ ] **IReadOnlyRepository<T, TKey>** definida en `domain/interfaces/repositories/`
  - [ ] Métodos síncronos: `Get()`, `Count()`
  - [ ] Métodos asíncronos: `GetAsync()`, `CountAsync()`
  - [ ] Paginación: `GetManyAndCount()`, `GetManyAndCountAsync()`

- [ ] **IRepository<T, TKey>** extiende `IReadOnlyRepository<T, TKey>`
  - [ ] Métodos CRUD: `Add()`, `Save()`, `Delete()`
  - [ ] Versiones async: `AddAsync()`, `SaveAsync()`, `DeleteAsync()`

- [ ] **Repositorios específicos** (ej: `IUserRepository`)
  - [ ] Métodos de negocio con nombres descriptivos
  - [ ] Queries específicas (ej: `GetByEmailAsync()`)

### 📋 Implementaciones (Infrastructure Layer)

- [ ] **NHReadOnlyRepository<T, TKey>** implementa `IReadOnlyRepository<T, TKey>`
  - [ ] Constructor recibe `ISession`
  - [ ] `_session` es `protected internal` para acceso desde clases derivadas
  - [ ] Todos los métodos implementados (sync/async)

- [ ] **NHRepository<T, TKey>** extiende `NHReadOnlyRepository<T, TKey>`
  - [ ] Constructor recibe `ISession` y `IServiceProvider`
  - [ ] Resuelve `AbstractValidator<T>` desde `IServiceProvider`
  - [ ] `Add()` y `Save()` validan con FluentValidation
  - [ ] `FlushWhenNotActiveTransaction()` implementado

- [ ] **Repositorios específicos** (ej: `NHUserRepository`)
  - [ ] Extiende `NHRepository<T, TKey>`
  - [ ] Implementa interfaz específica (ej: `IUserRepository`)
  - [ ] Métodos de negocio con validaciones específicas

### 📋 Validación

- [ ] **FluentValidation** configurada
  - [ ] Validadores registrados en DI
  - [ ] Validadores resueltos en `NHRepository<T, TKey>`

- [ ] **Validaciones de negocio** en repositorios específicos
  - [ ] Validación de unicidad (email, username, etc.)
  - [ ] Validaciones con queries a BD

### 📋 Session Management

- [ ] **FlushWhenNotActiveTransaction()** se llama en:
  - [ ] `Add()` (sync)
  - [ ] `Save()` (sync)
  - [ ] `Delete()` (sync)
  - [ ] Métodos específicos de negocio (ej: `CreateAsync()`)

---

## Ejemplos Completos

### Ejemplo 1: Flujo Completo de Usuario

#### Domain Layer

```csharp
// domain/entities/User.cs
public class User
{
    public Guid Id { get; protected set; }
    public string Email { get; protected set; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }

    protected User() { } // Constructor para NHibernate

    public User(string email, string name)
    {
        Id = Guid.NewGuid();
        Email = email;
        Name = name;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

// domain/interfaces/repositories/IUserRepository.cs
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateAsync(string email, string name);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
}
```

#### Infrastructure Layer

```csharp
// infrastructure/nhibernate/NHUserRepository.cs
public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        var user = new User(email, name);

        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"Email '{email}' already exists.");

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _session.Query<User>()
            .Where(u => u.IsActive)
            .ToListAsync();
    }
}
```

#### Application Layer

```csharp
// application/usecases/CreateUserUseCase.cs
public class CreateUserUseCase(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<User> ExecuteAsync(string email, string name)
    {
        // ✅ Toda la lógica está en el repositorio
        return await _userRepository.CreateAsync(email, name);
    }
}

// application/usecases/GetActiveUsersUseCase.cs
public class GetActiveUsersUseCase(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<IEnumerable<User>> ExecuteAsync()
    {
        return await _userRepository.GetActiveUsersAsync();
    }
}
```

### Ejemplo 2: Role Repository con Métodos Específicos

```csharp
// Domain Layer
public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role> CreateAsync(string name);
    Task<Role?> GetByNameAsync(string name);
    Task CreateDefaultRoles();
}

// Infrastructure Layer
public class NHRoleRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<Role, Guid>(session, serviceProvider), IRoleRepository
{
    public async Task<Role> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException("Role name cannot be null or empty.");

        if (await GetByNameAsync(name) != null)
            throw new DuplicatedDomainException($"Role '{name}' already exists.");

        var role = new Role(name);
        await AddAsync(role);
        this.FlushWhenNotActiveTransaction();
        return role;
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _session.Query<Role>()
            .Where(r => r.Name == name)
            .SingleOrDefaultAsync();
    }

    public async Task CreateDefaultRoles()
    {
        var defaultRoles = new[] { "Admin", "User", "Guest" };

        foreach (var roleName in defaultRoles)
        {
            var existingRole = await GetByNameAsync(roleName);
            if (existingRole == null)
                await CreateAsync(roleName);
        }
    }
}
```

---

## Recursos Adicionales

### 📚 Guías Relacionadas

- [Core Concepts](./core-concepts.md) - Conceptos fundamentales de Infrastructure Layer
- [Unit of Work Pattern](./unit-of-work-pattern.md) - Manejo de transacciones
- [Dependency Injection](./dependency-injection.md) - Registro de repositorios en DI
- [NHibernate Session Lifecycle](../nhibernate/session-lifecycle.md) - Ciclo de vida del ISession

### 🔗 Referencias Externas

- [Repository Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/repository.html)
- [NHibernate Documentation](https://nhibernate.info/)
- [FluentValidation](https://docs.fluentvalidation.net/)

---

**Versión:** 1.0.0
**Fecha:** 2025-01-14
**Autor:** Equipo de Arquitectura
