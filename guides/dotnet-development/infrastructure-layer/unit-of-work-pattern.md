# Unit of Work Pattern

**Versión:** 1.0.0
**Última actualización:** 2025-01-14

## Tabla de Contenidos
- [¿Qué es el Unit of Work Pattern?](#qué-es-el-unit-of-work-pattern)
- [¿Por qué usar Unit of Work?](#por-qué-usar-unit-of-work)
- [Relación con Repository Pattern](#relación-con-repository-pattern)
- [Arquitectura y Responsabilidades](#arquitectura-y-responsabilidades)
- [Implementación Paso a Paso](#implementación-paso-a-paso)
- [Manejo de Transacciones](#manejo-de-transacciones)
- [Ciclo de Vida y Dispose](#ciclo-de-vida-y-dispose)
- [Patrones de Uso](#patrones-de-uso)
- [Mejores Prácticas](#mejores-prácticas)
- [Antipatrones Comunes](#antipatrones-comunes)
- [Checklist de Implementación](#checklist-de-implementación)
- [Ejemplos Completos](#ejemplos-completos)

---

## ¿Qué es el Unit of Work Pattern?

El **Unit of Work Pattern** es un patrón de diseño que **mantiene una lista de objetos afectados por una transacción de negocio y coordina la escritura de cambios**.

### Concepto Visual

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
│                       (Use Case)                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
                  ┌─────────────┐
                  │ IUnitOfWork │  ◄─── Punto de entrada único
                  └──────┬──────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
  │IUserRepo    │ │IRoleRepo    │ │IProductRepo │
  └─────────────┘ └─────────────┘ └─────────────┘
         │               │               │
         └───────────────┼───────────────┘
                         │
                         ▼
                  ┌─────────────┐
                  │  ISession   │  ◄─── Una sola sesión compartida
                  └──────┬──────┘
                         │
                         ▼
                  ┌─────────────┐
                  │  Database   │
                  └─────────────┘
```

### Analogía del Mundo Real

Piensa en un **carrito de compras**:
- **Productos en el carrito** (Operaciones): Agregas, modificas, eliminas productos
- **Botón "Pagar"** (Commit): Confirma TODAS las operaciones de una vez
- **Botón "Cancelar"** (Rollback): Deshace TODAS las operaciones
- **Ticket de compra** (Transaction): Garantiza que todo se procese o nada se procese

---

## ¿Por qué usar Unit of Work?

### ✅ Beneficios

| Beneficio | Descripción |
|-----------|-------------|
| **Atomicidad** | Múltiples operaciones se ejecutan como una sola unidad (todo o nada) |
| **Consistencia** | Garantiza que la base de datos siempre esté en un estado válido |
| **Coordinación** | Coordina múltiples repositorios bajo una sola transacción |
| **Flush centralizado** | Un solo `Commit()` para todas las operaciones |
| **Session única** | Todos los repositorios comparten la misma `ISession` |

### 📊 Comparación: Con vs Sin Unit of Work

**❌ SIN Unit of Work**
```csharp
// Cada repositorio tiene su propia sesión
public class CreateUserWithRoleUseCase(IUserRepository userRepository, IRoleRepository roleRepository)
{
    public async Task ExecuteAsync(string email, string roleName)
    {
        // ❌ Problema 1: Dos sesiones diferentes
        var user = await userRepository.CreateAsync(email);  // Session 1
        var role = await roleRepository.GetByNameAsync(roleName); // Session 2

        // ❌ Problema 2: No hay transacción entre operaciones
        user.AddRole(role);
        await userRepository.SaveAsync(user); // Session 1

        // ❌ Si falla aquí, el user ya se guardó pero sin el role
    }
}
```

**✅ CON Unit of Work**
```csharp
// Todos los repositorios comparten la misma sesión y transacción
public class CreateUserWithRoleUseCase(IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(string email, string roleName)
    {
        unitOfWork.BeginTransaction(); // ✅ Inicia transacción

        try
        {
            // ✅ Misma sesión y transacción para ambos repositorios
            var user = await unitOfWork.Users.CreateAsync(email);
            var role = await unitOfWork.Roles.GetByNameAsync(roleName);

            user.AddRole(role);
            await unitOfWork.Users.SaveAsync(user);

            unitOfWork.Commit(); // ✅ Commit de TODO o NADA
        }
        catch
        {
            unitOfWork.Rollback(); // ✅ Deshace TODO
            throw;
        }
    }
}
```

---

## Relación con Repository Pattern

### 🔗 Unit of Work + Repository = Combo Perfecto

```
┌──────────────────────────────────────────────────────────────┐
│                      UNIT OF WORK                             │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Transacción                                            │  │
│  │ ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │  │
│  │ │ Repository 1 │  │ Repository 2 │  │ Repository 3 │ │  │
│  │ └──────────────┘  └──────────────┘  └──────────────┘ │  │
│  │                      ISession                          │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

**Responsabilidades:**

| Patrón | Responsabilidad |
|--------|----------------|
| **Repository** | Operaciones CRUD sobre una entidad específica |
| **Unit of Work** | Coordinar múltiples repositorios y gestionar transacciones |

---

## Arquitectura y Responsabilidades

### 📂 Estructura de Archivos

```
src/
├── hashira.stone.backend.domain/
│   └── interfaces/
│       └── repositories/
│           ├── IUnitOfWork.cs              ← Define contrato de UoW
│           ├── IUserRepository.cs
│           └── IRoleRepository.cs
│
└── hashira.stone.backend.infrastructure/
    └── nhibernate/
        ├── NHUnitOfWork.cs                 ← Implementa IUnitOfWork con NHibernate
        ├── NHUserRepository.cs
        └── NHRoleRepository.cs
```

### 🎯 Responsabilidades del Unit of Work

1. **Gestionar la sesión** (`ISession` en NHibernate)
2. **Proveer repositorios** (todos comparten la misma sesión)
3. **Manejar transacciones** (`BeginTransaction`, `Commit`, `Rollback`)
4. **Liberar recursos** (`Dispose`)

---

## Implementación Paso a Paso

### Paso 1: Definir Interfaz IUnitOfWork (Domain Layer)

```csharp
// Domain Layer: hashira.stone.backend.domain/interfaces/repositories/IUnitOfWork.cs
namespace hashira.stone.backend.domain.interfaces.repositories;

/// <summary>
/// Define el Unit of Work para la aplicación
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // ─────────────────────────────────────────────────────────────
    // Repositorios CRUD (Escritura)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Repositorio para gestionar roles</summary>
    IRoleRepository Roles { get; }

    /// <summary>Repositorio para gestionar usuarios</summary>
    IUserRepository Users { get; }

    /// <summary>Repositorio para gestionar prototipos</summary>
    IPrototypeRepository Prototypes { get; }

    /// <summary>Repositorio para gestionar estándares técnicos</summary>
    ITechnicalStandardRepository TechnicalStandards { get; }

    // ─────────────────────────────────────────────────────────────
    // Repositorios Read-Only (Solo Lectura)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Repositorio read-only para DAOs de estándares técnicos</summary>
    ITechnicalStandardDaoRepository TechnicalStandardDaos { get; }

    /// <summary>Repositorio read-only para DAOs de prototipos</summary>
    IPrototypeDaoRepository PrototypeDaos { get; }

    // ─────────────────────────────────────────────────────────────
    // Gestión de Transacciones
    // ─────────────────────────────────────────────────────────────

    /// <summary>Inicia una nueva transacción de base de datos</summary>
    void BeginTransaction();

    /// <summary>Confirma todos los cambios realizados durante la transacción</summary>
    void Commit();

    /// <summary>Deshace todos los cambios realizados durante la transacción</summary>
    void Rollback();

    /// <summary>Resetea la transacción actual</summary>
    void ResetTransaction();

    /// <summary>Determina si hay una transacción activa</summary>
    bool IsActiveTransaction();
}
```

**🔑 Puntos Clave:**
1. **Repositorios como propiedades**: Acceso directo a repositorios
2. **Métodos de transacción**: Control explícito del ciclo de vida
3. **IDisposable**: Libera recursos (sesión, transacción)

### Paso 2: Implementar NHUnitOfWork (Infrastructure Layer)

```csharp
// Infrastructure Layer: hashira.stone.backend.infrastructure/nhibernate/NHUnitOfWork.cs
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementación concreta de IUnitOfWork usando NHibernate
/// Gestiona transacciones y el ciclo de vida de operaciones de base de datos
/// </summary>
public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    // ─────────────────────────────────────────────────────────────
    // Repositorios CRUD
    // ─────────────────────────────────────────────────────────────

    public IRoleRepository Roles
        => new NHRoleRepository(_session, _serviceProvider);

    public IUserRepository Users
        => new NHUserRepository(_session, _serviceProvider);

    public IPrototypeRepository Prototypes
        => new NHPrototypeRepository(_session, _serviceProvider);

    public ITechnicalStandardRepository TechnicalStandards
        => new NHTechnicalStandardRepository(_session, _serviceProvider);

    // ─────────────────────────────────────────────────────────────
    // Repositorios Read-Only
    // ─────────────────────────────────────────────────────────────

    public ITechnicalStandardDaoRepository TechnicalStandardDaos
        => new NHTechnicalStandardDaoRepository(_session);

    public IPrototypeDaoRepository PrototypeDaos
        => new NHPrototypeDaoRepository(_session);

    // ─────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Constructor para NHUnitOfWork
    /// </summary>
    /// <param name="session">Sesión de NHibernate compartida</param>
    /// <param name="serviceProvider">Proveedor de servicios para resolución de dependencias</param>
    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    // ─────────────────────────────────────────────────────────────
    // Gestión de Transacciones
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia una nueva transacción
    /// </summary>
    public void BeginTransaction()
    {
        this._transaction = this._session.BeginTransaction();
    }

    /// <summary>
    /// Confirma la transacción actual
    /// </summary>
    /// <exception cref="TransactionException">Si no hay transacción activa</exception>
    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
        else
            throw new TransactionException("The actual transaction is not longer active");
    }

    /// <summary>
    /// Determina si hay una transacción activa
    /// </summary>
    public bool IsActiveTransaction()
        => _transaction != null && _transaction.IsActive;

    /// <summary>
    /// Resetea la transacción actual
    /// </summary>
    public void ResetTransaction()
        => _transaction = _session.BeginTransaction();

    /// <summary>
    /// Deshace la transacción actual
    /// </summary>
    /// <exception cref="ArgumentNullException">Si no hay transacción activa</exception>
    public void Rollback()
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
        }
        else
            throw new ArgumentNullException($"No active exception found for session {_session.Connection.ConnectionString}");
    }

    // ─────────────────────────────────────────────────────────────
    // IDisposable Implementation
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Libera los recursos de la sesión y transacción
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Liberar recursos administrados
                if (this._transaction != null)
                    this._transaction.Dispose();

                this._session.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Libera los recursos del Unit of Work
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

**🔑 Puntos Clave:**
1. **Session compartida**: Todos los repositorios reciben `_session`
2. **Lazy instantiation**: Repositorios se crean cuando se accede a la propiedad
3. **Transaction management**: `BeginTransaction()`, `Commit()`, `Rollback()`
4. **Dispose pattern**: Libera recursos correctamente

---

## Manejo de Transacciones

### 🔄 Ciclo de Vida de una Transacción

```
┌──────────────┐
│ BeginTransaction()
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────┐
│ Operaciones en Repositorios      │
│  - Users.CreateAsync()            │
│  - Roles.GetByNameAsync()         │
│  - Users.SaveAsync()              │
└──────┬───────────────────────────┘
       │
       ├──► ✅ Success ──► Commit()
       │
       └──► ❌ Exception ──► Rollback()
```

### 📋 Estados de Transacción

| Estado | Descripción | Método |
|--------|-------------|--------|
| **No iniciada** | No hay transacción activa | `IsActiveTransaction() == false` |
| **Activa** | Transacción en curso | `IsActiveTransaction() == true` |
| **Committed** | Cambios confirmados | `Commit()` |
| **Rolled back** | Cambios deshechos | `Rollback()` |

### 💡 Ejemplo: Flujo Completo de Transacción

```csharp
public class CreateUserWithRoleUseCase(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<User> ExecuteAsync(string email, string name, string roleName)
    {
        // ─────────────────────────────────────────────────────────────
        // 1. Iniciar Transacción
        // ─────────────────────────────────────────────────────────────
        _unitOfWork.BeginTransaction();

        try
        {
            // ─────────────────────────────────────────────────────────────
            // 2. Operaciones dentro de la transacción
            // ─────────────────────────────────────────────────────────────

            // Crear usuario
            var user = await _unitOfWork.Users.CreateAsync(email, name);

            // Obtener role
            var role = await _unitOfWork.Roles.GetByNameAsync(roleName);
            if (role == null)
                throw new NotFoundException($"Role '{roleName}' not found");

            // Asignar role al usuario
            user.AddRole(role);
            await _unitOfWork.Users.SaveAsync(user);

            // ─────────────────────────────────────────────────────────────
            // 3. Commit si todo sale bien
            // ─────────────────────────────────────────────────────────────
            _unitOfWork.Commit();

            return user;
        }
        catch (Exception ex)
        {
            // ─────────────────────────────────────────────────────────────
            // 4. Rollback si hay error
            // ─────────────────────────────────────────────────────────────
            _unitOfWork.Rollback();
            throw; // Re-lanzar excepción
        }
    }
}
```

---

## Ciclo de Vida y Dispose

### 🔄 Patrón de Dispose

El Unit of Work implementa `IDisposable` para liberar recursos correctamente.

```csharp
// ✅ CORRECTO: Usando 'using' statement
public async Task<User> ExecuteAsync(string email)
{
    using (var unitOfWork = serviceProvider.GetService<IUnitOfWork>())
    {
        unitOfWork.BeginTransaction();
        try
        {
            var user = await unitOfWork.Users.CreateAsync(email);
            unitOfWork.Commit();
            return user;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    } // ✅ Dispose() se llama automáticamente aquí
}
```

### 📊 Orden de Liberación de Recursos

```
Dispose()
   │
   ├──► 1. Transaction.Dispose()  ← Libera transacción
   │
   └──► 2. Session.Dispose()      ← Libera sesión y conexión a BD
```

### ⚠️ Importante: No Reutilizar Unit of Work

```csharp
// ❌ INCORRECTO: Reutilizar Unit of Work
public class Handler
{
    private readonly IUnitOfWork _unitOfWork; // ❌ NO guardar como field

    public Handler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync()
    {
        _unitOfWork.BeginTransaction();
        // ... operaciones
        _unitOfWork.Commit();

        // ❌ Problema: _unitOfWork ya fue usado
        _unitOfWork.BeginTransaction(); // ❌ Session puede estar cerrada
    }
}

// ✅ CORRECTO: Usar Unit of Work por scope
public class Handler(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task ExecuteAsync()
    {
        // ✅ Una sola transacción por scope
        _unitOfWork.BeginTransaction();
        try
        {
            // ... operaciones
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    } // ✅ DI container maneja el dispose
}
```

---

## Patrones de Uso

### 1️⃣ Operación Simple (Una sola entidad)

```csharp
public class CreateUserUseCase(IUnitOfWork unitOfWork)
{
    public async Task<User> ExecuteAsync(string email, string name)
    {
        unitOfWork.BeginTransaction();

        try
        {
            var user = await unitOfWork.Users.CreateAsync(email, name);
            unitOfWork.Commit();
            return user;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }
}
```

### 2️⃣ Operación Compleja (Múltiples entidades)

```csharp
public class CreateOrderUseCase(IUnitOfWork unitOfWork)
{
    public async Task<Order> ExecuteAsync(Guid userId, List<OrderItem> items)
    {
        unitOfWork.BeginTransaction();

        try
        {
            // 1. Obtener usuario
            var user = await unitOfWork.Users.GetAsync(userId);

            // 2. Crear orden
            var order = new Order(user);
            await unitOfWork.Orders.AddAsync(order);

            // 3. Agregar items
            foreach (var item in items)
            {
                var product = await unitOfWork.Products.GetAsync(item.ProductId);
                order.AddItem(product, item.Quantity);
            }

            // 4. Actualizar inventario
            foreach (var item in order.Items)
            {
                await unitOfWork.Products.DecrementStockAsync(item.ProductId, item.Quantity);
            }

            unitOfWork.Commit(); // ✅ TODO se confirma de una vez

            return order;
        }
        catch
        {
            unitOfWork.Rollback(); // ✅ TODO se deshace
            throw;
        }
    }
}
```

### 3️⃣ Operación con Servicio Externo (Compensación)

```csharp
public class CreateUserUseCase(IUnitOfWork unitOfWork, IIdentityService identityService)
{
    public async Task<User> ExecuteAsync(string email, string name)
    {
        unitOfWork.BeginTransaction();

        try
        {
            // 1. Crear usuario en servicio externo (Auth0)
            var password = GenerateRandomPassword();
            var auth0User = identityService.Create(email, name, password);

            // 2. Crear usuario en base de datos
            var user = await unitOfWork.Users.CreateAsync(email, name);

            unitOfWork.Commit();

            return user;
        }
        catch (HttpRequestException httpEx)
        {
            // ❌ Error en servicio externo
            unitOfWork.Rollback();
            throw new ExternalServiceException("Error creating user in Auth0", httpEx);
        }
        catch (Exception ex)
        {
            // ❌ Error en base de datos
            unitOfWork.Rollback();

            // ⚠️ COMPENSACIÓN: Eliminar usuario de Auth0
            try
            {
                identityService.Delete(email);
            }
            catch
            {
                // Log error de compensación
            }

            throw;
        }
    }
}
```

### 4️⃣ Operación de Solo Lectura (Sin Transacción)

```csharp
public class GetUserByEmailUseCase(IUnitOfWork unitOfWork)
{
    public async Task<User?> ExecuteAsync(string email)
    {
        // ✅ NO se necesita BeginTransaction() para solo lectura
        return await unitOfWork.Users.GetByEmailAsync(email);
    }
}
```

---

## Mejores Prácticas

### ✅ DO: Buenas Prácticas

#### 1. Usar Try-Catch-Finally para Transacciones

```csharp
// ✅ CORRECTO: Siempre manejar excepciones
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        // Operaciones
        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw; // Re-lanzar para que el caller maneje
    }
}
```

#### 2. Una Transacción por Use Case

```csharp
// ✅ CORRECTO: BeginTransaction al inicio, Commit al final
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction(); // ✅ Una sola vez

    try
    {
        await Operation1();
        await Operation2();
        await Operation3();
        unitOfWork.Commit(); // ✅ Una sola vez
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }
}
```

#### 3. Acceder a Repositorios a través de Unit of Work

```csharp
// ✅ CORRECTO: Acceso a través de Unit of Work
public class Handler(IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync()
    {
        var user = await unitOfWork.Users.GetByEmailAsync("test@example.com");
        var role = await unitOfWork.Roles.GetByNameAsync("Admin");
    }
}
```

#### 4. No Transacciones para Solo Lectura

```csharp
// ✅ CORRECTO: Sin transacción para consultas
public async Task<IEnumerable<User>> GetAllUsersAsync()
{
    // ✅ NO se necesita BeginTransaction()
    return await unitOfWork.Users.GetAsync();
}
```

#### 5. Usar Result<T> para Manejar Errores

```csharp
// ✅ CORRECTO: Usar FluentResults para manejo de errores
public async Task<Result<User>> ExecuteAsync(string email)
{
    unitOfWork.BeginTransaction();

    try
    {
        var user = await unitOfWork.Users.CreateAsync(email);
        unitOfWork.Commit();
        return Result.Ok(user);
    }
    catch (DuplicatedDomainException ex)
    {
        unitOfWork.Rollback();
        return Result.Fail(new Error("User already exists").CausedBy(ex));
    }
    catch (Exception ex)
    {
        unitOfWork.Rollback();
        return Result.Fail(new Error("Unexpected error").CausedBy(ex));
    }
}
```

### ❌ DON'T: Antipatrones

#### 1. NO Olvidar Rollback en Catch

```csharp
// ❌ INCORRECTO: Sin Rollback
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        // Operaciones
        unitOfWork.Commit();
    }
    catch
    {
        // ❌ FALTA Rollback
        throw;
    }
}

// ✅ CORRECTO: Con Rollback
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        // Operaciones
        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback(); // ✅ Siempre hacer Rollback
        throw;
    }
}
```

#### 2. NO Múltiples Transacciones en un Use Case

```csharp
// ❌ INCORRECTO: Múltiples transacciones
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction(); // ❌ Primera transacción
    // Operaciones 1
    unitOfWork.Commit();

    unitOfWork.BeginTransaction(); // ❌ Segunda transacción
    // Operaciones 2
    unitOfWork.Commit();
}

// ✅ CORRECTO: Una sola transacción
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        // Todas las operaciones en una sola transacción
        await Operation1();
        await Operation2();
        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }
}
```

#### 3. NO Inyectar Repositorios Directamente (cuando se usa UoW)

```csharp
// ❌ INCORRECTO: Inyectar repositorios directamente
public class Handler(IUserRepository userRepository, IRoleRepository roleRepository)
{
    // ❌ Problema: Cada repositorio puede tener su propia sesión
}

// ✅ CORRECTO: Inyectar Unit of Work
public class Handler(IUnitOfWork unitOfWork)
{
    // ✅ Todos los repositorios comparten la misma sesión
    var users = unitOfWork.Users;
    var roles = unitOfWork.Roles;
}
```

#### 4. NO Hacer Commit Parcial

```csharp
// ❌ INCORRECTO: Commit parcial
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        await Operation1();
        unitOfWork.Commit(); // ❌ Commit prematuro

        await Operation2(); // ❌ Esta operación no está en transacción
    }
    catch
    {
        unitOfWork.Rollback(); // ❌ Solo deshace Operation2
        throw;
    }
}

// ✅ CORRECTO: Commit al final
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        await Operation1();
        await Operation2();
        unitOfWork.Commit(); // ✅ Commit de TODO
    }
    catch
    {
        unitOfWork.Rollback(); // ✅ Deshace TODO
        throw;
    }
}
```

---

## Antipatrones Comunes

### ❌ 1. Nested Transactions (Transacciones Anidadas)

**Problema:** Intentar iniciar una transacción dentro de otra.

```csharp
// ❌ INCORRECTO
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction(); // Transacción 1

    try
    {
        await Operation1();

        // ❌ Intentar anidar transacción
        unitOfWork.BeginTransaction(); // Transacción 2 (ERROR)
        await Operation2();
        unitOfWork.Commit();

        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }
}

