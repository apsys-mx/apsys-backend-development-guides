# Common Utilities en Application Layer

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

Esta guía documenta las utilidades comunes, modelos auxiliares y patrones reutilizables en la capa de aplicación. Incluye interfaces de FastEndpoints, modelos de validación, patrones de dependency injection y helpers que facilitan el desarrollo consistente de use cases.

## Índice

- [ValidationError Model](#validationerror-model)
- [FastEndpoints Interfaces](#fastendpoints-interfaces)
- [Dependency Injection Patterns](#dependency-injection-patterns)
- [Async Patterns](#async-patterns)
- [Logging Patterns](#logging-patterns)
- [Checklist](#checklist)
- [Mejores Prácticas](#mejores-prácticas)
- [Recursos](#recursos)

---

## ValidationError Model

### Definición

**Clase auxiliar de `hashira.stone.backend.application.common.ValidationError`:**

```csharp
namespace hashira.stone.backend.application.common;

/// <summary>
/// Helper class for validation error structure.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name that caused the error.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
}
```

### Propósito

Esta clase se usa para deserializar errores de validación provenientes de **FluentValidation** en el Domain Layer, permitiendo:

1. **Estructurar errores de validación** de manera consistente
2. **Deserializar JSON** de `InvalidDomainException.Message`
3. **Extraer información específica** sobre qué propiedad falló
4. **Pasar contexto al frontend** sobre errores de validación

### Uso en Use Cases

**Ejemplo real de `CreatePrototypeUseCase.cs`:**

```csharp
catch (InvalidDomainException idex)
{
    _uoW.Rollback();

    // ✅ Deserializar mensaje JSON a lista de ValidationError
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid prototype data";

    return Result.Fail(
        new Error(firstErrorMessage)
            .CausedBy(idex)
            .WithMetadata("ValidationErrors", errors)  // ✅ Metadata con lista completa
    );
}
```

### Template de Uso

```csharp
using System.Text.Json;
using hashira.stone.backend.application.common;

catch (InvalidDomainException idex)
{
    _uoW.Rollback();

    // Deserializar errores de validación
    var validationErrors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);

    // Mensaje principal: primer error o mensaje genérico
    var primaryMessage = validationErrors?.FirstOrDefault()?.ErrorMessage ?? "Validation failed";

    // Result con metadata completa
    return Result.Fail(
        new Error(primaryMessage)
            .CausedBy(idex)
            .WithMetadata("ValidationErrors", validationErrors)
            .WithMetadata("ErrorCount", validationErrors?.Count ?? 0)
    );
}
```

### JSON Structure Example

**Cuando `InvalidDomainException` serializa múltiples errores:**

```json
[
  {
    "ErrorMessage": "Email must be a valid email address",
    "ErrorCode": "EmailValidator",
    "PropertyName": "Email"
  },
  {
    "ErrorMessage": "Name cannot be empty",
    "ErrorCode": "NotEmptyValidator",
    "PropertyName": "Name"
  }
]
```

---

## FastEndpoints Interfaces

### ICommand&lt;TResult&gt;

**Interface para comandos que retornan un resultado:**

```csharp
using FastEndpoints;
using FluentResults;

public class Command : ICommand<Result<User>>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
```

**Cuándo usar:**
- ✅ Use cases que retornan una entidad (Create, Get, Update)
- ✅ Use cases que retornan un resultado complejo (GetManyAndCount)
- ✅ Cualquier operación que necesite devolver datos

### ICommand (Void)

**Interface para comandos sin valor de retorno:**

```csharp
using FastEndpoints;
using FluentResults;

public class Command : ICommand<Result>
{
    public Guid Id { get; set; }
}
```

**Cuándo usar:**
- ✅ Use cases de Delete
- ✅ Operaciones que no retornan datos (solo éxito/fallo)
- ✅ Comandos de actualización que no necesitan devolver la entidad actualizada

### ICommandHandler&lt;TCommand, TResult&gt;

**Interface para handlers que retornan un resultado:**

```csharp
using FastEndpoints;
using FluentResults;

public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        // Implementation
    }
}
```

### ICommandHandler&lt;TCommand&gt;

**Interface para handlers sin valor de retorno:**

```csharp
using FastEndpoints;
using FluentResults;

public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result> ExecuteAsync(Command command, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Tabla de Referencia

| Operación | ICommand | ICommandHandler | Retorna |
|-----------|----------|-----------------|---------|
| CREATE | `ICommand<Result<Entity>>` | `ICommandHandler<Command, Result<Entity>>` | Entidad creada |
| GET | `ICommand<Result<Entity>>` | `ICommandHandler<Command, Result<Entity>>` | Entidad encontrada |
| GET MANY | `ICommand<Result<(List<Entity>, int)>>` | `ICommandHandler<Command, Result<(List<Entity>, int)>>` | Lista + count |
| UPDATE | `ICommand<Result<Entity>>` | `ICommandHandler<Command, Result<Entity>>` | Entidad actualizada |
| DELETE | `ICommand<Result>` | `ICommandHandler<Command, Result>` | Solo éxito/fallo |

---

## Dependency Injection Patterns

### Constructor Injection (Recomendado)

**Usar Primary Constructor (C# 13) para inyectar dependencias:**

```csharp
public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _logger.LogInformation("Executing CreateUser for {Email}", command.Email);
        // Implementation
    }
}
```

### Dependencias Comunes

#### 1. IUnitOfWork (Obligatoria para operaciones de datos)

```csharp
public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<Entity>>
{
    private readonly IUnitOfWork _uoW = uoW;

    public async Task<Result<Entity>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _uoW.BeginTransaction();
        try
        {
            var entity = await _uoW.Entities.CreateAsync(...);
            _uoW.Commit();
            return Result.Ok(entity);
        }
        catch (Exception ex)
        {
            _uoW.Rollback();
            return Result.Fail(new Error(ex.Message).CausedBy(ex));
        }
    }
}
```

#### 2. ILogger (Recomendada para debugging)

```csharp
public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<Entity>>
{
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<Entity>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _logger.LogInformation("Starting operation for {Id}", command.Id);

        try
        {
            // Implementation
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed for {Id}", command.Id);
        }
    }
}
```

#### 3. External Services (Auth0, Email, etc.)

```csharp
public class Handler(
    IUnitOfWork uoW,
    IIdentityService identityService,
    IEmailService emailService,
    ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        // Usar servicios externos
        var auth0User = _identityService.Create(command.Email, command.Name, password);
        await _emailService.SendWelcomeEmail(command.Email);

        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
        return Result.Ok(user);
    }
}
```

### Property Injection (Alternativa de FastEndpoints)

**Solo usar si constructor injection no es posible:**

```csharp
public class Handler : ICommandHandler<Command, Result<User>>
{
    public IUnitOfWork UoW { get; set; } = default!;
    public ILogger<Handler> Logger { get; set; } = default!;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        Logger.LogInformation("Executing command");
        var user = await UoW.Users.GetByEmailAsync(command.Email);
        return Result.Ok(user);
    }
}
```

### Manual Resolution (Evitar cuando sea posible)

```csharp
// ❌ NO RECOMENDADO: Manual resolution
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    var service = Resolve<ISomeService>();  // Evitar
    // Implementation
}

// ✅ RECOMENDADO: Constructor injection
public class Handler(ISomeService service) : ICommandHandler<Command, Result<User>>
{
    private readonly ISomeService _service = service;
}
```

---

## Async Patterns

### ExecuteAsync Signature

**Todos los handlers deben implementar ExecuteAsync:**

```csharp
public async Task<Result<T>> ExecuteAsync(Command command, CancellationToken ct)
{
    // Implementation
}
```

### CancellationToken Usage

**Siempre pasar CancellationToken a operaciones async:**

```csharp
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    // ✅ CORRECTO: Pasar ct a operaciones async
    var user = await _uoW.Users.GetByEmailAsync(command.Email, ct);
    var result = await _externalService.CallApiAsync(data, ct);

    return Result.Ok(user);
}
```

**Cuándo ignorar CancellationToken:**

```csharp
// ⚠️ ACEPTABLE: Operación rápida sin soporte para ct
var entity = await _uoW.Entities.CreateAsync(command.Name);  // Sin ct

