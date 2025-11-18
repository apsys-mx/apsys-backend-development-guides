# Guías de Desarrollo .NET - Clean Architecture

**Versión:** 1.0.0-rc3
**Estado:** Release Candidate - 97% completado
**Última actualización:** 2025-01-18

## Descripción

Conjunto completo de guías de desarrollo que documentan patrones, prácticas y convenciones para la construcción de aplicaciones backend .NET con Clean Architecture. Estas guías están diseñadas para el **desarrollo diario de features/módulos de negocio** en proyectos ya configurados con la arquitectura base.

A diferencia de las guías de setup (`init-clean-architecture`, `configure-database`) que se enfocan en la **configuración inicial** del proyecto, estas guías se centran en el **desarrollo diario** de funcionalidades específicas.

## Propósito

- Documentar patrones y prácticas observadas en proyectos reales de APSYS
- Estandarizar la forma de desarrollar features/módulos en Clean Architecture
- Servir como referencia durante el desarrollo de nuevas funcionalidades
- Facilitar la incorporación de nuevos desarrolladores al equipo
- Mantener consistencia arquitectónica entre proyectos


## Prerequisitos

Estas guías asumen que tu proyecto ya tiene:

- Solución .NET con estructura de Clean Architecture
- Gestión centralizada de paquetes (Directory.Packages.props)
- Capas: Domain, Application, Infrastructure, WebApi
- Sistema de migraciones configurado
- Framework de testing configurado

Si tu proyecto no tiene estos elementos, consulta primero:
- [init-clean-architecture](../init-clean-architecture/README.md)
- [configure-database](../configure-database/README.md)

## Secciones de Guías

### 1. Best Practices ✅
**Prácticas generales de desarrollo en .NET**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](best-practices/README.md) | ✅ v1.0.0 | Overview de mejores prácticas |
| [clean-architecture-principles.md](best-practices/clean-architecture-principles.md) | ✅ v1.0.0 | Principios de Clean Architecture |
| [code-organization.md](best-practices/code-organization.md) | ✅ v1.0.0 | Organización de namespaces, usings, naming |
| [async-await-patterns.md](best-practices/async-await-patterns.md) | ✅ v1.0.0 | Patrones de async/await |
| [error-handling.md](best-practices/error-handling.md) | ✅ v1.0.0 | FluentResults, excepciones |
| [dependency-injection.md](best-practices/dependency-injection.md) | ✅ v1.0.0 | DI patterns y best practices |
| [testing-conventions.md](best-practices/testing-conventions.md) | ✅ v1.0.0 | Convenciones de testing |

### 2. Feature Structure ✅
**Estructura y organización de features/módulos**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](feature-structure/README.md) | ✅ v1.1.0 | Overview de arquitectura de features |
| [user-story-decomposition.md](feature-structure/user-story-decomposition.md) | ✅ v1.0.0 | Análisis y descomposición de user stories en subtasks |
| [folder-organization.md](feature-structure/folder-organization.md) | ✅ v1.0.0 | Estructura de carpetas estándar |
| [entity-to-endpoint-flow.md](feature-structure/entity-to-endpoint-flow.md) | ✅ v1.0.0 | Flujo completo de una operación |
| [naming-conventions.md](feature-structure/naming-conventions.md) | ✅ v1.0.0 | Convenciones de nombres |

### 3. Domain Layer ✅
**Capa de dominio - Entidades y reglas de negocio**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](domain-layer/README.md) | ✅ v1.0.0 | Overview de la capa de dominio |
| [entities.md](domain-layer/entities.md) | ✅ v1.0.0 | Entidades, AbstractDomainObject |
| [validators.md](domain-layer/validators.md) | ✅ v1.0.0 | FluentValidation patterns |
| [repository-interfaces.md](domain-layer/repository-interfaces.md) | ✅ v1.0.0 | IRepository, IUnitOfWork |
| [daos.md](domain-layer/daos.md) | ✅ v1.0.0 | DAO pattern para consultas |
| [domain-exceptions.md](domain-layer/domain-exceptions.md) | ✅ v1.0.0 | Custom exceptions |
| [value-objects.md](domain-layer/value-objects.md) | ✅ v1.0.0 | Value Objects pattern |

