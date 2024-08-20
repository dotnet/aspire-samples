using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
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
/// <param name="configuration"></param>
public class KeycloakRolesClaimsTransformation(IHostEnvironment hostEnvironment, IOptions<KeycloakClaimsTransformationOptions> options) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var clientName = options.Value.ClientName ?? hostEnvironment.ApplicationName;

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

            if (resourceAccess[clientName] is JsonObject resourceNode && resourceNode["roles"] is JsonArray resourceRoles)
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

public class KeycloakClaimsTransformationOptions
{
    /// <summary>
    /// The client name of this application in Keycloak.
    /// </summary>
    public string? ClientName { get; set; }
}

public static class KeycloakClaimsTransformationExtensions
{
    /// <summary>
    /// Adds an <see cref="IClaimsTransformation" /> that transforms Keycloak resource access roles claims into regular role claims.
    /// </summary>
    public static IServiceCollection AddKeycloakClaimsTransformation(this IServiceCollection services, string clientName, Action<KeycloakClaimsTransformationOptions>? configure = null)
    {
        services.Configure<KeycloakClaimsTransformationOptions>(o =>
        {
            o.ClientName = clientName;
        });
        if (configure is not null)
        {
            services.Configure(configure);
        }
        return services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
    }
}
