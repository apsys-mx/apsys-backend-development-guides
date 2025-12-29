# In-Memory Cache (`IMemoryCache`) en ASP.NET Core

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Estado**: ✅ Completada

## Tabla de Contenidos

1. [Introducción](#1-introducción)
2. [Configuración](#2-configuración)
3. [Patrones de Uso](#3-patrones-de-uso)
4. [Opciones de Expiración](#4-opciones-de-expiración)
5. [Callbacks y Eviction](#5-callbacks-y-eviction)
6. [Gestión de Tamaño y Memoria](#6-gestión-de-tamaño-y-memoria)
7. [Patrones Avanzados](#7-patrones-avanzados)
8. [Best Practices](#8-best-practices)
9. [Anti-Patterns Comunes](#9-anti-patterns-comunes)
10. [Testing](#10-testing)
11. [Performance y Troubleshooting](#11-performance-y-troubleshooting)
12. [Referencias](#12-referencias)

---

## 1. Introducción

`IMemoryCache` es la implementación de cache **en memoria** de ASP.NET Core que almacena datos en la memoria del proceso de la aplicación.

### Características Principales

| Característica | Descripción |
|----------------|-------------|
| **Ubicación** | Memoria RAM del proceso |
| **Persistencia** | No persiste entre reinicios |
| **Alcance** | Single server (no compartido) |
| **Velocidad** | Extremadamente rápida (nanosegundos) |
| **Serialización** | No requerida |
| **Thread-Safety** | Sí, completamente thread-safe |
| **Costo** | Gratis (solo consume RAM) |

### ¿Cuándo Usar IMemoryCache?

#### ✅ Usar cuando:
- Aplicación en un solo servidor (no web farm)
- Datos pequeños a medianos (catálogos, configuraciones)
- Velocidad es crítica
- No requieres compartir entre múltiples instancias
- Los datos pueden regenerarse fácilmente

#### ❌ NO usar cuando:
- Web farm / múltiples instancias
- Necesitas persistencia entre deployments
- Datos deben compartirse entre servidores
- Cache debe sobrevivir reinicios de aplicación

---

## 2. Configuración

### 2.1. Configuración Básica

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✅ Agregar IMemoryCache al contenedor DI
builder.Services.AddMemoryCache();

var app = builder.Build();
```

### 2.2. Configuración Avanzada

```csharp
// Program.cs
builder.Services.AddMemoryCache(options =>
{
    // ✅ Límite de tamaño total del cache (en unidades abstractas)
    options.SizeLimit = 1024;

    // ✅ Porcentaje a compactar cuando se alcanza el límite
    // Si SizeLimit = 1024 y CompactionPercentage = 0.25:
    // Se removerán ~256 items de baja prioridad cuando se llene
    options.CompactionPercentage = 0.25;

    // ✅ Intervalo de escaneo para expirar items
    // Por defecto: 1 minuto
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});
```

### 2.3. Inyección de Dependencias

```csharp
public class ProductService
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;

    // ✅ Constructor injection
    public ProductService(
        IMemoryCache cache,
        AppDbContext dbContext,
        ILogger<ProductService> logger)
    {
        _cache = cache;
        _dbContext = dbContext;
        _logger = logger;
    }

    // Métodos del servicio...
}
```

---

## 3. Patrones de Uso

### 3.1. Patrón TryGetValue (Manual)

**Más control**: Separación explícita de lectura y escritura.

```csharp
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";

    // 1️⃣ Intentar obtener del cache
    if (_cache.TryGetValue(cacheKey, out Product cachedProduct))
    {
        _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
        return cachedProduct;
    }

    _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

    // 2️⃣ Cache miss: cargar desde DB
    var product = await _dbContext.Products
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    // 3️⃣ Guardar en cache (si existe)
    if (product != null)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _cache.Set(cacheKey, product, cacheOptions);
        _logger.LogDebug("Cached: {CacheKey}", cacheKey);
    }

    return product;
}
```

### 3.2. Patrón GetOrCreate (Síncrono)

**Más simple**: El cache ejecuta la factory si no existe el item.

```csharp
public List<Country> GetCountries()
{
    const string cacheKey = "countries:all";

    // ✅ GetOrCreate ejecuta la factory solo en cache miss
    return _cache.GetOrCreate(cacheKey, entry =>
    {
        // Configurar opciones de cache
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
        entry.Priority = CacheItemPriority.High;

        _logger.LogDebug("Loading countries from database");

        // Esta línea solo se ejecuta si hay cache miss
        return _dbContext.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToList();
    });
}
```

### 3.3. Patrón GetOrCreateAsync (Asíncrono)

**Recomendado**: Para operaciones asíncronas.

```csharp
public async Task<List<Category>> GetCategoriesAsync()
{
    const string cacheKey = "categories:all";

    // ✅ GetOrCreateAsync con factory asíncrona
    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        // ✅ Configurar expiración
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
        entry.SlidingExpiration = TimeSpan.FromMinutes(30);

        // ✅ Configurar prioridad
        entry.Priority = CacheItemPriority.Normal;

        // ✅ Configurar tamaño (si SizeLimit está configurado)
        entry.Size = 1;

        _logger.LogInformation("Loading categories from database");

        // ✅ Esta query solo se ejecuta en cache miss
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories;
    });
}
```

### 3.4. Patrón Get con Factory (Con Data Retriever)

**Útil para**: Casos donde la factory puede ser un delegate.

```csharp
public Product GetProductById(int id)
{
    var cacheKey = $"product:{id}";

    // ✅ Get con factory inline
    var product = _cache.Get(
        cacheKey,
        () => _dbContext.Products.Find(id) // ← Factory
    );

    return product;
}

// Versión asíncrona
public async Task<Product> GetProductByIdAsync(int id)
{
    var cacheKey = $"product:{id}";

    // ✅ GetAsync con factory asíncrona
    var product = await _cache.GetAsync(
        cacheKey,
        async () => await _dbContext.Products.FindAsync(id)
    );

    return product;
}
```

### 3.5. Comparación de Patrones

| Patrón | Sincronía | Control | Simplicidad | Thread-Safety |
|--------|-----------|---------|-------------|---------------|
| **TryGetValue** | Manual | Alto | Baja | Manual |
| **GetOrCreate** | Sync | Medio | Alta | Automática |
| **GetOrCreateAsync** | Async | Medio | Alta | Automática |
| **Get con factory** | Ambos | Bajo | Muy alta | Automática |

**Recomendación**: Usar `GetOrCreateAsync` para la mayoría de casos.

---

## 4. Opciones de Expiración

### 4.1. Absolute Expiration

**Definición**: El item expira después de un tiempo fijo, sin importar si se accede o no.

```csharp
public async Task<List<Country>> GetCountriesAsync()
{
    return await _cache.GetOrCreateAsync("countries:all", async entry =>
    {
        // ✅ Expira EXACTAMENTE en 24 horas desde ahora
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

        return await _dbContext.Countries.ToListAsync();
    });
}
```

**Casos de uso**:
- Datos que cambian en un horario predecible (ej: precios actualizados a las 00:00)
- Catálogos que se actualizan diariamente
- Datos de APIs externas con rate limits

### 4.2. Sliding Expiration

**Definición**: El item expira si NO se accede durante un período de tiempo.

```csharp
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    var cacheKey = $"user:{userId}:profile";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        // ✅ Expira si NO se accede durante 15 minutos
        // Cada acceso RENUEVA el tiempo de expiración
        entry.SlidingExpiration = TimeSpan.FromMinutes(15);

        return await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserProfile
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email
            })
            .FirstOrDefaultAsync();
    });
}
```

**Casos de uso**:
- Datos de sesión de usuario
- Configuraciones de usuario activo
- Datos que pierden relevancia si no se usan

### 4.3. Absolute + Sliding (Combinación)

**Mejor práctica**: Combinar para control preciso.

```csharp
public async Task<DashboardStats> GetDashboardStatsAsync(int userId)
{
    var cacheKey = $"dashboard:stats:{userId}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        // ✅ Sliding: Se renueva si se accede antes de 5 minutos
        entry.SlidingExpiration = TimeSpan.FromMinutes(5);

        // ✅ Absolute: Expira SÍ O SÍ después de 30 minutos
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        // Resultado:
        // - Si el usuario accede cada 4 minutos, se renueva
        // - Pero MÁXIMO 30 minutos de vida total

        return await CalculateDashboardStatsAsync(userId);
    });
}
```

### 4.4. Absolute Expiration (Fecha Específica)

**Útil para**: Expirar en un momento exacto del día.

```csharp
public async Task<ExchangeRates> GetExchangeRatesAsync()
{
    return await _cache.GetOrCreateAsync("exchange:rates", async entry =>
    {
        // ✅ Expira mañana a las 00:00
        var tomorrow = DateTime.Today.AddDays(1);
        entry.AbsoluteExpiration = new DateTimeOffset(tomorrow);

        return await _currencyApiClient.GetRatesAsync();
    });
}
```

### 4.5. Tabla de Tiempos Recomendados

| Tipo de Dato | Absolute | Sliding | Ejemplo |
|--------------|----------|---------|---------|
| **Catálogos** | 12-24h | - | Countries, Categories |
| **Configuración** | 30min | 10min | AppSettings, Features |
| **Usuario Activo** | 1h | 15min | UserProfile, Preferences |
| **Precios** | 5min | - | ProductPrices |
| **Estadísticas** | 10min | - | Dashboard, Reports |
| **Búsquedas** | - | 5min | SearchResults |

---

## 5. Callbacks y Eviction

### 5.1. PostEvictionCallback

**Útil para**: Logging, cleanup, reloading automático.

```csharp
public async Task<AppConfiguration> GetAppConfigurationAsync()
{
    const string cacheKey = "app:configuration";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

        // ✅ Callback cuando el item es removido
        entry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
        {
            EvictionCallback = (key, value, reason, state) =>
            {
                _logger.LogInformation(
                    "Cache entry {CacheKey} was evicted. Reason: {Reason}",
                    key,
                    reason);

                // ✅ Puedes ejecutar lógica de cleanup aquí
                if (reason == EvictionReason.Expired)
                {
                    _logger.LogDebug("Configuration expired, will reload on next access");
                }
            }
        });

        return await LoadConfigurationAsync();
    });
}
```

### 5.2. Razones de Eviction

```csharp
public enum EvictionReason
{
    None = 0,           // No evicted
    Removed = 1,        // Removido manualmente con Remove()
    Replaced = 2,       // Reemplazado con Set()
    Expired = 3,        // Expiró por tiempo
    TokenExpired = 4,   // CancellationToken expiró
    Capacity = 5        // Removido por límite de capacidad
}
```

### 5.3. Auto-Reload Pattern

**Patrón avanzado**: Recargar automáticamente cuando expira.

```csharp
private readonly SemaphoreSlim _reloadLock = new(1, 1);

