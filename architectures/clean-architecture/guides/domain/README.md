# Domain Layer en Clean Architecture

## Rol en la arquitectura

La capa de dominio es el **nucleo** de Clean Architecture. Contiene la logica de negocio y las reglas del dominio.

**Caracteristicas:**
- No tiene dependencias hacia otras capas
- Define interfaces que implementara Infrastructure
- Contiene entidades, value objects, excepciones y validadores

## Componentes

### Entidades
→ Ver: [fundamentals/patterns/domain-modeling/entities.md](../../../../fundamentals/patterns/domain-modeling/entities.md)

### Validadores (FluentValidation)
→ Ver: [fundamentals/patterns/domain-modeling/validators.md](../../../../fundamentals/patterns/domain-modeling/validators.md)

### Excepciones de Dominio
→ Ver: [fundamentals/patterns/domain-modeling/domain-exceptions.md](../../../../fundamentals/patterns/domain-modeling/domain-exceptions.md)

### Value Objects
→ Ver: [fundamentals/patterns/domain-modeling/value-objects.md](../../../../fundamentals/patterns/domain-modeling/value-objects.md)

### Interfaces de Repositorios
→ Ver: [fundamentals/patterns/domain-modeling/repository-interfaces.md](../../../../fundamentals/patterns/domain-modeling/repository-interfaces.md)

### DAOs
→ Ver: [fundamentals/patterns/domain-modeling/daos.md](../../../../fundamentals/patterns/domain-modeling/daos.md)

## Ejemplos de Entidades
→ Ver: [fundamentals/patterns/domain-modeling/examples/](../../../../fundamentals/patterns/domain-modeling/examples/)

## Convenciones en Clean Architecture

1. **Interfaces en Domain, implementaciones en Infrastructure**
   ```csharp
   // Domain define el contrato
   public interface IUserRepository : IRepository<User, Guid> { }

   // Infrastructure implementa
   public class NHUserRepository : NHRepository<User, Guid>, IUserRepository { }
   ```

2. **Sin dependencias de infraestructura**
   - No usar NHibernate, Entity Framework, etc. directamente
   - Solo referencias a .NET base y FluentValidation

3. **Validadores junto a las entidades**
   ```
   entities/
   ├── User.cs
   └── validators/
       └── UserValidator.cs
   ```

## Guia de Inicializacion

→ Ver: [init/02-domain-layer.md](../../init/02-domain-layer.md)
