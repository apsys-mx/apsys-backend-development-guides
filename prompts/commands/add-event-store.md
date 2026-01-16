# Add Event Store

> **Version:** 1.0.0
> **Ultima actualizacion:** 2025-01-09

Agrega el patron Event Store (Outbox Pattern) a un proyecto backend existente para auditoria y/o mensajeria.

---

## Configuracion

```
GUIDES_REPO: D:\apsys-mx\apsys-backend-development-guides
```

> **Nota:** Ajusta esta ruta segun la ubicacion del repositorio de guias en tu sistema.

---

## Prerequisitos

Antes de ejecutar este comando, el proyecto debe tener:

- [ ] Estructura de Clean Architecture (`domain`, `application`, `infrastructure`, `webapi`)
- [ ] ORM configurado con `IUnitOfWork` y su implementacion (ej: `NHUnitOfWork`, `UnitOfWork`, `EFUnitOfWork`)
- [ ] FluentMigrator configurado (opcional, para crear la tabla automaticamente)

> **Nota sobre nombres de clases:** Los nombres de las clases implementadoras pueden variar entre proyectos.
> Este comando detecta automaticamente las implementaciones buscando por las interfaces que implementan,
> no por nombres de archivo especificos.

---

## Recopilar Informacion

> **IMPORTANTE:** Hacer SIEMPRE como un asistente interactivo.
> Preguntar UNA cosa a la vez y ESPERAR la respuesta del usuario antes de continuar.

### 1. Detectar Proyecto

Buscar automaticamente la estructura del proyecto:

```bash
# Buscar solucion
Glob: **/*.sln

# Buscar interface IUnitOfWork
Glob: **/src/**/IUnitOfWork.cs

# Buscar la IMPLEMENTACION de IUnitOfWork (nombre puede variar)
# Buscar clases que implementen ": IUnitOfWork" o ", IUnitOfWork"
Grep: ": IUnitOfWork|, IUnitOfWork" en **/src/**/infrastructure/**/*.cs

# Buscar configuracion de session factory (nombre puede variar)
# Buscar clases que configuren mappers de NHibernate
Grep: "AddMapping<|ModelMapper|ISessionFactory" en **/src/**/infrastructure/**/*.cs
```

**Variables a detectar y almacenar:**
- `{UnitOfWorkImpl}` = nombre de la clase que implementa IUnitOfWork (ej: NHUnitOfWork, UnitOfWork)
- `{UnitOfWorkImplPath}` = ruta al archivo de la implementacion
- `{SessionFactoryClass}` = nombre de la clase de configuracion de session (ej: NHSessionFactory, SessionFactory)
- `{SessionFactoryPath}` = ruta al archivo de session factory

Si se detecta correctamente, mostrar:
```
Proyecto detectado:
- Solucion: {nombre}.sln
- IUnitOfWork: src/{nombre}.domain/interfaces/repositories/IUnitOfWork.cs
- Implementacion: {UnitOfWorkImplPath} (clase: {UnitOfWorkImpl})
- Session Factory: {SessionFactoryPath} (clase: {SessionFactoryClass})

¿Es correcto? (si/no)
```

Si no se detecta alguna implementacion, preguntar la ruta manualmente.

**Esperar respuesta del usuario.**

### 2. Tipo de Event Store

Preguntar con opciones:
```
¿Que tipo de Event Store necesitas?
1. Solo auditoria (tracking de cambios sin publicar)
2. Auditoria + Mensajeria (Outbox Pattern completo)

Selecciona una opcion (1-2):
```

**Esperar respuesta del usuario.**

### 3. Verificar Migraciones

Buscar proyecto de migraciones:

```bash
Glob: **/src/**/*.migrations/*.csproj
```

Si existe, preguntar:
```
Se detecto proyecto de migraciones: {nombre}.migrations
¿Crear migracion para la tabla domain_events automaticamente? (si/no)
```

