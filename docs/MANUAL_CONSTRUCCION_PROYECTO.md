# Manual de Construcción de Proyecto Base - APSYS

## Tabla de Contenidos

1. [Descripción General](#descripción-general)
2. [Parámetros de Entrada](#parámetros-de-entrada)
3. [Estructura Final del Proyecto](#estructura-final-del-proyecto)
4. [Proceso de Construcción Paso a Paso](#proceso-de-construcción-paso-a-paso)
5. [Detalles de Cada Proyecto](#detalles-de-cada-proyecto)
6. [Referencias Completas de Paquetes NuGet](#referencias-completas-de-paquetes-nuget)
7. [Templates y Archivos Generados](#templates-y-archivos-generados)

---

## Descripción General

Este documento describe el proceso completo de creación manual de un proyecto backend con Clean Architecture para APSYS. El CLI `apsys.builder` automatiza todos estos pasos.

El proyecto sigue una arquitectura limpia (Clean Architecture) con las siguientes capas:

- **Domain**: Lógica de negocio y entidades de dominio
- **Application**: Casos de uso y lógica de aplicación
- **Infrastructure**: Implementaciones de infraestructura (repositorios, ORM)
- **WebApi**: Capa de presentación (API REST con FastEndpoints)

Además incluye:

- **Migrations**: Sistema de migraciones de base de datos con FluentMigrator
- **Tests**: Proyectos de pruebas para cada capa
- **Scenarios**: Herramienta para generar escenarios de datos de prueba
- **NDbUnit**: Utilidad para gestión de datos de prueba

---

## Parámetros de Entrada

El CLI requiere los siguientes parámetros:

| Parámetro   | Descripción                  | Ejemplo                     |
| ----------- | ---------------------------- | --------------------------- |
| `--name`    | Nombre de la solución        | `MiProyecto`                |
| `--version` | Versión de .NET              | `net9.0`                    |
| `--path`    | Ruta donde crear el proyecto | `C:\projects\miproyecto`    |
| `--db`      | Tipo de base de datos        | `PostgreSQL` o `SQL Server` |

**Ejemplo de comando:**

```bash
apsys.builder init --name=MiProyecto --version=net9.0 --path=C:\projects\miproyecto --db=PostgreSQL
```

---

## Estructura Final del Proyecto

```
MiProyecto/
├── MiProyecto.sln
├── Directory.Packages.props
├── src/
│   ├── MiProyecto.domain/
│   ├── MiProyecto.application/
│   ├── MiProyecto.infrastructure/
│   ├── MiProyecto.webapi/
│   └── MiProyecto.migrations/
└── tests/
    ├── MiProyecto.domain.tests/
    ├── MiProyecto.application.tests/
    ├── MiProyecto.infrastructure.tests/
    ├── MiProyecto.webapi.tests/
    ├── MiProyecto.ndbunit/
    ├── MiProyecto.common.tests/
    └── MiProyecto.scenarios/
```

---

## Proceso de Construcción Paso a Paso

### PASO 1: Crear la Solución Base

#### 1.1 Crear estructura de carpetas

```bash
mkdir "C:\projects\miproyecto"
cd "C:\projects\miproyecto"
mkdir src
mkdir tests
```

#### 1.2 Crear archivo de solución

```bash
dotnet new sln -n MiProyecto -o "C:\projects\miproyecto"
```

#### 1.3 Crear archivo Directory.Packages.props

Crear el archivo `Directory.Packages.props` en la raíz de la solución con el siguiente contenido:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="AutoFixture.AutoMoq" Version="4.18.1" />
    <PackageVersion Include="AutoMapper" Version="15.0.1" />
    <PackageVersion Include="Castle.Core" Version="5.1.1" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="DotNetEnv" Version="3.1.1" />
    <PackageVersion Include="FastEndpoints" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Security" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Swagger" Version="7.0.1" />
    <PackageVersion Include="FastEndpoints.Testing" Version="7.0.1" />
    <PackageVersion Include="FluentAssertions" Version="8.5.0" />
    <PackageVersion Include="FluentMigrator" Version="7.1.0" />
    <PackageVersion Include="FluentMigrator.Runner" Version="7.1.0" />
    <PackageVersion Include="FluentValidation" Version="12.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.7" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.7" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="9.0.5" />
    <PackageVersion Include="Microsoft.AspNetCore.WebUtilities" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="5.2.2" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.7" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Log4Net.AspNetCore" Version="8.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="NHibernate" Version="5.5.2" />
    <PackageVersion Include="Npgsql" Version="9.0.3" />
    <PackageVersion Include="NUnit" Version="4.2.2" />
    <PackageVersion Include="NUnit.Analyzers" Version="4.4.0" />
    <PackageVersion Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageVersion Include="Scrutor" Version="6.1.0" />
    <PackageVersion Include="Spectre.Console" Version="0.50.0" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="9.0.3" />
    <PackageVersion Include="System.ComponentModel.TypeConverter" Version="4.3.0" />
    <PackageVersion Include="System.Linq.Dynamic.Core" Version="1.6.7" />
  </ItemGroup>
</Project>
```

**Propósito**: Este archivo habilita la gestión centralizada de paquetes NuGet. Todas las versiones de paquetes se definen aquí una sola vez, y los proyectos solo referencian el nombre del paquete sin especificar versión.

---

### PASO 2: Crear Proyecto Migrations

#### 2.1 Crear proyecto console

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.migrations"
dotnet new console -n MiProyecto.migrations -o "C:\projects\miproyecto\src\MiProyecto.migrations"
```

#### 2.2 Agregar a la solución

```bash
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj"
```

#### 2.3 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package FluentMigrator
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package FluentMigrator.Runner
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package Microsoft.Extensions.DependencyInjection
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package Spectre.Console
```

#### 2.4 Crear archivo Program.cs

Reemplazar el contenido de `src/MiProyecto.migrations/Program.cs`:

```csharp
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using MiProyecto.migrations;

const string _run = "run";
const string _rollback = "rollback";

try
{
    AnsiConsole.MarkupLine("Reading command line parameters...");
    CommandLineArgs parameter = [];
    if (!parameter.TryGetValue("cnn", out string? value))
        throw new ArgumentException("No [cnn] parameter received. You need pass the connection string in order to execute the migrations");

    AnsiConsole.MarkupLine("[bold yellow]Connecting to database...[/]");
    string connectionStringValue = value;
    var serviceProvider = CreateServices(connectionStringValue);
    using var scope = serviceProvider.CreateScope();
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    if (!parameter.TryGetValue("action", out string? action) && string.IsNullOrEmpty(action))
        action = _run;

    if (action == _run)
    {
        AnsiConsole.Status()
            .Start("Start running migrations...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                ctx.Status("Running migrations...");
                UpdateDatabase(scope.ServiceProvider);
                runner.MigrateUp();
            });
        AnsiConsole.MarkupLine("All migrations are updated");
    }
    else if (action == _rollback)
    {
        AnsiConsole.Status()
            .Start("Start rolling back the last migration...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("blue"));
                ctx.Status("Rolling back migration...");
                var lastMigration = runner.MigrationLoader.LoadMigrations().LastOrDefault();
                var rollBackToVersion = lastMigration.Value.Version - 1;
                runner.MigrateDown(rollBackToVersion);
            });
        AnsiConsole.MarkupLine("Last transaction rolled back");
    }
    else
    {
        throw new ArgumentException("Invalid action. Please use 'run' or 'rollback'");
    }
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.UnknownError;
}

static IServiceProvider CreateServices(string? connectionString)
{
    return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
}

static void UpdateDatabase(IServiceProvider serviceProvider)
{
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
```

#### 2.5 Crear archivos adicionales

**CommandLineArgs.cs**:

```csharp
namespace MiProyecto.migrations;

public class CommandLineArgs : Dictionary<string, string>
{
    public CommandLineArgs()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.Contains('='))
            {
                var parts = arg.Split('=');
                Add(parts[0].TrimStart('-'), parts[1]);
            }
        }
    }
}

public enum ExitCode : int
{
    Success = 0,
    UnknownError = 1
}
```

**M001Sandbox.cs** (migración ejemplo):

```csharp
using FluentMigrator;

namespace MiProyecto.migrations;

[Migration(1)]
public class M001Sandbox : Migration
{
    public override void Up()
    {
        // Implementar migraciones aquí
    }

    public override void Down()
    {
        // Implementar rollback aquí
    }
}
```

---

### PASO 3: Crear Proyecto Domain

#### 3.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.domain"
dotnet new classlib -n MiProyecto.domain -o "C:\projects\miproyecto\src\MiProyecto.domain"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 3.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj" package FluentValidation
```

#### 3.3 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.domain.tests"
dotnet new nunit -n MiProyecto.domain.tests -o "C:\projects\miproyecto\tests\MiProyecto.domain.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj"
```

#### 3.4 Remover versiones de paquetes en .csproj de tests

**IMPORTANTE**: Editar `tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference` (porque usamos gestión centralizada con `Directory.Packages.props`).

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 3.5 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package FluentAssertions
```

#### 3.6 Agregar referencia al proyecto domain en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 3.7 Crear estructura de carpetas y archivos del dominio

**Estructura de carpetas a crear en `src/MiProyecto.domain/`:**

```
entities/
exceptions/
interfaces/
  └── repositories/
```

**Archivo: `entities/AbstractDomainObject.cs`**

```csharp
using FluentValidation;
using FluentValidation.Results;

namespace MiProyecto.domain.entities;

public abstract class AbstractDomainObject
{
    protected AbstractDomainObject()
    { }

    protected AbstractDomainObject(Guid id, DateTime creationDate)
    {
        Id = id;
        CreationDate = creationDate;
    }

    public virtual Guid Id { get; set; } = Guid.NewGuid();
    public virtual DateTime CreationDate { get; set; } = DateTime.Now;

    public virtual bool IsValid()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return true;

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.IsValid;
    }

    public virtual IEnumerable<ValidationFailure> Validate()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return new List<ValidationFailure>();
        else
        {
            var context = new ValidationContext<object>(this);
            ValidationResult result = validator.Validate(context);
            return result.Errors;
        }
    }

    public virtual IValidator? GetValidator()
         => null;
}
```

**Archivo: `exceptions/InvalidDomainException.cs`**

```csharp
using FluentValidation.Results;

namespace MiProyecto.domain.exceptions;

public class InvalidDomainException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; set; }

    public InvalidDomainException(IEnumerable<ValidationFailure> errors)
        : base("Domain validation failed")
    {
        Errors = errors;
    }
}
```

**Archivo: `exceptions/InvalidFilterArgumentException.cs`**

```csharp
namespace MiProyecto.domain.exceptions;

public class InvalidFilterArgumentException : Exception
{
    public InvalidFilterArgumentException(string message) : base(message)
    {
    }

    public InvalidFilterArgumentException(string message, string argName) : base(message)
    {
    }
}
```

**Archivo: `interfaces/repositories/IRepository.cs`**

```csharp
namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Defines a repository for managing entities with full CRUD operations.
/// Extends IReadOnlyRepository to include write operations.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class, new()
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    T Add(T item);

    /// <summary>
    /// Asynchronously adds a new entity to the repository.
    /// </summary>
    Task AddAsync(T item);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    T Save(T item);

    /// <summary>
    /// Asynchronously updates an existing entity in the repository.
    /// </summary>
    Task SaveAsync(T item);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    void Delete(T item);

    /// <summary>
    /// Asynchronously deletes an entity from the repository.
    /// </summary>
    Task DeleteAsync(T item);
}
```

**Archivo: `interfaces/repositories/IReadOnlyRepository.cs`**

```csharp
using System.Linq.Expressions;

namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Defines a read-only repository for retrieving entities from a data store.
/// This interface provides both synchronous and asynchronous methods for querying data.
/// </summary>
public interface IReadOnlyRepository<T, TKey> where T : class, new()
{
    #region Synchronous Methods

    T Get(TKey id);
    IEnumerable<T> Get();
    IEnumerable<T> Get(Expression<Func<T, bool>> query);
    IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria);
    int Count();
    int Count(Expression<Func<T, bool>> query);
    GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting);

    #endregion

    #region Asynchronous Methods

    Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default);
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default);

    #endregion
}
```

**Archivo: `interfaces/repositories/IUnitOfWork.cs`**

```csharp
namespace MiProyecto.domain.interfaces.repositories;

public interface IUnitOfWork : IDisposable
{
    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

**Archivo: `interfaces/repositories/GetManyAndCountResult.cs`**

```csharp
namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Class to return the result for a paginated query with sorting capabilities.
/// Provides a container for collections of items along with pagination and sorting information.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class GetManyAndCountResult<T> : IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Default page size when no specific size is requested.
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 25;

    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the total count of records that match the query criteria.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based indexing).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the sorting criteria applied to the result set.
    /// Implements the IGetManyAndCountResultWithSorting interface.
    /// </summary>
    public SortingCriteria Sorting { get; set; }

    /// <summary>
    /// Constructor that initializes a new instance with the specified items, count, pagination, and sorting information.
    /// </summary>
    /// <param name="items">The collection of items for the current page.</param>
    /// <param name="count">The total number of records that match the query criteria.</param>
    /// <param name="pageNumber">The current page number (1-based indexing).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="sorting">The sorting criteria applied to the result set.</param>
    public GetManyAndCountResult(IEnumerable<T> items, long count, int pageNumber, int pageSize, SortingCriteria sorting)
    {
        Items = items;
        Count = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Sorting = sorting;
    }

    /// <summary>
    /// Default constructor that initializes a new instance with empty items and default values.
    /// The default values are:
    /// - Empty collection of items
    /// - Count set to 0
    /// - Page number set to 1
    /// - Page size set to DEFAULT_PAGE_SIZE (25)
    /// - Sorting criteria initialized with default values
    /// </summary>
    public GetManyAndCountResult()
    {
        Items = [];
        Count = 0;
        PageNumber = 1;
        PageSize = DEFAULT_PAGE_SIZE;
        Sorting = new SortingCriteria();
    }
}
```

**Archivo: `interfaces/repositories/SortingCriteria.cs`**

```csharp
namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Class representing a sorting criteria
/// </summary>
public class SortingCriteria
{

    /// <summary>
    /// Gets or sets the name of the field to sort by.
    /// This property is used to specify the field name that will be used for sorting the results
    /// </summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sorting criteria type.
    /// This property indicates whether the sorting should be done in ascending or descending order.
    /// </summary>
    public SortingCriteriaType Criteria { get; set; } = SortingCriteriaType.Ascending;

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria()
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy)
    {
        this.SortBy = sortBy;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public SortingCriteria(string sortBy, SortingCriteriaType criteria)
    {
        this.SortBy = sortBy;
        this.Criteria = criteria;
    }
}

/// <summary>
/// The sorting criteria type enumeration
/// </summary>
public enum SortingCriteriaType
{
    /// <summary>
    /// Sort ascending
    /// </summary>
    Ascending = 1,

    /// <summary>
    /// Sort descending
    /// </summary>
    Descending = 2
}
```

**Archivo: `interfaces/repositories/IGetManyAndCountResultWithSorting.cs`**

```csharp
namespace MiProyecto.domain.interfaces.repositories;

/// <summary>
/// Interface for objects that provide sorting capabilities in paginated results.
/// This allows implementing classes to expose sorting criteria information.
/// </summary>
public interface IGetManyAndCountResultWithSorting
{
    /// <summary>
    /// Gets the sorting criteria applied to the result set.
    /// </summary>
    SortingCriteria Sorting { get; }
}
```

**Archivo de test: `tests/MiProyecto.domain.tests/entities/DomainTestBase.cs`**

```csharp
namespace MiProyecto.domain.tests.entities;

public class DomainTestBase
{
    [SetUp]
    public void Setup()
    {
    }
}
```

---

### PASO 4: Crear Proyecto NDbUnit

#### 4.1 Crear proyecto classlib

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.ndbunit"
dotnet new classlib -n MiProyecto.ndbunit -o "C:\projects\miproyecto\tests\MiProyecto.ndbunit"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
```

#### 4.2 Instalar paquetes NuGet según base de datos

**Para PostgreSQL:**

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj" package Npgsql
```

**Para SQL Server:**

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj" package Microsoft.Data.SqlClient
```

#### 4.3 Crear archivos

**Archivo: `INDbUnit.cs`**

```csharp
using System.Data.Common;
using System.Data;

namespace MiProyecto.ndbunit;

public interface INDbUnit
{
    /// <summary>
    /// Gets the dataSet containing the tables where the operations are execute
    /// </summary>
    DataSet DataSet { get; }

    /// <summary>
    /// Gets the connection string to the database where the operations are execute
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Get a dataset with the tables and data from the database
    /// </summary>
    /// <returns></returns>
    DataSet GetDataSetFromDb();

    /// <summary>
    /// Create a data adapter to the database
    /// </summary>
    /// <returns></returns>
    DbDataAdapter CreateDataAdapter();

    /// <summary>
    /// Clear all the database data
    /// </summary>
    void ClearDatabase();

    /// <summary>
    /// Seed the database with the information contained in the dataset
    /// </summary>
    /// <param name="dataSet"></param>
    void SeedDatabase(DataSet dataSet);
}
```

**Archivo: `NDbUnit.cs`**

```csharp
using System.Data.Common;
using System.Data;

namespace MiProyecto.ndbunit;

public abstract class NDbUnit : INDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    protected NDbUnit(DataSet dataSet, string connectionString)
    {
        this.ConnectionString = connectionString;
        this.DataSet = dataSet;
    }

    /// <summary>
    /// Gets the dataSet containing the tables where the operations are execute
    /// </summary>
    public DataSet DataSet { get; private set; }

    /// <summary>
    /// Gets the connection string to the database where the operations are execute
    /// </summary>
    public string ConnectionString { get; private set; }

    /// <summary>
    /// Get a dataset with the tables and data from the database
    /// </summary>
    /// <returns></returns>
    public DataSet GetDataSetFromDb()
    {
        using DbConnection cnn = this.CreateConnection();
        DataSet dsetResult = this.DataSet.Clone();
        dsetResult.EnforceConstraints = false;
        DbProviderFactory? dbFactory = DbProviderFactories.GetFactory(cnn) ?? throw new ArgumentException("Cannot create [DbProviderFactory] from configuration");
        foreach (DataTable table in this.DataSet.Tables)
        {
            DbCommand selectCommand = cnn.CreateCommand();
            selectCommand.CommandText = $"SELECT * FROM {table.TableName}";

            DbDataAdapter? adapter = dbFactory.CreateDataAdapter() ?? throw new ArgumentException("Cannot create [DbDataAdapter] from configuration");
            adapter.SelectCommand = selectCommand;
            adapter.Fill(dsetResult, table.TableName);
        }
        dsetResult.EnforceConstraints = true;
        return dsetResult;

    }

    /// <summary>
    /// Clear all the database data
    /// </summary>
    public void ClearDatabase()
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();

        using IDbTransaction transaction = cnn.BeginTransaction();
        try
        {
            foreach (DataTable dataTable in this.DataSet.Tables)
                this.DisableTableConstraints(transaction, dataTable);

            foreach (DataTable dataTable in this.DataSet.Tables)
            {
                var cmd = cnn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = $"DELETE FROM {dataTable.TableName}";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }

            foreach (DataTable dataTable in this.DataSet.Tables)
                this.EnabledTableConstraints(transaction, dataTable);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        cnn.Close();
    }

    /// <summary>
    /// Seed the database with the information contained in the dataset
    /// </summary>
    /// <param name="dataSet"></param>
    public void SeedDatabase(DataSet dataSet)
    {
        using IDbConnection cnn = this.CreateConnection();
        cnn.Open();

        this.DataSet = dataSet;

        using (IDbTransaction transaction = cnn.BeginTransaction())
        {
            try
            {
                foreach (DataTable dataTable in this.DataSet.Tables)
                    this.DisableTableConstraints(transaction, dataTable);

                foreach (DataTable dataTable in this.DataSet.Tables)
                {
                    // Create select comand
                    var selectCommand = cnn.CreateCommand();
                    selectCommand.CommandText = $"SELECT * FROM {dataTable.TableName}";
                    selectCommand.Transaction = transaction;
                    // Crear un adaptador de datos
                    var adapter = this.CreateDataAdapter();
                    adapter.SelectCommand = selectCommand as DbCommand;
                    var commandBuilder = this.CreateCommandBuilder(adapter);
                    adapter.InsertCommand = commandBuilder.GetInsertCommand();
                    adapter.InsertCommand.Transaction = transaction as DbTransaction;
                    // Actualiza la tabla
                    adapter.Update(dataTable);
                }

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
        cnn.Close();
    }

    /// <summary>
    /// Creates a DbConnection
    /// </summary>
    /// <returns></returns>
    public abstract DbConnection CreateConnection();

    /// <summary>
    /// Create a DbDataAdapter
    /// </summary>
    /// <returns></returns>
    public abstract DbDataAdapter CreateDataAdapter();

    /// <summary>
    /// Create a command builder
    /// </summary>
    /// <param name="dataAdapter"></param>
    /// <returns></returns>
    public abstract DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter);

    /// <summary>
    /// Enable datatable's constraints
    /// </summary>
    /// <param name="dbTransaction"></param>
    /// <param name="dataTable"></param>
    protected abstract void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);

    /// <summary>
    /// Disable datatable's constraints
    /// </summary>
    /// <param name="dbTransaction"></param>
    /// <param name="dataTable"></param>
    protected abstract void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);
}
```

**Archivo para PostgreSQL: `PostgreSQLNDbUnit.cs`**

```csharp
using System.Data.Common;
using System.Data;
using Npgsql;

namespace MiProyecto.ndbunit;

public class PostgreSQLNDbUnit : NDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    public PostgreSQLNDbUnit(DataSet dataSet, string connectionString)
        : base(dataSet, connectionString)
    {
    }

    public override DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter)
    {
        return new NpgsqlCommandBuilder((NpgsqlDataAdapter)dataAdapter);
    }

    public override DbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }

    public override DbDataAdapter CreateDataAdapter()
    {
        return new NpgsqlDataAdapter();
    }

    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a disable constraint for PostgreSQL
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} DISABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }

    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a enable constraint for PostgreSQL
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} ENABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }
}
```

**Archivo para SQL Server: `SqlServerNDbUnit.cs`**

```csharp
using System.Data.Common;
using System.Data;
using Microsoft.Data.SqlClient;

namespace MiProyecto.ndbunit;

public class SqlServerNDbUnit : NDbUnit
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="connectionString"></param>
    public SqlServerNDbUnit(DataSet dataSet, string connectionString)
        : base(dataSet, connectionString)
    {
    }

    public override DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter)
    {
        return new SqlCommandBuilder((SqlDataAdapter)dataAdapter);
    }

    public override DbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public override DbDataAdapter CreateDataAdapter()
    {
        return new SqlDataAdapter();
    }

    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a disable constraint for SQL Server
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using SqlCommand command = (SqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (SqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} NOCHECK CONSTRAINT ALL";
        command.ExecuteNonQuery();
    }

    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        //Implements a enable constraint for SQL Server
        if (dbTransaction?.Connection == null)
            throw new ArgumentNullException(nameof(dbTransaction));

        using SqlCommand command = (SqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (SqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} WITH CHECK CHECK CONSTRAINT ALL";
        command.ExecuteNonQuery();
    }
}
```

**NOTA**: Solo debes crear el archivo correspondiente a la base de datos seleccionada (PostgreSQL o SQL Server), no ambos.

---

### PASO 5: Crear Proyecto Common Tests

#### 5.1 Crear proyecto classlib

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.common.tests"
dotnet new classlib -n MiProyecto.common.tests -o "C:\projects\miproyecto\tests\MiProyecto.common.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj"
```

#### 5.2 Agregar referencia al proyecto domain

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 5.3 Crear archivos de esquema de base de datos

Crear los siguientes archivos en `tests/MiProyecto.common.tests/`:

**AppSchema.xsd** - Esquema XSD para definir estructura de datos de prueba
**AppSchema.xsc** - Archivo de configuración del esquema
**AppSchema.xss** - Archivo de diseño del esquema
**AppSchema.Designer.cs** - Clase generada para el esquema

Estos archivos permiten trabajar con datasets tipados en .NET para las pruebas.

#### 5.4 Crear AppSchemaExtender

Este archivo proporciona métodos de extensión para facilitar el acceso a tablas y filas del DataSet en las pruebas de integración.

**Archivo: `AppSchemaExtender.cs`**

```csharp
using System.Data;
using MiProyecto.domain.resources;

namespace MiProyecto.common.tests;

public static class AppSchemaExtender
{

    #region Get full qualified table names
    public static readonly string FullRolesTableName = $"{AppSchemaResource.SchemaName}.{AppSchemaResource.RolesTable}";
    public static readonly string FullUsersTableName = $"{AppSchemaResource.SchemaName}.{AppSchemaResource.UsersTable}";
    // Agregar más tablas según sea necesario
    #endregion


    #region Get tables methods
    public static DataTable? GetRolesTable(this DataSet appSchema)
        => appSchema.Tables[FullRolesTableName];
    public static DataTable? GetUsersTable(this DataSet appSchema)
        => appSchema.Tables[FullUsersTableName];
    // Agregar más métodos según sea necesario
    #endregion


    #region Get rows methods

    public static IEnumerable<DataRow> GetRolesRows(this DataSet appSchema, string filterExpression)
        => GetRolesTable(appSchema)?.Select(filterExpression).AsEnumerable() ?? Enumerable.Empty<DataRow>();

    public static IEnumerable<DataRow> GetUsersRows(this DataSet appSchema, string filterExpression)
        => GetUsersTable(appSchema)?.Select(filterExpression).AsEnumerable() ?? Enumerable.Empty<DataRow>();

    // Agregar más métodos según sea necesario
    #endregion


    #region Get single row methods

    public static DataRow? GetFirstUserRow(this DataSet appSchema)
        => GetUsersTable(appSchema)?.AsEnumerable().FirstOrDefault();

    public static DataRow? GetFirstRoleRow(this DataSet appSchema)
        => GetRolesTable(appSchema)?.AsEnumerable().FirstOrDefault();

    // Agregar más métodos según sea necesario
    #endregion
}
```

**NOTA**: Este archivo debe ser extendido con métodos para cada tabla que se agregue al AppSchema. Es fundamental para las pruebas de integración ya que permite acceder fácilmente a los datos del DataSet para verificar resultados en el Assert.

---

### PASO 6: Crear Proyecto Application

#### 6.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.application"
dotnet new classlib -n MiProyecto.application -o "C:\projects\miproyecto\src\MiProyecto.application"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj"
```

#### 6.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj" package FastEndpoints
```

#### 6.3 Agregar referencia al proyecto domain

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 6.4 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.application.tests"
dotnet new nunit -n MiProyecto.application.tests -o "C:\projects\miproyecto\tests\MiProyecto.application.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj"
```

#### 6.5 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.application.tests/MiProyecto.application.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 6.6 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package FluentAssertions
```

#### 6.7 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj"
```

---

### PASO 7: Crear Proyecto Infrastructure

#### 7.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.infrastructure"
dotnet new classlib -n MiProyecto.infrastructure -o "C:\projects\miproyecto\src\MiProyecto.infrastructure"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 7.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package FluentValidation
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package NHibernate
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package System.Linq.Dynamic.Core
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package Microsoft.AspNetCore.WebUtilities
```

#### 7.3 Agregar referencias al proyecto

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

**NOTA**: Las referencias a `ndbunit` y `common.tests` solo son necesarias en los proyectos de prueba, no en infrastructure.

#### 7.4 Crear estructura de carpetas

```
src/MiProyecto.infrastructure/
└── nhibernate/
    └── filtering/
```

#### 7.5 Crear archivos de NHibernate

**Archivo: `nhibernate/NHRepository.cs`**

```csharp
using MiProyecto.domain.exceptions;
using MiProyecto.domain.interfaces.repositories;
using FluentValidation;
using NHibernate;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Base repository class for full CRUD operations using NHibernate.
/// Extends NHReadOnlyRepository to include write operations with validation support.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
public abstract class NHRepository<T, TKey> : NHReadOnlyRepository<T, TKey>, IRepository<T, TKey> where T : class, new()
{
    private readonly AbstractValidator<T> validator;

    /// <summary>
    /// Initializes a new instance of the NHRepository class.
    /// </summary>
    /// <param name="session">The NHibernate session</param>
    /// <param name="serviceProvider">The service provider for resolving validators</param>
    /// <exception cref="InvalidOperationException">Thrown when validator cannot be resolved</exception>
    protected NHRepository(ISession session, IServiceProvider serviceProvider)
    : base(session)
    {
        Type genericType = typeof(AbstractValidator<>).MakeGenericType(typeof(T));
        this.validator = serviceProvider.GetService(genericType) as AbstractValidator<T> ?? throw new InvalidOperationException($"The validator for {typeof(T)} type could not be created");
    }

    /// <summary>
    /// Adds a new entity to the repository after validation.
    /// </summary>
    /// <param name="item">The entity to add</param>
    /// <returns>The added entity</returns>
    /// <exception cref="InvalidDomainException">Thrown when validation fails</exception>
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
    /// </summary>
    /// <param name="item">The entity to add</param>
    public Task AddAsync(T item)
        => this._session.SaveAsync(item);

    /// <summary>
    /// Updates an existing entity in the repository after validation.
    /// </summary>
    /// <param name="item">The entity to update</param>
    /// <returns>The updated entity</returns>
    /// <exception cref="InvalidDomainException">Thrown when validation fails</exception>
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
    /// Asynchronously updates an existing entity in the repository.
    /// </summary>
    /// <param name="item">The entity to update</param>
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
    public Task DeleteAsync(T item)
        => this._session.DeleteAsync(item);

    /// <summary>
    /// Checks if there is an active transaction on the current session.
    /// </summary>
    /// <returns>True if a transaction is active, false otherwise</returns>
    protected internal bool IsTransactionActive()
        => this._session.GetCurrentTransaction() != null && this._session.GetCurrentTransaction().IsActive;

    /// <summary>
    /// Flushes the session if there is no active transaction.
    /// This ensures changes are persisted when not in a transaction context.
    /// </summary>
    protected internal void FlushWhenNotActiveTransaction()
    {
        var currentTransaction = this._session.GetCurrentTransaction();
        if (currentTransaction == null || !currentTransaction.IsActive)
            this._session.Flush();
    }
}
```

**Archivo: `nhibernate/NHReadOnlyRepository.cs`**

```csharp
using System.Linq.Expressions;
using MiProyecto.domain.interfaces.repositories;
using MiProyecto.infrastructure.nhibernate.filtering;
using System.Linq.Dynamic.Core;
using NHibernate;
using NHibernate.Linq;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Implementation of the read-only repository pattern using NHibernate ORM.
/// Provides methods for querying entities from a data store without modifying them.
/// Uses C# 12 primary constructor syntax.
/// </summary>
/// <typeparam name="T">The entity type that this repository handles</typeparam>
/// <typeparam name="TKey">The type of the primary key for the entity</typeparam>
/// <param name="session">The NHibernate session used for database operations</param>
public class NHReadOnlyRepository<T, TKey>(ISession session) : IReadOnlyRepository<T, TKey> where T : class, new()
{
    /// <summary>
    /// The NHibernate session used for database operations.
    /// Protected to allow access from derived classes.
    /// </summary>
    protected internal readonly ISession _session = session;

    /// <summary>
    /// Counts the total number of entities in the repository.
    /// </summary>
    /// <returns>The total count of entities</returns>
    public int Count()
        => this._session.QueryOver<T>().RowCount();

    /// <summary>
    /// Counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <returns>The count of entities that match the query</returns>
    public int Count(Expression<Func<T, bool>> query)
        => this._session.Query<T>().Where(query).Count();

    /// <summary>
    /// Asynchronously counts the total number of entities in the repository.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
    /// <returns>A task representing the asynchronous operation, with a result of the total count of entities</returns>

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => this._session.Query<T>().CountAsync(cancellationToken);

    /// <summary>
    /// Asynchronously counts the number of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities to be counted</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
    /// <returns>A task representing the asynchronous operation, with a result of the count of matching entities</returns>
    public Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
    => this._session.Query<T>().Where(query).CountAsync(cancellationToken);

    /// <summary>
    /// Retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <returns>The entity with the specified identifier, or null if not found</returns>
    public T Get(TKey id)
        => this._session.Get<T>(id);

    /// <summary>
    /// Retrieves all entities from the repository.
    /// </summary>
    /// <returns>An enumerable collection of all entities in the repository</returns>
    public IEnumerable<T> Get()
        => this._session.Query<T>();

    /// <summary>
    /// Retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <returns>An enumerable collection of entities that match the query</returns>
    public IEnumerable<T> Get(Expression<Func<T, bool>> query)
        => this._session.Query<T>()
                .Where(query);

    /// <summary>
    /// Retrieves a paginated and sorted subset of entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="page">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="sortingCriteria">The criteria to use for sorting the results</param>
    /// <returns>A paginated and sorted enumerable collection of entities that match the query</returns>
    public IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria)
        => this._session.Query<T>()
                .Where(query)
                .OrderBy(sortingCriteria.ToExpression())
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

    /// <summary>
    /// Asynchronously retrieves an entity by its typed identifier.
    /// </summary>
    /// <param name="id">The typed identifier of the entity to retrieve</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the entity with the specified identifier, or null if not found</returns>
    public Task<T> GetAsync(TKey id, CancellationToken cancellationToken = default)
        => this._session.GetAsync<T>(id, cancellationToken);

    /// <summary>
    /// Asynchronously retrieves all entities from the repository.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of all entities</returns>
    public async Task<IEnumerable<T>> GetAsync(CancellationToken cancellationToken = default)
        => await this._session.Query<T>().ToListAsync(cancellationToken);

    /// <summary>
    /// Asynchronously retrieves all entities that match a specified query expression.
    /// </summary>
    /// <param name="query">A LINQ expression to filter the entities</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of matching entities</returns>
    public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken cancellationToken = default)
        => await this._session.Query<T>()
                .Where(query)
                .ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A result object containing both the matching entities and the total count</returns>
    public GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute query
        var items = this.Get(expression, pageNumber, pageSize, sortingCriteria);
        var total = this.Count(expression);

        // Return results
        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    /// <summary>
    /// Asynchronously retrieves a paginated result set along with the total count of items that match a string query.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>A task representing the asynchronous operation, containing a result object with both the matching entities and the total count</returns>
    public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken cancellationToken = default)
    {
        var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

        // Execute queries sequentially to avoid DataReader conversion issues
        var total = await this._session.Query<T>()
            .Where(expression)
            .CountAsync(cancellationToken);

        var items = await this._session.Query<T>()
            .OrderBy(sortingCriteria.ToExpression())
            .Where(expression)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return results
        return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
    }

    /// <summary>
    /// Prepares a query by parsing the query string and extracting pagination, sorting, and filtering information.
    /// </summary>
    /// <param name="query">An optional string query that contains filtering, sorting, and pagination parameters</param>
    /// <param name="defaultSorting">The default sorting expression to use when no specific sorting is requested</param>
    /// <returns>A tuple containing the prepared expression, page number, page size, and sorting criteria</returns>
    private static (Expression<Func<T, bool>> expression, int pageNumber, int pageSize, SortingCriteria sortingCriteria) PrepareQuery(string? query, string defaultSorting)
    {
        var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
        QueryStringParser queryStringParser = new(queryString);

        // Get pagination info
        int pageNumber = queryStringParser.ParsePageNumber();
        int pageSize = queryStringParser.ParsePageSize();

        // Get sorting info
        Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
        SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
            ? SortingCriteriaType.Descending
            : SortingCriteriaType.Ascending;
        SortingCriteria sortingCriteria = new(sorting.By, directions);

        // Get filters
        IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
        QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
        var expression = FilterExpressionParser.ParsePredicate<T>(filters);
        if (quickSearch != null)
            expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

        return (expression, pageNumber, pageSize, sortingCriteria);
    }
}
```

**Archivo: `nhibernate/NHUnitOfWork.cs`**

```csharp
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
```

**Archivo: `nhibernate/NHSessionFactory.cs`**

```csharp
using MiProyecto.infrastructure.nhibernate.mappers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Session factory using C# 12 primary constructor syntax
/// </summary>
public class NHSessionFactory(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    /// <summary>
    /// Create the NHibernate Session Factory
    /// </summary>
    /// <returns></returns>
    public ISessionFactory BuildNHibernateSessionFactory()
    {
        var mapper = new ModelMapper();
        // Registrar todos los mappers del ensamblado
        // Usar cualquier mapper como referencia para obtener el assembly
        mapper.AddMappings(typeof(RoleMapper).Assembly.ExportedTypes);
        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            // Para PostgreSQL:
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            // Para SQL Server:
            // c.Driver<MicrosoftDataSqlClientDriver>();
            // c.Dialect<MsSql2012Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
            //c.SchemaAction = SchemaAutoAction.Validate;
        });
        cfg.AddMapping(domainMapping);
        return cfg.BuildSessionFactory();
    }
}
```

**NOTA**: Este patrón usa primary constructor de C# 12 y registra automáticamente todos los mappers del ensamblado. Debes crear al menos un mapper (ej. `RoleMapper`) antes de que este código funcione.

**Archivo: `nhibernate/ConnectionStringBuilder.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate;

