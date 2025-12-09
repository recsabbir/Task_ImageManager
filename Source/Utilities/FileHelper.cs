namespace ImageDownloader.Utilities;

public static class FileHelper
{
    public static string GetFileExtensionFromContentType(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/jpg" or "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/tiff" => ".tiff",
            "image/webp" => ".webp",
            _ => "ignore",
        };
    }

    public static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
