# Guía de Creación de Escenarios para Testing

**Version:** 2.0.0
**Estado:** Actualizado
**Última actualización:** 2025-12-29

## Descripción

Esta guía documenta cómo crear **clases generadoras de escenarios** para pruebas de integración. Los escenarios son archivos XML que contienen datos de prueba, pero estos archivos **nunca se crean manualmente**. Se generan automáticamente ejecutando clases C# que implementan `IScenario`.

---

## Conceptos Clave

### ¿Qué es un Escenario?

Un escenario es un conjunto de datos de prueba que se carga en la base de datos antes de ejecutar tests de integración. Los escenarios permiten:

1. **Aislamiento** - No depender del repositorio bajo prueba para preparar datos
2. **Reproducibilidad** - Mismos datos en cada ejecución
3. **Independencia** - Cada test carga solo lo que necesita
4. **Validación** - Los datos pasan por las mismas validaciones que el código de producción

### Flujo de Generación

```
┌─────────────────────────────────────────────────────────────────┐
│  Sc030CreateUsers.cs                                             │
│  ├── SeedData() → usa _uoW.Users.CreateAsync()                  │
│  ├── ScenarioFileName → "CreateUsers"                           │
│  └── PreloadScenario → typeof(Sc020CreateRoles)                 │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Program.cs (ejecuta ScenarioBuilder)                           │
│  1. ClearDatabase()                                              │
│  2. LoadXmlFile(PreloadScenario) si existe                      │
│  3. scenario.SeedData() → inserta datos via repositorios        │
│  4. GetDataSetFromDb() → lee datos de la BD                     │
│  5. WriteXml(outputPath) → GENERA el XML                        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  {output}/CreateUsers.xml  ← Archivo GENERADO                   │
└─────────────────────────────────────────────────────────────────┘
```

**IMPORTANTE:** Los archivos XML son **artefactos generados**. Nunca se crean ni editan manualmente.

---

## Estructura del Proyecto de Escenarios

```
tests/
└── {proyecto}.scenarios/
    ├── {proyecto}.scenarios.csproj
    ├── Program.cs                    # Punto de entrada del generador
    ├── CommandLineArgs.cs            # Parser de argumentos
    ├── ExitCode.cs                   # Códigos de salida
    ├── IScenario.cs                  # Interface base
    ├── ScenarioBuilder.cs            # Builder principal
    │
    ├── Sc010CreateSandBox.cs         # Escenario vacío
    ├── Sc020CreateRoles.cs           # Entidades sin dependencias
    ├── Sc030CreateUsers.cs           # Depende de Roles
    ├── Sc031CreateAdminUser.cs       # Variante: admin
    ├── Sc040CreateTechnicalStandards.cs
    └── Sc050CreatePrototypes.cs
```

---

## Componentes del Sistema

### 1. IScenario - Interface Base

Todas las clases generadoras deben implementar esta interface:

```csharp
namespace {proyecto}.scenarios;

/// <summary>
/// Defines the operations to seed the database with the data of the scenario
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Execute the operations to seed the database
    /// </summary>
    Task SeedData();

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    string ScenarioFileName { get; }

    /// <summary>
    /// If defined, the scenario will be pre-loaded before the current scenario
    /// </summary>
    Type? PreloadScenario { get; }
}
```

| Miembro | Propósito |
|---------|-----------|
| `SeedData()` | Método async que inserta datos usando repositorios |
| `ScenarioFileName` | Nombre del XML a generar (sin extensión) |
| `PreloadScenario` | Escenario que debe ejecutarse antes (opcional) |

### 2. ScenarioBuilder - Builder Principal

El ScenarioBuilder configura la inyección de dependencias y coordina la ejecución:

