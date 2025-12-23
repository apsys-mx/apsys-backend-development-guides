# 05 - Capa WebApi (WebApi Layer)

## Descripción

Crea la **capa de presentación WebApi** del proyecto. Esta guía crea la estructura base con:
- Program.cs mínimo con endpoint `/health`
- Estructura de carpetas para endpoints, DTOs y configuración
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
├── endpoints/
├── dtos/
├── configuration/
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
mkdir src/{ProjectName}.webapi/endpoints
mkdir src/{ProjectName}.webapi/dtos
mkdir src/{ProjectName}.webapi/configuration
```

### 5. Copiar templates

Copiar desde `templates/webapi/`:
- `Program.cs`
- `.env.example` → `.env`
- READMEs para cada carpeta
- `Properties/InternalsVisibleTo.cs`

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
builder.Services.AddSwaggerGen();

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
