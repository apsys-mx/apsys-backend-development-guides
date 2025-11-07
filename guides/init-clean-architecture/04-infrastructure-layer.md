# 04 - Capa de Infraestructura (Infrastructure Layer)

> **Versión:** 2.0.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo crear la **capa de infraestructura (Infrastructure Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta versión crea una estructura **agnóstica de tecnología** que podrá ser implementada posteriormente con diferentes frameworks de persistencia.

Esta capa contendrá:

- **Repositorios**: Implementaciones concretas de IRepository e IReadOnlyRepository
- **Persistencia**: Configuración de acceso a datos (ORM, conexiones, etc.)
- **Servicios externos**: Clientes HTTP, APIs externas, servicios de email, etc.
- **Configuración**: Setup de infraestructura y servicios

> **Nota:** Esta guía crea solo la estructura base. Para implementaciones específicas (NHibernate, Entity Framework, etc.), consulta las guías en `guides/stack-implementations/`.

## Dependencias

- ✅ **Requiere:** [01-estructura-base.md](01-estructura-base.md) completado
- ✅ **Requiere:** [02-domain-layer.md](02-domain-layer.md) completado
- ⚠️ **Recomendado:** [03-application-layer.md](03-application-layer.md) completado (para entender qué necesita Application)

## Validaciones Previas

Antes de ejecutar los comandos, verifica:

1. ✅ SDK de .NET 9.0 instalado: `dotnet --version`
2. ✅ Proyecto Domain existe: verificar `src/{ProjectName}.domain/`
3. ✅ Archivo `{ProjectName}.sln` existe en la raíz

## Pasos de Construcción

### Paso 1: Crear proyecto classlib para infrastructure

```bash
dotnet new classlib -n {ProjectName}.infrastructure -o src/{ProjectName}.infrastructure
dotnet sln add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

### Paso 2: Eliminar archivo Class1.cs autogenerado

```bash
rm src/{ProjectName}.infrastructure/Class1.cs
```

### Paso 3: Agregar referencia a Domain

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

> Infrastructure implementa las interfaces definidas en Domain.

### Paso 4: Copiar templates de estructura base

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/infrastructure/README.md` → `src/{ProjectName}.infrastructure/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/infrastructure/repositories/README.md` → `src/{ProjectName}.infrastructure/repositories/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/infrastructure/persistence/README.md` → `src/{ProjectName}.infrastructure/persistence/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/infrastructure/services/README.md` → `src/{ProjectName}.infrastructure/services/README.md`

📄 COPIAR TEMPLATE: `templates/init-clean-architecture/infrastructure/configuration/README.md` → `src/{ProjectName}.infrastructure/configuration/README.md`

> El servidor MCP debe:
> 1. Descargar cada archivo desde `templates/init-clean-architecture/infrastructure/` en GitHub
> 2. Copiar a las rutas de destino indicadas (creando carpetas si no existen)
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto
>
> **Archivos copiados (5 README.md):**
> - Cada README explica el propósito de la carpeta y qué tipo de archivos debe contener

### Paso 5: Crear proyecto de tests para infrastructure

```bash
dotnet new nunit -n {ProjectName}.infrastructure.tests -o tests/{ProjectName}.infrastructure.tests
dotnet sln add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj
```

### Paso 6: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** Editar `tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### Paso 7: Instalar paquetes NuGet básicos en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package FluentAssertions
```

> Paquetes básicos para testing unitario. Se agregarán más según la tecnología elegida.

### Paso 8: Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

### Paso 9: Eliminar archivo de test autogenerado

```bash
rm tests/{ProjectName}.infrastructure.tests/UnitTest1.cs
```

## Estructura Resultante

```
src/{ProjectName}.infrastructure/
├── README.md                          # Propósito general de la capa
├── repositories/
│   └── README.md                      # Guía para implementar repositorios
├── persistence/
│   └── README.md                      # Guía para configurar ORM/DB
├── services/
│   └── README.md                      # Guía para servicios externos
└── configuration/
    └── README.md                      # Guía para DI y setup
```

## Propósito de Cada Carpeta

### repositories/

Contiene implementaciones concretas de las interfaces de repositorio definidas en Domain.

**Ejemplos futuros:**
- `NHUserRepository.cs` (si usas NHibernate)
- `EFUserRepository.cs` (si usas Entity Framework)
- `DapperUserRepository.cs` (si usas Dapper)

### persistence/

Configuración de acceso a datos y persistencia.

**Ejemplos futuros:**
- `SessionFactory.cs` (NHibernate)
- `DbContext.cs` (Entity Framework)
- `ConnectionFactory.cs` (Dapper)
- Mappers/Configuraciones de entidades

### services/

Implementaciones de servicios externos e integraciones.

**Ejemplos:**
- Clientes HTTP para APIs externas
- Servicios de email (SMTP)
- Servicios de almacenamiento (S3, Azure Blob)
- Servicios de notificaciones

### configuration/

Configuración de Dependency Injection y setup de infraestructura.

**Ejemplos futuros:**
- `InfrastructureServiceCollectionExtensions.cs`
- Configuración de Connection Strings
- Registro de repositorios y servicios

## Principios de la Capa de Infraestructura

### 1. Implementa Interfaces de Domain

Infrastructure **NO debe exponer** detalles de implementación a otras capas.

```csharp
// ✅ CORRECTO
// Domain define la interfaz
public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure la implementa (con la tecnología elegida)
public class UserRepository : IUserRepository
{
    // Implementación específica (NHibernate, EF, Dapper, etc.)
}
```

### 2. Independencia de Framework

El código de negocio (Domain y Application) **NO debe conocer** qué ORM o tecnología usa Infrastructure.

```csharp
// ❌ INCORRECTO en Application
var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);

