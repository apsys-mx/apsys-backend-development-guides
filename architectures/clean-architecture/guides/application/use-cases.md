# Use Cases - Application Layer

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

Un **Use Case** (caso de uso) representa una operación específica de negocio que la aplicación puede realizar. En Clean Architecture, los use cases son la esencia de la Application Layer, encapsulando la lógica de orquestación necesaria para completar una acción del usuario o del sistema.

## Qué es un Use Case

Un Use Case es:

- ✅ **Una operación de negocio específica** (crear usuario, obtener producto, actualizar orden)
- ✅ **Orquestador de flujo** entre Domain, Infrastructure y servicios externos
- ✅ **Punto de entrada** para operaciones desde WebApi
- ✅ **Transaccional** cuando modifica datos
- ✅ **Independiente** de otros use cases

Un Use Case NO es:

- ❌ Lógica de negocio (esa va en Domain)
- ❌ Acceso directo a base de datos (usa repositorios)
- ❌ Lógica de presentación (esa va en WebApi)
- ❌ Un contenedor de múltiples operaciones
- ❌ Un endpoint HTTP (aunque se mapea 1:1 con endpoints)

## Command/Handler Pattern con FastEndpoints

En APSYS, los use cases siguen el patrón **Command/Handler** de FastEndpoints:

### Componentes

1. **Command**: Define QUÉ hacer (datos de entrada)
2. **Handler**: Define CÓMO hacerlo (lógica de ejecución)

### Diagrama del Patrón

```
┌─────────────────────────────────────────┐
│           Use Case Class                │
│         (Abstract/Sealed)               │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │          Command                  │ │
│  │  implements ICommand<TResult>     │ │
│  │  ─────────────────────────────    │ │
│  │  + Properties (data)              │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │          Handler                  │ │
│  │  implements ICommandHandler       │ │
│  │  ─────────────────────────────    │ │
│  │  - Dependencies (injected)        │ │
│  │  + ExecuteAsync(Command, CT)      │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

## Estructura de un Use Case

### Template Básico

```csharp
using FastEndpoints;
using FluentResults;
using {project}.domain.entities;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace {project}.application.usecases.{feature};

/// <summary>
/// Use case for {operation description}.
/// </summary>
public abstract class {Operation}{Entity}UseCase
{
    /// <summary>
    /// Command to {operation description}.
    /// </summary>
    public class Command : ICommand<{ResultType}>
    {
        // Properties representing input data
        public {Type} {Property} { get; set; } = {default};
    }

    /// <summary>
    /// Handler for executing the {operation} command.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, {ResultType}>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to {operation description}.
        /// </summary>
        public async Task<{ResultType}> ExecuteAsync(Command command, CancellationToken ct)
        {
            // Implementation
        }
    }
}
```

### Ejemplo Real: CreateUserUseCase

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.application.common;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.domain.interfaces.services;
using System.Security.Cryptography;
using System.Text.Json;

namespace hashira.stone.backend.application.usecases.users;

/// <summary>
/// Use case for creating a new user in the system.
/// </summary>
public abstract class CreateUserUseCase
{
    /// <summary>
    /// Command to create a new user.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        /// <summary>
        /// Gets or sets the email address for the new user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the new user.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for creating a new user.
    /// </summary>
    public class Handler(IUnitOfWork uoW, IIdentityService identityService)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IIdentityService _identityService = identityService;

        /// <summary>
        /// Executes the command to create a new user.
        /// </summary>
        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // Create user in identity service (Auth0)
                var password = GenerateRandomPassword();
                var auth0User = _identityService.Create(command.Email, command.Name, password);

                // Create user in domain
                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

                _uoW.Commit();
                return Result.Ok(user);
            }
            catch (HttpRequestException httpEx)
            {
                _uoW.Rollback();
                return Result.Fail(
                    new Error($"Error creating user {command.Email} on authentication service")
                        .CausedBy(httpEx)
                );
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage
                    ?? "Invalid user data";
                return Result.Fail(
                    new Error(firstErrorMessage)
                        .CausedBy(idex)
                        .WithMetadata("ValidationErrors", idex)
                );
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

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        const int length = 12;
        var passwordChars = new char[length];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];

        rng.GetBytes(bytes);

        for (int i = 0; i < length; i++)
        {
            passwordChars[i] = chars[bytes[i] % chars.Length];
        }

        return new string(passwordChars);
    }
}
```

