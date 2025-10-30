using FastEndpoints;
using System.Reflection;
using FluentValidation;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.infrastructure.nhibernate;
using {ProjectName}.webapi.infrastructure.authorization;
using {ProjectName}.webapi.mappingprofiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace {ProjectName}.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure authorization policies
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection ConfigurePolicy(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("DefaultAuthorizationPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
            options.AddPolicy("MustBeApplicationUser", policy =>
            {
                policy.AddRequirements(new MustBeApplicationUser.Requirement());
            });
        });
        services.AddScoped<IAuthorizationHandler, MustBeApplicationUser.Handler>();
        return services;
    }

    /// <summary>
    /// Configure CORS
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedCorsOrigins = GetAllowedCorsOrigins(configuration);
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                builder => builder
                .WithOrigins(allowedCorsOrigins)
                .SetIsOriginAllowed((host) => true)
                .AllowAnyMethod()
                .AllowAnyHeader());
        });
        return services;
    }
    private static string[] GetAllowedCorsOrigins(IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedHosts").Value;
        if (string.IsNullOrEmpty(allowedOrigins))
            throw new ArgumentException("No CORS configuration found in the configuration file");
        return allowedOrigins.Split(",");
    }
    /// <summary>
    /// Configure the unit of work dependency injection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection ConfigureUnitOfWork(this IServiceCollection services, IConfiguration configuration)
    {
        //TODO: To be implemented when database is configured with configure-database tool
        // Example implementation:
        // services.AddScoped<ISession>((serviceProvider) =>
        // {
        //     var sessionFactory = serviceProvider.GetRequiredService<ISessionFactory>();
        //     return sessionFactory.OpenSession();
        // });
        // services.AddScoped<IUnitOfWork, NHUnitOfWork>();
        return services;
    }

    /// <summary>
    /// Configure identity server authority for authorization
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection ConfigureIdentityServerClient(this IServiceCollection services, IConfiguration configuration)
    {
        string? identityServerUrl = configuration.GetSection("IdentityServerConfiguration:Address").Value;
        if (string.IsNullOrEmpty(identityServerUrl))
            throw new InvalidOperationException($"No identityServer configuration found in the configuration file");

        services.AddAuthentication("Bearer")
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = identityServerUrl;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });
        return services;
    }

    /// <summary>
    /// Configures dependency injections for services based on the environment.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="environment">The web host environment information.</param>
    /// <remarks>
    /// Add your project-specific service registrations here.
    /// Example: File handling services, external API clients, etc.
    /// </remarks>
    public static IServiceCollection ConfigureDependencyInjections(this IServiceCollection services, IWebHostEnvironment environment)
    {
        // TODO: Add project-specific dependency injections
        // Example:
        // if (environment.IsDevelopment())
        // {
        //     services.AddScoped<IFileService, LocalFileService>();
        // }
        // else
        // {
        //     services.AddScoped<IFileService, CloudFileService>();
        // }
        return services;
    }

    /// <summary>
    /// Configure AutoMapper
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        });
        return services;
    }

    /// <summary>
    /// Configure fluent validators
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureValidators(this IServiceCollection services)
    {
        // TODO: Register validators if needed
        // Example:
        // services.AddValidatorsFromAssemblyContaining<YourValidator>();
        return services;
    }


    /// <summary>
    /// Automatically registers all Commands and Handlers from a given assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly containing Commands and Handlers</param>
    public static IServiceProvider RegisterCommandsFromAssembly(this IServiceProvider services, Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsNested)
            .Where(t => t.Name == "Command")
            .ToList();

        foreach (var commandType in commandTypes)
        {
            var parentType = commandType.DeclaringType;
            if (parentType == null) continue;

            var handlerType = parentType.GetNestedType("Handler");
            if (handlerType == null) continue;

            // Register the Command and its Handler
            services.RegisterGenericCommand(commandType, handlerType);
        }
        return services;
    }
}
