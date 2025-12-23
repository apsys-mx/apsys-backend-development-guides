# 02 - Capa de Dominio (Domain Layer)

## Descripción

Crea la **capa de dominio** del proyecto. Esta capa contiene:
- Entidades de dominio (objetos de negocio con reglas y validaciones)
- Excepciones de dominio
- Proyecto de tests unitarios

Esta capa es **independiente de la infraestructura** y de cualquier base de datos.

**Requiere:** [01-estructura-base.md](./01-estructura-base.md)

## Estructura Final

```
src/{ProjectName}.domain/
├── {ProjectName}.domain.csproj
├── entities/
│   └── AbstractDomainObject.cs
└── exceptions/
    ├── InvalidDomainException.cs
    └── InvalidFilterArgumentException.cs

tests/{ProjectName}.domain.tests/
├── {ProjectName}.domain.tests.csproj
└── entities/
    └── DomainTestBase.cs
```

## Paquetes NuGet

**Domain:**
- `FluentValidation` - Validación de entidades

**Tests:**
- `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk` (incluidos en template)
- `AutoFixture.AutoMoq` - Generación de datos de prueba
- `FluentAssertions` - Aserciones fluidas
- `Castle.Core` - Evita warnings de Moq

## Pasos

### 1. Crear proyecto domain

```bash
dotnet new classlib -n {ProjectName}.domain -o src/{ProjectName}.domain
dotnet sln add src/{ProjectName}.domain/{ProjectName}.domain.csproj
rm src/{ProjectName}.domain/Class1.cs
```

### 2. Instalar FluentValidation

```bash
dotnet add src/{ProjectName}.domain/{ProjectName}.domain.csproj package FluentValidation
```

### 3. Crear proyecto de tests

```bash
dotnet new nunit -n {ProjectName}.domain.tests -o tests/{ProjectName}.domain.tests
dotnet sln add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj
rm tests/{ProjectName}.domain.tests/UnitTest1.cs
```

### 4. Remover versiones en .csproj de tests

Editar `tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj` y eliminar atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

### 5. Instalar paquetes en tests

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj package Castle.Core
```

### 6. Agregar referencia a domain

```bash
dotnet add tests/{ProjectName}.domain.tests/{ProjectName}.domain.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

### 7. Crear carpetas

```bash
mkdir src/{ProjectName}.domain/entities
mkdir src/{ProjectName}.domain/exceptions
mkdir tests/{ProjectName}.domain.tests/entities
```

### 8. Copiar templates

Copiar desde `templates/domain/` a `src/{ProjectName}.domain/`:
- `entities/AbstractDomainObject.cs`
- `exceptions/InvalidDomainException.cs`
- `exceptions/InvalidFilterArgumentException.cs`

Copiar desde `templates/domain.tests/` a `tests/{ProjectName}.domain.tests/`:
- `entities/DomainTestBase.cs`

**Reemplazar `{ProjectName}`** en namespaces con el nombre real del proyecto.

## Templates

| Archivo | Propósito |
|---------|-----------|
| `AbstractDomainObject.cs` | Clase base para entidades. Proporciona Id, CreationDate y validación con FluentValidation. |
| `InvalidDomainException.cs` | Excepción para validaciones de dominio fallidas. |
| `InvalidFilterArgumentException.cs` | Excepción para argumentos de filtrado inválidos. |
| `DomainTestBase.cs` | Clase base para tests. Configura AutoFixture con OmitOnRecursionBehavior. |

## Verificación

```bash
dotnet build
dotnet test
```

## Ejemplo: Validaciones con FluentValidation

```csharp
public class Usuario : AbstractDomainObject
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public override IValidator? GetValidator() => new UsuarioValidator();
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

## Siguiente Paso

→ [03-application-layer.md](./03-application-layer.md)