public async Task<List<Country>> GetCountriesWithAutoReloadAsync()
{
    const string cacheKey = "countries:all:autoreload";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

        // ✅ Auto-reload cuando expira
        entry.RegisterPostEvictionCallback(async (key, value, reason, state) =>
        {
            if (reason == EvictionReason.Expired)
            {
                await _reloadLock.WaitAsync();
                try
                {
                    _logger.LogInformation("Auto-reloading countries cache");

                    // Recargar inmediatamente
                    var countries = await _dbContext.Countries.ToListAsync();
                    _cache.Set(cacheKey, countries, TimeSpan.FromHours(24));
                }
                finally
                {
                    _reloadLock.Release();
                }
            }
        });

        return await _dbContext.Countries.ToListAsync();
    });
}
```

### 5.4. Cleanup de Recursos

```csharp
public async Task<Stream> GetReportStreamAsync(int reportId)
{
    var cacheKey = $"report:stream:{reportId}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

        // ✅ Cleanup de recursos al remover del cache
        entry.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            if (value is Stream stream)
            {
                _logger.LogDebug("Disposing stream for {CacheKey}", key);
                stream.Dispose();
            }
        });

        var stream = await GenerateReportStreamAsync(reportId);
        return stream;
    });
}
```

---

## 6. Gestión de Tamaño y Memoria

### 6.1. Configurar SizeLimit

```csharp
// Program.cs
builder.Services.AddMemoryCache(options =>
{
    // ✅ Límite total: 1024 unidades
    options.SizeLimit = 1024;

    // ✅ Remover 25% de items cuando se alcance el límite
    options.CompactionPercentage = 0.25;
});
```

### 6.2. Especificar Size por Entry

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    return await _cache.GetOrCreateAsync("products:all", async entry =>
    {
        // ✅ Este entry consume 10 unidades del SizeLimit
        entry.Size = 10;

        // ✅ Si SizeLimit = 1024, este entry consume ~1% del cache
        entry.Priority = CacheItemPriority.Normal;

        return await _dbContext.Products.ToListAsync();
    });
}
```

