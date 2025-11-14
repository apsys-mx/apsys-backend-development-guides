# Caching en ASP.NET Core

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Estado**: ✅ Completada

## Tabla de Contenidos

1. [Introducción](#1-introducción)
2. [¿Cuándo Usar Caching?](#2-cuándo-usar-caching)
3. [Tipos de Caching en ASP.NET Core](#3-tipos-de-caching-en-aspnet-core)
4. [Estrategias de Caching](#4-estrategias-de-caching)
5. [Patrones de Invalidación](#5-patrones-de-invalidación)
6. [Diseño de Cache Keys](#6-diseño-de-cache-keys)
7. [Configuración y Estrategias de Expiración](#7-configuración-y-estrategias-de-expiración)
8. [Comparación: Memory vs Distributed Cache](#8-comparación-memory-vs-distributed-cache)
9. [Best Practices](#9-best-practices)
10. [Anti-Patterns Comunes](#10-anti-patterns-comunes)
11. [Errores Comunes y Soluciones](#11-errores-comunes-y-soluciones)
12. [Referencias](#12-referencias)

---

## 1. Introducción

El **caching** es una técnica fundamental de optimización de rendimiento que almacena datos frecuentemente accedidos en una ubicación de rápido acceso, reduciendo:

- **Latencia**: Tiempo de respuesta de la aplicación
- **Carga en la base de datos**: Consultas repetitivas
- **Costos de infraestructura**: Procesamiento y ancho de banda
- **Llamadas a APIs externas**: Reducción de rate limiting

ASP.NET Core proporciona cuatro mecanismos principales de caching:

| Tipo | Ubicación | Alcance | Uso Principal |
|------|-----------|---------|---------------|
| **In-Memory Cache** | Memoria del servidor | Single server | Datos de aplicación |
| **Distributed Cache** | Redis/SQL Server | Multi-server | Datos compartidos |
| **Response Caching** | Cliente/Proxy | Cliente | Respuestas HTTP completas |
| **Output Caching** | Servidor (.NET 7+) | Servidor | Respuestas dinámicas |

---

## 2. ¿Cuándo Usar Caching?

### ✅ Escenarios Recomendados

#### 1. **Datos Raramente Cambiantes**
```csharp
// ✅ BIEN: Cachear catálogos que cambian pocas veces al día
public async Task<List<Country>> GetCountriesAsync()
{
    const string cacheKey = "countries:all";

    if (!_cache.TryGetValue(cacheKey, out List<Country> countries))
    {
        countries = await _dbContext.Countries
            .AsNoTracking()
            .ToListAsync();

        _cache.Set(cacheKey, countries, TimeSpan.FromHours(24));
    }

    return countries;
}
```

#### 2. **Operaciones Costosas**
```csharp
// ✅ BIEN: Cachear resultados de operaciones complejas
public async Task<DashboardStatistics> GetDashboardStatsAsync(int userId)
{
    var cacheKey = $"dashboard:stats:{userId}";

    if (!_cache.TryGetValue(cacheKey, out DashboardStatistics stats))
    {
        stats = new DashboardStatistics
        {
            TotalOrders = await _dbContext.Orders.CountAsync(o => o.UserId == userId),
            PendingInvoices = await _dbContext.Invoices.CountAsync(i => i.UserId == userId && i.Status == "Pending"),
            // 10+ queries más...
        };

        _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
    }

    return stats;
}
```

#### 3. **Llamadas a APIs Externas**
```csharp
// ✅ BIEN: Cachear respuestas de APIs de terceros
public async Task<ExchangeRates> GetExchangeRatesAsync()
{
    const string cacheKey = "exchange:rates";

    var cachedRates = await _distributedCache.GetStringAsync(cacheKey);
    if (cachedRates != null)
    {
        return JsonSerializer.Deserialize<ExchangeRates>(cachedRates);
    }

    var rates = await _currencyApiClient.GetRatesAsync();

    await _distributedCache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(rates),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });

    return rates;
}
```

#### 4. **Datos de Lookup/Reference**
```csharp
// ✅ BIEN: Cachear tablas de referencia
public async Task<ProductCategory> GetCategoryByIdAsync(int id)
{
    var cacheKey = $"category:{id}";

    if (!_cache.TryGetValue(cacheKey, out ProductCategory category))
    {
        category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category != null)
        {
            _cache.Set(cacheKey, category, TimeSpan.FromHours(12));
        }
    }

    return category;
}
```

### ❌ Escenarios NO Recomendados

#### 1. **Datos en Tiempo Real**
```csharp
// ❌ MAL: No cachear datos que deben estar siempre actualizados
public async Task<StockPrice> GetCurrentStockPriceAsync(string symbol)
{
    // No cachear - los precios cambian constantemente
    return await _stockApiClient.GetPriceAsync(symbol);
}
```

#### 2. **Datos Sensibles o de Usuario Autenticado**
```csharp
// ❌ MAL: No cachear información sensible sin cuidado
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    // ⚠️ Cuidado: Puede causar filtración de datos entre usuarios
    var cacheKey = $"profile:{userId}"; // ← Debe incluir información de sesión

    // Si usas ResponseCache, NUNCA cachees respuestas autenticadas
}
```

#### 3. **Datos Únicos por Usuario (Sin Estrategia)**
```csharp
// ❌ MAL: Cachear datos muy específicos de usuario sin estrategia
public async Task<UserCart> GetUserCartAsync(int userId, string sessionId)
{
    // ❌ Cada usuario/sesión tiene un carrito único
    // El cache hit rate será muy bajo, desperdiciando memoria

    var cacheKey = $"cart:{userId}:{sessionId}";

    // Solo tiene sentido si el mismo usuario hace múltiples requests
    // en un período corto (ej: durante checkout)
}
```

#### 4. **Datos Grandes Sin Justificación**
```csharp
// ❌ MAL: Cachear datasets masivos sin analizar costo/beneficio
public async Task<List<LogEntry>> GetAllLogsAsync()
{
    // ❌ Millones de registros consumirán toda la memoria
    // Mejor: paginar o cachear solo consultas específicas

    const string cacheKey = "logs:all";

    if (!_cache.TryGetValue(cacheKey, out List<LogEntry> logs))
    {
        logs = await _dbContext.Logs.ToListAsync(); // ← Potencialmente gigante
        _cache.Set(cacheKey, logs, TimeSpan.FromHours(1));
    }

    return logs;
}
```

---

## 3. Tipos de Caching en ASP.NET Core

### 3.1. In-Memory Cache (`IMemoryCache`)

**Ubicación**: Memoria del proceso de la aplicación
**Alcance**: Single server
**Ideal para**: Datos de aplicación, lookups, configuración

#### Configuración Básica

```csharp
// Program.cs o Startup.cs
builder.Services.AddMemoryCache();
```

#### Uso en Servicios

```csharp
using Microsoft.Extensions.Caching.Memory;

public class ProductService
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _dbContext;

    public ProductService(IMemoryCache cache, AppDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task<Product> GetProductByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";

        // ✅ Patrón TryGetValue
        if (!_cache.TryGetValue(cacheKey, out Product product))
        {
            product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    SlidingExpiration = TimeSpan.FromMinutes(10),
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(cacheKey, product, cacheOptions);
            }
        }

        return product;
    }
}
```

#### Opciones de Configuración

```csharp
var cacheOptions = new MemoryCacheEntryOptions
{
    // ✅ Expiración absoluta: El item expira después de 1 hora sin importar el acceso
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),

    // ✅ Expiración deslizante: Se renueva cada vez que se accede
    SlidingExpiration = TimeSpan.FromMinutes(15),

    // ✅ Prioridad: Qué tan importante es retener este item
    Priority = CacheItemPriority.High, // Low, Normal, High, NeverRemove

    // ✅ Tamaño: Para control de memoria (requiere configurar SizeLimit en AddMemoryCache)
    Size = 1,

    // ✅ Callbacks: Notificaciones cuando el item es removido
    PostEvictionCallbacks =
    {
        new PostEvictionCallbackRegistration
        {
            EvictionCallback = (key, value, reason, state) =>
            {
                // Lógica cuando el item es removido
                _logger.LogInformation($"Cache key {key} was evicted. Reason: {reason}");
            }
        }
    }
};

_cache.Set("myKey", myValue, cacheOptions);
```

### 3.2. Distributed Cache (`IDistributedCache`)

**Ubicación**: Redis, SQL Server, NCache
**Alcance**: Multi-server (web farms, clusters)
**Ideal para**: Sesiones, datos compartidos, escalabilidad horizontal

#### Configuración con Redis

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";
});
```

#### Configuración con SQL Server

```csharp
// Program.cs
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DistributedCache");
    options.SchemaName = "dbo";
    options.TableName = "AppCache";
});
```

#### Uso en Servicios

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class OrderService
{
    private readonly IDistributedCache _cache;

    public OrderService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<Order> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"order:{id}";

        // ✅ Intentar obtener del cache
        var cachedOrder = await _cache.GetStringAsync(cacheKey, ct);

        if (cachedOrder != null)
        {
            return JsonSerializer.Deserialize<Order>(cachedOrder);
        }

        // ✅ Cache miss: obtener de la fuente
        var order = await GetOrderFromDatabaseAsync(id, ct);

        if (order != null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(order),
                cacheOptions,
                ct);
        }

        return order;
    }

    // ✅ Método de invalidación
    public async Task InvalidateOrderCacheAsync(int orderId, CancellationToken ct = default)
    {
        var cacheKey = $"order:{orderId}";
        await _cache.RemoveAsync(cacheKey, ct);
    }
}
```

#### Métodos de `IDistributedCache`

```csharp
// ✅ Get: Obtener como byte array
byte[] bytes = await _cache.GetAsync("key");

// ✅ GetString: Obtener como string (más común)
string value = await _cache.GetStringAsync("key");

// ✅ Set: Almacenar byte array
await _cache.SetAsync("key", bytes, options);

// ✅ SetString: Almacenar string (más común)
await _cache.SetStringAsync("key", jsonString, options);

// ✅ Refresh: Renovar expiración sin leer el valor
await _cache.RefreshAsync("key");

// ✅ Remove: Eliminar del cache
await _cache.RemoveAsync("key");
```

### 3.3. Response Caching

**Ubicación**: Cliente (navegador) o proxy intermedio
**Alcance**: HTTP responses
**Ideal para**: Contenido estático, páginas públicas

#### Configuración

```csharp
// Program.cs
builder.Services.AddResponseCaching();

var app = builder.Build();

// ⚠️ IMPORTANTE: Orden del middleware
app.UseHttpsRedirection();
app.UseResponseCaching(); // ← Debe ir ANTES de UseAuthorization
app.UseAuthorization();
app.MapControllers();
```

#### Uso con Atributo `[ResponseCache]`

```csharp
[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    // ✅ Cachear por 60 segundos
    [HttpGet("categories")]
    [ResponseCache(Duration = 60)]
    public IActionResult GetCategories()
    {
        var categories = GetCategoriesFromDatabase();
        return Ok(categories);
    }

    // ✅ Cachear por 30 segundos, variar por User-Agent
    [HttpGet("products")]
    [ResponseCache(Duration = 30, VaryByHeader = "User-Agent")]
    public IActionResult GetProducts()
    {
        var products = GetProductsFromDatabase();
        return Ok(products);
    }

    // ✅ No cachear (para endpoints autenticados)
    [HttpGet("user/profile")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetUserProfile()
    {
        // Datos sensibles - nunca cachear
        return Ok(GetCurrentUserProfile());
    }
}
```

#### Headers HTTP Generados

```http
# Para [ResponseCache(Duration = 60)]
Cache-Control: public, max-age=60

# Para [ResponseCache(Duration = 30, VaryByHeader = "User-Agent")]
Cache-Control: public, max-age=30
Vary: User-Agent

# Para [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
Cache-Control: no-store, no-cache
```

### 3.4. Output Caching (.NET 7+)

**Ubicación**: Servidor (middleware)
**Alcance**: Respuestas HTTP del servidor
**Ideal para**: APIs dinámicas, revalidación, tag-based invalidation

#### Configuración

```csharp
// Program.cs (.NET 7+)
builder.Services.AddOutputCache(options =>
{
    // ✅ Política por defecto: cachear 20 segundos
    options.AddBasePolicy(policy =>
        policy.Expire(TimeSpan.FromSeconds(20)));

    // ✅ Política personalizada: cachear 60 segundos
    options.AddPolicy("60s", policy =>
        policy.Expire(TimeSpan.FromSeconds(60)));

    // ✅ Política con tags: permite invalidación por grupo
    options.AddPolicy("blog-posts", policy =>
        policy.Tag("blog"));

    // ✅ Política con VaryByQuery: cachear por parámetros
    options.AddPolicy("by-culture", policy =>
        policy.SetVaryByQuery("culture"));
});

var app = builder.Build();

app.UseOutputCache(); // ← Middleware de Output Cache
app.MapControllers();
```

#### Uso en Minimal APIs

```csharp
// ✅ Aplicar política por defecto
app.MapGet("/products", async (AppDbContext db) =>
{
    return await db.Products.ToListAsync();
})
.CacheOutput();

// ✅ Aplicar política personalizada
app.MapGet("/categories", async (AppDbContext db) =>
{
    return await db.Categories.ToListAsync();
})
.CacheOutput("60s");

// ✅ Variar por query string
app.MapGet("/search", async (string q, AppDbContext db) =>
{
    return await db.Products
        .Where(p => p.Name.Contains(q))
        .ToListAsync();
})
.CacheOutput(policy => policy.SetVaryByQuery("q"));

// ✅ Tags para invalidación grupal
app.MapGet("/blog/{id}", async (int id, BlogService service) =>
{
    return await service.GetPostByIdAsync(id);
})
.CacheOutput(policy => policy.Tag("blog"));

app.MapPost("/blog", async (BlogPost post, BlogService service) =>
{
    await service.CreatePostAsync(post);

    // ✅ Invalidar todos los endpoints con tag "blog"
    await service.InvalidateBlogCacheAsync();

    return Results.Created($"/blog/{post.Id}", post);
});
```

#### Uso en Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class BlogController : ControllerBase
{
    private readonly IOutputCacheStore _cacheStore;

    public BlogController(IOutputCacheStore cacheStore)
    {
        _cacheStore = cacheStore;
    }

    // ✅ Aplicar política por nombre
    [HttpGet]
    [OutputCache(PolicyName = "blog-posts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await GetPostsFromDatabase();
        return Ok(posts);
    }

    // ✅ Cachear con expiración específica
    [HttpGet("{id}")]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetPost(int id)
    {
        var post = await GetPostByIdAsync(id);
        return Ok(post);
    }

    // ✅ No cachear
    [HttpPost]
    [OutputCache(NoStore = true)]
    public async Task<IActionResult> CreatePost(BlogPost post)
    {
        await SavePostAsync(post);

        // ✅ Invalidar cache por tag
        await _cacheStore.EvictByTagAsync("blog", default);

        return Created($"/api/blog/{post.Id}", post);
    }
}
```

#### Revalidación con `If-Modified-Since`

Output Caching automáticamente maneja la revalidación HTTP:

```http
# Primera request
GET /api/products HTTP/1.1
Host: example.com

# Response
HTTP/1.1 200 OK
Last-Modified: Wed, 15 Nov 2023 12:00:00 GMT
Cache-Control: public, max-age=60
[... product data ...]

# Segunda request (antes de expiración)
GET /api/products HTTP/1.1
Host: example.com
If-Modified-Since: Wed, 15 Nov 2023 12:00:00 GMT

# Response si no cambió
HTTP/1.1 304 Not Modified
```

---

## 4. Estrategias de Caching

### 4.1. Cache-Aside (Lazy Loading)

**Descripción**: La aplicación lee del cache primero; si hay miss, lee de la fuente y actualiza el cache.

**Cuándo usar**: La mayoría de escenarios, especialmente datos de lectura frecuente.

```csharp
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";

    // 1️⃣ Intentar leer del cache
    if (_cache.TryGetValue(cacheKey, out Product product))
    {
        return product; // ✅ Cache hit
    }

    // 2️⃣ Cache miss: leer de la fuente
    product = await _dbContext.Products
        .FirstOrDefaultAsync(p => p.Id == id);

    // 3️⃣ Actualizar el cache
    if (product != null)
    {
        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(30));
    }

    return product;
}
```

**Pros**:
- ✅ Fácil de implementar
- ✅ El cache solo contiene datos realmente solicitados
- ✅ Fallos del cache no afectan disponibilidad

**Cons**:
- ❌ Primera request siempre es lenta (cache miss)
- ❌ Puede causar "cache stampede" (ver sección de anti-patterns)

### 4.2. Read-Through

**Descripción**: El cache mismo se encarga de cargar datos cuando hay miss (abstracción completa).

**Cuándo usar**: Cuando quieres simplificar la lógica del servicio.

```csharp
// ✅ Usando el método Get con factory
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";

    // El cache automáticamente ejecuta la factory si hay miss
    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        entry.SlidingExpiration = TimeSpan.FromMinutes(10);

        // Este código solo se ejecuta en cache miss
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    });
}
```

**Pros**:
- ✅ Código más limpio y conciso
- ✅ Menos propenso a errores
- ✅ La lógica de caching está centralizada

**Cons**:
- ❌ Menos control sobre el flujo de carga
- ❌ Puede ser más difícil de debuggear

### 4.3. Write-Through

**Descripción**: Los datos se escriben simultáneamente al cache y a la fuente de datos.

**Cuándo usar**: Cuando necesitas consistencia inmediata entre cache y DB.

```csharp
public async Task<Product> UpdateProductAsync(Product product)
{
    var cacheKey = $"product:{product.Id}";

    // 1️⃣ Actualizar en la base de datos
    _dbContext.Products.Update(product);
    await _dbContext.SaveChangesAsync();

    // 2️⃣ Actualizar en el cache (Write-Through)
    _cache.Set(cacheKey, product, TimeSpan.FromMinutes(30));

    // ✅ Ambos están ahora sincronizados
    return product;
}
```

**Pros**:
- ✅ Cache siempre está actualizado
- ✅ Lecturas subsecuentes son rápidas
- ✅ No hay inconsistencia

**Cons**:
- ❌ Escrituras son más lentas
- ❌ Puedes cachear datos que nunca se lean

### 4.4. Write-Behind (Write-Back)

**Descripción**: Los datos se escriben primero al cache y luego asincrónicamente a la fuente.

**Cuándo usar**: Escrituras muy frecuentes, tolerancia a pérdida temporal.

```csharp
public async Task<Product> UpdateProductAsync(Product product)
{
    var cacheKey = $"product:{product.Id}";

    // 1️⃣ Actualizar el cache inmediatamente
    _cache.Set(cacheKey, product, TimeSpan.FromMinutes(30));

    // 2️⃣ Encolar para persistencia asíncrona
    await _backgroundQueue.QueueBackgroundWorkItemAsync(async ct =>
    {
        // Esto se ejecuta en background
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(ct);
    });

    return product;
}
```

**Pros**:
- ✅ Escrituras extremadamente rápidas
- ✅ Reduce carga en la base de datos
- ✅ Ideal para high-throughput scenarios

**Cons**:
- ❌ Riesgo de pérdida de datos si el servidor falla
- ❌ Complejidad adicional
- ❌ Requiere queue/background processing

### 4.5. Refresh-Ahead

**Descripción**: El cache se actualiza automáticamente antes de que expire.

**Cuándo usar**: Datos críticos que no pueden tener latencia en cache miss.

```csharp
public async Task<List<Country>> GetCountriesAsync()
{
    const string cacheKey = "countries:all";

    if (_cache.TryGetValue(cacheKey, out List<Country> countries))
    {
        return countries;
    }

    countries = await LoadCountriesFromDatabaseAsync();

    var cacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),

        // ✅ Callback: Refresh antes de expirar
        RegisterPostEvictionCallback = async (key, value, reason, state) =>
        {
            if (reason == EvictionReason.Expired)
            {
                // Recargar inmediatamente
                var newCountries = await LoadCountriesFromDatabaseAsync();
                _cache.Set(cacheKey, newCountries, TimeSpan.FromHours(24));
            }
        }
    };

    _cache.Set(cacheKey, countries, cacheOptions);
    return countries;
}
```

---

## 5. Patrones de Invalidación

### 5.1. Invalidación por Tiempo (Time-Based)

**Más común**: Absolute y Sliding Expiration.

```csharp
// ✅ Absolute Expiration: Expira después de 1 hora sin importar acceso
_cache.Set("key", value, TimeSpan.FromHours(1));

// ✅ Sliding Expiration: Se renueva si se accede dentro de 15 minutos
var options = new MemoryCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromMinutes(15)
};
_cache.Set("key", value, options);

// ✅ Combinación: Expira en máximo 1 hora, pero se renueva si se accede
var combinedOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
    SlidingExpiration = TimeSpan.FromMinutes(15)
};
_cache.Set("key", value, combinedOptions);
```

### 5.2. Invalidación Explícita (Event-Based)

**Cuándo usar**: Cuando los datos cambian y necesitas invalidar inmediatamente.

```csharp
public class ProductService
{
    public async Task UpdateProductAsync(Product product)
    {
        // 1️⃣ Actualizar en base de datos
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync();

        // 2️⃣ Invalidar cache
        var cacheKey = $"product:{product.Id}";
        _cache.Remove(cacheKey);

        // ✅ También invalidar listas relacionadas
        _cache.Remove("products:all");
        _cache.Remove($"products:category:{product.CategoryId}");
    }

