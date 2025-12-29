using AutoFixture;
using AutoFixture.AutoMoq;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Configuration;
using {ProjectName}.ndbunit;
using {ProjectName}.infrastructure.nhibernate;
using {ProjectName}.common.tests;

namespace {ProjectName}.webapi.tests;

/// <summary>
/// Base class for endpoint tests.
/// </summary>
public abstract class EndpointTestBase
{
    protected internal INDbUnit nDbUnitTest;
    protected internal IConfiguration configuration;
    protected internal IFixture fixture;

    /// <summary>
    /// Gets or sets the HttpClient used for testing endpoints.
    /// </summary>
    protected internal HttpClient httpClient = null!;

    /// <summary>
    /// One-time setup for all tests.
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Env.Load();

        string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        this.configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", true)
            .Build();

        // Configurar AutoFixture para omitir referencias circulares
        this.fixture = new Fixture()
            .Customize(new AutoMoqCustomization());

        this.fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => this.fixture.Behaviors.Remove(b));
        this.fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    /// <summary>
    /// TearDown method to clean up after tests
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (this.httpClient != null)
            this.httpClient.Dispose();
    }

    /// <summary>
    /// Setup for each test
    /// </summary>
    [SetUp]
    public void Setup()
    {
        string connectionStringValue = ConnectionStringBuilder.BuildPostgresConnectionString();

        if (string.IsNullOrEmpty(connectionStringValue))
            throw new ConfigurationErrorsException("Could not build connection string from environment variables");
        var schema = new AppSchema();

        this.nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionStringValue);
        this.nDbUnitTest.ClearDatabase();
    }

    /// <summary>
    /// Creates a new HttpClient with the specified authorized user name
    /// </summary>
    protected static internal HttpClient CreateClient(string authorizedUserName, Action<IServiceCollection>? configureServices = null)
    {
        var factory = new CustomWebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(defaultScheme: "TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options =>
                        {
                            options.ClaimsIssuer = authorizedUserName;
                        });

                    configureServices?.Invoke(services);
                });
            });
        return factory.CreateClient();
    }

    /// <summary>
    /// Creates a new HttpClient without authentication
    /// </summary>
    protected static internal HttpClient CreateClient(Action<IServiceCollection>? configureServices = null)
    {
        if (configureServices == null)
            return new CustomWebApplicationFactory<Program>().CreateClient();

        var factory = new CustomWebApplicationFactory<Program>()
           .WithWebHostBuilder(builder =>
           {
               builder.ConfigureTestServices(services =>
               {
                   configureServices(services);
               });
           });
        return factory.CreateClient();
    }

    /// <summary>
    /// Load a scenario from the specified file path.
    /// </summary>
    protected internal void LoadScenario(string scenarioName)
    {
        var scenariosFolderPath = Environment.GetEnvironmentVariable("SCENARIOS_FOLDER_PATH");
        if (string.IsNullOrEmpty(scenariosFolderPath))
            throw new ConfigurationErrorsException("No [SCENARIOS_FOLDER_PATH] environment variable found");

        var scenarioFilePath = Path.Combine(scenariosFolderPath, $"{scenarioName}.xml");
        if (!File.Exists(scenarioFilePath))
            throw new FileNotFoundException($"Scenario file not found: {scenarioFilePath}");

        this.nDbUnitTest.ClearDatabase();
        var schema = new AppSchema();
        schema.ReadXml(scenarioFilePath);
        this.nDbUnitTest.SeedDatabase(schema);
    }

    /// <summary>
    /// Builds a StringContent from an object serialized to JSON.
    /// </summary>
    protected internal static HttpContent BuildHttpStringContent(object content)
    {
        var json = JsonConvert.SerializeObject(content);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}
