# Tool: init-clean-architecture

## Descripción General

Este tool del servidor MCP crea la estructura completa de un proyecto backend con **Clean Architecture** para APSYS. El proyecto generado es independiente de cualquier base de datos específica, permitiendo máxima flexibilidad en la elección de tecnología de persistencia.

## Propósito

Automatizar la creación de:
- Solución .NET con gestión centralizada de paquetes
- Capa de dominio con entidades, validaciones y repositorios
- Capa de infraestructura con sistema de filtering avanzado
- Capa de aplicación con casos de uso
- API REST con FastEndpoints
- Sistema de migraciones de base de datos
- Proyectos de testing completos

## Arquitectura Generada

El tool genera un proyecto siguiendo los principios de **Clean Architecture**:

```
┌─────────────────────────────────────────┐
│           WebApi Layer                  │
│  (FastEndpoints + Controllers)          │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│        Application Layer                │
│     (Use Cases + Services)              │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│          Domain Layer                   │
│  (Entities + Interfaces + Rules)        │
└──────────────▲──────────────────────────┘
               │
┌──────────────┴──────────────────────────┐
│      Infrastructure Layer               │
│  (Repositories + ORM + External)        │
└─────────────────────────────────────────┘
```

### Características Clave

✅ **Independencia de BD:** Sin código específico de PostgreSQL o SQL Server
✅ **Testing First:** Proyectos de test para cada capa
✅ **Gestión Centralizada:** Paquetes NuGet versionados en un solo lugar
✅ **Filtering Avanzado:** Sistema de filtrado con LINQ dinámico
✅ **Validaciones:** FluentValidation integrado en entidades
✅ **API Moderna:** FastEndpoints como framework de API
✅ **Migraciones:** FluentMigrator con UI interactiva

## Parámetros de Entrada

```bash
init-clean-architecture --name=<NombreProyecto> --version=<VersionNET> --path=<RutaDestino>
```

| Parámetro   | Descripción                        | Requerido | Ejemplo                  |
| ----------- | ---------------------------------- | --------- | ------------------------ |
| `--name`    | Nombre de la solución y proyectos  | ✅ Sí     | `MiProyecto`             |
| `--version` | Versión del framework .NET         | ✅ Sí     | `net9.0`                 |
| `--path`    | Ruta donde crear el proyecto       | ✅ Sí     | `C:\projects\miproyecto` |

**Nota:** El parámetro `--db` NO se usa en este tool. La configuración de base de datos se realiza posteriormente con el tool `configure-database`.

## Estructura Final Generada

```
{name}/
├── {name}.sln
├── Directory.Packages.props
├── src/
│   ├── {name}.domain/
│   ├── {name}.application/
│   ├── {name}.infrastructure/
│   ├── {name}.webapi/
│   └── {name}.migrations/
└── tests/
    ├── {name}.domain.tests/
    ├── {name}.application.tests/
    ├── {name}.infrastructure.tests/
    ├── {name}.webapi.tests/
    ├── {name}.common.tests/
    └── {name}.scenarios/
```

## Documentación por Milestones

La implementación está organizada en **3 milestones** para facilitar desarrollo y testing incremental:

### 📦 Milestone 1: Fundamentos (ACTUAL)

Documentos completados:

- **[01-estructura-base.md](./01-estructura-base.md)**
  - Solución .sln
  - Carpetas src/ y tests/
  - Directory.Packages.props

- **[02-domain-layer.md](./02-domain-layer.md)**
  - Proyecto domain + tests
  - Entidades base
  - Interfaces de repositorios
  - Excepciones de dominio

**Estado:** ✅ Completado

### 🔧 Milestone 2: Infrastructure (PRÓXIMO)

Documentos pendientes:

- **03-infrastructure-filtering.md**
  - Sistema de parsing de querystring
  - Operadores relacionales (equal, contains, between, etc.)
  - Construcción de expresiones LINQ dinámicas
  - Soporte para ordenamiento y paginación

- **04-infrastructure-repositories.md**
  - Implementación base de repositorios
  - UnitOfWork
  - Extensiones de NHibernate (sin configuración de BD específica)

**Estado:** ⏳ Pendiente

### 🚀 Milestone 3: Application, API y Testing (FUTURO)

Documentos pendientes:

- **05-application-layer.md**
  - Proyecto application + tests
  - Estructura para casos de uso

