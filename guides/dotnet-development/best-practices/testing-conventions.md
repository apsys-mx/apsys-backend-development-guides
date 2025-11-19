# Testing Conventions

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-13

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Estructura de Tests: Arrange-Act-Assert](#estructura-de-tests-arrange-act-assert)
3. [Naming Conventions](#naming-conventions)
4. [NUnit Framework](#nunit-framework)
5. [Mocking con Moq](#mocking-con-moq)
6. [FluentAssertions para Assertions Legibles](#fluentassertions-para-assertions-legibles)
7. [AutoFixture para Test Data](#autofixture-para-test-data)
8. [Test Organization por Capa](#test-organization-por-capa)
9. [Unit Tests vs Integration Tests](#unit-tests-vs-integration-tests)
10. [Testing de Clean Architecture Layers](#testing-de-clean-architecture-layers)
11. [Anti-patrones Comunes](#anti-patrones-comunes)
12. [Checklists](#checklists)

---

## Introducción

Esta guía establece las convenciones de testing para proyectos .NET siguiendo Clean Architecture en APSYS. Define patrones para unit tests, integration tests, mocking, assertions y organización de código de prueba.

### Frameworks y Librerías

| Librería | Versión | Propósito |
|----------|---------|-----------|
| NUnit | 4.2+ | Test runner y framework principal |
| Moq | 4.20+ | Mocking framework |
| FluentAssertions | 8.5+ | Assertions expresivas y legibles |
| AutoFixture | 4.18+ | Generación de test data |
| AutoFixture.AutoMoq | 4.18+ | Integración Moq + AutoFixture |
| Microsoft.AspNetCore.Mvc.Testing | 9.0+ | Integration tests para Web API |
| FastEndpoints.Testing | 7.0+ | Testing para FastEndpoints |

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
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    [Test]
    public async Task Handle_WithValidCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var handler = new CreateUserHandler(userRepositoryMock.Object, passwordHasherMock.Object);

        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password_123");

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Result<User> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(command.Email);
        result.Value.FirstName.Should().Be(command.FirstName);

        userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.Is<User>(u => u.Email == command.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### ✅ CORRECTO: Uso de Comentarios para Secciones AAA

```csharp
namespace Domain.Tests.Entities;

using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class OrderTests
{
    [Test]
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
[Test]
public async Task CreateUser_Test()
{
    var userRepositoryMock = new Mock<IUserRepository>();
    var handler = new CreateUserHandler(userRepositoryMock.Object);
    var command = new CreateUserCommand { Email = "test@example.com" };
    var result = await handler.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
    userRepositoryMock.Verify(
        x => x.SaveOrUpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
        Times.Once);
}
```

**Problema**: Todo mezclado sin estructura clara. Difícil de leer y mantener.

### ✅ CORRECTO: Multiple Acts Requieren Multiple Tests

```csharp
[TestFixture]
public class OrderTests
{
    [Test]
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

    [Test]
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
using Moq;
using NUnit.Framework;

[TestFixture]
public class CancelOrderHandlerTests
{
    // ✅ Nombre claro: método, escenario, resultado esperado
    [Test]
    public async Task Handle_WithPendingOrder_CancelsSuccessfully()
    {
        // Test implementation
    }

    // ✅ Escenario específico con estado esperado
    [Test]
    public async Task Handle_WithShippedOrder_ReturnsValidationError()
    {
        // Test implementation
    }

    // ✅ Caso edge bien documentado
    [Test]
    public async Task Handle_WithNonExistentOrder_ReturnsNotFoundError()
    {
        // Test implementation
    }

    // ✅ Comportamiento de dominio claro
    [Test]
    public async Task Handle_WhenCancelled_SendsCancellationEmail()
    {
        // Test implementation
    }
}
```

### ✅ CORRECTO: Nombres con Given-When-Then Style

```csharp
[TestFixture]
public class UserRegistrationTests
{
    [Test]
    public async Task GivenNewEmail_WhenRegistering_ThenCreatesUser()
    {
        // Test implementation
    }

    [Test]
    public async Task GivenExistingEmail_WhenRegistering_ThenReturnsConflictError()
    {
        // Test implementation
    }
}
```

### ❌ INCORRECTO: Nombres Vagos o Genéricos

```csharp
[TestFixture]
public class UserTests
{
    // ❌ No indica qué se está probando
    [Test]
    public void Test1()
    {
    }

    // ❌ Demasiado genérico
    [Test]
    public void CreateUser()
    {
    }

    // ❌ No indica el resultado esperado
    [Test]
    public void Handle_WithCommand()
    {
    }

    // ❌ Nombre técnico que no documenta comportamiento
    [Test]
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

## NUnit Framework

**NUnit** es el framework de testing usado en proyectos APSYS.

### Ventajas de NUnit

1. **Maduro y estable** - Más de 20 años de desarrollo activo
2. **Rich attribute system** - Amplio conjunto de atributos para diferentes escenarios
3. **Flexible test execution** - Control fino sobre orden y agrupación de tests
4. **Excellent tooling** - Excelente soporte en Visual Studio, Rider, CI/CD
5. **Widely adopted** - Gran comunidad y abundante documentación

### NUnit Attributes Principales

| Attribute | Propósito |
|-----------|-----------|
| `[TestFixture]` | Marca una clase como contenedor de tests |
| `[Test]` | Marca un método como test |
| `[TestCase]` | Test parametrizado con valores inline |
| `[SetUp]` | Ejecuta antes de cada test |
| `[TearDown]` | Ejecuta después de cada test |
| `[OneTimeSetUp]` | Ejecuta una vez antes de todos los tests |
| `[OneTimeTearDown]` | Ejecuta una vez después de todos los tests |
| `[Ignore]` | Omite un test (con razón) |
| `[Category]` | Categoriza tests para filtrado |

### ✅ CORRECTO: NUnit Test Structure

```csharp
namespace Application.Tests.UseCases.Users;

using System;
using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private CreateUserHandler _handler;

    // ✅ SetUp ejecuta antes de cada test
    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new CreateUserHandler(_userRepositoryMock.Object);
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

    // ✅ TearDown para cleanup (ejecuta después de cada test)
    [TearDown]
    public void TearDown()
    {
        // Cleanup si es necesario
    }
}
```

### [Test] - Test Simple

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class EmailTests
{
    // ✅ [Test] para tests sin parámetros
    [Test]
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

### [TestCase] - Parameterized Tests

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class EmailTests
{
    // ✅ [TestCase] para tests parametrizados con múltiples casos
    [TestCase("test@example.com")]
    [TestCase("user.name@example.com")]
    [TestCase("user+tag@example.co.uk")]
    [TestCase("user_name@example-domain.com")]
    public void Create_WithValidFormats_Succeeds(string validEmail)
    {
        // Act
        var result = Email.Create(validEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(validEmail);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid")]
    [TestCase("@example.com")]
    [TestCase("user@")]
    [TestCase("user @example.com")]
    public void Create_WithInvalidFormats_Fails(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        result.IsFailed.Should().BeTrue();
    }
}
```

### [TestCaseSource] - Data From Method

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Collections.Generic;
using Application.UseCases.Orders;
using Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class CalculateOrderTotalHandlerTests
{
    // ✅ [TestCaseSource] para casos de test complejos
    [TestCaseSource(nameof(GetOrderTestCases))]
    public void Calculate_WithVariousOrders_ReturnsCorrectTotal(Order order, decimal expectedTotal)
    {
        // Arrange
        var calculator = new OrderTotalCalculator();

        // Act
        decimal total = calculator.Calculate(order);

        // Assert
        total.Should().Be(expectedTotal);
    }

    public static IEnumerable<TestCaseData> GetOrderTestCases()
    {
        // Caso 1: Orden simple
        var order1 = new Order(customerId: 1);
        order1.AddItem(CreateProduct(price: 100m), quantity: 2);
        yield return new TestCaseData(order1, 200m).SetName("Simple order with 2 items");

        // Caso 2: Orden con descuento
        var order2 = new Order(customerId: 2);
        order2.AddItem(CreateProduct(price: 100m), quantity: 3);
        order2.ApplyDiscount(discountPercentage: 10);
        yield return new TestCaseData(order2, 270m).SetName("Order with 10% discount");

        // Caso 3: Orden con múltiples items
        var order3 = new Order(customerId: 3);
        order3.AddItem(CreateProduct(price: 50m), quantity: 2);
        order3.AddItem(CreateProduct(price: 75m), quantity: 1);
        yield return new TestCaseData(order3, 175m).SetName("Order with multiple items");
    }

    private static Product CreateProduct(decimal price) =>
        new(id: 1, name: "Product", price: Money.FromDecimal(price, "USD"));
}
```

### [OneTimeSetUp] - Shared Setup Across Tests

```csharp
namespace Infrastructure.Tests.Persistence;

using System;
using Infrastructure.Persistence;
using NHibernate;
using NUnit.Framework;

// ✅ Setup costoso que se ejecuta una sola vez
[TestFixture]
public class UserRepositoryTests
{
    private ISessionFactory _sessionFactory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var configuration = new Configuration();
        configuration.Configure();
        _sessionFactory = configuration.BuildSessionFactory();
    }

    [Test]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        using var session = _sessionFactory.OpenSession();
        var repository = new UserRepository(session);

        // Act & Assert
        // Test implementation usando session compartida
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _sessionFactory?.Dispose();
    }
}
```

### [Category] - Test Categorization

```csharp
namespace Application.Tests.UseCases.Users;

using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    [Test]
    [Category("Unit")]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        // Fast unit test
    }

    [Test]
    [Category("Integration")]
    public async Task Handle_WithDatabase_PersistsUser()
    {
        // Slower integration test
    }
}
```

### Comparación: NUnit vs xUnit

| Feature | NUnit | xUnit |
|---------|-------|-------|
| Test method attribute | `[Test]` | `[Fact]` |
| Parameterized tests | `[TestCase]` | `[Theory]` + `[InlineData]` |
| Setup per test | `[SetUp]` | Constructor |
| Teardown per test | `[TearDown]` | `IDisposable.Dispose()` |
| Setup once | `[OneTimeSetUp]` | `IClassFixture<T>` |
| Test fixture | `[TestFixture]` | No attribute needed |
| Execution | Sequential by default | Parallel by default |
| Instance per test | Shared by default | Always new instance |
| Assertions | Assert.* (o FluentAssertions) | Assert.* (o FluentAssertions) |

---

## Mocking con Moq

**Moq** es el mocking framework usado en proyectos APSYS.

### ¿Por Qué Moq?

1. **Industry standard** - Más usado en la industria .NET
2. **Type-safe** - Sintaxis fuertemente tipada
3. **LINQ syntax** - Setup y verificación con expresiones lambda
4. **Flexible verification** - `Times.Once`, `Times.Never`, `Times.AtLeast(n)`
5. **Callbacks support** - Permite ejecutar código en setups

### ✅ CORRECTO: Setup Básico con Moq

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class GetUserByIdHandlerTests
{
    [Test]
    public async Task Handle_WithExistingUser_ReturnsUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var handler = new GetUserByIdHandler(userRepositoryMock.Object);

        var existingUser = new User
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "John"
        };

        // ✅ Setup simple con Returns
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

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
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    [Test]
    public async Task Handle_WithValidCommand_SavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var handler = new CreateUserHandler(userRepositoryMock.Object);
        var command = new CreateUserCommand { Email = "test@example.com" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar que se llamó exactamente 1 vez
        userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_WithInvalidEmail_DoesNotSaveUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var handler = new CreateUserHandler(userRepositoryMock.Object);
        var command = new CreateUserCommand { Email = "invalid-email" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar que NO se llamó
        userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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
using Moq;
using NUnit.Framework;

[TestFixture]
public class PlaceOrderHandlerTests
{
    [Test]
    public async Task Handle_WithValidOrder_SendsConfirmationEmail()
    {
        // Arrange
        var orderRepositoryMock = new Mock<IOrderRepository>();
        var emailServiceMock = new Mock<IEmailService>();
        var handler = new PlaceOrderHandler(orderRepositoryMock.Object, emailServiceMock.Object);

        var command = new PlaceOrderCommand
        {
            CustomerId = 1,
            CustomerEmail = "customer@example.com"
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar con predicado específico
        emailServiceMock.Verify(
            x => x.SendAsync(
                It.Is<string>(email => email == "customer@example.com"),
                It.Is<string>(subject => subject.Contains("Order Confirmation")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_WithHighValueOrder_NotifiesSalesTeam()
    {
        // Arrange
        var orderRepositoryMock = new Mock<IOrderRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var handler = new PlaceOrderHandler(orderRepositoryMock.Object, notificationServiceMock.Object);

        var command = new PlaceOrderCommand
        {
            CustomerId = 1,
            TotalAmount = 5000m // High value
        };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - ✅ Verificar argumento complejo
        notificationServiceMock.Verify(
            x => x.NotifyAsync(
                It.Is<Notification>(n =>
                    n.Type == NotificationType.HighValueOrder &&
                    n.Amount == 5000m),
                It.IsAny<CancellationToken>()),
            Times.Once);
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
using Moq;
using NUnit.Framework;

[TestFixture]
public class GetProductsHandlerTests
{
    [Test]
    public async Task Handle_WithMultipleProducts_ReturnsAllProducts()
    {
        // Arrange
        var productRepositoryMock = new Mock<IProductRepository>();

        var product1 = new Product { Id = 1, Name = "Product 1" };
        var product2 = new Product { Id = 2, Name = "Product 2" };

        // ✅ Setup para diferentes llamadas
        productRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1);

        productRepositoryMock
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product2);

        var handler = new GetProductsHandler(productRepositoryMock.Object);

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
using Moq;
using NUnit.Framework;

[TestFixture]
public class GetUserHandlerTests
{
    [Test]
    public async Task Handle_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var handler = new GetUserHandler(userRepositoryMock.Object);

        // ✅ Configurar mock para lanzar excepción
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        Func<Task> act = async () => await handler.Handle(1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }
}
```

### ✅ CORRECTO: Callbacks

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Orders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PlaceOrderHandlerTests
{
    [Test]
    public async Task Handle_WithValidOrder_GeneratesOrderId()
    {
        // Arrange
        var orderRepositoryMock = new Mock<IOrderRepository>();
        var handler = new PlaceOrderHandler(orderRepositoryMock.Object);

        var generatedId = 0;

        // ✅ Callback para capturar el argumento
        orderRepositoryMock
            .Setup(x => x.SaveOrUpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, ct) =>
            {
                order.Id = 42; // Simular ID generado
                generatedId = order.Id;
            })
            .Returns(Task.CompletedTask);

        var command = new PlaceOrderCommand { CustomerId = 1 };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        generatedId.Should().Be(42);
    }
}
```

### ✅ CORRECTO: Verificación de Sequence

```csharp
namespace Application.Tests.UseCases.Orders;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Orders;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Moq;
using NUnit.Framework;

[TestFixture]
public class PlaceOrderHandlerTests
{
    [Test]
    public async Task Handle_ExecutesOperationsInCorrectOrder()
    {
        // Arrange
        var orderRepositoryMock = new Mock<IOrderRepository>(MockBehavior.Strict);
        var emailServiceMock = new Mock<IEmailService>(MockBehavior.Strict);

        var sequence = new MockSequence();

        // ✅ Definir secuencia esperada
        orderRepositoryMock
            .InSequence(sequence)
            .Setup(x => x.SaveOrUpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        emailServiceMock
            .InSequence(sequence)
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new PlaceOrderHandler(orderRepositoryMock.Object, emailServiceMock.Object);
        var command = new PlaceOrderCommand { CustomerId = 1, CustomerEmail = "test@example.com" };

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - Si el orden es incorrecto, fallará
        orderRepositoryMock.VerifyAll();
        emailServiceMock.VerifyAll();
    }
}
```

---

## FluentAssertions para Assertions Legibles

**FluentAssertions** proporciona una sintaxis expresiva y legible para assertions.

### ✅ CORRECTO: Basic Assertions

```csharp
namespace Domain.Tests.ValueObjects;

using Domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class MoneyTests
{
    [Test]
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

    [Test]
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

    [Test]
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

### ✅ CORRECTO: Collection Assertions

```csharp
namespace Domain.Tests.Entities;

using System.Linq;
using Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class OrderTests
{
    [Test]
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

    [Test]
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

    private static Product CreateProduct(int id, string name) =>
        new(id, name, Money.FromDecimal(100m, "USD"));
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
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    [Test]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var handler = new CreateUserHandler(userRepositoryMock.Object);
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

    [Test]
    public async Task Handle_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = "test@example.com" });

        var handler = new CreateUserHandler(userRepositoryMock.Object);
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

---

## AutoFixture para Test Data

**AutoFixture** genera datos de test automáticamente, reduciendo boilerplate.

### Instalación

```xml
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="AutoFixture.AutoMoq" Version="4.18.1" />
<PackageReference Include="AutoFixture.NUnit3" Version="4.18.1" />
```

### ✅ CORRECTO: Basic AutoFixture Usage

```csharp
namespace Application.Tests.UseCases.Users;

using Application.UseCases.Users;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class UserValidatorTests
{
    [Test]
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
}
```

### ✅ CORRECTO: AutoFixture con [AutoData]

```csharp
namespace Application.Tests.UseCases.Users;

using Application.UseCases.Users;
using AutoFixture.NUnit3;
using Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class UserServiceTests
{
    // ✅ [AutoData] inyecta parámetros generados por AutoFixture
    [Test, AutoData]
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
}
```

### ✅ CORRECTO: Frozen Dependencies con AutoMoq

```csharp
namespace Application.Tests.UseCases.Users;

using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Users;
using AutoFixture;
using AutoFixture.AutoMoq;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserHandlerTests
{
    [Test]
    public async Task Handle_WithAutoFixture_CreatesUser()
    {
        // Arrange
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization());

        // ✅ Freeze crea una instancia única que se reutiliza
        var userRepositoryMock = fixture.Freeze<Mock<IUserRepository>>();
        var handler = fixture.Create<CreateUserHandler>();

        var command = fixture.Create<CreateUserCommand>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Cuándo usar AutoFixture vs Datos Manuales

Es importante saber cuándo usar cada enfoque para escribir tests efectivos:

| Usar AutoFixture cuando: | Usar datos manuales cuando: |
|--------------------------|----------------------------|
| Los valores específicos **no importan** para el test | El test verifica comportamiento con **valores específicos** |
| Quieres reducir boilerplate en setup | Necesitas documentar un **caso de negocio concreto** |
| Tests de "happy path" donde cualquier dato válido sirve | Valores límite: `null`, `""`, `Guid.Empty`, fechas límite |
| Tests de propiedades/DTOs sin lógica | Control preciso sobre los datos de entrada |
| Generar múltiples instancias para tests de colecciones | El valor es parte de la **especificación del test** |

#### Ejemplo: Mismo test, diferentes enfoques

```csharp
// ✅ AutoFixture: El email específico NO importa, solo que sea válido
[Test, AutoData]
public void Validate_WithValidUser_Succeeds(User user)
{
    var result = new UserValidator().Validate(user);
    result.IsValid.Should().BeTrue();
}

// ✅ Datos manuales: El email VACÍO es el punto del test
[Test]
public void Validate_WithEmptyEmail_ReturnsError()
{
    var user = new User { Email = "", Name = "Test" };  // Valor específico
    var result = new UserValidator().Validate(user);
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Email");
}
```

#### Patrón recomendado: Build() para control parcial

Usa `Build<T>().With().Create()` cuando necesitas controlar **algunos** valores pero no todos:

```csharp
[TestFixture]
public class ActivedModuleTests
{
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }

    // ❌ ANTES: Verbose - debes especificar TODOS los parámetros
    [Test]
    public void Validate_WithEmptyOrganizationId_ReturnsError_Verbose()
    {
        var activationDate = DateTime.UtcNow;
        var module = new ActivedModule(
            Guid.Empty,                        // ← el punto del test
            Guid.NewGuid(),                    // ← irrelevante
            activationDate,                    // ← irrelevante
            activationDate.AddDays(30),        // ← irrelevante
            ActivationStatus.Active);          // ← irrelevante

        var result = module.Validate();

        result.Should().Contain(e => e.PropertyName == "OrganizationId");
    }

    // ✅ DESPUÉS: Limpio - solo especificas lo que importa
    [Test]
    public void Validate_WithEmptyOrganizationId_ReturnsError_Clean()
    {
        var module = _fixture.Build<ActivedModule>()
            .With(m => m.OrganizationId, Guid.Empty)  // ← el punto del test
            .Create();
        // AutoFixture genera: ActivatedByUserId, ActivationDate, ActiveUntilDate, Status

        var result = module.Validate();

        result.Should().Contain(e => e.PropertyName == "OrganizationId");
    }

    // ✅ Múltiples valores controlados
    [Test]
    public void Validate_WithInvalidDateRange_ReturnsError()
    {
        var activationDate = DateTime.UtcNow;
        var module = _fixture.Build<ActivedModule>()
            .With(m => m.ActivationDate, activationDate)
            .With(m => m.ActiveUntilDate, activationDate.AddDays(-1))  // ← fecha inválida
            .Create();

        var result = module.Validate();

        result.Should().Contain(e => e.PropertyName == "ActiveUntilDate");
    }
}
```

**Beneficios de Build():**
- **Menos código** - AutoFixture genera los valores irrelevantes
- **Más claro** - Solo ves las propiedades relevantes para el test
- **Mantenible** - Si agregas propiedades a la entidad, los tests no se rompen
- **Flexible** - Combina control preciso con generación automática

#### Cuándo usar cada enfoque

| Enfoque | Usar cuando |
|---------|-------------|
| `_fixture.Create<T>()` | Todos los valores son irrelevantes |
| `_fixture.Build<T>().With().Create()` | Algunos valores específicos importan |
| `new T(...)` constructor manual | El test documenta un caso de negocio completo |

#### Patrón adicional: Combinar ambos

```csharp
[TestFixture]
public class OrderTests
{
    private Fixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
    }

    // ✅ AutoFixture completo para datos irrelevantes
    [Test]
    public void AddItem_WithValidProduct_IncreasesTotal()
    {
        var order = _fixture.Create<Order>();
        var product = _fixture.Create<Product>();

        order.AddItem(product, quantity: 2);

        order.Items.Should().NotBeEmpty();
    }

    // ✅ Datos manuales para caso específico donde TODO importa
    [Test]
    public void Submit_WithEmptyItems_ThrowsException()
    {
        var order = new Order(customerId: 1);  // Intencionalmente vacío

        Action act = () => order.Submit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }
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
using Moq;
using NUnit.Framework;

// ✅ Unit Test: Mockeando todas las dependencias
[TestFixture]
[Category("Unit")]
public class CreateUserHandlerTests
{
    [Test]
    public async Task Handle_WithValidCommand_CreatesUser()
    {
        // Arrange - Todo mockeado, sin dependencias reales
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var emailServiceMock = new Mock<IEmailService>();

        var handler = new CreateUserHandler(
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            emailServiceMock.Object);

        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!"
        };

        passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        userRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.Is<User>(u => u.Email == command.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);

        emailServiceMock.Verify(
            x => x.SendWelcomeEmailAsync(
                command.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
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
using NUnit.Framework;

// ✅ Integration Test: Usando WebApplicationFactory con dependencias reales
[TestFixture]
[Category("Integration")]
public class UsersEndpointTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task POST_Users_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Email = "integration@test.com",
            Password = "SecurePass123!",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act - Request real a la API
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(request.Email);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

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
using NUnit.Framework;

[TestFixture]
public class OrderTests
{
    [Test]
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

    [Test]
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
using Moq;
using NUnit.Framework;

[TestFixture]
public class PlaceOrderHandlerTests
{
    [Test]
    public async Task Handle_WithValidOrder_SavesAndNotifies()
    {
        // Arrange
        var orderRepositoryMock = new Mock<IOrderRepository>();
        var productRepositoryMock = new Mock<IProductRepository>();
        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<ILogger<PlaceOrderHandler>>();

        var handler = new PlaceOrderHandler(
            orderRepositoryMock.Object,
            productRepositoryMock.Object,
            emailServiceMock.Object,
            loggerMock.Object);

        var product = new Product { Id = 1, Name = "Laptop", Price = Money.FromDecimal(1500m, "USD") };
        productRepositoryMock
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

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

        orderRepositoryMock.Verify(
            x => x.SaveOrUpdateAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        emailServiceMock.Verify(
            x => x.SendOrderConfirmationAsync(
                command.CustomerEmail,
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

---

## Anti-patrones Comunes

### ❌ Anti-patrón 1: Tests Frágiles (Brittle Tests)

**Problema**: Tests que fallan por cambios menores no relacionados con el comportamiento.

```csharp
// ❌ INCORRECTO: Assertion sobre representación de string
[Test]
public void GetUser_ReturnsCorrectFormat()
{
    var user = new User { FirstName = "John", LastName = "Doe" };
    var result = user.ToString();
    result.Should().Be("User: John Doe, Created: 2025-01-13 10:30:45"); // ❌ Frágil
}

// ✅ CORRECTO: Assertions sobre comportamiento, no formato
[Test]
public void GetUser_ReturnsCorrectData()
{
    var user = new User { FirstName = "John", LastName = "Doe" };

    user.FirstName.Should().Be("John");
    user.LastName.Should().Be("Doe");
}
```

### ❌ Anti-patrón 2: Mocking Everything

**Problema**: Mockear demasiado, incluso clases simples que no tienen efectos secundarios.

```csharp
// ❌ INCORRECTO: Mockear Value Objects
[Test]
public async Task Handle_WithEmail_SendsNotification()
{
    var emailMock = new Mock<Email>(); // ❌ Email es un Value Object
    emailMock.Setup(x => x.Value).Returns("test@example.com");

    // ...
}

// ✅ CORRECTO: Usar Value Objects reales
[Test]
public async Task Handle_WithEmail_SendsNotification()
{
    var email = Email.Create("test@example.com").Value; // ✅ Instancia real

    // ...
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

- [ ] **Todas las dependencias mockeadas** con Moq
- [ ] **Sin acceso a recursos externos** (DB, filesystem, red)
- [ ] **Verificaciones de interacción** usando `.Verify()` cuando corresponde
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
- [ ] **Marcado con `[Category("Integration")]`** para filtrado

---

## Recursos Adicionales

### Documentación Oficial

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
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
- **NUnit + Moq + FluentAssertions** - Stack estándar de APSYS
- **Unit tests rápidos** - Feedback inmediato durante desarrollo
- **Integration tests completos** - Confianza en el sistema end-to-end
- **Clean Architecture** - Tests organizados por capa
- **Evitar anti-patrones** - Tests mantenibles y legibles

Un test bien escrito es tan valioso como el código de producción que valida.
