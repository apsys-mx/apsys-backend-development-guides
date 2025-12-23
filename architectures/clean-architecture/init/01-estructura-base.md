# 01 - Estructura Base del Proyecto

## Descripción

Crea la **estructura base** de un proyecto backend con Clean Architecture. Esta es la primera etapa del proceso de inicialización.

**Se crea:**
- Solución (.sln)
- Carpetas `src/` y `tests/`
- Archivo `Directory.Packages.props` para gestión centralizada de paquetes NuGet

## Requisitos Previos

- .NET SDK 9.0 o superior
- Directorio vacío (o con solo archivos de Git: `.git/`, `.gitignore`, `README.md`)

## Estructura Final

```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── src/
└── tests/
```

## Pasos

### 1. Verificar .NET SDK

```bash
dotnet --version
# Debe mostrar 9.0 o superior
```

### 2. Crear carpetas

```bash
mkdir src tests
```

### 3. Crear solución

```bash
dotnet new sln -n {ProjectName}
```

### 4. Crear Directory.Packages.props

Copiar desde: `templates/Directory.Packages.props`

Este archivo centraliza las versiones de todos los paquetes NuGet. Con él habilitado, los paquetes se agregan sin especificar versión:

```bash
# ✅ Correcto (la versión se toma del archivo central)
dotnet add package FluentValidation

# ❌ Incorrecto
dotnet add package FluentValidation --version 12.0.0
```

## Verificación

```bash
dotnet build
# Debe mostrar: Build succeeded
```

## Reglas para Nombre del Proyecto

El nombre debe ser un identificador C# válido:
- ✅ Empieza con letra o guion bajo
- ✅ Solo letras, números, guiones bajos o puntos
- ❌ Sin espacios ni guiones medios
- ❌ Sin palabras reservadas de C#

**Ejemplos válidos:** `miproyecto`, `mi_proyecto`, `miproyecto.api`

## Siguiente Paso

→ [02-domain-layer.md](./02-domain-layer.md)
