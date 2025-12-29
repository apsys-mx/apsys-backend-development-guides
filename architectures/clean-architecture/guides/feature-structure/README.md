# Feature Structure en Clean Architecture

Como organizar features (funcionalidades) siguiendo Clean Architecture.

## Guias

| Guia | Descripcion |
|------|-------------|
| [folder-organization.md](./folder-organization.md) | Estructura de carpetas por feature |
| [naming-conventions.md](./naming-conventions.md) | Convenciones de nombres |
| [entity-to-endpoint-flow.md](./entity-to-endpoint-flow.md) | Flujo de una entidad hasta el endpoint |
| [user-story-decomposition.md](./user-story-decomposition.md) | Como descomponer user stories en codigo |

## Estructura por Feature

```
src/{ProjectName}.webapi/
└── features/
    └── users/
        ├── GetUserById/
        │   ├── Endpoint.cs
        │   ├── Request.cs
        │   └── Response.cs
        ├── GetUsers/
        │   └── ...
        └── CreateUser/
            └── ...

src/{ProjectName}.application/
└── usecases/
    └── users/
        ├── GetUserByIdUseCase.cs
        ├── GetUsersUseCase.cs
        └── CreateUserUseCase.cs

src/{ProjectName}.domain/
└── entities/
    ├── User.cs
    └── validators/
        └── UserValidator.cs
```

## Flujo de Datos

```
HTTP Request
    ↓
Endpoint (WebAPI)
    ↓
UseCase (Application)
    ↓
Repository Interface (Domain)
    ↓
Repository Implementation (Infrastructure)
    ↓
Database
```

## Ejemplos Completos

→ Ver: [examples/](../../examples/)

- CRUD Feature
- Read-Only Feature
- Complex Feature
