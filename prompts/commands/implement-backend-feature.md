# Implement Backend Feature

> **Version Comando:** 3.1.0
> **Ultima actualizacion:** 2025-12-30

---

Implementa un feature backend completo siguiendo el plan de implementacion y las guias de desarrollo de APSYS. Ejecuta las 3 fases secuencialmente: Domain → Infrastructure → Application/WebAPI.

## Entrada

**Plan a implementar:** $ARGUMENTS

- Si se proporciona un nombre de plan, busca en `.claude/planning/{$ARGUMENTS}-implementation-plan.md`
- Si se proporciona `--layer={domain|infrastructure|webapi}`, ejecuta solo esa fase
- Si `$ARGUMENTS` esta vacio, pregunta al usuario que plan desea implementar

**Ejemplos:**
```bash
/implement-backend-feature gestion-proveedores
/implement-backend-feature gestion-proveedores --layer=domain
/implement-backend-feature --layer=infrastructure
```

## Configuracion

**Ubicacion de planes:** `.claude/planning/`

**Repositorio de Guias:**

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Guias a Consultar

Antes de implementar cada fase, lee las guias relevantes desde `{GUIDES_REPO}`:

### Domain Layer

| Guia | Ruta |
|------|------|
| Entities | `fundamentals/patterns/domain-modeling/entities.md` |
| Validators | `fundamentals/patterns/domain-modeling/validators.md` |
| Repository Interfaces | `fundamentals/patterns/domain-modeling/repository-interfaces.md` |
| DAOs | `fundamentals/patterns/domain-modeling/daos.md` |
| Domain Exceptions | `fundamentals/patterns/domain-modeling/domain-exceptions.md` |

### Infrastructure Layer

| Guia | Ruta |
|------|------|
| Repositories | `stacks/orm/nhibernate/guides/repositories.md` |
| Mappers | `stacks/orm/nhibernate/guides/mappers.md` |
| Best Practices | `stacks/orm/nhibernate/guides/best-practices.md` |

### Application Layer

| Guia | Ruta |
|------|------|
| Use Cases | `architectures/clean-architecture/guides/application/use-cases.md` |
| Command Handler Patterns | `architectures/clean-architecture/guides/application/command-handler-patterns.md` |

### WebAPI Layer

| Guia | Ruta |
|------|------|
| FastEndpoints Basics | `stacks/webapi/fastendpoints/guides/fastendpoints-basics.md` |
| Request/Response Models | `stacks/webapi/fastendpoints/guides/request-response-models.md` |
| DTOs | `architectures/clean-architecture/guides/webapi/dtos.md` |
| AutoMapper Profiles | `stacks/webapi/fastendpoints/guides/automapper-profiles.md` |

---

## Proceso de Implementacion

### Paso 0: Cargar y Validar Plan

1. Lee el archivo de plan desde `.claude/planning/`
2. Extrae informacion general del feature
3. Muestra resumen al usuario:

```markdown
## Feature: {nombre}
**Entidad Principal:** {Entity}
**Fases:** Domain → Infrastructure → Application/WebAPI

¿Continuar con la implementacion?
```

### Paso 1: Explorar Proyecto Actual

Busca implementaciones existentes como referencia para todas las capas:

```bash
# Domain
Glob: **/entities/*.cs
Glob: **/entities/validators/*Validator.cs
Glob: **/interfaces/repositories/I*Repository.cs
Glob: **/interfaces/repositories/IUnitOfWork.cs

# Infrastructure
Glob: **/nhibernate/mappers/*Mapper.cs
Glob: **/nhibernate/NH*Repository.cs
Glob: **/nhibernate/NHUnitOfWork.cs
Glob: **/nhibernate/NHSessionFactory.cs

# Application + WebAPI
Glob: **/dtos/*Dto.cs
Glob: **/usecases/**/*UseCase.cs
Glob: **/features/**/*Endpoint.cs
Glob: **/mappingprofiles/*MappingProfile.cs
```

---

## Fase 1: Domain Layer

Extrae de la seccion "Fase 1: Domain Layer" del plan.

### 1.1 Entity

**Archivo:** `{proyecto}.domain/entities/{Entity}.cs`

- Heredar de `AbstractDomainObject`
- Propiedades `virtual` para NHibernate
- Constructor vacio + constructor parametrizado
- Override de `GetValidator()`

