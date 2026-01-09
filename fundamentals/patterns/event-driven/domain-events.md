# Domain Events

**Versión:** 1.0.0
**Última actualización:** 2025-01-09

## Tabla de Contenidos

- [¿Qué son los Domain Events?](#qué-son-los-domain-events)
- [Tipos de Eventos](#tipos-de-eventos)
- [Modelado de Eventos](#modelado-de-eventos)
- [Organización de Archivos](#organización-de-archivos)
- [Convenciones de Nombrado](#convenciones-de-nombrado)
- [Mejores Prácticas](#mejores-prácticas)
- [Ejemplos por Dominio](#ejemplos-por-dominio)

---

## ¿Qué son los Domain Events?

Los **Domain Events** son objetos que representan **algo que ocurrió** en el dominio de negocio. Capturan hechos importantes que otros componentes del sistema pueden necesitar conocer.

```
┌─────────────────────────────────────────────────────────────────┐
│                      DOMAIN EVENT                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  "Algo importante ocurrió en el pasado"                         │
│                                                                  │
│  ┌──────────────────┐                                           │
│  │ OrderCreated     │ ← Nombre en pasado                        │
│  │ ─────────────    │                                           │
│  │ OrderId: Guid    │ ← Identificador del agregado              │
│  │ CustomerId: Guid │ ← Datos relevantes                        │
│  │ TotalAmount: $   │ ← Solo lo necesario                       │
│  │ CreatedAt: Date  │ ← Cuándo ocurrió                          │
│  └──────────────────┘                                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Características de un Domain Event

| Característica | Descripción |
|----------------|-------------|
| **Inmutable** | Una vez creado, no cambia |
| **Pasado** | Representa algo que YA ocurrió |
| **Específico** | Contiene solo datos relevantes |
| **Nombrado** | Usa lenguaje de negocio (Ubiquitous Language) |

---

## Tipos de Eventos

### 1. Eventos de Auditoría

Solo se persisten para tracking y compliance. No se publican a sistemas externos.

```csharp
// Sin atributo = solo auditoría
public record RoleAddedToUserEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid RoleId,
    string RoleName);
```

**Casos de uso:**
- Historial de cambios
- Compliance y regulaciones
- Debugging y análisis
- Reconstrucción de estado

### 2. Eventos Publicables

Se persisten Y se publican al message bus para notificar a otros servicios.

```csharp
[PublishableEvent]
public record UserAccessGrantedEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid UserId);
```

**Casos de uso:**
- Notificar a otros microservicios
- Triggers de workflows externos
- Sincronización de datos
- Notificaciones push/email

### Diagrama de Decisión

```
                    ┌─────────────────────┐
                    │ ¿El evento necesita │
                    │ notificar a otros   │
                    │ servicios?          │
                    └──────────┬──────────┘
                               │
               ┌───────────────┴───────────────┐
               │                               │
               ▼                               ▼
        ┌──────────┐                    ┌──────────┐
        │    SÍ    │                    │    NO    │
        └────┬─────┘                    └────┬─────┘
             │                               │
             ▼                               ▼
   ┌─────────────────────┐        ┌─────────────────────┐
   │ [PublishableEvent]  │        │ Sin atributo        │
   │ - Persiste          │        │ - Solo persiste     │
   │ - Publica a bus     │        │ - Auditoría local   │
   └─────────────────────┘        └─────────────────────┘
```

---

## Modelado de Eventos

### Usar Records (Recomendado)

```csharp
// ✅ CORRECTO: Record inmutable
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CreatedAt);
```

**Beneficios de records:**
- Inmutables por defecto
- Igualdad por valor
- Sintaxis concisa
- Deconstrucción automática
- `with` expressions para copiar

### Estructura de un Evento

```csharp
namespace {ProjectName}.domain.events.orders;

/// <summary>
/// Raised when a new order is created.
/// </summary>
/// <remarks>
/// This event is published to notify inventory and shipping services.
/// </remarks>
[PublishableEvent]
public record OrderCreatedEvent(
    // ─────────────────────────────────────────────────────
    // Identificación del Agregado
    // ─────────────────────────────────────────────────────

    /// <summary>Organization for multi-tenant filtering.</summary>
    Guid OrganizationId,

    /// <summary>The unique identifier of the created order.</summary>
    Guid OrderId,

    // ─────────────────────────────────────────────────────
    // Datos del Evento
    // ─────────────────────────────────────────────────────

    /// <summary>Customer who placed the order.</summary>
    Guid CustomerId,

    /// <summary>Total amount of the order.</summary>
    decimal TotalAmount,

    /// <summary>Number of items in the order.</summary>
    int ItemCount,

    // ─────────────────────────────────────────────────────
    // Metadata
    // ─────────────────────────────────────────────────────

    /// <summary>When the order was created.</summary>
    DateTime CreatedAt
);
```

### Datos a Incluir

| Incluir | No Incluir |
|---------|------------|
| ✅ Identificadores (Guid) | ❌ Entidades completas |
| ✅ Datos necesarios para consumidores | ❌ Datos sensibles (passwords, tarjetas) |
| ✅ Timestamps relevantes | ❌ Relaciones navegables |
| ✅ Valores calculados simples | ❌ Colecciones grandes |
| ✅ Strings descriptivos cortos | ❌ Blobs o archivos |

---

## Organización de Archivos

### Estructura Recomendada

```
src/{project}.domain/
└── events/
    ├── PublishableEventAttribute.cs    ← Atributo compartido
    │
    ├── orders/                         ← Por feature/agregado
    │   ├── OrderCreatedEvent.cs
    │   ├── OrderCancelledEvent.cs
    │   ├── OrderShippedEvent.cs
    │   └── PaymentReceivedEvent.cs
    │
    ├── users/
    │   ├── UserCreatedEvent.cs
    │   ├── UserDeactivatedEvent.cs
    │   ├── RoleAddedToUserEvent.cs
    │   └── UserAccessGrantedEvent.cs
    │
    ├── inventory/
    │   ├── StockUpdatedEvent.cs
    │   └── LowStockAlertEvent.cs
    │
    └── organizations/
        ├── OrganizationCreatedEvent.cs
        └── OrganizationModuleActivatedEvent.cs
```

### Un Archivo por Evento

```csharp
// ✅ CORRECTO: Un evento por archivo
// File: events/orders/OrderCreatedEvent.cs
namespace {ProjectName}.domain.events.orders;

[PublishableEvent]
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId);
```

```csharp
// ❌ INCORRECTO: Múltiples eventos en un archivo
// File: events/OrderEvents.cs
public record OrderCreatedEvent(...);
public record OrderCancelledEvent(...);
public record OrderShippedEvent(...);
```

---

## Convenciones de Nombrado

### Nombres de Eventos

| Patrón | Ejemplo | Descripción |
|--------|---------|-------------|
| `{Sustantivo}{Verbo}Event` | `OrderCreatedEvent` | Estándar para CRUD |
| `{Sustantivo}{Acción}Event` | `PaymentProcessedEvent` | Para acciones de negocio |
| `{Sustantivo}{Estado}Event` | `OrderShippedEvent` | Para cambios de estado |

### Verbos Comunes

| Acción | Verbo Recomendado |
|--------|-------------------|
| Crear | `Created` |
| Actualizar | `Updated` |
| Eliminar | `Deleted` |
| Activar | `Activated` |
| Desactivar | `Deactivated` |
| Enviar | `Shipped`, `Sent` |
| Procesar | `Processed` |
| Completar | `Completed` |
| Cancelar | `Cancelled` |
| Aprobar | `Approved` |
| Rechazar | `Rejected` |

### Ejemplos por Dominio

```csharp
// Órdenes
OrderCreatedEvent
OrderCancelledEvent
OrderShippedEvent
OrderDeliveredEvent
PaymentReceivedEvent

// Usuarios
UserCreatedEvent
UserDeactivatedEvent
UserAccessGrantedEvent
UserAccessRevokedEvent
RoleAddedToUserEvent
RoleRemovedFromUserEvent

// Inventario
StockUpdatedEvent
LowStockAlertEvent
ProductAddedEvent
ProductDiscontinuedEvent

// Organizaciones
OrganizationCreatedEvent
OrganizationModuleActivatedEvent
SubscriptionRenewedEvent
```

---

## Mejores Prácticas

### ✅ DO: Buenas Prácticas

#### 1. Eventos Pequeños y Específicos

```csharp
// ✅ CORRECTO: Evento específico
public record OrderShippedEvent(
    Guid OrderId,
    string TrackingNumber,
    DateTime ShippedAt);

// ❌ INCORRECTO: Evento genérico
public record OrderUpdatedEvent(
    Guid OrderId,
    string FieldName,     // ❌ Genérico
    object OldValue,      // ❌ Sin tipo
    object NewValue);     // ❌ Sin tipo
```

#### 2. Incluir OrganizationId para Multi-Tenancy

```csharp
// ✅ CORRECTO: Siempre incluir OrganizationId
public record OrderCreatedEvent(
    Guid OrganizationId,  // ✅ Para filtrar por tenant
    Guid OrderId,
    Guid CustomerId);
```

#### 3. Documentar con XML Comments

```csharp
/// <summary>
/// Raised when an organization activates a new module.
/// </summary>
/// <remarks>
/// This event triggers:
/// - User provisioning in the module
/// - Billing system notification
/// - Welcome email to admin
/// </remarks>
[PublishableEvent]
public record OrganizationModuleActivatedEvent(
    /// <summary>The organization that activated the module.</summary>
    Guid OrganizationId,

    /// <summary>User who performed the activation.</summary>
    Guid ActivatedByUserId,

    /// <summary>The plan selected for the module.</summary>
    Guid PlanId);
```

#### 4. Usar Tipos Específicos

```csharp
// ✅ CORRECTO: Tipos específicos
public record PaymentProcessedEvent(
    Guid PaymentId,
    decimal Amount,
    string Currency,         // "USD", "EUR"
    PaymentMethod Method);   // Enum

// ❌ INCORRECTO: Tipos genéricos
public record PaymentProcessedEvent(
    string PaymentId,        // ❌ String en vez de Guid
    double Amount,           // ❌ Double en vez de decimal
    string Method);          // ❌ String en vez de enum
```

### ❌ DON'T: Antipatrones

#### 1. NO incluir entidades completas

```csharp
// ❌ INCORRECTO: Entidad completa
public record OrderCreatedEvent(Order FullOrder);

// ✅ CORRECTO: Solo datos necesarios
public record OrderCreatedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount);
```

#### 2. NO usar nombres en presente/futuro

```csharp
// ❌ INCORRECTO: Nombres en presente/futuro
public record CreateOrderEvent(...);      // ❌ Imperativo
public record OrderWillShipEvent(...);    // ❌ Futuro
public record ProcessingPaymentEvent(...); // ❌ Presente continuo

// ✅ CORRECTO: Nombres en pasado
public record OrderCreatedEvent(...);
public record OrderShippedEvent(...);
public record PaymentProcessedEvent(...);
```

#### 3. NO incluir lógica en eventos

```csharp
// ❌ INCORRECTO: Evento con lógica
public record OrderCreatedEvent(Guid OrderId)
{
    public bool IsHighValue => TotalAmount > 1000; // ❌ Lógica
    public void SendNotification() { ... }         // ❌ Comportamiento
}

// ✅ CORRECTO: Evento sin lógica (solo datos)
public record OrderCreatedEvent(
    Guid OrderId,
    decimal TotalAmount);
```

---

## Ejemplos por Dominio

### Usuarios y Acceso

```csharp
namespace {ProjectName}.domain.events.users;

/// <summary>Raised when a new module user is created.</summary>
public record ModuleUserCreatedEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string Username,
    string Email);

/// <summary>Raised when user access is granted to the module.</summary>
[PublishableEvent]
public record UserAccessGrantedEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid UserId);

