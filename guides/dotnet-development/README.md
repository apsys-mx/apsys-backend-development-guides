# Guías de Desarrollo .NET - Clean Architecture

**Versión:** 0.5.0
**Estado:** En desarrollo
**Última actualización:** 2025-01-14

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
| [README.md](feature-structure/README.md) | ✅ v1.0.0 | Overview de arquitectura de features |
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

### 5. Infrastructure Layer ⏳
**Capa de infraestructura - Persistencia y servicios externos**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](infrastructure-layer/README.md) | ⏳ Pendiente | Overview de la capa de infraestructura |
| [core-concepts.md](infrastructure-layer/core-concepts.md) | ⏳ Pendiente | Conceptos agnósticos de implementación |
| [repository-pattern.md](infrastructure-layer/repository-pattern.md) | ⏳ Pendiente | Repository pattern (agnóstico) |
| [unit-of-work-pattern.md](infrastructure-layer/unit-of-work-pattern.md) | ⏳ Pendiente | UoW pattern (agnóstico) |
| [transactions.md](infrastructure-layer/transactions.md) | ⏳ Pendiente | Manejo de transacciones |
| [dependency-injection.md](infrastructure-layer/dependency-injection.md) | ⏳ Pendiente | DI en infrastructure |

#### 5.1. ORM Implementations
| Guía | Estado | Descripción |
|------|--------|-------------|
| [orm-implementations/README.md](infrastructure-layer/orm-implementations/README.md) | ⏳ Pendiente | Comparación entre ORMs |
| [orm-implementations/nhibernate/README.md](infrastructure-layer/orm-implementations/nhibernate/README.md) | ⏳ Pendiente | NHibernate overview |
| [orm-implementations/nhibernate/repositories.md](infrastructure-layer/orm-implementations/nhibernate/repositories.md) | ⏳ Pendiente | NH*Repository implementations |
| [orm-implementations/nhibernate/mappers.md](infrastructure-layer/orm-implementations/nhibernate/mappers.md) | ⏳ Pendiente | ClassMapping patterns |
| [orm-implementations/nhibernate/queries.md](infrastructure-layer/orm-implementations/nhibernate/queries.md) | ⏳ Pendiente | LINQ, Dynamic LINQ |
| [orm-implementations/nhibernate/unit-of-work.md](infrastructure-layer/orm-implementations/nhibernate/unit-of-work.md) | ⏳ Pendiente | NHibernate UoW |
| [orm-implementations/nhibernate/session-management.md](infrastructure-layer/orm-implementations/nhibernate/session-management.md) | ⏳ Pendiente | Session lifecycle |
| [orm-implementations/nhibernate/best-practices.md](infrastructure-layer/orm-implementations/nhibernate/best-practices.md) | ⏳ Pendiente | NHibernate best practices |
| [orm-implementations/entity-framework/README.md](infrastructure-layer/orm-implementations/entity-framework/README.md) | ⏳ Pendiente | Entity Framework (futuro) |

#### 5.2. External Services
| Guía | Estado | Descripción |
|------|--------|-------------|
| [external-services/README.md](infrastructure-layer/external-services/README.md) | ⏳ Pendiente | Servicios externos overview |
| [external-services/http-clients.md](infrastructure-layer/external-services/http-clients.md) | ⏳ Pendiente | HttpClient patterns |
| [external-services/identity-providers/README.md](infrastructure-layer/external-services/identity-providers/README.md) | ⏳ Pendiente | Auth providers overview |
| [external-services/identity-providers/auth0.md](infrastructure-layer/external-services/identity-providers/auth0.md) | ⏳ Pendiente | Auth0 integration |
| [external-services/identity-providers/custom-jwt.md](infrastructure-layer/external-services/identity-providers/custom-jwt.md) | ⏳ Pendiente | Custom JWT |
| [external-services/caching/README.md](infrastructure-layer/external-services/caching/README.md) | ⏳ Pendiente | Caching overview |
| [external-services/caching/memory-cache.md](infrastructure-layer/external-services/caching/memory-cache.md) | ⏳ Pendiente | Memory cache |
| [external-services/caching/redis.md](infrastructure-layer/external-services/caching/redis.md) | ⏳ Pendiente | Redis (futuro) |

