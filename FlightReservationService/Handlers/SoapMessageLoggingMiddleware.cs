namespace FlightReservationService.Handlers;

public class SoapMessageLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SoapMessageLoggingMiddleware> _logger;

    public SoapMessageLoggingMiddleware(RequestDelegate next, ILogger<SoapMessageLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/FlightService.asmx"))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        var requestBody = string.Empty;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        _logger.LogInformation("=== SOAP REQUEST ===\nContent-Type: {ContentType}\n{Body}",
            context.Request.ContentType, requestBody);

        var originalBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);

        if (responseText.Length <= 2000)
        {
            _logger.LogInformation("=== SOAP RESPONSE ===\nStatus: {Status}\n{Body}",
                context.Response.StatusCode, responseText);
        }
        else
        {
            _logger.LogInformation("=== SOAP RESPONSE ===\nStatus: {Status}\n{Body}... [truncated, {Len} bytes total]",
                context.Response.StatusCode, responseText[..2000], responseText.Length);
        }

        await responseBody.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}
