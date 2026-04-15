using SoapCore.Extensibility;
using SoapCore.ServiceModel;

namespace FlightReservationService.Handlers;

public class SoapLoggingHandler : IServiceOperationTuner
{
    private readonly ILogger<SoapLoggingHandler> _logger;

    public SoapLoggingHandler(ILogger<SoapLoggingHandler> logger)
    {
        _logger = logger;
    }

    public void Tune(HttpContext httpContext, object serviceInstance, OperationDescription operation)
    {
        _logger.LogInformation(
            "=== SOAP Handler === Operacja: {Operation}, Metoda HTTP: {Method}, Czas: {Time}, IP: {IP}",
            operation.Name,
            httpContext.Request.Method,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            httpContext.Connection.RemoteIpAddress);
    }
}
