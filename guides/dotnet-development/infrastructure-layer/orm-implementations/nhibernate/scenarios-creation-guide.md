# Guía de Creación de Escenarios XML para Testing

**Version:** 1.0.0
**Estado:** ✅ Completo
**Última actualización:** 2025-01-20

## Descripción

Esta guía documenta cómo **crear y diseñar escenarios XML** para pruebas de integración de repositorios NHibernate. Los escenarios XML son archivos que contienen datos de prueba predefinidos que se cargan directamente en la base de datos usando NDbUnit, permitiendo aislar completamente los tests del repositorio bajo prueba.

**Guías relacionadas:**
- [Repository Testing Practices](./repository-testing-practices.md) - Cómo **usar** escenarios en tests
- [Integration Tests](./integration-tests.md) - Infraestructura de testing

---

## Tabla de Contenido

1. [¿Qué son los Escenarios XML?](#1-qué-son-los-escenarios-xml)
2. [Anatomía de un Escenario](#2-anatomía-de-un-escenario)
3. [Estructura del XML](#3-estructura-del-xml)
4. [Tipos de Datos](#4-tipos-de-datos)
5. [Manejo de Dependencias](#5-manejo-de-dependencias)
6. [Relaciones entre Entidades](#6-relaciones-entre-entidades)
7. [Nomenclatura y Organización](#7-nomenclatura-y-organización)
8. [Diseño de Escenarios](#8-diseño-de-escenarios)
9. [Patrones Comunes](#9-patrones-comunes)
10. [Best Practices](#10-best-practices)
11. [Anti-Patterns](#11-anti-patterns)
12. [Ejemplos Completos](#12-ejemplos-completos)
13. [Checklist](#13-checklist)

---

## 1. ¿Qué son los Escenarios XML?

### Definición

Los **escenarios XML** son archivos que definen un conjunto de datos de prueba que se insertan directamente en la base de datos antes de ejecutar un test. Utilizan el formato del **Typed DataSet** (AppSchema) y son procesados por **NDbUnit**.

### Propósito

1. **Aislamiento** - No depender del repositorio bajo prueba para preparar datos
2. **Reproducibilidad** - Mismos datos en cada ejecución
3. **Independencia** - Cada test carga solo lo que necesita
4. **Mantenibilidad** - Centralizar datos de prueba
5. **Velocidad** - Inserción directa en DB más rápida que usar repositorios

### Cuándo Crear un Escenario

| Situación | ¿Crear Escenario? | Ejemplo |
|-----------|------------------|---------|
| Test de GetByXXX | ✅ Sí | GetByEmailAsync necesita usuarios existentes |
| Test de UpdateAsync | ✅ Sí | Necesita entidad existente para actualizar |
| Test de DeleteAsync | ✅ Sí | Necesita entidad existente para eliminar |
| Test de CreateAsync (happy path) | ❌ No | No necesita datos previos |
| Test de duplicados | ✅ Sí | Necesita registro existente para validar duplicado |
| Test de relaciones | ✅ Sí | Necesita entidades relacionadas (User + Role) |

---

## 2. Anatomía de un Escenario

### Estructura General

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Comentario describiendo el escenario -->

  <!-- Entidades sin dependencias primero -->
  <tabla_1>
    <campo>valor</campo>
  </tabla_1>

  <!-- Entidades con FK después -->
  <tabla_2>
    <campo>valor</campo>
    <tabla_1_id>guid-de-tabla-1</tabla_1_id>
  </tabla_2>

  <!-- Tablas de join al final -->
  <tabla_1_tabla_2>
    <tabla_1_id>guid</tabla_1_id>
    <tabla_2_id>guid</tabla_2_id>
  </tabla_1_tabla_2>
</AppSchema>
```

### Elementos Clave

1. **Declaración XML** - `<?xml version="1.0" encoding="utf-8"?>`
2. **Elemento raíz** - `<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">`
3. **Elementos por tabla** - Cada elemento corresponde a una tabla de BD
4. **Campos** - Subelementos con valores de columnas
5. **Orden de inserción** - Respetar dependencias (FK)

---

## 3. Estructura del XML

### 3.1. Elemento Raíz - AppSchema

Todos los escenarios deben usar el elemento raíz `AppSchema`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Contenido -->
</AppSchema>
```

### 3.2. Nombres de Tablas

Los nombres de elementos XML deben coincidir **exactamente** con los nombres de tablas en la base de datos:

```xml
<!-- ✅ CORRECTO - nombre de tabla en snake_case -->
<users>
  <id>...</id>
</users>

<technical_standards>
  <id>...</id>
</technical_standards>

<!-- ❌ INCORRECTO - PascalCase o camelCase -->
<Users>...</Users>
<TechnicalStandards>...</TechnicalStandards>
```

### 3.3. Nombres de Columnas

Los nombres de campos deben coincidir con las columnas de la tabla:

```xml
<users>
  <!-- ✅ CORRECTO - snake_case como en BD -->
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <name>User Name</name>
  <creation_date>2024-01-15T10:00:00</creation_date>

  <!-- ❌ INCORRECTO - camelCase o PascalCase -->
  <Id>...</Id>
  <Email>...</Email>
  <CreationDate>...</CreationDate>
</users>
```

### 3.4. Múltiples Filas

Repetir el elemento tabla para cada fila:

```xml
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Primera fila -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>user1@example.com</email>
    <name>User One</name>
  </users>

  <!-- Segunda fila -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>user2@example.com</email>
    <name>User Two</name>
  </users>

  <!-- Tercera fila -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440003</id>
    <email>user3@example.com</email>
    <name>User Three</name>
  </users>
</AppSchema>
```

---

## 4. Tipos de Datos

### 4.1. GUID (UUID)

Usar GUIDs fijos en formato estándar:

```xml
<!-- ✅ CORRECTO - GUID estándar -->
<id>550e8400-e29b-41d4-a716-446655440001</id>
<user_id>660e8400-e29b-41d4-a716-446655440001</user_id>

<!-- ❌ INCORRECTO - Sin guiones -->
<id>550e8400e29b41d4a716446655440001</id>

<!-- ❌ INCORRECTO - Mayúsculas -->
<id>550E8400-E29B-41D4-A716-446655440001</id>
```

**Convención de GUIDs:**
- Usar secuencias lógicas para facilitar identificación
- Ejemplo: `550e8400-...-0001`, `550e8400-...-0002`, etc.
- Mantener consistencia entre escenarios relacionados

### 4.2. Strings

Texto simple sin escapar (a menos que contenga caracteres especiales):

```xml
<!-- ✅ CORRECTO -->
<name>John Doe</name>
<email>user@example.com</email>
<description>This is a description</description>

<!-- Caracteres especiales XML deben escaparse -->
<description>This &amp; That</description>
<note>Price &lt; 100</note>
```

### 4.3. Fechas y Timestamps

Formato ISO 8601:

```xml
<!-- ✅ CORRECTO - ISO 8601 -->
<creation_date>2024-01-15T10:00:00</creation_date>
<issue_date>2024-01-15T00:00:00</issue_date>
<expiration_date>2024-12-31T23:59:59</expiration_date>

<!-- Con timezone UTC -->
<creation_date>2024-01-15T10:00:00Z</creation_date>

<!-- ❌ INCORRECTO - Formato no estándar -->
<creation_date>15/01/2024</creation_date>
<creation_date>2024-01-15</creation_date>
```

**Convención de fechas:**
- Usar fechas fijas conocidas (2024-01-15)
- Para tests temporales, usar fechas relativas al "presente" del test
- Siempre incluir hora completa (HH:mm:ss)

### 4.4. Booleanos

Usar `true` o `false` en minúsculas:

```xml
<!-- ✅ CORRECTO -->
<locked>false</locked>
<is_active>true</is_active>
<enabled>true</enabled>

<!-- ❌ INCORRECTO -->
<locked>False</locked>
<locked>0</locked>
<locked>1</locked>
```

### 4.5. Números

Números enteros o decimales sin formato especial:

```xml
<!-- Enteros -->
<quantity>100</quantity>
<count>5</count>

<!-- Decimales con punto -->
<price>99.99</price>
<rate>0.05</rate>

<!-- ❌ INCORRECTO - Coma para decimales -->
<price>99,99</price>
```

### 4.6. Enums (almacenados como short/int)

Usar el valor numérico del enum:

```csharp
// En código C#
public enum UserStatus : short
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}
```

```xml
<!-- En XML - usar valor numérico -->
<status>1</status>  <!-- Active -->
<status>0</status>  <!-- Inactive -->

<!-- ❌ INCORRECTO - usar nombre del enum -->
<status>Active</status>
```

### 4.7. Campos Nullable

Para campos nullable, omitir el campo o usar elemento vacío:

```xml
<!-- Opción 1: Omitir el campo completamente -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <!-- granted_by_user_id es null - no se incluye -->
</users>

<!-- Opción 2: Campo vacío (depende del tipo) -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <middle_name></middle_name>
</users>
```

---

## 5. Manejo de Dependencias

### 5.1. Orden de Inserción

**CRÍTICO:** El orden de los elementos en el XML determina el orden de inserción en la BD. Siempre insertar en este orden:

1. **Tablas sin FK** (entidades independientes)
2. **Tablas con FK** (entidades dependientes)
3. **Tablas de join** (relaciones many-to-many)

### 5.2. Ejemplo de Orden Correcto

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">

  <!-- 1. PRIMERO: Tablas sin dependencias -->
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </roles>

  <organizations>
    <id>770e8400-e29b-41d4-a716-446655440001</id>
    <name>APSYS MX</name>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </organizations>

  <!-- 2. SEGUNDO: Tablas con FK a las anteriores -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>user@example.com</email>
    <name>User Name</name>
    <organization_id>770e8400-e29b-41d4-a716-446655440001</organization_id>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <!-- 3. TERCERO: Tablas de join -->
  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
  </users_in_roles>

</AppSchema>
```

### 5.3. Grafo de Dependencias

Ejemplo de dependencias en un proyecto:

```
roles (sin FK)
  ↑
users_in_roles (FK a roles y users)
  ↑
users (sin FK o FK a organizations)
  ↑
organizations (sin FK)

modules (sin FK)
  ↑
actived_modules (FK a modules y organizations)
  ↑
module_users (FK a actived_modules)
```

**Orden de inserción en XML:**

1. `roles`
2. `organizations`
3. `modules`
4. `users`
5. `actived_modules`
6. `module_users`
7. `users_in_roles`

### 5.4. Verificar Foreign Keys

Antes de crear un escenario, revisar el esquema de BD:

```sql
-- Ver FKs de una tabla
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_name = 'users';
```

---

## 6. Relaciones entre Entidades

### 6.1. One-to-Many (1:N)

Una organización tiene muchos usuarios:

```xml
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">

  <!-- Tabla padre (One) -->
  <organizations>
    <id>770e8400-e29b-41d4-a716-446655440001</id>
    <name>APSYS MX</name>
  </organizations>

  <!-- Tabla hija (Many) -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>user1@example.com</email>
    <name>User One</name>
    <organization_id>770e8400-e29b-41d4-a716-446655440001</organization_id>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>user2@example.com</email>
    <name>User Two</name>
    <organization_id>770e8400-e29b-41d4-a716-446655440001</organization_id>
  </users>

</AppSchema>
```

### 6.2. Many-to-Many (N:M)

Usuarios y roles (relación muchos a muchos con tabla de join):

```xml
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">

  <!-- Lado 1 de la relación -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>user1@example.com</email>
    <name>User One</name>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>user2@example.com</email>
    <name>User Two</name>
  </users>

  <!-- Lado 2 de la relación -->
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
  </roles>

  <roles>
    <id>660e8400-e29b-41d4-a716-446655440002</id>
    <name>User</name>
  </roles>

  <!-- Tabla de join (después de ambos lados) -->
  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
  </users_in_roles>

  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440002</role_id>
  </users_in_roles>

  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440002</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440002</role_id>
  </users_in_roles>

</AppSchema>
```

### 6.3. Self-Referencing (Árbol)

Entidades con FK a sí mismas:

```xml
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">

  <!-- Nodo raíz (parent_id es null) -->
  <categories>
    <id>880e8400-e29b-41d4-a716-446655440001</id>
    <name>Electronics</name>
    <!-- parent_id omitido = null -->
  </categories>

  <!-- Nodos hijos -->
  <categories>
    <id>880e8400-e29b-41d4-a716-446655440002</id>
    <name>Computers</name>
    <parent_id>880e8400-e29b-41d4-a716-446655440001</parent_id>
  </categories>

  <categories>
    <id>880e8400-e29b-41d4-a716-446655440003</id>
    <name>Laptops</name>
    <parent_id>880e8400-e29b-41d4-a716-446655440002</parent_id>
  </categories>

</AppSchema>
```

---

## 7. Nomenclatura y Organización

### 7.1. Convenciones de Nombres

| Patrón | Uso | Ejemplo |
|--------|-----|---------|
| `Create{Entity}s.xml` | Entidades individuales simples | `CreateUsers.xml` |
| `Create{Scenario}.xml` | Escenario específico complejo | `CreateAdminUser.xml` |
| `###_{Entity}.xml` | Con numeración para orden | `030_ActivedModules.xml` |
| `CreateSandBox.xml` | Escenario vacío (solo schema) | `CreateSandBox.xml` |

### 7.2. Cuándo Usar Cada Patrón

**Create{Entity}s.xml** - Para entidades básicas:
```
CreateUsers.xml       → Solo tabla users
CreateRoles.xml       → Solo tabla roles
CreatePrototypes.xml  → Solo tabla prototypes
```

**Create{Scenario}.xml** - Para escenarios de negocio:
```
CreateAdminUser.xml          → User + Role + users_in_roles con admin
CreateUserWithPrototypes.xml → User + múltiples Prototypes relacionados
CreateOrganizationSetup.xml  → Organization + Users + Modules completo
```

**###_{Entity}.xml** - Para orden de carga o jerarquía:
```
010_Organizations.xml  → Base: Organizations
020_Modules.xml        → Después: Modules
030_ActivedModules.xml → Después: ActivedModules (FK a Organizations y Modules)
040_ModuleUsers.xml    → Último: ModuleUsers (FK a ActivedModules)
```

**CreateSandBox.xml** - Escenario vacío:
```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Sin datos - solo estructura -->
</AppSchema>
```

### 7.3. Estructura de Carpetas

```
tests/
└── {proyecto}.infrastructure.tests/
    ├── scenarios/
    │   ├── base/                    # Escenarios base reutilizables
    │   │   ├── CreateUsers.xml
    │   │   ├── CreateRoles.xml
    │   │   └── CreateOrganizations.xml
    │   ├── complex/                 # Escenarios complejos
    │   │   ├── CreateAdminUser.xml
    │   │   └── CreateOrganizationSetup.xml
    │   ├── ordered/                 # Escenarios con orden
    │   │   ├── 010_Organizations.xml
    │   │   ├── 020_Modules.xml
    │   │   └── 030_ActivedModules.xml
    │   └── CreateSandBox.xml        # En raíz
    └── nhibernate/
        └── NH*RepositoryTests.cs
```

---

## 8. Diseño de Escenarios

### 8.1. Principios de Diseño

1. **Atomicidad** - Cada escenario debe tener un propósito claro
2. **Minimalismo** - Solo los datos necesarios para el test
3. **Reutilización** - Escenarios base pequeños y composables
4. **Claridad** - Nombres descriptivos y comentarios
5. **Independencia** - No asumir que otros escenarios fueron cargados

### 8.2. Granularidad

**✅ CORRECTO - Escenarios focalizados:**

```
CreateUsers.xml           → 2-3 usuarios básicos
CreateAdminUser.xml       → 1 admin con rol
CreateUsersWithProfiles.xml → Usuarios con perfiles completos
```

**❌ INCORRECTO - Escenario monolítico:**

```
CreateEverything.xml → Todas las entidades del sistema
```

### 8.3. Composición vs Duplicación

**Estrategia de composición:**

Si `TestA` necesita Users y `TestB` necesita Users + Roles:

```csharp
// TestA
LoadScenario("CreateUsers");

// TestB - Puede componer
LoadScenario("CreateUsers");
LoadScenario("CreateRoles");
// Y luego crear relación manualmente si es simple

// O usar escenario específico
LoadScenario("CreateUsersWithRoles");
```

**Cuándo duplicar vs componer:**

| Situación | Estrategia | Razón |
|-----------|-----------|-------|
| Datos idénticos en múltiples tests | Escenario compartido | DRY principle |
| Datos similares pero con variaciones | Escenarios separados | Claridad |
| Escenario simple (1-2 tablas) | Compartido | Fácil mantener |
| Escenario complejo (5+ tablas) | Específico por test | Evitar dependencias |

### 8.4. Datos Realistas vs Datos de Prueba

**Datos realistas** - Para tests de integración que simulan uso real:

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>carlos.almanza@apsysmx.com</email>
  <name>Carlos Almanza</name>
  <phone>+52-555-1234-5678</phone>
  <creation_date>2024-01-15T10:00:00</creation_date>
</users>
```

**Datos de prueba** - Para tests específicos de validación:

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>test1@example.com</email>
  <name>Test User 1</name>
  <creation_date>2024-01-01T00:00:00</creation_date>
</users>
```

**Recomendación:** Usar datos realistas para escenarios base, datos de prueba para edge cases.

### 8.5. Cantidad de Datos

| Tipo de Test | Cantidad Recomendada | Ejemplo |
|--------------|---------------------|---------|
| GetByXXX (existe) | 1-3 registros | 2 users para probar búsqueda |
| GetByXXX (filtros) | 5-10 registros | 5 users con diferentes statuses |
| GetAll / Pagination | 10-20 registros | 15 prototypes para probar paginación |
| Relaciones M:N | 2-4 por lado | 2 users, 3 roles, 4 relaciones |
| Duplicados | 1 registro | 1 user existente para validar duplicado |

---

## 9. Patrones Comunes

### 9.1. Escenario Base - Entidades Simples

**Propósito:** Crear registros mínimos para tests básicos.

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Escenario: 3 usuarios básicos para tests de búsqueda -->

  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <name>Usuario Uno</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>usuario2@example.com</email>
    <name>Usuario Dos</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440003</id>
    <email>usuario3@example.com</email>
    <name>Usuario Tres</name>
    <locked>true</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

</AppSchema>
```

**Usos:**
- Tests de `GetByEmailAsync`
- Tests de `GetAsync` con filtros
- Tests de `CountAsync`

### 9.2. Escenario con Relaciones - Setup Completo

**Propósito:** Crear un usuario admin completo con todos sus roles y permisos.

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Escenario: Admin user completamente configurado -->

  <!-- Rol de administrador -->
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </roles>

  <!-- Usuario admin -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>admin@apsysmx.com</email>
    <name>System Administrator</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <!-- Relación usuario-rol -->
  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
  </users_in_roles>

</AppSchema>
```

**Usos:**
- Tests de `AddUserToRoleAsync` (validar duplicado)
- Tests de `RemoveUserFromRoleAsync`
- Tests de autorización

### 9.3. Escenario de Validación - Edge Cases

**Propósito:** Datos específicos para probar edge cases.

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Escenario: Prototypes con diferentes estados para validación -->

  <!-- Prototipo activo -->
  <prototypes>
    <id>990e8400-e29b-41d4-a716-446655440001</id>
    <number>PR-001</number>
    <issue_date>2024-01-01T00:00:00</issue_date>
    <expiration_date>2024-12-31T23:59:59</expiration_date>
    <status>Active</status>
    <creation_date>2024-01-01T10:00:00</creation_date>
  </prototypes>

  <!-- Prototipo expirado -->
  <prototypes>
    <id>990e8400-e29b-41d4-a716-446655440002</id>
    <number>PR-002</number>
    <issue_date>2023-01-01T00:00:00</issue_date>
    <expiration_date>2023-12-31T23:59:59</expiration_date>
    <status>Expired</status>
    <creation_date>2023-01-01T10:00:00</creation_date>
  </prototypes>

  <!-- Prototipo cancelado -->
  <prototypes>
    <id>990e8400-e29b-41d4-a716-446655440003</id>
    <number>PR-003</number>
    <issue_date>2024-01-01T00:00:00</issue_date>
    <expiration_date>2024-06-30T23:59:59</expiration_date>
    <status>Cancelled</status>
    <creation_date>2024-01-01T10:00:00</creation_date>
  </prototypes>

</AppSchema>
```

**Usos:**
- Tests de `GetByStatusAsync`
- Tests de filtros por rango de fechas
- Tests de validación de estados

### 9.4. Escenario Jerárquico - Dependencias en Cadena

**Propósito:** Datos con múltiples niveles de dependencia.

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- Escenario: Organization → Modules → ActivedModules → ModuleUsers -->

  <!-- Nivel 1: Organization -->
  <organizations>
    <id>afdd687c-5cb1-4171-aa6e-77c40469ed4a</id>
    <name>APSYS MX</name>
    <email>contact@apsysmx.com</email>
    <creation_date>2024-01-01T00:00:00</creation_date>
  </organizations>

  <!-- Nivel 2: Module -->
  <modules>
    <id>2d0ae5b0-8c90-4c8a-917c-5e6d3a9c38f5</id>
    <name>Inspection Module</name>
    <description>Vehicle inspection management</description>
    <creation_date>2024-01-01T00:00:00</creation_date>
  </modules>

  <!-- Nivel 3: ActivedModule (depende de Organization y Module) -->
  <actived_modules>
    <id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</id>
    <organization_id>afdd687c-5cb1-4171-aa6e-77c40469ed4a</organization_id>
    <module_id>2d0ae5b0-8c90-4c8a-917c-5e6d3a9c38f5</module_id>
    <status>1</status>
    <activation_date>2024-01-15T00:00:00</activation_date>
    <creation_date>2024-01-15T00:00:00</creation_date>
  </actived_modules>

  <!-- Nivel 4: ModuleUser (depende de ActivedModule) -->
  <module_users>
    <id>7f8a9b0c-1d2e-3f4a-5b6c-7d8e9f0a1b2c</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-20T00:00:00</access_granted_date>
    <granted_by_user_id>f6ae8ae5-bdc9-4010-85ca-e7e7e7bfa34c</granted_by_user_id>
    <status>1</status>
    <creation_date>2024-01-20T00:00:00</creation_date>
  </module_users>

</AppSchema>
```

**Usos:**
- Tests de `CreateAsync` con dependencias complejas
- Tests de `GetActiveByOrganizationIdAsync`
- Tests de cascadas

---

## 10. Best Practices

### 10.1. Documentación Inline

**✅ CORRECTO - Comentarios descriptivos:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!--
    Escenario: CreateUsersWithDifferentStatuses
    Propósito: Tests de filtros por status
    Contiene: 3 usuarios activos, 2 inactivos, 1 suspendido
  -->

  <!-- Usuarios activos -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>active1@example.com</email>
    <name>Active User 1</name>
    <status>1</status>
  </users>

  <!-- ... -->

  <!-- Usuarios inactivos -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440004</id>
    <email>inactive1@example.com</email>
    <name>Inactive User 1</name>
    <status>0</status>
  </users>

</AppSchema>
```

### 10.2. GUIDs Consistentes

**Usar patrones de GUIDs fáciles de identificar:**

```xml
<!-- ✅ CORRECTO - Patrón identificable -->
<!-- Users: 550e8400-...-000X -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
</users>
<users>
  <id>550e8400-e29b-41d4-a716-446655440002</id>
</users>

<!-- Roles: 660e8400-...-000X -->
<roles>
  <id>660e8400-e29b-41d4-a716-446655440001</id>
</roles>

<!-- Organizations: 770e8400-...-000X -->
<organizations>
  <id>770e8400-e29b-41d4-a716-446655440001</id>
</organizations>
```

**Ventajas:**
- Fácil identificar tipo de entidad por el GUID
- Debugging más sencillo
- Copiar/pegar entre escenarios sin confusión

### 10.3. Fechas Relativas Documentadas

```xml
<!-- ✅ CORRECTO - Fechas con significado -->
<!-- Base date: 2024-01-15 -->
<prototypes>
  <id>990e8400-e29b-41d4-a716-446655440001</id>
  <number>PR-001</number>
  <!-- Issued: base date -->
  <issue_date>2024-01-15T00:00:00</issue_date>
  <!-- Expires: base + 365 days -->
  <expiration_date>2025-01-15T00:00:00</expiration_date>
</prototypes>
```

### 10.4. Valores de Enums Documentados

```xml
<!-- ✅ CORRECTO - Documentar qué significa el número -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <!-- UserStatus.Active = 1 -->
  <status>1</status>
</users>

<prototypes>
  <id>990e8400-e29b-41d4-a716-446655440001</id>
  <number>PR-001</number>
  <!-- PrototypeStatus: Active = "Active" -->
  <status>Active</status>
</prototypes>
```

### 10.5. Validar el XML

Antes de usar un escenario, validar que el XML es correcto:

```bash
# Validar sintaxis XML
xmllint --noout CreateUsers.xml

# O en PowerShell
[xml]$xml = Get-Content CreateUsers.xml
```

### 10.6. Usar Datos Mínimos

**✅ CORRECTO - Solo campos necesarios:**

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <name>User Name</name>
  <creation_date>2024-01-15T10:00:00</creation_date>
  <!-- Solo campos requeridos -->
</users>
```

**❌ INCORRECTO - Todos los campos aunque no sean necesarios:**

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>user@example.com</email>
  <name>User Name</name>
  <phone></phone>
  <address></address>
  <city></city>
  <state></state>
  <country></country>
  <postal_code></postal_code>
  <creation_date>2024-01-15T10:00:00</creation_date>
  <update_date></update_date>
  <deleted_date></deleted_date>
  <notes></notes>
  <!-- Demasiados campos vacíos -->
</users>
```

### 10.7. Versionamiento

Si un escenario cambia significativamente, considerar crear versión nueva:

```
scenarios/
├── CreateUsers.xml           # Versión actual
├── CreateUsers_v1.xml        # Versión antigua (deprecated)
└── CreateUsersMinimal.xml    # Variante minimalista
```

---

## 10.8. Generación de Escenarios mediante Clases C#

### ¿Por qué usar Clases Generadoras?

En proyectos maduros, los escenarios XML NO se crean ni editan manualmente. En su lugar, se **generan** a partir de **clases C# que implementan `IScenario`**. Este approach tiene múltiples ventajas:

**✅ Ventajas de Clases Generadoras:**

1. **Type Safety** - El compilador verifica que las propiedades y métodos existen
2. **Refactoring** - Cambios en entidades se reflejan automáticamente en escenarios
3. **Reutilización** - Usar repositorios y lógica de dominio para crear datos consistentes
4. **Validación** - Los datos generados pasan por las mismas validaciones que el código de producción
5. **Mantenibilidad** - Un cambio en el modelo actualiza todos los escenarios al regenerar
6. **Dependencias Explícitas** - Las clases pueden especificar escenarios prerequisitos

### Anatomía de una Clase Generadora

```csharp
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.scenarios;

namespace tests.hashira.stone.backend.scenarios;

/// <summary>
/// Scenario to create users
/// </summary>
public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    // Nombre del archivo XML que se generará
    public string ScenarioFileName => "CreateUsers";

    // Escenario que debe ejecutarse antes (opcional)
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    // Método que crea los datos usando repositorios
    public async Task SeedData()
    {
        var users = new List<(string Email, string Name)>
        {
            ("usuario1@example.com", "Carlos Rodríguez"),
            ("usuario2@example.com", "Ana María González"),
            ("usuario3@example.com", "José Luis Martínez")
        };

        try
        {
            this._uoW.BeginTransaction();
            foreach (var (email, name) in users)
                await this._uoW.Users.CreateAsync(email, name);
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

### Elementos Clave

| Elemento | Propósito | Ejemplo |
|----------|-----------|---------|
| `IScenario` | Interface que todas las clases deben implementar | `public class Sc030CreateUsers : IScenario` |
| `ScenarioFileName` | Nombre del XML que se generará (sin extensión) | `"CreateUsers"` → `CreateUsers.xml` |
| `PreloadScenario` | Escenario que debe ejecutarse antes (opcional) | `typeof(Sc020CreateRoles)` |
| `SeedData()` | Método async que crea los datos en BD | Usa repositorios del UnitOfWork |
| Inyección de Dependencias | Constructor recibe `IUnitOfWork` | `Sc030CreateUsers(IUnitOfWork uoW)` |

### Convención de Nombres

```
Sc###Create{Entity}.cs

Donde:
- Sc = Scenario
- ### = Número de orden (010, 020, 030...)
- Create{Entity} = Nombre descriptivo
```

**Ejemplos:**
```
Sc010CreateSandBox.cs       → Escenario vacío (limpia DB)
Sc020CreateRoles.cs         → Crea roles base
Sc030CreateUsers.cs         → Crea usuarios (depende de Roles)
Sc031CreateAdminUser.cs     → Variante: usuario admin
Sc040CreateTechnicalStandards.cs
Sc050CreatePrototypes.cs
```

### Flujo de Generación

```
1. Escribir clase Sc###Create{Entity}.cs
   ↓
2. Implementar SeedData() usando repositorios
   ↓
3. Ejecutar generador de escenarios (script/tool)
   ↓
4. Se genera archivo XML en carpeta scenarios/
   ↓
5. Los tests cargan el XML con LoadScenario()
```

### Estructura de Proyecto

```
tests/
├── {proyecto}.scenarios/           # Proyecto de clases generadoras
│   ├── IScenario.cs               # Interface base
│   ├── ScenarioBuilder.cs         # Builder para ejecutar generadores
│   ├── Sc010CreateSandBox.cs
│   ├── Sc020CreateRoles.cs
│   ├── Sc030CreateUsers.cs
│   ├── Sc031CreateAdminUser.cs
│   ├── Sc040CreateTechnicalStandards.cs
│   └── Sc050CreatePrototypes.cs
│
└── {proyecto}.infrastructure.tests/
    └── scenarios/                  # Archivos XML generados
        ├── CreateSandBox.xml
        ├── CreateRoles.xml
        ├── CreateUsers.xml
        ├── CreateAdminUser.xml
        ├── CreateTechnicalStandards.xml
        └── CreatePrototypes.xml
```

### Ejemplo con Dependencias

```csharp
// Sc050CreatePrototypes.cs
public class Sc050CreatePrototypes(IUnitOfWork uow) : IScenario
{
    private readonly IUnitOfWork _uow = uow;

    public string ScenarioFileName => "CreatePrototypes";

    // Depende de TechnicalStandards
    public Type? PreloadScenario => typeof(Sc040CreateTechnicalStandards);

    public async Task SeedData()
    {
        var prototypes = new List<(string Number, DateTime IssueDate, DateTime ExpirationDate, string Status)>
        {
            ("PR-001", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(20), "Active"),
            ("PR-002", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(25), "Active"),
            ("PR-003", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30), "Expired")
        };

        try
        {
            this._uow.BeginTransaction();
            foreach (var (number, issueDate, expirationDate, status) in prototypes)
                await this._uow.Prototypes.CreateAsync(number, issueDate, expirationDate, status);
            this._uow.Commit();
        }
        catch
        {
            this._uow.Rollback();
            throw;
        }
    }
}
```

### Cuándo Usar Clases Generadoras vs XML Manual

| Escenario | Usar Clases Generadoras | Usar XML Manual |
|-----------|------------------------|-----------------|
| Proyecto nuevo | ❌ No (empezar simple) | ✅ Sí |
| Proyecto maduro con >10 escenarios | ✅ Sí | ❌ No |
| Modelo cambia frecuentemente | ✅ Sí | ❌ No |
| Necesita validación de dominio | ✅ Sí | ❌ No |
| Prototipo rápido | ❌ No | ✅ Sí |
| CI/CD con regeneración automática | ✅ Sí | ❌ No |

### Best Practices para Clases Generadoras

1. **Usar repositorios, no SQL directo**
   ```csharp
   // ✅ CORRECTO
   await this._uoW.Users.CreateAsync(email, name);

   // ❌ INCORRECTO
   await session.ExecuteAsync("INSERT INTO users...");
   ```

2. **Manejar transacciones explícitamente**
   ```csharp
   try {
       this._uoW.BeginTransaction();
       // ... operaciones
       this._uoW.Commit();
   } catch {
       this._uoW.Rollback();
       throw;
   }
   ```

3. **Datos realistas y consistentes**
   ```csharp
   // ✅ CORRECTO - Datos realistas
   ("usuario1@example.com", "Carlos Rodríguez")

   // ❌ INCORRECTO - Datos sin sentido
   ("test@test.com", "Test Test")
   ```

4. **Documentar propósito del escenario**
   ```csharp
   /// <summary>
   /// Scenario to create 5 users with different statuses for filter testing
   /// Used in: NHUserRepositoryTests
   /// </summary>
   public class Sc030CreateUsers : IScenario
   ```

5. **Organizar por número de orden**
   - `Sc010` = Base/SandBox
   - `Sc020` = Entidades sin dependencias
   - `Sc030-Sc039` = Entidades con dependencias nivel 1
   - `Sc040-Sc049` = Entidades con dependencias nivel 2
   - Y así sucesivamente

---

## 11. Anti-Patterns

### 11.1. ❌ Escenarios Monolíticos

**INCORRECTO:**

```xml
<!-- CreateEverything.xml - 5000 líneas con todas las entidades -->
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!-- 100 users -->
  <!-- 50 roles -->
  <!-- 200 prototypes -->
  <!-- 300 technical standards -->
  <!-- 1000 relationships -->
  <!-- ... -->
</AppSchema>
```

**CORRECTO:**

```
scenarios/
├── CreateUsers.xml           # 3-5 users
├── CreateRoles.xml           # 2-3 roles
├── CreatePrototypes.xml      # 5-10 prototypes
└── CreateTechnicalStandards.xml  # 10-20 standards
```

### 11.2. ❌ GUIDs Aleatorios

**INCORRECTO:**

```xml
<!-- No hay patrón, difícil de debuggear -->
<users>
  <id>a7f3e2d1-9b8c-4f5e-a1d2-3c4b5a6e7f8g</id>
</users>
<users>
  <id>9d4c7b2a-1e8f-3a5d-6c7b-8e9f0a1b2c3d</id>
</users>
```

**CORRECTO:**

```xml
<!-- Patrón consistente: 550e8400-...-000X para users -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
</users>
<users>
  <id>550e8400-e29b-41d4-a716-446655440002</id>
</users>
```

### 11.3. ❌ Dependencias Circulares

**INCORRECTO:**

```xml
<!-- user_id referencia un user que aún no existe -->
<module_users>
  <id>...</id>
  <granted_by_user_id>550e8400-e29b-41d4-a716-446655440999</granted_by_user_id>
</module_users>

<!-- User definido DESPUÉS -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440999</id>
</users>
```

**CORRECTO:**

```xml
<!-- User PRIMERO -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440999</id>
  <email>granter@example.com</email>
  <name>Granter User</name>
</users>

<!-- ModuleUser DESPUÉS -->
<module_users>
  <id>...</id>
  <granted_by_user_id>550e8400-e29b-41d4-a716-446655440999</granted_by_user_id>
</module_users>
```

### 11.4. ❌ Datos Sin Sentido

**INCORRECTO:**

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>asdf@asdf.com</email>
  <name>asdf asdf</name>
  <phone>1234567890</phone>
</users>
```

**CORRECTO:**

```xml
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
  <email>test.user1@example.com</email>
  <name>Test User One</name>
  <phone>+52-555-1234-5678</phone>
</users>
```

### 11.5. ❌ Escenarios Acoplados

**INCORRECTO:**

```csharp
// Test asume que CreateUsers.xml tiene exactamente 3 usuarios
// con emails específicos
[Test]
public async Task Test()
{
    LoadScenario("CreateUsers");
    // Asume que usuario1@example.com existe
    var user = await repo.GetByEmailAsync("usuario1@example.com");
}
```

**CORRECTO:**

```csharp
// Test obtiene datos dinámicamente
[Test]
public async Task Test()
{
    LoadScenario("CreateUsers");
    var dataSet = nDbUnitTest.GetDataSetFromDb();
    var userRow = dataSet.GetFirstUserRow();
    var email = userRow.Field<string>("email");

    var user = await repo.GetByEmailAsync(email);
}
```

### 11.6. ❌ Fechas Hardcodeadas Sin Contexto

**INCORRECTO:**

```xml
<!-- ¿Por qué esta fecha? -->
<creation_date>2019-03-27T14:23:45</creation_date>
```

**CORRECTO:**

```xml
<!-- Fecha base del escenario: 2024-01-15 -->
<creation_date>2024-01-15T10:00:00</creation_date>
```

### 11.7. ❌ No Respetar Orden de Dependencias

**INCORRECTO:**

```xml
<!-- ❌ users_in_roles ANTES que users y roles -->
<users_in_roles>
  <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
  <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
</users_in_roles>

<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
</users>

<roles>
  <id>660e8400-e29b-41d4-a716-446655440001</id>
</roles>
```

**CORRECTO:**

```xml
<!-- ✅ users y roles PRIMERO -->
<users>
  <id>550e8400-e29b-41d4-a716-446655440001</id>
</users>

<roles>
  <id>660e8400-e29b-41d4-a716-446655440001</id>
</roles>

<!-- users_in_roles DESPUÉS -->
<users_in_roles>
  <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
  <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
</users_in_roles>
```

### 11.8. ❌ Editar XMLs Manualmente en Proyectos con Clases Generadoras

**⚠️ ANTI-PATRÓN CRÍTICO**

En proyectos que usan clases generadoras (`Sc###Create*.cs`), los archivos XML son **archivos generados** y NO deben editarse manualmente.

**INCORRECTO:**

```xml
<!-- ❌ Editar CreateUsers.xml directamente -->
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <!-- Agregando usuario manualmente en el XML -->
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>nuevousuario@example.com</email>  <!-- ❌ Editado a mano -->
    <name>Usuario Nuevo</name>
  </users>
</AppSchema>
```

**CORRECTO:**

```csharp
// ✅ Modificar la clase generadora Sc030CreateUsers.cs
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
            // ✅ Agregar nuevo usuario en la clase generadora
            ("nuevousuario@example.com", "Usuario Nuevo")
        };

        try
        {
            this._uoW.BeginTransaction();
            foreach (var (email, name) in users)
                await this._uoW.Users.CreateAsync(email, name);
            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();
            throw;
        }
    }
}

// Luego ejecutar el generador para regenerar CreateUsers.xml
```

**Por qué es un problema:**

1. **Pérdida de cambios** - Si se regenera el XML, los cambios manuales se pierden
2. **Inconsistencia** - El XML no refleja la clase generadora
3. **No hay validación** - Los cambios manuales no pasan por validaciones de dominio
4. **Breaking refactoring** - Cambios en el modelo rompen el XML editado manualmente
5. **Debugging difícil** - No está claro qué datos vienen de clases vs edición manual

**Cómo identificar si un proyecto usa clases generadoras:**

```
tests/
├── {proyecto}.scenarios/           # ✅ Si esta carpeta existe → USA CLASES GENERADORAS
│   ├── Sc010CreateSandBox.cs
│   ├── Sc020CreateRoles.cs
│   └── Sc030CreateUsers.cs
│
└── {proyecto}.infrastructure.tests/
    └── scenarios/                  # XMLs son archivos GENERADOS
        ├── CreateRoles.xml         # ❌ NO EDITAR MANUALMENTE
        └── CreateUsers.xml         # ❌ NO EDITAR MANUALMENTE
```

**Regla de oro:**

> **Si existe una carpeta `*.scenarios/` con clases `Sc###Create*.cs`, entonces los XMLs son archivos generados y NUNCA deben editarse manualmente. Siempre modificar las clases generadoras y regenerar.**

**Flujo correcto cuando necesitas cambiar un escenario:**

```
1. Localizar la clase generadora (Sc###Create*.cs)
   ↓
2. Modificar el método SeedData()
   ↓
3. Ejecutar el generador de escenarios
   ↓
4. Verificar el XML generado
   ↓
5. Ejecutar tests para validar
```

---

## 12. Ejemplos Completos

### 12.1. Ejemplo Simple - CreateUsers.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!--
    Escenario: CreateUsers
    Propósito: 3 usuarios básicos para tests de búsqueda y filtrado
    Usado en: NHUserRepositoryTests
  -->

  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <name>Usuario Uno</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440002</id>
    <email>usuario2@example.com</email>
    <name>Usuario Dos</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <users>
    <id>550e8400-e29b-41d4-a716-446655440003</id>
    <email>usuario3@example.com</email>
    <name>Usuario Tres</name>
    <locked>true</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

</AppSchema>
```

### 12.2. Ejemplo Medio - CreateAdminUser.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!--
    Escenario: CreateAdminUser
    Propósito: Usuario admin completo con rol para tests de autorización
    Usado en: NHRoleRepositoryTests, NHUserRepositoryTests
  -->

  <!-- Rol de administrador -->
  <roles>
    <id>660e8400-e29b-41d4-a716-446655440001</id>
    <name>PlatformAdministrator</name>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </roles>

  <!-- Usuario administrador -->
  <users>
    <id>550e8400-e29b-41d4-a716-446655440001</id>
    <email>usuario1@example.com</email>
    <name>Usuario Admin</name>
    <locked>false</locked>
    <creation_date>2024-01-15T10:00:00</creation_date>
  </users>

  <!-- Relación usuario-rol -->
  <users_in_roles>
    <user_id>550e8400-e29b-41d4-a716-446655440001</user_id>
    <role_id>660e8400-e29b-41d4-a716-446655440001</role_id>
  </users_in_roles>

</AppSchema>
```

### 12.3. Ejemplo Complejo - 040_ModuleUsers.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppSchema xmlns="http://tempuri.org/AppSchema.xsd">
  <!--
    Escenario: 040_ModuleUsers
    Propósito: Estructura completa Organization → Module → ActivedModule → ModuleUsers
    Usado en: NHModuleUserRepositoryTests
    Dependencias: Requiere organizations (010) y modules (020) previos
  -->

  <!-- Nivel 1: Organizations (podría estar en 010_Organizations.xml) -->
  <organizations>
    <id>afdd687c-5cb1-4171-aa6e-77c40469ed4a</id>
    <name>APSYS MX</name>
    <email>contact@apsysmx.com</email>
    <creation_date>2024-01-01T00:00:00</creation_date>
  </organizations>

  <!-- Nivel 2: Modules (podría estar en 020_Modules.xml) -->
  <modules>
    <id>2d0ae5b0-8c90-4c8a-917c-5e6d3a9c38f5</id>
    <name>Inspection Module</name>
    <description>Vehicle inspection management</description>
    <creation_date>2024-01-01T00:00:00</creation_date>
  </modules>

  <!-- Nivel 3: ActivedModules -->
  <actived_modules>
    <id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</id>
    <organization_id>afdd687c-5cb1-4171-aa6e-77c40469ed4a</organization_id>
    <module_id>2d0ae5b0-8c90-4c8a-917c-5e6d3a9c38f5</module_id>
    <!-- ModuleStatus.Active = 1 -->
    <status>1</status>
    <activation_date>2024-01-15T00:00:00</activation_date>
    <creation_date>2024-01-15T00:00:00</creation_date>
  </actived_modules>

  <!-- Nivel 4: ModuleUsers -->

  <!-- Carlos Almanza granted 2 users -->
  <module_users>
    <id>7f8a9b0c-1d2e-3f4a-5b6c-7d8e9f0a1b2c</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-20T00:00:00</access_granted_date>
    <granted_by_user_id>f6ae8ae5-bdc9-4010-85ca-e7e7e7bfa34c</granted_by_user_id>
    <!-- UserStatus.Active = 1 -->
    <status>1</status>
    <creation_date>2024-01-20T00:00:00</creation_date>
  </module_users>

  <module_users>
    <id>8g9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-20T00:00:00</access_granted_date>
    <granted_by_user_id>f6ae8ae5-bdc9-4010-85ca-e7e7e7bfa34c</granted_by_user_id>
    <status>1</status>
    <creation_date>2024-01-20T00:00:00</creation_date>
  </module_users>

  <!-- Erika Moreno granted 1 user -->
  <module_users>
    <id>9h0c1d2e-3f4a-5b6c-7d8e-9f0a1b2c3d4e</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-21T00:00:00</access_granted_date>
    <granted_by_user_id>e4551264-284c-422d-9d0d-4871cf10786e</granted_by_user_id>
    <status>1</status>
    <creation_date>2024-01-21T00:00:00</creation_date>
  </module_users>

  <!-- 1 inactive user -->
  <module_users>
    <id>0i1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-22T00:00:00</access_granted_date>
    <granted_by_user_id>e4551264-284c-422d-9d0d-4871cf10786e</granted_by_user_id>
    <!-- UserStatus.Inactive = 0 -->
    <status>0</status>
    <creation_date>2024-01-22T00:00:00</creation_date>
  </module_users>

  <!-- Self-granted (null granted_by_user_id) -->
  <module_users>
    <id>1j2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a</id>
    <active_module_id>1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d</active_module_id>
    <access_granted_date>2024-01-23T00:00:00</access_granted_date>
    <!-- granted_by_user_id es null - campo omitido -->
    <status>1</status>
    <creation_date>2024-01-23T00:00:00</creation_date>
  </module_users>

</AppSchema>
```

**Datos en el escenario:**
- 1 Organization
- 1 Module
- 1 ActivedModule
- 5 ModuleUsers:
  - 2 granted by Carlos Almanza (Active)
  - 2 granted by Erika Moreno (1 Active, 1 Inactive)
  - 1 self-granted (null granter, Active)

**Tests que lo usan:**
- `GetByGrantedByUserIdAsync_WithExistingGranter_ShouldReturnModuleUsers` - Verifica 2 users granted by Carlos
- `GetByStatusAsync_WithActiveStatus_ShouldReturnActiveUsers` - Verifica 4 active users
- `CountAsync_ShouldReturnCorrectNumber` - Verifica 5 total users

---

## 13. Checklist

### Antes de Crear el Escenario

- [ ] Identificar el propósito específico del escenario
- [ ] Revisar si ya existe un escenario similar reutilizable
- [ ] Listar las tablas necesarias
- [ ] Identificar las dependencias (Foreign Keys)
- [ ] Determinar el orden de inserción
- [ ] Elegir convención de nombre apropiada

### Durante la Creación

- [ ] Usar declaración XML correcta
- [ ] Usar elemento raíz `<AppSchema>`
- [ ] Nombres de tablas coinciden con BD (snake_case)
- [ ] Nombres de campos coinciden con columnas
- [ ] GUIDs siguen patrón consistente
- [ ] Fechas en formato ISO 8601
- [ ] Booleanos como `true`/`false`
- [ ] Enums como valores numéricos
- [ ] Orden de inserción respeta dependencias
- [ ] Comentarios documentan propósito y contenido
- [ ] Campos nullable manejados correctamente
- [ ] Datos mínimos necesarios (no sobrepoblar)

### Después de Crear

- [ ] Validar sintaxis XML
- [ ] Probar cargando el escenario en un test
- [ ] Verificar que los datos se insertaron correctamente
- [ ] Documentar en qué tests se usa
- [ ] Actualizar referencias si reemplaza otro escenario

### Mantenimiento

- [ ] Actualizar cuando el esquema de BD cambia
- [ ] Revisar si sigue siendo necesario
- [ ] Consolidar con otros escenarios similares si aplica
- [ ] Mantener documentación actualizada

---

## Referencias

- [Repository Testing Practices](./repository-testing-practices.md) - Cómo usar escenarios en tests
- [Integration Tests](./integration-tests.md) - Infraestructura de testing
- [NDbUnit Documentation](https://github.com/NDbUnit/NDbUnit) - Herramienta que procesa los escenarios
- [XML Schema (XSD)](https://www.w3.org/XML/Schema) - Especificación de XML Schema

---

## Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2025-01-20 | Versión inicial de la guía |

---

**Última actualización:** 2025-01-20
**Mantenedor:** Equipo APSYS
**Versión:** 1.0.0