// ✅ CORRECTO: Una sola transacción
public async Task ExecuteAsync()
{
    unitOfWork.BeginTransaction();

    try
    {
        await Operation1();
        await Operation2();
        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }
}
```

### ❌ 2. Silent Rollback (Rollback sin Re-lanzar)

**Problema:** Hacer rollback pero no propagar la excepción.

```csharp
// ❌ INCORRECTO: Rollback silencioso
public async Task<User?> ExecuteAsync(string email)
{
    unitOfWork.BeginTransaction();

    try
    {
        var user = await unitOfWork.Users.CreateAsync(email);
        unitOfWork.Commit();
        return user;
    }
    catch
    {
        unitOfWork.Rollback();
        return null; // ❌ Oculta el error
    }
}

// ✅ CORRECTO: Propagar excepción o usar Result<T>
public async Task<Result<User>> ExecuteAsync(string email)
{
    unitOfWork.BeginTransaction();

    try
    {
        var user = await unitOfWork.Users.CreateAsync(email);
        unitOfWork.Commit();
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        unitOfWork.Rollback();
        return Result.Fail(new Error("Failed to create user").CausedBy(ex));
    }
}
```

### ❌ 3. Long-Running Transactions (Transacciones de Larga Duración)

**Problema:** Transacciones que incluyen operaciones lentas o esperas.

```csharp
// ❌ INCORRECTO: Transacción con operación externa lenta
public async Task ExecuteAsync(string email)
{
    unitOfWork.BeginTransaction();

    try
    {
        var user = await unitOfWork.Users.CreateAsync(email);

        // ❌ Operación lenta dentro de transacción
        await SendWelcomeEmail(email); // Tarda 5 segundos

        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }
}

