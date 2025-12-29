# Application Layer en Clean Architecture

## Rol en la arquitectura

La capa de aplicacion contiene la **logica de orquestacion** (use cases). Coordina el flujo de datos entre Domain e Infrastructure.

**Caracteristicas:**
- Depende solo de Domain
- Define casos de uso (use cases)
- No contiene logica de negocio (esa va en Domain)
- No conoce detalles de infraestructura

## Guias en esta carpeta

### Use Cases
→ Ver: [use-cases.md](./use-cases.md)

### Patrones de Command/Handler
→ Ver: [command-handler-patterns.md](./command-handler-patterns.md)

### Manejo de Errores
→ Ver: [error-handling.md](./error-handling.md)

### Utilidades Comunes
→ Ver: [common-utilities.md](./common-utilities.md)

## Convenciones en Clean Architecture

1. **Use Cases agrupados por feature**
   ```
   usecases/
   ├── users/
   │   ├── GetUserByIdUseCase.cs
   │   ├── GetUsersUseCase.cs
   │   └── CreateUserUseCase.cs
   └── organizations/
       └── GetOrganizationByIdUseCase.cs
   ```

2. **Use interfaces de Domain, no implementaciones**
   ```csharp
   // ✅ CORRECTO
   public class GetUserByIdUseCase
   {
       private readonly IUserRepository _userRepository;
   }

   // ❌ INCORRECTO
   public class GetUserByIdUseCase
   {
       private readonly NHUserRepository _userRepository;
   }
   ```

3. **Un metodo Execute/ExecuteAsync por Use Case**
   ```csharp
   public class GetUserByIdUseCase
   {
       public async Task<User?> ExecuteAsync(Guid id)
       {
           return await _userRepository.GetByIdAsync(id);
       }
   }
   ```

## Guia de Inicializacion

→ Ver: [init/03-application-layer.md](../../init/03-application-layer.md)
