# 05 - Capa de Presentación WebApi (Base Layer)

> **Versión:** 2.0.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo crear la **estructura base de la capa de presentación (WebApi Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta guía crea la estructura común que será implementada con un framework específico en el siguiente paso.

Esta capa base contiene:

- **Program.cs básico**: Configuración mínima de ASP.NET Core con endpoint /health
- **Estructura de carpetas**: Organización estándar para endpoints, DTOs y configuración
- **Configuración de entorno**: Manejo de variables con .env
- **Tests**: Proyecto base para tests de integración

> **Nota:** Esta guía crea solo la estructura base. El framework específico (FastEndpoints, Minimal APIs, etc.) se configura en el siguiente paso según el parámetro `--webapi-framework`.

## Dependencias

- ✅ **Requiere:** [01-estructura-base.md](01-estructura-base.md) completado
- ✅ **Requiere:** [02-domain-layer.md](02-domain-layer.md) completado
- ✅ **Requiere:** [03-application-layer.md](03-application-layer.md) completado
- ✅ **Requiere:** [04-infrastructure-layer.md](04-infrastructure-layer.md) completado

## Validaciones Previas

Antes de ejecutar los comandos, verifica:

1. ✅ SDK de .NET 9.0 instalado: `dotnet --version`
2. ✅ Proyectos Domain, Application e Infrastructure existen
3. ✅ Archivo `{ProjectName}.sln` existe en la raíz

## Pasos de Construcción

### Paso 1: Crear proyecto web para WebApi

```bash
dotnet new web -n {ProjectName}.webapi -o src/{ProjectName}.webapi
dotnet sln add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
```

> Se usa `dotnet new web` (minimal API) para tener control total sobre la configuración.

### Paso 2: Instalar paquete base para variables de entorno

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package DotNetEnv
```

> **DotNetEnv**: Gestión de variables de entorno desde archivo .env. Otros paquetes se instalarán en la guía de implementación específica.

### Paso 3: Agregar referencias de proyectos

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.application/{ProjectName}.application.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

> WebApi depende de todas las capas: Domain (interfaces), Application (use cases), Infrastructure (implementaciones).

### Paso 4: Copiar templates de estructura base

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/Program.cs` → `src/{ProjectName}.webapi/Program.cs`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/README.md` → `src/{ProjectName}.webapi/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/.env.example` → `src/{ProjectName}.webapi/.env`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/endpoints/README.md` → `src/{ProjectName}.webapi/endpoints/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/dtos/README.md` → `src/{ProjectName}.webapi/dtos/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/configuration/README.md` → `src/{ProjectName}.webapi/configuration/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi/Properties/InternalsVisibleTo.cs` → `src/{ProjectName}.webapi/Properties/InternalsVisibleTo.cs`

> El servidor MCP debe:
> 1. Descargar cada archivo desde `templates/init-clean-architecture/webapi/` en GitHub
> 2. Copiar a las rutas de destino indicadas (creando carpetas si no existen)
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivos copiados (7 archivos):**
> - `Program.cs` - Configuración mínima ASP.NET Core (DotNetEnv + endpoint /health)
> - `README.md` - Propósito general de la capa y guía de estructura
> - `.env` - Variables de entorno (copiado desde .env.example)
> - `endpoints/README.md` - Guía para implementar endpoints según framework
> - `dtos/README.md` - Guía para DTOs
> - `configuration/README.md` - Guía para configuración de DI
> - `Properties/InternalsVisibleTo.cs` - Configuración de visibilidad para tests

### Paso 5: Crear proyecto de tests para webapi

```bash
dotnet new nunit -n {ProjectName}.webapi.tests -o tests/{ProjectName}.webapi.tests
dotnet sln add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj
```

### Paso 6: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** Editar `tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### Paso 7: Instalar paquetes NuGet en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package FluentAssertions
```

> **Microsoft.AspNetCore.Mvc.Testing**: Para tests de integración de API

### Paso 8: Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### Paso 9: Eliminar archivos de test autogenerados

```bash
rm tests/{ProjectName}.webapi.tests/UnitTest1.cs
```

## Estructura Resultante

```
src/{ProjectName}.webapi/
├── Program.cs                         # Configuración básica ASP.NET Core
├── README.md                          # Propósito de la capa
├── appsettings.json                   # Configuración aplicación (autogenerado)
├── .env                               # Variables de entorno (NO commitear)
├── endpoints/
│   └── README.md                      # Guía para implementación
├── dtos/
│   └── README.md                      # Guía para DTOs
├── configuration/
│   └── README.md                      # Guía para DI y middleware
└── Properties/
    └── InternalsVisibleTo.cs          # Configuración para tests
```

## Contenido de Program.cs Base

El `Program.cs` base incluye solo configuración mínima de ASP.NET Core:

```csharp
using DotNetEnv;

// Cargar variables de entorno desde .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configuración básica para Swagger (útil para todos los frameworks)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline básico
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Endpoint de health check (común para todos los frameworks)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}))
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

