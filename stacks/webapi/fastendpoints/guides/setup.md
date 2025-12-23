# Setup FastEndpoints - WebApi Implementation

> **Versión:** 1.1.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo implementar la **capa de presentación WebApi con FastEndpoints** para un proyecto backend con Clean Architecture para APSYS. Esta guía se ejecuta después de completar la estructura base de WebApi (guía 05).

Esta implementación incluye:

- **FastEndpoints**: Framework ligero y performante para endpoints REST
- **JWT Bearer Authentication**: Autenticación con Identity Server
- **AutoMapper**: Mapeo automático entre objetos (Domain ↔ DTOs)
- **CORS**: Configuración de orígenes permitidos
- **Swagger/OpenAPI**: Documentación automática de API
- **Autorización personalizada**: Handlers y políticas custom
- **BaseEndpoint**: Clase base con helpers para manejo de errores

## Dependencias

- ✅ **Requiere:** [05-webapi-layer.md](../../05-webapi-layer.md) completado
- ✅ **Requiere:** Estructura base de WebApi creada

## Validaciones Previas

Antes de ejecutar los comandos, verifica:

1. ✅ Proyecto `{ProjectName}.webapi` existe
2. ✅ Estructura base de carpetas existe (endpoints/, dtos/, configuration/)
3. ✅ Program.cs base existe con endpoint /health

## Pasos de Implementación

### Paso 1: Instalar paquetes NuGet de FastEndpoints

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FastEndpoints
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FastEndpoints.Swagger
```

> **FastEndpoints**: Framework REPR (Request-Endpoint-Response) para ASP.NET Core
> **FastEndpoints.Swagger**: Integración con OpenAPI/Swagger

### Paso 2: Instalar paquetes de autenticación y mapeo

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package AutoMapper
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FluentResults
```

> **Microsoft.AspNetCore.Authentication.JwtBearer**: Autenticación JWT
> **AutoMapper**: Mapeo automático Domain ↔ DTOs
> **FluentResults**: Manejo de resultados con éxito/error

### Paso 3: Copiar templates de infrastructure

📁 COPIAR DIRECTORIO COMPLETO: `templates/init-clean-architecture/webapi-implementations/fastendpoints/infrastructure/` → `src/{ProjectName}.webapi/infrastructure/`

> El servidor MCP debe:
> 1. Descargar todos los archivos desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/infrastructure/` en GitHub
> 2. Copiarlos a `src/{ProjectName}.webapi/infrastructure/` respetando estructura
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivos incluidos (2):**
> - `ServiceCollectionExtender.cs` - Métodos de extensión para configuración de DI
> - `authorization/MustBeApplicationUser.cs` - Ejemplo de autorización personalizada

### Paso 4: Copiar templates de features

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/features/BaseEndpoint.cs` → `src/{ProjectName}.webapi/features/BaseEndpoint.cs`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/features/hello/HelloEndpoint.cs` → `src/{ProjectName}.webapi/features/hello/HelloEndpoint.cs`

> El servidor MCP debe:
> 1. Descargar cada archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/features/` en GitHub
> 2. Copiar a `src/{ProjectName}.webapi/features/` (mantener estructura de carpetas)
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivos incluidos (2):**
> - `BaseEndpoint.cs` - Clase base con helpers de manejo de errores
> - `hello/HelloEndpoint.cs` - Endpoint de ejemplo (GET /hello)

### Paso 5: Copiar templates de DTOs y mapping

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/dtos/GetManyAndCountResultDto.cs` → `src/{ProjectName}.webapi/dtos/GetManyAndCountResultDto.cs`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/mappingprofiles/MappingProfile.cs` → `src/{ProjectName}.webapi/mappingprofiles/MappingProfile.cs`

> El servidor MCP debe:
> 1. Descargar cada archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/` en GitHub
> 2. Copiar a las rutas de destino indicadas
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivos copiados (2):**
> - `GetManyAndCountResultDto.cs` - DTO genérico para resultados paginados
> - `MappingProfile.cs` - Perfil de AutoMapper con mapeo genérico

### Paso 6: Copiar extensiones y configuración

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/IPrincipalExtender.cs` → `src/{ProjectName}.webapi/IPrincipalExtender.cs`