### 1.2 Validator

**Archivo:** `{proyecto}.domain/entities/validators/{Entity}Validator.cs`

- Heredar de `AbstractValidator<{Entity}>`
- Reglas con FluentValidation
- Mensajes de error descriptivos
- Error codes para cada regla

### 1.3 Repository Interface

**Archivo:** `{proyecto}.domain/interfaces/repositories/I{Entity}Repository.cs`

- Heredar de `IRepository<{Entity}, Guid>`
- Metodos custom segun el plan
- XML documentation completa

### 1.4 Actualizar IUnitOfWork

**Archivo:** `{proyecto}.domain/interfaces/repositories/IUnitOfWork.cs`

- Agregar: `I{Entity}Repository {Entities} { get; }`

### Verificacion Fase 1

- [ ] Entity hereda de AbstractDomainObject
- [ ] Propiedades son virtual
- [ ] Validator implementa todas las reglas
- [ ] Repository interface tiene todos los metodos
- [ ] IUnitOfWork actualizado

---

## Fase 2: Infrastructure Layer

Extrae de la seccion "Fase 2: Infrastructure Layer" del plan.

### 2.0 Verificar Migraciones de Base de Datos

**ANTES de implementar**, verifica si se necesita una migracion:

| Situacion | ¿Migracion? | Accion |
|-----------|-------------|--------|
| Entidad nueva sin tabla | ✅ Si | Crear tabla con columnas |
| Agregar columna a tabla existente | ✅ Si | ALTER TABLE ADD COLUMN |
| Eliminar columna | ✅ Si | ALTER TABLE DROP COLUMN |
| Cambiar tipo de columna | ✅ Si | ALTER TABLE ALTER COLUMN |
| Agregar FK o indice | ✅ Si | CREATE INDEX / ADD CONSTRAINT |
| Solo metodo nuevo (sin cambio de esquema) | ❌ No | No se necesita migracion |
| Tabla ya existe con columnas correctas | ❌ No | Usar esquema actual |

**Si necesita migracion:**

1. Buscar migraciones existentes:
   ```bash
   Glob: **/data-migrations/*.cs
   ```

2. Crear archivo de migracion:
   ```
   {proyecto}.infrastructure/data-migrations/{Timestamp}_{DescripcionCambio}.cs
   ```

3. Usar FluentMigrator:
   ```csharp
   [Migration({timestamp})]
   public class {DescripcionCambio} : Migration
   {
       public override void Up()
       {
           Create.Table("table_name")
               .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
               .WithColumn("name").AsString(100).NotNullable()
               .WithColumn("creation_date").AsDateTime().NotNullable();
       }

       public override void Down()
       {
           Delete.Table("table_name");
       }
   }
   ```

4. Ejecutar migracion o notificar al usuario si no es posible automaticamente.

### 2.1 Mapper

**Archivo:** `{proyecto}.infrastructure/nhibernate/mappers/{Entity}Mapper.cs`

- Heredar de `ClassMapping<{Entity}>`
- Tabla con nombre en snake_case
- Id con Generators.Assigned
- Propiedades con columnas en snake_case
- Relaciones ManyToOne con LazyRelation.Proxy

### 2.2 Repository

**Archivo:** `{proyecto}.infrastructure/nhibernate/NH{Entity}Repository.cs`

- Heredar de `NHRepository<{Entity}, Guid>`
- Implementar `I{Entity}Repository`
- Constructor con `ISession` y `IServiceProvider`
- FlushAsync() despues de operaciones de escritura
- Validar entidad antes de persistir

### 2.3 Registrar Mapper

**Archivo:** `{proyecto}.infrastructure/nhibernate/NHSessionFactory.cs`

- Agregar: `mapper.AddMapping<{Entity}Mapper>();`

### 2.4 Actualizar NHUnitOfWork

**Archivo:** `{proyecto}.infrastructure/nhibernate/NHUnitOfWork.cs`

```csharp
private I{Entity}Repository? _{entities};
public I{Entity}Repository {Entities} => _{entities} ??= new NH{Entity}Repository(_session, _serviceProvider);
```

### Verificacion Fase 2

- [ ] Mapper tiene tabla y columnas en snake_case
- [ ] Repository hereda de NHRepository
- [ ] Repository implementa todos los metodos
- [ ] NHSessionFactory tiene mapper registrado
- [ ] NHUnitOfWork tiene lazy property

