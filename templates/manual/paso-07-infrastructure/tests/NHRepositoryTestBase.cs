using System.Configuration;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Base class for NHibernate repository tests. Inherits infrastructure setup from NHRepositoryTestInfrastructureBase.
/// </summary>
/// <typeparam name="TRepo">The repository type being tested</typeparam>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
public abstract class NHRepositoryTestBase<TRepo, T, TKey> : NHRepositoryTestInfrastructureBase
    where T : class, new()
    where TRepo : NHRepository<T, TKey>
{

    /// <summary>
    /// Repository instance under test.
    /// This property is initialized in the Setup method and should be used in test methods.
    /// </summary>
    public TRepo RepositoryUnderTest { get; protected set; }

    /// <summary>
    /// Setup method for each test.
    /// This method initializes the repository under test and clears the database using NDbUnit.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        this.RepositoryUnderTest = this.BuildRepository();
        this.nDbUnitTest.ClearDatabase();
    }

    /// <summary>
    /// Builds the repository instance for testing.
    /// This method should be implemented in derived classes to return the specific repository instance.
    /// </summary>
    /// <returns>The repository instance to test</returns>
    abstract protected internal TRepo BuildRepository();
}
