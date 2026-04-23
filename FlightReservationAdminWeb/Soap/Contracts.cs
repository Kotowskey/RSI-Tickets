using System.Runtime.Serialization;
using System.ServiceModel;

namespace FlightReservationAdminWeb.Soap;

internal static class Ns
{
    public const string Models = "http://schemas.datacontract.org/2004/07/FlightReservationService.Models";
    public const string Service = "http://flightreservation.example.com/";
}

[DataContract(Name = "Flight", Namespace = Ns.Models)]
public class Flight
{
    [DataMember] public int Id { get; set; }
    [DataMember] public string CityFrom { get; set; } = string.Empty;
    [DataMember] public string CityTo { get; set; } = string.Empty;
    [DataMember] public DateTime DepartureDate { get; set; }
    [DataMember] public string DepartureTime { get; set; } = string.Empty;
    [DataMember] public decimal Price { get; set; }
    [DataMember] public int AvailableSeats { get; set; }
    [DataMember] public string FlightNumber { get; set; } = string.Empty;
    [DataMember] public bool HasPhoto { get; set; }
}

[DataContract(Name = "FlightSearchRequest", Namespace = Ns.Models)]
public class FlightSearchRequest
{
    [DataMember] public string? CityFrom { get; set; }
    [DataMember] public string? CityTo { get; set; }
    [DataMember] public DateTime? Date { get; set; }
}

[DataContract(Name = "TicketPurchaseRequest", Namespace = Ns.Models)]
public class TicketPurchaseRequest
{
    [DataMember] public int FlightId { get; set; }
    [DataMember] public string PassengerName { get; set; } = string.Empty;
    [DataMember] public string PassengerEmail { get; set; } = string.Empty;
}

[DataContract(Name = "TicketPurchaseResponse", Namespace = Ns.Models)]
public class TicketPurchaseResponse
{
    [DataMember] public bool Success { get; set; }
    [DataMember] public string Message { get; set; } = string.Empty;
    [DataMember] public string? ReservationNumber { get; set; }
}

[DataContract(Name = "ReservationDetails", Namespace = Ns.Models)]
public class ReservationDetails
{
    [DataMember] public bool Found { get; set; }
    [DataMember] public string ReservationNumber { get; set; } = string.Empty;
    [DataMember] public string PassengerName { get; set; } = string.Empty;
    [DataMember] public string PassengerEmail { get; set; } = string.Empty;
    [DataMember] public string FlightNumber { get; set; } = string.Empty;
    [DataMember] public string CityFrom { get; set; } = string.Empty;
    [DataMember] public string CityTo { get; set; } = string.Empty;
    [DataMember] public DateTime DepartureDate { get; set; }
    [DataMember] public string DepartureTime { get; set; } = string.Empty;
    [DataMember] public string SeatNumber { get; set; } = string.Empty;
    [DataMember] public DateTime PurchaseDate { get; set; }
}

[DataContract(Name = "PdfResponse", Namespace = Ns.Models)]
public class PdfResponse
{
    [DataMember] public bool Success { get; set; }
    [DataMember] public string Message { get; set; } = string.Empty;
    [DataMember] public byte[]? PdfData { get; set; }
    [DataMember] public string? FileName { get; set; }
}

[DataContract(Name = "FlightAdminRequest", Namespace = Ns.Models)]
public class FlightAdminRequest
{
    [DataMember] public int Id { get; set; }
    [DataMember] public string FlightNumber { get; set; } = string.Empty;
    [DataMember] public string CityFrom { get; set; } = string.Empty;
    [DataMember] public string CityTo { get; set; } = string.Empty;
    [DataMember] public DateTime DepartureDate { get; set; }
    [DataMember] public string DepartureTime { get; set; } = string.Empty;
    [DataMember] public decimal Price { get; set; }
    [DataMember] public int AvailableSeats { get; set; }
    [DataMember] public byte[]? PhotoData { get; set; }
    [DataMember] public string? PhotoFileName { get; set; }
    [DataMember] public string? PhotoContentType { get; set; }
    [DataMember] public bool RemovePhoto { get; set; }
}

[DataContract(Name = "FlightOperationResponse", Namespace = Ns.Models)]
public class FlightOperationResponse
{
    [DataMember] public bool Success { get; set; }
    [DataMember] public string Message { get; set; } = string.Empty;
    [DataMember] public int FlightId { get; set; }
}

[DataContract(Name = "FlightPhotoResponse", Namespace = Ns.Models)]
public class FlightPhotoResponse
{
    [DataMember] public bool Success { get; set; }
    [DataMember] public string Message { get; set; } = string.Empty;
    [DataMember] public byte[]? PhotoData { get; set; }
    [DataMember] public string? FileName { get; set; }
    [DataMember] public string? ContentType { get; set; }
}

[ServiceContract(Namespace = Ns.Service)]
public interface IFlightReservationService
{
    [OperationContract] List<Flight> GetAllFlights();
    [OperationContract] List<Flight> SearchFlights(FlightSearchRequest request);
    [OperationContract] TicketPurchaseResponse BuyTicket(TicketPurchaseRequest request);
    [OperationContract] ReservationDetails CheckReservation(string reservationNumber);
    [OperationContract] PdfResponse GetReservationPdf(string reservationNumber);
    [OperationContract] FlightOperationResponse AddFlight(FlightAdminRequest request);
    [OperationContract] FlightOperationResponse UpdateFlight(FlightAdminRequest request);
    [OperationContract] FlightOperationResponse DeleteFlight(int flightId);
    [OperationContract] Flight? GetFlight(int flightId);
    [OperationContract] FlightPhotoResponse GetFlightPhoto(int flightId);
}
