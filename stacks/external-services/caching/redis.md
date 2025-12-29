# Redis Distributed Cache en ASP.NET Core

**Versión**: 1.0.0
**Última actualización**: 2025-11-14
**Estado**: ✅ Completada

## Tabla de Contenidos

1. [Introducción](#1-introducción)
2. [Configuración](#2-configuración)
3. [IDistributedCache con Redis](#3-idistributedcache-con-redis)
4. [StackExchange.Redis Directo](#4-stackexchangeredis-directo)
5. [Patrones de Datos](#5-patrones-de-datos)
6. [Pub/Sub](#6-pubsub)
7. [Gestión de Conexiones](#7-gestión-de-conexiones)
8. [Serialización](#8-serialización)
9. [Patrones Avanzados](#9-patrones-avanzados)
10. [Best Practices](#10-best-practices)
11. [Anti-Patterns Comunes](#11-anti-patterns-comunes)
12. [Testing](#12-testing)
13. [Performance y Monitoring](#13-performance-y-monitoring)
14. [Referencias](#14-referencias)

---

## 1. Introducción

**Redis** (Remote Dictionary Server) es un almacén de datos en memoria de código abierto usado como base de datos, cache y broker de mensajes.

### Características Principales

| Característica | Descripción |
|----------------|-------------|
| **Ubicación** | In-memory con persistencia opcional |
| **Alcance** | Distributed (multi-server, multi-app) |
| **Velocidad** | Extremadamente rápida (sub-millisecond) |
| **Persistencia** | Sí (RDB snapshots, AOF logs) |
| **Estructuras de Datos** | String, Hash, List, Set, Sorted Set, HyperLogLog, Bitmap, Stream |
| **Pub/Sub** | Sí, con pattern matching |
| **Clustering** | Sí, con sharding automático |
| **Replicación** | Master-Replica con failover automático |

### ¿Cuándo Usar Redis?

#### ✅ Usar cuando:
- Web farm / múltiples instancias de aplicación
- Necesitas persistencia del cache entre deployments
- Compartir datos entre servicios/aplicaciones
- Session state distribuido
- Rate limiting distribuido
- Real-time leaderboards
- Pub/Sub messaging
- Distributed locks

#### ❌ NO usar cuando:
- Aplicación en un solo servidor (usar IMemoryCache)
- Budget limitado ($20-100+/mes)
- Datos extremadamente sensibles sin encriptación
- No requieres persistencia ni compartir datos

---

## 2. Configuración

### 2.1. Instalar Paquetes NuGet

```bash
# Para IDistributedCache (recomendado)
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis

# Para uso directo de StackExchange.Redis
dotnet add package StackExchange.Redis
```

### 2.2. Configuración Básica con IDistributedCache

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ✅ Agregar Redis como IDistributedCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_"; // Prefijo para todas las keys
});

var app = builder.Build();
```

### 2.3. appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpassword,ssl=false,abortConnect=false"
  }
}
```

### 2.4. Configuración Avanzada

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp_";

    // ✅ Opciones de configuración avanzadas
    options.ConfigurationOptions = new ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        Password = "yourpassword",
        Ssl = false,
        AbortOnConnectFail = false, // ← Importante para resilencia
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
        ConnectRetry = 3,
        KeepAlive = 60,
        DefaultDatabase = 0,
        ClientName = "MyApp",

        // ✅ Para Azure Redis
        // Ssl = true,
        // AbortOnConnectFail = false
    };
});
```

### 2.5. Configuración de ConnectionMultiplexer (Uso Directo)

```csharp
// Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configurationOptions = new ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        Password = "yourpassword",
        Ssl = false,
        AbortOnConnectFail = false,
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
        AsyncTimeout = 5000,
        ConnectRetry = 3,
        KeepAlive = 60,
        DefaultDatabase = 0,
        ClientName = "MyApp"
    };

    var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);

    // ✅ Event handlers para logging
    multiplexer.ConnectionFailed += (sender, args) =>
    {
        sp.GetRequiredService<ILogger<Program>>()
            .LogError("Redis connection failed: {Exception}", args.Exception);
    };

    multiplexer.ConnectionRestored += (sender, args) =>
    {
        sp.GetRequiredService<ILogger<Program>>()
            .LogInformation("Redis connection restored");
    };

    return multiplexer;
});
```

---

## 3. IDistributedCache con Redis

### 3.1. Uso Básico

```csharp
public class OrderService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IDistributedCache cache, ILogger<OrderService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Order> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"order:{id}";

        // 1️⃣ Intentar obtener del cache
        var cachedOrder = await _cache.GetStringAsync(cacheKey, ct);

        if (cachedOrder != null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<Order>(cachedOrder);
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);

        // 2️⃣ Cache miss: cargar desde DB
        var order = await LoadOrderFromDatabaseAsync(id, ct);

        // 3️⃣ Guardar en cache
        if (order != null)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            var serialized = JsonSerializer.Serialize(order);
            await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, ct);

            _logger.LogDebug("Cached: {CacheKey}", cacheKey);
        }

        return order;
    }

    public async Task InvalidateOrderCacheAsync(int orderId, CancellationToken ct = default)
    {
        var cacheKey = $"order:{orderId}";
        await _cache.RemoveAsync(cacheKey, ct);

        _logger.LogDebug("Invalidated cache: {CacheKey}", cacheKey);
    }
}
```

### 3.2. Métodos de IDistributedCache

```csharp
public interface IDistributedCache
{
    // ✅ Get: Obtener como byte array
    byte[] Get(string key);
    Task<byte[]> GetAsync(string key, CancellationToken token = default);

    // ✅ Set: Almacenar byte array
    void Set(string key, byte[] value, DistributedCacheEntryOptions options);
    Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = default);

    // ✅ Refresh: Renovar expiración (sliding)
    void Refresh(string key);
    Task RefreshAsync(string key, CancellationToken token = default);

    // ✅ Remove: Eliminar del cache
    void Remove(string key);
    Task RemoveAsync(string key, CancellationToken token = default);
}
```

### 3.3. Extension Methods para String

```csharp
// Microsoft.Extensions.Caching.Distributed proporciona helpers
public static class DistributedCacheExtensions
{
    // ✅ GetString / SetString
    public static string GetString(this IDistributedCache cache, string key);
    public static Task<string> GetStringAsync(this IDistributedCache cache, string key,
        CancellationToken token = default);

