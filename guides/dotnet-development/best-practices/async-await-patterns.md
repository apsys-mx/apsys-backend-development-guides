# Async/Await Patterns

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-13

---

## Descripción

Esta guía detalla los patrones y mejores prácticas para trabajar con código asincrónico en .NET usando `async/await`. El manejo correcto de operaciones asincrónicas es fundamental para crear aplicaciones escalables, responsivas y eficientes.

Un uso adecuado de async/await:
- **Mejora la respuesta** de las aplicaciones (especialmente UI)
- **Aumenta el throughput** de servicios web al liberar hilos
- **Reduce el consumo** de recursos del sistema
- **Previene bloqueos** (deadlocks) y race conditions
- **Facilita el mantenimiento** del código asincrónico

---

## Tabla de Contenido

1. [Cuándo Usar Async/Await](#cuándo-usar-asyncawait)
2. [Async All the Way](#async-all-the-way)
3. [ValueTask vs Task](#valuetask-vs-task)
4. [ConfigureAwait](#configureawait)
5. [Cancellation Tokens](#cancellation-tokens)
6. [Evitar Async Void](#evitar-async-void)
7. [Manejo de Excepciones](#manejo-de-excepciones)
8. [Anti-Patrones Comunes](#anti-patrones-comunes)
9. [Checklists](#checklists)

---

## Cuándo Usar Async/Await

### ✅ Operaciones I/O-Bound

Usar async/await para operaciones que esperan por recursos externos:

```csharp
namespace Application.UseCases.Users;

using Domain.Entities;
using Domain.Interfaces.Repositories;

public class GetUserHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // ✅ I/O-bound: Acceso a base de datos
    public async Task<User?> Handle(Guid userId, CancellationToken ct)
    {
        return await _userRepository.GetByIdAsync(userId, ct);
    }
}
```

```csharp
namespace Application.Services;

using System.Net.Http;

public class ExternalApiService
{
    private readonly HttpClient _httpClient;

    public ExternalApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ✅ I/O-bound: Llamada HTTP
    public async Task<string> GetDataAsync(string url, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

```csharp
namespace Infrastructure.Services;

public class FileService
{
    // ✅ I/O-bound: Lectura de archivo
    public async Task<string> ReadFileAsync(string filePath, CancellationToken ct)
    {
        return await File.ReadAllTextAsync(filePath, ct);
    }

    // ✅ I/O-bound: Escritura de archivo
    public async Task WriteFileAsync(string filePath, string content, CancellationToken ct)
    {
        await File.WriteAllTextAsync(filePath, content, ct);
    }
}
```

### ❌ Operaciones CPU-Bound

No usar async/await para operaciones que requieren procesamiento intensivo:

```csharp
namespace Application.Services;

public class CalculationService
{
    // ❌ MAL: async/await para operación CPU-bound
    public async Task<long> CalculateFactorialAsync(int n)
    {
        await Task.Delay(0); // ❌ No hacer esto

        long result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

    // ✅ CORRECTO: Síncrono para operaciones CPU-bound simples
    public long CalculateFactorial(int n)
    {
        long result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }

    // ✅ ALTERNATIVA: Task.Run para CPU-bound que debe ejecutarse en background
    public Task<long> CalculateFactorialInBackgroundAsync(int n)
    {
        return Task.Run(() =>
        {
            long result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        });
    }
}
```

### Casos de Uso Comunes

| Operación | Usar Async/Await | Razón |
|-----------|------------------|-------|
| **Llamadas a base de datos** | ✅ Sí | I/O-bound |
| **HTTP requests** | ✅ Sí | I/O-bound |
| **Lectura/escritura de archivos** | ✅ Sí | I/O-bound |
| **Operaciones de red** | ✅ Sí | I/O-bound |
| **Cálculos matemáticos** | ❌ No | CPU-bound |
| **Procesamiento de listas** | ❌ No | CPU-bound |
| **String manipulations** | ❌ No | CPU-bound |
| **Encriptación/compresión** | ⚠️ Task.Run | CPU-bound intensivo |

---

## Async All the Way

Una vez que introduces `async/await` en tu código, **debes usarlo en toda la cadena de llamadas**.

### ✅ Correcto: Async All the Way

```csharp
// Domain Layer
namespace Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task SaveOrUpdateAsync(User user, CancellationToken ct);
}

// Application Layer
namespace Application.UseCases.Users;

public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public CreateUserHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    // ✅ Async all the way
    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // Verificar si el email ya existe
        var existingUser = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (existingUser != null)
        {
            return Result.Fail<User>("Email already exists");
        }

        var user = new User
        {
            Email = command.Email,
            FullName = command.FullName
        };

        // Guardar usuario
        await _userRepository.SaveOrUpdateAsync(user, ct);
        await _unitOfWork.CommitAsync(ct);

        // Enviar email de bienvenida
        await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, ct);

        return Result.Ok(user);
    }
}

// WebAPI Layer
namespace WebApi.Features.Users.Endpoint;

public class CreateUserEndpoint : Endpoint<CreateUserRequest, CreateUserResponse>
{
    private readonly IMediator _mediator;

    public CreateUserEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    // ✅ Async all the way hasta el endpoint
    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var command = new CreateUserCommand(req.Email, req.FullName);
        var result = await _mediator.Send(command, ct);

        if (result.IsFailed)
        {
            AddError(result.Errors.First().Message);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendCreatedAtAsync<GetUserEndpoint>(
            new { userId = result.Value.Id },
            new CreateUserResponse(result.Value.Id),
            cancellation: ct
        );
    }
}
```

### ❌ Incorrecto: Mezclar Sync y Async

```csharp
namespace Application.UseCases.Users;

public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    // ❌ MAL: Método async pero bloqueando con .Result
    public async Task<User> Handle(CreateUserCommand command, CancellationToken ct)
    {
        // ❌ NUNCA: .Result bloquea el hilo
        var existingUser = _userRepository.GetByEmailAsync(command.Email, ct).Result;

        var user = new User { Email = command.Email };

        // ❌ NUNCA: .Wait() bloquea el hilo
        _userRepository.SaveOrUpdateAsync(user, ct).Wait();

        // ❌ NUNCA: .GetAwaiter().GetResult() también bloquea
        _unitOfWork.CommitAsync(ct).GetAwaiter().GetResult();

        return user;
    }
}
```

**Problemas de mezclar sync/async:**
- **Deadlocks** en aplicaciones UI (WPF, WinForms)
- **Bloqueo de hilos** del thread pool
- **Reducción de performance** en servicios web
- **Excepciones envueltas** en AggregateException

---

## ValueTask vs Task

### ¿Cuándo usar ValueTask<TResult>?

`ValueTask<TResult>` es una optimización de performance para escenarios donde el resultado puede estar disponible de forma síncrona.

### ✅ Usar ValueTask cuando:

```csharp
namespace Application.Services;

public class CacheService
{
    private readonly Dictionary<string, User> _cache = new();
    private readonly IUserRepository _repository;

    public CacheService(IUserRepository _repository)
    {
        _repository = repository;
    }

    // ✅ CORRECTO: ValueTask para operaciones que pueden completarse síncronamente
    public async ValueTask<User?> GetUserAsync(Guid userId, CancellationToken ct)
    {
        // Caso síncrono: resultado en cache
        if (_cache.TryGetValue(userId.ToString(), out var cachedUser))
        {
            return cachedUser; // Retorno síncrono sin allocar Task
        }

        // Caso asíncrono: buscar en repositorio
        var user = await _repository.GetByIdAsync(userId, ct);

        if (user != null)
        {
            _cache[userId.ToString()] = user;
        }

        return user;
    }
}
```

```csharp
namespace Infrastructure.Repositories;

public class OptimizedUserRepository
{
    private readonly ISession _session;
    private User? _lastFetchedUser;

    public OptimizedUserRepository(ISession session)
    {
        _session = session;
    }

    // ✅ ValueTask para operaciones que frecuentemente retornan de cache
    public ValueTask<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // Hot path: retornar valor cacheado
        if (_lastFetchedUser?.Id == id)
        {
            return new ValueTask<User?>(_lastFetchedUser);
        }

        // Cold path: consulta asíncrona real
        return new ValueTask<User?>(GetByIdFromDatabaseAsync(id, ct));
    }

    private async Task<User?> GetByIdFromDatabaseAsync(Guid id, CancellationToken ct)
    {
        var user = await _session.GetAsync<User>(id, ct);
        _lastFetchedUser = user;
        return user;
    }
}
```

### ❌ Usar Task cuando:

```csharp
namespace Application.UseCases.Orders;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    // ✅ Task: operación siempre asíncrona
    public async Task<Order> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var order = new Order
        {
            UserId = command.UserId,
            TotalAmount = command.TotalAmount
        };

        // Siempre va a la base de datos
        await _orderRepository.SaveAsync(order, ct);
        await _unitOfWork.CommitAsync(ct);

        return order;
    }
}
```

### Comparación Task vs ValueTask

| Aspecto | Task<TResult> | ValueTask<TResult> |
|---------|---------------|-------------------|
| **Allocación** | Siempre aloca en heap | Puede evitar allocación si es síncrono |
| **Performance** | Buena para casos async | Mejor si hay path síncrono |
| **Uso** | Cualquier escenario | Solo con hot path síncrono |
| **Await múltiple** | ✅ Permitido | ❌ Solo await una vez |
| **Almacenar** | ✅ Se puede guardar | ❌ No almacenar, usar inmediatamente |
| **Complejidad** | Simple | Más complejo |

### ⚠️ Advertencias sobre ValueTask

```csharp
namespace Application.Services;

public class ValueTaskExamples
{
    // ❌ MAL: Await múltiple de ValueTask
    public async Task<string> BadExample(ValueTask<string> valueTask)
    {
        var result1 = await valueTask; // Primera await
        var result2 = await valueTask; // ❌ Segunda await - comportamiento indefinido
        return result1 + result2;
    }

    // ❌ MAL: Almacenar ValueTask
    private ValueTask<string> _storedValueTask; // ❌ No hacer esto

    // ✅ CORRECTO: Await una sola vez
    public async Task<string> GoodExample(ValueTask<string> valueTask)
    {
        var result = await valueTask; // ✅ Solo una vez
        return result;
    }

    // ✅ CORRECTO: Convertir a Task si necesitas await múltiple
    public async Task<string> MultipleAwaitExample(ValueTask<string> valueTask)
    {
        Task<string> task = valueTask.AsTask(); // Convertir a Task

        var result1 = await task; // ✅ Permitido
        var result2 = await task; // ✅ Permitido

        return result1 + result2;
    }
}
```

---

## ConfigureAwait

### ¿Qué es ConfigureAwait?

`ConfigureAwait(false)` le indica al await que **no necesita capturar el contexto de sincronización** original.

### ✅ Usar ConfigureAwait(false) en Libraries y Application Layer

```csharp
namespace Application.UseCases.Users;

public class GetUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(IUserRepository userRepository, ILogger<GetUserHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User?> Handle(Guid userId, CancellationToken ct)
    {
        _logger.LogInformation("Fetching user {UserId}", userId);

        // ✅ ConfigureAwait(false) en código de librería/aplicación
        var user = await _userRepository.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return null;
        }

        _logger.LogInformation("User {UserId} found", userId);
        return user;
    }
}
```

```csharp
namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;

    public EmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var emailData = new { to, subject, body };
        var content = JsonContent.Create(emailData);

        // ✅ ConfigureAwait(false) para mejor performance
        var response = await _httpClient
            .PostAsync("/api/send-email", content, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await response.Content
            .ReadAsStringAsync(ct)
            .ConfigureAwait(false);
    }
}
```

### ❌ NO usar ConfigureAwait(false) en UI Code

```csharp
// WPF / WinForms
namespace DesktopApp.ViewModels;