    public async Task DeleteProductAsync(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        // ✅ Invalidar múltiples claves
        _cache.Remove($"product:{id}");
        _cache.Remove("products:all");
        _cache.Remove($"products:category:{product.CategoryId}");
    }
}
```

### 5.3. Invalidación por Prefix/Pattern

**Útil para**: Invalidar grupos de claves relacionadas.

```csharp
// ⚠️ IMemoryCache NO soporta RemoveByPrefix nativamente
// Solución 1: Tracking manual de keys

public class PrefixCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _keysByPrefix;

    public void Set(string prefix, string key, object value, TimeSpan expiration)
    {
        var fullKey = $"{prefix}:{key}";

        // Track key by prefix
        _keysByPrefix.AddOrUpdate(
            prefix,
            _ => new HashSet<string> { fullKey },
            (_, set) => { set.Add(fullKey); return set; });

        _cache.Set(fullKey, value, expiration);
    }

    public void RemoveByPrefix(string prefix)
    {
        if (_keysByPrefix.TryRemove(prefix, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }
    }
}

// Solución 2: Usar IDistributedCache con Redis (soporta SCAN)
// Ver guía redis.md para implementación
```

### 5.4. Invalidación por Tags (Output Caching .NET 7+)

**Mejor opción**: Para invalidación grupal en .NET 7+.

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("products", policy => policy.Tag("products-tag"));
    options.AddPolicy("categories", policy => policy.Tag("categories-tag"));
});

// Controller
[HttpPost("products")]
public async Task<IActionResult> CreateProduct(Product product)
{
    await _productService.CreateAsync(product);

    // ✅ Invalidar todos los endpoints con tag "products-tag"
    await HttpContext.RequestServices
        .GetRequiredService<IOutputCacheStore>()
        .EvictByTagAsync("products-tag", default);

    return Created($"/products/{product.Id}", product);
}
```

