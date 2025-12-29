# Session Management en NHibernate

**Version**: 1.0.0
**Última actualización**: 2025-11-14

---

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [¿Qué es ISession?](#qué-es-isession)
3. [Ciclo de Vida de ISession](#ciclo-de-vida-de-isession)
4. [Session Per Request Pattern](#session-per-request-pattern)
5. [First-Level Cache (Session Cache)](#first-level-cache-session-cache)
6. [Flush Modes y Estrategias](#flush-modes-y-estrategias)
7. [Clear() y Eviction](#clear-y-eviction)
8. [Threading y Concurrencia](#threading-y-concurrencia)
9. [LazyInitializationException](#lazyinitializationexception)
10. [Detached Entities](#detached-entities)
11. [Session Leaks y Prevención](#session-leaks-y-prevención)
12. [Mejores Prácticas](#mejores-prácticas)
13. [Antipatrones](#antipatrones)
14. [Ejemplos del Proyecto de Referencia](#ejemplos-del-proyecto-de-referencia)
15. [Referencias](#referencias)

---

## Introducción

La **gestión de sesiones** (session management) es uno de los aspectos más críticos al trabajar con NHibernate. El `ISession` es el punto de entrada principal para todas las operaciones de persistencia y actúa como un **caché de primer nivel** para las entidades.

### ¿Por qué es importante?

- ✅ **Performance**: El caché de sesión reduce consultas a la base de datos
- ✅ **Consistencia**: Garantiza que una entidad sea única dentro de la sesión
- ✅ **Transacciones**: Coordina operaciones dentro de transacciones atómicas
- ✅ **Lazy Loading**: Habilita carga diferida de asociaciones
- ⚠️ **Problemas comunes**: LazyInitializationException, session leaks, memory leaks

---

## ¿Qué es ISession?

### Definición

`ISession` es la interfaz principal de NHibernate que representa una **conversación** entre la aplicación y la base de datos. Es similar al concepto de `DbContext` en Entity Framework.

```csharp
public interface ISession : IDisposable
{
    // Operaciones de persistencia
    void Save(object obj);
    void Update(object obj);
    void Delete(object obj);
    T Get<T>(object id);
    T Load<T>(object id);

    // Consultas
    IQueryable<T> Query<T>();
    IQuery CreateQuery(string queryString);

    // Transacciones
    ITransaction BeginTransaction();
    ITransaction GetCurrentTransaction();

    // Caché y flush
    void Flush();
    void Clear();
    void Evict(object obj);

    // Propiedades
    bool IsOpen { get; }
    bool IsConnected { get; }
}
```

### Responsabilidades del ISession

| Responsabilidad | Descripción |
|-----------------|-------------|
| **Persistencia** | Save, Update, Delete de entidades |
| **Consultas** | LINQ, HQL, SQL nativo, Criteria API |
| **Identity Map** | Mantiene una única instancia de cada entidad por ID |
| **First-Level Cache** | Caché de entidades cargadas en la sesión |
| **Lazy Loading** | Carga diferida de asociaciones y colecciones |
| **Change Tracking** | Detecta cambios automáticamente para UPDATE |
| **Transaction Coordination** | Gestiona transacciones con la base de datos |

---

## Ciclo de Vida de ISession

### Estados del ISession

```
┌─────────────────────────────────────────────────┐
│              ISessionFactory                    │
│         (Singleton, Thread-Safe)                │
└────────────────┬────────────────────────────────┘
                 │ OpenSession()
                 ▼
┌─────────────────────────────────────────────────┐
│         ISession (Open, Connected)              │
│  - First-level cache activo                     │
│  - Connection pool checkout                     │
│  - Change tracking activo                       │
└────────────────┬────────────────────────────────┘
                 │ BeginTransaction()
                 ▼
┌─────────────────────────────────────────────────┐
│    ISession + ITransaction (Active)             │
│  - Operaciones de escritura                     │
│  - Cambios en memoria                           │
└────────────────┬────────────────────────────────┘
                 │ Commit() / Rollback()
                 ▼
┌─────────────────────────────────────────────────┐
│    ISession (Open, sin transacción)             │
│  - Caché sigue activo                           │
│  - Puede seguir consultando                     │
└────────────────┬────────────────────────────────┘
                 │ Dispose()
                 ▼
┌─────────────────────────────────────────────────┐
│       ISession (Closed, Disposed)               │
│  - Caché liberado                               │
│  - Connection devuelta al pool                  │
│  - Ya no se puede usar                          │
└─────────────────────────────────────────────────┘
```

### Apertura de Sesión

```csharp
public class ServiceCollectionExtender
{
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Crear SessionFactory (Singleton)
        string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();
        var factory = new NHSessionFactory(connectionString);
        var sessionFactory = factory.BuildNHibernateSessionFactory();

        services.AddSingleton(sessionFactory);

        // 2. ✅ Abrir sesión por request (Scoped)
        services.AddScoped(factory => sessionFactory.OpenSession());

        return services;
    }
}
```

**¿Qué hace `OpenSession()`?**

1. Obtiene una conexión del connection pool
2. Inicializa el caché de primer nivel (Identity Map)
3. Configura el change tracking
4. Retorna una nueva instancia de `ISession`

### Cierre de Sesión

```csharp
// ✅ CORRECTO: Dispose automático con using
using (var session = sessionFactory.OpenSession())
{
    // Operaciones con la sesión
    var user = session.Get<User>(userId);
}  // ✅ Dispose() se llama automáticamente

// ✅ CORRECTO: DI con Scoped lifetime
public class Handler(IUnitOfWork uoW)
{
    public async Task Execute()
    {
        // ISession se cierra automáticamente al final del request
        var user = await _uoW.Users.GetByIdAsync(userId);
    }
}  // ✅ Scope.Dispose() cierra la sesión
```

**¿Qué hace `Dispose()`?**

1. Cierra la transacción activa (si existe)
2. Libera el caché de primer nivel
3. Devuelve la conexión al pool
4. Marca la sesión como cerrada (`IsOpen = false`)

---

## Session Per Request Pattern

### ¿Qué es Session Per Request?

El patrón **Session Per Request** mantiene una única sesión de NHibernate durante **todo el ciclo de vida de un request HTTP**.

```
HTTP Request → Open Session → Use Case → Repositories → Close Session → HTTP Response
```

### Implementación en ASP.NET Core

#### Configuración con Scoped Lifetime

```csharp
public static IServiceCollection ConfigureUnitOfWork(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // SessionFactory: Singleton (una instancia para toda la app)
    services.AddSingleton(sessionFactory);

    // ✅ ISession: Scoped (una instancia por request HTTP)
    services.AddScoped(factory => sessionFactory.OpenSession());

    // ✅ IUnitOfWork: Scoped (envuelve ISession)
    services.AddScoped<IUnitOfWork, NHUnitOfWork>();

    return services;
}
```

#### Flujo de Request

```
┌────────────────────────────────────────────────┐
│  1. HTTP Request llega a ASP.NET Core          │
└───────────────────┬────────────────────────────┘
                    │
┌───────────────────▼────────────────────────────┐
│  2. DI Container crea Scope                    │
│     - OpenSession() se ejecuta                 │
│     - ISession creada para este request        │
└───────────────────┬────────────────────────────┘
                    │
┌───────────────────▼────────────────────────────┐
│  3. Handler recibe IUnitOfWork                 │
│     - BeginTransaction()                       │
│     - Operaciones de negocio                   │
│     - Commit() / Rollback()                    │
└───────────────────┬────────────────────────────┘
                    │
┌───────────────────▼────────────────────────────┐
│  4. Scope.Dispose() al finalizar request       │
│     - IUnitOfWork.Dispose()                    │
│     - ISession.Dispose()                       │
│     - Conexión devuelta al pool                │
└───────────────────┬────────────────────────────┘
                    │
┌───────────────────▼────────────────────────────┐
│  5. HTTP Response enviada al cliente           │
└────────────────────────────────────────────────┘
```

### Ventajas del Session Per Request

| Ventaja | Descripción |
|---------|-------------|
| ✅ **Simplicidad** | No requiere gestión manual de sesiones |
| ✅ **Consistencia** | Todos los repositorios comparten la misma sesión |
| ✅ **Lazy Loading** | Funciona dentro del request sin LazyInitializationException |
| ✅ **Automatic Cleanup** | Dispose automático al final del request |
| ✅ **Thread-Safe** | Cada request tiene su propia sesión |

### Desventajas y Consideraciones

| Consideración | Mitigación |
|---------------|-----------|
| ⚠️ **Memory Usage** | Limitar cantidad de entidades cargadas por request |
| ⚠️ **Long Requests** | Evitar requests largos con sesión abierta |
| ⚠️ **Session Leaks** | Usar Scoped lifetime correctamente |

---

## First-Level Cache (Session Cache)

### ¿Qué es el First-Level Cache?

El **first-level cache** es un caché de entidades **local a la sesión** (también llamado **Identity Map**). Garantiza que:

1. **Una entidad por ID**: Solo existe una instancia de cada entidad por clave primaria
2. **Reduce queries**: Si una entidad ya está en caché, no se consulta la BD
3. **Change tracking**: NHibernate detecta cambios comparando con la versión en caché

```csharp
using (var session = sessionFactory.OpenSession())
{
    // Primera consulta: va a la base de datos
    var user1 = session.Get<User>(userId);  // SELECT * FROM users WHERE id = ?

    // Segunda consulta: retorna del caché
    var user2 = session.Get<User>(userId);  // ✅ No ejecuta SQL

    // ✅ Ambas referencias apuntan a la MISMA instancia
    Assert.True(Object.ReferenceEquals(user1, user2));
}
```

### Funcionamiento del Identity Map

```
┌─────────────────────────────────────────────────┐
│            ISession (First-Level Cache)         │
├─────────────────────────────────────────────────┤
│  Identity Map:                                  │
│    [User, Guid("123...")] → User instance       │
│    [Role, Guid("456...")] → Role instance       │
│    [Prototype, Guid("789...")] → Prototype      │
└─────────────────────────────────────────────────┘
```

**Ejemplo del Proyecto de Referencia**:

```csharp
public class NHRepository<T, TKey> where T : class, new()
{
    protected internal readonly ISession _session;

    public T? GetById(TKey id)
    {
        // ✅ Get<T>() usa el Identity Map
        // Primera llamada: SELECT
        // Siguientes llamadas: retorna del caché
        return _session.Get<T>(id);
    }
}
```

### Clear() - Limpiar el Caché

```csharp
using (var session = sessionFactory.OpenSession())
{
    var user1 = session.Get<User>(userId);  // SELECT
    user1.Name = "Modified";

    // ✅ Limpiar TODA la sesión
    session.Clear();

    // Nueva consulta: va a la base de datos
    var user2 = session.Get<User>(userId);  // SELECT (nueva instancia)

    // ❌ user2.Name == "Original" (no tiene los cambios de user1)
    // ❌ user1 ahora está DETACHED (desconectado de la sesión)
}
```

**¿Cuándo usar `Clear()`?**

- ✅ Después de procesar lotes grandes de entidades
- ✅ Antes de empezar una nueva unidad de trabajo
- ✅ Para liberar memoria en requests largos
- ❌ NUNCA dentro de una transacción con cambios pendientes

### Evict() - Remover una Entidad

```csharp
using (var session = sessionFactory.OpenSession())
{
    var user1 = session.Get<User>(userId);
    var user2 = session.Get<User>(userId);

    // ✅ Remover solo esta entidad del caché
    session.Evict(user1);

    // Nueva consulta: va a la base de datos
    var user3 = session.Get<User>(userId);  // SELECT

    // ❌ user1 y user2 ahora están DETACHED
    // ✅ user3 es la nueva instancia en caché
}
```

---

## Flush Modes y Estrategias

### ¿Qué es Flush?

**Flush** es el proceso de **sincronizar** el estado de las entidades en memoria con la base de datos, ejecutando los statements SQL pendientes (INSERT, UPDATE, DELETE).

```
Memoria (ISession)                  Base de Datos
─────────────────                  ──────────────
user.Name = "New"    ──Flush()──>  UPDATE users
role.Delete()        ──Flush()──>  DELETE roles
```

### FlushModes en NHibernate

| FlushMode | Descripción | Uso Típico |
|-----------|-------------|------------|
| **Auto** | Flush antes de queries y al hacer Commit | **Default** |
| **Commit** | Flush solo al hacer Commit | Optimización |
| **Always** | Flush antes de CADA query | Debugging |
| **Manual** | Flush solo cuando se llama explícitamente | Control total |

### Configuración de FlushMode

```csharp
using (var session = sessionFactory.OpenSession())
{
    // Default: FlushMode.Auto
    Console.WriteLine(session.FlushMode);  // Auto

    // Cambiar a Commit
    session.FlushMode = FlushMode.Commit;

    var user = session.Get<User>(userId);
    user.Name = "Modified";

    // ❌ NO hace flush automático antes del query
    var users = session.Query<User>().Where(u => u.Name == "Modified").ToList();
    // ⚠️ users.Count == 0 (los cambios no se han flusheado)

    // ✅ Flush manual
    session.Flush();

    // ✅ Ahora sí encuentra el usuario
    users = session.Query<User>().Where(u => u.Name == "Modified").ToList();
    // ✅ users.Count == 1
}
```

### FlushWhenNotActiveTransaction Pattern

**Del proyecto de referencia** ([NHRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHRepository.cs:115-121)):

```csharp
public class NHRepository<T, TKey> where T : AbstractDomainObject
{
    protected internal readonly ISession _session;

    public T Add(T item)
    {
        // Validación
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(
                JsonSerializer.Serialize(
                    validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                )
            );

        // Guardar en sesión
        this._session.Save(item);

        // ✅ CLAVE: Flush si NO hay transacción activa
        this.FlushWhenNotActiveTransaction();

        return item;
    }

    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

**¿Por qué este patrón?**

```csharp
// ✅ CON TRANSACCIÓN: No flush inmediato
_uoW.BeginTransaction();
var user = await _uoW.Users.CreateAsync(email, name);
// ⚠️ Sin FlushWhenNotActiveTransaction: Los cambios están en memoria
_uoW.Commit();  // ✅ Flush + Commit aquí

// ✅ SIN TRANSACCIÓN: Flush inmediato
var user = await _uoW.Users.CreateAsync(email, name);
// ✅ FlushWhenNotActiveTransaction ejecuta Flush automático
// ✅ INSERT ejecutado inmediatamente
```

**Ventajas**:

1. **Con transacción**: Permite agrupar operaciones para Commit/Rollback atómico
2. **Sin transacción**: Flush inmediato para operaciones aisladas
3. **Flexibilidad**: Funciona en ambos escenarios sin cambiar el código

---

## Clear() y Eviction

### Session.Clear() - Limpiar TODO

```csharp
public async Task ProcessLargeBatch()
{
    using (var session = sessionFactory.OpenSession())
    using (var transaction = session.BeginTransaction())
    {
        for (int i = 0; i < 10000; i++)
        {
            var user = new User($"user{i}@example.com", $"User {i}");
            session.Save(user);

            // ✅ Limpiar caché cada 100 inserts
            if (i % 100 == 0)
            {
                session.Flush();    // Escribir a BD
                session.Clear();    // Limpiar caché
            }
        }

        transaction.Commit();
    }
}
```

**¿Por qué es importante?**

- ✅ Previene **OutOfMemoryException** con grandes lotes
- ✅ Libera memoria periódicamente
- ❌ Hace que todas las entidades cargadas queden **detached**

### Session.Evict() - Remover UNA Entidad

```csharp
public async Task UpdateLargeBatch()
{
    using (var session = sessionFactory.OpenSession())
    using (var transaction = session.BeginTransaction())
    {
        var users = session.Query<User>().Take(10000).ToList();

        foreach (var user in users)
        {
            user.Name = $"Updated {user.Name}";
            session.Update(user);

            // ✅ Remover solo esta entidad del caché
            session.Evict(user);
        }

        transaction.Commit();
    }
}
```

**Diferencias Clear() vs Evict()**:

| Método | Alcance | Uso Típico |
|--------|---------|------------|
| `Clear()` | **Toda la sesión** | Batch processing, liberar memoria masivamente |
| `Evict(entity)` | **Una entidad** | Remover entidades específicas del caché |

---

## Threading y Concurrencia

### ISession NO es Thread-Safe

```csharp
// ❌ MAL: Compartir ISession entre threads
public class BadService
{
    private readonly ISession _session;  // ❌ Shared across threads

    public BadService(ISession session)
    {
        _session = session;
    }

    public async Task ParallelWork()
    {
        var tasks = new[]
        {
            Task.Run(() => _session.Get<User>(userId1)),  // ❌ Thread 1
            Task.Run(() => _session.Get<User>(userId2)),  // ❌ Thread 2
        };

        await Task.WhenAll(tasks);  // ❌ Race conditions, corruption
    }
}

// ✅ BIEN: Una sesión por request (Scoped)
public class GoodService
{
    private readonly IUnitOfWork _uoW;  // ✅ Scoped, una por request

    public GoodService(IUnitOfWork uoW)
    {
        _uoW = uoW;
    }

    public async Task SequentialWork()
    {
        // ✅ Operaciones secuenciales en el mismo thread
        var user1 = await _uoW.Users.GetByIdAsync(userId1);
        var user2 = await _uoW.Users.GetByIdAsync(userId2);
    }
}
```

### ISessionFactory SÍ es Thread-Safe

```csharp
// ✅ CORRECTO: SessionFactory Singleton
services.AddSingleton(sessionFactory);  // ✅ Thread-safe

// ✅ CORRECTO: Una sesión por thread/request
services.AddScoped(factory => sessionFactory.OpenSession());
```

### Parallel Processing con Múltiples Sesiones

```csharp
public async Task ProcessUsersInParallel(List<Guid> userIds)
{
    var sessionFactory = serviceProvider.GetRequiredService<ISessionFactory>();

    // ✅ CORRECTO: Cada thread tiene su propia sesión
    await Parallel.ForEachAsync(userIds, async (userId, ct) =>
    {
        using (var session = sessionFactory.OpenSession())  // ✅ Nueva sesión por thread
        using (var transaction = session.BeginTransaction())
        {
            var user = await session.GetAsync<User>(userId, ct);
            user.Name = $"Processed {user.Name}";
            await session.UpdateAsync(user, ct);
            await transaction.CommitAsync(ct);
        }
    });
}
```

---

## LazyInitializationException

### ¿Qué es LazyInitializationException?

Ocurre cuando intentas acceder a una **asociación lazy** (lazy-loaded association) **fuera de la sesión** que cargó la entidad.

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    using (var session = sessionFactory.OpenSession())
    {
        var user = await session.GetAsync<User>(userId);
        return user;  // ✅ User cargado
    }  // ❌ Sesión cerrada aquí
}

// En otro lugar...
var user = await GetUserAsync(userId);
var roles = user.Roles;  // ❌ LazyInitializationException!
// Error: no-Session or session was closed
```

### ¿Por qué ocurre?

```
┌────────────────────────────────────────────────┐
│  1. Session abierta                            │
│     var user = session.Get<User>(userId);      │
│     // user.Roles NO está cargado (lazy)       │
└───────────────┬────────────────────────────────┘
                │
┌───────────────▼────────────────────────────────┐
│  2. Session cerrada (Dispose)                  │
│     // user ahora está DETACHED                │
└───────────────┬────────────────────────────────┘
                │
┌───────────────▼────────────────────────────────┐
│  3. Acceso a asociación lazy                   │
│     var roles = user.Roles;                    │
│     // ❌ NHibernate intenta cargar Roles      │
│     // ❌ pero la sesión está cerrada          │
│     // ❌ LazyInitializationException          │
└────────────────────────────────────────────────┘
```

### Soluciones

#### 1. Eager Loading (Fetch)

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    using (var session = sessionFactory.OpenSession())
    {
        // ✅ Cargar Roles en el mismo query (JOIN)
        var user = await session.Query<User>()
            .Where(u => u.Id == userId)
            .Fetch(u => u.Roles)  // ✅ Eager load
            .SingleOrDefaultAsync();

        return user;  // ✅ Roles ya cargados
    }
}

// Ahora funciona
var user = await GetUserAsync(userId);
var roles = user.Roles;  // ✅ Sin LazyInitializationException
```

#### 2. Session Per Request

```csharp
// ✅ MEJOR SOLUCIÓN: Session activa durante todo el request
public class Handler(IUnitOfWork uoW)
{
    public async Task<UserDto> Execute(Query query)
    {
        // ✅ Sesión activa durante todo el método
        var user = await _uoW.Users.GetByIdAsync(query.UserId);

        // ✅ Acceso a asociación lazy funciona
        var roleNames = user.Roles.Select(r => r.Name).ToList();

        return new UserDto(user, roleNames);
    }
}  // ✅ Sesión se cierra al final del request
```

#### 3. DTO Projection

```csharp
// ✅ MEJOR: Proyectar a DTO antes de cerrar la sesión
public async Task<UserDto> GetUserDtoAsync(Guid userId)
{
    using (var session = sessionFactory.OpenSession())
    {
        var dto = await session.Query<User>()
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                RoleNames = u.Roles.Select(r => r.Name).ToList()  // ✅ Se ejecuta en DB
            })
            .SingleOrDefaultAsync();

        return dto;  // ✅ DTO sin lazy proxies
    }
}

// Sin LazyInitializationException
var dto = await GetUserDtoAsync(userId);
Console.WriteLine(dto.RoleNames.Count);  // ✅ Funciona
```

---

## Detached Entities

### Estados de una Entidad

```
┌────────────────────────────────────────────────┐
│                 TRANSIENT                      │
│  - new User() creado en memoria                │
│  - NO tiene ID asignado                        │
│  - NO está en el Identity Map                  │
└───────────────┬────────────────────────────────┘
                │ session.Save()
                ▼
┌────────────────────────────────────────────────┐
│                 PERSISTENT                     │
│  - Tiene ID asignado                           │
│  - Está en el Identity Map                     │
│  - Cambios automáticamente detectados          │
│  - Sesión ABIERTA                              │
└───────────────┬────────────────────────────────┘
                │ session.Dispose() / session.Evict()
                ▼
┌────────────────────────────────────────────────┐
│                 DETACHED                       │
│  - Tiene ID asignado                           │
│  - NO está en el Identity Map                  │
│  - Cambios NO detectados automáticamente       │
│  - Sesión CERRADA                              │
└───────────────┬────────────────────────────────┘
                │ session.Update() / session.Merge()
                ▼
┌────────────────────────────────────────────────┐
│                 PERSISTENT                     │
│  - Re-attached a la sesión                     │
│  - Cambios detectados nuevamente               │
└────────────────────────────────────────────────┘
```

### Reattaching Detached Entities

#### Update()

```csharp
// 1. Cargar en sesión 1
User user;
using (var session1 = sessionFactory.OpenSession())
{
    user = session1.Get<User>(userId);
}  // user ahora está DETACHED

// 2. Modificar fuera de la sesión
user.Name = "Modified";

// 3. Reattach en sesión 2
using (var session2 = sessionFactory.OpenSession())
using (var transaction = session2.BeginTransaction())
{
    session2.Update(user);  // ✅ Reattach como PERSISTENT
    transaction.Commit();   // ✅ UPDATE ejecutado
}
```

#### Merge()

```csharp
// 1. Cargar en sesión 1
User user;
using (var session1 = sessionFactory.OpenSession())
{
    user = session1.Get<User>(userId);
}

// 2. Modificar fuera de la sesión
user.Name = "Modified";

// 3. Merge en sesión 2
using (var session2 = sessionFactory.OpenSession())
using (var transaction = session2.BeginTransaction())
{
    var mergedUser = session2.Merge(user);  // ✅ Retorna nueva instancia PERSISTENT
    // ⚠️ user sigue DETACHED
    // ✅ mergedUser es la instancia en la sesión
    transaction.Commit();
}
```

**Diferencias Update() vs Merge()**:

| Método | Comportamiento | Uso Típico |
|--------|----------------|------------|
| `Update()` | Reattach la **misma instancia** | Entidad de sesión anterior |
| `Merge()` | Retorna **nueva instancia**, original sigue detached | Web forms, DTOs deserializados |

---

## Session Leaks y Prevención

### ¿Qué es un Session Leak?

Un **session leak** ocurre cuando una `ISession` **no se cierra correctamente**, dejando la conexión abierta en el pool.

```csharp
// ❌ MAL: Session leak
public void BadMethod()
{
    var session = sessionFactory.OpenSession();
    var user = session.Get<User>(userId);
    // ❌ Nunca se llama session.Dispose()
    // ❌ Conexión queda abierta en el pool
}
```

### Consecuencias

- ❌ **Connection pool exhaustion**: Se agotan las conexiones disponibles
- ❌ **Memory leaks**: First-level cache no se libera
- ❌ **Performance degradation**: Consultas más lentas
- ❌ **Application hangs**: Requests esperan conexiones disponibles

### Prevención con Using Statement

```csharp
// ✅ CORRECTO: Using garantiza Dispose
public void GoodMethod()
{
    using (var session = sessionFactory.OpenSession())
    {
        var user = session.Get<User>(userId);
    }  // ✅ Dispose() se llama automáticamente
}
```

### Prevención con Scoped Lifetime

```csharp
// ✅ MEJOR: DI con Scoped lifetime
services.AddScoped(factory => sessionFactory.OpenSession());

public class Handler(IUnitOfWork uoW)
{
    public async Task Execute()
    {
        // ISession se cierra automáticamente al final del request
        var user = await _uoW.Users.GetByIdAsync(userId);
    }
}  // ✅ Scope.Dispose() cierra la sesión
```

### Detección de Session Leaks

**Logging en SessionFactory**:

```csharp
public class NHSessionFactory
{
    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var cfg = new Configuration();

        // ✅ Log SQL y session lifecycle
        cfg.SetProperty(Environment.ShowSql, "true");
        cfg.SetProperty(Environment.FormatSql, "true");

        return cfg.BuildSessionFactory();
    }
}
```

**Monitoreo de Connection Pool**:

```csharp
// Consultar estadísticas del pool
var stats = sessionFactory.Statistics;
Console.WriteLine($"Open sessions: {stats.SessionOpenCount}");
Console.WriteLine($"Closed sessions: {stats.SessionCloseCount}");
Console.WriteLine($"Active sessions: {stats.SessionOpenCount - stats.SessionCloseCount}");
```

---

## Mejores Prácticas

### ✅ 1. Session Per Request

```csharp
// ✅ CORRECTO: Una sesión por request HTTP
services.AddScoped(factory => sessionFactory.OpenSession());
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

**Por qué**:
- Simplicidad en la gestión de sesiones
- Dispose automático al final del request
- Lazy loading funciona durante todo el request

---

### ✅ 2. Usar Using Statement

```csharp
// ✅ CORRECTO: Using garantiza Dispose
using (var session = sessionFactory.OpenSession())
{
    // Operaciones
}

// ✅ MEJOR: DI con Scoped
public class Handler(IUnitOfWork uoW)
{
    // ISession se cierra automáticamente
}
```

---

### ✅ 3. FlushWhenNotActiveTransaction Pattern

```csharp
protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        this._session.Flush();
}
```

**Por qué**:
- Funciona con y sin transacciones
- Flush inmediato cuando no hay transacción
- Flush al Commit cuando hay transacción

---

### ✅ 4. Clear() en Batch Processing

```csharp
for (int i = 0; i < 10000; i++)
{
    session.Save(new User(...));

    if (i % 100 == 0)
    {
        session.Flush();   // Escribir a BD
        session.Clear();   // Liberar memoria
    }
}
```

---

### ✅ 5. Eager Loading para Evitar LazyInitializationException

```csharp
// ✅ CORRECTO: Fetch para cargar asociaciones
var user = await session.Query<User>()
    .Where(u => u.Id == userId)
    .Fetch(u => u.Roles)  // Eager load
    .SingleOrDefaultAsync();
```

---

### ✅ 6. DTO Projection

```csharp
// ✅ CORRECTO: Proyectar a DTO dentro de la sesión
var dto = await session.Query<User>()
    .Select(u => new UserDto
    {
        Id = u.Id,
        RoleNames = u.Roles.Select(r => r.Name).ToList()
    })
    .SingleOrDefaultAsync();
```

---

### ✅ 7. Monitorear Session Statistics

```csharp
var stats = sessionFactory.Statistics;
Console.WriteLine($"Sessions opened: {stats.SessionOpenCount}");
Console.WriteLine($"Sessions closed: {stats.SessionCloseCount}");
```

---

## Antipatrones

### ❌ 1. Compartir ISession Entre Threads

```csharp
// ❌ MAL: ISession no es thread-safe
public class BadService
{
    private readonly ISession _session;  // ❌ Shared

    public async Task ParallelWork()
    {
        await Task.WhenAll(
            Task.Run(() => _session.Get<User>(id1)),  // ❌ Race condition
            Task.Run(() => _session.Get<User>(id2))   // ❌ Corruption
        );
    }
}

// ✅ BIEN: Una sesión por thread
await Parallel.ForEachAsync(userIds, async (id, ct) =>
{
    using (var session = sessionFactory.OpenSession())  // ✅ Nueva sesión
    {
        var user = await session.GetAsync<User>(id, ct);
    }
});
```

---

### ❌ 2. Singleton ISession

```csharp
// ❌ MAL: ISession Singleton
services.AddSingleton(sessionFactory.OpenSession());  // ❌ NUNCA

// ✅ BIEN: ISession Scoped
services.AddScoped(factory => sessionFactory.OpenSession());
```

---

### ❌ 3. No Cerrar Sesión (Session Leak)

```csharp
// ❌ MAL: Sin Dispose
public void BadMethod()
{
    var session = sessionFactory.OpenSession();
    var user = session.Get<User>(userId);
    // ❌ Session leak
}

// ✅ BIEN: Using statement
public void GoodMethod()
{
    using (var session = sessionFactory.OpenSession())
    {
        var user = session.Get<User>(userId);
    }  // ✅ Dispose() automático
}
```

---

### ❌ 4. Clear() Dentro de Transacción con Cambios Pendientes

```csharp
// ❌ MAL: Clear() pierde cambios pendientes
using (var transaction = session.BeginTransaction())
{
    var user = session.Get<User>(userId);
    user.Name = "Modified";

    session.Clear();  // ❌ Pierde el cambio de user.Name

    transaction.Commit();  // ❌ No se ejecuta UPDATE
}

// ✅ BIEN: Flush antes de Clear
using (var transaction = session.BeginTransaction())
{
    var user = session.Get<User>(userId);
    user.Name = "Modified";

    session.Flush();  // ✅ Ejecuta UPDATE
    session.Clear();  // ✅ Ahora sí puede limpiar

    transaction.Commit();
}
```

---

### ❌ 5. Acceder a Lazy Associations Fuera de la Sesión

```csharp
// ❌ MAL: LazyInitializationException
User user;
using (var session = sessionFactory.OpenSession())
{
    user = session.Get<User>(userId);
}  // Sesión cerrada

var roles = user.Roles;  // ❌ LazyInitializationException

// ✅ BIEN: Eager loading
using (var session = sessionFactory.OpenSession())
{
    user = session.Query<User>()
        .Where(u => u.Id == userId)
        .Fetch(u => u.Roles)  // ✅ Cargar en el mismo query
        .SingleOrDefault();
}

var roles = user.Roles;  // ✅ Funciona
```

---

### ❌ 6. Mantener Sesión Abierta Durante I/O Lento

```csharp
// ❌ MAL: Sesión abierta durante operación lenta
using (var session = sessionFactory.OpenSession())
{
    var user = session.Get<User>(userId);

    // ❌ Sesión abierta durante operación lenta (1 minuto)
    await SendEmailAsync(user.Email);  // 1 minuto
    await UploadToS3Async(file);        // 2 minutos

    user.Name = "Modified";
}  // ❌ Sesión abierta 3+ minutos

// ✅ BIEN: Cerrar sesión antes de I/O lento
using (var session = sessionFactory.OpenSession())
{
    var user = session.Get<User>(userId);
}  // ✅ Sesión cerrada

// Operaciones lentas sin sesión abierta
await SendEmailAsync(user.Email);
await UploadToS3Async(file);

using (var session = sessionFactory.OpenSession())
{
    var user = session.Get<User>(userId);
    user.Name = "Modified";
}  // ✅ Sesión abierta solo para la actualización
```

---

## Ejemplos del Proyecto de Referencia

### Ejemplo 1: FlushWhenNotActiveTransaction

**Archivo**: [NHRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHRepository.cs:115-121)

```csharp
public class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey>
    where T : AbstractDomainObject, new()
{
    protected internal readonly ISession _session;
    protected internal readonly AbstractValidator<T> validator;

    public T Add(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(
                JsonSerializer.Serialize(
                    validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                )
            );

        this._session.Save(item);

        // ✅ Flush si no hay transacción activa
        this.FlushWhenNotActiveTransaction();

        return item;
    }

    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

### Ejemplo 2: Session Per Request Configuration

**Archivo**: [ServiceCollectionExtender.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs:77-86)

```csharp
public static class ServiceCollectionExtender
{
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();
        var factory = new NHSessionFactory(connectionString);
        var sessionFactory = factory.BuildNHibernateSessionFactory();

        // ✅ SessionFactory: Singleton (thread-safe)
        services.AddSingleton(sessionFactory);

        // ✅ ISession: Scoped (una por request HTTP)
        services.AddScoped(factory => sessionFactory.OpenSession());

        // ✅ IUnitOfWork: Scoped (envuelve ISession)
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        return services;
    }
}
```

### Ejemplo 3: NHUnitOfWork con Dispose

**Archivo**: [NHUnitOfWork.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHUnitOfWork.cs:93-120)

```csharp
public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // ✅ Liberar transacción primero
                if (this._transaction != null)
                    this._transaction.Dispose();

                // ✅ Luego cerrar sesión
                this._session.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NHUnitOfWork()
    {
        Dispose(false);
    }
}
```

---

## Referencias

### Proyecto de Referencia

- [NHRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHRepository.cs) - FlushWhenNotActiveTransaction pattern
- [NHUnitOfWork.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHUnitOfWork.cs) - ISession lifecycle management
- [ServiceCollectionExtender.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs) - Session Per Request configuration

### Documentación Oficial

- [NHibernate Session Documentation](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html)
- [NHibernate Caching](https://nhibernate.info/doc/nhibernate-reference/caches.html)
- [NHibernate Transactions](https://nhibernate.info/doc/nhibernate-reference/transactions.html)

### Documentación Interna

- [unit-of-work.md](./unit-of-work.md) - Unit of Work Pattern
- [repositories.md](./repositories.md) - Repository Pattern
- [queries.md](./queries.md) - LINQ and Query Patterns

---

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Mantenido por**: Equipo de Desarrollo APSYS
