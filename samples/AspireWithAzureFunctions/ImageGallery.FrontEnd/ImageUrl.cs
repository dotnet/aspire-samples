namespace ImageGallery.FrontEnd;

internal static class ImageUrl
{
    public static readonly string PathPrefix = "/images";
    public static readonly string RoutePattern = PathPrefix + "/{slug}";

    public static string GetImageUrl(string slug) => $"{PathPrefix}/{slug}";

    public static string GetThumbnailUrl(string slug) => GetImageUrl(slug) + "?thumbnail=true";

    public static string CreateNameSlug(string name)
    {
        var extension = Path.GetExtension(name);

        var nameSpan = name.AsSpan();
        Span<char> slugBuffer = stackalloc char[nameSpan.Length];

        // Replace invalid characters with '-'
        // Valid chars are letters, digits, '-', and '_'
        for (var i = 0; i < nameSpan.Length - extension.Length; i++)
        {
            var c = nameSpan[i];
            slugBuffer[i] = char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-';
        }

        // Add extension back
        nameSpan[^extension.Length..].CopyTo(slugBuffer[^extension.Length..]);

        var slug = new string(slugBuffer);

        return slug;
    }
}
