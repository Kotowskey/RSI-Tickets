using FlightReservationAdminWeb.Models;
using FlightReservationAdminWeb.Soap;
using Microsoft.AspNetCore.Mvc;

namespace FlightReservationAdminWeb.Controllers;

public class FlightsController : Controller
{
    private readonly ISoapClientFactory _soapFactory;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(ISoapClientFactory soapFactory, ILogger<FlightsController> logger)
    {
        _soapFactory = soapFactory;
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var flights = client.GetAllFlights() ?? new List<Flight>();
            return View(flights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load flights");
            TempData["Error"] = $"Nie udało się pobrać listy lotów: {ex.Message}";
            return View(new List<Flight>());
        }
    }

    public IActionResult Details(int id)
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var flight = client.GetFlight(id);
            if (flight == null) return NotFound();
            return View(flight);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Błąd pobierania lotu: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Photo(int id)
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var response = client.GetFlightPhoto(id);
            if (!response.Success || response.PhotoData == null || response.PhotoData.Length == 0)
            {
                return NotFound();
            }
            return File(response.PhotoData, response.ContentType ?? "application/octet-stream");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load photo for flight {Id}", id);
            return NotFound();
        }
    }

    public IActionResult Create() => View(new FlightFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlightFormModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var request = await BuildAdminRequest(model, isEdit: false);

        try
        {
            var client = _soapFactory.CreateClient();
            var response = client.AddFlight(request);
            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }
            TempData["Success"] = response.Message;
            return RedirectToAction(nameof(Details), new { id = response.FlightId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Błąd połączenia z serwisem SOAP: {ex.Message}");
            return View(model);
        }
    }

    public IActionResult Edit(int id)
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var flight = client.GetFlight(id);
            if (flight == null) return NotFound();

            var model = new FlightFormModel
            {
                Id = flight.Id,
                FlightNumber = flight.FlightNumber,
                CityFrom = flight.CityFrom,
                CityTo = flight.CityTo,
                DepartureDate = flight.DepartureDate,
                DepartureTime = flight.DepartureTime,
                Price = flight.Price,
                AvailableSeats = flight.AvailableSeats,
                HasExistingPhoto = flight.HasPhoto,
            };
            return View(model);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Błąd pobierania lotu: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FlightFormModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var request = await BuildAdminRequest(model, isEdit: true);

        try
        {
            var client = _soapFactory.CreateClient();
            var response = client.UpdateFlight(request);
            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }
            TempData["Success"] = response.Message;
            return RedirectToAction(nameof(Details), new { id = response.FlightId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Błąd połączenia z serwisem SOAP: {ex.Message}");
            return View(model);
        }
    }

    public IActionResult Delete(int id)
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var flight = client.GetFlight(id);
            if (flight == null) return NotFound();
            return View(flight);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Błąd: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            var client = _soapFactory.CreateClient();
            var response = client.DeleteFlight(id);
            if (!response.Success)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = response.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Błąd połączenia: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    private static async Task<FlightAdminRequest> BuildAdminRequest(FlightFormModel model, bool isEdit)
    {
        var request = new FlightAdminRequest
        {
            Id = isEdit ? model.Id : 0,
            FlightNumber = model.FlightNumber.Trim(),
            CityFrom = model.CityFrom.Trim(),
            CityTo = model.CityTo.Trim(),
            DepartureDate = model.DepartureDate.Date,
            DepartureTime = model.DepartureTime.Trim(),
            Price = model.Price,
            AvailableSeats = model.AvailableSeats,
            RemovePhoto = isEdit && model.RemovePhoto,
        };

        if (model.Photo != null && model.Photo.Length > 0)
        {
            using var ms = new MemoryStream();
            await model.Photo.CopyToAsync(ms);
            request.PhotoData = ms.ToArray();
            request.PhotoFileName = Path.GetFileName(model.Photo.FileName);
            request.PhotoContentType = model.Photo.ContentType;
        }

        return request;
    }
}