---

## Fase 3: Application + WebAPI Layer

Extrae de las secciones "Fase 3" y "Fase 4" del plan.

### 3.1 DTO

**Archivo:** `{proyecto}.webapi/dtos/{Entity}Dto.cs`

- Solo propiedades (sin logica)
- Strings inicializados con `string.Empty`
- Colecciones con `Enumerable.Empty<T>()`

### 3.2 Use Cases

**Archivo:** `{proyecto}.application/usecases/{entity}/{Action}{Entity}UseCase.cs`

- Clase interna `Command` o `Query`
- Metodo `ExecuteAsync` retorna `Result<T>`
- **Thin wrappers** - solo orquestacion, NO logica de negocio
- Inyectar repositorio via constructor

### 3.3 Request/Response Models

**Archivo:** `{proyecto}.webapi/features/{entity}/models/{Action}{Entity}Model.cs`

- Clase contenedora con `Request` y `Response` anidados
- Request: propiedades de entrada
- Response: DTO o lista de DTOs

### 3.4 Mapping Profile

**Archivo:** `{proyecto}.webapi/mappingprofiles/{Entity}MappingProfile.cs`

- Entity → DTO
- Request → Command/Query
- Entity/Result → Response

### 3.5 Endpoints

**Archivo:** `{proyecto}.webapi/features/{entity}/endpoint/{Action}{Entity}Endpoint.cs`

- Heredar de `Endpoint<TRequest, TResponse>`
- Configurar ruta y permisos en `Configure()`
- Implementar `HandleAsync()`
- Manejar errores con codigos HTTP correctos

**Patrones HTTP:**
| Accion | Metodo | Response |
|--------|--------|----------|
| Create | POST | 201 Created |
| Get | GET | 200 OK / 404 |
| GetMany | GET | 200 OK (lista) |
| Update | PUT | 200 OK / 404 |
| Delete | DELETE | 204 NoContent / 404 |

### 3.6 Validators (FastEndpoints)

- Heredar de `Validator<{Action}{Entity}Model.Request>`
- Reglas con FluentValidation

### 3.7 Registrar Use Cases

**Archivo:** `Program.cs`

- Agregar: `services.AddScoped<{Action}{Entity}UseCase>();`

### Verificacion Fase 3

- [ ] DTOs solo tienen propiedades
- [ ] Use Cases son thin wrappers
- [ ] Endpoints manejan errores correctamente
- [ ] Mapping Profile completo
- [ ] Use Cases registrados en DI

---

## Formato de Salida

Al finalizar todas las fases:

```markdown
# Feature Implementation Complete

**Feature:** {nombre}
**Entidad:** {Entity}

## Domain Layer

| Archivo | Descripcion |
|---------|-------------|
| `entities/{Entity}.cs` | Entidad con {n} propiedades |
| `entities/validators/{Entity}Validator.cs` | {n} reglas de validacion |
| `interfaces/repositories/I{Entity}Repository.cs` | {n} metodos |

## Infrastructure Layer

| Archivo | Descripcion |
|---------|-------------|
| `nhibernate/mappers/{Entity}Mapper.cs` | Mapeo a tabla `{table_name}` |
| `nhibernate/NH{Entity}Repository.cs` | {n} metodos implementados |

## Application + WebAPI Layer

| Componente | Cantidad |
|------------|----------|
| DTOs | {n} |
| Use Cases | {n} |
| Endpoints | {n} |
| Models | {n} |

## Archivos Modificados

- `IUnitOfWork.cs` - Agregada propiedad `{Entities}`
- `NHUnitOfWork.cs` - Agregada lazy property
- `NHSessionFactory.cs` - Registrado mapper
- `Program.cs` - Registrados Use Cases

**Status:** SUCCESS
```

---

## Manejo de Errores

Si alguna fase falla:

```markdown
# Feature Implementation PAUSED

**Fase:** {1: Domain | 2: Infrastructure | 3: WebAPI}
**Error:** {descripcion}

## Progreso

| Fase | Status |
|------|--------|
| Domain | {completado/fallido/pendiente} |
| Infrastructure | {completado/fallido/pendiente} |
| WebAPI | {completado/fallido/pendiente} |

## Opciones
1. "reintentar" - Volver a intentar la fase fallida
2. "continuar" - Saltar y seguir con la siguiente fase
3. "cancelar" - Abortar implementacion
```