public class UserViewModel
{
    private readonly IUserService _userService;

    public UserViewModel(IUserService userService)
    {
        _userService = userService;
    }

    public async Task LoadUserAsync(Guid userId)
    {
        // ❌ NO usar ConfigureAwait(false) en código UI
        // Necesitamos volver al UI thread para actualizar la UI
        var user = await _userService.GetUserAsync(userId);

        // Este código DEBE ejecutarse en el UI thread
        UserName = user.FullName; // Actualiza la UI
        UserEmail = user.Email;   // Actualiza la UI
    }
}
```

### Guía Rápida ConfigureAwait

| Contexto | ConfigureAwait(false) | Razón |
|----------|----------------------|-------|
| **ASP.NET Core** | ⚠️ Opcional | No hay SynchronizationContext |
| **Libraries** | ✅ Recomendado | Mejor performance |
| **Application Layer** | ✅ Recomendado | Mejor performance |
| **WPF/WinForms** | ❌ No | Necesitas UI thread |
| **Blazor Server** | ❌ No | Necesitas el contexto |
| **Xamarin/MAUI** | ❌ No | Necesitas UI thread |

### .NET 6+ Global Configuration

En .NET 6+, puedes configurar el comportamiento global:

```xml
<!-- .csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <!-- ConfigureAwait(false) por defecto en todo el proyecto -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

