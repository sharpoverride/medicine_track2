using System.Text;

public class RequestBodyLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyLoggingMiddleware> _logger;

    public RequestBodyLoggingMiddleware(RequestDelegate next, ILogger<RequestBodyLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ensure the request body can be read multiple times.
        context.Request.EnableBuffering();

        // Read the stream into a string
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true); // Important to leave the stream open

        var bodyAsString = await reader.ReadToEndAsync();

        if (!string.IsNullOrEmpty(bodyAsString))
        {
            _logger.LogInformation("--> HTTP Request Body: {Body}", bodyAsString);
        }

        // Reset the stream position for the next middleware in the pipeline
        context.Request.Body.Position = 0;

        try
        {
            await _next(context);
        }
        finally
        {

        }
    }
}
