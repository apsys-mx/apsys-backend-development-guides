# 02 - Capa de Dominio (Domain Layer)

## Descripción

Crea la **capa de dominio** del proyecto. Esta capa contiene:
- Entidades de dominio (objetos de negocio con reglas y validaciones)
- Interfaces de repositorios (contratos que implementará Infrastructure)
- Excepciones de dominio
- Proyecto de tests unitarios

Esta capa es **independiente de la infraestructura** y de cualquier base de datos.

**Requiere:** [01-estructura-base.md](./01-estructura-base.md)

## Estructura Final

```
src/{ProjectName}.domain/
├── {ProjectName}.domain.csproj
├── entities/
│   ├── AbstractDomainObject.cs
│   └── validators/             # Validadores de entidades
├── interfaces/
│   └── repositories/
│       ├── IRepository.cs      # Interfaz base CRUD
│       ├── IReadOnlyRepository.cs  # Interfaz solo lectura
│       ├── IUnitOfWork.cs
│       ├── SortingCriteria.cs
│       └── GetManyAndCountResult.cs
└── exceptions/
    ├── InvalidDomainException.cs
    ├── InvalidFilterArgumentException.cs
    ├── ResourceNotFoundException.cs
    └── DuplicatedDomainException.cs

tests/{ProjectName}.domain.tests/
├── {ProjectName}.domain.tests.csproj
└── entities/
    └── DomainTestBase.cs
```

> **Nota:** Las carpetas `enums/`, `events/`, `valueobjects/` se crean según se necesiten.

## Paquetes NuGet

**Domain:**
- `FluentValidation` - Validación de entidades

**Tests:**
- `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk` (incluidos en template)
- `AutoFixture.AutoMoq` - Generación de datos de prueba
- `FluentAssertions` - Aserciones fluidas
- `Castle.Core` - Evita warnings de Moq

## Pasos

### 1. Crear proyecto domain

```bash
dotnet new classlib -n {ProjectName}.domain -o src/{ProjectName}.domain
dotnet sln add src/{ProjectName}.domain/{ProjectName}.domain.csproj
rm src/{ProjectName}.domain/Class1.cs
```

### 2. Instalar FluentValidation

```bash
dotnet add src/{ProjectName}.domain/{ProjectName}.domain.csproj package FluentValidation
```

### 3. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.domain.tests -o tests/{ProjectName}.domain.tests
dotnet sln add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj
rm tests/{ProjectName}.domain.tests/UnitTest1.cs
```

### 4. Remover versiones en .csproj de tests

Editar `tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### 5. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package Castle.Core
```

### 6. Agregar referencia a domain

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### 7. Crear carpetas

```bash
mkdir src/{ProjectName}.domain/entities
mkdir src/{ProjectName}.domain/entities/validators
mkdir src/{ProjectName}.domain/interfaces
mkdir src/{ProjectName}.domain/interfaces/repositories
mkdir src/{ProjectName}.domain/exceptions
mkdir tests/{ProjectName}.domain.tests/entities
```

### 8. Copiar templates

Copiar desde `templates/domain/` a `src/{ProjectName}.domain/`:

| Template | Destino | Descripción |
|----------|---------|-------------|
| `entities/AbstractDomainObject.cs` | `entities/` | Clase base para entidades con Id, CreationDate y validación |
| `exceptions/InvalidDomainException.cs` | `exceptions/` | Excepción para validaciones de dominio fallidas |
| `exceptions/InvalidFilterArgumentException.cs` | `exceptions/` | Excepción para argumentos de filtrado inválidos |
| `exceptions/ResourceNotFoundException.cs` | `exceptions/` | Excepción cuando no se encuentra un recurso |
| `exceptions/DuplicatedDomainException.cs` | `exceptions/` | Excepción cuando hay duplicados |
| `interfaces/repositories/IRepository.cs` | `interfaces/repositories/` | Interfaz base con operaciones CRUD |
| `interfaces/repositories/IReadOnlyRepository.cs` | `interfaces/repositories/` | Interfaz solo lectura con paginación |
| `interfaces/repositories/IUnitOfWork.cs` | `interfaces/repositories/` | Patrón Unit of Work |
| `interfaces/repositories/SortingCriteria.cs` | `interfaces/repositories/` | Modelo para criterios de ordenamiento |
| `interfaces/repositories/GetManyAndCountResult.cs` | `interfaces/repositories/` | Resultado paginado con conteo |

Copiar desde `templates/tests/` a `tests/{ProjectName}.domain.tests/entities/`:
- `DomainTestBase.cs`

**Reemplazar `{ProjectName}`** en namespaces con el nombre real del proyecto.

## Principios

### Interfaces en Domain, Implementaciones en Infrastructure

```csharp
// Domain define la interfaz (contratos)
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure implementa (depende del ORM elegido)
public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
{
    // Implementación con NHibernate
}
```

### Validadores Junto a las Entidades

```csharp
// entities/User.cs
public class User : AbstractDomainObject
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// entities/validators/UserValidator.cs
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

## Verificación

```bash
dotnet build
dotnet test
```

## Siguiente Paso

→ [03-application-layer.md](./03-application-layer.md)
