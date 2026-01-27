# Configuración de SQL Server

## Descripción

Configura **SQL Server** como base de datos del proyecto. Esta guía agrega:
- Paquete NuGet de Microsoft.Data.SqlClient
- ConnectionStringBuilder para SQL Server
- Variables de entorno requeridas
- Configuración de NHibernate para SQL Server

**Requiere:** [04-infrastructure-layer.md](../../../../architectures/clean-architecture/init/04-infrastructure-layer.md)

## Paquetes NuGet

```bash
dotnet add src/{ProjectName}.infrastructure/{ProjectName}.infrastructure.csproj package Microsoft.Data.SqlClient
```

## Variables de Entorno

Agregar a `.env`:

```env
# SQL Server
DB_HOST=localhost
DB_PORT=1433
DB_NAME={projectname}-db
DB_USER=sa
DB_PASSWORD=your_password
```

## Pasos

### 1. Copiar ConnectionStringBuilder

Copiar `docs/guides/stacks/database/sqlserver/templates/ConnectionStringBuilder.cs` a `src/{ProjectName}.infrastructure/nhibernate/`

### 2. Configurar NHSessionFactory

En `NHSessionFactory.cs`, usar el driver y dialect de SQL Server:

```csharp
cfg.DataBaseIntegration(c =>
{
    c.Driver<MicrosoftDataSqlClientDriver>();
    c.Dialect<MsSql2012Dialect>();
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
# Verificar que SQL Server está corriendo (usando sqlcmd)
sqlcmd -S localhost,1433 -U sa -P your_password -Q "SELECT @@VERSION"

# Verificar conexión desde .NET
dotnet build
dotnet run --project src/{ProjectName}.webapi
```

## Troubleshooting

### Error: "A network-related or instance-specific error"
- Verificar que SQL Server está corriendo
- Verificar host y puerto en variables de entorno
- Verificar que TCP/IP está habilitado en SQL Server Configuration Manager

### Error: "Login failed for user"
- Verificar credenciales en `.env`
- Verificar que el usuario tiene permisos en la base de datos

### Error: "Certificate chain was issued by an authority that is not trusted"
- La connection string incluye `TrustServerCertificate=True` para desarrollo
- En producción, configurar certificados correctamente

## Siguiente Paso

→ [NHibernate Setup](../../../orm/nhibernate/guides/setup.md) (si no lo has configurado)
