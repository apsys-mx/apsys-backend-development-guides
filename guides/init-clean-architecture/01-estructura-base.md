# 01 - Estructura Base del Proyecto

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

```
{path}/
├── {name}.sln
├── Directory.Packages.props
├── src/
└── tests/
```

Donde:
- `{path}` = valor del parámetro `--path` (o `.` si no se especifica)
- `{name}` = valor del parámetro `--name`

## Proceso de Construcción

### Paso 0: Determinar directorio de trabajo

**Si se especificó `--path`:**
```bash
# El path es el especificado por el usuario
WORK_DIR="{path}"
```

**Si NO se especificó `--path`:**
```bash
# El path es el directorio actual
WORK_DIR="."
```

### Paso 1.1: Crear estructura de carpetas

**Si se especificó `--path`:**
```bash
mkdir "{path}"
cd "{path}"
mkdir src
mkdir tests
```

**Si NO se especificó `--path` (directorio actual):**
```bash
# Ya estamos en el directorio correcto
mkdir src
mkdir tests
```

**Ejemplo concreto con path explícito:**

```bash
mkdir "C:\projects\miproyecto"
cd "C:\projects\miproyecto"
mkdir src
mkdir tests
```

**Ejemplo concreto sin path (directorio actual):**

```bash
# Asumiendo que estamos en C:\projects\miproyecto
mkdir src
mkdir tests
```

### Paso 1.2: Crear archivo de solución

```bash
dotnet new sln -n {name} -o "{WORK_DIR}"
```

**Ejemplo concreto con path explícito:**

```bash
dotnet new sln -n MiProyecto -o "C:\projects\miproyecto"
```

**Ejemplo concreto sin path (directorio actual):**

```bash
dotnet new sln -n MiProyecto -o "."
```

**Resultado esperado:**

```
La plantilla "Archivo de solución" se creó correctamente.
```

### Paso 1.3: Crear archivo Directory.Packages.props

Crear el archivo `Directory.Packages.props` en la raíz de la solución con el siguiente contenido:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="AutoFixture.AutoMoq" Version="4.18.1" />
    <PackageVersion Include="AutoMapper" Version="15.0.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="DotNetEnv" Version="3.1.1" />
    <PackageVersion Include="FastEndpoints" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Security" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Swagger" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Testing" Version="7.0.1" />
    <PackageVersion Include="FluentAssertions" Version="8.5.0" />
    <PackageVersion Include="FluentMigrator" Version="7.1.0" />
    <PackageVersion Include="FluentMigrator.Runner" Version="7.1.0" />
    <PackageVersion Include="FluentValidation" Version="12.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.7" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="9.0.5" />
    <PackageVersion Include="Microsoft.AspNetCore.WebUtilities" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Log4Net.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="NHibernate" Version="5.5.2" />
    <PackageVersion Include="NUnit" Version="4.2.2" />
    <PackageVersion Include="NUnit.Analyzers" Version="4.4.0" />
    <PackageVersion Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageVersion Include="Scrutor" Version="6.1.0" />
    <PackageVersion Include="Spectre.Console" Version="0.50.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="9.0.3" />
    <PackageVersion Include="System.Linq.Dynamic.Core" Version="1.6.7" />
  </ItemGroup>
</Project>
```

**⚠️ NOTA IMPORTANTE:**
Este archivo **NO incluye** los paquetes específicos de bases de datos:
- ❌ `Npgsql` (PostgreSQL)
- ❌ `Microsoft.Data.SqlClient` (SQL Server)

Estos paquetes se agregarán posteriormente cuando se ejecute el tool `configure-database`.

**Propósito:**
Este archivo habilita la **gestión centralizada de paquetes NuGet**. Todas las versiones de paquetes se definen aquí una sola vez, y los proyectos individuales solo referencian el nombre del paquete sin especificar versión, evitando conflictos y facilitando el mantenimiento.

**Ubicación:** `{path}/Directory.Packages.props`

## Validación

Después de ejecutar estos pasos, verifica que se haya creado la siguiente estructura:

```bash
ls -la "{path}"
```

**Resultado esperado:**

```
Directory.Packages.props
{name}.sln
src/
tests/
```

**Verificar contenido del archivo .sln:**

```bash
cat "{path}/{name}.sln"
```

Debe contener algo similar a:

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
EndGlobal
```

**Verificar que Directory.Packages.props existe y es válido XML:**

```bash
cat "{path}/Directory.Packages.props" | head -n 5
```

Debe mostrar:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
```

## Siguientes Pasos

Una vez completada la estructura base, continuar con:
- **[02-domain-layer.md](./02-domain-layer.md)** - Creación de la capa de dominio

## Notas Adicionales

### Gestión Centralizada de Paquetes

Con `Directory.Packages.props` habilitado, cuando agregues un paquete a un proyecto, debes hacerlo sin especificar la versión:

**❌ Incorrecto:**
```bash
dotnet add package FluentValidation --version 12.0.0
```

**✅ Correcto:**
```bash
dotnet add package FluentValidation
```

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
