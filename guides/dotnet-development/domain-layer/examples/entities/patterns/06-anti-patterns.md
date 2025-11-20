# Anti-Patterns - Qué NO Hacer

**Categoría:** Patrones DON'T (No Hacer)
**Aplica a:** Todas las entidades de dominio

## Descripción

Guía de los anti-patrones que **NO DEBES SEGUIR** al crear entidades de dominio en APSYS. Estos patrones violan los principios de Domain-Driven Design y crean problemas de mantenibilidad, acoplamiento, y separación de concerns.

---

## ❌ DON'T #1: No Agregar Lógica de Persistencia

### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    // ❌ NO! Lógica de persistencia en la entidad
    public void SaveToDatabase()
    {
        using (var connection = new SqlConnection("connection string"))
        {
            connection.Open();
            var command = new SqlCommand("INSERT INTO Users...", connection);
            command.ExecuteNonQuery();
        }
    }

    // ❌ NO! Lógica de repository en la entidad
    public async Task UpdateAsync()
    {
        var repository = new UserRepository();
        await repository.UpdateAsync(this);
    }

    // ❌ NO! Acceso directo a base de datos
    public static User LoadFromDatabase(Guid id)
    {
        // ... código de base de datos
        return new User();
    }
}
```

### ✅ Correcto

```csharp
// ✅ Entidad SIN lógica de persistencia
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Persistencia va en Infrastructure Layer (Repository)
public class UserRepository : IUserRepository
{
    public async Task SaveAsync(User user)
    {
        // Lógica de persistencia AQUÍ
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        // Lógica de carga AQUÍ
    }
}
```

### Por qué es un problema

- ❌ Viola **Single Responsibility Principle**
- ❌ Acopla dominio con infraestructura
- ❌ Dificulta testing
- ❌ Imposible cambiar tecnología de persistencia
- ❌ Viola arquitectura de capas

---

## ❌ DON'T #2: No Usar Atributos de ORM

### ❌ Incorrecto

```csharp
// ❌ NO! Atributos de NHibernate/Entity Framework
[Table("users")]
public class User : AbstractDomainObject
{
    [Column("user_email")]
    [MaxLength(200)]
    public virtual string Email { get; set; } = string.Empty;

    [Column("user_name")]
    [Required]
    public virtual string Name { get; set; } = string.Empty;

    [ForeignKey("RoleId")]
    public virtual IList<Role> Roles { get; set; } = new List<Role>();
}
```

### ✅ Correcto

```csharp
// ✅ Entidad limpia SIN atributos de ORM
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Mapeo va en Infrastructure Layer (Mapper)
public class UserMapper : ClassMap<User>
{
    public UserMapper()
    {
        Table("users");
        Id(x => x.Id).Column("user_id");
        Map(x => x.Email).Column("user_email").Length(200);
        Map(x => x.Name).Column("user_name").Not.Nullable();
        HasMany(x => x.Roles).KeyColumn("user_id");
    }
}
```

### Por qué es un problema

- ❌ Acopla dominio con framework de persistencia
- ❌ Dificulta cambiar de ORM
- ❌ Contamina el modelo de dominio con detalles de infraestructura
- ❌ Viola Clean Architecture
- ❌ Validación debe ir en Validators, no en atributos

---

## ❌ DON'T #3: No Depender de Frameworks Externos

### ❌ Incorrecto

```csharp
// ❌ NO! Atributos de serialización
public class User : AbstractDomainObject
{
    [JsonProperty("email")]
    [JsonRequired]
    public virtual string Email { get; set; } = string.Empty;

    [JsonProperty("full_name")]
    public virtual string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual bool Locked { get; set; }
}

// ❌ NO! Dependencias de frameworks web
public class Order : AbstractDomainObject
{
    [FromQuery]
    public virtual string OrderNumber { get; set; } = string.Empty;

    [FromBody]
    public virtual decimal Amount { get; set; }
}

// ❌ NO! Dependencias de AutoMapper
public class Product : AbstractDomainObject
{
    [Ignore]
    public virtual string InternalCode { get; set; } = string.Empty;
}
```

### ✅ Correcto

```csharp
// ✅ Entidad limpia SIN dependencias externas
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Serialización va en WebApi Layer (DTOs)
public class UserDto
{
    [JsonProperty("email")]
    [JsonRequired]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("full_name")]
    public string Name { get; set; } = string.Empty;

    // Locked no se expone en DTO (JsonIgnore no necesario)
}

// ✅ Mapping va en Application Layer
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
    }
}
```

### Por qué es un problema

- ❌ Acopla dominio con frameworks de presentación
- ❌ Dificulta cambiar de framework (JSON.NET a System.Text.Json)
- ❌ El dominio debe ser **framework-agnostic**
- ❌ Contamina el modelo de dominio
- ❌ Responsabilidades mezcladas

---

## ❌ DON'T #4: No Exponer Propiedades Internas como Públicas

### ❌ Incorrecto

```csharp
public class Order : AbstractDomainObject
{
    // ❌ NO! Lista privada expuesta con nombre de campo
    public virtual List<OrderItem> _items { get; set; } = new List<OrderItem>();

