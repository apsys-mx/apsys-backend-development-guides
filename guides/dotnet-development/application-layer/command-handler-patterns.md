# Command Handler Patterns - Application Layer

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

Esta guía documenta los **patrones específicos** para implementar handlers de operaciones CRUD en la Application Layer. Cada tipo de operación (Create, Get, Update, Delete, GetManyAndCount) tiene un patrón establecido con manejo de transacciones, errores y logging apropiado.

## Principios Generales

Todos los command handlers en APSYS siguen estos principios:

1. ✅ **Transaccional para escritura** - Create, Update, Delete usan transacciones
2. ✅ **No transaccional para lectura simple** - Get individual no necesita transacción
3. ✅ **Transaccional para consultas complejas** - GetManyAndCount usa transacción
4. ✅ **Result Pattern** - Retornan `Result<T>` o `Result`
5. ✅ **Error Handling** - Try-catch con conversión a FluentResults
6. ✅ **Logging** - Operaciones importantes y errores
7. ✅ **CancellationToken** - Todos los métodos async lo reciben

---

## Create Pattern

El patrón Create se usa para **crear nuevas entidades** en el sistema.

### Características

- ✅ Retorna `Result<Entity>`
- ✅ Usa `BeginTransaction()` / `Commit()` / `Rollback()`
- ✅ Maneja validaciones de dominio (`InvalidDomainException`)
- ✅ Maneja duplicados (`DuplicatedDomainException`)
- ✅ Puede llamar servicios externos antes o después de crear
- ✅ Logging de inicio y fin de operación

### Template

```csharp
using FastEndpoints;
using FluentResults;
using {project}.domain.entities;
using {project}.application.common;
using {project}.domain.exceptions;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace {project}.application.usecases.{feature};

public abstract class Create{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        // Properties for creation
        public string Property1 { get; set; } = string.Empty;
        public string Property2 { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                _logger.LogInformation("Creating {entity}: {identifier}",
                    nameof({Entity}), command.Property1);

                var entity = await _uoW.{Entities}.CreateAsync(
                    command.Property1,
                    command.Property2
                );

                _uoW.Commit();
                _logger.LogInformation("{Entity} created: {Id}", nameof({Entity}), entity.Id);
                return Result.Ok(entity);
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage
                    ?? $"Invalid {nameof({Entity}).ToLower()} data";
                return Result.Fail(
                    new Error(firstErrorMessage)
                        .CausedBy(idex)
                        .WithMetadata("ValidationErrors", idex)
                );
            }
            catch (DuplicatedDomainException ddex)
            {
                _uoW.Rollback();
                _logger.LogWarning("Duplicate {entity}: {message}",
                    nameof({Entity}), ddex.Message);
                return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error creating {entity}", nameof({Entity}));
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Ejemplo Real: CreatePrototypeUseCase

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.application.common;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using System.Text.Json;

namespace hashira.stone.backend.application.usecases.prototypes;

public abstract class CreatePrototypeUseCase
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

### Variante: Create con Servicio Externo

Cuando se necesita crear en un servicio externo (Auth0, payment gateway, etc.):

```csharp
public class Handler(IUnitOfWork uoW, IIdentityService identityService, ILogger<Handler> logger)
    : ICommandHandler<Command, Result<User>>
{
    private readonly IUnitOfWork _uoW = uoW;
    private readonly IIdentityService _identityService = identityService;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        _uoW.BeginTransaction();
        try
        {
            _logger.LogInformation("Creating user: {Email}", command.Email);

            // 1. Create in external service first
            var password = GenerateRandomPassword();
            var auth0User = _identityService.Create(command.Email, command.Name, password);

            // 2. Create in domain
            var user = await _uoW.Users.CreateAsync(command.Email, command.Name);

            _uoW.Commit();
            _logger.LogInformation("User created: {Id}", user.Id);
            return Result.Ok(user);
        }
        catch (HttpRequestException httpEx)
        {
            _uoW.Rollback();
            return Result.Fail(
                new Error($"Error creating user on authentication service")
                    .CausedBy(httpEx)
            );
        }
        catch (InvalidDomainException idex)
        {
            _uoW.Rollback();
            // Handle validation errors...
        }
        catch (Exception ex)
        {
            _uoW.Rollback();
            _logger.LogError(ex, "Error creating user");
            return Result.Fail(new Error(ex.Message).CausedBy(ex));
        }
    }
}
```

### Checklist Create Pattern

- [ ] `BeginTransaction()` al inicio
- [ ] Logging de inicio con datos identificadores
- [ ] Llamada a `CreateAsync()` del repositorio
- [ ] `Commit()` después de creación exitosa
- [ ] Logging de éxito con ID generado
- [ ] Try-catch con `InvalidDomainException`
- [ ] Try-catch con `DuplicatedDomainException`
- [ ] Try-catch genérico `Exception`
- [ ] `Rollback()` en todos los catch
- [ ] Return `Result.Fail()` en errores
- [ ] Return `Result.Ok(entity)` en éxito

---

## Get Pattern

El patrón Get se usa para **obtener una entidad por ID** u otro identificador único.

### Características

- ✅ Retorna `Result<Entity>`
- ❌ **NO usa transacciones** (operación de solo lectura)
- ✅ Retorna `Result.Fail()` si la entidad no existe
- ✅ Usa error predefinido del dominio si existe
- ✅ Try-catch solo para errores inesperados

### Template

```csharp
using FastEndpoints;
using FluentResults;
using {project}.domain.entities;
using {project}.domain.errors;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace {project}.application.usecases.{feature};

