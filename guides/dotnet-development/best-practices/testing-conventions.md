# Testing Conventions

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-13

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Estructura de Tests: Arrange-Act-Assert](#estructura-de-tests-arrange-act-assert)
3. [Naming Conventions](#naming-conventions)
4. [xUnit vs NUnit: Framework Selection](#xunit-vs-nunit-framework-selection)
5. [xUnit Attributes y Setup](#xunit-attributes-y-setup)
6. [Mocking con NSubstitute](#mocking-con-nsubstitute)
7. [FluentAssertions para Assertions Legibles](#fluentassertions-para-assertions-legibles)
8. [AutoFixture para Test Data](#autofixture-para-test-data)
9. [Test Organization por Capa](#test-organization-por-capa)
10. [Unit Tests vs Integration Tests](#unit-tests-vs-integration-tests)
11. [Testing de Clean Architecture Layers](#testing-de-clean-architecture-layers)
12. [Anti-patrones Comunes](#anti-patrones-comunes)
13. [Checklists](#checklists)

---

## Introducción

Esta guía establece las convenciones de testing para proyectos .NET siguiendo Clean Architecture. Define patrones para unit tests, integration tests, mocking, assertions y organización de código de prueba.

### Frameworks y Librerías

| Librería | Versión | Propósito |
|----------|---------|-----------|
| xUnit | 2.6+ | Test runner y framework principal |
| NSubstitute | 5.1+ | Mocking framework (preferido sobre Moq) |
| FluentAssertions | 6.12+ | Assertions expresivas y legibles |
| AutoFixture | 4.18+ | Generación de test data |
| Microsoft.AspNetCore.Mvc.Testing | 9.0+ | Integration tests para Web API |

### Principios Fundamentales

1. **Tests como documentación** - Los tests deben servir como especificación del comportamiento
2. **AAA Pattern** - Arrange-Act-Assert para estructura consistente
3. **Isolation** - Cada test debe ser independiente y ejecutable en cualquier orden
4. **Fast feedback** - Unit tests rápidos, integration tests más lentos pero completos
5. **Meaningful names** - Nombres que documentan el escenario y expectativa

---

## Estructura de Tests: Arrange-Act-Assert

El patrón **Arrange-Act-Assert (AAA)** divide cada test en tres secciones claras:

1. **Arrange** - Configurar el estado inicial y dependencias
2. **Act** - Ejecutar la operación bajo prueba
3. **Assert** - Verificar el resultado esperado

### ✅ CORRECTO: Patrón AAA Bien Estructurado

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using Xunit;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new CreateUserHandler(userRepository, passwordHasher);

        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        passwordHasher.HashPassword(command.Password)
            .Returns("hashed_password_123");

        userRepository
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        Result<User> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);
        result.Value.FirstName.Should().Be(command.FirstName);

        await userRepository.Received(1).SaveOrUpdateAsync(
            Arg.Is<User>(u => u.Email == command.Email),
            Arg.Any<CancellationToken>());
    }
}
```

### ✅ CORRECTO: Uso de Comentarios para Secciones AAA

```csharp
namespace Domain.Tests.Entities;

using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class OrderTests
{
    [Fact]
    public void AddItem_WithValidProduct_IncreasesTotalAmount()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product = new Product(
            id: 100,
            name: "Laptop",
            price: Money.FromDecimal(1500.00m, "USD"));

        // Act
        order.AddItem(product, quantity: 2);

        // Assert
        order.TotalAmount.Amount.Should().Be(3000.00m);
        order.Items.Should().HaveCount(1);
        order.Items[0].Quantity.Should().Be(2);
    }
}
```

### ❌ INCORRECTO: Sin Separación de Secciones

```csharp
[Fact]
public async Task CreateUser_Test()
{
    var userRepository = Substitute.For<IUserRepository>();
    var handler = new CreateUserHandler(userRepository);
    var command = new CreateUserCommand { Email = "test@example.com" };
    var result = await handler.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
    await userRepository.Received(1).SaveOrUpdateAsync(
        Arg.Any<User>(), Arg.Any<CancellationToken>());
}
```

**Problema**: Todo mezclado sin estructura clara. Difícil de leer y mantener.

### ✅ CORRECTO: Multiple Acts Requieren Multiple Tests

```csharp
public class OrderTests
{
    [Fact]
    public void AddItem_FirstItem_SetsCorrectQuantity()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product = CreateProduct();

        // Act
        order.AddItem(product, quantity: 3);

        // Assert
        order.Items.Should().ContainSingle()
            .Which.Quantity.Should().Be(3);
    }

    [Fact]
    public void AddItem_SameProductTwice_CombinesQuantity()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product = CreateProduct();
        order.AddItem(product, quantity: 2);

        // Act
        order.AddItem(product, quantity: 3);

        // Assert
        order.Items.Should().ContainSingle()
            .Which.Quantity.Should().Be(5);
    }

    private static Product CreateProduct() =>
        new(id: 100, name: "Laptop", price: Money.FromDecimal(1500m, "USD"));
}
```

---

## Naming Conventions

Los nombres de tests deben ser **descriptivos y auto-documentados**, siguiendo el patrón:

```
[MethodName]_[Scenario]_[ExpectedBehavior]
```

### ✅ CORRECTO: Nombres Descriptivos

```csharp
namespace Application.Tests.UseCases.Orders;

using Application.UseCases.Orders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class CancelOrderHandlerTests
{
    // ✅ Nombre claro: método, escenario, resultado esperado
    [Fact]
    public async Task Handle_WithPendingOrder_CancelsSuccessfully()
    {
        // Test implementation
    }

    // ✅ Escenario específico con estado esperado
    [Fact]
    public async Task Handle_WithShippedOrder_ReturnsValidationError()
    {
        // Test implementation
    }

    // ✅ Caso edge bien documentado
    [Fact]
    public async Task Handle_WithNonExistentOrder_ReturnsNotFoundError()
    {
        // Test implementation
    }

    // ✅ Comportamiento de dominio claro
    [Fact]
    public async Task Handle_WhenCancelled_SendsCancellationEmail()
    {
        // Test implementation
    }
}
```

### ✅ CORRECTO: Nombres con Given-When-Then Style

```csharp
public class UserRegistrationTests
{
    [Fact]
    public async Task GivenNewEmail_WhenRegistering_ThenCreatesUser()
    {
        // Test implementation
    }

    [Fact]
    public async Task GivenExistingEmail_WhenRegistering_ThenReturnsConflictError()
    {
        // Test implementation
    }
}
```

### ❌ INCORRECTO: Nombres Vagos o Genéricos

```csharp
public class UserTests
{
    // ❌ No indica qué se está probando
    [Fact]
    public void Test1()
    {
    }

    // ❌ Demasiado genérico
    [Fact]
    public void CreateUser()
    {
    }

    // ❌ No indica el resultado esperado
    [Fact]
    public void Handle_WithCommand()
    {
    }

    // ❌ Nombre técnico que no documenta comportamiento
    [Fact]
    public void ShouldReturnTrue()
    {
    }
}
```

### Convenciones de Nombres de Clases de Test

```csharp
// ✅ CORRECTO: Sufijo "Tests" para la clase bajo prueba
namespace Application.Tests.UseCases.Users;

public class CreateUserHandlerTests { }
public class UserValidatorTests { }
public class PasswordHasherTests { }

// ✅ CORRECTO: Para integration tests
namespace WebApi.IntegrationTests.Endpoints;

public class UsersEndpointTests { }
public class OrdersEndpointIntegrationTests { }
```

### Estructura de Namespace para Tests

```csharp
// ✅ CORRECTO: Mirror de la estructura del código de producción
// Production: Application.UseCases.Users.CreateUserHandler
// Test:       Application.Tests.UseCases.Users.CreateUserHandlerTests

namespace Application.Tests.UseCases.Users;

public class CreateUserHandlerTests { }

// ✅ CORRECTO: Para domain entities
// Production: Domain.Entities.Order
// Test:       Domain.Tests.Entities.OrderTests

namespace Domain.Tests.Entities;

public class OrderTests { }
```

---

## xUnit vs NUnit: Framework Selection

Esta guía recomienda **xUnit** como framework principal por las siguientes razones:

### Ventajas de xUnit

1. **No state sharing** - Cada test method ejecuta en una instancia nueva de la clase
2. **Modern design** - Diseño moderno sin atributos legacy (TestFixtureSetUp, etc.)
3. **Parallel by default** - Ejecución paralela out-of-the-box
4. **Lightweight** - Sin necesidad de [SetUp] o [TearDown] complejos
5. **Used by Microsoft** - Framework usado en el desarrollo de .NET Core/ASP.NET Core

### Comparación xUnit vs NUnit

| Feature | xUnit | NUnit |
|---------|-------|-------|
| Test method attribute | `[Fact]` | `[Test]` |
| Parameterized tests | `[Theory]` + `[InlineData]` | `[TestCase]` |
| Setup per test | Constructor | `[SetUp]` |
| Teardown per test | `IDisposable.Dispose()` | `[TearDown]` |
| Setup once | `IClassFixture<T>` | `[OneTimeSetUp]` |
| Assertions | Assert.* (o FluentAssertions) | Assert.* (o FluentAssertions) |
| Execution | Parallel by default | Sequential by default |
| Instance per test | Always new instance | Shared by default |

### ✅ CORRECTO: xUnit Test Structure

```csharp
namespace Application.Tests.UseCases.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

// ✅ No necesita atributo [TestFixture] como NUnit
public class CreateUserHandlerTests : IDisposable
{
    private readonly IUserRepository _userRepository;
    private readonly CreateUserHandler _handler;

    // ✅ Constructor para setup (ejecuta antes de cada test)
    public CreateUserHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _handler = new CreateUserHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithInvalidEmail_ReturnsValidationError(string invalidEmail)
    {
        // Arrange
        var command = new CreateUserCommand { Email = invalidEmail };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    // ✅ Dispose para cleanup (ejecuta después de cada test)
    public void Dispose()
    {
        // Cleanup si es necesario
        GC.SuppressFinalize(this);
    }
}
```

### Para Usuarios de NUnit

Si tu proyecto usa NUnit, los mismos principios aplican con atributos diferentes:

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    private IUserRepository _userRepository;
    private CreateUserHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _handler = new CreateUserHandler(_userRepository);
    }

    [Test]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task Handle_WithInvalidEmail_ReturnsValidationError(string invalidEmail)
    {
        // Arrange
        var command = new CreateUserCommand { Email = invalidEmail };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup si es necesario
    }
}
```

---

## xUnit Attributes y Setup

### [Fact] - Test Simple

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class EmailTests
{
    // ✅ [Fact] para tests sin parámetros
    [Fact]
    public void Create_WithValidEmail_ReturnsEmailInstance()
    {
        // Arrange
        const string validEmail = "test@example.com";

        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.IsSuccess.Should().BeTrue();
        email.Value.Value.Should().Be(validEmail);
    }
}
```

### [Theory] + [InlineData] - Parameterized Tests

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class EmailTests
{
    // ✅ [Theory] para tests parametrizados con múltiples casos
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("user_name@example-domain.com")]
    public void Create_WithValidFormats_Succeeds(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    public void Create_WithInvalidFormats_Fails(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsFailed.Should().BeTrue();
    }
}
```

### [Theory] + [MemberData] - Data From Method

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Collections.Generic;
using Application.UseCases.Orders;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class CalculateOrderTotalHandlerTests
{
    // ✅ [MemberData] para casos de test complejos
    [Theory]
    [MemberData(nameof(GetOrderTestCases))]
    public void Calculate_WithVariousOrders_ReturnsCorrectTotal(
        Order order,
        decimal expectedTotal)
    {
        // Arrange
        var calculator = new OrderTotalCalculator();

        // Act
        decimal total = calculator.Calculate(order);

        // Assert
        total.Should().Be(expectedTotal);
    }

    public static IEnumerable<object[]> GetOrderTestCases()
    {
        // Caso 1: Orden simple
        var order1 = new Order(customerId: 1);
        order1.AddItem(CreateProduct(price: 100m), quantity: 2);
        yield return new object[] { order1, 200m };

        // Caso 2: Orden con descuento
        var order2 = new Order(customerId: 2);
        order2.AddItem(CreateProduct(price: 100m), quantity: 3);
        order2.ApplyDiscount(discountPercentage: 10);
        yield return new object[] { order2, 270m };

        // Caso 3: Orden con múltiples items
        var order3 = new Order(customerId: 3);
        order3.AddItem(CreateProduct(price: 50m), quantity: 2);
        order3.AddItem(CreateProduct(price: 75m), quantity: 1);
        yield return new object[] { order3, 175m };
    }

    private static Product CreateProduct(decimal price) =>
        new(id: 1, name: "Product", price: Money.FromDecimal(price, "USD"));
}
```

### [Theory] + [ClassData] - Data From Class

```csharp
namespace Application.Tests.UseCases.Payments;

using System.Collections;
using System.Collections.Generic;
using Application.UseCases.Payments;
using FluentAssertions;
using Xunit;

public class PaymentValidatorTests
{
    [Theory]
    [ClassData(typeof(ValidPaymentTestData))]
    public void Validate_WithValidPayment_Succeeds(decimal amount, string currency)
    {
        // Arrange
        var validator = new PaymentValidator();
        var payment = new Payment(amount, currency);

        // Act
        var result = validator.Validate(payment);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

// ✅ Clase separada para datos de test complejos
public class ValidPaymentTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 10.00m, "USD" };
        yield return new object[] { 100.50m, "EUR" };
        yield return new object[] { 1000.99m, "GBP" };
        yield return new object[] { 0.01m, "USD" }; // Mínimo permitido
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

### IClassFixture<T> - Shared Setup Across Tests

```csharp
namespace Infrastructure.Tests.Persistence;

using System;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using NHibernate;
using Xunit;

// ✅ Fixture compartida para tests costosos de setup
public class DatabaseFixture : IDisposable
{
    public ISessionFactory SessionFactory { get; }

    public DatabaseFixture()
    {
        var configuration = new Configuration();
        configuration.Configure();
        SessionFactory = configuration.BuildSessionFactory();
    }

    public void Dispose()
    {
        SessionFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// ✅ Tests que comparten la fixture
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly ISessionFactory _sessionFactory;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _sessionFactory = fixture.SessionFactory;
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        using var session = _sessionFactory.OpenSession();
        var repository = new UserRepository(session);

        // Act & Assert
        // Test implementation usando session compartida
    }
}
```

### Collection Fixtures - Shared Across Multiple Test Classes

```csharp
namespace WebApi.IntegrationTests;

using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

// ✅ Definir collection
[CollectionDefinition("WebApi Collection")]
public class WebApiCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}

// ✅ Fixture compartida entre múltiples clases de test
[Collection("WebApi Collection")]
public class UsersEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public UsersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

[Collection("WebApi Collection")]
public class OrdersEndpointTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/orders");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

---

## Mocking con NSubstitute

**NSubstitute** es el mocking framework recomendado por su sintaxis limpia y expresiva.

### ¿Por Qué NSubstitute sobre Moq?

1. **Sintaxis más limpia** - No requiere `.Object` para obtener el mock
2. **Setup más intuitivo** - `.Returns()` directamente sin `.Setup()`
3. **Verificación expresiva** - `.Received()` más legible que `.Verify()`
4. **Argument matching simple** - `Arg.Any<T>()`, `Arg.Is<T>(predicate)`

### ✅ CORRECTO: Setup Básico con NSubstitute

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetUserByIdHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new GetUserByIdHandler(userRepository);

        var existingUser = new User
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "John"
        };

        // ✅ Setup simple y directo
        userRepository
            .GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act
        var result = await handler.Handle(1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingUser);
    }
}
```

### ✅ CORRECTO: Verificación de Llamadas

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_SavesUser()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new CreateUserHandler(userRepository);
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar que se llamó exactamente 1 vez
        await userRepository.Received(1).SaveOrUpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_DoesNotSaveUser()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new CreateUserHandler(userRepository);
        var command = new CreateUserCommand { Email = "invalid-email" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar que NO se llamó
        await userRepository.DidNotReceive().SaveOrUpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>());
    }
}
```

### ✅ CORRECTO: Argument Matching

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Orders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class PlaceOrderHandlerTests
{
    [Fact]
    public async Task Handle_WithValidOrder_SendsConfirmationEmail()
    {
        // Arrange
        var orderRepository = Substitute.For<IOrderRepository>();
        var emailService = Substitute.For<IEmailService>();
        var handler = new PlaceOrderHandler(orderRepository, emailService);

        var command = new PlaceOrderCommand
        {
            CustomerId = 1,
            CustomerEmail = "customer@example.com"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar con predicado específico
        await emailService.Received(1).SendAsync(
            Arg.Is<string>(email => email == "customer@example.com"),
            Arg.Is<string>(subject => subject.Contains("Order Confirmation")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithHighValueOrder_NotifiesSalesTeam()
    {
        // Arrange
        var orderRepository = Substitute.For<IOrderRepository>();
        var notificationService = Substitute.For<INotificationService>();
        var handler = new PlaceOrderHandler(orderRepository, notificationService);

        var command = new PlaceOrderCommand
        {
            CustomerId = 1,
            TotalAmount = 5000m // High value
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar argumento complejo
        await notificationService.Received(1).NotifyAsync(
            Arg.Is<Notification>(n =>
                n.Type == NotificationType.HighValueOrder &&
                n.Amount == 5000m),
            Arg.Any<CancellationToken>());
    }
}
```

### ✅ CORRECTO: Returns para Diferentes Llamadas

```csharp
namespace Application.Tests.UseCases.Products;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Products;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetProductsHandlerTests
{
    [Fact]
    public async Task Handle_WithMultipleProducts_ReturnsAllProducts()
    {
        // Arrange
        var productRepository = Substitute.For<IProductRepository>();

        var product1 = new Product { Id = 1, Name = "Product 1" };
        var product2 = new Product { Id = 2, Name = "Product 2" };

        // ✅ Setup para diferentes llamadas
        productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(product1);

        productRepository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(product2);

        var handler = new GetProductsHandler(productRepository);

        // Act
        var result1 = await handler.GetById(1, CancellationToken.None);
        var result2 = await handler.GetById(2, CancellationToken.None);

        // Assert
        result1.Value.Should().Be(product1);
        result2.Value.Should().Be(product2);
    }
}
```

### ✅ CORRECTO: Throwing Exceptions

```csharp
namespace Application.Tests.UseCases.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

public class GetUserHandlerTests
{
    [Fact]
    public async Task Handle_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new GetUserHandler(userRepository);

        // ✅ Configurar mock para lanzar excepción
        userRepository
            .GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act
        Func<Task> act = async () => await handler.Handle(1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }
}
```

### ✅ CORRECTO: Partial Mocks con CallBase

```csharp
namespace Domain.Tests.Services;

using System.Threading.Tasks;
using Domain.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

// Servicio base con método virtual
public abstract class BaseEmailService
{
    public virtual async Task<bool> SendAsync(string to, string subject, string body)
    {
        // Implementación base
        return await ValidateEmailAsync(to);
    }

    protected abstract Task<bool> ValidateEmailAsync(string email);
}

public class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_CallsValidateEmail()
    {
        // Arrange
        var emailService = Substitute.ForPartsOf<BaseEmailService>();

        // ✅ Mockear solo el método abstracto
        emailService.ValidateEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await emailService.SendAsync(
            "test@example.com",
            "Subject",
            "Body");

        // Assert
        result.Should().BeTrue();
        await emailService.Received(1).ValidateEmailAsync("test@example.com");
    }
}
```

### Comparación: NSubstitute vs Moq

```csharp
// ✅ NSubstitute - Sintaxis limpia
var repository = Substitute.For<IUserRepository>();
repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
await repository.Received(1).SaveOrUpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

// ❌ Moq - Más verboso
var repository = new Mock<IUserRepository>();
repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
repository.Verify(r => r.SaveOrUpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
// Necesitas repository.Object para usar el mock
var handler = new Handler(repository.Object);
```

---

## FluentAssertions para Assertions Legibles

**FluentAssertions** proporciona una sintaxis expresiva y legible para assertions.

### ✅ CORRECTO: Basic Assertions

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_CreatesInstance()
    {
        // Arrange & Act
        var money = Money.FromDecimal(100.50m, "USD");

        // Assert
        // ✅ Assertions expresivas y legibles
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
        money.Should().NotBeNull();
    }

    [Fact]
    public void Add_TwoAmounts_ReturnsSum()
    {
        // Arrange
        var money1 = Money.FromDecimal(100m, "USD");
        var money2 = Money.FromDecimal(50m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsException()
    {
        // Arrange
        var usd = Money.FromDecimal(100m, "USD");
        var eur = Money.FromDecimal(50m, "EUR");

        // Act
        Action act = () => usd.Add(eur);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }
}
```

### ✅ CORRECTO: String Assertions

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class EmailTests
{
    [Fact]
    public void Value_AfterNormalization_IsLowerCase()
    {
        // Arrange & Act
        var email = Email.Create("TEST@EXAMPLE.COM");

        // Assert
        // ✅ String assertions
        email.Value.Value.Should()
            .Be("test@example.com")
            .And.NotBeNullOrWhiteSpace()
            .And.Contain("@")
            .And.EndWith(".com")
            .And.MatchRegex(@"^[^@]+@[^@]+\.[^@]+$");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    public void Value_WithValidFormat_ContainsAtSymbol(string validEmail)
    {
        // Arrange & Act
        var email = Email.Create(validEmail);

        // Assert
        email.Value.Value.Should().Contain("@");
    }
}
```

### ✅ CORRECTO: Collection Assertions

```csharp
namespace Domain.Tests.Entities;

using System.Linq;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class OrderTests
{
    [Fact]
    public void AddItem_WithMultipleProducts_ContainsAllItems()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product1 = CreateProduct(id: 1, name: "Laptop");
        var product2 = CreateProduct(id: 2, name: "Mouse");
        var product3 = CreateProduct(id: 3, name: "Keyboard");

        // Act
        order.AddItem(product1, quantity: 1);
        order.AddItem(product2, quantity: 2);
        order.AddItem(product3, quantity: 1);

        // Assert
        // ✅ Collection assertions
        order.Items.Should().HaveCount(3);
        order.Items.Should().Contain(item => item.Product.Name == "Laptop");
        order.Items.Should().Contain(item => item.Product.Name == "Mouse");
        order.Items.Should().NotContain(item => item.Quantity == 0);

        order.Items.Should().OnlyContain(item => item.Quantity > 0);
        order.Items.Should().AllSatisfy(item =>
        {
            item.Product.Should().NotBeNull();
            item.Quantity.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void RemoveItem_LastItem_EmptiesCollection()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product = CreateProduct(id: 1, name: "Laptop");
        order.AddItem(product, quantity: 1);

        // Act
        order.RemoveItem(product.Id);

        // Assert
        order.Items.Should().BeEmpty();
        order.Items.Should().HaveCount(0);
    }

    [Fact]
    public void Items_OrderedByAddedDate_AreInCorrectSequence()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product1 = CreateProduct(id: 1, name: "A");
        var product2 = CreateProduct(id: 2, name: "B");
        var product3 = CreateProduct(id: 3, name: "C");

        // Act
        order.AddItem(product1, quantity: 1);
        order.AddItem(product2, quantity: 1);
        order.AddItem(product3, quantity: 1);

        // Assert
        order.Items.Select(i => i.Product.Name)
            .Should().BeInAscendingOrder()
            .And.ContainInOrder("A", "B", "C");
    }

    private static Product CreateProduct(int id, string name) =>
        new(id, name, Money.FromDecimal(100m, "USD"));
}
```

### ✅ CORRECTO: Object Comparison

```csharp
namespace Application.Tests.UseCases.Users;

using Application.UseCases.Users;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class UserMapperTests
{
    [Fact]
    public void MapToDto_WithUser_ReturnsCorrectDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var mapper = new UserMapper();

        // Act
        var dto = mapper.MapToDto(user);

        // Assert
        // ✅ Object comparison detallada
        dto.Should().BeEquivalentTo(new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        });

        // ✅ Comparison con opciones
        dto.Should().BeEquivalentTo(user, options => options
            .Including(u => u.Id)
            .Including(u => u.Email)
            .Including(u => u.FirstName)
            .Including(u => u.LastName));
    }

    [Fact]
    public void MapToDto_IgnoresInternalProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            PasswordHash = "secret" // No debería estar en DTO
        };

        var mapper = new UserMapper();

        // Act
        var dto = mapper.MapToDto(user);

        // Assert
        dto.Should().BeEquivalentTo(user, options => options
            .ExcludingMissingMembers());
    }
}
```

### ✅ CORRECTO: Result<T> Assertions

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using Xunit;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new CreateUserHandler(userRepository);
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        Result<User> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        // ✅ FluentResults assertions
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new User { Email = "test@example.com" });

        var handler = new CreateUserHandler(userRepository);
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        Result<User> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("already exists");
    }
}
```

### ✅ CORRECTO: Exception Assertions

```csharp
namespace Domain.Tests.Entities;

using System;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

public class OrderTests
{
    [Fact]
    public void Submit_WhenEmpty_ThrowsInvariantViolation()
    {
        // Arrange
        var order = new Order(customerId: 1);

        // Act
        Action act = () => order.Submit();

        // Assert
        // ✅ Exception assertions
        act.Should().Throw<InvariantViolationException>()
            .WithMessage("*cannot be empty*")
            .And.EntityType.Should().Be("Order");
    }

    [Fact]
    public async Task AddItem_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        var order = new Order(customerId: 1);
        var product = CreateProduct();

        // Act
        Action act = () => order.AddItem(product, quantity: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("quantity")
            .WithMessage("*must be positive*");
    }

    private static Product CreateProduct() =>
        new(id: 1, name: "Product", price: Money.FromDecimal(100m, "USD"));
}
```

### ✅ CORRECTO: Date/Time Assertions

```csharp
namespace Domain.Tests.Entities;

using System;
using Domain.Entities;
using FluentAssertions;
using FluentAssertions.Extensions; // Para .Hours(), .Minutes(), etc.
using Xunit;

public class OrderTests
{
    [Fact]
    public void Create_SetsCreatedDateToNow()
    {
        // Arrange & Act
        var order = new Order(customerId: 1);

        // Assert
        // ✅ Date/Time assertions con tolerancia
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: 1.Seconds());
        order.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        order.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public void Submit_SetsSubmittedDate()
    {
        // Arrange
        var order = new Order(customerId: 1);
        order.AddItem(CreateProduct(), quantity: 1);

        // Act
        var beforeSubmit = DateTime.UtcNow;
        order.Submit();
        var afterSubmit = DateTime.UtcNow;

        // Assert
        order.SubmittedAt.Should().NotBeNull();
        order.SubmittedAt.Value.Should().BeOnOrAfter(beforeSubmit);
        order.SubmittedAt.Value.Should().BeOnOrBefore(afterSubmit);
    }

    private static Product CreateProduct() =>
        new(id: 1, name: "Product", price: Money.FromDecimal(100m, "USD"));
}
```

---

## AutoFixture para Test Data

**AutoFixture** genera datos de test automáticamente, reduciendo boilerplate y mejorando la legibilidad.

### Instalación

```xml
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
```

### ✅ CORRECTO: Basic AutoFixture Usage

```csharp
namespace Application.Tests.UseCases.Users;

using Application.UseCases.Users;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class UserValidatorTests
{
    [Fact]
    public void Validate_WithValidUser_Succeeds()
    {
        // Arrange
        var fixture = new Fixture();
        var validator = new UserValidator();

        // ✅ AutoFixture genera datos válidos automáticamente
        var user = fixture.Create<User>();

        // Act
        var result = validator.Validate(user);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleUsers_AllSucceed()
    {
        // Arrange
        var fixture = new Fixture();
        var validator = new UserValidator();

        // ✅ Generar múltiples instancias
        var users = fixture.CreateMany<User>(count: 10);

        // Act & Assert
        foreach (var user in users)
        {
            var result = validator.Validate(user);
            result.IsValid.Should().BeTrue();
        }
    }
}
```

### ✅ CORRECTO: AutoFixture con [AutoData]

```csharp
namespace Application.Tests.UseCases.Users;

using Application.UseCases.Users;
using AutoFixture.Xunit2;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class UserServiceTests
{
    // ✅ [AutoData] inyecta parámetros generados por AutoFixture
    [Theory]
    [AutoData]
    public void UpdateEmail_WithNewEmail_ChangesEmail(
        User user,
        string newEmail)
    {
        // Arrange
        var service = new UserService();

        // Act
        service.UpdateEmail(user, newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
    }

    [Theory]
    [AutoData]
    public void CalculateAge_WithBirthDate_ReturnsCorrectAge(
        User user,
        DateTime birthDate)
    {
        // Arrange
        user.BirthDate = birthDate;
        var service = new UserService();

        // Act
        var age = service.CalculateAge(user);

        // Assert
        age.Should().BeGreaterOrEqualTo(0);
    }
}
```

### ✅ CORRECTO: Customizing AutoFixture

```csharp
namespace Application.Tests.UseCases.Orders;

using System;
using Application.UseCases.Orders;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class OrderServiceTests
{
    [Fact]
    public void CalculateTotal_WithCustomizedOrder_ReturnsCorrectAmount()
    {
        // Arrange
        var fixture = new Fixture();

        // ✅ Customizar generación de datos
        fixture.Customize<Order>(composer => composer
            .With(o => o.Status, OrderStatus.Pending)
            .With(o => o.CustomerId, 123)
            .Without(o => o.Items)); // Excluir propiedad

        fixture.Customize<Money>(composer => composer
            .FromFactory(() => Money.FromDecimal(
                fixture.Create<decimal>() % 1000, // Limitar rango
                "USD")));

        var order = fixture.Create<Order>();
        var service = new OrderService();

        // Act
        var total = service.CalculateTotal(order);

        // Assert
        total.Should().NotBeNull();
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void Process_WithSpecificEmail_SendsToCorrectAddress()
    {
        // Arrange
        var fixture = new Fixture();

        // ✅ Valor específico para una propiedad
        var email = "specific@example.com";
        var user = fixture.Build<User>()
            .With(u => u.Email, email)
            .With(u => u.IsActive, true)
            .Create();

        var service = new UserService();

        // Act
        service.Process(user);

        // Assert
        user.Email.Should().Be(email);
    }
}
```

### ✅ CORRECTO: [InlineAutoData] para Mixing

```csharp
namespace Application.Tests.UseCases.Products;

using Application.UseCases.Products;
using AutoFixture.Xunit2;
using Domain.Entities;
using FluentAssertions;
using Xunit;

public class ProductServiceTests
{
    // ✅ [InlineAutoData] combina valores específicos con auto-generados
    [Theory]
    [InlineAutoData("Electronics")]
    [InlineAutoData("Clothing")]
    [InlineAutoData("Food")]
    public void GetByCategory_WithCategory_ReturnsFilteredProducts(
        string category,
        Product product1,
        Product product2)
    {
        // Arrange
        product1.Category = category;
        product2.Category = category;

        var service = new ProductService();

        // Act
        var products = service.GetByCategory(category);

        // Assert
        products.Should().NotBeEmpty();
        products.Should().OnlyContain(p => p.Category == category);
    }

    [Theory]
    [InlineAutoData(0)]
    [InlineAutoData(-10)]
    [InlineAutoData(-100)]
    public void Create_WithInvalidPrice_ThrowsException(
        decimal invalidPrice,
        Product product)
    {
        // Arrange
        var service = new ProductService();

        // Act
        Action act = () => service.Create(product.Name, invalidPrice);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("price");
    }
}
```

### ✅ CORRECTO: Frozen Dependencies

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WithAutoFixture_CreatesUser()
    {
        // Arrange
        var fixture = new Fixture()
            .Customize(new AutoNSubstituteCustomization());

        // ✅ Freeze crea una instancia única que se reutiliza
        var userRepository = fixture.Freeze<IUserRepository>();
        var handler = fixture.Create<CreateUserHandler>();

        var command = fixture.Create<CreateUserCommand>();

        userRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await userRepository.Received(1).SaveOrUpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>());
    }
}
```

### Cuándo NO Usar AutoFixture

```csharp
// ❌ NO usar para valores críticos del negocio
[Theory]
[AutoData]
public void CalculateInterest_WithRate_ReturnsCorrectAmount(
    decimal principal,
    decimal rate) // ❌ Rate podría ser cualquier valor
{
    var result = CalculateInterest(principal, rate);
    // Difícil verificar sin conocer los valores
}

// ✅ MEJOR: Valores explícitos para lógica de negocio crítica
[Theory]
[InlineData(1000, 0.05, 50)]  // principal, rate, expected
[InlineData(2000, 0.10, 200)]
public void CalculateInterest_WithKnownValues_ReturnsExpectedAmount(
    decimal principal,
    decimal rate,
    decimal expected)
{
    var result = CalculateInterest(principal, rate);
    result.Should().Be(expected);
}
```

---

## Test Organization por Capa

Los tests deben organizarse siguiendo la estructura de Clean Architecture.

### Estructura de Directorios Recomendada

```
solution/
├── src/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── WebApi/
└── tests/
    ├── Domain.Tests/
    │   ├── Entities/
    │   │   ├── OrderTests.cs
    │   │   └── UserTests.cs
    │   ├── ValueObjects/
    │   │   ├── EmailTests.cs
    │   │   └── MoneyTests.cs
    │   └── Services/
    │       └── DomainServiceTests.cs
    ├── Application.Tests/
    │   ├── UseCases/
    │   │   ├── Users/
    │   │   │   ├── CreateUserHandlerTests.cs
    │   │   │   └── GetUserHandlerTests.cs
    │   │   └── Orders/
    │   │       ├── PlaceOrderHandlerTests.cs
    │   │       └── CancelOrderHandlerTests.cs
    │   └── Validators/
    │       └── CreateUserValidatorTests.cs
    ├── Infrastructure.Tests/
    │   ├── Persistence/
    │   │   ├── Repositories/
    │   │   │   ├── UserRepositoryTests.cs
    │   │   │   └── OrderRepositoryTests.cs
    │   │   └── Mappings/
    │   │       └── UserMappingTests.cs
    │   └── ExternalServices/
    │       └── EmailServiceTests.cs
    └── WebApi.IntegrationTests/
        ├── Endpoints/
        │   ├── UsersEndpointTests.cs
        │   └── OrdersEndpointTests.cs
        └── Helpers/
            └── TestWebApplicationFactory.cs
```

### Convenciones de Namespace

```csharp
// ✅ Domain Tests
namespace Domain.Tests.Entities;
namespace Domain.Tests.ValueObjects;
namespace Domain.Tests.Services;

// ✅ Application Tests
namespace Application.Tests.UseCases.Users;
namespace Application.Tests.UseCases.Orders;
namespace Application.Tests.Validators;

// ✅ Infrastructure Tests
namespace Infrastructure.Tests.Persistence.Repositories;
namespace Infrastructure.Tests.ExternalServices;

// ✅ Integration Tests
namespace WebApi.IntegrationTests.Endpoints;
namespace WebApi.IntegrationTests.Middleware;
```

### ✅ CORRECTO: Test Organization Example

```csharp
// tests/Domain.Tests/Entities/OrderTests.cs
namespace Domain.Tests.Entities;

using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

public class OrderTests
{
    // Agrupar tests relacionados con nested classes
    public class Constructor
    {
        [Fact]
        public void WithValidCustomerId_CreatesOrder()
        {
            // Test implementation
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WithInvalidCustomerId_ThrowsException(int invalidId)
        {
            // Test implementation
        }
    }

    public class AddItem
    {
        [Fact]
        public void WithValidProduct_AddsToCollection()
        {
            // Test implementation
        }

        [Fact]
        public void WithSameProduct_CombinesQuantity()
        {
            // Test implementation
        }

        [Fact]
        public void WithNegativeQuantity_ThrowsException()
        {
            // Test implementation
        }
    }

    public class Submit
    {
        [Fact]
        public void WhenPending_ChangesStatusToSubmitted()
        {
            // Test implementation
        }

        [Fact]
        public void WhenEmpty_ThrowsInvariantViolation()
        {
            // Test implementation
        }

        [Fact]
        public void WhenAlreadySubmitted_ThrowsInvalidStateTransition()
        {
            // Test implementation
        }
    }
}
```

---

## Unit Tests vs Integration Tests

### Unit Tests

**Objetivo**: Probar una unidad de código en aislamiento, mockeando dependencias externas.

**Características**:
- ✅ Rápidos (milisegundos)
- ✅ Aislados (sin base de datos, sin red, sin filesystem)
- ✅ Determinísticos (siempre el mismo resultado)
- ✅ Ejecutables en paralelo
- ✅ No requieren configuración externa

### ✅ CORRECTO: Unit Test Example

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

// ✅ Unit Test: Mockeando todas las dependencias
public class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        // Arrange - Todo mockeado, sin dependencias reales
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var emailService = Substitute.For<IEmailService>();

        var handler = new CreateUserHandler(
            userRepository,
            passwordHasher,
            emailService);

        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        passwordHasher.HashPassword(command.Password)
            .Returns("hashed_password");

        userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await userRepository.Received(1).SaveOrUpdateAsync(
            Arg.Is<User>(u => u.Email == command.Email),
            Arg.Any<CancellationToken>());

        await emailService.Received(1).SendWelcomeEmailAsync(
            command.Email,
            Arg.Any<CancellationToken>());
    }
}
```

### Integration Tests

**Objetivo**: Probar la integración entre componentes reales (base de datos, APIs, etc.).

**Características**:
- ⏱️ Más lentos (segundos)
- 🔗 Con dependencias reales (DB, HTTP, filesystem)
- ⚙️ Requieren configuración (connection strings, test DB)
- 🔄 Pueden requerir cleanup entre tests
- 📦 Prueban el sistema end-to-end o subsistemas completos

### ✅ CORRECTO: Integration Test Example

```csharp
namespace WebApi.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

// ✅ Integration Test: Usando WebApplicationFactory con dependencias reales
public class UsersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UsersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_Users_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new
        {
            Email = "integration@test.com",
            Password = "SecurePass123!",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act - Request real a la API
        var response = await client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task GET_Users_ById_ReturnsUser()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Crear usuario primero (setup para integration test)
        var createRequest = new
        {
            Email = "gettest@example.com",
            Password = "SecurePass123!",
            FirstName = "Get",
            LastName = "Test"
        };

        var createResponse = await client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act - Request real usando ID del usuario creado
        var getResponse = await client.GetAsync($"/api/users/{createdUser!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedUser = await getResponse.Content.ReadFromJsonAsync<UserDto>();
        retrievedUser.Should().BeEquivalentTo(createdUser);
    }
}
```

### Custom WebApplicationFactory

```csharp
namespace WebApi.IntegrationTests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ✅ Custom factory para configurar test environment
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover DB de producción
            services.RemoveAll<DbContext>();

            // Agregar DB en memoria para tests
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Sobrescribir servicios externos con mocks si es necesario
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, FakeEmailService>();

            // Seed data para tests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedTestData(db);
        });
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Agregar datos iniciales para tests
        db.Users.Add(new User { Email = "existing@example.com" });
        db.SaveChanges();
    }
}

// Uso del custom factory
public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GET_Users_ReturnsSeededData()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users");
        // Test implementation
    }
}
```

### Decisión: Unit vs Integration Test

| Escenario | Tipo Recomendado | Razón |
|-----------|------------------|-------|
| Lógica de dominio (entidades, value objects) | Unit Test | Aislada, sin dependencias |
| Handlers con repositorios | Unit Test | Mockear repositorios |
| Validadores | Unit Test | Lógica pura |
| Mappers | Unit Test | Transformaciones sin side effects |
| Repositories con DB | Integration Test | Requiere DB real para validar queries |
| API Endpoints | Integration Test | End-to-end desde HTTP hasta DB |
| External API calls | Integration Test o Unit con mocks | Depende del objetivo |
| Background jobs | Integration Test | Requiere infraestructura completa |

---

## Testing de Clean Architecture Layers

### Domain Layer Tests

**Focus**: Lógica de negocio, invariantes, reglas de dominio.

```csharp
namespace Domain.Tests.Entities;

