# Best Practices

Mejores practicas generales para desarrollo en .NET. Aplicables independientemente de la arquitectura o stack.

## Guias

| Guia | Descripcion |
|------|-------------|
| [async-await-patterns.md](./async-await-patterns.md) | Patrones de programacion asincrona |
| [code-organization.md](./code-organization.md) | Organizacion de codigo y namespaces |
| [dependency-injection.md](./dependency-injection.md) | Inyeccion de dependencias |
| [error-handling.md](./error-handling.md) | Manejo de errores y excepciones |

## Principios Generales

### Async/Await
- Usar `async` hasta el final (no bloquear con `.Result` o `.Wait()`)
- Sufijo `Async` en metodos asincronos
- `CancellationToken` en operaciones largas

### Dependency Injection
- Constructor injection preferido
- Lifetimes apropiados (Scoped para DB, Singleton para config)
- Interfaces para testabilidad

### Error Handling
- Excepciones para situaciones excepcionales
- Validacion temprana (fail fast)
- Mensajes de error descriptivos

### Code Organization
- Un concepto por archivo
- Namespaces que reflejan la estructura de carpetas
- Nombres descriptivos

## Aplicacion por Arquitectura

| Arquitectura | Donde aplican |
|--------------|---------------|
| Clean Architecture | Todas las capas |
| Hexagonal | Puertos y adaptadores |
| Vertical Slices | Cada feature |
