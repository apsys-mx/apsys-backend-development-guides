# APSYS Backend Development Guides

> **Version:** 4.1.0 | **Last Update:** 2026-01-27

Guias de desarrollo, patrones y templates para crear proyectos backend .NET.

**Arquitectura actual:** Clean Architecture
**Proximas:** Vertical Slices, Modular Monolith

---

## Instalacion en Proyectos

Este repositorio esta disenado para usarse como **git submodule** en la ruta `docs/guides/`.

### Agregar a un proyecto nuevo

```bash
# Desde la raiz de tu proyecto
git submodule add https://github.com/apsys-mx/apsys-backend-development-guides.git docs/guides
git commit -m "Add backend development guides as submodule"
```

### Clonar un proyecto que ya tiene el submodule

```bash
# Opcion 1: Clonar con submodules incluidos
git clone --recurse-submodules <url-del-proyecto>

# Opcion 2: Si ya clonaste sin submodules
git submodule update --init --recursive
```

### Actualizar las guias

```bash
cd docs/guides
git pull origin master
cd ../..
git add docs/guides
git commit -m "Update development guides"
```

### Estructura resultante

```
tu-proyecto/
├── src/
├── tests/
├── docs/
│   └── guides/              <- Submodule aqui
│       ├── architectures/
│       ├── fundamentals/
│       ├── prompts/
│       ├── stacks/
│       ├── templates/
│       └── testing/
└── ...
```

> **Nota:** Todos los prompts y guias asumen que las guias estan en `docs/guides/`.

---

## Estructura del Repositorio

```
apsys-backend-development-guides/
│
├── fundamentals/              # Patrones agnósticos de arquitectura
│   └── patterns/
│       ├── domain-modeling/   # Entidades, validadores, excepciones
│       ├── repository/        # Repository, UoW, transacciones
│       └── best-practices/    # Async/await, DI, error handling
│
├── architectures/             # Guias de arquitectura
│   └── clean-architecture/
│       ├── init/              # Guias de inicializacion (01-05)
│       ├── guides/            # Guias por capa con referencias
│       └── examples/          # Ejemplos de features completos
│
├── stacks/                    # Stacks tecnologicos
│   ├── database/              # PostgreSQL, SQL Server, migrations
│   ├── orm/                   # NHibernate, Entity Framework
│   ├── webapi/                # FastEndpoints
│   └── external-services/     # Caching, identity providers
│
├── templates/                 # Templates de codigo reutilizables
│   ├── domain/                # Entidades, interfaces, excepciones
│   ├── webapi/                # Endpoints, configuracion
│   └── tests/                 # Test bases
│
├── testing/                   # Guias y templates de testing
│   ├── fundamentals/          # Convenciones de testing
│   ├── unit/                  # Tests unitarios
│   └── integration/           # Tests de integracion
│
└── prompts/                   # Prompts y comandos para agentes IA
    └── commands/              # Comandos ejecutables
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

Los proyectos generados actualmente siguen **Clean Architecture** (ver [architectures/](architectures/) para otras opciones):

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

## Organizacion de la Documentacion

### Patrones Fundamentales (agnósticos)
Patrones que funcionan en cualquier arquitectura:
- [fundamentals/patterns/domain-modeling/](fundamentals/patterns/domain-modeling/) - Entidades, validadores
- [fundamentals/patterns/repository/](fundamentals/patterns/repository/) - Repository, UoW
- [fundamentals/patterns/best-practices/](fundamentals/patterns/best-practices/) - Mejores practicas

### Guias de Arquitectura (específicas)
Como aplicar los patrones en cada arquitectura:
- [architectures/](architectures/) - Todas las arquitecturas disponibles
- [architectures/clean-architecture/](architectures/clean-architecture/) - Clean Architecture (actual)

### Stacks Tecnologicos (implementaciones)
Configuracion de tecnologias específicas:
- [stacks/orm/nhibernate/](stacks/orm/nhibernate/) - NHibernate
- [stacks/database/](stacks/database/) - PostgreSQL, SQL Server, migrations
- [stacks/webapi/fastendpoints/](stacks/webapi/fastendpoints/) - FastEndpoints

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
| `fundamentals/` | Patrones agnósticos de arquitectura |
| `architectures/` | Guias de arquitectura (Clean Architecture, etc.) |
| `stacks/` | Configuracion de tecnologias especificas |
| `templates/` | Codigo reutilizable con placeholders `{ProjectName}` |
| `testing/` | Guias y templates de testing |
| `prompts/` | Comandos y prompts para agentes IA |

---

## Versionado

Este repositorio usa **versionado semantico** (MAJOR.MINOR.PATCH).

- **Version actual:** 4.0.0
- **Compatibilidad:** .NET 9.0

---

## Licencia

Uso interno de APSYS.

---

**Mantenedores:** Equipo de Desarrollo APSYS