    public static void SetString(this IDistributedCache cache, string key, string value,
        DistributedCacheEntryOptions options);
    public static Task SetStringAsync(this IDistributedCache cache, string key, string value,
        DistributedCacheEntryOptions options, CancellationToken token = default);
}
```

### 3.4. Wrapper Service para IDistributedCache

```csharp
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default)
    {
        // 1️⃣ Intentar obtener del cache
        var cachedValue = await _cache.GetStringAsync(key, ct);

        if (cachedValue != null)
        {
            _logger.LogDebug("Cache HIT: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", key);

        // 2️⃣ Ejecutar factory
        var value = await factory();

        // 3️⃣ Guardar en cache
        var options = new DistributedCacheEntryOptions();

        if (absoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;

        if (slidingExpiration.HasValue)
            options.SlidingExpiration = slidingExpiration.Value;

        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options, ct);

        _logger.LogDebug("Cached: {CacheKey}", key);

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
        _logger.LogDebug("Removed cache key: {CacheKey}", key);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // ⚠️ IDistributedCache NO soporta RemoveByPrefix
        // Necesitas usar StackExchange.Redis directo (ver sección 4.5)
        throw new NotImplementedException(
            "Use StackExchange.Redis directly for pattern-based removal");
    }
}
```

---

## 4. StackExchange.Redis Directo

### 4.1. Obtener IDatabase

```csharp
public class ProductService
{
    private readonly IDatabase _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IConnectionMultiplexer redis, ILogger<ProductService> logger)
    {
        _db = redis.GetDatabase(); // Default database (0)
        _logger = logger;
    }

    // Métodos del servicio...
}
```

### 4.2. Operaciones con Strings

```csharp
public class RedisStringService
{
    private readonly IDatabase _db;

    public RedisStringService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ Set / Get
    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        return await _db.StringSetAsync(key, serialized, expiration);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return default;

        return JsonSerializer.Deserialize<T>(value);
    }

    // ✅ Increment / Decrement
    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        return await _db.StringIncrementAsync(key, value);
    }

    public async Task<long> DecrementAsync(string key, long value = 1)
    {
        return await _db.StringDecrementAsync(key, value);
    }

    // ✅ SetNX (Set if Not eXists) - Para distributed locks
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiration)
    {
        return await _db.StringSetAsync(key, value, expiration, When.NotExists);
    }

    // ✅ GetSet (Atomic get and set)
    public async Task<string> GetAndSetAsync(string key, string newValue)
    {
        return await _db.StringGetSetAsync(key, newValue);
    }

    // ✅ Delete
    public async Task<bool> DeleteAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    // ✅ Exists
    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    // ✅ Expire
    public async Task<bool> ExpireAsync(string key, TimeSpan expiration)
    {
        return await _db.KeyExpireAsync(key, expiration);
    }

    // ✅ TTL (Time To Live)
    public async Task<TimeSpan?> GetTtlAsync(string key)
    {
        return await _db.KeyTimeToLiveAsync(key);
    }
}
```

### 4.3. Operaciones con Hash

**Útil para**: Almacenar objetos con múltiples campos.

```csharp
public class RedisHashService
{
    private readonly IDatabase _db;

    public RedisHashService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ HashSet / HashGet
    public async Task SetUserAsync(int userId, User user)
    {
        var key = $"user:{userId}";

        var hashEntries = new HashEntry[]
        {
            new HashEntry("id", user.Id),
            new HashEntry("name", user.Name),
            new HashEntry("email", user.Email),
            new HashEntry("createdAt", user.CreatedAt.ToString("O"))
        };

        await _db.HashSetAsync(key, hashEntries);
        await _db.KeyExpireAsync(key, TimeSpan.FromHours(1));
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var key = $"user:{userId}";
        var hashEntries = await _db.HashGetAllAsync(key);

        if (hashEntries.Length == 0)
            return null;

        return new User
        {
            Id = (int)hashEntries.First(e => e.Name == "id").Value,
            Name = hashEntries.First(e => e.Name == "name").Value,
            Email = hashEntries.First(e => e.Name == "email").Value,
            CreatedAt = DateTime.Parse(hashEntries.First(e => e.Name == "createdAt").Value)
        };
    }

    // ✅ HashGet single field
    public async Task<string> GetUserEmailAsync(int userId)
    {
        var key = $"user:{userId}";
        return await _db.HashGetAsync(key, "email");
    }

    // ✅ HashIncrement
    public async Task<long> IncrementLoginCountAsync(int userId)
    {
        var key = $"user:{userId}";
        return await _db.HashIncrementAsync(key, "loginCount", 1);
    }

