using ImageDownloader.Models;

namespace ImageDownloader.Services.Contracts;

public interface IImageStorageService
{
    /// <summary>
    /// Downloads images from the provided URLs and stores them.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<ResponseDownload> DownloadAndStoreImagesAsync(RequestDownload request);

    /// <summary>
    /// Retrieves a stored image from the server's local storage and converts it to a Base64 string.
    /// </summary>
    /// <param name="storedFileName">The unique filename of the stored image.</param>
    /// <returns>
    /// A <see cref="ValueTuple"/> where:
    /// <list type="bullet">
    /// <item><description>IsSuccess (bool): Indicates whether the operation was successful (true) or failed (false).</description></item>
    /// <item><description>MessageOrBase64 (string): If successful, contains the Base64-encoded string of the image; 
    /// if failed, contains an error message (e.g., "Image not found." or "Error reading image: ...").</description></item>
    /// </list>
    /// </returns>
    Task<(bool IsSuccess, string MessageOrBase64)> GetStoredImageAsBase64Async(string storedFileName);
}
