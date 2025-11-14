# Core Concepts - Infrastructure Layer

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-14

## Descripción

Esta guía cubre los conceptos fundamentales de Infrastructure Layer aplicables a **cualquier ORM** o tecnología de persistencia. Los principios aquí documentados son agnósticos de implementación específica y forman la base arquitectónica de cualquier capa de infraestructura bien diseñada.

## Índice

- [Dependency Inversion Principle](#dependency-inversion-principle)
- [Interfaces vs Implementaciones](#interfaces-vs-implementaciones)
- [Abstraction Layers](#abstraction-layers)
- [ORM Agnostic Domain](#orm-agnostic-domain)
- [Separation of Concerns](#separation-of-concerns)
- [Session/Context Lifecycle](#sessioncontext-lifecycle)
- [Transactional Boundaries](#transactional-boundaries)
- [Testing Benefits](#testing-benefits)
- [Checklist](#checklist)
- [Mejores Prácticas](#mejores-prácticas)
- [Recursos](#recursos)

---

## Dependency Inversion Principle

### El Principio Fundamental

El **Dependency Inversion Principle (DIP)** es el pilar de Clean Architecture en Infrastructure Layer:

> Los módulos de alto nivel no deben depender de módulos de bajo nivel. Ambos deben depender de abstracciones.

### Aplicación en Clean Architecture

```
┌─────────────────────────────────────────┐
│         Application Layer               │
│    (Casos de uso - Alto nivel)          │
└───────────────┬─────────────────────────┘
                │ depende de
                ▼
┌─────────────────────────────────────────┐
│          Domain Layer                   │
│    (Interfaces - Abstracciones)         │
│                                          │
│  - IUserRepository                      │
│  - IUnitOfWork                          │
│  - IRepository<T>                       │
└───────────────▲─────────────────────────┘
                │ implementa
                │
┌─────────────────────────────────────────┐
│       Infrastructure Layer              │
│   (Implementaciones - Bajo nivel)       │
│                                          │
│  - NHUserRepository                     │
│  - NHUnitOfWork                         │
│  - NHRepository<T>                      │
└─────────────────────────────────────────┘
```

### Por qué es Importante

**✅ CORRECTO: Dependency Inversion**
```csharp
// Application Layer
public class CreateUserUseCase
{
    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;  // ✅ Depende de abstracción

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
            return Result.Ok(user);
        }
    }
}
```

**❌ INCORRECTO: Dependencia directa**
```csharp
// Application Layer
public class CreateUserUseCase
{
    public class Handler(NHUnitOfWork uoW) : ICommandHandler<Command, Result<User>>
    {
        private readonly NHUnitOfWork _uoW = uoW;  // ❌ Depende de implementación

        // Application ahora está acoplado a NHibernate
        // No puedes cambiar a Entity Framework sin romper Application
    }
}
```

### Beneficios del DIP

1. **Desacoplamiento**: Application no conoce NHibernate, Entity Framework, etc.
2. **Testabilidad**: Puedes crear mocks de IUnitOfWork para testing
3. **Intercambiabilidad**: Cambiar de ORM sin afectar Application
4. **Mantenibilidad**: Cambios en Infrastructure no impactan Application
5. **Evolución**: Puedes agregar nuevos ORMs sin romper código existente

---

## Interfaces vs Implementaciones

### Dónde Definir Qué

| Elemento | Dónde se define | Dónde se implementa | Quién lo usa |
|----------|-----------------|---------------------|--------------|
| **IRepository<T>** | Domain | Infrastructure | Application |
| **IUserRepository** | Domain | Infrastructure | Application (via IUnitOfWork) |
| **IUnitOfWork** | Domain | Infrastructure | Application |
| **NHRepository<T>** | - | Infrastructure | Infrastructure (interno) |
| **NHUserRepository** | - | Infrastructure | Infrastructure (interno) |
| **NHUnitOfWork** | - | Infrastructure | Ninguno (se inyecta como IUnitOfWork) |

### Ejemplo Completo: User Repository

**1. Interface en Domain Layer:**

```csharp
namespace YourProject.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// </summary>
public interface IUserRepository : IRepository<User, Guid>
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<User> CreateAsync(string email, string name);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
}
```

**2. Implementación en Infrastructure Layer:**

```csharp
namespace YourProject.Infrastructure.NHibernate;

/// <summary>
/// NHibernate implementation of IUserRepository.
/// </summary>
public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider)
    {
    }

    public async Task<User> CreateAsync(string email, string name)
    {
        var user = new User(email, name);

        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"User with email '{email}' already exists.");

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}
```

**3. Uso en Application Layer:**

```csharp
namespace YourProject.Application.UseCases.Users;

public abstract class CreateUserUseCase
{
    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<User>>
    {
        private readonly IUnitOfWork _uoW = uoW;

        public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // ✅ Usa IUserRepository (interface), no NHUserRepository (implementación)
                var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
                _uoW.Commit();
                return Result.Ok(user);
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

### Regla de Referencias de Proyectos

```
Domain.csproj
    ↑
    │ referencia
    │
Infrastructure.csproj
    ↑
    │ referencia
    │
Application.csproj
```

**NUNCA:**
- Application → Infrastructure (directamente)
- Domain → Infrastructure
- Infrastructure → Application

---

## Abstraction Layers

### Niveles de Abstracción

Infrastructure Layer tiene múltiples niveles de abstracción:

```
┌─────────────────────────────────────────┐
│   Application Layer                     │
│   Usa: IUnitOfWork, IUserRepository     │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   Abstraction Level 1: Interfaces       │
│   IUnitOfWork, IUserRepository          │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   Abstraction Level 2: Base Classes     │
│   NHRepository<T>, NHReadOnlyRepository │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   Abstraction Level 3: Implementations  │
│   NHUserRepository, NHUnitOfWork        │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   Abstraction Level 4: ORM              │
│   ISession (NHibernate)                 │
│   DbContext (Entity Framework)          │
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│   Database                              │
│   PostgreSQL, SQL Server, MySQL, etc.   │
└─────────────────────────────────────────┘
```

### Ventajas de Múltiples Niveles

1. **Reutilización**: NHRepository<T> se reutiliza para todas las entidades
2. **Consistencia**: Todos los repositories comparten comportamiento base
3. **Mantenibilidad**: Cambios en NHRepository<T> afectan a todos
4. **Testabilidad**: Puedes mockear en cualquier nivel
5. **Flexibilidad**: Puedes sobreescribir comportamiento en niveles específicos

---

## ORM Agnostic Domain

### El Principio

**Domain NO debe conocer el ORM usado.**

### ✅ CORRECTO: Domain Limpio

```csharp
// Domain Layer - Entidad
namespace YourProject.Domain.Entities;

public class User : AbstractDomainObject
{
    public virtual string Email { get; protected set; } = string.Empty;
    public virtual string Name { get; protected set; } = string.Empty;

    // Constructor protegido - solo para ORM
    protected User() { }

    // Constructor de negocio
    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    // ✅ No hay atributos de ORM
    // ✅ No hay [Table], [Column], etc.
    // ✅ No hay DbSet, ISession, etc.
}
```

### ❌ INCORRECTO: Domain Acoplado

```csharp
// Domain Layer - Entidad ACOPLADA
namespace YourProject.Domain.Entities;

using System.ComponentModel.DataAnnotations;  // ❌ ORM leak
using System.ComponentModel.DataAnnotations.Schema;  // ❌ ORM leak

[Table("Users")]  // ❌ Domain conoce la BD
public class User
{
    [Key]  // ❌ Domain conoce el ORM
    [Column("user_id")]  // ❌ Domain conoce el esquema
    public Guid Id { get; set; }

    [Required]  // ❌ Validación mezclada con ORM
    [MaxLength(100)]  // ❌ Constraints de BD en Domain
    public string Email { get; set; }
}
```

### Dónde va la Configuración de ORM

**Mapping separado en Infrastructure:**

```csharp
// Infrastructure Layer - NHibernate Mapper
namespace YourProject.Infrastructure.NHibernate.Mappers;

using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Table("users");
        Id(x => x.Id, m =>
        {
            m.Generator(Generators.GuidComb);
            m.Column("user_id");
        });

        Property(x => x.Email, m =>
        {
            m.Column("email");
            m.Length(100);
            m.NotNullable(true);
        });

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.Length(200);
            m.NotNullable(true);
        });
    }
}
```

**Beneficios:**
- ✅ Domain permanece limpio
- ✅ Puedes cambiar mapeos sin tocar Domain
- ✅ Puedes usar diferentes mapeos para diferentes BDs
- ✅ Testing más fácil sin dependencias de ORM

---

## Separation of Concerns

### Responsabilidades Claras

Cada capa tiene responsabilidades bien definidas:

| Capa | Responsabilidad | NO hace |
|------|-----------------|---------|
| **Domain** | Define interfaces, entidades, reglas de negocio | No implementa persistencia |
| **Application** | Orquesta casos de uso | No implementa repositorios |
| **Infrastructure** | Implementa persistencia, servicios externos | No define lógica de negocio |
| **WebApi** | Expone endpoints, DTOs, HTTP | No accede directamente a BD |

### Ejemplo: Create User Flow

**1. WebApi Layer - Solo HTTP:**
```csharp
public class CreateUserEndpoint : Endpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/api/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        // ✅ Solo maneja HTTP, delega a Application
        var command = new CreateUserUseCase.Command
        {
            Email = req.Email,
            Name = req.Name
        };

        var result = await command.ExecuteAsync(ct);

        if (result.IsSuccess)
            await SendOkAsync(Map.FromEntity(result.Value), ct);
        else
            await SendErrorsAsync(ct);
    }
}
```

**2. Application Layer - Solo Orquestación:**
```csharp
public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<User>>
{
    public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
    {
        // ✅ Solo orquesta, delega a Infrastructure
        _uoW.BeginTransaction();
        try
        {
            var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
            _uoW.Commit();
            return Result.Ok(user);
        }
        catch (Exception ex)
        {
            _uoW.Rollback();
            return Result.Fail(new Error(ex.Message).CausedBy(ex));
        }
    }
}
```

**3. Infrastructure Layer - Solo Persistencia:**
```csharp
public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public async Task<User> CreateAsync(string email, string name)
    {
        // ✅ Solo persiste, valida con Domain
        var user = new User(email, name);

        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }
}
```

**4. Domain Layer - Solo Lógica de Negocio:**
```csharp
public class User : AbstractDomainObject
{
    public User(string email, string name)
    {
        // ✅ Solo reglas de negocio
        Email = email;
        Name = name;
    }

    public override ValidationResult Validate()
    {
        // ✅ Solo validación de dominio
        var validator = new UserValidator();
        return validator.Validate(this);
    }
}
```

---

## Session/Context Lifecycle

### Concepto Agnóstico

Independientemente del ORM (NHibernate ISession, EF DbContext, Dapper IDbConnection), necesitas gestionar el ciclo de vida del contexto de BD.

### Patrón: Session Per Request

**Ciclo de vida típico:**

```
HTTP Request Starts
    ↓
Middleware crea Session/Context
    ↓
Session inyectado en IUnitOfWork
    ↓
IUnitOfWork inyectado en Handler
    ↓
Handler ejecuta operaciones
    ↓
Commit o Rollback
    ↓
HTTP Request Ends → Dispose Session
```

### Implementación Agnóstica

**NHibernate (ISession):**
```csharp
services.AddScoped<ISession>(provider =>
{
    var factory = provider.GetRequiredService<ISessionFactory>();
    return factory.OpenSession();
});
```

**Entity Framework (DbContext):**
```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
}, ServiceLifetime.Scoped);
```

**Dapper (IDbConnection):**
```csharp
services.AddScoped<IDbConnection>(provider =>
{
    var connection = new NpgsqlConnection(connectionString);
    connection.Open();
    return connection;
});
```

### Principios Comunes

1. **Scoped Lifetime**: Una instancia por request HTTP
2. **Lazy Creation**: Se crea solo cuando se necesita
3. **Automatic Disposal**: Se libera al final del request
4. **Thread Safety**: Una sesión por thread/request
5. **Transaction Support**: Puede participar en transacciones

---

## Transactional Boundaries

### Dónde Manejar Transacciones

**✅ CORRECTO: Transacciones en Application Layer**

```csharp
// Application Layer
public async Task<Result<User>> ExecuteAsync(Command command, CancellationToken ct)
{
    _uoW.BeginTransaction();  // ✅ Application inicia transacción
    try
    {
        var user = await _uoW.Users.CreateAsync(command.Email, command.Name);
        var role = await _uoW.Roles.GetDefaultRoleAsync();
        await _uoW.Roles.AddUserToRoleAsync(user.Id, role.Id);

        _uoW.Commit();  // ✅ Application hace commit
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _uoW.Rollback();  // ✅ Application hace rollback
        return Result.Fail(new Error(ex.Message).CausedBy(ex));
    }
}
```

**❌ INCORRECTO: Transacciones en Repository**

```csharp
// Infrastructure Layer - Repository
public async Task<User> CreateAsync(string email, string name)
{
    _session.BeginTransaction();  // ❌ Repository no debe iniciar transacciones
    try
    {
        var user = new User(email, name);
        await AddAsync(user);
        _session.CommitTransaction();  // ❌ Repository no controla boundaries
        return user;
    }
    catch
    {
        _session.RollbackTransaction();
        throw;
    }
}
```

### Por qué Application Controla Transacciones

1. **Business Transaction Boundary**: Application conoce el alcance del caso de uso
2. **Multiple Operations**: Un caso de uso puede involucrar múltiples repositories
3. **Atomicity**: Todo o nada a nivel de caso de uso
4. **Consistency**: Estado consistente al final del caso de uso

### Patrón: Flush When Not Active Transaction

Infrastructure debe detectar si hay transacción activa:

```csharp
// Infrastructure Layer
protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = this._session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        this._session.Flush();  // ✅ Flush solo si no hay transacción activa
}
```

**Por qué:**
- Si hay transacción: El commit hará flush automático
- Si no hay transacción: Flush inmediato para persistir cambios

---

## Testing Benefits

### Testabilidad con Interfaces

**Ventajas de usar interfaces:**

1. **Mocking Fácil**: Puedes crear mocks de IUnitOfWork
2. **Test Doubles**: Implementaciones fake para testing
3. **Isolation**: Testear Application sin BD real
4. **Speed**: Tests rápidos sin I/O de BD
5. **Determinism**: Tests predecibles sin dependencias externas

### Ejemplo: Unit Test con Mock

```csharp
using Moq;
using NUnit.Framework;

[TestFixture]
public class CreateUserUseCaseTests
{
    [Test]
    public async Task CreateUser_ValidData_ReturnsSuccess()
    {
        // Arrange
        var mockUoW = new Mock<IUnitOfWork>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockUserRepo
            .Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new User("test@example.com", "Test User"));

        mockUoW.Setup(u => u.Users).Returns(mockUserRepo.Object);
        mockUoW.Setup(u => u.BeginTransaction());
        mockUoW.Setup(u => u.Commit());

        var handler = new CreateUserUseCase.Handler(mockUoW.Object);
        var command = new CreateUserUseCase.Command
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        // Act
        var result = await handler.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Email, Is.EqualTo("test@example.com"));

        mockUoW.Verify(u => u.BeginTransaction(), Times.Once);
        mockUoW.Verify(u => u.Commit(), Times.Once);
        mockUserRepo.Verify(r => r.CreateAsync("test@example.com", "Test User"), Times.Once);
    }
}
```

### Ejemplo: Integration Test con In-Memory DB

```csharp
[TestFixture]
public class UserRepositoryIntegrationTests
{
    private ISessionFactory _sessionFactory;
    private ISession _session;