    // ✅ HashDelete field
    public async Task<bool> DeleteUserEmailAsync(int userId)
    {
        var key = $"user:{userId}";
        return await _db.HashDeleteAsync(key, "email");
    }

    // ✅ HashExists
    public async Task<bool> HasEmailAsync(int userId)
    {
        var key = $"user:{userId}";
        return await _db.HashExistsAsync(key, "email");
    }
}
```

### 4.4. Operaciones con Lists

**Útil para**: Colas, logs, notificaciones.

```csharp
public class RedisListService
{
    private readonly IDatabase _db;

    public RedisListService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ Push (agregar al final)
    public async Task<long> AddNotificationAsync(int userId, string message)
    {
        var key = $"notifications:{userId}";
        return await _db.ListRightPushAsync(key, message);
    }

    // ✅ Pop (obtener y remover del inicio)
    public async Task<string> GetNextNotificationAsync(int userId)
    {
        var key = $"notifications:{userId}";
        return await _db.ListLeftPopAsync(key);
    }

    // ✅ Range (obtener rango sin remover)
    public async Task<string[]> GetRecentNotificationsAsync(int userId, int count = 10)
    {
        var key = $"notifications:{userId}";
        var values = await _db.ListRangeAsync(key, 0, count - 1);
        return values.Select(v => v.ToString()).ToArray();
    }

    // ✅ Length
    public async Task<long> GetNotificationCountAsync(int userId)
    {
        var key = $"notifications:{userId}";
        return await _db.ListLengthAsync(key);
    }

    // ✅ Trim (mantener solo últimos N elementos)
    public async Task TrimNotificationsAsync(int userId, int maxCount = 100)
    {
        var key = $"notifications:{userId}";
        await _db.ListTrimAsync(key, 0, maxCount - 1);
    }
}
```

### 4.5. Operaciones con Sets

**Útil para**: Tags, followers, permisos únicos.

```csharp
public class RedisSetService
{
    private readonly IDatabase _db;

    public RedisSetService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ Add members
    public async Task<bool> AddTagAsync(int productId, string tag)
    {
        var key = $"product:{productId}:tags";
        return await _db.SetAddAsync(key, tag);
    }

    // ✅ Remove member
    public async Task<bool> RemoveTagAsync(int productId, string tag)
    {
        var key = $"product:{productId}:tags";
        return await _db.SetRemoveAsync(key, tag);
    }

    // ✅ Is member
    public async Task<bool> HasTagAsync(int productId, string tag)
    {
        var key = $"product:{productId}:tags";
        return await _db.SetContainsAsync(key, tag);
    }

    // ✅ Get all members
    public async Task<string[]> GetAllTagsAsync(int productId)
    {
        var key = $"product:{productId}:tags";
        var values = await _db.SetMembersAsync(key);
        return values.Select(v => v.ToString()).ToArray();
    }

    // ✅ Count
    public async Task<long> GetTagCountAsync(int productId)
    {
        var key = $"product:{productId}:tags";
        return await _db.SetLengthAsync(key);
    }

    // ✅ Union (combinar sets)
    public async Task<string[]> GetCommonTagsAsync(int productId1, int productId2)
    {
        var key1 = $"product:{productId1}:tags";
        var key2 = $"product:{productId2}:tags";
        var values = await _db.SetCombineAsync(SetOperation.Intersect, key1, key2);
        return values.Select(v => v.ToString()).ToArray();
    }
}
```

### 4.6. Operaciones con Sorted Sets

**Útil para**: Leaderboards, rankings, prioridades.

```csharp
public class RedisSortedSetService
{
    private readonly IDatabase _db;

    public RedisSortedSetService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ Add con score
    public async Task<bool> AddScoreAsync(string leaderboard, int userId, double score)
    {
        var key = $"leaderboard:{leaderboard}";
        return await _db.SortedSetAddAsync(key, userId.ToString(), score);
    }

    // ✅ Increment score
    public async Task<double> IncrementScoreAsync(string leaderboard, int userId, double increment)
    {
        var key = $"leaderboard:{leaderboard}";
        return await _db.SortedSetIncrementAsync(key, userId.ToString(), increment);
    }

    // ✅ Get rank (posición)
    public async Task<long?> GetRankAsync(string leaderboard, int userId)
    {
        var key = $"leaderboard:{leaderboard}";
        // Rank descendente (mayor score = rank 0)
        return await _db.SortedSetRankAsync(key, userId.ToString(), Order.Descending);
    }

    // ✅ Get score
    public async Task<double?> GetScoreAsync(string leaderboard, int userId)
    {
        var key = $"leaderboard:{leaderboard}";
        return await _db.SortedSetScoreAsync(key, userId.ToString());
    }

    // ✅ Get top N (leaderboard)
    public async Task<Dictionary<string, double>> GetTopPlayersAsync(
        string leaderboard,
        int count = 10)
    {
        var key = $"leaderboard:{leaderboard}";
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(
            key,
            0,
            count - 1,
            Order.Descending);

        return entries.ToDictionary(
            e => e.Element.ToString(),
            e => e.Score);
    }

    // ✅ Get range by score
    public async Task<string[]> GetPlayersByScoreAsync(
        string leaderboard,
        double minScore,
        double maxScore)
    {
        var key = $"leaderboard:{leaderboard}";
        var values = await _db.SortedSetRangeByScoreAsync(key, minScore, maxScore);
        return values.Select(v => v.ToString()).ToArray();
    }

