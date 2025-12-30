# 05 - Capa WebApi (WebApi Layer)

## Descripción

Crea la **capa de presentación WebApi** del proyecto. Esta guía crea la estructura base con:
- Program.cs mínimo con endpoint `/health`
- Estructura de carpetas para features (endpoints + modelos)
- Carpeta infrastructure para configuración de servicios
- Manejo de variables de entorno con `.env`

El framework específico (FastEndpoints, Minimal APIs, etc.) se configura en `stacks/webapi/`.

**Requiere:** [04-infrastructure-layer.md](./04-infrastructure-layer.md)

## Estructura Final

```
src/{ProjectName}.webapi/
├── {ProjectName}.webapi.csproj
├── Program.cs
├── .env
├── appsettings.json
├── features/                   # Endpoints organizados por feature
│   └── {feature}/
│       ├── endpoint/           # Endpoints del feature
│       └── models/             # DTOs request/response
├── infrastructure/             # Configuración de servicios (DI, etc.)
├── mappingprofiles/            # Perfiles de AutoMapper (opcional)
├── dtos/                       # DTOs compartidos (opcional)
└── Properties/
    └── InternalsVisibleTo.cs

tests/{ProjectName}.webapi.tests/
└── {ProjectName}.webapi.tests.csproj
```

## Paquetes NuGet

**WebApi:**
- `DotNetEnv` - Variables de entorno desde archivo .env

**Tests:**
- `Microsoft.AspNetCore.Mvc.Testing` - Tests de integración
- `FluentAssertions`

## Pasos

### 1. Crear proyecto webapi

```bash
dotnet new web -n {ProjectName}.webapi -o src/{ProjectName}.webapi
dotnet sln add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
```

### 2. Instalar DotNetEnv

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package DotNetEnv
```

### 3. Agregar referencias

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.application/{ProjectName}.application.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

### 4. Crear carpetas

```bash
mkdir src/{ProjectName}.webapi/features
mkdir src/{ProjectName}.webapi/infrastructure
```

### 5. Copiar templates

Copiar desde `{GUIDES_REPO}/templates/webapi/`:

| Template | Destino | Descripción |
|----------|---------|-------------|
| `Program.cs` | raíz | Program base con health endpoint |
| `.env` | raíz | Variables de entorno (no commitear) |
| `properties/InternalsVisibleTo.cs` | `Properties/` | Expone internals para tests |

### 6. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.webapi.tests -o tests/{ProjectName}.webapi.tests
dotnet sln add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj
rm tests/{ProjectName}.webapi.tests/UnitTest1.cs
```

### 7. Remover versiones en .csproj de tests

Editar el .csproj y eliminar atributos `Version`.

### 8. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package FluentAssertions
```

### 9. Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

## Program.cs Base

```csharp
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

app.Run();

public partial class Program { }
```

## Organización por Features

Los endpoints se organizan por feature, con sus modelos locales:

```
features/
├── users/
│   ├── endpoint/
│   │   ├── GetUserByIdEndpoint.cs
│   │   ├── GetUsersEndpoint.cs
│   │   └── CreateUserEndpoint.cs
│   └── models/
│       ├── GetUserByIdRequest.cs
│       ├── GetUserByIdResponse.cs
│       └── CreateUserRequest.cs
├── organizations/
│   ├── endpoint/
│   └── models/
└── hello/
    └── HelloEndpoint.cs          # Features simples sin modelos
```

## Configuración de Servicios (infrastructure/)

```csharp
// infrastructure/UseCaseServiceCollectionExtensions.cs
namespace {ProjectName}.webapi.infrastructure;

public static class UseCaseServiceCollectionExtensions
{
    public static IServiceCollection ConfigureUseCases(
        this IServiceCollection services)
    {
        // Use cases
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<GetUsersUseCase>();
        services.AddScoped<CreateUserUseCase>();

        return services;
    }
}
```

Usar en Program.cs:
```csharp
builder.Services.ConfigureUseCases();
```

## Verificación

```bash
dotnet build
dotnet run --project src/{ProjectName}.webapi

# En otra terminal:
curl http://localhost:5000/health
# Debe responder: {"status":"healthy",...}
```

## Variables de Entorno

- `.env` - Variables sensibles (NO commitear)
- `appsettings.json` - Configuración general

**Agregar a `.gitignore`:**
```
.env
```

## Implementaciones de Framework

Para agregar un framework de WebApi, ver:
- `stacks/webapi/fastendpoints/` - FastEndpoints (recomendado)
- `stacks/webapi/minimal-apis/` - Minimal APIs

## Proyecto Completo

Con este paso, el proyecto tiene la estructura completa de Clean Architecture:

```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── src/
│   ├── {ProjectName}.domain/
│   ├── {ProjectName}.application/
│   ├── {ProjectName}.infrastructure/
│   └── {ProjectName}.webapi/
└── tests/
    ├── {ProjectName}.domain.tests/
    ├── {ProjectName}.application.tests/
    ├── {ProjectName}.infrastructure.tests/
    └── {ProjectName}.webapi.tests/
```