### 6.3. Prioridades de Cache

```csharp
public async Task<T> CacheWithPriorityAsync<T>(
    string key,
    Func<Task<T>> factory,
    CacheItemPriority priority)
{
    return await _cache.GetOrCreateAsync(key, async entry =>
    {
        entry.Priority = priority;
        entry.Size = 1;

        return await factory();
    });
}

// Uso
var countries = await CacheWithPriorityAsync(
    "countries:all",
    () => _dbContext.Countries.ToListAsync(),
    CacheItemPriority.High // ← Se remueve al final
);

var searchResults = await CacheWithPriorityAsync(
    "search:temp:xyz",
    () => PerformSearchAsync("xyz"),
    CacheItemPriority.Low // ← Se remueve primero
);
```

#### Niveles de Prioridad

| Prioridad | Valor | Cuándo Usar |
|-----------|-------|-------------|
| **NeverRemove** | 4 | Configuración crítica, nunca evicted por capacidad |
| **High** | 3 | Catálogos importantes, datos frecuentemente accedidos |
| **Normal** | 2 | Datos regulares (default) |
| **Low** | 1 | Búsquedas temporales, datos que pueden regenerarse fácilmente |

### 6.4. Estrategia de Sizing

```csharp
public class CacheSizingHelper
{
    // ✅ Tamaños relativos según tipo de dato
    public static class Sizes
    {
        public const int Tiny = 1;      // Configuraciones, flags
        public const int Small = 5;     // Entidades individuales
        public const int Medium = 10;   // Listas pequeñas (< 100 items)
        public const int Large = 50;    // Listas grandes (100-1000 items)
        public const int XLarge = 100;  // Datasets masivos (> 1000 items)
    }

    public static int CalculateSize<T>(List<T> list)
    {
        var count = list.Count;

        if (count < 10) return Sizes.Tiny;
        if (count < 100) return Sizes.Small;
        if (count < 1000) return Sizes.Medium;
        if (count < 10000) return Sizes.Large;

        return Sizes.XLarge;
    }
}

// Uso
public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
{
    var cacheKey = $"products:category:{categoryId}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        var products = await _dbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();

        // ✅ Tamaño dinámico según cantidad de productos
        entry.Size = CacheSizingHelper.CalculateSize(products);
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        return products;
    });
}
```

