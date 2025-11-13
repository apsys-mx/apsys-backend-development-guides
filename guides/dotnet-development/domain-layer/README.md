# Domain Layer - Clean Architecture

**Version:** 0.1.0
**Estado:** ⏳ En desarrollo
**Última actualización:** 2025-01-13

## Descripción

La capa de dominio es el **núcleo** de Clean Architecture. Contiene las entidades de negocio, reglas de dominio, validaciones y definiciones de interfaces. Esta capa NO debe tener dependencias hacia otras capas y debe ser completamente independiente de frameworks externos.

## Principios Fundamentales

- **Independencia Total**: No depende de Application, Infrastructure o WebApi
- **Reglas de Negocio**: Contiene toda la lógica de negocio core
- **Persistencia Agnóstica**: No conoce cómo se persisten los datos
- **Framework Agnóstico**: No depende de NHibernate, EF, FastEndpoints, etc.
- **Interfaces en Domain**: Define contratos (IRepository, IUnitOfWork) que Infrastructure implementa

## Guías Disponibles

### 1. [Entities](./entities.md)

Entidades de dominio y clase base AbstractDomainObject.

**Contenido:**
- Qué es una entidad de dominio
- AbstractDomainObject pattern
- Propiedades virtuales (NHibernate requirement)
- Constructores
- Métodos de dominio
- GetValidator() integration
- Ejemplos: User, Role, Prototype, TechnicalStandard

**Cuándo usar:** Al crear nuevas entidades de dominio.

---

### 2. [Validators](./validators.md)

Validaciones de entidades con FluentValidation.

**Contenido:**
- AbstractValidator<T> pattern
- Reglas de validación (RuleFor)
- Validaciones comunes (NotNull, NotEmpty, EmailAddress)
- Validaciones custom
- Error codes y messages
- Integración con entidades
- IsValid() y Validate()
- Ejemplos: UserValidator, PrototypeValidator

**Cuándo usar:** Al agregar validaciones a entidades de dominio.

---

### 3. [Repository Interfaces](./repository-interfaces.md)

Interfaces de repositorios y Unit of Work.

**Contenido:**
- IRepository<T, TKey> interface
- IReadOnlyRepository<T, TKey> interface
- IEntityRepository interfaces específicas
- IUnitOfWork interface
- GetManyAndCountAsync patterns
- Métodos custom por repositorio
- Separation: Interfaces en Domain, Implementaciones en Infrastructure
- Ejemplos: IUserRepository, IPrototypeRepository

**Cuándo usar:** Al definir contratos de persistencia para entidades.

---

### 4. [DAOs](./daos.md)

Data Access Objects para consultas optimizadas.

**Contenido:**
- Qué es un DAO
- Cuándo usar DAO vs Entity
- Estructura de DAOs
- Propiedades read-only
- Search fields (SearchAll pattern)
- DAO Repositories (IReadOnlyRepository)
- Ejemplos: PrototypeDao, TechnicalStandardDao

**Cuándo usar:** Para listados, reportes y consultas de solo lectura.

---

### 5. [Domain Exceptions](./domain-exceptions.md)

Excepciones custom del dominio.

**Contenido:**
- InvalidDomainException
- DuplicatedDomainException
- DomainException base class
- Cuándo lanzar excepciones
- Excepciones vs Results
- Error messages
- Ejemplos del proyecto

**Cuándo usar:** Al definir reglas de dominio que pueden fallar.

---

### 6. [Value Objects](./value-objects.md)

Value Objects pattern para conceptos de dominio.

**Contenido:**
- Qué es un Value Object
- Immutability
- Equality by value
- Cuándo usar vs Entity
- Ejemplos comunes (Email, Money, Address)
- Integration con NHibernate (si aplicable)

**Cuándo usar:** Para representar conceptos sin identidad propia.

---

## Estructura de la Capa de Dominio

```
domain/
├── entities/                                # Entidades de dominio
│   ├── AbstractDomainObject.cs             # Clase base
│   ├── User.cs
│   ├── Role.cs
│   ├── Prototype.cs
│   ├── TechnicalStandard.cs
│   └── validators/                          # Validadores
│       ├── UserValidator.cs
│       ├── RoleValidator.cs
│       ├── PrototypeValidator.cs
│       └── TechnicalStandardValidator.cs
│
├── daos/                                    # Data Access Objects
│   ├── PrototypeDao.cs
│   └── TechnicalStandardDao.cs
│
├── interfaces/                              # Interfaces
│   ├── repositories/                        # Interfaces de repositorios
│   │   ├── IRepository.cs                  # Interface genérica
│   │   ├── IReadOnlyRepository.cs          # Interface read-only
│   │   ├── IUserRepository.cs              # Interface específica
│   │   ├── IPrototypeRepository.cs
│   │   ├── IPrototypeDaoRepository.cs
│   │   ├── ITechnicalStandardRepository.cs
│   │   ├── ITechnicalStandardDaoRepository.cs
│   │   ├── IUnitOfWork.cs                  # Unit of Work
│   │   ├── GetManyAndCountResult.cs        # DTOs de resultado
│   │   ├── SortingCriteria.cs
│   │   └── IGetManyAndCountResultWithSorting.cs
│   └── services/                            # Interfaces de servicios
│       └── IIdentityService.cs              # Ej: Auth0 service
│
├── exceptions/                              # Excepciones de dominio
│   ├── InvalidDomainException.cs
│   └── DuplicatedDomainException.cs
│
├── errors/                                  # (Si usan FluentResults)
│   └── DomainError.cs
│
└── resources/                               # Recursos
    └── AppSchemaResource.cs                 # Ej: Schema name
```

