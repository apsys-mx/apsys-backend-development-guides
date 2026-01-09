using {ProjectName}.domain.entities;

namespace {ProjectName}.domain.interfaces;

/// <summary>
/// Defines a high-level interface for appending and retrieving domain events.
/// This is the primary interface for use cases to interact with the event store.
/// </summary>
/// <remarks>
/// <para>
/// The EventStore automatically detects if an event should be published based on the
/// presence of the [PublishableEvent] attribute on the event class.
/// </para>
/// <para>
/// Events are stored within the same transaction as the business operation,
/// ensuring atomicity between the state change and event persistence (Outbox pattern).
/// </para>
/// </remarks>
public interface IEventStore
{
    /// <summary>
    /// Appends a domain event to the event store.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event. Must be a class.</typeparam>
    /// <param name="event">The event instance to store.</param>
    /// <param name="organizationId">The organization identifier for multi-tenant filtering.</param>
    /// <param name="aggregateType">The type name of the aggregate (e.g., "Order", "User").</param>
    /// <param name="aggregateId">The identifier of the aggregate that generated this event.</param>
    /// <param name="userId">Optional. The identifier of the user who triggered the event.</param>
    /// <param name="userName">Optional. The username of the user who triggered the event.</param>
    /// <param name="ipAddress">Optional. The IP address of the client.</param>
    /// <param name="correlationId">Optional. A correlation ID for tracing. Defaults to aggregateId if not provided.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The event's ShouldPublish flag is automatically set based on whether the event class
    /// has the [PublishableEvent] attribute.
    /// </remarks>
    Task AppendAsync<TEvent>(
        TEvent @event,
        Guid organizationId,
        string aggregateType,
        Guid aggregateId,
        Guid? userId = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null) where TEvent : class;

    /// <summary>
    /// Gets all events for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of events.</returns>
    Task<IList<DomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken ct);

    /// <summary>
    /// Gets all events for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of events.</returns>
    Task<IList<DomainEvent>> GetEventsByOrganizationAsync(Guid organizationId, CancellationToken ct);
}
