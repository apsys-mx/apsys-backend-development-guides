# AutoMapper Profiles

**Version:** 1.0.0
**Last Updated:** 2025-01-15
**Status:** ✅ Complete

---

## Table of Contents

1. [Introducción](#introducción)
2. [¿Qué es AutoMapper?](#qué-es-automapper)
3. [Configuración Inicial](#configuración-inicial)
4. [Profiles](#profiles)
5. [CreateMap - Mapeos Básicos](#createmap---mapeos-básicos)
6. [ForMember - Configuraciones Personalizadas](#formember---configuraciones-personalizadas)
7. [Mapeo de Colecciones](#mapeo-de-colecciones)
8. [Mapeos Genéricos](#mapeos-genéricos)
9. [ReverseMap](#reversemap)
10. [Configuraciones Avanzadas](#configuraciones-avanzadas)
11. [ProjectTo - Optimización de Queries](#projectto---optimización-de-queries)
12. [Ejemplos Completos del Proyecto](#ejemplos-completos-del-proyecto)
13. [Best Practices](#best-practices)
14. [Errores Comunes](#errores-comunes)
15. [Referencias](#referencias)

---

## Introducción

**AutoMapper** es una librería de mapeo objeto-a-objeto que elimina la necesidad de escribir código repetitivo para transferir datos entre objetos de diferentes tipos. En nuestra arquitectura, AutoMapper se usa principalmente para mapear entre:

- **Entities** (Domain) → **DTOs** (WebApi)
- **Request Models** → **Commands/Queries** (Application)
- **Entities** → **Response Models**

### ¿Por qué AutoMapper?

1. **Reduce boilerplate**: Elimina mapeo manual propiedad por propiedad
2. **Type-safe**: Errores de mapeo en tiempo de compilación
3. **Testeable**: Validación de configuración con `AssertConfigurationIsValid()`
4. **Expresivo**: Configuración declarativa fácil de leer
5. **Performante**: Optimizado para producción

---

## ¿Qué es AutoMapper?

AutoMapper es una librería de **convención sobre configuración**. Mapea automáticamente propiedades con el mismo nombre entre objetos source y destination.

### Ejemplo Simple

```csharp
// Entities
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// DTO
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Configuración
CreateMap<User, UserDto>();

// Uso
var user = new User { Id = Guid.NewGuid(), Name = "John", Email = "john@example.com" };
var dto = mapper.Map<UserDto>(user);
// dto.Id, dto.Name, dto.Email se mapean automáticamente
```

### Cuándo NO usar AutoMapper

❌ **No usar para**:
- Mapeo trivial de 1-2 propiedades (más rápido hacerlo manual)
- Lógica de transformación compleja (mejor usar métodos dedicados)
- Performance crítica en hot paths (evaluar overhead)

✅ **Usar para**:
- Mapeo de múltiples propiedades (>3)
- Transformaciones repetitivas
- DTOs de API
- ViewModels

---

## Configuración Inicial

### 1. Instalación

```bash
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

**Versión en nuestro proyecto**: AutoMapper 14.0.0

### 2. Registro en Dependency Injection

```csharp
// Program.cs
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Registrar AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);
// Esto escanea el assembly y registra todos los Profiles automáticamente

var app = builder.Build();
```

### 3. Inyección en Endpoints

```csharp
public class GetUserEndpoint(AutoMapper.IMapper mapper)
    : BaseEndpoint<GetUserModel.Request, GetUserModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override async Task HandleAsync(GetUserModel.Request req, CancellationToken ct)
    {
        // Usar mapper
        var dto = _mapper.Map<UserDto>(user);
    }
}
```

---

## Profiles

Los **Profiles** son clases que heredan de `AutoMapper.Profile` y contienen configuraciones de mapeo agrupadas lógicamente.

### ¿Por qué usar Profiles?

1. **Organización**: Agrupa mapeos relacionados
2. **Modularidad**: Cada feature tiene su propio profile
3. **Descubrimiento automático**: `AddAutoMapper()` los encuentra automáticamente
4. **Testeable**: Se pueden validar independientemente

### Estructura de un Profile

```csharp
using AutoMapper;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for {Entity} entity and {Entity}Dto.
/// </summary>
public class {Entity}MappingProfile : Profile
{
    public {Entity}MappingProfile()
    {
        // Configurar mapeos aquí
        CreateMap<Entity, EntityDto>();
        CreateMap<Request, Command>();
    }
}
```

### Ubicación en el Proyecto

```
webapi/
└── mappingprofiles/
    ├── MappingProfile.cs              ← Mapeos genéricos
    ├── UserMappingProfile.cs          ← User-specific
    ├── TechnicalStandardMappingProfile.cs
    └── PrototypeMappingProfile.cs
```

### Convención de Naming

```
{Entity}MappingProfile
```

Ejemplos:
- ✅ `UserMappingProfile`
- ✅ `TechnicalStandardMappingProfile`
- ✅ `PrototypeMappingProfile`
- ❌ `UserProfile` (confuso con otros tipos de Profile)
- ❌ `UserMapper` (no es un mapper, es un profile)

---

## CreateMap - Mapeos Básicos

### Sintaxis Base

```csharp
CreateMap<TSource, TDestination>();
```

### Mapeo Simple (Propiedades Coinciden)

```csharp
public class TechnicalStandardMappingProfile : Profile
{
    public TechnicalStandardMappingProfile()
    {
        // Mapeo automático - todas las propiedades coinciden por nombre
        CreateMap<TechnicalStandard, TechnicalStandardDto>();
    }
}
```

**Reglas de Mapeo Automático**:
- Propiedades con el **mismo nombre** se mapean automáticamente
- Tipos compatibles se convierten automáticamente (int → string, etc.)
- Propiedades no encontradas en destination se ignoran
- Propiedades no mapeadas en destination causan error de validación

### Múltiples Mapeos

```csharp
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO
        CreateMap<User, UserDto>();

        // DAO → DTO
        CreateMap<UserDao, UserDto>();

        // Request → Command
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
        CreateMap<GetUserModel.Request, GetUserUseCase.Command>();

        // Entity → Response
        CreateMap<User, CreateUserModel.Response>();
        CreateMap<User, GetUserModel.Response>();
    }
}
```

---

## ForMember - Configuraciones Personalizadas

`ForMember()` permite configurar el mapeo de propiedades individuales cuando no coinciden automáticamente.

### Sintaxis

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.PropertyName, opt => opt.{Configuration});
```

### MapFrom - Mapeo Personalizado

#### Mapeo Simple

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
```

#### Mapeo de Navegación

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.Roles,
        opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
// IList<Role> → IEnumerable<string>
```

#### Mapeo Anidado

```csharp
CreateMap<User, CreateUserModel.Response>()
    .ForMember(dest => dest.User,
        opt => opt.MapFrom(src => src));
// Response.User = mapper.Map<UserDto>(src)
```

### Ignore - Ignorar Propiedades

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.PropertyToIgnore, opt => opt.Ignore());
```

**Uso común**: Propiedades calculadas o de solo escritura

### NullSubstitute - Valor por Defecto

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.Value,
        opt => opt.NullSubstitute("Default Value"));
```

Cuando `src.Value == null`, usa "Default Value".

### Conditional - Mapeo Condicional

```csharp
CreateMap<Source, Destination>()
    .ForMember(dest => dest.Value,
        opt => opt.Condition(src => src.Value != null));
```

Solo mapea si la condición es verdadera.

### MapFrom con Lógica Compleja

```csharp
CreateMap<Prototype, PrototypeDto>()
    .ForMember(dest => dest.Status,
        opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
    .ForMember(dest => dest.DaysUntilExpiration,
        opt => opt.MapFrom(src => (src.ExpirationDate - DateTime.Now).Days));
```

---

## Mapeo de Colecciones

AutoMapper mapea colecciones automáticamente cuando el tipo de elemento es compatible.

### Colecciones Simples

```csharp
// Automático
CreateMap<User, UserDto>();

// Uso
List<User> users = repository.GetAll();
List<UserDto> dtos = mapper.Map<List<UserDto>>(users);
// O
IEnumerable<UserDto> dtos = mapper.Map<IEnumerable<UserDto>>(users);
```

### Transformación de Elementos

```csharp
// User.Roles es IList<Role>
// UserDto.Roles es IEnumerable<string>

CreateMap<User, UserDto>()
    .ForMember(dest => dest.Roles,
        opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
```

### Colecciones Genéricas Anidadas

```csharp
// GetManyAndCountResultDto<DAO> → GetManyAndCountResultDto<DTO>
CreateMap<GetManyAndCountResultDto<TechnicalStandardDao>,
          GetManyAndCountResultDto<TechnicalStandardDto>>()
    .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
    .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Count))
    .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.PageNumber))
    .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
    .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.SortBy))
    .ForMember(dest => dest.SortCriteria, opt => opt.MapFrom(src => src.SortCriteria));
```

---

## Mapeos Genéricos

Para mapear tipos genéricos, usa `typeof()`.

### Ejemplo del Proyecto

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeo genérico de GetManyAndCountResult<T> a GetManyAndCountResultDto<T>
        CreateMap(typeof(GetManyAndCountResult<>), typeof(GetManyAndCountResultDto<>))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortBy),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.SortBy;
                }))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortCriteria),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.Criteria switch
                    {
                        SortingCriteriaType.Ascending => "asc",
                        SortingCriteriaType.Descending => "desc",
                        _ => null
                    };
                }));
    }
}
```

### Explicación

- `typeof(GetManyAndCountResult<>)`: Tipo genérico sin especificar `T`
- `nameof()`: Obtiene el nombre de la propiedad como string
- Lambdas con múltiples parámetros: `(src, dest, destMember, context)`
- Pattern matching para enums

---

## ReverseMap

`ReverseMap()` crea automáticamente el mapeo inverso.

### Ejemplo

```csharp
// Solo definir una dirección
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
    .ReverseMap();