using System;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class OrderTests
{
    public class DomainRules
    {
        [Fact]
        public void Submit_WithEmptyItems_ThrowsInvariantViolation()
        {
            // Arrange
            var order = new Order(customerId: 1);

            // Act
            Action act = () => order.Submit();

            // Assert - Validar invariante de dominio
            act.Should().Throw<InvariantViolationException>()
                .WithMessage("*cannot submit empty order*");
        }

        [Fact]
        public void Cancel_WhenShipped_ThrowsInvalidStateTransition()
        {
            // Arrange
            var order = new Order(customerId: 1);
            order.AddItem(CreateProduct(), quantity: 1);
            order.Submit();
            order.Ship();

            // Act
            Action act = () => order.Cancel();

            // Assert - Validar transición de estado
            act.Should().Throw<InvalidStateTransitionException>()
                .WithMessage("*cannot cancel shipped order*");
        }

        [Fact]
        public void ApplyDiscount_ExceedsMaximum_CapsAtMaximum()
        {
            // Arrange
            var order = new Order(customerId: 1);
            order.AddItem(CreateProduct(price: 100m), quantity: 1);

            // Act - Aplicar descuento mayor al máximo
            order.ApplyDiscount(discountPercentage: 60); // Máximo es 50%

            // Assert - Validar regla de negocio
            order.DiscountPercentage.Should().Be(50);
            order.TotalAmount.Amount.Should().Be(50m);
        }
    }

    public class ValueObjectBehavior
    {
        [Fact]
        public void AddItem_WithMoney_CalculatesCorrectTotal()
        {
            // Arrange
            var order = new Order(customerId: 1);
            var product1 = CreateProduct(price: 100.50m);
            var product2 = CreateProduct(price: 50.25m);

            // Act
            order.AddItem(product1, quantity: 2);
            order.AddItem(product2, quantity: 3);

            // Assert - Validar cálculo con Value Objects
            order.TotalAmount.Amount.Should().Be(351.75m); // (100.50 * 2) + (50.25 * 3)
            order.TotalAmount.Currency.Should().Be("USD");
        }
    }

    private static Product CreateProduct(decimal price = 100m) =>
        new(id: 1, name: "Product", price: Money.FromDecimal(price, "USD"));
}
```

### Application Layer Tests

**Focus**: Orquestación de casos de uso, interacción entre servicios.

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Orders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class PlaceOrderHandlerTests
{
    public class SuccessfulScenarios
    {
        [Fact]
        public async Task Handle_WithValidOrder_SavesAndNotifies()
        {
            // Arrange
            var orderRepository = Substitute.For<IOrderRepository>();
            var productRepository = Substitute.For<IProductRepository>();
            var emailService = Substitute.For<IEmailService>();
            var logger = Substitute.For<ILogger<PlaceOrderHandler>>();

            var handler = new PlaceOrderHandler(
                orderRepository,
                productRepository,
                emailService,
                logger);

            var product = new Product { Id = 1, Name = "Laptop", Price = Money.FromDecimal(1500m, "USD") };
            productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
                .Returns(product);

            var command = new PlaceOrderCommand
            {
                CustomerId = 1,
                CustomerEmail = "customer@example.com",
                Items = new[]
                {
                    new OrderItemDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            Result<Order> result = await handler.Handle(command, CancellationToken.None);

            // Assert - Validar orquestación completa
            result.IsSuccess.Should().BeTrue();

            await orderRepository.Received(1).SaveOrUpdateAsync(
                Arg.Any<Order>(),
                Arg.Any<CancellationToken>());

            await emailService.Received(1).SendOrderConfirmationAsync(
                command.CustomerEmail,
                Arg.Any<Order>(),
                Arg.Any<CancellationToken>());
        }
    }

    public class ValidationScenarios
    {
        [Fact]
        public async Task Handle_WithNonExistentProduct_ReturnsFailure()
        {
            // Arrange
            var orderRepository = Substitute.For<IOrderRepository>();
            var productRepository = Substitute.For<IProductRepository>();
            var emailService = Substitute.For<IEmailService>();
            var logger = Substitute.For<ILogger<PlaceOrderHandler>>();

            var handler = new PlaceOrderHandler(
                orderRepository,
                productRepository,
                emailService,
                logger);

            // Product no existe
            productRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns((Product?)null);

            var command = new PlaceOrderCommand
            {
                CustomerId = 1,
                Items = new[]
                {
                    new OrderItemDto { ProductId = 999, Quantity = 1 }
                }
            };

            // Act
            Result<Order> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().Contain(e => e.Message.Contains("Product not found"));

            await orderRepository.DidNotReceive().SaveOrUpdateAsync(
                Arg.Any<Order>(),
                Arg.Any<CancellationToken>());
        }
    }

    public class ErrorHandling
    {
        [Fact]
        public async Task Handle_WhenRepositoryThrows_PropagatesException()
        {
            // Arrange
            var orderRepository = Substitute.For<IOrderRepository>();
            var productRepository = Substitute.For<IProductRepository>();
            var emailService = Substitute.For<IEmailService>();
            var logger = Substitute.For<ILogger<PlaceOrderHandler>>();

            var handler = new PlaceOrderHandler(
                orderRepository,
                productRepository,
                emailService,
                logger);

            orderRepository.SaveOrUpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Database unavailable"));

            var command = new PlaceOrderCommand { CustomerId = 1 };

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert - Excepciones inesperadas se propagan
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database unavailable");
        }
    }
}
```

