# Error Responses

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-11-15

## Descripción General

Esta guía documenta los patrones y mejores prácticas para el manejo de respuestas de error en la capa WebApi usando **FastEndpoints 7.0.1**. Cubre códigos de estado HTTP, formato de errores, integración con FluentResults, y el uso de ProblemDetails según los estándares RFC7807/RFC9457.

## Tabla de Contenidos

1. [HTTP Status Codes](#http-status-codes)
2. [Métodos de Envío de Errores](#métodos-de-envío-de-errores)
3. [BaseEndpoint - Helpers de Errores](#baseendpoint---helpers-de-errores)
4. [Patrones de Manejo de Errores](#patrones-de-manejo-de-errores)
5. [Integration con FluentResults](#integration-con-fluentresults)
6. [Excepciones del Dominio](#excepciones-del-dominio)
7. [ProblemDetails (RFC7807/RFC9457)](#problemdetails-rfc7807rfc9457)
8. [Errores de Validación](#errores-de-validación)
9. [Errores de Lógica de Negocio](#errores-de-lógica-de-negocio)
10. [Logging de Errores](#logging-de-errores)
11. [Mejores Prácticas](#mejores-prácticas)
12. [Anti-patrones](#anti-patrones)

---

## HTTP Status Codes

### Códigos de Estado Comunes

FastEndpoints sigue las convenciones estándar de HTTP para códigos de estado:

#### 2xx - Success

```csharp
// 200 OK - Operación exitosa
await Send.OkAsync(response, ct);

// 201 Created - Recurso creado exitosamente
await Send.CreatedAtAsync($"/users/{userId}", new[] { userId }, response, false, ct);

// 204 No Content - Operación exitosa sin contenido
await Send.NoContentAsync(ct);
```

#### 4xx - Client Errors

```csharp
// 400 Bad Request - Validación fallida
await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);

// 401 Unauthorized - No autenticado
await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, ct);

// 403 Forbidden - No autorizado
await Send.ErrorsAsync(StatusCodes.Status403Forbidden, ct);

// 404 Not Found - Recurso no encontrado
await Send.ErrorsAsync(StatusCodes.Status404NotFound, ct);

// 409 Conflict - Conflicto (ej. duplicado)
await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
```

#### 5xx - Server Errors

```csharp
// 500 Internal Server Error - Error inesperado del servidor
await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);

// 503 Service Unavailable - Servicio no disponible
await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
```

### Mapeo de Excepciones a Status Codes

El proyecto usa este mapeo estándar:

| Excepción del Dominio | Status Code | Descripción |
|----------------------|-------------|-------------|
| `InvalidDomainException` | 400 Bad Request | Validación de dominio fallida |
| `DuplicatedDomainException` | 409 Conflict | Recurso duplicado |
| `ResourceNotFoundException` | 404 Not Found | Recurso no encontrado |
| `ArgumentException` | 409 Conflict | Argumento inválido |
| `HttpRequestException` | 500 Internal Server Error | Error en llamada HTTP externa |
| Excepciones no manejadas | 500 Internal Server Error | Errores inesperados |

---

## Métodos de Envío de Errores

### AddError - Agregar Errores

FastEndpoints proporciona el método `AddError()` para acumular errores de validación:

```csharp
// Error general
AddError("An unexpected error occurred.");

// Error asociado a una propiedad específica
AddError(r => r.EmailAddress, "This email is already in use!");

// Error con código personalizado
ValidationFailures.Add(new ValidationFailure
{
    PropertyName = "EmailAddress",
    ErrorMessage = "Email already exists",
    ErrorCode = "EMAIL_EXISTS"
});
```

### Send.ErrorsAsync - Enviar Respuesta de Error

El método principal para enviar respuestas de error:

```csharp
// Error simple con status code
AddError("User not found");
await Send.ErrorsAsync(StatusCodes.Status404NotFound, ct);

// Múltiples errores
AddError(r => r.FirstName, "First name is required");
AddError(r => r.LastName, "Last name is required");
await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
```

### ThrowIfAnyErrors y ThrowError

Métodos de conveniencia para validación:

```csharp
// Verifica si hay errores y lanza excepción
AddError(r => r.Age, "Age must be greater than 18");
ThrowIfAnyErrors(); // Lanza ValidationFailureException si hay errores

// Lanza error inmediatamente
if (userId == null)
    ThrowError("Creating a user did not go so well!");
```

---

## BaseEndpoint - Helpers de Errores

### Clase BaseEndpoint

El proyecto implementa una clase base `BaseEndpoint<TRequest, TResponse>` con helpers para manejo consistente de errores:

```csharp
using FastEndpoints;
using FluentResults;
using System.Linq.Expressions;
using System.Net;

namespace hashira.stone.backend.webapi.features;

/// <summary>
/// Base endpoint with helpers for error handling.
/// </summary>
public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    /// <summary>
    /// Helper for property-based error handling.
    /// </summary>
    protected async Task HandleErrorAsync(
        Expression<Func<TRequest, object?>> property,
        string message,
        HttpStatusCode status,
        CancellationToken ct)
    {
        this.Logger.LogWarning(message);
        AddError(property, message);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }

    /// <summary>
    /// Helper for unexpected error handling.
    /// </summary>
    protected async Task HandleUnexpectedErrorAsync(
        IError? error,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        if (error != null && error.Metadata != null && error.Metadata.TryGetValue("Exception", out var exObj))
        {
            if (exObj is Exception ex)
                this.Logger.LogError(ex, UnexpectedErrorMessage);
            else
                this.Logger.LogError(UnexpectedErrorMessage);
        }
        else
            this.Logger.LogError(UnexpectedErrorMessage);

        AddError(UnexpectedErrorMessage);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }

    /// <summary>
    /// Helper for unexpected error handling with custom message.
    /// </summary>
    protected async Task HandleErrorWithMessageAsync(
        IError? error,
        string message,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        if (error != null && error.Metadata != null && error.Metadata.TryGetValue("Exception", out var exObj))
        {
            if (exObj is Exception ex)
                this.Logger.LogError(ex, string.IsNullOrEmpty(message) ? UnexpectedErrorMessage : message);
            else
                this.Logger.LogError(string.IsNullOrEmpty(message) ? UnexpectedErrorMessage : message);
        }
        else
            this.Logger.LogError(string.IsNullOrEmpty(message) ? UnexpectedErrorMessage : message);

        AddError(string.IsNullOrEmpty(message) ? UnexpectedErrorMessage : message);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }

    /// <summary>
    /// Handles unexpected errors from exceptions.
    /// </summary>
    protected async Task HandleUnexpectedErrorAsync(
        Exception? ex,
        CancellationToken ct,
        HttpStatusCode status = HttpStatusCode.InternalServerError)
    {
        if (ex != null)
            this.Logger.LogError(ex, UnexpectedErrorMessage);
        else
            this.Logger.LogError(UnexpectedErrorMessage);

        AddError(UnexpectedErrorMessage);
        await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
    }
}
```

### Beneficios de BaseEndpoint

1. **Logging consistente**: Todos los errores se registran automáticamente
2. **Código DRY**: Evita repetir lógica de manejo de errores
3. **Tipado fuerte**: Usa expresiones lambda para referencias a propiedades
4. **Integración con FluentResults**: Maneja `IError` de FluentResults
5. **Status codes parametrizables**: Flexibilidad en códigos HTTP

---

## Patrones de Manejo de Errores

### Patrón 1: Switch con Tipos de Error FluentResults

**Usado para**: Errores del dominio usando custom Error types

```csharp
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Get("/users/{UserName}");
        Description(d => d
            .WithTags("Users")
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        Policies("MustBeApplicationUser");
    }

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var command = new GetUserUseCase.Command { UserName = req.UserName };
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case UserNotFoundError e:
                    await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
                    break;
                default:
                    await HandleUnexpectedErrorAsync(error, ct);
                    break;
            }
            return;
        }

        var response = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

**Características**:
- Usa tipos de error personalizados (`UserNotFoundError`)
- Switch basado en tipo permite manejo específico
- Código limpio y fácil de leer
- Helper methods de `BaseEndpoint`

### Patrón 2: Switch con ExceptionalError

**Usado para**: Excepciones del dominio envueltas en FluentResults

```csharp
public class UpdateTechnicalStandardEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<UpdateTechnicalStandardModel.Request, UpdateTechnicalStandardModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Put("/technical-standards/{Id}");
        Policies("MustBeApplicationUser");
        Summary(s =>
        {
            s.Summary = "Update an existing technical standard";
            s.Response(200, "Technical standard updated");
            s.Response(404, "Technical standard not found");
            s.Response(400, "Invalid data");
            s.Response(409, "Duplicate code");
        });
    }

    public override async Task HandleAsync(UpdateTechnicalStandardModel.Request req, CancellationToken ct)
    {
        try
        {
            var command = _mapper.Map<UpdateTechnicalStandardUseCase.Command>(req);
            var result = await command.ExecuteAsync(ct);

            if (result.IsSuccess)
            {
                var response = _mapper.Map<UpdateTechnicalStandardModel.Response>(result.Value);
                await Send.OkAsync(response, ct);
                return;
            }

            var error = result.Errors.FirstOrDefault();
            var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

            switch (errorType?.Exception)
            {
                case ArgumentException:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
                    break;
                case HttpRequestException:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.InternalServerError);
                    break;
                case InvalidDomainException:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
                    break;
                case DuplicatedDomainException:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
                    break;
                case ResourceNotFoundException:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.NotFound);
                    break;
                default:
                    await HandleErrorWithMessageAsync(error, error?.Message ?? "Unknown error", ct, HttpStatusCode.InternalServerError);
                    break;
            }
        }
        catch (Exception ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
        }
    }
}
```

**Características**:
- Maneja excepciones envueltas en `ExceptionalError`
- Switch basado en tipo de excepción
- Try-catch como fallback de seguridad
- Mapeo consistente excepción → status code

### Patrón 3: If-Else con ExceptionalError

**Usado para**: Endpoints con pocas excepciones posibles

```csharp
public class CreateUserEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreateUserModel.Request, CreateUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Post("/users");
        Description(b => b
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
        DontThrowIfValidationFails();
        Policies("MustBeApplicationAdministrator");
    }

    public override async Task HandleAsync(CreateUserModel.Request request, CancellationToken ct)
    {
        var command = this._mapper.Map<CreateUserUseCase.Command>(request);
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();

            // Check for ArgumentException
            if (error?.Reasons.OfType<ExceptionalError>().Any(r => r.Exception is ArgumentException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                return;
            }

            // Check for HttpRequestException
            if (error?.Reasons.OfType<ExceptionalError>().Any(r => r.Exception is HttpRequestException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
                return;
            }

            // Check for InvalidDomainException
            if (error?.Reasons.OfType<ExceptionalError>().Any(r => r.Exception is InvalidDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return;
            }

            // Check for DuplicatedDomainException
            if (error?.Reasons.OfType<ExceptionalError>().Any(r => r.Exception is DuplicatedDomainException) == true)
            {
                AddError(error.Message);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                return;
            }

            // Default error
            AddError(error?.Message ?? "Unknown error");
            await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
            return;
        }

        var userResponse = this._mapper.Map<CreateUserModel.Response>(result.Value);
        await Send.CreatedAtAsync($"/users/{userResponse.User.Id}", new[] { userResponse.User.Id }, userResponse, false, ct);
    }
}
```

**Características**:
- If-else encadenado para cada tipo de excepción
- Return inmediato después de manejar error
- Verbose pero muy explícito
- Fácil de entender para desarrolladores junior

---

## Integration con FluentResults

### Custom Error Types

El proyecto define errores personalizados heredando de `FluentResults.Error`:

```csharp
using FluentResults;

namespace hashira.stone.backend.domain.errors;

public static class UserErrors
{
    /// <summary>
    /// Represents an error indicating that a user with the specified username was not found.
    /// </summary>
    public static UserNotFoundError UserNotFound(string userName)
    {
        return new UserNotFoundError(userName);
    }
}

/// <summary>
/// Represents an error indicating that a user was not found.
/// </summary>
public class UserNotFoundError : Error
{
    public UserNotFoundError(string userName)
        : base($"User '{userName}' not found.")
    {
    }
}
```

### Uso en Use Cases

Los Use Cases retornan `Result<T>` o `Result`:

```csharp
public class GetUserUseCase
{
    public class Command : ICommand
    {
        public string UserName { get; set; } = string.Empty;

        public async Task<Result<User>> ExecuteAsync(CancellationToken ct = default)
        {
            var user = await _repository.GetByUserNameAsync(UserName, ct);

            if (user == null)
                return Result.Fail(UserErrors.UserNotFound(UserName));

            return Result.Ok(user);
        }
    }
}
```

### Manejo en Endpoints

```csharp
var result = await command.ExecuteAsync(ct);

if (result.IsFailed)
{
    var error = result.Errors[0];

    // Pattern matching con custom error types
    switch (error)
    {
        case UserNotFoundError e:
            await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
            break;
        default:
            await HandleUnexpectedErrorAsync(error, ct);
            break;
    }
    return;
}

// Success path
var response = _mapper.Map<GetUserModel.Response>(result.Value);
await Send.OkAsync(response, ct);
```

### ExceptionalError - Wrapping Exceptions

FluentResults proporciona `ExceptionalError` para envolver excepciones:

```csharp
try
{
    var entity = await _repository.CreateAsync(data, ct);
    return Result.Ok(entity);
}
catch (InvalidDomainException ex)
{
    return Result.Fail(new ExceptionalError(ex));
}
catch (DuplicatedDomainException ex)
{
    return Result.Fail(new ExceptionalError(ex));
}
```

Extracción en endpoints:

```csharp
var error = result.Errors.FirstOrDefault();
var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

switch (errorType?.Exception)
{
    case InvalidDomainException:
        await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
        break;
    case DuplicatedDomainException:
        await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
        break;
}
```

---

## Excepciones del Dominio

### InvalidDomainException

Representa errores de validación del dominio:

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
    /// Constructor with validation failures
    /// </summary>
    public InvalidDomainException(IEnumerable<ValidationFailure> validationFailures)
    {
        this.ValidationFailures = validationFailures;
    }

    /// <summary>
    /// Constructor with single error
    /// </summary>
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
    /// Get error message as JSON
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

**Uso**:

```csharp
var validator = entity.GetValidator();
var validationResult = await validator.ValidateAsync(entity, ct);

if (!validationResult.IsValid)
    throw new InvalidDomainException(validationResult.Errors);
```

**Mapeo**: 400 Bad Request

### DuplicatedDomainException

Indica que un recurso ya existe:

```csharp
namespace hashira.stone.backend.domain.exceptions;

/// <summary>
/// Duplicated domain exception class
/// </summary>
public class DuplicatedDomainException : Exception
{
    public DuplicatedDomainException(string message) : base(message)
    {
    }
}
```

**Uso**:

```csharp
var existingUser = await _repository.FindByEmailAsync(email, ct);
if (existingUser != null)
    throw new DuplicatedDomainException($"User with email '{email}' already exists.");
```

**Mapeo**: 409 Conflict

### ResourceNotFoundException

Indica que un recurso solicitado no se encontró:

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

**Uso**:

```csharp
var entity = await _repository.GetByIdAsync(id, ct);
if (entity == null)
    throw new ResourceNotFoundException($"Technical standard with ID '{id}' not found.");
```

**Mapeo**: 404 Not Found

---

## ProblemDetails (RFC7807/RFC9457)

### Configuración

FastEndpoints puede generar respuestas compatibles con RFC7807/RFC9457:

```csharp
// En Program.cs
app.UseFastEndpoints(x => x.Errors.UseProblemDetails());
```

### Formato de Respuesta

```json
{
  "type": "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/users/create",
  "traceId": "0HMPNHL0JHL76:00000001",
  "errors": [
    {
      "name": "EmailAddress",
      "reason": "Email address is already in use!"
    },
    {
      "name": "Age",
      "reason": "Age must be greater than 18!"
    }
  ]
}
```

### Customización de ProblemDetails

```csharp
app.UseFastEndpoints(
    c => c.Errors.UseProblemDetails(
        x =>
        {
            x.AllowDuplicateErrors = true;  // Permite errores duplicados
            x.IndicateErrorCode = true;     // Serializa el error code de FluentValidation
            x.IndicateErrorSeverity = true; // Serializa la severidad del error
            x.TypeValue = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1";
            x.TitleValue = "One or more validation errors occurred.";
            x.TitleTransformer = pd => pd.Status switch
            {
                400 => "Validation Error",
                404 => "Not Found",
                409 => "Conflict",
                _ => "One or more errors occurred!"
            };
        }));
```

### Documentar ProblemDetails en Swagger

```csharp
public override void Configure()
{
    Post("/users");
    Description(b => b
        .Produces<UserDto>(StatusCodes.Status201Created)
        .ProducesProblemDetails(StatusCodes.Status400BadRequest)
        .ProducesProblemDetails(StatusCodes.Status409Conflict)
        .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
}
```

---

## Errores de Validación

### Validación de Request

FastEndpoints valida automáticamente el request usando FluentValidation:

```csharp
public class CreateUserValidator : Validator<CreateUserModel.Request>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be valid");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("Age must be greater than 0")
            .LessThan(150).WithMessage("Age must be less than 150");
    }
}
```

**Respuesta automática** (400 Bad Request):

```json
{
  "statusCode": 400,
  "message": "One or more errors occurred!",
  "errors": {
    "Name": [
      "Name is required"
    ],
    "Email": [
      "Email is required"
    ]
  }
}
```

### Deshabilitar Validación Automática

Si necesitas control manual:

```csharp
public override void Configure()
{
    Post("/users");
    DontThrowIfValidationFails(); // No lanzar automáticamente
}

public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    if (ValidationFailed)
    {
        foreach (var failure in ValidationFailures)
        {
            Logger.LogWarning($"Validation failed: {failure.PropertyName} - {failure.ErrorMessage}");
        }
        await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
        return;
    }

    // Continue with business logic
}
```

---

## Errores de Lógica de Negocio

### Validación en el Endpoint

```csharp
public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    // Validación de negocio: Email ya existe
    var existingUser = await _userRepository.FindByEmailAsync(req.Email, ct);
    if (existingUser != null)
    {
        AddError(r => r.Email, "This email is already in use!");
        await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
        return;
    }

    // Validación de negocio: Edad máxima
    var maxAge = await _userRepository.GetMaxAllowedAgeAsync(ct);
    if (req.Age >= maxAge)
    {
        AddError(r => r.Age, "You are not eligible for insurance!");
        await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
        return;
    }

    // Continue...
}
```

### Validación en el Use Case

**Preferido**: Mantener lógica de negocio en Application Layer

```csharp
public class CreateUserUseCase
{
    public class Command : ICommand
    {
        public async Task<Result<User>> ExecuteAsync(CancellationToken ct = default)
        {
            // Check for duplicate email
            var existingUser = await _repository.FindByEmailAsync(Email, ct);
            if (existingUser != null)
                return Result.Fail(new Error("User with this email already exists."));

            // Domain validation
            var user = new User(Name, Email, Age);
            var validator = user.GetValidator();
            var validationResult = await validator.ValidateAsync(user, ct);

            if (!validationResult.IsValid)
                return Result.Fail(new ExceptionalError(new InvalidDomainException(validationResult.Errors)));

            await _repository.CreateAsync(user, ct);
            await _unitOfWork.CommitAsync(ct);

            return Result.Ok(user);
        }
    }
}
```

Endpoint simplificado:

```csharp
public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    var command = _mapper.Map<CreateUserUseCase.Command>(req);
    var result = await command.ExecuteAsync(ct);

    if (result.IsFailed)
    {
        var error = result.Errors.FirstOrDefault();
        var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

        switch (errorType?.Exception)
        {
            case InvalidDomainException:
                await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
                break;
            case DuplicatedDomainException:
                await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
                break;
            default:
                await HandleErrorWithMessageAsync(error, error?.Message ?? "Unknown error", ct);
                break;
        }
        return;
    }

    var response = _mapper.Map<CreateUserModel.Response>(result.Value);
    await Send.CreatedAtAsync($"/users/{response.User.Id}", new[] { response.User.Id }, response, false, ct);
}
```

---

## Logging de Errores

### Logging en BaseEndpoint

Los helper methods de `BaseEndpoint` automáticamente registran errores:

```csharp
protected async Task HandleErrorAsync(
    Expression<Func<TRequest, object?>> property,
    string message,
    HttpStatusCode status,
    CancellationToken ct)
{
    this.Logger.LogWarning(message); // ← Logging automático
    AddError(property, message);
    await Send.ErrorsAsync(statusCode: (int)status, cancellation: ct);
}
```

### Niveles de Log

| Tipo de Error | Nivel de Log | Uso |
|--------------|-------------|-----|
| Validación (400) | `LogWarning` | Errores esperados del cliente |
| Not Found (404) | `LogWarning` | Recurso no encontrado |
| Conflict (409) | `LogWarning` | Duplicados, conflictos esperados |
| Server Error (500) | `LogError` | Errores inesperados del servidor |

### Logging Manual

```csharp
public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
{
    try
    {
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            Logger.LogWarning("Failed to get user {UserName}: {Error}",
                req.UserName,
                result.Errors.FirstOrDefault()?.Message);

            await Send.ErrorsAsync(StatusCodes.Status404NotFound, ct);
            return;
        }

        await Send.OkAsync(response, ct);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Unexpected error while getting user {UserName}", req.UserName);
        AddError("An unexpected error occurred.");
        await Send.ErrorsAsync(StatusCodes.Status500InternalServerError, ct);
    }
}
```

### Structured Logging

```csharp
Logger.LogError(ex,
    "Failed to create user. Email: {Email}, Age: {Age}",
    request.Email,
    request.Age);
```

---

## Mejores Prácticas

### 1. Usar BaseEndpoint para Consistencia

✅ **Hacer**:
```csharp
public class GetUserEndpoint : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            var error = result.Errors[0];
            switch (error)
            {
                case UserNotFoundError e:
                    await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
                    break;
            }
            return;
        }
    }
}
```

❌ **No hacer**:
```csharp
public class GetUserEndpoint : Endpoint<GetUserModel.Request, GetUserModel.Response>
{
    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed)
        {
            // Duplicando lógica en cada endpoint
            AddError(req.UserName, "User not found");
            await Send.ErrorsAsync(404, ct);
            return;
        }
    }
}
```

### 2. Documentar Errores en Swagger

✅ **Hacer**:
```csharp
public override void Configure()
{
    Post("/users");
    Description(b => b
        .Produces<UserDto>(StatusCodes.Status201Created)
        .ProducesProblemDetails(StatusCodes.Status400BadRequest)
        .ProducesProblemDetails(StatusCodes.Status409Conflict)
        .ProducesProblemDetails(StatusCodes.Status500InternalServerError));
}
```

### 3. Mapear Excepciones Consistentemente

✅ **Hacer**: Usar el mismo mapeo en todos los endpoints
```csharp
switch (errorType?.Exception)
{
    case InvalidDomainException:
        await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
        break;
    case DuplicatedDomainException:
        await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
        break;
    case ResourceNotFoundException:
        await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.NotFound);
        break;
}
```

### 4. No Exponer Detalles Internos

❌ **No hacer**:
```csharp
catch (Exception ex)
{
    AddError(ex.ToString()); // Expone stack trace
    await Send.ErrorsAsync(500, ct);
}
```

✅ **Hacer**:
```csharp
catch (Exception ex)
{
    Logger.LogError(ex, "Unexpected error");
    AddError("An unexpected error occurred."); // Mensaje genérico
    await Send.ErrorsAsync(500, ct);
}
```

### 5. Usar Custom Error Types para Casos Específicos

✅ **Hacer**:
```csharp
public class UserNotFoundError : Error
{
    public UserNotFoundError(string userName)
        : base($"User '{userName}' not found.")
    {
    }
}

