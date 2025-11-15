# ORM Implementations - Comparación y Guía de Selección

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-11-15

## 📋 Tabla de Contenidos
1. [¿Qué es un ORM?](#qué-es-un-orm)
2. [ORMs Disponibles en .NET](#orms-disponibles-en-net)
3. [Comparación Detallada](#comparación-detallada)
4. [Decisión de APSYS: NHibernate](#decisión-de-apsys-nhibernate)
5. [Matriz de Decisión](#matriz-de-decisión)
6. [Migración entre ORMs](#migración-entre-orms)
7. [Guías por ORM](#guías-por-orm)
8. [Referencias](#referencias)

---

## ¿Qué es un ORM?

Un **Object-Relational Mapper (ORM)** es una herramienta que permite a los desarrolladores trabajar con bases de datos relacionales utilizando objetos de programación orientada a objetos, en lugar de escribir consultas SQL directamente.

### 🎯 Ventajas de Usar un ORM

#### ✅ Productividad
```csharp
// ❌ SIN ORM - SQL manual
var command = connection.CreateCommand();
command.CommandText = @"
    SELECT u.id, u.email, u.name, r.id as role_id, r.name as role_name
    FROM users u
    LEFT JOIN user_in_roles ur ON u.id = ur.user_id
    LEFT JOIN roles r ON ur.role_id = r.id
    WHERE u.email = @email";
command.Parameters.AddWithValue("@email", email);
var reader = command.ExecuteReader();
// ... mapeo manual a objetos

// ✅ CON ORM - LINQ type-safe
var user = await _session.Query<User>()
    .Where(u => u.Email == email)
    .Include(u => u.Roles)
    .SingleOrDefaultAsync();
```

#### ✅ Mantenibilidad
- **Type-safety**: Errores en compile-time en lugar de runtime
- **Refactoring**: Renombrar propiedades actualiza automáticamente queries
- **IntelliSense**: Autocompletado de propiedades y métodos

#### ✅ Abstracción de Base de Datos
```csharp
// El mismo código funciona en PostgreSQL, SQL Server, MySQL, etc.
var users = await _session.Query<User>()
    .Where(u => u.IsActive)
    .ToListAsync();
```

#### ✅ Manejo de Relaciones
```csharp
// Lazy Loading automático
var user = await _session.GetAsync<User>(userId);
var roles = user.Roles; // Carga diferida automática

// Eager Loading explícito
var user = await _session.Query<User>()
    .Where(u => u.Id == userId)
    .Fetch(u => u.Roles)
    .SingleOrDefaultAsync();
```

### ⚠️ Desventajas de Usar un ORM

#### 1. Curva de Aprendizaje
- Requiere aprender conceptos del ORM (Session, Unit of Work, Lazy Loading, etc.)
- Configuración inicial más compleja que SQL directo

#### 2. Performance Overhead (Mínimo)
- Pequeña penalización por abstracción
- Mitigado con caching y lazy loading

#### 3. Queries Complejas
- Algunas queries muy específicas son más claras en SQL nativo
- Solución: Todos los ORMs permiten SQL nativo cuando sea necesario

---

## ORMs Disponibles en .NET

### 1. [NHibernate](./nhibernate/README.md) ✅ **Actual en APSYS**

**Descripción**: Port oficial de Hibernate (Java) para .NET, ORM maduro y completo.

**Estado**: ✅ En uso activo en todos los proyectos APSYS

**Versión**: 5.5.2 (proyecto de referencia: hashira.stone.backend)

**Características**:
- ✅ Madurez de 15+ años
- ✅ Mapping by Code (Fluent API sin XML)
- ✅ LINQ to NHibernate
- ✅ First-level y Second-level cache
- ✅ Lazy Loading granular
- ✅ Database Agnostic (PostgreSQL, SQL Server, MySQL, Oracle, SQLite)
- ✅ Filters dinámicos
- ✅ Batching avanzado

### 2. [Entity Framework Core](./entity-framework/README.md) ⏳ **Futuro**

**Descripción**: ORM oficial de Microsoft para .NET, moderno y activamente desarrollado.

**Estado**: ⏳ Evaluación para futuros proyectos

**Versión**: 9.x (actual en .NET 9)

**Características**:
- ✅ Oficial de Microsoft
- ✅ Code First, Database First, Migrations automáticas
- ✅ LINQ to Entities
- ✅ Memory cache (IMemoryCache)
- ✅ Change Tracking automático
- ✅ Database Agnostic (PostgreSQL, SQL Server, MySQL, SQLite, Cosmos DB)
- ✅ Integración nativa con ASP.NET Core
- ⚠️ Lazy Loading limitado (requiere proxies)

---

## Comparación Detallada

### 📊 Tabla Comparativa

| Característica | NHibernate 5.5 | Entity Framework Core 9.x | Ganador |
|----------------|----------------|---------------------------|---------|
| **Madurez y Estabilidad** | ✅ 15+ años, muy estable | ⚠️ ~7 años, en evolución | NHibernate |
| **Performance** | ✅ Excelente, batching avanzado | ✅ Excelente, optimizado | Empate |
| **Curva de Aprendizaje** | ⚠️ Más compleja | ✅ Más sencilla | EF Core |
| **Documentación** | ⚠️ Buena pero dispersa | ✅ Excelente, oficial MS | EF Core |
| **Comunidad** | ✅ Madura | ✅ Grande y activa | Empate |
| **Soporte Microsoft** | ❌ No oficial | ✅ Oficial | EF Core |
| **Mapping Configuration** | ✅ Mapping by Code | ✅ Fluent API | Empate |
| **LINQ Support** | ✅ LINQ to NHibernate | ✅ LINQ to Entities | Empate |
| **Lazy Loading** | ✅ Granular y configurable | ⚠️ Limitado, requiere proxies | NHibernate |
| **Caching** | ✅ 1st + 2nd level cache | ⚠️ Solo memory cache | NHibernate |
| **Database Agnostic** | ✅ Excelente | ✅ Excelente | Empate |
| **Migrations** | ⚠️ Requiere FluentMigrator | ✅ Built-in (EF Migrations) | EF Core |
| **Dynamic Filters** | ✅ Nativo | ⚠️ Requiere extensiones | NHibernate |
| **Batching** | ✅ Avanzado | ⚠️ Básico | NHibernate |
| **Change Tracking** | ⚠️ Manual (sesión) | ✅ Automático | EF Core |
| **Tooling** | ⚠️ Limitado | ✅ Visual Studio, CLI | EF Core |
| **Multi-tenancy** | ✅ Filters nativos | ⚠️ Requiere configuración | NHibernate |
| **Legacy DB Support** | ✅ Excelente | ⚠️ Bueno | NHibernate |

### 🔍 Análisis por Categoría

#### 1. Performance y Optimización

**NHibernate**:
```csharp
// ✅ Batching avanzado
cfg.DataBaseIntegration(c =>
{
    c.BatchSize = 25; // Batch INSERT/UPDATE automático
});

// ✅ Second-level cache (Redis, Memcached)
cfg.Cache(c =>
{
    c.UseSecondLevelCache = true;
    c.Provider<NHibernate.Caches.Redis.RedisCache>();
});

// ✅ Lazy loading granular por propiedad
Property(x => x.LargeText, map => map.Lazy(true));
```

**Entity Framework Core**:
```csharp
// ✅ Change tracking optimizado
context.ChangeTracker.AutoDetectChangesEnabled = false;

// ✅ No-tracking queries
var users = await context.Users
    .AsNoTracking()
    .ToListAsync();

// ⚠️ Lazy loading requiere proxies
services.AddDbContext<BloggingContext>(
    b => b.UseLazyLoadingProxies());
```

**Ganador**: **NHibernate** por batching avanzado y second-level cache.

---

#### 2. Mapping y Configuración

**NHibernate**:
```csharp
// ✅ Mapping by Code
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
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Unique(true);
        });

        Bag(x => x.Roles, map =>
        {
            map.Table("user_in_roles");
            map.Key(k => k.Column("user_id"));
        },
        map => map.ManyToMany(m => m.Column("role_id")));
    }
}
```

**Entity Framework Core**:
```csharp
// ✅ Fluent API
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.ToTable("users", "app");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        entity.HasIndex(e => e.Email)
            .IsUnique();

        entity.HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity<Dictionary<string, object>>(
                "user_in_roles",
                j => j.HasOne<Role>().WithMany().HasForeignKey("role_id"),
                j => j.HasOne<User>().WithMany().HasForeignKey("user_id"));
    });
}
```

**Ganador**: **Empate** - Ambos ofrecen APIs fluent completas y type-safe.

---

#### 3. LINQ Support

**NHibernate**:
```csharp
// ✅ LINQ to NHibernate
var users = await _session.Query<User>()
    .Where(u => u.Email.Contains("@apsys.com"))
    .Where(u => u.Roles.Any(r => r.Name == "Admin"))
    .OrderBy(u => u.Name)
    .Skip(20)
    .Take(10)
    .ToListAsync();

// ✅ QueryOver (alternativa type-safe)
var users = await _session.QueryOver<User>()
    .Where(u => u.Email.IsLike("%@apsys.com"))
    .List<User>();
```

**Entity Framework Core**:
```csharp
// ✅ LINQ to Entities
var users = await _context.Users
    .Where(u => u.Email.Contains("@apsys.com"))
    .Where(u => u.Roles.Any(r => r.Name == "Admin"))
    .OrderBy(u => u.Name)
    .Skip(20)
    .Take(10)
    .ToListAsync();

// ✅ GroupBy mejorado (EF Core 6+)
var grouping = _context.People
    .GroupBy(p => p.LastName)
    .Select(g => new { LastName = g.Key, Count = g.Count() })
    .ToList();
```

**Ganador**: **Empate** - Ambos tienen soporte LINQ excelente.

---

#### 4. Migrations

**NHibernate** (con FluentMigrator):
```csharp
// ⚠️ Requiere FluentMigrator como dependencia externa
[Migration(20250115001)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .InSchema("app")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("users").InSchema("app");
    }
}
```

**Entity Framework Core**:
```csharp
// ✅ Migrations built-in
// 1. Crear migración
dotnet ef migrations add CreateUsers

// 2. Aplicar migración
dotnet ef database update

// 3. Migración auto-generada
public partial class CreateUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("app");

        migrationBuilder.CreateTable(
            name: "users",
            schema: "app",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 255, nullable: false),
                Name = table.Column<string>(maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.Id);
            });
    }
}
```

**Ganador**: **Entity Framework Core** - Migrations integradas, auto-generadas, con CLI.

---

#### 5. Caching

**NHibernate**:
```csharp
// ✅ First-level cache (sesión)
var user1 = await _session.GetAsync<User>(userId); // DB hit
var user2 = await _session.GetAsync<User>(userId); // Cache hit

// ✅ Second-level cache (distribuido: Redis, Memcached)
cfg.Cache(c =>
{
    c.UseSecondLevelCache = true;
    c.UseQueryCache = true;
    c.Provider<RedisCacheProvider>();
});

// Uso en queries
var users = await _session.Query<User>()
    .Where(u => u.IsActive)
    .Cacheable()
    .ToListAsync();
```

**Entity Framework Core**:
```csharp
// ✅ Change tracking (similar a first-level cache)
var user1 = await _context.Users.FindAsync(userId); // DB hit
var user2 = await _context.Users.FindAsync(userId); // Cache hit (mismo contexto)

// ⚠️ No hay second-level cache nativo
// Requiere IMemoryCache o extensiones de terceros
private readonly IMemoryCache _cache;

public async Task<User?> GetUserAsync(Guid userId)
{
    return await _cache.GetOrCreateAsync($"user_{userId}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await _context.Users.FindAsync(userId);
    });
}
```

**Ganador**: **NHibernate** - Second-level cache nativo.

---

#### 6. Lazy Loading

**NHibernate**:
```csharp
// ✅ Lazy loading granular por propiedad/colección
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        // Lazy loading en colección
        Bag(x => x.Roles, map => map.Lazy(CollectionLazy.True));

        // Lazy loading en propiedad individual
        Property(x => x.LargeDescription, map => map.Lazy(true));
    }
}

// Uso
var user = await _session.GetAsync<User>(userId);
// Roles NO cargados aún
var roles = user.Roles; // DB hit aquí
```

**Entity Framework Core**:
```csharp
// ⚠️ Lazy loading requiere proxies y configuración
services.AddDbContext<BloggingContext>(
    b => b.UseLazyLoadingProxies());

// Entities deben tener propiedades virtual
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public virtual ICollection<Role> Roles { get; set; } // virtual!
}

// Uso
var user = await _context.Users.FindAsync(userId);
var roles = user.Roles; // DB hit si lazy loading habilitado
```

**Ganador**: **NHibernate** - Lazy loading más flexible y granular.

---

#### 7. Dynamic Filters (Multi-tenancy)

**NHibernate**:
```csharp
// ✅ Filters nativos
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        // Definir filter
        Filter("TenantFilter", filter =>
        {
            filter.Condition("tenant_id = :tenantId");
            filter.AddParameter("tenantId", NHibernateUtil.Guid);
        });
    }
}

// Activar filter en runtime
_session.EnableFilter("TenantFilter")
    .SetParameter("tenantId", currentTenantId);

// Todas las queries usan el filtro automáticamente
var users = await _session.Query<User>().ToListAsync();
// SELECT ... FROM users WHERE tenant_id = @tenantId
```

**Entity Framework Core**:
```csharp
// ⚠️ Requiere Global Query Filters
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .HasQueryFilter(u => u.TenantId == _currentTenantId);
}

// Problema: _currentTenantId debe ser accesible en DbContext
// Solución: Usar extension o interceptors
```

**Ganador**: **NHibernate** - Filters dinámicos más flexibles.

---

#### 8. Tooling y Developer Experience

**NHibernate**:
- ⚠️ Tooling limitado
- ⚠️ No hay CLI oficial
- ⚠️ Debugging más manual
- ✅ Profiler de terceros (NHibernate Profiler - pago)

**Entity Framework Core**:
- ✅ CLI completo (`dotnet ef`)
- ✅ Visual Studio integration
- ✅ Package Manager Console
- ✅ Debugging integrado en VS
- ✅ EF Core Power Tools (gratis)
- ✅ Scaffold desde DB existente

```bash
# EF Core CLI
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet ef dbcontext scaffold "ConnectionString" Npgsql.EntityFrameworkCore.PostgreSQL
```

**Ganador**: **Entity Framework Core** - Mejor tooling y DX.

---

## Decisión de APSYS: NHibernate

### ✅ Por qué APSYS usa NHibernate

#### 1. **Madurez y Estabilidad**
- **15+ años** en producción en miles de proyectos
- Port oficial de **Hibernate** (estándar de facto en Java)
- API estable, cambios mínimos entre versiones

#### 2. **Performance en Escenarios Complejos**
```csharp
// Batching automático (25 inserts en 1 round-trip)
cfg.DataBaseIntegration(c => c.BatchSize = 25);

// Second-level cache distribuido (Redis)
cfg.Cache(c =>
{
    c.UseSecondLevelCache = true;
    c.Provider<RedisCacheProvider>();
});
```

#### 3. **Lazy Loading Granular**
```csharp
// Control fino sobre qué cargar y cuándo
var user = await _session.Query<User>()
    .Where(u => u.Id == userId)
    .Fetch(u => u.Roles)           // Eager load roles
    .ThenFetch(r => r.Permissions) // Eager load permissions
    .SingleOrDefaultAsync();
```

#### 4. **Dynamic Filters para Multi-tenancy**
```csharp
// Multi-tenancy sin modificar queries
_session.EnableFilter("TenantFilter")
    .SetParameter("tenantId", currentTenantId);

// Todas las queries filtran automáticamente por tenant
var users = await _session.Query<User>().ToListAsync();
```

#### 5. **Database Agnostic Real**
- Mismo código para **PostgreSQL, SQL Server, MySQL, Oracle, SQLite**
- Dialects específicos optimizados por BD
- Sin vendor lock-in

#### 6. **Legacy Database Support**
- Excelente para bases de datos existentes
- Mapping flexible a esquemas complejos
- Soporte para stored procedures, views, etc.

### ⚠️ Desventajas Reconocidas

#### 1. **Curva de Aprendizaje Más Pronunciada**
- Conceptos: Session, SessionFactory, Unit of Work, Flush
- Requiere entender lifecycle de sesión

**Mitigación**: Estas guías de desarrollo documentan todos los patrones.

#### 2. **Tooling Limitado**
- No hay CLI oficial
- Debugging más manual

**Mitigación**: FluentMigrator para migrations, NHibernate Profiler para optimización.

#### 3. **Documentación Dispersa**
- Documentación oficial buena pero no tan completa como EF Core

**Mitigación**: Estas guías centralizan todo el conocimiento de APSYS.

---

## Matriz de Decisión

### 🎯 Usa **NHibernate** si:

| Criterio | Razón |
|----------|-------|
| ✅ Aplicaciones empresariales complejas | Batching, caching, multi-tenancy nativos |
| ✅ Legacy databases | Mapping flexible a esquemas complejos |
| ✅ Multi-tenancy | Dynamic filters nativos |
| ✅ Performance crítica | Second-level cache distribuido |
| ✅ Database agnostic real | Mismo código, múltiples BDs |
| ✅ Lazy loading granular | Control fino sobre carga de datos |
| ✅ Migración desde Hibernate (Java) | API similar, curva de aprendizaje reducida |

### 🎯 Usa **Entity Framework Core** si:

| Criterio | Razón |
|----------|-------|
| ✅ Nuevo proyecto greenfield | Migrations integradas, scaffolding |
| ✅ Equipo nuevo en ORMs | Curva de aprendizaje más suave |
| ✅ Ecosistema Microsoft | Integración nativa con ASP.NET Core |
| ✅ Tooling importante | CLI, Visual Studio, Power Tools |
| ✅ Rapid prototyping | Migrations auto-generadas |
| ✅ Code First workflow | Genera BD desde código fácilmente |
| ✅ Azure/Cosmos DB | Soporte nativo para Cosmos DB |

### ⚖️ Consideraciones Neutrales

| Característica | NHibernate | EF Core | Conclusión |
|----------------|------------|---------|------------|
| LINQ Support | ✅ Excelente | ✅ Excelente | Usar cualquiera |
| Performance básica | ✅ Excelente | ✅ Excelente | Usar cualquiera |
| PostgreSQL support | ✅ Npgsql | ✅ Npgsql | Usar cualquiera |
| Clean Architecture | ✅ Compatible | ✅ Compatible | Usar cualquiera |
| Testing | ✅ Mockeable | ✅ Mockeable | Usar cualquiera |

---

## Migración entre ORMs

### Escenario 1: NHibernate → Entity Framework Core

**Cuándo considerar**:
- Equipo nuevo sin experiencia en NHibernate
- Tooling de EF Core es crítico
- No se requiere second-level cache
- No se requiere multi-tenancy con filtros dinámicos

**Pasos**:
1. Mantener interfaces de repositorio (IRepository, IUnitOfWork)
2. Implementar nuevos repositorios con EF Core
3. Configurar DbContext equivalente a SessionFactory
4. Migrar mappers (ClassMapping → Fluent API)
5. Reemplazar FluentMigrator por EF Migrations
6. Testing exhaustivo (especialmente lazy loading)

**Esfuerzo estimado**: Alto (2-4 semanas para proyecto mediano)

### Escenario 2: Entity Framework Core → NHibernate

**Cuándo considerar**:
- Requerimientos de multi-tenancy complejo
- Performance crítica requiere second-level cache
- Legacy database compleja

**Pasos**:
1. Mantener interfaces de repositorio
2. Configurar NHSessionFactory
3. Crear mappers (Fluent API → ClassMapping)
4. Implementar NHRepository, NHUnitOfWork
5. Configurar FluentMigrator
6. Testing exhaustivo

**Esfuerzo estimado**: Alto (2-4 semanas para proyecto mediano)

### ✅ Recomendación APSYS

**No migrar proyectos existentes** a menos que:
- Haya un problema de performance no resoluble
- Haya un requerimiento de negocio que solo un ORM soporte
- El costo de migración esté justificado

**Para nuevos proyectos**:
- Evaluar criterios de matriz de decisión
- Consultar con equipo de arquitectura
- Documentar decisión en ADR (Architecture Decision Record)

---

## Guías por ORM

### [NHibernate](./nhibernate/README.md) ✅ Completado

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./nhibernate/README.md) | ✅ v1.0.0 | Overview de NHibernate |
| [repositories.md](./nhibernate/repositories.md) | ✅ v1.0.0 | NH*Repository implementations |
| [mappers.md](./nhibernate/mappers.md) | ✅ v1.0.0 | ClassMapping patterns |
| [queries.md](./nhibernate/queries.md) | ✅ v1.0.0 | LINQ, Dynamic LINQ, QueryOver |
| [unit-of-work.md](./nhibernate/unit-of-work.md) | ✅ v1.0.0 | NHUnitOfWork implementation |
| [session-management.md](./nhibernate/session-management.md) | ✅ v1.0.0 | ISession lifecycle |
| [best-practices.md](./nhibernate/best-practices.md) | ✅ v1.0.0 | Mejores prácticas |

### [Entity Framework Core](./entity-framework/README.md) ⏳ Futuro

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./entity-framework/README.md) | ⏳ Futuro | Overview de EF Core |
| dbcontext.md | ⏳ Futuro | DbContext configuration |
| repositories.md | ⏳ Futuro | Repository pattern con EF Core |
| migrations.md | ⏳ Futuro | EF Migrations workflow |
| queries.md | ⏳ Futuro | LINQ to Entities |
| best-practices.md | ⏳ Futuro | EF Core best practices |

---

## Referencias

### 📚 NHibernate

- [NHibernate Documentation](https://nhibernate.info/)
- [NHibernate Reference](https://nhibernate.info/doc/nhibernate-reference/index.html)
- [Mapping by Code](https://nhibernate.info/doc/nhibernate-reference/mapping-by-code.html)
- [LINQ to NHibernate](https://nhibernate.info/doc/nhibernate-reference/querylinq.html)

### 📚 Entity Framework Core

- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
- [EF Core Get Started](https://learn.microsoft.com/ef/core/get-started/overview/first-app)
- [EF Core Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [EF Core Performance](https://learn.microsoft.com/ef/core/performance/)

### 📚 Comparaciones y Artículos

- [ORM Comparison: NHibernate vs EF Core](https://stackshare.io/stackups/entity-framework-vs-nhibernate)
- [Choosing an ORM for .NET](https://blog.logrocket.com/choosing-orm-dotnet/)
- [NHibernate vs Entity Framework](https://www.c-sharpcorner.com/article/nhibernate-vs-entity-framework/)

### 🔗 Guías Relacionadas

- [Core Concepts](../../README.md) - Conceptos fundamentales de Infrastructure Layer
- [Repository Pattern](../../repository-pattern.md) - Patrón Repository (agnóstico)
- [Unit of Work Pattern](../../unit-of-work-pattern.md) - Patrón Unit of Work (agnóstico)
- [NHibernate Implementation](./nhibernate/README.md) - Implementación NHibernate en APSYS

---

## 🔄 Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2025-11-15 | Versión inicial: comparación completa NHibernate vs EF Core |

---

**Mantenedor**: Equipo APSYS
**Proyecto de referencia**: hashira.stone.backend (NHibernate 5.5.2)
