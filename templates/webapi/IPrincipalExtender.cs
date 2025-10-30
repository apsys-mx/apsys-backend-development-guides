using System.Configuration;
using System.Security.Claims;
using System.Security.Principal;

namespace {ProjectName}.webapi;

/// <summary>
/// IPrincipalExtender class provides extension methods for working with user claims
/// </summary>
public static class IPrincipalExtender
{

    private const string EMAILCLAIMTYPE = "email";
    private const string USERNAMECLAIMTYPE = "username";
    private const string NAMECLAIMTYPE = "name";


    /// <summary>
    /// Get a claim from iprincipal
    /// </summary>
    /// <param name="principal"></param>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public static Claim? GetClaim(this IPrincipal principal, string claimType)
    {
        ClaimsPrincipal? claims = principal as ClaimsPrincipal;
        return claims?.FindFirst(claimType);
    }

    /// <summary>
    /// Get a claim value
    /// </summary>
    /// <param name="principal"></param>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public static string GetClaimValue(this IPrincipal principal, string claimType)
    {
        var claimn = principal.GetClaim(claimType);
        return claimn == null ? string.Empty : claimn.Value;
    }

    /// <summary>
    /// Get the username from the iprincipal claims
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    public static string GetUserName(this IPrincipal principal)
    {
        var userName = principal.GetClaimValue(USERNAMECLAIMTYPE);
        if (string.IsNullOrEmpty(userName))
            throw new ConfigurationErrorsException("No username claim found in the user's principal. If the authority is Auth0, verify that a custom action is adding the additional claims in the login flow");
        return userName;
    }

    /// <summary>
    /// Get the email from the iprincipal claims
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    public static string GetUserEmail(this IPrincipal principal)
    {
        var userName = principal.GetClaimValue(EMAILCLAIMTYPE);
        if (string.IsNullOrEmpty(userName))
            throw new ConfigurationErrorsException("No email claim found in the user's principal. If the authority is Auth0, verify that a custom action is adding the additional claims in the login flow");
        return userName;
    }

    /// <summary>
    /// Get the name from the iprincipal claims
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    public static string GetName(this IPrincipal principal) => principal.GetClaimValue(NAMECLAIMTYPE);
}
