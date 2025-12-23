# Configuración de FluentMigrator

## Descripción

Configura **FluentMigrator** como sistema de migraciones de base de datos. Esta guía agrega:
- Proyecto console `{ProjectName}.migrations`
- Migration runner con CLI
- Custom version table
- Estructura base para migraciones

**Requiere:**
- [04-infrastructure-layer.md](../../../../architectures/clean-architecture/init/04-infrastructure-layer.md)
- **Base de datos configurada** (elegir una):
  - [PostgreSQL](../../postgresql/guides/setup.md)
  - [SQL Server](../../sqlserver/guides/setup.md)

## Estructura Final

```
src/{ProjectName}.migrations/
├── {ProjectName}.migrations.csproj
├── Program.cs                      ← Migration runner
├── CommandLineArgs.cs              ← CLI argument parser
├── CustomVersionTableMetaData.cs   ← Version tracking config
└── migrations/
    └── M001_InitialMigration.cs    ← Primera migración
```

## Paquetes NuGet

Los paquetes ya están definidos en `Directory.Packages.props`:

```xml
<PackageVersion Include="FluentMigrator" Version="7.1.0" />
<PackageVersion Include="FluentMigrator.Runner" Version="7.1.0" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.7" />
<PackageVersion Include="Spectre.Console" Version="0.50.0" />
```

## Pasos

### 1. Crear proyecto console

```bash
cd src
dotnet new console -n {ProjectName}.migrations
dotnet sln ../{ProjectName}.sln add {ProjectName}.migrations/{ProjectName}.migrations.csproj
```

### 2. Configurar .csproj

Editar `src/{ProjectName}.migrations/{ProjectName}.migrations.csproj`:

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
    <PackageReference Include="Spectre.Console" />
    <!-- Driver de BD - elegir uno -->
    <PackageReference Include="Npgsql" />                    <!-- PostgreSQL -->
    <!-- <PackageReference Include="Microsoft.Data.SqlClient" /> --> <!-- SQL Server -->
  </ItemGroup>
</Project>
```

### 3. Crear carpeta de migraciones

```bash
mkdir src/{ProjectName}.migrations/migrations
```

### 4. Copiar templates

Copiar desde `stacks/database/migrations/fluent-migrator/templates/` a `src/{ProjectName}.migrations/`:

| Template | Destino | Descripción |
|----------|---------|-------------|
| `Program.cs` | raíz | Migration runner con CLI |
| `CommandLineArgs.cs` | raíz | Parser de argumentos CLI |
| `CustomVersionTableMetaData.cs` | raíz | Configuración de tabla de versiones |
| `M001_InitialMigration.cs` | `migrations/` | Migración de ejemplo |

### 5. Configurar driver de BD

Editar `Program.cs` según la base de datos:

**Para PostgreSQL:**
```csharp
.ConfigureRunner(rb => rb
    .AddPostgres11_0()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(M001_InitialMigration).Assembly).For.Migrations())
```

**Para SQL Server:**
```csharp
.ConfigureRunner(rb => rb
    .AddSqlServer()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(M001_InitialMigration).Assembly).For.Migrations())
```

### 6. Reemplazar namespaces

En todos los archivos copiados, reemplazar:
- `{ProjectName}` → nombre real del proyecto

## Ejecutar Migraciones

### Desarrollo local

```bash
cd src/{ProjectName}.migrations

# Aplicar migraciones
dotnet run cnn="Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=pass"

# Revertir última migración
dotnet run cnn="Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=pass" action=rollback
```

### Usando variables de entorno

```bash
# Construir connection string desde .env
source ../{ProjectName}.webapi/.env
dotnet run cnn="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
```

## Crear Nueva Migración

1. Determinar el siguiente número de versión (revisar `migrations/`)
2. Crear archivo `migrations/M{NNN}_{Description}.cs`:

```csharp
using FluentMigrator;

namespace {ProjectName}.migrations.migrations;

[Migration(2)]
public class M002_CreateUsersTable : Migration
{
    private const string TableName = "users";
    private const string SchemaName = "public";

    public override void Up()
    {
        Create.Table(TableName)
            .InSchema(SchemaName)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString(255).NotNullable().Unique()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(TableName).InSchema(SchemaName);
    }
}
```

## Verificación

```bash
# Compilar proyecto
dotnet build src/{ProjectName}.migrations

# Ejecutar migraciones
dotnet run --project src/{ProjectName}.migrations cnn="..."

# Verificar en BD
SELECT * FROM public.versioninfo ORDER BY version;
```

## Convenciones

| Elemento | Formato | Ejemplo |
|----------|---------|---------|
| Archivo | `M{NNN}_{Description}.cs` | `M001_InitialMigration.cs` |
| Clase | `M{NNN}_{Description}` | `M001_InitialMigration` |
| Atributo | `[Migration(N)]` | `[Migration(1)]` |
| Tabla | `snake_case` | `users`, `user_roles` |
| Columna | `snake_case` | `created_at`, `user_id` |

## Siguiente Paso

→ Crear entidades de dominio y sus migraciones correspondientes

## Guías Relacionadas

- [Best Practices](./best-practices.md) - Mejores prácticas de migraciones
- [Patterns](./patterns.md) - Patrones de migración (FK, índices, vistas)
