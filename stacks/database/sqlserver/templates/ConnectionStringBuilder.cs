using System.Configuration;

namespace {ProjectName}.infrastructure.nhibernate;

/// <summary>
/// Builds the connection string for SQL Server database using environment variables.
/// </summary>
public static class ConnectionStringBuilder
{
    /// <summary>
    /// Required environment variables:
    /// - DB_HOST: The database server address (e.g., "localhost")
    /// - DB_PORT: The port number (e.g., "1433")
    /// - DB_NAME: The database name
    /// - DB_USER: The username (e.g., "sa")
    /// - DB_PASSWORD: The password
    /// </summary>
    /// <returns>SQL Server connection string</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when required variables are missing</exception>
    public static string Build()
    {
        var requiredVars = new[] { "DB_HOST", "DB_PORT", "DB_NAME", "DB_USER", "DB_PASSWORD" };
        var missingVars = requiredVars
            .Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)))
            .ToList();

        if (missingVars.Any())
        {
            throw new ConfigurationErrorsException(
                $"Missing required environment variables: {string.Join(", ", missingVars)}");
        }

        return $"Server={Environment.GetEnvironmentVariable("DB_HOST")},{Environment.GetEnvironmentVariable("DB_PORT")};" +
               $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
               $"User Id={Environment.GetEnvironmentVariable("DB_USER")};" +
               $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
               "TrustServerCertificate=True";
    }
}