public class Get{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        public Guid Id { get; set; }
        // O cualquier otro identificador
        // public string Username { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                var entity = await _uoW.{Entities}.GetByIdAsync(command.Id, ct);

                if (entity == null)
                {
                    return Result.Fail({Entity}Errors.NotFound(command.Id));
                    // O usar mensaje directo si no hay error predefinido:
                    // return Result.Fail($"{nameof({Entity})} not found");
                }

                return Result.Ok(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {entity}: {id}",
                    nameof({Entity}), command.Id);
                return Result.Fail($"Error retrieving {nameof({Entity}).ToLower()}");
            }
        }
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

### Checklist Get Pattern

- [ ] **NO** usar `BeginTransaction()`
- [ ] Llamada a `GetByIdAsync()` o método equivalente
- [ ] Pasar `CancellationToken` al método async
- [ ] Verificar si la entidad es `null`
- [ ] Return `Result.Fail()` si no encontrada
- [ ] Return `Result.Ok(entity)` si encontrada
- [ ] Try-catch para errores inesperados
- [ ] Logging de errores en catch

---

## GetManyAndCount Pattern

El patrón GetManyAndCount se usa para **obtener listas paginadas** con conteo total.

### Características

- ✅ Retorna `GetManyAndCountResult<Entity>` o `GetManyAndCountResult<EntityDao>`
- ✅ **Usa transacciones** (query compleja con count)
- ✅ Recibe query string para filtrado/paginación
- ✅ Especifica campo de ordenamiento por defecto
- ✅ Try-catch con rollback en caso de error

### Template