### 4. Application Layer ✅
**Capa de aplicación - Use Cases y lógica de negocio**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](application-layer/README.md) | ✅ v1.0.0 | Overview de la capa de aplicación |
| [use-cases.md](application-layer/use-cases.md) | ✅ v1.0.0 | Command/Handler pattern |
| [command-handler-patterns.md](application-layer/command-handler-patterns.md) | ✅ v1.0.0 | Create, Update, Delete, GetMany |
| [error-handling.md](application-layer/error-handling.md) | ✅ v1.0.0 | FluentResults usage |
| [common-utilities.md](application-layer/common-utilities.md) | ✅ v1.0.0 | Utilidades compartidas |

### 5. Infrastructure Layer ✅
**Capa de infraestructura - Persistencia y servicios externos**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](infrastructure-layer/README.md) | ✅ v1.1.0 | Overview de la capa de infraestructura |
| [core-concepts.md](infrastructure-layer/core-concepts.md) | ✅ v1.0.0 | Conceptos agnósticos de implementación |
| [repository-pattern.md](infrastructure-layer/repository-pattern.md) | ✅ v1.0.0 | Repository pattern (agnóstico) |
| [unit-of-work-pattern.md](infrastructure-layer/unit-of-work-pattern.md) | ✅ v1.0.0 | UoW pattern (agnóstico) |
| [transactions.md](infrastructure-layer/transactions.md) | ✅ v1.0.0 | Manejo de transacciones |
| [dependency-injection.md](infrastructure-layer/dependency-injection.md) | ✅ v1.0.0 | DI en infrastructure |

#### 5.1. ORM Implementations
| Guía | Estado | Descripción |
|------|--------|-------------|
| [orm-implementations/README.md](infrastructure-layer/orm-implementations/README.md) | ✅ v1.0.0 | Comparación entre ORMs |
| [orm-implementations/nhibernate/README.md](infrastructure-layer/orm-implementations/nhibernate/README.md) | ✅ v1.0.0 | NHibernate overview |
| [orm-implementations/nhibernate/repositories.md](infrastructure-layer/orm-implementations/nhibernate/repositories.md) | ✅ v1.0.0 | NH*Repository implementations |
| [orm-implementations/nhibernate/mappers.md](infrastructure-layer/orm-implementations/nhibernate/mappers.md) | ✅ v1.0.0 | ClassMapping patterns |
| [orm-implementations/nhibernate/queries.md](infrastructure-layer/orm-implementations/nhibernate/queries.md) | ✅ v1.0.0 | LINQ, Dynamic LINQ |
| [orm-implementations/nhibernate/unit-of-work.md](infrastructure-layer/orm-implementations/nhibernate/unit-of-work.md) | ✅ v1.0.0 | NHibernate UoW |
| [orm-implementations/nhibernate/session-management.md](infrastructure-layer/orm-implementations/nhibernate/session-management.md) | ✅ v1.0.0 | Session lifecycle |
| [orm-implementations/nhibernate/best-practices.md](infrastructure-layer/orm-implementations/nhibernate/best-practices.md) | ✅ v1.0.0 | NHibernate best practices |
| [orm-implementations/entity-framework/README.md](infrastructure-layer/orm-implementations/entity-framework/README.md) | ⏳ Pendiente | Entity Framework (futuro) |

