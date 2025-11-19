# Manual de Construcción de Proyecto Base - APSYS

## Tabla de Contenidos

1. [Descripción General](#descripción-general)
2. [Parámetros de Entrada](#parámetros-de-entrada)
3. [Estructura Final del Proyecto](#estructura-final-del-proyecto)
4. [Proceso de Construcción Paso a Paso](#proceso-de-construcción-paso-a-paso)
5. [Detalles de Cada Proyecto](#detalles-de-cada-proyecto)
6. [Referencias Completas de Paquetes NuGet](#referencias-completas-de-paquetes-nuget)
7. [Templates y Archivos Generados](#templates-y-archivos-generados)

---

## Descripción General

Este documento describe el proceso completo de creación manual de un proyecto backend con Clean Architecture para APSYS. El CLI `apsys.builder` automatiza todos estos pasos.

El proyecto sigue una arquitectura limpia (Clean Architecture) con las siguientes capas:

- **Domain**: Lógica de negocio y entidades de dominio
- **Application**: Casos de uso y lógica de aplicación
- **Infrastructure**: Implementaciones de infraestructura (repositorios, ORM)
- **WebApi**: Capa de presentación (API REST con FastEndpoints)

Además incluye:

- **Migrations**: Sistema de migraciones de base de datos con FluentMigrator
- **Tests**: Proyectos de pruebas para cada capa
- **Scenarios**: Herramienta para generar escenarios de datos de prueba
- **NDbUnit**: Utilidad para gestión de datos de prueba

---

## Parámetros de Entrada

El CLI requiere los siguientes parámetros:

| Parámetro   | Descripción                  | Ejemplo                     |
| ----------- | ---------------------------- | --------------------------- |
| `--name`    | Nombre de la solución        | `MiProyecto`                |
| `--version` | Versión de .NET              | `net9.0`                    |
| `--path`    | Ruta donde crear el proyecto | `C:\projects\miproyecto`    |
| `--db`      | Tipo de base de datos        | `PostgreSQL` o `SQL Server` |

**Ejemplo de comando:**

```bash
apsys.builder init --name=MiProyecto --version=net9.0 --path=C:\projects\miproyecto --db=PostgreSQL
```

---

## Estructura Final del Proyecto

```
MiProyecto/
├── MiProyecto.sln
├── Directory.Packages.props
├── src/
│   ├── MiProyecto.domain/
│   ├── MiProyecto.application/
│   ├── MiProyecto.infrastructure/
│   ├── MiProyecto.webapi/
│   └── MiProyecto.migrations/
└── tests/
    ├── MiProyecto.domain.tests/
    ├── MiProyecto.application.tests/
    ├── MiProyecto.infrastructure.tests/
    ├── MiProyecto.webapi.tests/
    ├── MiProyecto.ndbunit/
    ├── MiProyecto.common.tests/
    └── MiProyecto.scenarios/
```

---

## Proceso de Construcción Paso a Paso

### PASO 1: Crear la Solución Base

#### 1.1 Crear estructura de carpetas

```bash
mkdir "C:\projects\miproyecto"
cd "C:\projects\miproyecto"
mkdir src
mkdir tests
```

#### 1.2 Crear archivo de solución

```bash
dotnet new sln -n MiProyecto -o "C:\projects\miproyecto"
```

#### 1.3 Crear archivo Directory.Packages.props

Crear el archivo `Directory.Packages.props` en la raíz de la solución:

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `Directory.Packages.props` | [Ver template](../templates/manual/paso-01-solucion/Directory.Packages.props) | Gestión centralizada de paquetes NuGet |

**Propósito**: Este archivo habilita la gestión centralizada de paquetes NuGet. Todas las versiones de paquetes se definen aquí una sola vez, y los proyectos solo referencian el nombre del paquete sin especificar versión.

---

### PASO 2: Crear Proyecto Migrations

#### 2.1 Crear proyecto console

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.migrations"
dotnet new console -n MiProyecto.migrations -o "C:\projects\miproyecto\src\MiProyecto.migrations"
```

#### 2.2 Agregar a la solución

```bash
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj"
```

#### 2.3 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package FluentMigrator
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package FluentMigrator.Runner
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package Microsoft.Extensions.DependencyInjection
dotnet add "C:\projects\miproyecto\src\MiProyecto.migrations\MiProyecto.migrations.csproj" package Spectre.Console
```

#### 2.4 Crear archivos del proyecto

Crear los siguientes archivos en `src/MiProyecto.migrations/`:

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `Program.cs` | [Ver template](../templates/manual/paso-02-migrations/Program.cs) | Punto de entrada para ejecutar migraciones |
| `CommandLineArgs.cs` | [Ver template](../templates/manual/paso-02-migrations/CommandLineArgs.cs) | Parser de argumentos y códigos de salida |
| `M001Sandbox.cs` | [Ver template](../templates/manual/paso-02-migrations/M001Sandbox.cs) | Migración inicial de ejemplo |

> **NOTA**: El template usa `AddPostgres11_0()` para PostgreSQL. Para SQL Server, cambiar a `AddSqlServer()` en el método `CreateServices`.

---

### PASO 3: Crear Proyecto Domain

#### 3.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.domain"
dotnet new classlib -n MiProyecto.domain -o "C:\projects\miproyecto\src\MiProyecto.domain"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 3.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj" package FluentValidation
```

#### 3.3 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.domain.tests"
dotnet new nunit -n MiProyecto.domain.tests -o "C:\projects\miproyecto\tests\MiProyecto.domain.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj"
```

#### 3.4 Remover versiones de paquetes en .csproj de tests

**IMPORTANTE**: Editar `tests/MiProyecto.domain.tests/MiProyecto.domain.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference` (porque usamos gestión centralizada con `Directory.Packages.props`).

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 3.5 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" package FluentAssertions
```

#### 3.6 Agregar referencia al proyecto domain en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.domain.tests\MiProyecto.domain.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 3.7 Crear estructura de carpetas y archivos del dominio

**Estructura de carpetas a crear en `src/MiProyecto.domain/`:**

```
entities/
exceptions/
interfaces/
  └── repositories/
```

**Archivos de entidades:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `entities/AbstractDomainObject.cs` | [Ver template](../templates/manual/paso-03-domain/entities/AbstractDomainObject.cs) | Clase base con validación FluentValidation |

**Archivos de excepciones:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `exceptions/InvalidDomainException.cs` | [Ver template](../templates/manual/paso-03-domain/exceptions/InvalidDomainException.cs) | Excepción para errores de validación de dominio |
| `exceptions/InvalidFilterArgumentException.cs` | [Ver template](../templates/manual/paso-03-domain/exceptions/InvalidFilterArgumentException.cs) | Excepción para argumentos de filtro inválidos |

**Archivos de interfaces de repositorios:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `interfaces/repositories/IRepository.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/IRepository.cs) | Interface genérica para repositorios CRUD |
| `interfaces/repositories/IReadOnlyRepository.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/IReadOnlyRepository.cs) | Interface para repositorios de solo lectura |
| `interfaces/repositories/IUnitOfWork.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/IUnitOfWork.cs) | Interface Unit of Work con manejo de transacciones |
| `interfaces/repositories/GetManyAndCountResult.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/GetManyAndCountResult.cs) | Clase para resultados paginados |
| `interfaces/repositories/SortingCriteria.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/SortingCriteria.cs) | Criterios de ordenamiento |
| `interfaces/repositories/IGetManyAndCountResultWithSorting.cs` | [Ver template](../templates/manual/paso-03-domain/interfaces/repositories/IGetManyAndCountResultWithSorting.cs) | Interface para resultados con sorting |

> **NOTA**: El template de `IUnitOfWork` incluye ejemplos de repositorios (Roles, Users). Debes agregar los repositorios específicos de tu proyecto.

**Archivo de test:**

```csharp
// tests/MiProyecto.domain.tests/entities/DomainTestBase.cs
namespace MiProyecto.domain.tests.entities;

public class DomainTestBase
{
    [SetUp]
    public void Setup()
    {
    }
}
```

---

### PASO 4: Crear Proyecto NDbUnit

#### 4.1 Crear proyecto classlib

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.ndbunit"
dotnet new classlib -n MiProyecto.ndbunit -o "C:\projects\miproyecto\tests\MiProyecto.ndbunit"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
```

#### 4.2 Instalar paquetes NuGet según base de datos

**Para PostgreSQL:**

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj" package Npgsql
```

**Para SQL Server:**

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj" package Microsoft.Data.SqlClient
```

#### 4.3 Crear archivos

Crear los siguientes archivos en `tests/MiProyecto.ndbunit/`:

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `INDbUnit.cs` | [Ver template](../templates/manual/paso-04-ndbunit/INDbUnit.cs) | Interface que define las operaciones de NDbUnit |
| `NDbUnit.cs` | [Ver template](../templates/manual/paso-04-ndbunit/NDbUnit.cs) | Clase abstracta base con la implementación común |
| `PostgreSQLNDbUnit.cs` | [Ver template](../templates/manual/paso-04-ndbunit/PostgreSQLNDbUnit.cs) | Implementación para PostgreSQL |
| `SqlServerNDbUnit.cs` | [Ver template](../templates/manual/paso-04-ndbunit/SqlServerNDbUnit.cs) | Implementación para SQL Server |

**NOTA**: Solo debes crear el archivo correspondiente a la base de datos seleccionada (PostgreSQL o SQL Server), no ambos.

**Características principales de NDbUnit:**
- Usa `DataSet` y `connectionString` en el constructor
- Operaciones transaccionales con rollback automático
- Manejo de constraints (disable/enable triggers)
- Métodos: `ClearDatabase()`, `GetDataSetFromDb()`, `SeedDatabase(DataSet)`

---

### PASO 5: Crear Proyecto Common Tests

#### 5.1 Crear proyecto classlib

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.common.tests"
dotnet new classlib -n MiProyecto.common.tests -o "C:\projects\miproyecto\tests\MiProyecto.common.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj"
```

#### 5.2 Agregar referencia al proyecto domain

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 5.3 Crear archivos de esquema de base de datos

Crear los siguientes archivos en `tests/MiProyecto.common.tests/`:

**AppSchema.xsd** - Esquema XSD para definir estructura de datos de prueba
**AppSchema.xsc** - Archivo de configuración del esquema
**AppSchema.xss** - Archivo de diseño del esquema
**AppSchema.Designer.cs** - Clase generada para el esquema

Estos archivos permiten trabajar con datasets tipados en .NET para las pruebas.

#### 5.4 Crear AppSchemaExtender

Este archivo proporciona métodos de extensión para facilitar el acceso a tablas y filas del DataSet en las pruebas de integración.

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `AppSchemaExtender.cs` | [Ver template](../templates/manual/paso-05-common-tests/AppSchemaExtender.cs) | Métodos de extensión para acceso a DataSet |

**Métodos que incluye:**
- `GetXxxTable()` - Obtiene una tabla específica del DataSet
- `GetXxxRows(filterExpression)` - Obtiene filas filtradas
- `GetFirstXxxRow()` - Obtiene la primera fila de una tabla

**NOTA**: Este archivo debe ser extendido con métodos para cada tabla que se agregue al AppSchema. Es fundamental para las pruebas de integración ya que permite acceder fácilmente a los datos del DataSet para verificar resultados en el Assert.

---

### PASO 6: Crear Proyecto Application

#### 6.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.application"
dotnet new classlib -n MiProyecto.application -o "C:\projects\miproyecto\src\MiProyecto.application"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj"
```

#### 6.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj" package FastEndpoints
```

#### 6.3 Agregar referencia al proyecto domain

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

#### 6.4 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.application.tests"
dotnet new nunit -n MiProyecto.application.tests -o "C:\projects\miproyecto\tests\MiProyecto.application.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj"
```

#### 6.5 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.application.tests/MiProyecto.application.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 6.6 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" package FluentAssertions
```

#### 6.7 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.application.tests\MiProyecto.application.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.application\MiProyecto.application.csproj"
```

---

### PASO 7: Crear Proyecto Infrastructure

#### 7.1 Crear proyecto classlib para source

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.infrastructure"
dotnet new classlib -n MiProyecto.infrastructure -o "C:\projects\miproyecto\src\MiProyecto.infrastructure"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 7.2 Instalar paquetes NuGet en source

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package FluentValidation
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package NHibernate
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package System.Linq.Dynamic.Core
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" package Microsoft.AspNetCore.WebUtilities
```

#### 7.3 Agregar referencias al proyecto

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
```

**NOTA**: Las referencias a `ndbunit` y `common.tests` solo son necesarias en los proyectos de prueba, no en infrastructure.

#### 7.4 Crear estructura de carpetas

```
src/MiProyecto.infrastructure/
└── nhibernate/
    └── filtering/
```

#### 7.5 Crear archivos de NHibernate

Crear los siguientes archivos en `src/MiProyecto.infrastructure/nhibernate/`:

**Archivos principales:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `NHRepository.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/NHRepository.cs) | Repositorio base con CRUD y validación |
| `NHReadOnlyRepository.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/NHReadOnlyRepository.cs) | Repositorio base de solo lectura con paginación |
| `NHUnitOfWork.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/NHUnitOfWork.cs) | Unit of Work con manejo de transacciones |
| `NHSessionFactory.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/NHSessionFactory.cs) | Factory para crear sesiones NHibernate |
| `ConnectionStringBuilder.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/ConnectionStringBuilder.cs) | Constructor de connection strings desde variables de entorno |
| `SortingCriteriaExtender.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/SortingCriteriaExtender.cs) | Extensiones para criterios de ordenamiento |

**Archivos de filtering** (crear en `nhibernate/filtering/`):

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `FilterExpressionParser.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/FilterExpressionParser.cs) | Parser de expresiones de filtrado dinámico |
| `FilterOperator.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/FilterOperator.cs) | Modelo de operador de filtro |
| `RelationalOperator.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/RelationalOperator.cs) | Constantes de operadores relacionales |
| `StringExtender.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/StringExtender.cs) | Extensión ToPascalCase |
| `QuickSearch.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/QuickSearch.cs) | Modelo de búsqueda rápida |
| `Sorting.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/Sorting.cs) | Modelo de criterio de ordenamiento |
| `QueryStringParser.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/QueryStringParser.cs) | Parser de query strings con paginación y filtros |
| `QueryOperations.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/QueryOperations.cs) | Operaciones auxiliares de query |
| `InvalidQueryStringArgumentException.cs` | [Ver template](../templates/manual/paso-07-infrastructure/nhibernate/filtering/InvalidQueryStringArgumentException.cs) | Excepción para argumentos inválidos |

> **NOTA**: Todos los templates están actualizados con la versión del proyecto de referencia `hashira.stone.backend`. Asegúrate de ajustar los namespaces según tu proyecto

#### 7.6 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests"
dotnet new nunit -n MiProyecto.infrastructure.tests -o "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj"
```

