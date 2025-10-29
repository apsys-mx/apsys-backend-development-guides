# 02 - Capa de Dominio (Domain Layer)

## Descripción

Este documento describe cómo crear la **capa de dominio (Domain Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta capa contiene:

- **Entidades de dominio**: Objetos de negocio con sus reglas y validaciones
- **Interfaces de repositorios**: Contratos para acceso a datos
- **Excepciones de dominio**: Excepciones específicas del negocio
- **Proyecto de tests**: Pruebas unitarias para la capa de dominio

Esta capa es **completamente independiente de la infraestructura** y de cualquier base de datos específica. No tiene dependencias externas excepto FluentValidation para validaciones.

## Dependencias

Este paso requiere que se haya completado:
- ✅ **[01-estructura-base.md](./01-estructura-base.md)** - Estructura base del proyecto

## Parámetros de Entrada

Los mismos parámetros del paso anterior:

| Parámetro   | Valor desde paso anterior |
| ----------- | ------------------------- |
| `--name`    | Nombre de la solución     |
| `--path`    | Ruta del proyecto         |

## Estructura de Archivos a Crear

```
{path}/
├── src/
│   └── {name}.domain/
│       ├── {name}.domain.csproj
│       ├── entities/
│       │   └── AbstractDomainObject.cs
│       ├── exceptions/
│       │   ├── InvalidDomainException.cs
│       │   └── InvalidFilterArgumentException.cs
│       └── interfaces/
│           └── repositories/
│               ├── IRepository.cs
│               ├── IReadOnlyRepository.cs
│               ├── IUnitOfWork.cs
│               ├── IGetManyAndCountResultWithSorting.cs
│               ├── GetManyAndCountResult.cs
│               └── SortingCriteria.cs
└── tests/
    └── {name}.domain.tests/
        ├── {name}.domain.tests.csproj
        └── entities/
            └── DomainTestBase.cs
```

## Paquetes NuGet Requeridos

### Para el proyecto source (domain):
- `FluentValidation` - Validación de entidades

### Para el proyecto de tests (domain.tests):
- `NUnit` - Framework de testing (incluido en template)
- `Microsoft.NET.Test.Sdk` - SDK de testing (incluido en template)
- `NUnit3TestAdapter` - Adaptador de NUnit (incluido en template)
- `AutoFixture.AutoMoq` - Generación automática de datos de prueba
- `FluentAssertions` - Aserciones fluidas para tests

## Proceso de Construcción

### Paso 2.1: Crear proyecto classlib para source

```bash
mkdir "{path}/src/{name}.domain"
dotnet new classlib -n {name}.domain -o "{path}/src/{name}.domain"
dotnet sln "{path}/{name}.sln" add "{path}/src/{name}.domain/{name}.domain.csproj"
```

**Ejemplo concreto:**

```bash
mkdir "C:/projects/miproyecto/src/MiProyecto.domain"
dotnet new classlib -n MiProyecto.domain -o "C:/projects/miproyecto/src/MiProyecto.domain"
dotnet sln "C:/projects/miproyecto/MiProyecto.sln" add "C:/projects/miproyecto/src/MiProyecto.domain/MiProyecto.domain.csproj"
```

**Resultado esperado:**

```
La plantilla "Biblioteca de clases" se creó correctamente.
Proyecto agregado a la solución.
```

### Paso 2.2: Eliminar archivo Class1.cs autogenerado

```bash
rm "{path}/src/{name}.domain/Class1.cs"
```

**Ejemplo concreto:**

```bash
rm "C:/projects/miproyecto/src/MiProyecto.domain/Class1.cs"
```

### Paso 2.3: Instalar paquetes NuGet en source

```bash
dotnet add "{path}/src/{name}.domain/{name}.domain.csproj" package FluentValidation
```

**Ejemplo concreto:**

```bash
dotnet add "C:/projects/miproyecto/src/MiProyecto.domain/MiProyecto.domain.csproj" package FluentValidation
```

### Paso 2.4: Crear proyecto de tests

```bash
mkdir "{path}/tests/{name}.domain.tests"
dotnet new nunit -n {name}.domain.tests -o "{path}/tests/{name}.domain.tests"
dotnet sln "{path}/{name}.sln" add "{path}/tests/{name}.domain.tests/{name}.domain.tests.csproj"
```

**Ejemplo concreto:**

```bash
mkdir "C:/projects/miproyecto/tests/MiProyecto.domain.tests"
dotnet new nunit -n MiProyecto.domain.tests -o "C:/projects/miproyecto/tests/MiProyecto.domain.tests"
dotnet sln "C:/projects/miproyecto/MiProyecto.sln" add "C:/projects/miproyecto/tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj"
```

### Paso 2.5: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** Editar `tests/{name}.domain.tests/{name}.domain.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference` (porque usamos gestión centralizada con `Directory.Packages.props`).

**Cambiar de:**

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  <PackageReference Include="NUnit" Version="4.2.2" />
  <PackageReference Include="NUnit.Analyzers" Version="4.4.0" />
  <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
</ItemGroup>
```

**A:**

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="NUnit" />
  <PackageReference Include="NUnit.Analyzers" />
  <PackageReference Include="NUnit3TestAdapter" />
</ItemGroup>
```

### Paso 2.6: Instalar paquetes NuGet adicionales en tests

```bash
dotnet add "{path}/tests/{name}.domain.tests/{name}.domain.tests.csproj" package AutoFixture.AutoMoq
dotnet add "{path}/tests/{name}.domain.tests/{name}.domain.tests.csproj" package FluentAssertions
```

**Ejemplo concreto:**

```bash
dotnet add "C:/projects/miproyecto/tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:/projects/miproyecto/tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj" package FluentAssertions
```

### Paso 2.7: Agregar referencia al proyecto domain en tests

```bash
dotnet add "{path}/tests/{name}.domain.tests/{name}.domain.tests.csproj" reference "{path}/src/{name}.domain/{name}.domain.csproj"
```

**Ejemplo concreto:**

```bash
dotnet add "C:/projects/miproyecto/tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj" reference "C:/projects/miproyecto/src/MiProyecto.domain/MiProyecto.domain.csproj"
```

### Paso 2.8: Crear estructura de carpetas

```bash
mkdir "{path}/src/{name}.domain/entities"
mkdir "{path}/src/{name}.domain/exceptions"
mkdir "{path}/src/{name}.domain/interfaces"
mkdir "{path}/src/{name}.domain/interfaces/repositories"
mkdir "{path}/tests/{name}.domain.tests/entities"
```

**Ejemplo concreto:**

```bash
mkdir "C:/projects/miproyecto/src/MiProyecto.domain/entities"
mkdir "C:/projects/miproyecto/src/MiProyecto.domain/exceptions"
mkdir "C:/projects/miproyecto/src/MiProyecto.domain/interfaces"
mkdir "C:/projects/miproyecto/src/MiProyecto.domain/interfaces/repositories"
mkdir "C:/projects/miproyecto/tests/MiProyecto.domain.tests/entities"
```

### Paso 2.9: Eliminar archivo de test autogenerado

```bash
rm "{path}/tests/{name}.domain.tests/UnitTest1.cs"
```

**Ejemplo concreto:**

```bash
rm "C:/projects/miproyecto/tests/MiProyecto.domain.tests/UnitTest1.cs"
```

## Código Fuente de Archivos

### entities/AbstractDomainObject.cs

**Ubicación:** `{path}/src/{name}.domain/entities/AbstractDomainObject.cs`

```csharp
using FluentValidation;
using FluentValidation.Results;

namespace {name}.domain.entities;

public abstract class AbstractDomainObject
{
    protected AbstractDomainObject()
    { }

    protected AbstractDomainObject(Guid id, DateTime creationDate)
    {
        Id = id;
        CreationDate = creationDate;
    }

    public virtual Guid Id { get; set; } = Guid.NewGuid();
    public virtual DateTime CreationDate { get; set; } = DateTime.Now;

    public virtual bool IsValid()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return true;

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.IsValid;
    }

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

    public virtual IValidator? GetValidator()
         => null;
}
```

**Propósito:** Clase base abstracta para todas las entidades de dominio. Proporciona:
- Propiedades comunes (Id, CreationDate)
- Métodos de validación integrados con FluentValidation
- Patrón Template Method para validadores

### exceptions/InvalidDomainException.cs

**Ubicación:** `{path}/src/{name}.domain/exceptions/InvalidDomainException.cs`

```csharp
using FluentValidation.Results;

namespace {name}.domain.exceptions;

public class InvalidDomainException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; set; }

    public InvalidDomainException(IEnumerable<ValidationFailure> errors)
        : base("Domain validation failed")
    {
        Errors = errors;
    }
}
```

**Propósito:** Excepción lanzada cuando una entidad de dominio no cumple con sus reglas de validación.

### exceptions/InvalidFilterArgumentException.cs

**Ubicación:** `{path}/src/{name}.domain/exceptions/InvalidFilterArgumentException.cs`

```csharp
namespace {name}.domain.exceptions;

public class InvalidFilterArgumentException : Exception
{
    public InvalidFilterArgumentException(string message) : base(message)
    {
    }

    public InvalidFilterArgumentException(string message, string argName) : base(message)
    {
    }
}
```

**Propósito:** Excepción lanzada cuando los argumentos de filtrado (queries) son inválidos.

### interfaces/repositories/IRepository.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/IRepository.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class
{
    T Add(T item);
    Task AddAsync(T item);
    T Save(T item);
    Task SaveAsync(T item);
    void Delete(T item);
    Task DeleteAsync(T item);
}
```

**Propósito:** Interfaz genérica para operaciones de escritura en repositorios (CRUD completo).

### interfaces/repositories/IReadOnlyRepository.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/IReadOnlyRepository.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public interface IReadOnlyRepository<T, TKey> where T : class
{
    T? GetById(TKey id);
    Task<T?> GetByIdAsync(TKey id);
    IEnumerable<T> GetAll();
    Task<IEnumerable<T>> GetAllAsync();
}
```

**Propósito:** Interfaz genérica para operaciones de solo lectura en repositorios.

### interfaces/repositories/IUnitOfWork.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/IUnitOfWork.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public interface IUnitOfWork : IDisposable
{
    void BeginTransaction();
    void Commit();
    void Rollback();
}
```

**Propósito:** Patrón Unit of Work para gestionar transacciones de base de datos.

### interfaces/repositories/GetManyAndCountResult.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/GetManyAndCountResult.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public class GetManyAndCountResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Count { get; set; }
}
```

**Propósito:** DTO para resultados paginados (items + total count).

### interfaces/repositories/SortingCriteria.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/SortingCriteria.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public class SortingCriteria
{
    public string PropertyName { get; set; } = string.Empty;
    public bool Ascending { get; set; } = true;
}
```

**Propósito:** Representa criterios de ordenamiento para consultas.

### interfaces/repositories/IGetManyAndCountResultWithSorting.cs

**Ubicación:** `{path}/src/{name}.domain/interfaces/repositories/IGetManyAndCountResultWithSorting.cs`

```csharp
namespace {name}.domain.interfaces.repositories;

public interface IGetManyAndCountResultWithSorting<T> where T : class
{
    GetManyAndCountResult<T> GetManyAndCount(int offset, int limit, string? filter = null, IEnumerable<SortingCriteria>? sorting = null);
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(int offset, int limit, string? filter = null, IEnumerable<SortingCriteria>? sorting = null);
}
```

**Propósito:** Interfaz para consultas paginadas con soporte de filtrado y ordenamiento avanzado.

### tests/entities/DomainTestBase.cs

**Ubicación:** `{path}/tests/{name}.domain.tests/entities/DomainTestBase.cs`

```csharp
using NUnit.Framework;

namespace {name}.domain.tests.entities;

public class DomainTestBase
{
    [SetUp]
    public void Setup()
    {
        // Inicialización común para tests de dominio
    }
}
```

**Propósito:** Clase base para tests de entidades de dominio, proporciona setup común.

## Validación

### 1. Verificar que los proyectos se compiln correctamente

```bash
dotnet build "{path}/{name}.sln"
```

**Resultado esperado:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Verificar estructura de carpetas

```bash
ls -R "{path}/src/{name}.domain"
```

**Debe mostrar:**

```
entities/
  AbstractDomainObject.cs
exceptions/
  InvalidDomainException.cs
  InvalidFilterArgumentException.cs
interfaces/
  repositories/
    IRepository.cs
    IReadOnlyRepository.cs
    IUnitOfWork.cs
    IGetManyAndCountResultWithSorting.cs
    GetManyAndCountResult.cs
    SortingCriteria.cs
{name}.domain.csproj
```

### 3. Ejecutar tests

```bash
dotnet test "{path}/tests/{name}.domain.tests/{name}.domain.tests.csproj"
```

**Resultado esperado:**

```
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

### 4. Verificar referencias de paquetes

```bash
dotnet list "{path}/src/{name}.domain/{name}.domain.csproj" package
```

**Debe incluir:**

```
FluentValidation
```

## Siguientes Pasos

Una vez completada la capa de dominio, el proyecto está listo para continuar con los siguientes componentes:

- **03-infrastructure-filtering.md** - Sistema de filtrado avanzado (Milestone 2)
- **04-infrastructure-repositories.md** - Implementación de repositorios (Milestone 2)

## Notas Adicionales

### Principios de Clean Architecture

Esta capa de dominio sigue los principios de Clean Architecture:

✅ **Independencia de frameworks:** No depende de ningún framework específico
✅ **Independencia de UI:** No tiene referencias a capas de presentación
✅ **Independencia de BD:** No tiene código específico de base de datos
✅ **Independencia de agentes externos:** Puro código de negocio
✅ **Testeable:** Puede probarse sin infraestructura externa

### Patrón Repository

El patrón Repository implementado proporciona:

- **Abstracción del acceso a datos:** El dominio no conoce cómo se persisten los datos
- **Separación de lectura/escritura:** `IReadOnlyRepository` vs `IRepository`
- **Operaciones paginadas:** Con filtrado y ordenamiento
- **Soporte async:** Todos los métodos tienen versión asíncrona

### Validaciones con FluentValidation

Las entidades pueden definir sus propias validaciones heredando de `AbstractDomainObject` y sobreescribiendo `GetValidator()`:

```csharp
public class Usuario : AbstractDomainObject
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public override IValidator? GetValidator()
    {
        return new UsuarioValidator();
    }
}

public class UsuarioValidator : AbstractValidator<Usuario>
{
    public UsuarioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

## Troubleshooting

### Problema: "Package FluentValidation could not be found"

**Solución:** Verificar que `Directory.Packages.props` incluya FluentValidation y esté en la raíz de la solución.

### Problema: Errores de compilación con versiones de paquetes

**Solución:** Verificar que en el .csproj de tests se hayan removido todos los atributos `Version` de los `PackageReference`.

### Problema: Tests no se descubren en el Test Explorer

**Solución:**
- Hacer rebuild de la solución
- Verificar que NUnit3TestAdapter esté instalado
- Reiniciar el IDE
