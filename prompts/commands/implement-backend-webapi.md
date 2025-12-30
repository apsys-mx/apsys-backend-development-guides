# Implement Backend Application + WebAPI Layer

> **Version Comando:** 2.0.0
> **Ultima actualizacion:** 2025-12-30

---

Implementa las capas de Application y WebAPI Layer siguiendo el plan de implementacion y las guias de desarrollo de APSYS.

## Entrada

**Contexto:** $ARGUMENTS

- Si se proporciona un archivo de plan (`.claude/planning/*-implementation-plan.md`), extrae las secciones "Fase 3: Application Layer" y "Fase 4: WebAPI Layer"
- Si se proporciona una descripcion directa, usala como contexto
- Si `$ARGUMENTS` esta vacio, pregunta al usuario que desea implementar

## Configuracion

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Guias a Consultar

Antes de implementar, lee las guias relevantes desde `{GUIDES_REPO}`:

### Application Layer

| Guia | Ruta | Cuando Usar |
|------|------|-------------|
| Use Cases | `architectures/clean-architecture/guides/application/use-cases.md` | Siempre |
| Command Handler Patterns | `architectures/clean-architecture/guides/application/command-handler-patterns.md` | Siempre |

### WebAPI Layer

| Guia | Ruta | Cuando Usar |
|------|------|-------------|
| FastEndpoints Basics | `stacks/webapi/fastendpoints/guides/fastendpoints-basics.md` | Siempre |
| Request/Response Models | `stacks/webapi/fastendpoints/guides/request-response-models.md` | Siempre |
| DTOs | `architectures/clean-architecture/guides/webapi/dtos.md` | Siempre |
| AutoMapper Profiles | `stacks/webapi/fastendpoints/guides/automapper-profiles.md` | Siempre |

---

## Proceso de Implementacion

### Paso 1: Analizar el Plan

Extrae del plan o contexto:

- Entidad principal
- Endpoints a implementar (Create, Get, GetMany, Update, Delete)
- DTOs requeridos
- Use Cases con Commands/Queries
- Rutas de los endpoints

### Paso 2: Explorar Proyecto Actual

Busca implementaciones existentes como referencia:

```bash
# DTOs existentes
Glob: **/dtos/*Dto.cs

# Use Cases existentes
Glob: **/usecases/**/*UseCase.cs

# Endpoints existentes
Glob: **/features/**/*Endpoint.cs

# Mapping Profiles
Glob: **/mappingprofiles/*MappingProfile.cs

# Request/Response Models
Glob: **/features/**/models/*Model.cs
```

### Paso 3: Implementar Componentes

Implementa en este orden:

#### 3.1 DTO

**Archivo:** `{proyecto}.webapi/dtos/{Entity}Dto.cs`

Siguiendo la guia `dtos.md`:
- Solo propiedades (sin logica)
- Strings inicializados con `string.Empty`
- Colecciones con `Enumerable.Empty<T>()`

#### 3.2 Use Cases

**Archivo:** `{proyecto}.application/usecases/{entity}/{Action}{Entity}UseCase.cs`

Siguiendo la guia `use-cases.md`:
- Clase interna `Command` o `Query` con propiedades de entrada
- Metodo `ExecuteAsync` que retorna `Result<T>`
- Use Cases son **thin wrappers** - solo orquestacion, NO logica de negocio
- Inyectar repositorio via constructor

#### 3.3 Request/Response Models

**Archivo:** `{proyecto}.webapi/features/{entity}/models/{Action}{Entity}Model.cs`

Siguiendo la guia `request-response-models.md`:
- Clase contenedora con `Request` y `Response` anidados
- Request tiene propiedades de entrada
- Response contiene DTO o lista de DTOs

#### 3.4 Mapping Profile

**Archivo:** `{proyecto}.webapi/mappingprofiles/{Entity}MappingProfile.cs`

Siguiendo la guia `automapper-profiles.md`:
- Entity → DTO
- Request → Command/Query
- Entity/Result → Response

#### 3.5 Endpoints

**Archivo:** `{proyecto}.webapi/features/{entity}/endpoint/{Action}{Entity}Endpoint.cs`

