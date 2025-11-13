# Clean Architecture Principles

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-13

## Descripción

Esta guía detalla los principios fundamentales de Clean Architecture aplicados a proyectos .NET, explicando cómo estructurar aplicaciones para que sean mantenibles, testables e independientes de frameworks y tecnologías específicas.

---

## Tabla de Contenido

1. [Dependency Rule](#dependency-rule)
2. [Separation of Concerns](#separation-of-concerns)
3. [Dependency Inversion Principle](#dependency-inversion-principle)
4. [Testability](#testability)
5. [Independence of Frameworks](#independence-of-frameworks)
6. [Independence of UI](#independence-of-ui)
7. [Independence of Database](#independence-of-database)
8. [Organización en Capas](#organización-en-capas)
9. [Checklist](#checklist)

---

## Dependency Rule

La regla de dependencias es el principio más importante de Clean Architecture:

> **Las dependencias del código fuente deben apuntar solo hacia adentro, hacia políticas de alto nivel.**

### Capas de Adentro hacia Afuera

```
┌─────────────────────────────────────────┐
│           Infrastructure                │
│  ┌─────────────────────────────────┐   │
│  │         WebAPI                  │   │
│  │  ┌─────────────────────────┐   │   │
│  │  │     Application         │   │   │
│  │  │  ┌─────────────────┐   │   │   │
│  │  │  │    Domain       │   │   │   │
│  │  │  └─────────────────┘   │   │   │
│  │  └─────────────────────────┘   │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

### ✅ Correcto: Dependencias hacia adentro

```csharp
// Domain Layer - No depende de nadie
namespace Domain.Entities;

public class User
{
    public virtual int Id { get; protected set; }
    public virtual string Email { get; set; }
    public virtual bool IsActive { get; set; }
}

// Application Layer - Depende solo de Domain
namespace Application.UseCases.Users;

public interface IGetUserHandler
{
    Task<Result<User>> Handle(int userId, CancellationToken ct);
}

// Infrastructure Layer - Depende de Domain y Application
namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ISession _session;

    public UserRepository(ISession session)
    {
        _session = session;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _session.GetAsync<User>(id, ct);
    }
}

// WebAPI Layer - Depende de Application
namespace WebAPI.Endpoints.Users;

public class GetUserEndpoint : EndpointWithoutRequest<UserResponse>
{
    private readonly IGetUserHandler _handler;

    public GetUserEndpoint(IGetUserHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/users/{userId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<int>("userId");
        var result = await _handler.Handle(userId, ct);

        if (result.IsFailed)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(result.Value.ToResponse(), ct);
    }
}
```

### ❌ Incorrecto: Violación de Dependency Rule

```csharp
// ❌ Domain referenciando Infrastructure
namespace Domain.Entities;

using Infrastructure.Database; // ❌ NUNCA

public class User
{
    public void Save()
    {
        var db = new DbContext(); // ❌ Domain conoce detalles de infraestructura
        db.Save(this);
    }
}

// ❌ Application referenciando WebAPI
namespace Application.UseCases.Users;

using WebAPI.DTOs; // ❌ NUNCA

public class CreateUserHandler
{
    public Result<UserDto> Handle(UserDto dto) // ❌ Application conoce DTOs de WebAPI
    {
        // ...
    }
}

// ❌ Domain referenciando Application
namespace Domain.Entities;

using Application.Services; // ❌ NUNCA

public class Order
{
    public void Process()
    {
        var service = new OrderProcessingService(); // ❌ Domain conoce casos de uso
        service.Process(this);
    }
}
```

---

## Separation of Concerns

Cada capa tiene responsabilidades claramente definidas y no debe mezclarse con otras.

### Responsabilidades por Capa

#### Domain Layer
- **Responsabilidad:** Lógica de negocio pura
- **Contiene:** Entidades, Value Objects, Excepciones de dominio, Interfaces de repositorios
- **NO contiene:** Lógica de persistencia, validaciones de entrada, lógica de presentación

```csharp
// ✅ Correcto: Lógica de dominio pura
namespace Domain.Entities;

public class Order
{
    public virtual int Id { get; protected set; }
    public virtual OrderStatus Status { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual IList<OrderItem> Items { get; set; } = new List<OrderItem>();

    public virtual decimal CalculateTotal()
    {
        return Items.Sum(item => item.Quantity * item.UnitPrice);
    }

    public virtual Result Approve()
    {
        if (Status != OrderStatus.Pending)
            return Result.Fail("Only pending orders can be approved");

        if (Items.Count == 0)
            return Result.Fail("Cannot approve order without items");

        Status = OrderStatus.Approved;
        return Result.Ok();
    }
}
```

#### Application Layer
- **Responsabilidad:** Orquestación de casos de uso
- **Contiene:** Handlers, Interfaces de servicios, DTOs internos
- **NO contiene:** Lógica de negocio (va en Domain), detalles de persistencia (va en Infrastructure)

```csharp
// ✅ Correcto: Orquestación de caso de uso
namespace Application.UseCases.Orders;

public class ApproveOrderHandler : IApproveOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public ApproveOrderHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<Result> Handle(int orderId, CancellationToken ct)
    {
        // 1. Obtener orden (usa repositorio)
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order == null)
            return Result.Fail("Order not found");

        // 2. Aplicar lógica de dominio
        var approvalResult = order.Approve();
        if (approvalResult.IsFailed)
            return approvalResult;

        // 3. Persistir cambios
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.CommitAsync(ct);

        // 4. Notificar (servicio externo)
        await _emailService.SendOrderApprovedEmailAsync(order, ct);

        return Result.Ok();
    }
}
```

#### Infrastructure Layer
- **Responsabilidad:** Implementaciones técnicas y acceso a recursos externos
- **Contiene:** Repositorios, Configuración de ORM, Servicios externos, HttpClients
- **NO contiene:** Lógica de negocio, orquestación de casos de uso

```csharp
// ✅ Correcto: Implementación técnica
namespace Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ISession _session;

    public OrderRepository(ISession session)
    {
        _session = session;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _session
            .Query<Order>()
            .Where(o => o.Id == id)
            .Fetch(o => o.Items)
            .SingleOrDefaultAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct)
    {
        await _session.UpdateAsync(order, ct);
    }
}
```

#### WebAPI Layer
- **Responsabilidad:** Recibir requests HTTP y devolver responses
- **Contiene:** Endpoints, DTOs de request/response, Validadores de entrada
- **NO contiene:** Lógica de negocio, lógica de acceso a datos

```csharp
// ✅ Correcto: Endpoint enfocado en HTTP
namespace WebAPI.Endpoints.Orders;

public class ApproveOrderEndpoint : Endpoint<ApproveOrderRequest>
{
    private readonly IApproveOrderHandler _handler;

    public ApproveOrderEndpoint(IApproveOrderHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/orders/{orderId}/approve");
        Policies("RequireAdmin");
    }

    public override async Task HandleAsync(ApproveOrderRequest req, CancellationToken ct)
    {
        var result = await _handler.Handle(req.OrderId, ct);

        if (result.IsFailed)
        {
            await SendAsync(new ErrorResponse
            {
                Message = result.Errors.First().Message
            }, 400, ct);
            return;
        }

        await SendOkAsync(ct);
    }
}
```

---

## Dependency Inversion Principle

Las abstracciones (interfaces) deben definirse en las capas internas, y las implementaciones en las capas externas.

### ✅ Correcto: Interfaces en capas internas

```csharp
// Domain Layer - Define la abstracción
namespace Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}

// Application Layer - Define abstracciones de servicios
namespace Application.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string name, CancellationToken ct);
}

// Infrastructure Layer - Implementa las abstracciones
namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    // Implementación con NHibernate
}

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    // Implementación con SMTP o servicio externo
}
```

### ❌ Incorrecto: Interfaces en capas externas

```csharp
// ❌ Infrastructure define la interfaz
namespace Infrastructure.Repositories;

public interface IUserRepository // ❌ La abstracción está en la capa equivocada
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
}

public class UserRepository : IUserRepository
{
    // ...
}

// ❌ Application depende de Infrastructure
namespace Application.UseCases;

using Infrastructure.Repositories; // ❌ Viola Dependency Rule

public class GetUserHandler
{
    private readonly IUserRepository _repository;
    // ...
}
```

---

## Testability

Una arquitectura limpia facilita enormemente el testing al permitir reemplazar implementaciones con mocks.

### ✅ Unit Test - Aislado de dependencias externas

```csharp
namespace Application.Tests.UseCases.Orders;

[TestFixture]
public class ApproveOrderHandlerTests
{
    private Mock<IOrderRepository> _orderRepositoryMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<IEmailService> _emailServiceMock;
    private ApproveOrderHandler _handler;

    [SetUp]
    public void Setup()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();

        _handler = new ApproveOrderHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object
        );
    }

    [Test]
    public async Task Handle_WhenOrderExists_ShouldApproveOrder()
    {
        // Arrange
        var order = new Order { Id = 1, Status = OrderStatus.Pending };
        order.Items.Add(new OrderItem { Quantity = 1, UnitPrice = 100 });

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Approved);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendOrderApprovedEmailAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WhenOrderNotFound_ShouldReturnFailure()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(999, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Order not found");
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

---

## Independence of Frameworks

La lógica de negocio no debe depender de frameworks específicos como ASP.NET, NHibernate, o FastEndpoints.

### ✅ Correcto: Domain independiente

```csharp
// Domain Layer - Sin dependencias de frameworks
namespace Domain.Entities;

public class Product
{
    public virtual int Id { get; protected set; }
    public virtual string Name { get; set; }
    public virtual decimal Price { get; set; }
    public virtual int StockQuantity { get; set; }

    public virtual Result DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail("Quantity must be positive");

        if (StockQuantity < quantity)
            return Result.Fail("Insufficient stock");

        StockQuantity -= quantity;
        return Result.Ok();
    }
}
```

### ❌ Incorrecto: Domain acoplado a frameworks

```csharp
// ❌ Domain acoplado a Entity Framework
namespace Domain.Entities;

using System.ComponentModel.DataAnnotations; // ❌ Anotaciones de EF
using System.ComponentModel.DataAnnotations.Schema; // ❌

[Table("Products")] // ❌ Atributos de persistencia
public class Product
{
    [Key] // ❌
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ❌
    public int Id { get; set; }

    [Required] // ❌ Validación en Domain (debería ser FluentValidation en Application)
    [MaxLength(200)] // ❌
    public string Name { get; set; }
}
```

---

## Independence of UI

La lógica de negocio debe funcionar independientemente de si se accede vía REST API, gRPC, GraphQL, o UI directa.

### ✅ Correcto: Application independiente de UI

```csharp
// Application Layer - No sabe nada de HTTP
namespace Application.UseCases.Products;

public class CreateProductHandler : ICreateProductHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<Product> _validator;

    public CreateProductHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IValidator<Product> validator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<Product>> Handle(string name, decimal price, CancellationToken ct)
    {
        var product = new Product
        {
            Name = name,
            Price = price,
            StockQuantity = 0
        };

        var validationResult = await _validator.ValidateAsync(product, ct);
        if (!validationResult.IsValid)
            return Result.Fail(validationResult.Errors.First().ErrorMessage);

        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.CommitAsync(ct);

        return Result.Ok(product);
    }
}

// WebAPI Layer - Adaptador para FastEndpoints
namespace WebAPI.Endpoints.Products;

public class CreateProductEndpoint : Endpoint<CreateProductRequest, ProductResponse>
{
    private readonly ICreateProductHandler _handler;

    public CreateProductEndpoint(ICreateProductHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/products");
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var result = await _handler.Handle(req.Name, req.Price, ct);

        if (result.IsFailed)
        {
            await SendAsync(new ErrorResponse
            {
                Message = result.Errors.First().Message
            }, 400, ct);
            return;
        }

        await SendCreatedAtAsync<GetProductEndpoint>(
            new { productId = result.Value.Id },
            result.Value.ToResponse(),
            cancellation: ct
        );
    }
}

// CLI Layer - Mismo handler, diferente adaptador
namespace CLI.Commands;

public class CreateProductCommand
{
    private readonly ICreateProductHandler _handler;

    public CreateProductCommand(ICreateProductHandler handler)
    {
        _handler = handler;
    }

    public async Task Execute(string name, decimal price)
    {
        var result = await _handler.Handle(name, price, CancellationToken.None);

        if (result.IsSuccess)
            Console.WriteLine($"Product created: {result.Value.Name}");
        else
            Console.WriteLine($"Error: {result.Errors.First().Message}");
    }
}
```

---

## Independence of Database

La lógica de negocio debe funcionar independientemente de si se usa SQL Server, PostgreSQL, MongoDB, o in-memory.

### ✅ Correcto: Domain agnóstico de persistencia

```csharp
// Domain - Define solo la interfaz del repositorio
namespace Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct);
    Task<IList<Product>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    Task UpdateAsync(Product product, CancellationToken ct);
}

// Infrastructure - Implementación con NHibernate
namespace Infrastructure.Repositories.NHibernate;

public class NHibernateProductRepository : IProductRepository
{
    private readonly ISession _session;

    public NHibernateProductRepository(ISession session)
    {
        _session = session;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _session.GetAsync<Product>(id, ct);
    }

    // ... otras implementaciones
}

// Infrastructure - Implementación alternativa con Entity Framework
namespace Infrastructure.Repositories.EntityFramework;

public class EfProductRepository : IProductRepository
{
    private readonly DbContext _context;

    public EfProductRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Set<Product>().FindAsync(new object[] { id }, ct);
    }

    // ... otras implementaciones
}

// Infrastructure - Implementación para testing (in-memory)
namespace Infrastructure.Repositories.InMemory;

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> _products = new();
    private int _nextId = 1;

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task AddAsync(Product product, CancellationToken ct)
    {
        product.GetType().GetProperty("Id")!.SetValue(product, _nextId++);
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    // ... otras implementaciones
}
```

---

## Organización en Capas

### Estructura de Proyecto

```
Solution/
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Exceptions/
│   │   └── Repositories/          # Interfaces
│   │
│   ├── Application/
│   │   ├── UseCases/
│   │   │   ├── Products/
│   │   │   │   ├── CreateProduct/
│   │   │   │   ├── UpdateProduct/
│   │   │   │   └── GetProduct/
│   │   │   └── Orders/
│   │   ├── Services/              # Interfaces
│   │   └── Common/
│   │
│   ├── Infrastructure/
│   │   ├── Repositories/          # Implementaciones
│   │   ├── Services/              # Implementaciones
│   │   ├── Persistence/
│   │   │   ├── Mappings/          # NHibernate mappings
│   │   │   └── Migrations/
│   │   └── Configuration/
│   │
│   └── WebAPI/
│       ├── Endpoints/
│       ├── DTOs/
│       ├── Validators/
│       └── Configuration/
│
└── tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    ├── Infrastructure.Tests/
    └── WebAPI.Tests/
```

### Flujo de Dependencias

```
WebAPI ──────┐
             ├──> Application ──> Domain
Infrastructure┘
```

- **Domain**: No referencia a nadie
- **Application**: Referencia solo a Domain
- **Infrastructure**: Referencia a Domain y Application
- **WebAPI**: Referencia a Application (y posiblemente Infrastructure para DI)

---

## Checklist

### Diseño de Nueva Funcionalidad

- [ ] He identificado las entidades de dominio necesarias
- [ ] He definido las interfaces de repositorios en Domain
- [ ] He definido las interfaces de servicios en Application
- [ ] He creado el handler en Application sin lógica de persistencia
- [ ] He implementado los repositorios en Infrastructure
- [ ] He creado el endpoint en WebAPI como adaptador
- [ ] No hay violaciones de Dependency Rule

### Code Review

- [ ] Domain no referencia a Application, Infrastructure, o WebAPI
- [ ] Application no referencia a Infrastructure o WebAPI
- [ ] Interfaces están en capas internas (Domain/Application)
- [ ] Implementaciones están en capas externas (Infrastructure/WebAPI)
- [ ] Lógica de negocio está en Domain, no en Application
- [ ] Application solo orquesta, no contiene lógica de negocio
- [ ] Infrastructure solo implementa detalles técnicos
- [ ] WebAPI solo adapta HTTP a Application

### Refactoring

- [ ] Lógica de negocio movida desde Application a Domain
- [ ] Detalles de persistencia movidos desde Application a Infrastructure
- [ ] Interfaces extraídas a capas internas
- [ ] DTOs de HTTP mantenidos en WebAPI, no en Application
- [ ] Tests actualizados para reflejar nueva estructura

---

## Referencias

- [The Clean Architecture - Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Clean Architecture - Jason Taylor](https://github.com/jasontaylordev/CleanArchitecture)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle)

---

**Última actualización:** 2025-01-13