---

## 6. Diseño de Cache Keys

### 6.1. Convenciones de Naming

```csharp
// ✅ BIEN: Namespace jerárquico con separadores
"product:123"
"product:category:5"
"user:456:profile"
"order:789:items"
"cache:v1:exchange-rates:USD"

// ❌ MAL: Keys sin estructura
"p123"
"userprofile"
"mydata"
```

### 6.2. Patrones Comunes

```csharp
// ✅ Entidad individual
$"product:{productId}"

// ✅ Lista filtrada
$"products:category:{categoryId}"
$"orders:user:{userId}:status:pending"

// ✅ Con versionado
$"cache:v2:countries:all"

// ✅ Con scope de tenant
$"tenant:{tenantId}:products:all"

// ✅ Con cultura/idioma
$"product:{productId}:locale:{culture}"
```

### 6.3. Helper para Cache Keys

```csharp
public static class CacheKeys
{
    // ✅ Constantes para prefixes
    private const string ProductPrefix = "product";
    private const string CategoryPrefix = "category";
    private const string UserPrefix = "user";

    // ✅ Métodos tipados para generar keys
    public static string Product(int id) => $"{ProductPrefix}:{id}";

    public static string ProductsByCategory(int categoryId)
        => $"{ProductPrefix}:category:{categoryId}";

    public static string UserProfile(int userId)
        => $"{UserPrefix}:{userId}:profile";

    public static string UserOrders(int userId, string status = null)
    {
        var key = $"{UserPrefix}:{userId}:orders";
        return status != null ? $"{key}:status:{status}" : key;
    }

    // ✅ Métodos para invalidación por prefix
    public static string ProductPrefix() => ProductPrefix;
    public static string UserPrefix(int userId) => $"{UserPrefix}:{userId}";
}

// Uso
var cacheKey = CacheKeys.Product(123);
var ordersKey = CacheKeys.UserOrders(456, "pending");
```

