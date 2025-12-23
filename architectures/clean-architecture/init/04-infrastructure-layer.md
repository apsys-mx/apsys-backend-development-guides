# 04 - Capa de Infraestructura (Infrastructure Layer)

## Descripción

Crea la **capa de infraestructura** del proyecto. Esta capa contendrá:
- Implementaciones de repositorios
- Configuración de persistencia (ORM)
- Servicios externos (HTTP, email, autenticación, etc.)

Esta guía crea una **estructura base**. La implementación específica del ORM (NHibernate, EF, etc.) se configura en `stacks/orm/`.

**Requiere:** [03-application-layer.md](./03-application-layer.md)

## Estructura Final

```
src/{ProjectName}.infrastructure/
├── {ProjectName}.infrastructure.csproj
├── nhibernate/                    # Organizado por ORM
│   ├── ConnectionStringBuilder.cs
│   ├── NHSessionFactory.cs
│   ├── NHUnitOfWork.cs
│   ├── NHRepository.cs
│   ├── NHReadOnlyRepository.cs
│   ├── SortingCriteriaExtender.cs
│   ├── mappers/                   # ClassMaps de NHibernate
│   │   └── {Entity}Mapper.cs
│   └── filtering/                 # Lógica de filtrado de queries
│       ├── FilterExpressionParser.cs
│       ├── QueryStringParser.cs
│       └── ...
└── services/                      # Servicios externos (opcional)
    └── {ServiceName}Service.cs

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

### 3. Crear carpetas base

```bash
mkdir src/{ProjectName}.infrastructure/nhibernate
mkdir src/{ProjectName}.infrastructure/nhibernate/mappers
mkdir src/{ProjectName}.infrastructure/nhibernate/filtering
mkdir src/{ProjectName}.infrastructure/services
```

### 4. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.infrastructure.tests -o tests/{ProjectName}.infrastructure.tests
dotnet sln add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj
rm tests/{ProjectName}.infrastructure.tests/UnitTest1.cs
```

### 5. Remover versiones en .csproj de tests

Editar el .csproj y eliminar atributos `Version`.

### 6. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package FluentAssertions
```

### 7. Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

## Principios

### Organización por Tecnología

Los archivos se organizan por tecnología/ORM, no por tipo:

```
# ✅ CORRECTO - Por tecnología
nhibernate/
├── NHUserRepository.cs
├── NHRoleRepository.cs
└── mappers/
    ├── UserMapper.cs
    └── RoleMapper.cs

# ❌ INCORRECTO - Por tipo genérico
repositories/
├── UserRepository.cs
└── RoleRepository.cs
```

### Implementa Interfaces de Domain

```csharp
// Domain define la interfaz
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure la implementa con NHibernate
public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _session.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == email);
}
```

### Mappers de NHibernate

```csharp
// nhibernate/mappers/UserMapper.cs
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

### Independencia de Framework en Application

```csharp
// ❌ INCORRECTO en Application - Acoplamiento a ORM
var user = await _session.Query<User>().FirstOrDefaultAsync(x => x.Email == email);

// ✅ CORRECTO en Application - Usa abstracción
var user = await _userRepository.GetByEmailAsync(email);
```

## Verificación

```bash
dotnet build
```

## Implementaciones de ORM

Para configurar el ORM específico, ver:
- `stacks/orm/nhibernate/` - NHibernate (recomendado)
- `stacks/orm/entity-framework/` - Entity Framework

## Siguiente Paso

→ [05-webapi-layer.md](./05-webapi-layer.md)