#### 5.2. External Services
| Guía | Estado | Descripción |
|------|--------|-------------|
| [external-services/README.md](infrastructure-layer/external-services/README.md) | ✅ v1.1.0 | Overview de External Services |
| [external-services/http-clients.md](infrastructure-layer/external-services/http-clients.md) | ✅ v1.0.0 | IHttpClientFactory patterns y best practices |
| [external-services/identity-providers/README.md](infrastructure-layer/external-services/identity-providers/README.md) | ✅ v1.0.0 | Overview de proveedores de identidad |
| [external-services/identity-providers/auth0.md](infrastructure-layer/external-services/identity-providers/auth0.md) | ✅ v1.0.0 | Integración completa con Auth0 |
| [external-services/identity-providers/custom-jwt.md](infrastructure-layer/external-services/identity-providers/custom-jwt.md) | ✅ v1.0.0 | Implementación de JWT personalizado |
| [external-services/caching/README.md](infrastructure-layer/external-services/caching/README.md) | ✅ v1.0.0 | Overview de caching (Memory, Distributed, Response, Output) |
| [external-services/caching/memory-cache.md](infrastructure-layer/external-services/caching/memory-cache.md) | ✅ v1.0.0 | IMemoryCache: patrones, expiration, callbacks |
| [external-services/caching/redis.md](infrastructure-layer/external-services/caching/redis.md) | ✅ v1.0.0 | Redis: IDistributedCache, StackExchange.Redis, Pub/Sub |

#### 5.3. Data Migrations
| Guía | Estado | Descripción |
|------|--------|-------------|
| [data-migrations/README.md](infrastructure-layer/data-migrations/README.md) | ✅ v1.0.0 | Migraciones overview |
| [data-migrations/fluent-migrator/README.md](infrastructure-layer/data-migrations/fluent-migrator/README.md) | ✅ v1.0.0 | FluentMigrator setup y configuración |
| [data-migrations/fluent-migrator/migration-patterns.md](infrastructure-layer/data-migrations/fluent-migrator/migration-patterns.md) | ✅ v1.0.0 | Patrones de migración (tablas, índices, vistas, FK) |
| [data-migrations/fluent-migrator/best-practices.md](infrastructure-layer/data-migrations/fluent-migrator/best-practices.md) | ✅ v1.0.0 | FluentMigrator best practices |
| [data-migrations/ef-migrations/README.md](infrastructure-layer/data-migrations/ef-migrations/README.md) | ⏳ Futuro | EF Migrations (futuro) |

### 6. WebApi Layer ✅
**Capa de presentación - Endpoints y DTOs**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](webapi-layer/README.md) | ✅ v1.0.0 | Overview de la capa WebApi |
| [fastendpoints-basics.md](webapi-layer/fastendpoints-basics.md) | ✅ v1.0.0 | Endpoint structure |
| [request-response-models.md](webapi-layer/request-response-models.md) | ✅ v1.0.0 | Models pattern |
| [dtos.md](webapi-layer/dtos.md) | ✅ v1.0.0 | DTOs vs Models |
| [automapper-profiles.md](webapi-layer/automapper-profiles.md) | ✅ v1.0.0 | Mapping configurations |
| [error-responses.md](webapi-layer/error-responses.md) | ✅ v1.0.0 | Status codes, ProblemDetails |
| [authentication.md](webapi-layer/authentication.md) | ✅ v1.0.0 | JWT, Auth0, policies |
| [swagger-configuration.md](webapi-layer/swagger-configuration.md) | ✅ v1.0.0 | Swagger/OpenAPI |

### 7. Examples ✅
**Ejemplos completos de implementación de features**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [crud-feature/README.md](examples/crud-feature/README.md) | ✅ v1.0.0 | Overview de feature CRUD completo |
| [crud-feature/step-by-step.md](examples/crud-feature/step-by-step.md) | ✅ v1.0.0 | Implementación paso a paso de CRUD |
| [read-only-feature/README.md](examples/read-only-feature/README.md) | ✅ v1.0.0 | Overview de feature read-only con DAOs |
| [read-only-feature/step-by-step.md](examples/read-only-feature/step-by-step.md) | ✅ v1.0.0 | Implementación paso a paso read-only |
| [complex-feature/README.md](examples/complex-feature/README.md) | ✅ v1.0.0 | Overview de feature con entity relationships |
| [complex-feature/step-by-step.md](examples/complex-feature/step-by-step.md) | ✅ v1.0.0 | Feature con relaciones many-to-many |
| [integration-testing/README.md](examples/integration-testing/README.md) | ✅ v1.0.0 | Sistema de Scenarios para integration testing |
| [integration-testing/step-by-step.md](examples/integration-testing/step-by-step.md) | ✅ v1.0.0 | Implementación paso a paso del Scenarios System |