    // ✅ Remove member
    public async Task<bool> RemovePlayerAsync(string leaderboard, int userId)
    {
        var key = $"leaderboard:{leaderboard}";
        return await _db.SortedSetRemoveAsync(key, userId.ToString());
    }

    // ✅ Count
    public async Task<long> GetPlayerCountAsync(string leaderboard)
    {
        var key = $"leaderboard:{leaderboard}";
        return await _db.SortedSetLengthAsync(key);
    }
}
```

### 4.7. Scan Keys (Búsqueda por Patrón)

```csharp
public class RedisKeyService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisKeyService> _logger;

    public RedisKeyService(IConnectionMultiplexer redis, ILogger<RedisKeyService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    // ✅ Buscar keys por patrón (usa SCAN, no KEYS)
    public async Task<List<string>> FindKeysByPatternAsync(string pattern)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = new List<string>();

        // ✅ SCAN es seguro en producción (no bloquea Redis)
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }

        return keys;
    }

    // ✅ Remover keys por patrón
    public async Task<long> RemoveByPrefixAsync(string prefix)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        var pattern = $"{prefix}*";
        var keysToDelete = new List<RedisKey>();

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keysToDelete.Add(key);
        }

        if (keysToDelete.Count == 0)
            return 0;

        _logger.LogInformation("Deleting {Count} keys with prefix {Prefix}",
            keysToDelete.Count, prefix);

        return await db.KeyDeleteAsync(keysToDelete.ToArray());
    }

    // ✅ Flush database (CUIDADO: Borra TODO)
    public async Task FlushDatabaseAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await server.FlushDatabaseAsync();

        _logger.LogWarning("Database flushed - all keys deleted");
    }
}
```

---

## 5. Patrones de Datos

### 5.1. Cache-Aside Pattern

```csharp
public async Task<Product> GetProductAsync(int id, CancellationToken ct = default)
{
    var cacheKey = $"product:{id}";

    // 1️⃣ Intentar leer del cache
    var cachedProduct = await _cache.GetStringAsync(cacheKey, ct);

    if (cachedProduct != null)
    {
        return JsonSerializer.Deserialize<Product>(cachedProduct);
    }

    // 2️⃣ Cache miss: leer de DB
    var product = await _dbContext.Products.FindAsync(id);

    // 3️⃣ Actualizar cache
    if (product != null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        var serialized = JsonSerializer.Serialize(product);
        await _cache.SetStringAsync(cacheKey, serialized, options, ct);
    }

    return product;
}
```

### 5.2. Write-Through Pattern

```csharp
public async Task<Product> UpdateProductAsync(Product product, CancellationToken ct = default)
{
    var cacheKey = $"product:{product.Id}";

    // 1️⃣ Actualizar en DB
    _dbContext.Products.Update(product);
    await _dbContext.SaveChangesAsync(ct);

    // 2️⃣ Actualizar en cache (Write-Through)
    var options = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    var serialized = JsonSerializer.Serialize(product);
    await _cache.SetStringAsync(cacheKey, serialized, options, ct);

    return product;
}
```

### 5.3. Session State

```csharp
// Program.cs
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Session_";
});

var app = builder.Build();

app.UseSession(); // ← Agregar middleware de sesión

// Uso en controller
public class AccountController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        // Validar credenciales...

        // ✅ Guardar en sesión (Redis automáticamente)
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);

        return Ok();
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        // ✅ Leer de sesión
        var userId = HttpContext.Session.GetInt32("UserId");
        var userName = HttpContext.Session.GetString("UserName");

        if (!userId.HasValue)
            return Unauthorized();

        return Ok(new { userId, userName });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // ✅ Limpiar sesión
        HttpContext.Session.Clear();
        return Ok();
    }
}
```

### 5.4. Rate Limiting

```csharp
public class RateLimitService
{
    private readonly IDatabase _db;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ✅ Sliding Window Rate Limiting
    public async Task<bool> IsAllowedAsync(
        string userId,
        int maxRequests,
        TimeSpan window)
    {
        var key = $"ratelimit:{userId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = now - (long)window.TotalMilliseconds;

        // 1️⃣ Remover requests antiguos
        await _db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);

        // 2️⃣ Contar requests en ventana
        var count = await _db.SortedSetLengthAsync(key);

        if (count >= maxRequests)
        {
            return false; // ❌ Rate limit excedido
        }

        // 3️⃣ Agregar request actual
        await _db.SortedSetAddAsync(key, now, now);

        // 4️⃣ Establecer expiración
        await _db.KeyExpireAsync(key, window);

        return true; // ✅ Request permitido
    }
}

