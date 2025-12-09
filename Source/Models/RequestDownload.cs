namespace ImageDownloader.Models;

public class RequestDownload
{
    public IEnumerable<string> ImageUrls { get; set; } = new List<string>();
    public int MaxDownloadAtOnce { get; set; }
}
