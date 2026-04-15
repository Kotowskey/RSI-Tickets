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
        return _db.Flights.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime).ToList();
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

        return query.OrderBy(f => f.DepartureDate).ThenBy(f => f.DepartureTime).ToList();
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
}
