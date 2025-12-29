# Repository Pattern

Patrones para acceso a datos. Estos patrones son **agnósticos de ORM** - la implementacion puede ser NHibernate, Entity Framework, Dapper, etc.

## Guias

| Guia | Descripcion |
|------|-------------|
| [repository-pattern.md](./repository-pattern.md) | El patron Repository |
| [unit-of-work-pattern.md](./unit-of-work-pattern.md) | El patron Unit of Work |
| [transactions.md](./transactions.md) | Manejo de transacciones |
| [core-concepts.md](./core-concepts.md) | Conceptos fundamentales |

## Principios Clave

1. **Abstraccion del almacenamiento**
   - El dominio no sabe si es PostgreSQL, SQL Server, o memoria
   - Facilita testing con mocks/fakes

2. **Unit of Work para transacciones**
   - Una transaccion por request HTTP
   - Commit al final, rollback en error

3. **Repositorios por Aggregate Root**
   - Un repositorio por entidad raiz
   - No repositorios para entidades hijas

## Implementaciones

| ORM | Guias |
|-----|-------|
| NHibernate | [stacks/orm/nhibernate/guides/](../../../stacks/orm/nhibernate/guides/) |
| Entity Framework | [stacks/orm/entity-framework/](../../../stacks/orm/entity-framework/) |

## Interfaces Base

```csharp
// IRepository<T, TKey> - CRUD completo
public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey>
{
    T Add(T item);
    T Save(T item);
    void Delete(T item);
}

// IReadOnlyRepository<T, TKey> - Solo lectura
public interface IReadOnlyRepository<T, TKey>
{
    T Get(TKey id);
    IEnumerable<T> Get();
    IEnumerable<T> Get(Expression<Func<T, bool>> query);
}

// IUnitOfWork - Transacciones
public interface IUnitOfWork : IDisposable
{
    void Commit();
    void Rollback();
    void BeginTransaction();
}
```