> El servidor MCP debe:
> 1. Descargar archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/` en GitHub
> 2. Copiar a `src/{ProjectName}.webapi/`
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivo copiado:**
> - `IPrincipalExtender.cs` - Extensiones para obtener claims del usuario autenticado

### Paso 7: Sobrescribir Program.cs con configuración completa

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/Program.cs` → `src/{ProjectName}.webapi/Program.cs`

> El servidor MCP debe:
> 1. Descargar archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/Program.cs` en GitHub
> 2. **SOBRESCRIBIR** `src/{ProjectName}.webapi/Program.cs` (reemplazar el archivo base)
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivo principal:**
> - `Program.cs` - Configuración completa con FastEndpoints, JWT, CORS, Swagger, AutoMapper

### Paso 8: Actualizar archivo .env con configuración completa

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/.env.example` → `src/{ProjectName}.webapi/.env`

> El servidor MCP debe:
> 1. Descargar archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/.env.example` en GitHub
> 2. **SOBRESCRIBIR** `src/{ProjectName}.webapi/.env`
> 3. Mantener variables existentes si las hay
>
> **Contenido del .env:**
> ```bash
> # Identity Server Configuration
> IDENTITYSERVER_ADDRESS=https://your-identity-server.com
> IDENTITYSERVER_AUDIENCE=your-api-audience
>
> # CORS Configuration
> ALLOWED_HOSTS=http://localhost:3000,http://localhost:4200
>
> # Database Configuration
> DB_CONNECTION_STRING=your-connection-string
> ```

### Paso 9: Actualizar appsettings.json

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/webapi-implementations/fastendpoints/appsettings.json` → `src/{ProjectName}.webapi/appsettings.json`

> El servidor MCP debe:
> 1. Descargar archivo desde `templates/init-clean-architecture/webapi-implementations/fastendpoints/appsettings.json` en GitHub
> 2. **MERGE** con `src/{ProjectName}.webapi/appsettings.json` existente (no sobrescribir completamente)
> 3. Agregar secciones: `AllowedHosts`, `IdentityServerConfiguration`

## Estructura Resultante

```
src/{ProjectName}.webapi/
├── Program.cs                                    # ✅ Sobrescrito con FastEndpoints
├── README.md
├── appsettings.json                              # ✅ Actualizado
├── .env                                          # ✅ Actualizado
├── IPrincipalExtender.cs                         # 🆕 Nuevo
├── features/
│   ├── BaseEndpoint.cs                           # 🆕 Nuevo
│   └── hello/
│       └── HelloEndpoint.cs                      # 🆕 Nuevo
├── dtos/
│   ├── README.md
│   └── GetManyAndCountResultDto.cs               # 🆕 Nuevo
├── infrastructure/
│   ├── ServiceCollectionExtender.cs              # 🆕 Nuevo
│   └── authorization/
│       └── MustBeApplicationUser.cs              # 🆕 Nuevo
├── mappingprofiles/
│   └── MappingProfile.cs                         # 🆕 Nuevo
└── Properties/
    └── InternalsVisibleTo.cs
```

## Configuración Detallada

### Program.cs - Pipeline Completo

```csharp
using DotNetEnv;
using FastEndpoints;
using FastEndpoints.Swagger;
using {ProjectName}.webapi.infrastructure;
using {ProjectName}.application.usecases; // Para RegisterCommandsFromAssembly

// Cargar variables de entorno
Env.Load();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

// Configurar servicios (Dependency Injection)
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()                              // Políticas de autorización
    .ConfigureCors(configuration)                   // CORS
    .ConfigureIdentityServerClient(configuration)   // JWT Bearer
    .ConfigureUnitOfWork(configuration)             // UnitOfWork (TODO)
    .ConfigureAutoMapper()                          // AutoMapper
    .ConfigureValidators()                          // FluentValidation
    .ConfigureDependencyInjections(environment)     // DI custom
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()                             // FastEndpoints
    .SwaggerDocument();                              // Swagger

var app = builder.Build();

// Middleware pipeline
app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints(config =>
    {
        config.Endpoints.RoutePrefix = "api";
    })
    .UseSwagger()
    .UseSwaggerUI(opt =>
    {
        opt.DefaultModelsExpandDepth(-1);  // Ocultar schemas por defecto
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

// Registrar Commands/Handlers automáticamente
app.Services.RegisterCommandsFromAssembly(typeof(UseCaseExample).Assembly);

app.Run();

// Hacer Program accesible para tests de integración
public partial class Program { }
```