// Uso en middleware
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitService _rateLimitService;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        // ✅ Permitir 100 requests por minuto
        var isAllowed = await _rateLimitService.IsAllowedAsync(
            userId,
            maxRequests: 100,
            window: TimeSpan.FromMinutes(1));

        if (!isAllowed)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }
}
```

---

## 6. Pub/Sub

### 6.1. Subscriber (Escuchar Mensajes)

```csharp
public class NotificationSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationSubscriber> _logger;

    public NotificationSubscriber(
        IConnectionMultiplexer redis,
        ILogger<NotificationSubscriber> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        // ✅ Suscribirse a un canal
        await subscriber.SubscribeAsync("notifications", (channel, message) =>
        {
            _logger.LogInformation("Received on {Channel}: {Message}", channel, message);

            // Procesar notificación...
            ProcessNotification(message);
        });

        _logger.LogInformation("Subscribed to 'notifications' channel");

        // ✅ Suscribirse con patrón (wildcard)
        await subscriber.SubscribeAsync(
            new RedisChannel("user:*", RedisChannel.PatternMode.Pattern),
            (channel, message) =>
            {
                _logger.LogInformation("User notification on {Channel}: {Message}",
                    channel, message);
            });

        _logger.LogInformation("Subscribed to 'user:*' pattern");

        // Mantener el servicio corriendo
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ProcessNotification(string message)
    {
        // Implementar lógica de procesamiento
        _logger.LogDebug("Processing notification: {Message}", message);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.UnsubscribeAllAsync();

        _logger.LogInformation("Unsubscribed from all channels");

        await base.StopAsync(cancellationToken);
    }
}
```

### 6.2. Publisher (Enviar Mensajes)

```csharp
public class NotificationPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
        IConnectionMultiplexer redis,
        ILogger<NotificationPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    // ✅ Publicar mensaje a un canal
    public async Task PublishNotificationAsync(string message)
    {
        var subscriber = _redis.GetSubscriber();

        long subscriberCount = await subscriber.PublishAsync("notifications", message);

        _logger.LogInformation(
            "Published message to {SubscriberCount} subscribers: {Message}",
            subscriberCount,
            message);
    }

    // ✅ Publicar a canal específico de usuario
    public async Task PublishUserNotificationAsync(int userId, string message)
    {
        var subscriber = _redis.GetSubscriber();
        var channel = $"user:{userId}";

        long subscriberCount = await subscriber.PublishAsync(channel, message);

        _logger.LogInformation(
            "Published to user {UserId}: {Message} ({SubscriberCount} subscribers)",
            userId,
            message,
            subscriberCount);
    }

    // ✅ Publicar objeto serializado
    public async Task PublishEventAsync<T>(string channel, T eventData)
    {
        var subscriber = _redis.GetSubscriber();
        var serialized = JsonSerializer.Serialize(eventData);

        await subscriber.PublishAsync(channel, serialized);

        _logger.LogDebug("Published event to {Channel}", channel);
    }
}
```

### 6.3. Queue-Based Subscription (Async Processing)

```csharp
public class AsyncNotificationSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AsyncNotificationSubscriber> _logger;

    public AsyncNotificationSubscriber(
        IConnectionMultiplexer redis,
        ILogger<AsyncNotificationSubscriber> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        // ✅ Queue-based subscription con handler asíncrono
        var channel = await subscriber.SubscribeAsync("events");

        channel.OnMessage(async channelMessage =>
        {
            _logger.LogInformation("Processing async message from {Channel}",
                channelMessage.Channel);

            try
            {
                // ✅ Procesamiento asíncrono
                await ProcessEventAsync(channelMessage.Message, stoppingToken);

                _logger.LogDebug("Processed message: {Message}", channelMessage.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}",
                    channelMessage.Message);
            }
        });

        _logger.LogInformation("Async subscriber started for 'events' channel");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessEventAsync(string message, CancellationToken ct)
    {
        // Simulación de procesamiento asíncrono
        await Task.Delay(100, ct);

        _logger.LogDebug("Event processed: {Message}", message);
    }
}
```

---

## 7. Gestión de Conexiones

### 7.1. Connection Multiplexer (Singleton)

```csharp
// ❌ MAL: Crear nueva conexión cada vez
public class BadRedisService
{
    public async Task<string> GetValueAsync(string key)
    {
        // ⚠️ NUNCA hacer esto - muy costoso
        var redis = ConnectionMultiplexer.Connect("localhost");
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        await redis.DisposeAsync();
        return value;
    }
}

// ✅ BIEN: Reutilizar ConnectionMultiplexer (Singleton)
public class GoodRedisService
{
    private readonly IDatabase _db;

    public GoodRedisService(IConnectionMultiplexer redis)
    {
        // ✅ Redis connection es thread-safe y debe ser reutilizado
        _db = redis.GetDatabase();
    }

    public async Task<string> GetValueAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }
}
```

### 7.2. Connection Events

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var config = new ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        AbortOnConnectFail = false
    };

    var multiplexer = ConnectionMultiplexer.Connect(config);

    // ✅ Connection Failed
    multiplexer.ConnectionFailed += (sender, args) =>
    {
        logger.LogError(
            "Redis connection failed: {Endpoint} - {FailureType} - {Exception}",
            args.EndPoint,
            args.FailureType,
            args.Exception);
    };

    // ✅ Connection Restored
    multiplexer.ConnectionRestored += (sender, args) =>
    {
        logger.LogInformation(
            "Redis connection restored: {Endpoint} - {FailureType}",
            args.EndPoint,
            args.FailureType);
    };

    // ✅ Error Message
    multiplexer.ErrorMessage += (sender, args) =>
    {
        logger.LogWarning("Redis error: {Message}", args.Message);
    };

    // ✅ Internal Error
    multiplexer.InternalError += (sender, args) =>
    {
        logger.LogError(args.Exception, "Redis internal error");
    };

    // ✅ Configuration Changed
    multiplexer.ConfigurationChanged += (sender, args) =>
    {
        logger.LogInformation("Redis configuration changed: {Endpoint}", args.EndPoint);
    };

    // ✅ Configuration Changed Broadcast
    multiplexer.ConfigurationChangedBroadcast += (sender, args) =>
    {
        logger.LogInformation("Redis configuration broadcast: {Endpoint}", args.EndPoint);
    };

    return multiplexer;
});
```