// Ahora también existe: UserDto → User
// AutoMapper infiere: UserDto.FullName → User.FirstName y User.LastName
```

### Cuándo NO usar ReverseMap

❌ **No usar** cuando:
- El mapeo inverso no tiene sentido lógicamente
- Las transformaciones no son reversibles
- Diferentes reglas de validación en cada dirección

✅ **Usar** cuando:
- El mapeo es simétrico
- Necesitas bidireccionalidad simple

**Nota**: En nuestro proyecto **raramente usamos ReverseMap** porque:
- Request → Command (solo una dirección)
- Entity → DTO (solo una dirección)

---

## Configuraciones Avanzadas

### Custom Value Resolvers

Para lógica de mapeo compleja, crea un `IValueResolver`.

```csharp
// Resolver
public class CustomResolver : IValueResolver<Source, Destination, int>
{
    public int Resolve(Source source, Destination destination, int destMember, ResolutionContext context)
    {
        return source.Value1 + source.Value2;
    }
}

// Configuración
CreateMap<Source, Destination>()
    .ForMember(dest => dest.Total, opt => opt.MapFrom<CustomResolver>());
```

### Mapeo con Contexto

```csharp
// Pasar datos adicionales durante el mapeo
var dto = mapper.Map<UserDto>(user, opt =>
{
    opt.Items["CurrentUserId"] = currentUserId;
});