// Hacer Program accesible para tests de integración
public partial class Program { }
```

> **Nota:** Este Program.cs será **sobrescrito o extendido** por la guía de implementación específica (FastEndpoints, Minimal APIs, etc.).

## Propósito de las Carpetas

### endpoints/

Contendrá los controladores o endpoints HTTP según el framework elegido.

**Con FastEndpoints (por defecto):**
```
endpoints/
├── users/
│   ├── CreateUserEndpoint.cs
│   └── GetUserEndpoint.cs
└── BaseEndpoint.cs
```

**Con Minimal APIs (futuro):**
```
endpoints/
├── UsersEndpoints.cs
└── ProductsEndpoints.cs
```

### dtos/

Data Transfer Objects para la API.

**Estructura común:**
```
dtos/
├── users/
│   ├── CreateUserRequest.cs
│   ├── UserResponse.cs
│   └── UserListResponse.cs
└── common/
    ├── PaginatedResultDto.cs
    └── ErrorResponse.cs
```

### configuration/

Configuración de servicios, DI, middleware, etc.

**Ejemplos (se crearán en guía de implementación):**
- `ServiceCollectionExtensions.cs` - Registro de servicios
- `CorsConfiguration.cs` - Configuración de CORS
- `AuthenticationConfiguration.cs` - JWT/OAuth
- `SwaggerConfiguration.cs` - Documentación OpenAPI

## Verificación

### 1. Compilar la solución

```bash
dotnet build
```

> Debería mostrar: "Build succeeded. 0 Warning(s). 0 Error(s)."

### 2. Verificar estructura de carpetas

```bash
ls -R src/{ProjectName}.webapi
```

Deberías ver:
- `Program.cs`
- `README.md`
- `appsettings.json`
- `.env`
- `endpoints/README.md`
- `dtos/README.md`
- `configuration/README.md`
- `Properties/InternalsVisibleTo.cs`

### 3. Verificar referencias del proyecto

```bash
dotnet list src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference
```

Debería mostrar:
- `src/{ProjectName}.domain/{ProjectName}.domain.csproj`
- `src/{ProjectName}.application/{ProjectName}.application.csproj`
- `src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj`

### 4. Ejecutar la aplicación

```bash
cd src/{ProjectName}.webapi
dotnet run
```

Deberías ver:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### 5. Probar endpoint de health check

```bash
curl http://localhost:5000/health
```

Debería responder:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-30T12:34:56.789Z",
  "environment": "Development"
}
```

### 6. Verificar Swagger

Abrir en navegador: `http://localhost:5000/swagger`

Deberías ver la documentación de la API con el endpoint `/health`.

## Próximos Pasos

Una vez completada la estructura base de WebApi:

1. ✅ **Verificar que compila** - Todo debe compilar sin errores
2. ⏭️ **Ejecutar guía de implementación** según parámetro `--webapi-framework`:
   - **FastEndpoints (default)**: `webapi-implementations/fastendpoints/setup-fastendpoints.md`
   - **Minimal APIs (futuro)**: `webapi-implementations/minimal-apis/setup-minimal-apis.md`
   - **MVC (futuro)**: `webapi-implementations/mvc/setup-mvc.md`

