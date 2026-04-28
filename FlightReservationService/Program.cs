using FlightReservationService.Data;
using FlightReservationService.Handlers;
using FlightReservationService.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SoapCore;
using SoapCore.Extensibility;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var enableHttps = builder.Configuration.GetValue("Kestrel:EnableHttps", true);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=flights.db";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    if (enableHttps)
    {
        options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps());
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IFlightReservationService, FlightReservationServiceImpl>();
builder.Services.AddSingleton<IServiceOperationTuner, SoapLoggingHandler>();
builder.Services.AddSoapCore();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (db.Database.CanConnect())
    {
        try
        {
            _ = db.Flights.AsNoTracking()
                .Select(f => new { f.PhotoFileName })
                .FirstOrDefault();
        }
        catch
        {
            db.Database.EnsureDeleted();
        }
    }

    db.Database.EnsureCreated();
}

app.UseMiddleware<SoapMessageLoggingMiddleware>();

((IApplicationBuilder)app).UseSoapEndpoint<IFlightReservationService>(
    path: "/FlightService.asmx",
    encoder: new SoapEncoderOptions
    {
        MessageVersion = System.ServiceModel.Channels.MessageVersion.Soap11WSAddressingAugust2004,
        WriteEncoding = System.Text.Encoding.UTF8,
        ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
        {
            MaxStringContentLength = 10 * 1024 * 1024
        }
    },
    serializer: SoapSerializer.DataContractSerializer,
    caseInsensitivePath: true);

app.MapGet("/", () => Results.Redirect("/FlightService.asmx"));

app.Run();
