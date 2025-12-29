# Validation Usage - Uso de Validación

**Categoría:** Patrón de Validación
**Aplica a:** Todas las entidades de dominio

## Descripción

Guía completa del uso de los métodos de validación heredados de `AbstractDomainObject`. Todas las entidades integran FluentValidation a través de los métodos `IsValid()`, `Validate()`, y `GetValidator()`.

---

## Tres Métodos de Validación

### Resumen

| Método | Retorno | Propósito |
|--------|---------|-----------|
| `IsValid()` | `bool` | Verificar si la entidad es válida |
| `Validate()` | `IEnumerable<ValidationFailure>` | Obtener lista de errores |
| `GetValidator()` | `IValidator?` | Retornar el validator (override) |

---

## IsValid() - Verificar Validez

### Definición

```csharp
// Heredado de AbstractDomainObject
public virtual bool IsValid()
{
    IValidator? validator = GetValidator();
    if (validator == null)
        return true;

    var context = new ValidationContext<object>(this);
    ValidationResult result = validator.Validate(context);
    return result.IsValid;
}
```

### Uso Básico

```csharp
var user = new User("test@example.com", "Test User");

// Verificar si es válido
if (!user.IsValid())
{
    // La entidad NO es válida
    Console.WriteLine("User is invalid");
}
else
{
    // La entidad es válida
    Console.WriteLine("User is valid");
}
```

### Uso en Application Layer

```csharp
public async Task<User> CreateUserAsync(string email, string name)
{
    // Crear entidad
    var user = new User(email, name);

    // Validar antes de persistir
    if (!user.IsValid())
    {
        throw new InvalidDomainException("User validation failed");
    }

    // Persistir
    await _userRepository.SaveAsync(user);
    return user;
}
```

### Uso en Domain Services

```csharp
public class UserService
{
    public void AssignRole(User user, Role role)
    {
        // Agregar rol
        user.Roles.Add(role);

        // Validar después de cambio
        if (!user.IsValid())
        {
            user.Roles.Remove(role);  // Rollback
            throw new InvalidOperationException("Cannot assign role - validation failed");
        }
    }
}
```

---

## Validate() - Obtener Errores Detallados

### Definición

```csharp
// Heredado de AbstractDomainObject
public virtual IEnumerable<ValidationFailure> Validate()
{
    IValidator? validator = GetValidator();
    if (validator == null)
        return new List<ValidationFailure>();
    else
    {
        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.Errors;
    }
}
```

### Uso Básico

```csharp
var user = new User("", "");  // Email y Name vacíos

// Obtener errores
var errors = user.Validate();

foreach (var error in errors)
{
    Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}

// Output:
// Email: Email is required
// Email: Email must be a valid email address
// Name: Name is required
```

### Uso con Logging

```csharp
public async Task<User> CreateUserAsync(string email, string name)
{
    var user = new User(email, name);

    if (!user.IsValid())
    {
        var errors = user.Validate();

        // Log cada error
        foreach (var error in errors)
        {
            _logger.LogWarning($"Validation error on {error.PropertyName}: {error.ErrorMessage}");
        }

        throw new InvalidDomainException(errors);
    }

    await _userRepository.SaveAsync(user);
    return user;
}
```

### Uso en Exception Messages

```csharp
public async Task UpdateUserAsync(User user)
{
    if (!user.IsValid())
    {
        var errors = user.Validate();
        var errorMessages = string.Join(", ", errors.Select(e => e.ErrorMessage));

        throw new InvalidOperationException($"User validation failed: {errorMessages}");
    }

    await _userRepository.UpdateAsync(user);
}
```

### Formato de ValidationFailure

```csharp
var errors = user.Validate();
foreach (var error in errors)
{
    Console.WriteLine($"Property: {error.PropertyName}");
    Console.WriteLine($"Message: {error.ErrorMessage}");
    Console.WriteLine($"Attempted Value: {error.AttemptedValue}");
    Console.WriteLine($"Error Code: {error.ErrorCode}");
    Console.WriteLine("---");
}
```

---

## GetValidator() - Integración con FluentValidation

### Pattern: Override en Entidad

```csharp
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

    /// <summary>
    /// Get the validator for the User entity.
    /// </summary>
    public override IValidator GetValidator()
        => new UserValidator();
}
```

### Validator Correspondiente

```csharp
// domain/entities/validators/UserValidator.cs
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

        RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .WithMessage("Name is required");
    }
}
```

---

## Flujo Completo de Validación

