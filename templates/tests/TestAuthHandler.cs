using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MiProyecto.webapi.tests;

/// <summary>
/// Test authorization handler for simulating authenticated users in tests
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string EMAILCLAIMTYPE = "email";
    private const string USERNAMECLAIMTYPE = "username";

    /// <summary>
    /// Constructor
    /// </summary>
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Handle the authentication
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(EMAILCLAIMTYPE, this.ClaimsIssuer),
            new Claim(USERNAMECLAIMTYPE, this.ClaimsIssuer),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}