#### 7.7 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.infrastructure.tests/MiProyecto.infrastructure.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 7.8 Instalar paquetes NuGet en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Castle.Core
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package FluentAssertions
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package FluentValidation
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Microsoft.Extensions.Configuration.Json
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" package Microsoft.Extensions.DependencyInjection
```

#### 7.9 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.infrastructure.tests\MiProyecto.infrastructure.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 7.10 Crear archivos de test base para repositorios

Se requieren dos clases base para las pruebas de integración de repositorios:

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `NHRepositoryTestInfrastructureBase.cs` | [Ver template](../templates/manual/paso-07-infrastructure/tests/NHRepositoryTestInfrastructureBase.cs) | Infraestructura compartida (SessionFactory, NDbUnit, validators) |
| `NHRepositoryTestBase.cs` | [Ver template](../templates/manual/paso-07-infrastructure/tests/NHRepositoryTestBase.cs) | Clase genérica con repositorio bajo prueba |

**Características de NHRepositoryTestInfrastructureBase:**
- Setup de NHibernate SessionFactory
- Configuración de AutoFixture con AutoMoq
- Inicialización de NDbUnit con AppSchema
- Método `LoadScenario(string scenarioName)` para cargar datos de prueba
- Registro de validators en ServiceProvider

**Ejemplo de uso:**

```csharp
[TestFixture]
public class NHUserRepositoryTests : NHRepositoryTestBase<NHUserRepository, User, Guid>
{
    protected internal override NHUserRepository BuildRepository()
        => new NHUserRepository(_sessionFactory.OpenSession(), _serviceProvider);

