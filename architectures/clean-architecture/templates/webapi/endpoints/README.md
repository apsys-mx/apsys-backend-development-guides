# Endpoints - Puntos de Entrada HTTP

## Propósito

Esta carpeta contiene los **endpoints o controladores HTTP** que exponen la funcionalidad de la aplicación. Son los puntos de entrada para las peticiones HTTP.

## Responsabilidades

1. ✅ Recibir requests HTTP y extraer parámetros
2. ✅ Validar input (con FluentValidation u otra librería)
3. ✅ Invocar use cases de la capa de Application
4. ✅ Transformar resultados a DTOs
5. ✅ Retornar responses HTTP apropiadas

## Organización (Vertical Slicing)

Los endpoints se organizan por **feature** (vertical slicing), no por tipo técnico:

```
✅ CORRECTO (vertical slicing):
endpoints/
├── users/
│   ├── CreateUserEndpoint.cs
│   ├── GetUserEndpoint.cs
│   ├── UpdateUserEndpoint.cs
│   └── DeleteUserEndpoint.cs
├── products/
│   ├── CreateProductEndpoint.cs
│   ├── GetProductEndpoint.cs
│   └── ListProductsEndpoint.cs
└── orders/
    ├── CreateOrderEndpoint.cs
    └── GetOrderEndpoint.cs

❌ INCORRECTO (horizontal slicing):
controllers/
├── UsersController.cs         # Todos los métodos de users
├── ProductsController.cs       # Todos los métodos de products
└── OrdersController.cs         # Todos los métodos de orders
```

## Implementación según Framework

El contenido de esta carpeta dependerá del framework elegido:

### Con FastEndpoints (default)

```csharp
public class CreateUserEndpoint : BaseEndpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/api/users");
        Policies("MustBeAuthenticated");
        Summary(s => {
            s.Summary = "Creates a new user";
            s.Description = "Creates a new user with the provided information";
        });
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        try
        {
            var command = new CreateUserCommand(req.Name, req.Email);
            var result = await command.ExecuteAsync();

            if (result.IsFailed)
            {
                await HandleErrorAsync(x => x.Email, result.Errors.First().Message, HttpStatusCode.BadRequest, ct);
                return;
            }

            var response = Mapper.Map<UserResponse>(result.Value);
            await SendAsync(response, 201, ct);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedErrorAsync(ex, ct);
        }
    }
}
```

### Con Minimal APIs

```csharp
public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .WithOpenApi();

        group.MapGet("/{id}", GetUser)
            .WithName("GetUser")
            .WithOpenApi();
    }

    private static async Task<Results<Ok<UserResponse>, BadRequest>> CreateUser(
        CreateUserRequest request,
        IMediator mediator)
    {
        var command = new CreateUserCommand(request.Name, request.Email);
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.BadRequest();
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetUser(
        int id,
        IMediator mediator)
    {
        var query = new GetUserQuery(id);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.NotFound();
    }
}
```

### Con MVC Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Name, request.Email);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Errors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound();
    }
}
```

## Principios

### 1. Endpoints Delgados

```csharp
// ❌ INCORRECTO - Lógica de negocio en endpoint
public async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
{
    if (string.IsNullOrEmpty(req.Email))
        await SendErrorAsync("Email is required", ct);

    var existingUser = await _repository.GetByEmailAsync(req.Email);
    if (existingUser != null)
        await SendErrorAsync("Email already exists", ct);

    var user = new User(req.Name, req.Email);
    await _repository.AddAsync(user);
    await SendAsync(user, ct);
}

// ✅ CORRECTO - Delega a Application Layer
public async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
{
    var command = new CreateUserCommand(req.Name, req.Email);
    var result = await command.ExecuteAsync();

    if (result.IsFailed)
        await HandleErrorAsync(x => x.Email, result.Errors.First().Message, HttpStatusCode.BadRequest, ct);
    else
        await SendAsync(result.Value, 201, ct);
}
```

### 2. Mapeo a DTOs

Nunca retornar entidades de Domain directamente:

```csharp
// ❌ INCORRECTO
return Results.Ok(user); // User es entidad de Domain

// ✅ CORRECTO
var response = Mapper.Map<UserResponse>(user);
return Results.Ok(response);
```

### 3. Códigos de Estado HTTP Apropiados

```csharp
// Crear recurso
await SendAsync(response, 201, ct);  // 201 Created

// Actualizar recurso
await SendAsync(response, 200, ct);  // 200 OK

// Eliminar recurso
await SendNoContentAsync(ct);        // 204 No Content

// Recurso no encontrado
await SendNotFoundAsync(ct);         // 404 Not Found

// Error de validación
await SendAsync(errors, 400, ct);    // 400 Bad Request

// Error de autorización
await SendUnauthorizedAsync(ct);     // 401 Unauthorized
await SendForbiddenAsync(ct);        // 403 Forbidden
```

## Next Steps

Para implementar endpoints con un framework específico:

- **FastEndpoints**: Ver `guides/webapi-implementations/fastendpoints/setup-fastendpoints.md`
- **Minimal APIs**: Ver `guides/webapi-implementations/minimal-apis/setup-minimal-apis.md`
- **MVC**: Ver `guides/webapi-implementations/mvc/setup-mvc.md`