public static class ConnectionStringBuilder
{
    public static string BuildPostgresConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var port = Environment.GetEnvironmentVariable("DB_PORT");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var username = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    public static string BuildSqlServerConnectionString()
    {
        var server = Environment.GetEnvironmentVariable("DB_SERVER");
        var database = Environment.GetEnvironmentVariable("DB_NAME");
        var username = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");

        return $"Server={server};Database={database};User Id={username};Password={password}";
    }
}
```

**Archivo: `nhibernate/SortingCriteriaExtender.cs`**

```csharp
using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.infrastructure.nhibernate;

public static class SortingCriteriaExtender
{
    public static string ToOrderByClause(this IEnumerable<SortingCriteria> sortingCriteria)
    {
        if (sortingCriteria == null || !sortingCriteria.Any())
            return string.Empty;

        var orderByClauses = sortingCriteria
            .Select(sc => $"{sc.PropertyName} {(sc.Ascending ? "ASC" : "DESC")}");

        return string.Join(", ", orderByClauses);
    }
}
```

**Archivos de filtering**: Crear los siguientes archivos en `nhibernate/filtering/`:

**Archivo: `nhibernate/filtering/InvalidQueryStringArgumentException.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

public class InvalidQueryStringArgumentException : Exception
{
    public InvalidQueryStringArgumentException(string argName)
        : base($"Parameter [{argName}] has an invalid value.")
    {
    }