### ServiceCollectionExtender - Métodos de Configuración

**ConfigurePolicy():** Define políticas de autorización
```csharp
services.AddAuthorizationBuilder()
    .AddPolicy("DefaultAuthorizationPolicy", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("MustBeApplicationUser", policy => policy.AddRequirements(new MustBeApplicationUserRequirement()));

services.AddScoped<IAuthorizationHandler, MustBeApplicationUserHandler>();
```

**ConfigureCors():** Configura CORS desde appsettings.json
```csharp
var allowedHosts = configuration["AllowedHosts"]?.Split(',') ?? Array.Empty<string>();
services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins(allowedHosts)
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

**ConfigureIdentityServerClient():** Configura JWT Bearer
```csharp
var identityServerAddress = configuration["IdentityServerConfiguration:Address"];
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = identityServerAddress;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
```

**ConfigureAutoMapper():** Registra perfiles de AutoMapper
```csharp
services.AddAutoMapper(typeof(MappingProfile));
```

**RegisterCommandsFromAssembly():** Registro automático de Commands/Handlers
- Escanea assembly de Application buscando clases `Command` anidadas
- Registra automáticamente Command + Handler en DI
- Patrón: `UseCaseClass.Command` + `UseCaseClass.Handler`

### BaseEndpoint - Manejo de Errores

**HandleErrorAsync():** Para errores con propiedad específica
```csharp
await HandleErrorAsync(
    x => x.Email,
    "Email inválido",
    HttpStatusCode.BadRequest,
    ct);
```

**HandleUnexpectedErrorAsync():** Para errores inesperados
```csharp
try {
    // código
} catch (Exception ex) {
    await HandleUnexpectedErrorAsync(ex, ct);
}
```

### Ejemplo de Endpoint Completo

```csharp
using FastEndpoints;
using {ProjectName}.application.usecases.users;
using {ProjectName}.webapi.endpoints;

namespace {ProjectName}.webapi.endpoints.users;

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserEndpoint : BaseEndpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/users");
        Policies("MustBeApplicationUser");
        Summary(s =>
        {
            s.Summary = "Creates a new user";
            s.Description = "Creates a new user with the provided information";
        });
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        try
        {
            var command = new CreateUserUseCase.Command(req.Name, req.Email);
            var result = await command.ExecuteAsync();

            if (result.IsFailed)
            {
                await HandleErrorAsync(
                    x => x.Email,
                    result.Errors.First().Message,
                    HttpStatusCode.BadRequest,
                    ct);
                return;
            }

            var response = Mapper.Map<CreateUserResponse>(result.Value);
            await SendAsync(response, 201, ct);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedErrorAsync(ex, ct);
        }
    }
}
```

## Verificación

### 1. Compilar la solución

```bash
dotnet build
```

> Debería mostrar: "Build succeeded. 0 Warning(s). 0 Error(s)."

### 2. Verificar paquetes instalados

```bash
dotnet list src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package
```

Debería incluir:
- `DotNetEnv`
- `FastEndpoints`
- `FastEndpoints.Swagger`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `AutoMapper`
- `FluentResults`

### 3. Verificar estructura de archivos

```bash
ls -R src/{ProjectName}.webapi
```

Deberías ver todos los archivos mencionados en "Estructura Resultante".

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

### 5. Probar endpoint de hello

```bash
curl http://localhost:5000/api/hello
```

Debería responder (el mensaje varía según configuración):
```
Hello from {ProjectName} API - Environment: Development
```

### 6. Verificar Swagger

Abrir en navegador: `http://localhost:5000/swagger`

