using System.Data;

namespace {ProjectName}.common.tests;

/// <summary>
/// Extension methods to access DataSet tables and perform common queries.
/// Add methods for each table in your schema.
/// </summary>
public static class AppSchemaExtender
{
    #region Full qualified table names
    // Define table names with schema prefix
    // Example: public static readonly string FullUsersTableName = "public.users";
    #endregion


    #region Get tables methods
    // Example:
    // public static DataTable? GetUsersTable(this DataSet appSchema)
    //     => appSchema.Tables[FullUsersTableName];
    #endregion


    #region Get rows methods
    // Example:
    // public static IEnumerable<DataRow> GetUsersRows(this DataSet appSchema, string filterExpression)
    //     => GetUsersTable(appSchema)?.Select(filterExpression).AsEnumerable() ?? Enumerable.Empty<DataRow>();
    #endregion


    #region Get single row methods
    // Example:
    // public static DataRow? GetFirstUserRow(this DataSet appSchema)
    //     => GetUsersTable(appSchema)?.AsEnumerable().FirstOrDefault();
    #endregion


    #region Count methods
    // Example:
    // public static int CountUsers(this DataSet appSchema)
    //     => GetUsersTable(appSchema)?.Rows.Count ?? 0;
    #endregion


    #region Lookup methods
    // Example:
    // public static Guid GetUserIdByEmail(this DataSet appSchema, string email)
    // {
    //     var rows = appSchema.GetUsersRows($"email = '{email}'").ToList();
    //     if (rows.Count == 0)
    //         return Guid.Empty;
    //     return rows.First().Field<Guid>("id");
    // }
    #endregion
}
