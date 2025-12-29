# NHibernate Mappers (Mapping by Code)

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [ClassMapping\<T\> Pattern](#classmappingt-pattern)
3. [Estructura de un Mapper](#estructura-de-un-mapper)
4. [Mapeo de Propiedades](#mapeo-de-propiedades)
5. [Mapeo de Relaciones](#mapeo-de-relaciones)
6. [Mappers para DAOs (Read-Only)](#mappers-para-daos-read-only)
7. [Tipos de Datos (NHibernateUtil)](#tipos-de-datos-nhibernateutil)
8. [Restricciones y Validaciones](#restricciones-y-validaciones)
9. [Registro de Mappers](#registro-de-mappers)
10. [Mejores Prácticas](#mejores-prácticas)
11. [Antipatrones Comunes](#antipatrones-comunes)
12. [Checklist de Implementación](#checklist-de-implementación)
13. [Ejemplos Completos](#ejemplos-completos)
14. [Referencias](#referencias)

---

## Introducción

Los **Mappers** en NHibernate definen cómo las clases del dominio se mapean a tablas de base de datos. El proyecto **hashira.stone.backend** utiliza **Mapping by Code** (código C#) en lugar de archivos XML, proporcionando:

- ✅ **Type-safety**: Detección de errores en tiempo de compilación
- ✅ **Refactoring**: Renombrado automático con herramientas IDE
- ✅ **IntelliSense**: Autocompletado y documentación inline
- ✅ **Código mantenible**: Todo en C#, sin XML separado
- ✅ **Testeable**: Fácil de probar en unit tests

### ¿Qué es Mapping by Code?

Es el enfoque moderno de NHibernate que utiliza la clase `ClassMapping<T>` para definir mappings mediante código C# fluente:

```csharp
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema("public");
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
        });
    }
}
```

---

## ClassMapping\<T\> Pattern

### Namespace y Herencia

Todos los mappers heredan de `ClassMapping<T>` del namespace `NHibernate.Mapping.ByCode.Conformist`:

```csharp
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class RoleMapper : ClassMapping<Role>
{
    public RoleMapper()
    {
        // Configuración del mapeo
    }
}
```

### Componentes Principales

| Componente | Propósito | Ejemplo |
|------------|-----------|---------|
| `Schema()` | Define el schema de la tabla | `Schema("public")` |
| `Table()` | Define el nombre de la tabla | `Table("users")` |
| `Id()` | Mapea la primary key | `Id(x => x.Id, ...)` |
| `Property()` | Mapea propiedades simples | `Property(x => x.Name, ...)` |
| `Bag()` | Mapea colecciones | `Bag(x => x.Roles, ...)` |
| `ManyToMany()` | Mapea relaciones M:N | `ManyToMany(m => ...)` |
| `ManyToOne()` | Mapea relaciones N:1 | `ManyToOne(x => x.Parent, ...)` |
| `Mutable()` | Define si la entidad es read-only | `Mutable(false)` |

---

## Estructura de un Mapper

### Mapper Básico (Entidad Simple)

Del proyecto de referencia [RoleMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\RoleMapper.cs:1-34):

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="Role"/> entity.
/// </summary>
public class RoleMapper : ClassMapping<Role>
{
    public RoleMapper()
    {
        // 1️⃣ SCHEMA: Define el schema de PostgreSQL
        Schema(AppSchemaResource.SchemaName);

        // 2️⃣ TABLE: Define el nombre de la tabla
        Table("roles");

        // 3️⃣ ID: Mapea la primary key
        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        // 4️⃣ PROPERTIES: Mapea propiedades simples
        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

### Orden de Configuración

```
┌─────────────────────────────────────────┐
│ 1️⃣  Schema("public")                    │ ← Schema de la BD
├─────────────────────────────────────────┤
│ 2️⃣  Table("users")                      │ ← Nombre de tabla
├─────────────────────────────────────────┤
│ 3️⃣  Id(x => x.Id, ...)                  │ ← Primary Key
├─────────────────────────────────────────┤
│ 4️⃣  Property(x => x.Name, ...)          │ ← Propiedades
│    Property(x => x.Email, ...)          │
├─────────────────────────────────────────┤
│ 5️⃣  Bag(x => x.Roles, ...)              │ ← Relaciones
│    ManyToOne(x => x.Parent, ...)        │
└─────────────────────────────────────────┘
```

---

## Mapeo de Propiedades

### 1. Mapeo del Id (Primary Key)

**Patrón estándar en hashira.stone.backend**: Guid con generación manual (Assigned)

```csharp
Id(x => x.Id, map =>
{
    map.Column("id");                      // Nombre de la columna en BD
    map.Generator(Generators.Assigned);    // Generación manual (domain lo asigna)
    map.Type(NHibernateUtil.Guid);        // Tipo de dato: UUID/GUID
});
```

#### Estrategias de Generación de Id

| Generador | Descripción | Uso en hashira.stone.backend |
|-----------|-------------|------------------------------|
| `Generators.Assigned` | **Manual**: El dominio asigna el Id | ✅ **Usado en todos los mappers** |
| `Generators.Identity` | Auto-increment de la BD | ❌ No usado |
| `Generators.Sequence` | Secuencias de PostgreSQL | ❌ No usado |
| `Generators.GuidComb` | GUID optimizado para índices | ❌ No usado |

**¿Por qué Assigned?**: En DDD, el dominio controla la creación de entidades, incluyendo el Id. Esto permite validar duplicados antes de persistir.

### 2. Propiedades Simples (String)

```csharp
Property(x => x.Name, map =>
{
    map.Column("name");                   // Columna en BD
    map.NotNullable(true);                // NOT NULL constraint
    map.Type(NHibernateUtil.String);      // Tipo: VARCHAR
});
```

### 3. Propiedades con Unique Constraint

Del proyecto [TechnicalStandardMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\TechnicalStandardMapper.cs:30-36):

```csharp
Property(x => x.Code, map =>
{
    map.Column("code");
    map.NotNullable(true);
    map.Type(NHibernateUtil.String);
    map.Unique(true);                     // ✅ UNIQUE constraint
});
```

### 4. Propiedades DateTime

Del proyecto [PrototypeMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\PrototypeMapper.cs:31-43):

```csharp
Property(x => x.IssueDate, map =>
{
    map.Column("issue_date");
    map.NotNullable(true);
    map.Type(NHibernateUtil.DateTime);    // ✅ TIMESTAMP
});

Property(x => x.ExpirationDate, map =>
{
    map.Column("expiration_date");
    map.NotNullable(true);
    map.Type(NHibernateUtil.DateTime);
});
```

### 5. Propiedades Opcionales (Nullable)

```csharp
Property(x => x.Description, map =>
{
    map.Column("description");
    map.NotNullable(false);               // ✅ Permite NULL
    map.Type(NHibernateUtil.String);
});
```

---

## Mapeo de Relaciones

### 1. Many-to-Many (Bag + ManyToMany)

Del proyecto [UserMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\UserMapper.cs:41-53):

```csharp
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        // ✅ RELACIÓN MANY-TO-MANY
        Bag(x => x.Roles, map =>
        {
            map.Schema(AppSchemaResource.SchemaName);
            map.Table("user_in_roles");           // Tabla intermedia
            map.Key(k => k.Column("user_id"));    // FK a users
            map.Cascade(Cascade.All);             // Cascada de operaciones
            map.Inverse(false);                   // User es el owner
        },
        map => map.ManyToMany(m =>
        {
            m.Column("role_id");                  // FK a roles
            m.Class(typeof(Role));                // Entidad relacionada
        }));
    }
}
```

#### Estructura de Relación Many-to-Many

```
┌──────────────┐         ┌─────────────────┐         ┌──────────────┐
│    users     │         │ user_in_roles   │         │    roles     │
├──────────────┤         ├─────────────────┤         ├──────────────┤
│ id (PK)      │────────<│ user_id (FK)    │         │ id (PK)      │
│ email        │         │ role_id (FK)    │>────────│ name         │
│ name         │         └─────────────────┘         └──────────────┘
└──────────────┘
```

#### Configuración de Bag

| Método | Propósito | Ejemplo |
|--------|-----------|---------|
| `map.Schema()` | Schema de la tabla intermedia | `map.Schema("public")` |
| `map.Table()` | Nombre de la tabla intermedia | `map.Table("user_in_roles")` |
| `map.Key()` | Columna FK al owner | `map.Key(k => k.Column("user_id"))` |
| `map.Cascade()` | Cascada de operaciones | `map.Cascade(Cascade.All)` |
| `map.Inverse()` | ¿Es el dueño de la relación? | `map.Inverse(false)` |

#### Configuración de ManyToMany

| Método | Propósito | Ejemplo |
|--------|-----------|---------|
| `m.Column()` | Columna FK a la entidad relacionada | `m.Column("role_id")` |
| `m.Class()` | Tipo de la entidad relacionada | `m.Class(typeof(Role))` |

### 2. One-to-Many (Sin ejemplos en referencia)

**Patrón teórico** (no usado en hashira.stone.backend actualmente):

```csharp
Bag(x => x.Orders, map =>
{
    map.Key(k => k.Column("customer_id"));    // FK en tabla orders
    map.Cascade(Cascade.All);
},
map => map.OneToMany());
```

### 3. Many-to-One (Sin ejemplos en referencia)

**Patrón teórico** (no usado en hashira.stone.backend actualmente):

```csharp
ManyToOne(x => x.Customer, map =>
{
    map.Column("customer_id");
    map.NotNullable(true);
    map.Cascade(Cascade.None);
});
```

---

## Mappers para DAOs (Read-Only)

### ¿Qué son los DAOs?

Los **Data Access Objects (DAOs)** son entidades de solo lectura que mapean vistas de base de datos o consultas optimizadas. En hashira.stone.backend se usan para queries complejas con joins.

### Diferencias: Entidad vs DAO

| Característica | Entidad (User) | DAO (UserDao) |
|----------------|----------------|---------------|
| Fuente | Tabla (`users`) | Vista (`users_view`) |
| Mutabilidad | `Mutable(true)` | `Mutable(false)` ✅ |
| Operaciones | CRUD completo | Solo lectura |
| Repositorio | `NHRepository<User, Guid>` | `NHReadOnlyRepository<UserDao, Guid>` |
| Joins | No (lazy loading) | Sí (denormalizados) |

### Mapper para DAO

Del proyecto [TechnicalStandardDaoMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\TechnicalStandardDaoMapper.cs:10-74):

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);

        // ✅ CLAVE: Marca la entidad como READ-ONLY
        Mutable(false);

        // ✅ Mapea a una VISTA en lugar de una tabla
        Table("technical_standards_view");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.CreationDate, map =>
        {
            map.Column("creation_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Code, map =>
        {
            map.Column("code");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        // ✅ Propiedad derivada/calculada de la vista
        Property(x => x.SearchAll, map =>
        {
            map.Column("search_all");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

### Vista PostgreSQL Correspondiente

```sql
CREATE OR REPLACE VIEW public.technical_standards_view AS
SELECT
    ts.id,
    ts.creation_date,
    ts.code,
    ts.name,
    ts.edition,
    ts.status,
    ts.type,
    -- ✅ Columna calculada para búsquedas full-text
    CONCAT_WS(' ',
        LOWER(UNACCENT(ts.code)),
        LOWER(UNACCENT(ts.name)),
        LOWER(UNACCENT(ts.edition))
    ) AS search_all
FROM technical_standards ts;
```

### Beneficios de los DAOs

1. **Performance**: Datos denormalizados, sin lazy loading
2. **Búsquedas complejas**: Campos calculados como `search_all`
3. **Separación de responsabilidades**: Lectura vs Escritura
4. **CQRS**: Command Query Responsibility Segregation

---

## Tipos de Datos (NHibernateUtil)

### Mapeo de Tipos .NET a PostgreSQL

| Tipo .NET | NHibernateUtil | Tipo PostgreSQL | Ejemplo |
|-----------|----------------|-----------------|---------|
| `Guid` | `NHibernateUtil.Guid` | `UUID` | `map.Type(NHibernateUtil.Guid)` |
| `string` | `NHibernateUtil.String` | `VARCHAR` | `map.Type(NHibernateUtil.String)` |
| `DateTime` | `NHibernateUtil.DateTime` | `TIMESTAMP` | `map.Type(NHibernateUtil.DateTime)` |
| `int` | `NHibernateUtil.Int32` | `INTEGER` | `map.Type(NHibernateUtil.Int32)` |
| `long` | `NHibernateUtil.Int64` | `BIGINT` | `map.Type(NHibernateUtil.Int64)` |
| `decimal` | `NHibernateUtil.Decimal` | `NUMERIC` | `map.Type(NHibernateUtil.Decimal)` |
| `bool` | `NHibernateUtil.Boolean` | `BOOLEAN` | `map.Type(NHibernateUtil.Boolean)` |
| `byte[]` | `NHibernateUtil.BinaryBlob` | `BYTEA` | `map.Type(NHibernateUtil.BinaryBlob)` |

### Ejemplo Completo de Tipos

```csharp
public class ProductMapper : ClassMapping<Product>
{
    public ProductMapper()
    {
        Schema("public");
        Table("products");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);              // UUID
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.Type(NHibernateUtil.String);            // VARCHAR
        });

        Property(x => x.Price, map =>
        {
            map.Column("price");
            map.Type(NHibernateUtil.Decimal);           // NUMERIC
        });

        Property(x => x.Stock, map =>
        {
            map.Column("stock");
            map.Type(NHibernateUtil.Int32);             // INTEGER
        });

        Property(x => x.IsActive, map =>
        {
            map.Column("is_active");
            map.Type(NHibernateUtil.Boolean);           // BOOLEAN
        });

        Property(x => x.CreatedAt, map =>
        {
            map.Column("created_at");
            map.Type(NHibernateUtil.DateTime);          // TIMESTAMP
        });
    }
}
```

---

## Restricciones y Validaciones

### 1. Not Null

```csharp
Property(x => x.Email, map =>
{
    map.Column("email");
    map.NotNullable(true);                // ✅ NOT NULL en BD
    map.Type(NHibernateUtil.String);
});
```

### 2. Unique Constraint

```csharp
Property(x => x.Email, map =>
{
    map.Column("email");
    map.NotNullable(true);
    map.Unique(true);                     // ✅ UNIQUE constraint
    map.Type(NHibernateUtil.String);
});
```

### 3. Nullable (permite NULL)

```csharp
Property(x => x.MiddleName, map =>
{
    map.Column("middle_name");
    map.NotNullable(false);               // ✅ Permite NULL
    map.Type(NHibernateUtil.String);
});
```

### 4. Length (para Strings)

```csharp
Property(x => x.Description, map =>
{
    map.Column("description");
    map.Length(500);                      // ✅ VARCHAR(500)
    map.Type(NHibernateUtil.String);
});
```

### 5. Precision y Scale (para Decimales)

```csharp
Property(x => x.Price, map =>
{
    map.Column("price");
    map.Precision(10);                    // ✅ Total de dígitos
    map.Scale(2);                         // ✅ Decimales (NUMERIC(10,2))
    map.Type(NHibernateUtil.Decimal);
});
```

### Tabla Resumen de Restricciones

| Restricción | Método | Ejemplo |
|-------------|--------|---------|
| NOT NULL | `map.NotNullable(true)` | Campo obligatorio |
| NULL | `map.NotNullable(false)` | Campo opcional |
| UNIQUE | `map.Unique(true)` | Valor único en la tabla |
| LENGTH | `map.Length(n)` | Longitud máxima de string |
| PRECISION | `map.Precision(n)` | Total de dígitos (decimal) |
| SCALE | `map.Scale(n)` | Dígitos decimales |

---

## Registro de Mappers

### Configuración en NHSessionFactory

Del proyecto [NHSessionFactory.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHSessionFactory.cs:22-38):

```csharp
using hashira.stone.backend.infrastructure.nhibernate.mappers;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace hashira.stone.backend.infrastructure.nhibernate;

public class NHSessionFactory(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    public ISessionFactory BuildNHibernateSessionFactory()
    {
        // 1️⃣ Crear ModelMapper
        var mapper = new ModelMapper();

        // 2️⃣ ✅ REGISTRO AUTOMÁTICO: Busca todos los ClassMapping<T> en el assembly
        mapper.AddMappings(typeof(RoleMapper).Assembly.ExportedTypes);

        // 3️⃣ Compilar los mappings
        HbmMapping domainMapping = mapper.CompileMappingForAllExplicitlyAddedEntities();

        // 4️⃣ Configurar NHibernate
        var cfg = new Configuration();
        cfg.DataBaseIntegration(c =>
        {
            c.Driver<NpgsqlDriver>();
            c.Dialect<PostgreSQL83Dialect>();
            c.ConnectionString = this.ConnectionString;
            c.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
        });

        // 5️⃣ Agregar mappings compilados
        cfg.AddMapping(domainMapping);

        // 6️⃣ Construir SessionFactory
        return cfg.BuildSessionFactory();
    }
}
```

### ¿Cómo funciona el Registro Automático?

```csharp
mapper.AddMappings(typeof(RoleMapper).Assembly.ExportedTypes);
```

Este método:
1. Obtiene el **Assembly** donde está `RoleMapper`
2. Busca **todos los tipos públicos** (`ExportedTypes`)
3. Filtra solo los que heredan de `ClassMapping<T>`
4. Los registra automáticamente

### Ventajas del Registro Automático

✅ **No hay que registrar cada mapper manualmente**
✅ **Nuevos mappers se detectan automáticamente**
✅ **Menos código boilerplate**
✅ **Menos errores por olvidos**

### Registro Manual (Alternativa)

Si necesitas **control explícito**:

```csharp
var mapper = new ModelMapper();
mapper.AddMapping<UserMapper>();
mapper.AddMapping<RoleMapper>();
mapper.AddMapping<PrototypeMapper>();
mapper.AddMapping<TechnicalStandardMapper>();
// ... agregar cada mapper manualmente
```

❌ **No recomendado**: Requiere mantenimiento manual

---

## Mejores Prácticas

### ✅ 1. Usar AppSchemaResource para el Schema

**Correcto** ✅:
```csharp
using hashira.stone.backend.domain.resources;

public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema(AppSchemaResource.SchemaName);  // ✅ Centralizado
        Table("users");
    }
}
```

**Incorrecto** ❌:
```csharp
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema("public");  // ❌ Hardcoded, difícil cambiar
        Table("users");
    }
}
```

### ✅ 2. Siempre Especificar el Tipo con NHibernateUtil

**Correcto** ✅:
```csharp
Property(x => x.Name, map =>
{
    map.Column("name");
    map.Type(NHibernateUtil.String);  // ✅ Tipo explícito
});
```

**Incorrecto** ❌:
```csharp
Property(x => x.Name, map =>
{
    map.Column("name");
    // ❌ Tipo inferido, puede causar problemas
});
```

### ✅ 3. Usar Generators.Assigned para Ids en DDD

**Correcto** ✅ (patrón hashira.stone.backend):
```csharp
Id(x => x.Id, map =>
{
    map.Column("id");
    map.Generator(Generators.Assigned);  // ✅ Dominio controla el Id
    map.Type(NHibernateUtil.Guid);
});
```

**Incorrecto** ❌ (para DDD):
```csharp
Id(x => x.Id, map =>
{
    map.Column("id");
    map.Generator(Generators.Identity);  // ❌ BD controla el Id
    map.Type(NHibernateUtil.Guid);
});
```

### ✅ 4. Documentar los Mappers

**Correcto** ✅:
```csharp
/// <summary>
/// NHibernate mapping configuration for the <see cref="User"/> entity.
/// </summary>
public class UserMapper : ClassMapping<User>
{
    // ...
}
```

**Incorrecto** ❌:
```csharp
public class UserMapper : ClassMapping<User>  // ❌ Sin documentación
{
    // ...
}
```

### ✅ 5. Usar Mutable(false) para DAOs

**Correcto** ✅:
```csharp
public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Mutable(false);  // ✅ DAO es read-only
        Table("technical_standards_view");
    }
}
```

**Incorrecto** ❌:
```csharp
public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        // ❌ Falta Mutable(false), permite modificaciones
        Table("technical_standards_view");
    }
}
```

---

## Antipatrones Comunes

### ❌ 1. Hardcodear Nombres de Columnas en Múltiples Lugares

**Problema**:
```csharp
// UserMapper.cs
Property(x => x.Email, map => map.Column("email"));

// UserValidator.cs
RuleFor(x => x.Email).NotEmpty().WithMessage("email is required");

// UserRepository.cs
var hql = "from User u where u.email = :email";  // ❌ "email" repetido
```

**Solución** ✅:
Centralizar nombres en constantes o usar lambdas con propiedades del dominio.

### ❌ 2. No Especificar NotNullable

**Problema**:
```csharp
Property(x => x.Email, map =>
{
    map.Column("email");
    map.Type(NHibernateUtil.String);
    // ❌ Falta map.NotNullable(true)
});
```

**Impacto**: NHibernate permite NULL incluso si la columna de BD es NOT NULL, causando errores en runtime.

**Solución** ✅:
```csharp
Property(x => x.Email, map =>
{
    map.Column("email");
    map.NotNullable(true);  // ✅ Coincide con BD
    map.Type(NHibernateUtil.String);
});
```

### ❌ 3. Usar XML y Mapping by Code Simultáneamente

**Problema**:
```csharp
// UserMapper.cs (Mapping by Code)
public class UserMapper : ClassMapping<User> { }

// User.hbm.xml (XML mapping)
<class name="User" table="users">...</class>
```

**Impacto**: Conflictos, comportamiento indefinido, errores difíciles de debuggear.

**Solución** ✅: Elegir **solo un enfoque** (Mapping by Code recomendado).

### ❌ 4. Olvidar Cascade en Relaciones Many-to-Many

**Problema**:
```csharp
Bag(x => x.Roles, map =>
{
    map.Table("user_in_roles");
    map.Key(k => k.Column("user_id"));
    // ❌ Falta map.Cascade(Cascade.All)
},
map => map.ManyToMany(m => m.Column("role_id")));
```

**Impacto**: Cambios en `user.Roles` no se persisten automáticamente.

**Solución** ✅:
```csharp
Bag(x => x.Roles, map =>
{
    map.Table("user_in_roles");
    map.Key(k => k.Column("user_id"));
    map.Cascade(Cascade.All);  // ✅ Cascada de operaciones
},
map => map.ManyToMany(m => m.Column("role_id")));
```

### ❌ 5. No Usar Inverse Correctamente

**Problema**:
```csharp
// UserMapper
Bag(x => x.Roles, map =>
{
    map.Inverse(true);  // ❌ User NO es el owner
    // ...
});

// RoleMapper
Bag(x => x.Users, map =>
{
    map.Inverse(true);  // ❌ Role NO es el owner
    // ...
});
```

**Impacto**: Ninguna entidad controla la relación, cambios no se persisten.

**Solución** ✅: **Exactamente UNA entidad** debe tener `Inverse(false)`:
```csharp
// UserMapper
Bag(x => x.Roles, map =>
{
    map.Inverse(false);  // ✅ User es el owner
    map.Cascade(Cascade.All);
    // ...
});

// RoleMapper (si tiene Bag de Users)
Bag(x => x.Users, map =>
{
    map.Inverse(true);  // ✅ Role NO es el owner
    // ...
});
```

---

## Checklist de Implementación

### Antes de Crear un Mapper

- [ ] Entidad del dominio creada en `domain/entities`
- [ ] Tabla/Vista creada en PostgreSQL
- [ ] Definir si es entidad (mutable) o DAO (read-only)
- [ ] Identificar relaciones con otras entidades
- [ ] Revisar constraints de BD (NOT NULL, UNIQUE, FK)

### Durante la Creación del Mapper

- [ ] Heredar de `ClassMapping<T>`
- [ ] Usar `Schema(AppSchemaResource.SchemaName)`
- [ ] Configurar `Table("nombre_tabla")`
- [ ] Mapear `Id` con `Generators.Assigned` y `NHibernateUtil.Guid`
- [ ] Mapear todas las propiedades con `Property()`
- [ ] Especificar tipo con `NHibernateUtil.*`
- [ ] Configurar `NotNullable()` según BD
- [ ] Configurar `Unique()` si aplica
- [ ] Mapear relaciones con `Bag()` / `ManyToMany()` / `ManyToOne()`
- [ ] Si es DAO: agregar `Mutable(false)`
- [ ] Agregar XML doc (`<summary>`)

### Después de Crear el Mapper

- [ ] Verificar registro automático en `NHSessionFactory`
- [ ] Crear/actualizar tests de integración
- [ ] Probar operaciones CRUD
- [ ] Validar que las relaciones funcionen
- [ ] Verificar que los tipos de BD coincidan
- [ ] Confirmar que las restricciones funcionen

---

## Ejemplos Completos

### Ejemplo 1: Mapper Simple (Role)

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="Role"/> entity.
/// </summary>
public class RoleMapper : ClassMapping<Role>
{
    public RoleMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("roles");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

### Ejemplo 2: Mapper con Múltiples Propiedades (TechnicalStandard)

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class TechnicalStandardMapper : ClassMapping<TechnicalStandard>
{
    public TechnicalStandardMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("technical_standards");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.CreationDate, map =>
        {
            map.Column("creation_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Code, map =>
        {
            map.Column("code");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Edition, map =>
        {
            map.Column("edition");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Status, map =>
        {
            map.Column("status");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Type, map =>
        {
            map.Column("type");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

### Ejemplo 3: Mapper con Many-to-Many (User)

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

/// <summary>
/// NHibernate mapping configuration for the <see cref="User"/> entity.
/// </summary>
public class UserMapper : ClassMapping<User>
{
    public UserMapper()
    {
        Schema(AppSchemaResource.SchemaName);
        Table("users");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.Email, map =>
        {
            map.Column("email");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        // Many-to-Many relationship con Role
        Bag(x => x.Roles, map =>
        {
            map.Schema(AppSchemaResource.SchemaName);
            map.Table("user_in_roles");
            map.Key(k => k.Column("user_id"));
            map.Cascade(Cascade.All);
            map.Inverse(false);
        },
        map => map.ManyToMany(m =>
        {
            m.Column("role_id");
            m.Class(typeof(Role));
        }));
    }
}
```

### Ejemplo 4: Mapper DAO Read-Only (TechnicalStandardDao)

```csharp
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.resources;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace hashira.stone.backend.infrastructure.nhibernate.mappers;

public class TechnicalStandardDaoMapper : ClassMapping<TechnicalStandardDao>
{
    public TechnicalStandardDaoMapper()
    {
        Schema(AppSchemaResource.SchemaName);

        // ✅ MARCA LA ENTIDAD COMO READ-ONLY
        Mutable(false);

        // ✅ MAPEA A UNA VISTA
        Table("technical_standards_view");

        Id(x => x.Id, map =>
        {
            map.Column("id");
            map.Generator(Generators.Assigned);
            map.Type(NHibernateUtil.Guid);
        });

        Property(x => x.CreationDate, map =>
        {
            map.Column("creation_date");
            map.NotNullable(true);
            map.Type(NHibernateUtil.DateTime);
        });

        Property(x => x.Code, map =>
        {
            map.Column("code");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
            map.Unique(true);
        });

        Property(x => x.Name, map =>
        {
            map.Column("name");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Edition, map =>
        {
            map.Column("edition");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Status, map =>
        {
            map.Column("status");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        Property(x => x.Type, map =>
        {
            map.Column("type");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });

        // ✅ PROPIEDAD DERIVADA/CALCULADA DE LA VISTA
        Property(x => x.SearchAll, map =>
        {
            map.Column("search_all");
            map.NotNullable(true);
            map.Type(NHibernateUtil.String);
        });
    }
}
```

---

## Referencias

### Documentación Oficial

- [NHibernate Official Documentation](https://nhibernate.info/doc/)
- [NHibernate Mapping by Code](https://nhibernate.info/doc/nhibernate-reference/mapping-by-code.html)
- [NHibernate GitHub Repository](https://github.com/nhibernate/nhibernate-core)

### Archivos del Proyecto de Referencia

- [NHSessionFactory.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHSessionFactory.cs) - Registro de mappers
- [UserMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\UserMapper.cs) - Many-to-Many
- [RoleMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\RoleMapper.cs) - Mapper simple
- [PrototypeMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\PrototypeMapper.cs) - DateTime properties
- [TechnicalStandardMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\TechnicalStandardMapper.cs) - Múltiples propiedades
- [TechnicalStandardDaoMapper.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\mappers\TechnicalStandardDaoMapper.cs) - DAO read-only

### Guías Relacionadas

- [README.md](./README.md) - Overview de NHibernate
- [repositories.md](./repositories.md) - Implementación de repositorios

---

**Siguiente**: [queries.md](./queries.md) - Consultas con LINQ y HQL