// En el endpoint
switch (error)
{
    case UserNotFoundError e:
        await HandleErrorAsync(r => r.UserName, e.Message, HttpStatusCode.NotFound, ct);
        break;
}
```

### 6. Separar Validación de Request y Lógica de Negocio

✅ **Hacer**:
```csharp
// Validación de Request en Validator
public class CreateUserValidator : Validator<CreateUserModel.Request>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
    }
}

// Validación de negocio en Use Case
public class CreateUserUseCase
{
    public async Task<Result<User>> ExecuteAsync(CancellationToken ct)
    {
        var existingUser = await _repository.FindByEmailAsync(Email, ct);
        if (existingUser != null)
            return Result.Fail(new Error("Email already in use"));
        // ...
    }
}
```

### 7. Loggear con Información Contextual

✅ **Hacer**:
```csharp
Logger.LogError(ex,
    "Failed to create user. Email: {Email}, Name: {Name}",
    request.Email,
    request.Name);
```

❌ **No hacer**:
```csharp
Logger.LogError(ex, "Failed to create user");
```

---

## Anti-patrones

### 1. Catch-All sin Especificidad

❌ **Evitar**:
```csharp
try
{
    // ...
}
catch (Exception ex)
{
    // Maneja TODAS las excepciones igual
    AddError("Error");
    await Send.ErrorsAsync(500, ct);
}
```

✅ **Mejor**:
```csharp
var result = await command.ExecuteAsync(ct);

