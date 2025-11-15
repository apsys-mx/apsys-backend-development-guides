# Integration Testing with Scenarios - Step by Step

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-15

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Prerequisites](#prerequisites)
3. [Fase 1: Setup de la Infraestructura](#fase-1-setup-de-la-infraestructura)
4. [Fase 2: Implementación de NDbUnit](#fase-2-implementación-de-ndbunit)
5. [Fase 3: ScenarioBuilder](#fase-3-scenariobuilder)
6. [Fase 4: Base Classes para Tests](#fase-4-base-classes-para-tests)
7. [Fase 5: Crear Scenarios](#fase-5-crear-scenarios)
8. [Fase 6: Generar XML Files](#fase-6-generar-xml-files)
9. [Fase 7: Escribir Tests](#fase-7-escribir-tests)
10. [Verificación y Testing](#verificación-y-testing)
11. [Errores Comunes y Soluciones](#errores-comunes-y-soluciones)

---

## Introducción

Esta guía step-by-step te llevará desde cero hasta un sistema completo de scenarios para integration testing.

**Objetivo**: Implementar el Scenarios System para crear snapshots XML de la base de datos y usarlos en tests de integración.

**Resultado**:
- Infrastructure completa de NDbUnit
- ScenarioBuilder para generar XML
- Base classes para repository y endpoint tests
- Scenarios reutilizables
- Tests de integración usando scenarios

---

## Prerequisites

### Software Requirements

- ✅ .NET 9.0 SDK instalado
- ✅ PostgreSQL 16+ instalado y corriendo
- ✅ IDE (Visual Studio 2022, Rider, o VS Code)
- ✅ Git para version control

### Project Structure

```
solution/
├── src/
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── WebApi/
└── tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    ├── Infrastructure.Tests/
    ├── WebApi.Tests/
    ├── {ProjectName}.common.tests/         # ← We'll create this
    └── {ProjectName}.scenarios/            # ← We'll create this
```

### Packages que necesitaremos

| Package | Version | Purpose |
|---------|---------|---------|
| NUnit | 4.2.2 | Test framework |
| FluentAssertions | 8.5.0 | Assertions |
| Npgsql | 8.0.5 | PostgreSQL provider |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | DI container |
| Scrutor | 5.0.1 | Assembly scanning |
| DotNetEnv | 3.1.1 | Environment variables |

---

## Fase 1: Setup de la Infraestructura

### Paso 1.1: Crear proyecto {ProjectName}.common.tests

**Purpose**: Proyecto compartido para AppSchema y utilities de testing.

```bash
cd tests

# Create class library project
dotnet new classlib -n MyProject.common.tests -f net9.0

# Add to solution
dotnet sln add tests/MyProject.common.tests/MyProject.common.tests.csproj
```

### Paso 1.2: Add NuGet Packages

```bash
cd tests/MyProject.common.tests

dotnet add package Npgsql --version 8.0.5
dotnet add package System.Data.Common --version 9.0.0
```

**Archivo**: `MyProject.common.tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Npgsql" Version="8.0.5" />
    <PackageReference Include="System.Data.Common" Version="9.0.0" />
  </ItemGroup>

</Project>
```

### Paso 1.3: Crear AppSchema.xsd

**Purpose**: Definir estructura de tablas como Typed DataSet.

**Archivo**: `tests/MyProject.common.tests/AppSchema.xsd`

```xml
<?xml version="1.0" encoding="utf-8"?>
<xs:schema id="AppSchema"
           targetNamespace="http://tempuri.org/AppSchema.xsd"
           xmlns:mstns="http://tempuri.org/AppSchema.xsd"
           xmlns="http://tempuri.org/AppSchema.xsd"
           xmlns:xs="http://www.w3.org/2001/XMLSchema"
           xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
           attributeFormDefault="qualified"
           elementFormDefault="qualified">

  <xs:element name="AppSchema" msdata:IsDataSet="true" msdata:UseCurrentLocale="true">
    <xs:complexType>
      <xs:choice minOccurs="0" maxOccurs="unbounded">

        <!-- Roles Table -->
        <xs:element name="public.roles">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="id" type="xs:string" minOccurs="0" />
              <xs:element name="name" type="xs:string" minOccurs="0" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>

        <!-- Users Table -->
        <xs:element name="public.users">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="id" type="xs:string" minOccurs="0" />
              <xs:element name="email" type="xs:string" minOccurs="0" />
              <xs:element name="name" type="xs:string" minOccurs="0" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>

        <!-- User-Role Junction Table -->
        <xs:element name="public.user_in_roles">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="user_id" type="xs:string" minOccurs="0" />
              <xs:element name="role_id" type="xs:string" minOccurs="0" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>

      </xs:choice>
    </xs:complexType>
  </xs:element>

</xs:schema>
```

**⚠️ IMPORTANTE**:
- Table names MUST match exactly: `public.{table_name}`
- Column names MUST match database exactly
- GUIDs use `type="xs:string"`
- DateTimes use `type="xs:dateTime"`

### Paso 1.4: Generate Typed DataSet

**Visual Studio**:
1. Right-click `AppSchema.xsd`
2. Properties → Custom Tool → Set to `MSDataSetGenerator`
3. Save (auto-generates `AppSchema.Designer.cs`)

**Rider**:
1. Right-click `AppSchema.xsd`
2. Properties → Custom Tool → `MSDataSetGenerator`
3. Right-click → Generate Code

**Command Line**:
```bash
# Usar DataSetGenerator de .NET SDK
xsd AppSchema.xsd /dataset /language:CS /namespace:MyProject.common.tests
```

**Verificación**: Debe aparecer `AppSchema.Designer.cs` con clase `AppSchema : DataSet`.

### Paso 1.5: Crear AppSchemaExtender.cs

**Purpose**: Helper methods para acceso conveniente a datos del schema.

**Archivo**: `tests/MyProject.common.tests/AppSchemaExtender.cs`

```csharp
namespace MyProject.common.tests;

using System.Data;
using System.Linq;

/// <summary>
/// Extension methods for convenient access to AppSchema tables and rows
/// </summary>
public static class AppSchemaExtender
{
    // =====================================================
    // Table Name Constants
    // =====================================================

    public static readonly string FullRolesTableName = "public.roles";
    public static readonly string FullUsersTableName = "public.users";
    public static readonly string FullUserInRolesTableName = "public.user_in_roles";

    // =====================================================
    // Get Table Methods
    // =====================================================

    /// <summary>
    /// Gets the roles table from the DataSet
    /// </summary>
    public static DataTable? GetRolesTable(this DataSet appSchema)
        => appSchema.Tables[FullRolesTableName];

    /// <summary>
    /// Gets the users table from the DataSet
    /// </summary>
    public static DataTable? GetUsersTable(this DataSet appSchema)
        => appSchema.Tables[FullUsersTableName];

    /// <summary>
    /// Gets the user_in_roles junction table from the DataSet
    /// </summary>
    public static DataTable? GetUserInRolesTable(this DataSet appSchema)
        => appSchema.Tables[FullUserInRolesTableName];

    // =====================================================
    // Get Rows with Filtering
    // =====================================================

    /// <summary>
    /// Gets all role rows matching the filter expression
    /// </summary>
    /// <param name="appSchema">DataSet containing the schema</param>
    /// <param name="filterExpression">Filter expression (e.g., "name = 'Admin'")</param>
    public static IEnumerable<DataRow> GetRolesRows(this DataSet appSchema, string filterExpression)
        => GetRolesTable(appSchema)?.Select(filterExpression).AsEnumerable()
           ?? Enumerable.Empty<DataRow>();

    /// <summary>
    /// Gets all user rows matching the filter expression
    /// </summary>
    public static IEnumerable<DataRow> GetUsersRows(this DataSet appSchema, string filterExpression)
        => GetUsersTable(appSchema)?.Select(filterExpression).AsEnumerable()
           ?? Enumerable.Empty<DataRow>();

    /// <summary>
    /// Gets all user_in_roles rows matching the filter expression
    /// </summary>
    public static IEnumerable<DataRow> GetUserInRolesRows(this DataSet appSchema, string filterExpression)
        => GetUserInRolesTable(appSchema)?.Select(filterExpression).AsEnumerable()
           ?? Enumerable.Empty<DataRow>();

    // =====================================================
    // Get Single Row Helpers
    // =====================================================

    /// <summary>
    /// Gets the first user row from the users table
    /// </summary>
    public static DataRow? GetFirstUserRow(this DataSet appSchema)
        => GetUsersTable(appSchema)?.AsEnumerable().FirstOrDefault();

    /// <summary>
    /// Gets the first role row from the roles table
    /// </summary>
    public static DataRow? GetFirstRoleRow(this DataSet appSchema)
        => GetRolesTable(appSchema)?.AsEnumerable().FirstOrDefault();

    /// <summary>
    /// Gets a user row by email
    /// </summary>
    public static DataRow? GetUserRowByEmail(this DataSet appSchema, string email)
        => GetUsersRows(appSchema, $"email = '{email}'").FirstOrDefault();

    /// <summary>
    /// Gets a role row by name
    /// </summary>
    public static DataRow? GetRoleRowByName(this DataSet appSchema, string roleName)
        => GetRolesRows(appSchema, $"name = '{roleName}'").FirstOrDefault();
}
```

**Usage Example**:
```csharp
var dataSet = nDbUnit.GetDataSetFromDb();

// Get first user email
var firstUser = dataSet.GetFirstUserRow();
string email = firstUser!["email"].ToString();

// Get user by email
var user = dataSet.GetUserRowByEmail("usuario1@example.com");

// Get all admin roles
var adminRoles = dataSet.GetRolesRows("name LIKE '%Admin%'");
```

---

## Fase 2: Implementación de NDbUnit

### Paso 2.1: Crear INDbUnit Interface

**Archivo**: `tests/MyProject.common.tests/ndbunit/INDbUnit.cs`

```csharp
namespace MyProject.common.tests.ndbunit;

using System.Data;
using System.Data.Common;

/// <summary>
/// Interface for database seeding and clearing operations
/// </summary>
public interface INDbUnit
{
    /// <summary>
    /// Gets the DataSet schema used for database operations
    /// </summary>
    DataSet DataSet { get; }

    /// <summary>
    /// Gets the connection string to the database
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Reads the complete database state into a DataSet
    /// </summary>
    /// <returns>DataSet containing all data from database tables</returns>
    DataSet GetDataSetFromDb();

    /// <summary>
    /// Creates a DbDataAdapter for the current database provider
    /// </summary>
    DbDataAdapter CreateDataAdapter();

    /// <summary>
    /// Clears all data from database tables defined in the schema
    /// </summary>
    void ClearDatabase();

    /// <summary>
    /// Seeds the database with data from a DataSet
    /// </summary>
    /// <param name="dataSet">DataSet containing data to insert</param>
    void SeedDatabase(DataSet dataSet);
}
```

### Paso 2.2: Crear NDbUnit Abstract Base Class

**Archivo**: `tests/MyProject.common.tests/ndbunit/NDbUnit.cs`

```csharp
namespace MyProject.common.tests.ndbunit;

using System.Data;
using System.Data.Common;

/// <summary>
/// Abstract base class for database unit testing with data seeding capabilities
/// </summary>
public abstract class NDbUnit : INDbUnit
{
    protected NDbUnit(DataSet dataSet, string connectionString)
    {
        DataSet = dataSet;
        ConnectionString = connectionString;
    }

    public DataSet DataSet { get; set; }

    public string ConnectionString { get; }

    // =====================================================
    // Abstract Methods (Provider-Specific)
    // =====================================================

    /// <summary>
    /// Creates a database connection for the specific provider
    /// </summary>
    public abstract DbConnection CreateConnection();

    /// <summary>
    /// Creates a data adapter for the specific provider
    /// </summary>
    public abstract DbDataAdapter CreateDataAdapter();

    /// <summary>
    /// Creates a command builder for the specific provider
    /// </summary>
    public abstract DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter);

    /// <summary>
    /// Disables table constraints (FK, triggers) for safe data manipulation
    /// </summary>
    protected abstract void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);

    /// <summary>
    /// Re-enables table constraints after data manipulation
    /// </summary>
    protected abstract void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);

    // =====================================================
    // GetDataSetFromDb
    // =====================================================

    /// <summary>
    /// Reads the complete database state into a DataSet
    /// </summary>
    public DataSet GetDataSetFromDb()
    {
        using DbConnection cnn = this.CreateConnection();
        DataSet dsetResult = this.DataSet.Clone();  // Clone structure
        dsetResult.EnforceConstraints = false;       // Disable FK validation temporarily

        DbProviderFactory? dbFactory = DbProviderFactories.GetFactory(cnn);

        // Read each table defined in AppSchema.xsd
        foreach (DataTable table in this.DataSet.Tables)
        {
            DbCommand selectCommand = cnn.CreateCommand();
            selectCommand.CommandText = $"SELECT * FROM {table.TableName}";

            DbDataAdapter? adapter = dbFactory.CreateDataAdapter();
            if (adapter == null)
                throw new InvalidOperationException($"Failed to create adapter for {cnn.GetType().Name}");

            adapter.SelectCommand = selectCommand;
            adapter.Fill(dsetResult, table.TableName);
        }

        dsetResult.EnforceConstraints = true;
        return dsetResult;
    }

    // =====================================================
    // ClearDatabase
    // =====================================================

    /// <summary>
    /// Clears all data from database tables in a transactional manner
    /// </summary>
    public void ClearDatabase()
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();

        using IDbTransaction transaction = cnn.BeginTransaction();
        try
        {
            // 1. Disable all constraints (FK, triggers)
            foreach (DataTable dataTable in this.DataSet.Tables)
                this.DisableTableConstraints(transaction, dataTable);

            // 2. Delete all data from each table
            foreach (DataTable dataTable in this.DataSet.Tables)
            {
                var cmd = cnn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"DELETE FROM {dataTable.TableName}";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            // 3. Re-enable constraints
            foreach (DataTable dataTable in this.DataSet.Tables)
                this.EnabledTableConstraints(transaction, dataTable);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // =====================================================
    // SeedDatabase
    // =====================================================

    /// <summary>
    /// Seeds the database with data from a DataSet in a transactional manner
    /// </summary>
    public void SeedDatabase(DataSet dataSet)
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();
        this.DataSet = dataSet;

        using (IDbTransaction transaction = cnn.BeginTransaction())
        {
            try
            {
                // 1. Disable constraints
                foreach (DataTable dataTable in this.DataSet.Tables)
                    this.DisableTableConstraints(transaction, dataTable);

                // 2. Insert data using DbDataAdapter
                foreach (DataTable dataTable in this.DataSet.Tables)
                {
                    var selectCommand = cnn.CreateCommand();
                    selectCommand.CommandText = $"SELECT * FROM {dataTable.TableName}";
                    selectCommand.Transaction = transaction;

                    var adapter = this.CreateDataAdapter();
                    adapter.SelectCommand = selectCommand as DbCommand;

                    var commandBuilder = this.CreateCommandBuilder(adapter);
                    adapter.InsertCommand = commandBuilder.GetInsertCommand();
                    adapter.InsertCommand.Transaction = transaction as DbTransaction;

                    adapter.Update(dataTable);  // ✅ Generates and executes INSERTs
                }

                // 3. Re-enable constraints
                foreach (DataTable dataTable in this.DataSet.Tables)
                    this.EnabledTableConstraints(transaction, dataTable);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
```

### Paso 2.3: Crear PostgreSQLNDbUnit

**Archivo**: `tests/MyProject.common.tests/ndbunit/PostgreSQLNDbUnit.cs`

```csharp
namespace MyProject.common.tests.ndbunit;

using System.Data;
using System.Data.Common;
using Npgsql;

/// <summary>
/// PostgreSQL-specific implementation of NDbUnit
/// </summary>
public class PostgreSQLNDbUnit : NDbUnit
{
    public PostgreSQLNDbUnit(DataSet dataSet, string connectionString)
        : base(dataSet, connectionString)
    {
    }

    public override DbConnection CreateConnection()
        => new NpgsqlConnection(ConnectionString);

    public override DbDataAdapter CreateDataAdapter()
        => new NpgsqlDataAdapter();

    public override DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter)
        => new NpgsqlCommandBuilder((NpgsqlDataAdapter)dataAdapter);

    /// <summary>
    /// Disables all triggers on the table (includes FK constraints)
    /// PostgreSQL-specific: DISABLE TRIGGER ALL
    /// </summary>
    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection!.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} DISABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Re-enables all triggers on the table
    /// PostgreSQL-specific: ENABLE TRIGGER ALL
    /// </summary>
    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection!.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} ENABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }
}
```

**⚠️ PostgreSQL-Specific Notes**:
- `DISABLE TRIGGER ALL` - Deshabilita FK constraints + custom triggers
- `ENABLE TRIGGER ALL` - Re-habilita todo después del seeding
- Requiere permisos de superuser o table owner

**Para SQL Server**: Crear `SqlServerNDbUnit` con:
```csharp
protected override void DisableTableConstraints(...)
{
    command.CommandText = $"ALTER TABLE {dataTable.TableName} NOCHECK CONSTRAINT ALL";
}

protected override void EnabledTableConstraints(...)
{
    command.CommandText = $"ALTER TABLE {dataTable.TableName} CHECK CONSTRAINT ALL";
}
```

---

## Fase 3: ScenarioBuilder

### Paso 3.1: Crear proyecto {ProjectName}.scenarios

```bash
cd tests

# Create console application
dotnet new console -n MyProject.scenarios -f net9.0

# Add to solution
dotnet sln add tests/MyProject.scenarios/MyProject.scenarios.csproj
```

### Paso 3.2: Add NuGet Packages y Project References

```bash
cd tests/MyProject.scenarios

# Add packages
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0
dotnet add package Scrutor --version 5.0.1
dotnet add package DotNetEnv --version 3.1.1
dotnet add package Npgsql --version 8.0.5

# Add project references
dotnet add reference ../MyProject.common.tests/MyProject.common.tests.csproj
dotnet add reference ../../src/Domain/Domain.csproj
dotnet add reference ../../src/Application/Application.csproj
dotnet add reference ../../src/Infrastructure/Infrastructure.csproj
```

### Paso 3.3: Crear IScenario Interface

**Archivo**: `tests/MyProject.scenarios/IScenario.cs`

```csharp
namespace MyProject.scenarios;

/// <summary>
/// Defines the operations to seed the database with the data of the scenario
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Execute the operations to seed the database
    /// </summary>
    Task SeedData();

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    string ScenarioFileName { get; }

    /// <summary>
    /// If defined, the scenario will be pre-loaded before the current scenario
    /// </summary>
    Type? PreloadScenario { get; }
}
```

### Paso 3.4: Crear ScenarioBuilder

**Archivo**: `tests/MyProject.scenarios/ScenarioBuilder.cs`

```csharp
namespace MyProject.scenarios;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MyProject.common.tests;
using MyProject.common.tests.ndbunit;
using MyProject.scenarios.scenarios;  // Where scenarios will be

/// <summary>
/// Builder class for managing scenario generation and execution
/// </summary>
public class ScenarioBuilder
{
    private readonly ServiceProvider _serviceProvider;

    public INDbUnit NDbUnitTest { get; }
    public List<IScenario> Scenarios { get; private set; }

    public ScenarioBuilder(string connectionString)
    {
        // 1. Create NDbUnit with AppSchema
        var schema = new AppSchema();
        NDbUnitTest = new PostgreSQLNDbUnit(schema, connectionString);

        // 2. Configure DI container
        _serviceProvider = ConfigureServices(connectionString);

        // 3. Load all scenarios from assembly
        var assemblies = new[] { typeof(Sc010CreateSandBox).Assembly };
        Scenarios = ReadAllScenariosFromAssemblies(assemblies);
    }

    private ServiceProvider ConfigureServices(string connectionString)
    {
        var services = new ServiceCollection();

        // Register scenarios
        services.Scan(scan => scan
            .FromAssemblyOf<Sc010CreateSandBox>()
            .AddClasses(classes => classes.AssignableTo<IScenario>())
            .AsSelf()
            .WithScopedLifetime()
        );

        // Register infrastructure dependencies
        // Example: NHibernate session, repositories, validators, etc.
        // services.AddScoped<IUnitOfWork, NHUnitOfWork>();
        // services.AddScoped<ISession>(sp => sessionFactory.OpenSession());
        // ... add your dependencies

        services.AddSingleton<INDbUnit>(NDbUnitTest);

        return services.BuildServiceProvider();
    }

    private List<IScenario> ReadAllScenariosFromAssemblies(Assembly[] assemblies)
    {
        var scenarios = new List<IScenario>();

        foreach (var assembly in assemblies)
        {
            var scenarioTypes = assembly.GetTypes()
                .Where(t => typeof(IScenario).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var scenarioType in scenarioTypes)
            {
                var scenario = _serviceProvider.GetRequiredService(scenarioType) as IScenario;
                if (scenario != null)
                    scenarios.Add(scenario);
            }
        }

        return scenarios.OrderBy(s => s.GetType().Name).ToList();
    }

    /// <summary>
    /// Loads a preload scenario XML file into the database
    /// </summary>
    public void LoadXmlFile(Type preloadScenario, string outputPath)
    {
        // 1. Resolve scenario instance
        IScenario? preloadScenarioInstance = Scenarios.FirstOrDefault(s => s.GetType() == preloadScenario);
        if (preloadScenarioInstance == null)
            throw new TypeLoadException($"Preload scenario {preloadScenario.Name} not found");

        // 2. Build file path
        var fileName = preloadScenarioInstance.ScenarioFileName;
        var fileNameWithExtension = fileName.ToLower().EndsWith(".xml") ? fileName : $"{fileName}.xml";
        var fullFilePath = Path.Combine(outputPath, fileNameWithExtension);

        if (!File.Exists(fullFilePath))
            throw new FileNotFoundException($"File {fullFilePath} not found");

        // 3. Load XML into DataSet
        var dataSet = new AppSchema();
        dataSet.ReadXml(fullFilePath);

        // 4. Seed database with XML data
        NDbUnitTest.SeedDatabase(dataSet);
    }
}
```

### Paso 3.5: Crear Program.cs

**Archivo**: `tests/MyProject.scenarios/Program.cs`

```csharp
using System.Data;
using MyProject.scenarios;
using DotNetEnv;

// Load environment variables
Env.Load();

Console.WriteLine("Reading command line parameters...");

// Parse command-line arguments
var args = Environment.GetCommandLineArgs();
string? connectionStringValue = args.FirstOrDefault(arg => arg.StartsWith("/cnn:"))?[5..];
string? outputValue = args.FirstOrDefault(arg => arg.StartsWith("/output:"))?[8..];

if (string.IsNullOrEmpty(connectionStringValue))
{
    Console.WriteLine("ERROR: Connection string parameter (/cnn) is required");
    Console.WriteLine("Usage: dotnet run /cnn:\"Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=root\" /output:\"D:\\scenarios\"");
    return;
}

if (string.IsNullOrEmpty(outputValue))
{
    Console.WriteLine("ERROR: Output path parameter (/output) is required");
    return;
}

// Create output directory if doesn't exist
if (!Directory.Exists(outputValue))
{
    Console.WriteLine($"Creating output directory: {outputValue}");
    Directory.CreateDirectory(outputValue);
}

Console.WriteLine($"Connection string: {connectionStringValue}");
Console.WriteLine($"Output path: {outputValue}");
Console.WriteLine();

// Build scenario builder
Console.WriteLine("LOG: Loading scenarios...");
var builder = new ScenarioBuilder(connectionStringValue);

Console.WriteLine($"LOG: Found {builder.Scenarios.Count} scenarios");
foreach (var scenario in builder.Scenarios)
{
    Console.WriteLine($"  - {scenario.GetType().Name} ({scenario.ScenarioFileName})");
}
Console.WriteLine();

// Generate XML for each scenario
foreach (var scenario in builder.Scenarios)
{
    var scenarioName = scenario.ScenarioFileName;
    Console.WriteLine($"LOG: Creating scenario {scenarioName}...");

    try
    {
        // 1. Clear the database
        builder.NDbUnitTest.ClearDatabase();

        // 2. Load preload scenario if exists
        if (scenario.PreloadScenario != null)
        {
            Console.WriteLine($"  → Loading preload scenario: {scenario.PreloadScenario.Name}");
            builder.LoadXmlFile(scenario.PreloadScenario, outputValue);
        }

        // 3. Execute scenario's SeedData method
        Console.WriteLine($"  → Executing SeedData()...");
        await scenario.SeedData();

        // 4. Extract database state to DataSet
        Console.WriteLine($"  → Reading database state...");
        DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();

        // 5. Serialize DataSet to XML
        string filePath = Path.Combine(outputValue, $"{scenarioName}.xml");
        dataSet.WriteXml(filePath);

        Console.WriteLine($"  ✅ Generated: {filePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ❌ ERROR: {ex.Message}");
        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
    }

    Console.WriteLine();
}

Console.WriteLine("Scenario loading completed");
```

---

## Fase 4: Base Classes para Tests

### Paso 4.1: Crear NHRepositoryTestInfrastructureBase

**Purpose**: Base class con setup compartido para repository tests.

**Archivo**: `tests/Infrastructure.Tests/nhibernate/NHRepositoryTestInfrastructureBase.cs`

```csharp
namespace Infrastructure.Tests.nhibernate;

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.common.tests;
using MyProject.common.tests.ndbunit;
using NHibernate;
using NUnit.Framework;

/// <summary>
/// Base infrastructure for NHibernate repository testing
/// Provides setup for NHibernate session factory, NDbUnit, and AutoFixture
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
        // 1. Load environment variables from .env
        Env.Load();

        // 2. Configure appsettings
        string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        // 3. Build connection string
        string connectionStringValue = BuildConnectionString();

        // 4. Create NHibernate session factory
        // Replace with your NHibernate configuration
        // var nHSessionFactory = new NHSessionFactory(connectionStringValue);
        // _sessionFactory = nHSessionFactory.BuildNHibernateSessionFactory();

        // 5. Configure AutoFixture
        fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // 6. Create NDbUnit
        var schema = new AppSchema();
        nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionStringValue);

        // 7. Register validators and services
        var services = new ServiceCollection();
        LoadValidators(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _sessionFactory?.Dispose();
        _serviceProvider?.Dispose();
    }

    /// <summary>
    /// Loads a scenario XML file into the database
    /// </summary>
    protected internal void LoadScenario(string scenarioName)
    {
        var scenariosFolderPath = Environment.GetEnvironmentVariable("SCENARIOS_FOLDER_PATH");

        if (string.IsNullOrEmpty(scenariosFolderPath))
            throw new ConfigurationErrorsException(
                "No [SCENARIOS_FOLDER_PATH] value found in the .env file");

        var xmlFilePath = Path.Combine(scenariosFolderPath, $"{scenarioName}.xml");

        if (!File.Exists(xmlFilePath))
            throw new FileNotFoundException(
                $"No scenario file found at [{xmlFilePath}]");

        nDbUnitTest.ClearDatabase();
        var schema = new AppSchema();
        schema.ReadXml(xmlFilePath);
        nDbUnitTest.SeedDatabase(schema);
    }

    private string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "testdb";
        var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "root";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private void LoadValidators(ServiceCollection services)
    {
        // Register FluentValidation validators
        // Example:
        // services.AddTransient<AbstractValidator<Role>, RoleValidator>();
        // services.AddTransient<AbstractValidator<User>, UserValidator>();
    }
}
```

### Paso 4.2: Crear NHRepositoryTestBase

**Archivo**: `tests/Infrastructure.Tests/nhibernate/NHRepositoryTestBase.cs`

```csharp
namespace Infrastructure.Tests.nhibernate;

using NUnit.Framework;

/// <summary>
/// Base class for NHibernate repository tests
/// Provides repository instance and database cleanup
/// </summary>
/// <typeparam name="TRepo">Repository type under test</typeparam>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Entity key type</typeparam>
public abstract class NHRepositoryTestBase<TRepo, T, TKey> : NHRepositoryTestInfrastructureBase
    where T : class, new()
    where TRepo : class
{
    public TRepo RepositoryUnderTest { get; protected set; } = null!;

    /// <summary>
    /// Executed before each test
    /// Creates repository instance and clears database
    /// </summary>
    [SetUp]
    public void Setup()
    {
        RepositoryUnderTest = BuildRepository();
        nDbUnitTest.ClearDatabase();
    }

    /// <summary>
    /// Builds the repository instance under test
    /// Must be implemented by derived classes
    /// </summary>
    protected internal abstract TRepo BuildRepository();
}
```

### Paso 4.3: Crear EndpointTestBase

**Archivo**: `tests/WebApi.Tests/EndpointTestBase.cs`

```csharp
namespace WebApi.Tests;

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoMoq;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyProject.common.tests;
using MyProject.common.tests.ndbunit;
using NUnit.Framework;

/// <summary>
/// Base class for endpoint integration tests
/// Provides WebApplicationFactory and scenario loading
/// </summary>
public abstract class EndpointTestBase
{
    protected internal INDbUnit nDbUnitTest;
    protected internal IConfiguration configuration;
    protected internal IFixture fixture;
    protected internal HttpClient httpClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Env.Load();

        string? environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [SetUp]
    public void Setup()
    {
        string connectionStringValue = BuildConnectionString();
        var schema = new AppSchema();
        nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionStringValue);
        nDbUnitTest.ClearDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        httpClient?.Dispose();
    }

    /// <summary>
    /// Loads a scenario XML file into the database
    /// </summary>
    protected internal void LoadScenario(string scenarioName)
    {
        var scenariosFolderPath = Environment.GetEnvironmentVariable("SCENARIOS_FOLDER_PATH");

        if (string.IsNullOrEmpty(scenariosFolderPath))
            throw new ConfigurationErrorsException(
                "No [SCENARIOS_FOLDER_PATH] environment variable found");

        var scenarioFilePath = Path.Combine(scenariosFolderPath, $"{scenarioName}.xml");

        if (!File.Exists(scenarioFilePath))
            throw new FileNotFoundException(
                $"Scenario file not found: {scenarioFilePath}");

        nDbUnitTest.ClearDatabase();
        var schema = new AppSchema();
        schema.ReadXml(scenarioFilePath);
        nDbUnitTest.SeedDatabase(schema);
    }

    /// <summary>
    /// Creates an authenticated HttpClient for testing
    /// </summary>
    protected static internal HttpClient CreateClient(
        string authorizedUserName,
        Action<IServiceCollection>? configureServices = null)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Configure test authentication
                    services.AddAuthentication(defaultScheme: "TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "TestScheme",
                            options => { options.ClaimsIssuer = authorizedUserName; });

                    configureServices?.Invoke(services);
                });
            });

        return factory.CreateClient();
    }

    private string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "testdb";
        var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "root";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}
```

**⚠️ Note**: Necesitarás implementar `TestAuthHandler` para authentication en tests.

---

## Fase 5: Crear Scenarios

### Paso 5.1: Crear Sc010CreateSandBox (Empty Baseline)

**Archivo**: `tests/MyProject.scenarios/scenarios/Sc010CreateSandBox.cs`

```csharp
namespace MyProject.scenarios.scenarios;

/// <summary>
/// Base scenario - Creates an empty database state
/// </summary>
public class Sc010CreateSandBox : IScenario
{
    public string ScenarioFileName => "CreateSandBox";

    public Type? PreloadScenario => null;  // No dependencies

    public Task SeedData()
        => Task.CompletedTask;  // No operations, just empty DB
}
```

### Paso 5.2: Crear Sc020CreateRoles (Foundation Scenario)

**Archivo**: `tests/MyProject.scenarios/scenarios/Sc020CreateRoles.cs`

```csharp
namespace MyProject.scenarios.scenarios;

using Domain.Interfaces;

/// <summary>
/// Creates default system roles
/// </summary>
public class Sc020CreateRoles : IScenario
{
    private readonly IUnitOfWork _uoW;

    public Sc020CreateRoles(IUnitOfWork uoW)
    {
        _uoW = uoW;
    }

    public string ScenarioFileName => "CreateRoles";

    public Type? PreloadScenario => typeof(Sc010CreateSandBox);

    public Task SeedData()
    {
        // Delegate to repository method
        return _uoW.Roles.CreateDefaultRoles();
    }
}
```

### Paso 5.3: Crear Sc030CreateUsers (Bulk Creation Scenario)

**Archivo**: `tests/MyProject.scenarios/scenarios/Sc030CreateUsers.cs`

```csharp
namespace MyProject.scenarios.scenarios;

using Domain.Interfaces;

/// <summary>
/// Creates 5 test users with different characteristics
/// - usuario1: Will be admin in Sc031CreateAdminUser
/// - usuario2-3: Regular users
/// - usuario4-5: Additional test users
/// </summary>
public class Sc030CreateUsers : IScenario
{
    private readonly IUnitOfWork _uoW;

    public Sc030CreateUsers(IUnitOfWork uoW)
    {
        _uoW = uoW;
    }

    public string ScenarioFileName => "CreateUsers";

    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        // Well-known, predictable data
        var users = new List<(string Email, string Name)>
        {
            ("usuario1@example.com", "Carlos Rodríguez"),
            ("usuario2@example.com", "Ana María González"),
            ("usuario3@example.com", "José Luis Martínez"),
            ("usuario4@example.com", "María Fernanda López"),
            ("usuario5@example.com", "Juan Pablo Ramírez")
        };

        try
        {
            _uoW.BeginTransaction();

            foreach (var (email, name) in users)
            {
                await _uoW.Users.CreateAsync(email, name);
            }

            _uoW.Commit();
        }
        catch
        {
            _uoW.Rollback();
            throw;
        }
    }
}
```

### Paso 5.4: Crear Sc031CreateAdminUser (Modification Scenario)

**Archivo**: `tests/MyProject.scenarios/scenarios/Sc031CreateAdminUser.cs`

```csharp
namespace MyProject.scenarios.scenarios;

using Domain.Interfaces;
using Domain.Resources;

/// <summary>
/// Assigns PlatformAdministrator role to usuario1
/// </summary>
public class Sc031CreateAdminUser : IScenario
{
    private readonly IUnitOfWork _uoW;

    public Sc031CreateAdminUser(IUnitOfWork uoW)
    {
        _uoW = uoW;
    }

    public string ScenarioFileName => "CreateAdminUser";

    public Type? PreloadScenario => typeof(Sc030CreateUsers);

    public async Task SeedData()
    {
        try
        {
            _uoW.BeginTransaction();

            // Get user from preload scenario
            var adminUser = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
            if (adminUser == null)
                throw new InvalidOperationException(
                    "usuario1@example.com not found. " +
                    "Ensure Sc030CreateUsers was loaded.");

            // Get admin role
            var adminRole = await _uoW.Roles.GetByNameAsync(RolesResources.PlatformAdministrator);
            if (adminRole == null)
                throw new InvalidOperationException(
                    "PlatformAdministrator role not found. " +
                    "Ensure Sc020CreateRoles was loaded.");

            // Assign role if not already assigned
            if (!adminUser.Roles.Contains(adminRole))
            {
                adminUser.Roles.Add(adminRole);
                await _uoW.Users.SaveAsync(adminUser);
            }

            _uoW.Commit();
        }
        catch
        {
            _uoW.Rollback();
            throw;
        }
    }
}
```

---

## Fase 6: Generar XML Files

### Paso 6.1: Configurar Environment Variables

**Archivo**: `tests/MyProject.scenarios/.env`

```bash
# Database connection
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DATABASE=myproject_testdb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=root

# Scenarios output path
SCENARIOS_FOLDER_PATH=D:\scenarios\myproject
```

### Paso 6.2: Run Scenarios Generator

```bash
cd tests/MyProject.scenarios

# Build first
dotnet build

# Run with parameters
dotnet run \
  /cnn:"Host=localhost;Port=5432;Database=myproject_testdb;Username=postgres;Password=root" \
  /output:"D:\scenarios\myproject"
```

**Expected Output**:

```
Reading command line parameters...
Connection string: Host=localhost;Port=5432;Database=myproject_testdb;Username=postgres;Password=root
Output path: D:\scenarios\myproject

LOG: Loading scenarios...
LOG: Found 4 scenarios
  - Sc010CreateSandBox (CreateSandBox)
  - Sc020CreateRoles (CreateRoles)
  - Sc030CreateUsers (CreateUsers)
  - Sc031CreateAdminUser (CreateAdminUser)

LOG: Creating scenario CreateSandBox...
  → Executing SeedData()...
  → Reading database state...
  ✅ Generated: D:\scenarios\myproject\CreateSandBox.xml

LOG: Creating scenario CreateRoles...
  → Loading preload scenario: Sc010CreateSandBox
  → Executing SeedData()...
  → Reading database state...
  ✅ Generated: D:\scenarios\myproject\CreateRoles.xml

LOG: Creating scenario CreateUsers...
  → Loading preload scenario: Sc020CreateRoles
  → Executing SeedData()...
  → Reading database state...
  ✅ Generated: D:\scenarios\myproject\CreateUsers.xml

LOG: Creating scenario CreateAdminUser...
  → Loading preload scenario: Sc030CreateUsers
  → Executing SeedData()...
  → Reading database state...
  ✅ Generated: D:\scenarios\myproject\CreateAdminUser.xml

Scenario loading completed
```

### Paso 6.3: Verificar XML Files

**CreateSandBox.xml** (Empty):
```xml
<?xml version="1.0" standalone="yes"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd" />
```

**CreateRoles.xml** (1 Role):
```xml
<?xml version="1.0" standalone="yes"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <public.roles>
    <id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</id>
    <name>PlatformAdministrator</name>
  </public.roles>
</AppSchema>
```

**CreateUsers.xml** (1 Role + 5 Users):
```xml
<?xml version="1.0" standalone="yes"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <public.roles>
    <id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</id>
    <name>PlatformAdministrator</name>
  </public.roles>
  <public.users>
    <id>5802eee4-0b2b-40bf-854c-b64ad04094dd</id>
    <email>usuario1@example.com</email>
    <name>Carlos Rodríguez</name>
  </public.users>
  <!-- ... 4 more users -->
</AppSchema>
```

**CreateAdminUser.xml** (1 Role + 5 Users + 1 Assignment):
```xml
<?xml version="1.0" standalone="yes"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <public.roles>
    <id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</id>
    <name>PlatformAdministrator</name>
  </public.roles>
  <public.users>
    <!-- ... 5 users -->
  </public.users>
  <public.user_in_roles>
    <user_id>5802eee4-0b2b-40bf-854c-b64ad04094dd</user_id>
    <role_id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</role_id>
  </public.user_in_roles>
</AppSchema>
```

✅ **Success**: XML files generados correctamente con datos acumulados.

---

## Fase 7: Escribir Tests

### Paso 7.1: Repository Tests

**Archivo**: `tests/Infrastructure.Tests/Repositories/NHUserRepositoryTests.cs`

```csharp
namespace Infrastructure.Tests.Repositories;

using System;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Tests.nhibernate;
using NUnit.Framework;

[TestFixture]
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    [Test]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        LoadScenario("CreateUsers");  // ✅ Loads 5 users + 1 role
        const string email = "usuario1@example.com";

        // Act
        var user = await RepositoryUnderTest.GetByEmailAsync(email);

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Name.Should().Be("Carlos Rodríguez");
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        LoadScenario("CreateUsers");

        // Act
        var users = await RepositoryUnderTest.GetAllAsync();

        // Assert
        users.Should().HaveCount(5);
        users.Should().Contain(u => u.Email == "usuario1@example.com");
        users.Should().Contain(u => u.Email == "usuario5@example.com");
    }

    [Test]
    public async Task CreateAsync_WhenEmailIsDuplicated_ThrowsDuplicatedDomainException()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var existingEmail = GetFirstUserEmailFromDb();

        // Act
        Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(existingEmail!, "New User");

        // Assert
        await act.Should().ThrowAsync<DuplicatedDomainException>()
            .WithMessage($"A user with the email '{existingEmail}' already exists.");
    }

    [Test]
    public async Task SaveAsync_WhenAssigningRole_PersistsRelationship()
    {
        // Arrange
        LoadScenario("CreateAdminUser");  // ✅ Uses admin scenario
        var adminUser = await RepositoryUnderTest.GetByEmailAsync("usuario1@example.com");

        // Assert
        adminUser.Should().NotBeNull();
        adminUser!.Roles.Should().ContainSingle()
            .Which.Name.Should().Be("PlatformAdministrator");
    }

    // Helper method to access scenario data
    private string? GetFirstUserEmailFromDb()
    {
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        var userRow = dataSet.GetFirstUserRow();
        return userRow?["email"].ToString();
    }
}
```

### Paso 7.2: Endpoint Tests

**Archivo**: `tests/WebApi.Tests/Endpoints/GetManyAndCountUsersEndpointTests.cs`

```csharp
namespace WebApi.Tests.Endpoints;

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Application.DTOs;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

[TestFixture]
internal class GetManyAndCountUsersEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GET_Users_WithoutFilters_ReturnsAllUsers()
    {
        // Arrange
        LoadScenario("CreateUsers");  // ✅ Loads 5 users + 1 role
        var authenticatedUserName = "usuario1@example.com";
        httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<UserDto>>(content);

        result.Should().NotBeNull();
        result!.Count.Should().Be(5);  // ✅ We know exactly how many users
        result.Items.Should().Contain(u => u.Email == "usuario1@example.com");
    }

    [Test]
    public async Task GET_Users_WithEmailFilter_ReturnsFilteredUsers()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users?email=usuario2@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<UserDto>>(content);

        result!.Count.Should().Be(1);
        result.Items.First().Email.Should().Be("usuario2@example.com");
    }

    [Test]
    public async Task GET_Users_AsAdmin_ReturnsUsersWithRoles()
    {
        // Arrange
        LoadScenario("CreateAdminUser");  // ✅ Uses admin scenario
        var authenticatedUserName = "usuario1@example.com";
        httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<UserDto>>(content);

        result.Should().NotBeNull();
        result!.Items.Should().Contain(u =>
            u.Email == "usuario1@example.com" &&
            u.Roles.Contains("PlatformAdministrator"));
    }
}
```

---

## Verificación y Testing

### Checklist de Verificación

#### Infrastructure Setup

- [ ] `AppSchema.xsd` creado con todas las tablas
- [ ] `AppSchema.Designer.cs` auto-generado correctamente
- [ ] `AppSchemaExtender.cs` con extension methods
- [ ] `INDbUnit`, `NDbUnit`, `PostgreSQLNDbUnit` implementados
- [ ] `NHRepositoryTestInfrastructureBase` creado
- [ ] `NHRepositoryTestBase<TRepo, T, TKey>` creado
- [ ] `EndpointTestBase` creado

#### Scenarios

- [ ] `IScenario` interface creado
- [ ] `ScenarioBuilder` implementado
- [ ] `Program.cs` para generation
- [ ] `Sc010CreateSandBox` (empty baseline)
- [ ] `Sc020CreateRoles` (foundation)
- [ ] `Sc030CreateUsers` (bulk creation)
- [ ] `Sc031CreateAdminUser` (modification)

#### Generation

- [ ] `.env` file configurado con connection string
- [ ] `dotnet run` ejecuta sin errores
- [ ] XML files generados en output folder
- [ ] XML files contienen datos correctos
- [ ] Dependency chain funciona (preload scenarios)

#### Tests

- [ ] Repository tests creados usando `LoadScenario()`
- [ ] Endpoint tests creados usando `LoadScenario()`
- [ ] Tests pasan exitosamente
- [ ] `ClearDatabase()` ejecuta en [SetUp]
- [ ] Tests son independientes (no hay state leaking)

### Run Tests

```bash
# Run all tests
dotnet test

# Run only infrastructure tests
dotnet test --filter FullyQualifiedName~Infrastructure.Tests

# Run only endpoint tests
dotnet test --filter FullyQualifiedName~WebApi.Tests

# Run specific test
dotnet test --filter FullyQualifiedName~NHUserRepositoryTests.GetByEmailAsync_WithExistingUser_ReturnsUser
```

**Expected Output**:
```
Test run for MyProject.Tests.dll (.NET 9.0)
Microsoft (R) Test Execution Command Line Tool Version 17.9.0

Starting test execution, please wait...
A total of 10 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15, Duration: 2.5 s
```

---

## Errores Comunes y Soluciones

### Error 1: "Preload scenario XML file not found"

**Error**:
```
FileNotFoundException: File D:\scenarios\myproject\CreateRoles.xml not found
```

**Causa**: Scenarios no han sido generados o output path incorrecto.

**Solución**:
```bash
# Re-run scenarios generator
cd tests/MyProject.scenarios
dotnet run /cnn:"..." /output:"D:\scenarios\myproject"

# Verify output path
echo $SCENARIOS_FOLDER_PATH
```

### Error 2: "AppSchema table not found"

**Error**:
```
PostgresException: relation "public.users" does not exist
```

**Causa**: Tabla existe en BD pero no está definida en `AppSchema.xsd`.

**Solución**:
1. Agregar tabla a `AppSchema.xsd`:
```xml
<xs:element name="public.users">
  <xs:complexType>
    <xs:sequence>
      <xs:element name="id" type="xs:string" minOccurs="0" />
      <xs:element name="email" type="xs:string" minOccurs="0" />
      <!-- ... más columnas -->
    </xs:sequence>
  </xs:complexType>
</xs:element>
```

2. Re-generate `AppSchema.Designer.cs`:
   - Right-click `AppSchema.xsd` → Properties → Custom Tool → `MSDataSetGenerator`
   - Save

3. Re-run scenarios generator

### Error 3: "Column 'xyz' does not exist"

**Error**:
```
PostgresException: column "last_login_date" does not exist
```

**Causa**: Columna agregada a BD pero no está en `AppSchema.xsd`.

**Solución**:
1. Actualizar `AppSchema.xsd` con nueva columna
2. Re-generate Designer
3. Re-run scenarios generator

### Error 4: "Foreign key constraint violation"

**Error**:
```
PostgresException: insert or update on table "user_in_roles" violates foreign key constraint
```

**Causa**: Datos no existen en tabla referenciada.

**Solución**:
Verificar que scenario tiene PreloadScenario correcto:
```csharp
public class Sc031CreateAdminUser : IScenario
{
    // ✅ Debe depender de Sc030CreateUsers
    public Type? PreloadScenario => typeof(Sc030CreateUsers);
}
```

### Error 5: "Scenario SeedData() throws exception"

**Error**:
```
InvalidOperationException: usuario1@example.com not found
```

**Causa**: Preload scenario no se cargó correctamente.

**Solución**:
Agregar defensive checks en SeedData():
```csharp
var user = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
if (user == null)
    throw new InvalidOperationException(
        "usuario1@example.com not found. " +
        "Ensure Sc030CreateUsers was loaded as preload scenario.");
```

### Error 6: "Tests fail intermittently"

**Problema**: Tests pasan a veces, fallan otras veces.

**Causa**: State leaking entre tests (no se limpia BD).

**Solución**:
Verificar que `ClearDatabase()` se llama en [SetUp]:
```csharp
[SetUp]
public void Setup()
{
    RepositoryUnderTest = BuildRepository();
    nDbUnitTest.ClearDatabase();  // ✅ CRITICAL
}
```

### Error 7: "Cannot generate AppSchema.Designer.cs"

**Problema**: Visual Studio no genera el Designer file.

**Solución**:

**Opción 1: Command Line**
```bash
# Usar xsd.exe (incluido con Visual Studio)
xsd AppSchema.xsd /dataset /language:CS /namespace:MyProject.common.tests
```

**Opción 2: MSBuild Property**
Agregar a `.csproj`:
```xml
<ItemGroup>
  <None Update="AppSchema.xsd">
    <Generator>MSDataSetGenerator</Generator>
    <LastGenOutput>AppSchema.Designer.cs</LastGenOutput>
  </None>
  <Compile Update="AppSchema.Designer.cs">
    <DependentUpon>AppSchema.xsd</DependentUpon>
    <AutoGen>True</AutoGen>
    <DesignTime>True</DesignTime>
  </Compile>
</ItemGroup>
```

---

## Próximos Pasos

### 1. Agregar más Scenarios

Crear scenarios para otros features:

```csharp
// Sc040CreateTechnicalStandards
// Sc050CreatePrototypes
// Sc060CreateInspections
```

### 2. Integrar con CI/CD

**GitHub Actions**:
```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_DB: testdb
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: root
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x

      - name: Generate Scenarios
        run: |
          cd tests/MyProject.scenarios
          dotnet run /cnn:"Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=root" /output:"./scenarios"

      - name: Run Tests
        run: dotnet test
```

### 3. Documentar Scenarios

Crear `tests/MyProject.scenarios/README.md`:

```markdown
# Scenarios

## Available Scenarios

| Scenario | File | Dependencies | Purpose |
|----------|------|--------------|---------|
| Sc010CreateSandBox | CreateSandBox.xml | None | Empty database |
| Sc020CreateRoles | CreateRoles.xml | Sc010 | System roles |
| Sc030CreateUsers | CreateUsers.xml | Sc020 | 5 test users |
| Sc031CreateAdminUser | CreateAdminUser.xml | Sc030 | Admin user |

## Dependency Graph

```
Sc010CreateSandBox
    └── Sc020CreateRoles
        └── Sc030CreateUsers
            └── Sc031CreateAdminUser
```

## Usage in Tests

```csharp
[Test]
public async Task MyTest()
{
    LoadScenario("CreateUsers");  // Loads 5 users + 1 role
    // ...
}
```
```

---

## Conclusión

Has implementado exitosamente el **Scenarios System** para integration testing. Ahora tienes:

✅ **Infrastructure completa** - NDbUnit, AppSchema, Base test classes
✅ **Scenarios reutilizables** - XML snapshots de estados de BD
✅ **Tests rápidos** - Cargan datos pre-validados, no ejecutan lógica de negocio
✅ **Tests predecibles** - Datos conocidos, no aleatorios
✅ **Tests mantenibles** - Un scenario usado por múltiples tests

**Beneficios**:
- Tests no fallan por bugs en código de setup
- Datos de prueba versionados en Git
- Fácil debugging (sabes exactamente qué datos hay)
- Reuso entre repository y endpoint tests

**Next Steps**:
1. Agregar scenarios para cada feature nuevo
2. Mantener AppSchema.xsd sincronizado con BD
3. Re-generar XML cuando cambian entities
4. Documentar scenarios en README

¡El sistema está listo para usar! 🎉