// Usar en resolver
public class CustomResolver : IValueResolver<Source, Destination, string>
{
    public string Resolve(Source source, Destination dest, string destMember, ResolutionContext context)
    {
        var userId = context.Items["CurrentUserId"];
        return $"Modified by {userId}";
    }
}
```

### Constructores Personalizados

```csharp
CreateMap<Source, Destination>()
    .ConstructUsing(src => new Destination(src.Value));
```

### ForCtorParam - Mapeo a Parámetros de Constructor

```csharp
public class SourceDto
{
    public SourceDto(int valueParamSomeOtherName)
    {
        Value = valueParamSomeOtherName;
    }

    public int Value { get; }
}

CreateMap<Source, SourceDto>()
    .ForCtorParam("valueParamSomeOtherName", opt => opt.MapFrom(src => src.Value));
```

---

## ProjectTo - Optimización de Queries

`ProjectTo()` optimiza queries de Entity Framework / NHibernate al traducir la expresión de mapeo a SQL, seleccionando solo las columnas necesarias.

### Sin ProjectTo (N+1 Problem)

```csharp
❌ var users = context.Users.ToList();  // SELECT * - trae TODO
var dtos = mapper.Map<List<UserDto>>(users);  // Mapeo en memoria
```

### Con ProjectTo

```csharp
✅ var dtos = context.Users
    .ProjectTo<UserDto>(mapper.ConfigurationProvider)
    .ToList();