---

## 7. Patrones Avanzados

### 7.1. Cache Wrapper Service

**Centralizar lógica de caching**.

```csharp
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null);

    void Remove(string key);
    void RemoveByPrefix(string prefix);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null)
    {
        // ✅ TryGetValue primero para evitar lock innecesario
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", key);
            return cachedValue;
        }

        // ✅ Lock por key para evitar cache stampede
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // ✅ Double-check después del lock
            if (_cache.TryGetValue(key, out cachedValue))
            {
                return cachedValue;
            }

            _logger.LogDebug("Cache MISS: {CacheKey}", key);

            // ✅ Ejecutar factory
            var value = await factory();

            // ✅ Configurar opciones
            var options = new MemoryCacheEntryOptions();

            if (absoluteExpiration.HasValue)
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;

            if (slidingExpiration.HasValue)
                options.SlidingExpiration = slidingExpiration.Value;

            // ✅ Guardar en cache
            _cache.Set(key, value, options);

            _logger.LogDebug("Cached: {CacheKey}", key);

            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Removed cache key: {CacheKey}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        // ⚠️ IMemoryCache no soporta RemoveByPrefix nativamente
        // Necesitarías tracking manual de keys (ver sección 9.3)
        throw new NotImplementedException();
    }
}
```

### 7.2. Typed Cache Service

