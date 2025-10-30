# Templates - APSYS Backend

Este directorio contiene los **templates de código** que el servidor MCP utiliza para generar proyectos backend con Clean Architecture.

## Estructura de Templates

```
templates/
├── Directory.Packages.props          # Gestión centralizada de paquetes NuGet
│
├── domain/                            # Templates para la capa Domain
│   ├── entities/
│   │   └── AbstractDomainObject.cs
│   ├── exceptions/
│   │   ├── InvalidDomainException.cs
│   │   └── InvalidFilterArgumentException.cs
│   └── interfaces/
│       └── repositories/
│           ├── IRepository.cs
│           ├── IReadOnlyRepository.cs
│           ├── IUnitOfWork.cs
│           ├── GetManyAndCountResult.cs
│           ├── SortingCriteria.cs
│           └── IGetManyAndCountResultWithSorting.cs
│
└── domain.tests/                      # Templates para tests del Domain
    └── entities/
        └── DomainTestBase.cs
```

## Formato de los Templates

### Placeholders Soportados

Los templates pueden contener los siguientes placeholders que el servidor MCP debe reemplazar:

| Placeholder | Descripción | Ejemplo |
|------------|-------------|---------|
| `{ProjectName}` | Nombre del proyecto (PascalCase) | `InventorySystem` |
| `{projectName}` | Nombre del proyecto (lowercase) | `inventorysystem` |
| `{PROJECT_NAME}` | Nombre del proyecto (UPPERCASE) | `INVENTORYSYSTEM` |

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

## Cómo Usa el Servidor MCP estos Templates

### 1. Lectura desde GitHub

El servidor MCP lee los templates directamente desde el repositorio de GitHub:

```
https://raw.githubusercontent.com/[owner]/apsys-backend-development-guides/[branch]/templates/[path-to-file]
```

### 2. Procesamiento

Para cada template:

1. **Descargar** el archivo desde GitHub
2. **Reemplazar** los placeholders con los valores reales del proyecto
3. **Escribir** el archivo procesado en el directorio destino del proyecto

### 3. Instrucciones en las Guías

Las guías usan dos formatos para indicar operaciones con templates:

#### Formato A: Copiar archivo individual

```markdown
**📄 COPIAR TEMPLATE:** `templates/Directory.Packages.props` → `./Directory.Packages.props`
```

**Acción del servidor MCP:**
- Descargar `templates/Directory.Packages.props`
- Copiar a `./Directory.Packages.props`
- No reemplazar placeholders (si se indica)

#### Formato B: Copiar directorio completo

```markdown
**📁 COPIAR DIRECTORIO COMPLETO:** `templates/domain/` → `src/{ProjectName}.domain/`
```

**Acción del servidor MCP:**
- Descargar todos los archivos de `templates/domain/` recursivamente
- Copiar a `src/{ProjectName}.domain/` respetando estructura
- Reemplazar `{ProjectName}` en todos los archivos

## Validación de Templates

Todos los templates deben ser código C# válido que compile correctamente después del reemplazo de placeholders.

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

# Copiar y reemplazar
cp -r templates/domain/* .
find . -type f -exec sed -i 's/{ProjectName}/TestProject/g' {} +

# Compilar
dotnet build
```

## Convenciones de Código

Los templates siguen las siguientes convenciones:

### Namespaces

- Siempre usar formato: `{ProjectName}.[capa].[subcapa]`
- Ejemplos:
  - `{ProjectName}.domain.entities`
  - `{ProjectName}.domain.exceptions`
  - `{ProjectName}.domain.interfaces.repositories`

### Nombres de Archivos

- PascalCase para clases e interfaces
- Un tipo por archivo
- El nombre del archivo debe coincidir con el nombre del tipo

### Documentación

- Cada clase/interfaz debe tener comentarios XML (si es relevante)
- Los templates NO incluyen comentarios de ejemplo, solo código limpio

## Agregar Nuevos Templates

Para agregar un nuevo template:

1. **Crear el archivo** en la estructura correcta de `templates/`
2. **Usar placeholders** donde corresponda (`{ProjectName}`)
3. **Validar sintaxis** con un reemplazo manual
4. **Actualizar este README** si es necesario
5. **Actualizar las guías** en `guides/` para referenciar el nuevo template

**Ejemplo - Agregar un nuevo Repository:**

```bash
# 1. Crear archivo
touch templates/domain/interfaces/repositories/IAuditableRepository.cs

# 2. Contenido con placeholder
cat > templates/domain/interfaces/repositories/IAuditableRepository.cs << 'EOF'
namespace {ProjectName}.domain.interfaces.repositories;

public interface IAuditableRepository<T> : IRepository<T, Guid> where T : class
{
    void AuditChanges(T entity);
}
EOF

# 3. Actualizar guía correspondiente
# Editar: guides/init-clean-architecture/02-domain-layer.md
```

## Versionado

Los templates están versionados junto con el repositorio. El servidor MCP puede apuntar a:

- **main/master**: Última versión estable
- **develop**: Versión en desarrollo
- **Tags**: Versiones específicas (ej: `v1.0.0`)

**Ejemplo de uso con versión específica:**

```
https://raw.githubusercontent.com/[owner]/apsys-backend-development-guides/v1.0.0/templates/domain/entities/AbstractDomainObject.cs
```

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

**Causa:** Se usó el placeholder en un lugar donde no debía reemplazarse.

**Solución:** Si hay texto que literalmente debe ser `{ProjectName}`, escaparlo o usar otro formato.

## Contacto

Para problemas o sugerencias sobre los templates:
- Abre un issue en el repositorio
- Contacta al equipo de arquitectura de APSYS

---

**Última actualización:** 2025-01-29
**Versión:** 1.0.0