---

## Cancellation Tokens

Los `CancellationToken` permiten cancelar operaciones asincrónicas de forma cooperativa.

### ✅ Pasar CancellationToken en Toda la Cadena

```csharp
// Domain Layer
namespace Domain.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IList<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task SaveAsync(Order order, CancellationToken ct);
}

// Application Layer
namespace Application.UseCases.Orders;

public class GetUserOrdersHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetUserOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // ✅ Siempre aceptar y pasar CancellationToken
    public async Task<IList<Order>> Handle(Guid userId, CancellationToken ct)
    {
        // ✅ Pasar ct a todas las operaciones async
        return await _orderRepository.GetByUserIdAsync(userId, ct);
    }
}

// Infrastructure Layer
namespace Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ISession _session;

    public OrderRepository(ISession session)
    {
        _session = session;
    }

    public async Task<IList<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        // ✅ NHibernate respeta el CancellationToken
        return await _session
            .Query<Order>()
            .Where(o => o.UserId == userId)
            .ToListAsync(ct);
    }
}
```

### ✅ Usar CancellationToken en Loops

```csharp
namespace Application.Services;

public class BatchProcessingService
{
    private readonly IOrderRepository _orderRepository;

    public BatchProcessingService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task ProcessOrdersAsync(IList<Guid> orderIds, CancellationToken ct)
    {
        foreach (var orderId in orderIds)
        {
            // ✅ Verificar cancelación en cada iteración
            ct.ThrowIfCancellationRequested();

            var order = await _orderRepository.GetByIdAsync(orderId, ct);

            if (order != null)
            {
                await ProcessSingleOrderAsync(order, ct);
            }
        }
    }

    private async Task ProcessSingleOrderAsync(Order order, CancellationToken ct)
    {
        // Lógica de procesamiento
        await Task.Delay(100, ct); // ✅ Delay también respeta ct
    }
}
```

