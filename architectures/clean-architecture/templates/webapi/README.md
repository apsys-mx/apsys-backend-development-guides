# {ProjectName}.webapi - Capa de Presentación (WebApi)

## Propósito

Esta capa contiene los **puntos de entrada HTTP** de la aplicación: endpoints, controladores, DTOs, configuración de middleware, etc. Es la capa que expone la funcionalidad de la aplicación al mundo exterior.

## Principios

### 1. Thin Controllers/Endpoints

Los endpoints deben ser delgados, delegando toda la lógica de negocio a la capa de Application:

```csharp
// ✅ CORRECTO - Delega a Application Layer
app.MapPost("/users", async (CreateUserRequest request, IMediator mediator) =>
{
    var command = new CreateUserCommand(request.Email, request.Name);
    var result = await mediator.Send(command);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Errors);
});
```

### 2. DTOs para Input/Output

Nunca exponer entidades de Domain directamente en la API:

```csharp
// ❌ INCORRECTO - Expone entidad de Domain
app.MapGet("/users/{id}", async (int id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return Results.Ok(user); // User es entidad de Domain
});

// ✅ CORRECTO - Usa DTOs
app.MapGet("/users/{id}", async (int id, IMediator mediator) =>
{
    var query = new GetUserQuery(id);
    var result = await mediator.Send(query);

    var dto = new UserResponse
    {
        Id = result.Id,
        Name = result.Name,
        Email = result.Email
    };

    return Results.Ok(dto);
});
```

### 3. Manejo de Errores Centralizado

Usar middleware para manejo global de excepciones.

## Estructura de Carpetas

- **endpoints/**: Controladores o endpoints HTTP
- **dtos/**: Data Transfer Objects para requests/responses
- **configuration/**: Configuración de servicios, DI, middleware
- **Properties/**: Configuración del assembly

## Framework de WebApi

Esta capa base es **agnóstica de framework**. El framework específico se configura en la guía de implementación:

- **FastEndpoints (default)**: Ver `guides/webapi-implementations/fastendpoints/`
- **Minimal APIs**: Ver `guides/webapi-implementations/minimal-apis/`
- **MVC**: Ver `guides/webapi-implementations/mvc/`

## Ejemplo de Uso

### Endpoint Simple (Minimal API)

```csharp
app.MapGet("/api/users/{id}", async (int id, IMediator mediator) =>
{
    var query = new GetUserQuery(id);
    var result = await mediator.Send(query);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
});
```

### Endpoint con FastEndpoints

```csharp
public class GetUserEndpoint : Endpoint<GetUserRequest, UserResponse>
{
    public override void Configure()
    {
        Get("/api/users/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var query = new GetUserQuery(req.Id);
        var result = await query.ExecuteAsync();

        if (result.IsSuccess)
            await SendAsync(result.Value, cancellation: ct);
        else
            await SendNotFoundAsync(ct);
    }
}
```

## Variables de Entorno

El archivo `.env` contiene variables sensibles. **Nunca** lo commites a Git.

Usa `.env.example` como plantilla para nuevos desarrolladores.

## Next Steps

Para implementar con un framework específico:

1. Completar la estructura base (esta guía)
2. Ejecutar guía de implementación según framework elegido
3. Implementar endpoints reales para tus use cases