// ✅ CORRECTO: Operaciones lentas fuera de transacción
public async Task ExecuteAsync(string email)
{
    User user;

    // Transacción corta
    unitOfWork.BeginTransaction();
    try
    {
        user = await unitOfWork.Users.CreateAsync(email);
        unitOfWork.Commit();
    }
    catch
    {
        unitOfWork.Rollback();
        throw;
    }

    // ✅ Operación lenta después del commit
    await SendWelcomeEmail(email);
}
```

### ❌ 4. God Unit of Work (UoW con Demasiados Repositorios)

**Problema:** Unit of Work con decenas de repositorios.

```csharp
// ❌ INCORRECTO: Demasiados repositorios
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IInvoiceRepository Invoices { get; }
    IPaymentRepository Payments { get; }
    IShipmentRepository Shipments { get; }
    ICustomerRepository Customers { get; }
    // ... 20 repositorios más ❌
}

// ✅ CORRECTO: Múltiples UoW por bounded context
public interface IIdentityUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
}

public interface ISalesUnitOfWork
{
    IOrderRepository Orders { get; }
    IInvoiceRepository Invoices { get; }
    IPaymentRepository Payments { get; }
}
```

---

## Checklist de Implementación

### 📋 Interfaz (Domain Layer)

- [ ] **IUnitOfWork** definida en `domain/interfaces/repositories/`
  - [ ] Propiedades de repositorios
  - [ ] Métodos de transacción: `BeginTransaction()`, `Commit()`, `Rollback()`
  - [ ] Implementa `IDisposable`

### 📋 Implementación (Infrastructure Layer)

- [ ] **NHUnitOfWork** implementa `IUnitOfWork`
  - [ ] Constructor recibe `ISession` y `IServiceProvider`
  - [ ] Propiedades de repositorios crean instancias con `_session` compartida
  - [ ] `BeginTransaction()` llama a `_session.BeginTransaction()`
  - [ ] `Commit()` confirma transacción activa
  - [ ] `Rollback()` deshace transacción activa
  - [ ] `Dispose()` libera `_transaction` y `_session`

### 📋 Uso en Application Layer

- [ ] **Use Cases** reciben `IUnitOfWork` en constructor
  - [ ] `BeginTransaction()` al inicio de operaciones de escritura
  - [ ] Try-catch para manejar excepciones
  - [ ] `Commit()` en try si todo sale bien
  - [ ] `Rollback()` en catch para deshacer cambios
  - [ ] No se usa transacción para solo lectura

### 📋 Dependency Injection

- [ ] **NHUnitOfWork** registrado en DI
  - [ ] Lifetime: `Scoped` (una instancia por request HTTP)
  - [ ] `ISession` también es `Scoped`

---

## Ejemplos Completos

### Ejemplo 1: Crear Usuario con Rol (Transaccional)

```csharp
// Application Layer: CreateUserWithRoleUseCase.cs
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.users;

