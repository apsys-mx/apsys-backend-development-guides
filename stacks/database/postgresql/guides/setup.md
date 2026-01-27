# Configuración de PostgreSQL

## Descripción

Configura **PostgreSQL** como base de datos del proyecto. Esta guía agrega:
- Paquete NuGet de Npgsql
- ConnectionStringBuilder para PostgreSQL
- Variables de entorno requeridas
- Configuración de NHibernate para PostgreSQL

**Requiere:** [04-infrastructure-layer.md](../../../../architectures/clean-architecture/init/04-infrastructure-layer.md)

## Paquetes NuGet

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package Npgsql
```

## Variables de Entorno

Agregar a `.env`:

```env
# PostgreSQL
DB_HOST=localhost
DB_PORT=5432
DB_NAME={projectname}-db
DB_USER=postgres
DB_PASSWORD=your_password
```

## Pasos

### 1. Copiar ConnectionStringBuilder

Copiar `docs/guides/stacks/database/postgresql/templates/ConnectionStringBuilder.cs` a `src/{ProjectName}.infrastructure/nhibernate/`

### 2. Configurar NHSessionFactory

En `NHSessionFactory.cs`, usar el driver y dialect de PostgreSQL:

```csharp
cfg.DataBaseIntegration(c =>
{
    c.Driver<NpgsqlDriver>();
    c.Dialect<PostgreSQL83Dialect>();
    c.ConnectionString = this.ConnectionString;
    c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
});
```

**Imports requeridos:**
```csharp
using NHibernate.Driver;
using NHibernate.Dialect;
```

### 3. Configurar DI en WebApi

En `src/{ProjectName}.webapi/infrastructure/NHibernateServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection ConfigureNHibernate(
    this IServiceCollection services)
{
    var connectionString = ConnectionStringBuilder.Build();

    services.AddSingleton(new NHSessionFactory(connectionString));
    services.AddScoped(sp =>
        sp.GetRequiredService<NHSessionFactory>().BuildNHibernateSessionFactory().OpenSession());

    return services;
}
```

## Verificación

```bash
# Verificar que PostgreSQL está corriendo
psql -h localhost -U postgres -c "SELECT version();"

# Verificar conexión desde .NET
dotnet build
dotnet run --project src/{ProjectName}.webapi
```

## Troubleshooting

### Error: "Npgsql.NpgsqlException: Connection refused"
- Verificar que PostgreSQL está corriendo
- Verificar host y puerto en variables de entorno

### Error: "password authentication failed"
- Verificar credenciales en `.env`
- Verificar que el usuario existe en PostgreSQL

## Siguiente Paso

→ [NHibernate Setup](../../../orm/nhibernate/guides/setup.md) (si no lo has configurado)
