# Fundamentals

Patrones y mejores practicas de desarrollo **agnósticos de arquitectura**. Estos conceptos aplican independientemente de si usas Clean Architecture, Hexagonal, Vertical Slices, o cualquier otra arquitectura.

---

## Contenido

### patterns/

Patrones de diseño fundamentales para aplicaciones .NET.

| Categoria | Descripcion |
|-----------|-------------|
| [patterns/domain-modeling/](./patterns/domain-modeling/) | Entidades, value objects, validadores, excepciones |
| [patterns/repository/](./patterns/repository/) | Repository pattern, Unit of Work, transacciones |
| [patterns/best-practices/](./patterns/best-practices/) | Async/await, DI, manejo de errores, organizacion de codigo |

---

## Por que separar fundamentals/?

Estos patrones se han mantenido **estables a traves de multiples transiciones de arquitectura**:

1. **Domain modeling** - Las entidades, validadores y value objects siguen los mismos principios sin importar la arquitectura
2. **Repository pattern** - La abstraccion de acceso a datos es la misma en Clean Architecture, Hexagonal, etc.
3. **Unit of Work** - El manejo de transacciones es un concepto fundamental
4. **Best practices** - Async/await, DI, error handling aplican en cualquier proyecto

### Beneficios:

- **Sin duplicacion:** Una sola fuente de verdad para patrones fundamentales
- **Facil migracion:** Si cambias de arquitectura, estos patrones siguen siendo validos
- **Referencias claras:** Las guias de arquitectura (`architectures/`) referencian a estos patrones

---

## Uso

### Desde Clean Architecture

Las guias en `architectures/clean-architecture/guides/` hacen referencia a estos patrones:

```markdown
# Domain Layer en Clean Architecture

### Entidades
> Ver: [fundamentals/patterns/domain-modeling/entities.md](../../../fundamentals/patterns/domain-modeling/entities.md)
```

### Desde Stacks

Las implementaciones especificas (NHibernate, FastEndpoints, etc.) implementan estos patrones:

```markdown
# NHibernate Repository Implementation

Implementacion de [Repository Pattern](../../../fundamentals/patterns/repository/repository-pattern.md)
usando NHibernate como ORM.
```

---

## Diagrama de Relaciones

```
                    fundamentals/
                    (patrones base)
                          │
           ┌──────────────┼──────────────┐
           │              │              │
           ▼              ▼              ▼
    architectures/     stacks/       testing/
    (como aplicar)  (implementar)  (como probar)
```

---

## Contenido Detallado

### Domain Modeling

- [entities.md](./patterns/domain-modeling/entities.md) - Clases base, propiedades, constructores
- [value-objects.md](./patterns/domain-modeling/value-objects.md) - Value objects inmutables
- [validators.md](./patterns/domain-modeling/validators.md) - Validacion con FluentValidation
- [domain-exceptions.md](./patterns/domain-modeling/domain-exceptions.md) - Excepciones de dominio
- [repository-interfaces.md](./patterns/domain-modeling/repository-interfaces.md) - Interfaces de repositorio
- [daos.md](./patterns/domain-modeling/daos.md) - Data Access Objects

### Repository

- [repository-pattern.md](./patterns/repository/repository-pattern.md) - El patron Repository
- [unit-of-work-pattern.md](./patterns/repository/unit-of-work-pattern.md) - Unit of Work
- [transactions.md](./patterns/repository/transactions.md) - Manejo de transacciones
- [core-concepts.md](./patterns/repository/core-concepts.md) - Conceptos fundamentales

### Best Practices

- [async-await-patterns.md](./patterns/best-practices/async-await-patterns.md) - Programacion asincrona
- [dependency-injection.md](./patterns/best-practices/dependency-injection.md) - Inyeccion de dependencias
- [error-handling.md](./patterns/best-practices/error-handling.md) - Manejo de errores
- [code-organization.md](./patterns/best-practices/code-organization.md) - Organizacion de codigo

---

## Siguiente Paso

Despues de entender los patrones fundamentales, consulta las guias de arquitectura:

- [architectures/clean-architecture/](../architectures/clean-architecture/) - Clean Architecture con estos patrones