    [Test]
    public void GetById_WhenUserExists_ReturnsUser()
    {
        // Arrange
        this.LoadScenario("CreateUsers");
        var dataSet = this.nDbUnitTest.GetDataSetFromDb();
        var userId = dataSet.GetFirstUserRow().Field<Guid>("id");

        // Act
        var result = this.RepositoryUnderTest.Get(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
    }
}
```

**IMPORTANTE**:
- Crear archivo `.env` con `SCENARIOS_FOLDER_PATH` y variables de conexión a BD
- Crear `appsettings.json` con la configuración necesaria
- Registrar validators en `LoadValidators()` del template

---

### PASO 8: Crear Proyecto WebApi (Presentation)

#### 8.1 Crear proyecto webapi

```bash
mkdir "C:\projects\miproyecto\src\MiProyecto.webapi"
dotnet new webapi -n MiProyecto.webapi -o "C:\projects\miproyecto\src\MiProyecto.webapi"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj"
```

#### 8.2 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package AutoMapper
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints.Swagger
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package FastEndpoints.Security
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Microsoft.Extensions.Logging.Log4Net.AspNetCore
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" package Swashbuckle.AspNetCore
```

#### 8.3 Agregar referencias

```bash
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
```

#### 8.4 Crear estructura de carpetas

```
src/MiProyecto.webapi/
├── features/
│   └── hello/
├── infrastructure/
├── mappingprofiles/
├── dtos/
└── properties/
```

#### 8.5 Crear archivos del proyecto

Crear los siguientes archivos en `src/MiProyecto.webapi/`:

**Archivos principales:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `Program.cs` | [Ver template](../templates/manual/paso-08-webapi/Program.cs) | Configuración principal de FastEndpoints |
| `infrastructure/ServiceCollectionExtender.cs` | [Ver template](../templates/manual/paso-08-webapi/infrastructure/ServiceCollectionExtender.cs) | Extensiones de configuración de servicios (CORS, JWT, UoW, AutoMapper) |
| `properties/InternalsVisibleTo.cs` | [Ver template](../templates/manual/paso-08-webapi/properties/InternalsVisibleTo.cs) | Visibilidad para tests |

**Archivos de mapeo y DTOs:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `mappingprofiles/MappingProfile.cs` | [Ver template](../templates/manual/paso-08-webapi/mappingprofiles/MappingProfile.cs) | Perfil de AutoMapper con mapeo de resultados paginados |
| `dtos/GetManyAndCountResultDto.cs` | [Ver template](../templates/manual/paso-08-webapi/dtos/GetManyAndCountResultDto.cs) | DTO para resultados paginados |

**Endpoint de ejemplo:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `features/hello/HelloEndpoint.cs` | [Ver template](../templates/manual/paso-08-webapi/features/hello/HelloEndpoint.cs) | Endpoint de prueba |

> **NOTA**: El `ServiceCollectionExtender` incluye configuración completa de CORS con origins configurables, JWT con IdentityServer, UnitOfWork con NHibernate, y AutoMapper. Personaliza los métodos según las necesidades del proyecto.

#### 8.7 Crear proyecto de tests

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.webapi.tests"
dotnet new nunit -n MiProyecto.webapi.tests -o "C:\projects\miproyecto\tests\MiProyecto.webapi.tests"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj"
```

#### 8.8 Remover versiones de paquetes en tests

**IMPORTANTE**: Editar `tests/MiProyecto.webapi.tests/MiProyecto.webapi.tests.csproj` y **remover todos los atributos `Version`** de los `PackageReference`.

Cambiar de:

```xml
<PackageReference Include="NUnit" Version="4.2.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
```

A:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
```

#### 8.9 Instalar paquetes en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package AutoMapper
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package AutoFixture.AutoMoq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package DotNetEnv
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package FastEndpoints.Testing
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package FluentAssertions
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package Moq
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" package Microsoft.AspNetCore.Mvc.Testing
```

#### 8.10 Agregar referencias en tests

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.common.tests\MiProyecto.common.tests.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.webapi.tests\MiProyecto.webapi.tests.csproj" reference "C:\projects\miproyecto\src\MiProyecto.webapi\MiProyecto.webapi.csproj"
```

#### 8.11 Crear archivos de tests

Crear los siguientes archivos en `tests/MiProyecto.webapi.tests/`:

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `CustomWebApplicationFactory.cs` | [Ver template](../templates/manual/paso-08-webapi/tests/CustomWebApplicationFactory.cs) | Factory para crear instancia de la aplicación en tests |
| `EndpointTestBase.cs` | [Ver template](../templates/manual/paso-08-webapi/tests/EndpointTestBase.cs) | Clase base para tests de endpoints con NDbUnit y AutoFixture |
| `TestAuthHandler.cs` | [Ver template](../templates/manual/paso-08-webapi/tests/TestAuthHandler.cs) | Handler para simular autenticación en tests |

> **NOTA**: `EndpointTestBase` incluye métodos para crear clientes autenticados/no autenticados, cargar escenarios, y manejo de NDbUnit. Es la base recomendada para todos los tests de endpoints.

**Archivos en `mappingprofiles/`:**

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/AutoFixtureExtensions.cs`**

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;

namespace MiProyecto.webapi.tests.mappingprofiles;

public static class AutoFixtureExtensions
{
    public static IFixture WithoutRecursion(this IFixture fixture)
    {
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    public static IFixture WithAutoMoq(this IFixture fixture)
    {
        return fixture.Customize(new AutoMoqCustomization());
    }
}
```

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/MappingProfileTestsBase.cs`**

```csharp
using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace MiProyecto.webapi.tests.mappingprofiles;

public abstract class MappingProfileTestsBase
{
    protected internal IFixture fixture;
    protected internal MapperConfiguration configuration;
    protected internal IMapper mapper;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.fixture = new Fixture()
            .WithAutoMoq()
            .WithoutRecursion();

