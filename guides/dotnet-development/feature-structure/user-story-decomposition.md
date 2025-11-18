# User Story Decomposition

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-18

---

## Tabla de Contenidos

1. [Introducción](#1-introducción)
2. [Prerequisitos](#2-prerequisitos)
3. [Metodología General](#3-metodología-general)
4. [Quick Reference Checklist](#4-quick-reference-checklist)
5. [Matriz de Decisión](#5-matriz-de-decisión)
6. [Proceso Paso a Paso](#6-proceso-paso-a-paso)
7. [Templates de Subtasks](#7-templates-de-subtasks)
8. [Estimación y Planificación](#8-estimación-y-planificación)
9. [Definition of Done](#9-definition-of-done)
10. [Ejemplos Completos](#10-ejemplos-completos)
11. [Antipatrones y Errores Comunes](#11-antipatrones-y-errores-comunes)
12. [Herramientas y Recursos](#12-herramientas-y-recursos)

---

## 1. Introducción

### ¿Qué es esta guía?

Esta guía te enseña cómo **descomponer user stories del backend** en subtareas técnicas ejecutables por el equipo de desarrollo. A diferencia de las guías por capa (Domain, Infrastructure, etc.) que explican **cómo construir** cada componente, esta guía explica **cómo planificar** qué construir.

### ¿Cuándo usar esta guía?

Usa esta guía cuando:
- ✅ Tienes una user story asignada en Jira
- ✅ Necesitas crear subtasks técnicas para el equipo backend
- ✅ Quieres estimar esfuerzo y planificar sprints
- ✅ Necesitas un diagrama de secuencia para el equipo
- ✅ Quieres validar que no olvidaste ningún componente

### ¿Qué NO es esta guía?

Esta guía NO reemplaza las guías técnicas por capa:
- ❌ NO explica cómo escribir código de cada componente
- ❌ NO detalla sintaxis de NHibernate, FluentValidation, etc.
- ❌ NO cubre patrones de diseño en profundidad

Para esos temas, consulta las guías específicas:
- [Domain Layer Guide](../domain-layer/README.md)
- [Infrastructure Layer Guide](../infrastructure-layer/README.md)
- [Application Layer Guide](../application-layer/README.md)
- [WebApi Layer Guide](../webapi-layer/README.md)

### Principios Clave

1. **Simplicidad primero**: Empieza con lo mínimo necesario, agrega complejidad después
2. **Una subtask = Una entrega**: Cada subtask debe ser integrable y testeable
3. **Tests implícitos**: Cada subtask incluye tests según su tipo (no son subtasks separadas)
4. **Orden importa**: Las subtasks tienen dependencias claras
5. **Estimar realista**: Usa históricos del equipo, no idealismo

---

## 2. Prerequisitos

### Conocimientos Requeridos

Antes de descomponer tareas, debes estar familiarizado con:

- ✅ **Clean Architecture**: Entender las 4 capas (Domain, Infrastructure, Application, WebApi)
- ✅ **Repository Pattern**: Diferencia entre IRepository e IReadOnlyRepository
- ✅ **Entity vs DAO**: Cuándo usar cada uno
- ✅ **Unit of Work**: Cómo se organizan los repositorios
- ✅ **NHibernate básico**: Mappings y lazy loading
- ✅ **FastEndpoints**: Estructura básica de endpoints

### Documentos a Leer Primero

| Guía | Por qué es importante |
|------|----------------------|
| [Domain Layer](../domain-layer/README.md) | Entender Entities, DAOs, Validators, Repository Interfaces |
| [DAOs Guide](../domain-layer/daos.md) | Diferencia entre Entity y DAO, cuándo usar cada uno |
| [Repository Interfaces](../domain-layer/repository-interfaces.md) | IRepository vs IReadOnlyRepository, IUnitOfWork |
| [Infrastructure Layer](../infrastructure-layer/README.md) | Implementación de repositorios, NHibernate mappings |

### Herramientas Necesarias

- Jira (para crear subtasks)
- Acceso al código del proyecto
- Editor de diagramas (PlantUML, Mermaid, o Draw.io)
- Conocimiento de la user story a descomponer

---

## 3. Metodología General

### Flujo de Trabajo: User Story → Código

```
┌─────────────────┐
│  User Story     │
│  (Jira)         │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  1. Análisis    │  ← ¿Qué necesita el usuario?
│     Funcional   │  ← ¿Lectura o escritura?
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  2. Diagrama    │  ← Identificar actores y flujo
│     Secuencia   │  ← Cliente → Endpoint → UseCase → Repository → BD
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  3. Identificar │  ← Extraer clases del diagrama
│     Componentes │  ← Asignar a capas (Domain, Infrastructure, etc.)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  4. Crear       │  ← Una subtask por capa o componente
│     Subtasks    │  ← Con descripción, criterios y estimación
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  5. Desarrollo  │  ← Equipo ejecuta subtasks en orden
│     + Tests     │  ← Tests implícitos en cada subtask
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  6. Integración │  ← Smoke test, E2E, documentación
│     Final       │
└─────────────────┘
```

### Preguntas Clave en el Análisis

Antes de crear el diagrama, responde:

| Pregunta | Por qué importa | Impacto en diseño |
|----------|-----------------|-------------------|
| ¿Es lectura o escritura? | Define Entity vs DAO | Escritura → Entity, Lectura → DAO |
| ¿Necesita validaciones? | Define si necesitas Validator | Sí → Entity con AbstractValidator |
| ¿Necesita paginación? | Define estructura del repositorio | Sí → GetManyAndCountAsync |
| ¿Necesita búsqueda full-text? | Define propiedades del DAO | Sí → SearchAll property |
| ¿Necesita filtros? | Define query parameters | Sí → Filter DTO en query params |
| ¿Necesita sorting? | Define opciones de ordenamiento | Sí → SortingCriteria enum |
| ¿Necesita agregaciones? | Define DAOs adicionales | Sí → DAO específico (ej: RoleCountDao) |
| ¿Tiene reglas de negocio complejas? | Define lógica en Use Case | Sí → Use Case con validaciones |
| ¿Requiere permisos especiales? | Define autorización | Sí → Policy en endpoint |

---

## 4. Quick Reference Checklist

Usa este checklist rápido al analizar cualquier user story. Marca los items que aplican y tendrás la lista de componentes a crear.

### Checklist de Análisis Rápido

**Operación:**
- [ ] ¿Es solo lectura? → DAO + IReadOnlyRepository
- [ ] ¿Es escritura (Create/Update/Delete)? → Entity + IRepository + Validator
- [ ] ¿Es mixta (Dashboard con acciones)? → DAO para lista + Entity para acciones

**Componentes Domain:**
- [ ] ¿Hay nueva entidad? → Entity en `Domain/entities/`
- [ ] ¿Hay validaciones? → Validator en `Domain/entities/validators/`
- [ ] ¿Hay consulta optimizada? → DAO en `Domain/daos/`
- [ ] ¿Hay agregaciones (COUNT, SUM)? → DAO específico para aggregates
- [ ] ¿Hay repository interface? → IXRepository en `Domain/interfaces/repositories/`
- [ ] ¿Actualizar IUnitOfWork? → Agregar property para nuevo repositorio

**Componentes Infrastructure:**
- [ ] ¿Hay nueva tabla? → Migration CreateTable
- [ ] ¿Hay nueva vista SQL? → Migration CreateView (para DAOs)
- [ ] ¿Hay índices nuevos? → Migration CreateIndex
- [ ] ¿Hay mapper NHibernate? → XMap en `Infrastructure/mappings/`
- [ ] ¿Hay repository implementation? → XRepository en `Infrastructure/repositories/`
- [ ] ¿Registrar en DI? → Actualizar `Infrastructure/DependencyInjection.cs`

**Componentes Application:**
- [ ] ¿Hay operación Create? → CreateXCommand + Handler
- [ ] ¿Hay operación Update? → UpdateXCommand + Handler
- [ ] ¿Hay operación Delete? → DeleteXCommand + Handler
- [ ] ¿Hay operación Get (single)? → GetXQuery + Handler
- [ ] ¿Hay operación Get (list)? → GetManyXQuery + Handler
- [ ] ¿Validar permisos en handler? → Verificar rol/policy

**Componentes WebApi:**
- [ ] ¿Hay endpoint Create? → POST endpoint + Request model
- [ ] ¿Hay endpoint Update? → PUT endpoint + Request model
- [ ] ¿Hay endpoint Delete? → DELETE endpoint
- [ ] ¿Hay endpoint Get? → GET endpoint + Response model
- [ ] ¿Hay endpoint GetMany? → GET endpoint + Query params + Pagination
- [ ] ¿Hay DTOs? → XDto en `WebApi/DTOs/`
- [ ] ¿Configurar autorización? → Policy en endpoint
- [ ] ¿Documentar en Swagger? → XML comments

**Tests:**
- [ ] ¿Tests de dominio? → Validator tests
- [ ] ¿Tests de repositorio? → NHibernate integration tests
- [ ] ¿Tests de use cases? → Unit tests con mocks
- [ ] ¿Tests de endpoints? → Endpoint integration tests
- [ ] ¿Test E2E? → Postman/Swagger smoke test

**Resultado del Checklist:**
- Marca ✅ = Componente necesario → Incluir en subtask correspondiente
- Marca ❌ = Componente no necesario → Omitir

---

## 5. Matriz de Decisión

### ¿Entity o DAO?

Use esta tabla para decidir qué usar:

| Si necesitas... | Entonces usa... | Ejemplo |
|----------------|-----------------|---------|
| **Crear** un registro | Entity + IRepository | Crear factura |
| **Actualizar** un registro | Entity + IRepository | Editar cliente |
| **Eliminar** un registro | Entity + IRepository | Cancelar pedido |
| **Listar** registros (UI) | DAO + IReadOnlyRepository | Dashboard de usuarios |
| **Buscar** con filtros | DAO + IReadOnlyRepository | Buscar facturas vencidas |
| **Agregar** datos (COUNT, SUM) | DAO + método custom | Contadores por rol |
| **Validar** reglas de negocio | Entity + Validator | Validar RFC de cliente |
| **Navigation properties** | Entity (NO DAO) | Factura con ConceptosFactura |

### Regla de Oro

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  ¿Modifica datos en BD?                        │
│                                                 │
│         SÍ              │            NO         │
│         ↓               │            ↓          │
│      ENTITY             │          DAO          │
│   + IRepository         │  + IReadOnlyRepository│
│   + Validator           │  + SearchAll          │
│   + Navigation Props    │  (solo propiedades)   │
│                         │                       │
└─────────────────────────────────────────────────┘
```

---

## 6. Proceso Paso a Paso

### Paso 1: Leer y Analizar la User Story

#### Ejemplo Real: KC-87

**User Story:**
> Como administrador del módulo Facturación & Cobranza, quiero ver un dashboard con todos los usuarios que tienen acceso al módulo, para poder gestionar quién tiene acceso y qué roles tienen asignados.

**Criterios de Aceptación:**
- Ver lista de usuarios con nombre, email y roles
- Filtrar por búsqueda de texto (nombre o email)
- Filtrar por rol específico
- Ver contadores: cuántos usuarios tienen cada rol
- Paginación (20 usuarios por página)

#### Análisis Funcional

**Respuestas a preguntas clave:**

| Pregunta | Respuesta | Decisión técnica |
|----------|-----------|------------------|
| ¿Es lectura o escritura? | Solo lectura (ver usuarios) | ✅ Usar DAO |
| ¿Necesita validaciones? | No (solo consulta) | ❌ No necesita Validator |
| ¿Necesita paginación? | Sí (20 por página) | ✅ GetManyAndCountAsync |
| ¿Necesita búsqueda? | Sí (nombre o email) | ✅ SearchAll property |
| ¿Necesita filtros? | Sí (por rol) | ✅ Filter query params |
| ¿Necesita sorting? | No mencionado | ⚠️ Agregar por defecto (ByName) |
| ¿Necesita agregaciones? | Sí (contadores por rol) | ✅ RoleCountDao adicional |
| ¿Reglas de negocio? | Solo autorización | ✅ Validar Admin en Use Case |
| ¿Permisos especiales? | Sí (solo Admin del módulo) | ✅ Policy en endpoint |

**Conclusiones del análisis:**
- ✅ Necesitamos 2 DAOs: `UserDao` (lista) y `RoleCountDao` (contadores)
- ✅ Necesitamos 2 endpoints: GET /users y GET /users/role-counts
- ✅ Necesitamos vista SQL para calcular IsActive
- ✅ NO necesitamos Entity (solo lectura)

---

### Paso 2: Crear Diagrama de Secuencia

El diagrama de secuencia identifica **actores** y **flujo** de datos.

#### Template de Diagrama

```
┌─────────┐    ┌──────────┐    ┌─────────────┐    ┌──────────────┐    ┌─────────┐
│ Cliente │    │ Endpoint │    │  Use Case   │    │     DAO      │    │   BD    │
│ (React) │    │ WebApi   │    │ Application │    │Infrastructure│    │  (SQL)  │
└────┬────┘    └────┬─────┘    └──────┬──────┘    └──────┬───────┘    └────┬────┘
     │              │                  │                  │                  │
     │ HTTP GET     │                  │                  │                  │
     ├─────────────>│                  │                  │                  │
     │              │                  │                  │                  │
     │              │ Validar Auth     │                  │                  │
     │              │                  │                  │                  │
     │              │ Handle(query)    │                  │                  │
     │              ├─────────────────>│                  │                  │
     │              │                  │                  │                  │
     │              │                  │ Validar Permisos │                  │
     │              │                  │                  │                  │
     │              │                  │ GetManyAndCount()│                  │
     │              │                  ├─────────────────>│                  │
     │              │                  │                  │                  │
     │              │                  │                  │ SELECT FROM view │
     │              │                  │                  ├─────────────────>│
     │              │                  │                  │<─────────────────┤
     │              │                  │<─────────────────┤                  │
     │              │<─────────────────┤                  │                  │
     │<─────────────┤                  │                  │                  │
     │ 200 OK       │                  │                  │                  │
```

#### Diagrama Real: KC-87 (Dashboard de Usuarios)

```
┌─────────┐          ┌──────────┐          ┌─────────────┐          ┌──────────────┐          ┌─────────┐
│ Cliente │          │ Endpoint │          │  Use Case   │          │     DAO      │          │   BD    │
│ (React) │          │ WebApi   │          │ Application │          │Infrastructure│          │  (SQL)  │
└────┬────┘          └────┬─────┘          └──────┬──────┘          └──────┬───────┘          └────┬────┘
     │                    │                       │                        │                       │
     │ GET /api/modules/  │                       │                        │                       │
     │ {id}/users         │                       │                        │                       │
     │ ?search=juan       │                       │                        │                       │
     ├───────────────────>│                       │                        │                       │
     │                    │                       │                        │                       │
     │              ┌─────┴─────┐                 │                        │                       │
     │              │ Validar   │                 │                        │                       │
     │              │ Auth +    │                 │                        │                       │
     │              │ Policy    │                 │                        │                       │
     │              │ (Admin)   │                 │                        │                       │
     │              └─────┬─────┘                 │                        │                       │
     │                    │                       │                        │                       │
     │                    │ Handle(query)         │                        │                       │
     │                    ├──────────────────────>│                        │                       │
     │                    │                       │                        │                       │
     │                    │                  ┌────┴────┐                   │                       │
     │                    │                  │ Validar │                   │                       │
     │                    │                  │ Permisos│                   │                       │
     │                    │                  └────┬────┘                   │                       │
     │                    │                       │                        │                       │
     │                    │                       │ GetManyAndCountAsync(  │                       │
     │                    │                       │   "juan",              │                       │
     │                    │                       │   "ByName")            │                       │
     │                    │                       ├───────────────────────>│                       │
     │                    │                       │                        │                       │
     │                    │                       │                        │ SELECT * FROM         │
     │                    │                       │                        │ vw_module_users       │
     │                    │                       │                        │ WHERE SearchAll       │
     │                    │                       │                        │ LIKE '%juan%'         │
     │                    │                       │                        ├──────────────────────>│
     │                    │                       │                        │                       │
     │                    │                       │                        │<──────────────────────┤
     │                    │                       │                        │ Rows (users)          │
     │                    │                       │                        │                       │
     │                    │                       │<───────────────────────┤                       │
     │                    │                       │ GetManyAndCountResult  │                       │
     │                    │                       │                        │                       │
     │                    │<──────────────────────┤                        │                       │
     │                    │ Result<DTOs>          │                        │                       │
     │                    │                       │                        │                       │
     │<───────────────────┤                       │                        │                       │
     │ 200 OK + JSON      │                       │                        │                       │
     │ {items: [...],     │                       │                        │                       │
     │  totalCount: X}    │                       │                        │                       │
```

---

### Paso 3: Identificar Componentes

Del diagrama, extraemos **qué clases** necesitamos crear:

#### Tabla de Extracción

| Actor en Diagrama | Capa | Componentes a Crear |
|-------------------|------|---------------------|
| **DAO (Repositorio)** | Infrastructure | `UserDao`, `IUserDaoRepository`, `UserDaoRepository`, `UserDaoMap` |
| **DAO (Repositorio)** | Infrastructure | `RoleCountDao`, `IRoleCountDaoRepository`, `RoleCountDaoRepository` |
| **Vista SQL** | Infrastructure | `vw_module_users` (cálculo de IsActive, SearchAll) |
| **Use Case** | Application | `GetUsersQuery`, `GetUsersHandler`, `GetRoleCountsQuery`, `GetRoleCountsHandler` |
| **Endpoint** | WebApi | `GetUsersEndpoint`, `GetRoleCountsEndpoint` |
| **DTOs** | WebApi | `UserDto`, `RoleCountDto` |

#### Agrupación por Capa

**Domain Layer:**
- `UserDao` (DAO)
- `RoleCountDao` (DAO)
- `IUserDaoRepository` (interface)
- `IRoleCountDaoRepository` (interface)
- Actualizar `IUnitOfWork`

**Infrastructure Layer:**
- Vista SQL `vw_module_users`
- `UserDaoRepository` (implementación)
- `RoleCountDaoRepository` (implementación)
- `UserDaoMap` (NHibernate mapping)
- Registrar en DI

**Application Layer:**
- `GetUsersQuery` + `GetUsersHandler`
- `GetRoleCountsQuery` + `GetRoleCountsHandler`

**WebApi Layer:**
- `GetUsersEndpoint`
- `GetRoleCountsEndpoint`
- `UserDto`, `RoleCountDto`

---

### Paso 4: Crear Subtasks en Jira

Una vez identificados los componentes, creamos **subtasks agrupadas por capa**.

#### Principio de Agrupación

**Opción A - Por Componente (granular):**
```
❌ Demasiadas subtasks
- [Backend-Domain] Crear UserDao
- [Backend-Domain] Crear RoleCountDao
- [Backend-Domain] Crear IUserDaoRepository
- [Backend-Domain] Crear IRoleCountDaoRepository
- [Backend-Domain] Actualizar IUnitOfWork
Total: 5 subtasks solo en Domain
```

**Opción B - Por Capa (agrupada) ✅ RECOMENDADO:**
```
✅ Subtasks manejables
- [Backend-Domain] Crear DAOs e interfaces para dashboard
- [Backend-Infrastructure] Implementar capa de datos
- [Backend-Application] Implementar use cases
- [Backend-WebApi] Crear endpoints
- [Backend-Integration] Verificación E2E
Total: 5 subtasks para toda la feature
```

**Regla:** Agrupa componentes de la misma capa EN UNA SUBTASK si:
- Son parte de la misma feature
- Tienen dependencias entre sí
- Se pueden completar en 1-6 horas

---

### Paso 5: Orden de Ejecución

Las subtasks tienen un **orden natural** por dependencias:

```
1. Domain Layer
   └─> Define interfaces y DAOs
       │
       ▼
2. Infrastructure Layer
   └─> Implementa repositorios y mappings
       │
       ▼
3. Application Layer  (puede paralelizarse con 2 si coordinan)
   └─> Implementa use cases
       │
       ▼
4. WebApi Layer
   └─> Crea endpoints
       │
       ▼
5. Integration
   └─> Tests E2E y documentación
```

**¿Por qué este orden?**

- **Domain primero**: Las otras capas dependen de las interfaces definidas aquí
- **Infrastructure antes de Application**: Application necesita que Infrastructure esté listo para integración
- **WebApi al final**: Necesita que Use Cases estén implementados
- **Integration al final**: Necesita todo el flujo completo

---

## 7. Templates de Subtasks

### Template General

Usa este template para todas las subtasks:

```markdown
## Subtask: [Backend-{Capa}] {Verbo} {Componente} {Contexto}

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** {X horas}

### Descripción

{Explicación clara de qué se va a construir y por qué. Máximo 2-3 líneas.}

### Componentes a crear

{Lista de clases/interfaces con ubicación y propiedades principales}

* **Componente 1:** {Ubicación}
  * Propiedades/Métodos principales

* **Componente 2:** {Ubicación}
  * Propiedades/Métodos principales

### Criterios de Aceptación

- [ ] Criterio 1
- [ ] Criterio 2
- [ ] Código compila correctamente
- [ ] Tests esperados pasan

### Tests esperados (implícitos)

* Test escenario 1
* Test escenario 2

### Notas adicionales (opcional)

{Cualquier consideración técnica importante}
```

---

### Template por Capa

#### [Backend-Domain] Template

```markdown
## Subtask: [Backend-Domain] Crear DAOs e interfaces para {feature}

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** 1.5 horas

### Descripción

Definir los DAOs (Data Access Objects) y las interfaces de repositorios necesarias para {feature}.
Usamos DAOs para consultas de solo lectura optimizadas.

### Componentes a crear

#### 1. DAO: {EntityName}Dao

**Ubicación:** `Domain/daos/{EntityName}Dao.cs`

Propiedades:
* Property1 (Type) - Descripción
* Property2 (Type) - Descripción
* SearchAll (string) - Campo combinado para búsqueda

**Características:**
- NO hereda de `AbstractDomainObject`
- Todas las propiedades son `virtual`
- Sin navigation properties
- Sin métodos

#### 2. Interface: I{EntityName}DaoRepository

**Ubicación:** `Domain/interfaces/repositories/I{EntityName}DaoRepository.cs`

```csharp
public interface I{EntityName}DaoRepository : IReadOnlyRepository<{EntityName}Dao, Guid>
{
    // Hereda todos los métodos de IReadOnlyRepository
}
```

#### 3. Actualizar IUnitOfWork

Agregar en región `#region read-only Repositories`:
* `I{EntityName}DaoRepository {EntityName}Daos { get; }`

### Criterios de Aceptación

- [ ] DAOs creados en `Domain/daos/`
- [ ] DAOs NO heredan de AbstractDomainObject
- [ ] Todas las propiedades son `virtual`
- [ ] DAOs NO tienen métodos
- [ ] DAOs NO tienen navigation properties
- [ ] `SearchAll` incluido en DAO principal
- [ ] Interfaces de repositorios creadas en `Domain/interfaces/repositories/`
- [ ] Repositorio hereda de `IReadOnlyRepository<T, Guid>`
- [ ] Agregado a `IUnitOfWork`
- [ ] XML comments completos
- [ ] Código compila

### Tests esperados (implícitos)

* Validación de que los DAOs se serializan correctamente
* Validación de que las propiedades virtuales funcionan
```

---

#### [Backend-Infrastructure] Template

```markdown
## Subtask: [Backend-Infrastructure] Implementar capa de datos para {feature}

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** 4 horas

### Descripción

Crear vista SQL, mappings de NHibernate y repositorios para {feature}.

### Componentes a crear

#### 1. Vista SQL: vw_{table_name}

**Ubicación:** `Infrastructure/persistence/migrations/{timestamp}_Create_View_{ViewName}.sql`

Campos calculados:
* Campo1: Cálculo
* Campo2: Cálculo
* SearchAll: CONCAT(campo1, ' ', campo2)

#### 2. NHibernate Mapping: {EntityName}DaoMap

**Ubicación:** `Infrastructure/persistence/mappings/{EntityName}DaoMap.cs`

```csharp
public class {EntityName}DaoMap : ClassMap<{EntityName}Dao>
{
    public {EntityName}DaoMap()
    {
        Table("vw_{table_name}");
        ReadOnly();

        Id(x => x.Id).GeneratedBy.Assigned();
        Map(x => x.Property1);
        Map(x => x.SearchAll);
    }
}
```

#### 3. Repositorio: {EntityName}DaoRepository

**Ubicación:** `Infrastructure/persistence/repositories/{EntityName}DaoRepository.cs`

Hereda de `BaseReadOnlyRepository<{EntityName}Dao, Guid>`

#### 4. Registrar en DI

**Ubicación:** `Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<I{EntityName}DaoRepository, {EntityName}DaoRepository>();
```

### Criterios de Aceptación

- [ ] Vista SQL creada correctamente
- [ ] Vista retorna datos esperados
- [ ] Mapping de NHibernate configurado
- [ ] Repositorio implementado
- [ ] Métodos heredados funcionan (GetAsync, GetManyAndCountAsync)
- [ ] SearchAll funciona en búsquedas
- [ ] DI registrado correctamente
- [ ] Tests de persistencia pasan

### Tests esperados (implícitos)

* Test: Vista SQL retorna datos
* Test: Mapping de NHibernate funciona
* Test: GetAsync retorna DAO correctamente
* Test: GetManyAndCountAsync con filtro funciona
* Test: SearchAll busca en campos combinados
```

---

#### [Backend-Application] Template

```markdown
## Subtask: [Backend-Application] Implementar use cases para {feature}

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** 2 horas

### Descripción

Crear los use cases que orquestan la lógica de negocio para {feature}.

### Componentes a crear

#### 1. Query: Get{EntityName}sQuery

**Ubicación:** `Application/UseCases/{Feature}/Get{EntityName}s/Get{EntityName}sQuery.cs`

Propiedades:
* ModuleId (Guid)
* SearchTerm (string?)
* PageNumber (int)
* PageSize (int)

#### 2. Handler: Get{EntityName}sHandler

**Ubicación:** `Application/UseCases/{Feature}/Get{EntityName}s/Get{EntityName}sHandler.cs`

Lógica:
* Validar que usuario actual es Admin del Módulo
* Llamar `IUserDaoRepository.GetManyAndCountAsync`
* Mapear resultado a DTOs
* Retornar `Result<GetManyAndCountResult<{EntityName}Dto>>`

### Criterios de Aceptación

- [ ] Use cases validan permisos correctamente
- [ ] Use cases manejan errores apropiadamente
- [ ] Retornan `Result<T>` con éxito o falla
- [ ] Mapeo de DTOs es correcto
- [ ] Código compila

### Tests esperados (implícitos)

* Test unitario: usuario sin permisos recibe error 403
* Test unitario: llamada correcta al DAO con parámetros
* Test unitario: mapeo correcto de resultados
* Test unitario: manejo de errores del DAO
```

---

#### [Backend-WebApi] Template

```markdown
## Subtask: [Backend-WebApi] Crear endpoints para {feature}

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** 2 horas

### Descripción

Exponer los endpoints REST para que el frontend pueda consumir {feature}.

### Componentes a crear

#### 1. Endpoint: Get{EntityName}sEndpoint

**Ubicación:** `WebApi/Endpoints/{Feature}/Get{EntityName}sEndpoint.cs`

**Route:** `GET /api/modules/{moduleId}/{resource}`

**Query params:**
* search (string?, opcional)
* page (int, default: 1)
* pageSize (int, default: 20)

**Autorización:** Policy "module:admin"

**Response:** `GetManyAndCountResult<{EntityName}Dto>`

#### 2. DTOs

**Ubicación:** `WebApi/Endpoints/{Feature}/DTOs/`

* `{EntityName}Dto`: Propiedades de respuesta

### Criterios de Aceptación

- [ ] Endpoints registrados y accesibles
- [ ] Query params se mapean correctamente
- [ ] Autorización funciona (401 si no autenticado, 403 si no autorizado)
- [ ] Responses tienen formato correcto
- [ ] Swagger documentado
- [ ] Tests de endpoints pasan

### Tests esperados (implícitos)

* Test: endpoint requiere autenticación (401)
* Test: endpoint requiere rol correcto (403)
* Test: query params se procesan correctamente
* Test: response tiene estructura correcta
```

---

#### [Backend-Integration] Template

```markdown
## Subtask: [Backend-Integration] Verificación E2E y documentación

**Issue Type:** Sub-task
**Parent:** {Story Key}
**Estimación:** 1.5 horas

### Descripción

Verificar que todo el flujo funciona correctamente de extremo a extremo.

### Actividades

* **Ejecutar todos los tests**: unitarios, integración, endpoints
* **Smoke test manual**: Postman/Swagger con escenarios reales
* **Verificar performance**: Queries < 500ms
* **Actualizar documentación**: README o Confluence

### Criterios de Aceptación

- [ ] Todos los tests automatizados pasan
- [ ] Coverage de tests >80%
- [ ] Smoke test manual exitoso
- [ ] Performance aceptable
- [ ] No hay memory leaks
- [ ] No hay N+1 queries
- [ ] Documentación actualizada
- [ ] Code review aprobado

### Checklist de Smoke Test

- [ ] Endpoint sin filtros retorna datos
- [ ] Búsqueda por texto funciona
- [ ] Filtros funcionan correctamente
- [ ] Paginación funciona
- [ ] Autorización bloquea usuarios no autorizados
```

---

## 8. Estimación y Planificación

### 8.1. Reglas de Estimación Base

Usa esta tabla como referencia:

| Capa | Complejidad | Componentes | Estimación |
|------|-------------|-------------|------------|
| **Domain** | Simple (DAOs básicos) | 1-2 DAOs + Interfaces | 1-2h |
| **Domain** | Media (con agregaciones) | 3+ DAOs + Interfaces | 2-3h |
| **Domain** | Alta (Entities con validaciones) | Entities + Validators | 2-4h |
| **Infrastructure** | Simple (DAOs sin vista) | Mappings + Repos | 2-3h |
| **Infrastructure** | Media (DAOs con vista SQL) | Vista + Mappings + Repos | 3-4h |
| **Infrastructure** | Alta (Entities con relaciones) | Mappings complejos + FK | 4-6h |
| **Application** | Simple (Use Case básico) | Query + Handler | 1-2h |
| **Application** | Media (Validaciones complejas) | Query + Handler + Validaciones | 2-3h |
| **WebApi** | Simple (1-2 endpoints) | Endpoints + DTOs | 1.5-2h |
| **WebApi** | Media (3+ endpoints) | Múltiples endpoints | 2-3h |
| **Integration** | Estándar | Tests + Docs | 1.5-2h |

### 8.2. Factores de Complejidad

Ajusta las estimaciones base según estos factores:

#### Factores que AUMENTAN complejidad (+tiempo)

| Factor | Impacto | Ejemplo |
|--------|---------|---------|
| **Validaciones complejas** | +20-30% | RFC válido según reglas SAT, CURP con verificador |
| **Relaciones many-to-many** | +30-40% | Factura con múltiples conceptos, User con múltiples Roles |
| **Integraciones externas** | +50-100% | Validar RFC con API del SAT, enviar email, webhook |
| **Agregaciones complejas** | +20-30% | Reportes con sumas, promedios, agrupaciones múltiples |
| **Cascade operations** | +20-30% | Delete en cascada, update de entidades relacionadas |
| **Validaciones cross-entity** | +30-40% | "No puede haber 2 facturas con mismo folio y año" |
| **Permisos granulares** | +20-30% | "Solo puede editar facturas de su propia empresa" |
| **Campos calculados en vista** | +10-20% | IsActive = (DeletedAt IS NULL), SearchAll = CONCAT(...) |
| **Primera vez con el patrón** | +30-50% | Primer DAO, primer migration, primer cascade |

#### Factores que REDUCEN complejidad (-tiempo)

| Factor | Impacto | Ejemplo |
|--------|---------|---------|
| **Código similar existe** | -20-30% | Ya hay otra Entity similar, copiar estructura |
| **Patrón ya usado** | -10-20% | Ya tenemos varios DAOs, crear uno más es rápido |
| **No hay validaciones** | -15-20% | Solo lectura, no requiere Validator |
| **CRUD estándar** | -10-15% | Create/Update/Delete básico sin lógica especial |
| **Equipo familiarizado** | -10-20% | Feature similar a uno reciente (contexto fresco) |
| **Sin relaciones** | -15-20% | Entity standalone sin FK a otras tablas |

#### Ejemplo de Cálculo Ajustado

**Caso Base:** CRUD Simple = 12h

**Agregar factores:**
- ✅ Validaciones complejas (RFC del SAT): +30% → +3.6h
- ✅ Integración externa (API SAT): +50% → +6h
- ❌ Código similar existe: -20% → -2.4h

**Total ajustado:** 12h + 3.6h + 6h - 2.4h = **19.2h ≈ 19.5h**

**Redondeo sugerido:** Siempre redondea hacia arriba en múltiplos de 0.5h para buffer.

### 8.3. Total por Tipo de Feature

| Tipo de Feature | Total Estimado |
|-----------------|----------------|
| **Read-Only Simple** (lista básica) | 8-10 horas |
| **Read-Only Compleja** (filtros, agregaciones) | 10-13 horas |
| **CRUD Simple** (sin validaciones complejas) | 12-15 horas |
| **CRUD Compleja** (validaciones + relaciones) | 15-20 horas |
| **CRUD con Integraciones** (APIs externas) | 20-30 horas |

---

## 9. Definition of Done

### Checklist por Capa

#### Domain Layer ✅

- [ ] DAOs/Entities creados en carpeta correcta
- [ ] Propiedades son `virtual` (NHibernate requirement)
- [ ] DAOs NO heredan de `AbstractDomainObject`
- [ ] DAOs NO tienen métodos
- [ ] Entities tienen `GetValidator()` implementado
- [ ] Interfaces de repositorios creadas
- [ ] Agregado a `IUnitOfWork`
- [ ] XML comments completos
- [ ] Naming consistente
- [ ] Código compila

#### Infrastructure Layer ✅

- [ ] Vista SQL creada (si aplica)
- [ ] Mappings de NHibernate configurados
- [ ] Repositorios implementan interfaces de Domain
- [ ] Tests de persistencia pasan
- [ ] Queries son eficientes (verificar con EXPLAIN si es necesario)
- [ ] No hay N+1 queries
- [ ] DI registrado correctamente
- [ ] Código compila

#### Application Layer ✅

- [ ] Use Cases implementados
- [ ] Validación de permisos funciona
- [ ] Tests unitarios con mocks pasan
- [ ] Manejo de errores apropiado
- [ ] Retornan `Result<T>`
- [ ] Mapeo de DTOs correcto
- [ ] Código compila

#### WebApi Layer ✅

- [ ] Endpoints implementados
- [ ] Routes correctas
- [ ] Autorización configurada
- [ ] Swagger documentado con ejemplos
- [ ] Tests de endpoints pasan
- [ ] Validación de inputs funciona
- [ ] Responses tienen formato correcto
- [ ] Códigos HTTP apropiados (200, 400, 401, 403, 500)
- [ ] Código compila

#### Integration ✅

- [ ] Todos los tests automatizados pasan
- [ ] Coverage >80% en código nuevo
- [ ] Smoke test manual exitoso
- [ ] Performance verificada (< 500ms típico)
- [ ] No hay memory leaks
- [ ] Documentación actualizada
- [ ] Code review aprobado
- [ ] Listo para merge

---

## 10. Ejemplos Completos

### Ejemplo 1: Dashboard de Usuarios (Read-Only)

**User Story:** KC-87 - Dashboard de Usuarios del Módulo

**Tipo:** Read-Only con filtros y agregaciones

#### Análisis

| Pregunta | Respuesta | Decisión |
|----------|-----------|----------|
| ¿Lectura o escritura? | Solo lectura | DAO |
| ¿Validaciones? | No | N/A |
| ¿Paginación? | Sí | GetManyAndCountAsync |
| ¿Búsqueda? | Sí | SearchAll |
| ¿Agregaciones? | Sí (contadores) | RoleCountDao |

#### Componentes Identificados

**Domain:**
- `UserDao` (7 propiedades)
- `RoleCountDao` (3 propiedades)
- `IUserDaoRepository`
- `IRoleCountDaoRepository`

**Infrastructure:**
- Vista SQL `vw_module_users`
- `UserDaoMap` (mapping)
- `UserDaoRepository`
- `RoleCountDaoRepository`

**Application:**
- `GetUsersQuery` + `GetUsersHandler`
- `GetRoleCountsQuery` + `GetRoleCountsHandler`

**WebApi:**
- `GetUsersEndpoint`
- `GetRoleCountsEndpoint`
- `UserDto`, `RoleCountDto`

#### Subtasks Creadas

1. **[Backend-Domain] Crear DAOs e interfaces** - 1.5h
2. **[Backend-Infrastructure] Implementar capa de datos** - 4h
3. **[Backend-Application] Implementar use cases** - 2h
4. **[Backend-WebApi] Crear endpoints** - 2h
5. **[Backend-Integration] Verificación E2E** - 1.5h

**Total:** 11 horas

#### Diagrama de Secuencia

```
Cliente → Endpoint → UseCase → Repository → Vista SQL → BD
  GET      Auth       Permisos    GetMany      SELECT     Data
  ↓         ↓           ↓           ↓            ↓         ↓
 200 ← Response ← Result ← DAOs ← Rows ← Query Result
```

---

### Ejemplo 2: Crear Cliente (CRUD Simple)

**User Story:** KC-102 - Registro de Clientes

> Como vendedor, quiero registrar un nuevo cliente con sus datos fiscales, para poder generar facturas a su nombre.

**Criterios de Aceptación:**
- Capturar: Nombre, RFC, Razón Social, Email, Teléfono, Dirección
- Validar formato de RFC (13 caracteres persona física, 12 persona moral)
- No permitir RFC duplicados
- Retornar el cliente creado con su ID

**Tipo:** CRUD Simple (Create + Read)

#### Análisis

| Pregunta | Respuesta | Decisión |
|----------|-----------|----------|
| ¿Lectura o escritura? | Escritura (crear cliente) | Entity |
| ¿Validaciones? | Sí (RFC formato, duplicados) | Validator |
| ¿Paginación? | No (solo create) | N/A |
| ¿Relaciones? | No (cliente standalone) | Entity simple |
| ¿Permisos? | Sí (rol Vendedor) | Policy |

#### Componentes Identificados

**Domain:**
- `Cliente` (Entity con 8 propiedades)
- `ClienteValidator` (FluentValidation)
- `IClienteRepository`
- Actualizar `IUnitOfWork`

**Infrastructure:**
- Migration `M001_CreateClientesTable`
- `ClienteMap` (NHibernate mapping)
- `ClienteRepository`
- Registrar en DI

**Application:**
- `CreateClienteCommand` + `CreateClienteHandler`
- `GetClienteQuery` + `GetClienteHandler` (para retornar el creado)

**WebApi:**
- `CreateClienteEndpoint` (POST /api/clientes)
- `GetClienteEndpoint` (GET /api/clientes/{id})
- `ClienteDto`
- Request/Response models

#### Subtasks Creadas

```markdown
## Subtask 1: [Backend-Domain] Crear entidad Cliente con validaciones

**Estimación:** 2h

### Componentes

1. **Entity: Cliente**
   - Ubicación: `Domain/entities/Cliente.cs`
   - Propiedades: Id, Nombre, Rfc, RazonSocial, Email, Telefono, Direccion, CreatedAt

2. **Validator: ClienteValidator**
   - Ubicación: `Domain/entities/validators/ClienteValidator.cs`
   - Reglas: RFC formato (12-13 chars), Email válido, Nombre requerido

3. **Interface: IClienteRepository**
   - Ubicación: `Domain/interfaces/repositories/IClienteRepository.cs`
   - Métodos heredados de IRepository<Cliente, Guid>
   - Método adicional: ExistsByRfcAsync(string rfc)

4. **Actualizar IUnitOfWork**

### Criterios de Aceptación
- [ ] Entity hereda de AbstractDomainObject
- [ ] Validator implementa todas las reglas de negocio
- [ ] Validación de RFC funciona para ambos tipos
- [ ] Interface tiene método para verificar duplicados
```

```markdown
## Subtask 2: [Backend-Infrastructure] Implementar persistencia de Cliente

**Estimación:** 3.5h

### Componentes

1. **Migration: M001_CreateClientesTable**
   - Tabla: clientes
   - Índice único en rfc
   - Índice en nombre (para búsquedas futuras)

2. **Mapper: ClienteMap**
   - Table("clientes")
   - Mapeo de todas las propiedades

3. **Repository: ClienteRepository**
   - Implementa ExistsByRfcAsync con query LINQ

### Criterios de Aceptación
- [ ] Migration crea tabla correctamente
- [ ] Índice único previene RFC duplicados a nivel BD
- [ ] Repository puede crear y leer clientes
- [ ] ExistsByRfcAsync funciona correctamente
```

```markdown
## Subtask 3: [Backend-Application] Implementar use cases de Cliente

**Estimación:** 2.5h

### Componentes

1. **CreateClienteCommand + Handler**
   - Validar que RFC no existe (llamar ExistsByRfcAsync)
   - Crear entidad y persistir
   - Retornar Result<ClienteDto>

2. **GetClienteQuery + Handler**
   - Obtener cliente por ID
   - Retornar Result<ClienteDto>

### Criterios de Aceptación
- [ ] Handler valida duplicados antes de crear
- [ ] Retorna error descriptivo si RFC duplicado
- [ ] Mapeo correcto a DTO
- [ ] Tests unitarios con mocks pasan
```

```markdown
## Subtask 4: [Backend-WebApi] Crear endpoints de Cliente

**Estimación:** 2h

### Componentes

1. **CreateClienteEndpoint**
   - Route: POST /api/clientes
   - Request: CreateClienteRequest
   - Response: ClienteDto
   - Auth: Policy "vendedor"

2. **GetClienteEndpoint**
   - Route: GET /api/clientes/{id}
   - Response: ClienteDto

3. **DTOs y Models**
   - ClienteDto
   - CreateClienteRequest

### Criterios de Aceptación
- [ ] Endpoints accesibles
- [ ] Validación de request funciona (400 si inválido)
- [ ] Auth funciona (401/403)
- [ ] Swagger documentado
```

```markdown
## Subtask 5: [Backend-Integration] Verificación E2E

**Estimación:** 1.5h

### Actividades
- Ejecutar todos los tests
- Smoke test: Crear cliente válido
- Smoke test: Intentar crear con RFC duplicado (debe fallar)
- Smoke test: Intentar crear con RFC inválido (debe fallar)
- Verificar response codes correctos

### Criterios de Aceptación
- [ ] Flujo completo funciona
- [ ] Errores de validación retornan 400
- [ ] RFC duplicado retorna 409 Conflict
- [ ] Performance < 300ms
```

**Total:** 11.5 horas

---

### Ejemplo 3: Crear Factura con Conceptos (CRUD Compleja)

**User Story:** KC-150 - Generación de Facturas

> Como contador, quiero crear una factura con múltiples conceptos de facturación, para poder emitir comprobantes fiscales a los clientes.

**Criterios de Aceptación:**
- Capturar datos generales: Cliente, Fecha, Folio, Serie
- Agregar múltiples conceptos: Descripción, Cantidad, PrecioUnitario, Importe
- Calcular subtotal, IVA y total automáticamente
- Validar que suma de conceptos = subtotal
- No permitir facturas sin conceptos
- Validar que cliente existe

**Tipo:** CRUD Compleja con relaciones One-to-Many

#### Análisis

| Pregunta | Respuesta | Decisión |
|----------|-----------|----------|
| ¿Lectura o escritura? | Escritura | Entity |
| ¿Validaciones? | Sí (múltiples) | Validators |
| ¿Relaciones? | Sí (Factura → Conceptos) | One-to-Many con Bag() |
| ¿Cascade? | Sí (crear conceptos con factura) | Cascade.All |
| ¿Validaciones cross-entity? | Sí (suma conceptos = subtotal) | Validar en Handler |
| ¿Permisos? | Sí (rol Contador) | Policy |

#### Componentes Identificados

**Domain:**
- `Factura` (Entity con navigation property)
- `ConceptoFactura` (Entity hijo)
- `FacturaValidator`
- `ConceptoFacturaValidator`
- `IFacturaRepository`
- Actualizar `IUnitOfWork`

**Infrastructure:**
- Migration `M010_CreateFacturasTable`
- Migration `M011_CreateConceptosFacturaTable` (con FK)
- `FacturaMap` (con Bag() para conceptos)
- `ConceptoFacturaMap`
- `FacturaRepository`

**Application:**
- `CreateFacturaCommand` (con lista de conceptos)
- `CreateFacturaHandler` (validaciones complejas)
- `GetFacturaQuery` + `GetFacturaHandler`

**WebApi:**
- `CreateFacturaEndpoint`
- `GetFacturaEndpoint`
- `FacturaDto` (con lista de ConceptoDto)
- Request models con nested objects

#### Subtasks Creadas

```markdown
## Subtask 1: [Backend-Domain] Crear entities Factura y ConceptoFactura

**Estimación:** 3h

### Componentes

1. **Entity: Factura**
   - Propiedades: Id, ClienteId, Fecha, Folio, Serie, Subtotal, Iva, Total
   - Navigation: IList<ConceptoFactura> Conceptos
   - Método: AddConcepto(), CalcularTotales()

2. **Entity: ConceptoFactura**
   - Propiedades: Id, FacturaId, Descripcion, Cantidad, PrecioUnitario, Importe
   - Back-reference: Factura Factura

3. **Validators**
   - FacturaValidator: Folio requerido, al menos 1 concepto
   - ConceptoFacturaValidator: Cantidad > 0, PrecioUnitario > 0

4. **Interface: IFacturaRepository**
   - Métodos de IRepository
   - GetWithConceptosAsync(Guid id)

### Criterios de Aceptación
- [ ] Entities con navigation properties
- [ ] Propiedades virtual para lazy loading
- [ ] Validators implementados
- [ ] CalcularTotales() funciona correctamente
```

```markdown
## Subtask 2: [Backend-Infrastructure] Implementar persistencia con relaciones

**Estimación:** 5h

### Componentes

1. **Migration: M010_CreateFacturasTable**
   - FK a clientes
   - Índice en (serie, folio)

2. **Migration: M011_CreateConceptosFacturaTable**
   - FK a facturas con ON DELETE CASCADE
   - Índice en factura_id

3. **FacturaMap**
   - Bag() para Conceptos
   - Cascade.AllDeleteOrphan
   - Inverse() en el lado many

4. **ConceptoFacturaMap**
   - References() a Factura

5. **FacturaRepository**
   - GetWithConceptosAsync con Fetch()

### Criterios de Aceptación
- [ ] Migrations crean tablas y FK
- [ ] Cascade funciona (crear factura crea conceptos)
- [ ] Delete cascade funciona
- [ ] Fetch() evita N+1
- [ ] Tests de relaciones pasan
```

```markdown
## Subtask 3: [Backend-Application] Implementar use cases con validaciones complejas

**Estimación:** 3.5h

### Componentes

1. **CreateFacturaCommand**
   - ClienteId, Fecha, Folio, Serie
   - List<CreateConceptoDto> Conceptos

2. **CreateFacturaHandler**
   - Validar que cliente existe
   - Crear Factura y agregar conceptos
   - Calcular totales
   - Validar que suma de importes = subtotal
   - Persistir con UoW
   - Retornar FacturaDto

3. **GetFacturaQuery + Handler**
   - Obtener factura con conceptos

### Criterios de Aceptación
- [ ] Validación de cliente existe
- [ ] Validación de totales correctos
- [ ] Conceptos se crean con la factura
- [ ] Tests unitarios pasan
```

```markdown
## Subtask 4: [Backend-WebApi] Crear endpoints con nested models

**Estimación:** 2.5h

### Componentes

1. **CreateFacturaEndpoint**
   - Route: POST /api/facturas
   - Request con array de conceptos
   - Response: FacturaDto completa

2. **GetFacturaEndpoint**
   - Route: GET /api/facturas/{id}
   - Response: FacturaDto con conceptos

3. **DTOs**
   - FacturaDto (con IEnumerable<ConceptoDto>)
   - ConceptoDto
   - CreateFacturaRequest (con nested CreateConceptoRequest)

4. **AutoMapper Profiles**
   - Mapeo de colecciones

### Criterios de Aceptación
- [ ] Request con nested objects funciona
- [ ] Response incluye conceptos
- [ ] Swagger muestra estructura correcta
- [ ] Validación de array funciona
```

```markdown
## Subtask 5: [Backend-Integration] Verificación E2E

**Estimación:** 2h

### Actividades
- Test E2E: Crear factura con 3 conceptos
- Test E2E: Verificar totales calculados
- Test E2E: Intentar crear sin conceptos (debe fallar)
- Test E2E: Verificar cascade en delete
- Verificar performance (< 500ms)
- Verificar que no hay N+1 en GetFactura

### Criterios de Aceptación
- [ ] Flujo completo funciona
- [ ] Totales correctos
- [ ] Cascade funciona
- [ ] Sin N+1 queries
- [ ] Performance aceptable
```

**Total:** 16 horas

**Ajustes por complejidad:**
- Base CRUD: 12h
- +30% por relaciones many: +3.6h
- +10% por cascade: +1.2h
- -10% ya hay otros repos: -1.2h
- **Ajustado:** 15.6h ≈ 16h ✅

---

## 11. Antipatrones y Errores Comunes

### ❌ Error 1: DAO con Navigation Properties

```csharp
// ❌ MAL: DAO con navigation properties
public class UserDao
{
    public virtual Guid Id { get; set; }
    public virtual string UserName { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } // ¡NO!
}

// ✅ BIEN: DAO con string concatenado
public class UserDao
{
    public virtual Guid Id { get; set; }
    public virtual string UserName { get; set; } = string.Empty;
    public virtual string RoleNames { get; set; } = string.Empty; // "Admin, Contador"
}
```

**Por qué es malo:** Los DAOs son para lectura optimizada. Las navigation properties causan lazy loading y múltiples queries.

---

### ❌ Error 2: Subtask Demasiado Grande

```
❌ MAL: [Backend] Implementar todo el dashboard
   Estimación: 12 horas
   Problema: Muy grande, difícil de estimar y revisar

✅ BIEN: Dividir por capas
   - [Backend-Domain] DAOs (1.5h)
   - [Backend-Infrastructure] Capa de datos (4h)
   - [Backend-Application] Use cases (2h)
   - [Backend-WebApi] Endpoints (2h)
   - [Backend-Integration] E2E (1.5h)
```

**Regla:** Una subtask no debe exceder 6 horas.

---

### ❌ Error 3: No Mencionar Tests

```
❌ MAL:
Subtask: [Backend-WebApi] Crear endpoint
Criterios: Endpoint funciona

✅ BIEN:
Subtask: [Backend-WebApi] Crear endpoint
Criterios:
- [ ] Endpoint funciona
- [ ] Tests de autorización pasan
- [ ] Tests de validación pasan
- [ ] Swagger documentado
```

**Regla:** Siempre incluir "Tests esperados" en cada subtask.

---

### ❌ Error 4: Olvidar SearchAll en DAOs

```csharp
// ❌ MAL: DAO sin SearchAll
public class UserDao
{
    public virtual string UserName { get; set; } = string.Empty;
    public virtual string UserEmail { get; set; } = string.Empty;
}

// ✅ BIEN: DAO con SearchAll
public class UserDao
{
    public virtual string UserName { get; set; } = string.Empty;
    public virtual string UserEmail { get; set; } = string.Empty;
    public virtual string SearchAll { get; set; } = string.Empty; // Combina ambos
}
```

**Por qué es importante:** Permite búsquedas eficientes con una sola query.

---

### ❌ Error 5: Usar Entity para Lectura Simple

```csharp
// ❌ MAL: Entity compleja para listado simple
public class User : AbstractDomainObject
{
    public virtual IList<Role> Roles { get; set; }
    public virtual IValidator GetValidator() => new UserValidator();

    // Usado solo para GET /api/users (listado)
}

// ✅ BIEN: DAO simple para listado
public class UserDao
{
    public virtual Guid Id { get; set; }
    public virtual string UserName { get; set; } = string.Empty;
    public virtual string RoleNames { get; set; } = string.Empty;
    public virtual string SearchAll { get; set; } = string.Empty;
}
```

**Regla:** Si no modificas datos, usa DAO (más rápido).

---

### ❌ Error 6: No Validar Permisos en Use Case

```csharp
// ❌ MAL: Use Case sin validación de permisos
public async Task<Result<UserDto>> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _unitOfWork.UserDaos.GetAsync(query.UserId, ct);
    return Result.Ok(user);
}

// ✅ BIEN: Use Case valida permisos
public async Task<Result<UserDto>> Handle(GetUserQuery query, CancellationToken ct)
{
    // Validar que el usuario actual es Admin del Módulo
    if (!await _authService.IsModuleAdmin(query.ModuleId, ct))
        return Result.Fail("Unauthorized");

    var user = await _unitOfWork.UserDaos.GetAsync(query.UserId, ct);
    return Result.Ok(user);
}
```

**Regla:** Siempre validar permisos en Application Layer.

---

### ❌ Error 7: Olvidar Actualizar IUnitOfWork

```csharp
// ❌ MAL: Crear repositorio pero no agregarlo a UoW
public class ClienteRepository : IClienteRepository { ... }

// Y luego en el handler:
var cliente = await _unitOfWork.Clientes.GetAsync(id); // ¡Error! Clientes no existe

// ✅ BIEN: Siempre agregar a IUnitOfWork
public interface IUnitOfWork
{
    // ... otros repositorios
    IClienteRepository Clientes { get; } // ✅ Agregado
}
```

**Regla:** Todo repositorio nuevo debe agregarse a IUnitOfWork.

---

### ❌ Error 8: No Considerar Cascade en Relaciones

```csharp
// ❌ MAL: Relación sin cascade definido
HasMany(x => x.Conceptos);
// Al eliminar factura, conceptos quedan huérfanos

// ✅ BIEN: Cascade explícito
HasMany(x => x.Conceptos)
    .Cascade.AllDeleteOrphan()
    .Inverse();
```

**Regla:** Siempre definir cascade en relaciones parent-child.

---

## 12. Herramientas y Recursos

### Plantillas

- [Template de Subtask Domain](#backend-domain-template)
- [Template de Subtask Infrastructure](#backend-infrastructure-template)
- [Template de Subtask Application](#backend-application-template)
- [Template de Subtask WebApi](#backend-webapi-template)
- [Template de Subtask Integration](#backend-integration-template)

### Guías de Implementación

Una vez descompuesta la story, consulta estas guías para implementar:

- **CRUD Feature**: [step-by-step guide](../examples/crud-feature/step-by-step.md)
- **Read-Only Feature**: [step-by-step guide](../examples/read-only-feature/step-by-step.md)
- **Complex Feature**: [step-by-step guide](../examples/complex-feature/step-by-step.md)
- **Integration Testing**: [step-by-step guide](../examples/integration-testing/step-by-step.md)

### Guías por Capa

- [Domain Layer Guide](../domain-layer/README.md)
- [Infrastructure Layer Guide](../infrastructure-layer/README.md)
- [Application Layer Guide](../application-layer/README.md)
- [WebApi Layer Guide](../webapi-layer/README.md)

### Diagramas

- **PlantUML:** https://plantuml.com/
- **Mermaid:** https://mermaid.js.org/
- **Draw.io:** https://app.diagrams.net/

### Checklist de Jira

Configura en Jira un checklist reutilizable:
```
Definition of Done - Backend Subtask:
[ ] Código compila
[ ] Tests pasan
[ ] XML comments completos
[ ] Code review aprobado
[ ] Integrado a rama principal
```

---

## Conclusión

Esta guía te ayuda a:
1. ✅ Analizar user stories correctamente
2. ✅ Crear diagramas de secuencia
3. ✅ Identificar componentes necesarios
4. ✅ Crear subtasks ejecutables
5. ✅ Estimar esfuerzo realista
6. ✅ Mantener calidad con Definition of Done

**Próximos pasos:**
- Aplicar esta guía en tu próxima story
- Agregar más ejemplos conforme surjan
- Refinar estimaciones con datos reales del equipo

---

**Última actualización:** 2025-01-18
**Mantenedor:** Equipo APSYS
**Feedback:** Enviar sugerencias a #backend-team
