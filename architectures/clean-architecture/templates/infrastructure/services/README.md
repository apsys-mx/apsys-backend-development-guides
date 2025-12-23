# Services - Servicios Externos e Integraciones

## Propósito

Esta carpeta contiene implementaciones de **servicios externos** e integraciones con sistemas de terceros: APIs externas, servicios de email, almacenamiento en la nube, servicios de notificaciones, etc.

## Responsabilidades

1. ✅ Implementar interfaces de servicios definidas en Domain
2. ✅ Gestionar comunicación con APIs externas
3. ✅ Manejar autenticación y autorización con servicios externos
4. ✅ Implementar retry logic y manejo de errores
5. ✅ Logging de interacciones externas

## Estructura Recomendada

```
services/
├── email/
│   ├── SmtpEmailService.cs          # Servicio de email SMTP
│   └── SendGridEmailService.cs      # Servicio de email SendGrid
├── storage/
│   ├── S3StorageService.cs          # AWS S3
│   └── AzureBlobStorageService.cs   # Azure Blob Storage
├── notifications/
│   └── PushNotificationService.cs   # Notificaciones push
└── external-apis/
    └── WeatherApiClient.cs          # Cliente de API externa
```

## Ejemplo: Servicio de Email

### Interface en Domain

```csharp
namespace {ProjectName}.domain.interfaces.services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
    Task SendTemplatedAsync(string to, string templateId, object data);
}
```

### Implementación en Infrastructure

```csharp
using System.Net;
using System.Net.Mail;
using {ProjectName}.domain.interfaces.services;

namespace {ProjectName}.infrastructure.services.email;

public class SmtpEmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;

    public SmtpEmailService(IConfiguration configuration)
    {
        _smtpHost = configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host not configured");
        _smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
        _smtpUser = configuration["Smtp:User"] ?? throw new InvalidOperationException("SMTP User not configured");
        _smtpPassword = configuration["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
            EnableSsl = true
        };

        var message = new MailMessage(_smtpUser, to, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }

    public async Task SendTemplatedAsync(string to, string templateId, object data)
    {
        // Implementar lógica de templates
        throw new NotImplementedException();
    }
}
```

## Ejemplo: Cliente de API Externa

### Interface en Domain

```csharp
namespace {ProjectName}.domain.interfaces.services;

public interface IWeatherService
{
    Task<WeatherData> GetCurrentWeatherAsync(string city);
    Task<IEnumerable<WeatherForecast>> GetForecastAsync(string city, int days);
}

public record WeatherData(string City, double Temperature, string Description);
public record WeatherForecast(DateTime Date, double Temperature, string Description);
```

### Implementación con HttpClient

```csharp
using System.Text.Json;
using {ProjectName}.domain.interfaces.services;

namespace {ProjectName}.infrastructure.services.external-apis;

public class WeatherApiClient : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WeatherApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApi:ApiKey"] ?? throw new InvalidOperationException("Weather API Key not configured");

        _httpClient.BaseAddress = new Uri("https://api.weatherapi.com/v1/");
    }

    public async Task<WeatherData> GetCurrentWeatherAsync(string city)
    {
        var response = await _httpClient.GetAsync($"current.json?key={_apiKey}&q={city}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var weatherDto = JsonSerializer.Deserialize<WeatherApiResponse>(content);

        return new WeatherData(
            weatherDto!.Location.Name,
            weatherDto.Current.TempC,
            weatherDto.Current.Condition.Text
        );
    }

    public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(string city, int days)
    {
        var response = await _httpClient.GetAsync($"forecast.json?key={_apiKey}&q={city}&days={days}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var forecastDto = JsonSerializer.Deserialize<ForecastApiResponse>(content);

        return forecastDto!.Forecast.ForecastDay.Select(day => new WeatherForecast(
            day.Date,
            day.Day.AvgTempC,
            day.Day.Condition.Text
        ));
    }

    // DTOs internos para deserialización
    private record WeatherApiResponse(LocationDto Location, CurrentDto Current);
    private record LocationDto(string Name);
    private record CurrentDto(double TempC, ConditionDto Condition);
    private record ConditionDto(string Text);
    private record ForecastApiResponse(ForecastDataDto Forecast);
    private record ForecastDataDto(List<ForecastDayDto> ForecastDay);
    private record ForecastDayDto(DateTime Date, DayDto Day);
    private record DayDto(double AvgTempC, ConditionDto Condition);
}
```

## Ejemplo: Servicio de Almacenamiento (S3)

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using {ProjectName}.domain.interfaces.services;

namespace {ProjectName}.infrastructure.services.storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:S3:BucketName"] ?? throw new InvalidOperationException("S3 Bucket not configured");
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request);

        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request);
    }
}
```

## Principios

### 1. Interfaces en Domain, Implementaciones en Infrastructure

```csharp
// Domain - Define QUÉ se necesita
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

// Infrastructure - Define CÓMO se hace
public class SmtpEmailService : IEmailService { }
public class SendGridEmailService : IEmailService { }
```

### 2. Configuración desde Variables de Entorno

```csharp
// ❌ INCORRECTO - Hardcoded
var apiKey = "sk_live_12345...";

// ✅ CORRECTO - Desde configuración
var apiKey = configuration["ExternalApi:ApiKey"]
    ?? Environment.GetEnvironmentVariable("EXTERNAL_API_KEY")
    ?? throw new InvalidOperationException("API Key not configured");
```

### 3. HttpClient con Dependency Injection

```csharp
// En Program.cs o ServiceCollectionExtensions
services.AddHttpClient<IWeatherService, WeatherApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.weatherapi.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### 4. Retry Logic con Polly

```csharp
services.AddHttpClient<IWeatherService, WeatherApiClient>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

### 5. Logging de Interacciones

```csharp
public async Task SendAsync(string to, string subject, string body)
{
    _logger.LogInformation("Sending email to {To} with subject {Subject}", to, subject);

    try
    {
        await _smtpClient.SendMailAsync(message);
        _logger.LogInformation("Email sent successfully to {To}", to);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send email to {To}", to);
        throw;
    }
}
```

## Registro en Dependency Injection

```csharp
// En ServiceCollectionExtensions.cs o Program.cs
public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
{
    // Email
    services.AddScoped<IEmailService, SmtpEmailService>();

    // Storage
    services.AddAWSService<IAmazonS3>();
    services.AddScoped<IStorageService, S3StorageService>();

    // External APIs
    services.AddHttpClient<IWeatherService, WeatherApiClient>();

    return services;
}
```

## Next Steps

Para ejemplos completos de servicios externos:

- **Email con SendGrid**: Ver ejemplos en repositorio de plantillas
- **Storage con AWS S3**: Ver ejemplos en repositorio de plantillas
- **Push Notifications**: Ver ejemplos en repositorio de plantillas
