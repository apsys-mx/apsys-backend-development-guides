# DTOs - Data Transfer Objects

## Propósito

Esta carpeta contiene los **Data Transfer Objects (DTOs)** utilizados para transferir datos entre la API y los clientes. Los DTOs son objetos simples diseñados específicamente para la serialización/deserialización HTTP.

## Responsabilidades

1. ✅ Definir el contrato de input (requests) de la API
2. ✅ Definir el contrato de output (responses) de la API
3. ✅ Agrupar datos relacionados para transferencia
4. ✅ Simplificar la estructura de datos para clientes
5. ✅ Proteger el dominio de cambios en la API

## Estructura Recomendada

```
dtos/
├── common/
│   ├── PaginatedResultDto.cs
│   ├── ErrorResponseDto.cs
│   └── ValidationErrorDto.cs
├── users/
│   ├── CreateUserRequest.cs
│   ├── UpdateUserRequest.cs
│   ├── UserResponse.cs
│   └── UserListItemResponse.cs
└── products/
    ├── CreateProductRequest.cs
    ├── ProductResponse.cs
    └── ProductSearchRequest.cs
```

## Ejemplo: Request DTO

```csharp
namespace {ProjectName}.webapi.dtos.users;

/// <summary>
/// Request DTO for creating a new user
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

## Ejemplo: Response DTO

```csharp
namespace {ProjectName}.webapi.dtos.users;

/// <summary>
/// Response DTO for user data
/// </summary>
public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // NO incluir datos sensibles (passwords, tokens, etc.)
    // NO incluir propiedades complejas innecesarias
}
```

## Ejemplo: DTO Genérico para Paginación

```csharp
namespace {ProjectName}.webapi.dtos.common;

/// <summary>
/// Generic DTO for paginated results
/// </summary>
public class PaginatedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

## Ejemplo: Error Response DTO

```csharp
namespace {ProjectName}.webapi.dtos.common;

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

## Principios

### 1. DTOs ≠ Entidades de Domain

Los DTOs y las entidades de Domain son objetos diferentes con propósitos diferentes:

```csharp
// ❌ INCORRECTO - Exponer entidad directamente
public class UserResponse : User
{
    // Hereda todo de User, incluyendo detalles internos
}

// ✅ CORRECTO - DTO independiente
public class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // Solo lo que necesita el cliente
}
```

### 2. No Incluir Lógica de Negocio

Los DTOs son objetos de datos puros (POCOs):

```csharp
// ❌ INCORRECTO - Lógica en DTO
public class UserResponse
{
    public string Name { get; set; }
    public string Email { get; set; }

    public bool IsEmailValid()
    {
        return Email.Contains("@");
    }

    public void CalculateSomething() { }
}

// ✅ CORRECTO - Solo datos
public class UserResponse
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 3. DTOs Simples y Planos

Evitar estructuras complejas y anidadas excesivas:

```csharp
// ❌ INCORRECTO - Demasiado anidado
public class OrderResponse
{
    public Customer Customer { get; set; }
    public List<Product> Products { get; set; }
    public Address ShippingAddress { get; set; }
    // Cada uno con sus propias propiedades complejas
}

// ✅ CORRECTO - Aplanado cuando sea posible
public class OrderResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public string ShippingAddress { get; set; }
}
```

### 4. Usar Records para Immutability

En C# 9+, considera usar `record` para DTOs inmutables:

```csharp
// Request inmutable
public record CreateUserRequest(string Name, string Email, string Password);

// Response inmutable
public record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);
```

### 5. No Incluir Datos Sensibles

```csharp
// ❌ INCORRECTO - Incluye datos sensibles
public class UserResponse
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }        // ❌ NO
    public string PasswordHash { get; set; }    // ❌ NO
    public string ApiKey { get; set; }          // ❌ NO
}

// ✅ CORRECTO - Solo datos seguros
public class UserResponse
{
    public string Name { get; set; }
    public string Email { get; set; }
    // Sin datos sensibles
}
```

## Mapeo entre Domain y DTOs

Usa AutoMapper o mapeo manual:

### Con AutoMapper

```csharp
// En MappingProfile.cs
CreateMap<User, UserResponse>();
CreateMap<CreateUserRequest, CreateUserCommand>();
```

### Mapeo Manual

```csharp
// En el endpoint
var response = new UserResponse
{
    Id = user.Id,
    Name = user.Name,
    Email = user.Email,
    CreatedAt = user.CreatedAt
};
```

## Validación de DTOs

La validación puede hacerse con FluentValidation:

```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
```

## Next Steps

- Implementar DTOs específicos para tus endpoints
- Configurar AutoMapper o implementar mapeo manual
- Agregar validación con FluentValidation si es necesario
