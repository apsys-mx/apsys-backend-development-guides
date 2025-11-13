# Application Layer - Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

La capa de aplicación contiene los **Use Cases** (casos de uso) de la aplicación. Orquesta el flujo de datos hacia y desde las entidades, y dirige esas entidades a usar sus reglas de negocio para lograr los objetivos del caso de uso.

## Responsabilidades

- Implementar casos de uso de la aplicación
- Orquestar el flujo entre Domain e Infrastructure
- Manejar transacciones con Unit of Work
- Transformar errores de Domain a Results
- No contener lógica de negocio (esa va en Domain)

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [use-cases.md](./use-cases.md) | ⏳ Pendiente | Command/Handler pattern |
| [command-handler-patterns.md](./command-handler-patterns.md) | ⏳ Pendiente | Create, Update, Delete, GetMany |
| [error-handling.md](./error-handling.md) | ⏳ Pendiente | FluentResults usage |
| [common-utilities.md](./common-utilities.md) | ⏳ Pendiente | Utilidades compartidas |

**Proyecto de referencia:** `hashira.stone.backend`

---

**Última actualización:** 2025-01-13