Si no existe, informar:
```
No se detecto proyecto de migraciones.
Deberas crear la tabla domain_events manualmente.
¿Continuar? (si/no)
```

**Esperar respuesta del usuario.**

### 4. Schema de Base de Datos

Buscar schema existente en el proyecto:

```bash
Grep: "SchemaName" en **/*Resource.cs
Grep: "Schema(" en **/mappers/*.cs
```

Si se detecta, mostrar:
```
Schema detectado: {schema_name}
¿Usar este schema para domain_events? (si/no)
```

Si elige no o no se detecta, preguntar:
```
¿Cual es el nombre del schema para la tabla domain_events?
```

**Esperar respuesta del usuario.**

### 5. Confirmar configuracion

Mostrar resumen:
```
Configuracion de Event Store:
- Proyecto: {nombre}
- Tipo: {auditoria/auditoria+mensajeria}
- Crear migracion: {si/no}
- Schema: {schema_name}

¿Confirmar e iniciar? (si/no)
```

**Esperar confirmacion del usuario antes de continuar.**

---

## Rutas de Recursos

**Guia principal:**
```
{GUIDES_REPO}/fundamentals/patterns/event-driven/outbox-pattern.md
```

**Templates:**
```
{GUIDES_REPO}/templates/
├── domain/
│   ├── events/
│   │   ├── DomainEvent.cs
│   │   └── PublishableEventAttribute.cs
│   └── interfaces/
│       ├── IEventStore.cs
│       └── repositories/
│           └── IDomainEventRepository.cs
└── infrastructure/
    └── event-driven/
        ├── EventStore.cs
        └── nhibernate/
            ├── NHDomainEventRepository.cs
            └── DomainEventMapper.cs

{GUIDES_REPO}/stacks/database/migrations/fluent-migrator/templates/
└── CreateDomainEventsTable.cs
```

---

## Proceso de Ejecucion

### Fase 0: Mostrar Informacion del Comando

Al iniciar, mostrar:

```
Add Event Store
Version: 1.0.0
Ultima actualizacion: 2025-01-09

Este comando agrega el patron Event Store (Outbox Pattern) a un proyecto existente.
```

### Fase 1: Validacion

1. **Verificar estructura del proyecto:**
   - Debe existir `IUnitOfWork.cs` (interface)
   - Debe existir una clase que implemente `IUnitOfWork` (detectada como `{UnitOfWorkImpl}`)
   - Debe existir una clase de configuracion de session/mappers (detectada como `{SessionFactoryClass}`)

2. **Verificar que no exista Event Store:**
   ```bash
   Glob: **/IEventStore.cs
   Glob: **/DomainEvent.cs
   ```
   Si existen, DETENER y avisar que ya esta configurado.

### Fase 2: Crear Todo List

Invocar `TodoWrite` con las siguientes tareas (usar nombres detectados):

```
- [ ] Crear DomainEvent entity
- [ ] Crear PublishableEventAttribute
- [ ] Crear IEventStore interface
- [ ] Crear IDomainEventRepository interface
- [ ] Actualizar IUnitOfWork
- [ ] Crear EventStore implementation
- [ ] Crear DomainEventRepository implementation
- [ ] Crear DomainEventMapper
- [ ] Actualizar {UnitOfWorkImpl} (implementacion de IUnitOfWork)
- [ ] Registrar DomainEventMapper en {SessionFactoryClass}
- [ ] Registrar EventStore en DI
- [ ] Crear migracion (si aplica)
- [ ] Verificacion final
```

### Fase 3: Ejecutar Implementacion

#### 3.1 Domain Layer

**Crear DomainEvent.cs:**
```
{GUIDES_REPO}/templates/domain/events/DomainEvent.cs
  → src/{ProjectName}.domain/entities/DomainEvent.cs
```

