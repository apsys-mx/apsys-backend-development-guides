# APSYS Backend Development Guides

> **Version:** 3.0.0 | **Last Update:** 2025-12-23

Guias de desarrollo y templates para crear proyectos backend .NET con **Clean Architecture**.

---

## Estructura del Repositorio

```
apsys-backend-development-guides/
├── architectures/          # Guias de arquitectura
│   └── clean-architecture/ # Clean Architecture para .NET
│       ├── init/           # Guias de inicializacion (01-05)
│       └── layers/         # Guias por capa (domain, application, etc.)
│
├── stacks/                 # Stacks tecnologicos
│   ├── database/           # Bases de datos (postgresql, sqlserver, migrations)
│   ├── orm/                # ORMs (nhibernate, entity-framework)
│   └── webapi/             # Frameworks WebAPI (fastendpoints)
│
├── templates/              # Templates de codigo reutilizables
│   ├── domain/             # Entidades, value objects, repositorios
│   ├── application/        # Use cases, DTOs, validators
│   ├── infrastructure/     # Implementaciones de repositorios
│   ├── webapi/             # Endpoints, configuracion
│   └── tests/              # Unit tests, integration tests
│
├── prompts/                # Prompts y comandos para agentes IA
│   └── commands/           # Comandos ejecutables (/init-backend, etc.)
│
└── guides/                 # [Legacy] Guias en proceso de migracion
```

---

## Comandos Disponibles

### /init-backend

Inicializa un proyecto backend completo con Clean Architecture.

**Solicita:**
- Nombre del proyecto (minusculas, sin espacios)
- Ubicacion del proyecto
- Base de datos (PostgreSQL / SQL Server)
- Framework WebAPI (FastEndpoints / ninguno)
- Incluir migraciones (si/no)

**Genera:**
```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── Directory.Build.props
├── src/
│   ├── {ProjectName}.domain/
│   ├── {ProjectName}.application/
│   ├── {ProjectName}.infrastructure/
│   ├── {ProjectName}.webapi/
│   └── {ProjectName}.migrations/      (opcional)
└── tests/
```

**Ubicacion:** [prompts/commands/init-backend.md](prompts/commands/init-backend.md)

---

## Arquitectura de Proyectos

Los proyectos generados siguen **Clean Architecture**:

```
┌─────────────────────────────────────────┐
│            WebAPI Layer                 │
│      (FastEndpoints + Swagger)          │
└──────────────────┬──────────────────────┘
                   │ depende de
┌──────────────────▼──────────────────────┐
│         Application Layer               │
│       (Use Cases + DTOs)                │
└──────────────────┬──────────────────────┘
                   │ depende de
┌──────────────────▼──────────────────────┐
│           Domain Layer                  │
│    (Entities + Interfaces + Rules)      │
└──────────────────▲──────────────────────┘
                   │ implementa
┌──────────────────┴──────────────────────┐
│       Infrastructure Layer              │
│   (Repositories + ORM + Database)       │
└─────────────────────────────────────────┘
```

---

## Stack Tecnologico

| Categoria | Tecnologia | Version |
|-----------|------------|---------|
| Framework | .NET | 9.0 |
| Lenguaje | C# | 13 |
| ORM | NHibernate | 5.5 |
| WebAPI | FastEndpoints | 7.0 |
| Validacion | FluentValidation | 12.0 |
| Mapeo | AutoMapper | 15.0 |
| Migraciones | FluentMigrator | 7.1 |
| Testing | NUnit + Moq + AutoFixture | - |

### Bases de Datos Soportadas

- **PostgreSQL** - Recomendado
- **SQL Server** - Soportado

---

## Uso de las Guias

### Automatizado (con agente IA)

```
Usuario: /init-backend
Agente: [Ejecuta el comando siguiendo las guias]
```

### Manual (paso a paso)

1. Seguir guias en `architectures/clean-architecture/init/` (01-05)
2. Configurar base de datos con `stacks/database/{db}/guides/setup.md`
3. Configurar ORM con `stacks/orm/nhibernate/guides/setup.md`
4. (Opcional) Configurar WebAPI con `stacks/webapi/fastendpoints/guides/setup.md`
5. (Opcional) Configurar migraciones con `stacks/database/migrations/fluent-migrator/guides/setup.md`

---

## Carpetas Principales

| Carpeta | Proposito |
|---------|-----------|
| `architectures/` | Guias de arquitectura (Clean Architecture, etc.) |
| `stacks/` | Configuracion de tecnologias especificas |
| `templates/` | Codigo reutilizable con placeholders |
| `prompts/` | Comandos y prompts para agentes IA |
| `guides/` | [Legacy] Contenido en proceso de migracion |

---

## Versionado

Este repositorio usa **versionado semantico** (MAJOR.MINOR.PATCH).

- **Version actual:** 3.0.0
- **Compatibilidad:** .NET 9.0

---

## Licencia

Uso interno de APSYS.

---

**Mantenedores:** Equipo de Desarrollo APSYS
