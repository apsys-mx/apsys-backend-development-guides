# 03 - Capa de Aplicación (Application Layer)

## Descripción

Crea la **capa de aplicación** del proyecto. Esta capa contiene:
- Casos de uso (lógica de aplicación/orquestación)
- Errores de aplicación

Esta capa es **independiente de infraestructura**. Solo depende de Domain.

**Requiere:** [02-domain-layer.md](./02-domain-layer.md)

## Estructura Final

```
src/{ProjectName}.application/
├── {ProjectName}.application.csproj
├── usecases/
│   └── {feature}/           # Casos de uso agrupados por feature
└── errors/                  # Errores de aplicación

tests/{ProjectName}.application.tests/
├── {ProjectName}.application.tests.csproj
└── ApplicationTestBase.cs
```

> **Nota:** Los DTOs y validadores de request/response se definen en la capa WebApi junto a los endpoints (en `features/{feature}/models/`).

## Paquetes NuGet

**Application:**
- `FastEndpoints` - Framework de endpoints

**Tests:**
- `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`
- `AutoFixture.AutoMoq`, `FluentAssertions`, `Castle.Core`

## Pasos

### 1. Crear proyecto application

```bash
dotnet new classlib -n {ProjectName}.application -o src/{ProjectName}.application
dotnet sln add src/{ProjectName}.application/{ProjectName}.application.csproj
rm src/{ProjectName}.application/Class1.cs
```

### 2. Instalar FastEndpoints

```bash
dotnet add src/{ProjectName}.application/{ProjectName}.application.csproj package FastEndpoints
```

### 3. Agregar referencia a domain

```bash
dotnet add src/{ProjectName}.application/{ProjectName}.application.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### 4. Crear carpetas

```bash
mkdir src/{ProjectName}.application/usecases
mkdir src/{ProjectName}.application/errors
```

### 5. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.application.tests -o tests/{ProjectName}.application.tests
dotnet sln add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj
rm tests/{ProjectName}.application.tests/UnitTest1.cs
```

### 6. Remover versiones en .csproj de tests

Editar el .csproj y eliminar atributos `Version` de los `PackageReference`.

### 7. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package Castle.Core
```

### 8. Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj reference src/{ProjectName}.application/{ProjectName}.application.csproj
```

### 9. Copiar template

Copiar `docs/guides/templates/tests/ApplicationTestBase.cs` a `tests/{ProjectName}.application.tests/`

## Principios

### Independencia de Infraestructura

```csharp
// ✅ CORRECTO - Usa interfaces de Domain
public class GetUserByIdUseCase
{
    private readonly IUserRepository _userRepository;
    public GetUserByIdUseCase(IUserRepository userRepository) => _userRepository = userRepository;
}

// ❌ INCORRECTO - Referencia implementación
public class GetUserByIdUseCase
{
    private readonly NHUserRepository _userRepository; // NO
}
```

### Casos de Uso por Feature

```
usecases/
├── users/
│   ├── GetUserByIdUseCase.cs
│   ├── GetUsersUseCase.cs
│   └── CreateUserUseCase.cs
├── organizations/
│   ├── GetOrganizationByIdUseCase.cs
│   └── CreateOrganizationUseCase.cs
└── folios/
    └── GenerateFolioUseCase.cs
```

### Ejemplo de Use Case

```csharp
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.application.usecases.users;

public class GetUserByIdUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ExecuteAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
}
```

### Errores de Aplicación

```csharp
// errors/UserNotFoundError.cs
namespace {ProjectName}.application.errors;

public class UserNotFoundError : Exception
{
    public UserNotFoundError(Guid userId)
        : base($"User with ID {userId} was not found") { }
}
```

## Verificación

```bash
dotnet build
dotnet test
```

## Siguiente Paso

→ [04-infrastructure-layer.md](./04-infrastructure-layer.md)
