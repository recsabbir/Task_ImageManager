using System.Collections.Concurrent;
using ImageDownloader.Models;
using ImageDownloader.Services.Contracts;
using ImageDownloader.Utilities;

namespace ImageDownloader.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly ILogger<ImageStorageService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly string _imagesFolderName;

    public ImageStorageService(
        ILogger<ImageStorageService> logger,
        IConfiguration config, 
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _imagesFolderName = config["ImageDirectoryName"];
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    ///<inheritdoc/>
    public async Task<ResponseDownload> DownloadAndStoreImagesAsync(RequestDownload request)
    {
        if (request.ImageUrls.IsNullOrEmpty())
            return new ResponseDownload
            {
                Success = false,
                Message = "No image URLs supplied."
            };

        //Status
        
        var imageDownloadProcessTracker = 
            new ConcurrentDictionary<string, (DownloadProcessStatus Status, string MessageOrName)>(); 

        var imagesFolder = Path.Combine(_env.WebRootPath, _imagesFolderName);
        Directory.CreateDirectory(imagesFolder);//Create Folder if not exists

        //Considering same URL only once
        var distinctUrls = request.ImageUrls.Distinct().ToList();

        await Parallel.ForEachAsync(
            source: distinctUrls, 
            parallelOptions: new ParallelOptions
            {
                MaxDegreeOfParallelism = request.MaxDownloadAtOnce
            },
            body: async (url, _) =>
            {
                try
                {
                    // Validating URL format
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        imageDownloadProcessTracker[url] = (Status: DownloadProcessStatus.InvalidUrl, MessageOrName: "Invalid URL.");
                        return;
                    }

                    imageDownloadProcessTracker[url] = await DownloadAndSaveImageAsync(url, imagesFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading image from URL: {Url}", url);
                    imageDownloadProcessTracker[url] = (Status: DownloadProcessStatus.Failed, MessageOrName: $"FAILED: {ex.Message}");
                }
            });

        var successfulUrls = imageDownloadProcessTracker
                                .Where(x => x.Value.Status.Equals(DownloadProcessStatus.Success))
                                .ToDictionary(x => x.Key, x => x.Value.MessageOrName);

        return PrepareMessage(
            successCount: successfulUrls.Count,
            failureCount: distinctUrls.Count - successfulUrls.Count,
            duplicateCount : request.ImageUrls.Count() - distinctUrls.Count,
            urlAndNames : successfulUrls,
            invalidUrls : imageDownloadProcessTracker
                                    .Where(x=>x.Value.Status.Equals(DownloadProcessStatus.InvalidUrl))
                                    .Select(x=>x.Key),
            invalidFileTypeUrls : imageDownloadProcessTracker
                                    .Where(x => x.Value.Status.Equals(DownloadProcessStatus.InvalidFileType))
                                    .Select(x => x.Key)
        );
    }

    ///<inheritdoc/>
    public async Task<(bool IsSuccess, string MessageOrBase64)> GetStoredImageAsBase64Async(string storedFileName)
    {
        var imagesDir = Path.Combine(_env.WebRootPath, _imagesFolderName);

        if (!Directory.Exists(imagesDir))
            return (false, "Images directory does not exist.");

        // Searching file by name inside image directory
        string pattern = storedFileName + ".*";
        var matchedFiles = Directory.GetFiles(imagesDir, pattern);

        if (matchedFiles.Length == 0)
            return (false, "Image not found.");

        // Taking first matched file
        string path = matchedFiles[0];

        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var base64String = Convert.ToBase64String(bytes);
            var mimeType = FileHelper.GetMimeTypeFromExtension(Path.GetExtension(path));

            // Return full data URI
            string dataUri = $"data:{mimeType};base64,{base64String}";
            return (true, dataUri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading image file: {FilePath}", path);
            return (false, $"Error reading image: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<(DownloadProcessStatus, string)> DownloadAndSaveImageAsync(string imageUrl, string folder)
    {
        using var client = _httpClientFactory.CreateClient();

        using var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // Determine file extension based on content-type
        var ext = FileHelper.GetFileExtensionFromContentType(response.Content.Headers.ContentType?.MediaType ?? "");

        if (ext.Equals("ignore"))
            return (DownloadProcessStatus.InvalidFileType,"Invalid image URL.");

        var fileName = Guid.NewGuid().ToString();
        var filePath = Path.Combine(folder, $"{fileName}{ext}");

        await using var networkStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

        await networkStream.CopyToAsync(fileStream);

        return (DownloadProcessStatus.Success, fileName);
    }

    private ResponseDownload PrepareMessage(
    int successCount,
    int failureCount,
    int duplicateCount,
    IDictionary<string, string> urlAndNames,
    IEnumerable<string> invalidUrls,
    IEnumerable<string> invalidFileTypeUrls)
    {
        var response = new ResponseDownload
        {
            Success = successCount > 0,
            UrlAndNames = urlAndNames
        };

        #region Building Message
        var parts = new List<string>();

        if (successCount > 0)
            parts.Add($"{successCount} downloaded successfully");

        if (failureCount > 0)
            parts.Add($"{failureCount} failed");

        if (duplicateCount > 0)
            parts.Add($"{duplicateCount} duplicate URL(s) ignored");

        // Add base summary
        response.Message = string.Join(", ", parts) + ".";

        // Add invalid URLs
        if (invalidUrls.IsNotNullOrEmpty())
        {
            response.Message += 
                $" {invalidUrls.Count()} Invalid URL(s): [ " +
                string.Join(", ", invalidUrls) + " ].";
        }

        // Add unsupported file type URLs
        if (invalidFileTypeUrls.IsNotNullOrEmpty())
        {
            response.Message +=
                $" {invalidFileTypeUrls.Count()} Unsupported file type URL(s): [ " +
                string.Join(" , ", invalidFileTypeUrls) + " ]." ;
        }
        #endregion

        return response;
    }


    #endregion

    private enum DownloadProcessStatus
    {
        Success = 1,
        InvalidUrl = 2,
        InvalidFileType = 3,
        Failed = 4
    }
}