/// <summary>Raised when user access is revoked.</summary>
public record UserAccessRevokedEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid UserId);

/// <summary>Raised when a role is added to a user.</summary>
public record RoleAddedToUserEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid RoleId,
    string RoleName);

/// <summary>Raised when a role is removed from a user.</summary>
public record RoleRemovedFromUserEvent(
    Guid OrganizationId,
    Guid ModuleUserId,
    string ModuleUserName,
    Guid RoleId,
    string RoleName);
```

### Organizaciones

```csharp
namespace {ProjectName}.domain.events.organizations;

/// <summary>Raised when an organization activates a module.</summary>
[PublishableEvent]
public record OrganizationModuleActivatedEvent(
    Guid OrganizationId,
    Guid ActivatedByUserId,
    string ActivatedByUserName,
    Guid PlanId,
    string BillingCycle,
    DateTime ActivationDate,
    DateTime ActiveUntilDate);

/// <summary>Raised when organization subscription is renewed.</summary>
[PublishableEvent]
public record SubscriptionRenewedEvent(
    Guid OrganizationId,
    Guid PlanId,
    DateTime RenewedAt,
    DateTime NewExpirationDate);
```

### Configuración (Folios)

```csharp
namespace {ProjectName}.domain.events.folios;

/// <summary>Raised when a folio series is created.</summary>
public record FolioCreatedEvent(
    Guid OrganizationId,
    Guid FolioId,
    string Prefix,
    bool Enabled,
    DateTime ValidFrom,
    DateTime ValidUntil);

