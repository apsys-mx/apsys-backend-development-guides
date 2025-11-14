# Application Layer - Clean Architecture

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

La capa de aplicación contiene los **Use Cases** (casos de uso) de la aplicación. Orquesta el flujo de datos hacia y desde las entidades del dominio, coordinando la ejecución de reglas de negocio para lograr los objetivos del caso de uso.

Esta capa actúa como un **mediador** entre la capa de presentación (WebApi) y la capa de dominio, transformando requests en commands, ejecutando operaciones y devolviendo results.

## Responsabilidades

### ✅ SÍ hace la Application Layer

- **Orquestar casos de uso**: Coordinar llamadas entre Domain e Infrastructure
- **Manejar transacciones**: Usar IUnitOfWork para BeginTransaction(), Commit(), Rollback()
- **Transformar excepciones**: Convertir excepciones de Domain a FluentResults
- **Validar inputs**: Verificar que los datos de entrada sean válidos antes de procesarlos
- **Coordinar repositories**: Usar IUnitOfWork para acceder a repositorios
- **Logging**: Registrar información de ejecución de use cases
- **Manejo de servicios externos**: Coordinar llamadas a servicios de infraestructura

### ❌ NO hace la Application Layer

- **Lógica de negocio**: Esta va en Domain (entidades, validadores)
- **Acceso a base de datos**: Esto lo hace Infrastructure
- **Lógica de presentación**: Esto va en WebApi (DTOs, mappers, HTTP)
- **Configuración de infraestructura**: Esto va en Infrastructure
- **Crear entidades directamente**: Usa repositories que encapsulan la creación

## Principios Fundamentales

1. **Orquestación, no lógica**: La Application Layer orquesta, no contiene reglas de negocio
2. **Dependency Inversion**: Depende de interfaces de Domain, no de implementaciones de Infrastructure
3. **Command/Handler Pattern**: Usa FastEndpoints ICommand/ICommandHandler
4. **Result Pattern**: Usa FluentResults para manejo de errores sin excepciones
5. **Transaccionalidad**: Todo use case maneja transacciones con IUnitOfWork
6. **Separación de operaciones**: Cada operación (Create, Get, Update, Delete) es un use case separado

## Guías Disponibles

### 1. [Use Cases](./use-cases.md) ✅ v1.0.0

Patrón Command/Handler para implementar casos de uso.

**Contenido:**
- Qué es un Use Case
- Command/Handler pattern con FastEndpoints
- Estructura de un Use Case
- ICommand<TResult> interface
- ICommandHandler<TCommand, TResult> interface
- Naming conventions
- Dependencias (IUnitOfWork, ILogger, servicios)
- Ejemplos: CreateUserUseCase, GetUserUseCase, UpdateUserUseCase

**Cuándo usar:** Al crear un nuevo caso de uso para cualquier operación.

---

### 2. [Command Handler Patterns](./command-handler-patterns.md) ✅ v1.0.0

Patrones específicos para diferentes tipos de operaciones CRUD.

**Contenido:**
- Create pattern (transacción, validación, commit/rollback)
- Get pattern (consulta simple, no transaccional)
- GetManyAndCount pattern (paginación, filtrado)
- Update pattern (transacción, validación)
- Delete pattern (transacción, validación)
- Patrones de manejo de errores por operación
- Ejemplos de cada patrón

**Cuándo usar:** Al implementar operaciones CRUD específicas.

---

### 3. [Error Handling](./error-handling.md) ✅ v1.0.0

Manejo de errores con FluentResults en Application Layer.

**Contenido:**
- FluentResults: Result<T> vs Result
- Result.Ok() y Result.Fail()
- CausedBy() para excepciones
- WithMetadata() para metadata adicional
- Convertir excepciones de Domain a Results
- Try-catch patterns
- InvalidDomainException handling
- DuplicatedDomainException handling
- ResourceNotFoundException handling
- Error propagation a WebApi

**Cuándo usar:** En todos los handlers para manejo robusto de errores.