// ✅ CORRECTO en Application
var user = await _userRepository.GetByEmailAsync(email);
```

### 3. Configuración Separada

La configuración de infraestructura debe estar aislada y ser reemplazable.

```csharp
// ✅ CORRECTO
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Registrar repositorios
        services.AddScoped<IUserRepository, UserRepository>();

        // Configurar persistencia
        services.AddDbContext<AppDbContext>();

        return services;
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
ls -R src/{ProjectName}.infrastructure
```

Deberías ver:
- `README.md` en raíz
- `repositories/README.md`
- `persistence/README.md`
- `services/README.md`
- `configuration/README.md`

### 3. Verificar referencias del proyecto

```bash
dotnet list src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference
```

Debería mostrar:
- `src/{ProjectName}.domain/{ProjectName}.domain.csproj`

### 4. Verificar que compila sin errores

```bash
dotnet build src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

> Debería compilar sin warnings ni errores.

## Próximos Pasos

Una vez completada la estructura base de infraestructura:

1. ✅ **Continuar con WebApi Layer** - [05-webapi-configuration.md](05-webapi-configuration.md)
2. ⏭️ **Implementar tecnología específica** - Ver guías en `guides/stack-implementations/`:
   - NHibernate + PostgreSQL
   - Entity Framework + SQL Server
   - Dapper + MySQL
   - MongoDB

## Notas Importantes

### Esta es una Estructura Base

Esta guía crea **solo la estructura de carpetas** con README.md explicativos. No instala paquetes NuGet ni copia templates específicos de tecnología.

**Ventajas:**
- ✅ Proyecto compila sin dependencias pesadas
- ✅ Estructura visible para entender organización
- ✅ Flexibilidad para elegir stack después
- ✅ README.md en cada carpeta como documentación

### Implementaciones Específicas

Para agregar una implementación específica (NHibernate, EF, etc.), consulta las guías en:

```
guides/stack-implementations/
├── nhibernate-postgresql/
│   ├── 01-setup-nhibernate.md
│   └── 02-configure-postgresql.md
└── entityframework-sqlserver/
    ├── 01-setup-ef-core.md
    └── 02-configure-sqlserver.md
```

### Sin Proyectos Auxiliares de Testing

Esta versión **NO crea** proyectos como `{ProjectName}.ndbunit` o `{ProjectName}.common.tests`. Estos son específicos de NHibernate y se crearán en las guías de implementación específica.

## Historial de Versiones

### v2.0.0 (2025-01-30)

**Reestructuración mayor:**
- ✅ **Versión agnóstica**: Ya NO instala paquetes NuGet específicos (NHibernate, FluentValidation, etc.)
- ✅ **Solo estructura**: Crea carpetas + README.md explicativos
- ✅ **Sin templates específicos**: No copia código de NHibernate
- ✅ **Sin proyectos auxiliares**: No crea ndbunit ni common.tests (específicos de NHibernate)
- ✅ **Documentación clara**: Cada carpeta tiene README explicando su propósito

**Rationale:**
- Clean Architecture promueve independencia de frameworks
- Permite elegir tecnología (NHibernate, EF, Dapper) después
- Estructura más limpia y enfocada
- Guías específicas de stack separadas en `guides/stack-implementations/`

**Breaking changes:**
- Ya NO crea estructura específica de NHibernate (`nhibernate/filtering/`, etc.)
- Ya NO instala FluentValidation, NHibernate, System.Linq.Dynamic.Core
- Para usar NHibernate, consultar guía en `guides/stack-implementations/nhibernate-postgresql/`

### v1.3.5 (2025-01-30)

**Versión anterior con NHibernate:**
- Instalaba NHibernate, FluentValidation, System.Linq.Dynamic.Core
- Copiaba 12 templates de NHibernate (repositorios + sistema de filtrado)
- Creaba proyectos auxiliares (ndbunit, common.tests)
- **Esta versión fue movida a:** `guides/stack-implementations/nhibernate-postgresql/01-setup-nhibernate.md`

---

> **Guía:** 04-infrastructure-layer.md
> **Milestone:** 3 - Infrastructure Layer
> **Siguiente:** [05-webapi-configuration.md](05-webapi-configuration.md)