## Orden de Lectura Recomendado

### Para Nuevos Desarrolladores

Si eres nuevo en el equipo o en Clean Architecture, sigue este orden:

1. **Fundamentos**
   - [best-practices/README.md](best-practices/README.md)
   - [best-practices/clean-architecture-principles.md](best-practices/clean-architecture-principles.md)
   - [best-practices/code-organization.md](best-practices/code-organization.md)

2. **Estructura de Features**
   - [feature-structure/README.md](feature-structure/README.md)
   - [feature-structure/folder-organization.md](feature-structure/folder-organization.md)
   - [feature-structure/entity-to-endpoint-flow.md](feature-structure/entity-to-endpoint-flow.md)

3. **Capa de Dominio**
   - [domain-layer/README.md](domain-layer/README.md)
   - [domain-layer/entities.md](domain-layer/entities.md)
   - [domain-layer/validators.md](domain-layer/validators.md)
   - [domain-layer/repository-interfaces.md](domain-layer/repository-interfaces.md)

4. **Capa de Aplicación**
   - [application-layer/README.md](application-layer/README.md)
   - [application-layer/use-cases.md](application-layer/use-cases.md)
   - [application-layer/command-handler-patterns.md](application-layer/command-handler-patterns.md)

5. **Capa de Infraestructura**
   - [infrastructure-layer/README.md](infrastructure-layer/README.md)
   - [infrastructure-layer/orm-implementations/nhibernate/README.md](infrastructure-layer/orm-implementations/nhibernate/README.md)

6. **Capa de WebApi**
   - [webapi-layer/README.md](webapi-layer/README.md)
   - [webapi-layer/fastendpoints-basics.md](webapi-layer/fastendpoints-basics.md)

7. **Práctica**
   - [examples/crud-feature/step-by-step.md](examples/crud-feature/step-by-step.md)
   - [examples/read-only-feature/step-by-step.md](examples/read-only-feature/step-by-step.md)
   - [examples/complex-feature/step-by-step.md](examples/complex-feature/step-by-step.md)

### Para Consulta Rápida

Si necesitas implementar algo específico:

- **Descomponer user story en subtasks** → [feature-structure/user-story-decomposition.md](feature-structure/user-story-decomposition.md)
- **Crear un nuevo feature CRUD** → [examples/crud-feature/step-by-step.md](examples/crud-feature/step-by-step.md)
- **Crear feature read-only (consultas)** → [examples/read-only-feature/step-by-step.md](examples/read-only-feature/step-by-step.md)
- **Feature con relaciones entre entidades** → [examples/complex-feature/step-by-step.md](examples/complex-feature/step-by-step.md)
- **Setup de integration testing con Scenarios** → [examples/integration-testing/step-by-step.md](examples/integration-testing/step-by-step.md)
- **Agregar entidad de dominio** → [domain-layer/entities.md](domain-layer/entities.md)
- **Crear use case** → [application-layer/use-cases.md](application-layer/use-cases.md)
- **Implementar repository** → [infrastructure-layer/orm-implementations/nhibernate/repositories.md](infrastructure-layer/orm-implementations/nhibernate/repositories.md)
- **Crear endpoint** → [webapi-layer/fastendpoints-basics.md](webapi-layer/fastendpoints-basics.md)
- **Manejo de errores** → [best-practices/error-handling.md](best-practices/error-handling.md)

## Anatomía de un Feature Típico

Basado en los patrones documentados, un feature típico tiene esta estructura:

