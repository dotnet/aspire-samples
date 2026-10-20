using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Keycloak.Web.RazorPages.Pages
{
    public class LogoutModel : PageModel
    {
        [BindProperty]
        public string? ReturnUrl { get; set; }

        public async Task OnPost()
        {
            var returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Page("/Index", new { area = "" });
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new() { RedirectUri = returnUrl });
        }
    }
}
