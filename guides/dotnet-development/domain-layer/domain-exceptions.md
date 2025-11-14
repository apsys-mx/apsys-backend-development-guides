# Domain Exceptions

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenidos
- [Introducción](#introducción)
- [Excepciones vs Results (FluentResults)](#excepciones-vs-results-fluentresults)
- [InvalidDomainException](#invaliddomainexception)
- [DuplicatedDomainException](#duplicateddomainexception)
- [ResourceNotFoundException](#resourcenotfoundexception)
- [InvalidFilterArgumentException](#invalidfilterargumentexception)
- [Cuándo Lanzar Excepciones](#cuándo-lanzar-excepciones)
- [Manejo de Excepciones en Endpoints](#manejo-de-excepciones-en-endpoints)
- [Ejemplos Reales del Proyecto](#ejemplos-reales-del-proyecto)
- [Patrones y Best Practices](#patrones-y-best-practices)
- [Checklist para Nuevas Excepciones](#checklist-para-nuevas-excepciones)

---

## Introducción

Las **Domain Exceptions** son excepciones específicas del dominio que representan **errores de negocio** o **situaciones excepcionales** que violan reglas del dominio.

En APSYS, las Domain Exceptions se usan para:

- **Validaciones de dominio**: Cuando una entidad no cumple reglas de negocio (`InvalidDomainException`)
- **Duplicados**: Cuando se intenta crear una entidad que ya existe (`DuplicatedDomainException`)
- **Recursos no encontrados**: Cuando una entidad no existe en BD (`ResourceNotFoundException`)
- **Argumentos inválidos**: Cuando parámetros de filtro son incorrectos (`InvalidFilterArgumentException`)

### Jerarquía de Excepciones

```
System.Exception
    │
    ├── InvalidDomainException
    │   └── Contiene: IEnumerable<ValidationFailure>
    │
    ├── DuplicatedDomainException
    │   └── Simple Exception con mensaje
    │
    ├── ResourceNotFoundException
    │   └── Simple Exception con mensaje
    │
    └── System.ArgumentException
        └── InvalidFilterArgumentException
            └── Specific ArgumentException
```

**Nota:** En APSYS, las Domain Exceptions **no heredan de una clase base común**. Cada excepción hereda directamente de `Exception` o `ArgumentException` según su propósito.

---

## Excepciones vs Results (FluentResults)

### Excepciones: Para Errores Excepcionales

**Usa excepciones cuando:**
- La situación es **excepcional** y **no esperada**
- Requiere **rollback de transacción**
- Necesitas **interrumpir el flujo** inmediatamente
- Son **errores de sistema** o **violaciones graves de reglas**

```csharp
// Excepciones: Situaciones excepcionales
public async Task<User> CreateAsync(string email, string name)
{
    // Validar que no exista
    var existing = await GetByEmailAsync(email);
    if (existing != null)
    {
        // ¡Esto es excepcional! No debería intentarse crear duplicados
        throw new DuplicatedDomainException($"User with email {email} already exists");
    }

    var user = new User(email, name);

    // Validar entidad
    if (!user.IsValid())
    {
        // ¡Violación de reglas de dominio!
        throw new InvalidDomainException(user.Validate());
    }

    await AddAsync(user);
    return user;
}
```

### FluentResults: Para Flujos de Negocio

**Usa Results cuando:**
- El "error" es **parte del flujo de negocio**
- Quieres que el llamador **decida qué hacer**
- No requiere rollback
- Son **validaciones de entrada** o **reglas de negocio esperadas**

```csharp
// Results: Flujos de negocio esperados
public async Task<Result<User>> AuthenticateAsync(string email, string password)
{
    var user = await GetByEmailAsync(email);

    // Usuario no existe - parte del flujo esperado
    if (user == null)
    {
        return Result.Fail<User>("Invalid email or password");
    }

    // Usuario bloqueado - parte del flujo esperado
    if (user.Locked)
    {
        return Result.Fail<User>("Account is locked");
    }

    // Password incorrecta - parte del flujo esperado
    if (!VerifyPassword(password, user.PasswordHash))
    {
        return Result.Fail<User>("Invalid email or password");
    }

    return Result.Ok(user);
}
```

### Comparación

| Aspecto | Excepciones | Results (FluentResults) |
|---------|-------------|--------------------------|
| **Uso** | Errores excepcionales | Flujos de negocio esperados |
| **Performance** | Costoso (stack unwinding) | Ligero (objeto return) |
| **Control de flujo** | Interrumpe ejecución | Controlado por caller |
| **Rollback** | Sí (en catch) | No automático |
| **Testing** | Requiere Assert.Throws | Assert.IsFalse(result.IsSuccess) |
| **Ejemplos** | Validación de entidad, duplicados, recursos no encontrados | Login fallido, permisos insuficientes, reglas de negocio opcionales |

---

## InvalidDomainException

Excepción lanzada cuando una **entidad no cumple las reglas de validación** del dominio.

### Definición Completa

```csharp
using FluentValidation.Results;
using System.Text.Json;

namespace hashira.stone.backend.domain.exceptions;

/// <summary>
/// Invalid domain exception class
/// </summary>
public class InvalidDomainException : Exception
{
    public readonly IEnumerable<ValidationFailure> ValidationFailures;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="validationFailures"></param>
    public InvalidDomainException(IEnumerable<ValidationFailure> validationFailures)
    {
        this.ValidationFailures = validationFailures;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="property"></param>
    /// <param name="errorCode"></param>
    /// <param name="errorMessage"></param>
    public InvalidDomainException(string property, string errorCode, string errorMessage)
    {
        var validationResults = new List<ValidationFailure>
            {
                new()
                {
                    ErrorCode = errorCode,
                    PropertyName = property,
                    ErrorMessage = errorMessage
                }
            };
        this.ValidationFailures = validationResults.AsEnumerable();
    }

    /// <summary>
    /// Get error message
    /// </summary>
    public override string Message
    {
        get
        {
            var messages = from error in this.ValidationFailures
                           select new { error.ErrorMessage, error.ErrorCode, error.PropertyName };
            return JsonSerializer.Serialize(messages);
        }
    }
}
```

### Características

1. **Contiene ValidationFailures**: Lista de errores de FluentValidation
2. **Dos constructores**:
   - Con `IEnumerable<ValidationFailure>`: Para múltiples errores de validación
   - Con `(property, errorCode, errorMessage)`: Para un solo error específico
3. **Message serializado en JSON**: Fácil de consumir en API

### Uso Típico

```csharp
// En Repository o Service
public async Task<User> CreateAsync(string email, string name)
{
    var user = new User(email, name);

    // Validar usando FluentValidation
    if (!user.IsValid())
    {
        // Lanzar excepción con todos los errores de validación
        throw new InvalidDomainException(user.Validate());
    }

    await AddAsync(user);
    return user;
}
```

### Ejemplo con Error Único

```csharp
// Validación custom en repositorio
public async Task<TechnicalStandard> UpdateAsync(Guid id, string code, ...)
{
    var standard = await GetAsync(id);
    if (standard == null)
    {
        throw new ResourceNotFoundException($"TechnicalStandard with ID {id} not found");
    }

    // Validar código único
    var existingByCode = await GetByCodeAsync(code);
    if (existingByCode != null && existingByCode.Id != id)
    {
        // Lanzar con error específico
        throw new InvalidDomainException(
            property: "Code",
            errorCode: "Code_Duplicated",
            errorMessage: $"Another technical standard with code {code} already exists"
        );
    }

    // Actualizar propiedades...
    await SaveAsync(standard);
    return standard;
}
```

### JSON Output

Cuando se serializa `Message`, genera JSON:

```json
[
  {
    "errorMessage": "The [Email] cannot be null or empty",
    "errorCode": "Email",
    "propertyName": "Email"
  },
  {
    "errorMessage": "The [Name] cannot be null or empty",
    "errorCode": "Name",
    "propertyName": "Name"
  }
]
```

---

## DuplicatedDomainException

Excepción lanzada cuando se **intenta crear una entidad que ya existe** en el sistema.

### Definición Completa

```csharp
namespace hashira.stone.backend.domain.exceptions;

/// <summary>
/// Duplicated domain exception class
/// </summary>
public class DuplicatedDomainException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message"></param>
    public DuplicatedDomainException(string message) : base(message)
    {
    }
}
```

### Características

- **Simple Exception**: Solo contiene mensaje
- **Hereda de Exception**: No necesita propiedades adicionales
- **Mensaje descriptivo**: Debe indicar qué entidad está duplicada

### Uso Típico

```csharp
// En Repository
public async Task<User> CreateAsync(string email, string name)
{
    // Validar que no exista usuario con mismo email
    var existing = await GetByEmailAsync(email);
    if (existing != null)
    {
        throw new DuplicatedDomainException(
            $"User with email {email} already exists"
        );
    }

    var user = new User(email, name);
    await AddAsync(user);
    return user;
}
```

### Otro Ejemplo

```csharp
// En Repository
public async Task<TechnicalStandard> CreateAsync(string code, string name, ...)
{
    // Validar código único
    var existing = await GetByCodeAsync(code);
    if (existing != null)
    {
        throw new DuplicatedDomainException(
            $"Technical standard with code {code} already exists"
        );
    }

    var standard = new TechnicalStandard(code, name, edition, status, type);
    await AddAsync(standard);
    return standard;
}
```

---

## ResourceNotFoundException

Excepción lanzada cuando un **recurso solicitado no existe** en el sistema.

### Definición Completa

```csharp
namespace hashira.stone.backend.domain.exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException() { }
    public ResourceNotFoundException(string message) : base(message) { }
}
```

### Características

- **Simple Exception**: Solo mensaje (opcional)
- **Dos constructores**: Sin mensaje o con mensaje descriptivo
- **Indica entidad no encontrada**: Usado en Get, Update, Delete

### Uso Típico

```csharp
// En Repository
public async Task<TechnicalStandard> UpdateAsync(Guid id, string code, ...)
{
    var standard = await GetAsync(id);
    if (standard == null)
    {
        throw new ResourceNotFoundException(
            $"TechnicalStandard with ID {id} not found"
        );
    }

    // Actualizar propiedades
    standard.Code = code;
    standard.Name = name;
    // ...

    await SaveAsync(standard);
    return standard;
}
```

### Otro Ejemplo

```csharp
// En Endpoint
public override async Task HandleAsync(DeleteUserRequest req, CancellationToken ct)
{
    var user = await _unitOfWork.Users.GetAsync(req.UserId, ct);
    if (user == null)
    {
        throw new ResourceNotFoundException(
            $"User with ID {req.UserId} not found"
        );
    }

    await _unitOfWork.Users.DeleteAsync(user, ct);
    _unitOfWork.Commit();
}
```

---

## InvalidFilterArgumentException

Excepción lanzada cuando **argumentos de filtro son inválidos** en queries.

### Definición Completa

```csharp
namespace hashira.stone.backend.domain.exceptions;

/// <summary>
/// Exception that is thrown when an invalid argument is provided for a filter operation.
/// This exception is typically used to indicate that a filter argument does not meet the expected criteria
/// </summary>
public class InvalidFilterArgumentException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a default error message.
    /// </summary>
    public InvalidFilterArgumentException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public InvalidFilterArgumentException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a specified error message and parameter name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="param"></param>
    public InvalidFilterArgumentException(string message, string param) : base(message, param) { }
}
```

### Características

- **Hereda de ArgumentException**: Más específico que Exception
- **Tres constructores**: Vacío, mensaje, mensaje + nombre de parámetro
- **Para validación de parámetros**: En queries, filters, sorts

### Uso Típico

```csharp
// En Repository o Service
public async Task<GetManyAndCountResult<User>> GetManyAndCountAsync(
    string? query,
    string sortBy,
    CancellationToken ct = default)
{
    // Validar sortBy
    var validSortFields = new[] { "Name", "Email", "CreationDate" };
    if (!validSortFields.Contains(sortBy))
    {
        throw new InvalidFilterArgumentException(
            $"Invalid sort field: {sortBy}. Valid fields are: {string.Join(", ", validSortFields)}",
            nameof(sortBy)
        );
    }

    // Continuar con query...
}
```

### Otro Ejemplo

```csharp
// Validar rango de fechas
public async Task<IEnumerable<Prototype>> GetByDateRangeAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken ct = default)
{
    if (startDate > endDate)
    {
        throw new InvalidFilterArgumentException(
            "Start date cannot be after end date",
            nameof(startDate)
        );
    }

    return await GetAsync(
        p => p.IssueDate >= startDate && p.IssueDate <= endDate,
        ct
    );
}
```

---

## Cuándo Lanzar Excepciones

### ✅ Lanza Excepciones cuando:

1. **Validación de entidad falla**
   ```csharp
   if (!user.IsValid())
   {
       throw new InvalidDomainException(user.Validate());
   }
   ```

2. **Intento de crear duplicado**
   ```csharp
   if (existingUser != null)
   {
       throw new DuplicatedDomainException($"User with email {email} already exists");
   }
   ```

3. **Recurso no encontrado en Update/Delete**
   ```csharp
   if (user == null)
   {
       throw new ResourceNotFoundException($"User with ID {id} not found");
   }
   ```

4. **Parámetros de query inválidos**
   ```csharp
   if (!validSortFields.Contains(sortBy))
   {
       throw new InvalidFilterArgumentException($"Invalid sort field: {sortBy}");
   }
   ```

5. **Violación de regla de negocio crítica**
   ```csharp
   if (prototype.ExpirationDate < prototype.IssueDate)
   {
       throw new InvalidDomainException(
           "ExpirationDate",
           "ExpirationDate_Invalid",
           "ExpirationDate must be after IssueDate"
       );
   }
   ```

### ❌ NO Lances Excepciones cuando:

1. **Es parte del flujo de negocio esperado**
   ```csharp
   // ❌ MAL: Excepción para login fallido
   if (!ValidatePassword(password))
   {
       throw new InvalidDomainException("Invalid password");
   }

   // ✅ BIEN: Result para flujo esperado
   if (!ValidatePassword(password))
   {
       return Result.Fail("Invalid email or password");
   }
   ```

2. **El caller puede manejar el caso**
   ```csharp
   // ❌ MAL: Excepción cuando no hay resultados
   var users = await GetAsync(ct);
   if (!users.Any())
   {
       throw new ResourceNotFoundException("No users found");
   }

   // ✅ BIEN: Retornar colección vacía
   var users = await GetAsync(ct); // Puede ser vacío
   return users; // Caller decide qué hacer
   ```

3. **Es validación de entrada de UI**
   ```csharp
   // ❌ MAL: Excepción para validación de request
   if (string.IsNullOrEmpty(req.Email))
   {
       throw new InvalidDomainException("Email is required");
   }

   // ✅ BIEN: Validación de FastEndpoints
   public class CreateUserRequestValidator : Validator<CreateUserRequest>
   {
       public CreateUserRequestValidator()
       {
           RuleFor(x => x.Email).NotEmpty().EmailAddress();
       }
   }
   ```

---

## Manejo de Excepciones en Endpoints

### Patrón Try-Catch en FastEndpoints

```csharp
public class CreateUserEndpoint : Endpoint<CreateUserRequest, UserResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            var user = await _unitOfWork.Users.CreateAsync(req.Email, req.Name);

            _unitOfWork.Commit();

            await SendAsync(new UserResponse
            {
                Success = true,
                Data = user,
                Message = "User created successfully"
            }, 201, ct);
        }
        catch (DuplicatedDomainException ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new UserResponse
            {
                Success = false,
                Message = ex.Message
            }, 409, ct); // Conflict
        }
        catch (InvalidDomainException ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new UserResponse
            {
                Success = false,
                Message = ex.Message // JSON con ValidationFailures
            }, 400, ct); // Bad Request
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new UserResponse
            {
                Success = false,
                Message = "An unexpected error occurred"
            }, 500, ct); // Internal Server Error
        }
    }
}
```

### Mapeo de Excepciones a HTTP Status Codes

| Excepción | HTTP Status | Código | Razón |
|-----------|-------------|--------|-------|
| `InvalidDomainException` | 400 | Bad Request | Entidad no cumple validaciones |
| `DuplicatedDomainException` | 409 | Conflict | Entidad ya existe |
| `ResourceNotFoundException` | 404 | Not Found | Recurso no encontrado |
| `InvalidFilterArgumentException` | 400 | Bad Request | Parámetros de query inválidos |
| `Exception` (otros) | 500 | Internal Server Error | Error inesperado |

---

## Ejemplos Reales del Proyecto

### Ejemplo 1: Create con Validación y Duplicado

```csharp
// Repository
public class UserRepository : BaseRepository<User, Guid>, IUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        // 1. Validar duplicado
        var existing = await GetByEmailAsync(email);
        if (existing != null)
        {
            throw new DuplicatedDomainException(
                $"User with email {email} already exists"
            );
        }

        // 2. Crear entidad
        var user = new User(email, name);

        // 3. Validar entidad
        if (!user.IsValid())
        {
            throw new InvalidDomainException(user.Validate());
        }

        // 4. Agregar a BD
        await AddAsync(user);
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var users = await GetAsync(u => u.Email == email);
        return users.FirstOrDefault();
    }
}
```

### Ejemplo 2: Update con ResourceNotFound

```csharp
// Repository
public class TechnicalStandardRepository : BaseRepository<TechnicalStandard, Guid>, ITechnicalStandardRepository
{
    public async Task<TechnicalStandard> UpdateAsync(
        Guid id,
        string code,
        string name,
        string edition,
        string status,
        string type)
    {
        // 1. Buscar entidad
        var standard = await GetAsync(id);
        if (standard == null)
        {
            throw new ResourceNotFoundException(
                $"TechnicalStandard with ID {id} not found"
            );
        }

        // 2. Validar código único (si cambió)
        if (standard.Code != code)
        {
            var existingByCode = await GetByCodeAsync(code);
            if (existingByCode != null && existingByCode.Id != id)
            {
                throw new InvalidDomainException(
                    property: "Code",
                    errorCode: "Code_Duplicated",
                    errorMessage: $"Another technical standard with code {code} already exists"
                );
            }
        }

        // 3. Actualizar propiedades
        standard.Code = code;
        standard.Name = name;
        standard.Edition = edition;
        standard.Status = status;
        standard.Type = type;

        // 4. Validar entidad actualizada
        if (!standard.IsValid())
        {
            throw new InvalidDomainException(standard.Validate());
        }

        // 5. Guardar cambios
        await SaveAsync(standard);
        return standard;
    }

    public async Task<TechnicalStandard?> GetByCodeAsync(string code)
    {
        var standards = await GetAsync(s => s.Code == code);
        return standards.FirstOrDefault();
    }
}
```

### Ejemplo 3: Query con InvalidFilterArgument

```csharp
// Repository
public class PrototypeRepository : BaseRepository<Prototype, Guid>, IPrototypeRepository
{
    public async Task<GetManyAndCountResult<Prototype>> GetManyAndCountAsync(
        string? query,
        string sortBy,
        CancellationToken ct = default)
    {
        // Validar campo de ordenamiento
        var validSortFields = new[] { "Number", "IssueDate", "ExpirationDate", "Status", "CreationDate" };
        if (!validSortFields.Contains(sortBy))
        {
            throw new InvalidFilterArgumentException(
                $"Invalid sort field '{sortBy}'. Valid fields are: {string.Join(", ", validSortFields)}",
                nameof(sortBy)
            );
        }

        // Continuar con query...
        var sortingCriteria = new SortingCriteria(sortBy, SortingCriteriaType.Ascending);
        // ... implementación GetManyAndCount
    }
}
```

### Ejemplo 4: Endpoint Completo con Manejo de Excepciones

```csharp
public class UpdateTechnicalStandardEndpoint : Endpoint<UpdateTechnicalStandardRequest, TechnicalStandardResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTechnicalStandardEndpoint(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Put("/api/technical-standards/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateTechnicalStandardRequest req, CancellationToken ct)
    {
        try
        {
            _unitOfWork.BeginTransaction();

            var updated = await _unitOfWork.TechnicalStandards.UpdateAsync(
                req.Id,
                req.Code,
                req.Name,
                req.Edition,
                req.Status,
                req.Type
            );

            _unitOfWork.Commit();

            await SendAsync(new TechnicalStandardResponse
            {
                Success = true,
                Data = updated,
                Message = "Technical standard updated successfully"
            }, 200, ct);
        }
        catch (ResourceNotFoundException ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new TechnicalStandardResponse
            {
                Success = false,
                Message = ex.Message
            }, 404, ct);
        }
        catch (InvalidDomainException ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new TechnicalStandardResponse
            {
                Success = false,
                Message = ex.Message // JSON con ValidationFailures
            }, 400, ct);
        }
        catch (DuplicatedDomainException ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new TechnicalStandardResponse
            {
                Success = false,
                Message = ex.Message
            }, 409, ct);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            await SendAsync(new TechnicalStandardResponse
            {
                Success = false,
                Message = "An unexpected error occurred"
            }, 500, ct);
        }
    }
}
```

---

## Patrones y Best Practices

### ✅ DO: Lanzar Excepciones en Capa de Dominio

```csharp
// ✅ BIEN: Repository lanza excepciones de dominio
public async Task<User> CreateAsync(string email, string name)
{
    var existing = await GetByEmailAsync(email);
    if (existing != null)
    {
        throw new DuplicatedDomainException($"User with email {email} already exists");
    }

    var user = new User(email, name);
    if (!user.IsValid())
    {
        throw new InvalidDomainException(user.Validate());
    }

    await AddAsync(user);
    return user;
}
```

### ❌ DON'T: Devolver Null en Lugar de Excepción

```csharp
// ❌ MAL: Retornar null cuando debería lanzar excepción
public async Task<User> UpdateAsync(Guid id, string name)
{
    var user = await GetAsync(id);
    if (user == null)
    {
        return null; // ¡NO! El caller no sabe si falló
    }

    user.Name = name;
    await SaveAsync(user);
    return user;
}

// ✅ BIEN: Lanzar excepción clara
public async Task<User> UpdateAsync(Guid id, string name)
{
    var user = await GetAsync(id);
    if (user == null)
    {
        throw new ResourceNotFoundException($"User with ID {id} not found");
    }

    user.Name = name;
    await SaveAsync(user);
    return user;
}
```

### ✅ DO: Capturar Excepciones Específicas Primero

```csharp
// ✅ BIEN: Orden correcto de catch
try
{
    // ...
}
catch (DuplicatedDomainException ex)
{
    // Manejo específico para duplicados
}
catch (InvalidDomainException ex)
{
    // Manejo específico para validaciones
}
catch (ResourceNotFoundException ex)
{
    // Manejo específico para not found
}
catch (Exception ex)
{
    // Manejo genérico al final
}
```

### ❌ DON'T: Catch Exception Antes que Específicas

```csharp
// ❌ MAL: Exception genérico primero
try
{
    // ...
}
catch (Exception ex) // ¡Atrapa TODO!
{
    // Las siguientes nunca se ejecutarán
}
catch (DuplicatedDomainException ex) // ¡Unreachable!
{
    // ...
}
```

### ✅ DO: Hacer Rollback en Catch

```csharp
// ✅ BIEN: Rollback en catch
try
{
    _unitOfWork.BeginTransaction();

    var user = await _unitOfWork.Users.CreateAsync(email, name);
    await _unitOfWork.Roles.AddUserToRoleAsync("User", email);

    _unitOfWork.Commit();
}
catch (Exception ex)
{
    _unitOfWork.Rollback(); // SIEMPRE rollback en excepción
    throw; // Re-lanzar para que endpoint lo maneje
}
```

### ✅ DO: Mensajes Descriptivos

```csharp
// ✅ BIEN: Mensaje descriptivo con contexto
throw new DuplicatedDomainException(
    $"User with email {email} already exists"
);

throw new ResourceNotFoundException(
    $"TechnicalStandard with ID {id} not found"
);

throw new InvalidFilterArgumentException(
    $"Invalid sort field '{sortBy}'. Valid fields are: {string.Join(", ", validSortFields)}",
    nameof(sortBy)
);
```

### ❌ DON'T: Mensajes Genéricos

```csharp
// ❌ MAL: Mensajes genéricos sin contexto
throw new DuplicatedDomainException("Duplicated");
throw new ResourceNotFoundException("Not found");
throw new InvalidFilterArgumentException("Invalid");
```

### ✅ DO: Usar Constructor Apropiado de InvalidDomainException

```csharp
// ✅ BIEN: Con ValidationFailures de entidad
if (!user.IsValid())
{
    throw new InvalidDomainException(user.Validate());
}

// ✅ BIEN: Con error específico
if (existingByCode != null && existingByCode.Id != id)
{
    throw new InvalidDomainException(
        property: "Code",
        errorCode: "Code_Duplicated",
        errorMessage: $"Another technical standard with code {code} already exists"
    );
}
```

### ✅ DO: Mapear a HTTP Status Correcto

```csharp
// ✅ BIEN: Status codes apropiados
catch (InvalidDomainException ex)
{
    await SendAsync(response, 400, ct); // Bad Request
}
catch (DuplicatedDomainException ex)
{
    await SendAsync(response, 409, ct); // Conflict
}
catch (ResourceNotFoundException ex)
{
    await SendAsync(response, 404, ct); // Not Found
}
catch (InvalidFilterArgumentException ex)
{
    await SendAsync(response, 400, ct); // Bad Request
}
```

### ❌ DON'T: Usar 500 para Errores de Negocio

```csharp
// ❌ MAL: 500 para error de validación
catch (InvalidDomainException ex)
{
    await SendAsync(response, 500, ct); // ¡NO! No es error de servidor
}

// ✅ BIEN: 400 para validación
catch (InvalidDomainException ex)
{
    await SendAsync(response, 400, ct); // Bad Request correcto
}
```

### ✅ DO: Documentar Excepciones en XML Comments

```csharp
/// <summary>
/// Creates a new user with the specified email and name.
/// </summary>
/// <param name="email">The user's email address</param>
/// <param name="name">The user's full name</param>
/// <returns>The created user entity</returns>
/// <exception cref="DuplicatedDomainException">Thrown when a user with the same email already exists</exception>
/// <exception cref="InvalidDomainException">Thrown when the user entity fails validation</exception>
Task<User> CreateAsync(string email, string name);
```

---

## Checklist para Nuevas Excepciones

Si necesitas crear una nueva Domain Exception en APSYS, sigue esta checklist:

### 1. Evaluar Necesidad

- [ ] ¿Es realmente excepcional? (¿No es parte del flujo de negocio esperado?)
- [ ] ¿Requiere rollback de transacción?
- [ ] ¿No puede manejarse con Result<T>?
- [ ] ¿Es error de dominio, no de infraestructura?

### 2. Decidir Tipo de Excepción

- [ ] ¿Es error de validación? → `InvalidDomainException`
- [ ] ¿Es duplicado? → `DuplicatedDomainException`
- [ ] ¿Es recurso no encontrado? → `ResourceNotFoundException`
- [ ] ¿Es argumento de query inválido? → `InvalidFilterArgumentException`
- [ ] ¿Es nuevo tipo? → Crear nueva clase

### 3. Crear Nueva Excepción (si es necesario)

- [ ] Crear clase en `Domain/exceptions/{NombreExcepcion}.cs`
- [ ] Heredar de `Exception` o `ArgumentException` según corresponda
- [ ] Agregar XML comments con `<summary>`
- [ ] Implementar constructores apropiados
- [ ] Documentar con `<exception>` tags en métodos que la lanzan

### 4. Usar Excepción Correctamente

- [ ] Lanzar en Repository/Service (capa de dominio)
- [ ] Mensaje descriptivo con contexto
- [ ] No lanzar en flujos de negocio esperados
- [ ] Documentar en XML comments del método

### 5. Manejar en Endpoints

- [ ] Catch específico para la excepción
- [ ] Rollback de transacción en catch
- [ ] Mapear a HTTP status code correcto
- [ ] Retornar mensaje apropiado al cliente

### Ejemplo Completo: Nueva Excepción

```csharp
// 1. Crear excepción: Domain/exceptions/PermissionDeniedException.cs
namespace {proyecto}.domain.exceptions;

/// <summary>
/// Exception thrown when a user attempts an operation without sufficient permissions.
/// </summary>
public class PermissionDeniedException : Exception
{
    /// <summary>
    /// Gets the required permission that was missing.
    /// </summary>
    public string RequiredPermission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionDeniedException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="requiredPermission">The permission that was required</param>
    public PermissionDeniedException(string message, string requiredPermission) : base(message)
    {
        RequiredPermission = requiredPermission;
    }
}

// 2. Usar en Repository/Service
/// <summary>
/// Deletes a user from the system.
/// </summary>
/// <param name="userId">The ID of the user to delete</param>
/// <param name="currentUserId">The ID of the user performing the operation</param>
/// <exception cref="ResourceNotFoundException">Thrown when the user is not found</exception>
/// <exception cref="PermissionDeniedException">Thrown when the current user lacks delete permissions</exception>
public async Task DeleteUserAsync(Guid userId, Guid currentUserId)
{
    var user = await GetAsync(userId);
    if (user == null)
    {
        throw new ResourceNotFoundException($"User with ID {userId} not found");
    }

    var currentUser = await GetAsync(currentUserId);
    if (!currentUser.HasPermission("DeleteUser"))
    {
        throw new PermissionDeniedException(
            "You do not have permission to delete users",
            "DeleteUser"
        );
    }

    await DeleteAsync(user);
}

// 3. Manejar en Endpoint
try
{
    _unitOfWork.BeginTransaction();
    await _unitOfWork.Users.DeleteUserAsync(req.UserId, req.CurrentUserId);
    _unitOfWork.Commit();

    await SendOkAsync(ct);
}
catch (PermissionDeniedException ex)
{
    _unitOfWork.Rollback();
    await SendAsync(new ErrorResponse
    {
        Message = ex.Message,
        RequiredPermission = ex.RequiredPermission
    }, 403, ct); // Forbidden
}
catch (ResourceNotFoundException ex)
{
    _unitOfWork.Rollback();
    await SendAsync(new ErrorResponse { Message = ex.Message }, 404, ct);
}
```

---

## Recursos Adicionales

- **FluentValidation**: https://docs.fluentvalidation.net/
- **FluentResults**: https://github.com/altmann/FluentResults
- **Exception Best Practices**: https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions
- **HTTP Status Codes**: https://developer.mozilla.org/en-US/docs/Web/HTTP/Status

---

**Guía creada para APSYS Backend Development**
Basada en el proyecto: `hashira.stone.backend`
Stack: .NET 9.0, C# 13, FluentValidation 12.0, FluentResults 4.0, FastEndpoints 7.0
