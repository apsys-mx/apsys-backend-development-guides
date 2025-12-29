using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using System.Reflection;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.common.tests;
using {ProjectName}.infrastructure.nhibernate;
using {ProjectName}.ndbunit;

namespace {ProjectName}.scenarios;

public class ScenarioBuilder
{
    public IList<IScenario> Scenarios { get; private set; }
    protected internal ServiceProvider _serviceProvider;
    protected internal NHSessionFactory sessionFactory;
    protected internal INDbUnit NDbUnitTest;

    /// <summary>
    /// Constructor
    /// </summary>
    public ScenarioBuilder(string connectionString)
    {
        var assemblies = new List<Assembly> { typeof(IScenario).Assembly }.ToArray();

        // Create the NDbUnit instance
        var schema = new AppSchema();
        this.NDbUnitTest = new PostgreSQLNDbUnit(schema, connectionString);

        // Create the NHibernate session
        this.sessionFactory = new NHSessionFactory(connectionString);
        var nhSessionFactory = this.sessionFactory.BuildNHibernateSessionFactory();
        nhSessionFactory.OpenSession();

        _serviceProvider = new ServiceCollection()
            .Scan(scan => scan
                .FromAssemblyOf<Sc010CreateSandBox>()
                .AddClasses(classes => classes.AssignableTo<IScenario>())
                .AsSelf()
                .WithScopedLifetime()
        )
        .AddLogging()
        .AddScoped<IUnitOfWork, NHUnitOfWork>()
        .AddScoped<ISession>(session => nhSessionFactory.OpenSession())
        .AddSingleton<INDbUnit>(NDbUnitTest)
        // TODO: Register validators for each entity
        // .AddTransient<AbstractValidator<User>, UserValidator>()
        .BuildServiceProvider();

        this.Scenarios = ReadAllScenariosFromAssemblies(assemblies.ToList());
    }

    /// <summary>
    /// Load a preload scenario from XML file
    /// </summary>
    public void LoadXmlFile(Type preloadScenario, string outputFile)
    {
        IScenario? preloadScenarioInstance = this.Scenarios.FirstOrDefault(s => s.GetType() == preloadScenario);
        if (preloadScenarioInstance == null)
            throw new TypeLoadException($"Preload scenario {preloadScenario.Name} not found");

        var fileName = preloadScenarioInstance.ScenarioFileName;
        var fileNameWithExtension = fileName.ToLower().EndsWith(".xml") ? fileName : $"{fileName}.xml";
        var fullFilePath = Path.Combine(outputFile, fileNameWithExtension);
        if (!File.Exists(fullFilePath))
            throw new FileNotFoundException($"File {fullFilePath} not found");

        var dataSet = new AppSchema();
        dataSet.ReadXml(fullFilePath);
        this.NDbUnitTest.SeedDatabase(dataSet);
    }

    private IList<IScenario> ReadAllScenariosFromAssemblies(List<Assembly> assemblies)
    {
        var allScenarios = new List<IScenario>();
        foreach (Assembly assembly in assemblies)
        {
            try
            {
                var scenarioType = typeof(IScenario);
                var scenariosTypes = assembly
                    .GetTypes()
                    .Where(p => scenarioType.IsAssignableFrom(p));
                foreach (var scenario in scenariosTypes)
                    if (this._serviceProvider.GetService(scenario) is IScenario scenarioFound)
                        allScenarios.Add(scenarioFound);
            }
            catch (Exception ex)
            {
                throw new TypeLoadException($"Error loading scenario from assembly {assembly.FullName}", ex);
            }
        }
        return allScenarios;
    }
}
