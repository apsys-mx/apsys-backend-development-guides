# Date Handling - Backend

**Version:** 1.0.0
**Last Updated:** 2025-01-23
**Status:** Complete

---

Guia para el manejo correcto de fechas en el backend, complementaria a la guia de frontend.

## Principio Fundamental

> **"Store UTC, Display Local"** - Almacenar en UTC, Mostrar en Local

El backend SIEMPRE almacena y procesa fechas en UTC. La conversion a hora local es responsabilidad del frontend.

---

## Table of Contents

1. [El Problema](#el-problema)
2. [Recepcion de Fechas (WebApi Layer)](#recepcion-de-fechas-webapi-layer)
3. [Conversion en AutoMapper](#conversion-en-automapper)
4. [Almacenamiento (Domain Layer)](#almacenamiento-domain-layer)
5. [Comparaciones de Fechas](#comparaciones-de-fechas)
6. [Respuestas al Frontend](#respuestas-al-frontend)
7. [Configuracion de JSON Serializer](#configuracion-de-json-serializer)
8. [Ejemplos Completos](#ejemplos-completos)
9. [Checklist de Implementacion](#checklist-de-implementacion)
10. [Anti-Patterns](#anti-patterns)

---

## El Problema

Cuando el frontend envia una fecha **sin timezone**, el backend la interpreta incorrectamente:

```
Frontend envia:     "2026-01-22T06:00:00"     (6 AM, sin timezone)
Backend recibe:     DateTime con Kind = Unspecified
Backend compara:    DateTime.UtcNow           (Kind = UTC)
Resultado:          Comparacion INCORRECTA - mezcla fechas sin timezone con UTC
```

### Escenario Real

```csharp
// Entity con fecha
public class FollowUp : AbstractDomainObject
{
    public virtual DateTime ScheduledDate { get; set; }

    public virtual bool IsOverdue()
        => Status == FollowUpStatus.Pending && ScheduledDate.Date < DateTime.UtcNow.Date;
}

// Si ScheduledDate = "2026-01-22T06:00:00" (Kind = Unspecified)
// Y DateTime.UtcNow = "2026-01-22T12:00:00Z" (Kind = UTC)
// La comparacion falla porque mezcla diferentes "Kind"
```

---

## Recepcion de Fechas (WebApi Layer)

### Opcion 1: Usar DateTimeOffset (Recomendado)

`DateTimeOffset` captura fecha + offset, eliminando ambiguedad:

```csharp
/// <summary>
/// Data model for creating a follow-up
/// </summary>
public class CreateFollowUpModel
{
    public class Request
    {
        /// <summary>
        /// Scheduled date with timezone offset.
        /// Expected format: ISO 8601 with offset (e.g., "2026-01-22T06:00:00-06:00")
        /// </summary>
        public DateTimeOffset ScheduledDate { get; set; }
    }
}
```

**Ventajas:**
- El offset se preserva automaticamente
- System.Text.Json lo deserializa correctamente
- No hay ambiguedad sobre la zona horaria

### Opcion 2: Usar DateTime con Conversion Explicita

Si prefieres mantener `DateTime`, la conversion a UTC debe ser explicita:

```csharp
public class Request
{
    /// <summary>
    /// Scheduled date. Must include timezone offset in ISO 8601 format.
    /// </summary>
    public DateTime ScheduledDate { get; set; }
}
```

Y convertir en el mapping o use case.

---

## Conversion en AutoMapper

### Con DateTimeOffset en Request

```csharp
public class FollowUpMappingProfile : Profile
{
    public FollowUpMappingProfile()
    {
        // DateTimeOffset -> DateTime UTC
        CreateMap<CreateFollowUpModel.Request, CreateFollowUpUseCase.Command>()
            .ForMember(dest => dest.ScheduledDate,
                opt => opt.MapFrom(src => src.ScheduledDate.UtcDateTime));
    }
}
```

### Con DateTime en Request

```csharp
public class FollowUpMappingProfile : Profile
{
    public FollowUpMappingProfile()
    {
        // DateTime -> DateTime UTC
        CreateMap<CreateFollowUpModel.Request, CreateFollowUpUseCase.Command>()
            .ForMember(dest => dest.ScheduledDate,
                opt => opt.MapFrom(src => src.ScheduledDate.ToUniversalTime()));
    }
}
```

**Advertencia:** `.ToUniversalTime()` asume que el DateTime tiene `Kind = Local`. Si `Kind = Unspecified`, puede dar resultados incorrectos dependiendo del timezone del servidor.

### Patron Seguro para DateTime

Si no puedes usar `DateTimeOffset`, usa un helper:

```csharp
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to UTC safely.
    /// If Kind is Unspecified, assumes the datetime is already in UTC.
    /// </summary>
    public static DateTime ToUtcSafe(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            _ => dateTime
        };
    }
}

// Uso en mapping
.ForMember(dest => dest.ScheduledDate,
    opt => opt.MapFrom(src => src.ScheduledDate.ToUtcSafe()));
```

---

## Almacenamiento (Domain Layer)

### Entities: Siempre UTC

Las propiedades `DateTime` en entities deben almacenar valores UTC:

```csharp
public class FollowUp : AbstractDomainObject
{
    /// <summary>
    /// Scheduled date in UTC.
    /// </summary>
    public virtual DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Creation date in UTC (inherited from AbstractDomainObject).
    /// </summary>
    // public virtual DateTime CreationDate { get; set; } // Ya existe en base

    public FollowUp() { }

    public FollowUp(DateTime scheduledDateUtc, string notes)
    {
        // El parametro ya debe venir en UTC
        ScheduledDate = scheduledDateUtc;
        Notes = notes;
    }
}
```

### Convencion de Naming

Para claridad, considera nombrar parametros con sufijo `Utc`:

```csharp
public FollowUp(DateTime scheduledDateUtc, string notes)
{
    ScheduledDate = scheduledDateUtc;
}
```

---

## Comparaciones de Fechas

### Regla: SIEMPRE usar DateTime.UtcNow

```csharp
// CORRECTO
public virtual bool IsOverdue()
    => Status == FollowUpStatus.Pending && ScheduledDate.Date < DateTime.UtcNow.Date;

public virtual bool IsDueToday()
    => Status == FollowUpStatus.Pending && ScheduledDate.Date == DateTime.UtcNow.Date;

// INCORRECTO - DateTime.Now usa hora local del servidor
public virtual bool IsOverdue()
    => ScheduledDate.Date < DateTime.Now.Date; // NO usar DateTime.Now
```

### En Queries de Repositorio

```csharp
public async Task<IEnumerable<FollowUp>> GetOverdueFollowUpsAsync()
{
    var today = DateTime.UtcNow.Date;

    return await _session.Query<FollowUp>()
        .Where(f => f.Status == FollowUpStatus.Pending)
        .Where(f => f.ScheduledDate.Date < today)
        .ToListAsync();
}

public async Task<IEnumerable<FollowUp>> GetDueTodayAsync()
{
    var today = DateTime.UtcNow.Date;
    var tomorrow = today.AddDays(1);

    return await _session.Query<FollowUp>()
        .Where(f => f.Status == FollowUpStatus.Pending)
        .Where(f => f.ScheduledDate >= today && f.ScheduledDate < tomorrow)
        .ToListAsync();
}
```

---

## Respuestas al Frontend

### DTOs: Devolver UTC

Los DTOs devuelven fechas en UTC. El serializador agrega automaticamente el sufijo `Z`:

```csharp
public class FollowUpDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Scheduled date in UTC (ISO 8601 with Z suffix).
    /// </summary>
    public DateTime ScheduledDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

**Resultado JSON:**
```json
{
    "id": "...",
    "scheduledDate": "2026-01-22T12:00:00Z",
    "createdAt": "2026-01-20T15:30:00Z"
}
```

El frontend convierte a hora local para mostrar.

---

## Configuracion de JSON Serializer

### System.Text.Json (ASP.NET Core Default)

En `Program.cs`, configura el serializador para manejar fechas correctamente:

```csharp
builder.Services.AddFastEndpoints()
    .AddJsonOptions(options =>
    {
        // ISO 8601 format
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Para DateTimeOffset, el formato por defecto es correcto
        // Para DateTime, asegurar que se serialice como UTC
    });
```

### Custom Converter (Opcional)

Si necesitas forzar UTC en serialization:

```csharp
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTime = reader.GetDateTime();
        return dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : dateTime.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Siempre escribir como UTC con Z
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
```

---

## Ejemplos Completos

### Ejemplo 1: Create FollowUp (Flujo Completo)

#### Request Model (WebApi)

```csharp
public class CreateFollowUpModel
{
    public class Request
    {
        public DateTimeOffset ScheduledDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class Response
    {
        public FollowUpDto FollowUp { get; set; } = new();
    }
}
```

#### Mapping Profile (WebApi)

```csharp
public class FollowUpMappingProfile : Profile
{
    public FollowUpMappingProfile()
    {
        // Request -> Command (convierte a UTC)
        CreateMap<CreateFollowUpModel.Request, CreateFollowUpUseCase.Command>()
            .ForMember(dest => dest.ScheduledDate,
                opt => opt.MapFrom(src => src.ScheduledDate.UtcDateTime));

        // Entity -> DTO
        CreateMap<FollowUp, FollowUpDto>();

        // Entity -> Response
        CreateMap<FollowUp, CreateFollowUpModel.Response>()
            .ForMember(dest => dest.FollowUp, opt => opt.MapFrom(src => src));
    }
}
```

#### Use Case Command (Application)

```csharp
public class CreateFollowUpUseCase
{
    public class Command : ICommand<Result<FollowUp>>
    {
        /// <summary>
        /// Scheduled date in UTC.
        /// </summary>
        public DateTime ScheduledDate { get; set; }

        public string Notes { get; set; } = string.Empty;
    }

    public class Handler(IUnitOfWork uoW) : ICommandHandler<Command, Result<FollowUp>>
    {
        public async Task<Result<FollowUp>> Handle(Command command, CancellationToken ct)
        {
            _uoW.BeginTransaction();
            try
            {
                // ScheduledDate ya viene en UTC desde el mapping
                var followUp = await _uoW.FollowUps.CreateAsync(
                    command.ScheduledDate,
                    command.Notes);

                _uoW.Commit();
                return Result.Ok(followUp);
            }
            catch (Exception ex)
            {
                _uoW.Rollback();
                return Result.Fail(ex.Message);
            }
        }
    }
}
```

#### Entity (Domain)

```csharp
public class FollowUp : AbstractDomainObject
{
    public virtual DateTime ScheduledDate { get; set; }
    public virtual string Notes { get; set; } = string.Empty;
    public virtual FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;

    public FollowUp() { }

    public FollowUp(DateTime scheduledDateUtc, string notes)
    {
        ScheduledDate = scheduledDateUtc;
        Notes = notes;
    }

    public virtual bool IsOverdue()
        => Status == FollowUpStatus.Pending && ScheduledDate.Date < DateTime.UtcNow.Date;

    public virtual bool IsDueToday()
        => Status == FollowUpStatus.Pending && ScheduledDate.Date == DateTime.UtcNow.Date;

    public override IValidator GetValidator() => new FollowUpValidator();
}
```

### Ejemplo 2: Filtrar por Rango de Fechas

#### Request con Fechas de Filtro

```csharp
public class GetFollowUpsModel
{
    public class Request
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
```

#### Repository Query

```csharp
public async Task<GetManyAndCountResult<FollowUp>> GetByDateRangeAsync(
    DateTime? startDateUtc,
    DateTime? endDateUtc,
    int pageNumber,
    int pageSize)
{
    var query = _session.Query<FollowUp>();

    if (startDateUtc.HasValue)
        query = query.Where(f => f.ScheduledDate >= startDateUtc.Value);

    if (endDateUtc.HasValue)
        query = query.Where(f => f.ScheduledDate <= endDateUtc.Value);

    var count = await query.CountAsync();
    var items = await query
        .OrderBy(f => f.ScheduledDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new GetManyAndCountResult<FollowUp>(items, count, pageNumber, pageSize);
}
```

---

## Checklist de Implementacion

### Request Models (WebApi)

- [ ] Usar `DateTimeOffset` para propiedades de fecha de entrada
- [ ] Documentar con XML comments el formato esperado
- [ ] Si usas `DateTime`, documentar que debe incluir offset

### AutoMapper Profiles

- [ ] Convertir `DateTimeOffset` a `DateTime` UTC con `.UtcDateTime`
- [ ] O convertir `DateTime` a UTC con `.ToUniversalTime()` o helper seguro

### Entities (Domain)

- [ ] Propiedades `DateTime` almacenan UTC
- [ ] Constructores documentan que esperan UTC
- [ ] Comparaciones usan `DateTime.UtcNow`

### Repositories (Infrastructure)

- [ ] Queries de fecha usan `DateTime.UtcNow` como referencia
- [ ] Filtros de rango usan fechas UTC

### DTOs (WebApi)

- [ ] Fechas se serializan automaticamente con `Z` suffix
- [ ] Documentar que las fechas estan en UTC

### Validators

- [ ] Validar que fechas futuras realmente sean futuras (comparar con UTC)

---

## Anti-Patterns

### NO Hacer

| Anti-Pattern | Problema | Solucion |
|--------------|----------|----------|
| `DateTime.Now` en comparaciones | Usa hora local del servidor | Usar `DateTime.UtcNow` |
| `DateTime` sin Kind especificado | Ambiguo, depende del servidor | Usar `DateTimeOffset` o especificar Kind |
| Almacenar hora local | Inconsistente entre servidores | Almacenar siempre UTC |
| Comparar Kind diferentes | Resultados incorrectos | Normalizar todo a UTC |
| `.ToUniversalTime()` en Unspecified | Asume Local incorrectamente | Usar helper seguro o `DateTimeOffset` |
| Formatear fechas en backend | Locale-dependent | Devolver UTC, formatear en frontend |

### Ejemplos de Anti-Patterns

```csharp
// INCORRECTO - Usa hora local del servidor
public bool IsOverdue() => ScheduledDate < DateTime.Now;

// INCORRECTO - Almacena hora local
ScheduledDate = DateTime.Now;

// INCORRECTO - Compara sin normalizar
if (inputDate > existingDate) // Ambos pueden tener diferente Kind

// INCORRECTO - ToUniversalTime en Unspecified puede fallar
var utc = unspecifiedDateTime.ToUniversalTime(); // Depende del servidor
```

### Ejemplos Correctos

```csharp
// CORRECTO - Usa UTC
public bool IsOverdue() => ScheduledDate < DateTime.UtcNow;

// CORRECTO - Almacena UTC
ScheduledDate = DateTime.UtcNow;

// CORRECTO - Usa DateTimeOffset
public DateTimeOffset ScheduledDate { get; set; }
ScheduledDate.UtcDateTime // Siempre UTC

// CORRECTO - Helper seguro
var utc = inputDateTime.ToUtcSafe();
```

---

## Referencias

### Guias Relacionadas

- [Frontend Date Handling](link-to-frontend-guide) - Guia complementaria del frontend
- [Request/Response Models](../../../stacks/webapi/fastendpoints/guides/request-response-models.md)
- [DTOs](../../../architectures/clean-architecture/guides/webapi/dtos.md)
- [AutoMapper Profiles](../../../stacks/webapi/fastendpoints/guides/automapper-profiles.md)

### Documentacion Externa

- [DateTimeOffset - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset)
- [DateTime Best Practices - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)
- [ISO 8601 - Wikipedia](https://en.wikipedia.org/wiki/ISO_8601)

---

## Changelog

### Version 1.0.0 (2025-01-23)
- Initial release
- Documented UTC storage principle
- DateTimeOffset vs DateTime guidance
- AutoMapper conversion patterns
- Comparison best practices
- Complete examples
- Anti-patterns documentation
