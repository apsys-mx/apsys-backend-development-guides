# Infrastructure Layer - Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

La capa de infraestructura contiene las **implementaciones** de las interfaces definidas en Domain. Esta capa maneja la persistencia de datos, servicios externos, y cualquier detalle de implementación que el Domain no debe conocer.

## Responsabilidades

- Implementar interfaces de repositorios (IRepository, IUnitOfWork)
- Integración con ORMs (NHibernate, Entity Framework)
- Integración con servicios externos (Auth0, APIs REST)
- Implementación de caché
- Manejo de migraciones de BD
- File I/O, networking, etc.

## Principios

- **Implementa** interfaces definidas en Domain
- **No expone** detalles de implementación hacia afuera
- **Depende** solo de Domain (y librerías externas)
- **Es intercambiable**: Puedes cambiar ORM sin afectar Domain

## Estructura de Secciones

### Core Concepts (Agnóstico)
Guías de conceptos independientes de implementación específica.

### [ORM Implementations](./orm-implementations/README.md)
Implementaciones específicas de ORMs (NHibernate, Entity Framework, etc.)

### [External Services](./external-services/README.md)
Integraciones con servicios externos (Auth0, caching, HTTP clients, etc.)

### [Data Migrations](./data-migrations/README.md)
Sistemas de migraciones de base de datos (FluentMigrator, EF Migrations, etc.)

---

## Guías Core (Agnósticas)

| Guía | Estado | Descripción |
|------|--------|-------------|
| [core-concepts.md](./core-concepts.md) | ⏳ Pendiente | Conceptos fundamentales |
| [repository-pattern.md](./repository-pattern.md) | ⏳ Pendiente | Repository pattern |
| [unit-of-work-pattern.md](./unit-of-work-pattern.md) | ⏳ Pendiente | UoW pattern |
| [transactions.md](./transactions.md) | ⏳ Pendiente | Manejo de transacciones |
| [dependency-injection.md](./dependency-injection.md) | ⏳ Pendiente | DI en infrastructure |

**Proyecto de referencia:** `hashira.stone.backend`

---

**Última actualización:** 2025-01-13
