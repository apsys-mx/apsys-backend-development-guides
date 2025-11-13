# Feature Structure - .NET Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

Esta sección documenta la arquitectura y organización de features/módulos en aplicaciones .NET con Clean Architecture de APSYS. Un "feature" es un módulo de negocio independiente que atraviesa todas las capas de la arquitectura (Domain, Application, Infrastructure, WebApi).

La arquitectura por features promueve:
- **Modularidad**: Cada feature es auto-contenido
- **Trazabilidad**: Fácil seguir el flujo de un feature a través de todas las capas
- **Escalabilidad**: Agregar nuevos features sin afectar otros
- **Mantenibilidad**: Código organizado y fácil de encontrar
- **Colaboración**: Equipos pueden trabajar en features independientes

## Guías Disponibles

### 1. [Folder Organization](./folder-organization.md)

Estructura estándar de carpetas y archivos para un feature a través de todas las capas.

**Contenido:**
- Anatomía de un feature completo (Domain → Application → Infrastructure → WebApi)
- Convenciones de nombres por capa
- Ejemplos de features: CRUD, simple, infrastructure
- Feature vs shared components
- Organización por módulos de negocio

**Cuándo usar:** Al crear un nuevo feature desde cero o reorganizar uno existente.

---

### 2. [Entity to Endpoint Flow](./entity-to-endpoint-flow.md)

Flujo completo de una operación desde la entidad de dominio hasta el endpoint.

**Contenido:**
- Flujo de creación (Create flow)
- Flujo de consulta (Get flow)
- Flujo de actualización (Update flow)
- Flujo de eliminación (Delete flow)
- Flujo de GetManyAndCount
- Interacción entre capas
- Dependency flow
- Data transformation entre capas

**Cuándo usar:** Al implementar una nueva operación en un feature existente.

---

### 3. [Naming Conventions](./naming-conventions.md)

Convenciones de nombres para features a través de todas las capas.

**Contenido:**
- Naming de entidades (PascalCase, singular)
- Naming de use cases (Create, Get, Update, Delete patterns)
- Naming de repositorios (IEntityRepository, NHEntityRepository)
- Naming de endpoints (EntityEndpoint pattern)
- Naming de DTOs y Models
- Naming de archivos y carpetas
- Plural vs Singular
- Consistencia entre capas

**Cuándo usar:** Al crear nuevos archivos o refactorizar nombres.

---

## Flujo de Trabajo

### Crear un Nuevo Feature CRUD

1. **Planificar estructura** → [Folder Organization](./folder-organization.md)
   - Identificar entidad principal
   - Planear operaciones necesarias (CRUD completo, solo lectura, etc.)

2. **Definir nombres** → [Naming Conventions](./naming-conventions.md)
   - Establecer nombres consistentes en todas las capas
   - Verificar convenciones

3. **Implementar capa por capa** → [Entity to Endpoint Flow](./entity-to-endpoint-flow.md)
   - Domain: Entity + Validator + IRepository
   - Application: Use Cases (Command + Handler)
   - Infrastructure: Repository implementation + Mapper
   - WebApi: Endpoints + Models + DTOs

4. **Verificar flujo completo**
   - Probar cada operación end-to-end
   - Verificar manejo de errores
   - Agregar tests

### Migrar Feature Existente

1. **Analizar estructura actual**
   - Identificar qué capas están implementadas
   - Verificar si sigue convenciones

2. **Reorganizar según estructura estándar** → [Folder Organization](./folder-organization.md)
   - Mover archivos a carpetas correctas
   - Renombrar según convenciones

3. **Actualizar referencias**
   - Actualizar namespaces
   - Actualizar usings
   - Verificar que compile

4. **Verificar funcionamiento**
   - Correr tests
   - Verificar que endpoints funcionen

---

## Anatomía de un Feature

### Feature CRUD Completo (Ejemplo: Users)

