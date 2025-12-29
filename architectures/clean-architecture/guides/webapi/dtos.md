# DTOs (Data Transfer Objects)

**Version:** 1.0.0
**Last Updated:** 2025-01-15
**Status:** ✅ Complete

---

## Table of Contents

1. [Introducción](#introducción)
2. [¿Qué son los DTOs?](#qué-son-los-dtos)
3. [DTOs vs Entities vs Request/Response Models](#dtos-vs-entities-vs-requestresponse-models)
4. [¿Cuándo usar DTOs?](#cuándo-usar-dtos)
5. [Estructura de un DTO](#estructura-de-un-dto)
6. [Convenciones de Nomenclatura](#convenciones-de-nomenclatura)
7. [Tipos de DTOs](#tipos-de-dtos)
8. [Propiedades y Tipos de Datos](#propiedades-y-tipos-de-datos)
9. [Mapeo con AutoMapper](#mapeo-con-automapper)
10. [Ejemplos Completos](#ejemplos-completos)
11. [Best Practices](#best-practices)
12. [Errores Comunes](#errores-comunes)
13. [Referencias](#referencias)

---

## Introducción

Los **DTOs (Data Transfer Objects)** son objetos simples diseñados exclusivamente para transferir datos entre capas de la aplicación, especialmente entre el backend y el cliente (frontend). Son el "idioma común" que usa tu API para comunicarse con el mundo exterior.

### ¿Por qué usar DTOs?

1. **Separación de responsabilidades**: Las entities de dominio no se exponen directamente
2. **Control sobre la API**: Decides exactamente qué datos exponer
3. **Estabilidad de la API**: Cambios internos no afectan la API pública
4. **Seguridad**: No expones propiedades sensibles o de infraestructura
5. **Optimización**: Solo envías los datos necesarios al cliente

---

## ¿Qué son los DTOs?

Un DTO es una clase simple (POCO - Plain Old CLR Object) que contiene **solo propiedades**, sin lógica de negocio.

### Características de un DTO

✅ **Solo propiedades públicas** con getters y setters
✅ **Sin lógica de negocio** (sin métodos, sin validación)
✅ **Serializable a JSON** (para API REST)
✅ **Estructura plana o simple** (evita anidación compleja)
✅ **Inmutable en propósito** (aunque técnicamente mutable)

### Ejemplo Básico

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The full name of the user
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

---

## DTOs vs Entities vs Request/Response Models

### Comparación Rápida

| Aspecto | Entity | DTO | Request/Response Model |
|---------|--------|-----|----------------------|
| **Capa** | Domain | WebApi | WebApi |
| **Propósito** | Lógica de negocio | Transferencia de datos | Contrato de API |
| **Lógica** | ✅ Sí (validación, métodos) | ❌ No | ❌ No |
| **Herencia** | Sí (AbstractDomainObject) | No (POCO) | No (POCO) |
| **Relaciones** | Sí (navegación) | No (datos planos) | No (datos planos) |
| **Serialización** | ❌ No | ✅ Sí | ✅ Sí |
| **Expuesto en API** | ❌ Nunca | ✅ Sí | ✅ Sí |
| **NHibernate** | ✅ Mapeado | ❌ No | ❌ No |

### Entity (Domain Layer)

```csharp
// Domain/Entities/User.cs
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();  // ← Relación compleja
    public virtual string UserId { get; set; } = string.Empty;  // ← Propiedad interna

    public User(string email, string name) { /* constructor */ }

    public override IValidator GetValidator() => new UserValidator();  // ← Lógica
}
```

### DTO (WebApi Layer)

```csharp
// WebApi/dtos/UserDto.cs
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();  // ← Solo nombres
    // NO expone UserId (propiedad interna)
    // NO tiene métodos de lógica
}
```

### Request/Response Models (WebApi Layer)

```csharp
// WebApi/features/users/models/CreateUserModel.cs
public class CreateUserModel
{
    public class Request  // ← Entrada del endpoint
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Response  // ← Salida del endpoint
    {
        public UserDto User { get; set; } = new UserDto();  // ← Contiene DTO
    }
}
```

### Diagrama de Flujo

```
┌─────────────────────────────────────────────┐
│  Cliente (Frontend)                         │
└──────────────┬──────────────────────────────┘
               │ JSON Request
               ▼
┌─────────────────────────────────────────────┐
│  Request Model                              │  ← Entrada del endpoint
│  (CreateUserModel.Request)                  │
└──────────────┬──────────────────────────────┘
               │ AutoMapper
               ▼
┌─────────────────────────────────────────────┐
│  Command (Application Layer)                │
│  (CreateUserUseCase.Command)                │
└──────────────┬──────────────────────────────┘
               │ Ejecuta lógica
               ▼
┌─────────────────────────────────────────────┐
│  Entity (Domain Layer)                      │  ← Lógica de negocio
│  (User)                                     │
└──────────────┬──────────────────────────────┘
               │ AutoMapper
               ▼
┌─────────────────────────────────────────────┐
│  DTO                                        │  ← Datos expuestos
│  (UserDto)                                  │
└──────────────┬──────────────────────────────┘
               │ Dentro de Response
               ▼
┌─────────────────────────────────────────────┐
│  Response Model                             │  ← Salida del endpoint
│  (CreateUserModel.Response)                 │
└──────────────┬──────────────────────────────┘
               │ JSON Response
               ▼
┌─────────────────────────────────────────────┐
│  Cliente (Frontend)                         │
└─────────────────────────────────────────────┘
```

---

## ¿Cuándo usar DTOs?

### ✅ USA DTOs cuando:

1. **Expones datos al cliente**
   ```csharp
   public class Response
   {
       public UserDto User { get; set; }  // ✅ DTO en Response
   }
   ```

2. **Devuelves colecciones**
   ```csharp
   public class Response : GetManyAndCountResultDto<UserDto>  // ✅ Lista de DTOs
   {
   }
   ```

3. **Necesitas transformar relaciones complejas**
   ```csharp
   // Entity tiene: IList<Role> Roles
   // DTO tiene: IEnumerable<string> Roles  ← Solo nombres
   ```

4. **Quieres ocultar propiedades internas**
   ```csharp
   // Entity tiene: string UserId (Auth0 ID)
   // DTO NO expone UserId  ← Seguridad
   ```

5. **Optimizas el payload JSON**
   ```csharp
   // Solo envías lo que el cliente necesita
   ```

### ❌ NO uses DTOs cuando:

1. **Comunicación interna entre capas**
   ```csharp
   ❌ Application Layer → Infrastructure Layer (usa Entities)
   ❌ Domain Layer → Application Layer (usa Entities)
   ```

2. **Entrada de endpoints**
   ```csharp
   ❌ public class Request : UserDto { }  // Usa Request Model
   ```

3. **Lógica de negocio**
   ```csharp
   ❌ public class UserDto
   {
       public void Validate() { }  // DTOs NO tienen lógica
   }
   ```

---

## Estructura de un DTO

### Template Básico

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for {Entity} information
/// </summary>
public class {Entity}Dto
{
    /// <summary>
    /// The unique identifier of the {entity}
    /// </summary>
    public Guid Id { get; set; }

    // Propiedades primitivas
    public string PropertyName { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Propiedades opcionales
    public string? OptionalProperty { get; set; }

    // Colecciones simples
    public IEnumerable<string> Items { get; set; } = Enumerable.Empty<string>();
}
```

### Reglas de Estructura

1. **Namespace**: `{proyecto}.webapi.dtos`
2. **Documentación XML**: Obligatoria para todas las propiedades
3. **Sin constructores**: Deja el constructor por defecto
4. **Sin métodos**: Solo propiedades
5. **Sin validación**: Eso va en Request Validators
6. **Sin lógica**: Es solo un contenedor de datos

---

## Convenciones de Nomenclatura

### Patrón de Naming

```
{Entity}Dto
```

### Ejemplos

```csharp
✅ UserDto
✅ TechnicalStandardDto
✅ PrototypeDto
✅ OrderDto
✅ InvoiceDto

❌ User (confunde con Entity)
❌ UserDataTransferObject (muy largo)
❌ UserDTO (mayúsculas incorrectas)
❌ UserViewModel (esto es otro patrón)
```

### DTOs Genéricos

```csharp
✅ GetManyAndCountResultDto<T>
✅ PagedResultDto<T>
✅ ErrorResponseDto

// Uso:
public class Response : GetManyAndCountResultDto<UserDto> { }
```

---

## Tipos de DTOs

### 1. DTOs Simples (Entity-based)

Representan una entidad del dominio:

```csharp
/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

### 2. DTOs Compuestos

Contienen otros DTOs:

```csharp
/// <summary>
/// Data Transfer Object for Order with related data
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }

    // DTO anidado
    public UserDto Customer { get; set; } = new UserDto();

    // Colección de DTOs
    public IEnumerable<OrderItemDto> Items { get; set; } = Enumerable.Empty<OrderItemDto>();
}
```

### 3. DTOs Genéricos

Reutilizables para diferentes tipos:

```csharp
/// <summary>
/// Data transfer object for GetManyAndCountResult<T> class
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class GetManyAndCountResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public long Count { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortBy { get; set; } = string.Empty;
    public string SortCriteria { get; set; } = string.Empty;
}

// Uso:
GetManyAndCountResultDto<UserDto>
GetManyAndCountResultDto<TechnicalStandardDto>
```

### 4. DTOs de Solo Lectura (Read-only)

Para consultas que no corresponden a una entidad única:

```csharp
/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveOrders { get; set; }
    public decimal Revenue { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

---

## Propiedades y Tipos de Datos

### Tipos Primitivos

```csharp
public class TechnicalStandardDto
{
    // Identificador único
    public Guid Id { get; set; }

    // Strings - siempre inicializar
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    // Números
    public int Version { get; set; }
    public decimal Price { get; set; }
    public long Downloads { get; set; }

    // Booleanos
    public bool IsActive { get; set; }
    public bool IsPublished { get; set; }

    // Fechas
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTimeOffset PublishedDate { get; set; }
}
```

### Tipos Nullable

```csharp
public class PrototypeDto
{
    public Guid Id { get; set; }

    // Nullable para valores opcionales
    public string? Number { get; set; }
    public string? Status { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? Version { get; set; }
}
```

### Colecciones

```csharp
public class UserDto
{
    // Colecciones de primitivos
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<Guid> PermissionIds { get; set; } = Enumerable.Empty<Guid>();

    // Colecciones de DTOs
    public IEnumerable<AddressDto> Addresses { get; set; } = Enumerable.Empty<AddressDto>();
}
```

**💡 Recomendación**: Usa `IEnumerable<T>` en lugar de `List<T>` para DTOs. Es más genérico y comunica que es de solo lectura.

### DTOs Anidados

```csharp
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    // DTO anidado - solo cuando es necesario
    public UserDto Customer { get; set; } = new UserDto();
    public AddressDto ShippingAddress { get; set; } = new AddressDto();
}
```

**⚠️ Advertencia**: Evita anidar demasiado profundo. Máximo 2-3 niveles.

---

## Mapeo con AutoMapper

### Profile de Mapeo

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for User entity and UserDto.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO (salida)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));

        // DTO usado en Response
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
    }
}
```

### Mapeo Simple (Propiedades coinciden)

```csharp
// Cuando las propiedades tienen los mismos nombres
CreateMap<TechnicalStandard, TechnicalStandardDto>();
// AutoMapper mapea automáticamente: Id → Id, Name → Name, etc.
```

### Mapeo Complejo (Transformación)

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.Roles,
        opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)))  // IList<Role> → IEnumerable<string>
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));  // Combinar propiedades
```

### Mapeo con Lógica Personalizada

```csharp
CreateMap<Prototype, PrototypeDto>()
    .ForMember(dest => dest.Status,
        opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
    .ForMember(dest => dest.DaysUntilExpiration,
        opt => opt.MapFrom(src => (src.ExpirationDate - DateTime.Now).Days));
```

### Uso en Endpoint

```csharp
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        var result = await command.ExecuteAsync(ct);

        if (result.IsFailed) { /* error handling */ }

        // Entity → DTO → Response
        var response = _mapper.Map<GetUserModel.Response>(result.Value);
        await Send.OkAsync(response, ct);
    }
}
```

---

## Ejemplos Completos

### 1. DTO Simple - UserDto

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for User information
/// </summary>
public class UserDto
{
    /// <summary>
    /// The unique identifier of the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The full name of the user
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

**Entity correspondiente**:

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();  // ← Relación
    public virtual string UserId { get; set; } = string.Empty;  // ← NO expuesto en DTO

    public User(string email, string name) { /* ... */ }
    public override IValidator GetValidator() => new UserValidator();
}
```

**Mapeo**:

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.Roles,
        opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
```

---

### 2. DTO Completo - TechnicalStandardDto

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for Technical Standards information
/// </summary>
public class TechnicalStandardDto
{
    /// <summary>
    /// The unique identifier of the technical standard
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique code of the technical standard.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the technical standard.
    /// This is a descriptive name and is required.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edition or version of the technical standard.
    /// This property is required and typically indicates the publication or revision version.
    /// </summary>
    public string Edition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the technical standard.
    /// Typical values are "Active" or "Deprecated".
    /// This property is required.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the technical standard.
    /// This property is required.
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
```

---

### 3. DTO con Nullable - PrototypeDto

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Represents a data transfer object (DTO) for a prototype entity, containing key details such as identifiers,
/// dates, and status information.
/// </summary>
/// <remarks>
/// This class is typically used to transfer prototype-related data between application layers.
/// It includes properties for uniquely identifying the prototype, tracking its issue and expiration dates, and
/// maintaining its current status.
/// </remarks>
public class PrototypeDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the number associated with the entity.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Gets or sets the date when the issue was created or recorded.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the item.
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the current status of the operation.
    /// </summary>
    public string? Status { get; set; }
}
```

---

### 4. DTO Genérico - GetManyAndCountResultDto<T>

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data transfer object for GetManyAndCountResult<T> class
/// This class provides a container for transferring paginated data to the client.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class GetManyAndCountResultDto<T>
{
    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Gets or sets the total count of records that match the query criteria.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based indexing).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the name of the field used for sorting.
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction (e.g., "asc" or "desc").
    /// </summary>
    public string SortCriteria { get; set; } = string.Empty;
}
```

**Uso en Response Model**:

```csharp
public class GetManyAndCountModel
{
    public class Response : GetManyAndCountResultDto<UserDto>
    {
        // Hereda todas las propiedades
    }
}
```

---

### 5. DTO Compuesto - OrderDto

```csharp
namespace hashira.stone.backend.webapi.dtos;

/// <summary>
/// Data Transfer Object for Order information with related entities
/// </summary>
public class OrderDto
{
    /// <summary>
    /// The unique identifier of the order
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// The date when the order was created
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// The total amount of the order
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// The current status of the order
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The customer who placed the order
    /// </summary>
    public UserDto Customer { get; set; } = new UserDto();

    /// <summary>
    /// The items in the order
    /// </summary>
    public IEnumerable<OrderItemDto> Items { get; set; } = Enumerable.Empty<OrderItemDto>();
}

/// <summary>
/// Data Transfer Object for Order Item information
/// </summary>
public class OrderItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
```

---

## Best Practices

### ✅ DO

1. **Mantener DTOs simples y planos**
   ```csharp
   ✅ public class UserDto
   {
       public Guid Id { get; set; }
       public string Name { get; set; } = string.Empty;
       public string Email { get; set; } = string.Empty;
   }
   ```

2. **Inicializar strings con string.Empty**
   ```csharp
   ✅ public string Name { get; set; } = string.Empty;
   ```

3. **Inicializar colecciones con Enumerable.Empty<T>()**
   ```csharp
   ✅ public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
   ```

4. **Usar nullable solo para valores opcionales**
   ```csharp
   ✅ public string? OptionalField { get; set; }  // Realmente opcional
   ```

5. **Documentar todas las propiedades**
   ```csharp
   ✅ /// <summary>
   /// The unique identifier of the user
   /// </summary>
   public Guid Id { get; set; }
   ```

6. **Usar IEnumerable<T> para colecciones**
   ```csharp
   ✅ public IEnumerable<UserDto> Users { get; set; }
   ```

7. **Ocultar propiedades internas**
   ```csharp
   ✅ // Entity tiene UserId (Auth0)
   // DTO NO expone UserId
   ```

8. **Simplificar relaciones complejas**
   ```csharp
   ✅ // Entity: IList<Role> Roles
   // DTO: IEnumerable<string> Roles (solo nombres)
   ```

9. **Crear DTOs específicos por caso de uso**
   ```csharp
   ✅ UserSummaryDto  // Para listados
   ✅ UserDetailDto   // Para detalles completos
   ```

10. **Usar AutoMapper para conversión**
    ```csharp
    ✅ CreateMap<User, UserDto>();
    var dto = _mapper.Map<UserDto>(user);
    ```

---

### ❌ DON'T

1. **No agregar lógica de negocio**
   ```csharp
   ❌ public class UserDto
   {
       public void Validate() { }  // ❌ DTOs no tienen lógica
       public bool IsValid() { }   // ❌
   }
   ```

2. **No dejar strings sin inicializar**
   ```csharp
   ❌ public string Name { get; set; }  // null por defecto
   ```

3. **No usar List<T> directamente**
   ```csharp
   ❌ public List<UserDto> Users { get; set; }
   ✅ public IEnumerable<UserDto> Users { get; set; }
   ```

4. **No exponer entities directamente**
   ```csharp
   ❌ public class Response
   {
       public User User { get; set; }  // ❌ Entity
   }

   ✅ public class Response
   {
       public UserDto User { get; set; }  // ✅ DTO
   }
   ```

5. **No anidar demasiado profundo**
   ```csharp
   ❌ public class OrderDto
   {
       public CustomerDto Customer { get; set; }
       // Customer tiene AddressDto
       // AddressDto tiene CountryDto
       // CountryDto tiene RegionDto  ← Demasiado profundo!
   }
   ```

6. **No incluir propiedades de infraestructura**
   ```csharp
   ❌ public class UserDto
   {
       public string ConnectionString { get; set; }  // ❌ Seguridad
       public string DatabaseId { get; set; }        // ❌ Implementación
   }
   ```

7. **No usar herencia compleja**
   ```csharp
   ❌ public class UserDto : BaseDto<User>  // ❌ Complejo

   ✅ public class UserDto  // ✅ Simple POCO
   ```

8. **No mezclar Request/Response Models con DTOs**
   ```csharp
   ❌ public class CreateUserRequest : UserDto { }  // ❌ Confuso

   ✅ public class Request { }  // ✅ Separado
   ✅ public class UserDto { }  // ✅ Separado
   ```

9. **No usar DTOs para comunicación interna**
   ```csharp
   ❌ // Application → Infrastructure
   var result = repository.GetByDto(userDto);  // ❌ Usa Entity

   ✅ var result = repository.GetById(userId);  // ✅ Usa ID o Entity
   ```

10. **No incluir validación**
    ```csharp
    ❌ public class UserDto : IValidatable
    {
        public ValidationResult Validate() { }  // ❌
    }
    ```

---

## Errores Comunes

### Error 1: Exponer Entity en lugar de DTO

```csharp
❌ public class GetUserModel
{
    public class Response
    {
        public User User { get; set; }  // ❌ Entity expuesta
    }
}
```

**Solución**: Siempre usar DTO:

```csharp
✅ public class Response
{
    public UserDto User { get; set; } = new UserDto();
}
```

---

### Error 2: DTOs con Lógica

```csharp
❌ public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsEmailValid()  // ❌ Lógica en DTO
    {
        return Email.Contains("@");
    }
}
```

**Solución**: DTOs solo contienen datos:

```csharp
✅ public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Sin métodos de lógica
}
```

---

### Error 3: No Inicializar Colecciones

```csharp
❌ public class UserDto
{
    public IEnumerable<string> Roles { get; set; }  // null
}
```

**Solución**: Inicializar con colección vacía:

```csharp
✅ public class UserDto
{
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
```

---

### Error 4: Anidación Excesiva

```csharp
❌ public class OrderDto
{
    public CustomerDto Customer { get; set; }
    // CustomerDto tiene AddressDto
    // AddressDto tiene CityDto
    // CityDto tiene StateDto
    // StateDto tiene CountryDto  ← 5 niveles!
}
```

**Solución**: Aplanar o usar IDs:

```csharp
✅ public class OrderDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;
}
```

---

### Error 5: Usar DTOs en Comunicación Interna

```csharp
❌ // Application Layer
public async Task<UserDto> CreateUserAsync(UserDto userDto)  // ❌
{
    var user = _mapper.Map<User>(userDto);
    await _repository.AddAsync(user);
    return _mapper.Map<UserDto>(user);
}
```

**Solución**: Usar Commands/Entities internamente:

```csharp
✅ // Application Layer
public async Task<Result<User>> ExecuteAsync(Command command)  // ✅
{
    var user = new User(command.Email, command.Name);
    await _repository.AddAsync(user);
    return Result.Ok(user);
}

// WebApi convierte a DTO
var response = _mapper.Map<UserDto>(result.Value);
```

---

### Error 6: No Ocultar Propiedades Sensibles

```csharp
❌ public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // ❌ Seguridad!
    public string UserId { get; set; } = string.Empty;        // ❌ Auth0 ID expuesto
}
```

**Solución**: Solo exponer lo necesario:

```csharp
✅ public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // NO expone PasswordHash ni UserId
}
```

---

### Error 7: Mapeo Manual en Endpoint

```csharp
❌ public override async Task HandleAsync(Request req, CancellationToken ct)
{
    var result = await command.ExecuteAsync(ct);

    var dto = new UserDto  // ❌ Mapeo manual
    {
        Id = result.Value.Id,
        Name = result.Value.Name,
        Email = result.Value.Email
    };
}
```

**Solución**: Usar AutoMapper:

```csharp
✅ var dto = _mapper.Map<UserDto>(result.Value);
```

---

## Referencias

### Documentación Oficial

- **Martin Fowler - DTO Pattern**: https://martinfowler.com/eaaCatalog/dataTransferObject.html
- **AutoMapper**: https://docs.automapper.org/
- **JSON Serialization**: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview

### Guías Relacionadas

- [Request/Response Models](./request-response-models.md)
- [AutoMapper Profiles](./automapper-profiles.md)
- [Domain Entities](../domain-layer/entities.md)
- [FastEndpoints Basics](./fastendpoints-basics.md)

### Archivos de Referencia del Proyecto

**DTOs**:
- [UserDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\UserDto.cs)
- [TechnicalStandardDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\TechnicalStandardDto.cs)
- [PrototypeDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\PrototypeDto.cs)
- [GetManyAndCountResultDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\GetManyAndCountResultDto.cs)

**Entities (para comparación)**:
- [User.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\entities\User.cs)

**Mapping Profiles**:
- [UserMappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\UserMappingProfile.cs)

---

## Changelog

### Version 1.0.0 (2025-01-15)
- ✅ Initial release
- ✅ Complete documentation of DTO pattern
- ✅ Comparison: DTOs vs Entities vs Request/Response Models
- ✅ 5 complete working examples from reference project
- ✅ Mapping with AutoMapper
- ✅ Best practices and common errors
- ✅ Detailed diagrams and flow explanations

---

**Siguiente Guía**: [AutoMapper Profiles](./automapper-profiles.md)

[◀️ Volver al WebApi Layer](./README.md) | [🏠 Inicio](../README.md)
