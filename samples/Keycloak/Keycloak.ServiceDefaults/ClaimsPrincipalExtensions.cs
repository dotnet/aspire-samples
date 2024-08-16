namespace System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        var claim = claimsPrincipal.FindFirst(claimType);
        return claim?.Value;
    }
}