**Crear PublishableEventAttribute.cs:**
```
{GUIDES_REPO}/templates/domain/events/PublishableEventAttribute.cs
  → src/{ProjectName}.domain/events/PublishableEventAttribute.cs
```

**Crear IEventStore.cs:**
```
{GUIDES_REPO}/templates/domain/interfaces/IEventStore.cs
  → src/{ProjectName}.domain/interfaces/IEventStore.cs
```

**Crear IDomainEventRepository.cs:**
```
{GUIDES_REPO}/templates/domain/interfaces/repositories/IDomainEventRepository.cs
  → src/{ProjectName}.domain/interfaces/repositories/IDomainEventRepository.cs
```

**Actualizar IUnitOfWork.cs:**

Agregar propiedad:
```csharp
/// <summary>Repository for domain events (outbox pattern).</summary>
IDomainEventRepository DomainEvents { get; }
```

#### 3.2 Infrastructure Layer

**Crear EventStore.cs:**
```
{GUIDES_REPO}/templates/infrastructure/event-driven/EventStore.cs
  → src/{ProjectName}.infrastructure/nhibernate/EventStore.cs
```

**Crear DomainEventRepository:**

Usar el template como base y adaptar el nombre segun convenciones del proyecto:
```
{GUIDES_REPO}/templates/infrastructure/event-driven/nhibernate/NHDomainEventRepository.cs
  → src/{ProjectName}.infrastructure/nhibernate/{Prefix}DomainEventRepository.cs
```

> **Nota:** El nombre de la clase debe seguir la convencion del proyecto.
> Si el proyecto usa `NHUserRepository`, usar `NHDomainEventRepository`.
> Si usa `UserRepository`, usar `DomainEventRepository`.
> Detectar el prefijo usado en otros repositorios del proyecto.

**Crear DomainEventMapper.cs:**
```
{GUIDES_REPO}/templates/infrastructure/event-driven/nhibernate/DomainEventMapper.cs
  → src/{ProjectName}.infrastructure/nhibernate/mappers/DomainEventMapper.cs
```

Reemplazar `{SchemaName}` con el schema detectado/proporcionado.

**Actualizar la implementacion de IUnitOfWork ({UnitOfWorkImpl}):**

Agregar lazy property en `{UnitOfWorkImplPath}`:
```csharp
private IDomainEventRepository? _domainEvents;
public IDomainEventRepository DomainEvents => _domainEvents ??= new {DomainEventRepositoryClass}(_session);
```

> **Nota:** Usar el nombre de clase del DomainEventRepository creado (ej: `NHDomainEventRepository` o `DomainEventRepository`).

**Actualizar la clase de configuracion de session ({SessionFactoryClass}):**

Agregar en el metodo de configuracion de mappers en `{SessionFactoryPath}`:
```csharp
mapper.AddMapping<DomainEventMapper>();
```

#### 3.3 WebAPI Layer

**Actualizar Program.cs:**

Agregar registro de DI:
```csharp
builder.Services.AddScoped<IEventStore, EventStore>();
```

#### 3.4 Migraciones (si aplica)

**Crear migracion:**
```
{GUIDES_REPO}/stacks/database/migrations/fluent-migrator/templates/CreateDomainEventsTable.cs
  → src/{ProjectName}.migrations/M{NextNumber}CreateDomainEventsTable.cs
```

Reemplazar:
- `{MigrationNumber}` → Siguiente numero de migracion disponible
- `{SchemaName}` → Schema proporcionado

Para obtener el siguiente numero de migracion:
```bash
Glob: **/M*.cs en el proyecto de migraciones
```
Obtener el numero mas alto y sumar 1.

### Fase 4: Verificacion Final

1. **Compilar solucion:**
   ```bash
   dotnet build
   ```

