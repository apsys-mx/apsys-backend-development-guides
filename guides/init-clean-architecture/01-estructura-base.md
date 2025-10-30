# 01 - Estructura Base del Proyecto

> **Versión:** 1.0.0 | **Última actualización:** 2025-01-29 | **Estado:** Estable

## Descripción

Este documento describe cómo crear la **estructura base** de un proyecto backend con Clean Architecture para APSYS. Esta es la primera etapa del proceso de inicialización y es **independiente de cualquier base de datos específica**.

En esta etapa se crea:
- La solución (.sln)
- Las carpetas principales (`src/` y `tests/`)
- El archivo de gestión centralizada de paquetes NuGet (`Directory.Packages.props`)

## Dependencias

**Ninguna** - Este es el primer paso del proceso.

## Requisitos Previos

- .NET SDK instalado (versión 9.0 o superior)
- Permisos de escritura en el path donde se creará el proyecto

## Parámetros de Entrada

| Parámetro   | Descripción                  | Requerido | Ejemplo                     | Default          |
| ----------- | ---------------------------- | --------- | --------------------------- | ---------------- |
| `--name`    | Nombre de la solución        | ✅ Sí     | `MiProyecto`                | -                |
| `--version` | Versión de .NET              | ✅ Sí     | `net9.0`                    | -                |
| `--path`    | Ruta donde crear el proyecto | ❌ No     | `C:\projects\miproyecto`    | `.` (directorio actual) |

**Ejemplos de uso del MCP tool:**

```bash
# Con path explícito
init-clean-architecture --name=MiProyecto --version=net9.0 --path=C:\projects\miproyecto

# Sin path (usa directorio actual)
init-clean-architecture --name=MiProyecto --version=net9.0
```

## Validaciones Pre-ejecución

Antes de ejecutar el proceso de construcción, el tool debe validar:

### 1. Validación de .NET SDK

Verificar que .NET SDK esté instalado con la versión especificada:

```bash
dotnet --version
```

**Acción:** Si no está instalado o la versión es menor, mostrar error con instrucciones de instalación.

### 2. Validación del directorio destino

**Si se especifica `--path`:**
- Crear el directorio si no existe
- Validar permisos de escritura

**Si NO se especifica `--path` (usa directorio actual):**
- Verificar que el directorio esté vacío o solo contenga archivos de Git

### 3. Validación de directorio vacío

El directorio se considera **válido (vacío)** si contiene únicamente:

**Archivos/carpetas permitidos:**
- `.git/` - Directorio de Git
- `.gitignore` - Archivo de exclusiones de Git
- `.gitattributes` - Atributos de Git
- `README.md` - Documentación inicial
- `LICENSE` - Archivo de licencia
- `.editorconfig` - Configuración de editor

**Archivos/carpetas NO permitidos (indicarían que no está vacío):**
- `*.sln` - Ya existe una solución
- `*.csproj` - Ya existen proyectos
- `src/` - Ya existe carpeta de código fuente
- `tests/` - Ya existe carpeta de tests
- `bin/`, `obj/` - Directorios de compilación
- Cualquier otro archivo o carpeta

**Acción si el directorio NO está vacío:**

Mostrar error:
```
Error: El directorio no está vacío.
Encontrado: [lista de archivos/carpetas no permitidos]

El directorio debe estar vacío o contener solo archivos de Git (.git, .gitignore, README.md, etc.)

Opciones:
1. Especifica un directorio diferente con --path
2. Limpia el directorio actual
3. Usa un directorio nuevo
```

### 4. Validación del nombre del proyecto

El `--name` debe ser un identificador C# válido:

**Reglas:**
- ✅ Empieza con letra o guion bajo
- ✅ Contiene solo letras, números, guiones bajos o puntos
- ✅ No contiene espacios
- ❌ No puede ser una palabra reservada de C#

**Ejemplos válidos:**
- `MiProyecto`
- `Mi_Proyecto`
- `MiProyecto.API`
- `Proyecto123`

**Ejemplos inválidos:**
- `Mi Proyecto` (contiene espacio)
- `123Proyecto` (empieza con número)
- `Mi-Proyecto` (contiene guion medio)
- `class` (palabra reservada)

**Acción si el nombre es inválido:**
```
Error: El nombre del proyecto no es válido.
Proporcionado: "Mi Proyecto"

El nombre debe:
- Empezar con letra o guion bajo
- Contener solo letras, números, guiones bajos o puntos
- No contener espacios ni caracteres especiales
```

## Estructura de Archivos a Crear

La estructura final en el directorio actual será:

```
./
├── {ProjectName}.sln
├── Directory.Packages.props
├── src/
└── tests/
```

> **Ejemplo:** Si tu proyecto se llama "InventorySystem", tendrás:
> ```
> ./
> ├── InventorySystem.sln
> ├── Directory.Packages.props
> ├── src/
> └── tests/
> ```

## Proceso de Construcción

> **Nota:** Los placeholders como `{ProjectName}` serán reemplazados automáticamente por el servidor MCP con el nombre real de tu proyecto.

