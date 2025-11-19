using MiProyecto.domain.interfaces.repositories;
using NHibernate;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// NHUnitOfWork is a concrete implementation of the IUnitOfWork interface.
/// It is used to manage transactions and the lifecycle of database operations in an NHibernate context.
/// </summary>
public class NHUnitOfWork : IUnitOfWork
{
    private bool _disposed = false;
    protected internal readonly ISession _session;
    protected internal readonly IServiceProvider _serviceProvider;
    protected internal ITransaction? _transaction;

    #region CRUD Repositories
    // Agregar propiedades de repositorios según las entidades del dominio
    // Ejemplo:
    // public IRoleRepository Roles => new NHRoleRepository(_session, _serviceProvider);
    // public IUserRepository Users => new NHUserRepository(_session, _serviceProvider);
    #endregion

    #region Read-only Repositories
    // Agregar propiedades de repositorios de solo lectura (DAOs)
    // Ejemplo:
    // public IUserDaoRepository UserDaos => new NHUserDaoRepository(_session);
    #endregion

    /// <summary>
    /// Constructor for NHUnitOfWork
    /// </summary>
    /// <param name="session">The NHibernate session</param>
    /// <param name="serviceProvider">The service provider for resolving validators</param>
    public NHUnitOfWork(ISession session, IServiceProvider serviceProvider)
    {
        _session = session;
        _serviceProvider = serviceProvider;
    }


    /// <summary>
    /// Begin transaction
    /// </summary>
    public void BeginTransaction()
    {
        this._transaction = this._session.BeginTransaction();
    }

    /// <summary>
    /// Execute commit
    /// </summary>
    /// <exception cref="TransactionException"></exception>
    public void Commit()
    {
        if (_transaction != null && _transaction.IsActive)
            _transaction.Commit();
        else
            throw new TransactionException("The actual transaction is not longer active");
    }

    /// <summary>
    /// Determine if there is an active transaction
    /// </summary>
    /// <returns></returns>
    public bool IsActiveTransaction()
    => _transaction != null && _transaction.IsActive;


    /// <summary>
    /// Reset the current transaction
    /// </summary>
    public void ResetTransaction()
    => _transaction = _session.BeginTransaction();

    /// <summary>
    /// Execute rollback
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void Rollback()
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
        }
        else
            throw new ArgumentNullException($"No active exception found for session {_session.Connection.ConnectionString}");
    }

    /// <summary>
    /// Dispose the current session
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (this._transaction != null)
                    this._transaction.Dispose();
                this._session.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Dispose the current session
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NHUnitOfWork()
    {
        Dispose(false);
    }
}