Deberías ver:
- Endpoint `/api/hello` (GET)
- Configuración de seguridad JWT (candado en endpoints protegidos)
- Esquemas de DTOs

## Características de FastEndpoints

### Vertical Slicing

Los endpoints se organizan por **feature** en lugar de por tipo técnico:

```
✅ Correcto (vertical slicing):
endpoints/
├── users/
│   ├── CreateUserEndpoint.cs
│   ├── GetUserEndpoint.cs
│   └── UpdateUserEndpoint.cs
└── products/
    ├── CreateProductEndpoint.cs
    └── GetProductEndpoint.cs

❌ Incorrecto (horizontal slicing):
controllers/
├── UsersController.cs
└── ProductsController.cs
```

### REPR Pattern

FastEndpoints sigue el patrón **Request-Endpoint-Response**:
- Una clase por endpoint (no controladores grandes)
- Request y Response específicos para cada endpoint
- Validación integrada con FluentValidation

### Ventajas sobre MVC Controllers

- ✅ Menos boilerplate
- ✅ Mejor performance (hasta 3x más rápido)
- ✅ Mejor organización (vertical slicing)
- ✅ Validación declarativa integrada
- ✅ Testing más simple

## Próximos Pasos

Una vez completada la implementación de FastEndpoints:

1. ✅ **Verificar que todo funciona** - Compilar y ejecutar
2. 📝 **Implementar endpoints reales** - Crear endpoints para tus use cases
3. 🔐 **Configurar Identity Server** - Actualizar URL en .env
4. 🗄️ **Configurar base de datos** - Implementar infrastructure específica

## Troubleshooting

### Error: "No identityServer configuration found"

**Solución:** Verificar que `.env` o `appsettings.json` tengan configurado `IdentityServerConfiguration:Address`.

### Error: "No CORS configuration found"

**Solución:** Verificar que `appsettings.json` tenga configurado `AllowedHosts`.

### Swagger no muestra endpoints

**Solución:**
1. Verificar que endpoints llamen a `Configure()` correctamente
2. Verificar que endpoints hereden de `Endpoint<TRequest, TResponse>`
3. Recompilar proyecto

### Endpoints no requieren autenticación

**Solución:**
1. Verificar que el endpoint NO use `AllowAnonymous()`
2. Verificar que Identity Server esté configurado correctamente
3. Agregar política: `Policies("DefaultAuthorizationPolicy")`

## Notas Importantes

### ConfigureUnitOfWork - Pendiente

El método `ConfigureUnitOfWork()` está marcado como TODO:
```csharp
public static IServiceCollection ConfigureUnitOfWork(...)
{
    //TODO: To be implemented when database is setup
}
```

Se implementará cuando se configure la infraestructura específica (NHibernate, EF, etc.).

### Autorización Personalizada

Para crear más políticas de autorización:

1. Crear `Requirement` + `Handler` en `infrastructure/authorization/`
2. Registrar en `ConfigurePolicy()`
3. Usar en endpoints con `Policies("NombrePolicy")`

### Variables de Entorno

El archivo `.env` se carga automáticamente en `Program.cs`. Las variables están disponibles en:
- `Environment.GetEnvironmentVariable("NOMBRE")`
- `configuration.GetSection("Nombre").Value`

## Historial de Versiones

### v1.0.0 (2025-01-30)

**Release inicial de la guía de implementación:**
- ✅ Separación de la guía base (05-webapi-layer.md)
- ✅ Configuración completa de FastEndpoints
- ✅ JWT Bearer, AutoMapper, CORS, Swagger
- ✅ Templates específicos de FastEndpoints
- ✅ Ejemplos de endpoints y autorización personalizada

**Origen:**
- Basado en la versión 1.4.5 de `05-webapi-configuration.md`
- Reestructurado como guía de implementación específica

---

> **Guía:** setup-fastendpoints.md
> **Categoría:** WebApi Implementations
> **Prerequisito:** [05-webapi-layer.md](../../05-webapi-layer.md)