**Type-safe caching por entidad**.

```csharp
public interface IEntityCacheService<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    void Invalidate(int id);
    void InvalidateAll();
}

public class ProductCacheService : IEntityCacheService<Product>
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _dbContext;

    public ProductCacheService(IMemoryCache cache, AppDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        var cacheKey = $"product:{id}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);

            return await _dbContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        });
    }

    public async Task<List<Product>> GetAllAsync()
    {
        const string cacheKey = "products:all";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

            return await _dbContext.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        });
    }

    public void Invalidate(int id)
    {
        _cache.Remove($"product:{id}");
        _cache.Remove("products:all"); // También invalidar lista completa
    }

    public void InvalidateAll()
    {
        _cache.Remove("products:all");
    }
}
```

### 7.3. Cache Metrics

**Tracking de performance**.

```csharp
public class CacheMetrics
{
    private long _hits;
    private long _misses;
    private long _evictions;

    public void RecordHit() => Interlocked.Increment(ref _hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);
    public void RecordEviction() => Interlocked.Increment(ref _evictions);

    public long Hits => _hits;
    public long Misses => _misses;
    public long Evictions => _evictions;

    public double HitRate
    {
        get
        {
            var total = _hits + _misses;
            return total == 0 ? 0 : (double)_hits / total * 100;
        }
    }

    public void Reset()
    {
        _hits = 0;
        _misses = 0;
        _evictions = 0;
    }
}

public class MeteredCacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheMetrics _metrics;

    public MeteredCacheService(IMemoryCache cache, CacheMetrics metrics)
    {
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            _metrics.RecordHit();
            return value;
        }

        _metrics.RecordMiss();

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = expiration;

            // ✅ Registrar evictions
            entry.RegisterPostEvictionCallback((k, v, r, s) =>
            {
                _metrics.RecordEviction();
            });

            return await factory();
        });
    }

    public CacheMetrics GetMetrics() => _metrics;
}
```

---

## 8. Best Practices

### 8.1. Siempre Usar Keys Estructuradas

```csharp
// ✅ BIEN: Namespace jerárquico
public static class CacheKeys
{
    public static string Product(int id) => $"product:{id}";
    public static string ProductsByCategory(int categoryId)
        => $"products:category:{categoryId}";
    public static string AllProducts() => "products:all";
}

// Uso
var product = await _cache.GetOrCreateAsync(
    CacheKeys.Product(123),
    async entry => await LoadProductAsync(123)
);

// ❌ MAL: Keys sin estructura
var product = await _cache.GetOrCreateAsync(
    "p123",
    async entry => await LoadProductAsync(123)
);
```

### 8.2. Cachear Null con Expiración Corta