### Ejemplo Real: GetUserUseCase

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.users;

/// <summary>
/// Use case for retrieving a user by their username.
/// </summary>
public class GetUserUseCase
{
    /// <summary>
    /// Command to get a user by their username.
    /// </summary>
    public class Command : ICommand<Result<User>>
    {
        /// <summary>
        /// Gets or sets the username of the user to retrieve.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for executing the GetUser command.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        /// <summary>
        /// Executes the command to get a user by their username.
        /// </summary>
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
                _logger.LogError(ex, "Error retrieving user: {UserName}", request.UserName);
                return Result.Fail("Error retrieving user");
            }
        }
    }
}
```

---

## ICommand Interface

La interfaz `ICommand<TResult>` de FastEndpoints marca una clase como un comando ejecutable.

### Firma

```csharp
public interface ICommand<TResult>
{
}
```

### Características

- **Marker interface**: No tiene métodos, solo marca el tipo
- **Generic**: Define el tipo de resultado esperado
- **Propiedades**: Contiene las propiedades de entrada del comando
- **Sin lógica**: Solo es un contenedor de datos

### Tipos de Resultado Comunes

```csharp
// Comando que retorna una entidad dentro de un Result
public class Command : ICommand<Result<User>> { }

// Comando que retorna un Result sin valor
public class Command : ICommand<Result> { }

// Comando que retorna una lista con conteo
public class Command : ICommand<GetManyAndCountResult<User>> { }

// Comando que retorna un tipo primitivo
public class Command : ICommand<bool> { }
```

### Ejemplo de Command

```csharp
/// <summary>
/// Command to update a technical standard.
/// </summary>
public class Command : ICommand<Result<TechnicalStandard>>
{
    /// <summary>
    /// Gets or sets the ID of the technical standard to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edition.
    /// </summary>
    public string Edition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
```

---

## ICommandHandler Interface

La interfaz `ICommandHandler<TCommand, TResult>` define el handler que ejecuta un comando.

### Firma

```csharp
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> ExecuteAsync(TCommand command, CancellationToken ct);
}
```

### Características

- **Generic**: Vincula un comando específico con su resultado
- **ExecuteAsync**: Método único que ejecuta el comando
- **CancellationToken**: Soporte para cancelación de operaciones async
- **Dependencies**: Se inyectan por constructor

### Primary Constructor Pattern (C# 13)

APSYS usa **primary constructors** para inyección de dependencias:

```csharp
// ✅ Patrón actual (C# 13)
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

