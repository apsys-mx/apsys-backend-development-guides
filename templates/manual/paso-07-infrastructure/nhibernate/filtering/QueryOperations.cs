using Microsoft.AspNetCore.WebUtilities;

namespace MiProyecto.infrastructure.nhibernate.filtering;

/// <summary>
/// Defines QueryOperations class
/// </summary>
public static class QueryOperations
{

    /// <summary>
    /// Adds a user condition to an existing query string
    /// </summary>
    /// <param name="query"></param>
    /// <param name="organizationId"></param>
    /// <returns></returns>
    public static string AddOrganizationIdToQuery(string? query, string organizationId)
    {
        if (query == null)
            query = string.Empty;

        var updatedQuery = QueryHelpers.ParseQuery(query);
        var queryDict = updatedQuery.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        if (queryDict.ContainsKey("OrganizationId"))
        {
            queryDict["OrganizationId"] = $"{queryDict["user"]} and {organizationId}||eq";
        }
        else
        {
            queryDict["OrganizationId"] = $"{organizationId}||eq";
        }
        return QueryHelpers.AddQueryString(string.Empty, queryDict);
    }
}