### Diagrama de Flujo

```
┌──────────────────────────┐
│  Código de Aplicación    │
│  user.IsValid()          │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  AbstractDomainObject    │
│  IsValid()               │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  User                    │
│  GetValidator()          │
│  return new UserValidator()│
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  UserValidator           │
│  (FluentValidation)      │
└──────────┬───────────────┘
           │
           ▼
┌──────────────────────────┐
│  ValidationResult        │
│  - IsValid: bool         │
│  - Errors: []            │
└──────────────────────────┘
```

### Ejemplo Paso a Paso

```csharp
// 1️⃣ Crear entidad
var user = new User("invalid.email", "");

// 2️⃣ Llamar IsValid()
bool isValid = user.IsValid();

// Internamente:
// 3️⃣ IsValid() llama GetValidator()
// 4️⃣ GetValidator() retorna new UserValidator()
// 5️⃣ UserValidator ejecuta reglas de FluentValidation
// 6️⃣ FluentValidation retorna ValidationResult
// 7️⃣ IsValid() retorna result.IsValid (false en este caso)

// 8️⃣ Si necesitamos detalles:
if (!isValid)
{
    var errors = user.Validate();  // Obtener errores detallados
    foreach (var error in errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}

// Output:
// Email: Email must be a valid email address
// Name: Name is required
```

---

## Patrones de Uso Comunes

### Pattern 1: Validar Antes de Guardar

```csharp
public async Task<User> CreateUserAsync(CreateUserDto dto)
{
    // Crear entidad
    var user = new User(dto.Email, dto.Name);

    // Validar
    if (!user.IsValid())
    {
        throw new InvalidDomainException(user.Validate());
    }

    // Persistir
    await _userRepository.SaveAsync(user);
    return user;
}
```

### Pattern 2: Validar Después de Cambios

```csharp
public async Task LockUserAsync(Guid userId)
{
    // Cargar entidad
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null)
        throw new NotFoundException("User not found");

    // Hacer cambio
    user.Locked = true;

    // Validar
    if (!user.IsValid())
    {
        throw new InvalidOperationException("Cannot lock user - validation failed");
    }

    // Persistir
    await _userRepository.UpdateAsync(user);
}
```

### Pattern 3: Validar con Rollback

```csharp
public void ChangeUserEmail(User user, string newEmail)
{
    // Guardar valor anterior
    var oldEmail = user.Email;

    // Hacer cambio
    user.Email = newEmail;

    // Validar
    if (!user.IsValid())
    {
        // Rollback si falla
        user.Email = oldEmail;

        var errors = user.Validate();
        throw new InvalidOperationException(
            $"Cannot change email: {string.Join(", ", errors.Select(e => e.ErrorMessage))}"
        );
    }
}
```

### Pattern 4: Validación con Custom Exception

```csharp
// Domain exception personalizada
public class InvalidDomainException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; }

    public InvalidDomainException(IEnumerable<ValidationFailure> errors)
        : base($"Validation failed: {string.Join(", ", errors.Select(e => e.ErrorMessage))}")
    {
        Errors = errors;
    }
}

// Uso
public async Task SaveUserAsync(User user)
{
    if (!user.IsValid())
    {
        throw new InvalidDomainException(user.Validate());
    }

    await _userRepository.SaveAsync(user);
}
```

---

## Validación en Diferentes Capas

### Domain Layer

```csharp
// Domain Service
public class UserDomainService
{
    public void AssignRole(User user, Role role)
    {
        user.Roles.Add(role);

        // Validar en dominio
        if (!user.IsValid())
        {
            user.Roles.Remove(role);
            throw new InvalidOperationException("Cannot assign role");
        }
    }
}
```

### Application Layer

```csharp
// Application Service
public class UserApplicationService
{
    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        var user = new User(dto.Email, dto.Name);

        // Validar antes de persistir
        if (!user.IsValid())
        {
            var errors = user.Validate();
            throw new ValidationException(errors);
        }

        await _userRepository.SaveAsync(user);
        return _mapper.Map<UserDto>(user);
    }
}
```

### API/WebApi Layer

```csharp
// Controller
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    try
    {
        var user = await _userService.CreateUserAsync(dto);
        return Ok(user);
    }
    catch (InvalidDomainException ex)
    {
        // Retornar errores de validación al cliente
        var errors = ex.Errors.Select(e => new
        {
            property = e.PropertyName,
            message = e.ErrorMessage
        });

        return BadRequest(new { errors });
    }
}
```

