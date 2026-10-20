using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Transforms Keycloak resource roles claims into regular role claims.
/// </summary>
/// <remarks>
/// Learn more about claims transformation in ASP.NET Core at
/// <see href="https://learn.microsoft.com/aspnet/core/security/authentication/claims#extend-or-add-custom-claims-using-iclaimstransformation">
/// https://learn.microsoft.com/aspnet/core/security/authentication/claims#extend-or-add-custom-claims-using-iclaimstransformation
/// </see>
/// </remarks>
public class KeycloakRolesClaimsTransformation(IOptionsSnapshot<JwtBearerOptions> jwtBearerOptions) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var options = jwtBearerOptions.Get(JwtBearerDefaults.AuthenticationScheme);
        var clientId = options.TokenValidationParameters.ValidAudience
            ?? options.TokenValidationParameters.ValidAudiences.FirstOrDefault()
            ?? throw new InvalidOperationException("Audience is not set on JwtBearerOptions");

        if (principal.TryGetJsonClaim("resource_access", out var resourceAccess))
        {
            // Payload example:
            // {
            //   "resource-name": {
            //     "roles": [ "role-name" ]
            //   },
            //   "account": {
            //     "roles": [ "manage-account", "manage-account-links", "view-profile" ]
            //   }
            // }

            if (resourceAccess[clientId] is JsonObject resourceNode && resourceNode["roles"] is JsonArray resourceRoles)
            {
                // Convert resource roles to regular roles.
                var claimsIdentity = new ClaimsIdentity();
                foreach (var role in resourceRoles.GetValues<string>())
                {
                    if (!claimsIdentity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role))
                    {
                        claimsIdentity.AddClaim(new(ClaimTypes.Role, role));
                    }
                }
                principal.AddIdentity(claimsIdentity);
            }
        }

        return Task.FromResult(principal);
    }
}

public static class KeycloakClaimsTransformationExtensions
{
    /// <summary>
    /// Adds an <see cref="IClaimsTransformation" /> that transforms Keycloak resource access roles claims into regular role claims.
    /// </summary>
    public static IServiceCollection AddKeycloakClaimsTransformation(this IServiceCollection services)
    {
        return services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
    }
}
