# Templates - APSYS Backend

Este directorio contiene los **templates de código** utilizados para generar proyectos backend con Clean Architecture.

## Estructura de Templates

```
templates/
├── Directory.Packages.props          # Gestión centralizada de paquetes NuGet
│
├── domain/                            # Templates para la capa Domain
│   ├── IReadOnlyRepository.cs
│   ├── IRepository.cs
│   ├── IUnitOfWork.cs
│   ├── GetManyAndCountResult.cs
│   ├── SortingCriteria.cs
│   └── IGetManyAndCountResultWithSorting.cs
│
├── domain.tests/                      # Templates para tests del Domain
│   └── DomainTestBase.cs
│
├── application.tests/                 # Templates para tests de Application
│   └── ApplicationTestBase.cs
│
├── infrastructure/nhibernate/         # Templates de repositorios NHibernate
│   ├── NHReadOnlyRepository.cs
│   ├── NHRepository.cs
│   ├── NHUnitOfWork.cs
│   ├── SortingCriteriaExtender.cs
│   └── filtering/                     # Sistema de filtering (8 archivos)
│       ├── FilterExpressionParser.cs
│       ├── FilterOperator.cs
│       ├── InvalidQueryStringArgumentException.cs
│       ├── QueryStringParser.cs
│       ├── QuickSearch.cs
│       ├── RelationalOperator.cs
│       ├── Sorting.cs
│       └── StringExtender.cs
│
└── webapi/                            # Templates de WebApi
    ├── Program.cs
    ├── IPrincipalExtender.cs
    ├── infrastructure/
    │   ├── ServiceCollectionExtender.cs
    │   └── authorization/
    │       └── MustBeApplicationUser.cs
    ├── features/
    │   ├── BaseEndpoint.cs
    │   └── hello/
    │       └── HelloEndpoint.cs
    ├── dtos/
    │   └── GetManyAndCountResultDto.cs
    ├── mappingprofiles/
    │   └── MappingProfile.cs
    └── Properties/
        └── InternalsVisibleTo.cs
```

## Formato de los Templates

### Placeholders Soportados

Los templates contienen placeholders que deben reemplazarse con los valores reales del proyecto:

| Placeholder | Descripción | Ejemplo |
|------------|-------------|---------|
| `{ProjectName}` | Nombre del proyecto (PascalCase) | `InventorySystem` |

**Ejemplo de uso en templates:**

```csharp
namespace {ProjectName}.domain.entities;

public abstract class AbstractDomainObject
{
    // ...
}
```

**Resultado después del reemplazo (proyecto: InventorySystem):**

```csharp
namespace InventorySystem.domain.entities;

public abstract class AbstractDomainObject
{
    // ...
}
```

## Cómo Usar estos Templates

### Opción 1: Automatizado (con agente IA)

Un agente de IA o herramienta de automatización puede procesar estos templates:

1. **Leer** el template desde el repositorio
2. **Reemplazar** los placeholders (`{ProjectName}`) con valores reales
3. **Escribir** el archivo procesado en el directorio destino del proyecto

### Opción 2: Manual

Para usar los templates manualmente:

1. **Copiar** el archivo template al proyecto destino
2. **Buscar y reemplazar** `{ProjectName}` con el nombre real del proyecto
3. **Compilar** para verificar que el código es válido

### Instrucciones en las Guías

Las guías usan dos formatos para indicar operaciones con templates:

#### Formato A: Copiar archivo individual

```markdown
📄 COPIAR TEMPLATE: `templates/Directory.Packages.props` → `./Directory.Packages.props`
```

**Acción requerida:**
- Descargar/copiar `templates/Directory.Packages.props`
- Escribir en `./Directory.Packages.props`
- Reemplazar `{ProjectName}` si contiene el placeholder

#### Formato B: Copiar directorio completo

```markdown
📁 COPIAR DIRECTORIO COMPLETO: `templates/domain/` → `src/{ProjectName}.domain/`
```