```csharp
namespace {proyecto}.scenarios;

public class ScenarioBuilder
{
    public IList<IScenario> Scenarios { get; private set; }
    protected internal ServiceProvider _serviceProvider;
    protected internal NHSessionFactory sessionFactory;
    protected internal INDbUnit NDbUnitTest;

    public ScenarioBuilder(string connectionString)
    {
        // Create the NDbUnit instance
        var schema = new AppSchema();
        this.NDbUnitTest = new PostgreSQLNDbUnit(schema, connectionString);

        // Create the NHibernate session
        this.sessionFactory = new NHSessionFactory(connectionString);
        var nhSessionFactory = this.sessionFactory.BuildNHibernateSessionFactory();

        _serviceProvider = new ServiceCollection()
            .Scan(scan => scan
                .FromAssemblyOf<Sc010CreateSandBox>()
                .AddClasses(classes => classes.AssignableTo<IScenario>())
                .AsSelf()
                .WithScopedLifetime()
            )
            .AddLogging()
            .AddScoped<IUnitOfWork, NHUnitOfWork>()
            .AddScoped<ISession>(session => nhSessionFactory.OpenSession())
            .AddSingleton<INDbUnit>(NDbUnitTest)
            // Registrar validators para cada entidad
            .AddTransient<AbstractValidator<User>, UserValidator>()
            .AddTransient<AbstractValidator<Role>, RoleValidator>()
            // ... más validators
            .BuildServiceProvider();

        this.Scenarios = ReadAllScenariosFromAssemblies();
    }

    public void LoadXmlFile(Type preloadScenario, string outputPath)
    {
        // Carga un XML previamente generado para usar como prerequisito
        IScenario? instance = this.Scenarios.FirstOrDefault(s => s.GetType() == preloadScenario);
        if (instance == null)
            throw new TypeLoadException($"Preload scenario {preloadScenario.Name} not found");

        var fileName = $"{instance.ScenarioFileName}.xml";
        var fullPath = Path.Combine(outputPath, fileName);

        var dataSet = new AppSchema();
        dataSet.ReadXml(fullPath);
        this.NDbUnitTest.SeedDatabase(dataSet);
    }
}
```

### 3. Program.cs - Punto de Entrada

El Program.cs ejecuta todos los escenarios y genera los XMLs:

```csharp
using Spectre.Console;
using System.Data;
using {proyecto}.scenarios;

Console.OutputEncoding = Encoding.UTF8;

try
{
    // Leer argumentos de línea de comandos
    CommandLineArgs args = [];

    if (!args.TryGetValue("cnn", out string? connectionString))
        throw new ArgumentException("Falta argumento [cnn] con connection string");

    if (!args.TryGetValue("output", out string? outputPath))
        throw new ArgumentException("Falta argumento [output] con ruta de salida");

    if (!Directory.Exists(outputPath))
        Directory.CreateDirectory(outputPath);

    var builder = new ScenarioBuilder(connectionString);

    foreach (var scenario in builder.Scenarios)
    {
        var scenarioName = scenario.ScenarioFileName;
        Console.WriteLine($"Creando escenario {scenarioName}...");

        // 1. Limpiar base de datos
        builder.NDbUnitTest.ClearDatabase();

        // 2. Cargar escenario prerequisito si existe
        if (scenario.PreloadScenario != null)
            builder.LoadXmlFile(scenario.PreloadScenario, outputPath);

        // 3. Ejecutar SeedData() para insertar datos
        scenario.SeedData().Wait();

        // 4. Leer datos de la BD y escribir XML
        string filePath = Path.Combine(outputPath, $"{scenarioName}.xml");
        DataSet dataSet = builder.NDbUnitTest.GetDataSetFromDb();
        dataSet.WriteXml(filePath);
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return 1;
}
```

---

## Crear una Clase Generadora

### Convención de Nombres

```
Sc###Create{Entity}.cs

Donde:
- Sc = Scenario
- ### = Número de orden (010, 020, 030...)
- Create{Entity} = Nombre descriptivo
```

El número determina el **orden de dependencias**:
- `010` - Base/SandBox (limpia DB)
- `020` - Entidades sin dependencias (Roles, Modules)
- `030-039` - Entidades con dependencias nivel 1 (Users → Roles)
- `040-049` - Entidades con dependencias nivel 2
- Y así sucesivamente

### Ejemplo Básico - Entidades Sin Dependencias

