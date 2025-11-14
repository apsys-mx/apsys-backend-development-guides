# NHibernate Queries (LINQ, HQL y Dynamic Queries)

**Versión**: 1.0.0
**Última actualización**: 2025-11-14

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [LINQ to NHibernate](#linq-to-nhibernate)
3. [HQL (Hibernate Query Language)](#hql-hibernate-query-language)
4. [QueryOver API](#queryover-api)
5. [Dynamic LINQ](#dynamic-linq)
6. [Sistema de Query String Parsing](#sistema-de-query-string-parsing)
7. [Filter Expression Parser](#filter-expression-parser)
8. [Paginación y Ordenamiento](#paginación-y-ordenamiento)
9. [Patrón GetManyAndCount](#patrón-getmanyandcount)
10. [Operadores de Filtrado](#operadores-de-filtrado)
11. [Quick Search](#quick-search)
12. [Mejores Prácticas](#mejores-prácticas)
13. [Antipatrones Comunes](#antipatrones-comunes)
14. [Ejemplos Completos](#ejemplos-completos)
15. [Referencias](#referencias)

---

## Introducción

NHibernate ofrece **múltiples formas** de consultar datos, cada una con sus ventajas y casos de uso específicos. El proyecto **hashira.stone.backend** utiliza principalmente:

| Método | Ventajas | Casos de Uso |
|--------|----------|--------------|
| **LINQ to NHibernate** | Type-safe, IntelliSense, refactoring | Queries conocidas en tiempo de compilación |
| **HQL** | Flexible, funciones especiales (unaccent) | Queries complejas con funciones de BD |
| **Dynamic LINQ** | Runtime queries, filtros dinámicos | APIs REST con query strings |
| **QueryOver** | Type-safe, alternativa a LINQ | Queries complejas type-safe |

### Stack Tecnológico

```csharp
// Packages NuGet utilizados
using NHibernate.Linq;                    // LINQ to NHibernate
using System.Linq.Dynamic.Core;           // Dynamic LINQ
using NHibernate;                         // HQL, QueryOver
```

---

## LINQ to NHibernate

### ¿Qué es LINQ to NHibernate?

LINQ (Language Integrated Query) permite escribir queries en C# que se traducen automáticamente a SQL.

### Queries Básicas

#### 1. Obtener Todos los Registros

```csharp
public IEnumerable<T> Get()
    => this._session.Query<T>();
```

**SQL Generado**:
```sql
SELECT * FROM users;
```

#### 2. Filtrar con Where

Del proyecto [NHUserRepository.cs:48-50](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHUserRepository.cs:48-50):

```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync();
}
```

**SQL Generado**:
```sql
SELECT * FROM users
WHERE email = :p0;
```

#### 3. Filtrar con Expresiones

Del proyecto [NHReadOnlyRepository.cs:79-81](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs:79-81):

```csharp
public IEnumerable<T> Get(Expression<Func<T, bool>> query)
    => this._session.Query<T>()
            .Where(query);
```

**Uso**:
```csharp
var users = repository.Get(u => u.Name.Contains("John"));
```

#### 4. Contar Registros

```csharp
public int Count(Expression<Func<T, bool>> query)
    => this._session.Query<T>().Where(query).Count();

public Task<int> CountAsync(Expression<Func<T, bool>> query, CancellationToken ct = default)
    => this._session.Query<T>().Where(query).CountAsync(ct);
```

### Operadores LINQ Soportados

| Operador | Descripción | Ejemplo |
|----------|-------------|---------|
| `Where()` | Filtrado | `.Where(u => u.Age > 18)` |
| `Select()` | Proyección | `.Select(u => u.Name)` |
| `OrderBy()` | Ordenamiento ascendente | `.OrderBy(u => u.Name)` |
| `OrderByDescending()` | Ordenamiento descendente | `.OrderByDescending(u => u.CreatedAt)` |
| `Skip()` | Saltar registros (paginación) | `.Skip(10)` |
| `Take()` | Limitar registros | `.Take(25)` |
| `FirstOrDefault()` | Primer registro o null | `.FirstOrDefault()` |
| `SingleOrDefault()` | Único registro o null | `.SingleOrDefaultAsync()` |
| `Count()` | Contar registros | `.Count()` |
| `Any()` | Verificar existencia | `.Any(u => u.Email == "test")` |
| `ToList()` | Materializar a lista | `.ToListAsync()` |

### Paginación con LINQ

Del proyecto [NHReadOnlyRepository.cs:91-96](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs:91-96):

```csharp
public IEnumerable<T> Get(Expression<Func<T, bool>> query, int page, int pageSize, SortingCriteria sortingCriteria)
    => this._session.Query<T>()
            .Where(query)
            .OrderBy(sortingCriteria.ToExpression())      // ✅ Dynamic LINQ
            .Skip((page - 1) * pageSize)                  // ✅ Offset
            .Take(pageSize);                              // ✅ Limit
```

**SQL Generado**:
```sql
SELECT * FROM users
WHERE email LIKE :p0
ORDER BY name ASC
LIMIT 25 OFFSET 0;
```

### Queries Asíncronas

```csharp
// Get all asíncrono
public async Task<IEnumerable<T>> GetAsync(CancellationToken ct = default)
    => await this._session.Query<T>().ToListAsync(ct);

// Get con filtro asíncrono
public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> query, CancellationToken ct = default)
    => await this._session.Query<T>()
            .Where(query)
            .ToListAsync(ct);

// Count asíncrono
public Task<int> CountAsync(CancellationToken ct = default)
    => this._session.Query<T>().CountAsync(ct);
```

---

## HQL (Hibernate Query Language)

### ¿Qué es HQL?

HQL es un lenguaje de queries orientado a objetos similar a SQL pero que trabaja con entidades en lugar de tablas.

### Ventajas de HQL

- ✅ **Funciones específicas de BD**: `unaccent()`, `lower()`, funciones de fecha
- ✅ **Queries complejas**: JOINs, subqueries
- ✅ **Independiente de BD**: Se adapta al dialect
- ✅ **Parámetros nombrados**: Previene SQL injection

### Query Básica con HQL

Del proyecto [NHTechnicalStandardRepository.cs:54-60](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHTechnicalStandardRepository.cs:54-60):

```csharp
public async Task<TechnicalStandard?> GetByCodeAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
        return null;

    var hql = @"
        from TechnicalStandard ts
        where lower(unaccent(ts.Code)) = lower(unaccent(:code))";

    return await _session.CreateQuery(hql)
        .SetParameter("code", code)
        .UniqueResultAsync<TechnicalStandard?>();
}
```

### Características Clave del HQL

#### 1. Funciones PostgreSQL con unaccent

```csharp
var hql = @"
    from TechnicalStandard ts
    where lower(unaccent(ts.Code)) = lower(unaccent(:code))";
```

**¿Por qué `unaccent()`?**
- Elimina acentos/tildes para búsquedas case-insensitive
- `"José" == "jose"` ✅
- Requiere extensión PostgreSQL: `CREATE EXTENSION unaccent;`

#### 2. Parámetros Nombrados

```csharp
var hql = "from User u where u.Email = :email and u.IsActive = :active";

var users = await _session.CreateQuery(hql)
    .SetParameter("email", "test@example.com")
    .SetParameter("active", true)
    .List<User>();
```

❌ **Nunca concatenar strings** (SQL injection):
```csharp
// ❌ PELIGRO: SQL Injection
var hql = $"from User u where u.Email = '{email}'";
```

#### 3. UniqueResult vs List

```csharp
// ✅ UniqueResult: Espera 1 resultado (lanza excepción si hay más de 1)
var user = await _session.CreateQuery(hql)
    .SetParameter("email", email)
    .UniqueResultAsync<User?>();

// ✅ List: Puede devolver múltiples resultados
var users = await _session.CreateQuery(hql)
    .SetParameter("status", "active")
    .List<User>();
```

### Proyecciones en HQL

```csharp
var hql = "select u.Name, u.Email from User u where u.IsActive = true";

var results = await _session.CreateQuery(hql)
    .List<object[]>();

foreach (var row in results)
{
    string name = (string)row[0];
    string email = (string)row[1];
}
```

### JOINs en HQL

```csharp
var hql = @"
    select u, r
    from User u
    join u.Roles r
    where r.Name = :roleName";

var results = await _session.CreateQuery(hql)
    .SetParameter("roleName", "Admin")
    .List<object[]>();
```

---

## QueryOver API

### ¿Qué es QueryOver?

API type-safe alternativa a LINQ, útil para queries complejas con JOINs y subqueries.

### Uso en hashira.stone.backend

Del proyecto [NHReadOnlyRepository.cs:30](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs:30):

```csharp
public int Count()
    => this._session.QueryOver<T>().RowCount();
```

### Ejemplos de QueryOver

```csharp
// Count
int total = _session.QueryOver<User>()
    .RowCount();

// Filtro
var users = _session.QueryOver<User>()
    .Where(u => u.Email == email)
    .List();

// Paginación
var page = _session.QueryOver<User>()
    .Skip(10)
    .Take(25)
    .List();
```

---

## Dynamic LINQ

### ¿Qué es Dynamic LINQ?

Permite construir queries LINQ usando **strings en runtime**, útil para filtros dinámicos desde APIs.

### Package NuGet

```xml
<PackageReference Include="System.Linq.Dynamic.Core" Version="1.3.0+" />
```

### Uso en hashira.stone.backend

Del proyecto [NHReadOnlyRepository.cs:4](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs:4):

```csharp
using System.Linq.Dynamic.Core;
```

### OrderBy Dinámico

Del proyecto [SortingCriteriaExtender.cs:17-21](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\SortingCriteriaExtender.cs:17-21):

```csharp
public static string ToExpression(this SortingCriteria sort)
{
    string orderExpression = sort.Criteria == SortingCriteriaType.Ascending
        ? $"{sort.SortBy}"
        : $"{sort.SortBy} descending";
    return orderExpression;
}
```

**Uso**:
```csharp
var sortBy = "Name";             // Viene del query string
var direction = "descending";    // Viene del query string

var results = _session.Query<User>()
    .OrderBy($"{sortBy} {direction}")  // ✅ Dynamic LINQ
    .ToList();
```

**SQL Generado**:
```sql
SELECT * FROM users
ORDER BY name DESC;
```

---

## Sistema de Query String Parsing

### Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                    HTTP Request                                  │
│  GET /api/users?pageNumber=2&pageSize=25&sortBy=Name&           │
│                 sortDirection=asc&Email=john@||contains          │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               QueryStringParser                                  │
│  - ParsePageNumber()        → pageNumber = 2                     │
│  - ParsePageSize()          → pageSize = 25                      │
│  - ParseSorting<T>()        → Sorting("Name", "asc")             │
│  - ParseFilterOperators<T>()→ FilterOperator[]                   │
│  - ParseQuery<T>()          → QuickSearch                        │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│            FilterExpressionParser                                │
│  - ParsePredicate<T>()      → Expression<Func<T, bool>>          │
│  - ParseQueryValuesToExpression() → QuickSearch expression       │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                 LINQ to NHibernate                               │
│  _session.Query<User>()                                          │
│    .Where(expression)                                            │
│    .OrderBy("Name")                                              │
│    .Skip(25)                                                     │
│    .Take(25)                                                     │
└─────────────────────────────────────────────────────────────────┘
```

### QueryStringParser

Del proyecto [QueryStringParser.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\QueryStringParser.cs):

#### 1. Parsear Paginación

```csharp
public const int DEFAULT_PAGE_NUMBER = 1;
public const int DEFAULT_PAGE_SIZE = 25;

public int ParsePageNumber()
{
    int pageNumber = DEFAULT_PAGE_NUMBER;
    if (string.IsNullOrEmpty(_queryString))
        return pageNumber;

    QueryStringArgs parameters = new(_queryString);
    if (parameters.ContainsKey("pageNumber") &&
        !int.TryParse(parameters["pageNumber"], out pageNumber))
        throw new InvalidQueryStringArgumentException("pageNumber");

    if (pageNumber < 0)
        throw new InvalidQueryStringArgumentException("pageNumber");

    return pageNumber;
}
```

#### 2. Parsear Ordenamiento

```csharp
public Sorting ParseSorting<T>(string defaultFieldName)
{
    string? sortByField = defaultFieldName;
    string? sortDirection = "asc";

    QueryStringArgs parameters = new(_queryString);

    if (parameters.TryGetValue("sortBy", out var sortByValue))
    {
        PropertyInfo[] properties = typeof(T).GetProperties();
        if (!properties.Any(p => p.Name.Equals(sortByValue, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidQueryStringArgumentException("sortBy");
        sortByField = sortByValue;
    }

    if (parameters.TryGetValue("sortDirection", out var sortDirectionValue))
    {
        if (sortDirectionValue != "desc" && sortDirectionValue != "asc")
            throw new InvalidQueryStringArgumentException("sortDirection");
        sortDirection = sortDirectionValue;
    }

    return new Sorting(sortByField, sortDirection);
}
```

#### 3. Parsear Filtros

```csharp
public IList<FilterOperator> ParseFilterOperators<T>()
{
    IList<FilterOperator> filterOperatorsResult = new List<FilterOperator>();
    QueryStringArgs parameters = new(_queryString);

    // Excluir parámetros reservados
    string[] excludedKeys = new[] { "pageNumber", "pageSize", "sortBy", "sortDirection", "query" };
    IEnumerable<KeyValuePair<string, string>> allFilters =
        parameters.Where(parameter => !excludedKeys.Contains(parameter.Key));

    foreach (var filter in allFilters)
    {
        // Formato: "value1|value2||operator"
        string[] filterData = filter.Value.Split("||");
        string[] filterValues = filterData[0].Split("|");
        var filterOperator = filterData[1];
        var operatorFieldName = filter.Key.ToPascalCase();

        filterOperatorsResult.Add(
            new FilterOperator(operatorFieldName, filterValues, filterOperator)
        );
    }

    return filterOperatorsResult;
}
```

### Formato de Query String

```
GET /api/users?
  pageNumber=2&
  pageSize=25&
  sortBy=Name&
  sortDirection=desc&
  Email=john@example.com||eq&
  Age=18|65||between&
  Status=active|pending||contains&
  query=search text||Name|Email
```

**Desglose**:
- `pageNumber=2` → Página 2
- `pageSize=25` → 25 registros por página
- `sortBy=Name` → Ordenar por campo Name
- `sortDirection=desc` → Orden descendente
- `Email=john@example.com||eq` → Email equals "john@example.com"
- `Age=18|65||between` → Age between 18 and 65
- `Status=active|pending||contains` → Status contains "active" OR "pending"
- `query=search text||Name|Email` → Quick search en campos Name y Email

---

## Filter Expression Parser

### Construcción de Expresiones LINQ

Del proyecto [FilterExpressionParser.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\FilterExpressionParser.cs):

```csharp
static public Expression<Func<T, bool>> ParsePredicate<T>(IEnumerable<FilterOperator> operands)
{
    var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());
    List<Expression> allCriteria = new List<Expression>();

    foreach (FilterOperator filter in operands)
    {
        string propertyName = filter.FieldName.ToPascalCase();
        Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
        IList<string> filterValues = filter.Values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        Expression? criteria = null;

        switch (filter.RelationalOperatorType)
        {
            case RelationalOperator.Contains:
                propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                var constant = Expression.Constant(filterValues.FirstOrDefault());
                MethodInfo? strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
                if (strContainsMethod != null)
                {
                    criteria = Expression.Call(propertyExpression, strContainsMethod, new Expression[] { constant });
                }
                break;

            case RelationalOperator.GreaterThan:
                var constantExpression = CreateConstantExpression<T>(propertyName, filterValues[0]);
                criteria = Expression.GreaterThan(propertyExpression, constantExpression);
                break;

            case RelationalOperator.Between:
                if (filterValues.Count < 2)
                    throw new InvalidFilterArgumentException($"Between operator requires two values");

                var lowerLimit = CreateConstantExpression<T>(propertyName, filterValues[0]);
                var upperLimit = CreateConstantExpression<T>(propertyName, filterValues[1]);
                Expression lowerCriteria = Expression.GreaterThanOrEqual(propertyExpression, lowerLimit);
                Expression upperCriteria = Expression.LessThanOrEqual(propertyExpression, upperLimit);
                criteria = Expression.AndAlso(lowerCriteria, upperCriteria);
                break;

            default: // Equal
                propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                MethodInfo? arrContainsMethod = filterValues.GetType().GetMethod(nameof(filterValues.Contains), new Type[] { typeof(string) });
                if (arrContainsMethod != null)
                    criteria = Expression.Call(Expression.Constant(filterValues), arrContainsMethod, propertyExpression);
                break;
        }

        if (criteria != null)
            allCriteria.Add(criteria);
    }

    if (!allCriteria.Any())
        return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), parameterExpression);

    Expression? expression = null;
    foreach (Expression criteria in allCriteria)
        expression = expression != null ? Expression.AndAlso(expression, criteria) : criteria;

    return Expression.Lambda<Func<T, bool>>(expression!, parameterExpression);
}
```

### Conversión de Tipos

```csharp
private static Expression CreateConstantExpression<T>(string propertyName, string constantValue)
{
    PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName);
    var actualType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

    if (actualType == typeof(string))
        return Expression.Constant(constantValue, propertyInfo.PropertyType);

    // DateTime con formato yyyy-MM-dd
    if (actualType == typeof(DateTime))
    {
        if (DateTime.TryParseExact(constantValue,
            new[] { "yyyy-MM-dd" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dateValue))
        {
            dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
            return Expression.Constant(dateValue, propertyInfo.PropertyType);
        }
        throw new InvalidFilterArgumentException($"Invalid date format. Use yyyy-MM-dd");
    }

    // Enum o tipos primitivos
    object convertedValue = actualType.IsEnum
        ? Enum.Parse(actualType, constantValue)
        : Convert.ChangeType(constantValue, actualType);

    return Expression.Constant(convertedValue, propertyInfo.PropertyType);
}
```

---

## Paginación y Ordenamiento

### SortingCriteria

Del proyecto [SortingCriteria.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\repositories\SortingCriteria.cs):

```csharp
public class SortingCriteria
{
    public string SortBy { get; set; } = string.Empty;
    public SortingCriteriaType Criteria { get; set; } = SortingCriteriaType.Ascending;

    public SortingCriteria(string sortBy, SortingCriteriaType criteria)
    {
        this.SortBy = sortBy;
        this.Criteria = criteria;
    }
}

public enum SortingCriteriaType
{
    Ascending = 1,
    Descending = 2
}
```

### Conversión a Expression (Dynamic LINQ)

```csharp
public static string ToExpression(this SortingCriteria sort)
{
    string orderExpression = sort.Criteria == SortingCriteriaType.Ascending
        ? $"{sort.SortBy}"
        : $"{sort.SortBy} descending";
    return orderExpression;
}
```

**Uso**:
```csharp
var sorting = new SortingCriteria("Name", SortingCriteriaType.Descending);

var results = _session.Query<User>()
    .OrderBy(sorting.ToExpression())  // "Name descending"
    .ToList();
```

---

## Patrón GetManyAndCount

### GetManyAndCountResult<T>

Del proyecto [GetManyAndCountResult.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\repositories\GetManyAndCountResult.cs):

```csharp
public class GetManyAndCountResult<T>
{
    public const int DEFAULT_PAGE_SIZE = 25;

    public IEnumerable<T> Items { get; set; }
    public long Count { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public SortingCriteria Sorting { get; set; }

    public GetManyAndCountResult(IEnumerable<T> items, long count, int pageNumber, int pageSize, SortingCriteria sorting)
    {
        Items = items;
        Count = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        Sorting = sorting;
    }
}
```

### Implementación en NHReadOnlyRepository

Del proyecto [NHReadOnlyRepository.cs:134-144](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs:134-144):

```csharp
public GetManyAndCountResult<T> GetManyAndCount(string? query, string defaultSorting)
{
    var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

    // Execute query
    var items = this.Get(expression, pageNumber, pageSize, sortingCriteria);
    var total = this.Count(expression);

    // Return results
    return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
}
```

### Versión Asíncrona

```csharp
public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(
    string? query,
    string defaultSorting,
    CancellationToken ct = default)
{
    var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

    // ✅ Execute queries sequentially to avoid DataReader conversion issues
    var total = await this._session.Query<T>()
        .Where(expression)
        .CountAsync(ct);

    var items = await this._session.Query<T>()
        .OrderBy(sortingCriteria.ToExpression())
        .Where(expression)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
}
```

### PrepareQuery Helper

```csharp
private static (Expression<Func<T, bool>> expression, int pageNumber, int pageSize, SortingCriteria sortingCriteria)
    PrepareQuery(string? query, string defaultSorting)
{
    var queryString = string.IsNullOrEmpty(query) ? string.Empty : query;
    QueryStringParser queryStringParser = new(queryString);

    // Get pagination info
    int pageNumber = queryStringParser.ParsePageNumber();
    int pageSize = queryStringParser.ParsePageSize();

    // Get sorting info
    Sorting sorting = queryStringParser.ParseSorting<T>(defaultSorting);
    SortingCriteriaType directions = sorting.Direction == QueryStringParser.GetDescendingValue()
        ? SortingCriteriaType.Descending
        : SortingCriteriaType.Ascending;
    SortingCriteria sortingCriteria = new(sorting.By, directions);

    // Get filters
    IList<FilterOperator> filters = queryStringParser.ParseFilterOperators<T>();
    QuickSearch? quickSearch = queryStringParser.ParseQuery<T>();
    var expression = FilterExpressionParser.ParsePredicate<T>(filters);
    if (quickSearch != null)
        expression = FilterExpressionParser.ParseQueryValuesToExpression(expression, quickSearch);

    return (expression, pageNumber, pageSize, sortingCriteria);
}
```

---

## Operadores de Filtrado

### RelationalOperator

Del proyecto [RelationalOperator.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\RelationalOperator.cs):

```csharp
static class RelationalOperator
{
    public const string Equal = "equal";
    public const string NotEqual = "not_equal";
    public const string Contains = "contains";
    public const string StartsWith = "starts_with";
    public const string EndsWith = "ends_with";
    public const string Between = "between";
    public const string GreaterThan = "greater_than";
    public const string GreaterThanOrEqual = "greater_or_equal_than";
    public const string LessThan = "less_than";
    public const string LessThanOrEqual = "less_or_equal_than";
}
```

### Tabla de Operadores

| Operador | Alias | Query String | SQL Generado | Ejemplo |
|----------|-------|--------------|--------------|---------|
| `equal` | `eq` | `Status=active||eq` | `WHERE status = 'active'` | Estados exactos |
| `contains` | | `Name=john||contains` | `WHERE name LIKE '%john%'` | Búsqueda parcial |
| `greater_than` | `gt` | `Age=18||gt` | `WHERE age > 18` | Mayores de edad |
| `greater_or_equal_than` | `gte` | `Price=100||gte` | `WHERE price >= 100` | Precio mínimo |
| `less_than` | `lt` | `Stock=10||lt` | `WHERE stock < 10` | Stock bajo |
| `less_or_equal_than` | `lte` | `Discount=50||lte` | `WHERE discount <= 50` | Descuento máximo |
| `between` | | `Age=18|65||between` | `WHERE age >= 18 AND age <= 65` | Rango de edad |

### Ejemplos de Uso

#### 1. Equal (Igualdad)

```
GET /api/users?Email=john@example.com||eq
```

**Expresión generada**:
```csharp
u => u.Email == "john@example.com"
```

#### 2. Contains (Contiene)

```
GET /api/products?Name=laptop||contains
```

**Expresión generada**:
```csharp
p => p.Name.Contains("laptop")
```

#### 3. Between (Rango)

```
GET /api/users?Age=18|65||between
```

**Expresión generada**:
```csharp
u => u.Age >= 18 && u.Age <= 65
```

#### 4. Greater Than (Mayor que)

```
GET /api/products?Price=100||gt
```

**Expresión generada**:
```csharp
p => p.Price > 100
```

#### 5. Múltiples Filtros (AND)

```
GET /api/users?
  Email=john||contains&
  Age=18||gt&
  Status=active||eq
```

**Expresión generada**:
```csharp
u => u.Email.Contains("john") && u.Age > 18 && u.Status == "active"
```

---

## Quick Search

### ¿Qué es Quick Search?

Búsqueda rápida en **múltiples campos** simultáneamente, útil para cuadros de búsqueda global.

### ParseQuery en QueryStringParser

Del proyecto [QueryStringParser.cs:113-189](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\QueryStringParser.cs:113-189):

```csharp
public QuickSearch? ParseQuery<T>()
{
    if (string.IsNullOrEmpty(_queryString))
        return null;

    QueryStringArgs parameters = new(_queryString);

    if (!parameters.ContainsKey("query"))
        return null;

    string? query = parameters["query"].Split("||").FirstOrDefault();

    if (string.IsNullOrEmpty(query))
        throw new InvalidQueryStringArgumentException("query");

    QuickSearch quickSearch = new();
    PropertyInfo[] properties = typeof(T).GetProperties();

    // ✅ CASO 1: Búsqueda en TODOS los campos string
    if (parameters["query"].Split("||").Count() <= 1)
    {
        ICollection<string> stringFields = new List<string>();
        quickSearch.Value = query.RemoveAccents().ToLowerInvariant();

        foreach (PropertyInfo property in properties)
            if ((property.PropertyType == typeof(string) || property.PropertyType == typeof(int))
                && property.Name != "Id")
                stringFields.Add(property.Name);

        quickSearch.FieldNames = stringFields.ToList();
        return quickSearch;
    }

    // ✅ CASO 2: Búsqueda en campos ESPECÍFICOS
    quickSearch.Value = query;

    if (string.IsNullOrWhiteSpace(parameters["query"].Split("||")[1]))
        throw new InvalidQueryStringArgumentException("query_ColumnsToSearch");

    IList<string> fields = parameters["query"].Split("||")[1].Split("|");

    foreach (string field in fields)
        if (!properties.Any(p => p.Name.Equals(field, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidQueryStringArgumentException("query_ColumnsToSearch");

    quickSearch.FieldNames = fields;
    return quickSearch;
}
```

### Ejemplos de Quick Search

#### 1. Búsqueda en Todos los Campos String

```
GET /api/users?query=john
```

**Comportamiento**:
- Busca "john" en **TODOS** los campos de tipo string (excepto Id)
- Para `User`: buscaría en Name, Email, etc.

**Expresión generada** (OR):
```csharp
u => u.Name.Contains("john") || u.Email.Contains("john")
```

#### 2. Búsqueda en Campos Específicos

```
GET /api/users?query=john||Name|Email
```

**Comportamiento**:
- Busca "john" **SOLO** en campos Name y Email

**Expresión generada**:
```csharp
u => u.Name.Contains("john") || u.Email.Contains("john")
```

### ParseQueryValuesToExpression

```csharp
static public Expression<Func<T, bool>> ParseQueryValuesToExpression<T>(
    this Expression<Func<T, bool>> expression,
    QuickSearch quickSearch)
{
    Expression<Func<T, bool>> expressionJoin = c => true;
    int index = 0;

    foreach (var propertyName in quickSearch.FieldNames)
    {
        var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());
        Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
        propertyExpression = FilterExpressionParser.CallToStringMethod<T>(propertyExpression, propertyName);

        var constant = Expression.Constant(quickSearch.Value);
        var strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

        Expression? criteria = null;
        if (strContainsMethod != null)
        {
            criteria = Expression.Call(propertyExpression, strContainsMethod, new Expression[] { constant });
        }

        if (criteria != null)
        {
            var localExpressionJoin = Expression.Lambda<Func<T, bool>>(criteria, parameterExpression);
            expressionJoin = index == 0
                ? localExpressionJoin
                : ConcatExpressionsOrElse<T>(expressionJoin, localExpressionJoin);  // ✅ OR
        }
        index++;
    }

    expression = ConcatExpressionsAndAlso<T>(expression, expressionJoin);  // ✅ AND con otros filtros
    return expression;
}
```

---

## Mejores Prácticas

### ✅ 1. Usar Queries Asíncronas

**Correcto** ✅:
```csharp
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync();
}
```

**Incorrecto** ❌:
```csharp
public User? GetByEmail(string email)
{
    return _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefault();  // ❌ Bloquea el thread
}
```

### ✅ 2. Usar Parámetros en HQL (NO Concatenación)

**Correcto** ✅:
```csharp
var hql = "from User u where u.Email = :email";
var user = await _session.CreateQuery(hql)
    .SetParameter("email", email)
    .UniqueResultAsync<User?>();
```

**Incorrecto** ❌ (SQL Injection):
```csharp
var hql = $"from User u where u.Email = '{email}'";  // ❌ PELIGRO!
var user = await _session.CreateQuery(hql)
    .UniqueResultAsync<User?>();
```

### ✅ 3. Validar Propiedades en Dynamic LINQ

**Correcto** ✅:
```csharp
public Sorting ParseSorting<T>(string defaultFieldName)
{
    PropertyInfo[] properties = typeof(T).GetProperties();

    if (!properties.Any(p => p.Name.Equals(sortByValue, StringComparison.OrdinalIgnoreCase)))
        throw new InvalidQueryStringArgumentException("sortBy");

    return new Sorting(sortByValue, sortDirection);
}
```

**Incorrecto** ❌:
```csharp
// ❌ No valida si la propiedad existe
var sortBy = Request.Query["sortBy"];
var results = _session.Query<User>()
    .OrderBy(sortBy)  // ❌ Puede causar excepción si propiedad no existe
    .ToList();
```

### ✅ 4. Ejecutar Count y Query Secuencialmente (Async)

**Correcto** ✅:
```csharp
public async Task<GetManyAndCountResult<T>> GetManyAndCountAsync(string? query, string defaultSorting, CancellationToken ct = default)
{
    var (expression, pageNumber, pageSize, sortingCriteria) = PrepareQuery(query, defaultSorting);

    // ✅ Execute queries sequentially to avoid DataReader conversion issues
    var total = await this._session.Query<T>()
        .Where(expression)
        .CountAsync(ct);

    var items = await this._session.Query<T>()
        .OrderBy(sortingCriteria.ToExpression())
        .Where(expression)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    return new GetManyAndCountResult<T>(items, total, pageNumber, pageSize, sortingCriteria);
}
```

**Incorrecto** ❌:
```csharp
// ❌ Ejecutar en paralelo puede causar problemas con DataReader
var totalTask = _session.Query<T>().Where(expression).CountAsync(ct);
var itemsTask = _session.Query<T>().Where(expression).ToListAsync(ct);

await Task.WhenAll(totalTask, itemsTask);  // ❌ Puede fallar
```

### ✅ 5. Usar CancellationToken en Queries Asíncronas

**Correcto** ✅:
```csharp
public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, CancellationToken ct = default)
{
    return await _session.Query<User>()
        .Where(u => u.Name.Contains(searchTerm))
        .ToListAsync(ct);  // ✅ Permite cancelar la operación
}
```

**Incorrecto** ❌:
```csharp
public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
{
    return await _session.Query<User>()
        .Where(u => u.Name.Contains(searchTerm))
        .ToListAsync();  // ❌ No permite cancelación
}
```

---

## Antipatrones Comunes

### ❌ 1. Usar ToList() Antes de Filtrar

**Problema**:
```csharp
// ❌ Carga TODOS los usuarios en memoria primero
var users = _session.Query<User>().ToList()
    .Where(u => u.Age > 18)
    .ToList();
```

**Impacto**: Carga toda la tabla en memoria antes de filtrar.

**Solución** ✅:
```csharp
// ✅ Filtro se ejecuta en base de datos
var users = _session.Query<User>()
    .Where(u => u.Age > 18)
    .ToList();
```

### ❌ 2. N+1 Query Problem

**Problema**:
```csharp
var users = _session.Query<User>().ToList();

foreach (var user in users)
{
    // ❌ Una query por cada usuario para cargar Roles
    var roles = user.Roles.ToList();
}
```

**Impacto**: Si hay 100 usuarios, se ejecutan 101 queries (1 + 100).

**Solución** ✅:
```csharp
var users = _session.Query<User>()
    .Fetch(u => u.Roles)  // ✅ Eager loading con JOIN
    .ToList();

foreach (var user in users)
{
    var roles = user.Roles;  // ✅ Ya cargado, no ejecuta query
}
```

### ❌ 3. No Usar Skip/Take para Paginación

**Problema**:
```csharp
var users = _session.Query<User>()
    .ToList()
    .Skip(25)  // ❌ Skip en memoria
    .Take(25)  // ❌ Take en memoria
    .ToList();
```

**Impacto**: Carga TODOS los registros antes de paginar.

**Solución** ✅:
```csharp
var users = _session.Query<User>()
    .Skip(25)  // ✅ OFFSET en SQL
    .Take(25)  // ✅ LIMIT en SQL
    .ToList();
```

### ❌ 4. Concatenar Strings en HQL

**Problema**:
```csharp
// ❌ SQL Injection vulnerability
var email = userInput;
var hql = $"from User u where u.Email = '{email}'";
var user = _session.CreateQuery(hql).UniqueResult<User>();
```

**Impacto**: Si `userInput = "' OR '1'='1"`, se ejecuta:
```sql
SELECT * FROM users WHERE email = '' OR '1'='1'  -- Devuelve todos los usuarios!
```

**Solución** ✅:
```csharp
var hql = "from User u where u.Email = :email";
var user = _session.CreateQuery(hql)
    .SetParameter("email", email)  // ✅ Parámetros escapados
    .UniqueResult<User>();
```

### ❌ 5. No Validar Tipos en FilterExpressionParser

**Problema**:
```csharp
// ❌ No valida si el tipo de dato es compatible
var constantExpression = Expression.Constant(filterValue);
criteria = Expression.GreaterThan(propertyExpression, constantExpression);
```

**Impacto**: Falla en runtime si `filterValue` no es del tipo correcto.

**Solución** ✅:
```csharp
private static Expression CreateConstantExpression<T>(string propertyName, string constantValue)
{
    PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName);
    var actualType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

    // ✅ Validación de tipo DateTime
    if (actualType == typeof(DateTime))
    {
        if (DateTime.TryParseExact(constantValue,
            new[] { "yyyy-MM-dd" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dateValue))
        {
            return Expression.Constant(dateValue, propertyInfo.PropertyType);
        }
        throw new InvalidFilterArgumentException($"Invalid date format");
    }

    // ✅ Conversión con validación
    object convertedValue = Convert.ChangeType(constantValue, actualType);
    return Expression.Constant(convertedValue, propertyInfo.PropertyType);
}
```

---

## Ejemplos Completos

### Ejemplo 1: Query Simple con LINQ

```csharp
// Obtener usuario por email
public async Task<User?> GetByEmailAsync(string email)
{
    return await _session.Query<User>()
        .Where(u => u.Email == email)
        .SingleOrDefaultAsync();
}
```

**SQL Generado**:
```sql
SELECT * FROM users
WHERE email = :p0;
```

### Ejemplo 2: HQL con unaccent

```csharp
// Búsqueda case-insensitive sin acentos
public async Task<TechnicalStandard?> GetByCodeAsync(string code)
{
    if (string.IsNullOrWhiteSpace(code))
        return null;

    var hql = @"
        from TechnicalStandard ts
        where lower(unaccent(ts.Code)) = lower(unaccent(:code))";

    return await _session.CreateQuery(hql)
        .SetParameter("code", code)
        .UniqueResultAsync<TechnicalStandard?>();
}
```

**SQL Generado**:
```sql
SELECT * FROM technical_standards
WHERE LOWER(UNACCENT(code)) = LOWER(UNACCENT(:p0));
```

### Ejemplo 3: Paginación con LINQ

```csharp
public IEnumerable<User> GetUsersPaginated(int page, int pageSize)
{
    return _session.Query<User>()
        .OrderBy(u => u.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();
}
```

**SQL Generado**:
```sql
SELECT * FROM users
ORDER BY name ASC
LIMIT 25 OFFSET 25;
```

### Ejemplo 4: GetManyAndCount Completo

```csharp
public async Task<GetManyAndCountResult<User>> GetUsersAsync(string? queryString)
{
    return await GetManyAndCountAsync(queryString, "Name");
}
```

**Query String**:
```
GET /api/users?pageNumber=2&pageSize=25&sortBy=Email&sortDirection=desc&Status=active||eq&query=john
```

**Resultado**:
```json
{
  "items": [ /* 25 usuarios */ ],
  "count": 150,
  "pageNumber": 2,
  "pageSize": 25,
  "sorting": {
    "sortBy": "Email",
    "criteria": "Descending"
  }
}
```

### Ejemplo 5: Filtros Múltiples con Between

```csharp
// Query string
// GET /api/users?Age=18|65||between&Status=active|pending||contains&Email=gmail||contains

var result = await repository.GetManyAndCountAsync(queryString, "Name");
```

**SQL Generado**:
```sql
SELECT * FROM users
WHERE age >= 18
  AND age <= 65
  AND (status = 'active' OR status = 'pending')
  AND email LIKE '%gmail%'
ORDER BY name ASC
LIMIT 25 OFFSET 0;
```

### Ejemplo 6: Quick Search en Múltiples Campos

```csharp
// Query string
// GET /api/technical-standards?query=NOM||Code|Name|Edition

var result = await repository.GetManyAndCountAsync(queryString, "Code");
```

**Expresión generada**:
```csharp
ts => (ts.Code.Contains("NOM") || ts.Name.Contains("NOM") || ts.Edition.Contains("NOM"))
```

**SQL Generado**:
```sql
SELECT * FROM technical_standards
WHERE code LIKE '%NOM%'
   OR name LIKE '%NOM%'
   OR edition LIKE '%NOM%'
ORDER BY code ASC;
```

---

## Referencias

### Documentación Oficial

- [NHibernate Official Documentation](https://nhibernate.info/doc/)
- [NHibernate LINQ Provider](https://nhibernate.info/doc/nhibernate-reference/query-linq.html)
- [HQL Reference](https://nhibernate.info/doc/nhibernate-reference/queryhql.html)
- [QueryOver API](https://nhibernate.info/doc/nhibernate-reference/query-queryover.html)
- [System.Linq.Dynamic.Core](https://dynamic-linq.net/)

### Archivos del Proyecto de Referencia

**Repositorios con Queries**:
- [NHReadOnlyRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHReadOnlyRepository.cs) - LINQ, paginación, GetManyAndCount
- [NHUserRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHUserRepository.cs) - LINQ básico
- [NHTechnicalStandardRepository.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\NHTechnicalStandardRepository.cs) - HQL con unaccent

**Sistema de Filtrado**:
- [QueryStringParser.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\QueryStringParser.cs) - Parsing de query strings
- [FilterExpressionParser.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\FilterExpressionParser.cs) - Construcción de expresiones
- [RelationalOperator.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\RelationalOperator.cs) - Operadores soportados
- [FilterOperator.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\filtering\FilterOperator.cs) - Modelo de filtro

**Sorting y Paginación**:
- [SortingCriteria.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\repositories\SortingCriteria.cs) - Modelo de ordenamiento
- [SortingCriteriaExtender.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.infrastructure\nhibernate\SortingCriteriaExtender.cs) - Extensión ToExpression
- [GetManyAndCountResult.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.domain\interfaces\repositories\GetManyAndCountResult.cs) - Resultado paginado

### Guías Relacionadas

- [README.md](./README.md) - Overview de NHibernate
- [repositories.md](./repositories.md) - Implementación de repositorios
- [mappers.md](./mappers.md) - Mapping by Code

---

**Siguiente**: [unit-of-work.md](./unit-of-work.md) - Patrón Unit of Work y transacciones
