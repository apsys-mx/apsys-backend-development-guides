// =============================================================================
// TEMPLATE: EventStore
// =============================================================================
// Implementación de IEventStore que utiliza IUnitOfWork para persistir eventos.
//
// Esta clase NO necesita cambio de nombre ya que no sigue convención de prefijo
// de ORM (no es NHEventStore). El nombre "EventStore" es agnóstico del ORM.
//
// Dependencias:
//   - IUnitOfWork: debe tener la propiedad DomainEvents (IDomainEventRepository)
//
// Placeholders a reemplazar:
//   - {ProjectName} → nombre del proyecto (ej: mycompany.myproject)
// =============================================================================

using System.Reflection;
using System.Text.Json;
using {ProjectName}.domain.entities;
using {ProjectName}.domain.events;
using {ProjectName}.domain.interfaces;
using {ProjectName}.domain.interfaces.repositories;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Implementation of the <see cref="IEventStore"/> interface.
/// Provides high-level event sourcing functionality with automatic publishable event detection.
/// </summary>
/// <remarks>
/// <para>
/// This implementation:
/// <list type="bullet">
/// <item>Automatically detects if an event should be published based on [PublishableEvent] attribute</item>
/// <item>Serializes events to JSON for storage</item>
/// <item>Stores events within the same transaction as the business operation (Outbox pattern)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Note:</strong> This class depends on IUnitOfWork having a DomainEvents property
/// that returns an IDomainEventRepository. Ensure your IUnitOfWork implementation includes this.
/// </para>
/// </remarks>
public class EventStore(IUnitOfWork uoW) : IEventStore
{
    private readonly IUnitOfWork _uoW = uoW;

    /// <inheritdoc />
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
        // Detect if event should be published based on [PublishableEvent] attribute
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

    /// <inheritdoc />
    public async Task<IList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct)
    {
        return await _uoW.DomainEvents.GetByAggregateIdAsync(aggregateId, ct);
    }

    /// <inheritdoc />
    public async Task<IList<DomainEvent>> GetEventsByOrganizationAsync(Guid organizationId, CancellationToken ct)
    {
        return await _uoW.DomainEvents.GetByOrganizationIdAsync(organizationId, ct);
    }

    /// <summary>
    /// Gets the JSON serializer options for event serialization.
    /// </summary>
    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