```csharp
using {proyecto}.domain.interfaces.repositories;
using {proyecto}.scenarios;

namespace tests.{proyecto}.scenarios;

/// <summary>
/// Scenario to create roles
/// </summary>
public class Sc020CreateRoles(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateRoles";

    // Sin prerequisito
    public Type? PreloadScenario => null;

    public async Task SeedData()
    {
        var roles = new List<string>
        {
            "PlatformAdministrator",
            "OrganizationAdmin",
            "User"
        };

        try
        {
            this._uoW.BeginTransaction();
            foreach (var roleName in roles)
                await this._uoW.Roles.CreateAsync(roleName);
            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();
            throw;
        }
    }
}
```

### Ejemplo con Dependencias

```csharp
using {proyecto}.domain.interfaces.repositories;
using {proyecto}.scenarios;

namespace tests.{proyecto}.scenarios;

/// <summary>
/// Scenario to create users (requires roles)
/// </summary>
public class Sc030CreateUsers(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateUsers";

    // Requiere que CreateRoles se ejecute primero
    public Type? PreloadScenario => typeof(Sc020CreateRoles);

    public async Task SeedData()
    {
        var users = new List<(string Email, string Name)>
        {
            ("usuario1@example.com", "Carlos Rodríguez"),
            ("usuario2@example.com", "Ana María González"),
            ("usuario3@example.com", "José Luis Martínez")
        };

        try
        {
            this._uoW.BeginTransaction();
            foreach (var (email, name) in users)
                await this._uoW.Users.CreateAsync(email, name);
            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();
            throw;
        }
    }
}
```

### Ejemplo de Variante

```csharp
/// <summary>
/// Scenario to create an admin user with role assignment
/// </summary>
public class Sc031CreateAdminUser(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateAdminUser";

    // Carga usuarios base primero
    public Type? PreloadScenario => typeof(Sc030CreateUsers);

    public async Task SeedData()
    {
        try
        {
            this._uoW.BeginTransaction();

            // Obtener rol admin existente (cargado por Sc020)
            var adminRole = await this._uoW.Roles.GetByNameAsync("PlatformAdministrator");

            // Obtener primer usuario (cargado por Sc030)
            var users = await this._uoW.Users.GetAsync();
            var firstUser = users.First();

            // Asignar rol
            await this._uoW.Users.AddToRoleAsync(firstUser.Id, adminRole.Id);

            this._uoW.Commit();
        }
        catch
        {
            this._uoW.Rollback();
            throw;
        }
    }
}
```

### Ejemplo SandBox (Escenario Vacío)

```csharp
/// <summary>
/// Empty scenario - just clears the database
/// </summary>
public class Sc010CreateSandBox(IUnitOfWork uoW) : IScenario
{
    private readonly IUnitOfWork _uoW = uoW;

    public string ScenarioFileName => "CreateSandBox";

    public Type? PreloadScenario => null;

    public Task SeedData()
    {
        // No inserta nada - solo sirve para limpiar la BD
        return Task.CompletedTask;
    }
}
```

---

## Ejecutar el Generador

### Argumentos de Línea de Comandos

| Argumento | Descripción | Ejemplo |
|-----------|-------------|---------|
| `cnn` | Connection string a la BD de desarrollo | `Host=localhost;Database=mydb_test;...` |
| `output` | Ruta donde se generarán los XMLs | `../infrastructure.tests/scenarios` |

### Ejecución

```bash
# Desde la carpeta del proyecto de escenarios
cd tests/{proyecto}.scenarios

# Ejecutar el generador
dotnet run -- cnn="Host=localhost;Database=mydb_test;Username=user;Password=pass" output="../{proyecto}.infrastructure.tests/scenarios"
```

### Ruta de Salida

La ruta de salida (`output`) es **relativa al ambiente de trabajo** de cada desarrollador. Típicamente apunta a:

```
tests/{proyecto}.infrastructure.tests/scenarios/
```

Esta carpeta es donde los tests de integración buscan los XMLs para cargarlos.

---

## Cadena de Dependencias

El sistema resuelve dependencias automáticamente via `PreloadScenario`:

```
Sc010CreateSandBox (base)
    ↓
Sc020CreateRoles (sin dependencias)
    ↓
Sc030CreateUsers (depende de Roles)
    ↓
Sc031CreateAdminUser (depende de Users)
    ↓
Sc040CreateTechnicalStandards (sin dependencias)
    ↓
Sc050CreatePrototypes (depende de TechnicalStandards)
```

