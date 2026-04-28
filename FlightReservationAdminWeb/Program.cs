using FlightReservationAdminWeb.Soap;

var builder = WebApplication.CreateBuilder(args);
var enableHttps = builder.Configuration.GetValue("Kestrel:EnableHttps", true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5002);
    if (enableHttps)
    {
        options.ListenAnyIP(5003, listen => listen.UseHttps());
    }

    options.Limits.MaxRequestBodySize = 20 * 1024 * 1024;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISoapClientFactory, SoapClientFactory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
