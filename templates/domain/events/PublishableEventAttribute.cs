namespace {ProjectName}.domain.events;

/// <summary>
/// Marks a domain event as publishable to an external message bus.
/// Events without this attribute are only stored for auditing purposes.
/// </summary>
/// <remarks>
/// Use this attribute on domain event classes that need to be published
/// to external consumers via a message broker (e.g., RabbitMQ, Azure Service Bus).
/// Events without this attribute will still be persisted in the domain_events table
/// for audit trail and historical purposes, but will not be sent to the message bus.
/// </remarks>
/// <example>
/// <code>
/// [PublishableEvent]
/// public record OrderCreatedEvent(Guid OrderId, Guid CustomerId);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PublishableEventAttribute : Attribute
{
}
