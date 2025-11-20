# Entities - Domain Layer

**Version:** 1.0.0
**Estado:** ✅ Completado
**Última actualización:** 2025-01-13

## Descripción

Las **entidades** son el corazón del Domain Layer. Representan conceptos de negocio con identidad única y encapsulan reglas de dominio, validaciones y comportamiento. En APSYS, todas las entidades heredan de `AbstractDomainObject` que provee funcionalidad común.

## Objetivo

- Definir entidades de dominio con identidad única
- Encapsular reglas de negocio en las entidades
- Integrar validaciones con FluentValidation
- Mantener entidades independientes de frameworks de persistencia
- Seguir patrones consistentes en toda la codebase

---

## Tabla de Contenido

1. [¿Qué es una Entidad?](#qué-es-una-entidad)
2. [AbstractDomainObject](#abstractdomainobject)
3. [Propiedades Virtual](#propiedades-virtual)
4. [Constructores](#constructores)
5. [Métodos de Dominio](#métodos-de-dominio)
6. [GetValidator Integration](#getvalidator-integration)
7. [Ejemplos Reales](#ejemplos-reales)
8. [Patrones y Best Practices](#patrones-y-best-practices)

---

## ¿Qué es una Entidad?

### Definición

Una **entidad** es un objeto que:
- Tiene **identidad única** (normalmente un `Id`)
- Su identidad permanece constante a través del tiempo
- Dos entidades son iguales si tienen el mismo `Id`
- Encapsula **reglas de negocio** y comportamiento

### Entity vs DTO vs DAO

```
┌──────────────────┬──────────────────┬──────────────────┐
│     Entity       │       DTO        │       DAO        │
├──────────────────┼──────────────────┼──────────────────┤
│ Domain Layer     │ WebApi Layer     │ Domain Layer     │
│ Identidad única  │ Sin identidad    │ Sin identidad    │
│ Tiene validación │ Sin validación   │ Sin validación   │
│ Tiene métodos    │ Solo propiedades │ Solo propiedades │
│ Read/Write       │ Transferencia    │ Read-only        │
│ Hereda de Base   │ POCO             │ POCO             │
└──────────────────┴──────────────────┴──────────────────┘
```

### Características de Entidades en APSYS

✅ **Herencia de AbstractDomainObject**
```csharp
public class User : AbstractDomainObject
{
    // Hereda: Id, CreationDate, IsValid(), Validate(), GetValidator()
}
```

✅ **Propiedades virtuales (NHibernate)**
```csharp
public virtual string Name { get; set; } = string.Empty;
```

✅ **Constructores múltiples**
```csharp
public User() { }  // Para NHibernate
public User(string email, string name) { }  // Para creación
```

✅ **Validaciones integradas**
```csharp
public override IValidator GetValidator() => new UserValidator();
```

---

## AbstractDomainObject

Todas las entidades en APSYS heredan de `AbstractDomainObject`, que provee funcionalidad común como identidad única, fecha de creación, y métodos de validación.

### Propiedades y Métodos Heredados

| Elemento | Tipo | Descripción |
|----------|------|-------------|
| `Id` | `Guid` | Identificador único, generado automáticamente |
| `CreationDate` | `DateTime` | Fecha de creación, asignada en UTC |
| `IsValid()` | `bool` | Verifica si la entidad cumple validaciones |
| `Validate()` | `IEnumerable<ValidationFailure>` | Retorna lista de errores de validación |
| `GetValidator()` | `IValidator?` | Debe ser sobrescrito para retornar validator |

### Ejemplo de Uso

```csharp
public class User : AbstractDomainObject
{
    // Automáticamente tiene: Id, CreationDate, IsValid(), Validate(), GetValidator()
    public virtual string Email { get; set; } = string.Empty;

    public override IValidator GetValidator() => new UserValidator();
}
```

📖 **Ver documentación completa:** [AbstractDomainObject](examples/entities/patterns/01-base-class.md)

---

## Propiedades Virtual

### Regla Fundamental

Todas las propiedades deben ser `virtual` para compatibilidad con NHibernate (lazy loading y change tracking).

```csharp
✅ Correcto:
public virtual string Name { get; set; } = string.Empty;
public virtual IList<Role> Roles { get; set; } = new List<Role>();

❌ Incorrecto:
public string Name { get; set; }  // Falta virtual
```

### Tipos de Propiedades Comunes

- **Strings:** `public virtual string Name { get; set; } = string.Empty;`
- **Números:** `public virtual int Age { get; set; }`
- **Booleanos:** `public virtual bool Locked { get; set; }`
- **Fechas:** `public virtual DateTime IssueDate { get; set; }`
- **Colecciones:** `public virtual IList<Role> Roles { get; set; } = new List<Role>();`
- **Referencias:** `public virtual Category Category { get; set; } = null!;`

📖 **Ver guía completa de tipos de propiedades:** [Property Types](examples/entities/patterns/02-properties.md)

---

## Constructores

### Patrón: Dos Constructores Obligatorios

Todas las entidades deben tener **exactamente dos constructores**:

#### 1. Constructor Vacío (NHibernate)

```csharp
/// <summary>
/// This constructor is used by NHibernate for mapping purposes.
/// </summary>
public User() { }
```

#### 2. Constructor con Parámetros (Creación)

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

### Reglas

- ✅ Incluir solo propiedades **esenciales** como parámetros
- ❌ NO incluir `Id`, `CreationDate` (se asignan automáticamente)
- ❌ NO incluir colecciones (se inicializan en propiedades)

📖 **Ver guía completa de constructores:** [Constructor Patterns](examples/entities/patterns/03-constructors.md)

---

## Validación

### Tres Métodos de Validación

Todas las entidades heredan tres métodos para validación:

| Método | Retorno | Uso |
|--------|---------|-----|
| `IsValid()` | `bool` | Verificar si la entidad es válida |
| `Validate()` | `IEnumerable<ValidationFailure>` | Obtener lista de errores |
| `GetValidator()` | `IValidator` | Override obligatorio - retornar validator |

### Ejemplo de Uso

```csharp
// 1️⃣ Entidad sobrescribe GetValidator()
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;

    public override IValidator GetValidator() => new UserValidator();
}

// 2️⃣ Uso en código
var user = new User("test@example.com", "Test");

if (!user.IsValid())
{
    var errors = user.Validate();
    throw new InvalidDomainException(errors);
}
```

📖 **Ver guía completa de validación:** [Validation Usage](examples/entities/patterns/04-validation.md)

---

## Ejemplos Reales

Los siguientes ejemplos están basados en proyectos reales y organizados por complejidad. Cada ejemplo incluye la entidad completa, validator, tests y casos de uso.

### 📁 Ejemplos por Complejidad

#### [Role - Entidad Simple](examples/entities/simple/Role.md)

**Complejidad:** Simple | **Una sola propiedad**

```csharp
public class Role : AbstractDomainObject
{
    public virtual string Name { get; set; } = string.Empty;

    public Role() { }
    public Role(string name) { Name = name; }

    public override IValidator GetValidator() => new RoleValidator();
}
```

✅ **Aprende:** Estructura básica, patrón completo en entidad simple
📖 **Ver ejemplo completo con tests:** [Role.md](examples/entities/simple/Role.md)

---

#### [User - Complejidad Media](examples/entities/medium/User.md)

**Complejidad:** Media | **Propiedades + Colecciones**

```csharp
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual bool Locked { get; set; }
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User() { }
    public User(string email, string name) { /* ... */ }

    public override IValidator GetValidator() => new UserValidator();
}
```

✅ **Aprende:** Colecciones, email validation, boolean properties
📖 **Ver ejemplo completo con tests:** [User.md](examples/entities/medium/User.md)

---

#### [Prototype - Entidad Compleja](examples/entities/complex/Prototype.md)

**Complejidad:** Compleja | **DateTime + Cross-Property Validations**

```csharp
public class Prototype : AbstractDomainObject
{
    public virtual string Number { get; set; } = string.Empty;
    public virtual DateTime IssueDate { get; set; }
    public virtual DateTime ExpirationDate { get; set; }
    public virtual string Status { get; set; } = string.Empty;

    public Prototype() { }
    public Prototype(string number, DateTime issueDate,
                     DateTime expirationDate, string status) { /* ... */ }

    public override IValidator GetValidator() => new PrototypeValidator();
}
```

✅ **Aprende:** DateTime validation, cross-property rules, allowed values
📖 **Ver ejemplo completo con tests:** [Prototype.md](examples/entities/complex/Prototype.md)

---

#### [TechnicalStandard - Entidad Completa](examples/entities/complex/TechnicalStandard.md)

**Complejidad:** Compleja | **Múltiples Propiedades + Allowed Values**

```csharp
public class TechnicalStandard : AbstractDomainObject
{
    public virtual string Code { get; set; } = string.Empty;
    public virtual string Name { get; set; } = string.Empty;
    public virtual string Edition { get; set; } = string.Empty;
    public virtual string Status { get; set; } = string.Empty;
    public virtual string Type { get; set; } = string.Empty;

    public TechnicalStandard() { }
    public TechnicalStandard(string code, string name, string edition,
                             string status, string type) { /* ... */ }

    public override IValidator GetValidator() => new TechnicalStandardValidator();
}
```

✅ **Aprende:** Múltiples propiedades, allowed values, constructores complejos
📖 **Ver ejemplo completo con tests:** [TechnicalStandard.md](examples/entities/complex/TechnicalStandard.md)

---

### 📚 Más Ejemplos

**Por Proyecto:**
- [hashira-stone ejemplos](examples/entities/by-project/hashira-stone/) - Ejemplos del proyecto real

**Todos los ejemplos incluyen:**
- ✅ Código completo de Entity y Validator
- ✅ Tests unitarios completos con AAA pattern
- ✅ Ejemplos de uso en código
- ✅ Lecciones clave y conceptos demostrados
- ✅ Referencias cruzadas a guías relacionadas

---

## Patrones y Best Practices

### ✅ DO - Las 6 Reglas de Oro

1. **Heredar de AbstractDomainObject** - Funcionalidad común automática
2. **Propiedades virtual** - Obligatorio para NHibernate
3. **Dos constructores** - Vacío (NHibernate) + Parametrizado (Creación)
4. **Sobrescribir GetValidator** - Integración con FluentValidation
5. **Inicializar colecciones** - Evitar NullReferenceException
6. **Documentación XML** - Mejor experiencia de desarrollo

```csharp
// ✅ Entidad perfecta siguiendo todas las best practices
public class User : AbstractDomainObject
{
    public virtual string Email { get; set; } = string.Empty;
    public virtual IList<Role> Roles { get; set; } = new List<Role>();

    public User() { }
    public User(string email) { Email = email; }

    public override IValidator GetValidator() => new UserValidator();
}
```

📖 **Ver guía completa de best practices:** [Best Practices](examples/entities/patterns/05-best-practices.md)

---

### ❌ DON'T - Los 7 Anti-Patterns Críticos

1. **NO persistencia en entidades** - Va en Repositories
2. **NO atributos de ORM** - Va en Mappers
3. **NO dependencias externas** - Dominio debe ser limpio
4. **NO propiedades internas públicas** - Mantener encapsulación
5. **NO mezclar responsabilidades** - Cada capa tiene su rol
6. **NO factory methods estáticos** - Usar Domain Services
7. **NO validación en setters** - Usar Validators

```csharp
❌ NUNCA hacer esto:
[Table("users")]  // ❌ Atributo de ORM
public class User : AbstractDomainObject
{
    [JsonProperty("email")]  // ❌ Atributo de serialización
    public virtual string Email { get; set; }

    public void SaveToDatabase() { }  // ❌ Lógica de persistencia
}
```

📖 **Ver guía completa de anti-patterns:** [Anti-Patterns](examples/entities/patterns/06-anti-patterns.md)

---

## Checklist: Nueva Entidad

Al crear una nueva entidad de dominio:

- [ ] Clase hereda de `AbstractDomainObject`
- [ ] Namespace: `{proyecto}.domain.entities`
- [ ] Propiedades son `virtual`
- [ ] Propiedades tienen valores por defecto (`= string.Empty`, `= new List<>()`)
- [ ] Constructor vacío existe (para NHibernate)
- [ ] Constructor con parámetros existe (para creación)
- [ ] `GetValidator()` está sobrescrito
- [ ] Validator correspondiente existe en `validators/`
- [ ] Documentación XML completa en todas las propiedades
- [ ] Documentación XML en constructores
- [ ] Documentación XML en `GetValidator()`
- [ ] No tiene atributos de ORM
- [ ] No tiene lógica de persistencia
- [ ] No depende de frameworks externos
- [ ] Tests unitarios en `Domain.Tests/Entities/{EntityName}Tests.cs`
- [ ] Tests cubren constructores, validaciones y reglas de dominio

> **Ver**: [testing-conventions.md](../best-practices/testing-conventions.md#domain-layer-tests) para ejemplos de tests de entidades

---

## Conclusión

**Principios Clave para Entidades:**

1. ✅ **Heredar de AbstractDomainObject** - Funcionalidad común
2. ✅ **Propiedades virtual** - Requerido para NHibernate
3. ✅ **Dos constructores** - Vacío para ORM, con parámetros para creación
4. ✅ **GetValidator sobrescrito** - Integración con FluentValidation
5. ✅ **Documentación completa** - XML comments en todo
6. ✅ **Independencia** - No depender de frameworks de persistencia

**Flujo Mental:**

```
Entidad hereda AbstractDomainObject
   ↓
Propiedades virtual + valores por defecto
   ↓
Constructor vacío + Constructor con parámetros
   ↓
GetValidator() → retorna Validator
   ↓
IsValid() / Validate() disponibles
   ↓
Entidad lista para usar en Application/Infrastructure
```

**Ejemplos de entidades por complejidad:**

- **Simple:** `Role` (1 propiedad)
- **Media:** `User` (propiedades + colección)
- **Compleja:** `TechnicalStandard` (múltiples propiedades + lógica)

---

## Recursos Adicionales

### Guías Relacionadas

- [Validators](./validators.md) - Validaciones con FluentValidation
- [Repository Interfaces](./repository-interfaces.md) - Contratos de persistencia
- [DAOs](./daos.md) - Objetos de solo lectura
- [Domain Exceptions](./domain-exceptions.md) - Excepciones de dominio

### Documentación Oficial

- [FluentValidation](https://docs.fluentvalidation.net/)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
