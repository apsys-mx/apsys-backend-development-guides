# 04 - Capa de Infraestructura (Infrastructure Layer)

## Descripción

Crea la **capa de infraestructura** del proyecto. Esta capa contendrá:
- Implementaciones de repositorios
- Configuración de persistencia (ORM)
- Servicios externos
- Configuración de DI

Esta guía crea una **estructura agnóstica**. La implementación específica (NHibernate, EF, etc.) se configura en `stacks/orm/`.

**Requiere:** [03-application-layer.md](./03-application-layer.md)

## Estructura Final

```
src/{ProjectName}.infrastructure/
├── {ProjectName}.infrastructure.csproj
├── repositories/          # Implementaciones de IRepository
├── persistence/           # Configuración de ORM/DB
├── services/              # Servicios externos (HTTP, email, etc.)
└── configuration/         # Setup de DI

tests/{ProjectName}.infrastructure.tests/
└── {ProjectName}.infrastructure.tests.csproj
```

## Pasos

### 1. Crear proyecto infrastructure

```bash
dotnet new classlib -n {ProjectName}.infrastructure -o src/{ProjectName}.infrastructure
dotnet sln add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
rm src/{ProjectName}.infrastructure/Class1.cs
```

### 2. Agregar referencia a Domain

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### 3. Crear carpetas

```bash
mkdir src/{ProjectName}.infrastructure/repositories
mkdir src/{ProjectName}.infrastructure/persistence
mkdir src/{ProjectName}.infrastructure/services
mkdir src/{ProjectName}.infrastructure/configuration
```

### 4. Copiar templates

Copiar READMEs desde `templates/infrastructure/` a cada carpeta.

### 5. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.infrastructure.tests -o tests/{ProjectName}.infrastructure.tests
dotnet sln add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj
rm tests/{ProjectName}.infrastructure.tests/UnitTest1.cs
```

### 6. Remover versiones en .csproj de tests

Editar el .csproj y eliminar atributos `Version`.

### 7. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package FluentAssertions
```

### 8. Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

## Principios

### Implementa Interfaces de Domain

```csharp
// Domain define la interfaz
public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure la implementa
public class UserRepository : IUserRepository
{
    // Implementación con NHibernate, EF, Dapper, etc.
}
```

### Independencia de Framework

```csharp
// ❌ INCORRECTO en Application
var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

// ✅ CORRECTO en Application
var user = await _userRepository.GetByEmailAsync(email);
```

### Configuración de DI

```csharp
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
```

## Verificación

```bash
dotnet build
```

## Implementaciones de ORM

Para agregar un ORM específico, ver:
- `stacks/orm/nhibernate/` - NHibernate
- `stacks/orm/entity-framework/` - Entity Framework

## Siguiente Paso

→ [05-webapi-layer.md](./05-webapi-layer.md)
