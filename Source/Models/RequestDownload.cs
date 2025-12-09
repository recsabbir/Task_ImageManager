using System.ComponentModel.DataAnnotations;

namespace ImageDownloader.Models;

public class RequestDownload
{
    public IEnumerable<string> ImageUrls { get; set; } = new List<string>();

    [Range(1, int.MaxValue, ErrorMessage = "Value must be greater than or equal to 1.")]
    public int MaxDownloadAtOnce { get; set; }
}