```
domain/
├── entities/
│   ├── User.cs                              # Entidad principal
│   └── validators/
│       └── UserValidator.cs                 # FluentValidation
├── daos/
│   └── UserDao.cs                           # DAO para consultas optimizadas (opcional)
└── interfaces/
    └── repositories/
        ├── IUserRepository.cs               # Interface específica del repositorio
        └── IUnitOfWork.cs                   # Agregado a UoW

application/
└── usecases/
    └── users/
        ├── CreateUserUseCase.cs             # Command + Handler
        ├── GetUserUseCase.cs                # Command + Handler
        ├── GetManyAndCountUsersUseCase.cs   # Command + Handler
        ├── UpdateUserUseCase.cs             # Command + Handler
        └── DeleteUserUseCase.cs             # Command + Handler (opcional)

infrastructure/
└── nhibernate/
    ├── NHUserRepository.cs                  # Implementación del repositorio
    └── mappers/
        └── UserMapper.cs                    # NHibernate ClassMapping

webapi/
├── features/
│   └── users/
│       ├── endpoint/
│       │   ├── CreateUserEndpoint.cs        # FastEndpoint
│       │   ├── GetUserEndpoint.cs           # FastEndpoint
│       │   ├── GetManyAndCountUsersEndpoint.cs # FastEndpoint
│       │   └── UpdateUserEndpoint.cs        # FastEndpoint
│       └── models/
│           ├── CreateUserModel.cs           # Request/Response inner classes
│           ├── GetUserModel.cs              # Request/Response inner classes
│           ├── GetManyAndCountModel.cs      # Request/Response inner classes
│           └── UpdateUserModel.cs           # Request/Response inner classes
└── dtos/
    └── UserDto.cs                           # DTO para respuestas API
```

### Feature Solo Lectura (Ejemplo: Reports)

```
# Domain Layer
domain/
├── daos/
│   └── ReportDao.cs                         # Solo DAO, no entidad
└── interfaces/
    └── repositories/
        ├── IReportDaoRepository.cs          # Read-only repository
        └── IUnitOfWork.cs                   # Agregado a UoW

# Application Layer
application/
└── usecases/
    └── reports/
        └── GetReportUseCase.cs              # Solo lectura

# Infrastructure Layer
infrastructure/
└── nhibernate/
    ├── NHReportDaoRepository.cs             # NHReadOnlyRepository<ReportDao, Guid>
    └── mappers/
        └── ReportDaoMapper.cs               # NHibernate ClassMapping

# WebApi Layer
webapi/
├── features/
│   └── reports/
│       ├── endpoint/
│       │   └── GetReportEndpoint.cs         # FastEndpoint
│       └── models/
│           └── GetReportModel.cs            # Request/Response
└── dtos/
    └── ReportDto.cs                         # DTO
```

---

## Checklists Rápidas

### Feature CRUD Completo

**Domain Layer:**
- [ ] `entities/{Entity}.cs` creado
- [ ] `entities/validators/{Entity}Validator.cs` creado
- [ ] `interfaces/repositories/I{Entity}Repository.cs` creado
- [ ] `IUnitOfWork.cs` actualizado con propiedad del repositorio

**Application Layer:**
- [ ] `usecases/{entities}/Create{Entity}UseCase.cs` creado
- [ ] `usecases/{entities}/Get{Entity}UseCase.cs` creado
- [ ] `usecases/{entities}/GetManyAndCount{Entities}UseCase.cs` creado
- [ ] `usecases/{entities}/Update{Entity}UseCase.cs` creado
- [ ] Cada UseCase tiene Command + Handler

**Infrastructure Layer:**
- [ ] `nhibernate/NH{Entity}Repository.cs` creado
- [ ] `nhibernate/mappers/{Entity}Mapper.cs` creado
- [ ] UnitOfWork implementación actualizada

