# WebApi Layer - Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

La capa de WebApi es la capa de **presentación** que expone los endpoints HTTP. Utiliza FastEndpoints para definir endpoints, maneja la serialización/deserialización, y traduce entre los models de la API y los comandos de Application.

## Responsabilidades

- Definir endpoints HTTP (FastEndpoints)
- Request/Response models
- DTOs para respuestas
- AutoMapper profiles
- Manejo de errores HTTP
- Autenticación y autorización
- Swagger/OpenAPI documentation

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [fastendpoints-basics.md](./fastendpoints-basics.md) | ⏳ Pendiente | Endpoint structure |
| [request-response-models.md](./request-response-models.md) | ⏳ Pendiente | Models pattern |
| [dtos.md](./dtos.md) | ⏳ Pendiente | DTOs vs Models |
| [automapper-profiles.md](./automapper-profiles.md) | ⏳ Pendiente | Mapping configurations |
| [error-responses.md](./error-responses.md) | ⏳ Pendiente | Status codes, ProblemDetails |
| [authentication.md](./authentication.md) | ⏳ Pendiente | JWT, Auth0, policies |
| [swagger-configuration.md](./swagger-configuration.md) | ⏳ Pendiente | Swagger/OpenAPI |


---

**Última actualización:** 2025-01-13
