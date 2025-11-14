# Infrastructure Layer - Clean Architecture

**Versión:** 1.1.0
**Estado:** ✅ Core Concepts, NHibernate y External Services Completados
**Última actualización:** 2025-11-14

## Descripción

La capa de infraestructura contiene las **implementaciones concretas** de las interfaces definidas en Domain. Esta capa maneja persistencia de datos, servicios externos, caching, y cualquier detalle de implementación que el dominio no debe conocer.

Infrastructure es la capa que **conecta tu aplicación con el mundo exterior**: bases de datos, APIs externas, sistemas de archivos, servicios de identidad, etc.

## Responsabilidades

### ✅ SÍ hace la Infrastructure Layer

- **Implementar repositorios**: IRepository<T>, IReadOnlyRepository<T>, IUnitOfWork
- **Persistencia de datos**: ORM configuration, mappers, queries
- **Gestión de sesiones**: Session/Context lifecycle management
- **Transacciones**: Commit, Rollback, Isolation levels
- **Servicios externos**: Auth0, Email, SMS, APIs REST
- **Caching**: Memory cache, Redis, distributed cache
- **File I/O**: Almacenamiento de archivos, blob storage
- **Migraciones de BD**: FluentMigrator, EF Migrations
- **Logging infrastructure**: Configuración de loggers

### ❌ NO hace la Infrastructure Layer

- **Lógica de negocio**: Esta va en Domain
- **Orquestación de casos de uso**: Esto va en Application
- **Validación de reglas de dominio**: Esto va en Domain
- **Presentación**: Esto va en WebApi
- **Definir interfaces**: Las interfaces se definen en Domain

## Principios Fundamentales

1. **Implementación, no definición**: Infrastructure implementa, Domain define
2. **Dependency Inversion**: Infrastructure depende de Domain, nunca al revés
3. **Intercambiabilidad**: Puedes cambiar de ORM sin afectar Domain o Application
4. **Encapsulación de detalles**: Los detalles de implementación no se filtran fuera
5. **Single Responsibility**: Cada implementación hace una sola cosa bien
6. **ORM Agnostic Domain**: Domain no debe conocer el ORM usado

## Guías Disponibles

### Core Concepts (Agnóstico de ORM) ✅

Conceptos fundamentales aplicables a cualquier ORM o tecnología de persistencia.

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de Infrastructure Layer |
| [core-concepts.md](./core-concepts.md) | ✅ v1.0.0 | Conceptos fundamentales agnósticos |
| [repository-pattern.md](./repository-pattern.md) | ✅ v1.0.0 | Repository pattern implementación |
| [unit-of-work-pattern.md](./unit-of-work-pattern.md) | ✅ v1.0.0 | Unit of Work pattern |
| [transactions.md](./transactions.md) | ✅ v1.0.0 | Manejo de transacciones |
| [dependency-injection.md](./dependency-injection.md) | ✅ v1.0.0 | Registro de servicios en DI |

**Cuándo usar:** Al implementar cualquier capa de persistencia.

---

### ORM Implementations ⏳

Implementaciones específicas por ORM.

#### NHibernate
| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./orm-implementations/nhibernate/README.md) | ✅ v1.0.0 | NHibernate overview |
| [repositories.md](./orm-implementations/nhibernate/repositories.md) | ✅ v1.0.0 | NH*Repository implementations |
| [mappers.md](./orm-implementations/nhibernate/mappers.md) | ✅ v1.0.0 | ClassMapping patterns |
| [queries.md](./orm-implementations/nhibernate/queries.md) | ✅ v1.0.0 | LINQ, Dynamic LINQ, QueryOver |
| [unit-of-work.md](./orm-implementations/nhibernate/unit-of-work.md) | ✅ v1.0.0 | NHUnitOfWork implementation |
| [session-management.md](./orm-implementations/nhibernate/session-management.md) | ✅ v1.0.0 | ISession lifecycle |
| [best-practices.md](./orm-implementations/nhibernate/best-practices.md) | ✅ v1.0.0 | NHibernate best practices |

**Cuándo usar:** Al usar NHibernate como ORM.

#### Entity Framework (Futuro)
| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./orm-implementations/entity-framework/README.md) | ⏳ Futuro | Entity Framework Core overview |

**Cuándo usar:** Al usar Entity Framework Core como ORM.

---

### External Services ✅