---

## Testing de Validación

### Test IsValid() - Valid Case

```csharp
[Test]
public void IsValid_WhenInstanceIsValid_ReturnsTrue()
{
    // Arrange
    var user = new User("test@example.com", "Test User");

    // Act
    var result = user.IsValid();

    // Assert
    result.Should().BeTrue("User should be valid with all required properties");
}
```

### Test IsValid() - Invalid Case

```csharp
[Test]
public void IsValid_WhenEmailIsEmpty_ReturnsFalse()
{
    // Arrange
    var user = new User("", "Test User");

    // Act
    var result = user.IsValid();

    // Assert
    result.Should().BeFalse("User should be invalid when Email is empty");
}
```

### Test Validate() - Check Errors

```csharp
[Test]
public void Validate_WithEmptyEmail_ShouldReturnErrors()
{
    // Arrange
    var user = new User("", "Test User");

    // Act
    var errors = user.Validate().ToList();

    // Assert
    errors.Should().NotBeEmpty();
    errors.Should().Contain(e => e.PropertyName == "Email");
    errors.Should().Contain(e => e.ErrorMessage.Contains("Email is required"));
}
```

### Test GetValidator()

```csharp
[Test]
public void GetValidator_ShouldReturnUserValidator()
{
    // Arrange
    var user = new User();

    // Act
    var validator = user.GetValidator();

    // Assert
    validator.Should().NotBeNull();
    validator.GetType().Name.Should().Be("UserValidator");
}
```

---

## Ejemplos por Tipo de Validación

### String Validation

```csharp
var user = new User("", "Test");  // Email vacío

if (!user.IsValid())
{
    var errors = user.Validate();
    // Email: Email is required
}
```

### Email Format Validation

```csharp
var user = new User("invalid.email", "Test");  // Email inválido

if (!user.IsValid())
{
    var errors = user.Validate();
    // Email: Email must be a valid email address
}
```

### DateTime Validation

```csharp
var prototype = new Prototype("P-001", default, DateTime.Now, "Active");  // IssueDate default

if (!prototype.IsValid())
{
    var errors = prototype.Validate();
    // IssueDate: Issue date is required
}
```

### Cross-Property Validation

```csharp
var prototype = new Prototype(
    "P-001",
    DateTime.Today,
    DateTime.Today.AddDays(-1),  // ExpirationDate before IssueDate
    "Active"
);

if (!prototype.IsValid())
{
    var errors = prototype.Validate();
    // ExpirationDate: Expiration date must be after issue date
}
```

### Allowed Values Validation

```csharp
var prototype = new Prototype("P-001", DateTime.Today, DateTime.Today.AddDays(30), "Invalid");

if (!prototype.IsValid())
{
    var errors = prototype.Validate();
    // Status: Status must be one of: Active, Expired, Cancelled
}
```

---

## Lecciones Clave

### ✅ Conceptos Demostrados

- **IsValid()** - Verificación rápida de validez
- **Validate()** - Obtener errores detallados
- **GetValidator()** - Override obligatorio en entidades
- **Validación antes de persistir** - Patrón estándar
- **Validación después de cambios** - Asegurar consistencia
- **Rollback en validación fallida** - Mantener estado consistente

### 📚 Patrones Importantes

**Verificación básica:**
```csharp
if (!entity.IsValid())
{
    throw new InvalidDomainException(entity.Validate());
}
```

**Con logging:**
```csharp
if (!entity.IsValid())
{
    var errors = entity.Validate();
    foreach (var error in errors)
    {
        _logger.LogWarning($"{error.PropertyName}: {error.ErrorMessage}");
    }
    throw new InvalidDomainException(errors);
}
```

**Con rollback:**
```csharp
var oldValue = entity.Property;
entity.Property = newValue;

if (!entity.IsValid())
{
    entity.Property = oldValue;
    throw new InvalidOperationException("Validation failed");
}
```

---

## Referencias

**Guías Relacionadas:**
- [Entity Guidelines](../../entities.md)
- [Validators](../../validators.md)
- [Entity Testing Practices](../../entities-testing-practices.md)

**Patrones Relacionados:**
- [Base Class](01-base-class.md)
- [Best Practices](05-best-practices.md)

**Ejemplos Prácticos:**
- [Role - Simple](../simple/Role.md)
- [User - Medium](../medium/User.md)
- [Prototype - Complex](../complex/Prototype.md)

---

**Última actualización:** 2025-01-20