// ✅ MEJOR: Si el método lo soporta, siempre usarlo
var entity = await _uoW.Entities.CreateAsync(command.Name, ct);
```

### Task.FromResult para Operaciones Sincrónicas

```csharp
public async Task<Result<string>> ExecuteAsync(Command command, CancellationToken ct)
{
    // Operación sincrónica simple
    var result = command.FirstName + " " + command.LastName;

    // ✅ Retornar Task completado
    return await Task.FromResult(Result.Ok(result));
}
```

### ConfigureAwait en Application Layer

**NO usar ConfigureAwait(false) en Application Layer:**

```csharp
// ❌ INCORRECTO: ConfigureAwait en Application Layer
var user = await _uoW.Users.GetByEmailAsync(email).ConfigureAwait(false);

// ✅ CORRECTO: Usar await normal
var user = await _uoW.Users.GetByEmailAsync(email);
```

**Razón:** El contexto de sincronización es importante para DI y scoped services.

---

## Logging Patterns

### Log Levels en Use Cases

| Level | Cuándo usar | Ejemplo |
|-------|-------------|---------|
| `LogInformation` | Operaciones normales, flujo exitoso | "User created successfully" |
| `LogWarning` | Condiciones inusuales pero manejables | "User not found, returning empty result" |
| `LogError` | Excepciones capturadas, errores | "Failed to create user" |
| `LogDebug` | Información de debugging detallada | "Processing command with Id: {id}" |
| `LogTrace` | Información muy detallada | "Method entered with parameters: {params}" |

### Structured Logging

**Usar placeholders en lugar de interpolación:**

```csharp
// ✅ CORRECTO: Structured logging
_logger.LogInformation("Creating user with email {Email}", command.Email);
_logger.LogError(ex, "Failed to create user with email {Email}", command.Email);

