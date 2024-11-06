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

        var chars = name.AsSpan();
        Span<char> buffer = stackalloc char[chars.Length];

        // Replace invalid characters with '-'
        // Valid chars are letters, digits, '-', and '_'
        for (var i = 0; i < chars.Length - extension.Length; i++)
        {
            var c = chars[i];
            buffer[i] = char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-';
        }

        // Add extension back
        for (var i = 0; i < extension.Length; i++)
        {
            buffer[chars.Length - extension.Length + i] = extension[i];
        }

        var slug = new string(buffer);

        return slug;
    }
}