```csharp
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = CacheKeys.Product(id);

    if (_cache.TryGetValue(cacheKey, out Product product))
    {
        return product; // Puede ser null
    }

    product = await _dbContext.Products.FindAsync(id);

    // ✅ Cachear null con expiración corta para evitar DB hammering
    var expiration = product != null
        ? TimeSpan.FromMinutes(30)  // Producto existe: cache largo
        : TimeSpan.FromMinutes(2);  // Producto no existe: cache corto

    _cache.Set(cacheKey, product, expiration);

    return product;
}
```

### 8.3. Invalidar Cache en Escrituras

```csharp
public async Task<Product> UpdateProductAsync(Product product)
{
    // 1️⃣ Actualizar en DB
    _dbContext.Products.Update(product);
    await _dbContext.SaveChangesAsync();

    // 2️⃣ Invalidar cache relacionado
    _cache.Remove(CacheKeys.Product(product.Id));
    _cache.Remove(CacheKeys.AllProducts());
    _cache.Remove(CacheKeys.ProductsByCategory(product.CategoryId));

    return product;
}
```

### 8.4. Usar GetOrCreateAsync para Thread-Safety

```csharp
// ✅ BIEN: Thread-safe automáticamente
public async Task<List<Country>> GetCountriesAsync()
{
    return await _cache.GetOrCreateAsync("countries:all", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
        return await _dbContext.Countries.ToListAsync();
    });
}

// ❌ MAL: Race condition posible
public async Task<List<Country>> GetCountriesAsync()
{
    if (!_cache.TryGetValue("countries:all", out List<Country> countries))
    {
        // ⚠️ Múltiples threads pueden ejecutar esto simultáneamente
        countries = await _dbContext.Countries.ToListAsync();
        _cache.Set("countries:all", countries, TimeSpan.FromHours(24));
    }

    return countries;
}
```

### 8.5. Logging Estructurado

```csharp
public async Task<T> GetOrCreateWithLoggingAsync<T>(
    string key,
    Func<Task<T>> factory)
{
    var stopwatch = Stopwatch.StartNew();

    if (_cache.TryGetValue(key, out T value))
    {
        _logger.LogDebug(
            "Cache HIT: {CacheKey} in {ElapsedMs}ms",
            key,
            stopwatch.ElapsedMilliseconds);

        return value;
    }

    _logger.LogDebug("Cache MISS: {CacheKey}", key);

    value = await factory();

    _cache.Set(key, value, TimeSpan.FromMinutes(30));

    _logger.LogInformation(
        "Cached {CacheKey} in {ElapsedMs}ms",
        key,
        stopwatch.ElapsedMilliseconds);

    return value;
}
```

---

## 9. Anti-Patterns Comunes

### 9.1. Cache Sin Expiración

```csharp
// ❌ MAL: Sin expiración = memory leak potencial
_cache.Set("key", value);

// ✅ BIEN: Siempre especificar expiración
_cache.Set("key", value, TimeSpan.FromMinutes(30));
```

### 9.2. Cachear Objetos Grandes Sin Control

```csharp
// ❌ MAL: Cachear datasets masivos sin límites
public async Task<List<LogEntry>> GetAllLogsAsync()
{
    return await _cache.GetOrCreateAsync("logs:all", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

        // ⚠️ Potencialmente millones de registros
        return await _dbContext.Logs.ToListAsync();
    });
}

// ✅ BIEN: Solo cachear datasets pequeños o usar paginación
public async Task<List<LogEntry>> GetRecentLogsAsync()
{
    return await _cache.GetOrCreateAsync("logs:recent", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        entry.Size = CacheSizingHelper.Sizes.Medium;

        // ✅ Solo últimos 100 registros
        return await _dbContext.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync();
    });
}
```

### 9.3. No Invalidar al Actualizar

