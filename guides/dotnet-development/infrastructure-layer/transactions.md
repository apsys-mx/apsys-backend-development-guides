# Transactions (Transacciones)

**Versión:** 1.0.0
**Última actualización:** 2025-01-14

## Tabla de Contenidos
- [¿Qué es una Transacción?](#qué-es-una-transacción)
- [Propiedades ACID](#propiedades-acid)
- [Niveles de Aislamiento](#niveles-de-aislamiento)
- [Ciclo de Vida de una Transacción](#ciclo-de-vida-de-una-transacción)
- [Patrones de Manejo de Transacciones](#patrones-de-manejo-de-transacciones)
- [Manejo de Excepciones](#manejo-de-excepciones)
- [Transacciones vs Flush](#transacciones-vs-flush)
- [Problemas Comunes](#problemas-comunes)
- [Mejores Prácticas](#mejores-prácticas)
- [Antipatrones](#antipatrones)
- [Checklist](#checklist)
- [Ejemplos Completos](#ejemplos-completos)

---

## ¿Qué es una Transacción?

Una **transacción** es una unidad de trabajo que agrupa **múltiples operaciones** en la base de datos y garantiza que **todas se ejecuten correctamente o ninguna se ejecute**.

### Analogía del Mundo Real

Piensa en una **transferencia bancaria**:
- **Débito de cuenta origen**: -$100
- **Crédito a cuenta destino**: +$100

**Con transacción:**
- ✅ Ambas operaciones se completan → Transferencia exitosa
- ❌ Una operación falla → Se deshacen AMBAS (rollback)

**Sin transacción:**
- ❌ Se debita $100 pero el crédito falla → Se pierde el dinero

### Concepto Visual

```
┌─────────────────────────────────────────────────────────────┐
│                    TRANSACCIÓN                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  BeginTransaction()                                         │
│      │                                                       │
│      ├──► Operación 1: Crear Usuario                        │
│      ├──► Operación 2: Asignar Rol                          │
│      ├──► Operación 3: Crear Log de Auditoría              │
│      │                                                       │
│      ▼                                                       │
│  ✅ Commit()  ──► TODAS se ejecutan                         │
│      ó                                                       │
│  ❌ Rollback() ──► NINGUNA se ejecuta                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Propiedades ACID

Las transacciones deben cumplir con las propiedades **ACID**:

### 🔐 Atomicidad (Atomicity)

**"Todo o Nada"**

Una transacción es **indivisible**: todas las operaciones se completan o ninguna se completa.

```csharp
// ✅ Atomicidad garantizada
unitOfWork.BeginTransaction();
try
{
    await unitOfWork.Users.CreateAsync(email, name);     // Operación 1
    await unitOfWork.Roles.AssignToUserAsync(userId);     // Operación 2
    await unitOfWork.Logs.CreateAsync("User created");    // Operación 3

    unitOfWork.Commit(); // ✅ Las 3 operaciones se ejecutan
}
catch
{
    unitOfWork.Rollback(); // ❌ Las 3 operaciones se deshacen
    throw;
}
```

### ✔️ Consistencia (Consistency)

**"Estado válido a estado válido"**

La base de datos siempre se mantiene en un **estado consistente** (respeta constraints, foreign keys, etc.).

```csharp
// ✅ Consistencia garantizada
unitOfWork.BeginTransaction();
try
{
    var user = await unitOfWork.Users.CreateAsync(email);
    var role = await unitOfWork.Roles.GetByIdAsync(roleId);

    // ✅ La FK de User.RoleId apunta a un Role válido
    user.AssignRole(role);

    unitOfWork.Commit();
}
catch
{
    unitOfWork.Rollback(); // ❌ No se permite estado inconsistente
    throw;
}
```

### 🔒 Aislamiento (Isolation)

**"Operaciones concurrentes no interfieren"**

Las transacciones concurrentes se ejecutan de manera **aislada** (una no ve cambios parciales de otra).

```csharp
// Transacción A (Usuario 1)
unitOfWork.BeginTransaction();
var product = await unitOfWork.Products.GetAsync(productId);
product.DecrementStock(1); // Stock: 10 → 9
// ... (aún no hace Commit)

// Transacción B (Usuario 2) - ejecutándose al mismo tiempo
unitOfWork2.BeginTransaction();
var product2 = await unitOfWork2.Products.GetAsync(productId);
// ✅ Lee Stock: 10 (NO ve el cambio de la Transacción A)
product2.DecrementStock(1); // Stock: 10 → 9

// ❌ Problema: Ambas leen Stock=10, pero debería ser 8 al final
```

**Solución:** Usar niveles de aislamiento adecuados (ver siguiente sección).

### 💾 Durabilidad (Durability)

**"Los cambios confirmados persisten"**

Una vez que `Commit()` se ejecuta exitosamente, los cambios **persisten** incluso si hay un fallo del sistema.

```csharp
unitOfWork.BeginTransaction();
try
{
    await unitOfWork.Users.CreateAsync(email);
    unitOfWork.Commit(); // ✅ Usuario guardado en disco

    // 🔌 Sistema se cae aquí
}
catch { }

// ✅ Al reiniciar el sistema, el usuario creado sigue existiendo
```

---

## Niveles de Aislamiento

Los **niveles de aislamiento** controlan **qué tan aislada** está una transacción de otras transacciones concurrentes.

### 📊 Tabla de Niveles de Aislamiento

| Nivel | Read Uncommitted | Read Committed | Repeatable Read | Serializable |
|-------|------------------|----------------|-----------------|--------------|
| **Dirty Read** | ❌ Permitido | ✅ Previene | ✅ Previene | ✅ Previene |
| **Non-Repeatable Read** | ❌ Permitido | ❌ Permitido | ✅ Previene | ✅ Previene |
| **Phantom Read** | ❌ Permitido | ❌ Permitido | ❌ Permitido | ✅ Previene |
| **Performance** | 🚀 Muy rápido | ⚡ Rápido | 🐌 Lento | 🐢 Muy lento |
| **Locks** | Ninguno | Lectura | Lectura + Escritura | Todos |

### 1️⃣ Read Uncommitted (Menos estricto)

Lee datos **no confirmados** de otras transacciones.

```csharp
// ❌ NO RECOMENDADO: Permite "Dirty Reads"
unitOfWork.BeginTransaction(IsolationLevel.ReadUncommitted);

// Transacción A
var product = await unitOfWork.Products.GetAsync(productId);
product.Price = 100; // NO ha hecho Commit

// Transacción B lee Price = 100 (dato NO confirmado)
// Si Transacción A hace Rollback, Transacción B leyó un dato inválido
```

**⚠️ Usar solo para:** Reportes no críticos donde la precisión no es esencial.

### 2️⃣ Read Committed (Por defecto en SQL Server)

Lee **solo datos confirmados**. Previene "Dirty Reads".

```csharp
// ✅ RECOMENDADO para la mayoría de casos
unitOfWork.BeginTransaction(IsolationLevel.ReadCommitted);

try
{
    var user = await unitOfWork.Users.CreateAsync(email);
    unitOfWork.Commit();
}
catch
{
    unitOfWork.Rollback();
    throw;
}
```

**✅ Usar para:** Operaciones CRUD estándar.

### 3️⃣ Repeatable Read

Garantiza que **lecturas repetidas** devuelven el mismo valor.

```csharp
// ✅ Previene "Non-Repeatable Reads"
unitOfWork.BeginTransaction(IsolationLevel.RepeatableRead);

var product1 = await unitOfWork.Products.GetAsync(productId); // Price = 100

// Otra transacción actualiza el precio a 200 y hace Commit

var product2 = await unitOfWork.Products.GetAsync(productId); // Price = 100 (mismo valor)

unitOfWork.Commit();
```

**✅ Usar para:** Cálculos financieros, reportes críticos.

### 4️⃣ Serializable (Más estricto)

Transacciones se ejecutan **como si fueran seriales** (una después de otra).

```csharp
// ✅ Máxima consistencia, pero más lento
unitOfWork.BeginTransaction(IsolationLevel.Serializable);

var users = await unitOfWork.Users.GetAsync(u => u.IsActive);

// ✅ Ninguna otra transacción puede insertar/modificar/eliminar usuarios activos
// hasta que esta transacción haga Commit

unitOfWork.Commit();
```

**✅ Usar para:** Operaciones críticas (inventario, contabilidad).

**⚠️ Cuidado:** Puede causar **deadlocks** y es **más lento**.

### 🎯 Nivel de Aislamiento en NHibernate

```csharp
// Especificar nivel de aislamiento al iniciar transacción
public void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
{
    this._transaction = this._session.BeginTransaction(isolationLevel);
}
```

**Uso en Application Layer:**

```csharp
public class CreateUserUseCase(IUnitOfWork unitOfWork)
{
    public async Task<User> ExecuteAsync(string email)
    {
        // ✅ Usar nivel de aislamiento específico
        unitOfWork.BeginTransaction(IsolationLevel.ReadCommitted);

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
    }
}
```

---

## Ciclo de Vida de una Transacción

### 🔄 Estados de una Transacción

```
┌─────────────┐
│   INICIO    │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ BeginTransaction()
└──────┬──────┘
       │
       ▼
┌─────────────┐     ┌─────────────────┐
│   ACTIVA    │────►│  Operaciones    │
└──────┬──────┘     │  (CRUD, etc.)   │
       │            └─────────────────┘
       │
       ├──────────────────┬──────────────────┐
       │                  │                  │
       ▼                  ▼                  ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│  Commit()   │   │ Rollback()  │   │   Error     │
└──────┬──────┘   └──────┬──────┘   └──────┬──────┘
       │                  │                  │
       ▼                  ▼                  ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│ COMMITTED   │   │ ROLLED BACK │   │   FAILED    │
└─────────────┘   └─────────────┘   └─────────────┘
```

### 📋 Métodos del Ciclo de Vida

| Método | Descripción | Estado |
|--------|-------------|--------|
| `BeginTransaction()` | Inicia una nueva transacción | `ACTIVA` |
| `Commit()` | Confirma cambios a la BD | `COMMITTED` |
| `Rollback()` | Deshace todos los cambios | `ROLLED BACK` |
| `IsActiveTransaction()` | Verifica si hay transacción activa | - |
| `Dispose()` | Libera recursos de la transacción | - |

---

## Patrones de Manejo de Transacciones

### 1️⃣ Patrón Try-Catch-Rollback (Estándar)

**El más común en el proyecto de referencia.**

```csharp
public async Task<Result<User>> ExecuteAsync(string email, string name)
{
    _unitOfWork.BeginTransaction();

    try
    {
        // ─────────────────────────────────────────────────────────────
        // Operaciones dentro de la transacción
        // ─────────────────────────────────────────────────────────────
        var user = await _unitOfWork.Users.CreateAsync(email, name);

        // ─────────────────────────────────────────────────────────────
        // Commit si todo sale bien
        // ─────────────────────────────────────────────────────────────
        _unitOfWork.Commit();

        return Result.Ok(user);
    }
    catch (InvalidDomainException idex)
    {
        _unitOfWork.Rollback();

        var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
        var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";

        return Result.Fail(new Error(firstErrorMessage)
            .CausedBy(idex)
            .WithMetadata("ValidationErrors", idex));
    }
    catch (DuplicatedDomainException ddex)
    {
        _unitOfWork.Rollback();
        return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
    }
    catch (Exception ex)
    {
        _unitOfWork.Rollback();
        return Result.Fail(new Error(ex.Message).CausedBy(ex));
    }
}
```

**🔑 Puntos Clave:**
1. **BeginTransaction()** al inicio
2. **Try** envuelve todas las operaciones
3. **Commit()** en try si todo va bien
4. **Rollback()** en CADA catch
5. **Retornar Result.Fail** con información del error

### 2️⃣ Patrón Transaction Scope

**Transacción implícita (sin BeginTransaction explícito).**

```csharp
// ❌ NO usado en el proyecto de referencia, pero válido en otros contextos
using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    var user = await _unitOfWork.Users.CreateAsync(email);
    var role = await _unitOfWork.Roles.AssignToUserAsync(userId);

    scope.Complete(); // ✅ Commit implícito
} // ✅ Rollback automático si no se llama Complete()
```

**⚠️ Nota:** El proyecto de referencia usa `BeginTransaction()` explícito (Patrón 1).

### 3️⃣ Patrón Nested Try-Catch

**Para manejo de excepciones específicas.**

```csharp
public async Task<Result<User>> ExecuteAsync(string email)
{
    _unitOfWork.BeginTransaction();

    try
    {
        var user = await _unitOfWork.Users.CreateAsync(email);

        try
        {
            // Operación externa (Auth0)
            await _identityService.CreateAsync(email);
        }
        catch (HttpRequestException httpEx)
        {
            // ❌ Error en servicio externo
            _unitOfWork.Rollback();
            return Result.Fail("Error creating user in Auth0");
        }

        _unitOfWork.Commit();
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _unitOfWork.Rollback();
        return Result.Fail(ex.Message);
    }
}
```

### 4️⃣ Patrón Compensating Transaction

**Para operaciones con servicios externos.**

```csharp
public async Task<Result<User>> ExecuteAsync(string email, string name)
{
    _unitOfWork.BeginTransaction();

    try
    {
        // 1. Crear usuario en Auth0 (servicio externo)
        var auth0User = await _identityService.CreateAsync(email, name);

        // 2. Crear usuario en BD
        var user = await _unitOfWork.Users.CreateAsync(email, name);

        _unitOfWork.Commit();
        return Result.Ok(user);
    }
    catch (HttpRequestException httpEx)
    {
        // ❌ Error en Auth0
        _unitOfWork.Rollback();
        return Result.Fail("Error in Auth0");
    }
    catch (Exception ex)
    {
        // ❌ Error en BD
        _unitOfWork.Rollback();

        // ⚠️ COMPENSACIÓN: Eliminar usuario de Auth0
        try
        {
            await _identityService.DeleteAsync(email);
        }
        catch (Exception compEx)
        {
            // Log error de compensación
            _logger.LogError(compEx, "Failed to compensate Auth0 user");
        }

        return Result.Fail(ex.Message);
    }
}
```

---

## Manejo de Excepciones

### 🎯 Estrategia de Captura de Excepciones

El proyecto de referencia captura **excepciones específicas** en orden de más específica a más genérica:

```csharp
_unitOfWork.BeginTransaction();

try
{
    // Operaciones
    _unitOfWork.Commit();
    return Result.Ok(entity);
}
catch (InvalidDomainException idex)
{
    // 1. Excepciones de validación
    _unitOfWork.Rollback();

    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";

    return Result.Fail(new Error(firstErrorMessage)
        .CausedBy(idex)
        .WithMetadata("ValidationErrors", idex));
}
catch (DuplicatedDomainException ddex)
{
    // 2. Excepciones de duplicados
    _unitOfWork.Rollback();
    return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
}
catch (HttpRequestException httpEx)
{
    // 3. Excepciones de servicios externos
    _unitOfWork.Rollback();
    return Result.Fail(new Error("External service error").CausedBy(httpEx));
}
catch (Exception ex)
{
    // 4. Excepciones genéricas
    _unitOfWork.Rollback();
    return Result.Fail(new Error("Unexpected error").CausedBy(ex));
}
```

### 📋 Tipos de Excepciones del Proyecto

| Excepción | Descripción | Acción |
|-----------|-------------|--------|
| `InvalidDomainException` | Validación FluentValidation falló | Rollback + retornar errores de validación |
| `DuplicatedDomainException` | Entidad duplicada (email, código, etc.) | Rollback + retornar error de duplicado |
| `NotFoundException` | Entidad no encontrada | Rollback + retornar error 404 |
| `HttpRequestException` | Error en servicio externo (Auth0, API) | Rollback + compensación |
| `Exception` | Error inesperado | Rollback + log + retornar error genérico |

### 💡 Ejemplo Completo del Proyecto

```csharp
// CreatePrototypeUseCase.cs del proyecto de referencia
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
```

---

## Transacciones vs Flush

### 🔄 ¿Qué es Flush?

**Flush** sincroniza el estado en memoria de NHibernate con la base de datos, **ejecutando los comandos SQL pendientes**.

**⚠️ Importante:** `Flush()` **NO confirma** la transacción. Solo ejecuta SQL.

### 📊 Comparación: Flush vs Commit

| Operación | Flush() | Commit() |
|-----------|---------|----------|
| **Ejecuta SQL** | ✅ Sí | ✅ Sí (implícitamente) |
| **Confirma transacción** | ❌ No | ✅ Sí |
| **Puede hacer Rollback después** | ✅ Sí | ❌ No |
| **Libera locks** | ❌ No | ✅ Sí |

### 💡 Ejemplo: Flush vs Commit

```csharp
_unitOfWork.BeginTransaction();

try
{
    var user = new User("test@example.com");
    await _unitOfWork.Users.AddAsync(user);

    // ─────────────────────────────────────────────────────────────
    // Flush: Ejecuta INSERT pero NO confirma
    // ─────────────────────────────────────────────────────────────
    _session.Flush();
    // ✅ SQL ejecutado: INSERT INTO Users ...
    // ⚠️ Transacción sigue activa
    // ✅ Puede hacer Rollback aún

    var userId = user.Id; // ✅ ID ya generado por la BD

    // Hacer algo con el userId...

    // ─────────────────────────────────────────────────────────────
    // Commit: Confirma la transacción
    // ─────────────────────────────────────────────────────────────
    _unitOfWork.Commit();
    // ✅ Transacción confirmada
    // ❌ Ya NO se puede hacer Rollback
}
catch
{
    _unitOfWork.Rollback(); // ✅ Deshace incluso después de Flush
    throw;
}
```

### 🎯 FlushWhenNotActiveTransaction Pattern

El proyecto de referencia usa este patrón en repositorios:

```csharp
protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();

    if (currentTransaction == null || !currentTransaction.IsActive)
    {
        // ✅ NO hay transacción → Flush inmediato
        this._session.Flush();
    }
    else
    {
        // ⏳ HAY transacción → El Commit() hará el Flush
    }
}
```

**¿Por qué?**
- **CON transacción activa:** El `Commit()` del UnitOfWork hace `Flush()` automáticamente
- **SIN transacción:** El repositorio hace `Flush()` inmediato para persistir cambios

---

## Problemas Comunes

### ❌ 1. Deadlocks

**Problema:** Dos transacciones esperan recursos bloqueados por la otra.

```csharp
// Transacción A
_unitOfWork.BeginTransaction(IsolationLevel.Serializable);
var user = await _unitOfWork.Users.GetAsync(userId1);
var role = await _unitOfWork.Roles.GetAsync(roleId2); // ⏳ Espera lock

// Transacción B (al mismo tiempo)
_unitOfWork2.BeginTransaction(IsolationLevel.Serializable);
var role2 = await _unitOfWork2.Roles.GetAsync(roleId2); // ✅ Obtiene lock
var user2 = await _unitOfWork2.Users.GetAsync(userId1); // ⏳ Espera lock

// ❌ DEADLOCK: A espera a B, B espera a A
```

**✅ Solución:**
1. **Ordenar acceso a recursos** (siempre Users → Roles)
2. **Usar nivel de aislamiento más bajo** (ReadCommitted en lugar de Serializable)
3. **Timeouts** para detectar deadlocks

```csharp
// ✅ CORRECTO: Mismo orden en ambas transacciones
var user = await _unitOfWork.Users.GetAsync(userId);
var role = await _unitOfWork.Roles.GetAsync(roleId);
```

### ❌ 2. Transacciones de Larga Duración

**Problema:** Mantener transacción abierta durante operaciones lentas.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction();
try
{
    var user = await _unitOfWork.Users.CreateAsync(email);

    // ❌ Operación lenta DENTRO de transacción
    await SendWelcomeEmail(email); // 5 segundos
    await GenerateReport(); // 10 segundos

    _unitOfWork.Commit();
}
catch { }
```

**✅ Solución:** Operaciones lentas FUERA de transacción

```csharp
// ✅ CORRECTO
User user;

// Transacción corta
_unitOfWork.BeginTransaction();
try
{
    user = await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit();
}
catch
{
    _unitOfWork.Rollback();
    throw;
}

// ✅ Operaciones lentas DESPUÉS del commit
await SendWelcomeEmail(email);
await GenerateReport();
```

### ❌ 3. Olvidar Rollback en Catch

**Problema:** No hacer rollback cuando ocurre un error.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction();
try
{
    await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit();
}
catch
{
    // ❌ FALTA Rollback
    throw;
}
// ⚠️ La transacción queda abierta y puede causar locks
```

**✅ Solución:** Siempre hacer Rollback en catch

```csharp
// ✅ CORRECTO
_unitOfWork.BeginTransaction();
try
{
    await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit();
}
catch
{
    _unitOfWork.Rollback(); // ✅ Siempre hacer Rollback
    throw;
}
```

### ❌ 4. Transacciones Anidadas

**Problema:** Intentar iniciar una transacción dentro de otra.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction(); // Transacción 1

try
{
    await Operation1();

    _unitOfWork.BeginTransaction(); // ❌ Transacción 2 (ERROR)
    await Operation2();
    _unitOfWork.Commit();

    _unitOfWork.Commit();
}
catch { }
```

**✅ Solución:** Una sola transacción por scope

```csharp
// ✅ CORRECTO
_unitOfWork.BeginTransaction();

try
{
    await Operation1();
    await Operation2();
    _unitOfWork.Commit();
}
catch
{
    _unitOfWork.Rollback();
    throw;
}
```

---

## Mejores Prácticas

### ✅ 1. Una Transacción por Use Case

```csharp
// ✅ CORRECTO: BeginTransaction al inicio, Commit al final
public async Task<Result<User>> ExecuteAsync(string email)
{
    _unitOfWork.BeginTransaction(); // ✅ Una sola vez

    try
    {
        var user = await _unitOfWork.Users.CreateAsync(email);
        var role = await _unitOfWork.Roles.GetByNameAsync("User");
        user.AssignRole(role);
        await _unitOfWork.Users.SaveAsync(user);

        _unitOfWork.Commit(); // ✅ Una sola vez
        return Result.Ok(user);
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
```

### ✅ 2. Capturar Excepciones Específicas Primero

```csharp
// ✅ CORRECTO: De más específica a más genérica
try
{
    // Operaciones
}
catch (InvalidDomainException idex)     // 1. Más específica
{
    _unitOfWork.Rollback();
    return Result.Fail("Validation error");
}
catch (DuplicatedDomainException ddex)  // 2. Específica
{
    _unitOfWork.Rollback();
    return Result.Fail("Duplicate error");
}
catch (Exception ex)                    // 3. Más genérica
{
    _unitOfWork.Rollback();
    return Result.Fail("Unexpected error");
}
```

### ✅ 3. Usar Result<T> para Manejar Errores

```csharp
// ✅ CORRECTO: Usar FluentResults
public async Task<Result<User>> ExecuteAsync(string email)
{
    _unitOfWork.BeginTransaction();

    try
    {
        var user = await _unitOfWork.Users.CreateAsync(email);
        _unitOfWork.Commit();
        return Result.Ok(user); // ✅ Success
    }
    catch (Exception ex)
    {
        _unitOfWork.Rollback();
        return Result.Fail(new Error(ex.Message).CausedBy(ex)); // ✅ Failure
    }
}
```

### ✅ 4. Transacciones Cortas

```csharp
// ✅ CORRECTO: Transacción solo para operaciones de BD
_unitOfWork.BeginTransaction();
try
{
    var user = await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit(); // ✅ Transacción corta
}
catch
{
    _unitOfWork.Rollback();
    throw;
}

// ✅ Operaciones lentas DESPUÉS del commit
await SendEmail(email);
await GeneratePDF();
```

### ✅ 5. Logging de Transacciones

```csharp
// ✅ CORRECTO: Log de transacciones para debugging
public async Task<Result<User>> ExecuteAsync(string email)
{
    _logger.LogInformation("Starting transaction for CreateUser");
    _unitOfWork.BeginTransaction();

    try
    {
        var user = await _unitOfWork.Users.CreateAsync(email);
        _unitOfWork.Commit();
        _logger.LogInformation("Transaction committed successfully");
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Transaction failed, rolling back");
        _unitOfWork.Rollback();
        return Result.Fail(ex.Message);
    }
}
```

---

## Antipatrones

### ❌ 1. Silent Rollback

**Problema:** Hacer rollback pero no informar al usuario.

```csharp
// ❌ INCORRECTO
try
{
    await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit();
    return Result.Ok();
}
catch
{
    _unitOfWork.Rollback();
    return Result.Ok(); // ❌ Oculta el error
}
```

### ❌ 2. Commit Parcial

**Problema:** Hacer commit en medio de operaciones.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction();
try
{
    await _unitOfWork.Users.CreateAsync(email);
    _unitOfWork.Commit(); // ❌ Commit prematuro

    await _unitOfWork.Roles.AssignToUserAsync(userId); // ❌ Fuera de transacción
}
catch { }
```

### ❌ 3. No Usar Try-Catch

**Problema:** No manejar excepciones.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction();
await _unitOfWork.Users.CreateAsync(email);
_unitOfWork.Commit(); // ❌ Si falla, no hay rollback
```

### ❌ 4. Transacciones en Consultas

**Problema:** Usar transacciones para operaciones de solo lectura.

```csharp
// ❌ INCORRECTO
_unitOfWork.BeginTransaction(); // ❌ Innecesario para solo lectura
var users = await _unitOfWork.Users.GetAsync();
_unitOfWork.Commit();
```

---

## Checklist

### 📋 Antes de Implementar

- [ ] ¿La operación modifica datos? (Si no, NO usar transacción)
- [ ] ¿Hay múltiples operaciones que deben ser atómicas?
- [ ] ¿Qué nivel de aislamiento es apropiado?
- [ ] ¿Hay operaciones lentas que deben estar FUERA de la transacción?

### 📋 Durante la Implementación

- [ ] `BeginTransaction()` al inicio del use case
- [ ] Try-catch envuelve todas las operaciones
- [ ] `Commit()` en el try si todo va bien
- [ ] `Rollback()` en CADA catch
- [ ] Excepciones específicas antes que genéricas
- [ ] Result<T> para retornar éxito/error

### 📋 Después de Implementar

- [ ] Pruebas de rollback funcionan correctamente
- [ ] Logs de transacciones implementados
- [ ] No hay deadlocks en concurrencia
- [ ] Transacciones son cortas (< 2 segundos)

---

## Ejemplos Completos

### Ejemplo 1: Crear Usuario (Simple)

```csharp
// CreateUserUseCase.cs
public class CreateUserUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _logger.LogInformation("Starting CreateUser transaction for {Email}", command.Email);
            _unitOfWork.BeginTransaction();

            try
            {
                var user = await _unitOfWork.Users.CreateAsync(command.Email, command.Name);

                _unitOfWork.Commit();
                _logger.LogInformation("User created successfully: {UserId}", user.Id);

                return Result.Ok(user);
            }
            catch (InvalidDomainException idex)
            {
                _unitOfWork.Rollback();
                _logger.LogWarning("Validation failed: {Message}", idex.Message);

                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";

                return Result.Fail(new Error(firstErrorMessage)
                    .CausedBy(idex)
                    .WithMetadata("ValidationErrors", idex));
            }
            catch (DuplicatedDomainException ddex)
            {
                _unitOfWork.Rollback();
                _logger.LogWarning("Duplicate user: {Message}", ddex.Message);
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                _logger.LogError(ex, "Unexpected error creating user");
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Ejemplo 2: Crear Prototipo (Del Proyecto Real)

```csharp
// CreatePrototypeUseCase.cs (del proyecto hashira.stone.backend)
public class CreatePrototypeUseCase
{
    public class Command : ICommand<Result<Prototype>>
    {
        public string Number { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<Prototype>>
    {
        private readonly IUnitOfWork _uoW = uoW;

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

### Ejemplo 3: Consulta con Transacción (Del Proyecto Real)

```csharp
// GetManyAndCountPrototypesUseCase.cs (del proyecto hashira.stone.backend)
public class GetManyAndCountPrototypesUseCase
{
    public class Command : ICommand<GetManyAndCountResult<PrototypeDao>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<PrototypeDao>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<PrototypeDao>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                _logger.LogInformation("Executing GetManyAndCountPrototypesUseCase with query: {Query}", command.Query);

                var prototypes = await _uoW.PrototypeDaos.GetManyAndCountAsync(
                    command.Query,
                    nameof(Prototype.Number),
                    ct
                );

                _logger.LogInformation("End GetManyAndCountPrototypesUseCase with total: {Total}", prototypes.Count);

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

---

## Recursos Adicionales

### 📚 Guías Relacionadas

- [Unit of Work Pattern](./unit-of-work-pattern.md) - Coordinación de transacciones
- [Repository Pattern](./repository-pattern.md) - Operaciones de datos
- [Core Concepts](./core-concepts.md) - Conceptos fundamentales

### 🔗 Referencias Externas

- [ACID Properties](https://en.wikipedia.org/wiki/ACID)
- [Transaction Isolation Levels](https://docs.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql)
- [NHibernate Transactions](https://nhibernate.info/doc/nhibernate-reference/transactions.html)
- [FluentResults](https://github.com/altmann/FluentResults)

---

**Versión:** 1.0.0
**Fecha:** 2025-01-14
**Autor:** Equipo de Arquitectura