### Infrastructure Layer Tests

**Focus**: Integración con sistemas externos, persistencia.

```csharp
namespace Infrastructure.Tests.Persistence.Repositories;

using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using NHibernate;
using Xunit;

// ✅ Integration test para repository con DB real (in-memory)
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly ISessionFactory _sessionFactory;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _sessionFactory = fixture.SessionFactory;
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        var repository = new UserRepository(session);

        // Seed test data
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        await session.SaveAsync(user);
        await transaction.CommitAsync();

        // Act
        using var querySession = _sessionFactory.OpenSession();
        var queryRepository = new UserRepository(querySession);
        var result = await queryRepository.GetByEmailAsync("test@example.com", CancellationToken.None);

        // Assert - Validar que NHibernate mapping funciona
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("Test");
    }

    [Fact]
    public async Task SaveOrUpdateAsync_WithNewUser_PersistsToDatabase()
    {
        // Arrange
        using var session = _sessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();

        var repository = new UserRepository(session);

        var newUser = new User
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        await repository.SaveOrUpdateAsync(newUser, CancellationToken.None);
        await transaction.CommitAsync();

        // Assert - Verificar que se guardó con ID generado
        newUser.Id.Should().BeGreaterThan(0);

        // Verificar con query independiente
        using var verifySession = _sessionFactory.OpenSession();
        var retrieved = await verifySession.GetAsync<User>(newUser.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be("new@example.com");
    }
}
```