---

### 4. [Common Utilities](./common-utilities.md) ✅ v1.0.0

Utilidades compartidas, interfaces y patrones en la Application Layer.

**Contenido:**
- ValidationError model (deserialización de errores de FluentValidation)
- FastEndpoints interfaces (ICommand, ICommandHandler)
- Dependency Injection patterns (Primary Constructor C# 13)
- Async patterns (CancellationToken, ExecuteAsync, ConfigureAwait)
- Logging patterns (Structured logging, log levels)
- Ejemplos reales de CreateUserUseCase, CreatePrototypeUseCase, GetUserUseCase

**Cuándo usar:** Al implementar cualquier use case para seguir patrones consistentes.

---

## Estructura de la Capa de Aplicación

Basada en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```
application/
├── usecases/                                # Casos de uso organizados por feature
│   ├── users/                               # Feature: Users
│   │   ├── CreateUserUseCase.cs            # Command + Handler
│   │   ├── GetUserUseCase.cs               # Command + Handler
│   │   ├── GetManyAndCountUsersUseCase.cs  # Command + Handler
│   │   └── UpdateUserLockUseCase.cs        # Command + Handler
│   │
│   ├── roles/                               # Feature: Roles
│   │   ├── AddUsersToRoleUseCase.cs
│   │   └── RemoveUserFromRoleUseCase.cs
│   │
│   ├── prototypes/                          # Feature: Prototypes
│   │   ├── CreatePrototypeUseCase.cs
│   │   └── GetManyAndCountPrototypesUseCase.cs
│   │
│   └── technicalstandards/                  # Feature: Technical Standards
│       ├── CreateTechnicalStandardUseCase.cs
│       ├── GetManyAndCountTechnicalStandardsUseCase.cs
│       └── UpdateTechnicalStandardUseCase.cs
│
└── common/                                   # Utilidades compartidas
    └── ValidationError.cs                   # Helper para errores de validación
```

## Flujo de Trabajo

### Crear Nuevo Use Case CRUD

1. **Identificar operación** → Determinar qué tipo de operación es (Create, Get, Update, Delete)
2. **Crear archivo** → `usecases/{feature}/{Operation}{Entity}UseCase.cs`
3. **Definir Command** → [Use Cases](./use-cases.md)
4. **Implementar Handler** → [Command Handler Patterns](./command-handler-patterns.md)
5. **Agregar error handling** → [Error Handling](./error-handling.md)
6. **Agregar logging** → Inyectar ILogger y registrar eventos
7. **Testing** → Escribir tests unitarios

### Ejemplo Completo: Create Use Case

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.application.common;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using System.Text.Json;

namespace hashira.stone.backend.application.usecases.products;

public abstract class CreateProductUseCase
{
    /// <summary>
    /// Command to create a new product.
    /// </summary>
    public class Command : ICommand<Result<Product>>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Handler for creating a new product.
    /// </summary>
    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<Product>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<Product>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                _logger.LogInformation("Creating product: {Name}", command.Name);

                var product = await _uoW.Products.CreateAsync(
                    command.Name,
                    command.Price
                );

                _uoW.Commit();
                _logger.LogInformation("Product created: {Id}", product.Id);
                return Result.Ok(product);
            }
            catch (InvalidDomainException idex)
            {
                _uoW.Rollback();
                var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
                var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage
                    ?? "Invalid product data";
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
                _logger.LogError(ex, "Error creating product");
                return Result.Fail(new Error(ex.Message).CausedBy(ex));
            }
        }
    }
}
```

### Ejemplo: Get Use Case

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.errors;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.products;

public abstract class GetProductUseCase
{
    public class Command : ICommand<Result<Product>>
    {
        public Guid Id { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<Product>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<Product>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                var product = await _uoW.Products.GetByIdAsync(command.Id, ct);

                if (product == null)
                {
                    return Result.Fail(ProductErrors.ProductNotFound(command.Id));
                }

                return Result.Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product: {Id}", command.Id);
                return Result.Fail("Error retrieving product");
            }
        }
    }
}
```