### ✅ Crear CancellationTokenSource con Timeout

```csharp
namespace WebApi.Features.Orders.Endpoint;

public class ProcessOrderEndpoint : Endpoint<ProcessOrderRequest>
{
    private readonly IMediator _mediator;

    public ProcessOrderEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/orders/{orderId}/process");
    }

    public override async Task HandleAsync(ProcessOrderRequest req, CancellationToken ct)
    {
        // ✅ Crear CancellationTokenSource con timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // ✅ Combinar con el CancellationToken del request
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var command = new ProcessOrderCommand(req.OrderId);
            var result = await _mediator.Send(command, linkedCts.Token);

            if (result.IsSuccess)
            {
                await SendOkAsync(ct);
            }
            else
            {
                await SendAsync(new ErrorResponse { Message = result.Errors.First().Message }, 400, ct);
            }
        }
        catch (OperationCanceledException)
        {
            if (timeoutCts.Token.IsCancellationRequested)
            {
                await SendAsync(new ErrorResponse { Message = "Operation timed out" }, 408, ct);
            }
            else
            {
                await SendAsync(new ErrorResponse { Message = "Operation cancelled" }, 499, ct);
            }
        }
    }
}
```

### ✅ Pasar CancellationToken a HttpClient

```csharp
namespace Infrastructure.Services;

public class ExternalApiService
{
    private readonly HttpClient _httpClient;

    public ExternalApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> FetchDataAsync(string url, CancellationToken ct)
    {
        try
        {
            // ✅ HttpClient respeta CancellationToken
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // La operación fue cancelada
            throw;
        }
        catch (HttpRequestException ex)
        {
            // Error de red
            throw new InvalidOperationException($"Error fetching data from {url}", ex);
        }
    }
}
```

### ❌ Nunca Ignorar CancellationToken

```csharp
namespace Application.Services;

public class BadService
{
    // ❌ MAL: No aceptar CancellationToken
    public async Task ProcessDataAsync()
    {
        await Task.Delay(1000); // ❌ No se puede cancelar
    }

    // ❌ MAL: Aceptar pero no usar
    public async Task ProcessDataAsync2(CancellationToken ct)
    {
        await Task.Delay(1000); // ❌ No pasa el ct
    }

    // ❌ MAL: Pasar CancellationToken.None
    public async Task ProcessDataAsync3(CancellationToken ct)
    {
        await SomeMethodAsync(CancellationToken.None); // ❌ Ignora el ct real
    }
}
```