// ❌ INCORRECTO: String interpolation
_logger.LogInformation($"Creating user with email {command.Email}");
```

### Logging en Try-Catch

**Ejemplo completo de `GetUserUseCase.cs`:**

```csharp
public async Task<Result<User>> ExecuteAsync(Command request, CancellationToken ct)
{
    try
    {
        var user = await _uoW.Users.GetByEmailAsync(request.UserName);
        return user == null
            ? Result.Fail(UserErrors.UserNotFound(request.UserName))
            : Result.Ok(user);
    }
    catch (Exception ex)
    {
        // ✅ Log error con contexto
        _logger.LogError(ex, "Error retrieving user with username: {UserName}", request.UserName);
        return Result.Fail("Error retrieving user");
    }
}
```

### Template de Logging Pattern

```csharp
public async Task<Result<Entity>> ExecuteAsync(Command command, CancellationToken ct)
{
    _logger.LogInformation("Starting {Operation} for {EntityType}", nameof(ExecuteAsync), nameof(Entity));

    _uoW.BeginTransaction();
    try
    {
        var entity = await _uoW.Entities.CreateAsync(command.Name);
        _uoW.Commit();

        _logger.LogInformation("Successfully created {EntityType} with Id {EntityId}", nameof(Entity), entity.Id);
        return Result.Ok(entity);
    }
    catch (InvalidDomainException idex)
    {
        _uoW.Rollback();
        _logger.LogWarning("Validation failed for {EntityType}: {Message}", nameof(Entity), idex.Message);
        return Result.Fail(new Error(idex.Message).CausedBy(idex));
    }
    catch (Exception ex)
    {
        _uoW.Rollback();
        _logger.LogError(ex, "Unexpected error creating {EntityType}", nameof(Entity));
        return Result.Fail(new Error("An unexpected error occurred").CausedBy(ex));
    }
}
```

---

## Checklist

### ✅ ValidationError Usage

- [ ] Importar `using System.Text.Json;`
- [ ] Importar `using hashira.stone.backend.application.common;`
- [ ] Deserializar con `JsonSerializer.Deserialize<List<ValidationError>>()`
- [ ] Extraer primer error para mensaje principal
- [ ] Incluir lista completa en metadata con `WithMetadata("ValidationErrors", errors)`
- [ ] Manejar caso null con operador `??`

### ✅ FastEndpoints Interfaces

- [ ] Usar `ICommand<Result<T>>` para operaciones que retornan datos
- [ ] Usar `ICommand<Result>` para operaciones sin retorno de datos
- [ ] Implementar `ICommandHandler<TCommand, TResult>` correctamente
- [ ] Definir Command y Handler dentro de clase abstracta del UseCase
- [ ] Importar `using FastEndpoints;` y `using FluentResults;`

### ✅ Dependency Injection

- [ ] Usar Primary Constructor (C# 13) para inyección
- [ ] Inyectar `IUnitOfWork` para operaciones de datos
- [ ] Inyectar `ILogger<Handler>` para logging
- [ ] Inyectar servicios externos cuando sea necesario
- [ ] Asignar parámetros a campos readonly
- [ ] Evitar manual resolution con `Resolve<T>()`

### ✅ Async Patterns

- [ ] Implementar `ExecuteAsync` con signature correcta
- [ ] Pasar `CancellationToken` a operaciones async
- [ ] Usar `await` para todas las operaciones asíncronas
- [ ] NO usar `ConfigureAwait(false)` en Application Layer
- [ ] Usar `Task.FromResult()` para operaciones sincrónicas si es necesario

### ✅ Logging

- [ ] Usar structured logging con placeholders `{Variable}`
- [ ] NO usar string interpolation `$"..."` en logs
- [ ] Log Information para flujo normal
- [ ] Log Warning para condiciones inusuales
- [ ] Log Error para excepciones con contexto
- [ ] Incluir variables relevantes en logs

---

## Mejores Prácticas

### 1. ValidationError: Siempre Manejar Null

```csharp
// ✅ CORRECTO: Manejar null con ??
var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
var firstError = errors?.FirstOrDefault()?.ErrorMessage ?? "Validation failed";

