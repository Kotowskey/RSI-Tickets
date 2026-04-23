using Microsoft.AspNetCore.Mvc;

namespace FlightReservationAdminWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Flights");

    public IActionResult Error() => View();
}