## Framework de WebApi

El framework específico se elige mediante el parámetro `--webapi-framework` al ejecutar el comando de inicialización:

```bash
# Opción por defecto (FastEndpoints)
/init-clean-architecture --project-name=MyProject --webapi-framework=fastendpoints

# Futuras opciones
/init-clean-architecture --project-name=MyProject --webapi-framework=minimal-apis
/init-clean-architecture --project-name=MyProject --webapi-framework=mvc
```

### Implementaciones disponibles:

| Framework | Estado | Guía |
|-----------|--------|------|
| **FastEndpoints** | ✅ Disponible | [setup-fastendpoints.md](webapi-implementations/fastendpoints/setup-fastendpoints.md) |
| **Minimal APIs** | 🔜 Próximamente | `webapi-implementations/minimal-apis/` |
| **MVC** | 🔜 Próximamente | `webapi-implementations/mvc/` |

## Notas Importantes

### Esta es una Capa Base

Esta guía crea **solo la estructura base** común a todos los frameworks. El contenido específico (endpoints, autorización, DI, etc.) se agrega en la guía de implementación.

**Ventajas:**
- ✅ Proyecto funcional desde el inicio (endpoint /health)
- ✅ Estructura estándar independiente del framework
- ✅ Fácil cambiar de framework posteriormente
- ✅ README.md en cada carpeta como guía

### Variables de Entorno

El archivo `.env` contiene variables sensibles. **Nunca** lo commites a Git.

**Debe estar en `.gitignore`:**
```
# Environment variables
.env
```

**El archivo `.env.example` sirve como plantilla** para documentar qué variables necesita el proyecto.

### appsettings.json vs .env

- **appsettings.json**: Configuración general, NO sensible (puede commitearse)
- **.env**: Variables sensibles (passwords, secrets, connection strings)

```csharp
// Usar valores de .env sobrescribiendo appsettings.json
var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? configuration.GetConnectionString("DefaultConnection");
```

### Program.cs Será Sobrescrito

El `Program.cs` base es **temporal**. La guía de implementación específica lo sobrescribirá con la configuración completa del framework elegido.

## Historial de Versiones

### v2.0.0 (2025-01-30)

**Reestructuración para soporte multi-framework:**
- ✅ **Capa base agnóstica**: Estructura común para todos los frameworks
- ✅ **Program.cs mínimo**: Solo DotNetEnv + endpoint /health
- ✅ **Preparado para implementaciones**: Se complementa con guías específicas
- ✅ **Organización modular**: Base + implementación = WebApi completa
- ✅ **Soporte para slash command**: Parametrizable con `--webapi-framework`

**Rationale:**
- Permite elegir framework (FastEndpoints, Minimal APIs, MVC)
- Estructura base reutilizable entre frameworks
- Código específico separado en guías de implementación
- Facilita agregar nuevos frameworks en el futuro

**Flujo de ejecución:**
1. Esta guía (05) crea la estructura base
2. Guía de implementación agrega framework específico
3. Proyecto completo y funcional

**Breaking changes:**
- Ya NO instala FastEndpoints directamente (se hace en siguiente paso)
- Ya NO copia templates específicos (BaseEndpoint, ServiceCollectionExtender, etc.)
- Para FastEndpoints completo, ejecutar también `webapi-implementations/fastendpoints/setup-fastendpoints.md`

### v1.4.5 (2025-01-30)

**Versión anterior monolítica:**
- Instalaba FastEndpoints directamente en esta guía
- Copiaba todos los templates específicos de FastEndpoints
- No permitía elegir otro framework
- **Esta versión fue movida a:** `webapi-implementations/fastendpoints/setup-fastendpoints.md`

---

> **Guía:** 05-webapi-layer.md
> **Milestone:** 4 - WebApi Base Layer
> **Siguiente:** Implementación específica según `--webapi-framework` (default: FastEndpoints)