## Flujo de Trabajo

### Crear Nueva Entidad de Dominio

1. **Definir entidad** → [Entities](./entities.md)
   ```csharp
   public class Product : AbstractDomainObject
   {
       public virtual string Name { get; set; }
       public virtual decimal Price { get; set; }

       public override IValidator GetValidator() => new ProductValidator();
   }
   ```

2. **Crear validador** → [Validators](./validators.md)
   ```csharp
   public class ProductValidator : AbstractValidator<Product>
   {
       public ProductValidator()
       {
           RuleFor(x => x.Name).NotEmpty();
           RuleFor(x => x.Price).GreaterThan(0);
       }
   }
   ```

3. **Definir repositorio** → [Repository Interfaces](./repository-interfaces.md)
   ```csharp
   public interface IProductRepository : IRepository<Product, Guid>
   {
       Task<Product?> GetByNameAsync(string name);
   }
   ```

4. **Agregar a Unit of Work**
   ```csharp
   public interface IUnitOfWork
   {
       IProductRepository Products { get; }
       // ... otros repositorios
   }
   ```

### Crear DAO para Consultas

1. **Definir DAO** → [DAOs](./daos.md)
   ```csharp
   public class ProductDao
   {
       public virtual Guid Id { get; set; }
       public virtual string Name { get; set; }
       public virtual decimal Price { get; set; }
       public virtual string SearchAll { get; set; }
   }
   ```

2. **Definir repositorio read-only**
   ```csharp
   public interface IProductDaoRepository : IReadOnlyRepository<ProductDao, Guid>
   {
   }
   ```

3. **Agregar a Unit of Work**
   ```csharp
   public interface IUnitOfWork
   {
       IProductDaoRepository ProductDaos { get; }
       // ... otros repositorios
   }
   ```

---

## Checklists Rápidas

### Nueva Entidad CRUD

- [ ] Clase `{Entity}.cs` hereda de `AbstractDomainObject`
- [ ] Propiedades son `virtual` (para NHibernate)
- [ ] Constructor por defecto existe
- [ ] Constructor con parámetros para creación
- [ ] `GetValidator()` implementado
- [ ] `{Entity}Validator.cs` creado en `validators/`
- [ ] Reglas de validación definidas
- [ ] `I{Entity}Repository.cs` creado en `interfaces/repositories/`
- [ ] Hereda de `IRepository<{Entity}, Guid>`
- [ ] Métodos custom definidos si necesarios
- [ ] Agregado a `IUnitOfWork`

### Nuevo DAO

- [ ] Clase `{Entity}Dao.cs` creada en `daos/`
- [ ] Propiedades son `virtual`
- [ ] NO hereda de AbstractDomainObject
- [ ] NO tiene validaciones
- [ ] `I{Entity}DaoRepository.cs` creado
- [ ] Hereda de `IReadOnlyRepository<{Entity}Dao, Guid>`
- [ ] Agregado a `IUnitOfWork`

### Nueva Excepción de Dominio

- [ ] Clase hereda de `Exception` o custom base
- [ ] Constructor con mensaje
- [ ] Constructor con mensaje y inner exception
- [ ] Usado en métodos de entidad o repositorio
- [ ] Documentado con XML comments

---

## Patrones Clave

### 1. Dependency Inversion

Domain define interfaces, Infrastructure las implementa:
- ✅ `IUserRepository` en Domain
- ✅ `NHUserRepository` en Infrastructure
- ✅ Domain NO conoce NHibernate
- ✅ Domain NO conoce PostgreSQL

### 2. Entity Validation

Entidades auto-validables:
- ✅ `GetValidator()` retorna `IValidator`
- ✅ `IsValid()` verifica validación
- ✅ `Validate()` retorna errores
- ✅ Validación antes de persistir

### 3. DAO Pattern

Separación entre escritura y lectura:
- ✅ **Entity**: Para operaciones CRUD
- ✅ **DAO**: Para consultas optimizadas
- ✅ DAO no tiene validaciones
- ✅ DAO es más ligero

### 4. Rich Domain Model

Entidades con comportamiento:
- ✅ No son simples DTOs
- ✅ Tienen métodos de negocio
- ✅ Encapsulan reglas
- ✅ Validan su estado

---

## Reglas de Oro

### ✅ SÍ hacer en Domain

- Definir entidades con reglas de negocio
- Crear validadores con FluentValidation
- Definir interfaces de repositorios
- Lanzar excepciones de dominio
- Usar Value Objects para conceptos
- Documentar con XML comments

### ❌ NO hacer en Domain

- Referenciar Infrastructure
- Referenciar Application
- Referenciar WebApi
- Usar NHibernate directamente
- Usar Entity Framework directamente
- Usar HttpClient o servicios externos
- Depender de FastEndpoints
- Hacer queries a BD

---

## Stack Tecnológico

- **FluentValidation 12.0** - Validaciones
- **C# 13** - Lenguaje
- **.NET 9.0** - Framework base

---

## Recursos Adicionales

### Documentación Oficial

- [FluentValidation Docs](https://docs.fluentvalidation.net/)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)

### Otras Secciones de Guías

- [Best Practices](../best-practices/README.md)
- [Feature Structure](../feature-structure/README.md)
- [Application Layer](../application-layer/README.md)
- [Infrastructure Layer](../infrastructure-layer/README.md)

---

**Última actualización:** 2025-01-13
**Mantenedor:** Equipo APSYS
