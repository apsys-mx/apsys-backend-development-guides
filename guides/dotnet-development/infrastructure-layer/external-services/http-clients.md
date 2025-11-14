# HttpClient Patterns - External Services

**Versión**: 1.0.0
**Última actualización**: 2025-01-14

## 📋 Tabla de Contenidos
1. [El Problema con `new HttpClient()`](#el-problema-con-new-httpclient)
2. [IHttpClientFactory: La Solución](#ihttpclientfactory-la-solución)
3. [Patrones de Implementación](#patrones-de-implementación)
4. [Ejemplos del Proyecto de Referencia](#ejemplos-del-proyecto-de-referencia)
5. [Operaciones HTTP Comunes](#operaciones-http-comunes)
6. [Configuración Avanzada](#configuración-avanzada)
7. [Manejo de Errores](#manejo-de-errores)
8. [Best Practices](#best-practices)
9. [Referencias](#referencias)

---

## El Problema con `new HttpClient()`

### ❌ Anti-Pattern en Proyecto de Referencia

**Del proyecto hashira.stone.backend** (Auth0Service.cs):

```csharp
// ❌ ANTI-PATTERN: Crear nuevo HttpClient en cada llamada
private static HttpResponseMessage PostAsync(string requestUri, object? body, string authToken)
{
    using (var httpClient = new HttpClient())  // ← PROBLEMA
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        return httpClient.PostAsync(requestUri, content).Result;  // ← Blocking call
    }
}

private static HttpResponseMessage GetAsync(string requestUri, string authToken)
{
    using (var httpClient = new HttpClient())  // ← PROBLEMA
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        return httpClient.GetAsync(requestUri).Result;  // ← Blocking call
    }
}
```

### 🚨 Problemas Críticos

#### 1. **Socket Exhaustion**

Cada `new HttpClient()` abre una nueva conexión TCP:

```
Request 1: new HttpClient() → Socket A abierto
Request 2: new HttpClient() → Socket B abierto
Request 3: new HttpClient() → Socket C abierto
...
Request 100: ❌ No more sockets available!
```

**Resultado**: Excepción `SocketException: No connection could be made because the target machine actively refused it`

#### 2. **DNS Caching Indefinido**

`HttpClient` cachea DNS resolution **indefinidamente**:

```csharp
var client = new HttpClient();
// DNS resuelve api.example.com → 192.168.1.100

// 1 hora después, api.example.com cambia a 192.168.1.200
// HttpClient sigue usando 192.168.1.100 ❌
```

**Resultado**: Requests fallan después de cambios de DNS

#### 3. **Blocking I/O**

Uso de `.Result` bloquea threads:

```csharp
return httpClient.PostAsync(requestUri, content).Result;  // ❌ Bloquea thread
```

**Resultado**: Thread starvation, deadlocks en contextos síncronos

---

## IHttpClientFactory: La Solución

### ✅ Por qué IHttpClientFactory

**IHttpClientFactory** fue introducido en .NET Core 2.1 para resolver todos estos problemas:

- ✅ **Connection Pooling**: Reutiliza conexiones HTTP
- ✅ **DNS Refresh**: Respeta DNS TTL y cambios
- ✅ **Configuración Centralizada**: Un solo lugar para configurar timeouts, headers, etc.
- ✅ **Lifecycle Management**: Maneja correctamente la vida útil de HttpClient
- ✅ **Integración con Polly**: Retry policies, circuit breakers, etc.

### 📦 Instalación

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http" Version="9.0+" />
</ItemGroup>
```

---

## Patrones de Implementación

### 1. 🏷️ Named HttpClients (Básico)

**Registro en DI**:

```csharp
public static IServiceCollection ConfigureHttpClients(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Named HttpClient para Auth0
    services.AddHttpClient("Auth0", client =>
    {
        client.BaseAddress = new Uri(configuration["Auth0ManagementSettings:Domain"]);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    return services;
}
```

**Uso en Service**:

```csharp
public class Auth0Service : IIdentityService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public Auth0Service(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        // ✅ Obtiene HttpClient desde el factory
        var client = _httpClientFactory.CreateClient("Auth0");

        var authToken = await GetTokenAccessValueAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var body = new { email = username, name, password, connection = GetConnection() };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v2/users", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<User>(result);
    }
}
```

**Ventajas**:
- ✅ Simple de implementar
- ✅ Configuración centralizada
- ✅ Connection pooling automático

**Desventajas**:
- ⚠️ Requires string literal ("Auth0") - no type safety
- ⚠️ Headers dinámicos (como Authorization) deben configurarse en cada request

---

### 2. 🎯 Typed HttpClients (Recomendado)

**Definir Typed Client**:

```csharp
public class Auth0HttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public Auth0HttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;

        // Configuración en constructor
        _httpClient.BaseAddress = new Uri(configuration["Auth0ManagementSettings:Domain"]);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Get OAuth2 access token
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Auth0ManagementSettings:ClientId"];
        var clientSecret = _configuration["Auth0ManagementSettings:ClientSecret"];
        var audience = _configuration["Auth0ManagementSettings:Audience"];
        var grantType = _configuration["Auth0ManagementSettings:GrantType"];

        var body = new
        {
            client_id = clientId,
            client_secret = clientSecret,
            audience = audience,
            grant_type = grantType
        };

        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/oauth/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonConvert.DeserializeAnonymousType(result, new { access_token = "" });

        return tokenResponse?.access_token ?? throw new InvalidOperationException("Failed to get access token");
    }

    /// <summary>
    /// Create user in Auth0
    /// </summary>
    public async Task<Auth0UserResponse> CreateUserAsync(
        string email,
        string name,
        string password,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var connection = _configuration["Auth0ManagementSettings:Connection"];
        var body = new { email, name, password, connection };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/v2/users", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errorContent.Contains("user already exists"))
                throw new DuplicatedDomainException($"User with email '{email}' already exists");

            throw new HttpRequestException($"Auth0 API error: {errorContent}");
        }

        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<Auth0UserResponse>(result);
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    public async Task<Auth0UserResponse?> GetUserByEmailAsync(
        string email,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var encodedEmail = Uri.EscapeDataString($"\"{email}\"");
        var response = await _httpClient.GetAsync($"/api/v2/users?q=email={encodedEmail}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        var users = JsonConvert.DeserializeObject<List<Auth0UserResponse>>(result);

        return users?.FirstOrDefault();
    }

    /// <summary>
    /// Update user password
    /// </summary>
    public async Task<Auth0UserResponse> UpdatePasswordAsync(
        string userId,
        string newPassword,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var connection = _configuration["Auth0ManagementSettings:Connection"];
        var body = new { password = newPassword, connection };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync($"/api/v2/users/{userId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<Auth0UserResponse>(result);
    }
}

public class Auth0UserResponse
{
    public string user_id { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
}
```

**Registro en DI**:

```csharp
services.AddHttpClient<Auth0HttpClient>();
```

**Uso en Service**:

```csharp
public class Auth0Service : IIdentityService
{
    private readonly Auth0HttpClient _auth0Client;

    public Auth0Service(Auth0HttpClient auth0Client)
    {
        _auth0Client = auth0Client;
    }

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        // ✅ Obtener token
        var accessToken = await _auth0Client.GetAccessTokenAsync();

        // ✅ Crear usuario
        var auth0User = await _auth0Client.CreateUserAsync(username, name, password, accessToken);

        return new User
        {
            UserId = auth0User.user_id,
            Email = auth0User.email,
            Name = auth0User.name,
            CreationDate = auth0User.created_at
        };
    }

    public async Task<User?> GetByEmail(string email)
    {
        var accessToken = await _auth0Client.GetAccessTokenAsync();
        var auth0User = await _auth0Client.GetUserByEmailAsync(email, accessToken);

        if (auth0User == null)
            return null;

        return new User
        {
            UserId = auth0User.user_id,
            Email = auth0User.email,
            Name = auth0User.name,
            CreationDate = auth0User.created_at
        };
    }
}
```

**Ventajas**:
- ✅ **Type-safe**: No string literals
- ✅ **Encapsulación**: Lógica HTTP separada del service
- ✅ **Testeable**: Fácil de mockear Auth0HttpClient
- ✅ **Reutilizable**: Múltiples services pueden usar el mismo typed client

---

### 3. 🔄 HttpClient con Polly (Resiliencia)

**Instalación**:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="9.0+" />
</ItemGroup>
```

**Registro con Retry Policy**:

```csharp
services.AddHttpClient<Auth0HttpClient>()
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message}");
            }
        )
    );
```

**Retry Policy con Circuit Breaker**:

```csharp
services.AddHttpClient<Auth0HttpClient>()
    // Retry 3 veces con exponential backoff
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    )
    // Circuit breaker: abre después de 5 fallas consecutivas
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)
        )
    );
```

**Ventajas**:
- ✅ **Automatic retries**: Reintentos automáticos en fallas transitorias
- ✅ **Circuit breaker**: Evita sobrecargar servicio fallando
- ✅ **Exponential backoff**: Tiempo de espera creciente entre reintentos

---

## Ejemplos del Proyecto de Referencia

### 📋 Refactorización: Auth0Service con IHttpClientFactory

**Antes (Anti-pattern)**:

```csharp
// ❌ ANTI-PATTERN del proyecto de referencia
public User Create(string username, string name, string password)
{
    var authToken = this.GetTokenAccessValue();
    var domain = this.configuration.GetSection("Auth0ManagementSettings:Domain").Value;
    var connection = this.configuration.GetSection("Auth0ManagementSettings:Connection").Value;

    var url = $"{domain}/api/v2/users";
    var body = new { email = username, name, password, connection };
    var httpClienteResult = PostAsync(url, body, authToken);  // ← new HttpClient() inside
    var content = httpClienteResult.Content.ReadAsStringAsync().Result;

    if (!httpClienteResult.IsSuccessStatusCode)
        throw new HttpRequestException($"Error creating new user {content}");

    return new User() { Email = username, Name = name };
}

private static HttpResponseMessage PostAsync(string requestUri, object? body, string authToken)
{
    using (var httpClient = new HttpClient())  // ❌
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        return httpClient.PostAsync(requestUri, content).Result;  // ❌ Blocking
    }
}
```

**Después (Best Practice)**:

```csharp
// ✅ CORRECTO con IHttpClientFactory
public class Auth0Service : IIdentityService
{
    private readonly Auth0HttpClient _auth0Client;

    public Auth0Service(Auth0HttpClient auth0Client)
    {
        _auth0Client = auth0Client;
    }

    public async Task<User> CreateAsync(string username, string name, string password)
    {
        var accessToken = await _auth0Client.GetAccessTokenAsync();
        var auth0User = await _auth0Client.CreateUserAsync(username, name, password, accessToken);

        return new User
        {
            UserId = auth0User.user_id,
            Email = auth0User.email,
            Name = auth0User.name,
            CreationDate = auth0User.created_at
        };
    }
}
```

---

## Operaciones HTTP Comunes

### GET Request

```csharp
public async Task<List<TResource>> GetAllAsync<TResource>(
    string endpoint,
    CancellationToken cancellationToken = default)
{
    var response = await _httpClient.GetAsync(endpoint, cancellationToken);
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync(cancellationToken);
    return JsonConvert.DeserializeObject<List<TResource>>(content);
}
```

### POST Request

```csharp
public async Task<TResponse> PostAsync<TRequest, TResponse>(
    string endpoint,
    TRequest body,
    CancellationToken cancellationToken = default)
{
    var json = JsonConvert.SerializeObject(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadAsStringAsync(cancellationToken);
    return JsonConvert.DeserializeObject<TResponse>(result);
}
```

### PUT Request

```csharp
public async Task<TResponse> PutAsync<TRequest, TResponse>(
    string endpoint,
    TRequest body,
    CancellationToken cancellationToken = default)
{
    var json = JsonConvert.SerializeObject(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadAsStringAsync(cancellationToken);
    return JsonConvert.DeserializeObject<TResponse>(result);
}
```

### PATCH Request

```csharp
public async Task<TResponse> PatchAsync<TRequest, TResponse>(
    string endpoint,
    TRequest body,
    CancellationToken cancellationToken = default)
{
    var json = JsonConvert.SerializeObject(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PatchAsync(endpoint, content, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadAsStringAsync(cancellationToken);
    return JsonConvert.DeserializeObject<TResponse>(result);
}
```

### DELETE Request

```csharp
public async Task DeleteAsync(
    string endpoint,
    CancellationToken cancellationToken = default)
{
    var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
    response.EnsureSuccessStatusCode();
}
```

---

## Configuración Avanzada

### Headers Customizados

```csharp
services.AddHttpClient<Auth0HttpClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("X-Custom-Header", "value");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
```

### Timeout Personalizado

```csharp
services.AddHttpClient<Auth0HttpClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);  // 30 segundos
});
```

### Timeout por Request

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));  // 10 segundos
var response = await _httpClient.GetAsync("/api/endpoint", cts.Token);
```

### Compression

```csharp
services.AddHttpClient<Auth0HttpClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });
```

### Client Certificates

```csharp
services.AddHttpClient<Auth0HttpClient>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var certificate = new X509Certificate2("path/to/certificate.pfx", "password");
        handler.ClientCertificates.Add(certificate);
        return handler;
    });
```

---

## Manejo de Errores

### Uso de EnsureSuccessStatusCode

```csharp
try
{
    var response = await _httpClient.GetAsync("/api/users");
    response.EnsureSuccessStatusCode();  // ← Lanza HttpRequestException si status code != 2xx

    var content = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<List<User>>(content);
}
catch (HttpRequestException ex)
{
    // Log y manejo de error
    throw new ExternalServiceException($"Failed to get users: {ex.Message}", ex);
}
```

### Manejo Manual de Status Codes

```csharp
var response = await _httpClient.GetAsync("/api/users");

if (response.StatusCode == HttpStatusCode.NotFound)
{
    return null;  // Usuario no encontrado
}

if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    throw new UnauthorizedException("Access token expired or invalid");
}

if (!response.IsSuccessStatusCode)
{
    var errorContent = await response.Content.ReadAsStringAsync();
    throw new ExternalServiceException($"API error: {errorContent}");
}

var content = await response.Content.ReadAsStringAsync();
return JsonConvert.DeserializeObject<User>(content);
```

### Excepciones de Dominio

```csharp
var response = await _httpClient.PostAsync("/api/users", content);

if (!response.IsSuccessStatusCode)
{
    var errorContent = await response.Content.ReadAsStringAsync();

    // Mapear errores de API a excepciones de dominio
    if (errorContent.Contains("user already exists"))
        throw new DuplicatedDomainException($"User with email '{email}' already exists");

    if (errorContent.Contains("invalid email"))
        throw new InvalidDomainException("Invalid email format");

    throw new ExternalServiceException($"Auth0 API error: {errorContent}");
}
```

---

## Best Practices

### ✅ 1. Siempre Usar IHttpClientFactory

```csharp
// ❌ NUNCA
using var client = new HttpClient();

// ✅ SIEMPRE
services.AddHttpClient<MyHttpClient>();
```

---

### ✅ 2. Async/Await (NO Blocking Calls)

```csharp
// ❌ INCORRECTO
var result = httpClient.GetAsync(url).Result;

// ✅ CORRECTO
var result = await httpClient.GetAsync(url, cancellationToken);
```

---

### ✅ 3. Usar CancellationToken

```csharp
public async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
{
    var response = await _httpClient.GetAsync($"/api/users/{id}", cancellationToken);
    response.EnsureSuccessStatusCode();
    // ...
}
```

---

### ✅ 4. Typed HttpClients sobre Named

```csharp
// ⚠️ OK pero no ideal
var client = _httpClientFactory.CreateClient("Auth0");

// ✅ MEJOR
public class Auth0HttpClient { /* ... */ }
services.AddHttpClient<Auth0HttpClient>();
```

---

### ✅ 5. Configurar BaseAddress

```csharp
services.AddHttpClient<Auth0HttpClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Auth0:Domain"]);
});

// Uso
await _httpClient.GetAsync("/api/v2/users");  // ← Relative URL
```

---

### ✅ 6. Dispose NO es Necesario

```csharp
// ❌ INCORRECTO (cuando viene de IHttpClientFactory)
using var client = _httpClientFactory.CreateClient("Auth0");  // ← NO necesario

// ✅ CORRECTO
var client = _httpClientFactory.CreateClient("Auth0");
// No dispose, el factory maneja el lifecycle
```

---

### ✅ 7. Retry Policies con Polly

```csharp
services.AddHttpClient<Auth0HttpClient>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    );
```

---

### ✅ 8. Logging de Requests

```csharp
services.AddHttpClient<Auth0HttpClient>()
    .AddHttpMessageHandler<LoggingDelegatingHandler>();

public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP {Method} {Uri}", request.Method, request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogInformation("HTTP {StatusCode} from {Uri}", (int)response.StatusCode, request.RequestUri);

        return response;
    }
}
```

---

## Referencias

### 📚 Documentación Oficial

- [IHttpClientFactory - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [HttpClient Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Polly - Resilience Policies](https://github.com/App-vNext/Polly)
- [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http/)

### 🔗 Guías Relacionadas

- [External Services Overview](./README.md) - Overview de servicios externos
- [Auth0 Integration](./identity-providers/auth0.md) - Implementación completa de Auth0
- [Best Practices](../../best-practices/README.md) - Prácticas generales

---

## 🔄 Changelog

| Versión | Fecha      | Cambios                                  |
|---------|------------|------------------------------------------|
| 1.0.0   | 2025-01-14 | Versión inicial de HttpClient patterns   |

---

**Siguiente**: [Identity Providers](./identity-providers/README.md) - Auth providers overview →
