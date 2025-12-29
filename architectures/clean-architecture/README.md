# Clean Architecture

Guia para crear proyectos backend .NET con Clean Architecture.

---

## Estructura de Capas

```
┌─────────────────────────────────────────┐
│            WebAPI Layer                 │
│      (Endpoints + Swagger)              │
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

## Contenido

### init/

Guias de inicializacion para crear un proyecto desde cero.

| Guia | Descripcion |
|------|-------------|
| [01-estructura-base.md](init/01-estructura-base.md) | Solucion .NET, Directory.Packages.props |
| [02-domain-layer.md](init/02-domain-layer.md) | Entidades, interfaces de repositorios |
| [03-application-layer.md](init/03-application-layer.md) | Use cases, DTOs, validators |
| [04-infrastructure-layer.md](init/04-infrastructure-layer.md) | Estructura base para repositorios |
| [05-webapi-layer.md](init/05-webapi-layer.md) | Proyecto WebAPI base con Swagger |

**Orden de ejecucion:** 01 → 02 → 03 → 04 → 05

### guides/

Guias por capa con referencias a patrones fundamentales.

| Capa | Descripcion |
|------|-------------|
| [guides/domain/](guides/domain/) | Domain layer + referencias a fundamentals |
| [guides/application/](guides/application/) | Use cases, command handlers |
| [guides/infrastructure/](guides/infrastructure/) | Referencias a stacks |
| [guides/webapi/](guides/webapi/) | Auth, DTOs, error responses |
| [guides/feature-structure/](guides/feature-structure/) | Organizacion de features |

**Principios:** [guides/clean-architecture-principles.md](guides/clean-architecture-principles.md)

### examples/

Ejemplos completos de features.

| Ejemplo | Descripcion |
|---------|-------------|
| [examples/crud-feature/](examples/crud-feature/) | Feature CRUD completo |
| [examples/read-only-feature/](examples/read-only-feature/) | Feature solo lectura |
| [examples/complex-feature/](examples/complex-feature/) | Feature complejo |

---

## Proyecto Generado

```
{ProjectName}/
├── {ProjectName}.sln
├── Directory.Packages.props
├── Directory.Build.props
├── src/
│   ├── {ProjectName}.domain/
│   ├── {ProjectName}.application/
│   ├── {ProjectName}.infrastructure/
│   └── {ProjectName}.webapi/
└── tests/
    └── ...
```

---

## Relacion con Fundamentals

Los patrones fundamentales (agnósticos de arquitectura) se encuentran en:
- [fundamentals/patterns/domain-modeling/](../../fundamentals/patterns/domain-modeling/) - Entidades, validadores
- [fundamentals/patterns/repository/](../../fundamentals/patterns/repository/) - Repository, UoW
- [fundamentals/patterns/best-practices/](../../fundamentals/patterns/best-practices/) - Mejores practicas

Las guias en `guides/` hacen **referencia** a estos patrones y agregan convenciones específicas de Clean Architecture.

---

## Siguiente Paso

Despues de ejecutar las guias de `init/`, configurar los stacks tecnologicos:

1. **Base de datos:** [stacks/database/{postgresql|sqlserver}/](../../stacks/database/)
2. **ORM:** [stacks/orm/nhibernate/](../../stacks/orm/nhibernate/)
3. **WebAPI:** [stacks/webapi/fastendpoints/](../../stacks/webapi/fastendpoints/)
4. **Migraciones:** [stacks/database/migrations/fluent-migrator/](../../stacks/database/migrations/fluent-migrator/)