```
# Domain Layer
domain/
├── entities/
│   ├── User.cs                              # Entidad de dominio
│   └── validators/
│       └── UserValidator.cs                 # FluentValidation
├── daos/
│   └── UserDao.cs                           # DAO para consultas optimizadas
└── interfaces/
    └── repositories/
        ├── IUserRepository.cs               # Interface del repositorio
        └── IUnitOfWork.cs                   # Unit of Work

# Application Layer
application/
└── usecases/
    └── users/
        ├── CreateUserUseCase.cs             # Command + Handler
        ├── GetUserUseCase.cs
        ├── GetManyAndCountUsersUseCase.cs
        └── UpdateUserUseCase.cs

# Infrastructure Layer
infrastructure/
└── nhibernate/
    ├── NHUserRepository.cs                  # Implementación del repositorio
    └── mappers/
        └── UserMapper.cs                    # NHibernate mapping

# WebApi Layer
webapi/
├── features/
│   └── users/
│       ├── endpoint/
│       │   ├── CreateUserEndpoint.cs        # FastEndpoints
│       │   ├── GetUserEndpoint.cs
│       │   ├── GetManyAndCountUsersEndpoint.cs
│       │   └── UpdateUserEndpoint.cs
│       └── models/
│           ├── CreateUserModel.cs           # Request/Response
│           ├── GetUserModel.cs
│           └── UpdateUserModel.cs
└── dtos/
    └── UserDto.cs                           # DTO para respuestas
```

## Progreso de Desarrollo

**Estado actual:** 1.0.0-rc1 - Todas las guías principales completadas ✅

| Sección | Archivos | Completados | Progreso |
|---------|----------|-------------|----------|
| best-practices | 7 | 7 | ✅ 100% |
| feature-structure | 5 | 5 | ✅ 100% |
| domain-layer | 7 | 7 | ✅ 100% |
| application-layer | 5 | 5 | ✅ 100% |
| infrastructure-layer | 6 (core) | 6 | ✅ 100% |
| infrastructure-layer/orm-implementations | 9 | 8 | ⏳ ~89% (EF futuro) |
| infrastructure-layer/external-services | 8 | 8 | ✅ 100% |
| infrastructure-layer/data-migrations | 5 | 4 | ⏳ 80% (EF Migrations futuro) |
| webapi-layer | 8 | 8 | ✅ 100% |
| examples | 8 | 8 | ✅ 100% |
| **TOTAL** | **68** | **66** | **~97%** |

## Versionado

Este conjunto de guías sigue Semantic Versioning:

- **0.x.0** - Versión en desarrollo (pre-release)
- **0.x.y** - PATCH: Correcciones y mejoras menores
- **1.0.0** - Primera versión estable (todas las guías completadas)

### Changelog

#### v1.0.0-rc3 (2025-01-18)
- ✅ **Nueva guía: User Story Decomposition** - feature-structure/user-story-decomposition.md
  - Metodología completa de análisis y descomposición de user stories
  - Quick reference checklist para identificar componentes
  - Matriz de decisión Entity vs DAO
  - Templates de subtasks por capa (Domain, Infrastructure, Application, WebApi, Integration)
  - Factores de complejidad para ajustar estimaciones
  - Definition of Done por capa
  - 3 ejemplos completos: Read-Only, CRUD Simple, CRUD Compleja
  - 8 antipatrones documentados con correcciones
- Progreso total: 97% (66/68 guías)

#### v1.0.0-rc2 (2025-01-15) 🎉
- ✅ **Integration Testing Guides Completadas** - 8/8 guías de examples (100%)
  - **integration-testing/README.md**: Sistema de Scenarios completo (2,094 líneas)
    - Intención y objetivo: Evitar preparar BD con clases en desarrollo
    - Anatomía del sistema: IScenario, AppSchema, NDbUnit, ScenarioBuilder
    - Componentes clave: INDbUnit interface, PostgreSQLNDbUnit implementation
    - Diseño de scenarios: Principios fundamentales (Single Responsibility, Predictable Data, Minimal Dependencies)
    - Prácticas del desarrollador: Naming, transactions, data design, defensive coding
    - Integración con desarrollo: Scenarios como parte del feature development workflow
    - 6 patrones de diseño: Base, Foundation, Bulk Creation, Modification, Relationship, Domain Data
    - 7 anti-patrones documentados con correcciones
    - Ejemplos completos: User Management, Technical Standards
    - Checklist de implementación completa
  - **integration-testing/step-by-step.md**: Implementación paso a paso (1,521 líneas)
    - 7 fases de implementación: Setup, NDbUnit, ScenarioBuilder, Base Classes, Scenarios, Generation, Tests
    - Implementación completa de INDbUnit, NDbUnit, PostgreSQLNDbUnit
    - AppSchema.xsd con Typed DataSet generation
    - AppSchemaExtender con helper methods
    - ScenarioBuilder con dependency injection
    - NHRepositoryTestBase y EndpointTestBase
    - 4 scenarios de ejemplo: Sc010CreateSandBox, Sc020CreateRoles, Sc030CreateUsers, Sc031CreateAdminUser
    - Generación de XML snapshots
    - Repository y Endpoint tests usando scenarios
    - Troubleshooting: 7 errores comunes con soluciones
