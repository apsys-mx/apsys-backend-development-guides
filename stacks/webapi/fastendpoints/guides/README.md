# FastEndpoints Guides

Guias para implementar APIs REST con FastEndpoints en proyectos .NET.

## Contenido

| Guia | Descripcion |
|------|-------------|
| [Setup](setup.md) | Configuracion inicial de FastEndpoints |
| [FastEndpoints Basics](fastendpoints-basics.md) | Fundamentos y estructura de endpoints |
| [Request Response Models](request-response-models.md) | Modelos de request y response |
| [AutoMapper Profiles](automapper-profiles.md) | Configuracion de mapeos con AutoMapper |
| [Swagger Configuration](swagger-configuration.md) | Documentacion OpenAPI/Swagger |

## Orden Sugerido

1. **Setup** - Configurar FastEndpoints en el proyecto
2. **FastEndpoints Basics** - Entender la estructura de endpoints
3. **Request Response Models** - Definir modelos de entrada/salida
4. **AutoMapper Profiles** - Configurar transformaciones
5. **Swagger Configuration** - Documentar la API

## Prerequisitos

- Proyecto WebAPI con estructura Clean Architecture
- Capa de Application configurada con Use Cases
- Entidades y DTOs definidos

## Estructura de un Endpoint

```
features/{feature}/
├── endpoint/
│   ├── Create{Entity}Endpoint.cs
│   ├── Get{Entity}Endpoint.cs
│   ├── GetManyAndCount{Entities}Endpoint.cs
│   ├── Update{Entity}Endpoint.cs
│   └── Delete{Entity}Endpoint.cs
└── models/
    ├── Create{Entity}Model.cs
    ├── Get{Entity}Model.cs
    └── ...
```

## Referencias

- [WebApi Layer](../../../architectures/clean-architecture/guides/webapi/README.md)
- [DTOs](../../../architectures/clean-architecture/guides/webapi/dtos.md)
- [Feature Structure](../../../architectures/clean-architecture/guides/feature-structure/README.md)