    // ❌ NO! Backing field expuesto
    public virtual decimal _totalAmount;
    public virtual decimal TotalAmount
    {
        get => _totalAmount;
        set => _totalAmount = value;
    }

    // ❌ NO! Estado interno expuesto
    public virtual bool _isDirty { get; set; }
    public virtual int _version { get; set; }
}
```

### ✅ Correcto

```csharp
public class Order : AbstractDomainObject
{
    // ✅ Propiedad pública con nombre correcto
    public virtual IList<OrderItem> Items { get; set; } = new List<OrderItem>();

    // ✅ Propiedad calculada (si es necesario)
    public virtual decimal TotalAmount => Items.Sum(item => item.Amount);

    // ✅ Sin estado interno innecesario
    // NHibernate maneja versioning y change tracking

    public Order() { }

    public Order(IList<OrderItem> items)
    {
        Items = items;
    }

    public override IValidator GetValidator()
        => new OrderValidator();
}
```

### Por qué es un problema

- ❌ Expone detalles de implementación
- ❌ Rompe encapsulación
- ❌ Dificulta refactoring
- ❌ Confunde a otros desarrolladores
- ❌ NHibernate maneja versioning automáticamente

---

## Más Anti-Patterns Comunes

### ❌ DON'T #5: No Mezclar Responsabilidades

#### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;

    // ❌ NO! Lógica de validación de presentación
    public bool IsEmailValidForDisplay()
    {
        return Email.Contains("@") && Email.Length > 5;
    }

    // ❌ NO! Lógica de formato de UI
    public string GetFormattedName()
    {
        return $"<b>{Name}</b>";  // HTML en dominio!
    }

    // ❌ NO! Lógica de autorización
    public bool CanUserAccessResource(string resourceId)
    {
        // Lógica de permisos...
        return true;
    }

    // ❌ NO! Lógica de logging
    public void LogUserActivity(string activity)
    {
        Logger.Log($"User {Id} performed {activity}");
    }
}
```

#### ✅ Correcto

```csharp
// ✅ Entidad con solo lógica de dominio
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    // ✅ Método de dominio apropiado
    public void Lock()
    {
        Locked = true;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Formato va en Presentation Layer
public static class UserFormatter
{
    public static string FormatName(User user)
    {
        return $"<b>{user.Name}</b>";
    }
}

// ✅ Autorización va en Application/Security Layer
public class UserAuthorizationService
{
    public bool CanUserAccessResource(User user, string resourceId)
    {
        // Lógica de permisos...
        return true;
    }
}

// ✅ Logging va en Infrastructure Layer
public class UserActivityLogger
{
    public void LogActivity(User user, string activity)
    {
        _logger.Log($"User {user.Id} performed {activity}");
    }
}
```

---

### ❌ DON'T #6: No Usar Métodos Estáticos para Creación

#### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    // ❌ NO! Factory methods estáticos
    public static User CreateAdmin(string email, string name)
    {
        var user = new User(email, name);
        user.Roles.Add(new Role("Admin"));
        return user;
    }

    public static User CreateRegularUser(string email, string name)
    {
        var user = new User(email, name);
        user.Roles.Add(new Role("User"));
        return user;
    }
}
```

#### ✅ Correcto

```csharp
// ✅ Entidad simple
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Factory va en Domain Services o Application Layer
public class UserFactory
{
    public User CreateAdmin(string email, string name)
    {
        var user = new User(email, name);
        user.Roles.Add(new Role("Admin"));
        return user;
    }

    public User CreateRegularUser(string email, string name)
    {
        var user = new User(email, name);
        user.Roles.Add(new Role("User"));
        return user;
    }
}
```

---

### ❌ DON'T #7: No Sobre-Validar en Setters

#### ❌ Incorrecto

```csharp
public class User : AbstractDomainObject
{
    private string _email = string.Empty;

    // ❌ NO! Validación en setter
    public virtual string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Email cannot be empty");

            if (!value.Contains("@"))
                throw new ArgumentException("Email must contain @");

            _email = value;
        }
    }
}
```

#### ✅ Correcto

```csharp
// ✅ Propiedad simple
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;

    public User() { }

    public User(string email, string name)
    {
        Email = email;
        Name = name;
    }

    public override IValidator GetValidator()
        => new UserValidator();
}