---

## Evitar Async Void

### ❌ Async Void es Peligroso

```csharp
namespace Application.Services;

public class BadAsyncService
{
    // ❌ NUNCA: async void en código de aplicación
    public async void ProcessOrderAsync(Guid orderId)
    {
        // Si esto falla, la excepción no se puede capturar
        var order = await GetOrderAsync(orderId);
        await ProcessAsync(order);
    }

    // ❌ Las excepciones en async void CRASHEAN la aplicación
    public async void DangerousMethodAsync()
    {
        await Task.Delay(100);
        throw new InvalidOperationException("App will crash!");
    }
}
```

### ✅ Usar Async Task

```csharp
namespace Application.Services;

public class GoodAsyncService
{
    private readonly IOrderRepository _orderRepository;

    public GoodAsyncService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // ✅ CORRECTO: async Task
    public async Task ProcessOrderAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        await ProcessAsync(order, ct);
    }

    private async Task ProcessAsync(Order order, CancellationToken ct)
    {
        // Lógica de procesamiento
        await Task.Delay(100, ct);
    }
}
```

### ✅ Excepción: Event Handlers

La **única** excepción válida para `async void` son los **event handlers**:

```csharp
// ASP.NET Core - Background Service
namespace WebApi.HostedServices;

public class OrderProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OrderProcessingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingOrdersAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessPendingOrdersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        await orderService.ProcessPendingOrdersAsync(ct);
    }
}
```

```csharp
// Event Handler con async void (WPF/WinForms)
namespace DesktopApp;

public partial class MainWindow : Window
{
    private readonly IUserService _userService;

    public MainWindow(IUserService userService)
    {
        InitializeComponent();
        _userService = userService;
    }

    // ✅ PERMITIDO: Event handler UI
    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            UsersListBox.ItemsSource = users;
        }
        catch (Exception ex)
        {
            // ✅ IMPORTANTE: Siempre catch exceptions en async void
            MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
```

**Reglas para async void:**
1. ❌ **NUNCA** usar en código de aplicación/librería
2. ✅ **SOLO** usar en event handlers de UI
3. ✅ **SIEMPRE** envolver en try-catch
4. ✅ Considerar extraer lógica a método `async Task`

```csharp
namespace DesktopApp;

public partial class MainWindow : Window
{
    private readonly IUserService _userService;

    // ✅ MEJOR: Async void solo llama a async Task
    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ✅ Lógica en async Task (testeable)
    private async Task LoadUsersAsync()
    {
        var users = await _userService.GetAllUsersAsync();
        UsersListBox.ItemsSource = users;
    }
}
```

---

## Manejo de Excepciones

### ✅ Excepciones en Async Methods se Capturan Normalmente

```csharp
namespace Application.UseCases.Orders;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        try
        {
            var order = new Order
            {
                UserId = command.UserId,
                TotalAmount = command.TotalAmount
            };

            // ✅ Las excepciones async se capturan normalmente
            await _orderRepository.SaveAsync(order, ct);
            await _unitOfWork.CommitAsync(ct);

            return Result.Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            // ✅ Se captura correctamente
            return Result.Fail<Order>($"Invalid operation: {ex.Message}");
        }
        catch (Exception ex)
        {
            // ✅ Se captura correctamente
            return Result.Fail<Order>($"Unexpected error: {ex.Message}");
        }
    }
}
```

### ✅ Validar Antes de Async

```csharp
namespace Application.UseCases.Orders;

public class GetOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // ✅ CORRECTO: Validación síncrona primero, luego async
    public async Task<Order> Handle(Guid orderId, CancellationToken ct)
    {
        // ✅ Validaciones síncronas primero (fail-fast)
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));
        }

        // Después operaciones async
        var order = await _orderRepository.GetByIdAsync(orderId, ct);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        return order;
    }
}
```

### ✅ Patrón: Validación Síncrona + Core Async

