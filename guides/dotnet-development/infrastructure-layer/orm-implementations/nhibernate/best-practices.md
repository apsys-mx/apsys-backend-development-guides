# NHibernate Best Practices

**Version**: 1.0.0
**Última actualización**: 2025-11-14

---

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Configuración Inicial](#configuración-inicial)
3. [Mapping Best Practices](#mapping-best-practices)
4. [Repository Best Practices](#repository-best-practices)
5. [Query Best Practices](#query-best-practices)
6. [Transaction Management](#transaction-management)
7. [Session Management](#session-management)
8. [Performance Optimization](#performance-optimization)
9. [Security](#security)
10. [Testing](#testing)
11. [Common Pitfalls](#common-pitfalls)
12. [Production Checklist](#production-checklist)
13. [Referencias](#referencias)

---

## Introducción

Esta guía consolida las **mejores prácticas** para trabajar con NHibernate en proyectos empresariales, basadas en:

- ✅ Proyecto de referencia: `hashira.stone.backend`
- ✅ Documentación oficial de NHibernate
- ✅ Experiencia en producción
- ✅ Patrones de diseño probados (DDD, CQRS, Repository, Unit of Work)

### Principios Fundamentales

| Principio | Descripción |
|-----------|-------------|
| **Session Per Request** | Una sesión por request HTTP |
| **Explicit Transactions** | Transacciones explícitas para escrituras |
| **Mapping by Code** | Mapeos type-safe sin XML |
| **LINQ First** | LINQ sobre HQL cuando sea posible |
| **Async/Await** | Operaciones asíncronas para I/O |
| **Dispose Pattern** | Liberar recursos correctamente |

---

## Configuración Inicial

### ✅ 1. Dependency Injection

```csharp
public static IServiceCollection ConfigureUnitOfWork(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 1. Connection String desde variables de entorno
    string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();

    // 2. SessionFactory: Singleton (thread-safe, costoso de crear)
    var factory = new NHSessionFactory(connectionString);
    var sessionFactory = factory.BuildNHibernateSessionFactory();
    services.AddSingleton(sessionFactory);

    // 3. ISession: Scoped (una por request HTTP)
    services.AddScoped(factory => sessionFactory.OpenSession());

    // 4. IUnitOfWork: Scoped (envuelve ISession)
    services.AddScoped<IUnitOfWork, NHUnitOfWork>();

    return services;
}
```

**Por qué**:
- ✅ SessionFactory es **thread-safe** y debe ser **Singleton**
- ✅ ISession es **NOT thread-safe** y debe ser **Scoped**
- ✅ IUnitOfWork debe tener el mismo lifetime que ISession

---

### ✅ 2. SessionFactory Configuration

```csharp
public class NHSessionFactory
{
    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();

        // ✅ Registro automático de mappers
        mapper.AddMappings(typeof(UserMapper).Assembly.ExportedTypes);

        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;

            // ✅ Configuración para producción
            #if !DEBUG
            c.LogSqlInConsole = false;
            c.LogFormattedSql = false;
            #endif
        });

        cfg.AddMapping(domainMapping);

        return cfg.BuildSessionFactory();
    }
}
```

**Por qué**:
- ✅ Registro automático de mappers (no olvidar ninguno)
- ✅ AutoQuote para keywords reservados de SQL
- ✅ Logs de SQL solo en desarrollo

---

### ✅ 3. Connection String desde Variables de Entorno

```csharp
public static class ConnectionStringBuilder
{
    public static string BuildPostgresConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "mydb";
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("DB_PASSWORD environment variable is required");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}
```

**Por qué**:
- ✅ Secrets no están en el código
- ✅ Fácil configuración por ambiente (dev, staging, prod)
- ✅ Cumple con las 12-factor app

---

## Mapping Best Practices

### ✅ 1. Mapping by Code (ClassMapping<T>)

```csharp
// ✅ BIEN: Mapping by Code
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema("app");
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);  // ✅ DDD: Dominio controla ID
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Unique(true);
            map.Type(NHibernateUtil.String);
        });
    }
}

// ❌ MAL: XML mapping (evitar)
// <hibernate-mapping xmlns="...">
//   <class name="User" table="users">
//     ...
//   </class>
// </hibernate-mapping>
```

**Por qué**:
- ✅ Type-safe (errores en compile-time)
- ✅ IntelliSense y refactoring
- ✅ Sin archivos XML adicionales

---

### ✅ 2. Generators.Assigned para DDD

```csharp
Id(x => x.Id, map =>
{
    map.Generator(Generators.Assigned);  // ✅ Dominio genera el ID
});
```

**Por qué**:
- ✅ El **dominio controla** la generación de IDs
- ✅ IDs disponibles antes de `Save()`
- ✅ Facilita testing y consistencia

---

### ✅ 3. Separar Entity vs DAO Mappers

```csharp
// ✅ Entity Mapper (CRUD)
public class TechnicalStandardMapper : ClassMapping<TechnicalStandard>
{
    public TechnicalStandardMapper()
    {
        Schema("app");
        Table("technical_standards");
        // Mutable = true (default)
    }
}

// ✅ DAO Mapper (Read-Only)
public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema("app");
        Table("technical_standards_view");  // ✅ Vista de BD
        Mutable(false);  // ✅ Read-only
    }
}
```

**Por qué**:
- ✅ Separación CQRS (Command vs Query)
- ✅ DAOs para vistas de BD optimizadas
- ✅ Entidades para operaciones de escritura

---

### ✅ 4. NHibernateUtil Types Explícitos

```csharp
Property(x => x.Email, map =>
{
    map.Type(NHibernateUtil.String);  // ✅ Explícito
});

Property(x => x.CreatedAt, map =>
{
    map.Type(NHibernateUtil.DateTime);  // ✅ Explícito
});

Property(x => x.Id, map =>
{
    map.Type(NHibernateUtil.Guid);  // ✅ Explícito
});
```

**Por qué**:
- ✅ Mapeo correcto entre .NET ↔ PostgreSQL
- ✅ Evita conversiones implícitas incorrectas
- ✅ Claridad en el mapeo

---

### ✅ 5. Many-to-Many con Bag

```csharp
Bag(x => x.Roles, map =>
{
    map.Table("user_in_roles");  // ✅ Tabla intermedia
    map.Key(k => k.Column("user_id"));
    map.Cascade(Cascade.All);
    map.Inverse(false);  // ✅ User es el owner
},
map => map.ManyToMany(m =>
{
    m.Column("role_id");
    m.Class(typeof(Role));
}));
```

**Por qué**:
- ✅ Bag es más eficiente que Set para many-to-many
- ✅ Cascade.All para persistencia automática
- ✅ Inverse(false) indica el lado owner

---

## Repository Best Practices

### ✅ 1. Herencia con Repositorios Base

```csharp
// ✅ CORRECTO: Jerarquía de repositorios
NHReadOnlyRepository<T, TKey>           // Base read-only
    ↓
NHRepository<T, TKey>                   // Base CRUD + validación
    ↓
NHUserRepository                        // Específico con métodos de negocio
```

**Por qué**:
- ✅ Reutilización de código
- ✅ Validación centralizada
- ✅ Métodos específicos en repositorios concretos

---

### ✅ 2. FlushWhenNotActiveTransaction Pattern

```csharp
public T Add(T item)
{
    // 1. Validación
    var validationResult = this.validator.Validate(item);
    if (!validationResult.IsValid)
        throw new InvalidDomainException(validationResult.Errors);

    // 2. Guardar en sesión
    this._session.Save(item);

    // 3. ✅ Flush si NO hay transacción activa
    this.FlushWhenNotActiveTransaction();

    return item;
}

protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        this._session.Flush();
}
```

**Por qué**:
- ✅ Con transacción: Flush al Commit (batch)
- ✅ Sin transacción: Flush inmediato
- ✅ Flexibilidad sin cambiar código

---

### ✅ 3. Validación con FluentValidation

```csharp
public class NHRepository<T, TKey> where T : AbstractDomainObject
{
    protected internal readonly AbstractValidator<T> validator;

    public NHRepository(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        // ✅ Inyección de validador desde DI
        _validator = serviceProvider.GetRequiredService<AbstractValidator<T>>();
    }

    public T Add(T item)
    {
        // ✅ Validación antes de persistir
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Save(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }
}
```

**Por qué**:
- ✅ Validación antes de INSERT/UPDATE
- ✅ Errores de validación claros
- ✅ Consistencia de datos garantizada

---

### ✅ 4. Async/Await para I/O

```csharp
// ✅ CORRECTO: Async
public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync(ct);
}

// ❌ INCORRECTO: Blocking I/O
public User? GetByEmail(string email)
{
    return _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefault();  // ❌ Bloquea thread
}
```

**Por qué**:
- ✅ No bloquea threads del thread pool
- ✅ Mejor escalabilidad
- ✅ ASP.NET Core optimizado para async

---

## Query Best Practices

### ✅ 1. LINQ sobre HQL

```csharp
// ✅ CORRECTO: LINQ to NHibernate
var users = await _session.Query<User>()
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

// ⚠️ EVITAR: HQL (solo cuando LINQ no puede)
var users = _session.CreateQuery("from User u where u.Email like '%@example.com%'")
    .List<User>();
```

**Por qué**:
- ✅ LINQ es type-safe
- ✅ Errores en compile-time
- ✅ IntelliSense y refactoring

---

### ✅ 2. Paginación Eficiente

```csharp
// ✅ CORRECTO: Skip/Take
public IEnumerable<T> Get(
    Expression<Func<T, bool>> query,
    int pageNumber,
    int pageSize,
    SortingCriteria sortingCriteria)
{
    return _session.Query<T>()
        .Where(query)
        .OrderBy(sortingCriteria.ToExpression())
        .Skip((pageNumber - 1) * pageSize)  // ✅ OFFSET
        .Take(pageSize);                    // ✅ LIMIT
}

// ❌ INCORRECTO: ToList() antes de paginar
var allUsers = _session.Query<User>().ToList();  // ❌ Carga TODO
var page = allUsers.Skip(offset).Take(limit);
```

**Por qué**:
- ✅ Skip/Take se traduce a OFFSET/LIMIT en SQL
- ✅ Solo trae los registros necesarios
- ❌ ToList() carga TODA la tabla en memoria

---

### ✅ 3. GetManyAndCount: Queries Secuenciales

```csharp
// ✅ CORRECTO: Queries secuenciales
public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(
    Expression<Func<T, bool>> expression,
    int pageNumber,
    int pageSize,
    SortingCriteria sortingCriteria,
    CancellationToken ct = default)
{
    // ✅ 1. Count primero
    var total = await _session.Query<T>()
        .Where(expression)
        .CountAsync(ct);

    // ✅ 2. Items después
    var items = await _session.Query<T>()
        .Where(expression)
        .OrderBy(sortingCriteria.ToExpression())
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
}

// ❌ INCORRECTO: Queries en paralelo
var countTask = _session.Query<T>().CountAsync();
var itemsTask = _session.Query<T>().ToListAsync();
await Task.WhenAll(countTask, itemsTask);  // ❌ DataReader issues
```

**Por qué**:
- ✅ Evita "There is already an open DataReader"
- ✅ NHibernate no soporta múltiples DataReaders simultáneos
- ✅ Secuencial es seguro

---

### ✅ 4. Proyección a DTOs

```csharp
// ✅ CORRECTO: Proyección en la query
public async Task<List<UserDto>> GetUserDtosAsync()
{
    return await _session.Query<User>()
        .Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            RoleNames = u.Roles.Select(r => r.Name).ToList()
        })
        .ToListAsync();
}

// ❌ INCORRECTO: Proyección en memoria
var users = await _session.Query<User>().ToListAsync();  // ❌ Carga TODO
var dtos = users.Select(u => new UserDto { ... }).ToList();
```

**Por qué**:
- ✅ Proyección en SQL (SELECT solo columnas necesarias)
- ✅ Menos datos transferidos desde BD
- ✅ Mejor performance

---

### ✅ 5. HQL solo para Funciones PostgreSQL

```csharp
// ✅ CORRECTO: HQL para unaccent (función PostgreSQL)
public async Task<TechnicalStandard?> GetByCodeAsync(string code)
{
    var hql = @"
        from TechnicalStandard ts
        where lower(unaccent(ts.Code)) = lower(unaccent(:code))";

    return await _session.CreateQuery(hql)
        .SetParameter("code", code)
        .UniqueResultAsync<TechnicalStandard?>();
}

// ✅ CORRECTO: LINQ para queries estándar
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync();
}
```

**Por qué**:
- ✅ HQL permite usar funciones específicas de PostgreSQL
- ✅ LINQ es preferible cuando es suficiente
- ✅ Siempre usar parámetros (`:code`) para evitar SQL injection

---

## Transaction Management

### ✅ 1. BeginTransaction() para Escrituras

```csharp
// ✅ CORRECTO: Transacción para escritura
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    _uoW.BeginTransaction();

    try
    {
        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
        _uoW.Commit();
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _uoW.Rollback();
        return Result.Fail(ex.Message);
    }
}

// ✅ CORRECTO: Sin transacción para lectura
public async Task<Result<User>> ExecuteAsync(Query query, CancellationToken ct)
{
    try
    {
        var user = await _uoW.Users.GetByIdAsync(query.UserId);
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        return Result.Fail(ex.Message);
    }
}
```

**Por qué**:
- ✅ Transacciones solo para escrituras (INSERT, UPDATE, DELETE)
- ✅ Lecturas sin transacción (menos overhead)
- ✅ Rollback explícito en caso de error

---

### ✅ 2. Rollback en TODOS los Catch

```csharp
// ✅ CORRECTO: Rollback en cada catch
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
}
catch (InvalidDomainException ex)
{
    _uoW.Rollback();  // ✅ Rollback
    return Result.Fail(ex.Message);
}
catch (DuplicatedDomainException ex)
{
    _uoW.Rollback();  // ✅ Rollback
    return Result.Fail(ex.Message);
}
catch (Exception ex)
{
    _uoW.Rollback();  // ✅ Rollback
    return Result.Fail(ex.Message);
}

// ❌ INCORRECTO: Falta rollback genérico
try { ... }
catch (InvalidDomainException ex)
{
    _uoW.Rollback();
}
// ❌ Falta catch (Exception) con Rollback
```

**Por qué**:
- ✅ Garantiza que TODAS las excepciones hagan rollback
- ✅ Evita transacciones huérfanas
- ✅ Consistencia de datos

---

### ✅ 3. Transacciones Cortas

```csharp
// ✅ CORRECTO: Transacción corta
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
}
catch (Exception ex)
{
    _uoW.Rollback();
}

// ❌ INCORRECTO: Transacción larga
_uoW.BeginTransaction();
try
{
    var users = await _uoW.Users.GetAllAsync();
    foreach (var user in users)  // ❌ Loop largo
    {
        await ProcessUserAsync(user);  // ❌ Operación lenta
    }
    _uoW.Commit();
}
catch (Exception ex)
{
    _uoW.Rollback();
}
```

**Por qué**:
- ✅ Transacciones largas bloquean registros
- ✅ Aumenta deadlocks
- ❌ Reduce concurrencia

---

### ✅ 4. Una Transacción por Use Case

```csharp
// ✅ CORRECTO: Una transacción
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    var role = await _uoW.Roles.GetByNameAsync("User");
    user.Roles.Add(role);
    _uoW.Commit();  // ✅ Ambos cambios en una transacción
}
catch (Exception ex)
{
    _uoW.Rollback();
}

// ❌ INCORRECTO: Transacciones anidadas
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);

    _uoW.BeginTransaction();  // ❌ Anidada
    var role = await _uoW.Roles.CreateAsync(roleName);
    _uoW.Commit();

    _uoW.Commit();
}
```

**Por qué**:
- ✅ Atomicidad garantizada
- ❌ Transacciones anidadas causan confusión
- ✅ Rollback revierte TODA la operación

---

## Session Management

### ✅ 1. Session Per Request

```csharp
// ✅ CORRECTO: Scoped lifetime
services.AddScoped(factory => sessionFactory.OpenSession());
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

**Por qué**:
- ✅ Una sesión por request HTTP
- ✅ Dispose automático al final del request
- ✅ Lazy loading funciona durante todo el request

---

### ✅ 2. Using Statement o DI Scoped

```csharp
// ✅ CORRECTO: Using statement
using (var session = sessionFactory.OpenSession())
{
    var user = session.Get<User>(userId);
}  // ✅ Dispose() automático

// ✅ MEJOR: DI Scoped
public class Handler(IUnitOfWork uoW)
{
    public async Task Execute()
    {
        var user = await _uoW.Users.GetByIdAsync(userId);
    }
}  // ✅ Scope.Dispose() cierra sesión
```

**Por qué**:
- ✅ Previene session leaks
- ✅ Libera conexiones al pool
- ✅ Libera memoria (first-level cache)

---

### ✅ 3. Clear() en Batch Processing

```csharp
// ✅ CORRECTO: Clear cada N inserts
for (int i = 0; i < 10000; i++)
{
    session.Save(new User(...));

    if (i % 100 == 0)
    {
        session.Flush();   // ✅ Escribir a BD
        session.Clear();   // ✅ Liberar memoria
    }
}
```

**Por qué**:
- ✅ Previene OutOfMemoryException
- ✅ Libera first-level cache periódicamente
- ✅ Performance en batch processing

---

### ✅ 4. Eager Loading para Evitar LazyInitializationException

```csharp
// ✅ CORRECTO: Fetch para eager loading
var user = await _session.Query<User>()
    .Where(u => u.Id == userId)
    .Fetch(u => u.Roles)  // ✅ Cargar Roles en el mismo query
    .SingleOrDefaultAsync();

// ❌ INCORRECTO: Lazy loading fuera de sesión
User user;
using (var session = sessionFactory.OpenSession())
{
    user = session.Get<User>(userId);
}  // Sesión cerrada

var roles = user.Roles;  // ❌ LazyInitializationException
```

**Por qué**:
- ✅ Fetch carga asociaciones en el mismo query (JOIN)
- ✅ Evita LazyInitializationException
- ✅ Un solo viaje a la BD

---

### ✅ 5. NO Compartir ISession Entre Threads

```csharp
// ❌ INCORRECTO: ISession compartida entre threads
public class BadService
{
    private readonly ISession _session;  // ❌ Shared

    public async Task ParallelWork()
    {
        await Task.WhenAll(
            Task.Run(() => _session.Get<User>(id1)),  // ❌ Thread 1
            Task.Run(() => _session.Get<User>(id2))   // ❌ Thread 2
        );
    }
}

// ✅ CORRECTO: Una sesión por thread
await Parallel.ForEachAsync(userIds, async (id, ct) =>
{
    using (var session = sessionFactory.OpenSession())  // ✅ Nueva sesión
    {
        var user = await session.GetAsync<User>(id, ct);
    }
});
```

**Por qué**:
- ❌ ISession **NO es thread-safe**
- ✅ Una sesión por thread/request
- ✅ ISessionFactory **SÍ es thread-safe**

---

## Performance Optimization

### ✅ 1. Connection Pooling

```csharp
// ✅ CORRECTO: Connection string con pooling
"Host=localhost;Port=5432;Database=mydb;Username=user;Password=pass;Maximum Pool Size=100;Minimum Pool Size=10"
```

**Por qué**:
- ✅ Reutiliza conexiones
- ✅ Evita overhead de crear/cerrar conexiones
- ✅ Mejor throughput

---

### ✅ 2. Batch Processing

```csharp
// ✅ CORRECTO: Batch inserts
cfg.SetProperty(Environment.BatchSize, "50");  // Batch de 50 INSERTs

for (int i = 0; i < 10000; i++)
{
    session.Save(new User(...));

    if (i % 50 == 0)
    {
        session.Flush();
        session.Clear();
    }
}
```

**Por qué**:
- ✅ Reduce round-trips a la BD
- ✅ Mejor performance en inserts masivos
- ✅ Menos overhead de red

---

### ✅ 3. Select Only What You Need

```csharp
// ✅ CORRECTO: Proyección
var userDtos = await _session.Query<User>()
    .Select(u => new UserDto { Id = u.Id, Email = u.Email })
    .ToListAsync();

// ❌ INCORRECTO: Cargar entidad completa
var users = await _session.Query<User>().ToListAsync();
var dtos = users.Select(u => new UserDto { Id = u.Id, Email = u.Email }).ToList();
```

**Por qué**:
- ✅ SELECT solo columnas necesarias
- ✅ Menos datos transferidos
- ✅ Mejor performance

---

### ✅ 4. Índices en Base de Datos

```sql
-- ✅ CORRECTO: Índices para queries frecuentes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_created_at ON users(created_at);

-- ✅ Unique index para constraints
CREATE UNIQUE INDEX idx_technical_standards_code ON technical_standards(code);
```

**Por qué**:
- ✅ Queries más rápidas
- ✅ Evita full table scans
- ✅ Mejor escalabilidad

---

### ✅ 5. Second-Level Cache (Opcional)

```csharp
// ✅ Solo para datos estáticos/read-heavy
public class RoleMapper : ClassMapping<Role>
{
    public RoleMapper()
    {
        Table("roles");
        Cache(x => x.Usage(CacheUsage.ReadOnly));  // ✅ Cache L2
    }
}
```

**Por qué**:
- ✅ Reduce queries para datos estáticos
- ⚠️ Solo para datos que NO cambian frecuentemente
- ⚠️ Requiere invalidación manual si cambian

---

## Security

### ✅ 1. Parámetros en HQL (NO String Concatenation)

```csharp
// ✅ CORRECTO: Parámetros
var hql = "from User u where u.Email = :email";
var user = await _session.CreateQuery(hql)
    .SetParameter("email", userEmail)  // ✅ Parámetro
    .UniqueResultAsync<User>();

// ❌ INCORRECTO: Concatenación (SQL Injection)
var hql = $"from User u where u.Email = '{userEmail}'";  // ❌ PELIGRO
var user = await _session.CreateQuery(hql).UniqueResultAsync<User>();
```

**Por qué**:
- ✅ Previene SQL Injection
- ✅ Parámetros son escapados automáticamente
- ❌ Concatenación es vulnerable

---

### ✅ 2. Connection Strings desde Variables de Entorno

```csharp
// ✅ CORRECTO: Variables de entorno
var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

// ❌ INCORRECTO: Hardcoded
var connectionString = "Host=localhost;Password=admin123";  // ❌ PELIGRO
```

**Por qué**:
- ✅ Secrets no están en el código
- ✅ No se commitean a Git
- ✅ Fácil rotación de passwords

---

### ✅ 3. Validación en el Dominio

```csharp
// ✅ CORRECTO: FluentValidation
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

**Por qué**:
- ✅ Validación antes de persistir
- ✅ Previene datos inválidos en BD
- ✅ Consistencia de datos

---

### ✅ 4. Least Privilege para DB User

```sql
-- ✅ CORRECTO: Usuario con permisos mínimos
CREATE USER myapp_user WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE mydb TO myapp_user;
GRANT USAGE ON SCHEMA app TO myapp_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA app TO myapp_user;

-- ❌ INCORRECTO: Usuario con demasiados permisos
GRANT ALL PRIVILEGES ON DATABASE mydb TO myapp_user;  -- ❌ PELIGRO
```

**Por qué**:
- ✅ Principio de least privilege
- ✅ Limita daño en caso de compromiso
- ✅ No necesita DROP, CREATE TABLE, etc.

---

## Testing

### ✅ 1. In-Memory Database para Tests

```csharp
public class TestSessionFactory
{
    public static ISessionFactory CreateInMemorySessionFactory()
    {
        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            c.Driver<SQLiteDriver>();
            c.Dialect<SQLiteDialect>();
            c.ConnectionString = "Data Source=:memory:";
        });

        // Mappers
        var mapper = new ModelMapper();
        mapper.AddMappings(typeof(UserMapper).Assembly.ExportedTypes);
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

        var sessionFactory = cfg.BuildSessionFactory();

        // Crear schema
        using (var session = sessionFactory.OpenSession())
        {
            new SchemaExport(cfg).Execute(false, true, false, session.Connection, null);
        }

        return sessionFactory;
    }
}
```

**Por qué**:
- ✅ Tests rápidos (en memoria)
- ✅ No requiere BD real
- ✅ Aislamiento total entre tests

---

### ✅ 2. Repository Tests

```csharp
[Fact]
public async Task CreateAsync_ValidUser_ReturnsUser()
{
    // Arrange
    var sessionFactory = TestSessionFactory.CreateInMemorySessionFactory();
    using var session = sessionFactory.OpenSession();
    var serviceProvider = CreateServiceProvider();
    var repository = new NHUserRepository(session, serviceProvider);

    // Act
    using var transaction = session.BeginTransaction();
    var user = await repository.CreateAsync("test@example.com", "Test User");
    transaction.Commit();

    // Assert
    Assert.NotNull(user);
    Assert.Equal("test@example.com", user.Email);
}
```

**Por qué**:
- ✅ Verifica comportamiento de repositorios
- ✅ Tests aislados
- ✅ Rápido feedback

---

### ✅ 3. Integration Tests con TestContainers

```csharp
public class IntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task FullIntegrationTest()
    {
        var connectionString = _postgres.GetConnectionString();
        // ... test con PostgreSQL real
    }
}
```

**Por qué**:
- ✅ Tests con BD real (PostgreSQL)
- ✅ Verifica compatibilidad real
- ✅ Detecta problemas de dialecto

---

## Common Pitfalls

### ❌ 1. LazyInitializationException

```csharp
// ❌ PROBLEMA
User user;
using (var session = sessionFactory.OpenSession())
{
    user = session.Get<User>(userId);
}  // Sesión cerrada

var roles = user.Roles;  // ❌ LazyInitializationException

// ✅ SOLUCIÓN: Eager loading
using (var session = sessionFactory.OpenSession())
{
    user = session.Query<User>()
        .Fetch(u => u.Roles)  // ✅ Cargar en mismo query
        .SingleOrDefault();
}
```

---

### ❌ 2. Session Leaks

```csharp
// ❌ PROBLEMA
public void BadMethod()
{
    var session = sessionFactory.OpenSession();
    var user = session.Get<User>(userId);
    // ❌ Nunca se llama Dispose()
}

// ✅ SOLUCIÓN: Using statement
public void GoodMethod()
{
    using (var session = sessionFactory.OpenSession())
    {
        var user = session.Get<User>(userId);
    }  // ✅ Dispose() automático
}
```

---

### ❌ 3. N+1 Query Problem

```csharp
// ❌ PROBLEMA: N+1 queries
var users = await _session.Query<User>().ToListAsync();  // 1 query
foreach (var user in users)
{
    var roleNames = user.Roles.Select(r => r.Name).ToList();  // N queries
}

// ✅ SOLUCIÓN: Eager loading
var users = await _session.Query<User>()
    .Fetch(u => u.Roles)  // ✅ JOIN en 1 query
    .ToListAsync();
```

---

### ❌ 4. ToList() Antes de Filtrar

```csharp
// ❌ PROBLEMA: ToList() carga TODA la tabla
var allUsers = await _session.Query<User>().ToListAsync();  // ❌ 100k registros
var filtered = allUsers.Where(u => u.Email.Contains("@example.com")).ToList();

// ✅ SOLUCIÓN: Filtrar en SQL
var filtered = await _session.Query<User>()
    .Where(u => u.Email.Contains("@example.com"))  // ✅ WHERE en SQL
    .ToListAsync();
```

---

### ❌ 5. Shared ISession Entre Threads

```csharp
// ❌ PROBLEMA: ISession NO es thread-safe
public class BadService
{
    private readonly ISession _session;  // ❌ Shared

    public async Task ParallelWork()
    {
        await Task.WhenAll(
            Task.Run(() => _session.Get<User>(id1)),  // ❌ Race condition
            Task.Run(() => _session.Get<User>(id2))
        );
    }
}

// ✅ SOLUCIÓN: Una sesión por thread
await Parallel.ForEachAsync(userIds, async (id, ct) =>
{
    using (var session = sessionFactory.OpenSession())  // ✅ Nueva sesión
    {
        var user = await session.GetAsync<User>(id, ct);
    }
});
```

---

## Production Checklist

### 🔍 Pre-Deployment

- [ ] **Connection String**: Desde variables de entorno, NO hardcoded
- [ ] **SQL Logging**: Deshabilitado en producción (`LogSqlInConsole = false`)
- [ ] **Connection Pool**: Configurado correctamente (Max/Min Pool Size)
- [ ] **Índices de BD**: Creados para queries frecuentes
- [ ] **Migraciones**: Aplicadas y versionadas
- [ ] **Tests**: Unit tests + Integration tests ejecutados
- [ ] **Validación**: FluentValidation configurada para todas las entidades
- [ ] **Secrets**: No commiteados a Git (`.gitignore` configurado)

### 📊 Monitoring

- [ ] **Session Statistics**: Monitorear `SessionOpenCount` vs `SessionCloseCount`
- [ ] **Connection Pool**: Monitorear connections activas
- [ ] **Query Performance**: Logs de queries lentas (> 1 segundo)
- [ ] **Exceptions**: Logging de `LazyInitializationException`, `TransactionException`
- [ ] **Memory Usage**: Monitorear heap size del proceso

### 🚀 Performance

- [ ] **Batch Size**: Configurado para batch processing (`BatchSize = 50`)
- [ ] **Second-Level Cache**: Considerado para datos estáticos
- [ ] **Proyecciones**: Usadas en queries que no necesitan entidad completa
- [ ] **Eager Loading**: Configurado donde sea necesario (evitar N+1)
- [ ] **Paginación**: Implementada en queries que retornan listas

### 🔒 Security

- [ ] **SQL Injection**: TODOS los queries usan parámetros
- [ ] **Least Privilege**: Usuario de BD con permisos mínimos
- [ ] **Password Rotation**: Proceso documentado para rotar passwords
- [ ] **Audit Logging**: Considerado para operaciones sensibles

---

## Referencias

### Guías Relacionadas

- [repositories.md](./repositories.md) - Patrón Repository
- [mappers.md](./mappers.md) - ClassMapping patterns
- [queries.md](./queries.md) - LINQ, HQL, Dynamic LINQ
- [unit-of-work.md](./unit-of-work.md) - Unit of Work Pattern
- [session-management.md](./session-management.md) - ISession lifecycle

### Documentación Oficial

- [NHibernate Documentation](https://nhibernate.info/)
- [NHibernate Reference](https://nhibernate.info/doc/nhibernate-reference/index.html)
- [NHibernate Best Practices (Official)](https://nhibernate.info/doc/nhibernate-reference/best-practices.html)

### Proyecto de Referencia

- [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend) - Implementación de referencia

---

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Mantenido por**: Equipo de Desarrollo APSYS
