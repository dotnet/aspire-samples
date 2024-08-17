using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaimValue(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        var claim = claimsPrincipal.FindFirst(claimType);
        return claim?.Value;
    }

    public static bool TryGetJsonClaim(this ClaimsPrincipal claimsPrincipal, string claimType, [NotNullWhen(true)] out JsonNode? claimJson)
    {
        var candidateClaim = claimsPrincipal.FindFirst(claimType);

        claimJson = candidateClaim is not null && string.Equals(candidateClaim.ValueType, "JSON", StringComparison.OrdinalIgnoreCase)
            ? JsonNode.Parse(candidateClaim.Value)
            : null;

        return claimJson is not null;
    }
}