**WebApi Layer:**
- [ ] `features/{entities}/endpoint/Create{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/Get{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/GetManyAndCount{Entities}Endpoint.cs` creado
- [ ] `features/{entities}/endpoint/Update{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/models/Create{Entity}Model.cs` creado
- [ ] `features/{entities}/models/Get{Entity}Model.cs` creado
- [ ] `features/{entities}/models/GetManyAndCountModel.cs` creado
- [ ] `features/{entities}/models/Update{Entity}Model.cs` creado
- [ ] `dtos/{Entity}Dto.cs` creado

**Testing:**
- [ ] Tests de dominio creados
- [ ] Tests de application creados
- [ ] Tests de infrastructure creados
- [ ] Tests de webapi creados

### Feature Read-Only

**Domain Layer:**
- [ ] `daos/{Entity}Dao.cs` creado
- [ ] `interfaces/repositories/I{Entity}DaoRepository.cs` creado
- [ ] `IUnitOfWork.cs` actualizado

**Application Layer:**
- [ ] `usecases/{entities}/Get{Entity}UseCase.cs` creado

**Infrastructure Layer:**
- [ ] `nhibernate/NH{Entity}DaoRepository.cs` creado
- [ ] `nhibernate/mappers/{Entity}DaoMapper.cs` creado

**WebApi Layer:**
- [ ] `features/{entities}/endpoint/Get{Entity}Endpoint.cs` creado
- [ ] `features/{entities}/models/Get{Entity}Model.cs` creado
- [ ] `dtos/{Entity}Dto.cs` creado

---

## Patrones Clave

### 1. Vertical Slicing

Cada feature corta verticalmente todas las capas:
- ✅ Feature = funcionalidad completa de negocio
- ✅ Atraviesa Domain → Application → Infrastructure → WebApi
- ✅ Auto-contenido en su propia carpeta por capa
- ✅ Minimiza acoplamiento entre features

### 2. Naming Consistency

Nombres consistentes a través de capas:
- ✅ Entity: `User`
- ✅ Validator: `UserValidator`
- ✅ Repository Interface: `IUserRepository`
- ✅ Repository Implementation: `NHUserRepository`
- ✅ Use Case: `CreateUserUseCase`
- ✅ Endpoint: `CreateUserEndpoint`
- ✅ Model: `CreateUserModel`
- ✅ DTO: `UserDto`

### 3. Separation by Operation

Cada operación es independiente:
- ✅ Create: Propio UseCase + Endpoint + Model
- ✅ Get: Propio UseCase + Endpoint + Model
- ✅ Update: Propio UseCase + Endpoint + Model
- ✅ Delete: Propio UseCase + Endpoint + Model
- ✅ Facilita mantenimiento y testing

### 4. DAO Pattern

Para consultas optimizadas:
- ✅ DAO = Data Access Object (solo lectura)
- ✅ Usado para listados y reportes
- ✅ No tiene validaciones de dominio
- ✅ Optimizado para queries específicas

---

## Stack Tecnológico

- **.NET 9.0** - Framework
- **C# 13** - Lenguaje
- **FastEndpoints 7.0** - Endpoints
- **FluentValidation 12.0** - Validaciones
- **FluentResults 4.0** - Error handling
- **NHibernate 5.5** - ORM
- **AutoMapper 14.0** - Mapeo

---

## Recursos Adicionales

### Otras Secciones de Guías

- [Best Practices](../best-practices/README.md) - Prácticas generales
- [Domain Layer](../domain-layer/README.md) - Entidades y validaciones
- [Application Layer](../application-layer/README.md) - Use Cases
- [Infrastructure Layer](../infrastructure-layer/README.md) - Repositorios
- [WebApi Layer](../webapi-layer/README.md) - Endpoints

---

## Contribuir

Para documentar nuevos patrones de estructura:
1. Seguir el formato establecido
2. Incluir ejemplos de código real
3. Mostrar estructura de archivos visual
4. Explicar el "por qué" del patrón
5. Agregar checklist

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
