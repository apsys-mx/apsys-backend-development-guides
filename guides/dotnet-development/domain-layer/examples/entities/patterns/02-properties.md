# Property Types - Tipos de Propiedades

**Categoría:** Patrón de Propiedades
**Aplica a:** Todas las entidades de dominio

## Descripción

Guía completa de cómo definir propiedades en entidades de dominio. Todas las propiedades deben ser `virtual` para compatibilidad con NHibernate y deben tener valores por defecto apropiados.

---

## Regla Fundamental: Propiedades Virtual

### ¿Por qué virtual?

NHibernate requiere que todas las propiedades sean `virtual` para poder crear **proxies dinámicos** para:
- Lazy loading
- Change tracking
- Interceptación de propiedades

### Patrón Obligatorio

```csharp
✅ Correcto:
public virtual string Name { get; set; } = string.Empty;
public virtual DateTime IssueDate { get; set; }
public virtual IList<Role> Roles { get; set; } = new List<Role>();

❌ Incorrecto:
public string Name { get; set; }  // Falta virtual
```

---

## Propiedades Simples

### String Properties

```csharp
// String no-nullable con valor por defecto
public virtual string Email { get; set; } = string.Empty;
public virtual string Name { get; set; } = string.Empty;
public virtual string Description { get; set; } = string.Empty;

// String nullable (opcional)
public virtual string? MiddleName { get; set; }
public virtual string? OptionalNotes { get; set; }
```

**Reglas:**
- ✅ Usar `= string.Empty` para strings requeridos
- ✅ Usar `string?` para strings opcionales
- ❌ NO dejar strings sin inicializar (pueden ser null)

### Numeric Properties

```csharp
// Enteros
public virtual int Age { get; set; }
public virtual int Count { get; set; }
public virtual long FileSize { get; set; }

// Decimales
public virtual decimal Price { get; set; }
public virtual decimal Amount { get; set; }
public virtual double Percentage { get; set; }

// Nullable (opcional)
public virtual int? OptionalCount { get; set; }
public virtual decimal? OptionalPrice { get; set; }
```

**Reglas:**
- ✅ Value types tienen default automático (0, 0.0)
- ✅ Usar nullable (`int?`, `decimal?`) para valores opcionales
- ✅ `decimal` para dinero y valores precisos
- ✅ `double` para porcentajes o cálculos científicos

### Boolean Properties

```csharp
// Boolean con default false
public virtual bool Locked { get; set; }
public virtual bool IsActive { get; set; }
public virtual bool IsDeleted { get; set; }

// Boolean con default true (especificar en constructor)
public virtual bool IsEnabled { get; set; } // Default en constructor

// Nullable (raramente usado)
public virtual bool? OptionalFlag { get; set; }
```

**Reglas:**
- ✅ Default es `false` automáticamente
- ✅ Si necesitas `true` por defecto, asignar en constructor
- ✅ Usar nombres claros: `IsActive`, `HasPermission`, `CanEdit`

### DateTime Properties

```csharp
// DateTime no-nullable
public virtual DateTime IssueDate { get; set; }
public virtual DateTime ExpirationDate { get; set; }
public virtual DateTime LastModified { get; set; }

// DateTime nullable (opcional)
public virtual DateTime? CompletedDate { get; set; }
public virtual DateTime? DeletedDate { get; set; }
```

**Reglas:**
- ✅ DateTime no-nullable tiene `default(DateTime)` = `01/01/0001`
- ✅ Usar nullable para fechas opcionales
- ✅ Validar que no sean `default(DateTime)` si son requeridas
- ✅ **Siempre usar UTC** al asignar valores: `DateTime.UtcNow`

**Ejemplo de validación:**
```csharp
RuleFor(x => x.IssueDate)
    .NotEqual(default(DateTime))
    .WithMessage("Issue date is required");
```

### Guid Properties

```csharp
// Guid para referencias (Foreign Key)
public virtual Guid UserId { get; set; }
public virtual Guid CategoryId { get; set; }

// Guid nullable (opcional)
public virtual Guid? OptionalReferenceId { get; set; }
```

**Reglas:**
- ✅ Usar para foreign keys
- ✅ Default es `Guid.Empty` (00000000-0000-0000-0000-000000000000)
- ✅ Validar que no sea `Guid.Empty` si es requerido

---

## Colecciones (Relaciones)

### One-to-Many (IList<T>)

```csharp
// Colección de entidades relacionadas
public virtual IList<Role> Roles { get; set; } = new List<Role>();
public virtual IList<Order> Orders { get; set; } = new List<Order>();
public virtual IList<Comment> Comments { get; set; } = new List<Comment>();
```

**Reglas:**
- ✅ **SIEMPRE** usar `IList<T>` (no `List<T>`)
- ✅ **SIEMPRE** inicializar con `= new List<T>()`
- ✅ Esto evita `NullReferenceException`
- ❌ NO usar `List<T>` directamente
- ❌ NO dejar sin inicializar

**Por qué IList:**
- NHibernate puede reemplazar con su propia implementación
- Permite lazy loading
- Más flexible que `List<T>` concreto

### Many-to-One (Referencias a Entidades)

```csharp
// Referencia requerida
public virtual Category Category { get; set; } = null!;
public virtual User Owner { get; set; } = null!;

// Referencia opcional
public virtual User? AssignedTo { get; set; }
public virtual Department? Department { get; set; }
```

**Reglas:**
- ✅ Usar `= null!` para referencias requeridas (evita warning de nullable)
- ✅ Usar `Entity?` para referencias opcionales
- ✅ Validar que no sean null en el validator si son requeridas