**Acción requerida:**
- Descargar/copiar todos los archivos de `templates/domain/` recursivamente
- Escribir en `src/{ProjectName}.domain/` respetando estructura de subdirectorios
- Reemplazar `{ProjectName}` en todos los archivos y rutas

## Validación de Templates

Todos los templates son código C# válido que compila correctamente después del reemplazo de placeholders.

### Prueba Local

Puedes probar localmente los templates:

1. Crear un proyecto de prueba
2. Copiar los templates
3. Reemplazar manualmente `{ProjectName}` con un nombre real
4. Ejecutar `dotnet build`

**Ejemplo:**

```bash
# Crear proyecto de prueba
mkdir test-templates
cd test-templates
dotnet new sln -n TestProject
dotnet new classlib -n TestProject.domain -o src/TestProject.domain
dotnet sln add src/TestProject.domain

# Copiar y reemplazar (Linux/Mac)
cp templates/domain/*.cs src/TestProject.domain/
find src/TestProject.domain -type f -exec sed -i 's/{ProjectName}/TestProject/g' {} +

# Copiar y reemplazar (Windows PowerShell)
Copy-Item templates\domain\*.cs src\TestProject.domain\
Get-ChildItem src\TestProject.domain\*.cs -Recurse | ForEach-Object {
    (Get-Content $_. FullName) -replace '{ProjectName}', 'TestProject' | Set-Content $_.FullName
}

# Compilar
dotnet build
```

## Convenciones de Código

Los templates siguen las siguientes convenciones:

### Namespaces

- Siempre usar formato: `{ProjectName}.[capa].[subcapa]`
- Ejemplos:
  - `{ProjectName}.domain`
  - `{ProjectName}.domain.interfaces.repositories`
  - `{ProjectName}.infrastructure.nhibernate`
  - `{ProjectName}.webapi.features`

### Nombres de Archivos

- PascalCase para clases e interfaces
- Un tipo por archivo
- El nombre del archivo debe coincidir con el nombre del tipo

### Documentación

- Los templates incluyen comentarios XML para clases e interfaces públicas
- Documentación clara de parámetros y valores de retorno
- Ejemplos de uso cuando es relevante

## Agregar Nuevos Templates

Para agregar un nuevo template:

1. **Crear el archivo** en la estructura correcta de `templates/`
2. **Usar placeholders** donde corresponda (`{ProjectName}`)
3. **Validar sintaxis** con un reemplazo manual y compilación
4. **Actualizar este README** si se agrega un nuevo directorio o categoría
5. **Actualizar las guías** en `guides/` para referenciar el nuevo template

**Ejemplo - Agregar un nuevo filtro:**

```bash
# 1. Crear archivo
touch templates/infrastructure/nhibernate/filtering/AdvancedFilter.cs

# 2. Contenido con placeholder
cat > templates/infrastructure/nhibernate/filtering/AdvancedFilter.cs << 'EOF'
namespace {ProjectName}.infrastructure.nhibernate.filtering;

/// <summary>
/// Advanced filtering functionality
/// </summary>
public class AdvancedFilter
{
    // Implementation
}
EOF

# 3. Probar compilación manualmente

# 4. Actualizar guía correspondiente
# Editar: guides/init-clean-architecture/04-infrastructure-layer.md
```

## Versionado

Los templates están versionados junto con el repositorio usando Git tags.

**Estructura de versiones:**

- **main/master**: Última versión estable
- **Tags (v1.x.x)**: Versiones específicas

**Acceso a versiones específicas (GitHub):**

```
# Última versión (main branch)
https://raw.githubusercontent.com/[owner]/apsys-backend-development-guides/main/templates/domain/IRepository.cs

# Versión específica (tag)
https://raw.githubusercontent.com/[owner]/apsys-backend-development-guides/v1.4.7/templates/domain/IRepository.cs
```

