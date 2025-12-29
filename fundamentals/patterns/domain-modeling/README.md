# Domain Modeling

Patrones para modelar el dominio de negocio. Estos patrones son **agnósticos de arquitectura** - funcionan igual en Clean Architecture, Hexagonal, DDD, etc.

## Guias

| Guia | Descripcion |
|------|-------------|
| [entities.md](./entities.md) | Como disenar entidades de dominio |
| [validators.md](./validators.md) | Validacion con FluentValidation |
| [domain-exceptions.md](./domain-exceptions.md) | Excepciones especificas del dominio |
| [value-objects.md](./value-objects.md) | Objetos de valor inmutables |
| [repository-interfaces.md](./repository-interfaces.md) | Interfaces de repositorio (contratos) |
| [daos.md](./daos.md) | Data Access Objects |

## Ejemplos

→ Ver: [examples/](./examples/)

Ejemplos de entidades organizados por:
- Complejidad (simple, medium, complex)
- Patrones especificos
- Proyectos reales

## Principios Clave

1. **Entidades ricas, no anemicas**
   - La logica de negocio va EN las entidades
   - No solo getters/setters

2. **Validacion en el dominio**
   - FluentValidation para reglas de negocio
   - Validadores junto a las entidades

3. **Excepciones especificas**
   - `InvalidDomainException` para validaciones fallidas
   - `ResourceNotFoundException` para recursos no encontrados
   - `DuplicatedDomainException` para duplicados

4. **Interfaces, no implementaciones**
   - Domain define `IRepository<T>`
   - Infrastructure implementa `NHRepository<T>`