Cuando se genera `CreateAdminUser.xml`:
1. Se limpia la BD
2. Se carga `CreateUsers.xml` (que incluye roles porque Sc030 dependía de Sc020)
3. Se ejecuta `Sc031CreateAdminUser.SeedData()`
4. Se lee toda la BD y se escribe `CreateAdminUser.xml`

---

## Best Practices

### 1. Usar Repositorios, No SQL Directo

```csharp
// ✅ CORRECTO - Usa repositorios
await this._uoW.Users.CreateAsync(email, name);

// ❌ INCORRECTO - SQL directo
await session.ExecuteAsync("INSERT INTO users...");
```

### 2. Manejar Transacciones Explícitamente

```csharp
try
{
    this._uoW.BeginTransaction();
    // ... operaciones
    this._uoW.Commit();
}
catch
{
    this._uoW.Rollback();
    throw;
}
```

### 3. Datos Realistas y Consistentes

```csharp
// ✅ CORRECTO - Datos realistas
("usuario1@example.com", "Carlos Rodríguez")

// ❌ INCORRECTO - Datos sin sentido
("test@test.com", "Test Test")
```

### 4. Documentar Propósito del Escenario

```csharp
/// <summary>
/// Scenario to create 5 users with different statuses for filter testing
/// Used in: NHUserRepositoryTests
/// </summary>
public class Sc030CreateUsers : IScenario
```

### 5. Mantener Escenarios Focalizados

```csharp
// ✅ CORRECTO - Escenario específico
public class Sc030CreateUsers : IScenario  // Solo usuarios básicos
public class Sc031CreateAdminUser : IScenario  // Usuario admin con rol

// ❌ INCORRECTO - Escenario monolítico
public class ScCreateEverything : IScenario  // Todas las entidades
```

### 6. Numeración Consistente

| Rango | Uso |
|-------|-----|
| `010` | SandBox/Base |
| `020-029` | Entidades raíz (sin FK) |
| `030-039` | Entidades con 1 nivel de dependencia |
| `040-049` | Entidades con 2 niveles de dependencia |
| `0X1, 0X2...` | Variantes del escenario `0X0` |

---

## Registrar Nuevas Entidades

Cuando agregas una nueva entidad al dominio, debes:

1. **Actualizar ScenarioBuilder.cs** - Registrar el validator:

```csharp
.AddTransient<AbstractValidator<NuevaEntidad>, NuevaEntidadValidator>()
```

2. **Crear la clase generadora** con el número apropiado:

```csharp
public class Sc060CreateNuevaEntidad(IUnitOfWork uoW) : IScenario
{
    // ...
}
```

3. **Ejecutar el generador** para crear el XML.

---

## Troubleshooting

### Error: "Preload scenario not found"

El escenario prerequisito no existe o no implementa `IScenario`.

```csharp
// Verificar que el tipo existe
public Type? PreloadScenario => typeof(Sc020CreateRoles);
```

### Error: "File not found" al cargar prerequisito

El XML del prerequisito no existe en la ruta de salida. Ejecuta el generador primero para todos los escenarios base.

### Error: "Foreign key violation"

El orden de dependencias no es correcto. Verifica que `PreloadScenario` apunta al escenario que crea las entidades requeridas.

### Los datos no aparecen en el XML

Verifica que `Commit()` se ejecuta correctamente:

```csharp
this._uoW.Commit();  // Sin esto, los datos no se persisten
```

---

## Proyectos de Referencia

| Proyecto | Ubicación |
|----------|-----------|
| hollow.soulmaster.backend.scenarios | `D:\hollow\hollow.soulmaster.backend\tests\hollow.soulmaster.backend.scenarios` |
| hashira.stone.backend.scenarios | `D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\tests\hashira.stone.backend.scenarios` |

---

## Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 2.0.0 | 2025-12-29 | Reescritura completa: enfoque en clases generadoras |
| 1.0.0 | 2025-01-20 | Versión inicial (obsoleta) |

---

**Última actualización:** 2025-12-29
**Mantenedor:** Equipo APSYS
