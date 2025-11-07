# Repositories - Implementaciones de Repositorios

## Propósito

Esta carpeta contiene las **implementaciones concretas** de las interfaces de repositorio definidas en `{ProjectName}.domain/interfaces/repositories/`.

## Responsabilidades

1. ✅ Implementar interfaces `IRepository<T, TId>` e `IReadOnlyRepository<T, TId>`
2. ✅ Traducir operaciones de dominio a operaciones del ORM elegido
3. ✅ Manejar queries, filtrado, paginación y ordenamiento
4. ✅ Gestionar validación antes de persistir (si aplica)

## Estructura Recomendada

```
repositories/
├── UserRepository.cs          # Implementa IUserRepository
├── ProductRepository.cs       # Implementa IProductRepository
└── OrderRepository.cs         # Implementa IOrderRepository
```

## Ejemplo con NHibernate

```csharp
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;
using NHibernate;

namespace {ProjectName}.infrastructure.repositories;

public class UserRepository : NHRepository<User, int>, IUserRepository
{
    public UserRepository(ISession session, IServiceProvider serviceProvider)
        : base(session, serviceProvider)
    {
    }

    // Métodos específicos de IUserRepository
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _session.Query<User>()
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();
    }
}
```

## Ejemplo con Entity Framework

```csharp
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;
using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.infrastructure.repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
```

## Principios

### 1. No Exponer Detalles del ORM

```csharp
// ❌ INCORRECTO - Expone IQueryable (detalle de implementación)
public IQueryable<User> GetUsers()
{
    return _context.Users;
}

// ✅ CORRECTO - Retorna solo lo necesario
public async Task<IEnumerable<User>> GetUsersAsync()
{
    return await _context.Users.ToListAsync();
}
```

### 2. Métodos Específicos por Repositorio

Cada repositorio puede tener métodos específicos según las necesidades del dominio:

```csharp
public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
}
```

### 3. Repositorios de Solo Lectura

Para consultas complejas, usa repositorios de solo lectura (DAOs):

```csharp
public class UserDaoRepository : IReadOnlyRepository<UserDao, int>
{
    // Solo operaciones de lectura
    // Sin Add, Update, Delete
}
```

## Next Steps

Para implementar tus repositorios con una tecnología específica:

- **NHibernate**: Ver `guides/stack-implementations/nhibernate/01-setup-repositories.md`
- **Entity Framework**: Ver `guides/stack-implementations/entityframework/01-setup-repositories.md`
- **Dapper**: Ver `guides/stack-implementations/dapper/01-setup-repositories.md`