    [SetUp]
    public void SetUp()
    {
        // ✅ Usar BD en memoria o test database
        _sessionFactory = CreateInMemorySessionFactory();
        _session = _sessionFactory.OpenSession();
    }

    [Test]
    public async Task CreateAsync_ValidUser_PersistsToDatabase()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var repository = new NHUserRepository(_session, serviceProvider);

        // Act
        var user = await repository.CreateAsync("test@example.com", "Test User");

        // Assert
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));

        var retrieved = await repository.GetByEmailAsync("test@example.com");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved.Email, Is.EqualTo("test@example.com"));
    }

    [TearDown]
    public void TearDown()
    {
        _session?.Dispose();
        _sessionFactory?.Dispose();
    }
}
```

---

## Checklist

### ✅ Dependency Inversion

- [ ] Interfaces definidas en Domain Layer
- [ ] Implementaciones en Infrastructure Layer
- [ ] Application depende solo de interfaces
- [ ] Infrastructure referencia Domain, NO al revés
- [ ] Ninguna referencia directa a clases concretas de ORM en Application

### ✅ ORM Agnostic Domain

- [ ] Entidades sin atributos de ORM ([Table], [Column], etc.)
- [ ] Mapeos en archivos separados (ClassMapping, EntityTypeConfiguration)
- [ ] Constructor protegido para ORM
- [ ] Constructor público para lógica de negocio
- [ ] Validación en Domain, no en mapeos

### ✅ Separation of Concerns

- [ ] Domain: Solo lógica de negocio
- [ ] Application: Solo orquestación
- [ ] Infrastructure: Solo persistencia
- [ ] WebApi: Solo HTTP/presentación
- [ ] Sin mixing de responsabilidades

### ✅ Session Lifecycle

- [ ] Session/Context registrado como Scoped
- [ ] Una instancia por HTTP request
- [ ] Dispose automático al final del request
- [ ] Thread-safe (una sesión por thread)

### ✅ Transactional Boundaries

- [ ] Application controla BeginTransaction
- [ ] Application controla Commit
- [ ] Application controla Rollback
- [ ] Repository detecta transacción activa
- [ ] Flush solo cuando no hay transacción activa

### ✅ Testing

- [ ] Interfaces permiten mocking
- [ ] Unit tests usan mocks
- [ ] Integration tests usan BD real/in-memory
- [ ] Tests rápidos y determinísticos

---

## Mejores Prácticas

### 1. Siempre Usar Interfaces

```csharp
// ✅ CORRECTO
public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<User>>