public class CreateUserWithRoleUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            // ─────────────────────────────────────────────────────────────
            // Iniciar transacción
            // ─────────────────────────────────────────────────────────────
            _unitOfWork.BeginTransaction();

            try
            {
                // ─────────────────────────────────────────────────────────────
                // 1. Crear usuario
                // ─────────────────────────────────────────────────────────────
                var user = await _unitOfWork.Users.CreateAsync(command.Email, command.Name);

                // ─────────────────────────────────────────────────────────────
                // 2. Obtener role
                // ─────────────────────────────────────────────────────────────
                var role = await _unitOfWork.Roles.GetByNameAsync(command.RoleName);
                if (role == null)
                {
                    _unitOfWork.Rollback();
                    return Result.Fail($"Role '{command.RoleName}' not found");
                }

                // ─────────────────────────────────────────────────────────────
                // 3. Asignar role al usuario
                // ─────────────────────────────────────────────────────────────
                user.AddRole(role);
                await _unitOfWork.Users.SaveAsync(user);

                // ─────────────────────────────────────────────────────────────
                // 4. Commit de TODO
                // ─────────────────────────────────────────────────────────────
                _unitOfWork.Commit();

                return Result.Ok(user);
            }
            catch (DuplicatedDomainException ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error($"User with email '{command.Email}' already exists").CausedBy(ex));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error("Failed to create user").CausedBy(ex));
            }
        }
    }
}
```

### Ejemplo 2: Crear Usuario con Servicio Externo (Auth0)

```csharp
// Application Layer: CreateUserUseCase.cs
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.interfaces.services;