---

## 7. Configuración y Estrategias de Expiración

### 7.1. Guías por Tipo de Dato

| Tipo de Dato | Expiration Recomendada | Tipo |
|--------------|------------------------|------|
| **Catálogos** (Countries, Categories) | 12-24 horas | Absolute |
| **Configuración** (Settings, Features) | 5-15 minutos | Sliding |
| **Datos de Usuario** (Profile, Preferences) | 15-30 minutos | Sliding |
| **Precios/Inventario** | 1-5 minutos | Absolute |
| **APIs Externas** (Exchange rates) | 30-60 minutos | Absolute |
| **Estadísticas** (Dashboard) | 5-10 minutos | Absolute |
| **Búsquedas** (Search results) | 5-15 minutos | Sliding |

### 7.2. Configuración de Tamaño de Cache

```csharp
// Program.cs
builder.Services.AddMemoryCache(options =>
{
    // ✅ Límite de tamaño (en unidades abstractas)
    options.SizeLimit = 1024;

    // ✅ Porcentaje de compactación cuando se alcanza el límite
    options.CompactionPercentage = 0.25; // Remover 25% de items de baja prioridad

    // ✅ Intervalo de escaneo para expiración
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

// Uso con Size
var cacheOptions = new MemoryCacheEntryOptions
{
    Size = 1, // ← Este item consume 1 unidad del SizeLimit
    Priority = CacheItemPriority.Low
};

_cache.Set("myKey", largeObject, cacheOptions);
```

