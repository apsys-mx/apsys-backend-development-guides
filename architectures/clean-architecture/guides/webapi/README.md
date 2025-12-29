# WebAPI Layer en Clean Architecture

## Rol en la arquitectura

La capa WebAPI es el **punto de entrada** de la aplicacion. Expone endpoints HTTP y maneja la comunicacion con clientes.

**Caracteristicas:**
- Depende de Application (usa use cases)
- Depende de Infrastructure (para DI setup)
- Maneja autenticacion/autorizacion
- Transforma DTOs ↔ Entidades

## Guias en esta carpeta

### Autenticacion
→ Ver: [authentication.md](./authentication.md)

### DTOs
→ Ver: [dtos.md](./dtos.md)

### Respuestas de Error
→ Ver: [error-responses.md](./error-responses.md)

## Stack: FastEndpoints

### Guia de Setup
→ Ver: [stacks/webapi/fastendpoints/guides/setup.md](../../../../stacks/webapi/fastendpoints/guides/setup.md)

### Basicos de FastEndpoints
→ Ver: [stacks/webapi/fastendpoints/guides/fastendpoints-basics.md](../../../../stacks/webapi/fastendpoints/guides/fastendpoints-basics.md)

### Request/Response Models
→ Ver: [stacks/webapi/fastendpoints/guides/request-response-models.md](../../../../stacks/webapi/fastendpoints/guides/request-response-models.md)

### Configuracion de Swagger
→ Ver: [stacks/webapi/fastendpoints/guides/swagger-configuration.md](../../../../stacks/webapi/fastendpoints/guides/swagger-configuration.md)

### AutoMapper Profiles
→ Ver: [stacks/webapi/fastendpoints/guides/automapper-profiles.md](../../../../stacks/webapi/fastendpoints/guides/automapper-profiles.md)

## Convenciones en Clean Architecture

1. **Endpoints organizados por feature**
   ```
   features/
   ├── users/
   │   ├── GetUserById/
   │   │   ├── Endpoint.cs
   │   │   ├── Request.cs
   │   │   └── Response.cs
   │   └── CreateUser/
   │       └── ...
   └── organizations/
       └── ...
   ```

2. **Usar Use Cases, no repositorios directamente**
   ```csharp
   // ✅ CORRECTO
   public class GetUserByIdEndpoint : Endpoint<Request, Response>
   {
       private readonly GetUserByIdUseCase _useCase;
   }

   // ❌ INCORRECTO
   public class GetUserByIdEndpoint : Endpoint<Request, Response>
   {
       private readonly IUserRepository _repository;
   }
   ```

3. **DTOs separados de entidades**
   - Request/Response models en WebAPI
   - Entidades en Domain
   - AutoMapper para transformaciones

## Guia de Inicializacion

→ Ver: [init/05-webapi-layer.md](../../init/05-webapi-layer.md)
