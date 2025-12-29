# Constructor Patterns - Patrones de Constructores

**Categoría:** Patrón Estructural
**Aplica a:** Todas las entidades de dominio

## Descripción

Todas las entidades en APSYS deben tener **exactamente dos constructores**: uno vacío para NHibernate y uno con parámetros para creación de instancias en código de aplicación.

---

## Regla Fundamental: Dos Constructores

### Patrón Obligatorio

```csharp
public class User : AbstractDomainObject
{
    // 1️⃣ Constructor vacío - Para NHibernate
    public User()
    {
    }

    // 2️⃣ Constructor con parámetros - Para creación
    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
}
```

---

## Constructor Vacío (Para NHibernate)

### Definición

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="User"/> class.
/// This constructor is used by NHibernate for mapping purposes.
/// </summary>
public User()
{
}
```

### Características

- ✅ **Público** (required por NHibernate)
- ✅ Sin parámetros
- ✅ Generalmente vacío
- ✅ Documentado con XML comments
- ✅ Menciona que es para NHibernate

### Propósito

NHibernate usa este constructor para:
1. Crear instancias al cargar entidades desde la base de datos
2. Crear proxies para lazy loading
3. Hidratación de objetos con reflection

### Cuándo se usa

```csharp
// NHibernate internamente hace:
var user = new User();  // Constructor vacío
user.Id = loadedId;
user.Email = loadedEmail;
user.Name = loadedName;
// ... etc
```

---

## Constructor con Parámetros (Para Creación)

### Definición Básica

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="User"/> class with the specified email and name.
/// </summary>
/// <param name="email">The user's email address</param>
/// <param name="name">The user's full name</param>
public User(string email, string name)
{
    Email = email;
    Name = name;
    Locked = false;  // Valores por defecto
}
```

### Características

- ✅ **Público**
- ✅ Acepta parámetros para propiedades requeridas
- ✅ Asigna valores a propiedades
- ✅ Puede asignar valores por defecto
- ✅ Documentado con XML comments completos

### Propósito

Usado en código de aplicación para crear nuevas entidades:

```csharp
// En Application Layer o Domain Services
var user = new User("john@example.com", "John Doe");

// Validar
if (!user.IsValid())
{
    throw new InvalidDomainException(user.Validate());
}

// Persistir
await _userRepository.SaveAsync(user);
```

---

## Patrones por Complejidad

### Simple: Constructor con 1 Parámetro

```csharp
public class Role : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public Role()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class with the specified name.
    /// </summary>
    /// <param name="name">The role name</param>
    public Role(string name)
    {
        Name = name;
    }

    public virtual string Name { get; set; } = string.Empty;

    public override IValidator GetValidator()
        => new RoleValidator();
}
```

### Media: Constructor con 2-3 Parámetros

```csharp
public class User : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public User()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's full name</param>
    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Locked = false;
    }

    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public override IValidator GetValidator()
        => new UserValidator();
}
```

### Compleja: Constructor con 5+ Parámetros

```csharp
public class TechnicalStandard : AbstractDomainObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TechnicalStandard"/> class.
    /// </summary>
    public TechnicalStandard()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TechnicalStandard"/> class.
    /// </summary>
    /// <param name="code">The unique code</param>
    /// <param name="name">The name</param>
    /// <param name="edition">The edition</param>
    /// <param name="status">The status</param>
    /// <param name="type">The type</param>
    public TechnicalStandard(string code, string name, string edition, string status, string type)
    {
        this.CreationDate = DateTime.UtcNow;  // Re-asignar CreationDate
        Code = code;
        Name = name;
        Edition = edition;
        Status = status;
        Type = type;
    }

    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual string Edition { get; set; } = string.Empty;
    public virtual string Status { get; set; } = string.Empty;
    public virtual string Type { get; set; } = string.Empty;

    public override IValidator GetValidator()
        => new TechnicalStandardValidator();
}
```

---

## Re-asignación de CreationDate

### Cuándo Re-asignar

```csharp
public TechnicalStandard(string code, string name, string edition, string status, string type)
{
    this.CreationDate = DateTime.UtcNow;  // ✅ Re-asignar explícitamente
    Code = code;
    Name = name;
    Edition = edition;
    Status = status;
    Type = type;
}
```

### Cuándo NO Re-asignar

```csharp
public User(string email, string name)
{
    // ❌ NO re-asignar CreationDate - ya está asignado por AbstractDomainObject
    Email = email;
    Name = name;
}
```

### Regla General

- ✅ **Re-asignar** si el constructor es largo o complejo
- ✅ **Re-asignar** si quieres garantizar timestamp exacto de construcción
- ❌ **NO re-asignar** en constructores simples (redundante)

---

## Valores por Defecto en Constructores

### Asignar Valores por Defecto

```csharp
public User(string email, string name)
{
    Email = email;
    Name = name;
    Locked = false;          // ✅ Valor por defecto explícito
    IsActive = true;         // ✅ Override del default false
}
```

### NO Inicializar Colecciones

```csharp
public User(string email, string name)
{
    Email = email;
    Name = name;
    // ❌ NO hacer esto:
    // Roles = new List<Role>();
}

// ✅ Inicialización está en la propiedad:
public virtual IList<Role> Roles { get; set; } = new List<Role>();
```

**Razón:** Las colecciones ya están inicializadas en la declaración de la propiedad.

---

## Parámetros del Constructor

### Qué Incluir

✅ **SÍ incluir en constructor:**
- Propiedades requeridas del negocio
- Propiedades que definen la identidad/propósito de la entidad
- Valores que deben ser establecidos al momento de creación

