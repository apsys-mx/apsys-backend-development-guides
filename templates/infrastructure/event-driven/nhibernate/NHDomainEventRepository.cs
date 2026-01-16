// =============================================================================
// TEMPLATE: DomainEventRepository
// =============================================================================
// Este es un template de ejemplo. El nombre de la clase debe adaptarse según
// las convenciones del proyecto:
//
//   - Si el proyecto usa prefijo "NH" (NHUserRepository) → NHDomainEventRepository
//   - Si el proyecto NO usa prefijo (UserRepository)    → DomainEventRepository
//   - Si usa otro ORM (EF)                              → EFDomainEventRepository
//
// La clase base también debe adaptarse según el proyecto:
//   - NHReadOnlyRepository → clase base existente en el proyecto
//
// Placeholders a reemplazar:
//   - {ProjectName} → nombre del proyecto (ej: mycompany.myproject)
// =============================================================================

using {ProjectName}.domain.entities;
using {ProjectName}.domain.interfaces.repositories;
using NHibernate;
using NHibernate.Linq;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// NHibernate implementation of the <see cref="IDomainEventRepository"/> interface.
/// Provides data access operations for <see cref="DomainEvent"/> entities.
/// </summary>
/// <remarks>
/// <para>
/// This repository extends NHReadOnlyRepository instead of NHRepository because
/// DomainEvent is an infrastructure entity without domain validation rules.
/// Write operations are implemented directly without FluentValidation.
/// </para>
/// <para>
/// <strong>Note:</strong> The class name (NHDomainEventRepository) follows the project's
/// naming convention. Adapt the name according to your project's repository naming pattern.
/// </para>
/// </remarks>
public class NHDomainEventRepository : NHReadOnlyRepository<DomainEvent, Guid>, IDomainEventRepository
{
    private const int MaxRetries = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="NHDomainEventRepository"/> class.
    /// </summary>
    /// <param name="session">The NHibernate session used for database operations.</param>
    public NHDomainEventRepository(ISession session)
        : base(session)
    {
    }

    #region IRepository Implementation

    /// <inheritdoc />
    public DomainEvent Add(DomainEvent item)
    {
        _session.Save(item);
        FlushWhenNotActiveTransaction();
        return item;
    }

    /// <inheritdoc />
    public async Task AddAsync(DomainEvent item)
    {
        await _session.SaveAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    /// <inheritdoc />
    public DomainEvent Save(DomainEvent item)
    {
        _session.Update(item);
        FlushWhenNotActiveTransaction();
        return item;
    }

    /// <inheritdoc />
    public async Task SaveAsync(DomainEvent item)
    {
        await _session.UpdateAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    /// <inheritdoc />
    public void Delete(DomainEvent item)
    {
        _session.Delete(item);
        FlushWhenNotActiveTransaction();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(DomainEvent item)
    {
        await _session.DeleteAsync(item);
        await FlushWhenNotActiveTransactionAsync();
    }

    #endregion

    #region IDomainEventRepository Implementation

    /// <inheritdoc />
    public async Task<DomainEvent> CreateAsync(DomainEvent domainEvent)
    {
        await _session.SaveAsync(domainEvent);
        await FlushWhenNotActiveTransactionAsync();
        return domainEvent;
    }

    /// <inheritdoc />
    public async Task<IList<DomainEvent>> GetByAggregateIdAsync(Guid aggregateId, CancellationToken ct)
    {
        return await _session.Query<DomainEvent>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IList<DomainEvent>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct)
    {
        return await _session.Query<DomainEvent>()
            .Where(e => e.OrganizationId == organizationId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// Flushes changes to the database when there is no active transaction.
    /// </summary>
    private void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = _session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            _session.Flush();
    }

    /// <summary>
    /// Asynchronously flushes changes to the database when there is no active transaction.
    /// </summary>
    private async Task FlushWhenNotActiveTransactionAsync()
    {
        var currentTransaction = _session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            await _session.FlushAsync();
    }

    #endregion
}
