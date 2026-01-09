# Patrones Fundamentales

Patrones de diseno y mejores practicas **independientes de la arquitectura**. Estos conceptos se mantienen aunque cambies de Clean Architecture a otra arquitectura.

## Contenido

### Domain Modeling
Patrones para modelar el dominio de negocio.
→ Ver: [domain-modeling/](./domain-modeling/)

- Entidades
- Validadores (FluentValidation)
- Excepciones de dominio
- Value Objects
- Interfaces de repositorios
- DAOs

### Repository
Patrones de acceso a datos.
→ Ver: [repository/](./repository/)

- Repository Pattern
- Unit of Work Pattern
- Transacciones
- Conceptos core

### Event-Driven
Patrones para manejo de eventos de dominio, auditoría y mensajería.
→ Ver: [event-driven/](./event-driven/)

- Domain Events
- Outbox Pattern
- Event Store

### Best Practices
Mejores practicas generales de .NET.
→ Ver: [best-practices/](./best-practices/)

- Async/Await
- Organizacion de codigo
- Dependency Injection
- Manejo de errores

## Relacion con Arquitecturas

Estos patrones son **bloques de construccion** que se usan en diferentes arquitecturas:

| Arquitectura | Como usa estos patrones |
|--------------|------------------------|
| Clean Architecture | Domain layer + Infrastructure layer |
| Hexagonal | Domain + Adapters |
| Vertical Slices | Por feature, pero mismos patrones |
| N-Layer | Business layer + Data layer |

## Relacion con Stacks

Los patrones definen **que** hacer. Los stacks definen **como** implementarlo:

| Patron | Stack de implementacion |
|--------|------------------------|
| Repository Pattern | NHibernate, Entity Framework |
| Validacion | FluentValidation |
| Migraciones | FluentMigrator, EF Migrations |
| Event-Driven / Outbox | NHibernate + FluentMigrator |
