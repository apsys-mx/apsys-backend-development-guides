using System.Data;
using MiProyecto.domain.resources;

namespace MiProyecto.common.tests;

public static class AppSchemaExtender
{

    #region Get full qualified table names
    public static readonly string FullRolesTableName = $"{AppSchemaResource.SchemaName}.{AppSchemaResource.RolesTable}";
    public static readonly string FullUsersTableName = $"{AppSchemaResource.SchemaName}.{AppSchemaResource.UsersTable}";
    // Agregar más tablas según sea necesario
    #endregion


    #region Get tables methods
    public static DataTable? GetRolesTable(this DataSet appSchema)
        => appSchema.Tables[FullRolesTableName];
    public static DataTable? GetUsersTable(this DataSet appSchema)
        => appSchema.Tables[FullUsersTableName];
    // Agregar más métodos según sea necesario
    #endregion


    #region Get rows methods

    public static IEnumerable<DataRow> GetRolesRows(this DataSet appSchema, string filterExpression)
        => GetRolesTable(appSchema)?.Select(filterExpression).AsEnumerable() ?? Enumerable.Empty<DataRow>();

    public static IEnumerable<DataRow> GetUsersRows(this DataSet appSchema, string filterExpression)
        => GetUsersTable(appSchema)?.Select(filterExpression).AsEnumerable() ?? Enumerable.Empty<DataRow>();

    // Agregar más métodos según sea necesario
    #endregion


    #region Get single row methods

    public static DataRow? GetFirstUserRow(this DataSet appSchema)
        => GetUsersTable(appSchema)?.AsEnumerable().FirstOrDefault();

    public static DataRow? GetFirstRoleRow(this DataSet appSchema)
        => GetRolesTable(appSchema)?.AsEnumerable().FirstOrDefault();

    // Agregar más métodos según sea necesario
    #endregion
}
