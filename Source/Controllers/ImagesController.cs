using ImageDownloader.Models;
using ImageDownloader.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ImageDownloader.Controllers;

[ApiController]
[Route("[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ILogger<ImagesController> _logger;
    private readonly IImageStorageService _imageStorageService;

    public ImagesController(
        ILogger<ImagesController> logger, 
        IImageStorageService imageStorageService)
    {
        _logger = logger;
        _imageStorageService = imageStorageService;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDownload>> DownloadImages(
        [FromBody] RequestDownload request)
        => Ok(await _imageStorageService.DownloadAndStoreImagesAsync(request));


    [HttpGet("get-image-by-name/{imageName}")]
    public async Task<IActionResult> GetImageByName([FromRoute] string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return BadRequest("Image name is required.");

        var result = await _imageStorageService.GetStoredImageAsBase64Async(imageName);

        if (result.IsSuccess)
            return Ok(result.MessageOrBase64); // Return Base64 string
        else
            return BadRequest(result.MessageOrBase64); // Return error message
    }
}
