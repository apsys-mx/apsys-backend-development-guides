# NDbUnit - Setup Guide

**Version:** 1.0.0
**Ultima actualizacion:** 2025-12-29

## Descripcion

NDbUnit es una libreria que permite cargar y limpiar datos de prueba en la base de datos. Se utiliza en conjunto con el generador de escenarios para pruebas de integracion.

---

## Estructura del Proyecto

```
tests/
└── {ProjectName}.ndbunit/
    ├── {ProjectName}.ndbunit.csproj
    ├── INDbUnit.cs                 # Interface base
    ├── NDbUnit.cs                  # Clase abstracta
    └── PostgreSQLNDbUnit.cs        # Implementacion PostgreSQL
    └── SqlServerNDbUnit.cs         # Implementacion SQL Server (si aplica)
```

---

## Crear el Proyecto

### 1. Crear proyecto de libreria

```bash
cd tests
dotnet new classlib -n {ProjectName}.ndbunit
cd ..
dotnet sln add tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj
```

### 2. Configurar csproj

Editar `tests/{ProjectName}.ndbunit/{ProjectName}.ndbunit.csproj`:

**Para PostgreSQL:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Npgsql" />
  </ItemGroup>

</Project>
```

**Para SQL Server:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" />
  </ItemGroup>

</Project>
```

### 3. Eliminar Class1.cs

```bash
rm tests/{ProjectName}.ndbunit/Class1.cs
```

### 4. Copiar templates

Copiar los siguientes archivos desde `docs/guides/testing/integration/tools/ndbunit/templates/project/`:

| Archivo | Descripcion |
|---------|-------------|
| `INDbUnit.cs` | Interface que define operaciones de base de datos |
| `NDbUnit.cs` | Clase abstracta con implementacion base |
| `PostgreSQLNDbUnit.cs` | Implementacion para PostgreSQL |
| `SqlServerNDbUnit.cs` | Implementacion para SQL Server (si aplica) |

### 5. Reemplazar placeholders

En todos los archivos, reemplazar:
- `{ProjectName}` → Nombre del proyecto (ej: `miproyecto`)

---

## Componentes

### INDbUnit - Interface

Define las operaciones principales:

```csharp
public interface INDbUnit
{
    DataSet DataSet { get; }
    string ConnectionString { get; }
    DataSet GetDataSetFromDb();
    DbDataAdapter CreateDataAdapter();
    void ClearDatabase();
    void SeedDatabase(DataSet dataSet);
}
```

| Metodo | Descripcion |
|--------|-------------|
| `GetDataSetFromDb()` | Lee todos los datos de la BD y los retorna como DataSet |
| `ClearDatabase()` | Elimina todos los registros de las tablas del schema |
| `SeedDatabase()` | Inserta los datos del DataSet en la BD |

### NDbUnit - Clase Abstracta

Implementa la logica comun:

- Manejo de transacciones
- Iteracion sobre tablas del DataSet
- Deshabilitar/habilitar constraints

Los metodos abstractos que cada implementacion debe proveer:

```csharp
public abstract DbConnection CreateConnection();
public abstract DbDataAdapter CreateDataAdapter();
public abstract DbCommandBuilder CreateCommandBuilder(DbDataAdapter dataAdapter);
protected abstract void EnabledTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);
protected abstract void DisableTableConstraints(IDbTransaction dbTransaction, DataTable dataTable);
```

### PostgreSQLNDbUnit

Implementacion especifica para PostgreSQL:

- Usa `NpgsqlConnection`, `NpgsqlDataAdapter`, `NpgsqlCommandBuilder`
- Deshabilita triggers con `ALTER TABLE ... DISABLE TRIGGER ALL`

### SqlServerNDbUnit

Implementacion especifica para SQL Server:

- Usa `SqlConnection`, `SqlDataAdapter`, `SqlCommandBuilder`
- Deshabilita constraints con `ALTER TABLE ... NOCHECK CONSTRAINT ALL`

---

## Uso en Tests

### Configuracion en Test Base

```csharp
public abstract class NHRepositoryTestInfrastructureBase
{
    protected internal INDbUnit nDbUnitTest;
    protected internal ISession session;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var connectionString = "Host=localhost;Database=test_db;...";
        var schema = new AppSchema();
        this.nDbUnitTest = new PostgreSQLNDbUnit(schema, connectionString);

        // Configurar NHibernate session...
    }
}
```

### Limpiar BD antes de cada test

```csharp
[SetUp]
public void Setup()
{
    this.nDbUnitTest.ClearDatabase();
}
```

### Cargar escenario

```csharp
[Test]
public async Task TestWithScenario()
{
    // Cargar datos del escenario
    var dataSet = new AppSchema();
    dataSet.ReadXml("path/to/CreateUsers.xml");
    this.nDbUnitTest.SeedDatabase(dataSet);

    // Ejecutar test...
}
```

---

## Dependencias

El proyecto ndbunit es utilizado por:

| Proyecto | Uso |
|----------|-----|
| `{ProjectName}.scenarios` | Generar XMLs de escenarios |
| `{ProjectName}.infrastructure.tests` | Cargar escenarios en tests |
| `{ProjectName}.webapi.tests` | Cargar escenarios en tests de integracion |

---

## Templates

Los templates se encuentran en:

```
docs/guides/testing/integration/tools/ndbunit/templates/
├── project/                        # Templates del proyecto library
│   ├── INDbUnit.cs
│   ├── NDbUnit.cs
│   ├── PostgreSQLNDbUnit.cs
│   ├── SqlServerNDbUnit.cs
│   └── ndbunit.csproj.template
└── NHRepositoryTestBase.cs         # Template para tests de repositorio
└── NHRepositoryTestInfrastructureBase.cs
```

---

## Changelog

| Version | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2025-12-29 | Version inicial |

---

**Ultima actualizacion:** 2025-12-29
**Mantenedor:** Equipo APSYS