        var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        this.configuration = new MapperConfiguration(mc =>
        {
            ConfigureProfiles(mc);
        }, factory);
        this.mapper = this.configuration.CreateMapper();
    }

    [Test]
    public void MappingConfiguration_ShouldBeValid()
    {
        configuration.AssertConfigurationIsValid();
    }

    protected abstract void ConfigureProfiles(IMapperConfigurationExpression configuration);
}
```

**Archivo: `tests/MiProyecto.webapi.tests/mappingprofiles/MappingProfileTests.cs`**

```csharp
using AutoMapper;
using MiProyecto.webapi.mappingprofiles;

namespace MiProyecto.webapi.tests.mappingprofiles;

public class MappingProfileTests : MappingProfileTestsBase
{
    protected override void ConfigureProfiles(IMapperConfigurationExpression configuration)
        => configuration.AddProfile(new MappingProfile());
}
```

---

### PASO 9: Crear Proyecto Scenarios

El proyecto Scenarios es una aplicación de consola que genera archivos XML con datos de prueba. Estos archivos son utilizados por NDbUnit para cargar escenarios en las pruebas de integración.

#### 9.1 Crear proyecto console

```bash
mkdir "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet new console -n MiProyecto.scenarios -o "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet sln "C:\projects\miproyecto\MiProyecto.sln" add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj"
```

#### 9.2 Instalar paquetes NuGet

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Scrutor
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Spectre.Console
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Microsoft.Extensions.DependencyInjection
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" package Microsoft.Extensions.Logging
```