```csharp
namespace Infrastructure.Services;

public class EmailService
{
    private readonly HttpClient _httpClient;

    public EmailService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ✅ Método público: validación síncrona
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        // Validaciones síncronas (se lanzan inmediatamente)
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient email cannot be empty", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty", nameof(subject));
        }

        // Delegar a método async privado
        return SendEmailCoreAsync(to, subject, body, ct);
    }

    // ✅ Método privado: lógica async
    private async Task SendEmailCoreAsync(string to, string subject, string body, CancellationToken ct)
    {
        var emailData = new { to, subject, body };
        var content = JsonContent.Create(emailData);

        var response = await _httpClient.PostAsync("/api/send", content, ct);
        response.EnsureSuccessStatusCode();

        await response.Content.ReadAsStringAsync(ct);
    }
}
```

### ⚠️ Task.WhenAll y Excepciones

```csharp
namespace Application.Services;

public class BatchService
{
    public async Task ProcessMultipleAsync(IList<Guid> ids, CancellationToken ct)
    {
        var tasks = ids.Select(id => ProcessSingleAsync(id, ct)).ToList();

        try
        {
            // ⚠️ WhenAll solo lanza la PRIMERA excepción
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Solo captura la primera excepción
            Console.WriteLine($"First exception: {ex.Message}");

            // ✅ Para ver TODAS las excepciones:
            foreach (var task in tasks)
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    foreach (var innerEx in task.Exception.InnerExceptions)
                    {
                        Console.WriteLine($"Exception: {innerEx.Message}");
                    }
                }
            }
        }
    }

    private async Task ProcessSingleAsync(Guid id, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        throw new InvalidOperationException($"Error processing {id}");
    }
}
```

---

## Anti-Patrones Comunes

### ❌ Anti-Patrón 1: Async Over Sync

```csharp
namespace Application.Services;

public class BadService
{
    // ❌ MAL: Async method que no hace nada async
    public async Task<int> GetCountAsync()
    {
        return 42; // ❌ No hay await, no hay operación async
    }

    // ❌ MAL: Usar Task.Run para hacer sync parecer async
    public Task<int> CalculateSumAsync(int a, int b)
    {
        return Task.Run(() => a + b); // ❌ Solo agrega overhead
    }

    // ✅ CORRECTO: Métodos síncronos deben ser síncronos
    public int GetCount()
    {
        return 42;
    }

    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}
```

### ❌ Anti-Patrón 2: Sync Over Async (Blocking)

```csharp
namespace WebApi.Features.Users.Endpoint;

public class BadEndpoint : EndpointWithoutRequest
{
    private readonly IUserRepository _userRepository;

    // ❌ NUNCA: Bloquear con .Result o .Wait()
    public override Task HandleAsync(CancellationToken ct)
    {
        // ❌ DEADLOCK RISK: .Result bloquea
        var users = _userRepository.GetAllAsync(ct).Result;

        // ❌ DEADLOCK RISK: .Wait() bloquea
        _userRepository.SaveAsync(new User(), ct).Wait();

        // ❌ DEADLOCK RISK: .GetAwaiter().GetResult()
        var count = _userRepository.CountAsync(ct).GetAwaiter().GetResult();

        return SendOkAsync(ct);
    }
}
```

```csharp
// ✅ CORRECTO: Async all the way
public class GoodEndpoint : EndpointWithoutRequest
{
    private readonly IUserRepository _userRepository;

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await _userRepository.GetAllAsync(ct);
        await _userRepository.SaveAsync(new User(), ct);
        var count = await _userRepository.CountAsync(ct);

        await SendOkAsync(ct);
    }
}
```

### ❌ Anti-Patrón 3: Async Void (fuera de event handlers)

```csharp
namespace Application.Services;

public class BadAsyncVoid
{
    // ❌ NUNCA: async void en código de aplicación
    public async void ProcessDataAsync()
    {
        await Task.Delay(100);
        throw new Exception("This will crash the app!");
    }

    // ✅ CORRECTO: async Task
    public async Task ProcessDataCorrectAsync()
    {
        await Task.Delay(100);
        // Exceptions can be caught by caller
    }
}
```

### ❌ Anti-Patrón 4: Fire and Forget sin Manejo

```csharp
namespace Application.Services;

public class FireAndForgetBad
{
    // ❌ MAL: Fire-and-forget sin manejo de errores
    public void TriggerBackgroundWork()
    {
        _ = DoWorkAsync(); // ❌ Exceptions se pierden
    }

    private async Task DoWorkAsync()
    {
        await Task.Delay(100);
        throw new Exception("Lost exception!");
    }
}
```

