# Unit of Work Pattern con NHibernate

**Version**: 1.0.0
**Última actualización**: 2025-11-14

---

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Conceptos Fundamentales](#conceptos-fundamentales)
3. [Arquitectura del Unit of Work](#arquitectura-del-unit-of-work)
4. [Implementación de IUnitOfWork](#implementación-de-iunitofwork)
5. [Implementación de NHUnitOfWork](#implementación-de-nhunitofwork)
6. [Gestión de Transacciones](#gestión-de-transacciones)
7. [Ciclo de Vida y Disposable Pattern](#ciclo-de-vida-y-disposable-pattern)
8. [Uso en la Capa de Aplicación](#uso-en-la-capa-de-aplicación)
9. [Configuración de Dependency Injection](#configuración-de-dependency-injection)
10. [Manejo de Errores y Rollback](#manejo-de-errores-y-rollback)
11. [Niveles de Aislamiento de Transacciones](#niveles-de-aislamiento-de-transacciones)
12. [Mejores Prácticas](#mejores-prácticas)
13. [Antipatrones](#antipatrones)
14. [Ejemplos Completos](#ejemplos-completos)
15. [Referencias](#referencias)

---

## Introducción

El **Unit of Work Pattern** es un patrón de diseño que mantiene una lista de objetos afectados por una transacción de negocio y coordina la escritura de cambios y la resolución de problemas de concurrencia.

### ¿Qué problema resuelve?

- **Gestión de transacciones**: Coordina múltiples operaciones en una sola transacción atómica
- **Consistencia de datos**: Asegura que todos los cambios se confirmen o se reviertan juntos
- **Separación de responsabilidades**: Centraliza la gestión de sesiones y transacciones
- **Abstracción del ORM**: Permite cambiar la implementación sin afectar la capa de aplicación

### Relación con NHibernate

En NHibernate, el `ISession` ya implementa el patrón Unit of Work internamente:

- Rastrea todos los objetos cargados
- Detecta cambios automáticamente
- Coordina las operaciones de escritura al hacer `Commit()`

Nuestra implementación de `IUnitOfWork` **envuelve** el `ISession` de NHibernate y proporciona:

1. **Acceso centralizado a repositorios**
2. **Gestión explícita de transacciones** (BeginTransaction, Commit, Rollback)
3. **Interfaz del dominio** (independiente de NHibernate)

---

## Conceptos Fundamentales

### Unit of Work en DDD

```
┌─────────────────────────────────────────────────┐
│           Application Layer (Use Cases)         │
│                                                 │
│    ┌─────────────────────────────────────┐    │
│    │      IUnitOfWork (Interfaz)         │    │
│    │  - Repositorios                     │    │
│    │  - BeginTransaction()               │    │
│    │  - Commit()                         │    │
│    │  - Rollback()                       │    │
│    └─────────────────────────────────────┘    │
│                      ▲                          │
└──────────────────────┼──────────────────────────┘
                       │ Implementa
┌──────────────────────┼──────────────────────────┐
│                      │   Infrastructure Layer   │
│    ┌─────────────────────────────────────┐    │
│    │    NHUnitOfWork (Implementación)    │    │
│    │  - ISession _session                │    │
│    │  - ITransaction _transaction        │    │
│    │  - Lazy Repository Creation         │    │
│    └─────────────────────────────────────┘    │
│                      │                          │
│                      ▼                          │
│         ┌─────────────────────────┐            │
│         │   NHibernate ISession   │            │
│         └─────────────────────────┘            │
└─────────────────────────────────────────────────┘
```

### Responsabilidades del Unit of Work

| Responsabilidad | Descripción |
|-----------------|-------------|
| **Gestión de Sesión** | Mantiene una referencia al `ISession` de NHibernate |
| **Gestión de Transacciones** | Inicia, confirma y revierte transacciones |
| **Acceso a Repositorios** | Proporciona acceso a todos los repositorios necesarios |
| **Ciclo de Vida** | Implementa `IDisposable` para liberar recursos |
| **Validación de Estado** | Verifica que las operaciones se realicen en el orden correcto |

---

## Arquitectura del Unit of Work

### Diagrama de Componentes

```
┌────────────────────────────────────────────────────────────┐
│                      IUnitOfWork                           │
├────────────────────────────────────────────────────────────┤
│  Repositorios CRUD                                         │
│  + IRoleRepository Roles { get; }                          │
│  + IUserRepository Users { get; }                          │
│  + IPrototypeRepository Prototypes { get; }                │
│  + ITechnicalStandardRepository TechnicalStandards { get; }│
│                                                            │
│  Repositorios Read-Only                                    │
│  + ITechnicalStandardDaoRepository TechnicalStandardDaos   │
│  + IPrototypeDaoRepository PrototypeDaos                   │
│                                                            │
│  Gestión de Transacciones                                  │
│  + void BeginTransaction()                                 │
│  + void Commit()                                           │
│  + void Rollback()                                         │
│  + void ResetTransaction()                                 │
│  + bool IsActiveTransaction()                              │
└────────────────────────────────────────────────────────────┘
                            ▲
                            │ Implementa
                            │
┌────────────────────────────────────────────────────────────┐
│                    NHUnitOfWork                            │
├────────────────────────────────────────────────────────────┤
│  - ISession _session                                       │
│  - IServiceProvider _serviceProvider                       │
│  - ITransaction? _transaction                              │
│  - bool _disposed                                          │
│                                                            │
│  + NHUnitOfWork(ISession, IServiceProvider)                │
│  + void Dispose()                                          │
└────────────────────────────────────────────────────────────┘
```

---

## Implementación de IUnitOfWork

### Interfaz del Dominio

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Defines the unit of work for the application
/// </summary>
public interface IUnitOfWork : IDisposable
{
    #region crud Repositories

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

### Características Clave de la Interfaz

#### 1. **Separación CRUD vs Read-Only**

```csharp
// ✅ Repositorios CRUD: Operaciones de escritura
IRoleRepository Roles { get; }
IUserRepository Users { get; }

// ✅ Repositorios Read-Only: Solo consultas
ITechnicalStandardDaoRepository TechnicalStandardDaos { get; }
IPrototypeDaoRepository PrototypeDaos { get; }
```

**Ventajas**:
- Claridad de responsabilidades
- Seguridad: Los DAOs no permiten modificaciones
- Mapean a vistas de base de datos (read-only)

#### 2. **Gestión Explícita de Transacciones**

```csharp
void BeginTransaction();      // Inicia transacción
void Commit();                 // Confirma cambios
void Rollback();               // Revierte cambios
void ResetTransaction();       // Reinicia transacción
bool IsActiveTransaction();    // Verifica estado
```

**Ventajas**:
- Control total sobre el ciclo de vida de la transacción
- Permite transacciones anidadas (con precaución)
- Facilita el manejo de errores

#### 3. **IDisposable Implementation**

```csharp
public interface IUnitOfWork : IDisposable
```

**Ventajas**:
- Liberación automática de recursos con `using`
- Previene fugas de conexiones a la base de datos
- Garantiza limpieza de sesiones

---

## Implementación de NHUnitOfWork

### Código Completo

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// NHUnitOfWork is a concrete implementation of the IUnitOfWork interface.
/// It is used to manage transactions and the lifecycle of database operations
/// in an NHibernate context.
/// </summary>
public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    #region crud Repositories

    // ✅ Creación lazy de repositorios (property pattern)
    public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
    public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
    public IPrototypeRepository Prototypes => new NHPrototypeRepository(_session, _serviceProvider);
    public ITechnicalStandardRepository TechnicalStandards => new NHTechnicalStandardRepository(_session, _serviceProvider);

    #endregion

    #region read-only Repositories

    // ✅ Repositorios read-only no necesitan IServiceProvider
    public ITechnicalStandardDaoRepository TechnicalStandardDaos => new NHTechnicalStandardDaoRepository(_session);
    public IPrototypeDaoRepository PrototypeDaos => new NHPrototypeDaoRepository(_session);

    #endregion

    /// <summary>
    /// Constructor for NHUnitOfWork
    /// </summary>
    /// <param name="session">NHibernate session</param>
    /// <param name="serviceProvider">Dependency injection service provider</param>
    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Begin transaction
    /// </summary>
    public void BeginTransaction()
    {
        this._transaction = this._session.BeginTransaction();
    }

    /// <summary>
    /// Execute commit
    /// </summary>
    /// <exception cref="TransactionException"></exception>
    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
        else
            throw new TransactionException("The actual transaction is not longer active");
    }

    /// <summary>
    /// Determine if there is an active transaction
    /// </summary>
    /// <returns></returns>
    public bool IsActiveTransaction()
        => _transaction != null && _transaction.IsActive;

    /// <summary>
    /// Reset the current transaction
    /// </summary>
    public void ResetTransaction()
        => _transaction = _session.BeginTransaction();

    /// <summary>
    /// Execute rollback
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void Rollback()
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
        }
        else
            throw new ArgumentNullException($"No active exception found for session {_session.Connection.ConnectionString}");
    }

    /// <summary>
    /// Dispose the current session
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Liberar recursos gestionados
                if (this._transaction != null)
                    this._transaction.Dispose();
                this._session.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose the current session
    /// </summary>
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

### Análisis de la Implementación

#### 1. **Property Pattern para Repositorios**

```csharp
// ❌ MAL: Crear instancias en el constructor
public class NHUnitOfWork : IUnitOfWork
{
    private readonly IRoleRepository _roles;

    public NHUnitOfWork(ISession session)
    {
        _roles = new NHRoleRepository(session);  // ❌ Se crea aunque no se use
    }

    public IRoleRepository Roles => _roles;
}

// ✅ BIEN: Lazy creation con property
public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
```

**Ventajas del Property Pattern**:
- **Lazy creation**: Solo se crean los repositorios que se usan
- **Simplicidad**: No requiere lógica de inicialización compleja
- **Performance**: Evita instanciaciones innecesarias

**Desventaja**:
- Nueva instancia cada vez que se accede a la propiedad
- ⚠️ No mantener referencias a repositorios entre llamadas

#### 2. **Gestión de IServiceProvider**

```csharp
// Repositorios CRUD necesitan IServiceProvider para validaciones
public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);

// Repositorios read-only NO necesitan IServiceProvider
public ITechnicalStandardDaoRepository TechnicalStandardDaos
    => new NHTechnicalStandardDaoRepository(_session);
```

**¿Por qué necesitan IServiceProvider?**

Los repositorios CRUD inyectan validadores desde el contenedor DI:

```csharp
public class NHRepository<T> where T : AbstractDomainObject
{
    protected readonly ISession _session;
    protected readonly AbstractValidator<T> _validator;

    public NHRepository(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        // ✅ Obtiene el validador desde DI
        _validator = serviceProvider.GetRequiredService<AbstractValidator<T>>();
    }
}
```

#### 3. **Validación de Estado de Transacciones**

```csharp
public void Commit()
{
    // ✅ Valida que la transacción esté activa antes de hacer commit
    if (_transaction != null && _transaction.IsActive)
        _transaction.Commit();
    else
        throw new TransactionException("The actual transaction is not longer active");
}

public void Rollback()
{
    // ✅ Valida que la transacción exista antes de hacer rollback
    if (_transaction != null)
    {
        _transaction.Rollback();
    }
    else
        throw new ArgumentNullException($"No active exception found for session {_session.Connection.ConnectionString}");
}
```

**Ventajas**:
- Previene errores de estado inválido
- Mensajes de error claros
- Falla rápido (fail-fast)

---

## Gestión de Transacciones

### Flujo de Transacción Típico

```
┌─────────────────────────────────────────────────────┐
│  1. BeginTransaction()                              │
│     ↓                                               │
│  2. Operaciones de negocio                          │
│     - uoW.Users.CreateAsync(...)                    │
│     - uoW.Roles.AddUserToRole(...)                  │
│     - uoW.Prototypes.Update(...)                    │
│     ↓                                               │
│  3. Commit() ──────────────┐                        │
│                            │                        │
│                       ¿Éxito?                       │
│                            │                        │
│              ┌─────────────┴─────────────┐          │
│              ▼                           ▼          │
│         ✅ Success                   ❌ Exception   │
│    Cambios persistidos          4. Rollback()      │
│    Session cerrada              Cambios revertidos │
│                                 Session cerrada    │
└─────────────────────────────────────────────────────┘
```

### Métodos de Transacción

#### BeginTransaction()

```csharp
public void BeginTransaction()
{
    this._transaction = this._session.BeginTransaction();
}
```

**¿Qué hace internamente?**

```csharp
// NHibernate crea una transacción ADO.NET
IDbConnection connection = _session.Connection;
IDbTransaction adoTransaction = connection.BeginTransaction();
```

**Importante**:
- ✅ Siempre llamar antes de operaciones de escritura
- ❌ No es necesario para operaciones de solo lectura
- ⚠️ No llamar dos veces sin cerrar la primera

#### Commit()

```csharp
public void Commit()
{
    if (_transaction != null && _transaction.IsActive)
        _transaction.Commit();
    else
        throw new TransactionException("The actual transaction is not longer active");
}
```

**¿Qué hace internamente?**

1. **Flush automático**: NHibernate ejecuta todas las operaciones pendientes
   ```csharp
   _session.Flush();  // INSERT, UPDATE, DELETE
   ```

2. **Commit ADO.NET**: Confirma la transacción en la base de datos
   ```csharp
   adoTransaction.Commit();
   ```

3. **Estado final**: La transacción ya no está activa
   ```csharp
   _transaction.IsActive == false
   ```

#### Rollback()

```csharp
public void Rollback()
{
    if (_transaction != null)
    {
        _transaction.Rollback();
    }
    else
        throw new ArgumentNullException($"No active exception found for session {_session.Connection.ConnectionString}");
}
```

**¿Qué hace internamente?**

1. **Revierte transacción ADO.NET**: Descarta todos los cambios
   ```csharp
   adoTransaction.Rollback();
   ```

2. **Limpia ISession**: NHibernate descarta el caché de primer nivel
   ```csharp
   _session.Clear();  // Opcional pero recomendado
   ```

#### ResetTransaction()

```csharp
public void ResetTransaction()
    => _transaction = _session.BeginTransaction();
```

**Uso típico**:

```csharp
// Después de un rollback, para continuar con nueva transacción
_uoW.Rollback();
_uoW.ResetTransaction();  // Nueva transacción
_uoW.Users.CreateAsync(...);
_uoW.Commit();
```

⚠️ **Precaución**: Usar con cuidado, puede llevar a transacciones huérfanas.

#### IsActiveTransaction()

```csharp
public bool IsActiveTransaction()
    => _transaction != null && _transaction.IsActive;
```

**Uso típico**:

```csharp
if (_uoW.IsActiveTransaction())
{
    _uoW.Commit();
}
else
{
    // Transacción ya cerrada o no iniciada
}
```

---

## Ciclo de Vida y Disposable Pattern

### Implementación del Patrón Dispose

```csharp
private bool _disposed = false;

protected virtual void Dispose(bool disposing)
{
    if (!_disposed)
    {
        if (disposing)
        {
            // ✅ Liberar recursos gestionados
            if (this._transaction != null)
                this._transaction.Dispose();
            this._session.Dispose();
        }
        // Aquí irían recursos no gestionados (no aplicable en este caso)
        _disposed = true;
    }
}

public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);  // Evita llamar al finalizer
}

~NHUnitOfWork()
{
    Dispose(false);
}
```

### ¿Por qué Dispose Pattern?

#### 1. **Prevenir Fugas de Recursos**

```csharp
// ❌ MAL: Sin dispose, conexión queda abierta
var uoW = new NHUnitOfWork(session, serviceProvider);
uoW.BeginTransaction();
uoW.Users.CreateAsync(...);
uoW.Commit();
// ❌ Session y Connection quedan abiertos hasta el GC

// ✅ BIEN: Using libera recursos inmediatamente
using (var uoW = new NHUnitOfWork(session, serviceProvider))
{
    uoW.BeginTransaction();
    uoW.Users.CreateAsync(...);
    uoW.Commit();
}  // ✅ Dispose() se llama automáticamente
```

#### 2. **Orden de Liberación**

```csharp
protected virtual void Dispose(bool disposing)
{
    if (!_disposed)
    {
        if (disposing)
        {
            // ⚠️ ORDEN IMPORTANTE:
            // 1. Primero la transacción
            if (this._transaction != null)
                this._transaction.Dispose();

            // 2. Luego la sesión
            this._session.Dispose();
        }
        _disposed = true;
    }
}
```

**Razón del orden**:
- La transacción depende de la sesión
- Cerrar la sesión primero causaría `ObjectDisposedException` en la transacción

#### 3. **Prevenir Doble Dispose**

```csharp
private bool _disposed = false;

protected virtual void Dispose(bool disposing)
{
    if (!_disposed)  // ✅ Solo ejecuta una vez
    {
        // ... liberar recursos
        _disposed = true;
    }
}
```

### Finalizer (~NHUnitOfWork)

```csharp
~NHUnitOfWork()
{
    Dispose(false);
}
```

**¿Cuándo se ejecuta?**

- Solo si NO se llamó `Dispose()` manualmente
- Durante la recolección de basura (GC)
- **No garantiza tiempo de ejecución**

**Mejores prácticas**:
- ✅ Siempre usar `using` para llamar `Dispose()` explícitamente
- ❌ No depender del finalizer para liberar recursos críticos

---

## Uso en la Capa de Aplicación

### Operaciones CRUD con Transacciones

#### Ejemplo 1: Create User (Con Transacción)

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.users;

public class CreateUserUseCase
{
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
            // ✅ 1. Iniciar transacción
            _uoW.BeginTransaction();

            try
            {
                // ✅ 2. Operaciones de negocio
                var password = GenerateRandomPassword();
                var auth0User = _identityService.Create(command.Email, command.Name, password);

                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

                // ✅ 3. Confirmar transacción
                _uoW.Commit();

                return Result.Ok(user);
            }
            catch (HttpRequestException httpEx)
            {
                // ❌ Rollback en caso de error externo
                _uoW.Rollback();
                return Result.Fail(new Error($"Error creating user on authentication service").CausedBy(httpEx));
            }
            catch (InvalidDomainException idex)
            {
                // ❌ Rollback en caso de validación fallida
                _uoW.Rollback();

                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";

                return Result.Fail(new Error(firstErrorMessage).CausedBy(idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                // ❌ Rollback en caso de duplicado
                _uoW.Rollback();
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                // ❌ Rollback en caso de error inesperado
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

**Flujo de ejecución**:

```
BeginTransaction()
     ↓
CreateAsync() → Validación → Insert en memoria
     ↓
Commit() → Flush() → INSERT SQL → Commit ADO.NET
     ↓
✅ Usuario creado en BD + Auth0
```

**En caso de error**:

```
BeginTransaction()
     ↓
CreateAsync() → Validación → ❌ Exception
     ↓
Rollback() → Revertir cambios → Limpiar caché
     ↓
❌ Sin cambios en BD + Auth0
```

#### Ejemplo 2: Get User (Sin Transacción)

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.users;

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
                // ✅ No necesita BeginTransaction() para lectura
                var user = await _uoW.Users.GetByEmailAsync(request.UserName);

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

**¿Por qué no usa transacciones?**

- Solo realiza operaciones de **lectura**
- No modifica datos
- NHibernate usa **snapshot isolation** para consistencia
- Reduce overhead de transacciones innecesarias

### Patrón de Uso Recomendado

#### Operaciones de Escritura (CREATE, UPDATE, DELETE)

```csharp
public async Task<Result<T>> ExecuteAsync(Command command, CancellationToken ct)
{
    _uoW.BeginTransaction();  // ✅ Siempre iniciar transacción

    try
    {
        // Operaciones de negocio
        var entity = await _uoW.Repository.CreateAsync(...);

        _uoW.Commit();  // ✅ Confirmar cambios
        return Result.Ok(entity);
    }
    catch (Exception ex)
    {
        _uoW.Rollback();  // ❌ Revertir en caso de error
        return Result.Fail(ex.Message);
    }
}
```

#### Operaciones de Lectura (GET, LIST, SEARCH)

```csharp
public async Task<Result<T>> ExecuteAsync(Query query, CancellationToken ct)
{
    // ✅ No necesita transacción
    try
    {
        var entity = await _uoW.Repository.GetByIdAsync(query.Id);
        return Result.Ok(entity);
    }
    catch (Exception ex)
    {
        return Result.Fail(ex.Message);
    }
}
```

---

## Configuración de Dependency Injection

### ServiceCollectionExtender.cs

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.infrastructure.nhibernate;

namespace hashira.stone.backend.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure the unit of work dependency injection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Construir connection string desde variables de entorno
        string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();

        // 2. Crear NHSessionFactory
        var factory = new NHSessionFactory(connectionString);
        var sessionFactory = factory.BuildNHibernateSessionFactory();

        // 3. ✅ Registrar SessionFactory como Singleton
        //    Una sola instancia para toda la aplicación
        //    Thread-safe, optimizado para performance
        services.AddSingleton(sessionFactory);

        // 4. ✅ Registrar ISession como Scoped
        //    Una instancia por request HTTP
        //    Abierta al inicio del request, cerrada al final
        services.AddScoped(factory => sessionFactory.OpenSession());

        // 5. ✅ Registrar IUnitOfWork como Scoped
        //    Una instancia por request HTTP
        //    Comparte el mismo ISession del scope
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        return services;
    }
}
```

### Ciclos de Vida de DI

| Componente | Lifetime | Razón |
|------------|----------|-------|
| **ISessionFactory** | Singleton | Costoso de crear, thread-safe, inmutable |
| **ISession** | Scoped | Una sesión por request HTTP |
| **IUnitOfWork** | Scoped | Envuelve ISession, debe tener el mismo lifetime |
| **Repositories** | Transient | Creados on-demand por UnitOfWork |

### Flujo de Inyección

```
HTTP Request
    ↓
ASP.NET Core crea nuevo Scope
    ↓
┌────────────────────────────────────────┐
│  DI Container resuelve dependencias    │
│                                        │
│  1. SessionFactory (Singleton)         │
│      ↓                                 │
│  2. OpenSession() → ISession (Scoped)  │
│      ↓                                 │
│  3. new NHUnitOfWork(session, sp)      │
│      ↓                                 │
│  4. Inyectar en Handler                │
└────────────────────────────────────────┘
    ↓
Handler ejecuta lógica
    ↓
Scope.Dispose() → UnitOfWork.Dispose() → Session.Dispose()
    ↓
HTTP Response
```

### Program.cs

```csharp
using FastEndpoints;
using hashira.stone.backend.webapi.infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure dependency injection container
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)  // ✅ Configurar Unit of Work
    .ConfigureAutoMapper()
    .ConfigureValidators()
    .ConfigureDependencyInjections(environment)
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()
    .SwaggerDocument();

var app = builder.Build();

// Middleware pipeline
app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints()
    .UseSwagger()
    .UseSwaggerUI();

await app.RunAsync();
```

---

## Manejo de Errores y Rollback

### Estrategias de Manejo de Errores

#### 1. **Try-Catch con Rollback Explícito**

```csharp
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
    return Result.Ok(user);
}
catch (InvalidDomainException ex)
{
    _uoW.Rollback();  // ✅ Rollback explícito
    return Result.Fail(ex.Message);
}
catch (Exception ex)
{
    _uoW.Rollback();  // ✅ Rollback explícito
    return Result.Fail(ex.Message);
}
```

#### 2. **Múltiples Excepciones con Manejo Específico**

```csharp
_uoW.BeginTransaction();
try
{
    var password = GenerateRandomPassword();
    var auth0User = _identityService.Create(email, name, password);
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
    return Result.Ok(user);
}
catch (HttpRequestException httpEx)
{
    // ❌ Error en servicio externo (Auth0)
    _uoW.Rollback();
    _logger.LogError(httpEx, "Failed to create user in Auth0");
    return Result.Fail(new Error("Error creating user in authentication service").CausedBy(httpEx));
}
catch (InvalidDomainException idex)
{
    // ❌ Error de validación de dominio
    _uoW.Rollback();
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    return Result.Fail(new Error(errors?.First().ErrorMessage ?? "Invalid data").CausedBy(idex));
}
catch (DuplicatedDomainException ddex)
{
    // ❌ Error de duplicado (violación de unique constraint)
    _uoW.Rollback();
    return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
}
catch (Exception ex)
{
    // ❌ Error inesperado
    _uoW.Rollback();
    _logger.LogError(ex, "Unexpected error creating user");
    return Result.Fail(new Error("Unexpected error occurred").CausedBy(ex));
}
```

### Excepciones Comunes

| Excepción | Cuándo Ocurre | Acción |
|-----------|---------------|--------|
| **TransactionException** | `Commit()` sin transacción activa | Rollback + Log |
| **InvalidDomainException** | Validación de dominio falla | Rollback + Retornar errores de validación |
| **DuplicatedDomainException** | Violación de unique constraint | Rollback + Mensaje amigable |
| **StaleObjectStateException** | Concurrencia optimista | Rollback + Reintentar o informar |
| **ObjectDisposedException** | Acceso a sesión cerrada | Prevenir con using statement |
| **GenericADOException** | Error de base de datos | Rollback + Log detalles SQL |

---

## Niveles de Aislamiento de Transacciones

### ¿Qué son los Niveles de Aislamiento?

Los niveles de aislamiento definen cómo una transacción interactúa con otras transacciones concurrentes.

### Niveles Soportados por NHibernate

```csharp
using System.Data;

// ✅ Especificar nivel de aislamiento al iniciar transacción
using (var transaction = session.BeginTransaction(IsolationLevel.ReadCommitted))
{
    // Operaciones de negocio
    transaction.Commit();
}
```

### Tabla de Niveles de Aislamiento

| Nivel | Descripción | Fenómenos Permitidos | Uso Típico |
|-------|-------------|----------------------|------------|
| **ReadUncommitted** | Lee datos sin confirmar | Dirty Read, Non-Repeatable Read, Phantom Read | Reportes aproximados |
| **ReadCommitted** | Solo lee datos confirmados | Non-Repeatable Read, Phantom Read | **Default en PostgreSQL** |
| **RepeatableRead** | Lee siempre los mismos datos | Phantom Read | Cálculos críticos |
| **Serializable** | Máximo aislamiento | Ninguno | Transacciones financieras |
| **Snapshot** | MVCC en SQL Server | Ninguno | Lecturas consistentes |

---

## Mejores Prácticas

### ✅ 1. Siempre Usar `using` Statement

```csharp
// ❌ MAL: Sin using, requiere Dispose() manual
var uoW = serviceProvider.GetRequiredService<IUnitOfWork>();
uoW.BeginTransaction();
uoW.Users.CreateAsync(...);
uoW.Commit();
uoW.Dispose();  // ❌ Fácil de olvidar

// ✅ BIEN: Using garantiza Dispose()
using (var uoW = serviceProvider.GetRequiredService<IUnitOfWork>())
{
    uoW.BeginTransaction();
    uoW.Users.CreateAsync(...);
    uoW.Commit();
}  // ✅ Dispose() automático
```

### ✅ 2. BeginTransaction() Antes de Escribir

```csharp
// ❌ MAL: Escritura sin transacción
var user = await _uoW.Users.CreateAsync(email, name);
_uoW.Commit();  // ❌ Exception: No active transaction

// ✅ BIEN: BeginTransaction() primero
_uoW.BeginTransaction();
var user = await _uoW.Users.CreateAsync(email, name);
_uoW.Commit();
```

### ✅ 3. Rollback en TODOS los Catch

```csharp
// ❌ MAL: Rollback solo en algunas excepciones
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
}
catch (InvalidDomainException ex)
{
    _uoW.Rollback();  // ✅ Rollback aquí
    return Result.Fail(ex.Message);
}
// ❌ Falta catch genérico

// ✅ BIEN: Rollback en TODAS las excepciones
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    _uoW.Commit();
}
catch (InvalidDomainException ex)
{
    _uoW.Rollback();
    return Result.Fail(ex.Message);
}
catch (Exception ex)
{
    _uoW.Rollback();  // ✅ Rollback genérico
    return Result.Fail(ex.Message);
}
```

### ✅ 4. No Reusar UnitOfWork Entre Requests

```csharp
// ❌ MAL: Singleton UnitOfWork (compartido entre requests)
services.AddSingleton<IUnitOfWork, NHUnitOfWork>();

// ✅ BIEN: Scoped UnitOfWork (uno por request)
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

### ✅ 5. Operaciones de Lectura Sin Transacción

```csharp
// ❌ MAL: BeginTransaction() para lectura
_uoW.BeginTransaction();
var user = await _uoW.Users.GetByIdAsync(id);
_uoW.Commit();  // ❌ Innecesario

// ✅ BIEN: Sin transacción para lectura
var user = await _uoW.Users.GetByIdAsync(id);
```

---

## Antipatrones

### ❌ 1. Transacciones Anidadas

```csharp
// ❌ MAL: Nested transactions
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);

    _uoW.BeginTransaction();  // ❌ Segunda transacción
    var role = await _uoW.Roles.CreateAsync(roleName);
    _uoW.Commit();

    _uoW.Commit();
}
catch (Exception ex)
{
    _uoW.Rollback();  // ❌ ¿Cuál transacción?
}

// ✅ BIEN: Una sola transacción
_uoW.BeginTransaction();
try
{
    var user = await _uoW.Users.CreateAsync(email, name);
    var role = await _uoW.Roles.CreateAsync(roleName);
    _uoW.Commit();
}
catch (Exception ex)
{
    _uoW.Rollback();
}
```

### ❌ 2. Commit Sin Try-Catch

```csharp
// ❌ MAL: Commit puede lanzar excepciones
_uoW.BeginTransaction();
var user = await _uoW.Users.CreateAsync(email, name);
_uoW.Commit();  // ❌ ¿Qué pasa si falla?

// ✅ BIEN: Try-catch alrededor de Commit
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
```

### ❌ 3. Transacciones de Larga Duración

```csharp
// ❌ MAL: Transacción de larga duración
_uoW.BeginTransaction();
var users = await _uoW.Users.GetAllAsync();

foreach (var user in users)  // ❌ Loop largo con transacción abierta
{
    await ProcessUserAsync(user);  // ❌ Operación lenta
}

_uoW.Commit();

// ✅ BIEN: Transacciones cortas
var users = await _uoW.Users.GetAllAsync();

foreach (var user in users)
{
    using (var uoW = serviceProvider.GetRequiredService<IUnitOfWork>())
    {
        uoW.BeginTransaction();
        await ProcessUserAsync(user, uoW);
        uoW.Commit();
    }
}
```

---

## Ejemplos Completos

Ver [CreateUserUseCase.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.application\usecases\users\CreateUserUseCase.cs) y [GetUserUseCase.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.application\usecases\users\GetUserUseCase.cs) en el proyecto de referencia.

---

## Referencias

### Proyecto de Referencia

- [IUnitOfWork.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\repositories\IUnitOfWork.cs)
- [NHUnitOfWork.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHUnitOfWork.cs)
- [ServiceCollectionExtender.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\infrastructure\ServiceCollectionExtender.cs)

### Documentación Oficial

- [NHibernate Documentation - Sessions and Transactions](https://nhibernate.info/doc/nhibernate-reference/session-configuration.html)
- [Microsoft - Transaction Isolation Levels](https://learn.microsoft.com/en-us/dotnet/api/system.data.isolationlevel)
- [Martin Fowler - Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

### Documentación Interna

- [repositories.md](./repositories.md) - Patrón Repository
- [session-management.md](./session-management.md) - Gestión de sesiones NHibernate
- [transactions.md](../../transactions.md) - Gestión de transacciones en la infraestructura

---

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Mantenido por**: Equipo de Desarrollo APSYS
