using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using System.Configuration;
using DotNetEnv;
using MiProyecto.ndbunit;
using MiProyecto.common.tests;
using MiProyecto.domain.entities;
using FluentValidation;
using MiProyecto.domain.entities.validators;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Base infrastructure for NHibernate repository tests.
/// Contains shared setup, teardown, and utility methods.
/// </summary>
public abstract class NHRepositoryTestInfrastructureBase
{
    protected internal ISessionFactory _sessionFactory;
    protected internal IConfiguration configuration;
    protected internal INDbUnit nDbUnitTest;
    protected internal IFixture fixture;
    protected internal ServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Env.Load();
        string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        this.configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", true)
            .Build();
        string connectionStringValue = ConnectionStringBuilder.BuildPostgresConnectionString();
        var nHSessionFactory = new NHSessionFactory(connectionStringValue);
        this._sessionFactory = nHSessionFactory.BuildNHibernateSessionFactory();
        this.fixture = new Fixture().Customize(new AutoMoqCustomization());
        this.fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => this.fixture.Behaviors.Remove(b));
        this.fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        var schema = new AppSchema();
        this.nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionStringValue);
        var services = new ServiceCollection();
        LoadValidators(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void LoadValidators(ServiceCollection services)
    {
        // Registrar validators para cada entidad del dominio
        // Ejemplo:
        // services.AddTransient<AbstractValidator<Role>, RoleValidator>();
        // services.AddTransient<AbstractValidator<User>, UserValidator>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        this._sessionFactory.Dispose();
        this._serviceProvider.Dispose();
    }

    /// <summary>
    /// Load an scenario by name from the scenarios folder
    /// </summary>
    /// <param name="scenarioName">Name of the scenario file (without .xml extension)</param>
    protected internal void LoadScenario(string scenarioName)
    {
        var scenariosFolderPath = Environment.GetEnvironmentVariable("SCENARIOS_FOLDER_PATH");
        if (string.IsNullOrEmpty(scenariosFolderPath))
            throw new ConfigurationErrorsException("No [SCENARIOS_FOLDER_PATH] value found in the .env file");
        var xmlFilePath = Path.Combine(scenariosFolderPath, $"{scenarioName}.xml");
        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException($"No scenario file build found in [{xmlFilePath}]");
        this.nDbUnitTest.ClearDatabase();
        var schema = new AppSchema();
        schema.ReadXml(xmlFilePath);
        this.nDbUnitTest.SeedDatabase(schema);
    }

    public static string GetConnectionStringValue(string connectionStringName)
    {
        var connectionStringValue = Environment.GetEnvironmentVariable(connectionStringName);
        if (string.IsNullOrEmpty(connectionStringValue))
            throw new ConfigurationErrorsException($"No [{connectionStringName}] connection string value found in the system environment variables");
        return connectionStringValue;
    }
}
