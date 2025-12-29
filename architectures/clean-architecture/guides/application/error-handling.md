# Error Handling en Application Layer

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

Esta guía documenta las estrategias y patrones para el manejo de errores en la capa de aplicación usando **FluentResults**. El patrón Result permite manejar errores de forma funcional, evitando el uso de excepciones para control de flujo y proporcionando información rica sobre los fallos.

## Índice

- [Principios de Error Handling](#principios-de-error-handling)
- [Result Pattern Fundamentals](#result-pattern-fundamentals)
- [Exception to Result Conversion](#exception-to-result-conversion)
- [Custom Error Classes](#custom-error-classes)
- [Error Context with Metadata](#error-context-with-metadata)
- [Transaction-Based Error Handling](#transaction-based-error-handling)
- [Validation Error Handling](#validation-error-handling)
- [Checklist](#checklist)
- [Mejores Prácticas](#mejores-prácticas)
- [Recursos](#recursos)

---

## Principios de Error Handling

### 1. Result Pattern sobre Excepciones

**Usar Result<T> para flujo de control, excepciones solo para errores inesperados:**

```csharp
// ✅ CORRECTO: Result para errores de negocio
public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
{
    var user = await _uoW.Users.GetByEmailAsync(request.UserName);
    return user == null
        ? Result.Fail(UserErrors.UserNotFound(request.UserName))  // Error esperado
        : Result.Ok(user);
}

// ❌ INCORRECTO: Excepción para flujo de control
public async Task<User> ExecuteAsync(Command request, CancellationToken ct)
{
    var user = await _uoW.Users.GetByEmailAsync(request.UserName);
    if (user == null)
        throw new UserNotFoundException(request.UserName);  // No usar excepciones para control de flujo
    return user;
}
```

### 2. Capturar y Convertir Domain Exceptions

**Las excepciones del dominio deben convertirse a Result en el Handler:**

```csharp
try
{
    var entity = await _uoW.Entities.CreateAsync(...);
    _uoW.Commit();
    return Result.Ok(entity);
}
catch (InvalidDomainException idex)
{
    _uoW.Rollback();
    return Result.Fail(new Error(idex.Message).CausedBy(idex));
}
```

### 3. Errores Ricos con Contexto

**Agregar metadata para debugging y troubleshooting:**

```csharp
return Result.Fail(
    new Error(firstErrorMessage)
        .CausedBy(idex)
        .WithMetadata("ValidationErrors", errors)
        .WithMetadata("UserId", command.Id)
);
```

---

## Result Pattern Fundamentals

### Result&lt;T&gt; vs Result

#### Result&lt;T&gt; - Para operaciones que retornan valor

```csharp
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    try
    {
        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
        _uoW.Commit();
        return Result.Ok(user);  // Retorna el usuario creado
    }
    catch (Exception ex)
    {
        _uoW.Rollback();
        return Result.Fail<User>(new Error(ex.Message).CausedBy(ex));
    }
}
```

#### Result - Para operaciones sin valor de retorno

```csharp
public async Task<Result> ExecuteAsync(Command command, CancellationToken ct)
{
    try
    {
        await _uoW.Users.DeleteAsync(command.Id);
        _uoW.Commit();
        return Result.Ok();  // Operación exitosa sin retornar valor
    }
    catch (Exception ex)
    {
        _uoW.Rollback();
        return Result.Fail(new Error(ex.Message).CausedBy(ex));
    }
}
```

### Result.Ok() y Result.Fail()

```csharp
// Éxito con valor
Result<User> successResult = Result.Ok(user);

// Éxito sin valor
Result successResult = Result.Ok();

// Error con mensaje simple
Result<User> failResult = Result.Fail<User>("User not found");

// Error con objeto Error personalizado
Result<User> failResult = Result.Fail<User>(
    new Error("Validation failed")
        .WithMetadata("Field", "Email")
);
```

---

## Exception to Result Conversion

### Patrón Completo de Conversión

**Ejemplo real de `UpdateTechnicalStandardUseCase.cs`:**

```csharp
public async Task<Result<TechnicalStandard>> ExecuteAsync(Command request, CancellationToken ct)
{
    try
    {
        _uow.BeginTransaction();
        var updated = await _uow.TechnicalStandards.UpdateAsync(
            request.Id,
            request.Code,
            request.Name,
            request.Edition,
            request.Status,
            request.Type
        );
        _uow.Commit();
        return Result.Ok(updated);
    }
    catch (ResourceNotFoundException ex)
    {
        _uow.Rollback();
        return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
    }
    catch (InvalidDomainException ex)
    {
        _uow.Rollback();
        return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
    }
    catch (DuplicatedDomainException ex)
    {
        _uow.Rollback();
        return Result.Fail<TechnicalStandard>(new Error(ex.Message).CausedBy(ex));
    }
    catch (Exception ex)
    {
        _uow.Rollback();
        return Result.Fail<TechnicalStandard>(
            new Error("Internal server error: " + ex.Message).CausedBy(ex)
        );
    }
}
```

### Excepciones Específicas del Dominio

| Excepción | Cuándo usarla | Ejemplo |
|-----------|---------------|---------|
| `InvalidDomainException` | Validación de reglas de negocio fallida | Email inválido, precio negativo |
| `DuplicatedDomainException` | Violación de unicidad | Email duplicado, código existente |
| `ResourceNotFoundException` | Entidad no encontrada para actualizar/eliminar | Usuario no existe |
| `HttpRequestException` | Fallo en servicios externos | Auth0, APIs externas |
| `ArgumentException` | Parámetros inválidos | Parámetros nulos o fuera de rango |

### Template para Catch Blocks

```csharp
try
{
    _uoW.BeginTransaction();
    // Operación de dominio
    _uoW.Commit();
    return Result.Ok(entity);
}
catch (InvalidDomainException idex)
{
    _uoW.Rollback();
    // Deserializar errores de validación si aplica
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstError = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";
    return Result.Fail(new Error(firstError).CausedBy(idex).WithMetadata("ValidationErrors", errors));
}
catch (DuplicatedDomainException ddex)
{
    _uoW.Rollback();
    return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
}
catch (ResourceNotFoundException rnfex)
{
    _uoW.Rollback();
    return Result.Fail(new Error(rnfex.Message).CausedBy(rnfex));
}
catch (HttpRequestException httpEx)
{
    _uoW.Rollback();
    return Result.Fail(new Error($"External service error: {httpEx.Message}").CausedBy(httpEx));
}
catch (Exception ex)
{
    _uoW.Rollback();
    _logger.LogError(ex, "Unexpected error in {UseCase}", nameof(Handler));
    return Result.Fail(new Error("An unexpected error occurred").CausedBy(ex));
}
```

---

## Custom Error Classes

### Factory Pattern para Errores

**Ejemplo real de `UserErrors.cs`:**

```csharp
namespace hashira.stone.backend.domain.errors;

/// <summary>
/// Factory class for creating user-related errors.
/// </summary>
public static class UserErrors
{
    /// <summary>
    /// Creates an error indicating a user was not found.
    /// </summary>
    public static UserNotFoundError UserNotFound(string userName)
    {
        return new UserNotFoundError(userName);
    }
}

/// <summary>
/// Custom error for when a user is not found.
/// </summary>
public class UserNotFoundError : Error
{
    public UserNotFoundError(string userName)
        : base($"User '{userName}' not found.")
    {
    }
}
```

### Uso de Custom Errors

**Ejemplo real de `GetUserUseCase.cs`:**

```csharp
public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
{
    try
    {
        var user = await _uoW.Users.GetByEmailAsync(request.UserName);
        return user == null
            ? Result.Fail(UserErrors.UserNotFound(request.UserName))  // ✅ Error tipado
            : Result.Ok(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving user with username: {UserName}", request.UserName);
        return Result.Fail("Error retrieving user");
    }
}
```

### Template para Custom Error Class

```csharp
namespace {Project}.domain.errors;

/// <summary>
/// Factory class for {Entity}-related errors.
/// </summary>
public static class {Entity}Errors
{
    public static {Entity}NotFoundError NotFound(Guid id)
    {
        return new {Entity}NotFoundError(id);
    }

    public static {Entity}ValidationError ValidationFailed(string field, string message)
    {
        return new {Entity}ValidationError(field, message);
    }
}

/// <summary>
/// Error for when a {Entity} is not found.
/// </summary>
public class {Entity}NotFoundError : Error
{
    public {Entity}NotFoundError(Guid id)
        : base($"{Entity} with ID '{id}' not found.")
    {
        Metadata.Add("EntityId", id);
        Metadata.Add("EntityType", nameof({Entity}));
    }
}

/// <summary>
/// Error for {Entity} validation failures.
/// </summary>
public class {Entity}ValidationError : Error
{
    public {Entity}ValidationError(string field, string message)
        : base($"Validation failed for field '{field}': {message}")
    {
        Metadata.Add("Field", field);
        Metadata.Add("ValidationMessage", message);
    }
}
```

---

## Error Context with Metadata

### WithMetadata() para Contexto Rico

**Ejemplo real de `AbstractDomainObjectErrors.cs`:**

```csharp
public static Error WithValidationFailures(ValidationFailure validationFailure)
{
    return new Error(validationFailure.ErrorMessage)
        .WithMetadata("ErrorCode", "WithValidationFailures")
        .WithMetadata("PropertyName", validationFailure.PropertyName)
        .WithMetadata("AttemptedValue", validationFailure.AttemptedValue);
}
```

### CausedBy() para Exception Chaining

**Preservar la excepción original para debugging:**

```csharp
catch (InvalidDomainException idex)
{
    _uoW.Rollback();
    return Result.Fail(
        new Error("Failed to create user")
            .CausedBy(idex)  // ✅ Preserva stack trace y detalles
            .WithMetadata("Email", command.Email)
            .WithMetadata("Timestamp", DateTime.UtcNow)
    );
}
```

### Metadata Útil para Debugging

```csharp
return Result.Fail(
    new Error("Operation failed")
        .CausedBy(exception)
        .WithMetadata("UserId", userId)
        .WithMetadata("Operation", operationType)
        .WithMetadata("Timestamp", DateTime.UtcNow)
        .WithMetadata("CorrelationId", correlationId)
        .WithMetadata("ValidationErrors", validationErrors)  // Para errores de validación
);
```

---

## Transaction-Based Error Handling

### Patrón para Operaciones Transaccionales

**Todas las operaciones de escritura deben manejar transacciones:**

```csharp
public async Task<Result<Entity>> ExecuteAsync(Command command, CancellationToken ct)
{
    _uoW.BeginTransaction();  // ✅ Iniciar transacción SIEMPRE
    try
    {
        // Operaciones de escritura
        var entity = await _uoW.Entities.CreateAsync(...);

        // Más operaciones si es necesario
        await _uoW.RelatedEntities.UpdateAsync(...);

        _uoW.Commit();  // ✅ Commit solo si todo es exitoso
        return Result.Ok(entity);
    }
    catch (Exception ex)
    {
        _uoW.Rollback();  // ✅ Rollback en TODOS los catch blocks
        return Result.Fail(new Error(ex.Message).CausedBy(ex));
    }
}
```

### Patrón para Operaciones de Solo Lectura

**Operaciones GET simples no requieren transacciones:**

```csharp
public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
{
    try
    {
        // ✅ Sin BeginTransaction para lecturas simples
        var user = await _uoW.Users.GetByEmailAsync(request.UserName);
        return user == null
            ? Result.Fail(UserErrors.UserNotFound(request.UserName))
            : Result.Ok(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving user");
        return Result.Fail("Error retrieving user");
    }
}
```

### Reglas de Transacciones

| Operación | Requiere Transacción | Rollback | Ejemplo |
|-----------|---------------------|----------|---------|
| CREATE | ✅ Sí | En todos los catch | `CreateUserUseCase` |
| UPDATE | ✅ Sí | En todos los catch | `UpdateTechnicalStandardUseCase` |
| DELETE | ✅ Sí | En todos los catch | `DeletePrototypeUseCase` |
| GET (simple) | ❌ No | N/A | `GetUserUseCase` |
| GET (complejo con joins) | ⚠️ Opcional | Si se usa | `GetManyAndCountUsersUseCase` |

---

## Validation Error Handling

### Deserialización de Validation Errors

**Ejemplo real de `CreatePrototypeUseCase.cs`:**

```csharp
catch (InvalidDomainException idex)
{
    _uoW.Rollback();

    // ✅ Deserializar errores de validación de FluentValidation
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid prototype data";

    return Result.Fail(
        new Error(firstErrorMessage)
            .CausedBy(idex)
            .WithMetadata("ValidationErrors", errors)  // ✅ Incluir todos los errores
    );
}
```

### ValidationError Model

```csharp
namespace hashira.stone.backend.application.common;

/// <summary>
/// Represents a validation error from FluentValidation.
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
}
```

### Múltiples Errores de Validación

**Cuando hay múltiples errores, retornar el primero en el mensaje principal:**

```csharp
catch (InvalidDomainException idex)
{
    _uoW.Rollback();
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);

    // ✅ Mensaje principal: primer error
    var firstError = errors?.FirstOrDefault()?.ErrorMessage ?? "Validation failed";

    // ✅ Metadata: todos los errores para el frontend
    return Result.Fail(
        new Error(firstError)
            .CausedBy(idex)
            .WithMetadata("ValidationErrors", errors)
            .WithMetadata("ErrorCount", errors?.Count ?? 0)
    );
}
```

---

## Checklist

### ✅ Error Handling en Handlers

- [ ] Usar `Result<T>` como tipo de retorno en handlers
- [ ] Implementar try-catch comprehensivo
- [ ] Capturar excepciones específicas del dominio primero
- [ ] Usar `CausedBy()` para preservar exception original
- [ ] Agregar metadata relevante con `WithMetadata()`
- [ ] Hacer rollback de transacción en TODOS los catch blocks
- [ ] Loggear errores inesperados
- [ ] Retornar mensajes de error user-friendly
- [ ] Deserializar ValidationErrors cuando aplique
- [ ] Usar custom error classes cuando sea apropiado

### ✅ Custom Error Classes

- [ ] Crear factory class estática para errores de entidad
- [ ] Heredar de `FluentResults.Error`
- [ ] Incluir constructor con mensaje descriptivo
- [ ] Agregar metadata relevante en constructor
- [ ] Documentar con XML comments
- [ ] Nombrar errores descriptivamente (e.g., `UserNotFoundError`)

### ✅ Transaction Management

- [ ] Iniciar transacción para operaciones de escritura
- [ ] NO usar transacciones para lecturas simples
- [ ] Commit solo después de operaciones exitosas
- [ ] Rollback en TODOS los catch blocks
- [ ] Considerar transacciones para operaciones complejas de lectura

---

## Mejores Prácticas

### 1. Result Pattern es para Control de Flujo

```csharp
// ✅ CORRECTO: Result para errores esperados
var user = await _uoW.Users.GetByEmailAsync(email);
return user == null
    ? Result.Fail(UserErrors.UserNotFound(email))
    : Result.Ok(user);

// ❌ INCORRECTO: Excepción para control de flujo
if (user == null)
    throw new UserNotFoundException(email);
```

### 2. Excepciones son para Errores Inesperados

```csharp
// ✅ Capturar y convertir a Result
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return Result.Fail("An unexpected error occurred");
}
```

### 3. Siempre Hacer Rollback en Catch

```csharp
// ✅ CORRECTO: Rollback en catch
catch (InvalidDomainException ex)
{
    _uoW.Rollback();  // ✅ Crítico
    return Result.Fail(new Error(ex.Message).CausedBy(ex));
}

// ❌ INCORRECTO: Olvidar rollback
catch (InvalidDomainException ex)
{
    return Result.Fail(new Error(ex.Message));  // ❌ Transacción pendiente
}
```

### 4. Usar Custom Errors para Casos Comunes

```csharp
// ✅ CORRECTO: Error tipado y reutilizable
return Result.Fail(UserErrors.UserNotFound(userName));

// ❌ INCORRECTO: String literal repetido
return Result.Fail($"User '{userName}' not found.");
```

### 5. Preservar Exception Context

```csharp
// ✅ CORRECTO: CausedBy preserva stack trace
return Result.Fail(new Error("Operation failed").CausedBy(exception));

// ❌ INCORRECTO: Perder contexto
return Result.Fail(new Error("Operation failed"));
```

### 6. Metadata para Debugging

```csharp
// ✅ Metadata útil para troubleshooting
return Result.Fail(
    new Error("Validation failed")
        .WithMetadata("ValidationErrors", errors)
        .WithMetadata("UserId", userId)
        .WithMetadata("Timestamp", DateTime.UtcNow)
);
```

### 7. Mensajes User-Friendly

```csharp
// ✅ CORRECTO: Mensaje claro para el usuario
catch (Exception ex)
{
    _logger.LogError(ex, "Database connection failed");
    return Result.Fail("Unable to process your request. Please try again later.");
}

// ❌ INCORRECTO: Exponer detalles técnicos
catch (Exception ex)
{
    return Result.Fail($"SqlException: {ex.Message}");
}
```

### 8. Orden de Catch Blocks

```csharp
// ✅ CORRECTO: Más específico primero
catch (InvalidDomainException ex) { }
catch (DuplicatedDomainException ex) { }
catch (ResourceNotFoundException ex) { }
catch (Exception ex) { }  // Más genérico al final

// ❌ INCORRECTO: Genérico primero
catch (Exception ex) { }  // Captura todo
catch (InvalidDomainException ex) { }  // Nunca se ejecuta
```

---

## Recursos

### FluentResults

- **Documentación oficial**: [FluentResults GitHub](https://github.com/altmann/FluentResults)
- **Mejores prácticas**: Context7 documentation on Result patterns
- **Error classes**: Ver ejemplos en `domain/errors/`

### Proyecto de Referencia

- **Use cases reales**: `D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.application\usecases\`
- **Custom errors**: `D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\errors\`
- **Validation errors**: `hashira.stone.backend.application.common.ValidationError`

### Guías Relacionadas

- [Use Cases](./use-cases.md) - Estructura de handlers
- [Command/Handler Patterns](./command-handler-patterns.md) - Patrones de implementación
- [Domain Exceptions](../domain-layer/exceptions.md) - Excepciones del dominio

---

**Siguiente**: [Common Utilities](./common-utilities.md)
**Anterior**: [Command/Handler Patterns](./command-handler-patterns.md)
**Índice**: [Application Layer](./README.md)