2. **Verificar archivos creados:**
   - [ ] `DomainEvent.cs` existe
   - [ ] `PublishableEventAttribute.cs` existe
   - [ ] `IEventStore.cs` existe
   - [ ] `IDomainEventRepository.cs` existe
   - [ ] `EventStore.cs` existe
   - [ ] `{Prefix}DomainEventRepository.cs` existe (nombre segun convencion del proyecto)
   - [ ] `DomainEventMapper.cs` existe
   - [ ] Migracion creada (si aplica)

---

## Formato de Salida

Al finalizar:

```markdown
# Event Store Agregado

**Proyecto:** {nombre}
**Tipo:** {auditoria/auditoria+mensajeria}

## Archivos Creados

### Domain Layer
- `entities/DomainEvent.cs`
- `events/PublishableEventAttribute.cs`
- `interfaces/IEventStore.cs`
- `interfaces/repositories/IDomainEventRepository.cs`

### Infrastructure Layer
- `nhibernate/EventStore.cs`
- `nhibernate/{Prefix}DomainEventRepository.cs`
- `nhibernate/mappers/DomainEventMapper.cs`

### Migraciones
- `M{num}CreateDomainEventsTable.cs` (si aplica)

## Archivos Modificados

- `IUnitOfWork.cs` - Agregada propiedad `DomainEvents`
- `{UnitOfWorkImpl}.cs` - Agregada lazy property para DomainEvents
- `{SessionFactoryClass}.cs` - Registrado `DomainEventMapper`
- `Program.cs` - Registrado `IEventStore` en DI

## Proximos Pasos

1. **Si creaste migracion:** Ejecutar migraciones para crear la tabla
   ```bash
   dotnet run --project src/{ProjectName}.migrations -- cnn="..."
   ```

2. **Crear eventos de dominio:** En `domain/events/{feature}/`
   ```csharp
   // Solo auditoria
   public record OrderCreatedEvent(Guid OrderId, Guid CustomerId);

   // Con publicacion a message bus
   [PublishableEvent]
   public record PaymentProcessedEvent(Guid PaymentId, decimal Amount);
   ```

3. **Usar en Use Cases:**
   ```csharp
   await _eventStore.AppendAsync(
       new OrderCreatedEvent(order.Id, order.CustomerId),
       organizationId: command.OrganizationId,
       aggregateType: nameof(Order),
       aggregateId: order.Id,
       userId: currentUserId,
       userName: currentUserName);
   ```

## Referencias

- [Outbox Pattern]({GUIDES_REPO}/fundamentals/patterns/event-driven/outbox-pattern.md)
- [Domain Events]({GUIDES_REPO}/fundamentals/patterns/event-driven/domain-events.md)
```

---

## Manejo de Errores

Si ocurre un error:

1. **Registrar el error** con contexto
2. **No dejar archivos parciales** - hacer rollback si es posible
3. **Sugerir solucion**
4. **Preguntar** si continuar o cancelar

### Errores Comunes

| Error | Causa | Solucion |
|-------|-------|----------|
| IUnitOfWork no encontrado | Proyecto no sigue Clean Architecture | Verificar estructura |
| Event Store ya existe | Ya fue configurado previamente | No hacer nada |
| Migraciones no encontradas | Proyecto sin FluentMigrator | Crear tabla manualmente |
| Schema no detectado | No hay mappers existentes | Preguntar al usuario |

---

## Restricciones

### NO debes:
- Sobrescribir archivos existentes sin confirmacion
- Crear eventos de dominio de ejemplo (solo la infraestructura)
- Modificar la logica de negocio existente

### DEBES:
- Validar que el proyecto tenga la estructura correcta
- Reemplazar TODOS los placeholders
- Verificar compilacion al final
- Mostrar proximos pasos claros

---

## Notas Importantes

- El Event Store se integra con el `IUnitOfWork` existente
- Los eventos se persisten en la **misma transaccion** que el estado
- Solo eventos con `[PublishableEvent]` se publican al message bus
- La tabla `domain_events` tiene indices optimizados para outbox queries