    public InvalidQueryStringArgumentException(string message, string argName)
        : base(message)
    {
    }
}
```

**Archivo: `nhibernate/filtering/StringExtender.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

public static class StringExtender
{
    public static string ToPascalCase(this string instance)
    {
        return string.IsNullOrWhiteSpace(instance)
            ? instance
            : $"{instance.Substring(0, 1).ToUpper()}{instance.Substring(1)}";
    }
}
```

**Archivo: `nhibernate/filtering/RelationalOperator.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

static class RelationalOperator
{
    public const string Equal = "equal";
    public const string NotEqual = "not_equal";
    public const string Contains = "contains";
    public const string StartsWith = "starts_with";
    public const string EndsWith = "ends_with";
    public const string Between = "between";
    public const string GreaterThan = "greater_than";
    public const string GreaterThanOrEqual = "greater_or_equal_than";
    public const string LessThan = "less_than";
    public const string LessThanOrEqual = "less_or_equal_than";
}
```

**Archivo: `nhibernate/filtering/FilterOperator.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

public class FilterOperator
{
    public FilterOperator()
    { }

    public FilterOperator(string fileldName, IEnumerable<string> values, string relationalOperatorType)
    {
        FieldName = fileldName;
        RelationalOperatorType = relationalOperatorType;
        Values = values.ToList();
    }

