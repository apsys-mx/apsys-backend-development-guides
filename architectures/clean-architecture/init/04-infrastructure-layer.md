# 04 - Capa de Infraestructura (Infrastructure Layer)

## Descripción

Crea la **capa de infraestructura** del proyecto. Esta capa contendrá:
- Implementaciones de repositorios
- Configuración de persistencia (ORM)
- Servicios externos (HTTP, email, autenticación, etc.)

Esta guía crea una **estructura base agnóstica**. La implementación específica del ORM se configura después con las guías de `stacks/orm/`.

**Requiere:** [03-application-layer.md](./03-application-layer.md)

## Estructura Final

```
src/{ProjectName}.infrastructure/
├── {ProjectName}.infrastructure.csproj
└── services/                      # Servicios externos (opcional)

tests/{ProjectName}.infrastructure.tests/
└── {ProjectName}.infrastructure.tests.csproj
```

> **Nota:** La estructura específica del ORM (carpetas `nhibernate/`, `entityframework/`, etc.) se crea en el paso de configuración del stack correspondiente.

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

### 3. Crear carpeta services (opcional)

```bash
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

### Implementa Interfaces de Domain

```csharp
// Domain define la interfaz
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure la implementa (el ORM específico se define en stacks/)
public class UserRepository : IUserRepository
{
    // Implementación depende del ORM elegido
}
```

### Independencia de Framework en Application

```csharp
// ❌ INCORRECTO en Application - Acoplamiento a ORM
var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

// ✅ CORRECTO en Application - Usa abstracción
var user = await _userRepository.GetByEmailAsync(email);
```

## Verificación

```bash
dotnet build
```

## Siguiente Paso: Configurar ORM

Después de crear la estructura base, configura el ORM:

- **NHibernate (recomendado):** → [stacks/orm/nhibernate/setup.md](../../../stacks/orm/nhibernate/setup.md)
- **Entity Framework:** → `stacks/orm/entity-framework/setup.md`

## Siguiente Paso del Init

→ [05-webapi-layer.md](./05-webapi-layer.md)