Siguiendo la guia `fastendpoints-basics.md`:
- Heredar de `Endpoint<TRequest, TResponse>`
- Configurar ruta y permisos en `Configure()`
- Implementar `HandleAsync()`
- Mapear Request → Command → UseCase → Response
- Manejar errores con codigos HTTP correctos

#### 3.6 Validators (FastEndpoints)

**Archivo:** En el mismo archivo del Endpoint

- Heredar de `Validator<{Action}{Entity}Model.Request>`
- Reglas con FluentValidation
- Mensajes de error descriptivos

#### 3.7 Registrar Use Cases

**Archivo a modificar:** `Program.cs` o archivo de configuracion DI

- Agregar: `services.AddScoped<{Action}{Entity}UseCase>();`

### Paso 4: Verificar

- [ ] Codigo compila sin errores
- [ ] DTOs solo tienen propiedades
- [ ] Use Cases son thin wrappers
- [ ] Endpoints manejan errores correctamente
- [ ] Mapping Profile tiene todos los mapeos
- [ ] Validators tienen todas las reglas
- [ ] Use Cases registrados en DI

---

## Formato de Salida

Al finalizar, muestra:

```markdown
## Application + WebAPI Layer Implementado

### DTOs Creados

| Archivo | Descripcion |
|---------|-------------|
| `{proyecto}.webapi/dtos/{Entity}Dto.cs` | DTO con {n} propiedades |

### Use Cases Creados

| Archivo | Tipo | Descripcion |
|---------|------|-------------|
| `Create{Entity}UseCase.cs` | Command | Crear entidad |
| `Get{Entity}UseCase.cs` | Query | Obtener por ID |
| `GetManyAndCount{Entities}UseCase.cs` | Query | Listar con paginacion |
| `Update{Entity}UseCase.cs` | Command | Actualizar entidad |

### Endpoints Creados

| Endpoint | Metodo | Ruta |
|----------|--------|------|
| `Create{Entity}Endpoint` | POST | `/{entities}` |
| `Get{Entity}Endpoint` | GET | `/{entities}/{id}` |
| `GetManyAndCount{Entities}Endpoint` | GET | `/{entities}` |
| `Update{Entity}Endpoint` | PUT | `/{entities}/{id}` |

### Archivos de Soporte

| Archivo | Descripcion |
|---------|-------------|
| `{Entity}MappingProfile.cs` | {n} mapeos configurados |
| `{Action}{Entity}Model.cs` | Request/Response models |

### Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `Program.cs` | Registrados {n} Use Cases en DI |

**Status:** SUCCESS
```

---

## Patrones por Tipo de Endpoint

### CREATE (POST)
- Response: 201 Created con `SendCreatedAtAsync`
- Retorna el recurso creado

### GET Single (GET /{id})
- Response: 200 OK o 404 NotFound
- Retorna el recurso o error

### GET Many (GET /)
- Response: 200 OK con lista (puede ser vacia)
- Soporta filtros y paginacion

### UPDATE (PUT /{id})
- Response: 200 OK o 404 NotFound
- Retorna el recurso actualizado

### DELETE (DELETE /{id})
- Response: 204 NoContent o 404 NotFound
- Sin body en respuesta exitosa

---

## Restricciones

### NO debes:
- Implementar capas de Domain o Infrastructure (eso es otro comando)
- Poner logica de negocio en Use Cases o Endpoints
- Exponer entidades de dominio directamente (usar DTOs)
- Crear archivos fuera de `{proyecto}.application/` y `{proyecto}.webapi/`

### DEBES:
- Seguir estrictamente las guias de desarrollo
- Usar los patrones de implementaciones existentes como referencia
- Implementar TODOS los endpoints listados en el plan
- Registrar Use Cases en DI
- Use Cases deben ser thin wrappers

---

## Referencias

- [Use Cases]({GUIDES_REPO}/architectures/clean-architecture/guides/application/use-cases.md)
- [Command Handler Patterns]({GUIDES_REPO}/architectures/clean-architecture/guides/application/command-handler-patterns.md)
- [FastEndpoints Basics]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/fastendpoints-basics.md)
- [Request/Response Models]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/request-response-models.md)
- [DTOs]({GUIDES_REPO}/architectures/clean-architecture/guides/webapi/dtos.md)
- [AutoMapper Profiles]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/automapper-profiles.md)
