using {ProjectName}.domain.entities;

namespace {ProjectName}.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing <see cref="DomainEvent"/> entities.
/// This interface extends <see cref="IRepository{T, TKey}"/> to provide CRUD operations
/// and custom methods for event sourcing and outbox pattern support.
/// </summary>
public interface IDomainEventRepository : IRepository<DomainEvent, Guid>
{
    /// <summary>
    /// Creates a new domain event in the repository.
    /// </summary>
    /// <param name="domainEvent">The domain event to create.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created domain event.</returns>
    Task<DomainEvent> CreateAsync(DomainEvent domainEvent);

    /// <summary>
    /// Gets all events for a specific aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of events.</returns>
    Task<IList<DomainEvent>> GetByAggregateIdAsync(Guid aggregateId, CancellationToken ct);

    /// <summary>
    /// Gets all events for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of events.</returns>
    Task<IList<DomainEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct);

    /// <summary>
    /// Gets events that are pending to be published to the message bus.
    /// Only returns events where ShouldPublish=true, PublishedAt=null, and PublishAttempts &lt; maxRetries.
    /// </summary>
    /// <param name="batchSize">The maximum number of events to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of pending events.</returns>
    Task<IList<DomainEvent>> GetPendingToPublishAsync(int batchSize, CancellationToken ct);

    /// <summary>
    /// Marks an event as successfully published.
    /// Sets PublishedAt to the current UTC time.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAsPublishedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Marks an event as failed to publish.
    /// Increments PublishAttempts and sets LastPublishError.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="error">The error message from the failed publish attempt.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAsFailedAsync(Guid id, string error, CancellationToken ct);
}