if (result.IsFailed)
{
    var error = result.Errors[0];
    switch (error)
    {
        case UserNotFoundError:
            await HandleErrorAsync(r => r.UserName, error.Message, HttpStatusCode.NotFound, ct);
            break;
        default:
            await HandleUnexpectedErrorAsync(error, ct);
            break;
    }
    return;
}
```

### 2. No Loggear Errores

❌ **Evitar**:
```csharp
if (result.IsFailed)
{
    AddError("User not found");
    await Send.ErrorsAsync(404, ct);
    return;
}
```

✅ **Mejor**:
```csharp
if (result.IsFailed)
{
    Logger.LogWarning("User {UserName} not found", req.UserName);
    AddError("User not found");
    await Send.ErrorsAsync(404, ct);
    return;
}
```

### 3. Códigos de Estado Incorrectos

❌ **Evitar**:
```csharp
// Usar 500 para validación
if (!validationResult.IsValid)
{
    AddError("Validation failed");
    await Send.ErrorsAsync(500, ct); // ❌ Debería ser 400
}
```

✅ **Mejor**:
```csharp
if (!validationResult.IsValid)
{
    AddError("Validation failed");
    await Send.ErrorsAsync(400, ct); // ✅ Correcto
}
```

### 4. Mensajes de Error No Descriptivos

❌ **Evitar**:
```csharp
AddError("Error");
AddError("Failed");
AddError("Invalid");
```

✅ **Mejor**:
```csharp
AddError(r => r.Email, "Email address is already in use");
AddError(r => r.Age, "Age must be between 18 and 120");
AddError(r => r.Name, "Name must be at least 3 characters");
```

### 5. Lógica de Negocio en el Endpoint

❌ **Evitar**:
```csharp
public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    // Mucha lógica de negocio en el endpoint
    var existingUser = await _repository.FindByEmailAsync(req.Email, ct);
    if (existingUser != null)
    {
        AddError("Email exists");
        await Send.ErrorsAsync(409, ct);
        return;
    }

    var user = new User { Name = req.Name, Email = req.Email };
    var validationResult = await validator.ValidateAsync(user);

    if (!validationResult.IsValid)
    {
        // ...
    }

    await _repository.CreateAsync(user, ct);
    await _unitOfWork.CommitAsync(ct);
}
```

✅ **Mejor**:
```csharp
public override async Task HandleAsync(CreateUserModel.Request req, CancellationToken ct)
{
    // Endpoint delgado, lógica en Use Case
    var command = _mapper.Map<CreateUserUseCase.Command>(req);
    var result = await command.ExecuteAsync(ct);

    if (result.IsFailed)
    {
        var error = result.Errors.FirstOrDefault();
        var errorType = error?.Reasons.OfType<ExceptionalError>().FirstOrDefault();

        switch (errorType?.Exception)
        {
            case InvalidDomainException:
                await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.BadRequest);
                break;
            case DuplicatedDomainException:
                await HandleErrorWithMessageAsync(error, error?.Message ?? "", ct, HttpStatusCode.Conflict);
                break;
        }
        return;
    }

    var response = _mapper.Map<CreateUserModel.Response>(result.Value);
    await Send.CreatedAtAsync($"/users/{response.User.Id}", new[] { response.User.Id }, response, false, ct);
}
```

---

## Referencias

### Documentación Oficial

- [FastEndpoints - Error Handling](https://fast-endpoints.com/docs/error-handling)
- [FastEndpoints - Validation](https://fast-endpoints.com/docs/validation)
- [FluentResults](https://github.com/altmann/FluentResults)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [RFC7807 - Problem Details](https://www.rfc-editor.org/rfc/rfc7807)
- [RFC9457 - Problem Details (Updated)](https://www.rfc-editor.org/rfc/rfc9457)

### Guías Relacionadas

- [fastendpoints-basics.md](fastendpoints-basics.md) - Estructura de endpoints
- [request-response-models.md](request-response-models.md) - Request/Response patterns
- [../application-layer/error-handling.md](../application-layer/error-handling.md) - Error handling en Application Layer
- [../domain-layer/domain-exceptions.md](../domain-layer/domain-exceptions.md) - Excepciones del dominio

---

**Versión:** 1.0.0
**Última actualización:** 2025-11-15
**Maintainers:** Equipo APSYS