// ❌ Patrón antiguo (C# 12 y anteriores)
public class Handler : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW;
    private readonly ILogger<Handler> _logger;

    public Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    {
        _uoW = uoW;
        _logger = logger;
    }

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        // Implementation
    }
}
```

**Beneficios del Primary Constructor:**
- Menos código boilerplate
- Más legible
- Menos propenso a errores
- Patrón recomendado en C# 13

### Ejemplo de Handler Completo

```csharp
public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<Prototype>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<Prototype>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _uoW.BeginTransaction();
        try
        {
            _logger.LogInformation("Creating prototype: {Number}", command.Number);

            var prototype = await _uoW.Prototypes.CreateAsync(
                command.Number,
                command.IssueDate,
                command.ExpirationDate,
                command.Status
            );

            _uoW.Commit();
            _logger.LogInformation("Prototype created: {Id}", prototype.Id);
            return Result.Ok(prototype);
        }
        catch (InvalidDomainException idex)
        {
            _uoW.Rollback();
            var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
            var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage
                ?? "Invalid prototype data";
            return Result.Fail(
                new Error(firstErrorMessage)
                    .CausedBy(idex)
                    .WithMetadata("ValidationErrors", idex)
            );
        }
        catch (DuplicatedDomainException ddex)
        {
            _uoW.Rollback();
            _logger.LogWarning("Duplicate prototype: {Number}", command.Number);
            return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
        }
        catch (Exception ex)
        {
            _uoW.Rollback();
            _logger.LogError(ex, "Error creating prototype");
            return Result.Fail(new Error(ex.Message).CausedBy(ex));
        }
    }
}
```

---

## Naming Conventions

### Archivo

```
{Operation}{Entity}UseCase.cs
```

**Ejemplos:**
- `CreateUserUseCase.cs`
- `GetUserUseCase.cs`
- `UpdateUserUseCase.cs`
- `DeleteUserUseCase.cs`
- `GetManyAndCountUsersUseCase.cs`
- `AddUsersToRoleUseCase.cs`

### Clase Principal

```csharp
public abstract class {Operation}{Entity}UseCase
```

**Abstract vs Sealed:**
- ✅ **Abstract**: Cuando contiene métodos helper privados/estáticos
- ✅ **Sealed**: Cuando solo contiene Command + Handler (sin helpers)

**Ejemplos:**
```csharp
public abstract class CreateUserUseCase { } // Tiene GenerateRandomPassword helper
public class GetUserUseCase { }             // Solo Command + Handler
```

### Command Class

```csharp
public class Command : ICommand<{ResultType}>
```

**Siempre se llama `Command`**, no `CreateUserCommand` ni `CreateCommand`.

### Handler Class

```csharp
public class Handler(dependencies) : ICommandHandler<Command, {ResultType}>
```

**Siempre se llama `Handler`**, not `CreateUserHandler` ni `CreateHandler`.

### Namespace

```csharp
namespace {project}.application.usecases.{feature};
```

**Ejemplos:**
- `hashira.stone.backend.application.usecases.users`
- `hashira.stone.backend.application.usecases.prototypes`
- `hashira.stone.backend.application.usecases.technicalstandards`

---

## Dependencias Comunes

### IUnitOfWork (Siempre)

**Propósito:** Acceso a repositorios y manejo de transacciones.

```csharp
public class Handler(IUnitOfWork uoW, ...)
    : ICommandHandler<Command, Result>
{
    private readonly IUnitOfWork _uoW = uoW;

    public async Task<Result> ExecuteAsync(Command command, CancellationToken ct)
    {
        _uoW.BeginTransaction();
        try
        {
            await _uoW.Users.CreateAsync(...);
            _uoW.Commit();
            return Result.Ok();
        }
        catch
        {
            _uoW.Rollback();
            throw;
        }
    }
}
```

### ILogger<Handler> (Recomendado)

**Propósito:** Logging de operaciones y errores.

```csharp
public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _logger.LogInformation("Creating user: {Email}", command.Email);

        try
        {
            // ...
            _logger.LogInformation("User created: {Id}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            throw;
        }
    }
}
```

### Servicios Externos (Cuando aplique)

**Ejemplos:**
- `IIdentityService` - Auth0, JWT, etc.
- `IEmailService` - Envío de emails
- `IStorageService` - Almacenamiento de archivos
- `INotificationService` - Notificaciones push

```csharp
public class Handler(
    IUnitOfWork uoW,
    IIdentityService identityService,
    ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IIdentityService _identityService = identityService;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        // Create user in Auth0
        var auth0User = _identityService.Create(
            command.Email,
            command.Name,
            password
        );

        // Create user in domain
        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

        return Result.Ok(user);
    }
}
```

---

## Ciclo de Vida de un Use Case

### 1. Invocación desde Endpoint

```csharp
// WebApi Endpoint
public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
{
    var command = new CreateUserUseCase.Command
    {
        Email = req.Email,
        Name = req.Name
    };

    var result = await command.ExecuteAsync(ct);
    // ...
}
```

### 2. Ejecución del Handler

```
1. FastEndpoints resuelve ICommandHandler<Command, Result>
2. Inyecta dependencias (IUnitOfWork, ILogger, etc.)
3. Llama ExecuteAsync(command, ct)
4. Handler orquesta la operación
5. Retorna Result<T>
```

### 3. Flujo Interno del Handler

```
┌─────────────────────────────────────────┐
│  1. BeginTransaction()                  │
├─────────────────────────────────────────┤
│  2. Logging (inicio operación)          │
├─────────────────────────────────────────┤
│  3. Validaciones (si aplica)            │
├─────────────────────────────────────────┤
│  4. Llamadas a servicios externos       │
├─────────────────────────────────────────┤
│  5. Operaciones de dominio/repositorios │
├─────────────────────────────────────────┤
│  6. Commit() si éxito                   │
│     Rollback() si error                 │
├─────────────────────────────────────────┤
│  7. Logging (fin operación)             │
├─────────────────────────────────────────┤
│  8. Return Result.Ok() o Result.Fail()  │
└─────────────────────────────────────────┘
```

---

## Tipos de Use Cases

### Create Use Case

**Características:**
- Retorna `Result<Entity>`
- Usa transacciones
- Maneja validaciones
- Puede llamar servicios externos

**Patrón:**
```csharp
public abstract class Create{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        // Properties for creation
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                var entity = await _uoW.{Entities}.CreateAsync(...);
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
}
```

### Get Use Case

**Características:**
- Retorna `Result<Entity>`
- NO usa transacciones (solo lectura)
- Retorna error si no encuentra

**Patrón:**
```csharp
public class Get{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        public Guid Id { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                var entity = await _uoW.{Entities}.GetByIdAsync(command.Id, ct);
                return entity == null
                    ? Result.Fail({Entity}Errors.NotFound(command.Id))
                    : Result.Ok(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {entity}", command.Id);
                return Result.Fail("Error retrieving {entity}");
            }
        }
    }
}
```

### GetManyAndCount Use Case

**Características:**
- Retorna `GetManyAndCountResult<Entity>`
- Usa transacciones
- Soporta paginación y filtrado

**Patrón:**
```csharp
public abstract class GetManyAndCount{Entities}UseCase
{
    public class Command : ICommand<GetManyAndCountResult<{Entity}>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<{Entity}>>
    {
        public async Task<GetManyAndCountResult<{Entity}>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                var results = await _uoW.{Entities}.GetManyAndCountAsync(
                    command.Query,
                    nameof({Entity}.{SortField}),
                    ct
                );
                _uoW.Commit();
                return results;
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

### Update Use Case

**Características:**
- Retorna `Result<Entity>`
- Usa transacciones
- Maneja validaciones y excepciones

**Patrón:**
```csharp
public class Update{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        public Guid Id { get; set; }
        // Update properties
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                var updated = await _uoW.{Entities}.UpdateAsync(...);
                _uoW.Commit();
                return Result.Ok(updated);
            }
            catch (ResourceNotFoundException ex)
            {
                _uoW.Rollback();
                return Result.Fail<{Entity}>(new Error(ex.Message).CausedBy(ex));
            }
            catch (InvalidDomainException ex)
            {
                _uoW.Rollback();
                return Result.Fail<{Entity}>(new Error(ex.Message).CausedBy(ex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail<{Entity}>(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

---

## Mejores Prácticas

### ✅ DO

1. **Una operación por Use Case**
   ```csharp
   // ✅ Correcto
   public abstract class CreateUserUseCase { }
   public abstract class UpdateUserUseCase { }

