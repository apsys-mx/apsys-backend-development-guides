using {ProjectName}.domain.exceptions;
using {ProjectName}.domain.interfaces.repositories;
using FluentValidation;
using NHibernate;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Implementation of the repository pattern using NHibernate ORM.
/// Extends the read-only repository to provide full CRUD operations with validation support.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey> where T : class, new()
{

    /// <summary>
    /// The validator used to validate entities before saving them to the database.
    /// </summary>
    private readonly AbstractValidator<T> validator;

    /// <summary>
    /// Initializes a new instance of the NHibernateRepository class.
    /// </summary>
    /// <param name="session">The NHibernate session used for database operations</param>
    /// <param name="serviceProvider">The service provider used to resolve the validator for the entity type</param>
    /// <exception cref="InvalidOperationException">Thrown when the validator for the entity type cannot be resolved</exception>
    protected NHRepository(ISession session, IServiceProvider serviceProvider)
    : base(session)
    {
        Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
        this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T> ?? throw new InvalidOperationException($"The validator for {typeof(T)} type could not be created");
    }

    /// <summary>
    /// Adds a new entity to the repository after validating it.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>The added entity, possibly with updated properties (like generated IDs)</returns>
    /// <exception cref="InvalidDomainException">Thrown when the entity fails validation</exception>
    public T Add(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);

        this._session.Save(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// Note: This method does not perform validation.
    /// </summary>
    /// <param name="item">The entity to add to the repository</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task AddAsync(T item)
        => this._session.SaveAsync(item);

    /// <summary>
    /// Saves or updates an existing entity in the repository after validating it.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>The saved entity, possibly with updated properties</returns>
    /// <exception cref="InvalidDomainException">Thrown when the entity fails validation</exception>
    public T Save(T item)
    {
        var validationResult = this.validator.Validate(item);
        if (!validationResult.IsValid)
            throw new InvalidDomainException(validationResult.Errors);
        this._session.Update(item);
        this.FlushWhenNotActiveTransaction();
        return item;
    }

    /// <summary>
    /// Asynchronously saves or updates an existing entity in the repository.
    /// Note: This method does not perform validation.
    /// </summary>
    /// <param name="item">The entity to save or update</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task SaveAsync(T item)
        => this._session.UpdateAsync(item);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    public void Delete(T item)
    {
        this._session.Delete(item);
        this.FlushWhenNotActiveTransaction();
    }

    /// <summary>
    /// Asynchronously deletes an entity from the repository.
    /// </summary>
    /// <param name="item">The entity to delete</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public Task DeleteAsync(T item)
        => this._session.DeleteAsync(item);

    /// <summary>
    /// Determines if there is an active transaction in the current NHibernate session.
    /// </summary>
    /// <returns>True if a transaction is active; otherwise, false</returns>
    protected internal bool IsTransactionActive()
        => this._session.GetCurrentTransaction() != null && this._session.GetCurrentTransaction().IsActive;

    /// <summary>
    /// Flushes changes to the database when there is no active transaction.
    /// This method is used to ensure changes are persisted immediately when not within a transaction.
    /// </summary>
    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
