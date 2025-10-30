# 03 - Capa de Aplicación (Application Layer)

> **Versión:** 1.2.0 | **Última actualización:** 2025-01-30 | **Estado:** Estable

## Descripción

Este documento describe cómo crear la **capa de aplicación (Application Layer)** de un proyecto backend con Clean Architecture para APSYS. Esta capa contiene:

- **Casos de uso**: Lógica de aplicación y orquestación de operaciones
- **DTOs**: Objetos de transferencia de datos para comunicación entre capas
- **Validadores**: Validaciones específicas de la aplicación (complementan validaciones de dominio)
- **Endpoints**: Definiciones de FastEndpoints para la API REST

## Dependencias

- ✅ **Requiere:** [01-estructura-base.md](01-estructura-base.md) completado
- ✅ **Requiere:** [02-domain-layer.md](02-domain-layer.md) completado
- ⚠️ **NO requiere:** Infrastructure (Application es independiente de la implementación)

## Validaciones Previas

Antes de ejecutar los comandos, verifica:

1. ✅ SDK de .NET 9.0 instalado: `dotnet --version`
2. ✅ Proyecto Domain existe: verificar `src/{ProjectName}.domain/`
3. ✅ Archivo `{ProjectName}.sln` existe en la raíz

## Pasos de Construcción

### Paso 1: Crear proyecto classlib para application

```bash
mkdir src/{ProjectName}.application
dotnet new classlib -n {ProjectName}.application -o src/{ProjectName}.application
dotnet sln add src/{ProjectName}.application/{ProjectName}.application.csproj
```

> Esto crea el proyecto de clase library para la capa de aplicación.

### Paso 2: Eliminar archivo Class1.cs autogenerado

```bash
rm src/{ProjectName}.application/Class1.cs
```

### Paso 3: Instalar paquetes NuGet en application

```bash
dotnet add src/{ProjectName}.application/{ProjectName}.application.csproj package FastEndpoints
```

> **FastEndpoints** se usa para definir endpoints de API REST de manera declarativa y con alta performance.

### Paso 4: Agregar referencia al proyecto domain

```bash
dotnet add src/{ProjectName}.application/{ProjectName}.application.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
```

> Application depende **únicamente** de Domain. NO debe referenciar Infrastructure ni WebApi.

### Paso 5: Crear estructura de carpetas de application

```bash
mkdir src/{ProjectName}.application/usecases
mkdir src/{ProjectName}.application/dtos
mkdir src/{ProjectName}.application/validators
mkdir src/{ProjectName}.application/endpoints
```

> Esta estructura organiza el código por tipo:
> - `usecases/`: Casos de uso de la aplicación
> - `dtos/`: Data Transfer Objects
> - `validators/`: Validadores de aplicación con FluentValidation
> - `endpoints/`: Definiciones de FastEndpoints

### Paso 6: Crear proyecto de tests para application

```bash
mkdir tests/{ProjectName}.application.tests
dotnet new nunit -n {ProjectName}.application.tests -o tests/{ProjectName}.application.tests
dotnet sln add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj
```

> Proyecto de pruebas para la capa de aplicación.

### Paso 7: Remover versiones de paquetes en .csproj de tests

**⚠️ IMPORTANTE:** El template de NUnit genera referencias de paquetes con versiones explícitas. Debes removerlas porque usamos gestión centralizada.

Edita el archivo `tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj` y elimina todos los atributos `Version`:

```xml
<!-- Cambiar esto: -->
<PackageReference Include="NUnit" Version="4.2.2" />

<!-- A esto: -->
<PackageReference Include="NUnit" />
```

> Haz lo mismo para todos los `PackageReference` en el archivo.

### Paso 8: Instalar paquetes NuGet adicionales en tests

```bash
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package AutoFixture.AutoMoq
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package FluentAssertions
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package Castle.Core
```

> - **AutoFixture.AutoMoq**: Generación automática de datos de prueba con mocks
> - **FluentAssertions**: Assertions expresivas para tests
> - **Castle.Core**: Para evitar warnings de dependencias transitivas de Moq

### Paso 9: Agregar referencias en tests

```bash
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj reference src/{ProjectName}.domain/{ProjectName}.domain.csproj
dotnet add tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj reference src/{ProjectName}.application/{ProjectName}.application.csproj
```

> Los tests de aplicación referencian tanto Domain como Application.

### Paso 10: Crear estructura de carpetas de tests

```bash
mkdir tests/{ProjectName}.application.tests/usecases
mkdir tests/{ProjectName}.application.tests/validators
mkdir tests/{ProjectName}.application.tests/endpoints
```

### Paso 11: Eliminar archivo de test autogenerado

```bash
rm tests/{ProjectName}.application.tests/UnitTest1.cs
```

### Paso 12: Copiar archivos de tests desde templates

**📁 COPIAR DIRECTORIO COMPLETO:** `templates/application.tests/` → `tests/{ProjectName}.application.tests/`

> El servidor MCP debe:
> 1. Descargar todos los archivos desde `templates/application.tests/` en el repositorio de GitHub
> 2. Copiarlos a `tests/{ProjectName}.application.tests/` respetando la estructura de carpetas
> 3. **Reemplazar** el placeholder `{ProjectName}` con el nombre real del proyecto en todos los archivos

**Archivos que se copiarán:**
- `ApplicationTestBase.cs`

## Referencia de Templates

### Archivos de Tests