## Inventario Completo de Templates

### Directory.Packages.props (v1.0.1)
Gestión centralizada de paquetes NuGet para toda la solución.

### Domain Layer (v1.1.0)
- **IReadOnlyRepository.cs** - Interfaz para repositorios de solo lectura con GetManyAndCount
- **IRepository.cs** - Interfaz para repositorios CRUD
- **IUnitOfWork.cs** - Interfaz para Unit of Work pattern
- **GetManyAndCountResult.cs** - Resultado paginado con metadatos
- **SortingCriteria.cs** - Criterios de ordenamiento
- **IGetManyAndCountResultWithSorting.cs** - Interfaz para resultados con sorting

### Domain Tests (v1.1.1)
- **DomainTestBase.cs** - Clase base para tests con AutoFixture

### Application Tests (v1.2.0)
- **ApplicationTestBase.cs** - Clase base para tests con AutoFixture + AutoMoq

### Infrastructure Layer (v1.3.5)
- **NHReadOnlyRepository.cs** - Repositorio base de solo lectura con NHibernate
- **NHRepository.cs** - Repositorio base CRUD con validación FluentValidation
- **NHUnitOfWork.cs** - Unit of Work template (requiere configuración manual)
- **SortingCriteriaExtender.cs** - Extensiones para convertir sorting criteria

### Filtering System (v1.3.5)
- **FilterExpressionParser.cs** - Construye expresiones LINQ desde filtros
- **FilterOperator.cs** - Modelo de operador de filtro
- **InvalidQueryStringArgumentException.cs** - Excepción para query strings inválidos
- **QueryStringParser.cs** - Parser principal de query strings
- **QuickSearch.cs** - Modelo para búsqueda rápida
- **RelationalOperator.cs** - Enum de operadores relacionales
- **Sorting.cs** - Modelo de ordenamiento
- **StringExtender.cs** - Extensiones de string para conversión de casos

### WebApi Layer (v1.4.5)
- **Program.cs** - Configuración principal de la aplicación
- **IPrincipalExtender.cs** - Extensiones para obtener claims del usuario
- **ServiceCollectionExtender.cs** - Métodos de extensión para DI
- **MustBeApplicationUser.cs** - Handler de autorización personalizada
- **BaseEndpoint.cs** - Clase base para endpoints con helpers
- **HelloEndpoint.cs** - Endpoint de ejemplo (GET /hello)
- **GetManyAndCountResultDto.cs** - DTO genérico para resultados paginados
- **MappingProfile.cs** - Perfil de AutoMapper con mapeo genérico
- **InternalsVisibleTo.cs** - Configuración de visibilidad para tests

## Troubleshooting

### Problema: Template no se encuentra

**Causa:** Ruta incorrecta o archivo no existe en el repositorio.

**Solución:** Verificar que la ruta en la guía coincida con la estructura real en `templates/`.

### Problema: Error de compilación después de reemplazo

**Causa:** Placeholder no reemplazado correctamente o código inválido.

**Solución:**
1. Verificar que todos los `{ProjectName}` fueron reemplazados
2. Compilar localmente para detectar errores de sintaxis
3. Corregir el template y hacer commit

### Problema: Placeholder en lugar incorrecto

**Causa:** Se usó el placeholder en un lugar donde no debía reemplazarse (ej: en un comentario o string literal).

**Solución:** Escapar o usar formato alternativo si el texto debe ser literalmente `{ProjectName}`.

## Referencias

- **Guías de uso:** [guides/init-clean-architecture/README.md](../guides/init-clean-architecture/README.md)
- **Repositorio principal:** [README.md](../README.md)
- **Versionado:** [guides-version.json](../guides-version.json)

## Contacto

Para problemas o sugerencias sobre los templates:
- Abre un issue en el repositorio
- Contacta al equipo de arquitectura de APSYS

---

**Última actualización:** 2025-01-30
**Versión:** 1.4.8
