# 04 - Capa de Infraestructura (Infrastructure Layer)

> **Versión:** 1.3.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo crear la **capa de infraestructura (Infrastructure Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta capa contiene:

- **Repositorios NHibernate**: Implementaciones concretas de IRepository e IReadOnlyRepository
- **Unit of Work**: Gestión de transacciones y coordinación de repositorios
- **Sistema de filtrado**: Parsing y construcción de queries dinámicas desde query strings
- **Mappers**: Configuración de ORM con ClassMapping de NHibernate
- **Session Factory**: Configuración centralizada de NHibernate

## Dependencias

- ✅ **Requiere:** [01-estructura-base.md](01-estructura-base.md) completado
- ✅ **Requiere:** [02-domain-layer.md](02-domain-layer.md) completado
- ⚠️ **Recomendado:** [03-application-layer.md](03-application-layer.md) completado (para entender qué necesita Application)

## Validaciones Previas

Antes de ejecutar los comandos, verifica:

1. ✅ SDK de .NET 9.0 instalado: `dotnet --version`
2. ✅ Proyecto Domain existe: verificar `src/{ProjectName}.domain/`
3. ✅ Archivo `{ProjectName}.sln` existe en la raíz
4. ✅ Proyectos de testing auxiliares existen (se crearán si no existen)

## Pasos de Construcción

### Paso 1: Crear proyectos auxiliares de testing (si no existen)

Infrastructure depende de proyectos auxiliares de testing. Si aún no existen, créalos:

**Crear proyecto ndbunit:**
```bash
mkdir tests/{ProjectName}.ndbunit
dotnet new classlib -n {ProjectName}.ndbunit -o tests/{ProjectName}.ndbunit
dotnet sln add tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj
rm tests/{ProjectName}.ndbunit/Class1.cs
```

**Crear proyecto common.tests:**
```bash
mkdir tests/{ProjectName}.common.tests
dotnet new classlib -n {ProjectName}.common.tests -o tests/{ProjectName}.common.tests
dotnet sln add tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj
rm tests/{ProjectName}.common.tests/Class1.cs
```

> **Nota:** Estos proyectos contienen utilidades compartidas para tests. Se documentarán en detalle en guías futuras.

### Paso 2: Crear proyecto classlib para infrastructure

```bash
mkdir src/{ProjectName}.infrastructure
dotnet new classlib -n {ProjectName}.infrastructure -o src/{ProjectName}.infrastructure
dotnet sln add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
```

### Paso 3: Eliminar archivo Class1.cs autogenerado

```bash
rm src/{ProjectName}.infrastructure/Class1.cs
```

### Paso 4: Instalar paquetes NuGet en infrastructure

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package FluentValidation
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package NHibernate
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package System.Linq.Dynamic.Core
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package Microsoft.AspNetCore.WebUtilities
```

> - **FluentValidation**: Para validar entidades antes de persistirlas
> - **NHibernate**: ORM para mapeo objeto-relacional
> - **System.Linq.Dynamic.Core**: Para queries dinámicas desde strings
> - **Microsoft.AspNetCore.WebUtilities**: Para parsing de query strings

### Paso 5: Agregar referencias de proyectos

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj
```

> Infrastructure implementa las interfaces de Domain y puede usar utilidades de testing para datos de prueba.

### Paso 6: Crear estructura de carpetas de infrastructure

```bash
mkdir src/{ProjectName}.infrastructure/nhibernate
mkdir src/{ProjectName}.infrastructure/nhibernate/filtering
mkdir src/{ProjectName}.infrastructure/nhibernate/mappers
mkdir src/{ProjectName}.infrastructure/nhibernate/repositories
```

### Paso 7: Copiar templates del sistema de filtrado

**📁 COPIAR DIRECTORIO COMPLETO:** `templates/infrastructure/nhibernate/filtering/` → `src/{ProjectName}.infrastructure/nhibernate/filtering/`

> El servidor MCP debe:
> 1. Descargar todos los archivos desde `templates/infrastructure/nhibernate/filtering/` en GitHub
> 2. Copiarlos a `src/{ProjectName}.infrastructure/nhibernate/filtering/` respetando estructura
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto

**Archivos del sistema de filtrado (9 archivos):**
- `FilterExpressionParser.cs` - Construye expresiones LINQ desde filtros
- `FilterOperator.cs` - Modelo de operador de filtro
- `InvalidQueryStringArgumentException.cs` - Excepción para query strings inválidos
- `QueryOperations.cs` - Operaciones de query disponibles
- `QueryStringParser.cs` - Parser principal de query strings
- `QuickSearch.cs` - Modelo para búsqueda rápida
- `RelationalOperator.cs` - Enum de operadores relacionales (Equals, Contains, GreaterThan, etc.)
- `Sorting.cs` - Modelo de ordenamiento
- `StringExtender.cs` - Extensiones de string para pascalCase/camelCase

### Paso 8: Copiar templates de repositorios base

**📁 COPIAR ARCHIVOS:** `templates/infrastructure/nhibernate/` → `src/{ProjectName}.infrastructure/nhibernate/`

> Copiar archivos individuales (NO directorio completo, solo archivos raíz):

**Archivos core de NHibernate (4 archivos):**
- `NHReadOnlyRepository.cs` - Repositorio base de solo lectura con GetManyAndCount
- `NHRepository.cs` - Repositorio base CRUD con validación FluentValidation
- `NHUnitOfWork.cs` - Unit of Work (template vacío - requiere configuración)
- `SortingCriteriaExtender.cs` - Extensiones para convertir SortingCriteria a expresiones

> **Reemplazar** `{ProjectName}` en todos los archivos.

### Paso 9: Crear proyecto de tests para infrastructure

```bash
mkdir tests/{ProjectName}.infrastructure.tests
dotnet new nunit -n {ProjectName}.infrastructure.tests -o tests/{ProjectName}.infrastructure.tests
dotnet sln add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj
```

### Paso 10: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** Editar `tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### Paso 11: Instalar paquetes NuGet adicionales en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package Castle.Core
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj package NHibernate
```

> Se agrega NHibernate para poder hacer tests de integración con bases de datos.

### Paso 12: Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj
dotnet add tests/{ProjectName}.infrastructure.tests/{ProjectName}.infrastructure.tests.csproj reference tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj
```

### Paso 13: Crear estructura de carpetas de tests

```bash
mkdir tests/{ProjectName}.infrastructure.tests/nhibernate
mkdir tests/{ProjectName}.infrastructure.tests/nhibernate/repositories
```

### Paso 14: Eliminar archivo de test autogenerado

```bash
rm tests/{ProjectName}.infrastructure.tests/UnitTest1.cs
```

## Referencia de Templates

### Sistema de Filtrado (filtering/)

| Archivo | Propósito |
|---------|-----------|
| **FilterExpressionParser.cs** | Parser que convierte FilterOperators en expresiones LINQ para consultas dinámicas. Soporta operadores relacionales (Contains, GreaterThan, Between, etc.) |
| **FilterOperator.cs** | Modelo que representa un operador de filtro con nombre de campo, valores y tipo de operador relacional |
| **InvalidQueryStringArgumentException.cs** | Excepción lanzada cuando un query string contiene argumentos inválidos o mal formados |
| **QueryOperations.cs** | Enum y utilidades para operaciones de query disponibles en el sistema de filtrado |
| **QueryStringParser.cs** | Parser principal que extrae paginación (pageNumber, pageSize), ordenamiento (sortBy, sortDirection) y filtros desde query strings HTTP |
| **QuickSearch.cs** | Modelo para búsqueda rápida multi-columna con query general |
| **RelationalOperator.cs** | Enum de operadores relacionales: Equals, Contains, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Between |
| **Sorting.cs** | Modelo simple con propiedades By (campo) y Direction (asc/desc) |
| **StringExtender.cs** | Extensiones para convertir strings entre camelCase, PascalCase y otras transformaciones útiles para parsear properties |

### Repositorios Base NHibernate

| Archivo | Propósito |
|---------|-----------|
| **NHReadOnlyRepository.cs** | Implementación de IReadOnlyRepository con NHibernate. Incluye Get(), Count(), GetManyAndCount() con soporte completo para filtrado y paginación usando el sistema de filtering |
| **NHRepository.cs** | Implementación de IRepository que extiende NHReadOnlyRepository. Agrega Add(), Save(), Delete() con validación automática usando FluentValidation antes de persistir |
| **NHUnitOfWork.cs** | Template de Unit of Work con gestión de transacciones. **Requiere configuración manual** para agregar propiedades de repositorios específicos del proyecto |
| **SortingCriteriaExtender.cs** | Extensión que convierte SortingCriteria del domain en expresiones string para System.Linq.Dynamic.Core ("PropertyName" o "PropertyName descending") |

## Implementación de Repositorios Específicos

Los templates proporcionan clases BASE genéricas. Cada proyecto debe crear repositorios ESPECÍFICOS para sus entidades.

### Ejemplo: Repositorio de Usuario

**Crear archivo:** `src/{ProjectName}.infrastructure/nhibernate/repositories/NHUserRepository.cs`

```csharp
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;
using NHibernate;

namespace {ProjectName}.infrastructure.nhibernate.repositories;

/// <summary>
/// NHibernate implementation of the user repository
/// </summary>
public class NHUserRepository : NHRepository<User, int>, IUserRepository
{
    public NHUserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider)
    {
    }

    // Métodos específicos de IUserRepository si los hay
    // Por ejemplo: GetUserByEmail, etc.
}
```

### Ejemplo: Repositorio de Solo Lectura

**Crear archivo:** `src/{ProjectName}.infrastructure/nhibernate/repositories/NHUserDaoRepository.cs`

```csharp
using {ProjectName}.domain.daos;
using {ProjectName}.domain.interfaces.repositories;
using NHibernate;

namespace {ProjectName}.infrastructure.nhibernate.repositories;

/// <summary>
/// Read-only repository for User DAOs
/// </summary>
public class NHUserDaoRepository : NHReadOnlyRepository<UserDao, int>, IUserDaoRepository
{
    public NHUserDaoRepository(ISession session)
        : base(session)
    {
    }
}
```

> **Nota:** Los repositorios de solo lectura (DAOs) NO requieren ServiceProvider porque no validan.

### Configurar NHUnitOfWork

Editar `src/{ProjectName}.infrastructure/nhibernate/NHUnitOfWork.cs` y agregar propiedades de repositorios:

```csharp
#region crud Repositories
public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
#endregion

#region read-only Repositories
public IUserDaoRepository UserDaos => new NHUserDaoRepository(_session);
public IRoleDaoRepository RoleDaos => new NHRoleDaoRepository(_session);
#endregion
```

## Mappers de NHibernate

Cada entidad de dominio necesita un mapper para configurar el mapeo ORM.

### Ejemplo: Mapper de Usuario

**Crear archivo:** `src/{ProjectName}.infrastructure/nhibernate/mappers/UserMapper.cs`

```csharp
using {ProjectName}.domain.entities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace {ProjectName}.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping for User entity
/// </summary>
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Table("users");
        Schema("public");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.Identity);
        });

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.NotNullable(true);
            m.Length(100);
        });

        Property(x => x.Email, m =>
        {
            m.Column("email");
            m.NotNullable(true);
            m.Length(255);
            m.Unique(true);
        });

        Property(x => x.CreationDate, m =>
        {
            m.Column("creation_date");
            m.NotNullable(true);
        });
    }
}
```

> Los mappers se autodescubren si están en el mismo assembly que NHibernate Configuration.

## Principios de la Capa de Infraestructura

### 1. Implementa Interfaces de Domain

Infrastructure implementa las interfaces definidas en Domain:

```csharp
// ✅ CORRECTO
public class NHUserRepository : NHRepository<User, int>, IUserRepository
{
    // Implementa IUserRepository del domain
}
```

### 2. Validación Automática

Los repositorios CRUD validan automáticamente con FluentValidation:

```csharp
public T Add(T item)
{
    var validationResult = this.validator.Validate(item);
    if (!validationResult.IsValid)
        throw new InvalidDomainException(validationResult.Errors);

    this._session.Save(item);
    this.FlushWhenNotActiveTransaction();
    return item;
}
```

### 3. Sistema de Filtrado Dinámico

El sistema de filtering permite construir queries complejas desde query strings HTTP:

```
GET /api/users?pageNumber=1&pageSize=25&sortBy=Name&sortDirection=asc&Email__contains=john&Age__gte=18
```

Se convierte en:
```csharp
var result = repository.GetManyAndCount(queryString, "Name");
// Filtra: Email.Contains("john") AND Age >= 18
// Ordena: Name ascending
// Pagina: página 1, 25 items
```

### 4. Gestión de Transacciones

NHUnitOfWork coordina transacciones:

```csharp
using (var uow = new NHUnitOfWork(session, serviceProvider))
{
    uow.BeginTransaction();
    try
    {
        uow.Users.Add(newUser);
        uow.Roles.Add(newRole);
        uow.Commit();
    }
    catch
    {
        uow.Rollback();
        throw;
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
- `nhibernate/filtering/` con 9 archivos del sistema de filtrado
- `nhibernate/NHReadOnlyRepository.cs`
- `nhibernate/NHRepository.cs`
- `nhibernate/NHUnitOfWork.cs`
- `nhibernate/SortingCriteriaExtender.cs`
- `nhibernate/mappers/` (vacío - se crean por proyecto)
- `nhibernate/repositories/` (vacío - se crean por proyecto)

### 3. Verificar referencias del proyecto

```bash
dotnet list src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj reference
```

Debería mostrar:
- `src/{ProjectName}.domain/{ProjectName}.domain.csproj`
- `tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj`
- `tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj`

### 4. Verificar paquetes instalados

```bash
dotnet list src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package
```

Debería incluir:
- `FluentValidation`
- `NHibernate`
- `System.Linq.Dynamic.Core`
- `Microsoft.AspNetCore.WebUtilities`

## Próximos Pasos

Una vez completada la capa de infraestructura:

1. **WebApi Layer** - Configurar FastEndpoints, DI, SessionFactory de NHibernate
2. **Migrations** - Crear esquema de base de datos con FluentMigrator
3. **Testing Support** - Configurar proyectos de testing auxiliares completamente

## Notas Importantes

### Repositorios Específicos vs Genéricos

- **NHRepository/NHReadOnlyRepository**: Clases BASE genéricas (templates)
- **NHUserRepository/NHRoleRepository**: Implementaciones ESPECÍFICAS (creadas por proyecto)

### El Sistema de Filtrado es Reutilizable

El sistema de filtrado en `filtering/` es 100% genérico y reutilizable entre proyectos. No requiere modificación.

### Mappers son Específicos

Cada proyecto define sus propios mappers según su modelo de base de datos. NO hay templates de mappers.

### NHUnitOfWork Requiere Configuración

El template de NHUnitOfWork viene VACÍO en las secciones de repositorios. Debes agregar manualmente las propiedades para tus repositorios específicos.

---

> **Guía:** 04-infrastructure-layer.md
> **Milestone:** 3 - Infrastructure Layer
> **Siguiente:** 05-webapi-configuration.md