// SQL generado: SELECT Id, Name, Email FROM Users
// Solo trae las columnas que UserDto necesita
```

### Ejemplo con Navegación

```csharp
// Configuration
CreateMap<OrderLine, OrderLineDTO>()
    .ForMember(dto => dto.Item, conf => conf.MapFrom(ol => ol.Item.Name));

// Query optimizada
var dtos = context.OrderLines
    .Where(ol => ol.OrderId == orderId)
    .ProjectTo<OrderLineDTO>(mapper.ConfigurationProvider)
    .ToList();

// SQL: SELECT ol.Id, ol.OrderId, i.Name as Item, ol.Quantity
//      FROM OrderLines ol
//      INNER JOIN Items i ON ol.ItemId = i.Id
//      WHERE ol.OrderId = @p0
```

### Cuándo Usar ProjectTo

✅ **Usar cuando**:
- Consultas a base de datos (EF, NHibernate)
- Necesitas optimización de queries
- Trabajas con IQueryable

❌ **No usar cuando**:
- Objetos ya en memoria
- Lógica compleja de mapeo (resolvers)
- No trabajas con IQueryable

---

## Ejemplos Completos del Proyecto

### 1. UserMappingProfile

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.application.usecases.users;
using hashira.stone.backend.webapi.features.users.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for User entity and UserDto.
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Entity → DTO
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));

        // Entity → Response (anidando DTO)
        CreateMap<User, CreateUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));
        CreateMap<User, GetUserModel.Response>()
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src));

        // Request → Command
        CreateMap<CreateUserModel.Request, CreateUserUseCase.Command>();
        CreateMap<GetUserModel.Request, GetUserUseCase.Command>();
    }
}
```

**Patrones observados**:
- Transformación de colecciones: `IList<Role>` → `IEnumerable<string>`
- Anidación: Entity → Response con DTO dentro
- Mapeo directo: Request → Command (propiedades coinciden)

---

### 2. TechnicalStandardMappingProfile

```csharp
using AutoMapper;
using hashira.stone.backend.application.usecases.technicalstandards;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.technicalstandards.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

/// <summary>
/// Mapping profile for TechnicalStandard entity and TechnicalStandardDto.
/// </summary>
public class TechnicalStandardMappingProfile : Profile
{
    public TechnicalStandardMappingProfile()
    {
        // Entity → DTO
        CreateMap<TechnicalStandard, TechnicalStandardDto>();

        // DAO → DTO (optimización de queries)
        CreateMap<TechnicalStandardDao, TechnicalStandardDto>();

        // Entity → Response
        CreateMap<TechnicalStandard, GetTechnicalStandardModel.Response>()
            .ForMember(dest => dest.TechnicalStandard, opt => opt.MapFrom(src => src));

        // DTO paginado → DTO paginado (transformación de elementos)
        CreateMap<GetManyAndCountResultDto<TechnicalStandardDao>,
                  GetManyAndCountResultDto<TechnicalStandardDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Count))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.SortBy))
            .ForMember(dest => dest.SortCriteria, opt => opt.MapFrom(src => src.SortCriteria));

        // Request → Command
        CreateMap<CreateTechnicalStandardModel.Request, CreateTechnicalStandardUseCase.Command>();
        CreateMap<UpdateTechnicalStandardModel.Request, UpdateTechnicalStandardUseCase.Command>();

        // Entity → Response
        CreateMap<TechnicalStandard, CreateTechnicalStandardModel.Response>()
            .ForMember(dest => dest.TechnicalStandard, opt => opt.MapFrom(src => src));
        CreateMap<TechnicalStandard, UpdateTechnicalStandardModel.Response>()
            .ForMember(dest => dest.TechnicalStandard, opt => opt.MapFrom(src => src));
    }
}
```