- **06-webapi-base.md**
  - Proyecto webapi + tests
  - Configuración de FastEndpoints
  - Program.cs base (sin connection string)
  - Estructura de endpoints

- **07-migrations-base.md**
  - Proyecto migrations
  - CLI interactivo con Spectre.Console
  - Program.cs genérico (sin provider específico)

- **08-testing-projects.md**
  - common.tests (schemas XSD)
  - scenarios (generador de datos)

**Estado:** ⏳ Pendiente

## Orden de Ejecución de Documentos

Los documentos deben ejecutarse en orden secuencial dentro de cada milestone:

```
1. Milestone 1
   └─> 01-estructura-base.md
       └─> 02-domain-layer.md

2. Milestone 2
    └─> 03-infrastructure-filtering.md
        └─> 04-infrastructure-repositories.md

3. Milestone 3
    └─> 05-application-layer.md
        └─> 06-webapi-base.md
            └─> 07-migrations-base.md
                └─> 08-testing-projects.md
```

Cada documento tiene una sección **"Dependencias"** que indica qué pasos previos deben completarse.

## Uso Independiente de Documentos

Aunque el tool MCP ejecutará todos los documentos automáticamente, cada documento puede usarse de forma independiente para:

- **Consulta:** Entender cómo funciona cada componente
- **Troubleshooting:** Depurar problemas en componentes específicos
- **Extensión manual:** Agregar componentes adicionales siguiendo los patrones establecidos

## Siguiente Tool

Una vez completado `init-clean-architecture`, el proyecto está listo para configurar una base de datos específica con:

**[configure-database](../configure-database/README.md)** - Configura PostgreSQL o SQL Server

## Stack Tecnológico

### Frameworks y Bibliotecas

- **.NET 9.0** - Framework base
- **FastEndpoints** - API REST framework
- **NHibernate** - ORM
- **FluentMigrator** - Migraciones de BD
- **FluentValidation** - Validaciones
- **AutoMapper** - Mapeo de objetos
- **Scrutor** - Inyección de dependencias por convención

### Testing

- **NUnit** - Framework de testing
- **Moq** - Mocking
- **AutoFixture** - Generación de datos de prueba
- **FluentAssertions** - Aserciones fluidas

### DevOps

- **Spectre.Console** - CLI interactiva
- **DotNetEnv** - Variables de entorno

## Notas de Implementación para el Servidor MCP

### Substituciones de Variables

El servidor MCP debe reemplazar los siguientes placeholders en todos los archivos:

| Placeholder  | Fuente          | Ejemplo          |
| ------------ | --------------- | ---------------- |
| `{name}`     | `--name`        | `MiProyecto`     |
| `{path}`     | `--path`        | `C:\projects\..` |
| `{version}`  | `--version`     | `net9.0`         |

### Manejo de Rutas

- Todas las rutas en los documentos usan formato POSIX (`/`)
- El servidor MCP debe convertir a formato Windows (`\`) cuando corresponda
- Soportar tanto rutas absolutas como relativas

### Validaciones Pre-ejecución

Antes de ejecutar el tool, validar:

1. ✅ .NET SDK está instalado con la versión especificada
2. ✅ El path de destino existe y tiene permisos de escritura
3. ✅ No existe ya una solución con el mismo nombre en el path
4. ✅ El nombre del proyecto es un identificador C# válido

### Manejo de Errores

Si algún paso falla:
- Registrar el error con contexto (qué paso, qué comando)
- Mostrar mensaje descriptivo al usuario
- Opcionalmente, ofrecer rollback de cambios parciales

## Referencias

- **Manual completo:** [MANUAL_CONSTRUCCION_PROYECTO.md](../../MANUAL_CONSTRUCCION_PROYECTO.md)
- **Conversación de diseño:** [conversacion-mcp-servers.txt](../../conversacion-mcp-servers.txt)
- **Repositorio:** [apsys-backend-development-guides](../../README.md)

## Contribuir

Para agregar o modificar componentes:

1. Revisar el manual completo para extraer información
2. Seguir el formato de los documentos existentes
3. Actualizar este README con los cambios
4. Probar manualmente los comandos antes de commitear

## Changelog

- **2025-01-29:** Milestone 1 completado (estructura base + domain layer)
- **2025-01-29:** Creación inicial del tool y documentación