```csharp
// ✅ Correcto
public User(string email, string name)  // Email y Name son esenciales
{
    Email = email;
    Name = name;
}
```

### Qué NO Incluir

❌ **NO incluir en constructor:**
- `Id` (generado automáticamente)
- `CreationDate` (asignado por AbstractDomainObject)
- Colecciones (inicializadas en propiedades)
- Propiedades opcionales
- Propiedades calculadas

```csharp
// ❌ Incorrecto
public User(Guid id, DateTime creationDate, string email, string name, IList<Role> roles)
{
    Id = id;              // ❌ Se genera automáticamente
    CreationDate = creationDate;  // ❌ Se asigna automáticamente
    Email = email;
    Name = name;
    Roles = roles;        // ❌ Se inicializa en propiedad
}
```

---

## Constructores con DateTime

### Patrón Correcto

```csharp
public class Prototype : AbstractDomainObject
{
    public Prototype()
    {
    }

    public Prototype(string number, DateTime issueDate, DateTime expirationDate, string status)
    {
        Number = number;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        Status = status;
    }

    public virtual string Number { get; set; } = string.Empty;
    public virtual DateTime IssueDate { get; set; }
    public virtual DateTime ExpirationDate { get; set; }
    public virtual string Status { get; set; } = string.Empty;
}
```

### Uso

```csharp
// Crear prototype con fechas específicas
var prototype = new Prototype(
    "PROTO-001",
    DateTime.Today,
    DateTime.Today.AddMonths(6),
    "Active"
);
```

---

## Testing de Constructores

### Test Constructor Vacío

```csharp
[Test]
public void Constructor_Empty_ShouldCreateWithDefaults()
{
    // Arrange & Act
    var role = new Role();

    // Assert
    role.Id.Should().NotBeEmpty();
    role.Name.Should().BeEmpty();
    role.CreationDate.Should().NotBe(default);
}
```

### Test Constructor con Parámetros

```csharp
[Test]
public void Constructor_WithName_ShouldSetName()
{
    // Arrange
    var name = "Administrator";

    // Act
    var role = new Role(name);

    // Assert
    role.Name.Should().Be(name);
    role.Id.Should().NotBeEmpty();
}
```

### Test Constructor Complejo

```csharp
[Test]
public void Constructor_WithParameters_ShouldSetAllProperties()
{
    // Arrange
    var code = "CFE-001";
    var name = "Standard Name";
    var edition = "2025";
    var status = "Active";
    var type = "CFE";

    // Act
    var standard = new TechnicalStandard(code, name, edition, status, type);

    // Assert
    standard.Code.Should().Be(code);
    standard.Name.Should().Be(name);
    standard.Edition.Should().Be(edition);
    standard.Status.Should().Be(status);
    standard.Type.Should().Be(type);
    standard.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

---

## Orden de Elementos en la Clase

### Estructura Recomendada

```csharp
public class User : AbstractDomainObject
{
    // 1️⃣ Propiedades
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    // 2️⃣ Constructor vacío
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// This constructor is used by NHibernate for mapping purposes.
    /// </summary>
    public User()
    {
    }

    // 3️⃣ Constructor con parámetros
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <param name="name">The user's full name</param>
    public User(string email, string name)
    {
        Email = email;
        Name = name;
        Locked = false;
    }

    // 4️⃣ Métodos de dominio (si hay)
    public void Lock()
    {
        Locked = true;
    }

    // 5️⃣ GetValidator (al final)
    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **Dos constructores obligatorios** - Vacío y con parámetros
- **Constructor vacío público** - Requerido por NHibernate
- **Parámetros mínimos necesarios** - Solo propiedades esenciales
- **Valores por defecto** - Asignados en constructor parametrizado
- **Re-asignación de CreationDate** - Opcional, solo en casos específicos
- **XML documentation** - En todos los constructores

### 📚 Patrones Importantes

**Constructor vacío:**
```csharp
/// <summary>
/// This constructor is used by NHibernate for mapping purposes.
/// </summary>
public User()
{
}
```

**Constructor con parámetros:**
```csharp
/// <summary>
/// Initializes a new instance with the specified values.
/// </summary>
public User(string email, string name)
{
    Email = email;
    Name = name;
}
```

**Re-asignar CreationDate (solo si necesario):**
```csharp
public TechnicalStandard(string code, string name, ...)
{
    this.CreationDate = DateTime.UtcNow;  // Opcional
    Code = code;
    Name = name;
}
```

---

## Errores Comunes

### ❌ Solo un constructor

```csharp
// ❌ INCORRECTO - Falta constructor vacío
public class User : AbstractDomainObject
{
    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }
}
```

### ❌ Inicializar colecciones en constructor

```csharp
// ❌ INCORRECTO
public User(string email, string name)
{
    Email = email;
    Name = name;
    Roles = new List<Role>();  // ❌ Ya está inicializado en propiedad
}
```

### ❌ Incluir Id o CreationDate como parámetros

```csharp
// ❌ INCORRECTO
public User(Guid id, DateTime creationDate, string email, string name)
{
    Id = id;  // ❌ Se genera automáticamente
    CreationDate = creationDate;  // ❌ Se asigna automáticamente
    Email = email;
    Name = name;
}
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Entity Testing Practices](../../entities-testing-practices.md)

**Patrones Relacionados:**
- [Base Class](01-base-class.md)
- [Properties](02-properties.md)
- [Validation](04-validation.md)

**Ejemplos Prácticos:**
- [Role - Simple](../simple/Role.md) - 1 parámetro
- [User - Medium](../medium/User.md) - 2 parámetros
- [TechnicalStandard - Complex](../complex/TechnicalStandard.md) - 5 parámetros

---

**Última actualización:** 2025-01-20
