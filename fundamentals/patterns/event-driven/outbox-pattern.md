# Outbox Pattern

**Versión:** 1.0.0
**Última actualización:** 2025-01-09

## Tabla de Contenidos

- [¿Qué es el Outbox Pattern?](#qué-es-el-outbox-pattern)
- [¿Por qué usar Outbox Pattern?](#por-qué-usar-outbox-pattern)
- [Arquitectura de la Solución](#arquitectura-de-la-solución)
- [Componentes del Patrón](#componentes-del-patrón)
- [Implementación Paso a Paso](#implementación-paso-a-paso)
- [Uso en Use Cases](#uso-en-use-cases)
- [Procesamiento de Eventos Pendientes](#procesamiento-de-eventos-pendientes)
- [Mejores Prácticas](#mejores-prácticas)
- [Antipatrones Comunes](#antipatrones-comunes)
- [Checklist de Implementación](#checklist-de-implementación)

---

## ¿Qué es el Outbox Pattern?

El **Outbox Pattern** es un patrón de diseño que garantiza **consistencia eventual** entre el estado de la aplicación y la publicación de eventos a sistemas externos.

```
┌─────────────────────────────────────────────────────────────────┐
│                      PROBLEMA SIN OUTBOX                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Save to Database ─────────────► ✅ Success                   │
│                                                                  │
│  2. Publish to Message Bus ────────► ❌ Failure (network error)  │
│                                                                  │
│  RESULTADO: Estado guardado pero evento perdido                  │
│             Sistemas externos no reciben notificación            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      SOLUCIÓN CON OUTBOX                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. BEGIN TRANSACTION                                            │
│     ├─ Save Business State ──────► ✅                            │
│     └─ Save Event to Outbox ─────► ✅                            │
│  2. COMMIT TRANSACTION ──────────► ✅ Atómico                    │
│                                                                  │
│  3. Background Job (async)                                       │
│     └─ Publish pending events ───► Retry until success          │
│                                                                  │
│  RESULTADO: Estado y evento siempre consistentes                 │
│             Garantía de entrega (at-least-once)                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Analogía del Mundo Real

Piensa en una **tienda física con servicio de envío**:
- **Sin Outbox**: Cobras al cliente y LUEGO intentas enviar el paquete. Si el envío falla, el cliente pagó pero nunca recibe el producto.
- **Con Outbox**: Cobras al cliente y SIMULTÁNEAMENTE registras el pedido en el libro de envíos. Un mensajero revisa el libro periódicamente y envía los pedidos pendientes.

---

## ¿Por qué usar Outbox Pattern?

### ✅ Beneficios

| Beneficio | Descripción |
|-----------|-------------|
| **Atomicidad** | Estado y evento se persisten en la misma transacción |
| **Durabilidad** | Eventos persisten aunque falle el message bus |
| **Retry automático** | Background job reintenta eventos fallidos |
| **Auditoría** | Todos los eventos quedan registrados |
| **Desacoplamiento** | Use case no espera confirmación del message bus |

### 📊 Comparación: Con vs Sin Outbox

**❌ SIN Outbox Pattern**
```csharp
public class CreateOrderUseCase(IOrderRepository orderRepo, IMessageBus messageBus)
{
    public async Task<Order> ExecuteAsync(OrderDto dto, CancellationToken ct)
    {
        // 1. Guardar orden
        var order = new Order(dto.CustomerId, dto.Items);
        await orderRepo.CreateAsync(order);
        await unitOfWork.CommitAsync(); // ✅ Guardado

        // 2. Publicar evento
        await messageBus.PublishAsync(new OrderCreatedEvent(order.Id)); // ❌ Puede fallar
        // Si falla aquí: orden guardada pero evento perdido

        return order;
    }
}
```

**✅ CON Outbox Pattern**
```csharp
public class CreateOrderUseCase(IUnitOfWork uoW, IEventStore eventStore)
{
    public async Task<Order> ExecuteAsync(OrderDto dto, CancellationToken ct)
    {
        uoW.BeginTransaction();
        try
        {
            // 1. Guardar orden
            var order = new Order(dto.CustomerId, dto.Items);
            await uoW.Orders.CreateAsync(order);

            // 2. Appendear evento (misma transacción)
            await eventStore.AppendAsync(
                new OrderCreatedEvent(order.Id, order.CustomerId),
                organizationId: dto.OrganizationId,
                aggregateType: nameof(Order),
                aggregateId: order.Id);

            // 3. Commit atómico (orden + evento)
            uoW.Commit(); // ✅ Ambos o ninguno

            return order;
        }
        catch
        {
            uoW.Rollback();
            throw;
        }
    }
}
```

---

## Arquitectura de la Solución

### 📂 Estructura de Archivos

```
src/
├── {project}.domain/
│   ├── entities/
│   │   └── DomainEvent.cs              ← Entidad de evento
│   ├── events/
│   │   ├── PublishableEventAttribute.cs ← Marca eventos publicables
│   │   └── orders/
│   │       ├── OrderCreatedEvent.cs     ← Evento específico
│   │       └── OrderCancelledEvent.cs
│   └── interfaces/
│       ├── IEventStore.cs              ← Interface de alto nivel
│       └── repositories/
│           └── IDomainEventRepository.cs ← Interface de repositorio
│
├── {project}.infrastructure/
│   └── nhibernate/
│       ├── EventStore.cs               ← Implementación IEventStore
│       ├── NHDomainEventRepository.cs  ← Implementación repositorio
│       └── mappers/
│           └── DomainEventMapper.cs    ← Mapping NHibernate
│
└── {project}.migrations/
    └── M00XCreateDomainEventsTable.cs  ← Migración de tabla
```

### Diagrama de Capas

```
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                           │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    Use Case                              │    │
│  │  ┌─────────────┐         ┌─────────────┐                │    │
│  │  │ IUnitOfWork │         │ IEventStore │                │    │
│  │  └──────┬──────┘         └──────┬──────┘                │    │
│  └─────────┼───────────────────────┼────────────────────────┘    │
└────────────┼───────────────────────┼────────────────────────────┘
             │                       │
┌────────────┼───────────────────────┼────────────────────────────┐
│            ▼                       ▼        DOMAIN LAYER         │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  IDomainEventRepository        IEventStore              │    │
│  │  (interface)                   (interface)              │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  DomainEvent (entity)          [PublishableEvent]       │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
             ▲                       ▲
             │    Implementa         │
┌────────────┼───────────────────────┼────────────────────────────┐
│            │                       │     INFRASTRUCTURE LAYER    │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  NHDomainEventRepository       EventStore               │    │
│  │  (implementation)              (implementation)         │    │
│  └─────────────────────────────────────────────────────────┘    │
│                         │                                        │
│                         ▼                                        │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              domain_events TABLE                         │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Componentes del Patrón

### 1. DomainEvent Entity

Entidad que almacena eventos para auditoría y outbox.

```csharp
namespace {ProjectName}.domain.entities;

/// <summary>
/// Represents a domain event stored for auditing and outbox pattern purposes.
/// </summary>
public class DomainEvent
{
    // ─────────────────────────────────────────────────────────────
    // Identificación
    // ─────────────────────────────────────────────────────────────

    /// <summary>Unique identifier for this domain event.</summary>
    public virtual Guid Id { get; set; }

    /// <summary>Organization identifier for multi-tenant filtering.</summary>
    public virtual Guid OrganizationId { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Información del Agregado
    // ─────────────────────────────────────────────────────────────

    /// <summary>Type of aggregate that generated this event (e.g., "Order").</summary>
    public virtual string AggregateType { get; set; } = string.Empty;

    /// <summary>Identifier of the aggregate that generated this event.</summary>
    public virtual Guid AggregateId { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Datos del Evento
    // ─────────────────────────────────────────────────────────────

    /// <summary>Type name of the event (e.g., "OrderCreatedEvent").</summary>
    public virtual string EventType { get; set; } = string.Empty;

    /// <summary>JSON-serialized event data.</summary>
    public virtual string EventData { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the event occurred.</summary>
    public virtual DateTime OccurredAt { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Contexto de Auditoría
    // ─────────────────────────────────────────────────────────────

    /// <summary>Identifier of the user who triggered this event.</summary>
    public virtual Guid? UserId { get; set; }

    /// <summary>Username of the user who triggered this event.</summary>
    public virtual string? UserName { get; set; }

    /// <summary>IP address of the client that triggered this event.</summary>
    public virtual string? IpAddress { get; set; }

    /// <summary>Correlation identifier for tracing related events.</summary>
    public virtual string? CorrelationId { get; set; }

    /// <summary>Conversation identifier for message bus conversations.</summary>
    public virtual Guid? ConversationId { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Control de Outbox
    // ─────────────────────────────────────────────────────────────

    /// <summary>Whether this event should be published to the message bus.</summary>
    public virtual bool ShouldPublish { get; set; }

    /// <summary>UTC timestamp when the event was successfully published.</summary>
    public virtual DateTime? PublishedAt { get; set; }

    /// <summary>Number of publish attempts made for this event.</summary>
    public virtual int PublishAttempts { get; set; }

    /// <summary>Last error message from a failed publish attempt.</summary>
    public virtual string? LastPublishError { get; set; }

    /// <summary>Version of this event record.</summary>
    public virtual int Version { get; set; } = 1;
}
```

**🔑 Propiedades Clave:**

| Propiedad | Propósito |
|-----------|-----------|
| `ShouldPublish` | `true` si tiene `[PublishableEvent]`, automático |
| `PublishedAt` | `null` = pendiente, con fecha = publicado |
| `PublishAttempts` | Contador de reintentos (máx. 3) |
| `LastPublishError` | Último error para debugging |

### 2. PublishableEvent Attribute

Marca eventos que deben publicarse al message bus.

```csharp
namespace {ProjectName}.domain.events;

/// <summary>
/// Marks a domain event as publishable to an external message bus.
/// Events without this attribute are only stored for auditing purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PublishableEventAttribute : Attribute
{
}
```

**Uso:**

```csharp
// ✅ Este evento se publicará al message bus
[PublishableEvent]
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId);

// ❌ Este evento solo se guarda para auditoría
public record OrderViewedEvent(Guid OrderId, Guid ViewedByUserId);
```

### 3. IEventStore Interface

Interface de alto nivel para appendear eventos desde use cases.

```csharp
namespace {ProjectName}.domain.interfaces;

/// <summary>
/// High-level interface for appending and retrieving domain events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a domain event to the event store.
    /// Automatically detects if event should be published based on [PublishableEvent] attribute.
    /// </summary>
    Task AppendAsync<TEvent>(
        TEvent @event,
        Guid organizationId,
        string aggregateType,
        Guid aggregateId,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null) where TEvent : class;

    /// <summary>Gets all events for a specific aggregate.</summary>
    Task<IList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct);

    /// <summary>Gets all events for a specific organization.</summary>
    Task<IList<DomainEvent>> GetEventsByOrganizationAsync(Guid organizationId, CancellationToken ct);
}
```

### 4. IDomainEventRepository Interface

Interface de repositorio con métodos específicos para outbox.

```csharp
namespace {ProjectName}.domain.interfaces.repositories;

/// <summary>
/// Repository for managing DomainEvent entities with outbox pattern support.
/// </summary>
public interface IDomainEventRepository : IRepository<DomainEvent, Guid>
{
    /// <summary>Creates a new domain event.</summary>
    Task<DomainEvent> CreateAsync(DomainEvent domainEvent);

    /// <summary>Gets all events for a specific aggregate.</summary>
    Task<IList<DomainEvent>> GetByAggregateIdAsync(Guid aggregateId, CancellationToken ct);

    /// <summary>Gets all events for a specific organization.</summary>
    Task<IList<DomainEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct);

    // ─────────────────────────────────────────────────────────────
    // Métodos de Outbox Pattern
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets events pending to publish.
    /// Only returns events where ShouldPublish=true, PublishedAt=null, PublishAttempts < maxRetries.
    /// </summary>
    Task<IList<DomainEvent>> GetPendingToPublishAsync(int batchSize, CancellationToken ct);

    /// <summary>Marks an event as successfully published.</summary>
    Task MarkAsPublishedAsync(Guid id, CancellationToken ct);

    /// <summary>Marks an event as failed to publish.</summary>
    Task MarkAsFailedAsync(Guid id, string error, CancellationToken ct);
}
```

---

## Implementación Paso a Paso

### Paso 1: Crear la Migración de Base de Datos

```csharp
using FluentMigrator;

namespace {ProjectName}.migrations;

/// <summary>
/// Migration to create the domain_events table for Outbox Pattern.
/// </summary>
[Migration({MigrationNumber})]
public class M{MigrationNumber}CreateDomainEventsTable : Migration
{
    private readonly string _tableName = "domain_events";
    private readonly string _schemaName = "{SchemaName}";

    public override void Up()
    {
        Create.Table(_tableName)
            .InSchema(_schemaName)
            // Identificación
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("organization_id").AsGuid().NotNullable()
            // Agregado
            .WithColumn("aggregate_type").AsString(200).NotNullable()
            .WithColumn("aggregate_id").AsGuid().NotNullable()
            // Evento
            .WithColumn("event_type").AsString(200).NotNullable()
            .WithColumn("event_data").AsCustom("JSONB").NotNullable()
            .WithColumn("occurred_at").AsDateTime().NotNullable()
            // Auditoría
            .WithColumn("user_id").AsGuid().Nullable()
            .WithColumn("user_name").AsString(200).Nullable()
            .WithColumn("ip_address").AsString(45).Nullable()
            .WithColumn("correlation_id").AsString(100).Nullable()
            .WithColumn("conversation_id").AsGuid().Nullable()
            // Outbox
            .WithColumn("should_publish").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("published_at").AsDateTime().Nullable()
            .WithColumn("publish_attempts").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("last_publish_error").AsCustom("TEXT").Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Index: Query por organización (multi-tenant)
        Create.Index("ix_domain_events_organization")
            .OnTable(_tableName).InSchema(_schemaName)
            .OnColumn("organization_id").Ascending()
            .OnColumn("occurred_at").Descending();

        // Index: Query por agregado (auditoría)
        Create.Index("ix_domain_events_aggregate")
            .OnTable(_tableName).InSchema(_schemaName)
            .OnColumn("aggregate_id").Ascending()
            .OnColumn("occurred_at").Descending();

        // Index: Correlation tracking
        Create.Index("ix_domain_events_correlation")
            .OnTable(_tableName).InSchema(_schemaName)
            .OnColumn("correlation_id");

        // Index: Outbox pattern (eventos pendientes)
        Create.Index("ix_domain_events_outbox")
            .OnTable(_tableName).InSchema(_schemaName)
            .OnColumn("should_publish").Ascending()
            .OnColumn("published_at").Ascending()
            .OnColumn("publish_attempts").Ascending()
            .OnColumn("occurred_at").Ascending();
    }

    public override void Down()
    {
        Delete.Index("ix_domain_events_outbox").OnTable(_tableName).InSchema(_schemaName);
        Delete.Index("ix_domain_events_correlation").OnTable(_tableName).InSchema(_schemaName);
        Delete.Index("ix_domain_events_aggregate").OnTable(_tableName).InSchema(_schemaName);
        Delete.Index("ix_domain_events_organization").OnTable(_tableName).InSchema(_schemaName);
        Delete.Table(_tableName).InSchema(_schemaName);
    }
}
```

**🔑 Índices Importantes:**

| Índice | Propósito |
|--------|-----------|
| `ix_domain_events_organization` | Queries multi-tenant |
| `ix_domain_events_aggregate` | Historial de entidad |
| `ix_domain_events_correlation` | Trazabilidad de eventos relacionados |
| `ix_domain_events_outbox` | Obtener eventos pendientes eficientemente |

### Paso 2: Implementar EventStore

```csharp
using System.Reflection;
using System.Text.Json;
using {ProjectName}.domain.entities;
using {ProjectName}.domain.events;
using {ProjectName}.domain.interfaces;
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// NHibernate implementation of IEventStore.
/// Automatically detects publishable events and serializes to JSON.
/// </summary>
public class EventStore(IUnitOfWork uoW) : IEventStore
{
    private readonly IUnitOfWork _uoW = uoW;

    public async Task AppendAsync<TEvent>(
        TEvent @event,
        Guid organizationId,
        string aggregateType,
        Guid aggregateId,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null) where TEvent : class
    {
        // Detectar si evento debe publicarse basado en [PublishableEvent]
        var shouldPublish = typeof(TEvent)
            .GetCustomAttribute<PublishableEventAttribute>() != null;

        var domainEvent = new DomainEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = typeof(TEvent).Name,
            EventData = JsonSerializer.Serialize(@event, GetJsonSerializerOptions()),
            OccurredAt = DateTime.UtcNow,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            CorrelationId = correlationId ?? aggregateId.ToString(),
            ConversationId = Guid.NewGuid(),
            ShouldPublish = shouldPublish,
            PublishedAt = null,
            PublishAttempts = 0,
            Version = 1
        };

        await _uoW.DomainEvents.CreateAsync(domainEvent);
    }

    public async Task<IList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct)
        => await _uoW.DomainEvents.GetByAggregateIdAsync(aggregateId, ct);

    public async Task<IList<DomainEvent>> GetEventsByOrganizationAsync(Guid organizationId, CancellationToken ct)
        => await _uoW.DomainEvents.GetByOrganizationIdAsync(organizationId, ct);

    private static JsonSerializerOptions GetJsonSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
```

### Paso 3: Implementar NHDomainEventRepository

```csharp
using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// NHibernate implementation of IDomainEventRepository.
/// </summary>
public class NHDomainEventRepository : NHReadOnlyRepository<DomainEvent, Guid>, IDomainEventRepository
{
    private const int MaxRetries = 3;

    public NHDomainEventRepository(ISession session) : base(session) { }

    // ─────────────────────────────────────────────────────────────
    // IRepository Implementation
    // ─────────────────────────────────────────────────────────────

    public DomainEvent Add(DomainEvent item)
    {
        _session.Save(item);
        FlushWhenNotActiveTransaction();
        return item;
    }

    public async Task AddAsync(DomainEvent item)
    {
        await _session.SaveAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    public DomainEvent Save(DomainEvent item)
    {
        _session.Update(item);
        FlushWhenNotActiveTransaction();
        return item;
    }

    public async Task SaveAsync(DomainEvent item)
    {
        await _session.UpdateAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    public void Delete(DomainEvent item)
    {
        _session.Delete(item);
        FlushWhenNotActiveTransaction();
    }

    public async Task DeleteAsync(DomainEvent item)
    {
        await _session.DeleteAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // IDomainEventRepository Implementation
    // ─────────────────────────────────────────────────────────────

    public async Task<DomainEvent> CreateAsync(DomainEvent domainEvent)
    {
        await _session.SaveAsync(domainEvent);
        await FlushWhenNotActiveTransactionAsync();
        return domainEvent;
    }

    public async Task<IList<DomainEvent>> GetByAggregateIdAsync(Guid aggregateId, CancellationToken ct)
    {
        return await _session.Query<DomainEvent>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    public async Task<IList<DomainEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct)
    {
        return await _session.Query<DomainEvent>()
            .Where(e => e.OrganizationId == organizationId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    public async Task<IList<DomainEvent>> GetPendingToPublishAsync(int batchSize, CancellationToken ct)
    {
        return await _session.Query<DomainEvent>()
            .Where(e => e.ShouldPublish
                && e.PublishedAt == null
                && e.PublishAttempts < MaxRetries)
            .OrderBy(e => e.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsPublishedAsync(Guid id, CancellationToken ct)
    {
        var domainEvent = await _session.GetAsync<DomainEvent>(id, ct);
        if (domainEvent != null)
        {
            domainEvent.PublishedAt = DateTime.UtcNow;
            await _session.UpdateAsync(domainEvent, ct);
            await FlushWhenNotActiveTransactionAsync();
        }
    }

    public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken ct)
    {
        var domainEvent = await _session.GetAsync<DomainEvent>(id, ct);
        if (domainEvent != null)
        {
            domainEvent.PublishAttempts++;
            domainEvent.LastPublishError = error;
            await _session.UpdateAsync(domainEvent, ct);
            await FlushWhenNotActiveTransactionAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────────────────────

    private void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = _session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            _session.Flush();
    }

    private async Task FlushWhenNotActiveTransactionAsync()
    {
        var currentTransaction = _session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            await _session.FlushAsync();
    }
}
```

### Paso 4: Agregar al IUnitOfWork

```csharp
public interface IUnitOfWork
{
    // ... otros repositorios ...

    /// <summary>Repository for domain events (outbox pattern).</summary>
    IDomainEventRepository DomainEvents { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

### Paso 5: Registrar en Dependency Injection

```csharp
// Program.cs o ServiceCollectionExtensions.cs
services.AddScoped<IEventStore, EventStore>();
services.AddScoped<IDomainEventRepository, NHDomainEventRepository>();
```

---

## Uso en Use Cases

### Ejemplo Completo: AddRoleToUserUseCase

```csharp
using FastEndpoints;
using FluentResults;
using {ProjectName}.domain.entities;
using {ProjectName}.domain.events.users;
using {ProjectName}.domain.interfaces;
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.application.usecases.users;

public abstract class AddRoleToUserUseCase
{
    public class Command : ICommand<Result<ModuleUser>>
    {
        public Guid OrganizationId { get; set; }
        public Guid ModuleUserId { get; set; }
        public Guid RoleId { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW, IEventStore eventStore, ILogger<Handler> logger)
        : ICommandHandler<Command, Result<ModuleUser>>
    {
        private readonly IUnitOfWork _uoW = uoW;
        private readonly IEventStore _eventStore = eventStore;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<Result<ModuleUser>> ExecuteAsync(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                _logger.LogInformation(
                    "Adding role {RoleId} to user {ModuleUserId}",
                    command.RoleId, command.ModuleUserId);

                // 1. Get entities
                var moduleUser = await _uoW.ModuleUsers.GetAsync(command.ModuleUserId, ct);
                if (moduleUser == null)
                {
                    _uoW.Rollback();
                    return Result.Fail<ModuleUser>(new ModuleUserNotFoundError(command.ModuleUserId));
                }

                var role = await _uoW.ModuleRoles.GetAsync(command.RoleId, ct);
                if (role == null)
                {
                    _uoW.Rollback();
                    return Result.Fail<ModuleUser>(new RoleNotFoundError(command.RoleId));
                }

                // 2. Execute business logic
                var added = moduleUser.AddRole(role);
                if (!added)
                {
                    _uoW.Rollback();
                    return Result.Fail<ModuleUser>(
                        new RoleAlreadyAssignedError(command.RoleId, command.ModuleUserId));
                }

                // 3. Append domain event (same transaction)
                await _eventStore.AppendAsync(
                    new RoleAddedToUserEvent(
                        OrganizationId: command.OrganizationId,
                        ModuleUserId: command.ModuleUserId,
                        ModuleUserName: moduleUser.Username,
                        RoleId: command.RoleId,
                        RoleName: role.Name),
                    organizationId: command.OrganizationId,
                    aggregateType: nameof(ModuleUser),
                    aggregateId: command.ModuleUserId,
                    userName: command.CurrentUserName);

                // 4. Commit atomically (state + event)
                _uoW.Commit();

                _logger.LogInformation(
                    "Successfully added role {RoleName} to user {ModuleUserId}",
                    role.Name, command.ModuleUserId);

                return Result.Ok(moduleUser);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                _logger.LogError(ex, "Error adding role {RoleId} to user {ModuleUserId}",
                    command.RoleId, command.ModuleUserId);
                return Result.Fail<ModuleUser>(
                    new Error($"Error adding role: {ex.Message}").CausedBy(ex));
            }
        }
    }
}
```

### Definición del Evento

```csharp
namespace {ProjectName}.domain.events.users;

// Sin [PublishableEvent] = solo auditoría
public record RoleAddedToUserEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid RoleId,
    string RoleName);

// Con [PublishableEvent] = auditoría + publicación
[PublishableEvent]
public record UserAccessGrantedEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid UserId);
```

---

## Procesamiento de Eventos Pendientes

### Background Service para Publicar Eventos

```csharp
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.infrastructure.services;

/// <summary>
/// Background service that processes pending events from the outbox.
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDomainEventRepository>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pendingEvents = await repository.GetPendingToPublishAsync(BatchSize, ct);

        foreach (var domainEvent in pendingEvents)
        {
            try
            {
                await messageBus.PublishAsync(
                    domainEvent.EventType,
                    domainEvent.EventData,
                    domainEvent.CorrelationId,
                    ct);

                await repository.MarkAsPublishedAsync(domainEvent.Id, ct);

                _logger.LogInformation(
                    "Published event {EventType} for aggregate {AggregateId}",
                    domainEvent.EventType, domainEvent.AggregateId);
            }
            catch (Exception ex)
            {
                await repository.MarkAsFailedAsync(domainEvent.Id, ex.Message, ct);

                _logger.LogWarning(ex,
                    "Failed to publish event {EventId}, attempt {Attempt}",
                    domainEvent.Id, domainEvent.PublishAttempts + 1);
            }
        }
    }
}
```

---

## Mejores Prácticas

### ✅ DO: Buenas Prácticas

#### 1. Siempre usar transacción

```csharp
// ✅ CORRECTO: Evento en misma transacción
_uoW.BeginTransaction();
try
{
    await _uoW.Orders.CreateAsync(order);
    await _eventStore.AppendAsync(new OrderCreatedEvent(...), ...);
    _uoW.Commit(); // Atómico
}
catch
{
    _uoW.Rollback();
    throw;
}
```

#### 2. Usar records para eventos inmutables

```csharp
// ✅ CORRECTO: Record inmutable
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount);

// ❌ INCORRECTO: Clase mutable
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; } // Mutable
}
```

#### 3. Incluir contexto de auditoría

```csharp
// ✅ CORRECTO: Incluir usuario y contexto
await _eventStore.AppendAsync(
    @event,
    organizationId: command.OrganizationId,
    aggregateType: nameof(Order),
    aggregateId: order.Id,
    userId: currentUser.Id,           // ✅ Quién
    userName: currentUser.Username,   // ✅ Quién
    ipAddress: request.IpAddress);    // ✅ Desde dónde
```

#### 4. Nombrar eventos en pasado

```csharp
// ✅ CORRECTO: Nombre en pasado (ya ocurrió)
public record OrderCreatedEvent(...);
public record PaymentProcessedEvent(...);
public record UserDeactivatedEvent(...);

// ❌ INCORRECTO: Nombre en presente o futuro
public record CreateOrderEvent(...);
public record ProcessPaymentEvent(...);
```

### ❌ DON'T: Antipatrones

#### 1. NO publicar fuera de transacción

```csharp
// ❌ INCORRECTO: Evento fuera de transacción
await _uoW.Orders.CreateAsync(order);
_uoW.Commit();

// Si esto falla, el evento se pierde
await _eventStore.AppendAsync(...);
```

#### 2. NO incluir datos sensibles en eventos

```csharp
// ❌ INCORRECTO: Datos sensibles
public record UserCreatedEvent(
    Guid UserId,
    string Email,
    string Password,    // ❌ Nunca incluir
    string CreditCard); // ❌ Nunca incluir

// ✅ CORRECTO: Solo identificadores y datos públicos
public record UserCreatedEvent(
    Guid UserId,
    string Email);
```

#### 3. NO esperar resultado del message bus

```csharp
// ❌ INCORRECTO: Esperar confirmación del bus
await _eventStore.AppendAsync(...);
await _messageBus.PublishAndWaitAsync(...); // ❌ Bloquea el use case

// ✅ CORRECTO: Background service publica async
await _eventStore.AppendAsync(...); // Solo guarda
// Background service publica después
```

---

## Antipatrones Comunes

### ❌ 1. Dual Write Problem

**Problema:** Escribir a base de datos Y message bus sin transacción.

```csharp
// ❌ INCORRECTO: Dual write
await _orderRepo.CreateAsync(order);
await _messageBus.PublishAsync(new OrderCreatedEvent(...)); // Puede fallar
```

**Solución:** Usar Outbox Pattern.

### ❌ 2. Eventos Gigantes

**Problema:** Incluir toda la entidad en el evento.

```csharp
// ❌ INCORRECTO: Evento gigante
public record OrderCreatedEvent(
    Order FullOrder,           // ❌ Entidad completa
    Customer FullCustomer,     // ❌ Relación completa
    List<OrderItem> AllItems); // ❌ Colección completa

// ✅ CORRECTO: Solo datos necesarios
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    int ItemCount);
```

### ❌ 3. Eventos sin Idempotencia

**Problema:** Consumidor falla si recibe evento duplicado.

```csharp
// ❌ INCORRECTO: No idempotente
public async Task HandleOrderCreated(OrderCreatedEvent e)
{
    await _emailService.SendWelcomeEmail(e.CustomerId); // Envía múltiples veces
}

// ✅ CORRECTO: Idempotente
public async Task HandleOrderCreated(OrderCreatedEvent e)
{
    if (await _emailLog.WasSent(e.OrderId, "welcome"))
        return; // Ya procesado

    await _emailService.SendWelcomeEmail(e.CustomerId);
    await _emailLog.MarkAsSent(e.OrderId, "welcome");
}
```

---

## Checklist de Implementación

### 📋 Domain Layer

- [ ] `DomainEvent` entity creada en `domain/entities/`
- [ ] `PublishableEventAttribute` creado en `domain/events/`
- [ ] `IEventStore` interface en `domain/interfaces/`
- [ ] `IDomainEventRepository` interface en `domain/interfaces/repositories/`
- [ ] `IDomainEventRepository` agregado a `IUnitOfWork`
- [ ] Eventos de dominio creados como records en `domain/events/{feature}/`

### 📋 Infrastructure Layer

- [ ] `EventStore` implementado en `infrastructure/nhibernate/`
- [ ] `NHDomainEventRepository` implementado
- [ ] `DomainEventMapper` para NHibernate creado
- [ ] Migración de tabla `domain_events` creada
- [ ] Índices optimizados para outbox queries
- [ ] Servicios registrados en DI

### 📋 Application Layer

- [ ] Use cases usan `IEventStore.AppendAsync()` dentro de transacción
- [ ] Eventos se appendean antes del `Commit()`
- [ ] Contexto de auditoría incluido (userId, userName, ipAddress)

### 📋 Background Processing (Opcional)

- [ ] `OutboxProcessorService` implementado
- [ ] `IMessageBus` interface definida
- [ ] Retry logic con máximo de intentos
- [ ] Logging de eventos publicados/fallidos

---

## Recursos Adicionales

### 📚 Guías Relacionadas

- [Repository Pattern](../repository/repository-pattern.md)
- [Unit of Work Pattern](../repository/unit-of-work-pattern.md)
- [Domain Events](./domain-events.md)

### 🔗 Referencias Externas

- [Outbox Pattern - Microsoft](https://learn.microsoft.com/en-us/azure/architecture/best-practices/transactional-outbox-cosmos)
- [Reliable Messaging - Chris Richardson](https://microservices.io/patterns/data/transactional-outbox.html)

---

**Versión:** 1.0.0
**Fecha:** 2025-01-09
**Autor:** Equipo de Arquitectura