- Progreso total: 97% (65/67 guías)

#### v1.0.0-rc1 (2025-11-15) 🎉
- ✅ **Examples Section (4 guías iniciales)** - 6/6 guías (100%)
  - **read-only-feature/README.md**: Overview completo del patrón read-only (1,299 líneas)
    - Análisis del feature TechnicalStandards del proyecto de referencia
    - Patrón DAO vs Entity: cuándo usar cada uno
    - Anatomía completa: 13 archivos organizados por capa
    - IReadOnlyRepository<T> interface sin métodos de escritura
    - NHibernate mapper con Mutable(false)
    - Database views con campos pre-computados
    - 2 diagramas de flujo de datos (Get Single, Get Many)
    - 10 mejores prácticas específicas para read-only
    - Checklist de implementación con 25+ items
  - **read-only-feature/step-by-step.md**: Guía paso a paso (1,521 líneas)
    - 6 fases de implementación
    - 13 archivos con código completo
    - Énfasis en diferencias vs CRUD (NO validators, NO write endpoints)
    - Database view creation con SearchAll computed field
    - 8 common pitfalls con soluciones
  - **complex-feature/README.md**: Overview de features con entity relationships (1,849 líneas)
    - Análisis del feature User-Role del proyecto de referencia
    - 3 tipos de relaciones: One-to-Many, Many-to-Many, One-to-One
    - Patrón Many-to-Many con join table y composite primary key
    - NHibernate Bag() configuration para IList<T>
    - Cascade operations (All, SaveUpdate, AllDeleteOrphan)
    - Lazy vs Eager Loading estrategias
    - Navigation properties y virtual keyword
    - Flattened DTOs (IEnumerable<string> en lugar de IEnumerable<Role>)
    - AutoMapper custom mapping para collections
    - 10 mejores prácticas para relaciones
    - Checklist de implementación con 50+ items
  - **complex-feature/step-by-step.md**: Guía paso a paso (2,094 líneas)
    - 6 fases de implementación
    - 25 archivos con código completo
    - User entity con IList<Role> Roles navigation property
    - UserMapper con Bag() y ManyToMany() configuration
    - M024CreateUsersTable migration con join table user_in_roles
    - AddUsersToRoleUseCase y RemoveUserFromRoleUseCase
    - UserDto con flattened Roles property
    - 8 common pitfalls con soluciones
    - Verificación de cascade operations
- **Milestone alcanzado**: Todas las guías de NHibernate completadas
- Progreso total: 97% (63/65 guías)
- **Pendiente**: Solo guías de Entity Framework (futuro)

#### v0.9.1 (2025-11-15)
- ✅ **Examples - CRUD Feature Completado** - 2/6 guías
  - crud-feature/README.md: Overview completo del patrón CRUD (1,393 líneas)
  - crud-feature/step-by-step.md: Guía paso a paso completa (1,420+ líneas)
- Progreso: 89% (58/65 guías)

#### v0.9.0 (2025-11-15)
- ✅ **WebApi Layer COMPLETADA** - 8/8 guías (100%)
  - Todas las guías técnicas completadas: FastEndpoints, Request/Response Models, DTOs, AutoMapper, Error Responses, Authentication, Swagger
  - README.md overview con flujo completo de datos, patrones principales, y mejores prácticas
