namespace Microsoft.AspNetCore.Components;

public static class NavigationManagerExtensions
{
    /// <summary>
    /// Navigate to the given URL if it is a relative URL, otherwise navigate to the fallback URL.
    /// </summary>
    public static void NavigateToRelativeUrl(this NavigationManager navigationManager, string? url, string fallbackUrl = "/", bool forceLoad = true)
    {
        // Ensure we only redirect to relative URLs (prevent open redirect attacks)
        var relativeUrl = navigationManager.EnsureRelativeUrl(url, fallbackUrl);
        navigationManager.NavigateTo(relativeUrl, forceLoad);
    }

    /// <summary>
    /// Ensure the given URL is a relative URL and return it, otherwise return the fallback URL.
    /// </summary>
    public static string EnsureRelativeUrl(this NavigationManager _, string? url, string fallbackUrl = "/")
    {
        // Ensure we only redirect to relative URLs (prevent open redirect attacks)
        var relativeUrl = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) && !uri.IsAbsoluteUri
            ? url
            : fallbackUrl;
        return relativeUrl;
    }
}
