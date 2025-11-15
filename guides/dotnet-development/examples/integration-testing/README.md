# Integration Testing with Scenarios System

**Estado:** ✅ Completado
**Versión:** 1.0.0
**Última actualización:** 2025-01-15

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Intención y Objetivo del Sistema](#intención-y-objetivo-del-sistema)
3. [Anatomía del Sistema de Scenarios](#anatomía-del-sistema-de-scenarios)
4. [Componentes Clave](#componentes-clave)
5. [Flujo de Trabajo del Sistema](#flujo-de-trabajo-del-sistema)
6. [Diseño de Scenarios: Principios y Patrones](#diseño-de-scenarios-principios-y-patrones)
7. [Prácticas del Desarrollador](#prácticas-del-desarrollador)
8. [Integración con el Desarrollo de Features](#integración-con-el-desarrollo-de-features)
9. [Ejemplos Completos](#ejemplos-completos)
10. [Anti-patrones y Errores Comunes](#anti-patrones-y-errores-comunes)
11. [Best Practices](#best-practices)
12. [Checklist de Implementación](#checklist-de-implementación)

---

## Introducción

El **Scenarios System** es una práctica adoptada por los equipos de APSYS para facilitar las **pruebas de integración** de repositorios y endpoints de WebApi.

En lugar de preparar la base de datos usando clases en desarrollo (que pueden tener bugs), el sistema permite:
- Crear **snapshots XML** del estado de la base de datos
- **Cargar datos pre-validados** en tests de integración
- **Reutilizar escenarios** entre múltiples tests
- **Versionar datos de prueba** junto con el código

Este documento describe tanto la **intención y objetivo** del sistema, como las **prácticas que debe observar el desarrollador** al momento de crear scenarios.

> **⚠️ CRÍTICO:** El diseño de scenarios debe ser **parte del desarrollo de features**, no una tarea posterior. Los scenarios mal diseñados generan tests frágiles y difíciles de mantener.

---

## Intención y Objetivo del Sistema

### Problema que Resuelve

#### Sin Scenarios System
```csharp
[Test]
public async Task GetUsers_ReturnsAllUsers()
{
    // Arrange - Preparar BD usando clases en desarrollo
    var roleRepository = new NHRoleRepository(session, validator);
    await roleRepository.CreateAsync("Admin");  // ⚠️ Puede fallar si Role tiene bug

    var userRepository = new NHUserRepository(session, validator);
    await userRepository.CreateAsync("user1@test.com", "User 1");  // ⚠️ Depende de código en desarrollo
    await userRepository.CreateAsync("user2@test.com", "User 2");

    // Act
    var users = await userRepository.GetAllAsync();

    // Assert
    users.Should().HaveCount(2);
}
```

**Problemas**:
- ❌ Si `CreateAsync` tiene bugs, el test falla por razones incorrectas
- ❌ Mucho código boilerplate de setup
- ❌ Tests lentos (ejecutan lógica de negocio en arrange)
- ❌ Difícil de mantener (cambios en entidades rompen múltiples tests)
- ❌ No hay reuso de datos de prueba

#### Con Scenarios System
```csharp
[Test]
public async Task GetUsers_ReturnsAllUsers()
{
    // Arrange - Cargar datos pre-validados desde XML
    LoadScenario("CreateUsers");  // ✅ Carga datos conocidos y validados

    // Act
    var users = await RepositoryUnderTest.GetAllAsync();

    // Assert
    users.Should().HaveCount(5);  // ✅ Sabemos exactamente cuántos usuarios esperamos
    users.Should().Contain(u => u.Email == "usuario1@example.com");  // ✅ Datos predecibles
}
```

**Ventajas**:
- ✅ **Datos pre-validados**: XML generado desde BD en estado conocido
- ✅ **Reutilizable**: Múltiples tests usan el mismo scenario
- ✅ **Rápido**: Carga XML directamente, sin ejecutar lógica de negocio
- ✅ **Predecible**: Siempre los mismos datos, no hay randomización
- ✅ **Mantenible**: Un cambio en scenario actualiza todos los tests que lo usan
- ✅ **Versionable**: XML files en Git junto con el código

### Objetivo Principal

> **"Evitar preparar la base de datos con las clases que están en desarrollo"**

El sistema permite **separar la preparación de datos de prueba del código bajo prueba**, garantizando que:
1. Los tests no fallan por bugs en código de setup
2. Los datos de prueba son consistentes y predecibles
3. Los tests son rápidos y enfocados en lo que realmente prueban
4. Los scenarios son reutilizables entre tests de repositorios y endpoints

---

## Anatomía del Sistema de Scenarios

### Componentes del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                 SCENARIOS SYSTEM                            │
└─────────────────────────────────────────────────────────────┘
          │
          ├── 1. IScenario (Interface)
          │     Define el contrato: SeedData(), ScenarioFileName, PreloadScenario
          │
          ├── 2. Scenario Implementations (Clases)
          │     Sc010CreateSandBox, Sc020CreateRoles, Sc030CreateUsers, etc.
          │
          ├── 3. AppSchema.xsd (XSD Schema)
          │     Define estructura de tablas como Typed DataSet
          │
          ├── 4. AppSchema.Designer.cs (Generated)
          │     Typed DataSet generado automáticamente desde XSD
          │
          ├── 5. NDbUnit (Framework)
          │     ClearDatabase(), SeedDatabase(), GetDataSetFromDb()
          │
          ├── 6. XML Files (Snapshots)
          │     CreateSandBox.xml, CreateRoles.xml, CreateUsers.xml, etc.
          │
          └── 7. Test Infrastructure (Base Classes)
                NHRepositoryTestBase, EndpointTestBase → LoadScenario()
```

### Flujo del Sistema

```
┌──────────────────────────────────────────────────────────────────────┐
│ FASE 1: Generación de Scenarios (Una sola vez / Cuando cambian)    │
└──────────────────────────────────────────────────────────────────────┘

Developer              ScenarioBuilder            Database            XML Files
    |                        |                        |                    |
    | Define IScenario       |                        |                    |
    |----------------------->|                        |                    |
    |                        |                        |                    |
    | dotnet run             |                        |                    |
    |----------------------->|                        |                    |
    |                        | ClearDatabase()        |                    |
    |                        |----------------------->|                    |
    |                        |                        |                    |
    |                        | LoadXmlFile(Preload)   |                    |
    |                        |------------------------------------>|        |
    |                        |                        |           |        |
    |                        | scenario.SeedData()    |           |        |
    |                        | (executes repositories)|           |        |
    |                        |----------------------->|           |        |
    |                        |                        |           |        |
    |                        | GetDataSetFromDb()     |           |        |
    |                        |----------------------->|           |        |
    |                        | (reads all tables)     |           |        |
    |                        |                        |           |        |
    |                        | dataSet.WriteXml()     |           |        |
    |                        |------------------------------------------->|
    |                        |                        |           |   (XML saved)


┌──────────────────────────────────────────────────────────────────────┐
│ FASE 2: Ejecución de Tests (Cada vez que corren los tests)         │
└──────────────────────────────────────────────────────────────────────┘

Test                 Base Class              NDbUnit              Database
  |                      |                       |                    |
  | [SetUp]              |                       |                    |
  |--------------------->|                       |                    |
  |                      | ClearDatabase()       |                    |
  |                      |---------------------->|                    |
  |                      |                       | DELETE FROM tables |
  |                      |                       |------------------->|
  |                      |                       |                    |
  | LoadScenario("CreateUsers")                  |                    |
  |--------------------->|                       |                    |
  |                      | ReadXml("CreateUsers.xml")                 |
  |                      |---------------------->|                    |
  |                      |                       |                    |
  |                      | SeedDatabase(dataSet) |                    |
  |                      |---------------------->|                    |
  |                      |                       | INSERT data        |
  |                      |                       |------------------->|
  |                      |                       |                    |
  | // Test runs with pre-loaded data            |                    |
  |----------------------------------------------------->|             |
```

---

## Componentes Clave

### 1. IScenario Interface

**Ubicación**: `tests/{ProjectName}.scenarios/IScenario.cs`

```csharp
namespace hashira.stone.backend.scenarios;

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

**Propósito de cada miembro**:

| Miembro | Tipo | Propósito |
|---------|------|-----------|
| `SeedData()` | `Task` | Ejecuta operaciones de repositorio para poblar la BD |
| `ScenarioFileName` | `string` | Nombre del archivo XML (sin extensión) |
| `PreloadScenario` | `Type?` | Scenario prerequisito (dependency chain) |

### 2. AppSchema (Typed DataSet)

**Ubicación**: `tests/{ProjectName}.common.tests/`

**Archivos**:
- `AppSchema.xsd` - XML Schema Definition (define estructura de tablas)
- `AppSchema.Designer.cs` - Auto-generated typed DataSet
- `AppSchemaExtender.cs` - Extension methods para acceso conveniente

#### AppSchema.xsd Structure

```xml
<?xml version="1.0" encoding="utf-8"?>
<xs:schema id="AppSchema"
           targetNamespace="http://tempuri.org/AppSchema.xsd"
           xmlns:xs="http://www.w3.org/2001/XMLSchema">

  <xs:element name="AppSchema" msdata:IsDataSet="true">
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

**⚠️ IMPORTANTE**: El XSD debe estar **sincronizado con el schema de la base de datos**. Cada tabla debe tener su elemento correspondiente.

#### AppSchemaExtender (Helper Methods)

```csharp
namespace hashira.stone.backend.common.tests;

public static class AppSchemaExtender
{
    // Table name constants
    public static readonly string FullRolesTableName = "public.roles";
    public static readonly string FullUsersTableName = "public.users";
    public static readonly string FullUserInRolesTableName = "public.user_in_roles";

    // Get table methods
    public static DataTable? GetRolesTable(this DataSet appSchema)
        => appSchema.Tables[FullRolesTableName];

    public static DataTable? GetUsersTable(this DataSet appSchema)
        => appSchema.Tables[FullUsersTableName];

    // Get rows with filtering
    public static IEnumerable<DataRow> GetRolesRows(this DataSet appSchema, string filterExpression)
        => GetRolesTable(appSchema)?.Select(filterExpression).AsEnumerable()
           ?? Enumerable.Empty<DataRow>();

    public static IEnumerable<DataRow> GetUsersRows(this DataSet appSchema, string filterExpression)
        => GetUsersTable(appSchema)?.Select(filterExpression).AsEnumerable()
           ?? Enumerable.Empty<DataRow>();

    // Get single row helpers
    public static DataRow? GetFirstUserRow(this DataSet appSchema)
        => GetUsersTable(appSchema)?.AsEnumerable().FirstOrDefault();

    public static DataRow? GetFirstRoleRow(this DataSet appSchema)
        => GetRolesTable(appSchema)?.AsEnumerable().FirstOrDefault();
}
```

**Propósito**: Facilitar acceso a datos desde tests sin magic strings.

### 3. NDbUnit Framework

**Arquitectura**:

```
INDbUnit (Interface)
    |
    ├── DataSet DataSet { get; }
    ├── string ConnectionString { get; }
    ├── DataSet GetDataSetFromDb()
    ├── void ClearDatabase()
    └── void SeedDatabase(DataSet dataSet)
        |
        v
NDbUnit (Abstract Base)
    |
    ├── CreateConnection() - abstract
    ├── CreateDataAdapter() - abstract
    ├── CreateCommandBuilder() - abstract
    ├── DisableTableConstraints() - abstract
    └── EnabledTableConstraints() - abstract
        |
        v
PostgreSQLNDbUnit (Concrete Implementation)
    |
    ├── Uses Npgsql provider
    ├── DISABLE/ENABLE TRIGGER ALL
    └── NpgsqlConnection, NpgsqlDataAdapter, NpgsqlCommandBuilder
```

#### Key Methods

##### GetDataSetFromDb()

```csharp
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
        adapter.SelectCommand = selectCommand;
        adapter.Fill(dsetResult, table.TableName);
    }

    dsetResult.EnforceConstraints = true;
    return dsetResult;
}
```

**Propósito**: Lee el estado completo de la BD en un DataSet.

##### ClearDatabase()

```csharp
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

        // 2. Delete all data
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
```

**Propósito**: Limpia todas las tablas de forma transaccional y segura.

##### SeedDatabase(DataSet dataSet)

```csharp
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
```

**Propósito**: Carga datos desde DataSet/XML a la BD de forma transaccional.

#### PostgreSQLNDbUnit Implementation

```csharp
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

    protected override void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} DISABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }

    protected override void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable)
    {
        using NpgsqlCommand command = (NpgsqlCommand)dbTransaction.Connection.CreateCommand();
        command.Transaction = (NpgsqlTransaction)dbTransaction;
        command.CommandText = $"ALTER TABLE {dataTable.TableName} ENABLE TRIGGER ALL";
        command.ExecuteNonQuery();
    }
}
```

**PostgreSQL-Specific Details**:
- `DISABLE TRIGGER ALL` - Deshabilita FK constraints + triggers personalizados
- `ENABLE TRIGGER ALL` - Re-habilita todo después del seeding
- Compatible con Npgsql provider

### 4. Scenario Implementations

#### Naming Convention

```
Sc{Level}{Description}
│  │     │
│  │     └─ Descriptive name (CreateUsers, CreateAdminUser)
│  └─ Level number (increments of 10 for future insertion)
└─ "Sc" prefix
```

**Examples**:
- `Sc010CreateSandBox` - Base scenario (empty DB)
- `Sc020CreateRoles` - Level 20 (depends on 010)
- `Sc030CreateUsers` - Level 30 (depends on 020)
- `Sc031CreateAdminUser` - Level 31 (depends on 030, parallel to other Sc03X)

#### Dependency Hierarchy

```
Sc010CreateSandBox (Empty DB)
    |
    v
Sc020CreateRoles (Roles only)
    |
    v
Sc030CreateUsers (Roles + Users)
    |
    ├─> Sc031CreateAdminUser (Roles + Users + Admin assignment)
    |        |
    |        v
    |   Sc040CreateTechnicalStandards (Previous + Standards)
    |        |
    |        v
    |   Sc050CreatePrototypes (Previous + Prototypes)
    |
    └─> Sc032CreateInactiveUsers (Roles + Users + Inactive users)
```

**Design Principle**: Dependency tree, not dependency chain. Scenarios at same level (03X) can branch from common parent (020).

#### Example Implementations

##### Empty Baseline (Sc010CreateSandBox)

```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc010CreateSandBox : IScenario
{
    public string ScenarioFileName => "CreateSandBox";

    public Type? PreloadScenario => null;  // ✅ No dependencies

    public Task SeedData()
        => Task.CompletedTask;  // ✅ No operations, just empty DB
}
```

**Purpose**: Establece el baseline vacío. Todos los otros scenarios dependen de este (directa o indirectamente).

##### Simple Creation (Sc020CreateRoles)

```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateRoles";

    public Type? PreloadScenario => typeof(Sc010CreateSandBox);  // ✅ Depends on empty DB

    public Task SeedData()
    {
        // ✅ Delegate to repository method
        return _uoW.Roles.CreateDefaultRoles();
    }
}
```

**Purpose**: Crea roles base del sistema. Simple y reutilizable.

##### Batch Creation with Transaction (Sc030CreateUsers)

```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";

    public Type? PreloadScenario => typeof(Sc020CreateRoles);  // ✅ Needs roles first

    public async Task SeedData()
    {
        // ✅ Define well-known, predictable data
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
            this._uoW.BeginTransaction();  // ✅ Explicit transaction

            foreach (var (email, name) in users)
                await this._uoW.Users.CreateAsync(email, name);

            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();  // ✅ Proper cleanup
            throw;
        }
    }
}
```

**Design Decisions**:
- ✅ **Predictable data**: Always same emails/names (tests can reference "usuario1@example.com")
- ✅ **Realistic variety**: 5 users with Spanish characters, accents
- ✅ **Explicit transaction**: Ensures atomicity
- ✅ **Defensive coding**: Rollback on any exception

##### Relationship Management (Sc031CreateAdminUser)

```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc031CreateAdminUser(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateAdminUser";

    public Type? PreloadScenario => typeof(Sc030CreateUsers);  // ✅ Needs users created

    public async Task SeedData()
    {
        try
        {
            this._uoW.BeginTransaction();

            // ✅ Modify existing data from preload
            var adminUser = await this._uoW.Users.GetByEmailAsync("usuario1@example.com");
            if (adminUser != null)
            {
                var adminRole = await this._uoW.Roles.GetByNameAsync(RolesResources.PlatformAdministrator);
                if (adminRole != null && !adminUser.Roles.Contains(adminRole))
                {
                    adminUser.Roles.Add(adminRole);  // ✅ Add relationship
                    await this._uoW.Users.SaveAsync(adminUser);
                }
            }

            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();
            throw;
        }
    }
}
```

**Design Decisions**:
- ✅ **Builds on existing data**: Modifies user from Sc030CreateUsers
- ✅ **Defensive coding**: Null checks before operations
- ✅ **Single responsibility**: Only assigns admin role, nothing else

### 5. XML Snapshot Files

#### File Structure

```xml
<?xml version="1.0" standalone="yes"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">

  <!-- Roles table data -->
  <public.roles>
    <id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</id>
    <name>PlatformAdministrator</name>
  </public.roles>

  <!-- Users table data -->
  <public.users>
    <id>5802eee4-0b2b-40bf-854c-b64ad04094dd</id>
    <email>usuario1@example.com</email>
    <name>Carlos Rodríguez</name>
  </public.users>

  <public.users>
    <id>58864351-a485-430c-99d2-f46db850293a</id>
    <email>usuario2@example.com</email>
    <name>Ana María González</name>
  </public.users>

  <!-- Junction table for User-Role relationship -->
  <public.user_in_roles>
    <user_id>5802eee4-0b2b-40bf-854c-b64ad04094dd</user_id>
    <role_id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</role_id>
  </public.user_in_roles>

</AppSchema>
```

**Key Characteristics**:
- ✅ **Complete state**: Includes data from preload scenarios + new data
- ✅ **Strongly-typed**: Validated against AppSchema.xsd
- ✅ **All tables**: Even if scenario doesn't create new records in a table, preloaded data is included
- ✅ **GUIDs preserved**: Generated during SeedData execution, captured in XML
- ✅ **Dates in ISO 8601**: DateTime values serialized with timezone info

#### Example: Progressive State

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
  <!-- Role from Sc020CreateRoles -->
  <public.roles>
    <id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</id>
    <name>PlatformAdministrator</name>
  </public.roles>

  <!-- 5 Users from Sc030CreateUsers -->
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
  <!-- Role and Users from previous scenarios -->
  <public.roles>...</public.roles>
  <public.users>...</public.users>

  <!-- New: User-Role assignment from Sc031CreateAdminUser -->
  <public.user_in_roles>
    <user_id>5802eee4-0b2b-40bf-854c-b64ad04094dd</user_id>
    <role_id>adfa5c04-e3db-442a-9f4b-6376eb88f00e</role_id>
  </public.user_in_roles>
</AppSchema>
```

> **💡 Key Insight**: XML files are **cumulative snapshots**, not deltas. They contain the complete database state after executing SeedData + preloads.

---

## Flujo de Trabajo del Sistema

### Fase 1: Generación de Scenarios (Una vez o cuando cambian)

#### Paso 1: Definir IScenario Implementation

```csharp
public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";

    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        // Implementation
    }
}
```

#### Paso 2: Run Scenarios Generator

```bash
cd tests/{ProjectName}.scenarios

dotnet run \
  /cnn:"Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=root;" \
  /output:"D:\scenarios"
```

#### Paso 3: ScenarioBuilder Workflow

```csharp
foreach (var scenario in builder.Scenarios)
{
    // 1. Clear database
    builder.NDbUnitTest.ClearDatabase();

    // 2. Load preload scenario XML (if exists)
    if (scenario.PreloadScenario != null)
    {
        builder.LoadXmlFile(scenario.PreloadScenario, outputPath);
    }

    // 3. Execute scenario's SeedData method
    await scenario.SeedData();

    // 4. Extract database state to DataSet
    DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();

    // 5. Serialize DataSet to XML
    string filePath = Path.Combine(outputPath, $"{scenario.ScenarioFileName}.xml");
    dataSet.WriteXml(filePath);

    Console.WriteLine($"✅ Generated: {filePath}");
}
```

**Output**:
```
Reading command line parameters...
LOG: Loading scenarios...
LOG: Creating scenario CreateSandBox...
LOG: Creating scenario CreateRoles...
LOG: Creating scenario CreateUsers...
LOG: Creating scenario CreateAdminUser...
Scenario loading completed

✅ Generated: D:\scenarios\CreateSandBox.xml
✅ Generated: D:\scenarios\CreateRoles.xml
✅ Generated: D:\scenarios\CreateUsers.xml
✅ Generated: D:\scenarios\CreateAdminUser.xml
```

### Fase 2: Uso en Tests (Cada vez que se ejecutan)

#### Repository Tests

```csharp
namespace Infrastructure.Tests.Repositories;

using NUnit.Framework;
using FluentAssertions;

public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    [Test]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        LoadScenario("CreateUsers");  // ✅ Loads 5 users + 1 role
        const string email = "usuario1@example.com";  // ✅ Predictable data

        // Act
        var user = await RepositoryUnderTest.GetByEmailAsync(email);

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Name.Should().Be("Carlos Rodríguez");
    }

    [Test]
    public async Task CreateAsync_WhenEmailIsDuplicated_ThrowsDuplicatedDomainException()
    {
        // Arrange
        LoadScenario("CreateUsers");  // ✅ Pre-load existing users
        var existingEmail = GetFirstUserEmailFromDb();  // ✅ Get data from XML

        // Act
        Func<Task> act = async () => await RepositoryUnderTest.CreateAsync(existingEmail!, "Test User");

        // Assert
        await act.Should().ThrowAsync<DuplicatedDomainException>()
            .WithMessage($"A user with the email '{existingEmail}' already exists.");
    }

    // ✅ Helper method to access scenario data
    private string? GetFirstUserEmailFromDb()
    {
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        var userRow = dataSet.GetFirstUserRow();  // Extension method
        return userRow?["email"].ToString();
    }
}
```

#### Endpoint Tests

```csharp
namespace WebApi.Tests.Endpoints;

using System.Net;
using System.Net.Http;
using NUnit.Framework;
using FluentAssertions;
using Newtonsoft.Json;

internal class GetManyAndCountUsersEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GetUsers_WithoutFilters_ReturnsAllUsersInPage()
    {
        // Arrange
        LoadScenario("CreateUsers");  // ✅ 5 users + 1 role
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
    public async Task GetUsers_WithEmailFilter_ReturnsFilteredUsers()
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
}
```

---

## Diseño de Scenarios: Principios y Patrones

> **⚠️ CRÍTICO:** El diseño de scenarios es la parte más importante del sistema. Scenarios mal diseñados generan tests frágiles, lentos y difíciles de mantener.

### Principios Fundamentales

#### 1. Single Responsibility Principle

**✅ CORRECTO: Un scenario, un propósito**

```csharp
// ✅ Sc030CreateUsers - Solo crea usuarios
public async Task SeedData()
{
    var users = new List<(string Email, string Name)> { ... };
    foreach (var (email, name) in users)
        await _uoW.Users.CreateAsync(email, name);
}

// ✅ Sc031CreateAdminUser - Solo asigna rol admin
public async Task SeedData()
{
    var adminUser = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
    var adminRole = await _uoW.Roles.GetByNameAsync("PlatformAdministrator");
    adminUser.Roles.Add(adminRole);
    await _uoW.Users.SaveAsync(adminUser);
}
```

**❌ INCORRECTO: Scenario hace demasiado**

```csharp
// ❌ Sc030CreateUsersWithRolesAndStandards - Demasiadas responsabilidades
public async Task SeedData()
{
    // Creates users
    await _uoW.Users.CreateAsync(...);

    // AND assigns roles
    user.Roles.Add(role);

    // AND creates technical standards
    await _uoW.TechnicalStandards.CreateAsync(...);

    // AND creates prototypes
    await _uoW.Prototypes.CreateAsync(...);

    // ❌ Difícil de reutilizar, difícil de mantener
}
```

**Por qué importa**:
- ✅ Scenarios pequeños son más fáciles de entender
- ✅ Se pueden combinar de diferentes formas
- ✅ Cambios en un concepto no afectan otros scenarios

#### 2. Predictable Data Principle

**✅ CORRECTO: Datos conocidos y predecibles**

```csharp
public async Task SeedData()
{
    // ✅ Well-known emails that tests can reference
    var users = new List<(string Email, string Name)>
    {
        ("usuario1@example.com", "Carlos Rodríguez"),
        ("usuario2@example.com", "Ana María González"),
        ("usuario3@example.com", "José Luis Martínez"),
        ("usuario4@example.com", "María Fernanda López"),
        ("usuario5@example.com", "Juan Pablo Ramírez")
    };

    foreach (var (email, name) in users)
        await _uoW.Users.CreateAsync(email, name);
}
```

**Tests pueden referenciar datos específicos**:
```csharp
[Test]
public async Task GetByEmail_WithKnownUser_ReturnsUser()
{
    // Arrange
    LoadScenario("CreateUsers");

    // ✅ Test sabe exactamente qué datos esperar
    const string expectedEmail = "usuario1@example.com";
    const string expectedName = "Carlos Rodríguez";

    // Act
    var user = await repository.GetByEmailAsync(expectedEmail);

    // Assert
    user.Should().NotBeNull();
    user!.Name.Should().Be(expectedName);
}
```

**❌ INCORRECTO: Datos aleatorios**

```csharp
public async Task SeedData()
{
    var random = new Random();

    // ❌ Random data - diferentes cada vez que se genera
    for (int i = 0; i < 5; i++)
    {
        var email = $"user{random.Next(1000, 9999)}@example.com";
        var name = $"User {Guid.NewGuid()}";
        await _uoW.Users.CreateAsync(email, name);
    }
}
```

**Tests no pueden referenciar datos específicos**:
```csharp
[Test]
public async Task GetByEmail_WithKnownUser_ReturnsUser()
{
    // Arrange
    LoadScenario("CreateUsers");

    // ❌ ¿Qué email usar? No sabemos qué se generó
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var firstUser = dataSet.GetFirstUserRow();  // ❌ Forzado a buscar dinámicamente
    var email = firstUser["email"].ToString();  // ❌ No ideal

    // Act & Assert
    // ...
}
```

**Por qué importa**:
- ✅ Tests son más legibles ("usuario1@example.com" vs "firstUser["email"]")
- ✅ Tests son más robustos (no dependen de orden de generación)
- ✅ Debugging es más fácil (sabemos qué datos buscar en BD)

#### 3. Minimal Dependencies Principle

**✅ CORRECTO: Dependencia directa mínima**

```
Sc010CreateSandBox
    |
    v
Sc020CreateRoles
    |
    v
Sc030CreateUsers
    |
    v
Sc031CreateAdminUser
```

```csharp
public class Sc031CreateAdminUser : IScenario
{
    public Type? PreloadScenario => typeof(Sc030CreateUsers);  // ✅ Solo depende del inmediato anterior

    // Sc030CreateUsers ya incluye Sc020CreateRoles en su XML
    // No necesitamos cargar Sc020CreateRoles directamente
}
```

**❌ INCORRECTO: Dependencias innecesarias**

```csharp
public class Sc031CreateAdminUser : IScenario
{
    // ❌ No es necesario listar todas las dependencias transitivas
    public Type? PreloadScenario => typeof(Sc010CreateSandBox);
    public Type? SecondaryPreload => typeof(Sc020CreateRoles);
    public Type? TertiaryPreload => typeof(Sc030CreateUsers);
}
```

**Por qué importa**:
- ✅ XML files contienen estado acumulado (no necesitas cargar toda la cadena)
- ✅ Cambiar un scenario solo afecta sus dependientes directos
- ✅ Más fácil entender qué datos trae cada scenario

#### 4. Realistic Variety Principle

**✅ CORRECTO: Variedad realista que cubre edge cases**

```csharp
public async Task SeedData()
{
    var technicalStandards = new List<TechnicalStandardData>
    {
        // ✅ Different statuses
        ("CFE-001", "Active", "Electrical Standards"),
        ("CFE-002", "Deprecated", "Old Electrical Standards"),

        // ✅ Different types
        ("NOM-001", "Active", "Mexican Official Standard"),
        ("ISO-001", "Active", "International Standard"),

        // ✅ Different editions
        ("CFE-003", "Active", "Edition 1"),
        ("CFE-004", "Active", "Edition 2"),

        // ✅ Edge cases: Special characters
        ("NOM-Ñ01", "Active", "Estándar con Ñ"),
        ("CFE-É01", "Active", "Standard with Accent É"),
    };

    foreach (var (code, status, name) in technicalStandards)
        await _uoW.TechnicalStandards.CreateAsync(code, status, name);
}
```

**Tests pueden probar diferentes escenarios**:
```csharp
[Test]
public async Task GetByStatus_WithActive_ReturnsOnlyActive()
{
    // Arrange
    LoadScenario("CreateTechnicalStandards");

    // Act
    var activeStandards = await repository.GetByStatusAsync("Active");

    // Assert - Scenario incluye tanto Active como Deprecated
    activeStandards.Should().Contain(s => s.Code == "CFE-001");
    activeStandards.Should().NotContain(s => s.Code == "CFE-002");  // Deprecated
}

[Test]
public async Task GetByCode_WithSpecialCharacters_HandlesCorrectly()
{
    // Arrange
    LoadScenario("CreateTechnicalStandards");

    // Act
    var standard = await repository.GetByCodeAsync("NOM-Ñ01");

    // Assert - Scenario incluye edge case con Ñ
    standard.Should().NotBeNull();
    standard!.Name.Should().Be("Estándar con Ñ");
}
```

**❌ INCORRECTO: Datos homogéneos**

```csharp
public async Task SeedData()
{
    // ❌ Todos los registros son idénticos excepto ID
    for (int i = 1; i <= 30; i++)
    {
        await _uoW.TechnicalStandards.CreateAsync(
            code: $"STD-{i:D3}",
            status: "Active",  // ❌ Todos Active
            name: $"Standard {i}"  // ❌ Todos iguales
        );
    }
}
```

**Por qué importa**:
- ✅ Un scenario puede servir para múltiples tests (filtering, sorting, edge cases)
- ✅ Tests capturan bugs reales (special characters, different statuses)
- ✅ Mejor coverage sin crear múltiples scenarios

#### 5. Transactional Safety Principle

**✅ CORRECTO: Explicit transaction management**

```csharp
public async Task SeedData()
{
    try
    {
        _uoW.BeginTransaction();  // ✅ Explicit start

        // Multiple operations
        foreach (var (email, name) in users)
            await _uoW.Users.CreateAsync(email, name);

        _uoW.Commit();  // ✅ Explicit commit
    }
    catch
    {
        _uoW.Rollback();  // ✅ Explicit rollback
        throw;
    }
}
```

**❌ INCORRECTO: No transaction management**

```csharp
public async Task SeedData()
{
    // ❌ Si falla a la mitad, deja la BD en estado inconsistente
    await _uoW.Users.CreateAsync("usuario1@example.com", "User 1");
    await _uoW.Users.CreateAsync("usuario2@example.com", "User 2");
    await _uoW.Users.CreateAsync("invalid", "User 3");  // ❌ Falla aquí
    // Usuario 1 y 2 quedan en BD, Usuario 3 no
}
```

**Por qué importa**:
- ✅ All-or-nothing: Scenario completo o nada
- ✅ Debugging más fácil: No hay datos parciales
- ✅ XML generation confiable: Estado consistente

### Patrones de Diseño

#### Patrón 1: Base Scenario (Empty Baseline)

**Propósito**: Establecer punto de partida vacío.

```csharp
public class Sc010CreateSandBox : IScenario
{
    public string ScenarioFileName => "CreateSandBox";
    public Type? PreloadScenario => null;
    public Task SeedData() => Task.CompletedTask;
}
```

**Cuándo usar**:
- ✅ Primer scenario de la jerarquía
- ✅ Tests que necesitan BD completamente vacía

#### Patrón 2: Foundation Scenario (Core Entities)

**Propósito**: Crear entidades base del sistema (roles, configuraciones, etc.).

```csharp
public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    public string ScenarioFileName => "CreateRoles";
    public Type? PreloadScenario => typeof(Sc010CreateSandBox);

    public Task SeedData()
        => _uoW.Roles.CreateDefaultRoles();  // Delega a repository method
}
```

**Cuándo usar**:
- ✅ Entidades que son prerequisitos para todo (roles, catalogs)
- ✅ Configuraciones del sistema

#### Patrón 3: Bulk Creation Scenario (Multiple Entities)

**Propósito**: Crear múltiples registros de un tipo.

```csharp
public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    public string ScenarioFileName => "CreateUsers";
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        var users = new List<(string Email, string Name)>
        {
            ("usuario1@example.com", "Carlos Rodríguez"),
            ("usuario2@example.com", "Ana María González"),
            // ... más usuarios
        };

        try
        {
            _uoW.BeginTransaction();
            foreach (var (email, name) in users)
                await _uoW.Users.CreateAsync(email, name);
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

**Cuándo usar**:
- ✅ Necesitas varios registros para tests (pagination, filtering)
- ✅ Datos variados para cubrir edge cases

#### Patrón 4: Modification Scenario (Update Existing Data)

**Propósito**: Modificar datos del preload scenario.

```csharp
public class Sc031CreateAdminUser(IUnitOfWork uoW) : IScenario
{
    public string ScenarioFileName => "CreateAdminUser";
    public Type? PreloadScenario => typeof(Sc030CreateUsers);

    public async Task SeedData()
    {
        try
        {
            _uoW.BeginTransaction();

            // ✅ Modifica usuario existente
            var user = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
            var role = await _uoW.Roles.GetByNameAsync("PlatformAdministrator");

            if (user != null && role != null)
            {
                user.Roles.Add(role);
                await _uoW.Users.SaveAsync(user);
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

**Cuándo usar**:
- ✅ Necesitas datos en un estado específico (user with admin role)
- ✅ Tests de update operations

#### Patrón 5: Relationship Scenario (Many-to-Many)

**Propósito**: Crear relaciones entre entidades existentes.

```csharp
public class Sc040AssignUsersToRoles(IUnitOfWork uoW) : IScenario
{
    public string ScenarioFileName => "AssignUsersToRoles";
    public Type? PreloadScenario => typeof(Sc030CreateUsers);

    public async Task SeedData()
    {
        try
        {
            _uoW.BeginTransaction();

            // ✅ Multiple assignments
            var assignments = new List<(string UserEmail, string RoleName)>
            {
                ("usuario1@example.com", "PlatformAdministrator"),
                ("usuario2@example.com", "Inspector"),
                ("usuario3@example.com", "Inspector"),
            };

            foreach (var (userEmail, roleName) in assignments)
            {
                var user = await _uoW.Users.GetByEmailAsync(userEmail);
                var role = await _uoW.Roles.GetByNameAsync(roleName);

                if (user != null && role != null && !user.Roles.Contains(role))
                {
                    user.Roles.Add(role);
                    await _uoW.Users.SaveAsync(user);
                }
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

**Cuándo usar**:
- ✅ Tests de relaciones many-to-many
- ✅ Tests de authorization (users con diferentes roles)

#### Patrón 6: Domain Data Scenario (Feature-Specific)

**Propósito**: Crear datos de dominio específicos del feature.

```csharp
public class Sc050CreateTechnicalStandards(IUnitOfWork uoW) : IScenario
{
    public string ScenarioFileName => "CreateTechnicalStandards";
    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);

    public async Task SeedData()
    {
        var standards = new List<(string Code, string Status, string Name)>
        {
            // Active standards
            ("CFE-G0100-04", "Active", "Especificaciones de Transformadores"),
            ("NOM-001-SEDE-2012", "Active", "Instalaciones Eléctricas"),

            // Deprecated standards
            ("CFE-G0100-03", "Deprecated", "Especificaciones (Old Edition)"),

            // Edge cases
            ("NOM-Ñ01", "Active", "Estándar con Ñ"),
        };

        try
        {
            _uoW.BeginTransaction();

            foreach (var (code, status, name) in standards)
            {
                await _uoW.TechnicalStandards.CreateAsync(
                    code: code,
                    status: status,
                    name: name,
                    edition: "1st",
                    type: code.StartsWith("CFE") ? "CFE" : "NOM",
                    creationDate: DateTime.UtcNow
                );
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

**Cuándo usar**:
- ✅ Tests de features específicos del dominio
- ✅ Datos complejos con lógica de negocio

---

## Prácticas del Desarrollador

### 1. Naming Conventions

#### Scenario Classes

**Formato**: `Sc{Level}{Description}`

| Parte | Regla | Ejemplo |
|-------|-------|---------|
| `Sc` | Prefix fijo | `Sc` |
| `{Level}` | Número en incrementos de 10 | `010`, `020`, `030` |
| `{Description}` | PascalCase descriptivo | `CreateUsers`, `CreateAdminUser` |

**Examples**:
- ✅ `Sc010CreateSandBox`
- ✅ `Sc020CreateRoles`
- ✅ `Sc030CreateUsers`
- ✅ `Sc031CreateAdminUser` (branch at same level)
- ✅ `Sc040CreateTechnicalStandards`

**❌ INCORRECTO**:
- ❌ `Sc1CreateSandBox` (sin padding de ceros)
- ❌ `ScCreateUsers` (sin número de level)
- ❌ `Sc030Users` (sin verbo descriptivo)
- ❌ `Sc030_Create_Users` (no usar underscores)

**Rationale**:
- Increments of 10 permiten insertar scenarios intermedios (Sc031, Sc032, Sc033)
- PascalCase es consistente con C# naming conventions
- Verbo + Noun es claro sobre qué hace el scenario

#### ScenarioFileName Property

**Formato**: Mismo que el class name pero sin prefijo `Sc{Level}`

```csharp
public class Sc030CreateUsers : IScenario
{
    // ✅ Omite "Sc030" prefix
    public string ScenarioFileName => "CreateUsers";
}
```

**Rationale**:
- XML file no necesita número de level en el nombre
- Más legible: `CreateUsers.xml` vs `Sc030CreateUsers.xml`
- Tests usan nombre descriptivo: `LoadScenario("CreateUsers")`

### 2. Dependency Management

#### Dependency Hierarchy Strategy

**✅ CORRECTO: Tree Structure**

```
Sc010CreateSandBox
    |
    v
Sc020CreateRoles
    |
    +--> Sc030CreateUsers
    |        |
    |        +--> Sc031CreateAdminUser
    |        |        |
    |        |        v
    |        |    Sc040CreateTechnicalStandards
    |        |
    |        +--> Sc032CreateInactiveUsers
    |
    +--> Sc021CreateCustomRoles
```

**❌ INCORRECTO: Linear Chain**

```
Sc010 -> Sc020 -> Sc030 -> Sc031 -> Sc040 -> Sc050 -> ...
```

**Por qué Tree es mejor**:
- ✅ Scenarios en el mismo level pueden branch desde un parent común
- ✅ Tests pueden elegir qué branch necesitan
- ✅ Cambios en un scenario solo afectan su subtree

#### PreloadScenario Guidelines

**✅ CORRECTO: Direct parent only**

```csharp
public class Sc040CreateTechnicalStandards : IScenario
{
    // ✅ Solo lista el parent inmediato
    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);

    // Sc031CreateAdminUser.xml ya contiene:
    // - Datos de Sc010CreateSandBox
    // - Datos de Sc020CreateRoles
    // - Datos de Sc030CreateUsers
    // - Datos de Sc031CreateAdminUser
}
```

**❌ INCORRECTO: Multiple preloads**

```csharp
public class Sc040CreateTechnicalStandards : IScenario
{
    // ❌ El sistema no soporta múltiples preloads
    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);
    public Type? SecondaryPreload => typeof(Sc030CreateUsers);  // ❌ No existe esta property
}
```

### 3. Transaction Management

**✅ CORRECTO: Always use explicit transactions**

```csharp
public async Task SeedData()
{
    try
    {
        _uoW.BeginTransaction();

        // Operations
        await _uoW.Users.CreateAsync(...);
        await _uoW.Roles.CreateAsync(...);

        _uoW.Commit();
    }
    catch
    {
        _uoW.Rollback();
        throw;  // Re-throw para que ScenarioBuilder sepa que falló
    }
}
```

**❌ INCORRECTO: No transaction**

```csharp
public async Task SeedData()
{
    // ❌ Si falla a la mitad, BD queda inconsistente
    await _uoW.Users.CreateAsync(...);
    await _uoW.Users.CreateAsync(...);  // Falla aquí
    await _uoW.Users.CreateAsync(...);  // Nunca se ejecuta
}
```

### 4. Data Design

#### Well-Known Test Data

**✅ CORRECTO: Datos conocidos que tests pueden referenciar**

```csharp
public async Task SeedData()
{
    // ✅ Emails específicos y documentados
    var users = new List<(string Email, string Name)>
    {
        ("usuario1@example.com", "Carlos Rodríguez"),      // Admin user
        ("usuario2@example.com", "Ana María González"),    // Regular user
        ("usuario3@example.com", "José Luis Martínez"),    // Regular user
        ("usuario4@example.com", "María Fernanda López"),  // Inactive user
        ("usuario5@example.com", "Juan Pablo Ramírez"),    // Guest user
    };
}
```

**Tests pueden usar datos específicos**:
```csharp
[Test]
public async Task GetAdminUsers_ReturnsOnlyAdmins()
{
    LoadScenario("CreateAdminUser");

    // ✅ Test sabe que usuario1 es el admin
    var admins = await repository.GetAdminUsersAsync();
    admins.Should().ContainSingle()
        .Which.Email.Should().Be("usuario1@example.com");
}
```

**❌ INCORRECTO: Datos anónimos**

```csharp
public async Task SeedData()
{
    // ❌ Usuarios sin identidad clara
    for (int i = 1; i <= 5; i++)
    {
        await _uoW.Users.CreateAsync($"user{i}@test.com", $"User {i}");
    }
}
```

#### Realistic Variety

**✅ CORRECTO: Cover edge cases**

```csharp
public async Task SeedData()
{
    var standards = new List<(string Code, string Status)>
    {
        // Different statuses
        ("CFE-001", "Active"),
        ("CFE-002", "Deprecated"),
        ("CFE-003", "Draft"),

        // Special characters
        ("NOM-Ñ01", "Active"),          // Spanish Ñ
        ("CFE-É01", "Active"),          // Accent É
        ("ISO-Ø01", "Active"),          // Nordic Ø

        // Edge case: Very long code
        ("VERY-LONG-CODE-THAT-TESTS-LIMITS-9999", "Active"),

        // Edge case: Empty/null handling
        // (tests will verify these are NOT in DB)
    };
}
```

#### Quantity Guidelines

| Scenario Type | Recommended Quantity | Rationale |
|---------------|---------------------|-----------|
| Foundation (roles, catalogs) | 2-5 | Cover main cases |
| Users | 5-10 | Pagination, filtering tests |
| Domain entities | 10-30 | Variety for queries |
| Relationships | 3-10 | Different combinations |

**❌ INCORRECTO: Too many or too few**

```csharp
// ❌ Too few - not enough for pagination tests
var users = new[] { "user1@test.com" };

// ❌ Too many - slow generation, slow loading
for (int i = 0; i < 10000; i++)
    await _uoW.Users.CreateAsync($"user{i}@test.com", $"User {i}");
```

### 5. Comments and Documentation

**✅ CORRECTO: Document intent and special cases**

```csharp
public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    /// <summary>
    /// Creates 5 test users with different characteristics:
    /// - usuario1: Will be admin in Sc031CreateAdminUser
    /// - usuario2-3: Regular users with different roles
    /// - usuario4: Will be inactive
    /// - usuario5: Guest user
    /// </summary>
    public string ScenarioFileName => "CreateUsers";

    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        var users = new List<(string Email, string Name, string Comment)>
        {
            ("usuario1@example.com", "Carlos Rodríguez", "Future admin"),
            ("usuario2@example.com", "Ana María González", "Inspector"),
            // ...
        };

        try
        {
            _uoW.BeginTransaction();

            foreach (var (email, name, comment) in users)
            {
                // Note: Comment not stored, just for documentation
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

### 6. Defensive Coding

**✅ CORRECTO: Null checks and validation**

```csharp
public async Task SeedData()
{
    try
    {
        _uoW.BeginTransaction();

        var user = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
        if (user == null)
        {
            throw new InvalidOperationException(
                "usuario1@example.com not found. " +
                "Ensure Sc030CreateUsers was loaded as preload scenario.");
        }

        var role = await _uoW.Roles.GetByNameAsync("PlatformAdministrator");
        if (role == null)
        {
            throw new InvalidOperationException(
                "PlatformAdministrator role not found. " +
                "Ensure Sc020CreateRoles was loaded.");
        }

        if (!user.Roles.Contains(role))
        {
            user.Roles.Add(role);
            await _uoW.Users.SaveAsync(user);
        }

        _uoW.Commit();
    }
    catch
    {
        _uoW.Rollback();
        throw;
    }
}
```

**Por qué importa**:
- ✅ Errores claros durante generation (not during tests)
- ✅ Fácil debug si preload scenario falla
- ✅ Previene datos inconsistentes en XML

---

## Integración con el Desarrollo de Features

> **⚠️ CRÍTICO:** El diseño de scenarios debe ser **parte del desarrollo del feature**, no una tarea posterior.

### Flujo de Trabajo Recomendado

#### Fase 1: Diseño del Feature

```
1. Define Feature Requirements
2. Design Domain Entities
3. ✅ DESIGN SCENARIO - Qué datos necesito para tests?
4. Design Use Cases
5. Design Endpoints
```

**Example: Feature "Technical Standards Management"**

**Requirements**:
- CRUD operations for technical standards
- Filter by status (Active, Deprecated, Draft)
- Filter by type (CFE, NOM, ISO)
- Search by code

**Scenario Design (antes de implementar)**:
```
Sc040CreateTechnicalStandards:
- 10 Active standards (CFE, NOM, ISO mix)
- 3 Deprecated standards
- 2 Draft standards
- Edge cases: Codes with special characters (Ñ, É)
- Edge cases: Very long codes
- Variety of editions (1st, 2nd, 3rd)
```

#### Fase 2: Implementación del Feature

```
1. Implement Domain Layer (Entity, Validators)
2. Implement Infrastructure Layer (Repository, Mapper, Migration)
3. Implement Application Layer (Use Cases, DTOs)
4. ✅ IMPLEMENT SCENARIO - Using repositories
5. ✅ GENERATE XML - Run scenarios generator
6. Implement Repository Tests (using scenario)
7. Implement WebApi Layer (Endpoints)
8. Implement Endpoint Tests (using scenario)
```

**Example**:

**Step 4: Implement Scenario**

```csharp
public class Sc040CreateTechnicalStandards(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateTechnicalStandards";

    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);

    public async Task SeedData()
    {
        var standards = new List<(string Code, string Status, string Type, string Name)>
        {
            // Active CFE standards
            ("CFE-G0100-04", "Active", "CFE", "Especificaciones de Transformadores"),
            ("CFE-L0000-45", "Active", "CFE", "Libro de Especificaciones"),

            // Active NOM standards
            ("NOM-001-SEDE-2012", "Active", "NOM", "Instalaciones Eléctricas"),
            ("NOM-008-SCFI-2002", "Active", "NOM", "Sistema General de Unidades"),

            // Active ISO standards
            ("ISO-9001-2015", "Active", "ISO", "Quality Management Systems"),

            // Deprecated standards
            ("CFE-G0100-03", "Deprecated", "CFE", "Especificaciones (Old Edition)"),
            ("NOM-001-SEDE-1999", "Deprecated", "NOM", "Instalaciones (Old)"),

            // Draft standards
            ("CFE-G0200-01", "Draft", "CFE", "New Specifications"),

            // Edge cases
            ("NOM-Ñ01", "Active", "NOM", "Estándar con Ñ"),
            ("CFE-É01", "Active", "CFE", "Standard with Accent"),
        };

        try
        {
            _uoW.BeginTransaction();

            foreach (var (code, status, type, name) in standards)
            {
                await _uoW.TechnicalStandards.CreateAsync(
                    code: code,
                    name: name,
                    status: status,
                    type: type,
                    edition: "1st",
                    creationDate: DateTime.UtcNow
                );
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

**Step 5: Generate XML**

```bash
cd tests/hashira.stone.backend.scenarios

dotnet run \
  /cnn:"Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=root" \
  /output:"D:\scenarios"
```

**Output**:
```
✅ Generated: D:\scenarios\CreateTechnicalStandards.xml
```

**Step 6: Write Repository Tests**

```csharp
public class NHTechnicalStandardRepositoryTests : NHRepositoryTestBase<...>
{
    [Test]
    public async Task GetByStatusAsync_WithActive_ReturnsOnlyActive()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");  // ✅ Use scenario

        // Act
        var active = await RepositoryUnderTest.GetByStatusAsync("Active");

        // Assert
        active.Should().HaveCount(6);  // 5 Active + 2 edge cases
        active.Should().OnlyContain(s => s.Status == "Active");
    }

    [Test]
    public async Task GetByCodeAsync_WithSpecialCharacters_FindsCorrectly()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");

        // Act
        var standard = await RepositoryUnderTest.GetByCodeAsync("NOM-Ñ01");

        // Assert
        standard.Should().NotBeNull();
        standard!.Name.Should().Be("Estándar con Ñ");
    }
}
```

**Step 8: Write Endpoint Tests**

```csharp
public class GetTechnicalStandardsEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GET_TechnicalStandards_WithStatusFilter_ReturnsFiltered()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");  // ✅ Same scenario
        httpClient = CreateClient("usuario1@example.com");

        // Act
        var response = await httpClient.GetAsync("/technical-standards?status=Active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadAsAsync<GetManyResult<TechnicalStandardDto>>();
        result.Items.Should().HaveCount(6);
        result.Items.Should().OnlyContain(s => s.Status == "Active");
    }
}
```

### Benefits of This Workflow

| Aspecto | Benefit |
|---------|---------|
| **Design** | Pensar en datos de prueba obliga a considerar edge cases temprano |
| **Implementation** | Scenario usa repositorios (valida que funcionan correctamente) |
| **Testing** | Tests usan datos predecibles y variados desde el inicio |
| **Maintenance** | Un scenario sirve para repository tests Y endpoint tests |
| **Documentation** | Scenario documenta qué casos cubre el feature |

### Maintenance: Updating Scenarios

**Cuando agregar un nuevo scenario**:
- ✅ Nuevo feature requiere datos específicos
- ✅ Tests necesitan un estado particular que ningún scenario existente provee
- ✅ Simplificar scenarios existentes que hacen demasiado

**Cuando modificar un scenario existente**:
- ✅ Bug encontrado que necesita un edge case adicional
- ✅ Feature evolucionó y necesita más variedad de datos
- ✅ Entity cambió (new fields, validations)

**Proceso para modificar un scenario**:

```
1. Update Scenario SeedData() implementation
2. ✅ IMPORTANT: Update migration if entity changed
3. ✅ IMPORTANT: Update AppSchema.xsd if table structure changed
4. Re-run scenarios generator
5. Verify tests still pass (or update assertions)
6. Commit scenario code + XML files
```

**Example: Adding a new status to TechnicalStandard**

```csharp
// Before
public async Task SeedData()
{
    var standards = new List<(string Code, string Status)>
    {
        ("CFE-001", "Active"),
        ("CFE-002", "Deprecated"),
    };
}

// After - Add "Archived" status
public async Task SeedData()
{
    var standards = new List<(string Code, string Status)>
    {
        ("CFE-001", "Active"),
        ("CFE-002", "Deprecated"),
        ("CFE-003", "Archived"),  // ✅ New edge case
    };
}
```

**Re-generate**:
```bash
dotnet run /cnn:"..." /output:"D:\scenarios"
```

**Update tests**:
```csharp
[Test]
public async Task GetByStatus_WithArchived_ReturnsArchivedStandards()
{
    // Arrange
    LoadScenario("CreateTechnicalStandards");  // ✅ Now includes Archived

    // Act
    var archived = await RepositoryUnderTest.GetByStatusAsync("Archived");

    // Assert
    archived.Should().ContainSingle()
        .Which.Code.Should().Be("CFE-003");
}
```

---

## Ejemplos Completos

### Example 1: User Management Feature

#### Scenario Hierarchy

```
Sc010CreateSandBox (Empty DB)
    |
    v
Sc020CreateRoles (1 Role: PlatformAdministrator)
    |
    v
Sc030CreateUsers (1 Role + 5 Users)
    |
    v
Sc031CreateAdminUser (1 Role + 5 Users + 1 Admin Assignment)
```

#### Scenario Implementations

**Sc010CreateSandBox.cs**:
```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc010CreateSandBox : IScenario
{
    public string ScenarioFileName => "CreateSandBox";
    public Type? PreloadScenario => null;
    public Task SeedData() => Task.CompletedTask;
}
```

**Sc020CreateRoles.cs**:
```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateRoles";
    public Type? PreloadScenario => typeof(Sc010CreateSandBox);

    public Task SeedData()
        => _uoW.Roles.CreateDefaultRoles();
}
```

**Sc030CreateUsers.cs**:
```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
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
                await _uoW.Users.CreateAsync(email, name);
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

**Sc031CreateAdminUser.cs**:
```csharp
namespace hashira.stone.backend.scenarios.scenarios;

public class Sc031CreateAdminUser(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateAdminUser";
    public Type? PreloadScenario => typeof(Sc030CreateUsers);

    public async Task SeedData()
    {
        try
        {
            _uoW.BeginTransaction();

            var adminUser = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
            if (adminUser == null)
                throw new InvalidOperationException("usuario1 not found");

            var adminRole = await _uoW.Roles.GetByNameAsync(RolesResources.PlatformAdministrator);
            if (adminRole == null)
                throw new InvalidOperationException("PlatformAdministrator role not found");

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

#### Repository Tests Using Scenarios

**NHUserRepositoryTests.cs**:
```csharp
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    [Test]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        LoadScenario("CreateUsers");
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
        LoadScenario("CreateAdminUser");
        var adminUser = await RepositoryUnderTest.GetByEmailAsync("usuario1@example.com");

        // Assert
        adminUser.Should().NotBeNull();
        adminUser!.Roles.Should().ContainSingle()
            .Which.Name.Should().Be(RolesResources.PlatformAdministrator);
    }

    private string? GetFirstUserEmailFromDb()
    {
        var dataSet = nDbUnitTest.GetDataSetFromDb();
        var userRow = dataSet.GetFirstUserRow();
        return userRow?["email"].ToString();
    }
}
```

#### Endpoint Tests Using Scenarios

**GetManyAndCountUsersEndpointTests.cs**:
```csharp
internal class GetManyAndCountUsersEndpointTests : EndpointTestBase
{
    [Test]
    public async Task GET_Users_WithoutFilters_ReturnsAllUsers()
    {
        // Arrange
        LoadScenario("CreateUsers");
        var authenticatedUserName = "usuario1@example.com";
        httpClient = CreateClient(authenticatedUserName);

        // Act
        var response = await httpClient.GetAsync("/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GetManyAndCountResultDto<UserDto>>(content);

        result.Should().NotBeNull();
        result!.Count.Should().Be(5);
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
        LoadScenario("CreateAdminUser");  // Uses admin scenario
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
            u.Roles.Contains(RolesResources.PlatformAdministrator));
    }
}
```

### Example 2: Technical Standards Feature

#### Scenario Design

```csharp
public class Sc040CreateTechnicalStandards(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateTechnicalStandards";
    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);

    public async Task SeedData()
    {
        var standards = new List<TechnicalStandardData>
        {
            // Active CFE standards
            new("CFE-G0100-04", "Active", "CFE", "Especificaciones de Transformadores", "3rd"),
            new("CFE-L0000-45", "Active", "CFE", "Libro de Especificaciones", "2nd"),
            new("CFE-E0000-27", "Active", "CFE", "Especificaciones Generales", "1st"),

            // Active NOM standards
            new("NOM-001-SEDE-2012", "Active", "NOM", "Instalaciones Eléctricas", "4th"),
            new("NOM-008-SCFI-2002", "Active", "NOM", "Sistema General de Unidades", "2nd"),

            // Active ISO standards
            new("ISO-9001-2015", "Active", "ISO", "Quality Management Systems", "5th"),
            new("ISO-14001-2015", "Active", "ISO", "Environmental Management", "3rd"),

            // Deprecated standards (old editions)
            new("CFE-G0100-03", "Deprecated", "CFE", "Especificaciones (Old Edition)", "2nd"),
            new("CFE-L0000-44", "Deprecated", "CFE", "Libro (Old Edition)", "1st"),
            new("NOM-001-SEDE-1999", "Deprecated", "NOM", "Instalaciones (Old)", "3rd"),

            // Draft standards (in development)
            new("CFE-G0200-01", "Draft", "CFE", "New Specifications", "1st"),
            new("NOM-020-SEDE-2024", "Draft", "NOM", "New Electrical Standard", "1st"),

            // Edge cases: Special characters
            new("NOM-Ñ01", "Active", "NOM", "Estándar con Ñ", "1st"),
            new("CFE-É01", "Active", "CFE", "Standard with Accent É", "1st"),
            new("ISO-Ø01", "Active", "ISO", "Standard with Nordic Ø", "1st"),

            // Edge case: Very long code
            new("VERY-LONG-STANDARD-CODE-FOR-TESTING-LIMITS-9999", "Active", "CFE",
                "Standard with Very Long Code", "1st"),
        };

        try
        {
            _uoW.BeginTransaction();

            foreach (var std in standards)
            {
                await _uoW.TechnicalStandards.CreateAsync(
                    code: std.Code,
                    name: std.Name,
                    status: std.Status,
                    type: std.Type,
                    edition: std.Edition,
                    creationDate: DateTime.UtcNow
                );
            }

            _uoW.Commit();
        }
        catch
        {
            _uoW.Rollback();
            throw;
        }
    }

    private record TechnicalStandardData(
        string Code,
        string Status,
        string Type,
        string Name,
        string Edition);
}
```

#### Repository Tests

**NHTechnicalStandardRepositoryTests.cs**:
```csharp
public class NHTechnicalStandardRepositoryTests : NHRepositoryTestBase<...>
{
    [Test]
    public async Task GetByStatusAsync_WithActive_ReturnsOnlyActive()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");

        // Act
        var active = await RepositoryUnderTest.GetByStatusAsync("Active");

        // Assert
        active.Should().HaveCount(10);  // 7 normal + 3 edge cases
        active.Should().OnlyContain(s => s.Status == "Active");
    }

    [Test]
    public async Task GetByTypeAsync_WithCFE_ReturnsOnlyCFE()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");

        // Act
        var cfe = await RepositoryUnderTest.GetByTypeAsync("CFE");

        // Assert
        cfe.Should().HaveCount(7);  // 3 Active + 2 Deprecated + 1 Draft + 1 edge case
        cfe.Should().OnlyContain(s => s.Type == "CFE");
    }

    [Test]
    public async Task GetByCodeAsync_WithSpecialCharacters_FindsCorrectly()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");

        // Act
        var spanish = await RepositoryUnderTest.GetByCodeAsync("NOM-Ñ01");
        var french = await RepositoryUnderTest.GetByCodeAsync("CFE-É01");
        var nordic = await RepositoryUnderTest.GetByCodeAsync("ISO-Ø01");

        // Assert
        spanish.Should().NotBeNull();
        spanish!.Name.Should().Be("Estándar con Ñ");

        french.Should().NotBeNull();
        french!.Name.Should().Be("Standard with Accent É");

        nordic.Should().NotBeNull();
        nordic!.Name.Should().Be("Standard with Nordic Ø");
    }

    [Test]
    public async Task SearchAsync_WithPartialCode_ReturnsMatches()
    {
        // Arrange
        LoadScenario("CreateTechnicalStandards");

        // Act
        var results = await RepositoryUnderTest.SearchAsync("CFE-G");

        // Assert
        results.Should().HaveCount(3);  // CFE-G0100-04, CFE-G0100-03, CFE-G0200-01
        results.Should().OnlyContain(s => s.Code.StartsWith("CFE-G"));
    }
}
```

---

## Anti-patrones y Errores Comunes

### Anti-patrón 1: Random Data in Scenarios

**❌ INCORRECTO**:
```csharp
public async Task SeedData()
{
    var random = new Random();

    for (int i = 0; i < 10; i++)
    {
        await _uoW.Users.CreateAsync(
            email: $"user{random.Next(1000, 9999)}@example.com",
            name: $"User {Guid.NewGuid()}"
        );
    }
}
```

**Problemas**:
- Tests no pueden referenciar datos específicos
- XML files cambian cada vez que se re-genera
- Debugging difícil (¿qué datos se generaron?)
- Tests no determinísticos

**✅ CORRECTO**:
```csharp
public async Task SeedData()
{
    var users = new List<(string Email, string Name)>
    {
        ("usuario1@example.com", "Carlos Rodríguez"),
        ("usuario2@example.com", "Ana María González"),
        // ... well-known users
    };

    foreach (var (email, name) in users)
        await _uoW.Users.CreateAsync(email, name);
}
```

### Anti-patrón 2: Scenarios que hacen demasiado

**❌ INCORRECTO**:
```csharp
public class Sc030CreateEverything(IUnitOfWork uoW) : IScenario
{
    public async Task SeedData()
    {
        // Creates users
        await _uoW.Users.CreateAsync(...);

        // AND roles
        await _uoW.Roles.CreateAsync(...);

        // AND standards
        await _uoW.TechnicalStandards.CreateAsync(...);

        // AND prototypes
        await _uoW.Prototypes.CreateAsync(...);

        // ❌ Difícil de mantener, imposible de reutilizar parcialmente
    }
}
```

**✅ CORRECTO**: Scenarios separados
```csharp
Sc020CreateRoles
Sc030CreateUsers (depends on Sc020)
Sc040CreateTechnicalStandards (depends on Sc030)
Sc050CreatePrototypes (depends on Sc040)
```

### Anti-patrón 3: No usar Transactions

**❌ INCORRECTO**:
```csharp
public async Task SeedData()
{
    // ❌ No transaction - si falla a la mitad, datos inconsistentes
    await _uoW.Users.CreateAsync("user1@test.com", "User 1");
    await _uoW.Users.CreateAsync("user2@test.com", "User 2");
    await _uoW.Users.CreateAsync("invalid", "User 3");  // Falla aquí
}
```

**Resultado**: BD tiene user1 y user2, pero no user3. XML será inconsistente.

**✅ CORRECTO**:
```csharp
public async Task SeedData()
{
    try
    {
        _uoW.BeginTransaction();

        await _uoW.Users.CreateAsync("user1@test.com", "User 1");
        await _uoW.Users.CreateAsync("user2@test.com", "User 2");
        await _uoW.Users.CreateAsync("user3@test.com", "User 3");

        _uoW.Commit();  // ✅ All or nothing
    }
    catch
    {
        _uoW.Rollback();
        throw;
    }
}
```

### Anti-patrón 4: Scenarios muy granulares

**❌ INCORRECTO**:
```csharp
Sc030CreateUser1
Sc031CreateUser2
Sc032CreateUser3
Sc033CreateUser4
Sc034CreateUser5
```

**Problemas**:
- Demasiados scenarios para mantener
- Tests necesitan cargar múltiples scenarios
- Overhead de generation

**✅ CORRECTO**:
```csharp
Sc030CreateUsers  // Creates all 5 users at once
```

### Anti-patrón 5: Hardcoded GUIDs

**❌ INCORRECTO**:
```csharp
public async Task SeedData()
{
    // ❌ Hardcoded GUID - conflicts con otros scenarios
    var user = new User
    {
        Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
        Email = "user@test.com"
    };
    await _uoW.Users.SaveAsync(user);
}
```

**✅ CORRECTO**:
```csharp
public async Task SeedData()
{
    // ✅ Let repository/database generate IDs
    await _uoW.Users.CreateAsync("user@test.com", "User Name");
    // ID is auto-generated and captured in XML
}
```

### Anti-patrón 6: Scenarios sin PreloadScenario claro

**❌ INCORRECTO**:
```csharp
public class Sc040CreateTechnicalStandards : IScenario
{
    public Type? PreloadScenario => null;  // ❌ Necesita users para funcionar

    public async Task SeedData()
    {
        // Assumes users exist, but doesn't declare dependency
        var user = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
        // ...
    }
}
```

**Resultado**: Si scenario se ejecuta sin CreateUsers primero, falla.

**✅ CORRECTO**:
```csharp
public class Sc040CreateTechnicalStandards : IScenario
{
    public Type? PreloadScenario => typeof(Sc031CreateAdminUser);  // ✅ Clear dependency

    public async Task SeedData()
    {
        // usuario1@example.com guaranteed to exist
        var user = await _uoW.Users.GetByEmailAsync("usuario1@example.com");
        // ...
    }
}
```

### Anti-patrón 7: No actualizar AppSchema.xsd

**❌ INCORRECTO**:
```csharp
// Added new column to users table: last_login_date
// But forgot to update AppSchema.xsd
```

**Resultado**: XML no incluye la nueva columna, tests con datos incompletos.

**✅ CORRECTO**:
```xml
<!-- AppSchema.xsd updated -->
<xs:element name="public.users">
  <xs:complexType>
    <xs:sequence>
      <xs:element name="id" type="xs:string" minOccurs="0" />
      <xs:element name="email" type="xs:string" minOccurs="0" />
      <xs:element name="name" type="xs:string" minOccurs="0" />
      <xs:element name="last_login_date" type="xs:dateTime" minOccurs="0" />  <!-- ✅ New -->
    </xs:sequence>
  </xs:complexType>
</xs:element>
```

---

## Best Practices

### 1. Scenario Naming and Organization

✅ Use increments of 10 para numbering (Sc010, Sc020, Sc030)
✅ PascalCase para nombres descriptivos
✅ Verbo + Noun (CreateUsers, AssignRoles, ArchiveStandards)
✅ ScenarioFileName sin prefijo Sc{Level}

### 2. Data Design

✅ Well-known, predictable data (no random)
✅ Realistic variety (different statuses, types, edge cases)
✅ 5-30 records per scenario (enough for tests, not too many)
✅ Edge cases: special characters, long strings, null handling
✅ Comments documenting intent of specific data

### 3. Dependencies

✅ PreloadScenario apunta solo al parent inmediato
✅ Tree structure, no linear chain
✅ Minimal dependencies (no cargar más de lo necesario)
✅ Null checks si scenario modifica datos del preload

### 4. Transaction Management

✅ Always use BeginTransaction/Commit/Rollback
✅ Try-catch-rollback pattern
✅ Throw exceptions on failure (not swallow)

### 5. Generation and Maintenance

✅ Re-generate XML después de modificar scenarios
✅ Commit both scenario code and XML files
✅ Environment variable SCENARIOS_FOLDER_PATH in .env
✅ Update AppSchema.xsd cuando cambia schema de BD

### 6. Test Integration

✅ LoadScenario() en Arrange section
✅ Use scenario data en assertions (no re-query unnecessarily)
✅ One LoadScenario per test (clear dependencies)
✅ ClearDatabase() en [SetUp] (test isolation)

### 7. Documentation

✅ XML summary documenting purpose and data
✅ Comments explaining edge cases
✅ README listing all scenarios and dependencies

---

## Checklist de Implementación

### Fase 1: Setup Inicial

- [ ] Create `tests/{ProjectName}.scenarios/` project
- [ ] Create `IScenario` interface
- [ ] Create `ScenarioBuilder` class
- [ ] Create `tests/{ProjectName}.common.tests/` project
- [ ] Create `AppSchema.xsd` (or copy from reference project)
- [ ] Generate `AppSchema.Designer.cs` (set Custom Tool = MSDataSetGenerator)
- [ ] Create `AppSchemaExtender.cs` with helper methods
- [ ] Create `INDbUnit` interface
- [ ] Create `NDbUnit` abstract base class
- [ ] Create `PostgreSQLNDbUnit` (or SQL Server variant)
- [ ] Create `NHRepositoryTestInfrastructureBase`
- [ ] Create `NHRepositoryTestBase<TRepo, T, TKey>`
- [ ] Create `EndpointTestBase`
- [ ] Add `SCENARIOS_FOLDER_PATH` to .env file

### Fase 2: Create First Scenario

- [ ] Create `Sc010CreateSandBox` (empty baseline)
- [ ] Run scenarios generator
- [ ] Verify `CreateSandBox.xml` generated (empty)
- [ ] Create first test using `LoadScenario("CreateSandBox")`
- [ ] Verify test passes with empty DB

### Fase 3: Create Foundation Scenarios

- [ ] Create `Sc020CreateRoles` (foundation entities)
- [ ] Run generator, verify XML
- [ ] Create test using `LoadScenario("CreateRoles")`
- [ ] Create `Sc030CreateUsers` (bulk creation)
- [ ] Run generator, verify XML includes roles + users
- [ ] Create tests for user repository

### Fase 4: Create Feature Scenarios

- [ ] Design scenario for each feature (part of feature development)
- [ ] Implement scenario SeedData()
- [ ] Run generator
- [ ] Write repository tests using scenario
- [ ] Write endpoint tests using scenario

### Fase 5: Maintenance

- [ ] Document all scenarios in README
- [ ] Add scenario generation to CI/CD (optional)
- [ ] Version control: commit scenario code + XML files
- [ ] Update scenarios when entities change
- [ ] Re-generate XML after updates

---

## Recursos Adicionales

### Documentación Relacionada

- [testing-conventions.md](../../best-practices/testing-conventions.md) - General testing patterns
- [nhibernate-configuration.md](../../infrastructure-layer/orm-implementations/nhibernate/nhibernate-configuration.md) - NHibernate setup
- [unit-of-work.md](../../infrastructure-layer/orm-implementations/nhibernate/unit-of-work.md) - UnitOfWork pattern

### Stack Tecnológico

| Componente | Versión | Propósito |
|------------|---------|-----------|
| NUnit | 4.2+ | Test framework |
| FluentAssertions | 8.5+ | Assertions expresivas |
| NHibernate | 5.5+ | ORM (usado en SeedData) |
| Npgsql | 8.0+ | PostgreSQL provider |
| System.Data | Built-in | DataSet, DbDataAdapter |

### External Resources

- [DataSet and DataTable in ADO.NET](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/dataset-datatable-dataview/)
- [XML Serialization in .NET](https://learn.microsoft.com/en-us/dotnet/standard/serialization/introducing-xml-serialization)
- [NUnit Documentation](https://docs.nunit.org/)

---

## Conclusión

El **Scenarios System** es una práctica fundamental para integration testing en proyectos APSYS. Al separar la preparación de datos de prueba del código bajo prueba:

✅ **Tests más confiables** - No fallan por bugs en código de setup
✅ **Tests más rápidos** - Cargan XML directamente, no ejecutan lógica de negocio
✅ **Tests más mantenibles** - Un scenario reutilizable en múltiples tests
✅ **Tests más predecibles** - Datos conocidos, no aleatorios
✅ **Mejor coverage** - Scenarios con variedad de edge cases

> **💡 Key Insight:** El diseño de scenarios es tan importante como el diseño de las entidades. Scenarios bien diseñados facilitan testing, mal diseñados lo dificultan.

El tiempo invertido en diseñar buenos scenarios durante el desarrollo del feature es una inversión que se recupera multiplicada en mantenibilidad y confiabilidad de tests.