**Patrones observados**:
- DAO → DTO para queries optimizadas
- Mapeo de DTOs paginados genéricos
- Múltiples Commands para diferentes operaciones

---

### 3. PrototypeMappingProfile

```csharp
using AutoMapper;
using hashira.stone.backend.application.usecases.prototypes;
using hashira.stone.backend.domain.daos;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.prototypes.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

public class PrototypeMappingProfile : Profile
{
    public PrototypeMappingProfile()
    {
        // DAO → DTO
        CreateMap<PrototypeDao, PrototypeDto>();

        // DTO paginado → DTO paginado (solo Items necesita mapeo explícito)
        CreateMap<GetManyAndCountResultDto<PrototypeDao>,
                  GetManyAndCountResultDto<PrototypeDto>>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        // Entity → DTO
        CreateMap<Prototype, PrototypeDto>();

        // Request → Command
        CreateMap<CreatePrototypeModel.Request, CreatePrototypeUseCase.Command>();
        CreateMap<GetPrototypeModel.Request, GetPrototypeUseCase.Command>();
        CreateMap<UpdatePrototypeModel.Request, UpdatePrototypeUseCase.Command>();
        CreateMap<GetManyAndCountPrototypesModel.Request, GetManyAndCountPrototypesUseCase.Command>();

        // Entity → Response
        CreateMap<Prototype, CreatePrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));
        CreateMap<Prototype, GetPrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));
        CreateMap<Prototype, UpdatePrototypeModel.Response>()
            .ForMember(dest => dest.Prototype, opt => opt.MapFrom(src => src));
    }
}
```

**Patrones observados**:
- Mapeo simplificado de paginación (solo Items)
- Consistencia en Entity → Response pattern

---

### 4. MappingProfile (Genérico)

```csharp
using AutoMapper;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.mappingprofiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeo genérico de GetManyAndCountResult<T> a GetManyAndCountResultDto<T>
        CreateMap(typeof(GetManyAndCountResult<>), typeof(GetManyAndCountResultDto<>))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortBy),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.SortBy;
                }))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortCriteria),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.Criteria switch
                    {
                        SortingCriteriaType.Ascending => "asc",
                        SortingCriteriaType.Descending => "desc",
                        _ => null
                    };
                }));
    }
}
```

**Patrones observados**:
- Mapeo de tipos genéricos con `typeof()`
- Lambda con múltiples parámetros
- Pattern matching para transformar enums

---

## Best Practices

### ✅ DO

1. **Organizar mapeos en Profiles por feature**
   ```csharp
   ✅ UserMappingProfile
   ✅ TechnicalStandardMappingProfile
   ✅ PrototypeMappingProfile
   ```

2. **Usar nombres descriptivos para Profiles**
   ```csharp
   ✅ public class UserMappingProfile : Profile { }
   ```

3. **Documentar Profiles con XML comments**
   ```csharp
   ✅ /// <summary>
   /// Mapping profile for User entity and UserDto.
   /// </summary>
   public class UserMappingProfile : Profile { }
   ```

4. **Validar configuración en tests**
   ```csharp
   ✅ [Fact]
   public void AutoMapper_Configuration_IsValid()
   {
       var config = new MapperConfiguration(cfg => {
           cfg.AddMaps(typeof(Program).Assembly);
       });
       config.AssertConfigurationIsValid();
   }
   ```

5. **Usar ProjectTo para queries**
   ```csharp
   ✅ var dtos = context.Users
       .ProjectTo<UserDto>(mapper.ConfigurationProvider)
       .ToList();
   ```

6. **Mapear solo una dirección cuando sea apropiado**
   ```csharp
   ✅ CreateMap<User, UserDto>();  // Solo Entity → DTO
   // NO necesitas UserDto → User
   ```

7. **Usar ForMember para transformaciones complejas**
   ```csharp
   ✅ CreateMap<User, UserDto>()
       .ForMember(dest => dest.Roles,
           opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
   ```

8. **Inyectar IMapper, no MapperConfiguration**
   ```csharp
   ✅ public class Endpoint(IMapper mapper) { }
   ```

9. **Configurar en constructor del Profile**
   ```csharp
   ✅ public UserMappingProfile()
   {
       CreateMap<User, UserDto>();
   }
   ```