### Paso 1: Verificar instalación de .NET

Verifica que tengas .NET SDK instalado:

```bash
dotnet --version
```

> Deberías ver la versión 9.0 o superior.

### Paso 2: Crear estructura de carpetas

```bash
mkdir src
mkdir tests
```

> Esto crea las carpetas `src/` y `tests/` en el directorio actual.

### Paso 3: Crear archivo de solución

```bash
dotnet new sln -n {ProjectName}
```

> Este comando crea el archivo `{ProjectName}.sln` en el directorio actual.
>
> **Ejemplo:** Si tu proyecto se llama "InventorySystem", se creará `InventorySystem.sln`

### Paso 4: Crear archivo Directory.Packages.props

Este archivo habilita la gestión centralizada de paquetes NuGet. Todas las versiones se definen aquí una sola vez.

**📄 COPIAR TEMPLATE:** `templates/Directory.Packages.props` → `./Directory.Packages.props`

> El servidor MCP debe:
> 1. Descargar el archivo desde `templates/Directory.Packages.props` en el repositorio de GitHub
> 2. Copiarlo a `./Directory.Packages.props` en el directorio actual
> 3. **No requiere reemplazo de placeholders** (este archivo no tiene placeholders)

**⚠️ NOTA IMPORTANTE:**
Este archivo **NO incluye** los paquetes específicos de bases de datos:
- ❌ `Npgsql` (PostgreSQL)
- ❌ `Microsoft.Data.SqlClient` (SQL Server)

Estos paquetes se agregarán posteriormente cuando se ejecute el tool `configure-database`.

## Verificación

Después de ejecutar todos los pasos, valida que la estructura se creó correctamente:

### 1. Verificar estructura de archivos

```bash
ls -la
```

Deberías ver:
- `{ProjectName}.sln`
- `Directory.Packages.props`
- Carpeta `src/`
- Carpeta `tests/`

> **Ejemplo:** Para el proyecto "InventorySystem":
> ```
> InventorySystem.sln
> Directory.Packages.props
> src/
> tests/
> ```

### 2. Compilar la solución

```bash
dotnet build
```

> Debería mostrar: "Build succeeded. 0 Warning(s). 0 Error(s)."
>
> **Nota:** Es normal que aún no haya proyectos que compilar en este punto.

## Siguientes Pasos

Una vez completada la estructura base, continuar con:
- **[02-domain-layer.md](./02-domain-layer.md)** - Creación de la capa de dominio

## Notas Adicionales

### Gestión Centralizada de Paquetes

Con `Directory.Packages.props` habilitado, cuando agregues un paquete a un proyecto (en pasos posteriores), debes hacerlo sin especificar la versión:

**❌ Incorrecto:**
```
dotnet add package FluentValidation --version 12.0.0
```

**✅ Correcto:**
```
dotnet add package FluentValidation
```

> **Nota:** Estos son ejemplos para referencia futura. NO se ejecutan en este paso.
> Los paquetes se agregan en los pasos posteriores cuando se creen los proyectos.

La versión se toma automáticamente del archivo `Directory.Packages.props`.

### Compatibilidad con IDEs

Esta estructura es compatible con:
- Visual Studio 2022+
- Visual Studio Code
- JetBrains Rider
- Línea de comandos dotnet CLI

## Troubleshooting

### Problema: "dotnet command not found"

**Solución:** Instalar .NET SDK desde https://dotnet.microsoft.com/download

### Problema: "Access denied" al crear carpetas

**Solución:** Ejecutar terminal como administrador o verificar permisos en el path especificado

### Problema: "El directorio no está vacío"

**Causa:** El directorio ya contiene archivos o carpetas que no son permitidos (algo más que archivos de Git).

**Solución:**

**Opción 1 - Limpiar directorio:**
```bash
# Eliminar archivos no deseados (ten cuidado)
# Revisar qué hay en el directorio primero
ls -la

# Eliminar solo lo necesario manualmente
```

**Opción 2 - Usar otro directorio:**
```bash
# Especifica un path diferente
init-clean-architecture --name=MiProyecto --version=net9.0 --path=C:\projects\otro-directorio
```

**Opción 3 - Crear subdirectorio:**
```bash
# Crear y usar un subdirectorio nuevo
mkdir nuevo-proyecto
cd nuevo-proyecto
init-clean-architecture --name=MiProyecto --version=net9.0
```

### Problema: El archivo .sln ya existe

**Solución:**
- Eliminar el archivo existente, o
- Cambiar el parámetro `--name` o `--path`

### Problema: Nombre de proyecto inválido

**Causa:** El `--name` contiene caracteres no permitidos o espacios.

**Solución:**
```bash
# ❌ Incorrecto
init-clean-architecture --name="Mi Proyecto" --version=net9.0

# ✅ Correcto
init-clean-architecture --name=MiProyecto --version=net9.0
# o
init-clean-architecture --name=Mi_Proyecto --version=net9.0
```
