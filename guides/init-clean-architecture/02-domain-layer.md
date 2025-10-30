# 02 - Capa de Dominio (Domain Layer)

> **Versión:** 1.1.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

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

## Estructura de Archivos a Crear

```
./
├── src/
│   └── {ProjectName}.domain/
│       ├── {ProjectName}.domain.csproj
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
    └── {ProjectName}.domain.tests/
        ├── {ProjectName}.domain.tests.csproj
        └── entities/
            └── DomainTestBase.cs
```

> **Ejemplo:** Para el proyecto "InventorySystem":
> ```
> ./
> ├── src/
> │   └── InventorySystem.domain/
> └── tests/
>     └── InventorySystem.domain.tests/
> ```

## Paquetes NuGet Requeridos

### Para el proyecto source (domain):
- `FluentValidation` - Validación de entidades

### Para el proyecto de tests (domain.tests):
- `NUnit` - Framework de testing (incluido en template)
- `Microsoft.NET.Test.Sdk` - SDK de testing (incluido en template)
- `NUnit3TestAdapter` - Adaptador de NUnit (incluido en template)
- `AutoFixture.AutoMoq` - Generación automática de datos de prueba
- `FluentAssertions` - Aserciones fluidas para tests
- `Castle.Core` - Dependencia de Moq (previene warnings de versiones)

## Proceso de Construcción

> **Nota:** Los placeholders como `{ProjectName}` serán reemplazados automáticamente por el servidor MCP con el nombre real de tu proyecto.

### Paso 1: Crear proyecto domain

```bash
dotnet new classlib -n {ProjectName}.domain -o src/{ProjectName}.domain
dotnet sln add src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

> Esto crea un proyecto de biblioteca de clases para la capa de dominio y lo agrega a la solución.

### Paso 2: Eliminar archivo Class1.cs autogenerado

```bash
rm src/{ProjectName}.domain/Class1.cs
```

### Paso 3: Instalar paquetes NuGet en domain

```bash
dotnet add src/{ProjectName}.domain/{ProjectName}.domain.csproj package FluentValidation
```

> FluentValidation se usa para validaciones de entidades de dominio.

### Paso 4: Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.domain.tests -o tests/{ProjectName}.domain.tests
dotnet sln add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj
```

> Esto crea un proyecto de pruebas con NUnit.

### Paso 5: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** El template de NUnit genera referencias de paquetes con versiones explícitas. Debes removerlas porque usamos gestión centralizada.

Edita el archivo `tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj` y elimina todos los atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

> Haz lo mismo para todos los `PackageReference` en el archivo.