### WebApi Layer Tests (Integration)

**Focus**: End-to-end desde HTTP request hasta respuesta.

```csharp
namespace WebApi.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class UsersEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public class POST_Users
    {
        private readonly CustomWebApplicationFactory _factory;

        public POST_Users(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task WithValidData_ReturnsCreated()
        {
            // Arrange
            var client = _factory.CreateClient();

            var request = new
            {
                Email = "newuser@example.com",
                Password = "SecurePass123!",
                FirstName = "New",
                LastName = "User"
            };

            // Act - Request HTTP real
            var response = await client.PostAsJsonAsync("/api/users", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().NotBeNull();
            user!.Email.Should().Be(request.Email);
            user.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task WithDuplicateEmail_ReturnsConflict()
        {
            // Arrange
            var client = _factory.CreateClient();

            var request = new
            {
                Email = "duplicate@example.com",
                Password = "SecurePass123!",
                FirstName = "Test",
                LastName = "User"
            };

            // Crear usuario primero
            await client.PostAsJsonAsync("/api/users", request);

            // Act - Intentar crear con mismo email
            var response = await client.PostAsJsonAsync("/api/users", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var error = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            error.Should().NotBeNull();
            error!.Title.Should().Contain("already exists");
        }

        [Theory]
        [InlineData("", "Password123!", "First", "Last")] // Email vacío
        [InlineData("invalid-email", "Password123!", "First", "Last")] // Email inválido
        [InlineData("test@example.com", "weak", "First", "Last")] // Password débil
        public async Task WithInvalidData_ReturnsBadRequest(
            string email,
            string password,
            string firstName,
            string lastName)
        {
            // Arrange
            var client = _factory.CreateClient();

            var request = new
            {
                Email = email,
                Password = password,
                FirstName = firstName,
                LastName = lastName
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/users", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errors = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            errors.Should().NotBeNull();
            errors!.Errors.Should().NotBeEmpty();
        }
    }

    public class GET_Users
    {
        private readonly CustomWebApplicationFactory _factory;

        public GET_Users(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ById_WithExistingUser_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Crear usuario
            var createRequest = new
            {
                Email = "gettest@example.com",
                Password = "SecurePass123!",
                FirstName = "Get",
                LastName = "Test"
            };

            var createResponse = await client.PostAsJsonAsync("/api/users", createRequest);
            var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

            // Act
            var response = await client.GetAsync($"/api/users/{createdUser!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            user.Should().BeEquivalentTo(createdUser);
        }

        [Fact]
        public async Task ById_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/users/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
```

