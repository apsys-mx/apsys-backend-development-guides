# Configuración de NHibernate

## Descripción

Configura **NHibernate** como ORM en la capa de infraestructura. Esta guía agrega:
- Repositorios base (`NHRepository`, `NHReadOnlyRepository`)
- Session Factory y Unit of Work
- Sistema de filtrado de queries
- Mappers base

**Requiere:**
- [04-infrastructure-layer.md](../../../architectures/clean-architecture/init/04-infrastructure-layer.md)
- **Base de datos configurada** (elegir una):
  - [PostgreSQL](../../database/postgresql/guides/setup.md)
  - [SQL Server](../../database/sqlserver/guides/setup.md)

## Estructura Final

```
src/{ProjectName}.infrastructure/
├── {ProjectName}.infrastructure.csproj
├── nhibernate/
│   ├── ConnectionStringBuilder.cs    ← Viene del setup de BD
│   ├── NHSessionFactory.cs
│   ├── NHUnitOfWork.cs
│   ├── NHRepository.cs
│   ├── NHReadOnlyRepository.cs
│   ├── SortingCriteriaExtender.cs
│   ├── mappers/
│   │   └── {Entity}Mapper.cs
│   └── filtering/
│       └── *.cs
└── services/
```

## Paquetes NuGet

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package NHibernate
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package System.Linq.Dynamic.Core
```

## Pasos

### 1. Configurar Base de Datos (si no lo has hecho)

Antes de continuar, debes configurar tu base de datos:

| Base de Datos | Guía |
|---------------|------|
| PostgreSQL | [stacks/database/postgresql/guides/setup.md](../../database/postgresql/guides/setup.md) |
| SQL Server | [stacks/database/sqlserver/guides/setup.md](../../database/sqlserver/guides/setup.md) |

Esto instalará el driver y creará el `ConnectionStringBuilder.cs`.

### 2. Crear carpetas

```bash
mkdir src/{ProjectName}.infrastructure/nhibernate
mkdir src/{ProjectName}.infrastructure/nhibernate/mappers
mkdir src/{ProjectName}.infrastructure/nhibernate/filtering
```

### 3. Copiar templates base

Copiar desde `docs/guides/stacks/orm/nhibernate/templates/` a `src/{ProjectName}.infrastructure/nhibernate/`:

| Template | Destino | Descripción |
|----------|---------|-------------|
| `NHRepository.cs` | `nhibernate/` | Repositorio base con CRUD y validación |
| `NHReadOnlyRepository.cs` | `nhibernate/` | Repositorio solo lectura con paginación |
| `NHSessionFactory.cs` | `nhibernate/` | Factory para crear sesiones |
| `NHUnitOfWork.cs` | `nhibernate/` | Patrón Unit of Work |
| `SortingCriteriaExtender.cs` | `nhibernate/` | Extensiones para ordenamiento |

> **Nota:** `ConnectionStringBuilder.cs` viene del setup de la base de datos elegida.

### 4. Configurar Driver y Dialect

Editar `NHSessionFactory.cs` según la base de datos elegida:

**Para PostgreSQL:**
```csharp
using NHibernate.Driver;
using NHibernate.Dialect;

// En BuildNHibernateSessionFactory():
cfg.DataBaseIntegration(c =>
{
    c.Driver<NpgsqlDriver>();
    c.Dialect<PostgreSQL83Dialect>();
    c.ConnectionString = this.ConnectionString;
    c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
});
```

**Para SQL Server:**
```csharp
using NHibernate.Driver;
using NHibernate.Dialect;

// En BuildNHibernateSessionFactory():
cfg.DataBaseIntegration(c =>
{
    c.Driver<MicrosoftDataSqlClientDriver>();
    c.Dialect<MsSql2012Dialect>();
    c.ConnectionString = this.ConnectionString;
    c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
});
```

### 5. Copiar templates de filtering

Copiar desde `docs/guides/stacks/orm/nhibernate/templates/filtering/` a `src/{ProjectName}.infrastructure/nhibernate/filtering/`:

| Template | Descripción |
|----------|-------------|
| `FilterExpressionParser.cs` | Parser de expresiones de filtro |
| `FilterOperator.cs` | Operadores de filtro |
| `InvalidQueryStringArgumentException.cs` | Excepción para queries inválidos |
| `QueryStringParser.cs` | Parser de query strings |
| `QuickSearch.cs` | Búsqueda rápida |
| `RelationalOperator.cs` | Operadores relacionales |
| `Sorting.cs` | Modelo de ordenamiento |
| `StringExtender.cs` | Extensiones de string |

### 6. Reemplazar namespaces

En todos los archivos copiados, reemplazar:
- `{ProjectName}` → nombre real del proyecto

### 7. Crear mapper de ejemplo

Crear `src/{ProjectName}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs`:

```csharp
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using {ProjectName}.domain.entities;

namespace {ProjectName}.infrastructure.nhibernate.mappers;

public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Table("users");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Email, m => m.NotNullable(true));
        Property(x => x.Name, m => m.Length(100));
        Property(x => x.CreationDate);
    }
}
```

## Crear Repositorio Específico

Para cada entidad, crear un repositorio que herede de `NHRepository`:

```csharp
using NHibernate;
using NHibernate.Linq;
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.infrastructure.nhibernate;

public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == email);
}
```

## Configuración de DI

Agregar en `src/{ProjectName}.webapi/infrastructure/`:

```csharp
public static class NHibernateServiceCollectionExtensions
{
    public static IServiceCollection ConfigureNHibernate(
        this IServiceCollection services)
    {
        // Connection string from environment
        var connectionString = ConnectionStringBuilder.Build();

        // Session Factory
        services.AddSingleton(new NHSessionFactory(connectionString));
        services.AddScoped(sp =>
            sp.GetRequiredService<NHSessionFactory>()
              .BuildNHibernateSessionFactory()
              .OpenSession());

        // Unit of Work
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();

        // Repositories
        services.AddScoped<IUserRepository, NHUserRepository>();

        return services;
    }
}
```

## Verificación

```bash
dotnet build
```

## Resumen de Dependencias

```
┌─────────────────────────────────────────────────────────────┐
│                    Tu Proyecto                              │
├─────────────────────────────────────────────────────────────┤
│  stacks/orm/nhibernate/          stacks/database/{db}/      │
│  ├── NHSessionFactory.cs    +    ├── ConnectionStringBuilder│
│  ├── NHRepository.cs             └── Driver NuGet           │
│  └── ...                                                    │
└─────────────────────────────────────────────────────────────┘
```

## Siguiente Paso

→ [05-webapi-layer.md](../../../architectures/clean-architecture/init/05-webapi-layer.md)