#### 9.3 Agregar referencias

```bash
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\src\MiProyecto.domain\MiProyecto.domain.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\src\MiProyecto.infrastructure\MiProyecto.infrastructure.csproj"
dotnet add "C:\projects\miproyecto\tests\MiProyecto.scenarios\MiProyecto.scenarios.csproj" reference "C:\projects\miproyecto\tests\MiProyecto.ndbunit\MiProyecto.ndbunit.csproj"
```

#### 9.4 Crear archivos del proyecto

Crear los siguientes archivos en `tests/MiProyecto.scenarios/`:

**Archivos principales:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `Program.cs` | [Ver template](../templates/manual/paso-09-scenarios/Program.cs) | Punto de entrada que ejecuta los escenarios |
| `IScenario.cs` | [Ver template](../templates/manual/paso-09-scenarios/IScenario.cs) | Interface que define la estructura de un escenario |
| `CommandLineArgs.cs` | [Ver template](../templates/manual/paso-09-scenarios/CommandLineArgs.cs) | Parser de argumentos de línea de comandos |
| `ExitCode.cs` | [Ver template](../templates/manual/paso-09-scenarios/ExitCode.cs) | Códigos de salida del programa |
| `ScenarioBuilder.cs` | [Ver template](../templates/manual/paso-09-scenarios/ScenarioBuilder.cs) | Constructor principal que configura DI y carga escenarios |