### 7.3. Health Checks

```csharp
// Instalar paquete
// dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
// dotnet add package AspNetCore.HealthChecks.Redis

// Program.cs
builder.Services.AddHealthChecks()
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis"),
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cache", "redis" });

var app = builder.Build();

// Endpoint de health check
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });

        await context.Response.WriteAsync(result);
    }
});
```

---

## 8. Serialización

### 8.1. JSON (System.Text.Json)

```csharp
public class JsonRedisSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}

// Uso
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";
    var cached = await _cache.GetStringAsync(cacheKey);

    if (cached != null)
        return JsonRedisSerializer.Deserialize<Product>(cached);

    var product = await LoadFromDatabaseAsync(id);

    var serialized = JsonRedisSerializer.Serialize(product);
    await _cache.SetStringAsync(cacheKey, serialized, options);

    return product;
}
```

### 8.2. MessagePack (Más Compacto y Rápido)

```bash
dotnet add package MessagePack
```

```csharp
using MessagePack;

public class MessagePackRedisSerializer
{
    public static byte[] Serialize<T>(T value)
    {
        return MessagePackSerializer.Serialize(value);
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes);
    }
}

// Uso
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";
    var cached = await _cache.GetAsync(cacheKey);

    if (cached != null)
        return MessagePackRedisSerializer.Deserialize<Product>(cached);

    var product = await LoadFromDatabaseAsync(id);

    var serialized = MessagePackRedisSerializer.Serialize(product);
    await _cache.SetAsync(cacheKey, serialized, options);

    return product;
}
```

---

## 9. Patrones Avanzados

### 9.1. Distributed Lock

```csharp
public class DistributedLockService
{
    private readonly IDatabase _db;
    private readonly ILogger<DistributedLockService> _logger;

    public DistributedLockService(
        IConnectionMultiplexer redis,
        ILogger<DistributedLockService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    // ✅ Adquirir lock
    public async Task<bool> AcquireLockAsync(
        string resource,
        string lockId,
        TimeSpan expiration)
    {
        var key = $"lock:{resource}";

        // SET NX (Set if Not eXists) - Atómico
        var acquired = await _db.StringSetAsync(
            key,
            lockId,
            expiration,
            When.NotExists);

        if (acquired)
        {
            _logger.LogDebug("Lock acquired: {Resource} by {LockId}", resource, lockId);
        }

        return acquired;
    }

    // ✅ Liberar lock
    public async Task<bool> ReleaseLockAsync(string resource, string lockId)
    {
        var key = $"lock:{resource}";

        // ✅ Lua script para verificar ownership antes de eliminar
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        var result = await _db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[] { lockId });

        var released = (int)result == 1;

        if (released)
        {
            _logger.LogDebug("Lock released: {Resource} by {LockId}", resource, lockId);
        }

        return released;
    }

    // ✅ Try-Execute pattern con lock
    public async Task<T> ExecuteWithLockAsync<T>(
        string resource,
        Func<Task<T>> action,
        TimeSpan? lockExpiration = null,
        TimeSpan? timeout = null)
    {
        var lockId = Guid.NewGuid().ToString();
        var expiration = lockExpiration ?? TimeSpan.FromSeconds(30);
        var maxWait = timeout ?? TimeSpan.FromSeconds(10);

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxWait)
        {
            var acquired = await AcquireLockAsync(resource, lockId, expiration);

            if (acquired)
            {
                try
                {
                    return await action();
                }
                finally
                {
                    await ReleaseLockAsync(resource, lockId);
                }
            }

            // Esperar antes de reintentar
            await Task.Delay(100);
        }

        throw new TimeoutException($"Could not acquire lock for resource: {resource}");
    }
}

// Uso
public async Task ProcessOrderAsync(int orderId)
{
    await _lockService.ExecuteWithLockAsync(
        $"order:{orderId}",
        async () =>
        {
            // ✅ Solo un servidor procesará esto a la vez
            var order = await _dbContext.Orders.FindAsync(orderId);
            order.Status = "Processed";
            await _dbContext.SaveChangesAsync();
        },
        lockExpiration: TimeSpan.FromSeconds(30),
        timeout: TimeSpan.FromSeconds(10));
}
```

### 9.2. Cache Invalidation por Evento

```csharp
public class CacheInvalidationService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;

    public CacheInvalidationService(
        IDistributedCache cache,
        IConnectionMultiplexer redis)
    {
        _cache = cache;
        _redis = redis;
    }

    // ✅ Publicar evento de invalidación
    public async Task InvalidateProductCacheAsync(int productId)
    {
        var cacheKey = $"product:{productId}";

        // 1️⃣ Invalidar en cache local
        await _cache.RemoveAsync(cacheKey);

        // 2️⃣ Publicar evento para invalidar en otros servidores
        var subscriber = _redis.GetSubscriber();
        await subscriber.PublishAsync("cache:invalidate", cacheKey);
    }

    // ✅ Suscribirse a eventos de invalidación (BackgroundService)
    public async Task SubscribeToCacheInvalidationAsync(CancellationToken ct)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync("cache:invalidate", async (channel, message) =>
        {
            var cacheKey = message.ToString();

            // Invalidar en cache local cuando otro servidor publica
            await _cache.RemoveAsync(cacheKey);

            _logger.LogDebug("Cache invalidated via pub/sub: {CacheKey}", cacheKey);
        });
    }
}
```

