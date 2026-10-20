using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Keycloak.Web.RazorPages.Pages
{
    public class AuthRequiredModel(KeycloakUrls keycloakUrls) : PageModel
    {
        public string? UserProfileManagementUrl { get; set; }

        public UserDetails? CurrentUser { get; set; }

        public async Task OnGetAsync()
        {
            UserProfileManagementUrl = await keycloakUrls.GetAccountConsoleUrlAsync("keycloak");
            CurrentUser = UserDetails.CreateFromClaims(User);
        }

        public class UserDetails
        {
            public required string Username { get; set; }
            public string? Email { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? AddressStreet { get; set; }
            public string? AddressCity { get; set; }
            public string? AddressState { get; set; }
            public string? AddressZipCode { get; set; }
            public string? AddressCountry { get; set; }

            public static UserDetails? CreateFromClaims(ClaimsPrincipal claimsPrincipal)
            {
                var user = new UserDetails
                {
                    Username = claimsPrincipal.Identity?.Name ?? "[unknown]",
                    Email = claimsPrincipal.GetClaimValue(KeycloakClaimTypes.Email),
                    FirstName = claimsPrincipal.GetClaimValue(KeycloakClaimTypes.GivenName),
                    LastName = claimsPrincipal.GetClaimValue(KeycloakClaimTypes.FamilyName)
                };
                if (claimsPrincipal.TryGetJsonClaim(KeycloakClaimTypes.Address, out var addressJson))
                {
                    user.AddressStreet = addressJson[KeycloakClaimTypes.StreetAddress]?.GetValue<string>();
                    user.AddressCity = addressJson[KeycloakClaimTypes.Locality]?.GetValue<string>();
                    user.AddressState = addressJson[KeycloakClaimTypes.Region]?.GetValue<string>();
                    user.AddressZipCode = addressJson[KeycloakClaimTypes.PostalCode]?.GetValue<string>();
                    user.AddressCountry = addressJson[KeycloakClaimTypes.Country]?.GetValue<string>();
                }
                return user;
            }
        }
    }
}
