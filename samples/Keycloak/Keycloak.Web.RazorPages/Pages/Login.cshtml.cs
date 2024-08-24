using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Keycloak.Web.RazorPages.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
            HttpContext.Response.Redirect(ReturnUrl ?? "/");
        }

        private static readonly Uri _placeholderHostUri = new Uri("http://notused");

        public static string GetLoginUrl(NavigationManager nav, string? returnUrl = null, bool preserveQuery = true)
        {
            var loginUrl = "login";
            returnUrl ??= nav.Uri;

            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri))
            {
                if (!preserveQuery)
                {
                    // Remove the query string from the return URL
                    returnUrl = returnUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped);
                }
                // Make the return URL relative
                returnUrl = nav.ToBaseRelativePath(returnUrl);
            }
            else if (!preserveQuery)
            {
                // It's a relative URL so use a placeholder host
                returnUrl = new Uri(_placeholderHostUri, returnUrl).GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                loginUrl = QueryHelpers.AddQueryString(loginUrl, nameof(ReturnUrl), returnUrl);
            }

            return loginUrl;
        }
    }
}