#### 9.5 Crear escenarios de ejemplo

**Escenarios de ejemplo:**

| Archivo | Template | Descripción |
|---------|----------|-------------|
| `Sc010CreateSandBox.cs` | [Ver template](../templates/manual/paso-09-scenarios/Sc010CreateSandBox.cs) | Escenario base vacío (sandbox) |
| `Sc020CreateRoles.cs` | [Ver template](../templates/manual/paso-09-scenarios/Sc020CreateRoles.cs) | Escenario con inyección de dependencias |
| `Sc030CreateUsers.cs` | [Ver template](../templates/manual/paso-09-scenarios/Sc030CreateUsers.cs) | Escenario que depende de roles |

> **NOTA**: Los escenarios utilizan primary constructors de C# 12 para inyección de dependencias. La convención de nombres `Sc###NombreEscenario` determina el orden de ejecución.

#### 9.7 Ejecutar el generador de escenarios

Para generar los archivos XML de escenarios:

```bash
cd "C:\projects\miproyecto\tests\MiProyecto.scenarios"
dotnet run /cnn:"Host=localhost;Port=5432;Database=miproyecto_test;Username=postgres;Password=secret" /output:"C:\projects\miproyecto\tests\scenarios"
```

Esto generará archivos XML en la carpeta especificada:
- `CreateSandBox.xml`
- `CreateRoles.xml`
- `CreateUsers.xml`

Estos archivos son utilizados por `LoadScenario()` en las pruebas de integración

---

## Detalles de Cada Proyecto

### Domain (MiProyecto.domain)

- **Tipo**: Class Library
- **Propósito**: Contiene la lógica de negocio, entidades de dominio, interfaces de repositorios y excepciones personalizadas
- **Dependencias**: FluentValidation
- **Sin dependencias de otros proyectos**

### Application (MiProyecto.application)

- **Tipo**: Class Library
- **Propósito**: Casos de uso y lógica de aplicación usando FastEndpoints
- **Dependencias**: FastEndpoints
- **Referencia a**: Domain

### Infrastructure (MiProyecto.infrastructure)

- **Tipo**: Class Library
- **Propósito**: Implementaciones de infraestructura (repositorios con NHibernate, UnitOfWork)
- **Dependencias**: FluentValidation, NHibernate, System.Linq.Dynamic.Core, Microsoft.AspNetCore.WebUtilities
- **Referencia a**: Domain, NDbUnit, Common.Tests

### WebApi (MiProyecto.webapi)

- **Tipo**: Web API
- **Propósito**: Capa de presentación, endpoints REST con FastEndpoints
- **Dependencias**: AutoMapper, DotNetEnv, FastEndpoints, FastEndpoints.Swagger, FastEndpoints.Security, JWT, Log4Net, Swashbuckle
- **Referencia a**: Domain, Infrastructure

### Migrations (MiProyecto.migrations)