```csharp
using FastEndpoints;
using {project}.domain.entities;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace {project}.application.usecases.{feature};

public abstract class GetManyAndCount{Entities}UseCase
{
    public class Command : ICommand<GetManyAndCountResult<{Entity}>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<{Entity}>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<{Entity}>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                _logger.LogInformation("Getting {entities} with query: {Query}",
                    nameof({Entity}), command.Query);

                var results = await _uoW.{Entities}.GetManyAndCountAsync(
                    command.Query,
                    nameof({Entity}.{DefaultSortField}), // Ej: nameof(User.Email)
                    ct
                );

                _logger.LogInformation("Retrieved {Count} {entities}",
                    results.Count, nameof({Entity}));

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

### Ejemplo Real: GetManyAndCountUsersUseCase

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.users;

public abstract class GetManyAndCountUsersUseCase
{
    public class Command : ICommand<GetManyAndCountResult<User>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<User>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation("Executing GetManyAndCountUsersUseCase with query: {Query}",
                    command.Query);

                var users = await _uoW.Users.GetManyAndCountAsync(
                    command.Query,
                    nameof(User.Email),
                    ct
                );

                _logger.LogInformation("End GetManyAndCountUsersUseCase with total users: {TotalUsers}",
                    users.Count);

                _uoW.Commit();
                return users;
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

### Variante: GetManyAndCount con DAO

Para consultas optimizadas de solo lectura, usar DAO en lugar de Entity:

```csharp
public abstract class GetManyAndCountPrototypesUseCase
{
    // Usa PrototypeDao en lugar de Prototype
    public class Command : ICommand<GetManyAndCountResult<PrototypeDao>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<PrototypeDao>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<PrototypeDao>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                // Usa repositorio DAO
                var results = await _uoW.PrototypesDao.GetManyAndCountAsync(
                    command.Query,
                    nameof(PrototypeDao.Number),
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

### Checklist GetManyAndCount Pattern

- [ ] `BeginTransaction()` al inicio
- [ ] Logging de inicio con query
- [ ] Llamada a `GetManyAndCountAsync()`
- [ ] Pasar query, defaultSortField y CancellationToken
- [ ] Logging de resultado con count
- [ ] `Commit()` después de consulta exitosa
- [ ] Try-catch con `Rollback()` en error
- [ ] Re-lanzar excepción para manejo en WebApi

---

## Update Pattern

El patrón Update se usa para **actualizar entidades existentes**.

### Características

- ✅ Retorna `Result<Entity>`
- ✅ Usa `BeginTransaction()` / `Commit()` / `Rollback()`
- ✅ Maneja `ResourceNotFoundException` si no existe
- ✅ Maneja `InvalidDomainException` para validaciones
- ✅ Maneja `DuplicatedDomainException` para unicidad
- ✅ Logging de operación

### Template

```csharp
using FastEndpoints;
using FluentResults;
using {project}.domain.entities;
using {project}.domain.exceptions;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace {project}.application.usecases.{feature};

public class Update{Entity}UseCase
{
    public class Command : ICommand<Result<{Entity}>>
    {
        public Guid Id { get; set; }
        // Properties to update
        public string Property1 { get; set; } = string.Empty;
        public string Property2 { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<{Entity}>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<{Entity}>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                _logger.LogInformation("Updating {entity}: {Id}",
                    nameof({Entity}), command.Id);

                var updated = await _uoW.{Entities}.UpdateAsync(
                    command.Id,
                    command.Property1,
                    command.Property2
                );

                _uoW.Commit();
                _logger.LogInformation("{Entity} updated: {Id}", nameof({Entity}), command.Id);
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
            catch (DuplicatedDomainException ex)
            {
                _uoW.Rollback();
                return Result.Fail<{Entity}>(new Error(ex.Message).CausedBy(ex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error updating {entity}: {Id}",
                    nameof({Entity}), command.Id);
                return Result.Fail<{Entity}>(
                    new Error("Internal server error: " + ex.Message).CausedBy(ex)
                );
            }
        }
    }
}
```

### Ejemplo Real: UpdateTechnicalStandardUseCase

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.technicalstandards;

public class UpdateTechnicalStandardUseCase
{
    public class Command : ICommand<Result<TechnicalStandard>>
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uow) : ICommandHandler<Command, Result<TechnicalStandard>>
    {
        private readonly IUnitOfWork _uow = uow;

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
    }
}
```

### Variante: Update Parcial

Para actualizar solo algunas propiedades (lock/unlock, status change, etc.):

```csharp
public class UpdateUserLockUseCase
{
    public class Command : ICommand<Result<User>>
    {
        public Guid Id { get; set; }
        public bool Locked { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<User>>
    {
        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                // Get entity first
                var user = await _uoW.Users.GetByIdAsync(command.Id, ct);
                if (user == null)
                    return Result.Fail(UserErrors.UserNotFound(command.Id));

                // Update specific property
                user.Locked = command.Locked;

                // Save changes
                await _uoW.Users.UpdateAsync(user);

                _uoW.Commit();
                return Result.Ok(user);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error updating user lock status");
                return Result.Fail<User>(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Checklist Update Pattern

- [ ] `BeginTransaction()` al inicio
- [ ] Logging de inicio con ID
- [ ] Llamada a `UpdateAsync()` del repositorio
- [ ] `Commit()` después de actualización exitosa
- [ ] Logging de éxito
- [ ] Try-catch con `ResourceNotFoundException`
- [ ] Try-catch con `InvalidDomainException`
- [ ] Try-catch con `DuplicatedDomainException`
- [ ] Try-catch genérico `Exception`
- [ ] `Rollback()` en todos los catch
- [ ] Return `Result.Fail<Entity>()` en errores
- [ ] Return `Result.Ok(updated)` en éxito

---

## Delete Pattern

El patrón Delete se usa para **eliminar entidades** del sistema.

### Características

- ✅ Retorna `Result` o `Result<bool>`
- ✅ Usa `BeginTransaction()` / `Commit()` / `Rollback()`
- ✅ Maneja `ResourceNotFoundException` si no existe
- ✅ Puede hacer soft delete o hard delete
- ✅ Logging de operación

### Template

```csharp
using FastEndpoints;
using FluentResults;
using {project}.domain.exceptions;
using {project}.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace {project}.application.usecases.{feature};

public class Delete{Entity}UseCase
{
    public class Command : ICommand<Result>
    {
        public Guid Id { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();

                _logger.LogInformation("Deleting {entity}: {Id}",
                    nameof({Entity}), command.Id);

                await _uoW.{Entities}.DeleteAsync(command.Id, ct);

                _uoW.Commit();
                _logger.LogInformation("{Entity} deleted: {Id}", nameof({Entity}), command.Id);
                return Result.Ok();
            }
            catch (ResourceNotFoundException ex)
            {
                _uoW.Rollback();
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error deleting {entity}: {Id}",
                    nameof({Entity}), command.Id);
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Variante: Soft Delete

```csharp
public class SoftDelete{Entity}UseCase
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
                _uoW.BeginTransaction();

                var entity = await _uoW.{Entities}.GetByIdAsync(command.Id, ct);
                if (entity == null)
                    return Result.Fail({Entity}Errors.NotFound(command.Id));

                // Soft delete - mark as deleted
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;

                await _uoW.{Entities}.UpdateAsync(entity);

                _uoW.Commit();
                return Result.Ok(entity);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error soft deleting {entity}", command.Id);
                return Result.Fail<{Entity}>(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Checklist Delete Pattern

- [ ] `BeginTransaction()` al inicio
- [ ] Logging de inicio con ID
- [ ] Llamada a `DeleteAsync()` del repositorio
- [ ] `Commit()` después de eliminación exitosa
- [ ] Logging de éxito
- [ ] Try-catch con `ResourceNotFoundException`
- [ ] Try-catch genérico `Exception`
- [ ] `Rollback()` en todos los catch
- [ ] Return `Result.Fail()` en errores
- [ ] Return `Result.Ok()` en éxito

---

## Comparación de Patrones

| Patrón | Transacción | Return Type | Excepciones Comunes |
|--------|-------------|-------------|---------------------|
| **Create** | ✅ Sí | `Result<Entity>` | InvalidDomain, Duplicated |
| **Get** | ❌ No | `Result<Entity>` | (ninguna esperada) |
| **GetManyAndCount** | ✅ Sí | `GetManyAndCountResult<T>` | (ninguna esperada) |
| **Update** | ✅ Sí | `Result<Entity>` | ResourceNotFound, InvalidDomain, Duplicated |
| **Delete** | ✅ Sí | `Result` | ResourceNotFound |

---

## Manejo de Errores por Patrón

### Create Pattern - Errores

```csharp
catch (InvalidDomainException idex)
{
    // Validación de entidad falló
    _uoW.Rollback();
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";
    return Result.Fail(new Error(firstErrorMessage).CausedBy(idex).WithMetadata("ValidationErrors", idex));
}
catch (DuplicatedDomainException ddex)
{
    // Entidad duplicada
    _uoW.Rollback();
    return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
}
catch (HttpRequestException httpEx)
{
    // Error en servicio externo (si aplica)
    _uoW.Rollback();
    return Result.Fail(new Error("Error in external service").CausedBy(httpEx));
}
catch (Exception ex)
{
    // Error inesperado
    _uoW.Rollback();
    _logger.LogError(ex, "Unexpected error");
    return Result.Fail(new Error(ex.Message).CausedBy(ex));
}
```

### Get Pattern - Errores

```csharp
// Check null first (before try-catch)
var entity = await _uoW.Entities.GetByIdAsync(id, ct);
if (entity == null)
    return Result.Fail(EntityErrors.NotFound(id));

try
{
    return Result.Ok(entity);
}
catch (Exception ex)
{
    // Solo errores inesperados
    _logger.LogError(ex, "Error retrieving entity");
    return Result.Fail("Error retrieving entity");
}
```

### Update Pattern - Errores

```csharp
catch (ResourceNotFoundException ex)
{
    // Entidad no existe
    _uoW.Rollback();
    return Result.Fail<Entity>(new Error(ex.Message).CausedBy(ex));
}
catch (InvalidDomainException ex)
{
    // Validación de entidad falló
    _uoW.Rollback();
    return Result.Fail<Entity>(new Error(ex.Message).CausedBy(ex));
}
catch (DuplicatedDomainException ex)
{
    // Entidad duplicada (unicidad)
    _uoW.Rollback();
    return Result.Fail<Entity>(new Error(ex.Message).CausedBy(ex));
}
catch (Exception ex)
{
    // Error inesperado
    _uoW.Rollback();
    return Result.Fail<Entity>(new Error("Internal server error: " + ex.Message).CausedBy(ex));
}
```

---

## Mejores Prácticas

### ✅ DO

1. **Usar transacciones para operaciones de escritura**
   ```csharp
   // ✅ Correcto
   _uoW.BeginTransaction();
   try {
       await _uoW.Entities.CreateAsync(...);
       _uoW.Commit();
   }
   catch {
       _uoW.Rollback();
   }
   ```

2. **NO usar transacciones para Get simple**
   ```csharp
   // ✅ Correcto
   var entity = await _uoW.Entities.GetByIdAsync(id, ct);
   return entity == null ? Result.Fail(...) : Result.Ok(entity);
   ```

3. **Logging apropiado**
   ```csharp
   // ✅ Correcto
   _logger.LogInformation("Creating user: {Email}", command.Email);
   _logger.LogError(ex, "Error creating user");
   ```

4. **Rollback en TODOS los catch**
   ```csharp
   // ✅ Correcto
   catch (InvalidDomainException ex)
   {
       _uoW.Rollback(); // Siempre rollback
       return Result.Fail(...);
   }
   ```

5. **Deserializar ValidationErrors correctamente**
   ```csharp
   // ✅ Correcto
   var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
   var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";
   ```

### ❌ DON'T

1. **No olvidar Rollback**
   ```csharp
   // ❌ Incorrecto
   catch (Exception ex)
   {
       // Falta Rollback!
       return Result.Fail(...);
   }
   ```

2. **No usar transacciones innecesarias**
   ```csharp
   // ❌ Incorrecto - Get no necesita transacción
   _uoW.BeginTransaction();
   var user = await _uoW.Users.GetByIdAsync(id, ct);
   _uoW.Commit();
   ```

3. **No lanzar excepciones sin convertir a Result**
   ```csharp
   // ❌ Incorrecto
   if (user == null)
       throw new NotFoundException();

   // ✅ Correcto
   if (user == null)
       return Result.Fail(UserErrors.NotFound(id));
   ```

---

## Recursos Adicionales

### Guías Relacionadas

- [Use Cases](./use-cases.md) - Fundamentos de Use Cases
- [Error Handling](./error-handling.md) - Manejo de errores con FluentResults
- [Common Utilities](./common-utilities.md) - ValidationError y helpers

### Documentación Oficial

- [FastEndpoints Command Bus](https://fast-endpoints.com/docs/command-bus)
- [FluentResults GitHub](https://github.com/altmann/FluentResults)

---

**Última actualización:** 2025-01-14
**Mantenedor:** Equipo APSYS
