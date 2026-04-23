using FlightReservationService.Data;
using FlightReservationService.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FlightReservationService.Services;

public class FlightReservationServiceImpl : IFlightReservationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<FlightReservationServiceImpl> _logger;

    public FlightReservationServiceImpl(AppDbContext db, ILogger<FlightReservationServiceImpl> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Flight> GetAllFlights()
    {
        _logger.LogInformation("GetAllFlights called");
        var flights = _db.Flights.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime).ToList();
        return flights.Select(StripPhotoBytes).ToList();
    }

    public List<Flight> SearchFlights(FlightSearchRequest request)
    {
        _logger.LogInformation("SearchFlights called: From={From}, To={To}, Date={Date}",
            request.CityFrom, request.CityTo, request.Date);

        var query = _db.Flights.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.CityFrom))
            query = query.Where(f => f.CityFrom.ToLower().Contains(request.CityFrom.ToLower()));

        if (!string.IsNullOrWhiteSpace(request.CityTo))
            query = query.Where(f => f.CityTo.ToLower().Contains(request.CityTo.ToLower()));

        if (request.Date.HasValue)
            query = query.Where(f => f.DepartureDate.Date == request.Date.Value.Date);

        var flights = query.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime).ToList();
        return flights.Select(StripPhotoBytes).ToList();
    }

    private static Flight StripPhotoBytes(Flight flight)
    {
        flight.HasPhoto = flight.PhotoData != null && flight.PhotoData.Length > 0;
        flight.PhotoData = null;
        flight.PhotoFileName = null;
        flight.PhotoContentType = null;
        return flight;
    }

    public TicketPurchaseResponse BuyTicket(TicketPurchaseRequest request)
    {
        _logger.LogInformation("BuyTicket called: FlightId={FlightId}, Passenger={Passenger}",
            request.FlightId, request.PassengerName);

        var flight = _db.Flights.Find(request.FlightId);
        if (flight == null)
        {
            return new TicketPurchaseResponse
            {
                Success = false,
                Message = "Nie znaleziono lotu o podanym ID."
            };
        }

        if (flight.AvailableSeats <= 0)
        {
            return new TicketPurchaseResponse
            {
                Success = false,
                Message = "Brak dostępnych miejsc na tym locie."
            };
        }

        flight.AvailableSeats--;

        var reservation = new Reservation
        {
            FlightId = flight.Id,
            PassengerName = request.PassengerName,
            PassengerEmail = request.PassengerEmail,
            ReservationNumber = GenerateReservationNumber(),
            PurchaseDate = DateTime.Now,
            SeatNumber = $"{(char)('A' + Random.Shared.Next(6))}{Random.Shared.Next(1, 31)}"
        };

        _db.Reservations.Add(reservation);
        _db.SaveChanges();

        _logger.LogInformation("Ticket purchased: ReservationNumber={ResNum}", reservation.ReservationNumber);

        return new TicketPurchaseResponse
        {
            Success = true,
            Message = $"Bilet kupiony pomyślnie. Lot: {flight.FlightNumber}, Miejsce: {reservation.SeatNumber}",
            ReservationNumber = reservation.ReservationNumber
        };
    }

    public ReservationDetails CheckReservation(string reservationNumber)
    {
        _logger.LogInformation("CheckReservation called: {ResNum}", reservationNumber);

        var reservation = _db.Reservations
            .Include(r => r.Flight)
            .FirstOrDefault(r => r.ReservationNumber == reservationNumber);

        if (reservation?.Flight == null)
        {
            return new ReservationDetails { Found = false };
        }

        return new ReservationDetails
        {
            Found = true,
            ReservationNumber = reservation.ReservationNumber,
            PassengerName = reservation.PassengerName,
            PassengerEmail = reservation.PassengerEmail,
            FlightNumber = reservation.Flight.FlightNumber,
            CityFrom = reservation.Flight.CityFrom,
            CityTo = reservation.Flight.CityTo,
            DepartureDate = reservation.Flight.DepartureDate,
            DepartureTime = reservation.Flight.DepartureTime,
            SeatNumber = reservation.SeatNumber,
            PurchaseDate = reservation.PurchaseDate
        };
    }

    public PdfResponse GetReservationPdf(string reservationNumber)
    {
        _logger.LogInformation("GetReservationPdf called: {ResNum}", reservationNumber);

        var reservation = _db.Reservations
            .Include(r => r.Flight)
            .FirstOrDefault(r => r.ReservationNumber == reservationNumber);

        if (reservation?.Flight == null)
        {
            return new PdfResponse
            {
                Success = false,
                Message = "Nie znaleziono rezerwacji."
            };
        }

        var pdfBytes = GeneratePdf(reservation);

        return new PdfResponse
        {
            Success = true,
            Message = "PDF wygenerowany pomyślnie.",
            PdfData = pdfBytes,
            FileName = $"bilet_{reservation.ReservationNumber}.pdf"
        };
    }

    private static byte[] GeneratePdf(Reservation reservation)
    {
        var flight = reservation.Flight!;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken3).Padding(15).Row(row =>
                    {
                        row.RelativeItem().Text("POTWIERDZENIE REZERWACJI BILETU LOTNICZEGO")
                            .FontSize(18).Bold().FontColor(Colors.White);
                    });
                    col.Item().Height(5);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(inner =>
                    {
                        inner.Spacing(4);
                        inner.Item().Text($"Numer rezerwacji: {reservation.ReservationNumber}")
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                        inner.Item().Text($"Data zakupu: {reservation.PurchaseDate:dd.MM.yyyy HH:mm}");
                    });

                    col.Item().PaddingTop(10).Text("DANE LOTU").FontSize(14).Bold();
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    col.Item().Padding(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Numer lotu:").Bold();
                        table.Cell().Text(flight.FlightNumber);

                        table.Cell().Text("Miasto wylotu:").Bold();
                        table.Cell().Text(flight.CityFrom);

                        table.Cell().Text("Miasto przylotu:").Bold();
                        table.Cell().Text(flight.CityTo);

                        table.Cell().Text("Data wylotu:").Bold();
                        table.Cell().Text(flight.DepartureDate.ToString("dd.MM.yyyy"));

                        table.Cell().Text("Godzina wylotu:").Bold();
                        table.Cell().Text(flight.DepartureTime);

                        table.Cell().Text("Cena:").Bold();
                        table.Cell().Text($"{flight.Price:F2} PLN");
                    });

                    col.Item().PaddingTop(10).Text("DANE PASAŻERA").FontSize(14).Bold();
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    col.Item().Padding(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Text("Imię i nazwisko:").Bold();
                        table.Cell().Text(reservation.PassengerName);

                        table.Cell().Text("Email:").Bold();
                        table.Cell().Text(reservation.PassengerEmail);

                        table.Cell().Text("Numer miejsca:").Bold();
                        table.Cell().Text(reservation.SeatNumber);
                    });

                    col.Item().PaddingTop(20).Background(Colors.Yellow.Lighten3).Padding(10).Text(
                        "Prosimy o przybycie na lotnisko min. 2 godziny przed wylotem. " +
                        "Niniejszy dokument stanowi potwierdzenie zakupu biletu.")
                        .FontSize(10).Italic();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("System Rezerwacji Biletów Lotniczych © 2026 | Strona ");
                    text.CurrentPageNumber();
                    text.Span(" z ");
                    text.TotalPages();
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private static string GenerateReservationNumber()
    {
        return $"RES-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
    }

    public FlightOperationResponse AddFlight(FlightAdminRequest request)
    {
        _logger.LogInformation("AddFlight called: {FlightNumber} {From}->{To}",
            request.FlightNumber, request.CityFrom, request.CityTo);

        var validation = ValidateFlightRequest(request);
        if (validation != null) return validation;

        var flight = new Flight
        {
            FlightNumber = request.FlightNumber.Trim(),
            CityFrom = request.CityFrom.Trim(),
            CityTo = request.CityTo.Trim(),
            DepartureDate = request.DepartureDate.Date,
            DepartureTime = request.DepartureTime.Trim(),
            Price = request.Price,
            AvailableSeats = request.AvailableSeats,
            PhotoData = request.PhotoData,
            PhotoFileName = request.PhotoFileName,
            PhotoContentType = request.PhotoContentType,
        };

        _db.Flights.Add(flight);
        _db.SaveChanges();

        _logger.LogInformation("Flight added: Id={Id}", flight.Id);

        return new FlightOperationResponse
        {
            Success = true,
            Message = $"Lot {flight.FlightNumber} dodany pomyślnie.",
            FlightId = flight.Id,
        };
    }

    public FlightOperationResponse UpdateFlight(FlightAdminRequest request)
    {
        _logger.LogInformation("UpdateFlight called: Id={Id}", request.Id);

        var flight = _db.Flights.Find(request.Id);
        if (flight == null)
        {
            return new FlightOperationResponse
            {
                Success = false,
                Message = "Nie znaleziono lotu o podanym ID.",
            };
        }

        var validation = ValidateFlightRequest(request);
        if (validation != null) return validation;

        flight.FlightNumber = request.FlightNumber.Trim();
        flight.CityFrom = request.CityFrom.Trim();
        flight.CityTo = request.CityTo.Trim();
        flight.DepartureDate = request.DepartureDate.Date;
        flight.DepartureTime = request.DepartureTime.Trim();
        flight.Price = request.Price;
        flight.AvailableSeats = request.AvailableSeats;

        if (request.RemovePhoto)
        {
            flight.PhotoData = null;
            flight.PhotoFileName = null;
            flight.PhotoContentType = null;
        }
        else if (request.PhotoData != null && request.PhotoData.Length > 0)
        {
            flight.PhotoData = request.PhotoData;
            flight.PhotoFileName = request.PhotoFileName;
            flight.PhotoContentType = request.PhotoContentType;
        }

        _db.SaveChanges();

        _logger.LogInformation("Flight updated: Id={Id}", flight.Id);

        return new FlightOperationResponse
        {
            Success = true,
            Message = $"Lot {flight.FlightNumber} zaktualizowany.",
            FlightId = flight.Id,
        };
    }

    public FlightOperationResponse DeleteFlight(int flightId)
    {
        _logger.LogInformation("DeleteFlight called: Id={Id}", flightId);

        var flight = _db.Flights.Find(flightId);
        if (flight == null)
        {
            return new FlightOperationResponse
            {
                Success = false,
                Message = "Nie znaleziono lotu o podanym ID.",
            };
        }

        var hasReservations = _db.Reservations.Any(r => r.FlightId == flightId);
        if (hasReservations)
        {
            return new FlightOperationResponse
            {
                Success = false,
                Message = "Nie można usunąć lotu — istnieją powiązane rezerwacje.",
                FlightId = flightId,
            };
        }

        _db.Flights.Remove(flight);
        _db.SaveChanges();

        _logger.LogInformation("Flight deleted: Id={Id}", flightId);

        return new FlightOperationResponse
        {
            Success = true,
            Message = $"Lot {flight.FlightNumber} usunięty.",
            FlightId = flightId,
        };
    }

    public Flight? GetFlight(int flightId)
    {
        _logger.LogInformation("GetFlight called: Id={Id}", flightId);
        var flight = _db.Flights.Find(flightId);
        return flight == null ? null : StripPhotoBytes(flight);
    }

    public FlightPhotoResponse GetFlightPhoto(int flightId)
    {
        _logger.LogInformation("GetFlightPhoto called: Id={Id}", flightId);

        var flight = _db.Flights
            .AsNoTracking()
            .FirstOrDefault(f => f.Id == flightId);

        if (flight == null)
        {
            return new FlightPhotoResponse
            {
                Success = false,
                Message = "Nie znaleziono lotu o podanym ID.",
            };
        }

        if (flight.PhotoData == null || flight.PhotoData.Length == 0)
        {
            return new FlightPhotoResponse
            {
                Success = false,
                Message = "Lot nie posiada zdjęcia.",
            };
        }

        return new FlightPhotoResponse
        {
            Success = true,
            Message = "OK",
            PhotoData = flight.PhotoData,
            FileName = flight.PhotoFileName,
            ContentType = flight.PhotoContentType ?? "application/octet-stream",
        };
    }

    private static FlightOperationResponse? ValidateFlightRequest(FlightAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FlightNumber))
            return Fail("Numer lotu jest wymagany.");
        if (string.IsNullOrWhiteSpace(request.CityFrom))
            return Fail("Miasto wylotu jest wymagane.");
        if (string.IsNullOrWhiteSpace(request.CityTo))
            return Fail("Miasto przylotu jest wymagane.");
        if (string.IsNullOrWhiteSpace(request.DepartureTime))
            return Fail("Godzina wylotu jest wymagana.");
        if (request.Price < 0)
            return Fail("Cena nie może być ujemna.");
        if (request.AvailableSeats < 0)
            return Fail("Liczba wolnych miejsc nie może być ujemna.");
        return null;

        static FlightOperationResponse Fail(string msg) => new()
        {
            Success = false,
            Message = msg,
        };
    }
}