- **Tipo**: Console Application
- **Propósito**: Gestión de migraciones de base de datos con FluentMigrator
- **Dependencias**: FluentMigrator, FluentMigrator.Runner, DependencyInjection, Spectre.Console
- **Sin referencias a otros proyectos**

### NDbUnit (MiProyecto.ndbunit)

- **Tipo**: Class Library
- **Propósito**: Utilidad para cargar/limpiar datos de prueba en la base de datos
- **Dependencias**: Npgsql (PostgreSQL) o Microsoft.Data.SqlClient (SQL Server)
- **Sin referencias a otros proyectos**

### Common.Tests (MiProyecto.common.tests)

- **Tipo**: Class Library
- **Propósito**: Código común compartido entre proyectos de test (schemas XSD)
- **Referencia a**: Domain

### Scenarios (MiProyecto.scenarios)

- **Tipo**: Console Application
- **Propósito**: Generador de escenarios de datos de prueba exportados a XML
- **Dependencias**: Scrutor, Spectre.Console, DependencyInjection, Logging
- **Referencia a**: Domain, Infrastructure, NDbUnit

### Proyectos de Tests

Cada capa tiene su proyecto de tests correspondiente con NUnit, AutoFixture, FluentAssertions y las referencias necesarias.

---

## Referencias Completas de Paquetes NuGet

### Versiones Centralizadas (Directory.Packages.props)

| Paquete                                         | Versión |
| ----------------------------------------------- | ------- |
| AutoFixture.AutoMoq                             | 4.18.1  |
| AutoMapper                                      | 15.0.1  |
| coverlet.collector                              | 6.0.2   |
| DotNetEnv                                       | 3.1.1   |
| FastEndpoints                                   | 7.0.1   |
| FastEndpoints.Security                          | 7.0.1   |
| FastEndpoints.Swagger                           | 7.0.1   |
| FastEndpoints.Testing                           | 7.0.1   |
| FluentAssertions                                | 8.5.0   |
| FluentMigrator                                  | 7.1.0   |
| FluentMigrator.Runner                           | 7.1.0   |
| FluentValidation                                | 12.0.0  |
| Microsoft.AspNetCore.Authentication.JwtBearer   | 9.0.7   |
| Microsoft.AspNetCore.Mvc.Testing                | 9.0.7   |
| Microsoft.AspNetCore.OpenApi                    | 9.0.5   |
| Microsoft.AspNetCore.WebUtilities               | 9.0.7   |
| Microsoft.Data.SqlClient                        | 5.2.2   |
| Microsoft.Extensions.Configuration.Json         | 9.0.7   |
| Microsoft.Extensions.DependencyInjection        | 9.0.7   |
| Microsoft.Extensions.Logging                    | 9.0.7   |
| Microsoft.Extensions.Logging.Log4Net.AspNetCore | 8.0.0   |
| Microsoft.NET.Test.Sdk                          | 17.12.0 |
| Moq                                             | 4.20.72 |
| NHibernate                                      | 5.5.2   |
| Npgsql                                          | 9.0.3   |
| NUnit                                           | 4.2.2   |
| NUnit.Analyzers                                 | 4.4.0   |
| NUnit3TestAdapter                               | 4.6.0   |
| Scrutor                                         | 6.1.0   |
| Spectre.Console                                 | 0.50.0  |
| Swashbuckle.AspNetCore                          | 9.0.3   |
| System.Linq.Dynamic.Core                        | 1.6.7   |

---

## Templates y Archivos Generados

### Convención de Nombres de Templates

Los templates están organizados por namespace:

- `Templates.src.domain.*` - Archivos del dominio
- `Templates.src.infrastructure.*` - Archivos de infraestructura
- `Templates.src.webapi.*` - Archivos de WebAPI
- `Templates.tests.*` - Archivos de tests

### Reemplazos de Placeholders

En todos los templates se realizan los siguientes reemplazos:

| Placeholder                            | Descripción                             | Ejemplo                                                            |
| -------------------------------------- | --------------------------------------- | ------------------------------------------------------------------ |
| `{{ SOLUTION_NAME }}`                  | Nombre de la solución                   | `MiProyecto`                                                       |
| `{{ NDBUNIT_INSTANCE_NAME }}`          | Clase de NDbUnit según BD               | `PostgreSQLNDbUnit` o `SqlClienteNDbUnit`                          |
| `{{ CONNECTIONSTRINGBUILDER_METHOD }}` | Método para construir connection string | `BuildPostgresConnectionString` o `BuildSqlServerConnectionString` |
| `{{ DATABASE_DRIVER }}`                | Driver de NHibernate                    | `NpgsqlDriver` o `MicrosoftDataSqlClientDriver`                    |
| `{{ DATABASE_DIALECT }}`               | Dialecto de NHibernate                  | `PostgreSQL83Dialect` o `MsSql2012Dialect`                         |