// ❌ INCORRECTO
public class Handler(NHUnitOfWork uoW) : ICommandHandler<Command, Result<User>>
```

### 2. Domain Limpio de ORM

```csharp
// ✅ CORRECTO: Domain puro
public class User : AbstractDomainObject
{
    public virtual string Email { get; protected set; }
}

// ❌ INCORRECTO: Domain con atributos ORM
[Table("users")]
public class User
{
    [Column("email")]
    public string Email { get; set; }
}
```

### 3. Mapeos Separados

```csharp
// ✅ CORRECTO: Mapping en Infrastructure
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Table("users");
        Property(x => x.Email, m => m.Column("email"));
    }
}
```

### 4. Application Controla Transacciones

```csharp
// ✅ CORRECTO: Transaction en Application
_uoW.BeginTransaction();
try
{
    // Operations
    _uoW.Commit();
}
catch
{
    _uoW.Rollback();
    throw;
}
```

### 5. Session Scoped

```csharp
// ✅ CORRECTO: Session como Scoped
services.AddScoped<ISession>(provider =>
{
    var factory = provider.GetRequiredService<ISessionFactory>();
    return factory.OpenSession();
});
```

### 6. Repository No Inicia Transacciones

```csharp
// ✅ CORRECTO: Repository solo persiste
public async Task<User> CreateAsync(string email, string name)
{
    var user = new User(email, name);
    await AddAsync(user);
    FlushWhenNotActiveTransaction();  // ✅ Detecta transacción activa
    return user;
}
```

### 7. Interfaces con Métodos de Negocio

```csharp
// ✅ CORRECTO: Métodos específicos del dominio
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User> CreateAsync(string email, string name);
    Task<User?> GetByEmailAsync(string email);
}