---

## Anti-patrones Comunes

### ❌ Anti-patrón 1: Tests Frágiles (Brittle Tests)

**Problema**: Tests que fallan por cambios menores no relacionados con el comportamiento.

```csharp
// ❌ INCORRECTO: Assertion sobre representación de string
[Fact]
public void GetUser_ReturnsCorrectFormat()
{
    var user = new User { FirstName = "John", LastName = "Doe" };
    var result = user.ToString();
    result.Should().Be("User: John Doe, Created: 2025-01-13 10:30:45"); // ❌ Frágil
}

// ✅ CORRECTO: Assertions sobre comportamiento, no formato
[Fact]
public void GetUser_ReturnsCorrectData()
{
    var user = new User { FirstName = "John", LastName = "Doe" };

    user.FirstName.Should().Be("John");
    user.LastName.Should().Be("Doe");
}
```

### ❌ Anti-patrón 2: Tests con Lógica Compleja

**Problema**: Tests que contienen loops, conditionals o cálculos complejos.

```csharp
// ❌ INCORRECTO: Lógica compleja en el test
[Fact]
public void CalculateTotal_WithMultipleItems_ReturnsSum()
{
    var order = new Order();
    var products = GetProducts(); // ❌ Método que genera datos

    decimal expected = 0;
    foreach (var product in products) // ❌ Lógica en el test
    {
        order.AddItem(product, quantity: 2);
        expected += product.Price * 2;
    }

    order.TotalAmount.Should().Be(expected); // ❌ Esperado calculado, no conocido
}

// ✅ CORRECTO: Datos explícitos sin lógica
[Fact]
public void CalculateTotal_WithKnownItems_ReturnsExpectedSum()
{
    var order = new Order();
    order.AddItem(CreateProduct(price: 100m), quantity: 2); // 200
    order.AddItem(CreateProduct(price: 50m), quantity: 3);  // 150

    order.TotalAmount.Amount.Should().Be(350m); // ✅ Valor conocido explícito
}
```

