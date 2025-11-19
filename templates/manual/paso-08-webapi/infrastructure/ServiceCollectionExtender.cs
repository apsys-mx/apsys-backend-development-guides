using FluentValidation;
using MiProyecto.domain.entities;
using MiProyecto.domain.entities.validators;
using MiProyecto.domain.interfaces.repositories;
using MiProyecto.infrastructure.nhibernate;
using MiProyecto.webapi.mappingprofiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MiProyecto.webapi.infrastructure;

public static class ServiceCollectionExtender
{
    /// <summary>
    /// Configure authorization policies
    /// </summary>
    public static IServiceCollection ConfigurePolicy(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("DefaultAuthorizationPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
            // Agregar más políticas según las necesidades del proyecto
        });
        return services;
    }

    /// <summary>
    /// Configure CORS
    /// </summary>
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
    public static IServiceCollection ConfigureUnitOfWork(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = ConnectionStringBuilder.BuildPostgresConnectionString();
        var factory = new NHSessionFactory(connectionString);
        var sessionFactory = factory.BuildNHibernateSessionFactory();
        services.AddSingleton(sessionFactory);
        services.AddScoped(factory => sessionFactory.OpenSession());
        services.AddScoped<IUnitOfWork, NHUnitOfWork>();
        return services;
    }

    /// <summary>
    /// Configure identity server authority for authorization
    /// </summary>
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
    /// Configure AutoMapper
    /// </summary>
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
            // Agregar más profiles según las entidades del proyecto
            // cfg.AddProfile(new UserMappingProfile());
        });
        return services;
    }

    /// <summary>
    /// Configure fluent validators
    /// </summary>
    public static IServiceCollection ConfigureValidators(this IServiceCollection services)
    {
        // Registrar validators para cada entidad del dominio
        // services.AddScoped<AbstractValidator<User>, UserValidator>();
        // services.AddScoped<AbstractValidator<Role>, RoleValidator>();
        return services;
    }
}
