# Error Handling

**Estado:** ✅ Completado
**Versión:** 1.0.0

## Tabla de Contenido

1. [Introducción](#introducción)
2. [Result Pattern con FluentResults](#result-pattern-con-fluentresults)
3. [Cuándo Usar Excepciones vs Results](#cuándo-usar-excepciones-vs-results)
4. [Custom Exceptions en Domain](#custom-exceptions-en-domain)
5. [Error Propagation entre Capas](#error-propagation-entre-capas)
6. [Error Messages y Códigos](#error-messages-y-códigos)
7. [Logging de Errores](#logging-de-errores)
8. [Traducción a HTTP Status Codes](#traducción-a-http-status-codes)
9. [Anti-Patrones Comunes](#anti-patrones-comunes)
10. [Checklists](#checklists)
11. [Referencias](#referencias)

---

## Introducción

El manejo de errores es un aspecto crítico del desarrollo de software. Una estrategia bien definida permite:

- **Distinguir** entre errores esperados (validaciones, reglas de negocio) y errores inesperados (infraestructura, sistema)
- **Comunicar** claramente el problema al consumidor de la API
- **Diagnosticar** problemas en producción mediante logging estructurado
- **Mantener** la integridad del sistema mediante el manejo apropiado de excepciones

### Principios Fundamentales

1. **Result Pattern para errores esperados**: Validaciones, reglas de negocio, casos de "no encontrado"
2. **Exceptions para errores inesperados**: Fallas de infraestructura, errores de sistema
3. **Logging estructurado**: Usar ILogger con información contextual
4. **Nunca ocultar información**: Propagar errores apropiadamente a través de las capas

---

## Result Pattern con FluentResults

FluentResults proporciona un `Result<T>` type-safe para representar el éxito o fallo de una operación sin usar excepciones.

### Instalación

```xml
<PackageReference Include="FluentResults" Version="4.0.0" />
```

### Uso Básico

```csharp
namespace Application.UseCases.Users;

using FluentResults;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public class GetUserByEmailHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserByEmailHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // ✅ CORRECTO: Result para caso "no encontrado" (esperado)
    public async Task<Result<User>> Handle(string email, CancellationToken ct)
    {
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user == null)
        {
            return Result.Fail<User>($"Usuario con email '{email}' no encontrado");
        }

        return Result.Ok(user);
    }
}
```

### Result con Múltiples Errores

```csharp
namespace Application.UseCases.Users;

using FluentResults;
using FluentValidation;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserCommand> _validator;

    public CreateUserHandler(
        IUserRepository userRepository,
        IValidator<CreateUserCommand> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    // ✅ CORRECTO: Result para validaciones (esperadas)
    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Validar comando
        var validationResult = await _validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            return Result.Fail<User>(errors);
        }

        // Verificar email único
        var existingUser = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (existingUser != null)
        {
            return Result.Fail<User>("El email ya está registrado");
        }

        // Crear usuario
        var user = new User
        {
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        await _userRepository.SaveOrUpdateAsync(user, ct);

        return Result.Ok(user);
    }
}
```

### Result con Custom Error Types

```csharp
namespace Application.Common.Results;

using FluentResults;

// ✅ CORRECTO: Custom error con metadata
public class NotFoundError : Error
{
    public string EntityType { get; }
    public object EntityId { get; }

    public NotFoundError(string entityType, object entityId)
        : base($"{entityType} con ID '{entityId}' no encontrado")
    {
        EntityType = entityType;
        EntityId = entityId;
        Metadata.Add("ErrorCode", "NOT_FOUND");
        Metadata.Add("EntityType", entityType);
        Metadata.Add("EntityId", entityId.ToString());
    }
}

public class ValidationError : Error
{
    public Dictionary<string, string[]> ValidationErrors { get; }

    public ValidationError(Dictionary<string, string[]> errors)
        : base("Error de validación")
    {
        ValidationErrors = errors;
        Metadata.Add("ErrorCode", "VALIDATION_ERROR");
        Metadata.Add("ValidationErrors", errors);
    }
}

public class BusinessRuleError : Error
{
    public string RuleCode { get; }

    public BusinessRuleError(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
        Metadata.Add("ErrorCode", "BUSINESS_RULE_VIOLATION");
        Metadata.Add("RuleCode", ruleCode);
    }
}
```

### Uso de Custom Errors

```csharp
namespace Application.UseCases.Orders;

using FluentResults;
using Application.Common.Results;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public class PlaceOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;

    public PlaceOrderHandler(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
    }

    public async Task<Result<Order>> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        // Verificar usuario existe
        var user = await _userRepository.GetByIdAsync(command.UserId, ct);
        if (user == null)
        {
            return Result.Fail<Order>(
                new NotFoundError("User", command.UserId));
        }

        // Verificar productos existen
        var products = new List<Product>();
        foreach (var item in command.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, ct);
            if (product == null)
            {
                return Result.Fail<Order>(
                    new NotFoundError("Product", item.ProductId));
            }

            // Verificar stock disponible
            if (product.Stock < item.Quantity)
            {
                return Result.Fail<Order>(
                    new BusinessRuleError(
                        "INSUFFICIENT_STOCK",
                        $"Producto '{product.Name}' no tiene stock suficiente. Disponible: {product.Stock}, Solicitado: {item.Quantity}"));
            }

            products.Add(product);
        }

        // Crear orden
        var order = new Order
        {
            UserId = command.UserId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        // Agregar items y reducir stock
        for (int i = 0; i < command.Items.Count; i++)
        {
            var item = command.Items[i];
            var product = products[i];

            order.AddItem(product, item.Quantity, product.Price);
            product.ReduceStock(item.Quantity);

            await _productRepository.SaveOrUpdateAsync(product, ct);
        }

        await _orderRepository.SaveOrUpdateAsync(order, ct);

        return Result.Ok(order);
    }
}
```

---

## Cuándo Usar Excepciones vs Results

### Tabla de Decisión

| Escenario | Usar | Razón |
|-----------|------|-------|
| Validación de entrada | `Result<T>` | Error esperado, parte del flujo normal |
| Regla de negocio violada | `Result<T>` | Error esperado, parte del dominio |
| Recurso no encontrado | `Result<T>` | Caso esperado, no es excepcional |
| Email duplicado | `Result<T>` | Validación de unicidad, esperada |
| Stock insuficiente | `Result<T>` | Regla de negocio, esperada |
| Falla de conexión a BD | `Exception` | Error inesperado de infraestructura |
| Timeout de red | `Exception` | Error inesperado de infraestructura |
| Archivo no encontrado (config) | `Exception` | Error inesperado de sistema |
| División por cero | `Exception` | Error de programación |
| ArgumentNullException | `Exception` | Error de programación |

### Ejemplo: Excepciones para Errores Inesperados

```csharp
namespace Infrastructure.Persistence.Repositories;

using NHibernate;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class UserRepository : IUserRepository
{
    private readonly ISession _session;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ISession session, ILogger<UserRepository> logger)
    {
        _session = session;
        _logger = logger;
    }

    // ✅ CORRECTO: Dejar que excepciones de infraestructura se propaguen
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await _session.GetAsync<User>(id, ct);
        }
        catch (Exception ex)
        {
            // Log y re-throw para excepciones de infraestructura
            _logger.LogError(ex,
                "Error al obtener usuario {UserId} de la base de datos",
                id);
            throw;
        }
    }

    public async Task SaveOrUpdateAsync(User user, CancellationToken ct)
    {
        try
        {
            await _session.SaveOrUpdateAsync(user, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al guardar usuario {UserId} en la base de datos",
                user.Id);
            throw;
        }
    }
}
```

### Ejemplo: Results para Errores Esperados

```csharp
namespace Application.UseCases.Products;

using FluentResults;
using Application.Common.Results;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public class UpdateProductStockHandler
{
    private readonly IProductRepository _productRepository;

    public UpdateProductStockHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // ✅ CORRECTO: Result para validaciones y reglas de negocio
    public async Task<Result<Product>> Handle(
        Guid productId,
        int newStock,
        CancellationToken ct)
    {
        // Validación de entrada
        if (newStock < 0)
        {
            return Result.Fail<Product>(
                new ValidationError(new Dictionary<string, string[]>
                {
                    ["Stock"] = new[] { "El stock no puede ser negativo" }
                }));
        }

        // Recurso no encontrado (esperado)
        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product == null)
        {
            return Result.Fail<Product>(
                new NotFoundError("Product", productId));
        }

        // Actualizar stock
        product.UpdateStock(newStock);

        // La excepción de SaveOrUpdateAsync se propagará (infraestructura)
        await _productRepository.SaveOrUpdateAsync(product, ct);

        return Result.Ok(product);
    }
}
```

---

## Custom Exceptions en Domain

Las custom exceptions se usan para errores inesperados específicos del dominio que representan violaciones graves de invariantes.

### Definición de Custom Exceptions

```csharp
namespace Domain.Exceptions;

using System;

// ✅ CORRECTO: Excepción base del dominio
[Serializable]
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string errorCode, string message, Exception inner)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}

// ✅ CORRECTO: Excepción para invariante violada
[Serializable]
public class InvariantViolationException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public InvariantViolationException(
        string entityType,
        object entityId,
        string message)
        : base("INVARIANT_VIOLATION", message)
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public InvariantViolationException(
        string entityType,
        object entityId,
        string message,
        Exception inner)
        : base("INVARIANT_VIOLATION", message, inner)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

// ✅ CORRECTO: Excepción para estado inválido
[Serializable]
public class InvalidStateTransitionException : DomainException
{
    public string CurrentState { get; }
    public string TargetState { get; }

    public InvalidStateTransitionException(
        string currentState,
        string targetState,
        string message)
        : base("INVALID_STATE_TRANSITION", message)
    {
        CurrentState = currentState;
        TargetState = targetState;
    }
}
```

### Uso de Custom Exceptions en Entities

```csharp
namespace Domain.Entities;

using Domain.Exceptions;

public class Order
{
    public virtual Guid Id { get; protected set; }
    public virtual Guid UserId { get; set; }
    public virtual OrderStatus Status { get; protected set; }
    public virtual DateTime OrderDate { get; set; }
    public virtual IList<OrderItem> Items { get; protected set; } = new List<OrderItem>();

    // ✅ CORRECTO: Throw exception para invariante crítica
    public virtual void Cancel()
    {
        // Solo se puede cancelar si está Pending
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidStateTransitionException(
                Status.ToString(),
                OrderStatus.Cancelled.ToString(),
                $"No se puede cancelar una orden en estado {Status}. Solo se pueden cancelar órdenes en estado Pending.");
        }

        Status = OrderStatus.Cancelled;
    }

    // ✅ CORRECTO: Throw exception para invariante crítica
    public virtual void Ship()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidStateTransitionException(
                Status.ToString(),
                OrderStatus.Shipped.ToString(),
                $"No se puede enviar una orden en estado {Status}. Solo se pueden enviar órdenes en estado Pending.");
        }

        if (!Items.Any())
        {
            throw new InvariantViolationException(
                nameof(Order),
                Id,
                "No se puede enviar una orden sin items");
        }

        Status = OrderStatus.Shipped;
    }

    // ✅ CORRECTO: Validación defensiva con ArgumentException
    public virtual void AddItem(Product product, int quantity, decimal price)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero", nameof(quantity));

        if (price < 0)
            throw new ArgumentException("El precio no puede ser negativo", nameof(price));

        var item = new OrderItem
        {
            OrderId = Id,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = price
        };

        Items.Add(item);
    }
}

public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}
```

### ❌ INCORRECTO: Usar Excepciones para Flujo Normal

```csharp
namespace Application.UseCases.Orders;

using Domain.Entities;
using Domain.Interfaces.Repositories;

// ❌ INCORRECTO: No usar excepciones para casos esperados
public class CancelOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task CancelOrder(Guid orderId, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);

        // ❌ INCORRECTO: Throw exception para "no encontrado"
        if (order == null)
        {
            throw new NotFoundException("Order", orderId);
        }

        order.Cancel(); // Esto puede throw InvalidStateTransitionException
        await _orderRepository.SaveOrUpdateAsync(order, ct);
    }
}
```

### ✅ CORRECTO: Usar Result para Flujo Normal

```csharp
namespace Application.UseCases.Orders;

using FluentResults;
using Application.Common.Results;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

// ✅ CORRECTO: Usar Result para casos esperados
public class CancelOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public CancelOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(Guid orderId, CancellationToken ct)
    {
        // ✅ Caso esperado: orden no encontrada
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
        {
            return Result.Fail(new NotFoundError("Order", orderId));
        }

        try
        {
            // La excepción del dominio se captura y convierte a Result
            order.Cancel();
        }
        catch (InvalidStateTransitionException ex)
        {
            // ✅ Convertir domain exception a Result
            return Result.Fail(new BusinessRuleError(
                ex.ErrorCode,
                ex.Message));
        }

        // La excepción de infraestructura se propaga
        await _orderRepository.SaveOrUpdateAsync(order, ct);

        return Result.Ok();
    }
}
```

---

## Error Propagation entre Capas

### Reglas de Propagación

1. **Domain → Application**: Application captura domain exceptions y las convierte a Results
2. **Application → WebApi**: WebApi mapea Results a HTTP responses
3. **Infrastructure exceptions**: Se propagan hasta el Exception Handler global

### Domain Layer

```csharp
namespace Domain.Entities;

using Domain.Exceptions;

public class BankAccount
{
    public virtual Guid Id { get; protected set; }
    public virtual decimal Balance { get; protected set; }
    public virtual bool IsActive { get; protected set; }

    // ✅ Domain: Throw exceptions para invariantes críticas
    public virtual void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (!IsActive)
            throw new InvariantViolationException(
                nameof(BankAccount),
                Id,
                "No se puede retirar de una cuenta inactiva");

        if (Balance < amount)
            throw new InvariantViolationException(
                nameof(BankAccount),
                Id,
                $"Saldo insuficiente. Saldo: {Balance}, Monto solicitado: {amount}");

        Balance -= amount;
    }

    public virtual void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(amount));

        if (!IsActive)
            throw new InvariantViolationException(
                nameof(BankAccount),
                Id,
                "No se puede depositar en una cuenta inactiva");

        Balance += amount;
    }
}
```

### Application Layer

```csharp
namespace Application.UseCases.BankAccounts;

using FluentResults;
using Application.Common.Results;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class WithdrawHandler
{
    private readonly IBankAccountRepository _accountRepository;
    private readonly ILogger<WithdrawHandler> _logger;

    public WithdrawHandler(
        IBankAccountRepository accountRepository,
        ILogger<WithdrawHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    // ✅ Application: Convierte domain exceptions a Results
    public async Task<Result<decimal>> Handle(
        Guid accountId,
        decimal amount,
        CancellationToken ct)
    {
        // Validación de entrada
        if (amount <= 0)
        {
            return Result.Fail<decimal>(
                new ValidationError(new Dictionary<string, string[]>
                {
                    ["Amount"] = new[] { "El monto debe ser mayor a cero" }
                }));
        }

        // Buscar cuenta
        var account = await _accountRepository.GetByIdAsync(accountId, ct);
        if (account == null)
        {
            return Result.Fail<decimal>(
                new NotFoundError("BankAccount", accountId));
        }

        try
        {
            // ✅ Capturar domain exceptions y convertir a Results
            account.Withdraw(amount);
            await _accountRepository.SaveOrUpdateAsync(account, ct);

            _logger.LogInformation(
                "Retiro exitoso. Cuenta: {AccountId}, Monto: {Amount}, Nuevo saldo: {Balance}",
                accountId, amount, account.Balance);

            return Result.Ok(account.Balance);
        }
        catch (InvariantViolationException ex)
        {
            _logger.LogWarning(ex,
                "Violación de invariante al retirar. Cuenta: {AccountId}, Monto: {Amount}",
                accountId, amount);

            return Result.Fail<decimal>(
                new BusinessRuleError(ex.ErrorCode, ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Argumento inválido al retirar. Cuenta: {AccountId}, Monto: {Amount}",
                accountId, amount);

            return Result.Fail<decimal>(
                new ValidationError(new Dictionary<string, string[]>
                {
                    [ex.ParamName ?? "Unknown"] = new[] { ex.Message }
                }));
        }
        // ✅ Dejar que excepciones de infraestructura se propaguen
    }
}
```

### WebApi Layer (FastEndpoints)

```csharp
namespace WebApi.Endpoints.BankAccounts;

using FastEndpoints;
using FluentResults;
using Application.UseCases.BankAccounts;
using Application.Common.Results;

public class WithdrawRequest
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
}

public class WithdrawResponse
{
    public decimal NewBalance { get; set; }
}

public class WithdrawEndpoint : Endpoint<WithdrawRequest, WithdrawResponse>
{
    private readonly WithdrawHandler _handler;

    public WithdrawEndpoint(WithdrawHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/bank-accounts/{AccountId}/withdraw");
        AllowAnonymous();
    }

    // ✅ WebApi: Mapea Results a HTTP responses
    public override async Task HandleAsync(WithdrawRequest req, CancellationToken ct)
    {
        var result = await _handler.Handle(req.AccountId, req.Amount, ct);

        if (result.IsFailed)
        {
            // Mapear errores a HTTP status codes
            var error = result.Errors.First();

            if (error is NotFoundError notFoundError)
            {
                await SendNotFoundAsync(ct);
                return;
            }

            if (error is ValidationError validationError)
            {
                foreach (var kvp in validationError.ValidationErrors)
                {
                    foreach (var errorMsg in kvp.Value)
                    {
                        AddError(errorMsg, kvp.Key);
                    }
                }
                await SendErrorsAsync(cancellation: ct);
                return;
            }

            if (error is BusinessRuleError businessError)
            {
                AddError(businessError.Message);
                await SendErrorsAsync(statusCode: 422, cancellation: ct);
                return;
            }

            // Error desconocido
            AddError(error.Message);
            await SendErrorsAsync(statusCode: 400, cancellation: ct);
            return;
        }

        // Éxito
        await SendOkAsync(new WithdrawResponse
        {
            NewBalance = result.Value
        }, ct);
    }
}
```

### Global Exception Handler

```csharp
namespace WebApi.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    // ✅ Maneja excepciones no capturadas (principalmente infraestructura)
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception,
            "Excepción no manejada: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "Internal Server Error",
            status = 500,
            detail = "Ha ocurrido un error interno en el servidor",
            traceId = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = 500;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails),
            ct);

        return true;
    }
}
```

### Registro en Program.cs

```csharp
namespace WebApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Middleware;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Registrar Global Exception Handler
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // ... otros servicios

        var app = builder.Build();

        // ✅ IMPORTANTE: UseExceptionHandler debe estar antes de otros middleware
        app.UseExceptionHandler();

        // ... otros middleware

        app.Run();
    }
}
```

---

## Error Messages y Códigos

### Estructura de Error Codes

```csharp
namespace Application.Common.ErrorCodes;

// ✅ CORRECTO: Códigos de error estructurados
public static class ErrorCodes
{
    // Validación (1000-1999)
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string REQUIRED_FIELD = "REQUIRED_FIELD";
    public const string INVALID_FORMAT = "INVALID_FORMAT";
    public const string INVALID_LENGTH = "INVALID_LENGTH";
    public const string INVALID_RANGE = "INVALID_RANGE";

    // Recursos (2000-2999)
    public const string NOT_FOUND = "NOT_FOUND";
    public const string ALREADY_EXISTS = "ALREADY_EXISTS";
    public const string DUPLICATE_ENTRY = "DUPLICATE_ENTRY";

    // Reglas de Negocio (3000-3999)
    public const string BUSINESS_RULE_VIOLATION = "BUSINESS_RULE_VIOLATION";
    public const string INSUFFICIENT_STOCK = "INSUFFICIENT_STOCK";
    public const string INSUFFICIENT_BALANCE = "INSUFFICIENT_BALANCE";
    public const string INVALID_STATE_TRANSITION = "INVALID_STATE_TRANSITION";
    public const string OPERATION_NOT_ALLOWED = "OPERATION_NOT_ALLOWED";

    // Seguridad (4000-4999)
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";

    // Sistema (5000-5999)
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string TIMEOUT = "TIMEOUT";
    public const string EXTERNAL_SERVICE_ERROR = "EXTERNAL_SERVICE_ERROR";
}
```

### Error Messages Descriptivos

```csharp
namespace Application.Common.Results;

using FluentResults;
using Application.Common.ErrorCodes;

// ✅ CORRECTO: Mensajes descriptivos y accionables
public static class ErrorFactory
{
    public static Error NotFound(string entityType, object entityId)
    {
        var error = new Error($"{entityType} con ID '{entityId}' no encontrado");
        error.Metadata.Add("ErrorCode", ErrorCodes.NOT_FOUND);
        error.Metadata.Add("EntityType", entityType);
        error.Metadata.Add("EntityId", entityId.ToString());
        return error;
    }

    public static Error ValidationFailed(string field, string message)
    {
        var error = new Error($"Error de validación en '{field}': {message}");
        error.Metadata.Add("ErrorCode", ErrorCodes.VALIDATION_ERROR);
        error.Metadata.Add("Field", field);
        return error;
    }

    public static Error RequiredField(string field)
    {
        var error = new Error($"El campo '{field}' es requerido");
        error.Metadata.Add("ErrorCode", ErrorCodes.REQUIRED_FIELD);
        error.Metadata.Add("Field", field);
        return error;
    }

    public static Error InvalidFormat(string field, string expectedFormat)
    {
        var error = new Error(
            $"El campo '{field}' tiene un formato inválido. Formato esperado: {expectedFormat}");
        error.Metadata.Add("ErrorCode", ErrorCodes.INVALID_FORMAT);
        error.Metadata.Add("Field", field);
        error.Metadata.Add("ExpectedFormat", expectedFormat);
        return error;
    }

    public static Error DuplicateEntry(string field, object value)
    {
        var error = new Error($"Ya existe un registro con {field} = '{value}'");
        error.Metadata.Add("ErrorCode", ErrorCodes.DUPLICATE_ENTRY);
        error.Metadata.Add("Field", field);
        error.Metadata.Add("Value", value.ToString());
        return error;
    }

    public static Error InsufficientStock(string productName, int available, int requested)
    {
        var error = new Error(
            $"Stock insuficiente para '{productName}'. Disponible: {available}, Solicitado: {requested}");
        error.Metadata.Add("ErrorCode", ErrorCodes.INSUFFICIENT_STOCK);
        error.Metadata.Add("ProductName", productName);
        error.Metadata.Add("Available", available.ToString());
        error.Metadata.Add("Requested", requested.ToString());
        return error;
    }

    public static Error InvalidStateTransition(
        string entityType,
        string currentState,
        string targetState)
    {
        var error = new Error(
            $"No se puede cambiar el estado de {entityType} de '{currentState}' a '{targetState}'");
        error.Metadata.Add("ErrorCode", ErrorCodes.INVALID_STATE_TRANSITION);
        error.Metadata.Add("EntityType", entityType);
        error.Metadata.Add("CurrentState", currentState);
        error.Metadata.Add("TargetState", targetState);
        return error;
    }
}
```

### Uso de ErrorFactory

```csharp
namespace Application.UseCases.Products;

using FluentResults;
using Application.Common.Results;
using Application.Common.ErrorCodes;
using Domain.Entities;
using Domain.Interfaces.Repositories;

public class ReserveStockHandler
{
    private readonly IProductRepository _productRepository;

    public ReserveStockHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result> Handle(
        Guid productId,
        int quantity,
        CancellationToken ct)
    {
        // Validación
        if (quantity <= 0)
        {
            return Result.Fail(
                ErrorFactory.ValidationFailed("Quantity", "Debe ser mayor a cero"));
        }

        // Buscar producto
        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product == null)
        {
            return Result.Fail(ErrorFactory.NotFound("Product", productId));
        }

        // Verificar stock
        if (product.Stock < quantity)
        {
            return Result.Fail(
                ErrorFactory.InsufficientStock(
                    product.Name,
                    product.Stock,
                    quantity));
        }

        // Reservar stock
        product.ReserveStock(quantity);
        await _productRepository.SaveOrUpdateAsync(product, ct);

        return Result.Ok();
    }
}
```

---

## Logging de Errores

### Configuración de ILogger

```csharp
namespace WebApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ✅ Configurar logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // En producción, usar un provider como Serilog
        // builder.Host.UseSerilog((context, services, configuration) =>
        //     configuration
        //         .ReadFrom.Configuration(context.Configuration)
        //         .Enrich.FromLogContext());

        var app = builder.Build();
        app.Run();
    }
}
```

### Logging Estructurado

```csharp
namespace Application.UseCases.Orders;

using FluentResults;
using Application.Common.Results;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        ILogger<CreateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<Order>> Handle(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        // ✅ CORRECTO: Log al inicio con contexto
        _logger.LogInformation(
            "Iniciando creación de orden. Usuario: {UserId}, Items: {ItemCount}",
            command.UserId,
            command.Items.Count);

        // Verificar usuario
        var user = await _userRepository.GetByIdAsync(command.UserId, ct);
        if (user == null)
        {
            // ✅ CORRECTO: Log warning para caso esperado (no encontrado)
            _logger.LogWarning(
                "Usuario no encontrado al crear orden. UserId: {UserId}",
                command.UserId);

            return Result.Fail<Order>(
                new NotFoundError("User", command.UserId));
        }

        try
        {
            // Crear orden
            var order = new Order
            {
                UserId = command.UserId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            foreach (var item in command.Items)
            {
                order.AddItem(
                    new Product { Id = item.ProductId },
                    item.Quantity,
                    item.UnitPrice);
            }

            await _orderRepository.SaveOrUpdateAsync(order, ct);

            // ✅ CORRECTO: Log éxito con información relevante
            _logger.LogInformation(
                "Orden creada exitosamente. OrderId: {OrderId}, UserId: {UserId}, Total: {Total}",
                order.Id,
                order.UserId,
                order.GetTotal());

            return Result.Ok(order);
        }
        catch (Exception ex)
        {
            // ✅ CORRECTO: Log error con excepción y contexto
            _logger.LogError(ex,
                "Error al crear orden. UserId: {UserId}, Items: {ItemCount}",
                command.UserId,
                command.Items.Count);

            throw; // Re-throw para que lo maneje el global handler
        }
    }
}
```

### Log Levels

```csharp
namespace Application.UseCases.Payments;

using FluentResults;
using Domain.Entities;
using Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

public class ProcessPaymentHandler
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(
        IPaymentGateway paymentGateway,
        ILogger<ProcessPaymentHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<Result<Payment>> Handle(
        ProcessPaymentCommand command,
        CancellationToken ct)
    {
        // ✅ LogDebug: Información detallada para debugging
        _logger.LogDebug(
            "Procesando pago. OrderId: {OrderId}, Amount: {Amount}, Method: {Method}",
            command.OrderId,
            command.Amount,
            command.PaymentMethod);

        // ✅ LogInformation: Eventos normales del negocio
        _logger.LogInformation(
            "Iniciando proceso de pago. OrderId: {OrderId}, Amount: {Amount}",
            command.OrderId,
            command.Amount);

        try
        {
            var result = await _paymentGateway.ProcessAsync(
                command.Amount,
                command.PaymentMethod,
                ct);

            if (!result.IsSuccess)
            {
                // ✅ LogWarning: Situación esperada pero no deseada
                _logger.LogWarning(
                    "Pago rechazado. OrderId: {OrderId}, Reason: {Reason}",
                    command.OrderId,
                    result.ErrorMessage);

                return Result.Fail<Payment>(result.ErrorMessage);
            }

            // ✅ LogInformation: Éxito
            _logger.LogInformation(
                "Pago procesado exitosamente. OrderId: {OrderId}, TransactionId: {TransactionId}",
                command.OrderId,
                result.TransactionId);

            var payment = new Payment
            {
                OrderId = command.OrderId,
                Amount = command.Amount,
                TransactionId = result.TransactionId,
                Status = PaymentStatus.Completed
            };

            return Result.Ok(payment);
        }
        catch (TimeoutException ex)
        {
            // ✅ LogError: Error de infraestructura
            _logger.LogError(ex,
                "Timeout al procesar pago. OrderId: {OrderId}",
                command.OrderId);

            throw;
        }
        catch (Exception ex)
        {
            // ✅ LogCritical: Error crítico que requiere atención inmediata
            _logger.LogCritical(ex,
                "Error crítico al procesar pago. OrderId: {OrderId}, Amount: {Amount}",
                command.OrderId,
                command.Amount);

            throw;
        }
    }
}
```

### Log Scopes para Contexto

```csharp
namespace Application.UseCases.Orders;

using FluentResults;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

public class ProcessOrderWorkflowHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly ILogger<ProcessOrderWorkflowHandler> _logger;

    public ProcessOrderWorkflowHandler(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        IShippingService shippingService,
        ILogger<ProcessOrderWorkflowHandler> logger)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _shippingService = shippingService;
        _logger = logger;
    }

    public async Task<Result> Handle(Guid orderId, CancellationToken ct)
    {
        // ✅ CORRECTO: Usar scope para agrupar logs relacionados
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = orderId,
            ["WorkflowId"] = Guid.NewGuid(),
            ["Timestamp"] = DateTime.UtcNow
        }))
        {
            _logger.LogInformation("Iniciando workflow de procesamiento de orden");

            var order = await _orderRepository.GetByIdAsync(orderId, ct);
            if (order == null)
            {
                _logger.LogWarning("Orden no encontrada");
                return Result.Fail(new NotFoundError("Order", orderId));
            }

            // Paso 1: Procesar pago
            _logger.LogInformation("Procesando pago");
            var paymentResult = await _paymentService.ProcessAsync(order, ct);
            if (paymentResult.IsFailed)
            {
                _logger.LogWarning("Pago falló: {Reason}", paymentResult.Errors.First().Message);
                return paymentResult;
            }

            // Paso 2: Preparar envío
            _logger.LogInformation("Preparando envío");
            var shippingResult = await _shippingService.PrepareShipmentAsync(order, ct);
            if (shippingResult.IsFailed)
            {
                _logger.LogError("Fallo al preparar envío: {Reason}", shippingResult.Errors.First().Message);
                return shippingResult;
            }

            _logger.LogInformation("Workflow completado exitosamente");
            return Result.Ok();
        }
    }
}
```

---

## Traducción a HTTP Status Codes

### Mapeo de Errores a Status Codes

```csharp
namespace WebApi.Common;

using System.Net;
using FluentResults;
using Application.Common.Results;

// ✅ CORRECTO: Helper para mapear errores a status codes
public static class ResultMapper
{
    public static (int StatusCode, string Message) MapToHttpResponse(IError error)
    {
        return error switch
        {
            NotFoundError => (404, error.Message),
            ValidationError => (400, error.Message),
            BusinessRuleError => (422, error.Message),
            _ when IsUnauthorizedError(error) => (401, error.Message),
            _ when IsForbiddenError(error) => (403, error.Message),
            _ when IsConflictError(error) => (409, error.Message),
            _ => (400, error.Message)
        };
    }

    public static int GetStatusCode(IError error)
    {
        var (statusCode, _) = MapToHttpResponse(error);
        return statusCode;
    }

    private static bool IsUnauthorizedError(IError error)
    {
        return error.Metadata.TryGetValue("ErrorCode", out var code) &&
               code?.ToString() == "UNAUTHORIZED";
    }

    private static bool IsForbiddenError(IError error)
    {
        return error.Metadata.TryGetValue("ErrorCode", out var code) &&
               code?.ToString() == "FORBIDDEN";
    }

    private static bool IsConflictError(IError error)
    {
        return error.Metadata.TryGetValue("ErrorCode", out var code) &&
               (code?.ToString() == "DUPLICATE_ENTRY" ||
                code?.ToString() == "ALREADY_EXISTS");
    }
}
```

### Endpoint Base con Manejo de Errores

```csharp
namespace WebApi.Common;

using FastEndpoints;
using FluentResults;
using Application.Common.Results;

// ✅ CORRECTO: Endpoint base con manejo consistente de errores
public abstract class BaseEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected async Task HandleResultAsync<T>(
        Result<T> result,
        Func<T, TResponse> mapper,
        CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await SendOkAsync(mapper(result.Value), ct);
            return;
        }

        await HandleErrorsAsync(result.Errors, ct);
    }

    protected async Task HandleResultAsync(Result result, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await SendNoContentAsync(ct);
            return;
        }

        await HandleErrorsAsync(result.Errors, ct);
    }

    private async Task HandleErrorsAsync(List<IError> errors, CancellationToken ct)
    {
        var primaryError = errors.First();

        if (primaryError is NotFoundError)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (primaryError is ValidationError validationError)
        {
            foreach (var kvp in validationError.ValidationErrors)
            {
                foreach (var errorMsg in kvp.Value)
                {
                    AddError(errorMsg, kvp.Key);
                }
            }
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        if (primaryError is BusinessRuleError businessError)
        {
            AddError(businessError.Message);
            await SendErrorsAsync(statusCode: 422, cancellation: ct);
            return;
        }

        // Otros errores
        var (statusCode, message) = ResultMapper.MapToHttpResponse(primaryError);
        AddError(message);
        await SendErrorsAsync(statusCode: statusCode, cancellation: ct);
    }
}
```

### Uso del Endpoint Base

```csharp
namespace WebApi.Endpoints.Products;

using FastEndpoints;
using Application.UseCases.Products;
using WebApi.Common;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class CreateProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// ✅ CORRECTO: Usar endpoint base para manejo consistente
public class CreateProductEndpoint : BaseEndpoint<CreateProductRequest, CreateProductResponse>
{
    private readonly CreateProductHandler _handler;

    public CreateProductEndpoint(CreateProductHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var command = new CreateProductCommand
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            Stock = req.Stock
        };

        var result = await _handler.Handle(command, ct);

        // ✅ El manejo de errores es automático y consistente
        await HandleResultAsync(
            result,
            product => new CreateProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price
            },
            ct);
    }
}
```

### Response Detallado con Problem Details

```csharp
namespace WebApi.Common;

using Microsoft.AspNetCore.Http;

// ✅ CORRECTO: Usar RFC 9457 Problem Details
public class ProblemDetailsResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, object>? Extensions { get; set; }
}

public static class ProblemDetailsFactory
{
    public static ProblemDetailsResponse CreateValidationProblem(
        HttpContext httpContext,
        Dictionary<string, string[]> errors)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred",
            Status = 400,
            Detail = "See the errors property for details",
            Instance = httpContext.Request.Path,
            Extensions = new Dictionary<string, object>
            {
                ["errors"] = errors,
                ["traceId"] = httpContext.TraceIdentifier
            }
        };
    }

    public static ProblemDetailsResponse CreateNotFoundProblem(
        HttpContext httpContext,
        string detail)
    {
        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Title = "Resource not found",
            Status = 404,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = new Dictionary<string, object>
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        };
    }

    public static ProblemDetailsResponse CreateBusinessRuleProblem(
        HttpContext httpContext,
        string detail,
        string? errorCode = null)
    {
        var extensions = new Dictionary<string, object>
        {
            ["traceId"] = httpContext.TraceIdentifier
        };

        if (!string.IsNullOrEmpty(errorCode))
        {
            extensions["errorCode"] = errorCode;
        }

        return new ProblemDetailsResponse
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.23",
            Title = "Business rule violation",
            Status = 422,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = extensions
        };
    }
}
```

---

## Anti-Patrones Comunes

### ❌ Anti-Patrón 1: Catching y Ocultando Excepciones

```csharp
// ❌ INCORRECTO: Capturar y ocultar excepciones
public async Task<User?> GetUserAsync(Guid id, CancellationToken ct)
{
    try
    {
        return await _repository.GetByIdAsync(id, ct);
    }
    catch (Exception)
    {
        // ❌ Oculta el error, retorna null silenciosamente
        return null;
    }
}
```

```csharp
// ✅ CORRECTO: Dejar que la excepción se propague o convertirla a Result
public async Task<Result<User>> GetUserAsync(Guid id, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(id, ct);
    // Las excepciones de infraestructura se propagan automáticamente

    if (user == null)
    {
        return Result.Fail<User>(new NotFoundError("User", id));
    }

    return Result.Ok(user);
}
```

### ❌ Anti-Patrón 2: Throw new Exception() Genérico

```csharp
// ❌ INCORRECTO: Excepción genérica sin información
public void ProcessOrder(Order order)
{
    if (order.Items.Count == 0)
    {
        throw new Exception("Error");
    }
}
```

```csharp
// ✅ CORRECTO: Excepción específica o Result con información
public Result ProcessOrder(Order order)
{
    if (order.Items.Count == 0)
    {
        return Result.Fail(
            new BusinessRuleError(
                "ORDER_EMPTY",
                "No se puede procesar una orden sin items"));
    }

    // ... procesamiento
    return Result.Ok();
}
```

### ❌ Anti-Patrón 3: Usar Excepciones para Control de Flujo

```csharp
// ❌ INCORRECTO: Excepción para control de flujo
public decimal CalculateDiscount(User user)
{
    try
    {
        var vipStatus = user.GetVipStatus();
        return vipStatus.DiscountPercentage;
    }
    catch (VipStatusNotFoundException)
    {
        return 0m; // Usuario no VIP
    }
}
```

```csharp
// ✅ CORRECTO: Retorno explícito o Result
public decimal CalculateDiscount(User user)
{
    var vipStatus = user.GetVipStatusOrDefault();
    return vipStatus?.DiscountPercentage ?? 0m;
}
```

### ❌ Anti-Patrón 4: No Logear Excepciones Antes de Re-throw

```csharp
// ❌ INCORRECTO: Re-throw sin logging
public async Task<Order> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
{
    try
    {
        // ... lógica
        return order;
    }
    catch (Exception)
    {
        throw; // Se pierde contexto
    }
}
```

```csharp
// ✅ CORRECTO: Log antes de re-throw
public async Task<Order> CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
{
    try
    {
        // ... lógica
        return order;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Error al crear orden. UserId: {UserId}, Items: {ItemCount}",
            command.UserId,
            command.Items.Count);
        throw;
    }
}
```

### ❌ Anti-Patrón 5: throw ex (Pierde Stack Trace)

```csharp
// ❌ INCORRECTO: throw ex pierde el stack trace original
public async Task ProcessAsync()
{
    try
    {
        await DoSomethingAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error");
        throw ex; // ❌ Pierde stack trace
    }
}
```

```csharp
// ✅ CORRECTO: throw sin variable preserva stack trace
public async Task ProcessAsync()
{
    try
    {
        await DoSomethingAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error");
        throw; // ✅ Preserva stack trace completo
    }
}
```

### ❌ Anti-Patrón 6: Mensajes de Error No Informativos

```csharp
// ❌ INCORRECTO: Mensaje vago
return Result.Fail("Error al procesar");

// ❌ INCORRECTO: Mensaje técnico para usuario final
return Result.Fail("NullReferenceException in line 42");

// ❌ INCORRECTO: Sin contexto
return Result.Fail("No encontrado");
```

```csharp
// ✅ CORRECTO: Mensajes descriptivos y accionables
return Result.Fail(
    new NotFoundError("Product", productId)); // Contexto claro

return Result.Fail(
    new BusinessRuleError(
        "INSUFFICIENT_STOCK",
        $"Stock insuficiente para '{productName}'. Disponible: {available}, Solicitado: {requested}"));

return Result.Fail(
    new ValidationError(new Dictionary<string, string[]>
    {
        ["Email"] = new[] { "El formato del email es inválido. Ejemplo: usuario@dominio.com" }
    }));
```

---

## Checklists

### Checklist: Implementar Error Handling en Use Case

- [ ] **Validación de entrada**
  - [ ] Validar parámetros requeridos (null checks)
  - [ ] Validar formatos (email, teléfono, etc.)
  - [ ] Validar rangos (valores mínimos/máximos)
  - [ ] Usar FluentValidation para validación compleja
  - [ ] Retornar `Result.Fail` con `ValidationError`

- [ ] **Verificación de recursos**
  - [ ] Verificar que entidades requeridas existan
  - [ ] Retornar `Result.Fail` con `NotFoundError` si no existen
  - [ ] No usar excepciones para "no encontrado"

- [ ] **Reglas de negocio**
  - [ ] Validar todas las reglas de negocio
  - [ ] Retornar `Result.Fail` con `BusinessRuleError`
  - [ ] Incluir código de error descriptivo
  - [ ] Incluir mensaje accionable

- [ ] **Llamadas a Domain**
  - [ ] Capturar domain exceptions en try-catch
  - [ ] Convertir domain exceptions a `Result.Fail`
  - [ ] No ocultar domain exceptions

- [ ] **Llamadas a Infrastructure**
  - [ ] Dejar que excepciones se propaguen
  - [ ] Logear con `_logger.LogError` antes de re-throw
  - [ ] Incluir contexto en el log (IDs, parámetros)

- [ ] **Logging**
  - [ ] `LogInformation` al inicio del use case
  - [ ] `LogWarning` para casos esperados pero no deseados
  - [ ] `LogError` para excepciones de infraestructura
  - [ ] Usar structured logging (placeholders)

- [ ] **Retorno**
  - [ ] Retornar `Result<T>` o `Result` (no void)
  - [ ] No mezclar excepciones y Results
  - [ ] Documentar qué errores puede retornar

### Checklist: Implementar Custom Exception en Domain

- [ ] **Definición**
  - [ ] Heredar de `DomainException` base
  - [ ] Incluir `[Serializable]` attribute
  - [ ] Definir propiedad `ErrorCode`
  - [ ] Incluir propiedades para contexto (EntityId, etc.)

- [ ] **Constructores**
  - [ ] Constructor con mensaje
  - [ ] Constructor con mensaje e inner exception
  - [ ] Llamar al constructor base apropiadamente

- [ ] **Uso**
  - [ ] Usar solo para invariantes críticas
  - [ ] No usar para validaciones simples
  - [ ] Incluir mensaje descriptivo
  - [ ] Incluir toda la información relevante

- [ ] **Manejo**
  - [ ] Capturar en Application layer
  - [ ] Convertir a Result
  - [ ] Logear apropiadamente

### Checklist: Implementar Endpoint con Error Handling

- [ ] **Configuración**
  - [ ] Heredar de `BaseEndpoint<TRequest, TResponse>`
  - [ ] Configurar ruta y método HTTP
  - [ ] Configurar autenticación si es necesaria

- [ ] **Validación**
  - [ ] Validar request usando FluentValidation
  - [ ] Enviar 400 Bad Request para validación fallida

- [ ] **Llamada a Handler**
  - [ ] Llamar al handler apropiado
  - [ ] Pasar CancellationToken

- [ ] **Mapeo de Result**
  - [ ] Usar `HandleResultAsync` para mapeo automático
  - [ ] O mapear manualmente según tipo de error:
    - [ ] `NotFoundError` → 404
    - [ ] `ValidationError` → 400
    - [ ] `BusinessRuleError` → 422
    - [ ] Otros → código apropiado

- [ ] **Respuesta exitosa**
  - [ ] Mapear entidad a DTO de respuesta
  - [ ] No exponer entidades de dominio directamente
  - [ ] Usar código HTTP apropiado (200, 201, 204)

- [ ] **No manejar excepciones**
  - [ ] No usar try-catch en el endpoint
  - [ ] Dejar que GlobalExceptionHandler las maneje

### Checklist: Code Review - Error Handling

- [ ] **Result Pattern**
  - [ ] Se usa `Result<T>` para errores esperados
  - [ ] No se usan excepciones para control de flujo
  - [ ] Todos los métodos públicos retornan Result o Task<Result>

- [ ] **Excepciones**
  - [ ] Solo se usan para errores inesperados
  - [ ] Se usan tipos específicos (no Exception genérico)
  - [ ] Se preserva el stack trace (throw sin variable)
  - [ ] Se logean antes de re-throw

- [ ] **Custom Exceptions**
  - [ ] Heredan de clase base apropiada
  - [ ] Incluyen [Serializable]
  - [ ] Tienen constructores estándar
  - [ ] Se usan solo para invariantes del dominio

- [ ] **Error Messages**
  - [ ] Son descriptivos y accionables
  - [ ] Incluyen contexto relevante (IDs, valores)
  - [ ] Tienen códigos de error consistentes
  - [ ] No exponen detalles de implementación

- [ ] **Logging**
  - [ ] Nivel apropiado (Debug, Info, Warning, Error, Critical)
  - [ ] Structured logging (placeholders, no interpolación)
  - [ ] Contexto suficiente para diagnóstico
  - [ ] No se logea información sensible

- [ ] **HTTP Responses**
  - [ ] Status codes correctos según tipo de error
  - [ ] Formato consistente (Problem Details)
  - [ ] Mensajes apropiados para cliente
  - [ ] TraceId incluido para correlación

- [ ] **Propagación**
  - [ ] Domain exceptions capturadas en Application
  - [ ] Infrastructure exceptions propagadas a global handler
  - [ ] No se ocultan errores (catch sin re-throw)

---

## Referencias

### Documentación Oficial Microsoft

- [Exception Handling (C#)](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/exception-handling)
- [Creating and Throwing Exceptions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/exceptions/creating-and-throwing-exceptions)
- [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [ILogger Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)
- [Problem Details (RFC 9457)](https://tools.ietf.org/html/rfc9457)

### Librerías

- [FluentResults](https://github.com/altmann/FluentResults) - Result pattern para .NET
- [FluentValidation](https://docs.fluentvalidation.net/) - Librería de validación
- [Serilog](https://serilog.net/) - Structured logging para .NET

### Artículos y Recursos

- [Exception Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Concepto detrás del Result pattern

---

**Última actualización:** 2025-11-13
**Versión del documento:** 1.0.0