### 7.3. Prioridades de Cache

```csharp
// ✅ NeverRemove: Datos críticos que no deben ser evicted
var criticalOptions = new MemoryCacheEntryOptions
{
    Priority = CacheItemPriority.NeverRemove
};
_cache.Set("app:config", appConfig, criticalOptions);

// ✅ High: Datos importantes
var highOptions = new MemoryCacheEntryOptions
{
    Priority = CacheItemPriority.High,
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
};
_cache.Set("countries:all", countries, highOptions);

// ✅ Normal: Datos regulares (default)
_cache.Set("product:123", product, TimeSpan.FromMinutes(30));

// ✅ Low: Datos que pueden ser removidos fácilmente
var lowOptions = new MemoryCacheEntryOptions
{
    Priority = CacheItemPriority.Low,
    SlidingExpiration = TimeSpan.FromMinutes(5)
};
_cache.Set("search:temp:xyz", searchResults, lowOptions);
```

---

## 8. Comparación: Memory vs Distributed Cache

| Característica | In-Memory Cache | Distributed Cache |
|----------------|-----------------|-------------------|
| **Ubicación** | Memoria del proceso | Redis, SQL Server, etc. |
| **Alcance** | Single server | Multi-server (web farm) |
| **Velocidad** | Muy rápida (nanosegundos) | Rápida (milisegundos) |
| **Persistencia** | No (se pierde al reiniciar) | Sí (depende del provider) |
| **Escalabilidad** | Limitada a RAM del servidor | Altamente escalable |
| **Complejidad** | Muy simple | Requiere infraestructura |
| **Costo** | Gratis | Redis: $20-100+/mes |
| **Uso típico** | Lookups, configuración | Sesiones, datos compartidos |
| **Serialización** | No requerida | Requerida (JSON, Binary) |
| **Network Latency** | No | Sí |

