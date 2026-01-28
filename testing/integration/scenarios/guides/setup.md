# Scenarios & Common.Tests - Setup Guide

**Version:** 1.1.0
**Ultima actualizacion:** 2026-01-27

## Descripcion

Esta guia describe como configurar los proyectos `{ProjectName}.scenarios` y `{ProjectName}.common.tests` para generar escenarios de prueba.

---

## Estructura de Proyectos

```
tests/
├── {ProjectName}.ndbunit/         # Libreria NDbUnit (prerequisito)
├── {ProjectName}.common.tests/    # Recursos compartidos
│   ├── {ProjectName}.common.tests.csproj
│   ├── AppSchema.xsd              # Schema tipado de la BD
│   ├── AppSchema.Designer.cs      # Generado desde XSD
│   ├── AppSchemaExtender.cs       # Extension methods
│   └── ScenarioIds.cs             # IDs constantes
└── {ProjectName}.scenarios/       # Generador de escenarios
    ├── {ProjectName}.scenarios.csproj
    ├── Program.cs
    ├── CommandLineArgs.cs
    ├── ExitCode.cs
    ├── IScenario.cs
    ├── ScenarioBuilder.cs
    └── Sc010CreateSandBox.cs
```

---

## Parte 1: Proyecto common.tests

### 1. Crear proyecto

```bash
cd tests
dotnet new classlib -n {ProjectName}.common.tests
cd ..
dotnet sln add tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj
```

### 2. Configurar csproj

Editar `tests/{ProjectName}.common.tests/{ProjectName}.common.tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\src\{ProjectName}.domain\{ProjectName}.domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Compile Update="AppSchema.Designer.cs">
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <DependentUpon>AppSchema.xsd</DependentUpon>
    </Compile>
  </ItemGroup>

  <ItemGroup>
    <None Update="AppSchema.xsd">
      <Generator>MSDataSetGenerator</Generator>
      <LastGenOutput>AppSchema.Designer.cs</LastGenOutput>
    </None>
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

### 3. Crear AppSchema.xsd

Crear el archivo `tests/{ProjectName}.common.tests/AppSchema.xsd` con las tablas del schema.

**Ejemplo basico:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<xs:schema id="AppSchema"
           targetNamespace="http://tempuri.org/AppSchema.xsd"
           elementFormDefault="qualified"
           xmlns="http://tempuri.org/AppSchema.xsd"
           xmlns:mstns="http://tempuri.org/AppSchema.xsd"
           xmlns:xs="http://www.w3.org/2001/XMLSchema"
           xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
  <xs:element name="AppSchema" msdata:IsDataSet="true" msdata:UseCurrentLocale="true">
    <xs:complexType>
      <xs:choice minOccurs="0" maxOccurs="unbounded">
        <!-- Agregar tablas aqui -->
        <xs:element name="public.users">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="id" type="xs:string" minOccurs="0" />
              <xs:element name="email" type="xs:string" minOccurs="0" />
              <xs:element name="name" type="xs:string" minOccurs="0" />
              <xs:element name="created_at" type="xs:dateTime" minOccurs="0" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>
```

### 4. Generar Designer.cs

En Visual Studio, hacer clic derecho en `AppSchema.xsd` → "Run Custom Tool" para generar `AppSchema.Designer.cs`.

### 5. Copiar templates

Copiar desde `docs/guides/testing/integration/scenarios/templates/common.tests/`:

| Archivo | Descripcion |
|---------|-------------|
| `AppSchemaExtender.cs` | Extension methods para acceder a tablas |
| `ScenarioIds.cs` | IDs constantes para escenarios |

### 6. Eliminar Class1.cs

```bash
rm tests/{ProjectName}.common.tests/Class1.cs
```

---

## Parte 2: Proyecto scenarios

### 1. Crear proyecto

```bash
cd tests
dotnet new console -n {ProjectName}.scenarios
cd ..
dotnet sln add tests/{ProjectName}.scenarios/{ProjectName}.scenarios.csproj
```

### 2. Configurar csproj