Integraciones con servicios externos.

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./external-services/README.md) | ✅ v1.1.0 | Overview de External Services |
| [http-clients.md](./external-services/http-clients.md) | ✅ v1.0.0 | IHttpClientFactory patterns y best practices |
| [identity-providers/README.md](./external-services/identity-providers/README.md) | ✅ v1.0.0 | Overview de proveedores de identidad |
| [identity-providers/auth0.md](./external-services/identity-providers/auth0.md) | ✅ v1.0.0 | Integración completa con Auth0 |
| [identity-providers/custom-jwt.md](./external-services/identity-providers/custom-jwt.md) | ✅ v1.0.0 | Implementación de JWT personalizado |
| [caching/README.md](./external-services/caching/README.md) | ✅ v1.0.0 | Overview de caching (Memory, Distributed, Response, Output) |
| [caching/memory-cache.md](./external-services/caching/memory-cache.md) | ✅ v1.0.0 | IMemoryCache: patrones, expiration, callbacks |
| [caching/redis.md](./external-services/caching/redis.md) | ✅ v1.0.0 | Redis: IDistributedCache, StackExchange.Redis, Pub/Sub |

**Cuándo usar:** Al integrar servicios externos (Auth0, APIs, caching, etc.).

---

### Data Migrations ⏳

Sistemas de migraciones de base de datos.

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./data-migrations/README.md) | ⏳ Pendiente | Migraciones overview |
| [fluent-migrator/README.md](./data-migrations/fluent-migrator/README.md) | ⏳ Pendiente | FluentMigrator overview |
| [fluent-migrator/migration-patterns.md](./data-migrations/fluent-migrator/migration-patterns.md) | ⏳ Pendiente | Patrones de migración |

**Cuándo usar:** Al gestionar cambios de esquema de base de datos.

---

## Estructura de la Capa de Infraestructura

Basada en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```
infrastructure/
├── nhibernate/                              # Implementaciones NHibernate
│   ├── NHRepository.cs                      # Base repository con CRUD + validación
│   ├── NHReadOnlyRepository.cs             # Repository solo lectura
│   ├── NHUnitOfWork.cs                     # Unit of Work con ISession
│   ├── NHSessionFactory.cs                 # Session factory configuration
│   │
│   ├── NHUserRepository.cs                 # Repository específico: Users
│   ├── NHRoleRepository.cs                 # Repository específico: Roles
│   ├── NHPrototypeRepository.cs            # Repository específico: Prototypes
│   ├── NHTechnicalStandardRepository.cs    # Repository específico: TechnicalStandards
│   │
│   ├── mappers/                            # ClassMappings para entidades
│   │   ├── UserMapper.cs                   # Mapping de User
│   │   ├── RoleMapper.cs                   # Mapping de Role
│   │   ├── PrototypeMapper.cs              # Mapping de Prototype
│   │   ├── TechnicalStandardMapper.cs      # Mapping de TechnicalStandard
│   │   ├── PrototypeDaoMapper.cs           # Mapping de PrototypeDao (read-only)
│   │   └── TechnicalStandardDaoMapper.cs   # Mapping de TechnicalStandardDao
│   │
│   └── filtering/                           # Dynamic LINQ queries
│       ├── QueryStringParser.cs             # Parse query strings
│       ├── FilterExpressionParser.cs        # Build LINQ expressions
│       ├── FilterOperator.cs                # Filter operators (eq, gt, lt, etc.)
│       ├── Sorting.cs                       # Sorting logic
│       └── QuickSearch.cs                   # Quick search functionality
│
├── services/                                 # External services
│   ├── Auth0Service.cs                      # Auth0 integration (real)
│   └── Auth0ServiceMock.cs                  # Auth0 mock para testing
│
└── ConnectionStringBuilder.cs               # Build connection strings
```

## Flujo de Trabajo

### Implementar Repository Pattern

1. **Definir interface en Domain** → IUserRepository : IRepository<User, Guid>
2. **Implementar en Infrastructure** → NHUserRepository : NHRepository<User, Guid>, IUserRepository
3. **Registrar en DI** → services.AddScoped<IUserRepository, NHUserRepository>()
4. **Usar en Application** → IUnitOfWork.Users

### Flujo Completo: Application → Infrastructure → Database

```
Application Layer
    ↓
CreateUserUseCase.Handler
    ↓
_uoW.Users.CreateAsync(email, name)
    ↓
Infrastructure Layer
    ↓
NHUserRepository.CreateAsync()
    ↓
NHRepository.Add(user) → Validation
    ↓
_session.Save(user)
    ↓
NHibernate ORM
    ↓
SQL INSERT INTO Users (...)
    ↓
Database (PostgreSQL, SQL Server, etc.)
```

## Ejemplos Completos

### 1. Repository Base (NHRepository)

**Clase base para CRUD con validación automática:**