---

## 10. Best Practices

### 10.1. Connection String Segura

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}

// appsettings.Production.json
{
  "ConnectionStrings": {
    "Redis": "production-redis.cache.windows.net:6380,password=,ssl=True,abortConnect=False"
  }
}
```

```csharp
// Mejor: Usar Azure Key Vault o User Secrets
// dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
```

### 10.2. Naming Conventions

```csharp
public static class CacheKeys
{
    // ✅ Namespace jerárquico con versión
    public static string Product(int id) => $"v1:product:{id}";
    public static string ProductsByCategory(int categoryId)
        => $"v1:products:category:{categoryId}";

    // ✅ Con tenant isolation
    public static string TenantProduct(string tenantId, int id)
        => $"v1:tenant:{tenantId}:product:{id}";

    // ✅ Locks
    public static string ProductLock(int id) => $"lock:product:{id}";

    // ✅ Rate limiting
    public static string RateLimit(string userId) => $"ratelimit:{userId}";
}
```

### 10.3. Expiration Strategies

```csharp
public class CacheExpirationHelper
{
    // ✅ Diferentes estrategias según tipo de dato
    public static TimeSpan GetExpiration(CacheType cacheType)
    {
        return cacheType switch
        {
            CacheType.StaticData => TimeSpan.FromHours(24),      // Catálogos
            CacheType.UserData => TimeSpan.FromMinutes(30),      // Datos de usuario
            CacheType.SessionData => TimeSpan.FromMinutes(20),   // Sesión
            CacheType.TempData => TimeSpan.FromMinutes(5),       // Datos temporales
            CacheType.Lock => TimeSpan.FromSeconds(30),          // Distributed locks
            _ => TimeSpan.FromMinutes(15)
        };
    }
}

public enum CacheType
{
    StaticData,
    UserData,
    SessionData,
    TempData,
    Lock
}
```

### 10.4. Error Handling

```csharp
public async Task<T> GetOrSetSafeAsync<T>(
    string key,
    Func<Task<T>> factory,
    TimeSpan expiration,
    CancellationToken ct = default)
{
    try
    {
        var cached = await _cache.GetStringAsync(key, ct);

        if (cached != null)
            return JsonSerializer.Deserialize<T>(cached);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Redis GET failed for key: {CacheKey}. Falling back to database.", key);
        // ✅ Continuar sin cache en caso de error
    }

    var value = await factory();

    try
    {
        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        }, ct);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Redis SET failed for key: {CacheKey}. Data will not be cached.", key);
        // ✅ No fallar si no se puede cachear
    }

    return value;
}
```

### 10.5. Monitoring y Métricas

```csharp
public class RedisMetrics
{
    private long _hits;
    private long _misses;
    private long _errors;

    public void RecordHit() => Interlocked.Increment(ref _hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);
    public void RecordError() => Interlocked.Increment(ref _errors);

    public double HitRate
    {
        get
        {
            var total = _hits + _misses;
            return total == 0 ? 0 : (double)_hits / total * 100;
        }
    }

    public (long hits, long misses, long errors, double hitRate) GetStats()
    {
        return (_hits, _misses, _errors, HitRate);
    }
}
```

---

## 11. Anti-Patterns Comunes

### 11.1. Usar KEYS en Producción

```csharp
// ❌ MAL: KEYS bloquea Redis
public async Task<List<string>> FindKeysAsync(string pattern)
{
    var server = _redis.GetServer(_redis.GetEndPoints().First());

    // ⚠️ KEYS * bloquea Redis completamente
    var keys = server.Keys(pattern: pattern).ToList();

    return keys.Select(k => k.ToString()).ToList();
}

// ✅ BIEN: Usar SCAN (no bloquea)
public async Task<List<string>> FindKeysAsync(string pattern)
{
    var server = _redis.GetServer(_redis.GetEndPoints().First());
    var keys = new List<string>();

    // ✅ SCAN es iterativo y no bloquea
    await foreach (var key in server.KeysAsync(pattern: pattern))
    {
        keys.Add(key.ToString());
    }

    return keys;
}
```

### 11.2. No Configurar Expiración

```csharp
// ❌ MAL: Sin expiración
await _db.StringSetAsync("key", "value");

// ✅ BIEN: Siempre establecer expiración
await _db.StringSetAsync("key", "value", TimeSpan.FromMinutes(30));
```

### 11.3. Cachear Objetos Masivos

```csharp
// ❌ MAL: Cachear datasets gigantes
public async Task<List<LogEntry>> GetAllLogsAsync()
{
    // ⚠️ Millones de registros
    return await _cache.GetOrSetAsync("logs:all",
        () => _dbContext.Logs.ToListAsync(),
        TimeSpan.FromHours(1));
}

// ✅ BIEN: Cachear solo datasets razonables
public async Task<List<LogEntry>> GetRecentLogsAsync()
{
    return await _cache.GetOrSetAsync("logs:recent",
        () => _dbContext.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync(),
        TimeSpan.FromMinutes(5));
}
```

### 11.4. No Manejar Conexión Fallida

```csharp
// ❌ MAL: AbortOnConnectFail = true (default antes)
var config = new ConfigurationOptions
{
    EndPoints = { "localhost:6379" },
    AbortOnConnectFail = true // ← Fallará al iniciar si Redis no está disponible
};