**Ejemplo de validación:**
```csharp
RuleFor(x => x.Category)
    .NotNull()
    .WithMessage("Category is required");
```

### Colecciones de Primitivos

```csharp
// Colección de strings
public virtual IList<string> Tags { get; set; } = new List<string>();

// Colección de números
public virtual IList<int> Ratings { get; set; } = new List<int>();
```

**Reglas:**
- ✅ Mismo patrón que colecciones de entidades
- ✅ Usar `IList<T>` e inicializar
- ⚠️ Menos común que relaciones a entidades

---

## Enums

```csharp
// Enum como propiedad
public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public virtual OrderStatus Status { get; set; }
```

**Reglas:**
- ✅ Default es el primer valor del enum (Pending)
- ✅ Asignar valor específico en constructor si necesario
- ⚠️ **CUIDADO:** En APSYS preferimos usar **string con allowed values**

**Alternativa recomendada (String con validación):**
```csharp
// En lugar de enum, usar string
public virtual string Status { get; set; } = string.Empty;

// Validar allowed values en validator
RuleFor(x => x.Status)
    .Must(status => new[] { "Pending", "Processing", "Completed", "Cancelled" }.Contains(status))
    .WithMessage("Status must be one of: Pending, Processing, Completed, Cancelled");
```

**Ventajas del enfoque string:**
- Más flexible para cambios
- Mejor para serialización/deserialización
- Compatible con bases de datos legacy
- Más fácil de testear

---

## Propiedades Nullable

### Cuándo usar Nullable

```csharp
// ✅ Usar nullable cuando:
public virtual string? MiddleName { get; set; }        // Opcional por naturaleza
public virtual DateTime? CompletedDate { get; set; }   // Puede no existir aún
public virtual int? OptionalCount { get; set; }        // Puede no tener valor

// ❌ NO usar nullable para:
public virtual string? Name { get; set; }  // Name es requerido, usar string.Empty
public virtual DateTime? CreationDate { get; set; }  // CreationDate siempre existe
```

### Patrón con Nullable Reference Types (C# 8+)

```csharp
// Requerido (no-nullable)
public virtual string Email { get; set; } = string.Empty;

// Opcional (nullable)
public virtual string? MiddleName { get; set; }

// Requerido pero puede ser null temporalmente (null-forgiving operator)
public virtual Category Category { get; set; } = null!;
```

---

## Ejemplos Completos por Complejidad

### Simple: Una Propiedad

```csharp
public class Role : AbstractDomainObject
{
    public virtual string Name { get; set; } = string.Empty;
}
```

### Media: Múltiples Propiedades + Colección

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
}
```

### Compleja: Muchas Propiedades de Diferentes Tipos

```csharp
public class TechnicalStandard : AbstractDomainObject
{
    // Strings
    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual string Edition { get; set; } = string.Empty;
    public virtual string Status { get; set; } = string.Empty;
    public virtual string Type { get; set; } = string.Empty;

    // DateTime
    public virtual DateTime PublicationDate { get; set; }

    // Nullable
    public virtual string? Notes { get; set; }
    public virtual DateTime? DeprecationDate { get; set; }
}
```

---

## Inicialización de Propiedades

### Valores por Defecto en la Declaración

```csharp
✅ Correcto - Inicialización en declaración:
public virtual string Name { get; set; } = string.Empty;
public virtual IList<Role> Roles { get; set; } = new List<Role>();
public virtual bool IsActive { get; set; }  // Default false

❌ Incorrecto - Sin inicialización:
public virtual string Name { get; set; }  // Puede ser null
public virtual IList<Role> Roles { get; set; }  // Puede ser null
```

### Valores por Defecto en Constructor

```csharp
public class User : AbstractDomainObject
{
    public virtual bool Locked { get; set; }

    public User()
    {
        // No necesario, default es false
    }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Locked = false;  // Explícito, aunque redundante
    }
}
```

---

## Testing de Propiedades

### Test de Valores por Defecto

```csharp
[Test]
public void Constructor_Empty_ShouldInitializeCollections()
{
    // Arrange & Act
    var user = new User();

    // Assert
    user.Roles.Should().NotBeNull();
    user.Roles.Should().BeEmpty();
}

[Test]
public void Constructor_Empty_ShouldInitializeStrings()
{
    // Arrange & Act
    var role = new Role();

    // Assert
    role.Name.Should().NotBeNull();
    role.Name.Should().BeEmpty();
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Propiedades virtual** - Obligatorio para NHibernate
- **Inicialización** - Evitar null con valores por defecto
- **IList<T> vs List<T>** - Siempre IList para colecciones
- **String vs String?** - Nullable solo cuando es opcional
- **DateTime validation** - Verificar que no sea default
- **Allowed values** - Preferir string con validación sobre enum

### 📚 Patrones Importantes

**Strings requeridos:**
```csharp
public virtual string Name { get; set; } = string.Empty;
```

**Strings opcionales:**
```csharp
public virtual string? MiddleName { get; set; }
```

**Colecciones:**
```csharp
public virtual IList<Role> Roles { get; set; } = new List<Role>();
```

**Referencias requeridas:**
```csharp
public virtual Category Category { get; set; } = null!;
```

**Referencias opcionales:**
```csharp
public virtual User? AssignedTo { get; set; }
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Validators](../../validators.md)

**Patrones Relacionados:**
- [Base Class](01-base-class.md)
- [Constructors](03-constructors.md)
- [Best Practices](05-best-practices.md)

**Ejemplos Prácticos:**
- [Role - Simple](../simple/Role.md)
- [User - Medium](../medium/User.md)
- [TechnicalStandard - Complex](../complex/TechnicalStandard.md)

---

**Última actualización:** 2025-01-20