    public string FieldName { get; set; } = string.Empty;
    public string RelationalOperatorType { get; set; } = string.Empty;
    public IList<string> Values { get; set; } = new List<string>();
}
```

**Archivo: `nhibernate/filtering/QuickSearch.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

public class QuickSearch
{
    public string Value { get; set; } = string.Empty;
    public IList<string> FieldNames { get; set; } = new List<string>();
}
```

**Archivo: `nhibernate/filtering/Sorting.cs`**

```csharp
namespace MiProyecto.infrastructure.nhibernate.filtering;

public class Sorting
{
    public Sorting() { }

    public Sorting(string by, string direction)
    {
        By = by;
        Direction = direction;
    }

    public string By
    {
        get => string.IsNullOrEmpty(_by) ? _by : _by.ToPascalCase();
        set => _by = value;
    }
    private string _by = string.Empty;

    public string Direction { get; set; } = string.Empty;

    public bool IsValid()
        => !string.IsNullOrEmpty(By) && !string.IsNullOrEmpty(Direction);
}
```

**Archivo: `nhibernate/filtering/QueryOperations.cs`**

```csharp
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Specialized;

namespace MiProyecto.infrastructure.nhibernate.filtering;

/// <summary>
/// Provides utility methods for manipulating query strings and parsing expressions.
/// </summary>
public static class QueryOperations
{
    /// <summary>
    /// Adds or updates the OrganizationId parameter in a query string.
    /// If OrganizationId already exists, it combines the values with 'and' operator.
    /// </summary>
    /// <param name="query">The original query string</param>
    /// <param name="organizationId">The organization ID to add</param>
    /// <returns>The modified query string</returns>
    public static string AddOrganizationIdToQuery(string? query, string organizationId)
    {
        if (query == null)
            query = string.Empty;

        var updatedQuery = QueryHelpers.ParseQuery(query);
        var queryDict = updatedQuery.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        if (queryDict.ContainsKey("OrganizationId"))
        {
            queryDict["OrganizationId"] = $"{queryDict["user"]} and {organizationId}||eq";
        }
        else
        {
            queryDict["OrganizationId"] = $"{organizationId}||eq";
        }
        return QueryHelpers.AddQueryString(string.Empty, queryDict);
    }