// ✅ BIEN: AbortOnConnectFail = false para resiliencia
var config = new ConfigurationOptions
{
    EndPoints = { "localhost:6379" },
    AbortOnConnectFail = false, // ← Permite iniciar sin Redis
    ConnectRetry = 3,
    ConnectTimeout = 5000
};
```

---

## 12. Testing

### 12.1. Mock IDistributedCache

```csharp
using Moq;

public class OrderServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _sut = new OrderService(_cacheMock.Object, mockDbContext, mockLogger);
    }

    [Fact]
    public async Task GetOrderAsync_OnCacheHit_ReturnsFromCache()
    {
        // Arrange
        var orderId = 1;
        var cachedOrder = new Order { Id = orderId, Total = 100 };
        var serialized = JsonSerializer.Serialize(cachedOrder);

        _cacheMock
            .Setup(x => x.GetAsync(
                It.Is<string>(k => k == $"order:{orderId}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(serialized));

        // Act
        var result = await _sut.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.Equal(100, result.Total);
    }

    [Fact]
    public async Task GetOrderAsync_OnCacheMiss_LoadsFromDatabase()
    {
        // Arrange
        var orderId = 1;

        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null);

        // Act
        var result = await _sut.GetOrderByIdAsync(orderId);

        // Assert
        Assert.NotNull(result);

        _cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### 12.2. Integration Tests con Testcontainers

```bash
dotnet add package Testcontainers.Redis
```

```csharp
using Testcontainers.Redis;

public class RedisIntegrationTests : IAsyncLifetime
{
    private RedisContainer _redisContainer;
    private IConnectionMultiplexer _redis;

    public async Task InitializeAsync()
    {
        // ✅ Iniciar contenedor de Redis
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // ✅ Conectar a Redis en el contenedor
        _redis = await ConnectionMultiplexer.ConnectAsync(
            _redisContainer.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task StringSet_AndGet_WorksCorrectly()
    {
        // Arrange
        var db = _redis.GetDatabase();
        var key = "test:key";
        var value = "test value";

        // Act
        await db.StringSetAsync(key, value, TimeSpan.FromMinutes(1));
        var result = await db.StringGetAsync(key);

        // Assert
        Assert.Equal(value, result.ToString());
    }

    [Fact]
    public async Task PubSub_PublishAndSubscribe_WorksCorrectly()
    {
        // Arrange
        var subscriber = _redis.GetSubscriber();
        var receivedMessages = new List<string>();
        var channel = "test:channel";

        await subscriber.SubscribeAsync(channel, (ch, message) =>
        {
            receivedMessages.Add(message);
        });

        // Act
        await subscriber.PublishAsync(channel, "message 1");
        await subscriber.PublishAsync(channel, "message 2");

        // Wait for messages
        await Task.Delay(100);

        // Assert
        Assert.Equal(2, receivedMessages.Count);
        Assert.Contains("message 1", receivedMessages);
        Assert.Contains("message 2", receivedMessages);
    }
}
```

---

## 13. Performance y Monitoring

### 13.1. Benchmarking

```csharp
public class RedisBenchmark
{
    private readonly IDatabase _db;

    public async Task<BenchmarkResult> BenchmarkStringOperationsAsync(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await _db.StringSetAsync($"bench:key:{i}", $"value{i}");
        }

        var setTime = stopwatch.Elapsed;
        stopwatch.Restart();

        for (int i = 0; i < iterations; i++)
        {
            await _db.StringGetAsync($"bench:key:{i}");
        }

        var getTime = stopwatch.Elapsed;

        return new BenchmarkResult
        {
            Iterations = iterations,
            SetTime = setTime,
            GetTime = getTime,
            SetOpsPerSecond = iterations / setTime.TotalSeconds,
            GetOpsPerSecond = iterations / getTime.TotalSeconds
        };
    }
}
```

### 13.2. Monitoring con ConnectionMultiplexer

```csharp
public class RedisMonitoringService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMonitoringService> _logger;

    public async Task LogServerInfoAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        // ✅ Info del servidor
        var info = await server.InfoAsync();

        foreach (var section in info)
        {
            _logger.LogInformation("Redis Info - {Section}:", section.Key);

            foreach (var kvp in section)
            {
                _logger.LogDebug("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }

        // ✅ Estadísticas de conexión
        var counters = _redis.GetCounters();

        _logger.LogInformation(
            "Redis Stats - Connections: {TotalOutstanding}, Operations: {TotalOutstanding}",
            counters.TotalOutstanding,
            counters.Interactive.CompletedSynchronously + counters.Interactive.CompletedAsynchronously);
    }
}
```

---

## 14. Referencias

### Documentación Oficial
- [StackExchange.Redis Documentation](https://stackexchange.github.io/StackExchange.Redis/)
- [Redis Commands](https://redis.io/commands)
- [IDistributedCache - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)

### Guías Relacionadas
- [Caching Overview](./README.md) - Comparación de tipos de caching
- [Memory Cache](./memory-cache.md) - In-memory caching con IMemoryCache

### Recursos
- [Redis University](https://university.redis.com/) - Cursos gratuitos de Redis
- [Redis Best Practices](https://redis.io/docs/manual/patterns/)
- [Azure Redis Cache](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/)

---

**Próximos pasos**:
1. Configurar Redis en ambiente de desarrollo (Docker, Azure Redis)
2. Implementar patrón cache-aside con IDistributedCache
3. Configurar health checks y monitoring
4. Implementar rate limiting distribuido
5. Explorar Pub/Sub para notificaciones en tiempo real
