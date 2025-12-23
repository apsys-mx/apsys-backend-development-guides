# FluentMigrator - Migration Patterns

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## 📋 Tabla de Contenidos
1. [Creating Tables](#creating-tables)
2. [Altering Tables](#altering-tables)
3. [Creating Indexes](#creating-indexes)
4. [Foreign Keys](#foreign-keys)
5. [Creating Views](#creating-views)
6. [Executing Raw SQL](#executing-raw-sql)
7. [Data Seeding](#data-seeding)
8. [Complex Patterns](#complex-patterns)
9. [Column Types Reference](#column-types-reference)
10. [Referencias](#referencias)

---

## Creating Tables

### Pattern 1: Simple Table

**Ejemplo del proyecto real** ([M023CreateRolesTable.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M023CreateRolesTable.cs)):

```csharp
using FluentMigrator;

namespace hashira.stone.backend.migrations;

[Migration(23)]
public class M023CreateRolesTable : Migration
{
    private readonly string _tableName = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_tableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("name").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table(_tableName)
            .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Primary key de tipo GUID
- ✅ Column name como string sin límite de longitud
- ✅ NOT NULL constraint
- ✅ Down() elimina la tabla completa

---

### Pattern 2: Table with Multiple Columns and Constraints

**Ejemplo del proyecto real** ([M027CreatePrototypeTable.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M027CreatePrototypeTable.cs)):

```csharp
[Migration(27)]
public class M027CreatePrototypeTable : Migration
{
    private readonly string _tableName = "prototypes";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

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

    public override void Down()
    {
        Delete.Table(_tableName)
            .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Primary key explícito con NotNullable()
- ✅ String con longitud específica (50, 20)
- ✅ UNIQUE constraint inline
- ✅ DateTime columns
- ✅ Multiple constraints en single chain

---

### Pattern 3: Table with Default Values

```csharp
[Migration(30)]
public class M030CreateAuditLogsTable : Migration
{
    private readonly string _tableName = "audit_logs";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_tableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("user_id").AsGuid().NotNullable()
              .WithColumn("action").AsString(100).NotNullable()
              .WithColumn("entity_type").AsString(50).NotNullable()
              .WithColumn("entity_id").AsString(50).NotNullable()
              .WithColumn("timestamp").AsDateTime().NotNullable()
                  .WithDefault(SystemMethods.CurrentDateTime)
              .WithColumn("ip_address").AsString(45).Nullable()
              .WithColumn("user_agent").AsString(500).Nullable();
    }

    public override void Down()
    {
        Delete.Table(_tableName).InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Default value con `SystemMethods.CurrentDateTime`
- ✅ Nullable columns explícitos
- ✅ Different string lengths por column

---

### Pattern 4: Table with Decimal and Numeric Types

```csharp
[Migration(31)]
public class M031CreateProductsTable : Migration
{
    private readonly string _tableName = "products";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Create.Table(_tableName)
              .InSchema(_schemaName)
              .WithColumn("id").AsGuid().PrimaryKey()
              .WithColumn("name").AsString(200).NotNullable()
              .WithColumn("description").AsString(1000).Nullable()
              .WithColumn("price").AsDecimal(18, 2).NotNullable()
              .WithColumn("stock").AsInt32().NotNullable().WithDefaultValue(0)
              .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
              .WithColumn("weight_kg").AsDouble().Nullable()
              .WithColumn("created_at").AsDateTime().NotNullable()
                  .WithDefault(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Table(_tableName).InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Decimal with precision (18, 2)
- ✅ Int32 with default value
- ✅ Boolean with default value
- ✅ Double for floating point
- ✅ Mix of nullable and not nullable columns

---

## Altering Tables

### Pattern 5: Add Column to Existing Table

```csharp
[Migration(32)]
public class M032AddEmailToUsers : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AddColumn("email").AsString(255).Nullable();
    }

    public override void Down()
    {
        Delete.Column("email")
              .FromTable(_tableName)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Alter.Table() para modificar tabla existente
- ✅ AddColumn() con tipo y constraints
- ✅ Down() elimina la columna agregada

---

### Pattern 6: Add Multiple Columns

```csharp
[Migration(33)]
public class M033AddAuditColumnsToUsers : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AddColumn("created_at").AsDateTime().NotNullable()
                 .WithDefault(SystemMethods.CurrentDateTime)
             .AddColumn("updated_at").AsDateTime().Nullable()
             .AddColumn("created_by").AsGuid().Nullable()
             .AddColumn("updated_by").AsGuid().Nullable();
    }

    public override void Down()
    {
        Delete.Column("created_at").FromTable(_tableName).InSchema(_schemaName);
        Delete.Column("updated_at").FromTable(_tableName).InSchema(_schemaName);
        Delete.Column("created_by").FromTable(_tableName).InSchema(_schemaName);
        Delete.Column("updated_by").FromTable(_tableName).InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Multiple AddColumn() chained
- ✅ Audit trail pattern (created_at, updated_at, created_by, updated_by)
- ✅ Down() elimina todas las columnas agregadas

---

### Pattern 7: Alter Column Type

```csharp
[Migration(34)]
public class M034AlterUserNameLength : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AlterColumn("name").AsString(500).NotNullable();
    }

    public override void Down()
    {
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AlterColumn("name").AsString(255).NotNullable();
    }
}
```

**Características**:
- ✅ AlterColumn() para cambiar tipo/longitud
- ✅ Down() revierte al tipo original
- ✅ Mantiene NotNullable constraint

---

### Pattern 8: Rename Column

```csharp
[Migration(35)]
public class M035RenameUserEmailColumn : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Rename.Column("email")
              .OnTable(_tableName)
              .InSchema(_schemaName)
              .To("email_address");
    }

    public override void Down()
    {
        Rename.Column("email_address")
              .OnTable(_tableName)
              .InSchema(_schemaName)
              .To("email");
    }
}
```

**Características**:
- ✅ Rename.Column() para renombrar
- ✅ OnTable() especifica tabla
- ✅ To() especifica nuevo nombre
- ✅ Down() revierte el rename

---

### Pattern 9: Delete Column

```csharp
[Migration(36)]
public class M036RemoveObsoleteColumns : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Delete.Column("legacy_field")
              .FromTable(_tableName)
              .InSchema(_schemaName);
    }

    public override void Down()
    {
        // ⚠️ CUIDADO: Re-agregar columna puede causar pérdida de datos
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AddColumn("legacy_field").AsString().Nullable();
    }
}
```

**Características**:
- ✅ Delete.Column() para eliminar
- ⚠️ Down() re-agrega pero no recupera datos
- ⚠️ Siempre hacer backup antes de eliminar columnas

---

## Creating Indexes

### Pattern 10: Simple Index

```csharp
[Migration(37)]
public class M037AddIndexToUsersEmail : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _indexName = "ix_users_email";

    public override void Up()
    {
        Create.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName)
              .OnColumn("email")
              .Ascending();
    }

    public override void Down()
    {
        Delete.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Create.Index() con nombre explícito
- ✅ OnTable() especifica tabla
- ✅ OnColumn() especifica columna
- ✅ Ascending() especifica orden

---

### Pattern 11: Unique Index

**Ejemplo del proyecto real** ([M024CreateUsersTable.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M024CreateUsersTable.cs:61)):

```csharp
[Migration(38)]
public class M038AddUniqueIndexToUsersEmail : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _indexName = "ix_users_email";

    public override void Up()
    {
        Create.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName)
              .OnColumn("email")
              .Ascending()
              .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ WithOptions().Unique() para índice único
- ✅ Previene duplicados en la columna
- ✅ Más eficiente que UNIQUE constraint para búsquedas

---

### Pattern 12: Composite Index (Multiple Columns)

```csharp
[Migration(39)]
public class M039AddCompositeIndexToOrders : Migration
{
    private readonly string _tableName = "orders";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _indexName = "ix_orders_user_status";

    public override void Up()
    {
        Create.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName)
              .OnColumn("user_id").Ascending()
              .OnColumn("status").Ascending()
              .OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Index(_indexName)
              .OnTable(_tableName)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Multiple OnColumn() para composite index
- ✅ Mix de Ascending() y Descending()
- ✅ Útil para queries con múltiples filtros

---

### Pattern 13: Filtered Index (PostgreSQL, SQL Server)

```csharp
[Migration(40)]
public class M040AddFilteredIndexToUsers : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _indexName = "ix_users_active_email";

    public override void Up()
    {
        // PostgreSQL syntax
        Execute.Sql($@"
            CREATE INDEX {_indexName}
            ON {_schemaName}.{_tableName} (email)
            WHERE is_active = true;
        ");
    }

    public override void Down()
    {
        Execute.Sql($"DROP INDEX IF EXISTS {_schemaName}.{_indexName};");
    }
}
```

**Características**:
- ✅ Filtered index (WHERE clause)
- ✅ Solo indexa filas que cumplen condición
- ✅ Más eficiente para queries filtrados

---

## Foreign Keys

### Pattern 14: Simple Foreign Key

**Ejemplo del proyecto real** ([M024CreateUsersTable.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M024CreateUsersTable.cs:44)):

```csharp
[Migration(41)]
public class M041AddForeignKeyToOrders : Migration
{
    private readonly string _ordersTable = "orders";
    private readonly string _usersTable = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _fkName = "fk_orders_user_id";

    public override void Up()
    {
        Create.ForeignKey(_fkName)
              .FromTable(_ordersTable)
              .InSchema(_schemaName)
              .ForeignColumn("user_id")
              .ToTable(_usersTable)
              .InSchema(_schemaName)
              .PrimaryColumn("id");
    }

    public override void Down()
    {
        Delete.ForeignKey(_fkName)
              .OnTable(_ordersTable)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Create.ForeignKey() con nombre explícito
- ✅ FromTable() especifica tabla origen
- ✅ ForeignColumn() especifica columna FK
- ✅ ToTable() especifica tabla referenciada
- ✅ PrimaryColumn() especifica columna PK

---

### Pattern 15: Foreign Key with Cascade Delete

```csharp
[Migration(42)]
public class M042AddForeignKeyWithCascade : Migration
{
    private readonly string _orderItemsTable = "order_items";
    private readonly string _ordersTable = "orders";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;
    private readonly string _fkName = "fk_order_items_order_id";

    public override void Up()
    {
        Create.ForeignKey(_fkName)
              .FromTable(_orderItemsTable)
              .InSchema(_schemaName)
              .ForeignColumn("order_id")
              .ToTable(_ordersTable)
              .InSchema(_schemaName)
              .PrimaryColumn("id")
              .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.ForeignKey(_fkName)
              .OnTable(_orderItemsTable)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ OnDelete(Rule.Cascade) - elimina registros relacionados
- ✅ Opciones: Cascade, SetNull, SetDefault, NoAction
- ✅ Útil para relaciones padre-hijo

---

### Pattern 16: Multiple Foreign Keys

**Ejemplo del proyecto real** ([M024CreateUsersTable.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M024CreateUsersTable.cs:44)):

```csharp
[Migration(43)]
public class M043CreateJunctionTableWithFKs : Migration
{
    private readonly string _junctionTable = "user_in_roles";
    private readonly string _usersTable = "users";
    private readonly string _rolesTable = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        // 1️⃣ Crear junction table
        Create.Table(_junctionTable)
              .InSchema(_schemaName)
              .WithColumn("user_id").AsGuid().NotNullable()
              .WithColumn("role_id").AsGuid().NotNullable();

        // 2️⃣ Composite primary key
        Create.PrimaryKey($"pk_{_junctionTable}")
              .OnTable(_junctionTable)
              .WithSchema(_schemaName)
              .Columns("user_id", "role_id");

        // 3️⃣ Foreign key a users
        Create.ForeignKey($"fk_{_junctionTable}_user_id")
              .FromTable(_junctionTable)
              .InSchema(_schemaName)
              .ForeignColumn("user_id")
              .ToTable(_usersTable)
              .InSchema(_schemaName)
              .PrimaryColumn("id")
              .OnDelete(System.Data.Rule.Cascade);

        // 4️⃣ Foreign key a roles
        Create.ForeignKey($"fk_{_junctionTable}_role_id")
              .FromTable(_junctionTable)
              .InSchema(_schemaName)
              .ForeignColumn("role_id")
              .ToTable(_rolesTable)
              .InSchema(_schemaName)
              .PrimaryColumn("id")
              .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table(_junctionTable).InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Junction table para relación many-to-many
- ✅ Composite primary key
- ✅ Dos foreign keys con cascade delete
- ✅ Down() elimina tabla completa (FKs se eliminan automáticamente)

---

## Creating Views

### Pattern 17: Simple View with SQL

**Ejemplo del proyecto real** ([M026TechnicalStandardsView.cs](file:///D:/apsys-mx/inspeccion-distancia/hashira.stone.backend/src/hashira.stone.backend.migrations/M026TechnicalStandardsView.cs)):

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
        Execute.Sql($@"DROP VIEW IF EXISTS {fullViewName};");
    }
}
```

**Características**:
- ✅ CREATE OR REPLACE VIEW para PostgreSQL
- ✅ Computed column (search_all) para búsqueda
- ✅ Down() elimina vista con IF EXISTS

---

### Pattern 18: View with JOIN

```csharp
[Migration(44)]
public class M044CreateOrdersWithUserView : Migration
{
    private readonly string _viewName = "orders_with_user_view";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        var fullViewName = $"{_schemaName}.{_viewName}";
        var sql = $@"
            CREATE OR REPLACE VIEW {fullViewName} AS
            SELECT
                o.id AS order_id,
                o.order_number,
                o.total_amount,
                o.status,
                o.created_at,
                u.id AS user_id,
                u.name AS user_name,
                u.email AS user_email
            FROM {_schemaName}.orders o
            INNER JOIN {_schemaName}.users u ON o.user_id = u.id;
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

**Características**:
- ✅ JOIN entre múltiples tablas
- ✅ Alias para columnas (AS order_id, user_id)
- ✅ Útil para queries read-only frecuentes

---

## Executing Raw SQL

### Pattern 19: Execute Custom SQL

```csharp
[Migration(45)]
public class M045AddCustomConstraint : Migration
{
    private readonly string _tableName = "products";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Execute.Sql($@"
            ALTER TABLE {_schemaName}.{_tableName}
            ADD CONSTRAINT chk_price_positive
            CHECK (price > 0);
        ");
    }

    public override void Down()
    {
        Execute.Sql($@"
            ALTER TABLE {_schemaName}.{_tableName}
            DROP CONSTRAINT IF EXISTS chk_price_positive;
        ");
    }
}
```

**Características**:
- ✅ Execute.Sql() para SQL custom
- ✅ CHECK constraint
- ✅ Down() elimina constraint

---

### Pattern 20: Create Function/Stored Procedure

```csharp
[Migration(46)]
public class M046CreateGetUserByEmailFunction : Migration
{
    private readonly string _functionName = "get_user_by_email";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Execute.Sql($@"
            CREATE OR REPLACE FUNCTION {_schemaName}.{_functionName}(p_email VARCHAR)
            RETURNS TABLE(
                id UUID,
                name VARCHAR,
                email VARCHAR,
                created_at TIMESTAMP
            ) AS $$
            BEGIN
                RETURN QUERY
                SELECT u.id, u.name, u.email, u.created_at
                FROM {_schemaName}.users u
                WHERE u.email = p_email;
            END;
            $$ LANGUAGE plpgsql;
        ");
    }

    public override void Down()
    {
        Execute.Sql($@"
            DROP FUNCTION IF EXISTS {_schemaName}.{_functionName}(VARCHAR);
        ");
    }
}
```

**Características**:
- ✅ Stored function en PostgreSQL
- ✅ RETURNS TABLE para resultados
- ✅ PL/pgSQL language

---

## Data Seeding

### Pattern 21: Insert Reference Data

```csharp
[Migration(47)]
public class M047SeedRoles : Migration
{
    private readonly string _tableName = "roles";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Insert.IntoTable(_tableName)
              .InSchema(_schemaName)
              .Row(new
              {
                  id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                  name = "Admin"
              })
              .Row(new
              {
                  id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                  name = "User"
              })
              .Row(new
              {
                  id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                  name = "Guest"
              });
    }

    public override void Down()
    {
        Delete.FromTable(_tableName)
              .InSchema(_schemaName)
              .Row(new { id = Guid.Parse("11111111-1111-1111-1111-111111111111") })
              .Row(new { id = Guid.Parse("22222222-2222-2222-2222-222222222222") })
              .Row(new { id = Guid.Parse("33333333-3333-3333-3333-333333333333") });
    }
}
```

**Características**:
- ✅ Insert.IntoTable() para seed data
- ✅ Row() con anonymous object
- ✅ GUIDs predefinidos para datos de referencia
- ✅ Down() elimina datos insertados

---

### Pattern 22: Bulk Insert with SQL

```csharp
[Migration(48)]
public class M048SeedCategories : Migration
{
    private readonly string _tableName = "categories";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Execute.Sql($@"
            INSERT INTO {_schemaName}.{_tableName} (id, name, description, created_at)
            VALUES
                (gen_random_uuid(), 'Electronics', 'Electronic devices', NOW()),
                (gen_random_uuid(), 'Books', 'Books and magazines', NOW()),
                (gen_random_uuid(), 'Clothing', 'Apparel and accessories', NOW()),
                (gen_random_uuid(), 'Food', 'Food and beverages', NOW());
        ");
    }

    public override void Down()
    {
        Execute.Sql($@"
            DELETE FROM {_schemaName}.{_tableName}
            WHERE name IN ('Electronics', 'Books', 'Clothing', 'Food');
        ");
    }
}
```

**Características**:
- ✅ Bulk insert con SQL
- ✅ gen_random_uuid() para PostgreSQL
- ✅ NOW() para timestamps
- ✅ Down() elimina por WHERE clause

---

## Complex Patterns

### Pattern 23: Rename Table

```csharp
[Migration(49)]
public class M049RenameProductsTable : Migration
{
    private readonly string _oldTableName = "products";
    private readonly string _newTableName = "items";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        Rename.Table(_oldTableName)
              .InSchema(_schemaName)
              .To(_newTableName);
    }

    public override void Down()
    {
        Rename.Table(_newTableName)
              .InSchema(_schemaName)
              .To(_oldTableName);
    }
}
```

**Características**:
- ✅ Rename.Table() para renombrar tabla
- ✅ Mantiene todos los datos y constraints
- ✅ Down() revierte el rename

---

### Pattern 24: Create Schema

```csharp
[Migration(50)]
public class M050CreateReportsSchema : Migration
{
    private readonly string _schemaName = "reports";

    public override void Up()
    {
        if (!Schema.Schema(_schemaName).Exists())
        {
            Create.Schema(_schemaName);
        }
    }

    public override void Down()
    {
        Delete.Schema(_schemaName);
    }
}
```

**Características**:
- ✅ Create.Schema() para nuevo schema
- ✅ Schema.Exists() check para idempotencia
- ✅ Útil para organizar tablas por módulo

---

### Pattern 25: Add Column with Data Migration

```csharp
[Migration(51)]
public class M051AddFullNameToUsers : Migration
{
    private readonly string _tableName = "users";
    private readonly string _schemaName = CustomVersionTableMetaData.SchemaNameValue;

    public override void Up()
    {
        // 1️⃣ Agregar columna nullable
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AddColumn("full_name").AsString(500).Nullable();

        // 2️⃣ Poblar datos desde columnas existentes
        Execute.Sql($@"
            UPDATE {_schemaName}.{_tableName}
            SET full_name = CONCAT(first_name, ' ', last_name)
            WHERE full_name IS NULL;
        ");

        // 3️⃣ Hacer NOT NULL después de poblar
        Alter.Table(_tableName)
             .InSchema(_schemaName)
             .AlterColumn("full_name").AsString(500).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("full_name")
              .FromTable(_tableName)
              .InSchema(_schemaName);
    }
}
```

**Características**:
- ✅ Three-step migration para evitar errores
- ✅ Nullable primero para permitir UPDATE
- ✅ NOT NULL después de poblar datos
- ✅ Útil para transformaciones complejas

---

## Column Types Reference

### Numeric Types

| FluentMigrator Method | SQL Type (PostgreSQL) | Descripción |
|-----------------------|----------------------|-------------|
| `.AsInt16()` | SMALLINT | 2 bytes, -32,768 a 32,767 |
| `.AsInt32()` | INTEGER | 4 bytes, -2.1B a 2.1B |
| `.AsInt64()` | BIGINT | 8 bytes, -9.2E18 a 9.2E18 |
| `.AsDecimal(18, 2)` | NUMERIC(18, 2) | Precisión fija, para dinero |
| `.AsDouble()` | DOUBLE PRECISION | 8 bytes, ~15 dígitos |
| `.AsFloat()` | REAL | 4 bytes, ~6 dígitos |

---

### String Types

| FluentMigrator Method | SQL Type (PostgreSQL) | Descripción |
|-----------------------|----------------------|-------------|
| `.AsString()` | TEXT | Longitud ilimitada |
| `.AsString(50)` | VARCHAR(50) | Longitud máxima 50 |
| `.AsAnsiString()` | VARCHAR | ASCII only |
| `.AsFixedLengthString(10)` | CHAR(10) | Longitud fija |

---

### Date/Time Types

| FluentMigrator Method | SQL Type (PostgreSQL) | Descripción |
|-----------------------|----------------------|-------------|
| `.AsDate()` | DATE | Solo fecha (sin hora) |
| `.AsTime()` | TIME | Solo hora (sin fecha) |
| `.AsDateTime()` | TIMESTAMP | Fecha y hora |
| `.AsDateTime2()` | TIMESTAMP | Alta precisión (SQL Server) |
| `.AsDateTimeOffset()` | TIMESTAMPTZ | Con timezone |

---

### Other Types

| FluentMigrator Method | SQL Type (PostgreSQL) | Descripción |
|-----------------------|----------------------|-------------|
| `.AsBoolean()` | BOOLEAN | true/false |
| `.AsGuid()` | UUID | GUID/UUID |
| `.AsBinary()` | BYTEA | Datos binarios |
| `.AsXml()` | XML | Documentos XML |

---

### SystemMethods

| Method | SQL Equivalent | Descripción |
|--------|----------------|-------------|
| `SystemMethods.CurrentDateTime` | NOW() / GETDATE() | Fecha/hora actual |
| `SystemMethods.CurrentUTCDateTime` | NOW() AT TIME ZONE 'UTC' | UTC timestamp |
| `SystemMethods.CurrentUser` | CURRENT_USER | Usuario actual |
| `SystemMethods.NewGuid` | gen_random_uuid() | Nuevo GUID |

---

## Referencias

### 📚 Documentación Oficial

- [FluentMigrator Migration Syntax](https://fluentmigrator.github.io/articles/migration/migration-syntax.html)
- [FluentMigrator Expression Reference](https://fluentmigrator.github.io/articles/migration/expression-reference.html)
- [PostgreSQL Data Types](https://www.postgresql.org/docs/current/datatype.html)

### 🔗 Guías Relacionadas

- [FluentMigrator Setup](./README.md) - Setup y configuración
- [Best Practices](./best-practices.md) - Best practices
- [Data Migrations Overview](../README.md) - Overview de migraciones

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-11-14 | Versión inicial de Migration Patterns guide |

---

**Siguiente**: [Best Practices](./best-practices.md) - Best practices de FluentMigrator →
