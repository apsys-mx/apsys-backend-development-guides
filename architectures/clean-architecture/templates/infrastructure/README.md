# {ProjectName}.infrastructure - Capa de Infraestructura

## Propósito

Esta capa contiene las **implementaciones concretas** de las interfaces definidas en la capa de Domain. Es la capa que interactúa con el mundo exterior: bases de datos, APIs externas, servicios de email, almacenamiento, etc.

## Principios

### 1. Implementa Interfaces de Domain

```csharp
// Domain define la interfaz
public interface IUserRepository : IRepository<User, int>
{
    Task<User?> GetByEmailAsync(string email);
}

// Infrastructure la implementa
public class UserRepository : IUserRepository
{
    // Implementación específica (NHibernate, EF, Dapper, etc.)
}
```

### 2. Independencia de Framework

El código de negocio (Domain y Application) **NO debe conocer** qué ORM o tecnología usa Infrastructure.

### 3. Configuración Separada

La configuración de infraestructura debe estar aislada y ser reemplazable.

## Estructura de Carpetas

- **repositories/**: Implementaciones de repositorios
- **persistence/**: Configuración de acceso a datos (ORM, conexiones)
- **services/**: Servicios externos e integraciones
- **configuration/**: Dependency Injection y setup

## Implementación Específica

Esta capa base es **agnóstica de tecnología**. Para implementar con una tecnología específica:

- **NHibernate**: Ver `guides/stack-implementations/nhibernate/`
- **Entity Framework**: Ver `guides/stack-implementations/entityframework/`
- **Dapper**: Ver `guides/stack-implementations/dapper/`

## Ejemplo de Uso

```csharp
// En Application Layer
public class CreateUserUseCase
{
    private readonly IUserRepository _userRepository;

    public CreateUserUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository; // Inyección de dependencia
    }

    public async Task<User> Execute(string name, string email)
    {
        var user = new User(name, email);
        return await _userRepository.AddAsync(user);
    }
}
```

Infrastructure proporciona la implementación concreta de `IUserRepository` sin que Application conozca los detalles.