```csharp
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using FluentValidation;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// Implementation of the repository pattern using NHibernate ORM.
/// Extends the read-only repository to provide full CRUD operations with validation support.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey>
    where T : class, new()
{
    private readonly AbstractValidator<T> validator;

    protected NHRepository(ISession session, IServiceProvider serviceProvider)
        : base(session)
    {
        Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
        this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T>
            ?? throw new InvalidOperationException($"Validator for {typeof(T)} could not be created");
    }

    /// <summary>
    /// Adds a new entity after validating it.
    /// </summary>
    public T Add(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Save(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Saves or updates an entity after validating it.
    /// </summary>
    public T Save(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Update(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    public void Delete(T item)
    {
        this._session.Delete(item);
        this.FlushWhenNotActiveTransaction();
    }

    /// <summary>
    /// Flushes changes when there is no active transaction.
    /// </summary>
    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

### 2. Repository Específico (NHUserRepository)

**Implementación concreta con métodos de negocio:**

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHUserRepository(ISession session, IServiceProvider serviceProvider)
    : NHRepository<User, Guid>(session, serviceProvider), IUserRepository
{
    /// <summary>
    /// Create a new user with the specified email.
    /// </summary>
    public async Task<User> CreateAsync(string email, string name)
    {
        var user = new User(email, name);

        // Validación de entidad
        if (!user.IsValid())
            throw new InvalidDomainException(user.Validate());

        // Validación de unicidad
        if (await GetByEmailAsync(email) != null)
            throw new DuplicatedDomainException($"A user with email '{email}' already exists.");

        await AddAsync(user);
        FlushWhenNotActiveTransaction();
        return user;
    }

    /// <summary>
    /// Get a user by their email address.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .SingleOrDefaultAsync();
    }
}
```

### 3. Unit of Work (NHUnitOfWork)

**Gestión de sesión y transacciones:**

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using NHibernate;

namespace hashira.stone.backend.infrastructure.nhibernate;

/// <summary>
/// NHUnitOfWork manages transactions and database operations lifecycle.
/// </summary>
public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    #region CRUD Repositories
    public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
    public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
    public IPrototypeRepository Prototypes => new NHPrototypeRepository(_session, _serviceProvider);
    public ITechnicalStandardRepository TechnicalStandards
        => new NHTechnicalStandardRepository(_session, _serviceProvider);
    #endregion

    #region Read-only Repositories
    public ITechnicalStandardDaoRepository TechnicalStandardDaos
        => new NHTechnicalStandardDaoRepository(_session);
    public IPrototypeDaoRepository PrototypeDaos
        => new NHPrototypeDaoRepository(_session);
    #endregion

    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Begin transaction.
    /// </summary>
    public void BeginTransaction()
    {
        this._transaction = this._session.BeginTransaction();
    }

    /// <summary>
    /// Execute commit.
    /// </summary>
    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
        else
            throw new TransactionException("Transaction is not active");
    }

    /// <summary>
    /// Execute rollback.
    /// </summary>
    public void Rollback()
    {
        if (_transaction != null)
            _transaction.Rollback();
        else
            throw new ArgumentNullException($"No active transaction found");
    }

    /// <summary>
    /// Dispose the current session.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction?.Dispose();
            _session.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
```

## Checklists Rápidas

### ✅ Implementar Repository

- [ ] Definir interface IXRepository en Domain Layer
- [ ] Heredar de IRepository<T, TKey>
- [ ] Crear clase NHXRepository en Infrastructure
- [ ] Heredar de NHRepository<T, TKey>
- [ ] Implementar interface IXRepository
- [ ] Inyectar ISession y IServiceProvider en constructor
- [ ] Implementar métodos específicos del dominio (GetByEmail, etc.)
- [ ] Validar entidades antes de persistir
- [ ] Lanzar excepciones de dominio apropiadas
- [ ] Usar async/await para operaciones I/O
- [ ] Flush when not active transaction
- [ ] Documentar con XML comments

### ✅ Implementar Unit of Work

- [ ] Implementar IUnitOfWork interface
- [ ] Inyectar ISession en constructor
- [ ] Exponer todos los repositorios como properties
- [ ] Implementar BeginTransaction()
- [ ] Implementar Commit()
- [ ] Implementar Rollback()
- [ ] Implementar IsActiveTransaction()
- [ ] Implementar IDisposable pattern
- [ ] Dispose transaction en Dispose()
- [ ] Dispose session en Dispose()
- [ ] Lanzar excepciones apropiadas en Commit/Rollback

### ✅ Configurar Dependency Injection

- [ ] Registrar ISession como Scoped
- [ ] Registrar IUnitOfWork como Scoped
- [ ] Registrar Session Factory como Singleton
- [ ] Registrar validadores (AbstractValidator<T>)
- [ ] Registrar servicios externos (IIdentityService, etc.)
- [ ] Configurar connection string desde appsettings
- [ ] Registrar mappers si es necesario

## Patrones Clave

### 1. Repository Pattern

Abstrae el acceso a datos detrás de una interfaz:

```
┌─────────────────┐
│  Application    │  Usa
│  Use Case       │─────────┐
└─────────────────┘         │
                            ▼
                   ┌─────────────────┐
                   │     Domain      │
                   │  IUserRepository│
                   └─────────────────┘
                            ▲
                            │ Implementa
