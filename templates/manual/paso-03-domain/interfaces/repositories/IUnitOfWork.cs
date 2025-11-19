namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Defines the unit of work for the application
/// </summary>
public interface IUnitOfWork : IDisposable
{

    #region crud Repositories

    /** * Define the repositories for managing entities in this region
    * These repositories are used to create, update, delete, and retrieve entities from the database.
    */

    /// <summary>
    /// Repository for managing roles
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Repository for managing users
    /// </summary>
    IUserRepository Users { get; }

    // Agregar más repositorios según las entidades del proyecto

    #endregion

    #region read-only Repositories

    /**
     * Define the read-only repositories for retrieving entities in this region
     * These repositories are used to retrieve entities from the database without modifying them.
     */

    // Agregar repositorios de solo lectura según las necesidades del proyecto

    #endregion

    #region transactions management

    /// <summary>
    /// Commits all changes made during the transaction to the database.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back all changes made during the transaction.
    /// </summary>
    void Rollback();

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    void BeginTransaction();

    /// <summary>
    ///  Resets the current transaction, to clear any existing transaction state.
    /// </summary>
    void ResetTransaction();

    /// <summary>
    /// Determines whether there is an active transaction.
    /// </summary>
    /// <returns></returns>
    bool IsActiveTransaction();

    #endregion
}