Editar `tests/{ProjectName}.scenarios/{ProjectName}.scenarios.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
    <PackageReference Include="Scrutor" />
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{ProjectName}.domain\{ProjectName}.domain.csproj" />
    <ProjectReference Include="..\..\src\{ProjectName}.infrastructure\{ProjectName}.infrastructure.csproj" />
    <ProjectReference Include="..\{ProjectName}.ndbunit\{ProjectName}.ndbunit.csproj" />
    <ProjectReference Include="..\{ProjectName}.common.tests\{ProjectName}.common.tests.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="data\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

### 3. Copiar templates

Copiar desde `docs/guides/testing/integration/scenarios/templates/project/`:

| Archivo | Descripcion |
|---------|-------------|
| `Program.cs` | Punto de entrada con validacion robusta |
| `CommandLineArgs.cs` | Parser de argumentos CLI (/key:value) |
| `ExitCode.cs` | Codigos de salida (Success, InvalidParameters, etc.) |
| `IScenario.cs` | Interface base para escenarios |
| `ScenarioBuilder.cs` | Builder principal con DI |
| `Sc010CreateSandBox.cs` | Escenario base con limpieza de tablas |

### 4. Eliminar Program.cs generado

El template de console genera un Program.cs basico que debe ser reemplazado por el template.

### 5. Reemplazar placeholders

En todos los archivos, reemplazar:
- `{ProjectName}` → Nombre del proyecto

### 6. Actualizar ScenarioBuilder.cs

Modificar `ScenarioBuilder.cs` para:

1. Usar la implementacion correcta de NDbUnit segun la base de datos:

```csharp
// Para PostgreSQL:
this.NDbUnitTest = new PostgreSQLNDbUnit(schema, connectionString);

// Para SQL Server:
this.NDbUnitTest = new SqlServerNDbUnit(schema, connectionString);
```

2. Registrar validators para cada entidad del dominio:

```csharp
.AddTransient<AbstractValidator<User>, UserValidator>()
.AddTransient<AbstractValidator<Role>, RoleValidator>()
// ... mas validators
```

---

## Script de Ejecucion

Crear `buildscenarios.bat` en la raiz del proyecto:

```batch
cd tests/{ProjectName}.scenarios\bin\Debug\net9.0
{ProjectName}.scenarios.exe /cnn:"Host=localhost;Port=5432;Database={dbname};Username=postgres;Password=root;" /output:"D:\path\to\scenarios"
cd "../../../../.."
```

---

## Uso

```bash
# 1. Compilar el proyecto
dotnet build tests/{ProjectName}.scenarios

# 2. Ejecutar
buildscenarios.bat
```

---

## Proximos Pasos

1. Agregar tablas al `AppSchema.xsd`
2. Regenerar `AppSchema.Designer.cs`
3. Actualizar `AppSchemaExtender.cs` con metodos para las nuevas tablas
4. Crear clases `Sc###Create*.cs` para cada escenario
5. Actualizar `ScenarioBuilder.cs` con validators

Para mas informacion sobre crear escenarios, ver:
`docs/guides/testing/integration/scenarios/guides/scenarios-creation-guide.md`

---

## Codigos de Salida

El ejecutable retorna los siguientes codigos:

| Codigo | Nombre | Descripcion |
|--------|--------|-------------|
| 0 | Success | Todos los escenarios procesados correctamente |
| 1 | UnknownError | Error inesperado |
| 2 | InvalidParameters | Parametros /cnn o /output faltantes o invalidos |
| 3 | OutputFolderError | Error al crear/acceder la carpeta de salida |
| 4 | DatabaseConnectionError | Error de conexion a la base de datos |
| 5 | ScenarioExecutionError | Error al ejecutar un escenario |

---

## Changelog

| Version | Fecha | Cambios |
|---------|-------|---------|
| 1.1.0 | 2026-01-27 | Actualizacion de templates: ExitCodes granulares, Program.cs robusto, Sc010CreateSandBox con INDbUnit |
| 1.0.0 | 2025-12-29 | Version inicial |

---

**Ultima actualizacion:** 2026-01-27
**Mantenedor:** Equipo APSYS
