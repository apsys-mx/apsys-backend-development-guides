using {ProjectName}.ndbunit;
using Npgsql;

namespace {ProjectName}.scenarios;

/// <summary>
/// Empty scenario - clears the database.
/// This is the base scenario that all other scenarios can depend on.
/// </summary>
public class Sc010CreateSandBox(INDbUnit nDbUnit) : IScenario
{
    private readonly INDbUnit _nDbUnit = nDbUnit;

    /// <summary>
    /// Get the scenario file name used to store in the file system
    /// </summary>
    public string ScenarioFileName => "CreateSandBox";

    /// <summary>
    /// No pre-load scenario for this scenario
    /// </summary>
    public Type? PreloadScenario => null;

    /// <summary>
    /// Seed data - Clean all tables before starting scenarios
    /// </summary>
    public async Task SeedData()
    {
        await using var connection = new NpgsqlConnection(_nDbUnit.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // TODO: Add tables to clean in correct order (respecting foreign keys)
            // Clean child tables first, then parent tables
            var tablesToClean = new[]
            {
                // Example:
                // "public.order_items",
                // "public.orders",
                // "public.users",
                // "public.roles"
            };

            foreach (var table in tablesToClean)
            {
                await using var cmd = new NpgsqlCommand($"TRUNCATE TABLE {table} CASCADE", connection, transaction);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