### ❌ Anti-patrón 3: Test Interdependence

**Problema**: Tests que dependen del orden de ejecución o estado compartido.

```csharp
// ❌ INCORRECTO: Estado compartido entre tests
public class UserServiceTests
{
    private static User _sharedUser = new User(); // ❌ Estado compartido

    [Fact]
    public void Test1_CreateUser()
    {
        _sharedUser.FirstName = "John"; // ❌ Modifica estado compartido
    }

    [Fact]
    public void Test2_UpdateUser()
    {
        _sharedUser.LastName = "Doe"; // ❌ Depende de Test1
        // Fallará si Test1 no se ejecutó primero
    }
}

// ✅ CORRECTO: Cada test es independiente
public class UserServiceTests
{
    [Fact]
    public void CreateUser_SetsFirstName()
    {
        var user = new User(); // ✅ Estado local
        user.FirstName = "John";
        user.FirstName.Should().Be("John");
    }

    [Fact]
    public void UpdateUser_SetsLastName()
    {
        var user = new User(); // ✅ Nueva instancia
        user.LastName = "Doe";
        user.LastName.Should().Be("Doe");
    }
}
```

### ❌ Anti-patrón 4: Mocking Everything

**Problema**: Mockear demasiado, incluso clases simples que no tienen efectos secundarios.

