# NHibernate Guides

Guias para implementar la capa de persistencia con NHibernate en proyectos .NET.

## Contenido

| Guia | Descripcion |
|------|-------------|
| [Setup](setup.md) | Configuracion inicial de NHibernate en el proyecto |
| [Repositories](repositories.md) | Implementacion de repositorios con NHibernate |
| [Mappers](mappers.md) | Configuracion de mapeos entidad-tabla |
| [Queries](queries.md) | Consultas con LINQ, HQL y Criteria API |
| [Unit of Work](unit-of-work.md) | Patron Unit of Work con NHibernate |
| [Session Management](session-management.md) | Manejo del ciclo de vida de sesiones |
| [Best Practices](best-practices.md) | Mejores practicas y optimizaciones |

## Orden Sugerido

1. **Setup** - Configurar NHibernate en el proyecto
2. **Mappers** - Definir mapeos de entidades
3. **Repositories** - Implementar repositorios
4. **Unit of Work** - Configurar transacciones
5. **Queries** - Aprender diferentes formas de consultar
6. **Session Management** - Optimizar manejo de sesiones
7. **Best Practices** - Aplicar mejores practicas

## Prerequisitos

- Proyecto con estructura Clean Architecture configurada
- Base de datos configurada (ver `stacks/database/`)
- Entidades de dominio definidas (ver `fundamentals/patterns/domain-modeling/`)

## Referencias

- [Repository Pattern](../../../fundamentals/patterns/repository/repository-pattern.md)
- [Unit of Work Pattern](../../../fundamentals/patterns/repository/unit-of-work-pattern.md)
- [Transactions](../../../fundamentals/patterns/repository/transactions.md)
