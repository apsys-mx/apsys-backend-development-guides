# Templates - APSYS Backend

Este directorio contiene los **templates de código** utilizados para generar proyectos backend con Clean Architecture.

## Estructura de Templates

```
templates/
├── Directory.Packages.props      # Gestión centralizada de paquetes NuGet
│
├── domain/                       # Templates para la capa Domain
│   ├── entities/
│   │   └── AbstractDomainObject.cs
│   ├── exceptions/
│   │   ├── InvalidDomainException.cs
│   │   ├── InvalidFilterArgumentException.cs
│   │   ├── ResourceNotFoundException.cs
│   │   └── DuplicatedDomainException.cs
│   └── interfaces/repositories/
│       ├── IRepository.cs
│       ├── IReadOnlyRepository.cs
│       ├── IUnitOfWork.cs
│       ├── GetManyAndCountResult.cs
│       ├── SortingCriteria.cs
│       └── IGetManyAndCountResultWithSorting.cs
│
├── tests/                        # Templates de tests (compartidos)
│   ├── DomainTestBase.cs
│   ├── ApplicationTestBase.cs
│   ├── EndpointTestBase.cs
│   ├── NHRepositoryTestBase.cs
│   └── CustomWebApplicationFactory.cs
│
└── webapi/                       # Templates de WebApi
    ├── Program.cs
    ├── .env
    ├── properties/
    │   └── InternalsVisibleTo.cs
    ├── features/
    │   └── hello/
    │       └── HelloEndpoint.cs
    └── infrastructure/
        └── ServiceCollectionExtender.cs
```

> **Nota:** Los templates específicos de tecnología (NHibernate, FastEndpoints, etc.) están en `stacks/`.

## Templates por Tecnología (stacks/)

Los templates específicos de cada stack tecnológico se encuentran en:

```
stacks/
├── orm/nhibernate/templates/     # NHibernate ORM
│   ├── NHRepository.cs
│   ├── NHReadOnlyRepository.cs
│   ├── NHSessionFactory.cs
│   ├── NHUnitOfWork.cs
│   └── filtering/
│
├── database/postgresql/          # PostgreSQL específico
└── webapi/fastendpoints/         # FastEndpoints específico
```

## Formato de los Templates

### Placeholders Soportados

Los templates contienen placeholders que deben reemplazarse con los valores reales del proyecto:

| Placeholder | Descripción | Ejemplo |
|------------|-------------|---------|
| `{ProjectName}` | Nombre del proyecto (lowercase) | `inventorysystem` |

**Ejemplo de uso en templates:**

```csharp
namespace {ProjectName}.domain.entities;

public abstract class AbstractDomainObject
{
    // ...
}
```

**Resultado después del reemplazo (proyecto: inventorysystem):**

```csharp
namespace inventorysystem.domain.entities;

public abstract class AbstractDomainObject
{
    // ...
}
```

## Cómo Usar estos Templates

### Opción 1: Con Agente IA

Un agente de IA puede procesar estos templates:

1. **Leer** el template desde el repositorio
2. **Reemplazar** los placeholders (`{ProjectName}`) con valores reales
3. **Escribir** el archivo procesado en el directorio destino del proyecto

### Opción 2: Manual

Para usar los templates manualmente:

1. **Copiar** el archivo template al proyecto destino
2. **Buscar y reemplazar** `{ProjectName}` con el nombre real del proyecto
3. **Compilar** para verificar que el código es válido

## Referencia en las Guías

Las guías de inicialización referencian estos templates:

| Guía | Templates Usados |
|------|------------------|
| `01-estructura-base.md` | `templates/Directory.Packages.props` |
| `02-domain-layer.md` | `templates/domain/*`, `templates/tests/DomainTestBase.cs` |
| `03-application-layer.md` | `templates/tests/ApplicationTestBase.cs` |
| `05-webapi-layer.md` | `templates/webapi/*` |

Para NHibernate:
| Guía | Templates Usados |
|------|------------------|
| `stacks/orm/nhibernate/guides/setup.md` | `stacks/orm/nhibernate/templates/*` |

## Convenciones de Código

### Namespaces

- Formato: `{ProjectName}.[capa].[subcapa]`
- Ejemplos:
  - `{ProjectName}.domain`
  - `{ProjectName}.domain.interfaces.repositories`
  - `{ProjectName}.infrastructure.nhibernate`
  - `{ProjectName}.webapi.features`

### Nombres de Archivos

- PascalCase para clases e interfaces
- Un tipo por archivo
- El nombre del archivo debe coincidir con el nombre del tipo

---

**Última actualización:** 2025-12-23
