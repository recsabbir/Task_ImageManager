using System.Collections.Concurrent;
using ImageDownloader.Models;
using ImageDownloader.Services.Contracts;
using ImageDownloader.Utilities;

namespace ImageDownloader.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly string _imagesFolderName;

    public ImageStorageService(
        IConfiguration config, 
        IWebHostEnvironment env,
        IHttpClientFactory httpClientFactory)
    {
        _imagesFolderName = config["ImageDirectoryName"];
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    ///<inheritdoc/>
    public async Task<ResponseDownload> DownloadAndStoreImagesAsync(RequestDownload request)
    {
        if (request.ImageUrls == null)
            return new ResponseDownload
            {
                Success = false,
                Message = "No image URLs supplied."
            };

        var imageDownloadProcessTracker = new ConcurrentDictionary<string, (bool IsSuccess, string MessageOrName)>();

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
                        imageDownloadProcessTracker[url] = (IsSuccess: false, MessageOrName: "Invalid URL.");
                        return;
                    }

                    var fileName = await DownloadAndSaveImageAsync(url, imagesFolder);
                    imageDownloadProcessTracker[url] = (IsSuccess: true, MessageOrName: fileName);
                }
                catch (Exception ex)
                {
                    imageDownloadProcessTracker[url] = (IsSuccess: false, MessageOrName: $"FAILED: {ex.Message}");
                }
            });

        return PrepareMessage(
            successCount: imageDownloadProcessTracker.Count(x=>x.Value.IsSuccess),
            failureCount: imageDownloadProcessTracker.Count(x => !x.Value.IsSuccess),
            duplicateCount : request.ImageUrls.Count() - distinctUrls.Count,
            urlAndNames : imageDownloadProcessTracker
                                .Where(x=>x.Value.IsSuccess)
                                .ToDictionary(x => x.Key, x => x.Value.MessageOrName)
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
            return (false, $"Error reading image: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string folder)
    {
        using var client = _httpClientFactory.CreateClient();

        using var response = await client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // Determine file extension based on content-type
        var ext = FileHelper.GetFileExtensionFromContentType(response.Content.Headers.ContentType?.MediaType ?? "");

        if (ext.Equals("ignore"))
            return "Not a valid image URL.";

        var fileName = Guid.NewGuid().ToString();
        var filePath = Path.Combine(folder, $"{fileName}{ext}");

        await using var networkStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);

        await networkStream.CopyToAsync(fileStream);

        return fileName;
    }

    private ResponseDownload PrepareMessage(
        int successCount, 
        int failureCount, 
        int duplicateCount, 
        IDictionary<string, string> urlAndNames)
    {
        var total = successCount + failureCount + duplicateCount;

        var response = new ResponseDownload
        {
            Success = successCount > 0, // success only if at least 1 image downloaded
            UrlAndNames = urlAndNames
        };

        #region Building Message
        var partOfMessage = new List<string>();

        if (successCount > 0)
            partOfMessage.Add($"{successCount} downloaded successfully");

        if (failureCount > 0)
            partOfMessage.Add($"{failureCount} failed");

        if (duplicateCount > 0)
            partOfMessage.Add($"{duplicateCount} duplicate URL(s) ignored");

        response.Message = string.Join(", ", partOfMessage) + ".";

        #endregion

        return response;
    }

    #endregion
}