┌─────────────────┐         │
│ Infrastructure  │         │
│ NHUserRepository│─────────┘
└─────────────────┘
```

### 2. Unit of Work Pattern

Gestiona transacciones y agrupa operaciones:

```
Application
    ↓
_uoW.BeginTransaction()
    ↓
_uoW.Users.CreateAsync(...)
    ↓
_uoW.Roles.AddUserToRoleAsync(...)
    ↓
_uoW.Commit()  ← Todo o nada
```

### 3. Dependency Inversion

Infrastructure implementa, Domain define:

```
Domain Layer (Interfaces)
    ↑
    │ depende
    │
Infrastructure Layer (Implementaciones)
```

### 4. Session Per Request

Una sesión de BD por request HTTP:

```
HTTP Request
    ↓
Middleware creates ISession
    ↓
IUnitOfWork uses ISession
    ↓
Repositories use ISession
    ↓
End Request → Dispose ISession
```

## Reglas de Oro

### ✅ SÍ hacer en Infrastructure

- Implementar todas las interfaces de Domain
- Usar ORM para persistencia (NHibernate, EF Core)
- Gestionar transacciones con Unit of Work
- Validar entidades antes de persistir
- Lanzar excepciones de dominio apropiadas
- Implementar IDisposable correctamente
- Usar Dependency Injection
- Configurar mappers/fluent config
- Implementar caché cuando sea necesario
- Registrar servicios en DI container

### ❌ NO hacer en Infrastructure

- Definir lógica de negocio
- Definir interfaces (van en Domain)
- Referenciar Application o WebApi
- Exponer detalles de ORM hacia afuera
- Hacer validaciones de dominio (usar validators de Domain)
- Crear entidades con constructores públicos directamente
- Olvidar Dispose de recursos
- Hardcodear connection strings
- Ignorar transacciones
- Mezclar concerns (persistencia + lógica de negocio)

## Stack Tecnológico

### ORM
- **NHibernate 6.0+** - ORM principal (proyecto de referencia)
- **FluentValidation** - Validación en repositories
- **System.Linq.Dynamic.Core** - Dynamic LINQ queries

### External Services
- **Auth0** - Identity provider
- **HttpClient** - HTTP requests

### Migrations
- **FluentMigrator** - Database migrations

### Dependencies
```xml
<ItemGroup>
  <PackageReference Include="FluentValidation" />
  <PackageReference Include="NHibernate" />
  <PackageReference Include="System.Linq.Dynamic.Core" />
  <PackageReference Include="Microsoft.Extensions.Configuration" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\{project}.domain\{project}.domain.csproj" />
</ItemGroup>
```

**Nota:** Infrastructure solo referencia Domain, NUNCA Application o WebApi.

## Recursos Adicionales

### Documentación Oficial

- [NHibernate Documentation](https://nhibernate.info/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)

### Otras Secciones de Guías

- [Best Practices](../best-practices/README.md) - Prácticas generales
- [Domain Layer](../domain-layer/README.md) - Interfaces que implementamos
- [Application Layer](../application-layer/README.md) - Quién usa Infrastructure
- [WebApi Layer](../webapi-layer/README.md) - Configuración de DI

---

## Conclusión

**Principios Clave del Infrastructure Layer:**

1. ✅ **Implementación, no definición** - Implementa interfaces de Domain
2. ✅ **Dependency Inversion** - Depende de Domain, no al revés
3. ✅ **Encapsulación** - Detalles de implementación ocultos
4. ✅ **Repository Pattern** - Abstrae persistencia
5. ✅ **Unit of Work Pattern** - Gestiona transacciones
6. ✅ **Session Management** - Session per request lifecycle

**Flujo Mental:**

```
Application → IUnitOfWork → IRepository → NHRepository → ISession → Database
                ↑              ↑              ↑             ↑
              Domain        Domain      Infrastructure   NHibernate
```

**Responsabilidad:**
- Infrastructure **implementa** el acceso a datos
- Domain **define** las interfaces
- Application **orquesta** usando las interfaces
- Infrastructure **conecta** con el mundo exterior

---

**Última actualización:** 2025-11-14
**Mantenedor:** Equipo APSYS

## Resumen de Progreso

| Sección | Guías | Completadas | Progreso |
|---------|-------|-------------|----------|
| Core Concepts | 6 | 6 | ✅ **100% Completado** |
| NHibernate | 7 | 7 | ✅ **100% Completado** |
| External Services | 8 | 8 | ✅ **100% Completado** |
| Data Migrations | 5 | 0 | ⏳ Pendiente |
| **TOTAL** | **26** | **21** | **~81%** |