// ❌ INCORRECTO: Solo CRUD genérico
public interface IUserRepository : IRepository<User, Guid>
{
    // Sin métodos específicos de negocio
}
```

### 8. Testing con Mocks e Integration Tests

```csharp
// ✅ Unit Test: Mock
var mockUoW = new Mock<IUnitOfWork>();
var handler = new CreateUserUseCase.Handler(mockUoW.Object);

// ✅ Integration Test: BD real/in-memory
var repository = new NHUserRepository(session, serviceProvider);
var user = await repository.CreateAsync("test@example.com", "Test");
```

---

## Recursos

### Patrones de Diseño

- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) - Martin Fowler
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html) - Martin Fowler
- [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle) - SOLID

### Clean Architecture

- [The Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Uncle Bob
- [Clean Architecture Book](https://www.amazon.com/Clean-Architecture-Craftsmans-Software-Structure/dp/0134494164) - Robert C. Martin

### ORMs

- [NHibernate](https://nhibernate.info/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Dapper](https://github.com/DapperLib/Dapper)

### Guías Relacionadas

- [Repository Pattern](./repository-pattern.md) - Implementación detallada
- [Unit of Work Pattern](./unit-of-work-pattern.md) - Implementación detallada
- [Transactions](./transactions.md) - Manejo de transacciones
- [Dependency Injection](./dependency-injection.md) - Registro en DI

---

**Siguiente**: [Repository Pattern](./repository-pattern.md)
**Anterior**: [README](./README.md)
**Índice**: [Infrastructure Layer](./README.md)