   // ❌ Incorrecto
   public abstract class UserCrudUseCase { } // Múltiples operaciones
   ```

2. **Usar transacciones para escritura**
   ```csharp
   // ✅ Correcto - Create/Update/Delete
   _uoW.BeginTransaction();
   try {
       // operación
       _uoW.Commit();
   }
   catch {
       _uoW.Rollback();
   }
   ```

3. **NO usar transacciones para lectura simple**
   ```csharp
   // ✅ Correcto - Get simple
   var user = await _uoW.Users.GetByIdAsync(id, ct);
   return user == null ? Result.Fail(...) : Result.Ok(user);

   // ❌ Incorrecto - No necesita transacción
   _uoW.BeginTransaction();
   var user = await _uoW.Users.GetByIdAsync(id, ct);
   _uoW.Commit();
   ```

4. **Logging apropiado**
   ```csharp
   // ✅ Correcto
   _logger.LogInformation("Creating user: {Email}", command.Email);
   _logger.LogError(ex, "Error creating user");

   // ❌ Incorrecto - Demasiado verbose
   _logger.LogInformation("Starting CreateUserUseCase");
   _logger.LogInformation("Validating email");
   _logger.LogInformation("Checking duplicates");
   ```

5. **XML Comments completos**
   ```csharp
   /// <summary>
   /// Command to create a new user.
   /// </summary>
   public class Command : ICommand<Result<User>>
   {
       /// <summary>
       /// Gets or sets the email address for the new user.
       /// </summary>
       public string Email { get; set; } = string.Empty;
   }
   ```

6. **Primary Constructors (C# 13)**
   ```csharp
   // ✅ Correcto
   public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
       : ICommandHandler<Command, Result<User>>