    /// <summary>
    /// Parses query string parameters into a dynamic LINQ expression.
    /// Supports operators: eq, contains, between, greater_than, less_than, starts_with, ends_with
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="parameters">The query string parameters collection</param>
    /// <returns>A LINQ expression string</returns>
    public static string ParseExpression<T>(NameValueCollection parameters)
    {
        var parser = new FilterExpressionParser();
        return parser.Parse<T>(parameters);
    }
}
```

**Archivo: `nhibernate/SortingCriteriaExtender.cs`**

```csharp
using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Extension methods for SortingCriteria class.
/// Provides conversion from SortingCriteria objects to LINQ Dynamic expressions.
/// </summary>
public static class SortingCriteriaExtender
{
    /// <summary>
    /// Converts a SortingCriteria object to a LINQ Dynamic ordering expression.
    /// </summary>
    /// <param name="sort">The sorting criteria to convert</param>
    /// <returns>A string expression compatible with LINQ Dynamic (e.g., "Name" or "Name descending")</returns>
    public static string ToExpression(this SortingCriteria sort)
    {
        string orderExpression = sort.Criteria == SortingCriteriaType.Ascending
            ? $"{sort.SortBy}"
            : $"{sort.SortBy} descending";
        return orderExpression;
    }
}
```

**Archivo: `nhibernate/filtering/QueryStringParser.cs`**

```csharp
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace MiProyecto.infrastructure.nhibernate.filtering;

public class QueryStringParser
{
    public const int DEFAULT_PAGE_NUMBER = 1;
    public const int DEFAULT_PAGE_SIZE = 25;

    private const string _pageNumber = "pageNumber";
    private const string _pageSize = "pageSize";
    private const string _sortBy = "sortBy";
    private const string _sortDirection = "sortDirection";
    private const string _query = "query";
    private const string _query_ColumnsToSearch = "query_ColumnsToSearch";
    private readonly string _queryString;
    private readonly string _descending = "desc";
    private readonly string _ascending = "asc";
    private readonly string[] _excludedKeys = new string[] { _pageNumber, _pageSize, _sortBy, _sortDirection, _query };

    public QueryStringParser(string queryString)
    {
        _queryString = HttpUtility.UrlDecode(queryString);
    }

    public static string GetDescendingValue() => "desc";
    public static string GetAscendingValue() => "asc";

    public int ParsePageNumber()
    {
        int pageNumber = DEFAULT_PAGE_NUMBER;
        if (string.IsNullOrEmpty(_queryString))
            return pageNumber;

        QueryStringArgs parameters = new(_queryString);
        if (parameters.ContainsKey(_pageNumber) && !int.TryParse(parameters[_pageNumber], out pageNumber))
            throw new InvalidQueryStringArgumentException(_pageNumber);

        if (pageNumber < 0)
            throw new InvalidQueryStringArgumentException(_pageNumber);
        return pageNumber;
    }

    public int ParsePageSize()
    {
        int pageSize = DEFAULT_PAGE_SIZE;
        if (string.IsNullOrEmpty(_queryString))
            return pageSize;
        QueryStringArgs parameters = new(_queryString);

        if (parameters.ContainsKey(_pageSize) && !int.TryParse(parameters[_pageSize], out pageSize))
            throw new InvalidQueryStringArgumentException(_pageSize);

        if (pageSize <= 0)
            throw new InvalidQueryStringArgumentException(_pageSize);
        return pageSize;
    }