```csharp
// ❌ INCORRECTO: Mockear Value Objects
[Fact]
public async Task Handle_WithEmail_SendsNotification()
{
    var emailMock = Substitute.For<Email>(); // ❌ Email es un Value Object
    emailMock.Value.Returns("test@example.com");

    // ...
}

// ✅ CORRECTO: Usar Value Objects reales
[Fact]
public async Task Handle_WithEmail_SendsNotification()
{
    var email = Email.Create("test@example.com").Value; // ✅ Instancia real

    // ...
}
```

### ❌ Anti-patrón 5: Test Fixtures Gigantes

**Problema**: Setup methods que crean demasiados objetos innecesarios.

```csharp
// ❌ INCORRECTO: Setup complejo con muchas dependencias
public class OrderHandlerTests
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IPaymentGateway _paymentGateway;
    // ... 10 más

    public OrderHandlerTests()
    {
        // ❌ Crear todas las dependencias aunque no todas se usen
        _orderRepository = Substitute.For<IOrderRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        // ... setup de todas
    }
}

// ✅ CORRECTO: Setup mínimo, crear solo lo necesario
public class OrderHandlerTests
{
    [Fact]
    public async Task PlaceOrder_WithValidData_Succeeds()
    {
        // ✅ Crear solo las dependencias necesarias para este test
        var orderRepository = Substitute.For<IOrderRepository>();
        var productRepository = Substitute.For<IProductRepository>();

        var handler = new PlaceOrderHandler(orderRepository, productRepository);

        // Test implementation
    }
}
```

