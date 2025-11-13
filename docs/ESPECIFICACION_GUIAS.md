# Especificación de Guías Markdown para MCP Server

> **Versión:** 1.0.0
> **Fecha:** 2025-01-30
> **Propósito:** Definir el formato estándar para guías markdown procesables por el MCP Server

## 📋 Tabla de Contenidos

1. [Estructura General](#estructura-general)
2. [Metadatos de Versión](#metadatos-de-versión)
3. [Instrucciones de Templates](#instrucciones-de-templates)
4. [Comandos de Terminal](#comandos-de-terminal)
5. [Edición de Archivos](#edición-de-archivos)
6. [Placeholders](#placeholders)
7. [Reglas de Formateo](#reglas-de-formateo)
8. [Ejemplos Completos](#ejemplos-completos)
9. [Errores Comunes](#errores-comunes)

---

## 1. Estructura General

Cada guía debe seguir esta estructura:

```markdown
# Título de la Guía

**Versión:** X.Y.Z

## Descripción
[Descripción breve de qué hace esta guía]

## Paso 1: [Nombre del paso]

[Descripción del paso]

### Comandos

```bash
comando1
comando2
```

### Templates

📄 COPIAR TEMPLATE: `origen` → `destino`

## Paso 2: [Siguiente paso]
...
```

### Jerarquía de Títulos

- **Título Principal (`#`)**: Nombre de la guía
- **Secciones (`##`)**: Pasos numerados
- **Subsecciones (`###`)**: Comandos, Templates, Configuración, etc.

---

## 2. Metadatos de Versión

**Formato obligatorio:**

```markdown
**Versión:** X.Y.Z
```

**Ubicación:** Inmediatamente después del título principal, antes de cualquier contenido.

**Reglas:**
- ✅ Debe usar negrita: `**Versión:**`
- ✅ Formato semántico: `MAJOR.MINOR.PATCH`
- ✅ Sin espacios adicionales
- ❌ No usar `Version:` o `version:`
- ❌ No usar paréntesis o corchetes

**Ejemplos válidos:**
```markdown
**Versión:** 1.0.0
**Versión:** 2.3.1
```

**Ejemplos inválidos:**
```markdown
Versión: 1.0.0        ❌ (sin negrita)
**Version:** 1.0.0    ❌ (en inglés)
**Versión: 1.0.0**    ❌ (versión en negrita)
(Versión: 1.0.0)      ❌ (con paréntesis)
```

---

## 3. Instrucciones de Templates

### 3.1 Copiar Archivo Individual

**Sintaxis:**

```markdown
📄 COPIAR TEMPLATE: `ruta/origen.ext` → `ruta/destino.ext`
```

**Reglas:**
- ✅ Emoji 📄 seguido de espacio y "COPIAR TEMPLATE:"
- ✅ Rutas entre backticks (`)
- ✅ Flecha Unicode → (no `->` o `=>`)
- ✅ Espacios alrededor de la flecha
- ❌ No usar asteriscos para negrita en esta línea

**Ejemplos válidos:**
```markdown
📄 COPIAR TEMPLATE: `templates/Entity.cs` → `src/{ProjectName}.domain/Entities/Entity.cs`
📄 COPIAR TEMPLATE: `templates/IRepository.cs` → `src/{ProjectName}.domain/Interfaces/IRepository.cs`
```

**Ejemplos inválidos:**
```markdown
**📄 COPIAR TEMPLATE:** `...` → `...`     ❌ (negrita)
📄 COPIAR TEMPLATE: templates/Entity.cs → src/Entity.cs  ❌ (sin backticks)
📄 COPIAR TEMPLATE: `...` -> `...`       ❌ (flecha ASCII)
📄COPIAR TEMPLATE:`...`→`...`            ❌ (sin espacios)
```

### 3.2 Copiar Directorio Completo

**Sintaxis:**

```markdown
📁 COPIAR DIRECTORIO COMPLETO: `ruta/origen/` → `ruta/destino/`
```

**Reglas:**
- ✅ Emoji 📁 seguido de espacio y "COPIAR DIRECTORIO COMPLETO:"
- ✅ Rutas terminan en `/` para indicar directorio
- ✅ Copia recursivamente todo el contenido
- ✅ Reemplaza placeholders en todos los archivos

**Ejemplos válidos:**
```markdown
📁 COPIAR DIRECTORIO COMPLETO: `templates/Entities/` → `src/{ProjectName}.domain/Entities/`
📁 COPIAR DIRECTORIO COMPLETO: `templates/Infrastructure/` → `src/{ProjectName}.infrastructure/`
```

---

## 4. Comandos de Terminal

### 4.1 Formato de Bloques de Código

**Sintaxis:**

````markdown
```bash
comando1
comando2 argumento
comando3 --flag
```
````

**Reglas:**
- ✅ Usar lenguaje `bash` en el fence
- ✅ Un comando por línea
- ✅ NO incluir prompts (`$`, `>`, `C:\>`)
- ✅ NO incluir output esperado dentro del bloque
- ✅ Líneas de cierre sin espacios al final

**Comandos soportados actualmente:**
- ✅ Comandos que inician con `dotnet`
- ✅ Comandos que inician con `dotnet.exe`

**Próximamente soportados:**
- ⏳ `rm` - Eliminar archivos
- ⏳ `mkdir` - Crear directorios
- ⏳ `mv` - Mover/renombrar archivos
- ⏳ `cp` - Copiar archivos

**Ejemplos válidos:**
````markdown
```bash
dotnet new classlib -n {ProjectName}.domain
dotnet sln add src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add src/{ProjectName}.webapi reference src/{ProjectName}.application
```
````

**Ejemplos inválidos:**
````markdown
```bash
$ dotnet build              ❌ (incluye prompt)
> dotnet run                ❌ (incluye prompt)
```

```bash
dotnet build
Building...                 ❌ (incluye output)
Build succeeded
```

```bash                     ❌ (espacios al final de línea de cierre)
dotnet build
```
````

### 4.2 Comandos con Placeholders

Los comandos pueden usar placeholders que serán reemplazados:

```bash
dotnet new classlib -n {ProjectName}.domain
mkdir src/{ProjectName}.webapi/Endpoints
```

---

## 5. Edición de Archivos

### 5.1 Buscar y Reemplazar

**Sintaxis:**

````markdown
✏️ EDITAR ARCHIVO: `ruta/del/archivo.cs`
🔍 BUSCAR:
```csharp
código a buscar (literal)
```
✍️ REEMPLAZAR CON:
```csharp
código nuevo
```
````

**Reglas:**
- ✅ Emoji ✏️ seguido de "EDITAR ARCHIVO:"
- ✅ Emoji 🔍 seguido de "BUSCAR:"
- ✅ Emoji ✍️ seguido de "REEMPLAZAR CON:"
- ✅ Búsqueda literal (no regex)
- ✅ Preserva indentación del código encontrado
- ❌ Si no encuentra el patrón, genera error

**Ejemplo:**

````markdown
✏️ EDITAR ARCHIVO: `src/{ProjectName}.webapi/Program.cs`
🔍 BUSCAR:
```csharp
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```
✍️ REEMPLAZAR CON:
```csharp
var app = builder.Build();

// Configurar FastEndpoints
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

app.Run();
```
````

### 5.2 Insertar Después De

**Sintaxis:**

````markdown
➕ INSERTAR EN: `ruta/del/archivo.cs`
📍 DESPUÉS DE:
```csharp
patrón de búsqueda
```
📝 INSERTAR:
```csharp
código a insertar
```
````

**Reglas:**
- ✅ Emoji ➕ seguido de "INSERTAR EN:"
- ✅ Emoji 📍 seguido de "DESPUÉS DE:"
- ✅ Emoji 📝 seguido de "INSERTAR:"
- ✅ Preserva indentación del patrón
- ✅ Útil para agregar using statements, configuraciones

**Ejemplo:**

````markdown
➕ INSERTAR EN: `src/{ProjectName}.webapi/Program.cs`
📍 DESPUÉS DE:
```csharp
var builder = WebApplication.CreateBuilder(args);
```
📝 INSERTAR:
```csharp

// Configurar servicios de FastEndpoints
builder.Services.AddFastEndpoints();
```
````

### 5.3 Agregar al Final

**Sintaxis:**

````markdown
➕ AGREGAR AL FINAL: `ruta/del/archivo.cs`
📝 CONTENIDO:
```csharp
código a agregar
```
````

**Reglas:**
- ✅ Emoji ➕ seguido de "AGREGAR AL FINAL:"
- ✅ Emoji 📝 seguido de "CONTENIDO:"
- ✅ Agrega al final del archivo
- ✅ Útil para agregar nuevas clases, métodos

**Ejemplo:**

````markdown
➕ AGREGAR AL FINAL: `src/{ProjectName}.domain/Entities/BaseEntity.cs`
📝 CONTENIDO:
```csharp

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```
````

### 5.4 Reemplazar Sección

**Sintaxis:**

````markdown
🔄 REEMPLAZAR SECCIÓN: `ruta/del/archivo.cs`
📍 DESDE:
```csharp
// === INICIO SECCIÓN ===
```
📍 HASTA:
```csharp
// === FIN SECCIÓN ===
```
✍️ REEMPLAZAR CON:
```csharp
nuevo código de la sección
```
````

**Reglas:**
- ✅ Emoji 🔄 seguido de "REEMPLAZAR SECCIÓN:"
- ✅ Emoji 📍 seguido de "DESDE:" y "HASTA:"
- ✅ Reemplaza todo entre los marcadores (inclusive)
- ✅ Útil para reemplazar bloques completos

---

## 6. Placeholders

### 6.1 Placeholders Soportados

| Placeholder | Descripción | Ejemplo Input | Ejemplo Output |
|-------------|-------------|---------------|----------------|
| `{ProjectName}` | Nombre del proyecto (PascalCase) | `MiProyecto` | `MiProyecto` |
| `{projectname}` | Nombre del proyecto (lowercase) | `MiProyecto` | `miproyecto` |
| `{PROJECT_NAME}` | Nombre del proyecto (UPPER_SNAKE_CASE) | `MiProyecto` | `MI_PROYECTO` |

### 6.2 Uso de Placeholders

**En rutas:**
```markdown
📄 COPIAR TEMPLATE: `templates/Entity.cs` → `src/{ProjectName}.domain/Entities/Entity.cs`
```

**En comandos:**
```bash
dotnet new classlib -n {ProjectName}.domain
```

**En código:**
```csharp
namespace {ProjectName}.Domain.Entities
{
    public class Entity
    {
        // ...
    }
}
```

### 6.3 Reglas de Placeholders

- ✅ Usar llaves `{}` exactamente
- ✅ Respeta mayúsculas/minúsculas
- ❌ No usar `${ProjectName}` (sintaxis de variables)
- ❌ No usar `<ProjectName>` (sintaxis de generics)

---

## 7. Reglas de Formateo

### 7.1 Bloques de Código

**Correcto:**
````markdown
```csharp
public class Entity
{
    public int Id { get; set; }
}
```
````

**Incorrecto:**
````markdown
```csharp
public class Entity
{
    public int Id { get; set; }
}
```   ❌ (espacios al final)
````

### 7.2 Espacios y Líneas en Blanco

- ✅ Una línea en blanco entre secciones
- ✅ NO líneas en blanco al inicio/final de bloques de código
- ✅ NO espacios al final de líneas (trailing whitespace)
- ✅ NO tabs, usar espacios para indentación

### 7.3 Emojis

**Emojis estándar:**
- 📄 Copiar archivo
- 📁 Copiar directorio
- ✏️ Editar archivo
- 🔍 Buscar patrón
- ✍️ Reemplazar con
- ➕ Insertar/Agregar
- 📍 Posición (después de, desde, hasta)
- 📝 Contenido
- 🔄 Reemplazar sección

**Reglas:**
- ✅ Un emoji por instrucción
- ✅ Espacio después del emoji
- ✅ Emoji al inicio de línea
- ❌ NO usar múltiples emojis en la misma instrucción

---

## 8. Ejemplos Completos

### 8.1 Guía con Templates y Comandos

````markdown
# Milestone 2: Application Layer

**Versión:** 1.0.0

## Descripción

Implementa la capa de aplicación con patrones CQRS usando MediatR.

## Paso 1: Crear proyecto

### Comandos

```bash
dotnet new classlib -n {ProjectName}.application
dotnet sln add src/{ProjectName}.application/{ProjectName}.application.csproj
dotnet add src/{ProjectName}.application reference src/{ProjectName}.domain
```

### Templates

📄 COPIAR TEMPLATE: `templates/ICommand.cs` → `src/{ProjectName}.application/Abstractions/ICommand.cs`
📄 COPIAR TEMPLATE: `templates/IQuery.cs` → `src/{ProjectName}.application/Abstractions/IQuery.cs`
📁 COPIAR DIRECTORIO COMPLETO: `templates/Behaviors/` → `src/{ProjectName}.application/Behaviors/`

## Paso 2: Configurar MediatR

### Comandos

```bash
dotnet add src/{ProjectName}.application package MediatR
dotnet add src/{ProjectName}.application package FluentValidation
```
````

### 8.2 Guía con Edición de Archivos

````markdown
# Milestone 4: WebAPI Configuration

**Versión:** 1.0.0

## Descripción

Configura FastEndpoints en el proyecto WebAPI.

## Paso 1: Crear proyecto WebAPI

### Comandos

```bash
dotnet new web -n {ProjectName}.webapi
dotnet sln add src/{ProjectName}.webapi/{ProjectName}.webapi.csproj
```

## Paso 2: Configurar Program.cs

✏️ EDITAR ARCHIVO: `src/{ProjectName}.webapi/Program.cs`
🔍 BUSCAR:
```csharp
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```
✍️ REEMPLAZAR CON:
```csharp
var app = builder.Build();

// Configurar FastEndpoints
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

app.Run();
```

➕ INSERTAR EN: `src/{ProjectName}.webapi/Program.cs`
📍 DESPUÉS DE:
```csharp
var builder = WebApplication.CreateBuilder(args);
```
📝 INSERTAR:
```csharp

// Configurar servicios de FastEndpoints
builder.Services.AddFastEndpoints();
```

## Paso 3: Copiar templates

📁 COPIAR DIRECTORIO COMPLETO: `templates/Endpoints/` → `src/{ProjectName}.webapi/Endpoints/`
📄 COPIAR TEMPLATE: `templates/IPrincipalExtender.cs` → `src/{ProjectName}.webapi/Extensions/IPrincipalExtender.cs`
````

---

## 9. Errores Comunes

### ❌ Error 1: Versión sin formato correcto

**Incorrecto:**
```markdown
Versión: 1.0.0
```

**Correcto:**
```markdown
**Versión:** 1.0.0
```

### ❌ Error 2: Espacios al final del bloque de código

**Incorrecto:**
````markdown
```bash
dotnet build
```
````

**Correcto:**
````markdown
```bash
dotnet build
```
````

### ❌ Error 3: Flecha ASCII en lugar de Unicode

**Incorrecto:**
```markdown
📄 COPIAR TEMPLATE: `origen.cs` -> `destino.cs`
```

**Correcto:**
```markdown
📄 COPIAR TEMPLATE: `origen.cs` → `destino.cs`
```

### ❌ Error 4: Sin backticks en rutas

**Incorrecto:**
```markdown
📄 COPIAR TEMPLATE: templates/Entity.cs → src/Entity.cs
```

**Correcto:**
```markdown
📄 COPIAR TEMPLATE: `templates/Entity.cs` → `src/Entity.cs`
```

### ❌ Error 5: Negrita en instrucciones de templates

**Incorrecto:**
```markdown
**📄 COPIAR TEMPLATE:** `origen.cs` → `destino.cs`
```

**Correcto:**
```markdown
📄 COPIAR TEMPLATE: `origen.cs` → `destino.cs`
```

### ❌ Error 6: Incluir prompt en comandos

**Incorrecto:**
````markdown
```bash
$ dotnet build
> dotnet run
```
````

**Correcto:**
````markdown
```bash
dotnet build
dotnet run
```
````

---

## 10. Validación de Guías

### Checklist antes de publicar una guía:

- [ ] **Versión:** Formato `**Versión:** X.Y.Z` presente
- [ ] **Bloques de código:** Sin espacios al final de líneas de cierre
- [ ] **Templates:** Emojis correctos (📄/📁) y flecha Unicode (→)
- [ ] **Rutas:** Todas entre backticks (`)
- [ ] **Comandos:** Sin prompts (`$`, `>`) ni output
- [ ] **Placeholders:** Formato correcto `{ProjectName}`
- [ ] **Indentación:** Espacios (no tabs)
- [ ] **Trailing whitespace:** Eliminado de todo el archivo
- [ ] **Emojis:** Un emoji por instrucción
- [ ] **Lenguaje de código:** Especificado (`bash`, `csharp`, etc.)

### Herramientas de Validación

```bash
# Buscar espacios al final de líneas
grep -n ' $' guia.md

# Buscar tabs
grep -P '\t' guia.md

# Validar formato de versión
grep -n '\*\*Versión:\*\*' guia.md
```

---

## 11. Versionado de Guías

### Formato Semántico (SemVer)

- **MAJOR (X.0.0)**: Cambios incompatibles (cambio de estructura, sintaxis)
- **MINOR (0.X.0)**: Nuevas funcionalidades compatibles (nuevos templates, pasos)
- **PATCH (0.0.X)**: Correcciones de bugs (typos, formato, rutas)

### Ejemplos:

- `1.0.0 → 1.0.1`: Corrección de typo en ruta
- `1.0.0 → 1.1.0`: Agregar nuevo template opcional
- `1.0.0 → 2.0.0`: Cambiar sintaxis de instrucciones

---

## 12. Compatibilidad con MCP Server

### Versiones de Guías Soportadas

| MCP Server | Guías Soportadas | Funcionalidades |
|------------|------------------|-----------------|
| 0.7.x | 1.0.x - 1.4.x | Templates, Comandos dotnet |
| 0.8.x | 1.5.x+ | + Edición de archivos |
| 0.9.x | 2.0.x+ | + Comandos bash completos |

### Verificar Compatibilidad

El MCP Server valida automáticamente la versión de las guías:

```
✅ Guías compatibles: v1.4.2
⚠️  MCP Server: v0.7.2 soporta guías 1.0.x - 1.4.x
```

---

## 📚 Referencias

- **MCP Server Repository:** https://github.com/anthropics/mcp-server
- **Markdown Spec:** CommonMark 0.30
- **SemVer:** https://semver.org/

---

## 📝 Changelog de esta Especificación

### 1.0.0 (2025-01-30)
- ✨ Especificación inicial
- 📄 Sintaxis de templates (archivo y directorio)
- 💻 Sintaxis de comandos bash
- ✏️ Sintaxis de edición de archivos (4 operaciones)
- 🔤 Especificación de placeholders
- ✅ Reglas de formateo y validación