### 8.1. ¿Cuándo usar cada uno?

#### Usar **In-Memory Cache** cuando:
- ✅ Aplicación en un solo servidor
- ✅ Datos pequeños (catálogos, lookups)
- ✅ Velocidad es crítica (microsegundos)
- ✅ No requieres compartir entre instancias
- ✅ Reinicio de aplicación no es problema

#### Usar **Distributed Cache** cuando:
- ✅ Web farm / múltiples instancias
- ✅ Sesiones de usuario (session state)
- ✅ Datos deben persistir entre deployments
- ✅ Compartir datos entre servicios
- ✅ Escalabilidad horizontal

### 8.2. Patrón Híbrido (L1/L2 Cache)

**Mejor de ambos mundos**: Combinar In-Memory (L1) con Distributed Cache (L2).

```csharp
public class HybridCacheService
{
    private readonly IMemoryCache _memoryCache; // L1: Fast, local
    private readonly IDistributedCache _distributedCache; // L2: Shared, persistent

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration)
    {
        // 1️⃣ Intentar L1 (In-Memory) primero
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value; // ✅ L1 hit - más rápido
        }

        // 2️⃣ Intentar L2 (Distributed)
        var cachedJson = await _distributedCache.GetStringAsync(key);
        if (cachedJson != null)
        {
            value = JsonSerializer.Deserialize<T>(cachedJson);

            // ✅ Poblar L1 para próximas requests
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));

            return value; // ✅ L2 hit
        }

        // 3️⃣ Cache miss: ejecutar factory
        value = await factory();

        // 4️⃣ Guardar en L2 (distributed) con expiración larga
        var json = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });

        // 5️⃣ Guardar en L1 (memory) con expiración corta
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));

        return value;
    }
}
```

---

## 9. Best Practices

### 9.1. Siempre Usar CancellationToken

```csharp
// ✅ BIEN: Permite cancelación de operaciones largas
public async Task<Order> GetOrderAsync(int id, CancellationToken ct = default)
{
    var cacheKey = $"order:{id}";
    var cached = await _distributedCache.GetStringAsync(cacheKey, ct);

    if (cached != null)
        return JsonSerializer.Deserialize<Order>(cached);

    var order = await _dbContext.Orders
        .FirstOrDefaultAsync(o => o.Id == id, ct);

    if (order != null)
    {
        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(order),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            },
            ct); // ← CancellationToken
    }

    return order;
}
```

### 9.2. Manejar Null Values

```csharp
// ✅ BIEN: Cachear "no encontrado" para evitar hammering en DB
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";

    if (_cache.TryGetValue(cacheKey, out Product product))
    {
        return product; // Puede ser null
    }

    product = await _dbContext.Products.FindAsync(id);

    // ✅ Cachear null con expiración corta
    var expiration = product != null
        ? TimeSpan.FromMinutes(30)  // Producto existe: cache largo
        : TimeSpan.FromMinutes(2);  // Producto no existe: cache corto

    _cache.Set(cacheKey, product, expiration);

    return product;
}
```

### 9.3. Logging de Cache Operations

```csharp
public class CachedProductService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedProductService> _logger;

    public async Task<Product> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";

        if (_cache.TryGetValue(cacheKey, out Product product))
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return product;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        product = await LoadProductFromDatabaseAsync(id);

        if (product != null)
        {
            _cache.Set(cacheKey, product, TimeSpan.FromMinutes(30));
            _logger.LogDebug("Cached product with key: {CacheKey}", cacheKey);
        }

        return product;
    }
}
```

### 9.4. Métricas de Cache Performance