```csharp
// ❌ MAL: Actualizar sin invalidar cache
public async Task UpdateCategoryAsync(Category category)
{
    _dbContext.Categories.Update(category);
    await _dbContext.SaveChangesAsync();

    // ⚠️ El cache tiene datos obsoletos
}

// ✅ BIEN: Invalidar cache relacionado
public async Task UpdateCategoryAsync(Category category)
{
    _dbContext.Categories.Update(category);
    await _dbContext.SaveChangesAsync();

    // ✅ Invalidar todas las claves relacionadas
    _cache.Remove($"category:{category.Id}");
    _cache.Remove("categories:all");
}
```

### 9.4. Cachear Datos de Usuario Sin Aislamiento

```csharp
// ❌ MAL: Posible filtración entre usuarios
public async Task<UserCart> GetUserCartAsync(int userId)
{
    return await _cache.GetOrCreateAsync("cart", async entry =>
    {
        // ⚠️ Todos los usuarios ven el mismo carrito!
        return await _dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);
    });
}

// ✅ BIEN: Key con userId
public async Task<UserCart> GetUserCartAsync(int userId)
{
    var cacheKey = $"cart:user:{userId}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(15);

        return await _dbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);
    });
}
```

### 9.5. Usar Cache para Sincronización

```csharp
// ❌ MAL: Usar cache como mecanismo de locking
public async Task ProcessOrderAsync(int orderId)
{
    var lockKey = $"lock:order:{orderId}";

    if (_cache.TryGetValue(lockKey, out _))
    {
        throw new InvalidOperationException("Order is being processed");
    }

    _cache.Set(lockKey, true, TimeSpan.FromMinutes(5));

    // ⚠️ Si el proceso falla, el lock queda indefinidamente
    await ProcessAsync(orderId);

    _cache.Remove(lockKey);
}

// ✅ BIEN: Usar SemaphoreSlim o locks apropiados
private readonly SemaphoreSlim _orderLock = new(1, 1);

public async Task ProcessOrderAsync(int orderId)
{
    await _orderLock.WaitAsync();
    try
    {
        await ProcessAsync(orderId);
    }
    finally
    {
        _orderLock.Release();
    }
}
```

---

## 10. Testing

### 10.1. Configurar IMemoryCache en Tests

```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public class ProductServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        // ✅ Crear instancia real de MemoryCache
        var options = Options.Create(new MemoryCacheOptions());
        _cache = new MemoryCache(options);

        _sut = new ProductService(_cache, mockDbContext, mockLogger);
    }

    [Fact]
    public async Task GetProductAsync_CachesResult()
    {
        // Arrange
        var productId = 1;

        // Act
        var result1 = await _sut.GetProductAsync(productId);
        var result2 = await _sut.GetProductAsync(productId);

        // Assert
        Assert.NotNull(result1);
        Assert.Same(result1, result2); // ✅ Misma instancia = cached
    }

    public void Dispose()
    {
        // ✅ Limpiar cache después de cada test
        _cache.Dispose();
    }
}
```

### 10.2. Mockear IMemoryCache (Alternativa)

```csharp
using Moq;

public class ProductServiceTests
{
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _cacheMock = new Mock<IMemoryCache>();

        _sut = new ProductService(
            _cacheMock.Object,
            mockDbContext,
            mockLogger);
    }

    [Fact]
    public async Task GetProductAsync_OnCacheMiss_LoadsFromDatabase()
    {
        // Arrange
        var productId = 1;
        object cachedValue = null;

        // ✅ Simular cache miss
        _cacheMock
            .Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        // Act
        var result = await _sut.GetProductAsync(productId);

        // Assert
        Assert.NotNull(result);
        _cacheMock.Verify(
            x => x.CreateEntry(It.IsAny<object>()),
            Times.Once);
    }
}
```

### 10.3. Test de Expiración