### Paso 6: Instalar paquetes NuGet adicionales en tests

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package Castle.Core
```

> **Nota:** Castle.Core se agrega explícitamente para evitar warnings de dependencias transitivas de Moq.

### Paso 7: Agregar referencia al proyecto domain en tests

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

> Esto permite que los tests accedan a las clases del dominio.

### Paso 8: Crear estructura de carpetas del domain

```bash
mkdir src/{ProjectName}.domain/entities
mkdir src/{ProjectName}.domain/exceptions
mkdir src/{ProjectName}.domain/interfaces
mkdir src/{ProjectName}.domain/interfaces/repositories
mkdir tests/{ProjectName}.domain.tests/entities
```

### Paso 9: Eliminar archivo de test autogenerado

```bash
rm tests/{ProjectName}.domain.tests/UnitTest1.cs
```

### Paso 10: Copiar archivos de código desde templates

**📁 COPIAR DIRECTORIO COMPLETO:** `templates/domain/` → `src/{ProjectName}.domain/`

> El servidor MCP debe:
> 1. Descargar todos los archivos desde `templates/domain/` en el repositorio de GitHub
> 2. Copiarlos a `src/{ProjectName}.domain/` respetando la estructura de carpetas
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto en todos los archivos

**Archivos que se copiarán:**
- `entities/AbstractDomainObject.cs`
- `exceptions/InvalidDomainException.cs`
- `exceptions/InvalidFilterArgumentException.cs`
- `interfaces/repositories/IRepository.cs`
- `interfaces/repositories/IReadOnlyRepository.cs`
- `interfaces/repositories/IUnitOfWork.cs`
- `interfaces/repositories/GetManyAndCountResult.cs`
- `interfaces/repositories/SortingCriteria.cs`
- `interfaces/repositories/IGetManyAndCountResultWithSorting.cs`

### Paso 11: Copiar archivos de tests desde templates

**📁 COPIAR DIRECTORIO COMPLETO:** `templates/domain.tests/` → `tests/{ProjectName}.domain.tests/`

> El servidor MCP debe:
> 1. Descargar todos los archivos desde `templates/domain.tests/` en el repositorio de GitHub
> 2. Copiarlos a `tests/{ProjectName}.domain.tests/` respetando la estructura de carpetas
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto en todos los archivos

**Archivos que se copiarán:**
- `entities/DomainTestBase.cs`

## Referencia de Templates

> Los templates están en el directorio `templates/` del repositorio de GitHub.
> Para ver el código completo de cada archivo, consulta directamente los archivos en `templates/domain/` y `templates/domain.tests/`.

### Archivos del Domain

| Archivo | Propósito |
|---------|-----------|
| **entities/AbstractDomainObject.cs** | Clase base abstracta para todas las entidades de dominio. Proporciona propiedades comunes (Id, CreationDate) y métodos de validación integrados con FluentValidation. |
| **exceptions/InvalidDomainException.cs** | Excepción lanzada cuando una entidad de dominio no cumple con sus reglas de validación. |
| **exceptions/InvalidFilterArgumentException.cs** | Excepción lanzada cuando los argumentos de filtrado (queries) son inválidos. |
| **interfaces/repositories/IRepository.cs** | Interfaz genérica para operaciones de escritura en repositorios (Add, Save, Delete) con documentación XML completa. |
| **interfaces/repositories/IReadOnlyRepository.cs** | Interfaz genérica para operaciones de solo lectura con soporte para Expression queries, paginación, Count, GetManyAndCount y CancellationToken. |
| **interfaces/repositories/IUnitOfWork.cs** | Patrón Unit of Work para gestionar transacciones (BeginTransaction, Commit, Rollback, ResetTransaction, IsActiveTransaction). |
| **interfaces/repositories/GetManyAndCountResult.cs** | Clase para resultados paginados con Items, Count (long), PageNumber, PageSize, Sorting y constructores completos. |
| **interfaces/repositories/SortingCriteria.cs** | Clase para criterios de ordenamiento con SortBy (string) y Criteria (enum Ascending/Descending) con múltiples constructores. |
| **interfaces/repositories/IGetManyAndCountResultWithSorting.cs** | Interfaz simple que expone una property Sorting para objetos con capacidades de ordenamiento. |

### Archivos de Tests

| Archivo | Propósito |
|---------|-----------|
| **entities/DomainTestBase.cs** | Clase base para tests de entidades de dominio, proporciona setup común con NUnit. |

> **Nota:** Todos los archivos usan el placeholder `{ProjectName}` en sus namespaces, que el servidor MCP debe reemplazar con el nombre real del proyecto.

## Verificación

### 1. Compilar la solución

```bash
dotnet build
```

> Debería mostrar: "Build succeeded. 0 Warning(s). 0 Error(s)."

### 2. Verificar estructura de carpetas

```bash
ls -R src/{ProjectName}.domain
```

Deberías ver:
- `entities/AbstractDomainObject.cs`
- `exceptions/InvalidDomainException.cs`
- `exceptions/InvalidFilterArgumentException.cs`
- `interfaces/repositories/` con todas las interfaces

> **Ejemplo:** Para el proyecto "InventorySystem":
> ```
> src/InventorySystem.domain/
> ├── entities/
> │   └── AbstractDomainObject.cs
> ├── exceptions/
> │   ├── InvalidDomainException.cs
> │   └── InvalidFilterArgumentException.cs
> └── interfaces/
>     └── repositories/
>         ├── IRepository.cs
>         ├── IReadOnlyRepository.cs
>         ├── IUnitOfWork.cs
>         ├── IGetManyAndCountResultWithSorting.cs
>         ├── GetManyAndCountResult.cs
>         └── SortingCriteria.cs
> ```

### 3. Ejecutar tests

```bash
dotnet test
```

> Debería mostrar: "Passed! - Failed: 0, Passed: 1"

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
