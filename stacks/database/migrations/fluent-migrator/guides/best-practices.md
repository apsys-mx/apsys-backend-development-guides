# FluentMigrator - Best Practices

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## Tabla de Contenidos

- [Introducción](#introducción)
- [1. Diseño de Migraciones](#1-diseño-de-migraciones)
  - [1.1 Una Responsabilidad por Migración](#11-una-responsabilidad-por-migración)
  - [1.2 Migraciones Atómicas](#12-migraciones-atómicas)
  - [1.3 Migraciones Reversibles](#13-migraciones-reversibles)
  - [1.4 Independencia de Migraciones](#14-independencia-de-migraciones)
- [2. Versionado](#2-versionado)
  - [2.1 Estrategias de Versionado](#21-estrategias-de-versionado)
  - [2.2 Números Secuenciales](#22-números-secuenciales)
  - [2.3 Timestamps](#23-timestamps)
  - [2.4 Gaps en la Numeración](#24-gaps-en-la-numeración)
- [3. Naming Conventions](#3-naming-conventions)
  - [3.1 Nombres de Archivos](#31-nombres-de-archivos)
  - [3.2 Nombres de Clases](#32-nombres-de-clases)
  - [3.3 Nombres de Tablas e Índices](#33-nombres-de-tablas-e-índices)
- [4. Performance](#4-performance)
  - [4.1 Índices](#41-índices)
  - [4.2 Operaciones en Batch](#42-operaciones-en-batch)
  - [4.3 Migraciones Largas](#43-migraciones-largas)
  - [4.4 Downtime Considerations](#44-downtime-considerations)
- [5. Testing](#5-testing)
  - [5.1 Testing Local](#51-testing-local)
  - [5.2 Testing en CI/CD](#52-testing-en-cicd)
  - [5.3 Testing de Rollback](#53-testing-de-rollback)
  - [5.4 Testing de Performance](#54-testing-de-performance)
- [6. Datos de Prueba](#6-datos-de-prueba)
  - [6.1 Separación de Datos](#61-separación-de-datos)
  - [6.2 Perfiles de Migración](#62-perfiles-de-migración)
  - [6.3 Seed Data](#63-seed-data)
- [7. Trabajo en Equipo](#7-trabajo-en-equipo)
  - [7.1 Resolución de Conflictos](#71-resolución-de-conflictos)
  - [7.2 Code Review](#72-code-review)
  - [7.3 Comunicación](#73-comunicación)
- [8. Deployment](#8-deployment)
  - [8.1 Estrategias de Deployment](#81-estrategias-de-deployment)
  - [8.2 Backups](#82-backups)
  - [8.3 Monitoreo](#83-monitoreo)
  - [8.4 Rollback Plan](#84-rollback-plan)
- [9. Seguridad](#9-seguridad)
  - [9.1 SQL Injection](#91-sql-injection)
  - [9.2 Datos Sensibles](#92-datos-sensibles)
  - [9.3 Permisos](#93-permisos)
- [10. Mantenimiento](#10-mantenimiento)
  - [10.1 Documentación](#101-documentación)
  - [10.2 Limpieza](#102-limpieza)
  - [10.3 Refactoring](#103-refactoring)
- [Common Pitfalls](#common-pitfalls)
- [Checklist](#checklist)

---

## Introducción

Este documento establece las **best practices** para trabajar con **FluentMigrator** en el contexto de desarrollo backend de APSYS. Estas prácticas están basadas en:

- 📚 Código real del proyecto de referencia (`hashira.stone.backend.migrations`)
- ✅ Experiencia práctica con PostgreSQL 11.0
- 🔧 Patrones probados en producción
- ⚠️ Lecciones aprendidas de errores comunes

**Principio fundamental**: Las migraciones son **código de producción crítico** que modifica el estado de la base de datos. Deben tratarse con el mismo rigor que el código de la aplicación.

---

## 1. Diseño de Migraciones

### 1.1 Una Responsabilidad por Migración

✅ **CORRECTO**: Una migración hace una cosa
```csharp
[Migration(27)]
public class M027CreatePrototypeTable : Migration
{
    private readonly string _tableName = "prototypes";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        // Solo crear tabla prototypes
        Create.Table(_tableName)
            .InSchema(_schemaName)
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("number").AsString(50).NotNullable().Unique()
            .WithColumn("issue_date").AsDateTime().NotNullable()
            .WithColumn("expiration_date").AsDateTime().NotNullable()
            .WithColumn("status").AsString(20).NotNullable();
    }

    public override void Down()
    {
        Delete.Table(_tableName).InSchema(_schemaName);
    }
}
```

❌ **INCORRECTO**: Múltiples responsabilidades
```csharp
[Migration(999)]
public class M999CreateMultipleTables : Migration
{
    public override void Up()
    {
        // ❌ Crear tabla users
        Create.Table("users")...

        // ❌ Crear tabla roles
        Create.Table("roles")...

        // ❌ Crear tabla permissions
        Create.Table("permissions")...

        // ❌ Seed data
        Insert.IntoTable("roles")...
    }
}
```

**Razones**:
- **Rollback granular**: Si algo falla, solo se revierte una operación
- **Code review fácil**: Cambios pequeños son más fáciles de revisar
- **Debugging**: Más fácil identificar qué migración causó un problema
- **Merge conflicts**: Menos conflictos al trabajar en equipo

### 1.2 Migraciones Atómicas

Las migraciones deben ser **transaccionales** (todo o nada).

✅ **CORRECTO**: Usar TransactionBehavior
```csharp
[Migration(50, TransactionBehavior.Default)] // ✅ Transaccional
public class M050AddColumnToPrototypes : Migration
{
    public override void Up()
    {
        Alter.Table("prototypes")
            .AddColumn("notes").AsString(1000).Nullable();
    }

    public override void Down()
    {
        Delete.Column("notes").FromTable("prototypes");
    }
}
```

⚠️ **CUIDADO**: Operaciones no transaccionales
```csharp
[Migration(51, TransactionBehavior.None)] // ⚠️ No transaccional
public class M051CreateIndexConcurrently : Migration
{
    public override void Up()
    {
        // PostgreSQL: CREATE INDEX CONCURRENTLY no puede estar en transacción
        Execute.Sql(@"
            CREATE INDEX CONCURRENTLY idx_prototypes_status
            ON public.prototypes(status);
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"DROP INDEX IF EXISTS public.idx_prototypes_status;");
    }
}
```

**Regla**: Solo usar `TransactionBehavior.None` cuando sea **absolutamente necesario** (ej: `CREATE INDEX CONCURRENTLY` en PostgreSQL).

### 1.3 Migraciones Reversibles

Siempre implementar `Down()` de forma **simétrica** a `Up()`.

✅ **CORRECTO**: Down() es exactamente inverso a Up()
```csharp
[Migration(52)]
public class M052AddCompanyColumn : Migration
{
    private readonly string _tableName = "prototypes";
    private readonly string _schemaName = "public";

    public override void Up()
    {
        Alter.Table(_tableName)
            .InSchema(_schemaName)
            .AddColumn("company_id").AsGuid().NotNullable()
            .WithDefaultValue(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    }

    public override void Down()
    {
        // Exactamente inverso: eliminar columna
        Delete.Column("company_id")
            .FromTable(_tableName)
            .InSchema(_schemaName);
    }
}
```

❌ **INCORRECTO**: Down() no es inverso
```csharp
[Migration(999)]
public class M999BadReversibility : Migration
{
    public override void Up()
    {
        Alter.Table("prototypes")
            .AddColumn("company_id").AsGuid().NotNullable();
    }

    public override void Down()
    {
        // ❌ No hace nada
        // ❌ O hace algo diferente a Up()
        Execute.Sql("TRUNCATE TABLE prototypes;"); // ❌❌❌
    }
}
```

**Importante**: Algunas operaciones son **irreversibles** por naturaleza:

```csharp
[Migration(53)]
public class M053DeleteColumnIrreversible : Migration
{
    public override void Up()
    {
        // Eliminar columna = pérdida de datos
        Delete.Column("old_field").FromTable("prototypes");
    }

    public override void Down()
    {
        // ⚠️ Podemos recrear columna, pero datos se perdieron
        Alter.Table("prototypes")
            .AddColumn("old_field").AsString(100).Nullable();

        // 🔴 Los datos originales NO se pueden recuperar
    }
}
```

### 1.4 Independencia de Migraciones

Las migraciones **NO** deben depender de código de la aplicación.

❌ **INCORRECTO**: Dependencia de modelo de dominio
```csharp
[Migration(999)]
public class M999BadDependency : Migration
{
    public override void Up()
    {
        // ❌ NO referenciar modelos de dominio
        var defaultStatus = PrototypeStatus.Draft.ToString(); // ❌

        Create.Table("prototypes")
            .WithColumn("status").AsString(20).WithDefaultValue(defaultStatus);
    }
}
```

✅ **CORRECTO**: Usar valores literales
```csharp
[Migration(54)]
public class M054IndependentMigration : Migration
{
    // ✅ Constantes dentro de la migración
    private const string DefaultStatus = "Draft";

    public override void Up()
    {
        Create.Table("prototypes")
            .InSchema("public")
            .WithColumn("status").AsString(20).WithDefaultValue(DefaultStatus);
    }

    public override void Down()
    {
        Delete.Table("prototypes").InSchema("public");
    }
}
```

**Razón**: El código de la aplicación cambia con el tiempo. Las migraciones deben ser **inmutables** y ejecutarse igual hoy que dentro de 5 años.

---

## 2. Versionado

### 2.1 Estrategias de Versionado

FluentMigrator soporta dos estrategias principales:

#### **Opción A: Números Secuenciales** ⭐ (Recomendado para APSYS)

```csharp
[Migration(1)]  // Primera migración
[Migration(2)]  // Segunda migración
[Migration(3)]  // Tercera migración
...
[Migration(27)] // Migración actual (referencia: M027CreatePrototypeTable)
```

**Ventajas**:
- ✅ Simple y legible
- ✅ Orden claro de ejecución
- ✅ Fácil de rastrear

**Desventajas**:
- ⚠️ Conflictos en branches paralelos

#### **Opción B: Timestamps**

```csharp
[Migration(20250114120000)] // 2025-01-14 12:00:00
[Migration(20250114120100)] // 2025-01-14 12:01:00
```

**Ventajas**:
- ✅ Sin conflictos en branches paralelos
- ✅ Incluye información temporal

**Desventajas**:
- ❌ Números largos y difíciles de leer
- ❌ Difícil de referenciar en conversaciones

### 2.2 Números Secuenciales

**Convención APSYS** (basada en `hashira.stone.backend.migrations`):

```csharp
// ✅ Formato: Migration(N) donde N es secuencial
[Migration(23)]
public class M023CreateRolesTable : Migration { }

[Migration(24)]
public class M024CreateUsersTable : Migration { }

[Migration(26)]  // ⚠️ Gap permitido (25 fue eliminada o reservada)
public class M026TechnicalStandardsView : Migration { }

[Migration(27)]
public class M027CreatePrototypeTable : Migration { }
```

**Reglas**:
1. **Siempre incremental**: Cada nueva migración debe tener número mayor al anterior
2. **Sin reutilizar números**: Si una migración se elimina, su número NO se reutiliza
3. **Gaps permitidos**: No es necesario que sean consecutivos (26, 27, 30 es válido)

### 2.3 Timestamps

Si el equipo decide usar timestamps:

```csharp
// Formato: YYYYMMDDHHmmss
[Migration(20250114153045)] // 2025-01-14 15:30:45
public class M20250114153045_CreatePrototypeTable : Migration { }
```

**Generación automática** (PowerShell):
```powershell
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
Write-Host "Migration($timestamp)"
```

**Generación automática** (Bash):
```bash
timestamp=$(date +%Y%m%d%H%M%S)
echo "Migration($timestamp)"
```

### 2.4 Gaps en la Numeración

✅ **PERMITIDO**: Gaps en la secuencia
```csharp
[Migration(10)]
[Migration(11)]
[Migration(15)]  // ✅ OK - 12, 13, 14 fueron removidas
[Migration(16)]
```

❌ **NO PERMITIDO**: Llenar gaps de versiones antiguas
```csharp
// Ya existen: 10, 11, 15, 16
[Migration(12)] // ❌ NO hacer esto
```

**Razón**: Llenar gaps puede causar que migraciones **nuevas** se ejecuten **antes** que migraciones **antiguas** en ambientes donde las antiguas ya se aplicaron.

---

## 3. Naming Conventions

### 3.1 Nombres de Archivos

**Formato**: `M{NNN}{DescripcionEnPascalCase}.cs`

```
✅ Ejemplos correctos del proyecto de referencia:
- M023CreateRolesTable.cs
- M024CreateUsersTable.cs
- M026TechnicalStandardsView.cs
- M027CreatePrototypeTable.cs

❌ Ejemplos incorrectos:
- Migration023.cs                        ❌ No descriptivo
- M023_create_roles_table.cs            ❌ Usar PascalCase, no snake_case
- M23CreateRolesTable.cs                ❌ Falta zero-padding
- CreateRolesTable.cs                   ❌ Falta prefijo M y número
```

**Convenciones**:
- Prefijo `M` + número con zero-padding (M001, M023, M100)
- PascalCase para descripción
- Descripción clara de la acción (Create/Add/Alter/Delete + Target)

### 3.2 Nombres de Clases

**Formato**: `M{NNN}{DescripcionEnPascalCase} : Migration`

```csharp
// ✅ CORRECTO
[Migration(27)]
public class M027CreatePrototypeTable : Migration
{
    // El nombre de la clase coincide con el archivo
}

// ❌ INCORRECTO
[Migration(27)]
public class PrototypeMigration : Migration  // ❌ No sigue convención
{
}
```

**Regla**: El nombre de la clase debe ser **idéntico** al nombre del archivo (sin `.cs`).

### 3.3 Nombres de Tablas e Índices

**Tablas**: `snake_case`, plural

```csharp
// ✅ CORRECTO
Create.Table("prototypes")          // ✅ snake_case, plural
Create.Table("technical_standards") // ✅ palabras múltiples con underscore
Create.Table("user_roles")          // ✅ junction table

// ❌ INCORRECTO
Create.Table("Prototypes")          // ❌ PascalCase
Create.Table("prototype")           // ❌ singular
Create.Table("technicalStandards")  // ❌ camelCase
```

**Índices**: `idx_{tabla}_{columna(s)}`

```csharp
// ✅ CORRECTO
Create.Index("idx_prototypes_number")           // ✅ Single column
Create.Index("idx_prototypes_status_issue_date") // ✅ Multiple columns
Create.Index("idx_users_email")                 // ✅ Unique index

// ❌ INCORRECTO
Create.Index("PrototypesNumberIndex")           // ❌ No sigue convención
Create.Index("idx1")                            // ❌ No descriptivo
```

**Foreign Keys**: `fk_{tabla_origen}_{columna_origen}`

```csharp
// ✅ CORRECTO (del proyecto de referencia)
Create.ForeignKey("fk_user_roles_user_id")
    .FromTable("user_roles")
    .ForeignColumn("user_id")
    .ToTable("users")
    .PrimaryColumn("id");

Create.ForeignKey("fk_user_roles_role_id")
    .FromTable("user_roles")
    .ForeignColumn("role_id")
    .ToTable("roles")
    .PrimaryColumn("id");
```

**Primary Keys**: `pk_{tabla}`

```csharp
// ✅ CORRECTO (del proyecto de referencia)
Create.PrimaryKey("pk_user_roles")
    .OnTable("user_roles")
    .WithSchema("public")
    .Columns("user_id", "role_id");
```

---

## 4. Performance

### 4.1 Índices

#### **Crear Índices con la Tabla**

✅ **CORRECTO**: Índices en migración de creación de tabla
```csharp
[Migration(60)]
public class M060CreateOrdersTable : Migration
{
    public override void Up()
    {
        // 1️⃣ Crear tabla
        Create.Table("orders")
            .InSchema("public")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("order_number").AsString(50).NotNullable().Unique() // ✅ Unique crea índice
            .WithColumn("customer_id").AsGuid().NotNullable()
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // 2️⃣ Crear índices adicionales
        Create.Index("idx_orders_customer_id")
            .OnTable("orders")
            .OnColumn("customer_id");

        Create.Index("idx_orders_status")
            .OnTable("orders")
            .OnColumn("status");

        Create.Index("idx_orders_created_at")
            .OnTable("orders")
            .OnColumn("created_at");
    }

    public override void Down()
    {
        // Los índices se eliminan automáticamente al eliminar la tabla
        Delete.Table("orders").InSchema("public");
    }
}
```

#### **Índices en Tablas Grandes (PostgreSQL)**

Para tablas con millones de registros, usar `CREATE INDEX CONCURRENTLY`:

```csharp
[Migration(61, TransactionBehavior.None)] // ⚠️ CONCURRENTLY requiere no transaccional
public class M061CreateIndexOnLargeTable : Migration
{
    public override void Up()
    {
        // ✅ CONCURRENTLY no bloquea escrituras
        Execute.Sql(@"
            CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_customer_id_created_at
            ON public.orders(customer_id, created_at);
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DROP INDEX IF EXISTS public.idx_orders_customer_id_created_at;
        ");
    }
}
```

**Cuándo usar CONCURRENTLY**:
- ✅ Tabla con > 1 millón de registros
- ✅ Producción con tráfico activo
- ✅ No puedes permitir downtime

**Desventajas**:
- ⚠️ Más lento que índice normal
- ⚠️ Requiere `TransactionBehavior.None`
- ⚠️ Puede fallar y dejar índice INVALID

### 4.2 Operaciones en Batch

Para insertar grandes cantidades de datos, usar SQL directo en lugar de API:

❌ **LENTO**: Insert.IntoTable() en loop
```csharp
public override void Up()
{
    // ❌ LENTO: 10,000 transacciones individuales
    for (int i = 0; i < 10000; i++)
    {
        Insert.IntoTable("products")
            .Row(new { id = Guid.NewGuid(), name = $"Product {i}" });
    }
}
```

✅ **RÁPIDO**: SQL batch insert
```csharp
public override void Up()
{
    // ✅ RÁPIDO: 1 transacción
    Execute.Sql(@"
        INSERT INTO public.products (id, name)
        SELECT
            gen_random_uuid(),
            'Product ' || generate_series
        FROM generate_series(1, 10000);
    ");
}
```

### 4.3 Migraciones Largas

Para migraciones que modifican millones de registros:

```csharp
[Migration(62)]
public class M062UpdateLargeDataset : Migration
{
    public override void Up()
    {
        // ✅ Procesar en batches
        Execute.Sql(@"
            DO $$
            DECLARE
                batch_size INT := 10000;
                rows_affected INT;
            BEGIN
                LOOP
                    UPDATE public.orders
                    SET normalized_status = LOWER(status)
                    WHERE id IN (
                        SELECT id
                        FROM public.orders
                        WHERE normalized_status IS NULL
                        LIMIT batch_size
                    );

                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    EXIT WHEN rows_affected = 0;

                    -- Log progress
                    RAISE NOTICE 'Updated % rows', rows_affected;

                    -- Sleep para no saturar
                    PERFORM pg_sleep(0.1);
                END LOOP;
            END $$;
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"
            UPDATE public.orders SET normalized_status = NULL;
        ");
    }
}
```

### 4.4 Downtime Considerations

#### **Zero-Downtime Migrations**

Para cambios que requieren alta disponibilidad:

**Paso 1**: Agregar columna nullable
```csharp
[Migration(70)]
public class M070AddFullNameStep1 : Migration
{
    public override void Up()
    {
        // ✅ Agregar columna NULLABLE
        Alter.Table("users")
            .AddColumn("full_name").AsString(500).Nullable();
    }

    public override void Down()
    {
        Delete.Column("full_name").FromTable("users");
    }
}
```

**Paso 2**: Deploy código que popula la columna

**Paso 3**: Migración para poblar datos existentes
```csharp
[Migration(71)]
public class M071AddFullNameStep2 : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            UPDATE public.users
            SET full_name = CONCAT(first_name, ' ', last_name)
            WHERE full_name IS NULL;
        ");
    }

    public override void Down()
    {
        Execute.Sql(@"UPDATE public.users SET full_name = NULL;");
    }
}
```

**Paso 4**: Hacer columna NOT NULL
```csharp
[Migration(72)]
public class M071AddFullNameStep3 : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AlterColumn("full_name").AsString(500).NotNullable();
    }

    public override void Down()
    {
        Alter.Table("users")
            .AlterColumn("full_name").AsString(500).Nullable();
    }
}
```

---

## 5. Testing

### 5.1 Testing Local

Antes de hacer commit, **SIEMPRE** probar:

```bash
# 1️⃣ Ejecutar migración Up
dotnet run --project src/your.migrations -- --cnn "Host=localhost;..." run

# 2️⃣ Verificar en base de datos
psql -h localhost -U postgres -d yourdb -c "\dt public.*"

# 3️⃣ Ejecutar rollback Down
dotnet run --project src/your.migrations -- --cnn "Host=localhost;..." rollback

# 4️⃣ Verificar que la tabla/columna fue eliminada
psql -h localhost -U postgres -d yourdb -c "\dt public.*"

# 5️⃣ Volver a ejecutar Up para confirmar idempotencia
dotnet run --project src/your.migrations -- --cnn "Host=localhost;..." run
```

### 5.2 Testing en CI/CD

**GitHub Actions ejemplo**:

```yaml
name: Test Migrations

on: [pull_request]

jobs:
  test-migrations:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:11
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: testdb
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Migrations Up
        run: |
          dotnet run --project src/your.migrations -- \
            --cnn "Host=localhost;Database=testdb;Username=postgres;Password=postgres" \
            run

      - name: Run Migrations Down
        run: |
          dotnet run --project src/your.migrations -- \
            --cnn "Host=localhost;Database=testdb;Username=postgres;Password=postgres" \
            rollback

      - name: Run Migrations Up Again (idempotency test)
        run: |
          dotnet run --project src/your.migrations -- \
            --cnn "Host=localhost;Database=testdb;Username=postgres;Password=postgres" \
            run
```

### 5.3 Testing de Rollback

**Critical**: Probar `Down()` es TAN importante como probar `Up()`.

```bash
# Scenario: Nueva migración M080AddColumnToOrders

# 1️⃣ Estado inicial: migraciones hasta M079
dotnet run -- --cnn "..." run

# 2️⃣ Aplicar nueva migración M080
dotnet run -- --cnn "..." run

# 3️⃣ Verificar que se aplicó
psql -c "SELECT * FROM public.versioninfo ORDER BY version DESC LIMIT 5;"

# 4️⃣ Rollback M080
dotnet run -- --cnn "..." rollback

# 5️⃣ Verificar que volvimos a M079
psql -c "SELECT * FROM public.versioninfo ORDER BY version DESC LIMIT 5;"

# 6️⃣ Verificar que cambios de M080 se revirtieron
psql -c "\d public.orders"
```

### 5.4 Testing de Performance

Para migraciones que modifican datos:

```csharp
// Test: ¿Cuánto tarda en tabla con 1M registros?

// 1️⃣ Crear datos de prueba
Execute.Sql(@"
    INSERT INTO public.orders (id, status, created_at)
    SELECT
        gen_random_uuid(),
        'pending',
        NOW() - (random() * interval '365 days')
    FROM generate_series(1, 1000000);
");

// 2️⃣ Ejecutar migración y medir tiempo
// Log: "Migration took 5 minutes"

// 3️⃣ Evaluar: ¿Es aceptable?
// - < 1 min: ✅ OK
// - 1-5 min: ⚠️ Warning
// - > 5 min: 🔴 Requiere optimización
```

---

## 6. Datos de Prueba

### 6.1 Separación de Datos

**Regla**: NUNCA mezclar schema migrations con seed data.

✅ **CORRECTO**: Migraciones separadas
```csharp
// Migración de schema
[Migration(100)]
public class M100CreateRolesTable : Migration
{
    public override void Up()
    {
        Create.Table("roles")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("roles");
    }
}

// Migración de seed data (DESPUÉS)
[Migration(101)]
[Tags("SeedData")] // ✅ Tag para identificar
public class M101SeedDefaultRoles : Migration
{
    public override void Up()
    {
        Insert.IntoTable("roles")
            .Row(new { id = Guid.Parse("..."), name = "Admin" })
            .Row(new { id = Guid.Parse("..."), name = "User" });
    }

    public override void Down()
    {
        Delete.FromTable("roles").AllRows();
    }
}
```

### 6.2 Perfiles de Migración

Usar **Tags** para ejecutar selectivamente:

```csharp
[Migration(110)]
[Tags("Development", "SeedData")]
public class M110SeedTestUsers : Migration
{
    public override void Up()
    {
        Insert.IntoTable("users")
            .Row(new {
                id = Guid.NewGuid(),
                email = "test@example.com",
                name = "Test User"
            });
    }

    public override void Down()
    {
        Delete.FromTable("users")
            .Row(new { email = "test@example.com" });
    }
}
```

Ejecutar solo migraciones con tag:

```csharp
.ConfigureRunner(rb => rb
    .AddPostgres11_0()
    .WithGlobalConnectionString(connectionString)
    .ScanIn(typeof(M001Sandbox).Assembly).For.Migrations()
    .WithMigrationsIn("Development") // Solo ejecutar migraciones con tag "Development"
)
```

### 6.3 Seed Data

Para datos de referencia (códigos postales, países, etc.):

```csharp
[Migration(120)]
[Tags("ReferenceData")]
public class M120SeedCountries : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            INSERT INTO public.countries (id, code, name) VALUES
            ('11111111-1111-1111-1111-111111111111', 'MX', 'México'),
            ('22222222-2222-2222-2222-222222222222', 'US', 'United States'),
            ('33333333-3333-3333-3333-333333333333', 'CA', 'Canada')
            ON CONFLICT (code) DO NOTHING;
        ");
    }

    public override void Down()
    {
        Delete.FromTable("countries").AllRows();
    }
}
```

**Best Practices**:
- ✅ Usar `ON CONFLICT DO NOTHING` para idempotencia
- ✅ Usar GUIDs fijos para datos de referencia
- ✅ Tag `ReferenceData` para identificar
- ⚠️ Considerar archivos CSV/JSON para grandes volúmenes

---

## 7. Trabajo en Equipo

### 7.1 Resolución de Conflictos

**Scenario**: Dos desarrolladores crean migración M050 simultáneamente.

**Developer A** (branch `feature/add-comments`):
```csharp
[Migration(50)]
public class M050AddCommentsColumn : Migration { ... }
```

**Developer B** (branch `feature/add-tags`):
```csharp
[Migration(50)]
public class M050AddTagsColumn : Migration { ... }
```

**Resolución**:
1. El primero en hacer merge a `main` mantiene M050
2. El segundo **renumera** su migración a M051 (o siguiente disponible)

```csharp
// Developer B renumera ANTES de merge
[Migration(51)] // ✅ Era 50, ahora 51
public class M051AddTagsColumn : Migration { ... }
```

### 7.2 Code Review

**Checklist para reviewer**:

```markdown
## Migration Code Review Checklist

- [ ] **Naming**: Archivo y clase siguen convención M{NNN}{Description}
- [ ] **Version**: Número de migración es secuencial y no reutiliza números
- [ ] **Down()**: Implementado y es inverso de Up()
- [ ] **Transactions**: TransactionBehavior apropiado
- [ ] **Independence**: No depende de código de aplicación
- [ ] **Indexes**: Columnas frecuentemente consultadas tienen índices
- [ ] **Performance**: Consideraciones para tablas grandes
- [ ] **Security**: No contiene SQL injection, no expone datos sensibles
- [ ] **Testing**: Autor confirmó que probó Up() y Down() localmente
- [ ] **Documentation**: Comentarios para lógica compleja
```

### 7.3 Comunicación

**Antes de crear migración**:

```markdown
# En Slack/Teams

@team Voy a crear una migración para agregar columna `full_name` a tabla `users`.
Número: M081
Branch: feature/user-full-name
ETA: Mañana

¿Alguien está trabajando en migraciones para `users`?
```

**Previene**:
- Conflictos de numeración
- Múltiples personas modificando misma tabla
- Sorpresas en code review

---

## 8. Deployment

### 8.1 Estrategias de Deployment

#### **Opción A: Deploy Manual** (Producción pequeña)

```bash
# 1️⃣ Conectarse al servidor de producción
ssh user@production-server

# 2️⃣ Ir al directorio de migraciones
cd /app/migrations

# 3️⃣ Ejecutar migraciones
dotnet run -- --cnn "$PROD_CONNECTION_STRING" run

# 4️⃣ Verificar
psql $PROD_CONNECTION_STRING -c "SELECT * FROM versioninfo ORDER BY version DESC LIMIT 5;"
```

#### **Opción B: CI/CD Automático** (Recomendado)

```yaml
# GitHub Actions
- name: Run Migrations
  run: |
    dotnet run --project src/your.migrations -- \
      --cnn "${{ secrets.PROD_CONNECTION_STRING }}" \
      run
```

#### **Opción C: Docker Container**

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish/ .
ENTRYPOINT ["dotnet", "your.migrations.dll"]
```

```bash
# Run migrations
docker run --rm \
  -e ConnectionString="Host=..." \
  your-migrations:latest \
  --cnn "$ConnectionString" run
```

### 8.2 Backups

**CRÍTICO**: SIEMPRE hacer backup antes de aplicar migraciones en producción.

```bash
# PostgreSQL: Backup completo
pg_dump -h production-db -U postgres -d yourdb -F c -f backup_$(date +%Y%m%d_%H%M%S).dump

# PostgreSQL: Backup solo schema
pg_dump -h production-db -U postgres -d yourdb --schema-only -f schema_$(date +%Y%m%d_%H%M%S).sql

# Verificar backup
ls -lh backup_*.dump
```

**Automatización**:

```bash
#!/bin/bash
# pre-migration-backup.sh

BACKUP_DIR="/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/pre_migration_$TIMESTAMP.dump"

echo "Creating backup: $BACKUP_FILE"
pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME -F c -f $BACKUP_FILE

if [ $? -eq 0 ]; then
    echo "✅ Backup successful: $BACKUP_FILE"

    # Ejecutar migraciones
    dotnet run --project migrations -- --cnn "$CONNECTION_STRING" run
else
    echo "❌ Backup failed. Aborting migrations."
    exit 1
fi
```

### 8.3 Monitoreo

Monitorear durante y después de deployment:

```csharp
// Program.cs - agregar logging
using (var scope = serviceProvider.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting migrations...");

        runner.MigrateUp();

        stopwatch.Stop();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✅ Migrations completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Migrations failed after {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Error: {ex.Message}");
        throw;
    }
}
```

**PostgreSQL: Monitorear locks**:

```sql
-- Ver queries bloqueadas
SELECT
    pid,
    usename,
    application_name,
    state,
    query,
    age(clock_timestamp(), query_start) AS duration
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY duration DESC;
```

### 8.4 Rollback Plan

Tener plan de rollback **ANTES** de deployment:

```markdown
## Rollback Plan: M085AddOrdersStatusIndex

### Scenario 1: Migración falla durante ejecución
**Action**: FluentMigrator automáticamente hace rollback (transaccional)
**Verification**: Verificar en versioninfo que M085 NO está presente

### Scenario 2: Migración exitosa pero causa problemas
**Option A - Rollback programático**:
```bash
dotnet run --project migrations -- --cnn "$PROD_CN" rollback --steps 1
```

**Option B - Rollback manual**:
```sql
-- Revertir cambios
DROP INDEX IF EXISTS public.idx_orders_status;

-- Eliminar de versioninfo
DELETE FROM public.versioninfo WHERE version = 85;
```

### Scenario 3: Pérdida de datos (DELETE column)
**Action**: Restaurar desde backup
```bash
pg_restore -h production-db -U postgres -d yourdb backup_20250114_120000.dump
```

**Verification**:
- [ ] Verificar versión en versioninfo
- [ ] Verificar estructura de tabla
- [ ] Verificar cantidad de registros
- [ ] Smoke test de aplicación
```

---

## 9. Seguridad

### 9.1 SQL Injection

❌ **VULNERABLE**: Concatenación de strings
```csharp
[Migration(999)]
public class M999VulnerableToSQLInjection : Migration
{
    public override void Up()
    {
        string userInput = GetUserInput(); // ❌ NUNCA hacer esto

        Execute.Sql($@"
            INSERT INTO users (name) VALUES ('{userInput}');
        "); // ❌ SQL Injection
    }
}
```

✅ **SEGURO**: Usar valores literales
```csharp
[Migration(130)]
public class M130SafeDataInsertion : Migration
{
    // ✅ Valores hardcoded
    private const string DefaultRole = "User";

    public override void Up()
    {
        Execute.Sql(@"
            INSERT INTO roles (id, name) VALUES
            ('11111111-1111-1111-1111-111111111111', 'Admin'),
            ('22222222-2222-2222-2222-222222222222', 'User');
        ");
    }

    public override void Down()
    {
        Delete.FromTable("roles").AllRows();
    }
}
```

**Regla de oro**: Las migraciones NUNCA deben aceptar input externo.

### 9.2 Datos Sensibles

❌ **INCORRECTO**: Hardcodear datos sensibles
```csharp
[Migration(999)]
public class M999HardcodedSecrets : Migration
{
    public override void Up()
    {
        Insert.IntoTable("users")
            .Row(new {
                email = "admin@example.com",
                password = "Admin123!" // ❌ Contraseña en código
            });
    }
}
```

✅ **CORRECTO**: Usar seed scripts separados
```csharp
// Migración solo crea estructura
[Migration(140)]
public class M140CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("email").AsString().NotNullable()
            .WithColumn("password_hash").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}

// Seed data ejecutado manualmente en producción
// seed-admin-user.sql (NO en repo)
INSERT INTO users (id, email, password_hash)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'admin@example.com',
    '$2a$11$...' -- Hash generado externamente
);
```

### 9.3 Permisos

Migración debe ejecutarse con **mínimos privilegios necesarios**:

```sql
-- PostgreSQL: Usuario de migraciones con permisos limitados
CREATE USER migrations_user WITH PASSWORD 'secure_password';

-- ✅ Permisos necesarios
GRANT CONNECT ON DATABASE yourdb TO migrations_user;
GRANT USAGE ON SCHEMA public TO migrations_user;
GRANT CREATE ON SCHEMA public TO migrations_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO migrations_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO migrations_user;

-- ❌ NO dar permisos innecesarios
-- GRANT ALL PRIVILEGES ON DATABASE yourdb TO migrations_user; -- ❌ Demasiado amplio
```

---

## 10. Mantenimiento

### 10.1 Documentación

Para migraciones complejas, agregar comentarios:

```csharp
[Migration(150)]
public class M150ComplexDataMigration : Migration
{
    // 📝 IMPORTANTE: Esta migración transforma datos legacy
    // del formato antiguo (first_name, last_name) al nuevo formato (full_name).
    //
    // Impacto: ~500K registros
    // Tiempo estimado: 2-3 minutos
    // Rollback: Seguro (se puede recuperar datos de first_name y last_name)

    public override void Up()
    {
        // Paso 1: Agregar columna nullable
        Alter.Table("users").AddColumn("full_name").AsString(500).Nullable();

        // Paso 2: Poblar datos (esto puede tardar)
        Execute.Sql(@"
            UPDATE public.users
            SET full_name = CONCAT(first_name, ' ', last_name)
            WHERE full_name IS NULL;
        ");

        // Paso 3: Hacer NOT NULL
        Alter.Table("users").AlterColumn("full_name").AsString(500).NotNullable();
    }

    public override void Down()
    {
        // Rollback seguro: first_name y last_name siguen existiendo
        Delete.Column("full_name").FromTable("users");
    }
}
```

### 10.2 Limpieza

**NO eliminar migraciones antiguas** que ya se ejecutaron en producción.

❌ **INCORRECTO**:
```bash
# ❌ Eliminar migraciones antiguas
rm M001CreateUsersTable.cs
rm M002CreateRolesTable.cs
```

**Razón**: Si necesitas recrear una base de datos desde cero (ej: nuevo ambiente de desarrollo), necesitas TODAS las migraciones.

✅ **CORRECTO**: Mantener todas las migraciones en el repo.

**Excepción**: Si TODOS los ambientes (dev, staging, prod) han migrado más allá de cierta versión, puedes crear una **migración consolidada**:

```csharp
// ANTES: M001-M100 (100 archivos)

// DESPUÉS: M000InitialSchema.sql
[Migration(0)]
public class M000InitialSchema : Migration
{
    public override void Up()
    {
        Execute.EmbeddedScript("M000InitialSchema.sql");
    }

    public override void Down()
    {
        Execute.Sql(@"DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
    }
}

// M000InitialSchema.sql contiene dump completo del schema
// M001-M100 se archivan (no eliminan)
```

### 10.3 Refactoring

Si una migración tiene un bug **ANTES de aplicarse en producción**:

✅ **CORRECTO**: Modificar la migración
```csharp
// Antes (con bug)
[Migration(160)]
public class M160AddEmailColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("email").AsString(100).NotNullable(); // ❌ Bug: debería ser 255
    }
}

// Después (corregido ANTES de producción)
[Migration(160)]
public class M160AddEmailColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("email").AsString(255).NotNullable(); // ✅ Corregido
    }
}
```

Si una migración tiene un bug **DESPUÉS de aplicarse en producción**:

❌ **INCORRECTO**: Modificar la migración aplicada
```csharp
// ❌ NO modificar M160 si ya está en producción
```

✅ **CORRECTO**: Crear nueva migración correctiva
```csharp
[Migration(161)]
public class M161FixEmailColumnLength : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AlterColumn("email").AsString(255).NotNullable();
    }

    public override void Down()
    {
        Alter.Table("users")
            .AlterColumn("email").AsString(100).NotNullable();
    }
}
```

---

## Common Pitfalls

### 1. Modificar Migraciones Aplicadas

❌ **ERROR**:
```csharp
// Migración ya en producción
[Migration(50)]
public class M050AddColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("age").AsInt32().NotNullable(); // Ya aplicado
    }
}

// Developer cambia la migración
[Migration(50)]
public class M050AddColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("age").AsInt32().Nullable(); // ❌ Cambio en migración aplicada
    }
}
```

**Resultado**: La migración en `versioninfo` dice que M050 está aplicada, pero el schema no coincide con el código.

**Solución**: NUNCA modificar migraciones aplicadas. Crear nueva migración correctiva.

### 2. Dependencias entre Migraciones

❌ **ERROR**:
```csharp
[Migration(60)]
public class M060CreateOrdersTable : Migration
{
    public override void Up()
    {
        Create.Table("orders")...
    }
}

[Migration(61)]
public class M061AddForeignKeyToOrders : Migration
{
    public override void Up()
    {
        // ⚠️ Asume que tabla "customers" existe
        Create.ForeignKey("fk_orders_customer_id")
            .FromTable("orders")
            .ForeignColumn("customer_id")
            .ToTable("customers") // ❌ ¿Qué si "customers" no existe?
            .PrimaryColumn("id");
    }
}
```

**Problema**: Si `customers` fue creada en M070, M061 fallará.

**Solución**: Crear FK en la misma migración que crea la tabla, o documentar dependencias:

```csharp
[Migration(61)]
public class M061AddForeignKeyToOrders : Migration
{
    // 📝 PREREQUISITO: Requiere que tabla "customers" exista (creada en M055)

    public override void Up()
    {
        Create.ForeignKey("fk_orders_customer_id")
            .FromTable("orders")
            .ForeignColumn("customer_id")
            .ToTable("customers")
            .PrimaryColumn("id");
    }
}
```

### 3. No Probar Down()

❌ **ERROR**: Solo probar `Up()`, nunca `Down()`

```csharp
[Migration(70)]
public class M070AddColumn : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("nickname").AsString(100).NotNullable();
    }

    public override void Down()
    {
        // ❌ Desarrollador nunca probó esto
        Delete.Column("nickname").FromTable("user"); // ❌ Typo: "user" en vez de "users"
    }
}
```

**Problema**: `Down()` fallará en producción si necesitas rollback.

**Solución**: SIEMPRE probar `Down()` localmente:

```bash
dotnet run -- --cnn "..." run      # ✅ Probar Up()
dotnet run -- --cnn "..." rollback # ✅ Probar Down()
dotnet run -- --cnn "..." run      # ✅ Probar Up() nuevamente
```

### 4. Migraciones No Transaccionales Innecesarias

❌ **ERROR**:
```csharp
[Migration(80, TransactionBehavior.None)] // ❌ Innecesario
public class M080CreateTable : Migration
{
    public override void Up()
    {
        Create.Table("products")...
    }
}
```

**Problema**: Si la migración falla a mitad de ejecución, dejará la BD en estado inconsistente.

**Solución**: Solo usar `TransactionBehavior.None` cuando sea **necesario** (ej: `CREATE INDEX CONCURRENTLY`).

### 5. Olvidar Índices

❌ **ERROR**: Crear tabla sin índices en columnas frecuentemente consultadas

```csharp
[Migration(90)]
public class M090CreateOrdersTable : Migration
{
    public override void Up()
    {
        Create.Table("orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("customer_id").AsGuid().NotNullable() // ❌ Sin índice
            .WithColumn("status").AsString(20).NotNullable()  // ❌ Sin índice
            .WithColumn("created_at").AsDateTime().NotNullable();
    }
}
```

**Problema**: Queries lentos cuando la tabla crece.

**Solución**: Agregar índices desde el inicio:

```csharp
[Migration(90)]
public class M090CreateOrdersTable : Migration
{
    public override void Up()
    {
        Create.Table("orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("customer_id").AsGuid().NotNullable()
            .WithColumn("status").AsString(20).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        // ✅ Índices desde el inicio
        Create.Index("idx_orders_customer_id").OnTable("orders").OnColumn("customer_id");
        Create.Index("idx_orders_status").OnTable("orders").OnColumn("status");
        Create.Index("idx_orders_created_at").OnTable("orders").OnColumn("created_at");
    }

    public override void Down()
    {
        Delete.Table("orders"); // Índices se eliminan automáticamente
    }
}
```

---

## Checklist

Usa este checklist antes de hacer commit de una migración:

```markdown
## Pre-Commit Checklist

### Naming & Versioning
- [ ] Archivo nombrado `M{NNN}{Description}.cs` (ej: M027CreatePrototypeTable.cs)
- [ ] Clase nombrada igual que archivo
- [ ] Número de migración es secuencial (mayor que última migración)
- [ ] No reutiliza número de migración eliminada

### Code Quality
- [ ] `Up()` implementado correctamente
- [ ] `Down()` implementado y es inverso de `Up()`
- [ ] No depende de código de aplicación (modelos, enums, etc.)
- [ ] Usa constantes para nombres de tablas/columnas
- [ ] Nombres de tablas en `snake_case` y plural
- [ ] Nombres de índices siguen convención `idx_{tabla}_{columnas}`
- [ ] Nombres de FK siguen convención `fk_{tabla}_{columna}`

### Performance & Schema
- [ ] Columnas frecuentemente consultadas tienen índices
- [ ] Foreign keys definidos donde aplica
- [ ] Unique constraints donde aplica
- [ ] Default values apropiados
- [ ] Null/NotNull apropiado
- [ ] TransactionBehavior apropiado

### Security
- [ ] No contiene SQL injection
- [ ] No contiene datos sensibles hardcodeados
- [ ] No contiene contraseñas o secretos

### Testing
- [ ] Probé `Up()` localmente
- [ ] Probé `Down()` localmente
- [ ] Probé `Up()` nuevamente (idempotencia)
- [ ] Verifiqué schema en base de datos
- [ ] Si modifica datos, probé con dataset realista

### Documentation
- [ ] Agregué comentarios para lógica compleja
- [ ] Documenté prerequisitos si existen dependencias
- [ ] Agregué estimado de tiempo si es migración larga

### Team
- [ ] Comuniqué al equipo que estoy trabajando en esta migración
- [ ] Verifiqué que no hay conflictos de numeración con otros branches
```

---

## Conclusión

Las **best practices** de FluentMigrator se resumen en:

1. **Diseño**: Una responsabilidad, atómicas, reversibles, independientes
2. **Versionado**: Secuencial, sin reutilizar, gaps permitidos
3. **Naming**: Convenciones claras y consistentes
4. **Performance**: Índices, batches, downtime considerations
5. **Testing**: Up(), Down(), idempotencia, CI/CD
6. **Team**: Comunicación, resolución de conflictos, code review
7. **Deployment**: Backups, monitoring, rollback plan
8. **Seguridad**: No SQL injection, no secrets, permisos mínimos
9. **Mantenimiento**: Documentación, no eliminar, refactoring correcto

**Principio fundamental**: Trata las migraciones como **código de producción crítico** que merece el mismo rigor que tu lógica de negocio.

---

**Referencias**:
- Proyecto de referencia: `hashira.stone.backend.migrations`
- FluentMigrator Official Docs: https://fluentmigrator.github.io
- PostgreSQL Best Practices: https://wiki.postgresql.org/wiki/Don%27t_Do_This

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Mantenedor**: APSYS Backend Team