```csharp
[Fact]
public async Task GetCountriesAsync_Expires_After_Configured_Time()
{
    // Arrange
    var options = Options.Create(new MemoryCacheOptions
    {
        ExpirationScanFrequency = TimeSpan.FromMilliseconds(100)
    });
    var cache = new MemoryCache(options);
    var sut = new CountryService(cache, mockDbContext);

    // Act
    var result1 = await sut.GetCountriesAsync();

    // Esperar a que expire
    await Task.Delay(TimeSpan.FromSeconds(2));

    // Forzar escaneo de expiración
    cache.Compact(1.0);

    var result2 = await sut.GetCountriesAsync();

    // Assert
    Assert.NotSame(result1, result2); // ✅ Diferentes instancias = recargado
}
```

---

## 11. Performance y Troubleshooting

### 11.1. Monitoreo de Uso de Memoria

```csharp
using System.Diagnostics;

public class CacheMonitoringService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheMonitoringService> _logger;

    public void LogCacheStatistics()
    {
        var process = Process.GetCurrentProcess();

        _logger.LogInformation(
            "Memory Usage: {MemoryMB} MB, " +
            "Working Set: {WorkingSetMB} MB",
            process.PrivateMemorySize64 / 1024 / 1024,
            process.WorkingSet64 / 1024 / 1024);

        // ✅ Opcional: Forzar compactación si uso es alto
        if (process.WorkingSet64 > 500 * 1024 * 1024) // > 500 MB
        {
            _logger.LogWarning("High memory usage detected, compacting cache");
            _cache.Compact(0.25); // Remover 25%
        }
    }
}
```

### 11.2. Diagnóstico de Cache Miss Rate

```csharp
public class CacheDiagnostics
{
    private readonly ConcurrentDictionary<string, (long hits, long misses)> _stats = new();

    public void RecordAccess(string key, bool isHit)
    {
        _stats.AddOrUpdate(
            key,
            _ => isHit ? (1, 0) : (0, 1),
            (_, current) => isHit
                ? (current.hits + 1, current.misses)
                : (current.hits, current.misses + 1));
    }

    public Dictionary<string, double> GetHitRates()
    {
        return _stats.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var (hits, misses) = kvp.Value;
                var total = hits + misses;
                return total == 0 ? 0 : (double)hits / total * 100;
            });
    }

    public void LogLowHitRateKeys(ILogger logger, double threshold = 50.0)
    {
        var lowHitRates = GetHitRates()
            .Where(kvp => kvp.Value < threshold)
            .OrderBy(kvp => kvp.Value);

        foreach (var (key, hitRate) in lowHitRates)
        {
            logger.LogWarning(
                "Low cache hit rate for {CacheKey}: {HitRate:F2}%",
                key,
                hitRate);
        }
    }
}
```

### 11.3. Errores Comunes

#### Error 1: InvalidOperationException con Size

```
InvalidOperationException: Cache entry must specify a size when SizeLimit is set.
```

**Solución**:

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

#### Error 2: ObjectDisposedException

```
ObjectDisposedException: Cannot access a disposed object.
```

**Causa**: El cache fue disposed pero se sigue usando.

**Solución**: Asegurar que IMemoryCache se inyecte con lifetime Singleton (default).

---

## 12. Referencias

### Documentación Oficial
- [In-Memory Caching - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [IMemoryCache Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache)
- [MemoryCacheEntryOptions Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions)

### Guías Relacionadas
- [Caching Overview](./README.md) - Comparación de tipos de caching
- [Redis Cache](./redis.md) - Distributed cache con Redis

### Artículos y Recursos
- [High-Performance Caching with IMemoryCache](https://andrewlock.net/exploring-the-dotnet-8-preview-avoiding-cache-stampedes-with-the-output-cache/)
- [Cache Stampede Prevention](https://www.baeldung.com/cs/cache-stampede-preventing)

---

**Próximos pasos**:
1. Implementar wrapper service para centralizar lógica de caching
2. Agregar métricas de cache performance
3. Revisar [redis.md](./redis.md) para distributed caching scenarios
