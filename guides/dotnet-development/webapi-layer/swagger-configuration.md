# Swagger/OpenAPI Configuration with FastEndpoints

**Version:** 1.0.0
**Last Updated:** 2025-11-15
**Status:** ✅ Complete

## Table of Contents

- [Overview](#overview)
- [Why Swagger/OpenAPI?](#why-swaggeropenapi)
- [Swagger Setup in FastEndpoints](#swagger-setup-in-fastendpoints)
  - [Installation](#installation)
  - [Basic Configuration](#basic-configuration)
  - [Advanced Configuration](#advanced-configuration)
- [Documenting Endpoints](#documenting-endpoints)
  - [Description Method](#description-method)
  - [Summary Method](#summary-method)
  - [Choosing Between Description and Summary](#choosing-between-description-and-summary)
- [Endpoint Metadata](#endpoint-metadata)
  - [Tags](#tags)
  - [Operation Names](#operation-names)
  - [Response Types](#response-types)
  - [Request Examples](#request-examples)
- [Authentication and Authorization](#authentication-and-authorization)
  - [JWT Bearer Configuration](#jwt-bearer-configuration)
  - [Security Requirements](#security-requirements)
- [SwaggerUI Customization](#swaggerui-customization)
  - [UI Settings](#ui-settings)
  - [Custom CSS and JavaScript](#custom-css-and-javascript)
- [Complete Examples from Reference Project](#complete-examples-from-reference-project)
- [Best Practices](#best-practices)
- [Anti-Patterns](#anti-patterns)
- [Related Guides](#related-guides)

---

## Overview

Swagger (OpenAPI) provides interactive API documentation that allows developers to explore and test your API endpoints directly from a web interface. FastEndpoints integrates seamlessly with Swagger/OpenAPI through the `FastEndpoints.Swagger` package.

This guide demonstrates how to configure and document your API using Swagger based on the patterns used in the Hashira Stone Backend reference project.

**Key Concepts:**
- **OpenAPI Specification**: A standard format for describing REST APIs
- **Swagger UI**: A web interface for exploring and testing API endpoints
- **Endpoint Documentation**: Metadata that describes endpoint behavior, parameters, and responses
- **Schema Generation**: Automatic generation of request/response schemas from DTOs

---

## Why Swagger/OpenAPI?

### Benefits

1. **Interactive Documentation**: Developers can test endpoints directly from the browser
2. **API Discovery**: New team members can quickly understand available endpoints
3. **Client Generation**: Tools like NSwag can generate client SDKs from OpenAPI specs
4. **Contract-First Development**: OpenAPI specs can serve as API contracts
5. **Standardization**: OpenAPI is an industry standard supported by many tools

### When to Use Swagger

✅ **Use Swagger when:**
- Building public APIs that external developers will consume
- Working in teams where API documentation is critical
- Need to generate client SDKs automatically
- Want to provide interactive API testing capabilities

❌ **Consider alternatives when:**
- Building internal microservices with minimal external consumers
- Performance is critical and you want to minimize middleware overhead
- Your API is not RESTful (e.g., GraphQL, gRPC)

---

## Swagger Setup in FastEndpoints

### Installation

Add the FastEndpoints Swagger package to your project:

```bash
dotnet add package FastEndpoints.Swagger
```

**Reference Project Dependencies:**
```xml
<PackageReference Include="FastEndpoints" Version="7.0.1" />
<PackageReference Include="FastEndpoints.Swagger" Version="7.0.1" />
```

### Basic Configuration

**From Reference Project: [Program.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\Program.cs)**

The reference project configures Swagger in two places:

**1. Service Registration (Before app.Build()):**

```csharp
// Add Swagger generation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hashira Stone API",
        Version = "v1",
        Description = "API for Hashira Stone Backend",
        Contact = new OpenApiContact
        {
            Name = "APSYS Development Team",
            Email = "dev@apsys.mx"
        }
    });

    // Configure JWT Bearer authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
```

**2. Middleware Configuration (After app.Build()):**

```csharp
// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hashira Stone API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at app root
    c.DocExpansion(DocExpansion.None); // Collapse all sections by default
    c.DisplayRequestDuration(); // Show request duration
});

// Note: Order matters!
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Serializer.Options.PropertyNamingPolicy = null; // Preserve property names
});

// Generate Swagger document
app.UseSwaggerGen();
```

### Advanced Configuration

**Customizing OpenAPI Info:**

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "Comprehensive API documentation",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments (if enabled)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

**Enabling XML Documentation:**

Add to your `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
</PropertyGroup>
```

---

## Documenting Endpoints

FastEndpoints provides two primary methods for documenting endpoints: `Description()` and `Summary()`.

### Description Method

The `Description()` method provides a fluent API for adding metadata to your endpoint.

**From Reference Project: [GetUserEndpoint.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\endpoint\GetUserEndpoint.cs)**

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos.users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

/// <summary>
/// Endpoint to get a user by ID
/// </summary>
[HttpGet("users/{userId}")]
[Authorize(Policy = "MustBeApplicationUser")]
public class GetUserEndpoint(IUnitOfWork unitOfWork) : Endpoint<GetUserRequest, Results<Ok<GetUserResponse>, ProblemHttpResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override void Configure()
    {
        Get("users/{userId}");
        Policies("MustBeApplicationUser");

        Description(d => d
            .WithTags("Users")
            .WithName("GetUser")
            .WithDescription("Retrieves a user by their unique identifier")
            .Produces<GetUserResponse>(200, "application/json")
            .ProducesProblemDetails(404)
            .ProducesProblemDetails(401)
            .ProducesProblemDetails(403));
    }

    public override async Task<Results<Ok<GetUserResponse>, ProblemHttpResult>> ExecuteAsync(
        GetUserRequest req,
        CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(req.UserId);

        if (user == null)
        {
            return TypedResults.Problem(
                title: "User not found",
                statusCode: (int)HttpStatusCode.NotFound);
        }

        var response = new GetUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };

        return TypedResults.Ok(response);
    }
}
```

**Description Method Options:**

```csharp
Description(d => d
    .WithTags("Users", "Administration")           // Group endpoint under tags
    .WithName("GetUserById")                       // Unique operation ID
    .WithDescription("Detailed description here")  // Endpoint description
    .WithSummary("Short summary")                  // Brief summary
    .Accepts<GetUserRequest>("application/json")   // Request content type
    .Produces<GetUserResponse>(200)                // Success response
    .Produces<ErrorResponse>(400)                  // Error response
    .ProducesProblemDetails(404)                   // Problem details response
    .ProducesProblemDetails(401)
    .ProducesProblemDetails(403)
    .WithMetadata(new CustomMetadata())            // Custom metadata
    .ClearDefaultProduces()                        // Remove default 200 response
    .ExcludeFromDescription());                    // Hide from Swagger
```

### Summary Method

The `Summary()` method provides more detailed documentation with request/response examples.

**From Reference Project: [UpdateTechnicalStandardEndpoint.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\technicalstandards\endpoint\UpdateTechnicalStandardEndpoint.cs)**

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos.technicalstandards;
using hashira.stone.backend.webapi.features.BaseEndpoint;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace hashira.stone.backend.webapi.features.technicalstandards.endpoint;

/// <summary>
/// Endpoint to update a technical standard
/// </summary>
[HttpPut("technical-standards/{id}")]
public class UpdateTechnicalStandardEndpoint(IUnitOfWork unitOfWork)
    : BaseEndpoint<UpdateTechnicalStandardRequest, Results<Ok<UpdateTechnicalStandardResponse>, ProblemHttpResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override void Configure()
    {
        Put("technical-standards/{id}");
        Policies("MustBeApplicationAdministrator");

        Summary(s =>
        {
            s.Summary = "Update a technical standard";
            s.Description = "Updates an existing technical standard with new information. Only administrators can perform this operation.";
            s.RequestParam(r => r.Id, "The unique identifier of the technical standard to update");
            s.Response<UpdateTechnicalStandardResponse>(200, "Technical standard updated successfully");
            s.Response(400, "Invalid request data");
            s.Response(401, "Unauthorized - Missing or invalid authentication token");
            s.Response(403, "Forbidden - User does not have administrator privileges");
            s.Response(404, "Technical standard not found");
            s.Response(409, "Conflict - A technical standard with this name already exists");

            s.ExampleRequest = new UpdateTechnicalStandardRequest
            {
                Id = 1,
                Name = "NOM-001-SEDE-2012",
                Description = "Electrical installations (utilization)",
                Category = "Electrical",
                Version = "2.0",
                IsActive = true
            };
        });
    }

    public override async Task<Results<Ok<UpdateTechnicalStandardResponse>, ProblemHttpResult>> ExecuteAsync(
        UpdateTechnicalStandardRequest req,
        CancellationToken ct)
    {
        // Get existing technical standard
        var existingStandard = await _unitOfWork.TechnicalStandards.GetByIdAsync(req.Id);
        if (existingStandard == null)
        {
            return await HandleErrorWithMessageAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                "Technical standard not found",
                HttpStatusCode.NotFound,
                ct);
        }

        // Check for duplicates (excluding current record)
        var duplicateCheck = await _unitOfWork.TechnicalStandards
            .FindAsync(ts => ts.Name == req.Name && ts.Id != req.Id);

        if (duplicateCheck.Any())
        {
            return await HandleErrorAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                r => r.Name,
                "A technical standard with this name already exists",
                HttpStatusCode.Conflict,
                ct);
        }

        // Update entity
        var updateResult = existingStandard.Update(
            req.Name,
            req.Description,
            req.Category,
            req.Version,
            req.IsActive);

        if (updateResult.IsFailed)
        {
            return await HandleErrorAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                updateResult,
                HttpStatusCode.BadRequest,
                ct);
        }

        // Save changes
        _unitOfWork.TechnicalStandards.Update(existingStandard);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = new UpdateTechnicalStandardResponse
        {
            Id = existingStandard.Id,
            Name = existingStandard.Name,
            Description = existingStandard.Description,
            Category = existingStandard.Category,
            Version = existingStandard.Version,
            IsActive = existingStandard.IsActive,
            UpdatedAt = existingStandard.UpdatedAt
        };

        return TypedResults.Ok(response);
    }
}
```

**Summary Method Options:**

```csharp
Summary(s =>
{
    // Basic information
    s.Summary = "Short endpoint summary";
    s.Description = "Detailed description of what this endpoint does";

    // Parameter documentation
    s.RequestParam(r => r.UserId, "The unique user identifier");
    s.RequestParam(r => r.IncludeRoles, "Include user roles in response");

    // Response documentation
    s.Response<SuccessResponse>(200, "Success description");
    s.Response(400, "Bad request description");
    s.Response(401, "Unauthorized description");
    s.Response(403, "Forbidden description");
    s.Response(404, "Not found description");
    s.Response(500, "Internal server error");

    // Request examples
    s.ExampleRequest = new CreateUserRequest
    {
        Email = "user@example.com",
        Name = "John Doe",
        Roles = new[] { "User" }
    };

    // Response examples (optional)
    s.Responses[200] = new ResponseExample
    {
        Example = new CreateUserResponse
        {
            Id = 123,
            Email = "user@example.com",
            Name = "John Doe"
        }
    };
});
```

### Choosing Between Description and Summary

**Use `Description()` when:**
- You need simple, quick documentation
- Metadata is straightforward (tags, response types)
- You don't need request/response examples
- The endpoint is self-explanatory

**Use `Summary()` when:**
- You need detailed parameter documentation
- Request/response examples are important
- You want to document all possible status codes
- The endpoint has complex behavior that needs explanation

**Mixed Approach (Not Recommended):**

Avoid using both methods on the same endpoint as it can lead to confusion:

```csharp
// ❌ Anti-pattern: Using both
public override void Configure()
{
    Get("users/{id}");
    Description(d => d.WithTags("Users"));
    Summary(s => s.Summary = "Get user"); // Confusing!
}

// ✅ Better: Choose one
public override void Configure()
{
    Get("users/{id}");
    Summary(s =>
    {
        s.Summary = "Get user by ID";
        s.Description = "Retrieves a user's information";
        // ... more details
    });
}
```

---

## Endpoint Metadata

### Tags

Tags group related endpoints in Swagger UI:

```csharp
Description(d => d.WithTags("Users", "Administration"));
```

**Reference Project Tag Examples:**
- `Users` - User management endpoints
- `TechnicalStandards` - Technical standards CRUD
- `Prototypes` - Prototype management
- `Authentication` - Login/logout endpoints

**Best Practices:**
- Use consistent tag naming across endpoints
- Group by feature/domain, not by HTTP method
- Keep tag names clear and business-focused

### Operation Names

Operation names must be unique across all endpoints:

```csharp
Description(d => d.WithName("GetUserById"));
```

**Naming Conventions:**

```csharp
// ✅ Good: Clear, unique names
.WithName("GetUserById")
.WithName("CreateUser")
.WithName("UpdateUserEmail")
.WithName("DeleteUser")
.WithName("ListUsers")

// ❌ Bad: Generic or duplicate names
.WithName("Get")       // Too generic
.WithName("GetUser")   // Might conflict with GetUser(email)
.WithName("Update")    // Too generic
```

### Response Types

Document all possible response types:

**Using `Produces<T>()`:**

```csharp
Description(d => d
    .Produces<GetUserResponse>(200, "application/json")
    .Produces<ErrorResponse>(400, "application/json")
    .Produces(404)
    .Produces(401)
    .Produces(403));
```

**Using `ProducesProblemDetails()`:**

```csharp
Description(d => d
    .Produces<GetUserResponse>(200)
    .ProducesProblemDetails(400)
    .ProducesProblemDetails(401)
    .ProducesProblemDetails(403)
    .ProducesProblemDetails(404)
    .ProducesProblemDetails(500));
```

**Using `Summary()` Response Documentation:**

```csharp
Summary(s =>
{
    s.Response<UserResponse>(200, "User retrieved successfully");
    s.Response(400, "Invalid user ID format");
    s.Response(401, "Authentication required");
    s.Response(403, "Insufficient permissions");
    s.Response(404, "User not found");
    s.Response(500, "Internal server error");
});
```

### Request Examples

Provide clear examples to help API consumers:

```csharp
Summary(s =>
{
    s.ExampleRequest = new CreateUserRequest
    {
        Email = "john.doe@example.com",
        Name = "John Doe",
        PhoneNumber = "+1234567890",
        Roles = new[] { "User", "Viewer" },
        Department = "Engineering",
        IsActive = true
    };
});
```

**Multiple Examples (Advanced):**

```csharp
Summary(s =>
{
    s.Params["userId"] = "Example: 123";
    s.Params["includeDeleted"] = "Set to true to include deleted users";

    s.ExampleRequest = new UpdateUserRequest
    {
        Name = "Jane Smith",
        Email = "jane.smith@example.com"
    };

    // Alternate example
    s.Responses[200] = new
    {
        Example = new UserResponse
        {
            Id = 123,
            Name = "Jane Smith",
            Email = "jane.smith@example.com",
            CreatedAt = DateTime.UtcNow
        }
    };
});
```

---

## Authentication and Authorization

### JWT Bearer Configuration

**From Reference Project: [Program.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\Program.cs)**

Configure JWT Bearer authentication in Swagger:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Add security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Add security requirement (applies to all endpoints)
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

This configuration:
1. Adds a "Bearer" security definition
2. Shows "Authorize" button in Swagger UI
3. Automatically includes `Authorization` header in requests
4. Documents that endpoints require JWT authentication

### Security Requirements

**Per-Endpoint Security (Advanced):**

If you need different security requirements per endpoint:

```csharp
// Remove global security requirement
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // ... configuration
    });

    // Don't add global security requirement
});

// Add security per endpoint
public override void Configure()
{
    Get("users/{id}");
    Policies("MustBeApplicationUser");

    Description(d => d
        .WithMetadata(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new[] { "read:users" }
            }
        }));
}
```

**Anonymous Endpoints:**

For endpoints that don't require authentication:

```csharp
public override void Configure()
{
    Post("auth/login");
    AllowAnonymous(); // No authentication required

    Description(d => d
        .WithTags("Authentication")
        .WithDescription("Authenticates a user and returns a JWT token"));
}
```

---

## SwaggerUI Customization

### UI Settings

**From Reference Project: [Program.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\Program.cs)**

```csharp
app.UseSwaggerUI(c =>
{
    // Swagger endpoint
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hashira Stone API v1");

    // Serve at root (https://localhost:5001/)
    c.RoutePrefix = string.Empty;

    // Collapse all sections by default
    c.DocExpansion(DocExpansion.None);

    // Show request duration in UI
    c.DisplayRequestDuration();
});
```

**Additional UI Options:**

```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");

    // UI customization
    c.RoutePrefix = "api-docs";                    // Serve at /api-docs
    c.DocumentTitle = "My API Documentation";      // Browser tab title
    c.DocExpansion(DocExpansion.List);            // Show endpoints, hide details
    c.DefaultModelsExpandDepth(-1);               // Hide schema section
    c.DisplayOperationId();                       // Show operation IDs
    c.DisplayRequestDuration();                   // Show request timing
    c.EnableDeepLinking();                        // Enable URL deep linking
    c.EnableFilter();                             // Enable filter box
    c.ShowExtensions();                           // Show vendor extensions
    c.EnableValidator();                          // Enable spec validator

    // Try it out
    c.EnableTryItOutByDefault();                  // Enable "Try it out" by default

    // Authentication
    c.PersistAuthorization(true);                 // Remember auth token
});
```

### Custom CSS and JavaScript

**Adding Custom Styling:**

```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");

    // Inject custom CSS
    c.InjectStylesheet("/swagger-ui/custom.css");

    // Inject custom JavaScript
    c.InjectJavascript("/swagger-ui/custom.js");
});
```

**Serve Custom Files:**

```csharp
// In Program.cs
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "SwaggerUI")),
    RequestPath = "/swagger-ui"
});
```

**Example Custom CSS (SwaggerUI/custom.css):**

```css
/* Custom branding */
.swagger-ui .topbar {
    background-color: #1e3a8a;
}

.swagger-ui .topbar .download-url-wrapper {
    display: none;
}

/* Custom colors */
.swagger-ui .opblock.opblock-post {
    background-color: #dcfce7;
    border-color: #22c55e;
}

.swagger-ui .opblock.opblock-get {
    background-color: #dbeafe;
    border-color: #3b82f6;
}
```

---

## Complete Examples from Reference Project

### Example 1: Simple GET Endpoint with Description

**[GetUserEndpoint.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\users\endpoint\GetUserEndpoint.cs)**

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos.users;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace hashira.stone.backend.webapi.features.users.endpoint;

[HttpGet("users/{userId}")]
public class GetUserEndpoint(IUnitOfWork unitOfWork)
    : Endpoint<GetUserRequest, Results<Ok<GetUserResponse>, ProblemHttpResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override void Configure()
    {
        Get("users/{userId}");
        Policies("MustBeApplicationUser");

        Description(d => d
            .WithTags("Users")
            .WithName("GetUser")
            .WithDescription("Retrieves a user by their unique identifier")
            .Produces<GetUserResponse>(200, "application/json")
            .ProducesProblemDetails(404)
            .ProducesProblemDetails(401)
            .ProducesProblemDetails(403));
    }

    public override async Task<Results<Ok<GetUserResponse>, ProblemHttpResult>> ExecuteAsync(
        GetUserRequest req,
        CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(req.UserId);

        if (user == null)
        {
            return TypedResults.Problem(
                title: "User not found",
                statusCode: (int)HttpStatusCode.NotFound);
        }

        var response = new GetUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Roles = user.Roles.Select(r => r.Name).ToList()
        };

        return TypedResults.Ok(response);
    }
}
```

**Swagger Output:**
- **Tag**: Users
- **Operation**: GET /api/users/{userId}
- **Operation ID**: GetUser
- **Description**: Retrieves a user by their unique identifier
- **Responses**: 200 (GetUserResponse), 401, 403, 404

### Example 2: Complex PUT Endpoint with Summary

**[UpdateTechnicalStandardEndpoint.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\technicalstandards\endpoint\UpdateTechnicalStandardEndpoint.cs)**

```csharp
using FastEndpoints;
using FluentResults;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos.technicalstandards;
using hashira.stone.backend.webapi.features.BaseEndpoint;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace hashira.stone.backend.webapi.features.technicalstandards.endpoint;

[HttpPut("technical-standards/{id}")]
public class UpdateTechnicalStandardEndpoint(IUnitOfWork unitOfWork)
    : BaseEndpoint<UpdateTechnicalStandardRequest, Results<Ok<UpdateTechnicalStandardResponse>, ProblemHttpResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override void Configure()
    {
        Put("technical-standards/{id}");
        Policies("MustBeApplicationAdministrator");

        Summary(s =>
        {
            s.Summary = "Update a technical standard";
            s.Description = "Updates an existing technical standard with new information. " +
                          "Only administrators can perform this operation.";

            s.RequestParam(r => r.Id, "The unique identifier of the technical standard to update");

            s.Response<UpdateTechnicalStandardResponse>(200, "Technical standard updated successfully");
            s.Response(400, "Invalid request data or validation failure");
            s.Response(401, "Unauthorized - Missing or invalid authentication token");
            s.Response(403, "Forbidden - User does not have administrator privileges");
            s.Response(404, "Technical standard not found");
            s.Response(409, "Conflict - A technical standard with this name already exists");

            s.ExampleRequest = new UpdateTechnicalStandardRequest
            {
                Id = 1,
                Name = "NOM-001-SEDE-2012",
                Description = "Electrical installations (utilization)",
                Category = "Electrical",
                Version = "2.0",
                IsActive = true
            };
        });
    }

    public override async Task<Results<Ok<UpdateTechnicalStandardResponse>, ProblemHttpResult>> ExecuteAsync(
        UpdateTechnicalStandardRequest req,
        CancellationToken ct)
    {
        var existingStandard = await _unitOfWork.TechnicalStandards.GetByIdAsync(req.Id);
        if (existingStandard == null)
        {
            return await HandleErrorWithMessageAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                "Technical standard not found",
                HttpStatusCode.NotFound,
                ct);
        }

        var duplicateCheck = await _unitOfWork.TechnicalStandards
            .FindAsync(ts => ts.Name == req.Name && ts.Id != req.Id);

        if (duplicateCheck.Any())
        {
            return await HandleErrorAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                r => r.Name,
                "A technical standard with this name already exists",
                HttpStatusCode.Conflict,
                ct);
        }

        var updateResult = existingStandard.Update(
            req.Name,
            req.Description,
            req.Category,
            req.Version,
            req.IsActive);

        if (updateResult.IsFailed)
        {
            return await HandleErrorAsync<UpdateTechnicalStandardRequest, UpdateTechnicalStandardResponse>(
                updateResult,
                HttpStatusCode.BadRequest,
                ct);
        }

        _unitOfWork.TechnicalStandards.Update(existingStandard);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = new UpdateTechnicalStandardResponse
        {
            Id = existingStandard.Id,
            Name = existingStandard.Name,
            Description = existingStandard.Description,
            Category = existingStandard.Category,
            Version = existingStandard.Version,
            IsActive = existingStandard.IsActive,
            UpdatedAt = existingStandard.UpdatedAt
        };

        return TypedResults.Ok(response);
    }
}
```

**Swagger Output:**
- **Tag**: (Inferred from route)
- **Operation**: PUT /api/technical-standards/{id}
- **Summary**: Update a technical standard
- **Description**: Updates an existing technical standard with new information. Only administrators can perform this operation.
- **Parameters**: id (path parameter with description)
- **Request Body**: UpdateTechnicalStandardRequest with example
- **Responses**: 200, 400, 401, 403, 404, 409 (all documented)

### Example 3: POST Endpoint with Detailed Documentation

**[CreatePrototypeEndpoint.cs](D:\apsys-mx\inspeccion-distancia\hashira.stone.backend\src\hashira.stone.backend.webapi\features\prototypes\endpoint\CreatePrototypeEndpoint.cs)**

```csharp
using FastEndpoints;
using hashira.stone.backend.domain.entities;
using hashira.stone.backend.domain.interfaces.repositories;
using hashira.stone.backend.webapi.dtos.prototypes;
using hashira.stone.backend.webapi.features.BaseEndpoint;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net;

namespace hashira.stone.backend.webapi.features.prototypes.endpoint;

[HttpPost("prototypes")]
public class CreatePrototypeEndpoint(IUnitOfWork unitOfWork)
    : BaseEndpoint<CreatePrototypeRequest, Results<Created<CreatePrototypeResponse>, ProblemHttpResult>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override void Configure()
    {
        Post("prototypes");
        Policies("MustBeApplicationUser");

        Description(d => d
            .WithTags("Prototypes")
            .WithName("CreatePrototype")
            .WithDescription("Creates a new prototype in the system")
            .Produces<CreatePrototypeResponse>(201, "application/json")
            .ProducesProblemDetails(400)
            .ProducesProblemDetails(401)
            .ProducesProblemDetails(403)
            .ProducesProblemDetails(409));
    }

    public override async Task<Results<Created<CreatePrototypeResponse>, ProblemHttpResult>> ExecuteAsync(
        CreatePrototypeRequest req,
        CancellationToken ct)
    {
        // Check for duplicates
        var existingPrototype = await _unitOfWork.Prototypes
            .FindAsync(p => p.Name == req.Name);

        if (existingPrototype.Any())
        {
            return await HandleErrorAsync<CreatePrototypeRequest, CreatePrototypeResponse>(
                r => r.Name,
                "A prototype with this name already exists",
                HttpStatusCode.Conflict,
                ct);
        }

        // Create entity
        var createResult = Prototype.Create(
            req.Name,
            req.Description,
            req.Version,
            req.Status);

        if (createResult.IsFailed)
        {
            return await HandleErrorAsync<CreatePrototypeRequest, CreatePrototypeResponse>(
                createResult,
                HttpStatusCode.BadRequest,
                ct);
        }

        var prototype = createResult.Value;
        await _unitOfWork.Prototypes.AddAsync(prototype);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = new CreatePrototypeResponse
        {
            Id = prototype.Id,
            Name = prototype.Name,
            Description = prototype.Description,
            Version = prototype.Version,
            Status = prototype.Status,
            CreatedAt = prototype.CreatedAt
        };

        return TypedResults.Created($"/api/prototypes/{prototype.Id}", response);
    }
}
```

**Swagger Output:**
- **Tag**: Prototypes
- **Operation**: POST /api/prototypes
- **Operation ID**: CreatePrototype
- **Description**: Creates a new prototype in the system
- **Request Body**: CreatePrototypeRequest
- **Responses**: 201 (Created with CreatePrototypeResponse), 400, 401, 403, 409

---

## Best Practices

### 1. Consistent Documentation

✅ **Document all endpoints consistently:**

```csharp
// Use the same pattern across all endpoints in a feature
public override void Configure()
{
    Get("users/{id}");
    Policies("MustBeApplicationUser");

    Description(d => d
        .WithTags("Users")              // Always use tags
        .WithName("GetUser")            // Always provide operation name
        .WithDescription("...")         // Always describe the endpoint
        .Produces<UserResponse>(200)    // Document success response
        .ProducesProblemDetails(404)    // Document error responses
        .ProducesProblemDetails(401)
        .ProducesProblemDetails(403));
}
```

### 2. Document All Response Codes

✅ **Document every HTTP status code your endpoint can return:**

```csharp
Summary(s =>
{
    s.Response<UserResponse>(200, "User retrieved successfully");
    s.Response(400, "Invalid user ID format");
    s.Response(401, "Authentication required");
    s.Response(403, "Insufficient permissions to view user");
    s.Response(404, "User not found");
    s.Response(500, "Internal server error occurred");
});
```

### 3. Provide Request Examples

✅ **Include realistic examples:**

```csharp
Summary(s =>
{
    s.ExampleRequest = new CreateUserRequest
    {
        Email = "john.doe@company.com",    // Use realistic data
        Name = "John Doe",
        PhoneNumber = "+1-555-123-4567",
        Department = "Engineering",
        Roles = new[] { "User", "Developer" }
    };
});
```

### 4. Use Meaningful Tags

✅ **Group related endpoints:**

```csharp
// Group by domain/feature
Description(d => d.WithTags("Users"));           // User management
Description(d => d.WithTags("Authentication"));  // Auth endpoints
Description(d => d.WithTags("Prototypes"));      // Prototype management

// Multiple tags for cross-cutting concerns
Description(d => d.WithTags("Users", "Administration"));
```

### 5. Keep Operation Names Unique

✅ **Use unique, descriptive operation names:**

```csharp
.WithName("GetUserById")           // Specific
.WithName("GetUserByEmail")        // Distinguishes from above
.WithName("ListUsers")             // Clear intent
.WithName("CreateUser")            // Action-based
.WithName("UpdateUserProfile")     // Specific update operation
```

### 6. Document Security Requirements

✅ **Clearly indicate authentication/authorization:**

```csharp
Summary(s =>
{
    s.Summary = "Update user profile";
    s.Description = "Updates the authenticated user's profile information. " +
                   "Requires valid JWT token and 'User' role.";
    // ...
});
```

### 7. Use XML Comments

✅ **Add XML documentation to DTOs:**

```csharp
/// <summary>
/// Request to create a new user
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// User's email address (must be unique)
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = string.Empty;
}
```

**Enable in `.csproj`:**

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

**Include in Swagger:**

```csharp
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});
```

### 8. Version Your API

✅ **Include version in OpenAPI document:**

```csharp
options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "My API",
    Version = "v1",
    Description = "Version 1 of My API"
});
```

### 9. Organize Tags Hierarchically

✅ **Use consistent tag naming:**

```csharp
// Feature-based
"Users"
"Prototypes"
"TechnicalStandards"

// With sub-categories (if needed)
"Users - Management"
"Users - Authentication"
"Users - Profiles"
```

### 10. Keep Swagger UI Responsive

✅ **Optimize for performance:**

```csharp
app.UseSwaggerUI(c =>
{
    c.DocExpansion(DocExpansion.None);     // Collapse by default
    c.DefaultModelsExpandDepth(-1);        // Hide schemas
    c.EnableFilter();                      // Enable search
});
```

---

## Anti-Patterns

### ❌ 1. No Documentation

**Bad:**
```csharp
public override void Configure()
{
    Get("users/{id}");
    // No documentation!
}
```

**Good:**
```csharp
public override void Configure()
{
    Get("users/{id}");
    Description(d => d
        .WithTags("Users")
        .WithName("GetUser")
        .WithDescription("Retrieves a user by ID")
        .Produces<UserResponse>(200)
        .ProducesProblemDetails(404));
}
```

### ❌ 2. Incomplete Response Documentation

**Bad:**
```csharp
Description(d => d.Produces<UserResponse>(200));
// Missing error responses!
```

**Good:**
```csharp
Description(d => d
    .Produces<UserResponse>(200)
    .ProducesProblemDetails(400)
    .ProducesProblemDetails(401)
    .ProducesProblemDetails(403)
    .ProducesProblemDetails(404));
```

### ❌ 3. Generic Operation Names

**Bad:**
```csharp
.WithName("Get")        // Too generic
.WithName("GetData")    // Vague
.WithName("Endpoint1")  // Meaningless
```

**Good:**
```csharp
.WithName("GetUserById")
.WithName("ListActiveUsers")
.WithName("UpdateUserEmail")
```

### ❌ 4. Missing Request Examples

**Bad:**
```csharp
Summary(s =>
{
    s.Summary = "Create user";
    // No example - users don't know expected format
});
```

**Good:**
```csharp
Summary(s =>
{
    s.Summary = "Create user";
    s.ExampleRequest = new CreateUserRequest
    {
        Email = "user@example.com",
        Name = "John Doe"
    };
});
```

### ❌ 5. Inconsistent Tag Usage

**Bad:**
```csharp
// Inconsistent naming
Description(d => d.WithTags("user"));         // lowercase
Description(d => d.WithTags("Users"));        // capitalized
Description(d => d.WithTags("User Management")); // different name
```

**Good:**
```csharp
// Consistent naming
Description(d => d.WithTags("Users"));
Description(d => d.WithTags("Users"));
Description(d => d.WithTags("Users"));
```

### ❌ 6. Exposing Swagger in Production

**Bad:**
```csharp
// Always enabled
app.UseSwagger();
app.UseSwaggerUI();
```

**Good:**
```csharp
// Only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Alternative (Controlled Access):**
```csharp
// Require authentication for Swagger in production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    if (!app.Environment.IsDevelopment())
    {
        c.UseRequestInterceptor("(request) => { " +
            "request.headers['Authorization'] = 'Bearer ' + getToken(); " +
            "return request; }");
    }
});
```

### ❌ 7. Not Documenting Authentication

**Bad:**
```csharp
builder.Services.AddSwaggerGen();
// No security definition!
```

**Good:**
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

### ❌ 8. Vague Descriptions

**Bad:**
```csharp
Summary(s =>
{
    s.Summary = "Gets data";
    s.Description = "This endpoint gets data";
});
```

**Good:**
```csharp
Summary(s =>
{
    s.Summary = "Retrieve user profile";
    s.Description = "Retrieves detailed profile information for a user, " +
                   "including personal details, roles, and department assignment. " +
                   "Requires authentication and appropriate permissions.";
});
```

### ❌ 9. Using Both Description and Summary

**Bad:**
```csharp
public override void Configure()
{
    Get("users/{id}");
    Description(d => d.WithTags("Users"));
    Summary(s => s.Summary = "Get user"); // Confusing - which takes precedence?
}
```

**Good:**
```csharp
public override void Configure()
{
    Get("users/{id}");
    Summary(s =>
    {
        s.Summary = "Get user by ID";
        s.Description = "Retrieves user information";
        // All documentation in one place
    });
}
```

### ❌ 10. Not Documenting Parameters

**Bad:**
```csharp
Summary(s =>
{
    s.Summary = "Update user";
    // No parameter documentation
});
```

**Good:**
```csharp
Summary(s =>
{
    s.Summary = "Update user";
    s.RequestParam(r => r.UserId, "The unique identifier of the user to update");
    s.RequestParam(r => r.NotifyUser, "Send email notification to user (default: true)");
});
```

---

## Related Guides

### WebApi Layer
- **[FastEndpoints Basics](fastendpoints-basics.md)** - Core FastEndpoints concepts and setup
- **[Request/Response Models](request-response-models.md)** - TypedResults and endpoint contracts
- **[Error Responses](error-responses.md)** - Error handling and ProblemDetails
- **[Authentication](authentication.md)** - JWT authentication and authorization

### Infrastructure Layer
- **Dependency Injection** - Registering services for endpoints
- **Middleware** - Custom middleware integration

### Testing
- **Integration Testing** - Testing Swagger generation
- **API Testing** - Using Swagger for manual testing

---

## Summary

This guide covered Swagger/OpenAPI documentation in FastEndpoints:

1. **Setup**: Installing FastEndpoints.Swagger and configuring middleware
2. **Documentation Methods**: Using `Description()` and `Summary()` for endpoint metadata
3. **Endpoint Metadata**: Tags, operation names, response types, and examples
4. **Authentication**: Documenting JWT Bearer authentication in Swagger UI
5. **Customization**: SwaggerUI settings and custom styling
6. **Best Practices**: Consistent documentation, complete response codes, meaningful examples
7. **Anti-Patterns**: Avoiding common mistakes in API documentation

**Key Takeaways:**
- Choose `Description()` for simple documentation, `Summary()` for detailed
- Document all possible response codes
- Provide realistic request/response examples
- Use consistent tag naming and organization
- Configure authentication properly in Swagger
- Only expose Swagger in development or with proper security

**Next Steps:**
- Review the [FastEndpoints Basics](fastendpoints-basics.md) guide for endpoint fundamentals
- Study [Error Responses](error-responses.md) for proper error documentation
- Explore [Authentication](authentication.md) for securing documented endpoints
- Practice writing comprehensive API documentation for your endpoints

---

**Version History:**
- **1.0.0** (2025-11-15): Initial version with complete Swagger configuration examples