    public Sorting ParseSorting<T>(string defaultFieldName)
    {
        string? sortByField = defaultFieldName;
        string? sortDirection = _ascending;

        QueryStringArgs parameters = new(_queryString);
        if (parameters.TryGetValue(_sortBy, out var sortByValue))
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            if (!properties.Any(p => p.Name.Equals(sortByValue, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidQueryStringArgumentException(_sortBy);
            sortByField = sortByValue;
        }
        if (parameters.TryGetValue(_sortDirection, out var sortDirectionValue))
        {
            if (sortDirectionValue != _descending && sortDirectionValue != _ascending)
                throw new InvalidQueryStringArgumentException(_sortDirection);
            sortDirection = sortDirectionValue;
        }

        if (sortByField == null || sortDirection == null)
            throw new InvalidQueryStringArgumentException("Sorting parameters cannot be null");

        return new Sorting(sortByField, sortDirection);
    }

    public IList<FilterOperator> ParseFilterOperators<T>()
    {
        IList<FilterOperator> filterOperatorsResult = new List<FilterOperator>();
        QueryStringArgs parameters = new(_queryString);
        IEnumerable<KeyValuePair<string, string>> allFilters = parameters.Where(parameter => !_excludedKeys.Contains(parameter.Key));
        foreach (var filter in allFilters)
        {
            string[] filterData = filter.Value.Split("||");
            string[] filterValues = filterData[0].Split("|");
            var fileterOperator = filterData[1];
            var operatorFieldName = filter.Key.ToPascalCase();
            filterOperatorsResult.Add(new FilterOperator(operatorFieldName, filterValues, fileterOperator));
        }
        return filterOperatorsResult;
    }

    public QuickSearch? ParseQuery<T>()
    {
        string? query = string.Empty;
        IList<string> fields = new List<string>();
        QuickSearch quickSearch = new();

        if (string.IsNullOrEmpty(_queryString))
            return null;

        QueryStringArgs parameters = new(_queryString);

        if (!parameters.ContainsKey(_query))
            return null;

        if (!parameters.Any() && string.IsNullOrWhiteSpace(parameters[_query]))
            return null;

        query = parameters[_query].Split("||").FirstOrDefault();

        if (string.IsNullOrEmpty(query))
            throw new InvalidQueryStringArgumentException(_query);

        PropertyInfo[] properties = typeof(T).GetProperties();

        if (parameters[_query].Split("||").Count() <= 1)
        {
            ICollection<string> stringFields = new List<string>();
            quickSearch.Value = parameters[_query];

            foreach (PropertyInfo property in properties)
                if ((property.PropertyType == typeof(string) || property.PropertyType == typeof(int)) && property.Name != "Id")
                    stringFields.Add(property.Name);

            quickSearch.FieldNames = stringFields.ToList();
            return quickSearch;
        }

        quickSearch.Value = query;

        if (string.IsNullOrWhiteSpace(parameters[_query].Split("||")[1]))
            throw new InvalidQueryStringArgumentException(_query_ColumnsToSearch);

        fields = parameters[_query].Split("||")[1].Split("|");

        foreach (string field in fields)
            if (!properties.Any(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidQueryStringArgumentException(_query_ColumnsToSearch);

        quickSearch.FieldNames = fields;
        return quickSearch;
    }
}

internal class QueryStringArgs : Dictionary<string, string>
{
    private const string Pattern = @"(?<argName>\w+)=(?<argValue>.+)";
    private readonly Regex _regex = new(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public bool ContainsValidArguments()
    {
        return (this.ContainsKey("cnn"));
    }

    public QueryStringArgs(string query)
    {
        var args = query.Split('&');
        foreach (var match in args.Select(arg => _regex.Match(arg)).Where(m => m.Success))
        {
            try
            {
                this.Add(match.Groups["argName"].Value, match.Groups["argValue"].Value);
            }
            catch
            {
                // Continues execution
            }
        }
    }
}
```

**Archivo: `nhibernate/filtering/FilterExpressionParser.cs`**

```csharp
using MiProyecto.domain.exceptions;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace MiProyecto.infrastructure.nhibernate.filtering;

public static class FilterExpressionParser
{
    static public Expression<Func<T, bool>> ParsePredicate<T>(IEnumerable<FilterOperator> operands)
    {
        var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());

        List<Expression> allCritera = new List<Expression>();
        foreach (FilterOperator filter in operands)
        {
            string propertyName = filter.FieldName.ToPascalCase();
            Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
            IList<string> filterValues = filter.Values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            if (!filterValues.Any())
                throw new ArgumentException("Error getting valid filter values");

            string firstFilter = filterValues[0];
            Expression? criteria = null;
            switch (filter.RelationalOperatorType)
            {
                case RelationalOperator.Contains:
                    propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                    var constant = Expression.Constant(filterValues.FirstOrDefault());
                    MethodInfo? strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
                    if (strContainsMethod != null)
                    {
                        var expressions = new Expression[] { constant };
                        criteria = Expression.Call(propertyExpression, strContainsMethod, expressions);
                    }
                    break;
                case RelationalOperator.GreaterThan:
                    var constantExpression01 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.GreaterThan(propertyExpression, constantExpression01);
                    break;
                case RelationalOperator.GreaterThanOrEqual:
                    var constantExpression02 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.GreaterThanOrEqual(propertyExpression, constantExpression02);
                    break;
                case RelationalOperator.LessThan:
                    var constantExpression03 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.LessThan(propertyExpression, constantExpression03);
                    break;
                case RelationalOperator.LessThanOrEqual:
                    var constantExpression04 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.LessThanOrEqual(propertyExpression, constantExpression04);
                    break;
                case RelationalOperator.Between:
                    if (filterValues.Count < 2)
                        throw new InvalidFilterArgumentException($"Between operator requires two values, but only one was provided for property {propertyName}");
                    var secondFilter = filterValues[1];
                    if (string.IsNullOrWhiteSpace(secondFilter))
                        throw new InvalidFilterArgumentException($"Second value for Between operator cannot be null or empty for property {propertyName}");

                    var lowerLimitExpression = CreateConstantExpression<T>(propertyName, firstFilter);
                    var upperLimitExpression = CreateConstantExpression<T>(propertyName, secondFilter);
                    Expression lowerLimitCriteria = Expression.GreaterThanOrEqual(propertyExpression, lowerLimitExpression);
                    Expression upperLimitCriteria = Expression.LessThanOrEqual(propertyExpression, upperLimitExpression);
                    criteria = Expression.AndAlso(lowerLimitCriteria, upperLimitCriteria);
                    break;
                default:
                    propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                    MethodInfo? arrContainsMethod = filterValues.GetType().GetMethod(nameof(filterValues.Contains), new Type[] { typeof(string) });
                    if (arrContainsMethod != null)
                        criteria = Expression.Call(Expression.Constant(filterValues), arrContainsMethod, propertyExpression);
                    break;
            }
            if (criteria != null)
                allCritera.Add(criteria);
        }

        if (!allCritera.Any())
            return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), parameterExpression);

        Expression? expression = null;
        foreach (Expression criteria in allCritera)
            expression = expression != null ? Expression.AndAlso(expression, criteria) : criteria;
        if (expression == null)
            throw new ArgumentException("No valid criteria found for the filter operands");

        var lambda = Expression.Lambda<Func<T, bool>>(expression, parameterExpression);
        return lambda;
    }

    static public Expression<Func<T, bool>> ParseQueryValuesToExpression<T>(this Expression<Func<T, bool>> expression, QuickSearch quickSearch)
    {
        Expression<Func<T, bool>> expressionJoin = c => true;
        int index = 0;
        foreach (var propertyName in quickSearch.FieldNames)
        {
            Expression? criteria = null;
            var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());
            Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
            propertyExpression = FilterExpressionParser.CallToStringMethod<T>(propertyExpression, propertyName);
            var constant = Expression.Constant(quickSearch.Value);
            var strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            if (strContainsMethod != null)
            {
                criteria = Expression.Call(propertyExpression, strContainsMethod, new Expression[] { constant });
            }
            if (criteria != null)
            {
                var localExpressionJoin = Expression.Lambda<Func<T, bool>>(criteria, parameterExpression);
                expressionJoin = index == 0 ? localExpressionJoin : ConcatExpressionsOrElse<T>(expressionJoin, localExpressionJoin);
            }
            index++;
        }
        expression = ConcatExpressionsAndAlso<T>(expression, expressionJoin);
        return expression;
    }

    private static Expression<Func<T, bool>> ConcatExpressionsAndAlso<T>(this Expression<Func<T, bool>> expr1,
                                                 Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
              (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }

    private static Expression<Func<T, bool>> ConcatExpressionsOrElse<T>(this Expression<Func<T, bool>> expr1,
                                                 Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
              (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }

    private static Expression CallToStringMethod<T>(Expression propertyExpression, string propertyName)
    {
        PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName)
            ?? throw new ArgumentNullException($"No property found with name [{propertyName}]");
        if (!propertyInfo.PropertyType.Equals(typeof(string)))
            return Expression.Call(propertyExpression, "ToString", Type.EmptyTypes);

        return propertyExpression;
    }

    private static Expression CreateConstantExpression<T>(string propertyName, string constantValue)
    {
        PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName)
        ?? throw new ArgumentNullException($"No property found with name [{propertyName}]");

        var actualType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        if (actualType == typeof(string))
            return Expression.Constant(constantValue, propertyInfo.PropertyType);

        if (actualType == typeof(DateTime))
        {
            if (DateTime.TryParseExact(constantValue,
                new[] { "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dateValue))
            {
                dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                return Expression.Constant(dateValue, propertyInfo.PropertyType);
            }
            throw new InvalidFilterArgumentException(
                $"Invalid date format. Use yyyy-MM-dd. Value provided: {constantValue} for property {propertyName}",
                propertyName);
        }

        object convertedValue = actualType.IsEnum
            ? Enum.Parse(actualType, constantValue)
            : Convert.ChangeType(constantValue, actualType);

        return Expression.Constant(convertedValue, propertyInfo.PropertyType);
    }
}
```

#### 7.6 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests"
dotnet new nunit -n MiProyecto.infrastructure.tests -o "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj"
```

#### 7.7 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.infrastructure.tests/MiProyecto.infrastructure.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 7.8 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package FluentAssertions
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package FluentValidation
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Microsoft.Extensions.Configuration.Json
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Microsoft.Extensions.DependencyInjection
```

#### 7.9 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 7.10 Crear archivo de test base

**Archivo: `tests/MiProyecto.infrastructure.tests/nhibernate/NHRepositoryTestBase.cs`**

```csharp
namespace MiProyecto.infrastructure.tests.nhibernate;

public class NHRepositoryTestBase
{
    [SetUp]
    public void Setup()
    {
    }
}
```

---

### PASO 8: Crear Proyecto WebApi (Presentation)

#### 8.1 Crear proyecto webapi

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.webapi"
dotnet new webapi -n MiProyecto.webapi -o "C:\projects\miproyecto\src\MiProyecto.webapi"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj"
```

#### 8.2 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package AutoMapper
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints.Swagger
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints.Security
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Microsoft.Extensions.Logging.Log4Net.AspNetCore
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Swashbuckle.AspNetCore
```

#### 8.3 Agregar referencias

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 8.4 Crear estructura de carpetas

```
src/MiProyecto.webapi/
├── features/
│   └── hello/
├── infrastructure/
├── mappingprofiles/
├── dtos/
└── properties/
```

#### 8.5 Crear Program.cs

Reemplazar el contenido de `Program.cs`:

```csharp
using FastEndpoints;
using FastEndpoints.Swagger;
using MiProyecto.webapi.infrastructure;

// Load environment variables from .env file
// This is necessary to ensure that the connection string and other settings are available
DotNetEnv.Env.Load();

IConfiguration configuration;

var builder = WebApplication.CreateBuilder(args);
configuration = builder.Configuration;
var environment = builder.Environment;

builder.Services
    .AddSwaggerGen()
    .AddEndpointsApiExplorer()
    .ConfigurePolicy()
    .ConfigureCors(configuration)
    .ConfigureIdentityServerClient(configuration)
    .ConfigureUnitOfWork(configuration)
    .ConfigureAutoMapper()
    .ConfigureValidators()
    .AddLogging()
    .AddAuthorization()
    .AddFastEndpoints()
    .SwaggerDocument();

var app = builder.Build();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("CorsPolicy")
    .UseHttpsRedirection()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UseFastEndpoints()
    .UseSwagger()
    .UseSwaggerUI(opt =>
    {
        opt.DefaultModelsExpandDepth(-1); // Hide schemas by default
        opt.DisplayRequestDuration();
        opt.EnableTryItOutByDefault();
    });

await app.RunAsync();

// Make Program accessible to tests
public partial class Program { }
```

#### 8.6 Crear archivos clave

**Archivo: `infrastructure/ServiceCollectionExtender.cs`**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AutoMapper;

namespace MiProyecto.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    public static IServiceCollection ConfigurePolicy(this IServiceCollection services)
    {
        // Configurar políticas de autorización
        return services;
    }

    public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        return services;
    }

    public static IServiceCollection ConfigureIdentityServerClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Configurar JWT
            });
        return services;
    }

    public static IServiceCollection ConfigureUnitOfWork(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar Unit of Work
        return services;
    }

    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            // Add profiles here if needed
        }, typeof(ServiceCollectionExtender).Assembly);
        return services;
    }

    public static IServiceCollection ConfigureValidators(this IServiceCollection services)
    {
        // Registrar validadores
        return services;
    }
}
```

**Archivo: `properties/InternalsVisibleTo.cs`**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MiProyecto.webapi.tests")]
```

**Archivo: `mappingprofiles/MappingProfile.cs`**

```csharp
using AutoMapper;

namespace MiProyecto.webapi.mappingprofiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Configurar mapeos aquí
    }
}
```

**Archivo: `dtos/GetManyAndCountResultDto.cs`**

```csharp
namespace MiProyecto.webapi.dtos;

public class GetManyAndCountResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Count { get; set; }
}
```

**Archivo: `features/hello/HelloEndpoint.cs`**

```csharp
using FastEndpoints;

namespace MiProyecto.webapi.features.hello;

public class HelloEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/api/hello");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendStringAsync("Hello from FastEndpoints!", cancellation: ct);
    }
}
```

#### 8.7 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.webapi.tests"
dotnet new nunit -n MiProyecto.webapi.tests -o "C:\projects\miproyecto\tests\MiProyecto.webapi.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj"
```