10. **Usar tipos específicos en lugar de object cuando sea posible**
    ```csharp
    ✅ CreateMap<User, UserDto>();
    ❌ CreateMap(sourceType, destType);  // Solo si es dinámico
    ```

---

### ❌ DON'T

1. **No crear mapeos bidireccionales innecesarios**
   ```csharp
   ❌ CreateMap<User, UserDto>().ReverseMap();
   // Si solo necesitas Entity → DTO
   ```

2. **No mapear entities directamente en API**
   ```csharp
   ❌ var response = mapper.Map<User>(request);  // Usar Command
   ```

3. **No usar AutoMapper para lógica de negocio**
   ```csharp
   ❌ CreateMap<Order, OrderDto>()
       .AfterMap((src, dest) => {
           // Lógica de negocio aquí ❌
           dest.Total = CalculateTotal(src);  // Debería estar en Domain
       });
   ```

4. **No ignorar errores de configuración**
   ```csharp
   ❌ try {
       config.AssertConfigurationIsValid();
   } catch { }  // ❌ No silenciar errores
   ```

5. **No crear Profiles gigantes**
   ```csharp
   ❌ public class MappingProfile : Profile
   {
       // 500 líneas de CreateMap
   }
   // Dividir por feature
   ```

6. **No usar AutoMapper para mapeo trivial**
   ```csharp
   ❌ var dto = mapper.Map<SimpleDto>(simple);

   ✅ var dto = new SimpleDto { Id = simple.Id };  // Más rápido
   ```

7. **No mezclar lógica de transformación en mapeos**
   ```csharp
   ❌ CreateMap<Source, Dest>()
       .ForMember(dest => dest.Value, opt => opt.MapFrom(src => {
           // 50 líneas de lógica ❌
       }));

   ✅ // Usa un ValueResolver o método dedicado
   ```

8. **No mapear en loops internos**
   ```csharp
   ❌ foreach (var item in items) {
       var dto = mapper.Map<Dto>(item);  // Ineficiente
   }

   ✅ var dtos = mapper.Map<List<Dto>>(items);  // Eficiente
   ```

9. **No usar reflection en mapeos cuando no es necesario**
   ```csharp
   ❌ CreateMap<Source, Dest>()
       .ForMember("PropertyName", ...)  // String-based

   ✅ CreateMap<Source, Dest>()
       .ForMember(dest => dest.PropertyName, ...)  // Type-safe
   ```

10. **No olvidar documentar mapeos complejos**
    ```csharp
    ❌ CreateMap<A, B>()
        .ForMember(x => x.Y, o => o.MapFrom(s => s.Z.W.V));

    ✅ // Mapea Y desde la navegación Z.W.V
    CreateMap<A, B>()
        .ForMember(dest => dest.Y, opt => opt.MapFrom(src => src.Z.W.V));
    ```

---

## Errores Comunes

### Error 1: Mapeo No Configurado

```csharp
❌ Unmapped members were found. Review the types and members below.
Add a custom mapping expression, ignore, add a custom resolver, or modify the source/destination type
For no matching constructor, add a no-arg ctor, add optional arguments, or map all of the constructor parameters
```

**Causa**: Propiedad en destination sin mapeo

**Solución 1**: Mapear explícitamente
```csharp
✅ CreateMap<Source, Dest>()
    .ForMember(dest => dest.UnmappedProperty, opt => opt.MapFrom(src => src.SourceProperty));
```

**Solución 2**: Ignorar
```csharp
✅ CreateMap<Source, Dest>()
    .ForMember(dest => dest.UnmappedProperty, opt => opt.Ignore());
```

---

### Error 2: Mapeo Circular

```csharp
❌ AutoMapper.AutoMapperMappingException: Maximum depth exceeded
```

**Causa**: Navegación circular (User → Roles → User)

**Solución**: Configurar MaxDepth
```csharp
✅ CreateMap<User, UserDto>()
    .MaxDepth(3);
```

O aplanar:
```csharp
✅ CreateMap<User, UserDto>()
    .ForMember(dest => dest.RoleNames,
        opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
```

---

### Error 3: Falta AssertConfigurationIsValid

