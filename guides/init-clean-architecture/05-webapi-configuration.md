# 05 - Configuración de WebApi (Presentation Layer)

> **Versión:** 1.4.2 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo crear y configurar la **capa de presentación (WebApi Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta capa contiene:

- **Program.cs**: Configuración de la aplicación y middleware pipeline
- **FastEndpoints**: Endpoints RESTful con FastEndpoints
- **Dependency Injection**: Configuración de servicios y contenedor DI
- **Authentication & Authorization**: JWT Bearer con Identity Server
- **CORS**: Configuración de orígenes permitidos
- **Swagger/OpenAPI**: Documentación automática de API
- **AutoMapper**: Configuración de mapeo de objetos
- **BaseEndpoint**: Clase base con helpers para manejo de errores

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
mkdir src/{ProjectName}.webapi
dotnet new web -n {ProjectName}.webapi -o src/{ProjectName}.webapi
dotnet sln add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
```

> Se usa `dotnet new web` (minimal API) en lugar de `webapi` para tener control total sobre la configuración.

### Paso 2: Eliminar archivo Program.cs autogenerado

```bash
rm src/{ProjectName}.webapi/Program.cs
```

> Lo reemplazaremos con nuestra versión personalizada usando templates.

### Paso 3: Instalar paquetes NuGet en webapi

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FastEndpoints
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FastEndpoints.Swagger
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package AutoMapper
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package FluentResults
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package DotNetEnv
```

> **Paquetes:**
> - **FastEndpoints**: Framework ligero para endpoints REST
> - **FastEndpoints.Swagger**: Generación automática de documentación OpenAPI
> - **Microsoft.AspNetCore.Authentication.JwtBearer**: Autenticación JWT Bearer
> - **AutoMapper**: Mapeo automático entre objetos (Domain ↔ DTOs)
> - **FluentResults**: Manejo de resultados con éxito/error
> - **DotNetEnv**: Carga de variables de entorno desde archivo .env

### Paso 4: Agregar referencias de proyectos

```bash
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.application/{ProjectName}.application.csproj
dotnet add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

> WebApi depende de todas las capas: Domain (interfaces), Application (use cases), Infrastructure (implementaciones).

### Paso 5: Crear estructura de carpetas

```bash
mkdir src/{ProjectName}.webapi/features
mkdir src/{ProjectName}.webapi/features/hello
mkdir src/{ProjectName}.webapi/infrastructure
mkdir src/{ProjectName}.webapi/infrastructure/authorization
mkdir src/{ProjectName}.webapi/dtos
mkdir src/{ProjectName}.webapi/mappingprofiles
mkdir src/{ProjectName}.webapi/Properties
```

> **Estructura:**
> - `features/`: Endpoints organizados por feature (vertical slicing)
> - `infrastructure/`: Configuración de DI, autorización, etc.
> - `dtos/`: Data Transfer Objects para API
> - `mappingprofiles/`: Perfiles de AutoMapper
> - `Properties/`: Configuración del assembly (InternalsVisibleTo)

### Paso 6: Copiar templates de configuración base

📄 COPIAR TEMPLATE: `templates/webapi/Program.cs` → `src/{ProjectName}.webapi/Program.cs`

📄 COPIAR TEMPLATE: `templates/webapi/IPrincipalExtender.cs` → `src/{ProjectName}.webapi/IPrincipalExtender.cs`

📄 COPIAR TEMPLATE: `templates/webapi/Properties/InternalsVisibleTo.cs` → `src/{ProjectName}.webapi/Properties/InternalsVisibleTo.cs`

> **Archivos copiados (3):**
> - `Program.cs` - Configuración principal y pipeline de middleware
> - `IPrincipalExtender.cs` - Extensiones para obtener claims del usuario autenticado
> - `Properties/InternalsVisibleTo.cs` - Configuración de visibilidad para tests

### Paso 7: Copiar templates de infrastructure

📁 COPIAR DIRECTORIO COMPLETO: `templates/webapi/infrastructure/` → `src/{ProjectName}.webapi/infrastructure/`

> **Archivos incluidos (2):**
> - `ServiceCollectionExtender.cs` - Métodos de extensión para configuración de DI
> - `authorization/MustBeApplicationUser.cs` - Ejemplo de autorización personalizada

### Paso 8: Copiar templates de features

📁 COPIAR DIRECTORIO COMPLETO: `templates/webapi/features/` → `src/{ProjectName}.webapi/features/`

> **Archivos incluidos (2):**
> - `BaseEndpoint.cs` - Clase base con helpers de manejo de errores
> - `hello/HelloEndpoint.cs` - Endpoint de ejemplo (GET /hello)

### Paso 9: Copiar templates de DTOs y mapping

📄 COPIAR TEMPLATE: `templates/webapi/dtos/GetManyAndCountResultDto.cs` → `src/{ProjectName}.webapi/dtos/GetManyAndCountResultDto.cs`

📄 COPIAR TEMPLATE: `templates/webapi/mappingprofiles/MappingProfile.cs` → `src/{ProjectName}.webapi/mappingprofiles/MappingProfile.cs`

> **Archivos copiados (2):**
> - `GetManyAndCountResultDto.cs` - DTO genérico para resultados paginados
> - `MappingProfile.cs` - Perfil de AutoMapper con mapeo genérico

### Paso 10: Crear proyecto de tests para webapi

```bash
mkdir tests/{ProjectName}.webapi.tests
dotnet new nunit -n {ProjectName}.webapi.tests -o tests/{ProjectName}.webapi.tests
dotnet sln add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj
```

### Paso 11: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** Editar `tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### Paso 12: Instalar paquetes NuGet en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj package FluentAssertions
```

> **Microsoft.AspNetCore.Mvc.Testing**: Para tests de integración de API

### Paso 13: Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
dotnet add tests/{ProjectName}.webapi.tests/{ProjectName}.webapi.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### Paso 14: Eliminar archivos de test autogenerados

```bash
rm tests/{ProjectName}.webapi.tests/UnitTest1.cs
```

### Paso 15: Crear archivo .env de configuración

**Crear archivo:** `src/{ProjectName}.webapi/.env`

```bash
# Identity Server Configuration
IDENTITYSERVER_ADDRESS=https://your-identity-server.com
IDENTITYSERVER_AUDIENCE=your-api-audience

# CORS Configuration
ALLOWED_HOSTS=http://localhost:3000,http://localhost:4200

# Database Configuration (for future use)
DB_CONNECTION_STRING=your-connection-string
```

> **Importante:** Agregar `.env` al `.gitignore` para no commitear credenciales.

### Paso 16: Configurar appsettings.json

**Editar:** `src/{ProjectName}.webapi/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "http://localhost:3000,http://localhost:4200",
  "IdentityServerConfiguration": {
    "Address": "https://your-identity-server.com",
    "Audience": "your-api-audience"
  }
}
```

## Referencia de Templates

### Configuración Base

| Archivo | Propósito |
|---------|-----------|
| **Program.cs** | Punto de entrada de la aplicación. Configura servicios (DI), middleware pipeline, FastEndpoints, Swagger, autenticación/autorización. Carga variables de entorno desde .env |
| **IPrincipalExtender.cs** | Extensiones para `IPrincipal` que facilitan obtener claims del usuario autenticado (username, email, name). Útil en endpoints para identificar usuario actual |
| **Properties/InternalsVisibleTo.cs** | Permite que el assembly de tests acceda a miembros `internal` del proyecto webapi |

### Infrastructure

| Archivo | Propósito |
|---------|-----------|
| **ServiceCollectionExtender.cs** | Métodos de extensión para `IServiceCollection` que organizan la configuración de DI: políticas de autorización, CORS, Identity Server (JWT Bearer), UnitOfWork, AutoMapper, validadores, registro automático de Commands/Handlers |
| **authorization/MustBeApplicationUser.cs** | Ejemplo de autorización personalizada usando `IAuthorizationRequirement` y `AuthorizationHandler`. Verifica que el usuario existe en la base de datos |

### Features

| Archivo | Propósito |
|---------|-----------|
| **BaseEndpoint.cs** | Clase base genérica para endpoints con helpers de manejo de errores: `HandleErrorAsync` (errores con propiedad específica), `HandleUnexpectedErrorAsync` (errores inesperados con logging) |
| **hello/HelloEndpoint.cs** | Endpoint de ejemplo (GET /hello) sin request/response tipado. Muestra cómo crear un endpoint simple con FastEndpoints, logging y acceso a configuración |

### DTOs y Mapping

| Archivo | Propósito |
|---------|-----------|
| **GetManyAndCountResultDto.cs** | DTO genérico para resultados paginados. Contiene: Items, Count, PageNumber, PageSize, SortBy, SortCriteria. Usado para transferir resultados de GetManyAndCount al cliente |
| **MappingProfile.cs** | Perfil de AutoMapper con mapeo genérico de `GetManyAndCountResult<T>` (Domain) a `GetManyAndCountResultDto<T>` (DTO). Mapea automáticamente propiedades de sorting |

## Configuración Detallada

### Program.cs - Pipeline de Configuración

```csharp
// 1. Cargar variables de entorno
DotNetEnv.Env.Load();

