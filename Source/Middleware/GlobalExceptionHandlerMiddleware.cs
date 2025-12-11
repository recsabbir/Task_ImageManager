using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ImageDownloader.Middleware;

/// <summary>
/// Global exception handler for all unhandled exceptions
/// and returns standardized RFC 7807 Problem Details responses.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        // Prepare the problem details response
        var problemDetails = CreateProblemDetails(context, exception);

        // Set response properties
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Serialize and write the response
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
        => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.0"
        };
}
