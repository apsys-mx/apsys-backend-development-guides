# NHibernate Implementation Guide
**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [¿Qué es NHibernate?](#qué-es-nhibernate)
2. [Por qué NHibernate](#por-qué-nhibernate)
3. [Stack Tecnológico](#stack-tecnológico)
4. [Arquitectura de Implementación](#arquitectura-de-implementación)
5. [Patrones Principales](#patrones-principales)
6. [Guías Disponibles](#guías-disponibles)
7. [Configuración Inicial](#configuración-inicial)
8. [Ejemplos Rápidos](#ejemplos-rápidos)
9. [Mejores Prácticas](#mejores-prácticas)
10. [Referencias](#referencias)

---

## ¿Qué es NHibernate?

**NHibernate** es un **Object-Relational Mapper (ORM)** maduro y completo para .NET Framework y .NET Core. Es el port oficial de **Hibernate** (Java) para el ecosistema .NET.

### 🎯 Características Clave

- ✅ **ORM Completo**: Mapeo bidireccional entre objetos C# y tablas de base de datos
- ✅ **LINQ Provider**: Consultas type-safe usando LINQ to NHibernate
- ✅ **Mapping by Code**: Configuración de mapeos mediante código C# (sin XML)
- ✅ **Lazy Loading**: Carga diferida de entidades y colecciones
- ✅ **Caching**: Soporte para first-level cache (sesión) y second-level cache (distribuido)
- ✅ **Database Agnostic**: Soporta múltiples motores de BD (PostgreSQL, SQL Server, MySQL, Oracle, etc.)
- ✅ **Transactions**: Manejo robusto de transacciones y concurrencia
- ✅ **Query Flexibility**: LINQ, HQL, SQL Nativo, QueryOver, Criteria API

---

## Por qué NHibernate

### ✅ Ventajas sobre otros ORMs

| Característica | NHibernate | Entity Framework Core |
|----------------|------------|----------------------|
| **Madurez** | ✅ 15+ años | ⚠️ Relativamente nuevo |
| **Mapping by Code** | ✅ Fluent API completo | ✅ Fluent API |
| **Lazy Loading** | ✅ Granular y configurable | ⚠️ Limitado |
| **Caching** | ✅ 1st y 2nd level cache | ⚠️ Solo en memoria |
| **Database Agnostic** | ✅ Excelente | ✅ Bueno |
| **Performance** | ✅ Optimizado | ✅ Optimizado |
| **Batching** | ✅ Avanzado | ⚠️ Básico |
| **Filtering** | ✅ Filters dinámicos | ❌ No nativo |
| **Community** | ✅ Madura | ✅ Grande |

### 🏆 Casos de Uso Ideales

- ✅ **Aplicaciones empresariales** con lógica de negocio compleja
- ✅ **Sistemas legacy** que migran de Hibernate (Java)
- ✅ **Multi-tenant applications** con filtros dinámicos
- ✅ **Alta concurrencia** con caching distribuido
- ✅ **Domain-Driven Design** con entidades ricas

---

## Stack Tecnológico

### 📦 Proyecto de Referencia: hashira.stone.backend

```xml
<ItemGroup>
  <!-- NHibernate Core -->
  <PackageReference Include="NHibernate" Version="6.0.0+" />

  <!-- LINQ Provider -->
  <PackageReference Include="NHibernate.Linq" Version="6.0.0+" />

  <!-- PostgreSQL Driver -->
  <PackageReference Include="Npgsql" Version="8.0.0+" />

  <!-- Dynamic LINQ for Queries -->
  <PackageReference Include="System.Linq.Dynamic.Core" Version="1.3.0+" />

  <!-- Validation -->
  <PackageReference Include="FluentValidation" Version="11.0.0+" />
</ItemGroup>
```

### 🗄️ Base de Datos Soportada

- **PostgreSQL** (proyecto actual)
- SQL Server
- MySQL
- Oracle
- SQLite

---

## Arquitectura de Implementación

### 📁 Estructura de Carpetas

```
infrastructure/nhibernate/
├── NHSessionFactory.cs              # Session factory configuration
├── ConnectionStringBuilder.cs        # Connection string builder
│
├── NHReadOnlyRepository.cs          # Base repository (read-only)
├── NHRepository.cs                   # Base repository (CRUD + validation)
├── NHUnitOfWork.cs                   # Unit of Work implementation
│
├── mappers/                          # Entity mappings
│   ├── UserMapper.cs                 # User entity mapping
│   ├── RoleMapper.cs                 # Role entity mapping
│   ├── PrototypeMapper.cs            # Prototype entity mapping
│   ├── TechnicalStandardMapper.cs    # TechnicalStandard mapping
│   ├── PrototypeDaoMapper.cs         # Read-only DAO mapping
│   └── TechnicalStandardDaoMapper.cs # Read-only DAO mapping
│
├── filtering/                        # Dynamic query filtering
│   ├── QueryStringParser.cs          # Parse query strings
│   ├── FilterExpressionParser.cs     # Build LINQ expressions
│   ├── FilterOperator.cs             # Filter operators
│   ├── RelationalOperator.cs         # Relational operators
│   ├── Sorting.cs                    # Sorting logic
│   ├── QuickSearch.cs                # Quick search functionality
│   └── StringExtender.cs             # String utilities
│
├── NHUserRepository.cs               # User repository
├── NHRoleRepository.cs               # Role repository
├── NHPrototypeRepository.cs          # Prototype repository
├── NHTechnicalStandardRepository.cs  # TechnicalStandard repository
├── NHPrototypeDaoRepository.cs       # Read-only Prototype DAO
└── NHTechnicalStandardDaoRepository.cs # Read-only TechnicalStandard DAO
```

---

## Patrones Principales

### 1. 🗺️ Mapping by Code (ClassMapping<T>)

NHibernate permite definir mapeos **sin XML**, usando clases C#:

```csharp
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema("app");
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
        });

        // Many-to-Many relationship
        Bag(x => x.Roles, map =>
        {
            map.Table("user_in_roles");
            map.Key(k => k.Column("user_id"));
            map.Cascade(Cascade.All);
        },
        map => map.ManyToMany(m =>
        {
            m.Column("role_id");
            m.Class(typeof(Role));
        }));
    }
}
```

**Ventajas**:
- ✅ **Type-safe**: Detección de errores en compile-time
- ✅ **IntelliSense**: Autocompletado en VS/Rider
- ✅ **Refactoring-friendly**: Renombrar propiedades actualiza mapeos
- ✅ **Sin XML**: Todo en código C#

---

### 2. 📚 Repository Pattern

Repositorios base genéricos con **LINQ to NHibernate**:

```csharp
public class NHReadOnlyRepository<T, TKey> : IReadOnlyRepository<T, TKey>
    where T : class, new()
{
    protected internal readonly ISession _session;

    public NHReadOnlyRepository(ISession session)
    {
        _session = session;
    }

    // Queries usando LINQ
    public IEnumerable<T> Get(Expression<Func<T, bool>> query)
        => _session.Query<T>().Where(query);

    public async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>> query,
        CancellationToken cancellationToken = default)
        => await _session.Query<T>()
            .Where(query)
            .ToListAsync(cancellationToken);

    // Paginación y sorting
    public IEnumerable<T> Get(
        Expression<Func<T, bool>> query,
        int page,
        int pageSize,
        SortingCriteria sortingCriteria)
        => _session.Query<T>()
            .Where(query)
            .OrderBy(sortingCriteria.ToExpression())
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
}
```

---

### 3. 🔄 Unit of Work Pattern

Gestión de transacciones y coordinación de repositorios:

```csharp
public class NHUnitOfWork : IUnitOfWork
{
    private readonly ISession _session;
    private readonly IServiceProvider _serviceProvider;
    private ITransaction? _transaction;

    // Repositorios creados on-demand
    public IUserRepository Users
        => new NHUserRepository(_session, _serviceProvider);

    public IRoleRepository Roles
        => new NHRoleRepository(_session, _serviceProvider);

    public void BeginTransaction()
    {
        _transaction = _session.BeginTransaction();
    }

    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
    }

    public void Rollback()
    {
        _transaction?.Rollback();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _session.Dispose();
    }
}
```

---

### 4. 🔍 LINQ to NHibernate

Consultas **type-safe** usando LINQ estándar:

```csharp
// Query simple
var activeUsers = await _session.Query<User>()
    .Where(u => u.IsActive)
    .ToListAsync();

// Query con join
var usersWithRoles = await _session.Query<User>()
    .Where(u => u.Roles.Any(r => r.Name == "Admin"))
    .ToListAsync();

// Query con paginación
var pagedUsers = await _session.Query<User>()
    .OrderBy(u => u.Email)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Query con proyección
var userDtos = await _session.Query<User>()
    .Select(u => new UserDto
    {
        Id = u.Id,
        Email = u.Email,
        RoleCount = u.Roles.Count
    })
    .ToListAsync();
```

---

### 5. 🎯 Dynamic LINQ Filtering

Sistema avanzado de filtrado dinámico desde query strings:

```csharp
public async Task<GetManyAndCountResult<User>> GetManyAndCountAsync(
    string? query,
    string defaultSorting,
    CancellationToken cancellationToken = default)
{
    // Parse query string
    var queryStringParser = new QueryStringParser(query);
    int pageNumber = queryStringParser.ParsePageNumber();
    int pageSize = queryStringParser.ParsePageSize();

    // Parse sorting
    Sorting sorting = queryStringParser.ParseSorting<User>(defaultSorting);

    // Parse filters
    IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<User>();
    var expression = FilterExpressionParser.ParsePredicate<User>(filters);

    // Execute query
    var items = await _session.Query<User>()
        .Where(expression)
        .OrderBy(sorting.By)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    var total = await _session.Query<User>()
        .Where(expression)
        .CountAsync(cancellationToken);

    return new GetManyAndCountResult<User>(items, total, pageNumber, pageSize);
}
```

**Ejemplo de Query String**:
```
?pageNumber=1&pageSize=25&sortBy=Email&sortDirection=asc&IsActive=true||eq
```

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de NHibernate |
| [repositories.md](./repositories.md) | ⏳ Pendiente | NH*Repository implementations |
| [mappers.md](./mappers.md) | ⏳ Pendiente | ClassMapping patterns |
| [queries.md](./queries.md) | ⏳ Pendiente | LINQ, Dynamic LINQ, QueryOver |
| [unit-of-work.md](./unit-of-work.md) | ⏳ Pendiente | NHUnitOfWork implementation |
| [session-management.md](./session-management.md) | ⏳ Pendiente | ISession lifecycle |
| [best-practices.md](./best-practices.md) | ⏳ Pendiente | Mejores prácticas |

---

## Configuración Inicial

### 1. Instalar Paquetes NuGet

```bash
dotnet add package NHibernate
dotnet add package Npgsql
dotnet add package System.Linq.Dynamic.Core
dotnet add package FluentValidation
```

### 2. Configurar SessionFactory

```csharp
public class NHSessionFactory
{
    public string ConnectionString { get; }

    public NHSessionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();

        // Agregar todos los mappers del assembly
        mapper.AddMappings(typeof(UserMapper).Assembly.ExportedTypes);

        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
        });

        cfg.AddMapping(domainMapping);

        return cfg.BuildSessionFactory();
    }
}
```

### 3. Registrar en Dependency Injection

```csharp
public static IServiceCollection ConfigureUnitOfWork(
    this IServiceCollection services,
    IConfiguration configuration)
{
    string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();
    var factory = new NHSessionFactory(connectionString);
    var sessionFactory = factory.BuildNHibernateSessionFactory();

    // Singleton - Una sola instancia de SessionFactory
    services.AddSingleton(sessionFactory);

    // Scoped - Una sesión por request HTTP
    services.AddScoped(factory => sessionFactory.OpenSession());

    // Scoped - Unit of Work
    services.AddScoped<IUnitOfWork, NHUnitOfWork>();

    return services;
}
```

---

## Ejemplos Rápidos

### 📋 Ejemplo 1: Crear Mapper para Entidad

```csharp
public class TechnicalStandardMapper : ClassMapping<TechnicalStandard>
{
    public TechnicalStandardMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("technical_standards");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Title, map =>
        {
            map.Column("title");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Code, map =>
        {
            map.Column("code");
            map.NotNullable(true);
            map.Unique(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Description, map =>
        {
            map.Column("description");
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.CreatedAt, map =>
        {
            map.Column("created_at");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });
    }
}
```

### 📋 Ejemplo 2: Repository Específico

```csharp
public class NHTechnicalStandardRepository : NHRepository<TechnicalStandard, Guid>, ITechnicalStandardRepository
{
    public NHTechnicalStandardRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider)
    {
    }

    public async Task<TechnicalStandard> CreateAsync(string code, string title, string description)
    {
        var standard = new TechnicalStandard(code, title, description);

        // Validación
        if (!standard.IsValid())
            throw new InvalidDomainException(standard.Validate());

        // Verificar unicidad
        var exists = await GetByCodeAsync(code);
        if (exists != null)
            throw new DuplicatedDomainException($"Technical standard with code '{code}' already exists");

        await AddAsync(standard);
        FlushWhenNotActiveTransaction();
        return standard;
    }

    public async Task<TechnicalStandard?> GetByCodeAsync(string code)
    {
        return await _session.Query<TechnicalStandard>()
            .Where(ts => ts.Code == code)
            .SingleOrDefaultAsync();
    }
}
```

### 📋 Ejemplo 3: Uso en UseCase

```csharp
public class CreateTechnicalStandardUseCase
{
    public class Handler : ICommandHandler<Command, Result<TechnicalStandardDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public Handler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TechnicalStandardDto>> Handle(Command command)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                var standard = await _unitOfWork.TechnicalStandards.CreateAsync(
                    command.Code,
                    command.Title,
                    command.Description
                );

                _unitOfWork.Commit();
                return Result.Ok(new TechnicalStandardDto(standard));
            }
            catch (InvalidDomainException ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error(ex.Message));
            }
            catch (DuplicatedDomainException ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error(ex.Message));
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result.Fail(new Error("An error occurred while creating the technical standard"));
            }
        }
    }
}
```

---

## Mejores Prácticas

### ✅ 1. Session Per Request

```csharp
// ✅ CORRECTO - ISession Scoped
services.AddScoped(factory => sessionFactory.OpenSession());
```

**Por qué**:
- Una sesión por request HTTP
- Evita LazyInitializationException
- Dispose automático al final del request

---

### ✅ 2. Mapping by Code sobre XML

```csharp
// ✅ CORRECTO - ClassMapping<T>
public class UserMapper : ClassMapping<User>
{
    public UserMapper() { /* ... */ }
}

// ❌ EVITAR - XML mapping
// <hibernate-mapping xmlns="...">
//   <class name="User" table="users">
//     ...
//   </class>
// </hibernate-mapping>
```

**Por qué**:
- Type-safe
- Refactoring-friendly
- IntelliSense support

---

### ✅ 3. LINQ to NHibernate sobre HQL

```csharp
// ✅ CORRECTO - LINQ
var users = await _session.Query<User>()
    .Where(u => u.Email.Contains("@example.com"))
    .ToListAsync();

// ⚠️ EVITAR - HQL (a menos que sea necesario)
var users = _session.CreateQuery("from User u where u.Email like '%@example.com%'")
    .List<User>();
```

**Por qué**:
- Type-safe
- IntelliSense
- Compile-time errors

---

### ✅ 4. Async/Await para I/O

```csharp
// ✅ CORRECTO
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync();
}

// ❌ INCORRECTO
public User? GetByEmail(string email)
{
    return _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefault(); // Blocking I/O
}
```

---

### ✅ 5. Flush When Not Active Transaction

```csharp
public T Add(T item)
{
    var validationResult = validator.Validate(item);
    if (!validationResult.IsValid)
        throw new InvalidDomainException(validationResult.Errors);

    _session.Save(item);
    FlushWhenNotActiveTransaction(); // ✅ IMPORTANTE
    return item;
}

protected internal void FlushWhenNotActiveTransaction()
{
    var currentTransaction = _session.GetCurrentTransaction();
    if (currentTransaction == null || !currentTransaction.IsActive)
        _session.Flush();
}
```

**Por qué**:
- Sin transacción explícita: Flush inmediato
- Con transacción: Flush al hacer Commit
- Evita datos huérfanos en memoria

---

## Referencias

### 📚 Documentación Oficial

- [NHibernate Documentation](https://nhibernate.info/)
- [NHibernate Reference](https://nhibernate.info/doc/nhibernate-reference/index.html)
- [Mapping by Code](https://nhibernate.info/doc/nhibernate-reference/mapping-by-code.html)
- [LINQ to NHibernate](https://nhibernate.info/doc/nhibernate-reference/querylinq.html)

### 🔗 Guías Relacionadas

- [Core Concepts](../../README.md) - Conceptos fundamentales
- [Repository Pattern](../../repository-pattern.md) - Patrón Repository
- [Unit of Work Pattern](../../unit-of-work-pattern.md) - Patrón Unit of Work
- [Dependency Injection](../../dependency-injection.md) - Configuración de DI

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de NHibernate README     |

---

**Siguiente**: [Repositories](./repositories.md) - NH*Repository implementations →