// 2. Configurar servicios (Dependency Injection)
builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()              // Políticas de autorización
    .ConfigureCors(configuration)   // CORS desde config
    .ConfigureIdentityServerClient(configuration)  // JWT Bearer
    .ConfigureUnitOfWork(configuration)  // UnitOfWork (TODO)
    .ConfigureAutoMapper()          // AutoMapper
    .ConfigureValidators()          // FluentValidation
    .ConfigureDependencyInjections(environment)  // DI custom
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()             // FastEndpoints
    .SwaggerDocument();              // Swagger

// 3. Configurar middleware pipeline
app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints()
    .UseSwagger()
    .UseSwaggerUI(opt => {
        opt.DefaultModelsExpandDepth(-1);  // Ocultar schemas por defecto
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

// 4. Registrar Commands/Handlers automáticamente
app.Services.RegisterCommandsFromAssembly(typeof(UseCaseExample).Assembly);
```

### ServiceCollectionExtender.cs - Métodos de Configuración

**ConfigurePolicy():** Define políticas de autorización
- `DefaultAuthorizationPolicy`: Requiere usuario autenticado
- `MustBeApplicationUser`: Verifica usuario en BD

**ConfigureCors():** Configura CORS desde appsettings.json
- Lee `AllowedHosts` de configuración
- Permite cualquier método y header

**ConfigureIdentityServerClient():** Configura JWT Bearer
- Lee URL de Identity Server desde configuración
- Configura validación de tokens JWT
- `ValidateAudience = false` (ajustar según necesidad)

**ConfigureUnitOfWork():** Configura DI del UnitOfWork
- ⚠️ **TODO**: Implementar cuando configure-database esté listo
- Registrará `IUnitOfWork` → `NHUnitOfWork` con Session de NHibernate

**ConfigureAutoMapper():** Registra perfiles de AutoMapper
- Registra `MappingProfile` con mapeo genérico

**RegisterCommandsFromAssembly():** Registro automático de Commands/Handlers
- Escanea assembly de Application buscando clases `Command` anidadas
- Registra automáticamente Command + Handler en DI
- Patrón: `UseCaseClass.Command` + `UseCaseClass.Handler`

### BaseEndpoint.cs - Manejo de Errores

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
public class CreateUserEndpoint : BaseEndpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/api/users");
        Policies("MustBeApplicationUser");
        Summary(s => {
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

### 2. Verificar estructura de carpetas

```bash
ls -R src/{ProjectName}.webapi
```

Deberías ver:
- `Program.cs`
- `IPrincipalExtender.cs`
- `appsettings.json`
- `.env`
- `features/` con `BaseEndpoint.cs` y `hello/HelloEndpoint.cs`
- `infrastructure/` con `ServiceCollectionExtender.cs` y `authorization/MustBeApplicationUser.cs`
- `dtos/` con `GetManyAndCountResultDto.cs`
- `mappingprofiles/` con `MappingProfile.cs`
- `Properties/` con `InternalsVisibleTo.cs`

### 3. Verificar referencias del proyecto

```bash
dotnet list src/{ProjectName}.webapi/{ProjectName}.webapi.csproj reference
```

Debería mostrar:
- `src/{ProjectName}.domain/{ProjectName}.domain.csproj`
- `src/{ProjectName}.application/{ProjectName}.application.csproj`
- `src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj`

### 4. Verificar paquetes instalados

```bash
dotnet list src/{ProjectName}.webapi/{ProjectName}.webapi.csproj package
```

Debería incluir:
- `FastEndpoints`
- `FastEndpoints.Swagger`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `AutoMapper`
- `FluentResults`
- `DotNetEnv`

### 5. Ejecutar la aplicación

```bash
cd src/{ProjectName}.webapi
dotnet run
```

Deberías ver:
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

### 6. Probar endpoint de ejemplo

```bash
curl http://localhost:5000/hello
```

Debería responder:
```
Hello to the datos maestros api from environment Development
```

### 7. Verificar Swagger

Abrir en navegador: `http://localhost:5000/swagger`

Deberías ver la documentación de la API con el endpoint `/hello`.

## Próximos Pasos

Una vez completada la capa de presentación:

1. **Migrations** - Crear esquema de base de datos con FluentMigrator (guía 06)
2. **Testing Support** - Configurar proyectos de testing auxiliares completamente (guía 07)
3. **Implementar endpoints reales** - Crear endpoints para tus use cases
4. **Configurar base de datos** - Tool `configure-database` para PostgreSQL/SQL Server

## Notas Importantes

### Vertical Slicing con Features

Los endpoints se organizan por **feature** (vertical slicing) en lugar de por tipo técnico:

```
✅ Correcto (vertical slicing):
features/
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

**Ventajas:**
- Cambios aislados por feature
- Fácil encontrar código relacionado
- Deploy independiente de features

### FastEndpoints vs MVC Controllers

FastEndpoints ofrece:
- Menos boilerplate
- Mejor performance
- Un endpoint = una clase (REPR pattern)
- Validación integrada con FluentValidation
- Configuración declarativa simple

### Autorización Personalizada

Para crear más políticas de autorización:

1. Crear `Requirement` + `Handler` en `infrastructure/authorization/`
2. Registrar en `ConfigurePolicy()`
3. Usar en endpoints con `Policies("NombrePolicy")`

### AutoMapper Genérico

El mapeo genérico en `MappingProfile.cs` funciona automáticamente para:
- `GetManyAndCountResult<User>` → `GetManyAndCountResultDto<UserDto>`
- `GetManyAndCountResult<Product>` → `GetManyAndCountResultDto<ProductDto>`

Para mapeos específicos, agregar al perfil:
```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
```

### Variables de Entorno

El archivo `.env` se carga automáticamente en `Program.cs`:
```csharp
DotNetEnv.Env.Load();
```

Las variables están disponibles en:
- `Environment.GetEnvironmentVariable("NOMBRE")`
- `configuration.GetSection("Nombre").Value` (si están en appsettings.json)

### ConfigureUnitOfWork - Pendiente

El método `ConfigureUnitOfWork()` está marcado como TODO:
```csharp
public static IServiceCollection ConfigureUnitOfWork(...)
{
    //TODO: To be implemented when database is setup
}
```

Se implementará en la guía `configure-database` con:
```csharp
services.AddScoped<ISession>((serviceProvider) =>
{
    var sessionFactory = serviceProvider.GetRequiredService<ISessionFactory>();
    return sessionFactory.OpenSession();
});
services.AddScoped<IUnitOfWork, NHUnitOfWork>();
```

## Troubleshooting

### Error: "No identityServer configuration found"

**Solución:** Verificar que `appsettings.json` y/o `.env` tengan configurado `IdentityServerConfiguration:Address`.

### Error: "No CORS configuration found"

**Solución:** Verificar que `appsettings.json` tenga configurado `AllowedHosts`.

### Swagger no muestra endpoints

**Solución:**
1. Verificar que endpoints llamen a `Configure()` correctamente
2. Verificar que endpoints hereden de `Endpoint<TRequest, TResponse>` o `EndpointWithoutRequest`
3. Recompilar proyecto

### Endpoints no requieren autenticación

**Solución:**
1. Verificar que el endpoint NO use `AllowAnonymous()`
2. Verificar que Identity Server esté configurado correctamente
3. Agregar política: `Policies("DefaultAuthorizationPolicy")`

## Historial de Versiones

### v1.4.2 (2025-01-30)

**Correcciones:**
- ✅ **Bloques de código**: Eliminados espacios en blanco al final de todas las líneas de cierre de bloques de código (```)
- ✅ Afectó 32 líneas de cierre de bloques de código en toda la guía
- ✅ El parser del MCP espera que las líneas terminen con ```$ (tres backticks seguidos por fin de línea)
- ✅ Los espacios en blanco al final rompían la detección de bloques de código

**Impacto:**
- El parser del servidor MCP ahora puede detectar correctamente el inicio y fin de bloques de código
- Mejora la extracción de comandos y ejemplos de código de la guía

**Patrón correcto:**
```bash
comando aqui
```
(sin espacios después de los backticks de cierre)

### v1.4.1 (2025-01-30)

**Correcciones:**
- ✅ **Pasos 6-9**: Corregidos patrones de copia de templates para ser compatibles con el parser del servidor MCP
- ✅ **Paso 6**: Cambiado de `📁 COPIAR ARCHIVOS:` a `📄 COPIAR TEMPLATE:` para archivos individuales
- ✅ **Paso 7-8**: Cambiado de `📁 COPIAR ARCHIVOS:` a `📁 COPIAR DIRECTORIO COMPLETO:` para directorios
- ✅ **Paso 9**: Cambiado de `📁 COPIAR ARCHIVOS:` a `📄 COPIAR TEMPLATE:` para archivos individuales

**Impacto:**
- El servidor MCP ahora puede parsear correctamente los pasos y copiar los templates
- Los proyectos generados ahora incluyen todos los archivos de webapi correctamente

**Patrones correctos:**
- Archivo individual: `📄 COPIAR TEMPLATE: source → destination`
- Directorio completo: `📁 COPIAR DIRECTORIO COMPLETO: source → destination`

### v1.4.0 (2025-01-30)

**Release inicial:**
- ✅ Guía completa de WebApi Layer
- ✅ 9 templates de webapi (Program.cs, ServiceCollectionExtender, BaseEndpoint, etc.)
- ✅ Configuración de FastEndpoints, Swagger, JWT Bearer, CORS, AutoMapper
- ✅ Ejemplos de endpoints y autorización personalizada

---

> **Guía:** 05-webapi-configuration.md
> **Milestone:** 4 - WebApi Configuration
> **Siguiente:** 06-migrations-base.md
