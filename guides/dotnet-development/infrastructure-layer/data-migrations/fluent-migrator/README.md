# FluentMigrator - Setup y Configuración

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## 📋 Tabla de Contenidos
1. [¿Qué es FluentMigrator?](#qué-es-fluentmigrator)
2. [Instalación](#instalación)
3. [Estructura del Proyecto](#estructura-del-proyecto)
4. [Configuración](#configuración)
5. [Custom Version Table](#custom-version-table)
6. [Migration Runner](#migration-runner)
7. [Crear Migraciones](#crear-migraciones)
8. [Ejecutar Migraciones](#ejecutar-migraciones)
9. [Integración con CI/CD](#integración-con-cicd)
10. [Guías Disponibles](#guías-disponibles)
11. [Referencias](#referencias)

---

## ¿Qué es FluentMigrator?

**FluentMigrator** es una biblioteca .NET para gestionar **migraciones de base de datos** como código. Permite versionar y aplicar cambios de esquema de forma **automatizada, reversible y rastreable**.

### 🎯 Características Principales

- ✅ **ORM Agnostic**: Funciona con cualquier ORM (NHibernate, EF Core, Dapper)
- ✅ **Multi-database**: SQL Server, PostgreSQL, MySQL, SQLite, Oracle, etc.
- ✅ **Fluent API**: Sintaxis expresiva y legible
- ✅ **Versionado**: Control total de versiones de migraciones
- ✅ **Rollback**: Down() método para revertir cambios
- ✅ **Type-safe**: Fuertemente tipado, compile-time safety

---

## Instalación

### Paso 1: Crear Proyecto Console

Basado en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```bash
# Navegar a la carpeta src de tu solución
cd src

# Crear proyecto console para migraciones
dotnet new console -n {YourProject}.migrations

# Agregar proyecto a la solución
dotnet sln add src/{YourProject}.migrations/{YourProject}.migrations.csproj
```

---

### Paso 2: Instalar Paquetes NuGet

**Opción A: Usando Directory.Packages.props (Recomendado)**

```xml
<!-- Directory.Packages.props en la raíz de la solución -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="FluentMigrator" Version="7.1.0" />
    <PackageVersion Include="FluentMigrator.Runner" Version="7.1.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageVersion Include="Npgsql" Version="8.0.5" />
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
  </ItemGroup>
</Project>
```

**{YourProject}.migrations.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentMigrator" />
    <PackageReference Include="FluentMigrator.Runner" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Npgsql" />  <!-- Para PostgreSQL -->
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>
</Project>
```

---

**Opción B: Sin Directory.Packages.props**

```bash
cd src/{YourProject}.migrations

dotnet add package FluentMigrator --version 7.1.0
dotnet add package FluentMigrator.Runner --version 7.1.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0
dotnet add package Npgsql --version 8.0.5
dotnet add package Spectre.Console --version 0.49.1
```

---

### Paso 3: Configurar Providers por Base de Datos

| Base de Datos | Provider Package | Runner Method |
|---------------|------------------|---------------|
| **PostgreSQL** | `Npgsql` | `.AddPostgres11_0()` |
| **SQL Server** | `Microsoft.Data.SqlClient` | `.AddSqlServer()` |
| **MySQL** | `MySql.Data` | `.AddMySql5()` |
| **SQLite** | `Microsoft.Data.Sqlite` | `.AddSQLite()` |
| **Oracle** | `Oracle.ManagedDataAccess` | `.AddOracle()` |

**Ejemplo para SQL Server**:
```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.0" />
```

```csharp
.ConfigureRunner(rb => rb
    .AddSqlServer()  // ← Cambia de AddPostgres11_0() a AddSqlServer()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations())
```

---

## Estructura del Proyecto

Basado en el proyecto real:

```
solution/
├── src/
│   ├── {project}.migrations/
│   │   │
│   │   ├── Migrations/                        # ✅ Carpeta de migraciones (opcional)
│   │   │   ├── M001Sandbox.cs
│   │   │   ├── M024CreateUsersTable.cs
│   │   │   ├── M025TechnicalStandardsTable.cs
│   │   │   ├── M026TechnicalStandardsView.cs
│   │   │   └── M027CreatePrototypeTable.cs
│   │   │
│   │   ├── CustomVersionTableMetaData.cs     # ✅ Custom version tracking
│   │   ├── Program.cs                        # ✅ Migration runner
│   │   ├── CommandLineArgs.cs                # ✅ CLI argument parser (opcional)
│   │   └── {project}.migrations.csproj
│   │
│   ├── {project}.domain/
│   ├── {project}.infrastructure/
│   └── {project}.webapi/
│
└── Directory.Packages.props
```

**Nota**: Las migraciones pueden estar en la raíz del proyecto o en una carpeta `Migrations/` para mejor organización.

---

## Configuración

### 1. Custom Version Table Metadata

**CustomVersionTableMetaData.cs** (basado en proyecto real):

```csharp
using FluentMigrator.Runner.VersionTableInfo;

namespace hashira.stone.backend.migrations;

/// <summary>
/// Configura la tabla que FluentMigrator usa para rastrear qué migraciones se han aplicado.
/// </summary>
public class CustomVersionTableMetaData : IVersionTableMetaData
{
    public static string SchemaNameValue => "public";

    public required object ApplicationContext { get; set; }

    /// <summary>
    /// Define si FluentMigrator debe crear el esquema si no existe.
    /// </summary>
    public bool OwnsSchema => true;

    /// <summary>
    /// Nombre del esquema donde se creará la tabla de versiones.
    /// </summary>
    public string SchemaName => SchemaNameValue;

    /// <summary>
    /// Nombre de la tabla que rastrea las migraciones aplicadas.
    /// </summary>
    public string TableName => "versioninfo";

    /// <summary>
    /// Nombre de la columna que almacena el número de versión.
    /// </summary>
    public string ColumnName => "version";

    /// <summary>
    /// Nombre del índice único en la columna de versión.
    /// </summary>
    public string UniqueIndexName => "uc_version";

    /// <summary>
    /// Nombre de la columna que almacena la fecha de aplicación.
    /// </summary>
    public string AppliedOnColumnName => "appliedon";

    /// <summary>
    /// Nombre de la columna que almacena la descripción de la migración.
    /// </summary>
    public string DescriptionColumnName => "description";

    /// <summary>
    /// Define si la tabla debe tener una clave primaria.
    /// false = solo unique index (más eficiente para este caso de uso)
    /// </summary>
    public bool CreateWithPrimaryKey => false;
}
```

**Resultado en base de datos**:
```sql
-- Tabla creada automáticamente por FluentMigrator
CREATE TABLE public.versioninfo (
    version BIGINT NOT NULL,
    appliedon TIMESTAMP NOT NULL,
    description VARCHAR(1024)
);

CREATE UNIQUE INDEX uc_version ON public.versioninfo (version);
```

---

### 2. Command Line Arguments Parser (Opcional)

**CommandLineArgs.cs** (helper para parsear argumentos):

```csharp
namespace hashira.stone.backend.migrations;

/// <summary>
/// Parsea argumentos de línea de comando en formato key=value.
/// Ejemplo: dotnet run cnn="Host=localhost" action=rollback
/// </summary>
public class CommandLineArgs : Dictionary<string, string>
{
    public CommandLineArgs()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            var parts = arg.Split('=', 2);
            if (parts.Length == 2)
            {
                this[parts[0]] = parts[1];
            }
        }
    }
}
```

**Uso**:
```csharp
CommandLineArgs args = new();

if (args.TryGetValue("cnn", out string? connectionString))
{
    // Usar connection string
}

if (args.TryGetValue("action", out string? action))
{
    // run o rollback
}
```

---

### 3. Migration Runner - Program.cs

**Program.cs completo** (basado en proyecto real):

```csharp
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using hashira.stone.backend.migrations;

const string _run = "run";
const string _rollback = "rollback";

try
{
    // 1️⃣ Leer parámetros de línea de comando
    AnsiConsole.MarkupLine("Reading command line parameters...");
    CommandLineArgs parameter = [];

    if (!parameter.TryGetValue("cnn", out string? value))
        throw new ArgumentException("No [cnn] parameter received. You need pass the connection string in order to execute the migrations");

    // 2️⃣ Crear service provider con FluentMigrator
    AnsiConsole.MarkupLine("[bold yellow]Connecting to database...[/]");
    string connectionStringValue = value;
    var serviceProvider = CreateServices(connectionStringValue);

    using var scope = serviceProvider.CreateScope();
    var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

    // 3️⃣ Determinar acción (run o rollback)
    if (!parameter.TryGetValue("action", out string? action) && string.IsNullOrEmpty(action))
        action = _run;

    // 4️⃣ Ejecutar acción con Spectre.Console spinner
    if (action == _run)
    {
        AnsiConsole.Status()
            .Start("Start running migrations...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                ctx.Status("Running migrations...");
                runner.MigrateUp();
            });
        AnsiConsole.MarkupLine("[bold green]All migrations are updated[/]");
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
                if (lastMigration.Value != null)
                {
                    var rollBackToVersion = lastMigration.Value.Version - 1;
                    runner.MigrateDown(rollBackToVersion);
                }
            });
        AnsiConsole.MarkupLine("[bold blue]Last migration rolled back[/]");
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

/// <summary>
/// Configura Dependency Injection para FluentMigrator
/// </summary>
static IServiceProvider CreateServices(string? connectionString)
{
    return new ServiceCollection()
        // ✅ Agregar FluentMigrator Core
        .AddFluentMigratorCore()

        // ✅ Configurar runner
        .ConfigureRunner(rb => rb
            // Agregar soporte para PostgreSQL 11.0
            .AddPostgres11_0()

            // Connection string global
            .WithGlobalConnectionString(connectionString)

            // Escanear assembly para encontrar migraciones
            .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations())

        // ✅ Agregar logging de consola
        .AddLogging(lb => lb.AddFluentMigratorConsole())

        // ✅ Build service provider
        .BuildServiceProvider(false);
}

/// <summary>
/// Exit codes para el proceso
/// </summary>
public enum ExitCode
{
    Success = 0,
    UnknownError = 1
}
```

**Alternativa sin Spectre.Console**:
```csharp
if (action == _run)
{
    Console.WriteLine("Running migrations...");
    runner.MigrateUp();
    Console.WriteLine("✅ All migrations applied");
}
else if (action == _rollback)
{
    Console.WriteLine("Rolling back last migration...");
    var lastMigration = runner.MigrationLoader.LoadMigrations().LastOrDefault();
    if (lastMigration.Value != null)
    {
        runner.MigrateDown(lastMigration.Value.Version - 1);
    }
    Console.WriteLine("✅ Rollback complete");
}
```

---

## Custom Version Table

### Registrar Custom Version Table Metadata

Si estás usando custom version table, debes registrarla en DI:

```csharp
static IServiceProvider CreateServices(string? connectionString)
{
    return new ServiceCollection()
        .AddFluentMigratorCore()
        .ConfigureRunner(rb => rb
            .AddPostgres11_0()
            .WithGlobalConnectionString(connectionString)
            .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations()

            // ✅ Registrar custom version table metadata
            .WithVersionTable(new CustomVersionTableMetaData()))

        .AddLogging(lb => lb.AddFluentMigratorConsole())
        .BuildServiceProvider(false);
}
```

**O usando generic registration**:
```csharp
.ConfigureRunner(rb => rb
    .AddPostgres11_0()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations())

// Registrar metadata como servicio
.AddScoped<IVersionTableMetaData, CustomVersionTableMetaData>()
```

---

## Crear Migraciones

### Anatomía de una Migración

```csharp
using FluentMigrator;

namespace hashira.stone.backend.migrations;

[Migration(27)]  // ← Número de versión único
public class M027CreatePrototypeTable : Migration
{
    // ✅ Constantes para nombres de tablas
    private readonly string _tableName = "prototypes";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    /// <summary>
    /// Aplicar cambios (crear tabla)
    /// </summary>
    public override void Up()
    {
        Create.Table(_tableName)
            .InSchema(_schemaName)
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("number").AsString(50).NotNullable().Unique()
            .WithColumn("issue_date").AsDateTime().NotNullable()
            .WithColumn("expiration_date").AsDateTime().NotNullable()
            .WithColumn("status").AsString(20).NotNullable();
    }

    /// <summary>
    /// Revertir cambios (eliminar tabla)
    /// </summary>
    public override void Down()
    {
        Delete.Table(_tableName)
            .InSchema(_schemaName);
    }
}
```

---

### Convenciones de Nombres

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| **Clase** | `M{version}{Description}` | `M027CreatePrototypeTable` |
| **Versión** | Número secuencial | `[Migration(27)]` |
| **Namespace** | `{project}.migrations` | `hashira.stone.backend.migrations` |
| **Archivo** | Mismo nombre que clase | `M027CreatePrototypeTable.cs` |

---

### Ejemplos de Migraciones

**Crear tabla simple**:
```csharp
[Migration(24)]
public class M024CreateUsersTable : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_tableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("email").AsString().NotNullable().Unique()
              .WithColumn("name").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table(_tableName).InSchema(_schemaName);
    }
}
```

---

**Crear vista SQL**:
```csharp
[Migration(26)]
public class M026TechnicalStandardsView : Migration
{
    private readonly string _viewName = "technical_standards_view";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        var fullViewName = $"{_schemaName}.{_viewName}";
        var sql = $@"
            CREATE OR REPLACE VIEW {fullViewName} AS
            SELECT
                id,
                code,
                creation_date,
                name,
                edition,
                status,
                type,
                lower(unaccent(code || ' ' || name || ' ' || edition)) as search_all
            FROM public.technical_standards;
        ";
        Execute.Sql(sql);
    }

    public override void Down()
    {
        var fullViewName = $"{_schemaName}.{_viewName}";
        Execute.Sql($"DROP VIEW IF EXISTS {fullViewName};");
    }
}
```

---

## Ejecutar Migraciones

### Desarrollo Local

**Windows (PowerShell)**:
```powershell
# Navegar al proyecto de migraciones
cd src\hashira.stone.backend.migrations

# Ejecutar migraciones (run)
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass"

# Revertir última migración (rollback)
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass" action=rollback

# Ver ayuda
dotnet run --help
```

**Linux/macOS (Bash)**:
```bash
# Navegar al proyecto
cd src/hashira.stone.backend.migrations

# Run
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass"

# Rollback
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass" action=rollback
```

---

### Connection Strings por Ambiente

**Development (appsettings.Development.json)**:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=mydb_dev;Username=postgres;Password=dev123"
  }
}
```

**Staging (Environment Variable)**:
```bash
# Linux/macOS
export DB_CONNECTION_STRING="Host=staging-db.example.com;Database=mydb_staging;Username=app_user;Password=staging_pass"

# Windows PowerShell
$env:DB_CONNECTION_STRING="Host=staging-db.example.com;Database=mydb_staging;Username=app_user;Password=staging_pass"
```

**Production (Azure Key Vault o AWS Secrets Manager)**:
```bash
# Obtener de Key Vault
az keyvault secret show --name db-connection-string --vault-name myvault --query value -o tsv
```

---

### Verificar Migraciones Aplicadas

**SQL Query**:
```sql
-- Ver todas las migraciones aplicadas
SELECT * FROM public.versioninfo ORDER BY version DESC;

-- Resultado esperado:
-- version | appliedon           | description
-- --------|---------------------|---------------------------
-- 27      | 2025-11-14 10:30:00 | M027CreatePrototypeTable
-- 26      | 2025-11-10 09:15:00 | M026TechnicalStandardsView
-- 25      | 2025-11-08 14:20:00 | M025TechnicalStandardsTable
-- 24      | 2025-11-05 08:45:00 | M024CreateUsersTable
```

---

## Integración con CI/CD

### GitHub Actions

```yaml
name: Database Migrations

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  migrate:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Run migrations
      run: |
        cd src/YourProject.migrations
        dotnet run cnn="${{ secrets.DB_CONNECTION_STRING }}"
      env:
        DB_CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}
```

---

### Azure DevOps Pipeline

```yaml
trigger:
- main
- develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- script: dotnet restore
  displayName: 'Restore dependencies'

- script: |
    cd src/YourProject.migrations
    dotnet run cnn="$(DB_CONNECTION_STRING)"
  displayName: 'Run database migrations'
  env:
    DB_CONNECTION_STRING: $(DB_CONNECTION_STRING)
```

---

### Docker

**Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solución y proyectos
COPY ["src/YourProject.migrations/YourProject.migrations.csproj", "src/YourProject.migrations/"]
RUN dotnet restore "src/YourProject.migrations/YourProject.migrations.csproj"

COPY . .
WORKDIR "/src/src/YourProject.migrations"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=publish /app/publish .

# Ejecutar migraciones
ENTRYPOINT ["dotnet", "YourProject.migrations.dll"]
```

**docker-compose.yml**:
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: mydb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    ports:
      - "5432:5432"

  migrations:
    build: .
    depends_on:
      - postgres
    environment:
      DB_CONNECTION_STRING: "Host=postgres;Database=mydb;Username=postgres;Password=password"
    command: ["cnn=${DB_CONNECTION_STRING}"]
```

---

## Troubleshooting

### Error: "Could not load file or assembly FluentMigrator"

**Causa**: Version mismatch entre `FluentMigrator` y `FluentMigrator.Runner`.

**Solución**:
```bash
# Verificar versiones
dotnet list package

# Asegurar misma versión
dotnet add package FluentMigrator --version 7.1.0
dotnet add package FluentMigrator.Runner --version 7.1.0
```

---

### Error: "No migrations found"

**Causa**: FluentMigrator no encuentra las clases de migración.

**Solución**:
```csharp
// Verificar que el assembly se escanea correctamente
.ScanIn(typeof(M001Sandbox).Assembly).For.Migrations()

// O especificar assembly directamente
.ScanIn(Assembly.GetExecutingAssembly()).For.Migrations()
```

---

### Error: "Connection string not provided"

**Causa**: Falta parámetro `cnn` en línea de comando.

**Solución**:
```bash
# ✅ CORRECTO
dotnet run cnn="Host=localhost;Database=mydb"

# ❌ INCORRECTO
dotnet run
```

---

### Error: "VersionInfo table already exists"

**Causa**: Tabla de versiones ya existe con esquema diferente.

**Solución**:
```sql
-- Eliminar tabla de versiones existente (CUIDADO: perderás el historial)
DROP TABLE IF EXISTS public.versioninfo;

-- O renombrar la tabla existente
ALTER TABLE public.versioninfo RENAME TO versioninfo_backup;
```

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | FluentMigrator setup y configuración |
| [migration-patterns.md](./migration-patterns.md) | ⏳ Pendiente | Patrones de migración (tablas, índices, vistas, FK) |
| [best-practices.md](./best-practices.md) | ⏳ Pendiente | Best practices de FluentMigrator |

---

## Referencias

### 📚 Documentación Oficial

- [FluentMigrator Documentation](https://fluentmigrator.github.io/)
- [FluentMigrator GitHub](https://github.com/fluentmigrator/fluentmigrator)
- [FluentMigrator Wiki](https://github.com/fluentmigrator/fluentmigrator/wiki)
- [PostgreSQL with FluentMigrator](https://fluentmigrator.github.io/articles/runners/dotnet-fm.html)

### 🔗 Guías Relacionadas

- [Data Migrations Overview](../README.md) - Overview de migraciones
- [Migration Patterns](./migration-patterns.md) - Patrones de migración
- [Best Practices](./best-practices.md) - Best practices
- [Core Concepts](../../core-concepts.md) - Conceptos de Infrastructure

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-11-14 | Versión inicial de FluentMigrator setup guide |

---

**Siguiente**: [Migration Patterns](./migration-patterns.md) - Patrones de migración →