```csharp
// ✅ MEJOR: Background service con error handling
namespace WebApi.HostedServices;

public class BackgroundWorkService : BackgroundService
{
    private readonly ILogger<BackgroundWorkService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BackgroundWorkService(ILogger<BackgroundWorkService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background work");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        // Work here
        await Task.Delay(100, ct);
    }
}
```

### ❌ Anti-Patrón 5: Ignorar CancellationToken

```csharp
namespace Application.Services;

public class BadCancellation
{
    // ❌ MAL: No aceptar CancellationToken
    public async Task ProcessAsync()
    {
        await Task.Delay(10000); // No se puede cancelar
    }

    // ❌ MAL: Aceptar pero no usar
    public async Task ProcessAsync2(CancellationToken ct)
    {
        await Task.Delay(10000); // No pasa ct
    }

    // ✅ CORRECTO: Aceptar y usar CancellationToken
    public async Task ProcessCorrectAsync(CancellationToken ct)
    {
        await Task.Delay(10000, ct); // ✅ Respeta cancelación
    }
}
```

### ❌ Anti-Patrón 6: Capturar y Reenvolver Exceptions

```csharp
namespace Application.Services;

public class BadExceptionHandling
{
    // ❌ MAL: Capturar y re-throw pierde stack trace
    public async Task ProcessAsync()
    {
        try
        {
            await DoWorkAsync();
        }
        catch (Exception ex)
        {
            throw ex; // ❌ Pierde stack trace original
        }
    }

    // ✅ CORRECTO: Re-throw preserva stack trace
    public async Task ProcessCorrectAsync()
    {
        try
        {
            await DoWorkAsync();
        }
        catch (InvalidOperationException)
        {
            // Manejar específicamente o...
            throw; // ✅ Preserva stack trace
        }
    }

    private async Task DoWorkAsync()
    {
        await Task.Delay(100);
    }
}
```

---

## Checklists

### Checklist: Nuevo Método Async

Al crear un nuevo método asincrónico:

- [ ] El método retorna `Task` o `Task<TResult>` (NO `async void` salvo event handlers)
- [ ] El método acepta `CancellationToken ct` como último parámetro
- [ ] El nombre del método termina con `Async` (ej: `GetUserAsync`)
- [ ] El método pasa el `CancellationToken` a todas las llamadas async
- [ ] Todas las operaciones I/O usan `await` (no `.Result` o `.Wait()`)
- [ ] Si es una librería, usa `ConfigureAwait(false)` en los awaits
- [ ] Las validaciones de parámetros se hacen síncronamente al inicio
- [ ] Las excepciones se manejan apropiadamente con try-catch
- [ ] No hay código síncrono bloqueante mezclado con async

### Checklist: Code Review Async

Al revisar código asincrónico:

- [ ] No hay uso de `.Result`, `.Wait()`, o `.GetAwaiter().GetResult()`
- [ ] No hay `async void` fuera de event handlers
- [ ] Todos los métodos async terminan con `Async`
- [ ] Los `CancellationToken` se pasan correctamente
- [ ] No hay `Task.Run` innecesario para operaciones simples
- [ ] `ConfigureAwait(false)` se usa en librerías/Application layer
- [ ] No hay async-over-sync (async methods sin await)
- [ ] Las excepciones en loops async se manejan correctamente
- [ ] `Task.WhenAll` se usa para paralelismo cuando es apropiado
- [ ] No hay deadlocks potenciales por mezclar sync/async

### Checklist: Performance

Para optimizar performance de código async:

- [ ] Usar `ValueTask<T>` donde hay hot-path síncrono (ej: cache)
- [ ] `ConfigureAwait(false)` en código de librería
- [ ] Evitar allocaciones innecesarias de Task
- [ ] Usar `Task.WhenAll` para operaciones paralelas
- [ ] Evitar await en loops cuando se puede paralelizar
- [ ] Verificar que async es necesario (I/O-bound vs CPU-bound)
- [ ] No crear Tasks manualmente sin razón (`new Task()`)
- [ ] Usar `CancellationToken` para permitir cancelación temprana

---

## Referencias

- [Async/Await - Best Practices (Microsoft)](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [Task Asynchronous Programming (Microsoft)](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)
- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [ValueTask (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1)
- [Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