```csharp
❌ // Configuración inválida, pero no se detecta hasta runtime
var config = new MapperConfiguration(cfg => {
    cfg.CreateMap<Source, Dest>();  // Falta mapeo
});
```

**Solución**: Siempre validar en tests
```csharp
✅ [Fact]
public void AutoMapper_Configuration_Should_Be_Valid()
{
    var config = new MapperConfiguration(cfg => {
        cfg.AddMaps(typeof(Program).Assembly);
    });

    config.AssertConfigurationIsValid();
}
```

---

### Error 4: No Usar ProjectTo con IQueryable

```csharp
❌ var users = context.Users.ToList();  // Trae TODA la data
var dtos = mapper.Map<List<UserDto>>(users);
```

**Solución**: Usar ProjectTo
```csharp
✅ var dtos = context.Users
    .ProjectTo<UserDto>(mapper.ConfigurationProvider)
    .ToList();
```

---

### Error 5: Mapeo de Colecciones Null

```csharp
❌ List<User> users = null;
var dtos = mapper.Map<List<UserDto>>(users);  // NullReferenceException
```

**Solución**: Verificar null
```csharp
✅ var dtos = users != null
    ? mapper.Map<List<UserDto>>(users)
    : new List<UserDto>();
```

O configurar globalmente:
```csharp
✅ var config = new MapperConfiguration(cfg => {
    cfg.AllowNullCollections = true;
});
```

---

### Error 6: ForMember sin Configuración

```csharp
❌ CreateMap<Source, Dest>()
    .ForMember(dest => dest.Property);  // Falta opt => ...
```

**Solución**: Siempre proporcionar configuración
```csharp
✅ CreateMap<Source, Dest>()
    .ForMember(dest => dest.Property, opt => opt.MapFrom(src => src.SourceProperty));
```

---

### Error 7: Mapeo de Enums

```csharp
❌ // Enum values no coinciden
public enum SourceStatus { Active, Inactive }
public enum DestStatus { Enabled, Disabled }

CreateMap<Source, Dest>();  // No mapea correctamente
```

**Solución**: Mapeo explícito
```csharp
✅ CreateMap<Source, Dest>()
    .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
        src.Status == SourceStatus.Active ? DestStatus.Enabled : DestStatus.Disabled));
```

---

## Referencias

### Documentación Oficial

- **AutoMapper Documentation**: https://docs.automapper.org/
- **GitHub Repository**: https://github.com/AutoMapper/AutoMapper
- **Configuration Guide**: https://docs.automapper.org/en/stable/Configuration.html
- **Projection (ProjectTo)**: https://docs.automapper.org/en/stable/Queryable-Extensions.html

### Guías Relacionadas

- [Request/Response Models](./request-response-models.md)
- [DTOs](./dtos.md)
- [FastEndpoints Basics](./fastendpoints-basics.md)
- [Domain Entities](../domain-layer/entities.md)

### Archivos de Referencia del Proyecto

**Mapping Profiles**:
- [MappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\MappingProfile.cs) - Mapeos genéricos
- [UserMappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\UserMappingProfile.cs) - Ejemplo completo
- [TechnicalStandardMappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\TechnicalStandardMappingProfile.cs) - DAO → DTO
- [PrototypeMappingProfile.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\mappingprofiles\PrototypeMappingProfile.cs) - Paginación

**DTOs** (para ver qué se mapea):
- [UserDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\UserDto.cs)
- [TechnicalStandardDto.cs](d:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\dtos\TechnicalStandardDto.cs)

---

## Changelog

### Version 1.0.0 (2025-01-15)
- ✅ Initial release
- ✅ Complete AutoMapper documentation
- ✅ Configuration and setup with ASP.NET Core DI
- ✅ Profiles organization patterns
- ✅ CreateMap and ForMember configurations
- ✅ Generic mappings with typeof()
- ✅ ProjectTo for query optimization
- ✅ 4 complete examples from reference project
- ✅ Best practices and common errors
- ✅ Integration with Context7 official documentation

---

**Siguiente Guía**: [Error Responses](./error-responses.md)

[◀️ Volver al WebApi Layer](./README.md) | [🏠 Inicio](../README.md)
