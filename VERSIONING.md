# Sistema de Versionado - APSYS Backend Development Guides

## Descripción General

Este repositorio utiliza **versionado semántico** (Semantic Versioning 2.0.0) para gestionar las versiones de las guías y templates.

## Formato de Versión

```
MAJOR.MINOR.PATCH

Ejemplo: 1.2.3
```

- **MAJOR**: Cambios incompatibles con versiones anteriores
- **MINOR**: Nueva funcionalidad compatible con versiones anteriores
- **PATCH**: Correcciones de bugs compatibles con versiones anteriores

## Niveles de Versionado

### 1. Versión Global del Repositorio

**Archivo:** [`guides-version.json`](guides-version.json)

```json
{
  "version": "1.0.0",
  "releaseDate": "2025-01-29",
  ...
}
```

**Uso por el servidor MCP:**

```typescript
// Leer versión desde GitHub
const versionInfo = await fetch(
  'https://raw.githubusercontent.com/.../guides-version.json'
).then(r => r.json())

console.log(`Using guides version: ${versionInfo.version}`)
```

### 2. Versión por Tool

Cada tool tiene su propia versión dentro de `guides-version.json`:

```json
{
  "guides": {
    "init-clean-architecture": {
      "version": "1.0.0",
      "milestone": 1,
      "status": "stable"
    }
  }
}
```

### 3. Versión por Documento

Cada guía individual tiene metadata de versión en su header:

```markdown
# 01 - Estructura Base del Proyecto

> **Versión:** 1.0.0 | **Última actualización:** 2025-01-29 | **Estado:** Estable
```

## Git Tags

Cada release debe tener un Git tag correspondiente:

```bash
# Crear tag para nueva versión
git tag -a v1.0.0 -m "Release 1.0.0 - Milestone 1 completado"
git push origin v1.0.0

# Listar tags
git tag -l

# Ver detalles de un tag
git show v1.0.0
```

## Uso por el Servidor MCP

### Opción 1: Usar última versión (main/master)

```typescript
const baseUrl = 'https://raw.githubusercontent.com/owner/repo/main'
```

### Opción 2: Usar versión específica (tag)

```typescript
const version = 'v1.0.0'
const baseUrl = `https://raw.githubusercontent.com/owner/repo/${version}`
```

### Opción 3: Leer versión de guides-version.json

```typescript
// 1. Leer metadata de versión
const versionUrl = `${baseUrl}/guides-version.json`
const metadata = await fetch(versionUrl).then(r => r.json())

// 2. Validar compatibilidad
if (metadata.compatibility.dotnetVersion !== currentDotnetVersion) {
  console.warn('Incompatible .NET version')
}

// 3. Reportar versión al usuario
console.log(`
APSYS Guides Information:
- Version: ${metadata.version}
- Release Date: ${metadata.releaseDate}
- .NET Compatibility: ${metadata.compatibility.dotnetVersion}
- MCP Protocol: ${metadata.compatibility.mcpProtocol}
`)

// 4. Usar guías
const guideUrl = `${baseUrl}/guides/init-clean-architecture/01-estructura-base.md`
```

## Ejemplo de Reporte de Versión

El servidor MCP debe mostrar algo como:

```
🚀 Inicializando proyecto con APSYS Clean Architecture

📚 Guías:
   Version: 1.0.0
   Release: 2025-01-29
   Tool: init-clean-architecture v1.0.0 (Milestone 1)

🔧 Compatibilidad:
   .NET: 9.0 ✓
   MCP Protocol: 1.0 ✓

📄 Ejecutando:
   ✓ 01-estructura-base.md (v1.0.0)
   ✓ 02-domain-layer.md (v1.0.0)

✅ Proyecto creado exitosamente
```

## Estados de Documentos

| Estado | Descripción | Puede usarse |
|--------|-------------|--------------|
| **stable** | Documento completo y probado | ✅ Sí |
| **beta** | Documento funcional pero en pruebas | ⚠️ Con precaución |
| **draft** | Documento en desarrollo | ❌ No |
| **pending** | Documento planificado | ❌ No |
| **deprecated** | Documento obsoleto | ❌ No (usar versión nueva) |

## Proceso de Release

### 1. Preparar el Release

```bash
# 1. Actualizar guides-version.json
# - Incrementar version
# - Actualizar releaseDate
# - Agregar entrada en changelog

# 2. Actualizar headers de guías modificadas
# - Incrementar version en header
# - Actualizar fecha