- **Milestone alcanzado**: Todas las capas fundamentales completadas (Best Practices, Feature Structure, Domain, Application, Infrastructure Core, WebApi)
- Progreso total: 86% (56/65 guías)
- **Siguiente objetivo**: Examples section (6 guías, 0% → 100%)

#### v0.8.3 (2025-11-15)
- ✅ WebApi Layer - Siguientes 3 guías completadas (7/8 guías, 88%)
  - Error Responses: HTTP status codes, ProblemDetails, BaseEndpoint helpers, FluentResults integration
  - Authentication: JWT Bearer con Auth0, custom authorization handlers, claims, policies
  - Swagger Configuration: Description vs Summary, tags, OpenAPI metadata, SwaggerUI customization
- Progreso total: 85% (55/65 guías)

#### v0.8.2 (2025-11-15)
- ✅ WebApi Layer - Primeras 4 guías completadas (4/8 guías, 50%)
  - FastEndpoints basics: Estructura de endpoints, configuración, binding
  - Request/Response Models: Patrón de nested classes, integración con FastEndpoints
  - DTOs: Data Transfer Objects vs Entities vs Models
  - AutoMapper Profiles: Configuración, CreateMap, ForMember, ProjectTo
- Progreso total: 80% (52/65 guías)

#### v0.8.1 (2025-11-15)
- ✅ Infrastructure Layer - ORM Implementations README completado (8/9 guías, 89%)
  - Comparación completa NHibernate vs Entity Framework Core
  - Matriz de decisión para selección de ORM
  - Justificación técnica de decisión APSYS
  - Guía de migración entre ORMs
- Progreso total: 75% (49/65 guías)

#### v0.8.0 (2025-11-14)
- ✅ Infrastructure Layer - Data Migrations completado (4/5 guías, 80%)
  - Data Migrations overview
  - FluentMigrator setup y configuración
  - FluentMigrator migration patterns (25 patrones)
  - FluentMigrator best practices
- Progreso total: 74% (48/65 guías)

#### v0.7.0 (2025-11-14)
- ✅ Infrastructure Layer - External Services completado (8/8 guías)
- Progreso total: 68% (44/65 guías)

#### v0.6.0 (2025-11-14)
- ✅ Infrastructure Layer - Core Concepts completado (6/6 guías)
- Progreso total: 55% (36/65 guías)

#### v0.5.0 (2025-01-14)
- ✅ Application Layer completado (5/5 guías)
- Progreso total: 35% (23/65 guías)

#### v0.4.0 (2025-01-14)
- ✅ Best Practices completado (7/7 guías)
- ✅ Feature Structure completado (4/4 guías)
- ✅ Domain Layer completado (7/7 guías)
- Progreso total: 18/65 guías (~28%)

#### v0.1.0 (2025-01-13)
- Estructura inicial creada
- 65 archivos de guías preparados
- Metadata y versionado establecido

## Stack Tecnológico

Estas guías asumen el siguiente stack:

- **.NET 9.0** - Framework
- **C# 13** - Lenguaje
- **FastEndpoints 7.0** - API framework
- **FluentValidation 12.0** - Validaciones
- **FluentResults 4.0** - Manejo de errores
- **NHibernate 5.5** - ORM (actual)
- **AutoMapper 14.0** - Mapeo de objetos
- **FluentMigrator 7.1** - Migraciones
- **NUnit 4.2** - Testing framework
- **Moq 4.20** - Mocking
- **AutoFixture 4.18** - Test data generation

## Contribución

Al desarrollar estas guías, ten en cuenta:

1. **Basarse en código real** - Todos los patrones deben ser probados en proyectos reales
2. **Incluir ejemplos prácticos** - Código real, no pseudocódigo
3. **Explicar el "por qué"** - No solo el "cómo", sino la razón detrás del patrón
4. **Mantener consistencia** - Seguir el formato establecido en otras guías
5. **Actualizar progreso** - Marcar guías como completadas en este README

## Referencias

- **Guías de Setup**: [../init-clean-architecture/](../init-clean-architecture/)

---

**Última actualización:** 2025-01-18
**Mantenedor:** Equipo APSYS
