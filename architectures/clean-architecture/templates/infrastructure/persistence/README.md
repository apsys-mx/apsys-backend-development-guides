# Persistence - Configuración de Acceso a Datos

## Propósito

Esta carpeta contiene la **configuración de persistencia** y acceso a datos: ORM, conexiones a base de datos, mappers, y configuración de entidades.

## Responsabilidades

1. ✅ Configurar ORM (NHibernate, Entity Framework, etc.)
2. ✅ Definir mapeos entre entidades de dominio y tablas de base de datos
3. ✅ Configurar conexiones y sesiones de base de datos
4. ✅ Gestionar migraciones y esquema (opcional, puede estar separado)

## Estructura Recomendada

### Con NHibernate

```
persistence/
├── SessionFactory.cs                 # Configuración del SessionFactory
├── mappers/
│   ├── UserMapper.cs                 # Mapeo de User
│   ├── ProductMapper.cs              # Mapeo de Product
│   └── OrderMapper.cs                # Mapeo de Order
└── NHibernateConfiguration.cs        # Configuración global
```

### Con Entity Framework

```
persistence/
├── AppDbContext.cs                   # DbContext principal
├── configurations/
│   ├── UserConfiguration.cs          # Configuración de User
│   ├── ProductConfiguration.cs       # Configuración de Product
│   └── OrderConfiguration.cs         # Configuración de Order
└── DbContextFactory.cs               # Factory para migraciones
```

### Con Dapper

```
persistence/
├── ConnectionFactory.cs              # Factory de conexiones
└── SqlMappers.cs                     # Configuración de mapeo custom
```

## Ejemplo: NHibernate SessionFactory

```csharp
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace {ProjectName}.infrastructure.persistence;

public class SessionFactoryBuilder
{
    public static ISessionFactory Build(string connectionString)
    {
        var configuration = new Configuration();

        // Configuración de base de datos
        configuration.DataBaseIntegration(db =>
        {
            db.ConnectionString = connectionString;
            db.Dialect<PostgreSQLDialect>();
            db.Driver<NpgsqlDriver>();
            db.LogSqlInConsole = true;
        });

        // Mapeo de entidades
        var mapper = new ModelMapper();
        mapper.AddMappings(typeof(SessionFactoryBuilder).Assembly.GetTypes());

        var mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
        configuration.AddMapping(mapping);

        return configuration.BuildSessionFactory();
    }
}
```

## Ejemplo: Entity Framework DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using {ProjectName}.domain.entities;

namespace {ProjectName}.infrastructure.persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas las configuraciones del assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

## Ejemplo: Mapper de Entidad (NHibernate)

```csharp
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using {ProjectName}.domain.entities;

namespace {ProjectName}.infrastructure.persistence.mappers;

public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Table("users");
        Schema("public");

        Id(x => x.Id, m =>
        {
            m.Column("id");
            m.Generator(Generators.Identity);
        });

        Property(x => x.Name, m =>
        {
            m.Column("name");
            m.NotNullable(true);
            m.Length(100);
        });

        Property(x => x.Email, m =>
        {
            m.Column("email");
            m.NotNullable(true);
            m.Length(255);
            m.Unique(true);
        });
    }
}
```

## Ejemplo: Configuration de Entidad (Entity Framework)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {ProjectName}.domain.entities;

namespace {ProjectName}.infrastructure.persistence.configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "public");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Email)
            .IsUnique();
    }
}
```

## Principios

### 1. Mapeo Declarativo

Usa configuración declarativa en lugar de atributos en las entidades de dominio:

```csharp
// ❌ INCORRECTO - Contamina el dominio
public class User
{
    [Column("id")]
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
}

// ✅ CORRECTO - Configuración separada en Infrastructure
public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; }
}

// Configuración en UserMapper.cs
```

### 2. Connection Strings en Configuración

Nunca hardcodear connection strings:

```csharp
// ❌ INCORRECTO
var connectionString = "Server=localhost;Database=mydb;...";

// ✅ CORRECTO
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? configuration.GetConnectionString("DefaultConnection");
```

### 3. Autodescubrimiento de Mappers

Usa reflexión para cargar automáticamente todos los mappers/configuraciones:

```csharp
// NHibernate
mapper.AddMappings(typeof(SessionFactoryBuilder).Assembly.GetTypes());

// Entity Framework
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

## Next Steps

Para configurar persistencia con una tecnología específica:

- **NHibernate + PostgreSQL**: Ver `guides/stack-implementations/nhibernate/02-setup-persistence.md`
- **Entity Framework + SQL Server**: Ver `guides/stack-implementations/entityframework/02-setup-persistence.md`
- **Dapper + MySQL**: Ver `guides/stack-implementations/dapper/02-setup-persistence.md`
