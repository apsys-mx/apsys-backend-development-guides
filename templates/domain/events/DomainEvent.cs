namespace {ProjectName}.domain.entities;

/// <summary>
/// Represents a domain event stored for auditing and outbox pattern purposes.
/// This entity serves as a unified storage for both audit trail and message publishing.
/// </summary>
/// <remarks>
/// <para>
/// This entity implements Event Sourcing Lite pattern, combining:
/// - Outbox Pattern: Events marked with ShouldPublish=true are processed by a background service
/// - Audit Trail: All events are persisted for historical and compliance purposes
/// </para>
/// <para>
/// Events are automatically marked for publishing based on the [PublishableEvent] attribute
/// on the event class.
/// </para>
/// </remarks>
public class DomainEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this domain event.
    /// </summary>
    public virtual Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier for multi-tenant filtering.
    /// </summary>
    public virtual Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the type of aggregate that generated this event.
    /// Example: "Order", "User", "Organization"
    /// </summary>
    public virtual string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the aggregate that generated this event.
    /// </summary>
    public virtual Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the type name of the event.
    /// Example: "OrderCreatedEvent", "UserDeactivatedEvent"
    /// </summary>
    public virtual string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON-serialized event data.
    /// </summary>
    public virtual string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the event occurred.
    /// </summary>
    public virtual DateTime OccurredAt { get; set; }

    #region Audit Context

    /// <summary>
    /// Gets or sets the identifier of the user who triggered this event.
    /// </summary>
    public virtual Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username of the user who triggered this event.
    /// </summary>
    public virtual string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the client that triggered this event.
    /// </summary>
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for tracing related events.
    /// </summary>
    public virtual string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier for message bus conversations.
    /// </summary>
    public virtual Guid? ConversationId { get; set; }

    #endregion

    #region Outbox Pattern

    /// <summary>
    /// Gets or sets a value indicating whether this event should be published to the message bus.
    /// Events with [PublishableEvent] attribute are automatically marked as true.
    /// </summary>
    public virtual bool ShouldPublish { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the event was successfully published.
    /// Null if not yet published or if ShouldPublish is false.
    /// </summary>
    public virtual DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of publish attempts made for this event.
    /// </summary>
    public virtual int PublishAttempts { get; set; }

    /// <summary>
    /// Gets or sets the last error message from a failed publish attempt.
    /// </summary>
    public virtual string? LastPublishError { get; set; }

    #endregion

    /// <summary>
    /// Gets or sets the version of this event record.
    /// </summary>
    public virtual int Version { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class.
    /// Required for ORM mapping.
    /// </summary>
    public DomainEvent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEvent"/> class with the specified values.
    /// </summary>
    public DomainEvent(
        Guid id,
        Guid organizationId,
        string aggregateType,
        Guid aggregateId,
        string eventType,
        string eventData,
        DateTime occurredAt,
        bool shouldPublish)
    {
        Id = id;
        OrganizationId = organizationId;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        EventType = eventType;
        EventData = eventData;
        OccurredAt = occurredAt;
        ShouldPublish = shouldPublish;
        PublishAttempts = 0;
        Version = 1;
    }
}