   // ❌ Anticuado (pero funciona)
   public class Handler : ICommandHandler<Command, Result<User>>
   {
       public Handler(IUnitOfWork uoW, ILogger<Handler> logger) { }
   }
   ```

### ❌ DON'T

1. **No retornar entidades directamente**
   ```csharp
   // ❌ Incorrecto
   public class Command : ICommand<User> { }

   // ✅ Correcto
   public class Command : ICommand<Result<User>> { }
   ```

2. **No lanzar excepciones sin convertir a Result**
   ```csharp
   // ❌ Incorrecto
   if (user == null)
       throw new NotFoundException("User not found");

   // ✅ Correcto
   if (user == null)
       return Result.Fail(UserErrors.UserNotFound(id));
   ```

3. **No hacer lógica de negocio en el handler**
   ```csharp
   // ❌ Incorrecto - Lógica de negocio en handler
   if (user.Email.Length < 5)
       return Result.Fail("Email too short");

   // ✅ Correcto - Lógica en Domain (UserValidator)
   var user = await _uoW.Users.CreateAsync(...); // CreateAsync valida
   ```

4. **No olvidar CancellationToken**
   ```csharp
   // ❌ Incorrecto
   var user = await _uoW.Users.GetByIdAsync(id);

   // ✅ Correcto
   var user = await _uoW.Users.GetByIdAsync(id, ct);
   ```

5. **No mezclar operaciones en un solo Use Case**
   ```csharp
   // ❌ Incorrecto
   public class CreateAndSendEmailUserUseCase { } // Dos responsabilidades

   // ✅ Correcto
   public class CreateUserUseCase { }
   // Email se envía como efecto secundario o en otro use case
   ```

---

## Checklist de Implementación

Al crear un nuevo Use Case, verifica:

- [ ] Archivo nombrado: `{Operation}{Entity}UseCase.cs`
- [ ] Clase abstracta o sealed según necesidad
- [ ] XML comments en clase, Command, Handler
- [ ] Command implementa `ICommand<{ResultType}>`
- [ ] Command tiene propiedades con XML comments
- [ ] Handler usa primary constructor (C# 13)
- [ ] Handler implementa `ICommandHandler<Command, {ResultType}>`
- [ ] IUnitOfWork inyectado
- [ ] ILogger<Handler> inyectado
- [ ] Transacciones para operaciones de escritura
- [ ] NO transacciones para operaciones de solo lectura simples
- [ ] Try-catch apropiado
- [ ] Logging de operaciones importantes
- [ ] Logging de errores
- [ ] Commit en éxito, Rollback en error
- [ ] Retorna Result.Ok() o Result.Fail()
- [ ] Manejo de excepciones de dominio
- [ ] CancellationToken pasado a métodos async

---

## Recursos Adicionales

### Documentación Oficial

- [FastEndpoints Command Bus](https://fast-endpoints.com/docs/command-bus)
- [FluentResults GitHub](https://github.com/altmann/FluentResults)
- [Use Case Pattern](https://martinfowler.com/bliki/UseCase.html)

### Guías Relacionadas

- [Command Handler Patterns](./command-handler-patterns.md) - Patrones específicos CRUD
- [Error Handling](./error-handling.md) - Manejo de errores con FluentResults
- [Common Utilities](./common-utilities.md) - ValidationError y helpers

---

**Última actualización:** 2025-01-14
**Mantenedor:** Equipo APSYS