/// <summary>Raised when a folio is updated.</summary>
public record FolioUpdatedEvent(
    Guid OrganizationId,
    Guid FolioId,
    // Old values
    string OldPrefix,
    bool OldEnabled,
    DateTime OldValidFrom,
    DateTime OldValidUntil,
    // New values
    string NewPrefix,
    bool NewEnabled,
    DateTime NewValidFrom,
    DateTime NewValidUntil);

/// <summary>Raised when labels are added to a folio.</summary>
public record LabelAddedToFolioEvent(
    Guid OrganizationId,
    Guid FolioId,
    Guid LabelId,
    string LabelName);

/// <summary>Raised when labels are removed from a folio.</summary>
public record LabelRemovedFromFolioEvent(
    Guid OrganizationId,
    Guid FolioId,
    Guid LabelId,
    string LabelName);
```

---

## Checklist de Creación de Eventos

### 📋 Al Crear un Nuevo Evento

- [ ] Nombre en pasado (`{Sustantivo}{Verbo}Event`)
- [ ] Usar `record` (no `class`)
- [ ] Incluir `OrganizationId` para multi-tenancy
- [ ] Incluir identificador del agregado (`{Agregado}Id`)
- [ ] Solo datos necesarios (no entidades completas)
- [ ] Sin datos sensibles (passwords, tarjetas)
- [ ] XML documentation con `<summary>`
- [ ] Archivo separado en `domain/events/{feature}/`
- [ ] Decidir si necesita `[PublishableEvent]`

---

**Versión:** 1.0.0
**Fecha:** 2025-01-09
**Autor:** Equipo de Arquitectura