### Ejemplo: GetManyAndCount Use Case

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.products;

public abstract class GetManyAndCountProductsUseCase
{
    public class Command : ICommand<GetManyAndCountResult<Product>>
    {
        public string? Query { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, GetManyAndCountResult<Product>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<GetManyAndCountResult<Product>> ExecuteAsync(
            Command command,
            CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation("Getting products with query: {Query}", command.Query);

                var products = await _uoW.Products.GetManyAndCountAsync(
                    command.Query,
                    nameof(Product.Name),
                    ct
                );

                _logger.LogInformation("Retrieved {Count} products", products.Count);
                _uoW.Commit();
                return products;
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

### Ejemplo: Update Use Case

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using Microsoft.Extensions.Logging;

namespace hashira.stone.backend.application.usecases.products;

public abstract class UpdateProductUseCase
{
    public class Command : ICommand<Result<Product>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class Handler(IUnitOfWork uoW, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<Product>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<Product>> ExecuteAsync(Command command, CancellationToken ct)
        {
            try
            {
                _uoW.BeginTransaction();
                _logger.LogInformation("Updating product: {Id}", command.Id);

                var updated = await _uoW.Products.UpdateAsync(
                    command.Id,
                    command.Name,
                    command.Price
                );

                _uoW.Commit();
                _logger.LogInformation("Product updated: {Id}", command.Id);
                return Result.Ok(updated);
            }
            catch (ResourceNotFoundException ex)
            {
                _uoW.Rollback();
                return Result.Fail<Product>(new Error(ex.Message).CausedBy(ex));
            }
            catch (InvalidDomainException ex)
            {
                _uoW.Rollback();
                return Result.Fail<Product>(new Error(ex.Message).CausedBy(ex));
            }
            catch (DuplicatedDomainException ex)
            {
                _uoW.Rollback();
                return Result.Fail<Product>(new Error(ex.Message).CausedBy(ex));
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error updating product: {Id}", command.Id);
                return Result.Fail<Product>(
                    new Error("Internal server error: " + ex.Message).CausedBy(ex)
                );
            }
        }
    }
}
```

---

## Checklists Rápidas

### Nuevo Use Case Create

- [ ] Archivo `usecases/{feature}/Create{Entity}UseCase.cs` creado
- [ ] Clase abstracta `Create{Entity}UseCase`
- [ ] Inner class `Command` implementa `ICommand<Result<{Entity}>>`
- [ ] Inner class `Handler` implementa `ICommandHandler<Command, Result<{Entity}>>`
- [ ] Constructor injection de `IUnitOfWork` y `ILogger<Handler>`
- [ ] `ExecuteAsync` método implementado
- [ ] `BeginTransaction()` al inicio
- [ ] Try-catch para manejar excepciones
- [ ] `Commit()` en caso de éxito
- [ ] `Rollback()` en caso de error
- [ ] Logging de operaciones importantes
- [ ] Return `Result.Ok()` en éxito
- [ ] Return `Result.Fail()` en errores
- [ ] XML comments en Command, Handler y ExecuteAsync

### Nuevo Use Case Get

- [ ] Archivo `usecases/{feature}/Get{Entity}UseCase.cs` creado
- [ ] Command con propiedad de identificación (Id, Username, etc.)
- [ ] Handler con IUnitOfWork y ILogger
- [ ] ExecuteAsync con try-catch
- [ ] NO usar transacciones para operaciones de solo lectura simples
- [ ] Return `Result.Fail()` si entidad no encontrada
- [ ] Return `Result.Ok(entity)` si encontrada
- [ ] Logging de operación

### Nuevo Use Case GetManyAndCount

- [ ] Archivo `usecases/{feature}/GetManyAndCount{Entities}UseCase.cs` creado
- [ ] Command con propiedad `Query` (string?)
- [ ] Handler retorna `GetManyAndCountResult<{Entity}>`
- [ ] `BeginTransaction()` al inicio
- [ ] Llamada a `GetManyAndCountAsync()` del repositorio
- [ ] `Commit()` después de consulta exitosa
- [ ] Try-catch con `Rollback()` en caso de error
- [ ] Logging de query y resultado

### Nuevo Use Case Update

- [ ] Archivo `usecases/{feature}/Update{Entity}UseCase.cs` creado
- [ ] Command con Id y propiedades a actualizar
- [ ] Handler con transacciones
- [ ] Manejo de `ResourceNotFoundException`
- [ ] Manejo de `InvalidDomainException`
- [ ] Manejo de `DuplicatedDomainException`
- [ ] Return Result con entidad actualizada

---

## Patrones Clave

### 1. Command/Handler Pattern

Separación clara entre el comando (qué hacer) y el handler (cómo hacerlo):

```
┌─────────────────┐
│    Command      │  Qué hacer
│  (Data holder)  │  - Propiedades
└─────────────────┘  - Sin lógica
        │
        │ Ejecutado por
        ▼
┌─────────────────┐
│     Handler     │  Cómo hacerlo
│  (Logic)        │  - Orquestación
└─────────────────┘  - Transacciones
```

### 2. Result Pattern

Manejo de errores sin excepciones usando FluentResults:

```csharp
// Éxito
return Result.Ok(entity);

// Fallo con mensaje
return Result.Fail("Error message");

// Fallo con excepción
return Result.Fail(new Error("Error").CausedBy(exception));

// Fallo con metadata
return Result.Fail(new Error("Error").WithMetadata("Key", value));
```

### 3. Unit of Work Pattern

Manejo transaccional consistente:

```csharp
_uoW.BeginTransaction();
try
{
    // Operaciones
    _uoW.Commit();
    return Result.Ok(entity);
}
catch (Exception ex)
{
    _uoW.Rollback();
    return Result.Fail(new Error(ex.Message).CausedBy(ex));
}
```

### 4. Dependency Inversion

Application depende de interfaces de Domain, no de implementaciones:

```
┌─────────────────┐
│  Infrastructure │  Implementa
│  NHUserRepo     │───────────────┐
└─────────────────┘               │
                                  ▼
                         ┌─────────────────┐
                         │     Domain      │
                         │  IUserRepository│
                         └─────────────────┘
                                  ▲
┌─────────────────┐               │
│   Application   │  Usa          │
│   CreateUserUC  │───────────────┘
└─────────────────┘
```

---

## Reglas de Oro

### ✅ SÍ hacer en Application

- Orquestar flujo entre Domain e Infrastructure
- Manejar transacciones con IUnitOfWork
- Convertir excepciones a FluentResults
- Inyectar dependencias por constructor
- Usar ILogger para logging
- Separar cada operación en su propio Use Case
- Usar try-catch para capturar excepciones
- Commit en éxito, Rollback en error
- Documentar con XML comments
- Nombrar consistentemente (Create, Get, Update, Delete)

### ❌ NO hacer en Application

- Implementar lógica de negocio (va en Domain)
- Acceder directamente a base de datos (usar repositorios)
- Crear entidades directamente (usar métodos del repositorio)
- Hacer validaciones de dominio (van en Domain)
- Retornar DTOs (retornar entidades, WebApi mapea a DTOs)
- Lanzar excepciones sin convertirlas a Results
- Olvidar transacciones en operaciones de escritura
- Hacer múltiples operaciones en un solo Use Case
- Referenciar Infrastructure o WebApi

---

## Excepciones vs FluentResults

La Application Layer convierte excepciones de Domain a FluentResults para control de flujo:

### Excepciones (lanzadas por Domain)

```csharp
// Domain lanza excepciones
throw new InvalidDomainException(entity.Validate());
throw new DuplicatedDomainException("Entity already exists");
throw new ResourceNotFoundException("Entity not found");
```

### FluentResults (retornados por Application)

```csharp
// Application convierte a Results
catch (InvalidDomainException idex)
{
    var errors = JsonSerializer.Deserialize<List<ValidationError>>(idex.Message);
    var firstErrorMessage = errors?.FirstOrDefault()?.ErrorMessage ?? "Invalid data";
    return Result.Fail(new Error(firstErrorMessage)
        .CausedBy(idex)
        .WithMetadata("ValidationErrors", idex));
}

catch (DuplicatedDomainException ddex)
{
    return Result.Fail(new Error(ddex.Message).CausedBy(ddex));
}

catch (ResourceNotFoundException rnfex)
{
    return Result.Fail(new Error(rnfex.Message).CausedBy(rnfex));
}
```

**Criterio:**
- Domain usa **excepciones** para errores de dominio
- Application convierte a **FluentResults** para control de flujo
- WebApi traduce Results a HTTP status codes

---

## Stack Tecnológico

- **FastEndpoints 7.0** - ICommand/ICommandHandler interfaces
- **FluentResults 4.0** - Result pattern
- **.NET 9.0** - Framework
- **C# 13** - Lenguaje
- **Microsoft.Extensions.Logging** - Logging

### Dependencias del Proyecto

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints" />
  <PackageReference Include="FluentResults" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\{project}.domain\{project}.domain.csproj" />
</ItemGroup>
```

**Nota:** Application solo referencia Domain, NUNCA Infrastructure o WebApi.

---

## Recursos Adicionales

### Documentación Oficial

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [FluentResults GitHub](https://github.com/altmann/FluentResults)
- [Command Pattern](https://refactoring.guru/design-patterns/command)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

### Otras Secciones de Guías

- [Best Practices](../best-practices/README.md) - Prácticas generales
- [Domain Layer](../domain-layer/README.md) - Entidades y reglas de negocio
- [Infrastructure Layer](../infrastructure-layer/README.md) - Implementaciones
- [WebApi Layer](../webapi-layer/README.md) - Endpoints y DTOs

---

## Conclusión

**Principios Clave del Application Layer:**

1. ✅ **Orquestación, no lógica** - Coordina, no implementa reglas de negocio
2. ✅ **Command/Handler pattern** - Separación clara de comandos y handlers
3. ✅ **Result pattern** - FluentResults para manejo de errores
4. ✅ **Transaccionalidad** - IUnitOfWork para manejo de transacciones
5. ✅ **Dependency Inversion** - Depende de interfaces de Domain
6. ✅ **Separación de operaciones** - Un Use Case por operación

**Flujo Mental:**

```
WebApi Endpoint → Command → Handler → IUnitOfWork → Domain
                                         ↓
                                    BeginTransaction()
                                         ↓
                                    Repositories
                                         ↓
                                    Commit/Rollback
                                         ↓
                                    Result<T>
                                         ↓
                                    WebApi Response
```

**Responsabilidad:**
- Application **orquesta** el flujo
- Domain **contiene** la lógica de negocio
- Infrastructure **implementa** el acceso a datos
- WebApi **expone** los endpoints HTTP

---

**Última actualización:** 2025-01-14
**Mantenedor:** Equipo APSYS

## Resumen de Guías Completadas

| Guía | Estado | Versión | Contenido Principal |
|------|--------|---------|---------------------|
| [README.md](./README.md) | ✅ | v1.0.0 | Overview completo, ejemplos, patrones |
| [use-cases.md](./use-cases.md) | ✅ | v1.0.0 | Command/Handler pattern, estructura |
| [command-handler-patterns.md](./command-handler-patterns.md) | ✅ | v1.0.0 | Create, Get, Update, Delete patterns |
| [error-handling.md](./error-handling.md) | ✅ | v1.0.0 | FluentResults, exception handling |
| [common-utilities.md](./common-utilities.md) | ✅ | v1.0.0 | ValidationError, FastEndpoints, DI, Async, Logging |
| **TOTAL** | **5 guías** | **v1.0.0** | **Application Layer completo** |
