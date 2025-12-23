# Configuración de NHibernate

## Descripción

Configura **NHibernate** como ORM en la capa de infraestructura. Esta guía agrega:
- Repositorios base (`NHRepository`, `NHReadOnlyRepository`)
- Session Factory y Unit of Work
- Sistema de filtrado de queries
- Mappers base

**Requiere:** [04-infrastructure-layer.md](../../../architectures/clean-architecture/init/04-infrastructure-layer.md)

## Estructura Final

```
src/{ProjectName}.infrastructure/
├── {ProjectName}.infrastructure.csproj
├── nhibernate/
│   ├── ConnectionStringBuilder.cs
│   ├── NHSessionFactory.cs
│   ├── NHUnitOfWork.cs
│   ├── NHRepository.cs
│   ├── NHReadOnlyRepository.cs
│   ├── SortingCriteriaExtender.cs
│   ├── mappers/
│   │   └── {Entity}Mapper.cs
│   └── filtering/
│       ├── FilterExpressionParser.cs
│       ├── FilterOperator.cs
│       ├── InvalidQueryStringArgumentException.cs
│       ├── QueryStringParser.cs
│       ├── QuickSearch.cs
│       ├── RelationalOperator.cs
│       ├── Sorting.cs
│       └── StringExtender.cs
└── services/
```

## Paquetes NuGet

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package NHibernate
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package System.Linq.Dynamic.Core
```

## Pasos

### 1. Crear carpetas

```bash
mkdir src/{ProjectName}.infrastructure/nhibernate
mkdir src/{ProjectName}.infrastructure/nhibernate/mappers
mkdir src/{ProjectName}.infrastructure/nhibernate/filtering
```

### 2. Copiar templates base

Copiar desde `stacks/orm/nhibernate/templates/` a `src/{ProjectName}.infrastructure/nhibernate/`:

| Template | Destino | Descripción |
|----------|---------|-------------|
| `NHRepository.cs` | `nhibernate/` | Repositorio base con CRUD y validación |
| `NHReadOnlyRepository.cs` | `nhibernate/` | Repositorio solo lectura con paginación |
| `NHSessionFactory.cs` | `nhibernate/` | Factory para crear sesiones |
| `NHUnitOfWork.cs` | `nhibernate/` | Patrón Unit of Work |
| `SortingCriteriaExtender.cs` | `nhibernate/` | Extensiones para ordenamiento |
| `ConnectionStringBuilder.cs` | `nhibernate/` | Constructor de connection string |

### 3. Copiar templates de filtering

Copiar desde `stacks/orm/nhibernate/templates/filtering/` a `src/{ProjectName}.infrastructure/nhibernate/filtering/`:

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

### 4. Reemplazar namespaces

En todos los archivos copiados, reemplazar:
- `{ProjectName}` → nombre real del proyecto

### 5. Crear mapper de ejemplo

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
    public static IServiceCollection ConfigureUnitOfWork(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Session Factory
        services.AddSingleton<NHSessionFactory>();
        services.AddScoped(sp =>
            sp.GetRequiredService<NHSessionFactory>().OpenSession());

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

## Siguiente Paso

→ [05-webapi-layer.md](../../../architectures/clean-architecture/init/05-webapi-layer.md)