### Valores según Base de Datos

**PostgreSQL:**

- NDBUNIT_INSTANCE_NAME: `PostgreSQLNDbUnit`
- CONNECTIONSTRINGBUILDER_METHOD: `BuildPostgresConnectionString`
- DATABASE_DRIVER: `NpgsqlDriver`
- DATABASE_DIALECT: `PostgreSQL83Dialect`

**SQL Server:**

- NDBUNIT_INSTANCE_NAME: `SqlClienteNDbUnit`
- CONNECTIONSTRINGBUILDER_METHOD: `BuildSqlServerConnectionString`
- DATABASE_DRIVER: `MicrosoftDataSqlClientDriver`
- DATABASE_DIALECT: `MsSql2012Dialect`

---

## Notas Importantes

### Gestión Centralizada de Paquetes

El archivo `Directory.Packages.props` habilita la gestión centralizada de versiones de paquetes NuGet. Esto significa:

- Todas las versiones se definen una sola vez en `Directory.Packages.props`
- Los proyectos referencian paquetes SIN especificar versión
- Al crear proyectos con `dotnet new`, se deben remover los atributos `Version` de los `PackageReference` generados automáticamente

### Orden de Creación

El orden de los pasos es importante debido a las dependencias entre proyectos:

1. Solution y Directory.Packages.props (base)
2. Migrations (independiente)
3. Domain (sin dependencias)
4. NDbUnit (independiente)
5. Common.Tests (depende de Domain)
6. Application (depende de Domain)
7. Infrastructure (depende de Domain, NDbUnit, Common.Tests)
8. WebApi (depende de Domain, Infrastructure)
9. Scenarios (depende de Domain, Infrastructure, NDbUnit)

### Configuración según Base de Datos

Algunos archivos cambian según la base de datos seleccionada:

- NDbUnit: Solo se crea la clase correspondiente (PostgreSQL o SQL Server)
- NHSessionFactory: Configurar driver y dialecto correcto
- ConnectionStringBuilder: Usar el método correcto
- Migrations Program.cs: Configurar el runner correcto

### Variables de Entorno

El proyecto usa archivos `.env` para configuración. Crear un archivo `.env.example` en la raíz de la solución:

#### Para PostgreSQL

**Archivo: `.env.example`**

```bash
# Database Configuration - PostgreSQL
DB_HOST=localhost
DB_PORT=5432
DB_NAME=miproyecto_db
DB_USER=postgres
DB_PASSWORD=your_password_here

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# JWT Settings (opcional)
JWT_SECRET=your_jwt_secret_key_here
JWT_ISSUER=MiProyecto
JWT_AUDIENCE=MiProyectoAPI
JWT_EXPIRATION_MINUTES=60

# Logging
LOG_LEVEL=Information
```

#### Para SQL Server

**Archivo: `.env.example`**

```bash
# Database Configuration - SQL Server
DB_SERVER=localhost
DB_NAME=miproyecto_db
DB_USER=sa
DB_PASSWORD=your_password_here
DB_INTEGRATED_SECURITY=false

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001

# JWT Settings (opcional)
JWT_SECRET=your_jwt_secret_key_here
JWT_ISSUER=MiProyecto
JWT_AUDIENCE=MiProyectoAPI
JWT_EXPIRATION_MINUTES=60

# Logging
LOG_LEVEL=Information
```

**IMPORTANTE**:

- Copiar `.env.example` a `.env` y configurar con valores reales
- Agregar `.env` al `.gitignore` para no versionar credenciales
- El archivo `.env.example` se versiona como plantilla

**Archivo: `.gitignore`** (agregar estas líneas)

```
# Environment variables
.env
.env.local
.env.*.local
```

---

## Verificación Final

Después de completar todos los pasos, verifica:

1. La solución compila sin errores: `dotnet build`
2. Todos los proyectos están agregados a la solución
3. Las referencias entre proyectos son correctas
4. Los paquetes NuGet están instalados
5. La estructura de carpetas coincide con lo esperado
6. Los archivos de templates están en su lugar

---

## Uso del Proyecto Generado

### Ejecutar Migraciones

```bash
cd src/MiProyecto.migrations
dotnet run -- cnn="tu_connection_string" action=run
```

### Ejecutar WebAPI

```bash
cd src/MiProyecto.webapi
dotnet run
```

### Generar Escenarios

```bash
cd tests/MiProyecto.scenarios
dotnet run -- cnn="tu_connection_string" output="./scenarios"
```

### Ejecutar Tests

```bash
dotnet test
```

---

**Documento generado por**: apsys.builder CLI
**Versión**: 1.0
**Fecha**: 2025