# 3. Actualizar README.md si es necesario
```

### 2. Crear Tag y Release

```bash
# 1. Commit cambios
git add .
git commit -m "Release v1.1.0: [descripción]"

# 2. Crear tag
git tag -a v1.1.0 -m "Release 1.1.0

Changelog:
- Feature 1
- Feature 2
- Bugfix 1
"

# 3. Push con tags
git push origin main
git push origin v1.1.0
```

### 3. Crear GitHub Release (opcional)

En GitHub:
1. Ir a "Releases"
2. "Draft a new release"
3. Seleccionar tag `v1.1.0`
4. Título: "Release 1.1.0"
5. Descripción: Copiar del changelog en `guides-version.json`
6. Publish release

## Compatibilidad entre Versiones

### MAJOR (x.0.0)

**Cambios que rompen compatibilidad:**
- Cambio de estructura de carpetas
- Renombrado de archivos
- Cambio de formato de placeholders
- Eliminación de pasos

**Acción:** El servidor MCP debe actualizar su parser

### MINOR (0.x.0)

**Nuevas funcionalidades compatibles:**
- Nuevas guías
- Nuevos templates
- Nuevos pasos opcionales
- Mejoras en documentación

**Acción:** El servidor MCP puede seguir usando versión anterior

### PATCH (0.0.x)

**Correcciones:**
- Fix de typos
- Corrección de comandos
- Actualización de versiones de paquetes
- Mejoras en validaciones

**Acción:** Se recomienda actualizar, pero no es crítico

## Matriz de Compatibilidad

| Guías Version | .NET Version | MCP Protocol | Estado |
|---------------|--------------|--------------|--------|
| 1.0.0 | 9.0 | 1.0 | ✅ Actual |
| 0.x.x | 8.0 | 0.9 | ❌ No soportado |

## Ejemplo de Changelog

```json
{
  "changelog": [
    {
      "version": "1.1.0",
      "date": "2025-02-15",
      "changes": [
        "Added Milestone 2: Infrastructure filtering and repositories",
        "Updated NHibernate to 5.5.3",
        "Fixed issue with Windows paths in templates"
      ],
      "breaking": false
    },
    {
      "version": "1.0.1",
      "date": "2025-02-01",
      "changes": [
        "Fixed Castle.Core dependency warning",
        "Updated documentation for clarity"
      ],
      "breaking": false
    },
    {
      "version": "1.0.0",
      "date": "2025-01-29",
      "changes": [
        "Initial release with Milestone 1",
        "Created init-clean-architecture tool",
        "Added templates system with GitHub integration"
      ],
      "breaking": false
    }
  ]
}
```

## Validación de Versión

El servidor MCP puede validar la versión antes de ejecutar:

```typescript
async function validateVersion(metadata: any) {
  // 1. Verificar versión mínima requerida
  const minVersion = '1.0.0'
  if (compareVersions(metadata.version, minVersion) < 0) {
    throw new Error(`Guides version ${metadata.version} is too old. Minimum required: ${minVersion}`)
  }

  // 2. Verificar compatibilidad de .NET
  const requiredDotNet = '9.0'
  if (metadata.compatibility.dotnetVersion !== requiredDotNet) {
    console.warn(`Warning: Guides expect .NET ${metadata.compatibility.dotnetVersion}, but you have ${requiredDotNet}`)
  }

  // 3. Verificar estado del tool
  const toolStatus = metadata.guides['init-clean-architecture'].status
  if (toolStatus !== 'stable') {
    console.warn(`Warning: Tool is in ${toolStatus} state`)
  }
}
```

## Migraciones entre Versiones

Si el servidor MCP necesita migrar entre versiones mayores:

```typescript
const migrations = {
  '0.x.x -> 1.0.0': async (project) => {
    // Migrar de estructura antigua a nueva
    console.log('Migrating project structure...')
    // ... código de migración
  }
}
```

## Recomendaciones

1. **Para desarrollo:** Usar `main` branch
2. **Para producción:** Usar tags específicos (ej: `v1.0.0`)
3. **Para testing:** Usar `develop` branch (si existe)
4. **Actualizar regularmente:** Revisar changelog para mejoras

## Contacto

Para preguntas sobre versionado:
- Abrir issue en el repositorio
- Contactar al equipo de arquitectura de APSYS

---

**Versión de este documento:** 1.4.7
**Última actualización:** 2025-01-30