// ✅ Validación en Validator
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotNull()
            .NotEmpty()
            .WithMessage("Email is required");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email must be a valid email address");
    }
}
```

**Por qué:**
- ✅ Validación centralizada en Validators
- ✅ NHibernate puede asignar sin lanzar excepciones
- ✅ Más fácil de testear
- ✅ Mensajes de error consistentes

---

## Resumen de Anti-Patterns

### ❌ Nunca Hagas Esto

| Anti-Pattern | Por Qué Es Malo | Dónde Va |
|-------------|-----------------|----------|
| Lógica de persistencia | Viola SRP, acopla dominio con infraestructura | Infrastructure Layer (Repositories) |
| Atributos de ORM | Acopla con framework, contamina dominio | Infrastructure Layer (Mappers) |
| Atributos de serialización | Acopla con framework de presentación | WebApi Layer (DTOs) |
| Propiedades privadas públicas | Rompe encapsulación | Refactor a propiedades limpias |
| Lógica de presentación | Mezcla responsabilidades | Presentation Layer |
| Lógica de autorización | No es responsabilidad del dominio | Application/Security Layer |
| Factory methods estáticos | Dificulta testing y extensión | Domain Services o Factories |
| Validación en setters | Rompe NHibernate, dificulta testing | Validators |

---

## Separación de Concerns

### Dónde Va Cada Cosa

```
┌─────────────────────────────────────────────────────────┐
│ Domain Layer (Entidades)                                 │
│ - Propiedades de negocio                                │
│ - Reglas de dominio (métodos de negocio)               │
│ - GetValidator() override                               │
│ - NADA MÁS                                              │
└─────────────────────────────────────────────────────────┘
                          ▲
                          │
                          │ Usa
                          │
┌─────────────────────────────────────────────────────────┐
│ Domain Layer (Validators)                                │
│ - Validaciones con FluentValidation                     │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Infrastructure Layer (Repositories)                      │
│ - Persistencia                                          │
│ - Queries a base de datos                               │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Infrastructure Layer (Mappers)                           │
│ - Mapeo ORM (NHibernate ClassMap)                      │
│ - Configuración de tabla, columnas, relaciones          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Application Layer (DTOs, AutoMapper)                     │
│ - Transferencia de datos                                │
│ - Mapping entre Entities y DTOs                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ WebApi Layer (Controllers, DTOs)                         │
│ - Serialización JSON                                    │
│ - Atributos de validación de API                        │
│ - Formato de respuesta                                  │
└─────────────────────────────────────────────────────────┘
```

---

## Testing de Anti-Patterns

### Detectar Atributos de ORM

```csharp
[Test]
public void User_ShouldNotHaveORMAttributes()
{
    // Arrange
    var type = typeof(User);

    // Act
    var hasTableAttribute = type.GetCustomAttributes(typeof(TableAttribute), false).Any();
    var properties = type.GetProperties();
    var hasColumnAttribute = properties.Any(p =>
        p.GetCustomAttributes(typeof(ColumnAttribute), false).Any());

    // Assert
    hasTableAttribute.Should().BeFalse("Entity should not have Table attribute");
    hasColumnAttribute.Should().BeFalse("Entity should not have Column attributes");
}
```

### Detectar Métodos de Persistencia

```csharp
[Test]
public void User_ShouldNotHavePersistenceMethods()
{
    // Arrange
    var type = typeof(User);

    // Act
    var hasSaveMethod = type.GetMethods().Any(m => m.Name.Contains("Save"));
    var hasUpdateMethod = type.GetMethods().Any(m => m.Name.Contains("Update"));
    var hasDeleteMethod = type.GetMethods().Any(m => m.Name.Contains("Delete"));

    // Assert
    hasSaveMethod.Should().BeFalse("Entity should not have Save methods");
    hasUpdateMethod.Should().BeFalse("Entity should not have Update methods");
    hasDeleteMethod.Should().BeFalse("Entity should not have Delete methods");
}
```

---

## Lecciones Clave

### ❌ Los 7 Anti-Patterns Críticos

1. **NO persistencia en entidades** - Va en Repositories
2. **NO atributos de ORM** - Va en Mappers
3. **NO dependencias externas** - Dominio debe ser limpio
4. **NO propiedades internas públicas** - Mantener encapsulación
5. **NO mezclar responsabilidades** - Cada capa tiene su rol
6. **NO factory methods estáticos** - Usar Domain Services
7. **NO validación en setters** - Usar Validators

### 📚 Principios Violados

- ❌ **Single Responsibility Principle** - Cuando mezclas persistencia, validación, presentación
- ❌ **Dependency Inversion** - Cuando dependes de frameworks concretos
- ❌ **Separation of Concerns** - Cuando mezclas capas
- ❌ **Clean Architecture** - Cuando el dominio conoce infraestructura

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Validators](../../validators.md)
- [Repository Interfaces](../../repository-interfaces.md)

**Patrones Relacionados:**
- [Base Class](01-base-class.md)
- [Properties](02-properties.md)
- [Best Practices](05-best-practices.md) ✅

**Arquitectura:**
- Clean Architecture Principles
- Domain-Driven Design
- Separation of Concerns

---

**Última actualización:** 2025-01-20