### ❌ Anti-patrón 6: Assertion Roulette

**Problema**: Múltiples assertions sin mensajes claros.

```csharp
// ❌ INCORRECTO: Muchas assertions sin contexto
[Fact]
public void CreateUser_SetsAllProperties()
{
    var user = CreateUser();

    user.Should().NotBeNull(); // ❌ ¿Cuál assertion falló?
    user.Email.Should().NotBeNull();
    user.FirstName.Should().NotBeNull();
    user.LastName.Should().NotBeNull();
    user.CreatedAt.Should().NotBe(default);
    // Si alguna falla, no sabemos cuál sin revisar el stack trace
}

// ✅ CORRECTO: Assertions agrupadas con mensajes claros
[Fact]
public void CreateUser_SetsAllProperties()
{
    var user = CreateUser();

    user.Should().NotBeNull("user should be created");

    user.Email.Should().NotBeNull("email is required");
    user.FirstName.Should().NotBeNull("first name is required");
    user.LastName.Should().NotBeNull("last name is required");
    user.CreatedAt.Should().NotBe(default, "created date should be set");
}

// ✅ MEJOR: Un concepto por test
[Fact]
public void CreateUser_SetsEmail()
{
    var user = CreateUser(email: "test@example.com");
    user.Email.Should().Be("test@example.com");
}

[Fact]
public void CreateUser_SetsCreatedDate()
{
    var user = CreateUser();
    user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: 1.Seconds());
}
```

---

## Checklists

### Checklist: Escribiendo un Nuevo Test

- [ ] **Nombre descriptivo** siguiendo patrón `[Method]_[Scenario]_[Expected]`
- [ ] **Estructura AAA** clara con comentarios separando secciones
- [ ] **Una sola responsabilidad** - Test prueba un solo comportamiento
- [ ] **Independiente** - No depende de otros tests o estado compartido
- [ ] **Determinístico** - Siempre produce el mismo resultado
- [ ] **Rápido** - Unit tests ejecutan en < 100ms
- [ ] **Setup mínimo** - Solo crear dependencias necesarias
- [ ] **Assertions claras** con FluentAssertions
- [ ] **Sin lógica** - No usar loops, conditionals o cálculos complejos

### Checklist: Unit Test

- [ ] **Todas las dependencias mockeadas** con NSubstitute
- [ ] **Sin acceso a recursos externos** (DB, filesystem, red)
- [ ] **Verificaciones de interacción** usando `.Received()` cuando corresponde
- [ ] **Edge cases cubiertos** (null, empty, negative numbers, etc.)
- [ ] **Exceptions esperadas** probadas con `Should().Throw<T>()`
- [ ] **Result<T> validado** tanto Success como Failure paths

### Checklist: Integration Test

- [ ] **WebApplicationFactory configurado** para environment de test
- [ ] **Database de test** (in-memory o containerizada)
- [ ] **Cleanup entre tests** para evitar state leaking
- [ ] **Seed data mínimo** - Solo datos necesarios
- [ ] **HTTP status codes validados** correctamente
- [ ] **Response body deserializado** y validado
- [ ] **Marcado con `[Trait("Category", "Integration")]`** para filtrado

### Checklist: Code Review de Tests

- [ ] **Tests pasan en CI/CD** consistentemente
- [ ] **Coverage adecuado** - Código crítico > 80%
- [ ] **Nombres expresivos** - Se entiende qué prueban sin leer implementación
- [ ] **No hay código duplicado** - Helpers o fixtures para setup común
- [ ] **Mocks apropiados** - Solo interfaces, no clases concretas
- [ ] **Assertions suficientes** - Validan el comportamiento completo
- [ ] **Test data realista** - Valores que podrían ocurrir en producción
- [ ] **Performance aceptable** - Suite completa ejecuta en < 5 minutos

### Checklist: Mantenimiento de Test Suite

- [ ] **Tests que fallan se arreglan inmediatamente** - No skip/ignore sin razón válida
- [ ] **Refactoring de tests** junto con código de producción
- [ ] **Eliminar tests obsoletos** que ya no agregan valor
- [ ] **Actualizar fixtures** cuando cambian las dependencias
- [ ] **Revisar cobertura regularmente** - Agregar tests para gaps
- [ ] **Optimizar tests lentos** - Mover a integration suite si corresponde
- [ ] **Documentar test helpers** complejos con comentarios XML
- [ ] **Mantener consistencia** - Seguir convenciones establecidas

---

## Recursos Adicionales

### Documentación Oficial

- [xUnit Documentation](https://xunit.net/)
- [NSubstitute Documentation](https://nsubstitute.github.io/help.html)
- [FluentAssertions Documentation](https://fluentassertions.com/introduction)
- [AutoFixture Documentation](https://github.com/AutoFixture/AutoFixture)
- [Microsoft: Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

### Libros Recomendados

- **"Unit Testing Principles, Practices, and Patterns"** by Vladimir Khorikov
- **"The Art of Unit Testing"** by Roy Osherove
- **"Test Driven Development: By Example"** by Kent Beck

---

## Conclusión

Los tests son una inversión en la calidad y mantenibilidad del código. Siguiendo estas convenciones:

- **Tests como documentación** - Nombres descriptivos que explican el comportamiento esperado
- **AAA Pattern** - Estructura consistente en todos los tests
- **xUnit + NSubstitute + FluentAssertions** - Stack moderno y expresivo
- **Unit tests rápidos** - Feedback inmediato durante desarrollo
- **Integration tests completos** - Confianza en el sistema end-to-end
- **Clean Architecture** - Tests organizados por capa
- **Evitar anti-patrones** - Tests mantenibles y legibles

Un test bien escrito es tan valioso como el código de producción que valida.