#### 5.3. Data Migrations
| Guía | Estado | Descripción |
|------|--------|-------------|
| [data-migrations/README.md](infrastructure-layer/data-migrations/README.md) | ⏳ Pendiente | Migraciones overview |
| [data-migrations/fluent-migrator/README.md](infrastructure-layer/data-migrations/fluent-migrator/README.md) | ⏳ Pendiente | FluentMigrator overview |
| [data-migrations/fluent-migrator/migration-patterns.md](infrastructure-layer/data-migrations/fluent-migrator/migration-patterns.md) | ⏳ Pendiente | Patrones de migración |
| [data-migrations/fluent-migrator/best-practices.md](infrastructure-layer/data-migrations/fluent-migrator/best-practices.md) | ⏳ Pendiente | FluentMigrator best practices |
| [data-migrations/ef-migrations/README.md](infrastructure-layer/data-migrations/ef-migrations/README.md) | ⏳ Pendiente | EF Migrations (futuro) |

### 6. WebApi Layer ⏳
**Capa de presentación - Endpoints y DTOs**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](webapi-layer/README.md) | ⏳ Pendiente | Overview de la capa WebApi |
| [fastendpoints-basics.md](webapi-layer/fastendpoints-basics.md) | ⏳ Pendiente | Endpoint structure |
| [request-response-models.md](webapi-layer/request-response-models.md) | ⏳ Pendiente | Models pattern |
| [dtos.md](webapi-layer/dtos.md) | ⏳ Pendiente | DTOs vs Models |
| [automapper-profiles.md](webapi-layer/automapper-profiles.md) | ⏳ Pendiente | Mapping configurations |
| [error-responses.md](webapi-layer/error-responses.md) | ⏳ Pendiente | Status codes, ProblemDetails |
| [authentication.md](webapi-layer/authentication.md) | ⏳ Pendiente | JWT, Auth0, policies |
| [swagger-configuration.md](webapi-layer/swagger-configuration.md) | ⏳ Pendiente | Swagger/OpenAPI |

### 7. Examples ⏳
**Ejemplos completos de implementación de features**

| Guía | Estado | Descripción |
|------|--------|-------------|
| [crud-feature/README.md](examples/crud-feature/README.md) | ⏳ Pendiente | Overview de feature CRUD |
| [crud-feature/step-by-step.md](examples/crud-feature/step-by-step.md) | ⏳ Pendiente | Implementación paso a paso de CRUD |
| [read-only-feature/README.md](examples/read-only-feature/README.md) | ⏳ Pendiente | Overview de feature read-only |
| [read-only-feature/step-by-step.md](examples/read-only-feature/step-by-step.md) | ⏳ Pendiente | Implementación paso a paso read-only |
| [complex-feature/README.md](examples/complex-feature/README.md) | ⏳ Pendiente | Overview de feature complejo |
| [complex-feature/step-by-step.md](examples/complex-feature/step-by-step.md) | ⏳ Pendiente | Feature con relaciones |

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

### Para Consulta Rápida

Si necesitas implementar algo específico:

- **Crear un nuevo feature CRUD** → [examples/crud-feature/step-by-step.md](examples/crud-feature/step-by-step.md)
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

**Estado actual:** 0.5.0 - Capas fundamentales + Application Layer completadas

| Sección | Archivos | Completados | Progreso |
|---------|----------|-------------|----------|
| best-practices | 7 | 7 | ✅ 100% |
| feature-structure | 4 | 4 | ✅ 100% |
| domain-layer | 7 | 7 | ✅ 100% |
| application-layer | 5 | 5 | ✅ 100% |
| infrastructure-layer | 6 (core) | 0 | 0% |
| infrastructure-layer/orm-implementations | 9 | 0 | 0% |
| infrastructure-layer/external-services | 8 | 0 | 0% |
| infrastructure-layer/data-migrations | 5 | 0 | 0% |
| webapi-layer | 8 | 0 | 0% |
| examples | 6 | 0 | 0% |
| **TOTAL** | **65** | **23** | **~35%** |

## Versionado

Este conjunto de guías sigue Semantic Versioning:

- **0.x.0** - Versión en desarrollo (pre-release)
- **0.x.y** - PATCH: Correcciones y mejoras menores
- **1.0.0** - Primera versión estable (todas las guías completadas)

### Changelog

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

**Última actualización:** 2025-01-14
**Mantenedor:** Equipo APSYS