```csharp
public class CacheMetricsService
{
    private long _hits;
    private long _misses;

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            Interlocked.Increment(ref _hits);
            return value;
        }

        Interlocked.Increment(ref _misses);

        value = await factory();
        _cache.Set(key, value, expiration);

        return value;
    }

    public double GetHitRate()
    {
        var total = _hits + _misses;
        return total == 0 ? 0 : (double)_hits / total * 100;
    }

    // ✅ Exponer métricas en endpoint
    // GET /api/metrics/cache
    // { "hits": 1523, "misses": 234, "hitRate": 86.7 }
}
```

### 9.5. Evitar Cache de Datos Sensibles

```csharp
// ❌ MAL: Cachear información sensible sin cuidado
_cache.Set($"user:{userId}:creditcard", creditCard, TimeSpan.FromHours(1));

// ✅ BIEN: No cachear información altamente sensible
// O usar cache cifrado
public class EncryptedCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;

    public void Set(string key, string sensitiveData, TimeSpan expiration)
    {
        var encrypted = _protector.Protect(sensitiveData);
        _cache.Set(key, encrypted, expiration);
    }

    public string Get(string key)
    {
        if (_cache.TryGetValue(key, out string encrypted))
        {
            return _protector.Unprotect(encrypted);
        }
        return null;
    }
}
```

---

## 10. Anti-Patterns Comunes

### 10.1. Cache Stampede (Thundering Herd)

**Problema**: Múltiples requests simultáneas al expirar el cache.

```csharp
// ❌ MAL: Cache stampede
public async Task<List<Product>> GetProductsAsync()
{
    const string cacheKey = "products:all";

    if (!_cache.TryGetValue(cacheKey, out List<Product> products))
    {
        // ⚠️ Si 100 requests llegan al mismo tiempo,
        // las 100 ejecutarán esta query!
        products = await _dbContext.Products.ToListAsync();
        _cache.Set(cacheKey, products, TimeSpan.FromMinutes(30));
    }

    return products;
}

// ✅ BIEN: Solución con SemaphoreSlim
public class ProductService
{
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public async Task<List<Product>> GetProductsAsync()
    {
        const string cacheKey = "products:all";

        if (_cache.TryGetValue(cacheKey, out List<Product> products))
        {
            return products;
        }

        // ✅ Solo un thread ejecuta la query
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check después del lock
            if (_cache.TryGetValue(cacheKey, out products))
            {
                return products;
            }

            products = await _dbContext.Products.ToListAsync();
            _cache.Set(cacheKey, products, TimeSpan.FromMinutes(30));

            return products;
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}

// ✅ MEJOR: Usar GetOrCreateAsync (built-in locking)
public async Task<List<Product>> GetProductsAsync()
{
    return await _cache.GetOrCreateAsync("products:all", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        return await _dbContext.Products.ToListAsync();
    });
}
```

### 10.2. Stale Data Sin Estrategia de Invalidación

**Problema**: Cache desactualizado porque no se invalida al cambiar datos.

```csharp
// ❌ MAL: Actualizar DB sin invalidar cache
public async Task UpdateProductAsync(Product product)
{
    _dbContext.Products.Update(product);
    await _dbContext.SaveChangesAsync();

    // ⚠️ El cache tiene datos obsoletos!
}

// ✅ BIEN: Invalidar cache al actualizar
public async Task UpdateProductAsync(Product product)
{
    _dbContext.Products.Update(product);
    await _dbContext.SaveChangesAsync();

    // ✅ Invalidar cache relacionado
    _cache.Remove($"product:{product.Id}");
    _cache.Remove("products:all");
    _cache.Remove($"products:category:{product.CategoryId}");
}
```

### 10.3. Over-Caching (Cachear Todo)

**Problema**: Cachear datos que no se reutilizan o cambian constantemente.

```csharp
// ❌ MAL: Cachear datos únicos por usuario que no se reutilizan
public async Task<OrderSummary> GetOrderSummaryAsync(int userId, string sessionId)
{
    var cacheKey = $"order:summary:{userId}:{sessionId}:{DateTime.Now.Ticks}";

    // ⚠️ Este key es único cada vez - cache hit rate = 0%
    // Desperdicio de memoria

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        return await CalculateOrderSummaryAsync(userId);
    });
}

// ✅ BIEN: Solo cachear si hay probabilidad de reuso
public async Task<List<Country>> GetCountriesAsync()
{
    // ✅ Todos los usuarios reciben la misma lista
    // Cache hit rate alto

    return await _cache.GetOrCreateAsync("countries:all", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
        return await _dbContext.Countries.ToListAsync();
    });
}
```

### 10.4. Cache Sin Límite de Tamaño

**Problema**: Memory leak por acumulación ilimitada de cache entries.

```csharp
// ❌ MAL: Sin límite de tamaño
builder.Services.AddMemoryCache();

// En la aplicación:
foreach (var user in millionsOfUsers)
{
    _cache.Set($"user:{user.Id}", user, TimeSpan.FromDays(365));
    // ⚠️ Eventualmente consumirá toda la RAM
}

// ✅ BIEN: Configurar límites
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Límite de 1024 unidades
    options.CompactionPercentage = 0.25; // Compactar 25% cuando se llena
});

// Especificar Size en cada entry
_cache.Set("key", value, new MemoryCacheEntryOptions
{
    Size = 1,
    Priority = CacheItemPriority.Normal
});
```

### 10.5. Serialization Overhead en Distributed Cache

**Problema**: Serializar/deserializar objetos grandes constantemente.

