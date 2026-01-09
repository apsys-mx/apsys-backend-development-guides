# Event-Driven Patterns

Patrones para manejo de eventos de dominio, auditoría y mensajería.

## Contenido

| Documento | Descripción |
|-----------|-------------|
| [Domain Events](./domain-events.md) | Concepto de eventos de dominio y cómo modelarlos |
| [Outbox Pattern](./outbox-pattern.md) | Patrón para garantizar consistencia entre estado y eventos |

## Visión General

```
┌─────────────────────────────────────────────────────────────────┐
│                    EVENT-DRIVEN PATTERNS                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │   Domain     │    │   Outbox     │    │   Message    │      │
│  │   Events     │ ─► │   Table      │ ─► │   Bus        │      │
│  │              │    │              │    │              │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│        │                    │                    │               │
│        │                    │                    │               │
│        ▼                    ▼                    ▼               │
│   Modelado de         Persistencia         Publicación          │
│   eventos de          atómica con          asíncrona a          │
│   negocio             transacción          consumidores         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Propósitos del Patrón

Esta implementación sirve **dos propósitos principales**:

### 1. Auditoría (Tracking)

Todos los eventos se persisten para:
- Historial de cambios
- Compliance y regulaciones
- Debugging y análisis
- Reconstrucción de estado (Event Sourcing Lite)

### 2. Mensajería (Outbox Pattern)

Solo eventos marcados con `[PublishableEvent]` se publican:
- Garantía de entrega (at-least-once)
- Consistencia eventual con sistemas externos
- Desacoplamiento de servicios

## Flujo de Trabajo

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Use Case   │     │  EventStore │     │  Database   │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │ 1. BeginTransaction                   │
       │──────────────────────────────────────►│
       │                   │                   │
       │ 2. Business Logic │                   │
       │──────────────────────────────────────►│
       │                   │                   │
       │ 3. AppendAsync    │                   │
       │──────────────────►│                   │
       │                   │                   │
       │                   │ 4. Insert Event   │
       │                   │──────────────────►│
       │                   │                   │
       │ 5. Commit (atomic)                    │
       │──────────────────────────────────────►│
       │                   │                   │
```

## Componentes Principales

| Componente | Capa | Propósito |
|------------|------|-----------|
| `IEventStore` | Domain | Interface de alto nivel para appendear eventos |
| `IDomainEventRepository` | Domain | Interface de repositorio con métodos de outbox |
| `DomainEvent` | Domain | Entidad que almacena eventos |
| `[PublishableEvent]` | Domain | Atributo que marca eventos para publicar |
| `EventStore` | Infrastructure | Implementación con serialización JSON |
| `NHDomainEventRepository` | Infrastructure | Implementación NHibernate del repositorio |

## Relación con Otros Patrones

```
┌─────────────────────────────────────────────────────────────┐
│                    REPOSITORY PATTERN                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ IDomainEventRepository : IRepository<DomainEvent>   │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   UNIT OF WORK PATTERN                       │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ IUnitOfWork.DomainEvents                            │    │
│  │ - BeginTransaction()                                │    │
│  │ - Commit() ← Persiste evento + estado en 1 TX      │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## Cuándo Usar

| Escenario | Recomendación |
|-----------|---------------|
| Auditoría de cambios | ✅ Usar sin `[PublishableEvent]` |
| Notificar a otros servicios | ✅ Usar con `[PublishableEvent]` |
| Historial de entidad | ✅ Consultar por `AggregateId` |
| Compliance/regulaciones | ✅ Todos los eventos se persisten |
| Event Sourcing completo | ⚠️ Considerar soluciones especializadas |

## Templates Disponibles

Los templates para implementar este patrón están en:

| Template | Ubicación |
|----------|-----------|
| `DomainEvent.cs` | `templates/domain/events/` |
| `IEventStore.cs` | `templates/domain/interfaces/` |
| `IDomainEventRepository.cs` | `templates/domain/interfaces/repositories/` |
| `PublishableEventAttribute.cs` | `templates/domain/events/` |
| `EventStore.cs` | `templates/infrastructure/event-driven/` |
| `NHDomainEventRepository.cs` | `templates/infrastructure/event-driven/nhibernate/` |
| `DomainEventMapper.cs` | `templates/infrastructure/event-driven/nhibernate/` |
| `CreateDomainEventsTable.cs` | `stacks/database/migrations/fluent-migrator/templates/` |