namespace hashira.stone.backend.application.usecases.users;

public class CreateUserUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IIdentityService _identityService = identityService;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                // ─────────────────────────────────────────────────────────────
                // 1. Crear usuario en Auth0 (servicio externo)
                // ─────────────────────────────────────────────────────────────
                var password = GenerateRandomPassword();
                var auth0User = _identityService.Create(command.Email, command.Name, password);

                // ─────────────────────────────────────────────────────────────
                // 2. Crear usuario en base de datos
                // ─────────────────────────────────────────────────────────────
                var user = await _unitOfWork.Users.CreateAsync(command.Email, command.Name);

                // ─────────────────────────────────────────────────────────────
                // 3. Commit
                // ─────────────────────────────────────────────────────────────
                _unitOfWork.Commit();

                return Result.Ok(user);
            }
            catch (HttpRequestException httpEx)
            {
                // Error creando usuario en Auth0
                _unitOfWork.Rollback();
                return Result.Fail(new Error($"Error creating user in Auth0").CausedBy(httpEx));
            }
            catch (DuplicatedDomainException ddex)
            {
                // Usuario duplicado en BD
                _unitOfWork.Rollback();

                // COMPENSACIÓN: Eliminar usuario de Auth0
                try
                {
                    _identityService.Delete(command.Email);
                }
                catch { /* Log error */ }

                return Result.Fail(new Error($"User already exists").CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();

                // COMPENSACIÓN: Eliminar usuario de Auth0
                try
                {
                    _identityService.Delete(command.Email);
                }
                catch { /* Log error */ }

                return Result.Fail(new Error("Unexpected error").CausedBy(ex));
            }
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            const int length = 12;
            var random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }
    }
}
```

### Ejemplo 3: Consulta Simple (Sin Transacción)

```csharp
// Application Layer: GetUserByEmailUseCase.cs
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.users;

public class GetUserByEmailUseCase
{
    public class Query : IQuery<Result<User?>>
    {
        public string Email { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork) : IQueryHandler<Query, Result<User?>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<User?>> ExecuteAsync(Query query, CancellationToken ct)
        {
            // ✅ NO se necesita BeginTransaction() para solo lectura
            var user = await _unitOfWork.Users.GetByEmailAsync(query.Email);

            if (user == null)
                return Result.Fail("User not found");

            return Result.Ok(user);
        }
    }
}
```

---

## Recursos Adicionales

### 📚 Guías Relacionadas

- [Repository Pattern](./repository-pattern.md) - Patrón complementario al Unit of Work
- [Core Concepts](./core-concepts.md) - Conceptos fundamentales de Infrastructure Layer
- [Transactions](./transactions.md) - Guía detallada de manejo de transacciones
- [Dependency Injection](./dependency-injection.md) - Registro de UoW en DI

### 🔗 Referencias Externas

- [Unit of Work Pattern - Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [NHibernate Transactions](https://nhibernate.info/doc/nhibernate-reference/transactions.html)
- [FluentResults](https://github.com/altmann/FluentResults)

---

**Versión:** 1.0.0
**Fecha:** 2025-01-14
**Autor:** Equipo de Arquitectura