---

## Restricciones

### NO debes:
- Inventar propiedades, metodos o validaciones no especificadas en el plan
- Poner logica de negocio en Use Cases o Endpoints
- Exponer entidades de dominio directamente (usar DTOs)
- Continuar automaticamente si hay errores de compilacion

### DEBES:
- Seguir estrictamente las guias de desarrollo
- Usar implementaciones existentes como referencia de patrones
- Implementar TODOS los componentes listados en el plan
- Verificar compilacion al final de cada fase
- Mostrar progreso claro al usuario

---

## Anti-Patterns a Evitar

### Domain Layer

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Logica de persistencia en entidad | Viola separacion de responsabilidades | Mover a repositorio |
| Atributos de ORM en entidad | Acopla dominio a infraestructura | Usar mappers separados |
| Propiedades no-virtual | NHibernate no puede crear proxies | Todas las propiedades `virtual` |
| Dependencias externas en entidad | Dificulta testing | Solo logica de negocio pura |
| Colecciones sin inicializar | NullReferenceException | Inicializar con `new List<>()` |

### Infrastructure Layer

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Olvidar `FlushAsync()` | Cambios no se persisten | Siempre despues de Save/Update/Delete |
| Exponer `ISession` publicamente | Viola encapsulamiento | Solo usar internamente |
| Logica de negocio en repositorio | Responsabilidad incorrecta | Mover a entidad o servicio de dominio |
| Queries case-sensitive | Busquedas inconsistentes | Usar `.ToLower()` en comparaciones |
| Retornar `IQueryable` publico | Permite queries arbitrarias | Retornar `IEnumerable` o `List` |

### Application Layer (Use Cases)

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Logica de negocio en Use Case | Use Cases deben ser thin wrappers | Mover logica a entidades |
| Validaciones complejas en Use Case | Duplica logica de entidad | Usar validators de entidad |
| Acceso directo a BD | Viola patron Repository | Usar repositorios inyectados |
| Use Case con mas de 30 lineas | Hace demasiado | Simplificar o dividir |

### WebAPI Layer

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Exponer entidades de dominio | Acopla API a dominio | Siempre usar DTOs |
| Logica de negocio en Endpoint | Responsabilidad incorrecta | Mover a Use Case o entidad |
| Strings sin inicializar en DTOs | Null reference en serializacion | Inicializar con `string.Empty` |
| Colecciones null en DTOs | Null reference en serializacion | Inicializar con `Enumerable.Empty<T>()` |
| Ignorar codigos HTTP correctos | API no RESTful | Usar 201, 204, 404, 409 segun caso |

### Manejo de Errores

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| Swallow exceptions | Oculta errores | Propagar o manejar explicitamente |
| Excepciones genericas | Dificil debugging | Usar excepciones de dominio especificas |
| No validar antes de persistir | Datos invalidos en BD | Llamar `IsValid()` antes de guardar |

---

## Referencias

### Domain
- [Entities]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/entities.md)
- [Validators]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/validators.md)
- [Repository Interfaces]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/repository-interfaces.md)
- [DAOs]({GUIDES_REPO}/fundamentals/patterns/domain-modeling/daos.md)

### Infrastructure
- [Repositories]({GUIDES_REPO}/stacks/orm/nhibernate/guides/repositories.md)
- [Mappers]({GUIDES_REPO}/stacks/orm/nhibernate/guides/mappers.md)
- [Best Practices]({GUIDES_REPO}/stacks/orm/nhibernate/guides/best-practices.md)

### Application + WebAPI
- [Use Cases]({GUIDES_REPO}/architectures/clean-architecture/guides/application/use-cases.md)
- [Command Handler Patterns]({GUIDES_REPO}/architectures/clean-architecture/guides/application/command-handler-patterns.md)
- [FastEndpoints Basics]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/fastendpoints-basics.md)
- [Request/Response Models]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/request-response-models.md)
- [DTOs]({GUIDES_REPO}/architectures/clean-architecture/guides/webapi/dtos.md)
- [AutoMapper Profiles]({GUIDES_REPO}/stacks/webapi/fastendpoints/guides/automapper-profiles.md)