// ❌ INCORRECTO: Puede causar NullReferenceException
var firstError = errors.FirstOrDefault().ErrorMessage;
```

### 2. FastEndpoints: Estructura Consistente

```csharp
// ✅ CORRECTO: Command y Handler en clase abstracta
public abstract class CreateUserUseCase
{
    public class Command : ICommand<Result<User>> { }
    public class Handler : ICommandHandler<Command, Result<User>> { }
}

// ❌ INCORRECTO: Clases separadas
public class CreateUserCommand : ICommand<Result<User>> { }
public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<User>> { }
```

### 3. Dependency Injection: Primary Constructor

```csharp
// ✅ CORRECTO: Primary constructor (C# 13)
public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly ILogger<Handler> _logger = logger;
}

// ❌ INCORRECTO: Constructor tradicional (más verboso)
public class Handler : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW;
    private readonly ILogger<Handler> _logger;

    public Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    {
        _uoW = uoW;
        _logger = logger;
    }
}
```

### 4. CancellationToken: Siempre Propagar

```csharp
// ✅ CORRECTO: Pasar ct a métodos que lo soporten
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    var user = await _uoW.Users.GetByEmailAsync(command.Email, ct);
    var data = await _externalService.FetchDataAsync(user.Id, ct);
    return Result.Ok(user);
}

// ❌ INCORRECTO: Ignorar ct disponible
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    var user = await _uoW.Users.GetByEmailAsync(command.Email);  // Sin ct
    return Result.Ok(user);
}
```

### 5. Logging: Structured sobre Interpolation

```csharp
// ✅ CORRECTO: Structured logging
_logger.LogInformation("User {UserId} performed action {Action}", userId, actionName);
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);

// ❌ INCORRECTO: String interpolation
_logger.LogInformation($"User {userId} performed action {actionName}");
_logger.LogError(ex, $"Failed to process order {orderId}");
```

### 6. Readonly Fields: Siempre Asignar en Constructor

```csharp
// ✅ CORRECTO: Asignar a readonly fields
public class Handler(IUnitOfWork uoW)
{
    private readonly IUnitOfWork _uoW = uoW;  // ✅
}

// ❌ INCORRECTO: No asignar
public class Handler(IUnitOfWork uoW)
{
    // ❌ uoW no está guardado en un field
}
```

### 7. Error Messages: User-Friendly

```csharp
// ✅ CORRECTO: Mensaje claro para el usuario
var firstError = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid user data";
return Result.Fail(new Error(firstError));

// ❌ INCORRECTO: Mensaje técnico
return Result.Fail(new Error("InvalidDomainException thrown in CreateAsync"));
```

### 8. Namespace Imports: Organizar Correctamente

```csharp
// ✅ CORRECTO: Imports organizados
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.application.common;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using System.Text.Json;
```

---

## Recursos

### FastEndpoints

- **Documentación oficial**: [FastEndpoints Documentation](https://fast-endpoints.com/)
- **Command Bus**: [Command Bus Guide](https://fast-endpoints.com/docs/command-bus)
- **Dependency Injection**: [DI Patterns](https://fast-endpoints.com/docs/dependency-injection)

### FluentResults

- **GitHub**: [FluentResults Repository](https://github.com/altmann/FluentResults)
- **Result Pattern**: Context7 FluentResults documentation

### Proyecto de Referencia

- **ValidationError**: `D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.application\common\ValidationError.cs`
- **Use cases**: `D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.application\usecases\`
- **Ejemplos reales**: CreateUserUseCase, CreatePrototypeUseCase, GetUserUseCase

### Guías Relacionadas

- [Use Cases](./use-cases.md) - Estructura de use cases
- [Command/Handler Patterns](./command-handler-patterns.md) - Patrones de implementación
- [Error Handling](./error-handling.md) - Manejo de errores y Result pattern

---

**Anterior**: [Error Handling](./error-handling.md)
**Índice**: [Application Layer](./README.md)
