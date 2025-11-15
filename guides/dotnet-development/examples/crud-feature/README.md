# CRUD Feature - Ejemplo Completo

**Versión:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-11-15
**Proyecto de referencia:** hashira.stone.backend - Feature Prototypes

## Tabla de Contenidos

1. [¿Qué es un Feature CRUD?](#qué-es-un-feature-crud)
2. [Anatomía de un Feature CRUD](#anatomía-de-un-feature-crud)
3. [Ejemplo: Prototypes Feature](#ejemplo-prototypes-feature)
4. [Componentes por Capa](#componentes-por-capa)
5. [Flujo de Datos](#flujo-de-datos)
6. [Código de Ejemplo](#código-de-ejemplo)
7. [Mejores Prácticas](#mejores-prácticas)
8. [Checklist de Implementación](#checklist-de-implementación)
9. [Referencias](#referencias)

---

## ¿Qué es un Feature CRUD?

Un **Feature CRUD** es un módulo completo que implementa las operaciones básicas de Create, Read, Update, Delete sobre una entidad de negocio, atravesando todas las capas de Clean Architecture.

### Operaciones Estándar

| Operación | HTTP Verb | Descripción |
|-----------|-----------|-------------|
| **Create** | POST | Crear una nueva entidad |
| **Read (Get)** | GET /{id} | Obtener una entidad por ID |
| **Read (GetMany)** | GET | Listar entidades con paginación |
| **Update** | PUT | Actualizar una entidad existente |
| **Delete** | DELETE | Eliminar una entidad |

### Características de un CRUD Completo

✅ **Domain Layer:**
- Entity con propiedades de negocio
- Validator con FluentValidation
- Repository interface (IEntityRepository)
- DAO para consultas optimizadas
- Domain exceptions

✅ **Application Layer:**
- Use Case por operación (CreateUseCase, GetUseCase, etc.)
- Command/Handler pattern
- Manejo de transacciones
- FluentResults para errores

✅ **Infrastructure Layer:**
- Repository implementation con NHibernate
- Entity mapper (tabla principal)
- DAO mapper (vista de consulta)
- Manejo de excepciones de dominio

✅ **WebApi Layer:**
- Endpoint por operación (CreateEndpoint, GetEndpoint, etc.)
- Request/Response models
- DTO compartido
- AutoMapper profile
- Swagger documentation

---

## Anatomía de un Feature CRUD

### Estructura de Archivos Completa

```
hashira.stone.backend/
│
├── domain/
│   ├── entities/
│   │   ├── Prototype.cs                        # Entity principal
│   │   └── validators/
│   │       └── PrototypeValidator.cs           # Validador FluentValidation
│   ├── daos/
│   │   └── PrototypeDao.cs                     # DAO para consultas
│   └── interfaces/
│       └── repositories/
│           └── IPrototypeRepository.cs         # Contrato del repositorio
│
├── application/
│   └── usecases/
│       └── prototypes/
│           ├── CreatePrototypeUseCase.cs       # Create operation
│           ├── GetPrototypeUseCase.cs          # Get by ID
│           ├── GetManyAndCountPrototypesUseCase.cs  # List with pagination
│           └── UpdatePrototypeUseCase.cs       # Update operation
│
├── infrastructure/
│   └── nhibernate/
│       ├── NHPrototypeRepository.cs            # Repository implementation
│       └── mappers/
│           ├── PrototypeMapper.cs              # Entity → Table mapping
│           └── PrototypeDaoMapper.cs           # DAO → View mapping
│
└── webapi/
    ├── features/
    │   └── prototypes/
    │       ├── endpoint/
    │       │   ├── CreatePrototypeEndpoint.cs
    │       │   ├── GetPrototypeEndpoint.cs
    │       │   ├── GetManyAndCountPrototypesEndpoint.cs
    │       │   └── UpdatePrototypeEndpoint.cs
    │       └── models/
    │           ├── CreatePrototypeModel.cs     # Request/Response for Create
    │           ├── GetPrototypeModel.cs
    │           ├── GetManyAndCountPrototypesModel.cs
    │           └── UpdatePrototypeModel.cs
    ├── dtos/
    │   └── PrototypeDto.cs                     # Shared DTO
    └── mappingprofiles/
        └── PrototypeMappingProfile.cs          # AutoMapper configuration
```

**Total de archivos**: 17 archivos para un CRUD completo

---

## Ejemplo: Prototypes Feature

El feature **Prototypes** del proyecto de referencia gestiona prototipos de productos con las siguientes propiedades:

### Modelo de Dominio

```csharp
public class Prototype
{
    public virtual Guid Id { get; protected set; }
    public virtual string Number { get; protected set; }      // Número único del prototipo
    public virtual DateTime IssueDate { get; protected set; } // Fecha de emisión
    public virtual DateTime? ExpirationDate { get; protected set; }  // Fecha de expiración
    public virtual string Status { get; protected set; }      // Estado: "Active", "Inactive", "Expired"
}
```

### Operaciones Disponibles

| Operación | Endpoint | Autenticación |
|-----------|----------|---------------|
| Crear prototipo | POST /api/prototypes | MustBeApplicationUser |
| Obtener prototipo | GET /api/prototypes/{id} | MustBeApplicationUser |
| Listar prototipos | GET /api/prototypes | MustBeApplicationUser |
| Actualizar prototipo | PUT /api/prototypes | MustBeApplicationUser |

### Reglas de Negocio

1. **Number**: Obligatorio, único, no puede estar vacío
2. **IssueDate**: Obligatorio, no puede ser fecha futura
3. **ExpirationDate**: Opcional, debe ser posterior a IssueDate
4. **Status**: Obligatorio, valores permitidos: "Active", "Inactive", "Expired"

---

## Componentes por Capa

### 1. Domain Layer

#### Entity: Prototype.cs

```csharp
using hashira.stone.backend.domain.entities.validators;
using FluentValidation;

namespace hashira.stone.backend.domain.entities;

public class Prototype : AbstractDomainObject
{
    public virtual string Number { get; protected set; } = string.Empty;
    public virtual DateTime IssueDate { get; protected set; }
    public virtual DateTime? ExpirationDate { get; protected set; }
    public virtual string Status { get; protected set; } = string.Empty;

    protected Prototype() { }

    public static Prototype Create(
        string number,
        DateTime issueDate,
        DateTime? expirationDate,
        string status)
    {
        var prototype = new Prototype
        {
            Id = Guid.NewGuid(),
            Number = number,
            IssueDate = issueDate,
            ExpirationDate = expirationDate,
            Status = status,
            CreationDate = DateTime.UtcNow
        };

        if (!prototype.IsValid())
            throw new InvalidDomainException(prototype.GetValidationErrors());

        return prototype;
    }

    public void Update(
        string number,
        DateTime issueDate,
        DateTime? expirationDate,
        string status)
    {
        Number = number;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Status = status;

        if (!IsValid())
            throw new InvalidDomainException(GetValidationErrors());
    }

    public override IValidator GetValidator() => new PrototypeValidator();
}
```

**Responsabilidades:**
- Encapsular propiedades de negocio
- Método factory `Create()` para creación segura
- Método `Update()` para actualización con validación
- Validación automática al crear/actualizar

#### Validator: PrototypeValidator.cs

```csharp
using FluentValidation;

namespace hashira.stone.backend.domain.entities.validators;

public class PrototypeValidator : AbstractValidator<Prototype>
{
    public PrototypeValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty()
            .WithMessage("Prototype number is required")
            .WithErrorCode("PROTOTYPE_NUMBER_REQUIRED");

        RuleFor(x => x.IssueDate)
            .NotEmpty()
            .WithMessage("Issue date is required")
            .WithErrorCode("PROTOTYPE_ISSUE_DATE_REQUIRED")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Issue date cannot be in the future")
            .WithErrorCode("PROTOTYPE_ISSUE_DATE_FUTURE");

        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.IssueDate)
            .When(x => x.ExpirationDate.HasValue)
            .WithMessage("Expiration date must be after issue date")
            .WithErrorCode("PROTOTYPE_EXPIRATION_DATE_INVALID");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .WithErrorCode("PROTOTYPE_STATUS_REQUIRED")
            .Must(s => new[] { "Active", "Inactive", "Expired" }.Contains(s))
            .WithMessage("Status must be Active, Inactive, or Expired")
            .WithErrorCode("PROTOTYPE_STATUS_INVALID");
    }
}
```

**Responsabilidades:**
- Validar reglas de negocio
- Mensajes de error claros
- Códigos de error únicos

#### Repository Interface: IPrototypeRepository.cs

```csharp
namespace hashira.stone.backend.domain.interfaces.repositories;

public interface IPrototypeRepository : IRepository<Prototype>
{
    Task<Prototype?> GetByNumberAsync(string number);
}
```

**Responsabilidades:**
- Definir contrato de persistencia
- Métodos específicos del dominio (GetByNumberAsync)

#### DAO: PrototypeDao.cs

```csharp
namespace hashira.stone.backend.domain.daos;

public class PrototypeDao
{
    public virtual Guid Id { get; set; }
    public virtual string Number { get; set; } = string.Empty;
    public virtual DateTime IssueDate { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    public virtual string Status { get; set; } = string.Empty;
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

**Responsabilidades:**
- Espejo de la Entity para lectura
- Propiedad `SearchAll` para búsquedas full-text

---

### 2. Application Layer

#### Use Case: CreatePrototypeUseCase.cs

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.application.usecases.prototypes;

public static class CreatePrototypeUseCase
{
    public class Command : ICommand<Result<Prototype>>
    {
        public string Number { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Result<Prototype>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<Prototype>> ExecuteAsync(
            Command command,
            CancellationToken ct = default)
        {
            try
            {
                _unitOfWork.BeginTransaction();

                // 1. Verificar duplicados
                var existingPrototype = await _unitOfWork.Prototypes
                    .GetByNumberAsync(command.Number);

                if (existingPrototype != null)
                {
                    return Result.Fail(new ExceptionalError(
                        new DuplicatedDomainException(
                            $"A prototype with number '{command.Number}' already exists")));
                }

                // 2. Crear entidad
                var prototype = Prototype.Create(
                    command.Number,
                    command.IssueDate,
                    command.ExpirationDate,
                    command.Status);

                // 3. Persistir
                await _unitOfWork.Prototypes.AddAsync(prototype);
                await _unitOfWork.CommitAsync();

                return Result.Ok(prototype);
            }
            catch (InvalidDomainException ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail(new ExceptionalError(ex));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail(new ExceptionalError(ex));
            }
        }
    }
}
```

**Responsabilidades:**
- Orquestar creación del prototipo
- Verificar duplicados
- Manejar transacciones
- Convertir excepciones a Results

#### Use Case: GetPrototypeUseCase.cs

```csharp
public static class GetPrototypeUseCase
{
    public class Command : ICommand<Result<Prototype>>
    {
        public Guid Id { get; set; }
    }

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Result<Prototype>>
    {
        public async Task<Result<Prototype>> ExecuteAsync(
            Command command,
            CancellationToken ct = default)
        {
            var prototype = await unitOfWork.Prototypes.GetByIdAsync(command.Id);

            if (prototype == null)
            {
                return Result.Fail(new ExceptionalError(
                    new ResourceNotFoundException($"Prototype with ID '{command.Id}' not found")));
            }

            return Result.Ok(prototype);
        }
    }
}
```

**Responsabilidades:**
- Recuperar prototipo por ID
- Manejar caso de "not found"

#### Use Case: GetManyAndCountPrototypesUseCase.cs

```csharp
using hashira.stone.backend.domain.daos;
using hashira.stone.backend.application.common;

public static class GetManyAndCountPrototypesUseCase
{
    public class Command : ICommand<GetManyAndCountResult<PrototypeDao>>
    {
        public string? Query { get; set; }
    }

    public class Handler(IUnitOfWork unitOfWork)
        : ICommandHandler<Command, GetManyAndCountResult<PrototypeDao>>
    {
        public async Task<GetManyAndCountResult<PrototypeDao>> ExecuteAsync(
            Command command,
            CancellationToken ct = default)
        {
            return await unitOfWork.Prototypes.GetManyAndCountAsync<PrototypeDao>(
                command.Query ?? string.Empty);
        }
    }
}
```

**Responsabilidades:**
- Ejecutar consulta con paginación
- Retornar items + count total
- Soportar filtrado dinámico

---

### 3. Infrastructure Layer

#### Repository: NHPrototypeRepository.cs

```csharp
using NHibernate;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.exceptions;
using hashira.stone.backend.domain.interfaces.repositories;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHPrototypeRepository : NHRepository<Prototype>, IPrototypeRepository
{
    public NHPrototypeRepository(ISession session) : base(session) { }

    public async Task<Prototype?> GetByNumberAsync(string number)
    {
        return await _session.Query<Prototype>()
            .Where(p => p.Number.ToLower() == number.ToLower())
            .SingleOrDefaultAsync();
    }

    public override async Task AddAsync(Prototype entity)
    {
        // Validar entidad
        if (!entity.IsValid())
            throw new InvalidDomainException(entity.GetValidationErrors());

        // Verificar duplicados
        var existingPrototype = await GetByNumberAsync(entity.Number);
        if (existingPrototype != null)
            throw new DuplicatedDomainException(
                $"A prototype with number '{entity.Number}' already exists");

        await _session.SaveAsync(entity);
    }

    public override async Task UpdateAsync(Prototype entity)
    {
        // Validar entidad
        if (!entity.IsValid())
            throw new InvalidDomainException(entity.GetValidationErrors());

        // Verificar que existe
        var existingPrototype = await GetByIdAsync(entity.Id);
        if (existingPrototype == null)
            throw new ResourceNotFoundException(
                $"Prototype with ID '{entity.Id}' not found");

        // Verificar duplicados del número (excluyendo el actual)
        var duplicatePrototype = await _session.Query<Prototype>()
            .Where(p => p.Number.ToLower() == entity.Number.ToLower() && p.Id != entity.Id)
            .SingleOrDefaultAsync();

        if (duplicatePrototype != null)
            throw new DuplicatedDomainException(
                $"A prototype with number '{entity.Number}' already exists");

        await _session.UpdateAsync(entity);
    }
}
```

**Responsabilidades:**
- Implementar operaciones CRUD
- Validar entidades antes de persistir
- Verificar duplicados
- Lanzar excepciones de dominio apropiadas

#### Mapper: PrototypeMapper.cs

```csharp
using FluentNHibernate.Mapping;
using hashira.stone.backend.domain.entities;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class PrototypeMapper : ClassMap<Prototype>
{
    public PrototypeMapper()
    {
        Table("prototypes");

        Id(x => x.Id).GeneratedBy.Assigned();

        Map(x => x.Number).Not.Nullable().Unique();
        Map(x => x.IssueDate).Not.Nullable();
        Map(x => x.ExpirationDate).Nullable();
        Map(x => x.Status).Not.Nullable();
        Map(x => x.CreationDate).Not.Nullable();
    }
}
```

**Responsabilidades:**
- Mapear Entity a tabla de BD
- Definir constraints (NOT NULL, UNIQUE)

#### DAO Mapper: PrototypeDaoMapper.cs

```csharp
using FluentNHibernate.Mapping;
using hashira.stone.backend.domain.daos;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class PrototypeDaoMapper : ClassMap<PrototypeDao>
{
    public PrototypeDaoMapper()
    {
        Table("prototypes_view");
        ReadOnly();
        Mutable(false);

        Id(x => x.Id).GeneratedBy.Assigned();

        Map(x => x.Number);
        Map(x => x.IssueDate);
        Map(x => x.ExpirationDate);
        Map(x => x.Status);
        Map(x => x.SearchAll);
    }
}
```

**Responsabilidades:**
- Mapear DAO a vista de BD
- Marcar como ReadOnly (no modificable)

---

### 4. WebApi Layer

#### Endpoint: CreatePrototypeEndpoint.cs

```csharp
using FastEndpoints;
using hashira.stone.backend.application.usecases.prototypes;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.webapi.features.prototypes.models;
using hashira.stone.backend.domain.exceptions;
using FluentResults;

namespace hashira.stone.backend.webapi.features.prototypes.endpoint;

public class CreatePrototypeEndpoint(AutoMapper.IMapper mapper)
    : Endpoint<CreatePrototypeModel.Request, CreatePrototypeModel.Response>
{
    private readonly AutoMapper.IMapper _mapper = mapper;

    public override void Configure()
    {
        Post("/prototypes");
        Policies("MustBeApplicationUser");

        Description(d => d
            .WithTags("Prototypes")
            .WithName("CreatePrototype")
            .WithDescription("Creates a new prototype in the system")
            .Produces<CreatePrototypeModel.Response>(201)
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(401)
            .ProducesProblemDetails(403)
            .ProducesProblemDetails(409));
    }

    public override async Task HandleAsync(
        CreatePrototypeModel.Request request,
        CancellationToken ct)
    {
        // Mapear Request → Command
        var command = _mapper.Map<CreatePrototypeUseCase.Command>(request);

        // Ejecutar Use Case
        var result = await command.ExecuteAsync(ct);

        // Manejar errores
        if (result.IsFailed)
        {
            var error = result.Errors.FirstOrDefault();

            // 409 Conflict - Duplicate
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is DuplicatedDomainException) == true)
            {
                AddError(error.Message);
                await SendErrorsAsync(409, ct);
                return;
            }

            // 400 Bad Request - Invalid
            if (error?.Reasons.OfType<ExceptionalError>()
                .Any(r => r.Exception is InvalidDomainException) == true)
            {
                AddError(error.Message);
                await SendErrorsAsync(400, ct);
                return;
            }

            // 500 Internal Server Error
            AddError("An unexpected error occurred");
            await SendErrorsAsync(500, ct);
            return;
        }

        // Mapear Entity → Response
        var response = _mapper.Map<CreatePrototypeModel.Response>(result.Value);

        // 201 Created
        await SendCreatedAtAsync<GetPrototypeEndpoint>(
            new { id = response.Prototype.Id },
            response,
            cancellation: ct);
    }
}
```

**Responsabilidades:**
- Configurar ruta y políticas
- Mapear Request → Command
- Ejecutar Use Case
- Mapear excepciones → HTTP status codes
- Mapear Entity → Response DTO
- Retornar respuesta apropiada

#### Model: CreatePrototypeModel.cs

```csharp
using hashira.stone.backend.webapi.dtos;

namespace hashira.stone.backend.webapi.features.prototypes.models;

public class CreatePrototypeModel
{
    public class Request
    {
        public string Number { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class Response
    {
        public PrototypeDto Prototype { get; set; } = new();
    }
}
```

**Responsabilidades:**
- Definir estructura de Request HTTP
- Definir estructura de Response HTTP

#### DTO: PrototypeDto.cs

```csharp
namespace hashira.stone.backend.webapi.dtos;

public class PrototypeDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

**Responsabilidades:**
- DTO compartido entre endpoints
- Serializable a JSON
- Sin lógica de negocio

#### Mapping Profile: PrototypeMappingProfile.cs

```csharp
using AutoMapper;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.daos;
using hashira.stone.backend.webapi.dtos;
using hashira.stone.backend.application.usecases.prototypes;
using hashira.stone.backend.webapi.features.prototypes.models;

namespace hashira.stone.backend.webapi.mappingprofiles;

public class PrototypeMappingProfile : Profile
{
    public PrototypeMappingProfile()
    {
        // Entity → DTO
        CreateMap<Prototype, PrototypeDto>();

        // DAO → DTO
        CreateMap<PrototypeDao, PrototypeDto>();

        // Request → Command
        CreateMap<CreatePrototypeModel.Request, CreatePrototypeUseCase.Command>();
        CreateMap<UpdatePrototypeModel.Request, UpdatePrototypeUseCase.Command>();
        CreateMap<GetPrototypeModel.Request, GetPrototypeUseCase.Command>();

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

**Responsabilidades:**
- Configurar todos los mapeos necesarios
- Request → Command
- Entity/DAO → DTO
- Entity → Response

---

## Flujo de Datos

### Create Operation Flow

```
1. HTTP Request (Client)
   ↓
   POST /api/prototypes
   Body: { "number": "P-001", "issueDate": "2025-01-15", ... }

2. FastEndpoints Routing
   ↓
   CreatePrototypeEndpoint

3. Model Binding
   ↓
   CreatePrototypeModel.Request

4. AutoMapper: Request → Command
   ↓
   CreatePrototypeUseCase.Command

5. Use Case Execution
   ↓
   - BeginTransaction()
   - Verify no duplicates (GetByNumberAsync)
   - Prototype.Create() → validates entity
   - AddAsync() → persists to DB
   - CommitAsync()

6. Result
   ↓
   Result<Prototype>

7. AutoMapper: Entity → DTO
   ↓
   PrototypeDto

8. AutoMapper: Entity → Response
   ↓
   CreatePrototypeModel.Response

9. HTTP Response
   ↓
   201 Created
   Location: /api/prototypes/{id}
   Body: { "prototype": { "id": "...", "number": "P-001", ... } }
```

### Get Operation Flow

```
1. HTTP Request
   ↓
   GET /api/prototypes/{id}

2. GetPrototypeEndpoint
   ↓
   GetPrototypeModel.Request { Id = ... }

3. AutoMapper: Request → Command
   ↓
   GetPrototypeUseCase.Command

4. Use Case Execution
   ↓
   - GetByIdAsync(id)
   - Check if found

5. Result
   ↓
   Result<Prototype> or ResourceNotFoundException

6. AutoMapper: Entity → Response
   ↓
   GetPrototypeModel.Response

7. HTTP Response
   ↓
   200 OK or 404 Not Found
```

### Update Operation Flow

```
1. HTTP Request
   ↓
   PUT /api/prototypes
   Body: { "id": "...", "number": "P-001", ... }

2. UpdatePrototypeEndpoint
   ↓
   UpdatePrototypeModel.Request

3. AutoMapper: Request → Command
   ↓
   UpdatePrototypeUseCase.Command

4. Use Case Execution
   ↓
   - BeginTransaction()
   - GetByIdAsync(id) → verify exists
   - Check duplicate number (excluding current)
   - entity.Update(...) → validates
   - UpdateAsync() → persists
   - CommitAsync()

5. Result
   ↓
   Result<Prototype>

6. AutoMapper: Entity → Response
   ↓
   UpdatePrototypeModel.Response

7. HTTP Response
   ↓
   200 OK
```

---

## Código de Ejemplo

### Ejemplo 1: Crear Prototipo

**Request:**
```http
POST /api/prototypes
Content-Type: application/json
Authorization: Bearer {token}

{
  "number": "P-2025-001",
  "issueDate": "2025-01-15T10:00:00Z",
  "expirationDate": "2026-01-15T10:00:00Z",
  "status": "Active"
}
```

**Response (201 Created):**
```json
{
  "prototype": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "number": "P-2025-001",
    "issueDate": "2025-01-15T10:00:00Z",
    "expirationDate": "2026-01-15T10:00:00Z",
    "status": "Active"
  }
}
```

**Response (409 Conflict):**
```json
{
  "errors": {
    "GeneralErrors": [
      "A prototype with number 'P-2025-001' already exists"
    ]
  }
}
```

### Ejemplo 2: Obtener Prototipo

**Request:**
```http
GET /api/prototypes/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "prototype": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "number": "P-2025-001",
    "issueDate": "2025-01-15T10:00:00Z",
    "expirationDate": "2026-01-15T10:00:00Z",
    "status": "Active"
  }
}
```

**Response (404 Not Found):**
```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Prototype with ID '...' not found"
}
```

### Ejemplo 3: Listar Prototipos

**Request:**
```http
GET /api/prototypes?pageNumber=1&pageSize=10&sortBy=Number&sortDirection=asc&Status=Active||eq
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "number": "P-2025-001",
      "issueDate": "2025-01-15T10:00:00Z",
      "expirationDate": "2026-01-15T10:00:00Z",
      "status": "Active"
    }
  ],
  "count": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "sortBy": "Number",
  "sortCriteria": "asc"
}
```

### Ejemplo 4: Actualizar Prototipo

**Request:**
```http
PUT /api/prototypes
Content-Type: application/json
Authorization: Bearer {token}

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "number": "P-2025-001-UPDATED",
  "issueDate": "2025-01-15T10:00:00Z",
  "expirationDate": "2026-12-31T10:00:00Z",
  "status": "Active"
}
```

**Response (200 OK):**
```json
{
  "prototype": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "number": "P-2025-001-UPDATED",
    "issueDate": "2025-01-15T10:00:00Z",
    "expirationDate": "2026-12-31T10:00:00Z",
    "status": "Active"
  }
}
```

---

## Mejores Prácticas

### 1. ✅ Validación en Múltiples Niveles

```csharp
// Domain Layer - Reglas de negocio
public class PrototypeValidator : AbstractValidator<Prototype>
{
    RuleFor(x => x.IssueDate).LessThanOrEqualTo(DateTime.UtcNow);
}

// WebApi Layer - Validación de formato (opcional)
public class CreatePrototypeRequestValidator : Validator<CreatePrototypeModel.Request>
{
    RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
}

// Repository Layer - Validación antes de persistir
public override async Task AddAsync(Prototype entity)
{
    if (!entity.IsValid())
        throw new InvalidDomainException(entity.GetValidationErrors());

    await _session.SaveAsync(entity);
}
```

**Por qué**: Defensa en profundidad - cada capa valida lo que le corresponde.

---

### 2. ✅ Verificar Duplicados Antes de Crear/Actualizar

```csharp
// En CreateUseCase
var existingPrototype = await _unitOfWork.Prototypes.GetByNumberAsync(command.Number);
if (existingPrototype != null)
{
    return Result.Fail(new ExceptionalError(
        new DuplicatedDomainException($"Prototype '{command.Number}' already exists")));
}

// En UpdateUseCase
var duplicatePrototype = await _session.Query<Prototype>()
    .Where(p => p.Number == entity.Number && p.Id != entity.Id)
    .SingleOrDefaultAsync();

if (duplicatePrototype != null)
    throw new DuplicatedDomainException(...);
```

**Por qué**: Evitar violaciones de UNIQUE constraints en BD.

---

### 3. ✅ Transacciones en Use Cases

```csharp
public async Task<Result<Prototype>> ExecuteAsync(Command command, CancellationToken ct)
{
    try
    {
        _unitOfWork.BeginTransaction();

        // Operaciones de negocio
        var prototype = Prototype.Create(...);
        await _unitOfWork.Prototypes.AddAsync(prototype);

        await _unitOfWork.CommitAsync();
        return Result.Ok(prototype);
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackAsync();
        return Result.Fail(new ExceptionalError(ex));
    }
}
```

**Por qué**: Garantizar atomicidad de operaciones.

---

### 4. ✅ Mapeo de Excepciones a HTTP Status Codes

```csharp
if (result.IsFailed)
{
    var error = result.Errors.FirstOrDefault();

    // 409 Conflict
    if (error?.Reasons.OfType<ExceptionalError>()
        .Any(r => r.Exception is DuplicatedDomainException) == true)
    {
        return await SendErrorsAsync(409, ct);
    }

    // 404 Not Found
    if (error?.Reasons.OfType<ExceptionalError>()
        .Any(r => r.Exception is ResourceNotFoundException) == true)
    {
        return await SendErrorsAsync(404, ct);
    }

    // 400 Bad Request
    if (error?.Reasons.OfType<ExceptionalError>()
        .Any(r => r.Exception is InvalidDomainException) == true)
    {
        return await SendErrorsAsync(400, ct);
    }

    // 500 Internal Server Error (default)
    return await SendErrorsAsync(500, ct);
}
```

**Por qué**: Errores HTTP semánticamente correctos.

---

### 5. ✅ DAO Separado de Entity

```csharp
// Entity - Para Write operations
public class Prototype : AbstractDomainObject
{
    // Propiedades + métodos de dominio + validación
}

// DAO - Para Read operations
public class PrototypeDao
{
    // Solo propiedades + SearchAll
}

// Mapper ReadOnly
public class PrototypeDaoMapper : ClassMap<PrototypeDao>
{
    Table("prototypes_view");
    ReadOnly();
    Mutable(false);
}
```

**Por qué**:
- Optimización de queries (vistas de BD)
- Separación read/write (CQRS lite)

---

### 6. ✅ Usar AutoMapper Profiles

```csharp
public class PrototypeMappingProfile : Profile
{
    public PrototypeMappingProfile()
    {
        // Todos los mapeos en un solo lugar
        CreateMap<Prototype, PrototypeDto>();
        CreateMap<CreatePrototypeModel.Request, CreatePrototypeUseCase.Command>();
        // ... más mapeos
    }
}
```

**Por qué**:
- Centralización de mapeos
- Fácil mantenimiento
- Type-safe

---

### 7. ✅ Documentar Endpoints con Swagger

```csharp
public override void Configure()
{
    Post("/prototypes");

    Description(d => d
        .WithTags("Prototypes")
        .WithName("CreatePrototype")
        .WithDescription("Creates a new prototype")
        .Produces<CreatePrototypeModel.Response>(201)
        .ProducesProblemDetails(400)
        .ProducesProblemDetails(409));

    Summary(s => {
        s.Summary = "Create a new prototype";
        s.ExampleRequest = new CreatePrototypeModel.Request
        {
            Number = "P-2025-001",
            IssueDate = DateTime.UtcNow,
            Status = "Active"
        };
    });
}
```

**Por qué**: API autodocumentada, fácil de consumir.

---

### 8. ✅ Nombres Consistentes en Todas las Capas

```
Domain:       Prototype
Application:  CreatePrototypeUseCase, GetPrototypeUseCase
Infrastructure: NHPrototypeRepository, PrototypeMapper
WebApi:       CreatePrototypeEndpoint, PrototypeDto, PrototypeMappingProfile
```

**Por qué**: Fácil navegación y búsqueda en el código.

---

### 9. ✅ Un Use Case por Operación

```csharp
// ✅ CORRECTO - Use Cases separados
CreatePrototypeUseCase.cs
GetPrototypeUseCase.cs
GetManyAndCountPrototypesUseCase.cs
UpdatePrototypeUseCase.cs

// ❌ INCORRECTO - Un Use Case para todo
PrototypeManagementUseCase.cs  // con métodos Create(), Get(), Update()...
```

**Por qué**: Single Responsibility Principle, fácil testing.

---

### 10. ✅ Constructor Protegido + Factory Method

```csharp
public class Prototype : AbstractDomainObject
{
    protected Prototype() { }  // Para NHibernate

    // Factory method público
    public static Prototype Create(
        string number,
        DateTime issueDate,
        DateTime? expirationDate,
        string status)
    {
        var prototype = new Prototype
        {
            Id = Guid.NewGuid(),
            Number = number,
            // ... set properties
        };

        if (!prototype.IsValid())
            throw new InvalidDomainException(prototype.GetValidationErrors());

        return prototype;
    }
}
```

**Por qué**:
- Constructor protected para NHibernate
- Factory method garantiza validación
- Encapsulación de creación

---

## Checklist de Implementación

Use este checklist al implementar un nuevo feature CRUD:

### Domain Layer
- [ ] Crear Entity con propiedades protected
- [ ] Constructor protected (para NHibernate)
- [ ] Factory method `Create()` con validación
- [ ] Método `Update()` con validación
- [ ] Override `GetValidator()`
- [ ] Crear Validator con FluentValidation
- [ ] Crear DAO con propiedad SearchAll
- [ ] Crear interface IEntityRepository
- [ ] Agregar métodos específicos (ej: GetByNumberAsync)

### Application Layer
- [ ] CreateUseCase (Command + Handler)
- [ ] GetUseCase (Command + Handler)
- [ ] GetManyAndCountUseCase (Command + Handler)
- [ ] UpdateUseCase (Command + Handler)
- [ ] Manejo de transacciones (BeginTransaction, Commit, Rollback)
- [ ] Verificación de duplicados
- [ ] Conversión de excepciones a FluentResults

### Infrastructure Layer
- [ ] Implementar NHEntityRepository
- [ ] Override AddAsync con validación
- [ ] Override UpdateAsync con validación
- [ ] Implementar métodos custom (GetByNumberAsync, etc.)
- [ ] Crear EntityMapper (ClassMap)
- [ ] Configurar tabla, columnas, constraints
- [ ] Crear DaoMapper (ClassMap)
- [ ] Marcar como ReadOnly y Mutable(false)

### WebApi Layer
- [ ] CreateEndpoint (POST)
- [ ] GetEndpoint (GET /{id})
- [ ] GetManyAndCountEndpoint (GET)
- [ ] UpdateEndpoint (PUT)
- [ ] Configurar rutas y políticas
- [ ] Documentación Swagger (Description/Summary)
- [ ] Crear Models (Request/Response) por endpoint
- [ ] Crear DTO compartido
- [ ] Crear MappingProfile con todos los mapeos
- [ ] Mapeo de excepciones a HTTP status codes

### Testing (Opcional pero Recomendado)
- [ ] Unit tests para Validator
- [ ] Unit tests para Entity methods (Create, Update)
- [ ] Unit tests para Use Cases
- [ ] Integration tests para Repository
- [ ] Integration tests para Endpoints

---

## Referencias

### Guías Relacionadas

**Domain Layer:**
- [Entities](../../domain-layer/entities.md) - AbstractDomainObject pattern
- [Validators](../../domain-layer/validators.md) - FluentValidation
- [Repository Interfaces](../../domain-layer/repository-interfaces.md) - IRepository, IUnitOfWork
- [DAOs](../../domain-layer/daos.md) - DAO pattern

**Application Layer:**
- [Use Cases](../../application-layer/use-cases.md) - Command/Handler pattern
- [Command Handler Patterns](../../application-layer/command-handler-patterns.md) - CRUD patterns
- [Error Handling](../../application-layer/error-handling.md) - FluentResults

**Infrastructure Layer:**
- [Repository Pattern](../../infrastructure-layer/repository-pattern.md) - Implementation
- [NHibernate Repositories](../../infrastructure-layer/orm-implementations/nhibernate/repositories.md)
- [NHibernate Mappers](../../infrastructure-layer/orm-implementations/nhibernate/mappers.md)

**WebApi Layer:**
- [FastEndpoints Basics](../../webapi-layer/fastendpoints-basics.md) - Endpoint structure
- [Request/Response Models](../../webapi-layer/request-response-models.md) - Models pattern
- [DTOs](../../webapi-layer/dtos.md) - Data Transfer Objects
- [AutoMapper Profiles](../../webapi-layer/automapper-profiles.md) - Mapping configuration
- [Error Responses](../../webapi-layer/error-responses.md) - HTTP status codes
- [Authentication](../../webapi-layer/authentication.md) - JWT, policies

### Siguiente Paso

- **[Step-by-Step Guide](./step-by-step.md)** - Implementación paso a paso de un CRUD completo desde cero

---

**Última actualización:** 2025-11-15
**Mantenedor:** Equipo APSYS
**Proyecto de referencia:** hashira.stone.backend - Prototypes Feature
