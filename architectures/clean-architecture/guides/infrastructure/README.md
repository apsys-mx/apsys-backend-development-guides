# Infrastructure Layer en Clean Architecture

## Rol en la arquitectura

La capa de infraestructura **implementa** las interfaces definidas en Domain. Contiene todo el codigo que interactua con recursos externos.

**Caracteristicas:**
- Depende de Domain (implementa sus interfaces)
- Contiene implementaciones de repositorios, Unit of Work
- Interactua con bases de datos, servicios externos, cache, etc.

## Patrones Fundamentales

### Repository Pattern
→ Ver: [fundamentals/patterns/repository/repository-pattern.md](../../../../fundamentals/patterns/repository/repository-pattern.md)

### Unit of Work Pattern
→ Ver: [fundamentals/patterns/repository/unit-of-work-pattern.md](../../../../fundamentals/patterns/repository/unit-of-work-pattern.md)

### Transacciones
→ Ver: [fundamentals/patterns/repository/transactions.md](../../../../fundamentals/patterns/repository/transactions.md)

### Conceptos Core
→ Ver: [fundamentals/patterns/repository/core-concepts.md](../../../../fundamentals/patterns/repository/core-concepts.md)

## Implementaciones por Stack

### NHibernate (ORM)
→ Ver: [stacks/orm/nhibernate/guides/](../../../../stacks/orm/nhibernate/guides/)

### Entity Framework (ORM)
→ Ver: [stacks/orm/entity-framework/](../../../../stacks/orm/entity-framework/)

### PostgreSQL
→ Ver: [stacks/database/postgresql/](../../../../stacks/database/postgresql/)

### SQL Server
→ Ver: [stacks/database/sqlserver/](../../../../stacks/database/sqlserver/)

### Migraciones (FluentMigrator)
→ Ver: [stacks/database/migrations/fluent-migrator/](../../../../stacks/database/migrations/fluent-migrator/)

### Servicios Externos
→ Ver: [stacks/external-services/](../../../../stacks/external-services/)

## Convenciones en Clean Architecture

1. **Implementar interfaces de Domain**
   ```csharp
   // Domain define
   public interface IUserRepository : IRepository<User, Guid> { }

   // Infrastructure implementa
   public class NHUserRepository : NHRepository<User, Guid>, IUserRepository
   {
       public NHUserRepository(ISession session) : base(session) { }
   }
   ```

2. **Registrar en DI con Scoped lifetime**
   ```csharp
   services.AddScoped<IUserRepository, NHUserRepository>();
   services.AddScoped<IUnitOfWork, NHUnitOfWork>();
   ```

3. **Estructura de carpetas**
   ```
   infrastructure/
   ├── nhibernate/
   │   ├── repositories/
   │   ├── mappers/
   │   └── NHSessionFactory.cs
   └── external/
       └── ...
   ```

## Guia de Inicializacion

→ Ver: [init/04-infrastructure-layer.md](../../init/04-infrastructure-layer.md)