#### 8.8 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.webapi.tests/MiProyecto.webapi.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 8.9 Instalar paquetes en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package AutoMapper
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package FastEndpoints.Testing
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package FluentAssertions
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package Moq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package Microsoft.AspNetCore.Mvc.Testing
```

#### 8.10 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj"
```

#### 8.11 Crear archivos de tests

**Archivo: `tests/MiProyecto.webapi.tests/CustomWebApplicationFactory.cs`**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiProyecto.webapi.tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configurar servicios para testing
        });
    }
}
```

**Archivo: `tests/MiProyecto.webapi.tests/EndpointTestBase.cs`**

```csharp
namespace MiProyecto.webapi.tests;

public class EndpointTestBase
{
    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var factory = new CustomWebApplicationFactory<Program>();
        Client = factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Client?.Dispose();
    }
}
```

**Archivo: `tests/MiProyecto.webapi.tests/TestAuthHandler.cs`**

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MiProyecto.webapi.tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "Test user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

**Archivos en `mappingprofiles/`:**

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/AutoFixtureExtensions.cs`**

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;

namespace MiProyecto.webapi.tests.mappingprofiles;

public static class AutoFixtureExtensions
{
    public static IFixture WithoutRecursion(this IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    public static IFixture WithAutoMoq(this IFixture fixture)
    {
        return fixture.Customize(new AutoMoqCustomization());
    }
}
```

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/MappingProfileTestsBase.cs`**

```csharp
using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace MiProyecto.webapi.tests.mappingprofiles;

public abstract class MappingProfileTestsBase
{
    protected internal IFixture fixture;
    protected internal MapperConfiguration configuration;
    protected internal IMapper mapper;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.fixture = new Fixture()
            .WithAutoMoq()
            .WithoutRecursion();

        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        this.configuration = new MapperConfiguration(mc =>
        {
            ConfigureProfiles(mc);
        }, factory);
        this.mapper = this.configuration.CreateMapper();
    }

    [Test]
    public void MappingConfiguration_ShouldBeValid()
    {
        configuration.AssertConfigurationIsValid();
    }

    protected abstract void ConfigureProfiles(IMapperConfigurationExpression configuration);
}
```

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/MappingProfileTests.cs`**

```csharp
using AutoMapper;
using MiProyecto.webapi.mappingprofiles;

namespace MiProyecto.webapi.tests.mappingprofiles;

public class MappingProfileTests : MappingProfileTestsBase
{
    protected override void ConfigureProfiles(IMapperConfigurationExpression configuration)
        => configuration.AddProfile(new MappingProfile());
}
```

---

### PASO 9: Crear Proyecto Scenarios

El proyecto Scenarios es una aplicación de consola que genera archivos XML con datos de prueba. Estos archivos son utilizados por NDbUnit para cargar escenarios en las pruebas de integración.

#### 9.1 Crear proyecto console

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet new console -n MiProyecto.scenarios -o "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj"
```

#### 9.2 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Scrutor
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Spectre.Console
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Microsoft.Extensions.DependencyInjection
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Microsoft.Extensions.Logging
```

#### 9.3 Agregar referencias

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
```

#### 9.4 Crear Program.cs

```csharp
using Spectre.Console;
using System.Data;
using System.Text;
using MiProyecto.scenarios;

Console.OutputEncoding = Encoding.UTF8;
ScenarioBuilder builder;

try
{
    // Read the command line parameters
    AnsiConsole.MarkupLine("Reading command line parameters...");
    CommandLineArgs parameter = [];

    if (!parameter.TryGetValue("cnn", out string? connectionStringValue))
        throw new ArgumentException("No [cnn] parameter received. You need pass the connection string in order to execute the scenarios");

    if (!parameter.TryGetValue("output", out string? outputValue))
        throw new ArgumentException("No [output] parameter received. You need pass the output folder path in order to save the scenarios");

    if (!Directory.Exists(outputValue))
    {
        try
        {
            Directory.CreateDirectory(outputValue);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"The output folder path [{outputValue}] is not valid. Error: {ex.Message}");
        }
    }

    // Synchronous
    AnsiConsole.Status()
    .AutoRefresh(true)
        .Spinner(Spinner.Known.Default)
        .Start("[yellow]Reading assemblies from assemblies...[/]", ctx =>
        {
            AnsiConsole.MarkupLine("[grey]LOG:[/] Loading scenarios[grey]...[/]");
            builder = new ScenarioBuilder(connectionStringValue);

            // Simulate some work
            foreach (var scenario in builder.Scenarios)
            {
                var scenarioName = scenario.ScenarioFileName;
                AnsiConsole.MarkupLine($"[grey]LOG:[/] Creating scenario {scenarioName}...");

                //Clear the database
                ctx.Status = "[yellow]Cleaning database...[/]";
                builder.NDbUnitTest.ClearDatabase();

                //Check if the scenario has a preload scenario and load it
                if (scenario.PreloadScenario != null)
                {
                    ctx.Status = "[yellow]Preloading scenario required...[/]";
                    builder.LoadXmlFile(scenario.PreloadScenario, outputValue);
                }

                //Seed the scenario and save it in a xml file
                ctx.Status = "[yellow]Creating scenario...[/]";
                scenario.SeedData().Wait();
                ctx.Status = "[yellow]Saving scenario in file system...[/]";
                string filePath = Path.Combine(outputValue, $"{scenarioName}.xml");
                DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();
                dataSet.WriteXml(filePath);
            }
        });

    AnsiConsole.MarkupLine("[green3_1]Scenario loading completed[/]");
    return (int)ExitCode.Success;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return (int)ExitCode.UnknownError;
}
```

#### 9.5 Crear archivos de soporte

**IScenario.cs** - Interface que define la estructura de un escenario:

```csharp
namespace MiProyecto.scenarios;

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

**CommandLineArgs.cs** - Parser de argumentos de línea de comandos:

```csharp
using System.Text.RegularExpressions;

namespace MiProyecto.scenarios;

/// <summary>
/// Dictionary with input parameters of console application
/// </summary>
internal class CommandLineArgs : Dictionary<string, string>
{
    private const string Pattern = @"\/(?<argname>\w+):(?<argvalue>.+)";
    private readonly Regex _regex = new(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

    /// <summary>
    /// Determine if the user pass at least one valid parameter
    /// </summary>
    public bool ContainsValidArguments()
    {
        return (this.ContainsKey("cnn"));
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public CommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();

        if (args == null || args.Length == 0)
            return;

        foreach (var groups in args.Select(arg => _regex.Match(arg)).Where(match => match.Success).Select(match => match.Groups))
        {
            this.Add(groups["argname"].Value, groups["argvalue"].Value);
        }
    }
}
```

**ExitCode.cs** - Códigos de salida del programa:

```csharp
namespace MiProyecto.scenarios;

/// <summary>
/// Enumerate the exit codes
/// </summary>
public enum ExitCode
{
    Success = 0,
    UnknownError = 1
}
```

**ScenarioBuilder.cs** - Constructor principal que configura DI y carga escenarios:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using System.Reflection;
using MiProyecto.domain.interfaces.repositories;
using MiProyecto.common.tests;
using MiProyecto.infrastructure.nhibernate;
using MiProyecto.ndbunit;
using MiProyecto.domain.entities;
using MiProyecto.domain.entities.validators;

namespace MiProyecto.scenarios;

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
        // Registrar validators para cada entidad
        .AddTransient<AbstractValidator<Role>, RoleValidator>()
        .AddTransient<AbstractValidator<User>, UserValidator>()
        // Agregar más validators según las entidades del proyecto
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
```

#### 9.6 Crear escenarios de ejemplo

**Sc010CreateSandBox.cs** - Escenario base vacío (sandbox):

```csharp
namespace MiProyecto.scenarios;

public class Sc010CreateSandBox : IScenario
{
    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    public string ScenarioFileName => "CreateSandBox";

    /// <summary>
    /// No pre-load scenario for this scenario
    /// </summary>
    public Type? PreloadScenario => null;

    /// <summary>
    /// Seed data - Empty scenario for sandbox
    /// </summary>
    public Task SeedData()
        => Task.CompletedTask;
}
```

**Sc020CreateRoles.cs** - Escenario con inyección de dependencias:

```csharp
using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.scenarios;

public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    public string ScenarioFileName => "CreateRoles";

    /// <summary>
    /// Pre-load the sandbox scenario
    /// </summary>
    public Type? PreloadScenario => typeof(Sc010CreateSandBox);

    /// <summary>
    /// Seed data using the repository
    /// </summary>
    public Task SeedData()
    {
        return _uoW.Roles.CreateDefaultRoles();
    }
}
```

**Sc030CreateUsers.cs** - Escenario que depende de roles:

```csharp
using MiProyecto.domain.interfaces.repositories;

namespace MiProyecto.scenarios;

public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";

    /// <summary>
    /// Pre-load the roles scenario (which includes sandbox)
    /// </summary>
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        await _uoW.Users.CreateAsync("usuario1@example.com", "Usuario Uno");
        await _uoW.Users.CreateAsync("usuario2@example.com", "Usuario Dos");
    }
}
```

#### 9.7 Ejecutar el generador de escenarios

Para generar los archivos XML de escenarios:

```bash
cd "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet run /cnn:"Host=localhost;Port=5432;Database=miproyecto_test;Username=postgres;Password=secret" /output:"C:\projects\miproyecto\tests\scenarios"
```

Esto generará archivos XML en la carpeta especificada:
- `CreateSandBox.xml`
- `CreateRoles.xml`
- `CreateUsers.xml`

Estos archivos son utilizados por `LoadScenario()` en las pruebas de integración

---

## Detalles de Cada Proyecto

### Domain (MiProyecto.domain)

- **Tipo**: Class Library
- **Propósito**: Contiene la lógica de negocio, entidades de dominio, interfaces de repositorios y excepciones personalizadas
- **Dependencias**: FluentValidation
- **Sin dependencias de otros proyectos**

### Application (MiProyecto.application)

- **Tipo**: Class Library
- **Propósito**: Casos de uso y lógica de aplicación usando FastEndpoints
- **Dependencias**: FastEndpoints
- **Referencia a**: Domain

### Infrastructure (MiProyecto.infrastructure)

- **Tipo**: Class Library
- **Propósito**: Implementaciones de infraestructura (repositorios con NHibernate, UnitOfWork)
- **Dependencias**: FluentValidation, NHibernate, System.Linq.Dynamic.Core, Microsoft.AspNetCore.WebUtilities
- **Referencia a**: Domain, NDbUnit, Common.Tests

### WebApi (MiProyecto.webapi)

- **Tipo**: Web API
- **Propósito**: Capa de presentación, endpoints REST con FastEndpoints
- **Dependencias**: AutoMapper, DotNetEnv, FastEndpoints, FastEndpoints.Swagger, FastEndpoints.Security, JWT, Log4Net, Swashbuckle
- **Referencia a**: Domain, Infrastructure

### Migrations (MiProyecto.migrations)

- **Tipo**: Console Application
- **Propósito**: Gestión de migraciones de base de datos con FluentMigrator
- **Dependencias**: FluentMigrator, FluentMigrator.Runner, DependencyInjection, Spectre.Console
- **Sin referencias a otros proyectos**

### NDbUnit (MiProyecto.ndbunit)

- **Tipo**: Class Library
- **Propósito**: Utilidad para cargar/limpiar datos de prueba en la base de datos
- **Dependencias**: Npgsql (PostgreSQL) o Microsoft.Data.SqlClient (SQL Server)
- **Sin referencias a otros proyectos**

### Common.Tests (MiProyecto.common.tests)

- **Tipo**: Class Library
- **Propósito**: Código común compartido entre proyectos de test (schemas XSD)
- **Referencia a**: Domain

### Scenarios (MiProyecto.scenarios)

- **Tipo**: Console Application
- **Propósito**: Generador de escenarios de datos de prueba exportados a XML
- **Dependencias**: Scrutor, Spectre.Console, DependencyInjection, Logging
- **Referencia a**: Domain, Infrastructure, NDbUnit

### Proyectos de Tests

Cada capa tiene su proyecto de tests correspondiente con NUnit, AutoFixture, FluentAssertions y las referencias necesarias.

---

## Referencias Completas de Paquetes NuGet

### Versiones Centralizadas (Directory.Packages.props)

| Paquete                                         | Versión |
| ----------------------------------------------- | ------- |
| AutoFixture.AutoMoq                             | 4.18.1  |
| AutoMapper                                      | 15.0.1  |
| coverlet.collector                              | 6.0.2   |
| DotNetEnv                                       | 3.1.1   |
| FastEndpoints                                   | 7.0.1   |
| FastEndpoints.Security                          | 7.0.1   |
| FastEndpoints.Swagger                           | 7.0.1   |
| FastEndpoints.Testing                           | 7.0.1   |
| FluentAssertions                                | 8.5.0   |
| FluentMigrator                                  | 7.1.0   |
| FluentMigrator.Runner                           | 7.1.0   |
| FluentValidation                                | 12.0.0  |
| Microsoft.AspNetCore.Authentication.JwtBearer   | 9.0.7   |
| Microsoft.AspNetCore.Mvc.Testing                | 9.0.7   |
| Microsoft.AspNetCore.OpenApi                    | 9.0.5   |
| Microsoft.AspNetCore.WebUtilities               | 9.0.7   |
| Microsoft.Data.SqlClient                        | 5.2.2   |
| Microsoft.Extensions.Configuration.Json         | 9.0.7   |
| Microsoft.Extensions.DependencyInjection        | 9.0.7   |
| Microsoft.Extensions.Logging                    | 9.0.7   |
| Microsoft.Extensions.Logging.Log4Net.AspNetCore | 8.0.0   |
| Microsoft.NET.Test.Sdk                          | 17.12.0 |
| Moq                                             | 4.20.72 |
| NHibernate                                      | 5.5.2   |
| Npgsql                                          | 9.0.3   |
| NUnit                                           | 4.2.2   |
| NUnit.Analyzers                                 | 4.4.0   |
| NUnit3TestAdapter                               | 4.6.0   |
| Scrutor                                         | 6.1.0   |
| Spectre.Console                                 | 0.50.0  |
| Swashbuckle.AspNetCore                          | 9.0.3   |
| System.Linq.Dynamic.Core                        | 1.6.7   |

---

## Templates y Archivos Generados

### Convención de Nombres de Templates

Los templates están organizados por namespace:

- `Templates.src.domain.*` - Archivos del dominio
- `Templates.src.infrastructure.*` - Archivos de infraestructura
- `Templates.src.webapi.*` - Archivos de WebAPI
- `Templates.tests.*` - Archivos de tests

### Reemplazos de Placeholders

En todos los templates se realizan los siguientes reemplazos:

| Placeholder                            | Descripción                             | Ejemplo                                                            |
| -------------------------------------- | --------------------------------------- | ------------------------------------------------------------------ |
| `{{ SOLUTION_NAME }}`                  | Nombre de la solución                   | `MiProyecto`                                                       |
| `{{ NDBUNIT_INSTANCE_NAME }}`          | Clase de NDbUnit según BD               | `PostgreSQLNDbUnit` o `SqlClienteNDbUnit`                          |
| `{{ CONNECTIONSTRINGBUILDER_METHOD }}` | Método para construir connection string | `BuildPostgresConnectionString` o `BuildSqlServerConnectionString` |
| `{{ DATABASE_DRIVER }}`                | Driver de NHibernate                    | `NpgsqlDriver` o `MicrosoftDataSqlClientDriver`                    |
| `{{ DATABASE_DIALECT }}`               | Dialecto de NHibernate                  | `PostgreSQL83Dialect` o `MsSql2012Dialect`                         |

### Valores según Base de Datos

**PostgreSQL:**

- NDBUNIT_INSTANCE_NAME: `PostgreSQLNDbUnit`
- CONNECTIONSTRINGBUILDER_METHOD: `BuildPostgresConnectionString`
- DATABASE_DRIVER: `NpgsqlDriver`
- DATABASE_DIALECT: `PostgreSQL83Dialect`

**SQL Server:**

- NDBUNIT_INSTANCE_NAME: `SqlClienteNDbUnit`
- CONNECTIONSTRINGBUILDER_METHOD: `BuildSqlServerConnectionString`
- DATABASE_DRIVER: `MicrosoftDataSqlClientDriver`
- DATABASE_DIALECT: `MsSql2012Dialect`

---

## Notas Importantes

### Gestión Centralizada de Paquetes

El archivo `Directory.Packages.props` habilita la gestión centralizada de versiones de paquetes NuGet. Esto significa:

- Todas las versiones se definen una sola vez en `Directory.Packages.props`
- Los proyectos referencian paquetes SIN especificar versión
- Al crear proyectos con `dotnet new`, se deben remover los atributos `Version` de los `PackageReference` generados automáticamente

### Orden de Creación

El orden de los pasos es importante debido a las dependencias entre proyectos:

1. Solution y Directory.Packages.props (base)
2. Migrations (independiente)
3. Domain (sin dependencias)
4. NDbUnit (independiente)
5. Common.Tests (depende de Domain)
6. Application (depende de Domain)
7. Infrastructure (depende de Domain, NDbUnit, Common.Tests)
8. WebApi (depende de Domain, Infrastructure)
9. Scenarios (depende de Domain, Infrastructure, NDbUnit)

### Configuración según Base de Datos

Algunos archivos cambian según la base de datos seleccionada:

- NDbUnit: Solo se crea la clase correspondiente (PostgreSQL o SQL Server)
- NHSessionFactory: Configurar driver y dialecto correcto
- ConnectionStringBuilder: Usar el método correcto
- Migrations Program.cs: Configurar el runner correcto

### Variables de Entorno

El proyecto usa archivos `.env` para configuración. Crear un archivo `.env.example` en la raíz de la solución:

#### Para PostgreSQL

**Archivo: `.env.example`**

```bash
# Database Configuration - PostgreSQL
DB_HOST=localhost
DB_PORT=5432
DB_NAME=miproyecto_db
DB_USER=postgres
DB_PASSWORD=your_password_here

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# JWT Settings (opcional)
JWT_SECRET=your_jwt_secret_key_here
JWT_ISSUER=MiProyecto
JWT_AUDIENCE=MiProyectoAPI
JWT_EXPIRATION_MINUTES=60

# Logging
LOG_LEVEL=Information
```

#### Para SQL Server

**Archivo: `.env.example`**

```bash
# Database Configuration - SQL Server
DB_SERVER=localhost
DB_NAME=miproyecto_db
DB_USER=sa
DB_PASSWORD=your_password_here
DB_INTEGRATED_SECURITY=false

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# JWT Settings (opcional)
JWT_SECRET=your_jwt_secret_key_here
JWT_ISSUER=MiProyecto
JWT_AUDIENCE=MiProyectoAPI
JWT_EXPIRATION_MINUTES=60

# Logging
LOG_LEVEL=Information
```

**IMPORTANTE**:

- Copiar `.env.example` a `.env` y configurar con valores reales
- Agregar `.env` al `.gitignore` para no versionar credenciales
- El archivo `.env.example` se versiona como plantilla

**Archivo: `.gitignore`** (agregar estas líneas)

```
# Environment variables
.env
.env.local
.env.*.local
```

---

## Verificación Final

Después de completar todos los pasos, verifica:

1. La solución compila sin errores: `dotnet build`
2. Todos los proyectos están agregados a la solución
3. Las referencias entre proyectos son correctas
4. Los paquetes NuGet están instalados
5. La estructura de carpetas coincide con lo esperado
6. Los archivos de templates están en su lugar

---

## Uso del Proyecto Generado

### Ejecutar Migraciones

```bash
cd src/MiProyecto.migrations
dotnet run -- cnn="tu_connection_string" action=run
```

### Ejecutar WebAPI

```bash
cd src/MiProyecto.webapi
dotnet run
```

### Generar Escenarios

```bash
cd tests/MiProyecto.scenarios
dotnet run -- cnn="tu_connection_string" output="./scenarios"
```

### Ejecutar Tests

```bash
dotnet test
```

---

**Documento generado por**: apsys.builder CLI
**Versión**: 1.0
**Fecha**: 2025