| Archivo | Propósito |
|---------|-----------|
| **ApplicationTestBase.cs** | Clase base abstracta para tests de aplicación. Configura AutoFixture con AutoMoq para generar mocks automáticamente y manejo de recursión. |

> **Nota:** Todos los archivos usan el placeholder `{ProjectName}` en sus namespaces, que el servidor MCP debe reemplazar con el nombre real del proyecto.

## Principios de la Capa de Aplicación

### 1. Independencia de Infraestructura

La capa de aplicación **NO debe conocer** detalles de implementación:
- ❌ NO importar NHibernate
- ❌ NO referenciar proyectos de infraestructura
- ✅ SÍ trabajar solo con interfaces del dominio (IRepository, IUnitOfWork)

```csharp
// ✅ CORRECTO
public class GetUserByIdUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
}

// ❌ INCORRECTO
public class GetUserByIdUseCase
{
    private readonly NHUserRepository _userRepository; // ¡NO!
    private readonly ISession _session; // ¡NO!
}
```

### 2. Casos de Uso

Cada caso de uso representa una operación de negocio:
- Un archivo por caso de uso
- Nombre descriptivo: `CreateUserUseCase`, `UpdateProductUseCase`
- Recibe dependencias por constructor (IRepository, IUnitOfWork)
- Retorna DTOs, no entidades de dominio directamente

### 3. DTOs (Data Transfer Objects)

Los DTOs transfieren datos entre capas:
- Records de C# para inmutabilidad
- Solo propiedades, sin lógica
- Usados en endpoints y respuestas

```csharp
namespace {ProjectName}.application.dtos;

public record UserDto(
    int Id,
    string Name,
    string Email,
    DateTime CreatedAt
);
```

### 4. Validadores de Aplicación

Complementan las validaciones de dominio:
- Validaciones de reglas de negocio complejas
- Validaciones que requieren consultar repositorios
- FluentValidation para expresividad

```csharp
namespace {ProjectName}.application.validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

### 5. Endpoints con FastEndpoints

Los endpoints se definen en Application:
- Heredan de `Endpoint<TRequest, TResponse>`
- Configure() define ruta HTTP
- HandleAsync() ejecuta la lógica (llama a casos de uso)

```csharp
namespace {ProjectName}.application.endpoints.users;

public class GetUserEndpoint : Endpoint<GetUserRequest, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserEndpoint(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override void Configure()
    {
        Get("/api/users/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
    {
        var user = await _userRepository.GetAsync(req.Id, ct);

        if (user == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.CreationDate
        ), cancellation: ct);
    }
}
```

## Verificación

### 1. Compilar la solución

```bash
dotnet build
```

> Debería mostrar: "Build succeeded. 0 Warning(s). 0 Error(s)."

### 2. Verificar estructura de carpetas

```bash
ls -R src/{ProjectName}.application
```

Deberías ver:
- `usecases/` (vacío por ahora)
- `dtos/` (vacío por ahora)
- `validators/` (vacío por ahora)
- `endpoints/` (vacío por ahora)

> **Nota:** Estas carpetas están vacías porque los casos de uso son específicos de cada proyecto. Los desarrolladores crearán los archivos según sus necesidades.

### 3. Verificar referencias del proyecto

```bash
dotnet list src/{ProjectName}.application/{ProjectName}.application.csproj reference
```

Debería mostrar:
- `src/{ProjectName}.domain/{ProjectName}.domain.csproj`

> Application solo debe referenciar Domain.

### 4. Verificar paquetes instalados

```bash
dotnet list src/{ProjectName}.application/{ProjectName}.application.csproj package
```

Debería mostrar:
- `FastEndpoints`

### 5. Verificar proyecto de tests

```bash
dotnet list tests/{ProjectName}.application.tests/{ProjectName}.application.tests.csproj package
```

Debería incluir:
- `NUnit`
- `Microsoft.NET.Test.Sdk`
- `NUnit3TestAdapter`
- `AutoFixture.AutoMoq`
- `FluentAssertions`
- `Castle.Core`

## Próximos Pasos

Una vez completada la capa de aplicación, los siguientes pasos son:

1. **Infrastructure Layer** - Implementar repositorios con NHibernate
2. **WebApi** - Configurar FastEndpoints, DI, y middleware
3. **Migrations** - Crear esquema de base de datos con FluentMigrator

## Notas Importantes

### Testing con Mocks

Los tests de Application usan mocks de repositorios:

```csharp
[Test]
public void GetUserById_UserExists_ReturnsUser()
{
    // Arrange
    var mockRepository = fixture.Freeze<Mock<IUserRepository>>();
    mockRepository
        .Setup(x => x.Get(1))
        .Returns(new User { Id = 1, Name = "John" });

    var useCase = fixture.Create<GetUserByIdUseCase>();

    // Act
    var result = useCase.Execute(1);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be("John");
}
```

### Sin Template de Casos de Uso

No incluimos templates de casos de uso porque:
- Cada proyecto tiene casos de uso únicos
- La estructura depende de los requisitos del negocio
- Los desarrolladores deben diseñar según sus necesidades

### FastEndpoints vs Controllers

APSYS usa FastEndpoints en lugar de Controllers tradicionales:
- ✅ Mejor performance
- ✅ Menos boilerplate
- ✅ Validación integrada
- ✅ Mejor organización (un archivo por endpoint)

---

> **Guía:** 03-application-layer.md
> **Milestone:** 2 - Application Layer
> **Siguiente:** 04-infrastructure-filtering.md