```csharp
// ❌ MAL: Serializar objetos masivos
public async Task<LargeReport> GetReportAsync(int id)
{
    var cacheKey = $"report:{id}";
    var cached = await _distributedCache.GetStringAsync(cacheKey);

    if (cached != null)
    {
        // ⚠️ Deserializar 50MB de JSON cada request
        return JsonSerializer.Deserialize<LargeReport>(cached);
    }

    var report = await GenerateLargeReportAsync(id);

    // ⚠️ Serializar 50MB de JSON
    await _distributedCache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(report));

    return report;
}

// ✅ BIEN: Usar Memory Cache para objetos grandes
// O comprimir antes de guardar en Distributed Cache
public async Task<LargeReport> GetReportAsync(int id)
{
    var cacheKey = $"report:{id}";

    // ✅ Primero intentar memory cache (sin serialización)
    if (_memoryCache.TryGetValue(cacheKey, out LargeReport report))
    {
        return report;
    }

    // Si no está en memory, generar
    report = await GenerateLargeReportAsync(id);

    // Cachear en memoria con expiración corta
    _memoryCache.Set(cacheKey, report, TimeSpan.FromMinutes(15));

    return report;
}
```

---

## 11. Errores Comunes y Soluciones

### Error 1: Cache No Funciona Después de Reinicio

**Síntoma**: Cache vacío después de reiniciar la aplicación.

**Causa**: Usando `IMemoryCache` que no persiste.

```csharp
// ❌ Problema
builder.Services.AddMemoryCache();

// ✅ Solución: Usar Distributed Cache con Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### Error 2: InvalidOperationException al Usar Size

**Síntoma**: `InvalidOperationException: Cache entry must specify a size when SizeLimit is set.`

**Causa**: No especificar `Size` cuando `SizeLimit` está configurado.

```csharp
// ❌ Error
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
});

_cache.Set("key", value, TimeSpan.FromMinutes(30)); // ← Sin Size

// ✅ Solución
_cache.Set("key", value, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
    Size = 1 // ← Especificar Size
});
```

### Error 3: Response Caching No Funciona

**Síntoma**: Headers `Cache-Control` no aparecen en responses.

**Causa**: Middleware en orden incorrecto o cacheando requests autenticadas.

```csharp
// ❌ Problema
app.UseAuthorization();
app.UseResponseCaching(); // ← Orden incorrecto

// ✅ Solución
app.UseResponseCaching(); // ← Debe ir ANTES de UseAuthorization
app.UseAuthorization();

// ❌ Problema: Cachear requests autenticadas
[HttpGet("profile")]
[ResponseCache(Duration = 60)] // ← No funcionará con [Authorize]
public IActionResult GetProfile() { }

// ✅ Solución: No cachear endpoints autenticados
[HttpGet("profile")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public IActionResult GetProfile() { }
```

### Error 4: Cache Stampede (Thundering Herd)

**Síntoma**: Picos de carga en DB cuando el cache expira.

**Causa**: Múltiples requests regeneran el cache simultáneamente.

```csharp
// ❌ Problema
if (!_cache.TryGetValue(key, out var value))
{
    value = await LoadFromDatabaseAsync(); // ← 100 threads ejecutan esto
    _cache.Set(key, value, TimeSpan.FromMinutes(30));
}

// ✅ Solución 1: GetOrCreateAsync (built-in locking)
value = await _cache.GetOrCreateAsync(key, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
    return await LoadFromDatabaseAsync();
});

// ✅ Solución 2: SemaphoreSlim manual
private readonly SemaphoreSlim _lock = new(1, 1);

await _lock.WaitAsync();
try
{
    if (!_cache.TryGetValue(key, out var value))
    {
        value = await LoadFromDatabaseAsync();
        _cache.Set(key, value, TimeSpan.FromMinutes(30));
    }
    return value;
}
finally
{
    _lock.Release();
}
```

### Error 5: Distributed Cache No Comparte Entre Instancias

**Síntoma**: Cache funciona en una instancia pero no en otras.

**Causa**: Configuración incorrecta o uso de `IMemoryCache` en lugar de `IDistributedCache`.

```csharp
// ❌ Problema: Inyectando IMemoryCache
public class OrderService
{
    public OrderService(IMemoryCache cache) { } // ← Solo local
}

// ✅ Solución: Usar IDistributedCache
public class OrderService
{
    public OrderService(IDistributedCache cache) { } // ← Compartido
}

// ✅ Configuración correcta de Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis-server:6379"; // ← Mismo Redis para todas las instancias
    options.InstanceName = "MyApp_";
});
```

---

## 12. Referencias

### Documentación Oficial
- [In-Memory Caching - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [Distributed Caching - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)
- [Response Caching - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response)
- [Output Caching (.NET 7+) - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output)

### Guías Relacionadas
- [Memory Cache](./memory-cache.md) - Implementación detallada de IMemoryCache
- [Redis Cache](./redis.md) - Configuración y patrones de Redis
- [HTTP Clients](../http-clients.md) - Caching de responses HTTP

### Bibliotecas Externas
- [EasyCaching](https://github.com/dotnetcore/EasyCaching) - Framework de caching con múltiples providers
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) - Cliente de Redis para .NET
- [FusionCache](https://github.com/ZiggyCreatures/FusionCache) - High-performance cache con features avanzados

---

**Próximos pasos**:
1. Revisar [memory-cache.md](./memory-cache.md) para implementación práctica de `IMemoryCache`
2. Consultar [redis.md](./redis.md) para configurar caching distribuido con Redis
3. Implementar métricas de cache performance en tu aplicación
