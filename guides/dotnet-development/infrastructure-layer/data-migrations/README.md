# Data Migrations - Infrastructure Layer

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## 📋 Tabla de Contenidos
1. [¿Qué son las Data Migrations?](#qué-son-las-data-migrations)
2. [Responsabilidades](#responsabilidades)
3. [Cuándo Usar Migrations](#cuándo-usar-migrations)
4. [Stack Tecnológico](#stack-tecnológico)
5. [Implementaciones Disponibles](#implementaciones-disponibles)
6. [Quick Start](#quick-start)
7. [Guías Disponibles](#guías-disponibles)
8. [Mejores Prácticas](#mejores-prácticas)
9. [Referencias](#referencias)

---

## ¿Qué son las Data Migrations?

Las **Data Migrations** (migraciones de datos) son cambios versionados y rastreables del esquema de base de datos, expresados como código. Permiten **evolucionar la estructura de la BD** a medida que el proyecto crece, manteniendo sincronizadas las versiones de código y esquema.

### 🎯 Características Clave

- ✅ **Versionado**: Cada migración tiene un número de versión único y secuencial
- ✅ **Reversibilidad**: Cada migración define `Up()` (aplicar) y `Down()` (revertir)
- ✅ **Rastreabilidad**: Se registra qué migraciones se han aplicado en cada ambiente
- ✅ **Control de versiones**: Las migraciones son código fuente (viven en Git)
- ✅ **Automatización**: Se ejecutan automáticamente en CI/CD o manualmente
- ✅ **Independencia de ambiente**: Mismas migraciones para dev, staging, producción

---

## Responsabilidades

### ✅ SÍ hacen las Data Migrations

- **Crear tablas**: Definir estructura de tablas nuevas
- **Modificar tablas**: Agregar/eliminar/modificar columnas
- **Crear índices**: Optimizar consultas con índices
- **Gestionar constraints**: PK, FK, UNIQUE, CHECK
- **Crear vistas**: Vistas de SQL para consultas optimizadas
- **Ejecutar SQL custom**: Scripts SQL específicos cuando sea necesario
- **Seed data**: Insertar datos iniciales o de referencia
- **Versionar cambios**: Mantener historial de cambios de esquema
- **Rollback**: Revertir cambios cuando sea necesario

### ❌ NO hacen las Data Migrations

- **Lógica de negocio**: La lógica va en Domain/Application
- **Validaciones de dominio**: Esto va en FluentValidation
- **Transformaciones complejas**: Usar scripts de datos por separado
- **Backups**: Esto es responsabilidad de infraestructura/DevOps
- **Modificar datos en producción**: Los datos de producción no se modifican en migraciones (solo esquema)

---

## Cuándo Usar Migrations

### ✅ Cuándo SÍ usar migrations

| Escenario | Descripción |
|-----------|-------------|
| **Nueva tabla** | Crear una tabla para una nueva entidad de dominio |
| **Nueva columna** | Agregar un campo a una tabla existente |
| **Cambiar tipo de dato** | Modificar el tipo de una columna (string → int) |
| **Crear índices** | Optimizar consultas con índices en columnas frecuentes |
| **Relaciones** | Agregar/modificar foreign keys entre tablas |
| **Constraints** | Agregar restricciones (UNIQUE, NOT NULL, CHECK) |
| **Vistas** | Crear vistas de SQL para DAOs read-only |
| **Refactoring de esquema** | Renombrar tablas/columnas, dividir tablas |
| **Seed data** | Insertar datos de referencia (roles, categorías) |

### ❌ Cuándo NO usar migrations

| Escenario | Alternativa |
|-----------|-------------|
| **Datos de producción** | Usar scripts SQL manuales auditados |
| **Backups** | Configurar backups automáticos del servidor |
| **Optimizaciones de queries** | Crear índices vía migration, no modificar queries |
| **Datos temporales de testing** | Usar seed data en ambiente de dev/test |
| **Hotfixes de datos** | Ejecutar scripts SQL directos (con backup previo) |

---

## Stack Tecnológico

### Proyecto de Referencia: hashira.stone.backend

**Herramienta**: FluentMigrator 7.1.0
**Base de datos**: PostgreSQL 11.0 (Npgsql driver)
**Ejecución**: Aplicación console con argumentos CLI

```xml
<ItemGroup>
  <PackageReference Include="FluentMigrator" Version="7.1.0" />
  <PackageReference Include="FluentMigrator.Runner" Version="7.1.0" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0+" />
  <PackageReference Include="Npgsql" Version="8.0+" />
  <PackageReference Include="Spectre.Console" Version="0.49+" />
</ItemGroup>
```

**Features del proyecto**:
- ✅ Migraciones con versionado numérico (`[Migration(24)]`)
- ✅ Console application para ejecutar migraciones
- ✅ Soporte para `run` (aplicar) y `rollback` (revertir)
- ✅ Custom version table para tracking
- ✅ Spectre.Console para UX mejorada en terminal

---

## Implementaciones Disponibles

### 1. FluentMigrator (Recomendado)

**Estado**: ✅ Usado en proyecto de referencia
**Versión**: 7.1.0
**Ventajas**:
- ✅ **ORM agnostic**: Funciona con cualquier ORM (NHibernate, EF, Dapper)
- ✅ **Soporte multi-base de datos**: SQL Server, PostgreSQL, MySQL, SQLite, Oracle
- ✅ **Fluent API**: Sintaxis legible y expresiva
- ✅ **Versionado explícito**: Control total sobre números de versión
- ✅ **Rollback**: Down() método para revertir cambios
- ✅ **Migrations como código**: Fuertemente tipado, compile-safe

**Desventajas**:
- ⚠️ **Setup manual**: Requiere proyecto console separado
- ⚠️ **No auto-migración**: No genera migraciones automáticamente desde entidades

**Cuándo usar**:
- Proyectos con NHibernate (como hashira.stone.backend)
- Cuando necesitas control total sobre el esquema
- Multi-database support

---

### 2. Entity Framework Migrations (Futuro)

**Estado**: ⏳ Pendiente de documentar
**Versión**: N/A
**Ventajas**:
- ✅ **Auto-generación**: Genera migraciones desde DbContext
- ✅ **Integración con EF**: Workflow integrado
- ✅ **Code-first**: Ideal para proyectos EF Core

**Desventajas**:
- ⚠️ **EF-only**: Requiere EF Core como ORM
- ⚠️ **Menos flexible**: Menos control granular del SQL

**Cuándo usar**:
- Proyectos con Entity Framework Core
- Code-first approach

---

## Quick Start

### Estructura de Proyecto

Basado en el proyecto real [hashira.stone.backend](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend):

```
solution/
├── src/
│   ├── {project}.migrations/              # ✅ Proyecto de migraciones
│   │   ├── M001Sandbox.cs                 # Migración #1
│   │   ├── M024CreateUsersTable.cs        # Migración #24
│   │   ├── M025TechnicalStandardsTable.cs
│   │   ├── M026TechnicalStandardsView.cs
│   │   ├── CustomVersionTableMetaData.cs  # Custom version tracking
│   │   ├── Program.cs                     # Migration runner
│   │   └── {project}.migrations.csproj
│   │
│   ├── {project}.domain/                  # Domain layer
│   ├── {project}.infrastructure/          # Infrastructure layer
│   └── {project}.webapi/                  # WebApi layer
│
└── Directory.Packages.props               # Centralized package versions
```

---

### Flujo de Trabajo: Crear y Aplicar Migración

#### 1️⃣ Crear nueva migración

```csharp
using FluentMigrator;

namespace hashira.stone.backend.migrations;

[Migration(29)]  // ← Número de versión único y secuencial
public class M029CreateProductsTable : Migration
{
    private readonly string _tableName = "products";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_tableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("name").AsString(200).NotNullable()
              .WithColumn("price").AsDecimal(18, 2).NotNullable()
              .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(_tableName)
            .InSchema(_schemaName);
    }
}
```

---

#### 2️⃣ Ejecutar migración

**Desarrollo local** (Windows):
```powershell
# Navegar al proyecto de migraciones
cd src\hashira.stone.backend.migrations

# Ejecutar migraciones (aplicar)
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass"

# Revertir última migración (rollback)
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass" action=rollback
```

**Desarrollo local** (Linux/Mac):
```bash
# Navegar al proyecto de migraciones
cd src/hashira.stone.backend.migrations

# Ejecutar migraciones
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass"

# Rollback
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass" action=rollback
```

---

#### 3️⃣ Verificar en base de datos

```sql
-- Ver tabla de versiones
SELECT * FROM public.versioninfo ORDER BY version DESC;

-- Resultado esperado:
-- version | appliedon           | description
-- --------|---------------------|---------------------------
-- 29      | 2025-11-14 10:30:00 | M029CreateProductsTable
-- 28      | 2025-11-10 09:15:00 | M028CreatePrototypesView
-- 27      | 2025-11-08 14:20:00 | M027CreatePrototypeTable
```

---

## Guías Disponibles

| Guía | Estado | Descripción |
|------|--------|-------------|
| [README.md](./README.md) | ✅ v1.0.0 | Overview de Data Migrations |
| [fluent-migrator/README.md](./fluent-migrator/README.md) | ✅ v1.0.0 | FluentMigrator setup y configuración |
| [fluent-migrator/migration-patterns.md](./fluent-migrator/migration-patterns.md) | ✅ v1.0.0 | Patrones de migración (tablas, índices, vistas, FK) |
| [fluent-migrator/best-practices.md](./fluent-migrator/best-practices.md) | ✅ v1.0.0 | Best practices de FluentMigrator |
| [ef-migrations/README.md](./ef-migrations/README.md) | ⏳ Futuro | Entity Framework Migrations (futuro) |

---

## Mejores Prácticas

### ✅ 1. Versionado secuencial

```csharp
// ✅ CORRECTO: Números secuenciales y únicos
[Migration(24)] public class M024CreateUsersTable : Migration { }
[Migration(25)] public class M025TechnicalStandardsTable : Migration { }
[Migration(26)] public class M026TechnicalStandardsView : Migration { }

// ❌ INCORRECTO: Números duplicados
[Migration(24)] public class M024CreateUsersTable : Migration { }
[Migration(24)] public class M024CreateRolesTable : Migration { }  // ← Conflicto
```

**Por qué**:
- FluentMigrator usa el número de versión para tracking
- Números duplicados causan errores en runtime
- Números no secuenciales dificultan debugging

---

### ✅ 2. Siempre implementar Down()

```csharp
// ✅ CORRECTO: Down() revierte los cambios de Up()
public class M024CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("email").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("users");  // ← Revierte la creación de tabla
    }
}

// ❌ INCORRECTO: Down() vacío
public override void Down()
{
    // No hacer nada
}
```

**Por qué**:
- Permite rollback en caso de problemas
- Facilita testing de migraciones
- Esencial para CI/CD pipelines

---

### ✅ 3. Usar constantes para nombres

```csharp
// ✅ CORRECTO: Constantes para reutilización
public class M024CreateUsersTable : Migration
{
    private readonly string _usersTableName = "users";
    private readonly string _rolesTableName = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_usersTableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey();

        Create.ForeignKey($"fk_{_usersTableName}_role_id")
              .FromTable(_usersTableName)  // ← Reutiliza constante
              .ToTable(_rolesTableName);
    }
}

// ❌ INCORRECTO: Hardcoded strings
public override void Up()
{
    Create.Table("users")  // ← String duplicado
          .WithColumn("id").AsGuid().PrimaryKey();

    Create.ForeignKey("fk_users_role_id")
          .FromTable("users");  // ← Propenso a typos
}
```

**Por qué**:
- Evita typos y errores de string
- Facilita refactoring
- Más legible y mantenible

---

### ✅ 4. Convenciones de nombres

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| **Clase** | `M{version}{Description}` | `M024CreateUsersTable` |
| **Tabla** | `snake_case` | `users`, `technical_standards` |
| **Columna** | `snake_case` | `created_at`, `user_id` |
| **Primary Key** | `pk_{table_name}` | `pk_users` |
| **Foreign Key** | `fk_{table}_{column}` | `fk_users_role_id` |
| **Index** | `ix_{table}_{column}` | `ix_users_email` |
| **Vista** | `{table}_view` | `technical_standards_view` |

---

### ✅ 5. Agregar índices para FK y queries frecuentes

```csharp
// ✅ CORRECTO: Índice en FK y columnas de búsqueda
public override void Up()
{
    Create.Table("users")
          .WithColumn("id").AsGuid().PrimaryKey()
          .WithColumn("email").AsString().NotNullable()
          .WithColumn("role_id").AsGuid().NotNullable();

    // FK con índice automático
    Create.ForeignKey("fk_users_role_id")
          .FromTable("users").ForeignColumn("role_id")
          .ToTable("roles").PrimaryColumn("id");

    // Índice UNIQUE en email (columna de búsqueda frecuente)
    Create.Index("ix_users_email")
          .OnTable("users")
          .OnColumn("email")
          .Ascending()
          .WithOptions().Unique();
}
```

**Por qué**:
- Foreign keys sin índices causan performance issues
- Columnas de búsqueda (email, code) deben tener índices
- UNIQUE constraints automáticamente crean índices

---

### ✅ 6. Testing de migraciones

```csharp
// ✅ Test: Aplicar Up() y Down() en orden
[Test]
public void M024_Should_CreateUsersTable_And_Rollback()
{
    // Arrange
    var migration = new M024CreateUsersTable();

    // Act: Aplicar migración
    migration.Up();
    var tableExists = _session.CreateSQLQuery("SELECT 1 FROM users LIMIT 1").UniqueResult();

    // Assert: Tabla existe
    Assert.That(tableExists, Is.Not.Null);

    // Act: Revertir migración
    migration.Down();
    Assert.Throws<Exception>(() =>
    {
        _session.CreateSQLQuery("SELECT 1 FROM users LIMIT 1").UniqueResult();
    });
}
```

---

### ✅ 7. No modificar migraciones aplicadas

```csharp
// ❌ INCORRECTO: Modificar migración ya aplicada en producción
[Migration(24)]
public class M024CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("email").AsString().NotNullable()
              .WithColumn("phone").AsString().Nullable();  // ← Agregado después de aplicar
    }
}

// ✅ CORRECTO: Crear nueva migración
[Migration(30)]
public class M030AddPhoneToUsers : Migration
{
    public override void Up()
    {
        Alter.Table("users")
             .AddColumn("phone").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Column("phone").FromTable("users");
    }
}
```

**Por qué**:
- Las migraciones aplicadas ya están en la tabla `versioninfo`
- Modificarlas no las re-ejecuta automáticamente
- Causa inconsistencias entre ambientes

---

## Comparación: FluentMigrator vs EF Migrations

| Característica | FluentMigrator | EF Migrations |
|----------------|----------------|---------------|
| **ORM Agnostic** | ✅ Sí (funciona con cualquier ORM) | ❌ Solo EF Core |
| **Base de datos** | ✅ Múltiples (SQL Server, PostgreSQL, MySQL, SQLite) | ✅ Múltiples |
| **Auto-generación** | ❌ Manual | ✅ Desde DbContext |
| **Control granular** | ✅ Total control del SQL | ⚠️ Menos control |
| **Fluent API** | ✅ Expresiva y legible | ✅ Expresiva |
| **Rollback** | ✅ Down() método | ✅ Down() método |
| **Versionado** | ✅ Números explícitos | ✅ Timestamps automáticos |
| **Setup** | ⚠️ Proyecto console separado | ✅ Integrado en proyecto |
| **Testing** | ✅ Fácil de testear | ✅ Fácil de testear |
| **Proyecto de referencia** | ✅ hashira.stone.backend | ⏳ No usado |

---

## Flujo Mental: Desarrollo con Migrations

```
1. Diseñar cambio de esquema
   ↓
2. Crear migración con número de versión único
   ↓
3. Implementar Up() (aplicar cambios)
   ↓
4. Implementar Down() (revertir cambios)
   ↓
5. Ejecutar dotnet run cnn="..." en dev
   ↓
6. Verificar tabla versioninfo
   ↓
7. Probar rollback con action=rollback
   ↓
8. Commit y push a Git
   ↓
9. CI/CD ejecuta migraciones en staging/prod
```

---

## Anti-Patterns

### ❌ 1. Modificar datos de producción en migraciones

```csharp
// ❌ INCORRECTO: Modificar datos de producción
[Migration(30)]
public class M030UpdateUserEmails : Migration
{
    public override void Up()
    {
        Execute.Sql("UPDATE users SET email = 'new@example.com' WHERE id = '123'");
    }
}
```

**Por qué es malo**:
- Las migraciones son para esquema, no datos
- Datos de producción deben manejarse con scripts SQL auditados
- No hay forma segura de revertir cambios de datos

**Solución**: Crear script SQL separado y ejecutarlo manualmente con backup previo.

---

### ❌ 2. SQL injection en migraciones

```csharp
// ❌ INCORRECTO: Vulnerable a SQL injection
public override void Up()
{
    var tableName = GetTableNameFromUserInput();  // ← Peligroso
    Execute.Sql($"CREATE TABLE {tableName} (id int)");
}

// ✅ CORRECTO: Usar constantes hardcoded
public override void Up()
{
    const string tableName = "users";
    Create.Table(tableName)
          .WithColumn("id").AsInt32().PrimaryKey();
}
```

---

### ❌ 3. Dependencias entre migraciones

```csharp
// ❌ INCORRECTO: Migración depende de datos de otra migración
[Migration(25)]
public class M025SeedRoles : Migration
{
    public override void Up()
    {
        Execute.Sql("INSERT INTO roles (name) VALUES ('Admin')");
    }
}

[Migration(26)]
public class M026SeedUsers : Migration
{
    public override void Up()
    {
        // ❌ Asume que M025 ya insertó 'Admin'
        Execute.Sql("INSERT INTO users (role) VALUES ('Admin')");
    }
}
```

**Por qué es malo**:
- Migraciones deben ser independientes
- Orden de ejecución no garantizado en algunos casos

---

## Troubleshooting

### Error: "Migration version already applied"

**Causa**: Intentando aplicar una migración que ya fue ejecutada.

**Solución**:
```sql
-- Ver migraciones aplicadas
SELECT * FROM public.versioninfo ORDER BY version DESC;

-- Si la migración no debería estar aplicada, eliminar registro
DELETE FROM public.versioninfo WHERE version = 24;
```

---

### Error: "Connection string not provided"

**Causa**: Falta el parámetro `cnn` en command line.

**Solución**:
```bash
# ✅ CORRECTO
dotnet run cnn="Host=localhost;Database=mydb;Username=postgres;Password=pass"

# ❌ INCORRECTO (sin cnn)
dotnet run
```

---

### Error: "Table already exists"

**Causa**: Ejecutando migración Up() cuando la tabla ya existe.

**Solución**:
```csharp
// ✅ Usar IfNotExists
public override void Up()
{
    if (!Schema.Table("users").Exists())
    {
        Create.Table("users")
              .WithColumn("id").AsGuid().PrimaryKey();
    }
}
```

---

## Referencias

### 📚 Documentación Oficial

- [FluentMigrator Documentation](https://fluentmigrator.github.io/)
- [FluentMigrator GitHub](https://github.com/fluentmigrator/fluentmigrator)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Npgsql - .NET Data Provider for PostgreSQL](https://www.npgsql.org/)

### 🔗 Guías Relacionadas

- [Core Concepts](../core-concepts.md) - Conceptos fundamentales de Infrastructure
- [Repository Pattern](../repository-pattern.md) - Patrón Repository
- [NHibernate Mappers](../orm-implementations/nhibernate/mappers.md) - Mappers de NHibernate
- [Best Practices](../../best-practices/README.md) - Prácticas generales

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-11-14 | Versión inicial de Data Migrations README |

---

**Siguiente**: [FluentMigrator README](./fluent-migrator/README.md) - Setup y configuración →
