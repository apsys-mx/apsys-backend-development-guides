using System.Configuration;

namespace MiProyecto.infrastructure.nhibernate;

/// <summary>
/// Utility class to build the connection string for the database.
/// This class retrieves the necessary database connection parameters from environment variables.
/// </summary>
public static class ConnectionStringBuilder
{
    /// <summary>
    /// Builds the connection string for the database using environment variables.
    /// The required environment variables are:
    /// - SQL_SERVER: The database server address.
    /// - SQL_SERVER_DATABASE: The name of the database.
    /// - SQ_SERVER_USER: The username for the database.
    /// - SQL_SERVER_SA_PASSWORD: The password for the database user.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ConfigurationErrorsException"></exception>
    public static string BuildSqlServerConnectionString()
    {
        var requiredVars = new[] { "SQL_SERVER_NAME", "SQL_SERVER_PORT", "SQL_SERVER_DATABASE", "SQ_SERVER_USER", "SQL_SERVER_SA_PASSWORD" };
        var missingVars = requiredVars.Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var))).ToList();
        if (missingVars.Any())
        {
            throw new ConfigurationErrorsException(
                $"Missing required environment variables: {string.Join(", ", missingVars)}");
        }

        return $"Server={Environment.GetEnvironmentVariable("SQL_SERVER_NAME")},{Environment.GetEnvironmentVariable("SQL_SERVER_PORT")};" +
                                      $"Database={Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE")};" +
                                      $"User Id={Environment.GetEnvironmentVariable("SQ_SERVER_USER")};" +
                                      $"Password={Environment.GetEnvironmentVariable("SQL_SERVER_SA_PASSWORD")};" +
                                      "TrustServerCertificate=True";
    }

    /// <summary>
    /// Builds the connection string for a PostgreSQL database using environment variables.
    /// The required environment variables are:
    /// - POSTGRES_HOST: The database host address.
    /// - POSTGRES_PORT: The port number for the database.
    /// - POSTGRES_DATABASE: The name of the database.
    /// - POSTGRES_USER: The username for the database.
    /// - POSTGRES_PASSWORD: The password for the database user.
    /// </summary>
    /// <exception cref="ConfigurationErrorsException"></exception>
    public static string BuildPostgresConnectionString()
    {
        var requiredVars = new[] { "POSTGRES_HOST", "POSTGRES_PORT", "POSTGRES_DATABASE", "POSTGRES_USER", "POSTGRES_PASSWORD" };
        var missingVars = requiredVars.Where(var => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var))).ToList();
        if (missingVars.Any())
        {
            throw new ConfigurationErrorsException(
                $"Missing required environment variables: {string.Join(", ", missingVars)}");
        }
        return $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST")};" +
            $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT")};" +
            $"Database={Environment.GetEnvironmentVariable("POSTGRES_DATABASE")};" +
            $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")};" +
            $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")};";
    }
}
