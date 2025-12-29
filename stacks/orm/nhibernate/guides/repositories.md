# NHibernate Repositories
**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [Introducción](#introducción)
2. [NHReadOnlyRepository](#nhreadonlyrepository)
3. [NHRepository](#nhrepository)
4. [Repositorios Específicos](#repositorios-específicos)
5. [Validación con FluentValidation](#validación-con-fluentvalidation)
6. [FlushWhenNotActiveTransaction](#flushwhennotactivetransaction)
7. [Patrón de Duplicados](#patrón-de-duplicados)
8. [Mejores Prácticas](#mejores-prácticas)
9. [Antipatrones](#antipatrones)
10. [Checklist de Implementación](#checklist-de-implementación)
11. [Ejemplos Completos](#ejemplos-completos)

---

## Introducción

Los **repositorios NHibernate** en el proyecto hashira.stone.backend siguen una jerarquía de herencia que proporciona funcionalidad incremental:

```
┌─────────────────────────────────┐
│  IReadOnlyRepository<T, TKey>   │ ← Interface (Domain Layer)
└─────────────────────────────────┘
              ▲
              │ Implementa
              │
┌─────────────────────────────────┐
│ NHReadOnlyRepository<T, TKey>   │ ← Base (Solo lectura)
│  • Get(), GetAsync()            │
│  • Count(), CountAsync()        │
│  • GetManyAndCount()            │
│  • LINQ to NHibernate           │
│  • Dynamic filtering            │
└─────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────┐
│    NHRepository<T, TKey>        │ ← Base (CRUD + Validación)
│  • Add(), AddAsync()            │
│  • Save(), SaveAsync()          │
│  • Delete(), DeleteAsync()      │
│  • FluentValidation             │
│  • FlushWhenNotActiveTransaction│
└─────────────────────────────────┘
              ▲
              │ Hereda
              │
┌─────────────────────────────────┐
│  NHPrototypeRepository          │ ← Específico
│  • CreateAsync()                │
│  • GetByNumberAsync()           │
│  • Validación de duplicados     │
└─────────────────────────────────┘
```

---

## NHReadOnlyRepository

### 🔍 Propósito

Repositorio base para **operaciones de solo lectura** usando LINQ to NHibernate.

### 📦 Implementación Completa

```csharp
using System.Linq.Expressions;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.infrastructure.nhibernate.filtering;
using System.Linq.Dynamic.Core;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHReadOnlyRepository<T, TKey>(ISession session) : IReadOnlyRepository<T, TKey>
    where T : class, new()
{
    protected internal readonly ISession _session = session;

    // ─────────────────────────────────────────────────────────
    // COUNT OPERATIONS
    // ─────────────────────────────────────────────────────────

    public int Count()
        => _session.QueryOver<T>().RowCount();

    public int Count(Expression<Func<T, bool>> query)
        => _session.Query<T>().Where(query).Count();

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _session.Query<T>().CountAsync(cancellationToken);

    public Task<int> CountAsync(
        Expression<Func<T, bool>> query,
        CancellationToken cancellationToken = default)
        => _session.Query<T>().Where(query).CountAsync(cancellationToken);

    // ─────────────────────────────────────────────────────────
    // GET BY ID
    // ─────────────────────────────────────────────────────────

    public T Get(TKey id)
        => _session.Get<T>(id);

    public Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default)
        => _session.GetAsync<T>(id, cancellationToken);

    // ─────────────────────────────────────────────────────────
    // GET ALL / WITH FILTER
    // ─────────────────────────────────────────────────────────

    public IEnumerable<T> Get()
        => _session.Query<T>();

    public IEnumerable<T> Get(Expression<Func<T, bool>> query)
        => _session.Query<T>().Where(query);

    public async Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default)
        => await _session.Query<T>().ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>> query,
        CancellationToken cancellationToken = default)
        => await _session.Query<T>()
            .Where(query)
            .ToListAsync(cancellationToken);

    // ─────────────────────────────────────────────────────────
    // GET WITH PAGINATION
    // ─────────────────────────────────────────────────────────

    public IEnumerable<T> Get(
        Expression<Func<T, bool>> query,
        int page,
        int pageSize,
        SortingCriteria sortingCriteria)
        => _session.Query<T>()
            .Where(query)
            .OrderBy(sortingCriteria.ToExpression())
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

    // ─────────────────────────────────────────────────────────
    // GET MANY AND COUNT (Dynamic Query String)
    // ─────────────────────────────────────────────────────────

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
        CancellationToken cancellationToken = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute queries sequentially to avoid DataReader conversion issues
        var total = await _session.Query<T>()
            .Where(expression)
            .CountAsync(cancellationToken);

        var items = await _session.Query<T>()
            .OrderBy(sortingCriteria.ToExpression())
            .Where(expression)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    // ─────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────

    private static (
        Expression<Func<T, bool>> expression,
        int pageNumber,
        int pageSize,
        SortingCriteria sortingCriteria)
        PrepareQuery(string? query, string defaultSorting)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
        QueryStringParser queryStringParser = new(queryString);

        // Get pagination info
        int pageNumber = queryStringParser.ParsePageNumber();
        int pageSize = queryStringParser.ParsePageSize();

        // Get sorting info
        Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
        SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
            ? SortingCriteriaType.Descending
            : SortingCriteriaType.Ascending;
        SortingCriteria sortingCriteria = new(sorting.By, directions);

        // Get filters
        IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
        QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
        var expression = FilterExpressionParser.ParsePredicate<T>(filters);
        if (quickSearch != null)
            expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

        return (expression, pageNumber, pageSize, sortingCriteria);
    }
}
```

### 🔑 Características Clave

- ✅ **Solo lectura**: No modifica datos
- ✅ **LINQ to NHibernate**: Queries type-safe
- ✅ **Paginación**: Skip/Take para grandes datasets
- ✅ **Sorting**: Ordenamiento dinámico
- ✅ **Filtering**: Query string parsing
- ✅ **Async/Await**: Operaciones no bloqueantes

---

## NHRepository

### 🔧 Propósito

Repositorio base para **operaciones CRUD completas** con **validación automática** usando FluentValidation.

### 📦 Implementación Completa

```csharp
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using FluentValidation;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey>
    where T : class, new()
{
    private readonly AbstractValidator<T> validator;

    protected NHRepository(ISession session, IServiceProvider serviceProvider)
        : base(session)
    {
        Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
        this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T>
            ?? throw new InvalidOperationException($"Validator for {typeof(T)} could not be created");
    }

    // ─────────────────────────────────────────────────────────
    // ADD (Sync)
    // ─────────────────────────────────────────────────────────

    public T Add(T item)
    {
        // ✅ VALIDACIÓN AUTOMÁTICA
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Save(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    // ─────────────────────────────────────────────────────────
    // ADD (Async - Sin validación automática)
    // ─────────────────────────────────────────────────────────

    public Task AddAsync(T item)
        => this._session.SaveAsync(item);

    // ─────────────────────────────────────────────────────────
    // SAVE/UPDATE (Sync)
    // ─────────────────────────────────────────────────────────

    public T Save(T item)
    {
        // ✅ VALIDACIÓN AUTOMÁTICA
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Update(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    // ─────────────────────────────────────────────────────────
    // SAVE/UPDATE (Async - Sin validación automática)
    // ─────────────────────────────────────────────────────────

    public Task SaveAsync(T item)
        => this._session.UpdateAsync(item);

    // ─────────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────────

    public void Delete(T item)
    {
        this._session.Delete(item);
        this.FlushWhenNotActiveTransaction();
    }

    public Task DeleteAsync(T item)
        => this._session.DeleteAsync(item);

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    protected internal bool IsTransactionActive()
        => this._session.GetCurrentTransaction() != null
           && this._session.GetCurrentTransaction().IsActive;

    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

### 🔑 Características Clave

- ✅ **Validación automática**: FluentValidation en Add() y Save()
- ✅ **CRUD completo**: Create, Read, Update, Delete
- ✅ **Flush condicional**: FlushWhenNotActiveTransaction()
- ✅ **IServiceProvider**: Resolución dinámica de validators
- ✅ **Excepciones de dominio**: InvalidDomainException

---

## Repositorios Específicos

### 📋 Ejemplo 1: NHPrototypeRepository

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHPrototypeRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<Prototype, Guid>(session, serviceProvider), IPrototypeRepository
{
    /// <summary>
    /// Creates a new Prototype instance and persists it to the database.
    /// </summary>
    public async Task<Prototype> CreateAsync(
        string number,
        DateTime issueDate,
        DateTime expirationDate,
        string status)
    {
        // 1. Crear entidad
        var prototype = new Prototype(number, issueDate, expirationDate, status);

        // 2. Validación de dominio
        if (!prototype.IsValid())
            throw new InvalidDomainException(prototype.Validate());

        // 3. Verificar duplicados
        var count = await this.CountAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant());
        if (count > 0)
            throw new DuplicatedDomainException($"A prototype with number '{number}' already exists.");

        // 4. Persistir
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

        var prototype = await this.GetAsync(
            p => p.Number.ToLowerInvariant() == number.ToLowerInvariant(),
            ct);
        return prototype.FirstOrDefault();
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

---

### 📋 Ejemplo 2: NHTechnicalStandardRepository

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHTechnicalStandardRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<TechnicalStandard, Guid>(session, serviceProvider), ITechnicalStandardRepository
{
    /// <summary>
    /// Creates a new technical standard with the specified details.
    /// </summary>
    public async Task<TechnicalStandard> CreateAsync(
        string code,
        string name,
        string edition,
        string status,
        string type)
    {
        // 1. Crear y validar
        var technicalStandard = new TechnicalStandard(code, name, edition, status, type);
        if (!technicalStandard.IsValid())
            throw new InvalidDomainException(technicalStandard.Validate());

        // 2. Verificar duplicados usando GetByCodeAsync
        var existing = await GetByCodeAsync(code);
        if (existing != null)
            throw new DuplicatedDomainException($"A technical standard with code '{code}' already exists.");

        // 3. Persistir
        await AddAsync(technicalStandard);
        this.FlushWhenNotActiveTransaction();
        return technicalStandard;
    }

    /// <summary>
    /// Retrieves a technical standard by its code (HQL with unaccent).
    /// </summary>
    public async Task<TechnicalStandard?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var hql = @"
            from TechnicalStandard ts
            where lower(unaccent(ts.Code)) = lower(unaccent(:code))";

        return await _session.CreateQuery(hql)
            .SetParameter("code", code)
            .UniqueResultAsync<TechnicalStandard?>();
    }

    /// <summary>
    /// Updates an existing technical standard.
    /// </summary>
    public async Task<TechnicalStandard> UpdateAsync(
        Guid id,
        string code,
        string name,
        string edition,
        string status,
        string type)
    {
        // 1. Obtener entidad existente
        var technicalStandard = await _session.Query<TechnicalStandard>()
            .Where(ts => ts.Id == id)
            .SingleOrDefaultAsync();

        if (technicalStandard == null)
            throw new ResourceNotFoundException($"Technical standard with id '{id}' does not exist.");

        // 2. Verificar duplicados
        var existingWithCode = await GetByCodeAsync(code);
        if (existingWithCode != null && existingWithCode.Id != id)
            throw new DuplicatedDomainException($"A technical standard with code '{code}' already exists.");

        // 3. Actualizar propiedades
        technicalStandard.Code = code;
        technicalStandard.Name = name;
        technicalStandard.Edition = edition;
        technicalStandard.Status = status;
        technicalStandard.Type = type;

        // 4. Validar
        if (!technicalStandard.IsValid())
            throw new InvalidDomainException(technicalStandard.Validate());

        // 5. Persistir
        await _session.UpdateAsync(technicalStandard);
        this.FlushWhenNotActiveTransaction();
        return technicalStandard;
    }

    /// <summary>
    /// Retrieves a technical standard by its ID.
    /// </summary>
    public async Task<TechnicalStandard?> GetByIdAsync(Guid id)
    {
        var technicalStandard = await _session.Query<TechnicalStandard>()
            .Where(ts => ts.Id == id)
            .SingleOrDefaultAsync();
        return technicalStandard;
    }
}
```

---

## Validación con FluentValidation

### 🔍 Patrón de Validación

```csharp
// 1. Resolver validator desde IServiceProvider en constructor
protected NHRepository(ISession session, IServiceProvider serviceProvider)
    : base(session)
{
    Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
    this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T>
        ?? throw new InvalidOperationException($"Validator for {typeof(T)} could not be created");
}

// 2. Validar en Add() y Save()
public T Add(T item)
{
    var validationResult = this.validator.Validate(item);
    if (!validationResult.IsValid)
        throw new InvalidDomainException(validationResult.Errors);

    this._session.Save(item);
    this.FlushWhenNotActiveTransaction();
    return item;
}
```

### ✅ Ventajas

- ✅ **Validación centralizada** en repositorio base
- ✅ **Reutilizable** en todos los repositorios
- ✅ **Excepciones de dominio** consistentes
- ✅ **Validators registrados** en DI

### 📋 Registro de Validators en DI

```csharp
public static IServiceCollection ConfigureValidators(this IServiceCollection services)
{
    services.AddScoped<AbstractValidator<User>, UserValidator>();
    services.AddScoped<AbstractValidator<Prototype>, PrototypeValidator>();
    services.AddScoped<AbstractValidator<TechnicalStandard>, TechnicalStandardValidator>();

    return services;
}
```

---

## FlushWhenNotActiveTransaction

### 🔍 ¿Qué hace?

Este método **fuerza el flush de cambios** al base de datos **solo si NO hay una transacción activa**.

### 📦 Implementación

```csharp
protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        this._session.Flush();
}
```

### 🎯 Casos de Uso

| Escenario | Comportamiento |
|-----------|----------------|
| **Sin transacción** | ✅ **Flush inmediato** - Cambios persisten inmediatamente |
| **Con transacción activa** | ⏳ **Flush al Commit** - Cambios persisten al hacer Commit |

### 📋 Ejemplo de Uso

```csharp
// CASO 1: Sin transacción explícita
public async Task<Prototype> CreateAsync(string number, ...)
{
    var prototype = new Prototype(number, ...);
    await AddAsync(prototype);
    this.FlushWhenNotActiveTransaction(); // ✅ FLUSH INMEDIATO
    return prototype;
}

// CASO 2: Con transacción (desde UseCase)
_unitOfWork.BeginTransaction();
try
{
    var prototype = await _unitOfWork.Prototypes.CreateAsync(number, ...);
    // FlushWhenNotActiveTransaction() NO hace flush aquí
    _unitOfWork.Commit(); // ✅ FLUSH AL HACER COMMIT
}
catch
{
    _unitOfWork.Rollback();
}
```

---

## Patrón de Duplicados

### 🔍 Verificar antes de Crear

```csharp
public async Task<Prototype> CreateAsync(string number, ...)
{
    var prototype = new Prototype(number, ...);

    // ✅ VALIDACIÓN DE DUPLICADOS
    var count = await this.CountAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant());
    if (count > 0)
        throw new DuplicatedDomainException($"A prototype with number '{number}' already exists.");

    await AddAsync(prototype);
    this.FlushWhenNotActiveTransaction();
    return prototype;
}
```

### 🔍 Verificar al Actualizar

```csharp
public async Task<TechnicalStandard> UpdateAsync(Guid id, string code, ...)
{
    var technicalStandard = await GetByIdAsync(id);
    if (technicalStandard == null)
        throw new ResourceNotFoundException($"Technical standard with id '{id}' does not exist.");

    // ✅ VERIFICAR DUPLICADOS (excluyendo el actual)
    var existingWithCode = await GetByCodeAsync(code);
    if (existingWithCode != null && existingWithCode.Id != id)
        throw new DuplicatedDomainException($"A technical standard with code '{code}' already exists.");

    technicalStandard.Code = code;
    // ... actualizar propiedades

    await _session.UpdateAsync(technicalStandard);
    this.FlushWhenNotActiveTransaction();
    return technicalStandard;
}
```

---

## Mejores Prácticas

### ✅ 1. Métodos CreateAsync específicos

```csharp
// ✅ CORRECTO
public async Task<Prototype> CreateAsync(string number, DateTime issueDate, ...)
{
    var prototype = new Prototype(number, issueDate, ...);
    if (!prototype.IsValid())
        throw new InvalidDomainException(prototype.Validate());

    // Validación de duplicados
    await AddAsync(prototype);
    return prototype;
}

// ❌ INCORRECTO - Usar Add() directamente desde UseCase
var prototype = new Prototype(...);
_unitOfWork.Prototypes.Add(prototype); // Sin encapsulación
```

---

### ✅ 2. Validación en dos niveles

```csharp
// ✅ CORRECTO - Doble validación
public async Task<Prototype> CreateAsync(...)
{
    var prototype = new Prototype(...);

    // Nivel 1: Validación de dominio (entidad)
    if (!prototype.IsValid())
        throw new InvalidDomainException(prototype.Validate());

    // Nivel 2: Validación de negocio (duplicados)
    var count = await this.CountAsync(p => p.Number == number);
    if (count > 0)
        throw new DuplicatedDomainException(...);

    await AddAsync(prototype);
    return prototype;
}
```

---

### ✅ 3. ToLowerInvariant para búsquedas

```csharp
// ✅ CORRECTO - Case-insensitive
var count = await this.CountAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant());

// ❌ INCORRECTO - Case-sensitive
var count = await this.CountAsync(p => p.Number == number);
```

---

### ✅ 4. Métodos GetByXXX específicos

```csharp
// ✅ CORRECTO
public async Task<Prototype?> GetByNumberAsync(string number, CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(number))
        return null;

    var prototype = await this.GetAsync(
        p => p.Number.ToLowerInvariant() == number.ToLowerInvariant(),
        ct);
    return prototype.FirstOrDefault();
}
```

---

### ✅ 5. HQL con unaccent para búsquedas avanzadas

```csharp
// ✅ CORRECTO - HQL con unaccent (para búsquedas sin acentos)
public async Task<TechnicalStandard?> GetByCodeAsync(string code)
{
    var hql = @"
        from TechnicalStandard ts
        where lower(unaccent(ts.Code)) = lower(unaccent(:code))";

    return await _session.CreateQuery(hql)
        .SetParameter("code", code)
        .UniqueResultAsync<TechnicalStandard?>();
}
```

---

## Antipatrones

### ❌ 1. Usar Add() directamente sin CreateAsync()

```csharp
// ❌ INCORRECTO - Sin encapsulación
var prototype = new Prototype(number, ...);
_unitOfWork.Prototypes.Add(prototype);

// ✅ CORRECTO - Método CreateAsync
var prototype = await _unitOfWork.Prototypes.CreateAsync(number, ...);
```

---

### ❌ 2. No validar duplicados

```csharp
// ❌ INCORRECTO - Sin validación de duplicados
public async Task<Prototype> CreateAsync(string number, ...)
{
    var prototype = new Prototype(number, ...);
    await AddAsync(prototype);
    return prototype; // ⚠️ Puede crear duplicados
}

// ✅ CORRECTO
public async Task<Prototype> CreateAsync(string number, ...)
{
    var prototype = new Prototype(number, ...);

    var count = await this.CountAsync(p => p.Number.ToLowerInvariant() == number.ToLowerInvariant());
    if (count > 0)
        throw new DuplicatedDomainException(...);

    await AddAsync(prototype);
    return prototype;
}
```

---

### ❌ 3. No usar FlushWhenNotActiveTransaction()

```csharp
// ❌ INCORRECTO - Sin Flush
public async Task<Prototype> CreateAsync(...)
{
    var prototype = new Prototype(...);
    await AddAsync(prototype);
    return prototype; // ⚠️ Cambios pueden no persistir sin transacción
}

// ✅ CORRECTO
public async Task<Prototype> CreateAsync(...)
{
    var prototype = new Prototype(...);
    await AddAsync(prototype);
    this.FlushWhenNotActiveTransaction();
    return prototype;
}
```

---

### ❌ 4. Registrar repositorios en DI

```csharp
// ❌ INCORRECTO - Repositorios NO se registran en DI
services.AddScoped<IPrototypeRepository, NHPrototypeRepository>();

// ✅ CORRECTO - Solo registrar IUnitOfWork
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

---

### ❌ 5. Usar FirstOrDefault() sin validación

```csharp
// ❌ INCORRECTO - Puede lanzar excepción si hay múltiples resultados
public async Task<Prototype?> GetByIdAsync(Guid id)
{
    return await _session.Query<Prototype>()
        .Where(p => p.Id == id)
        .FirstOrDefaultAsync(); // ⚠️ Retorna el primero incluso si hay múltiples
}

// ✅ CORRECTO - SingleOrDefaultAsync para claves únicas
public async Task<Prototype?> GetByIdAsync(Guid id)
{
    return await _session.Query<Prototype>()
        .Where(p => p.Id == id)
        .SingleOrDefaultAsync(); // ✅ Lanza excepción si hay múltiples
}
```

---

## Checklist de Implementación

### ✅ Antes de Crear Repository

- [ ] Interface IXRepository definida en Domain Layer
- [ ] Heredar de IRepository<T, TKey> o IReadOnlyRepository<T, TKey>
- [ ] Validator AbstractValidator<T> creado
- [ ] Validator registrado en DI

### ✅ Durante la Implementación

- [ ] Clase NHXRepository hereda de NHRepository<T, TKey>
- [ ] Constructor recibe ISession y IServiceProvider
- [ ] Método CreateAsync() implementado
- [ ] Validación de dominio (IsValid())
- [ ] Validación de duplicados (CountAsync o GetByXXXAsync)
- [ ] FlushWhenNotActiveTransaction() llamado
- [ ] Métodos GetByXXX específicos implementados
- [ ] Excepciones de dominio lanzadas correctamente

### ✅ Después de la Implementación

- [ ] Tests unitarios creados
- [ ] Repository accesible desde IUnitOfWork
- [ ] Documentación XML agregada
- [ ] Casos de uso usando CreateAsync()

---

## Ejemplos Completos

### 📋 Ejemplo 1: Repository Read-Only para DAOs

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Read-only repository for PrototypeDao (no modifications allowed)
/// </summary>
public class NHPrototypeDaoRepository(ISession session)
    : NHReadOnlyRepository<PrototypeDao, Guid>(session), IPrototypeDaoRepository
{
    // Solo heredamos métodos de lectura de NHReadOnlyRepository
    // No hay métodos de escritura (Add, Save, Delete)
}
```

---

### 📋 Ejemplo 2: Repository Completo con CRUD

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Create a new user with the specified email and name.
    /// </summary>
    public async Task<User> CreateAsync(string email, string name)
    {
        // 1. Crear entidad
        var user = new User { Email = email, Name = name };

        // 2. Validación de dominio
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // 3. Validación de duplicados
        var existing = await GetByEmailAsync(email);
        if (existing != null)
            throw new DuplicatedDomainException($"A user with email '{email}' already exists.");

        // 4. Persistir
        await AddAsync(user);
        this.FlushWhenNotActiveTransaction();
        return user;
    }

    /// <summary>
    /// Get a user by their email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _session.Query<User>()
            .Where(u => u.Email.ToLowerInvariant() == email.ToLowerInvariant())
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Update user name.
    /// </summary>
    public async Task<User> UpdateNameAsync(Guid id, string newName)
    {
        var user = await GetAsync(id);
        if (user == null)
            throw new ResourceNotFoundException($"User with id '{id}' not found.");

        user.Name = newName;

        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        await _session.UpdateAsync(user);
        this.FlushWhenNotActiveTransaction();
        return user;
    }
}
```

---

## 📚 Referencias

- [Core Concepts - Repository Pattern](../../repository-pattern.md)
- [NHibernate README](./README.md)
- [Unit of Work Pattern](./unit-of-work.md)
- [Queries](./queries.md)

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de Repositories guide    |

---

**Siguiente**: [Mappers](./mappers.md) - ClassMapping patterns →
