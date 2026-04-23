using System.ServiceModel;
using FlightReservationService.Models;

namespace FlightReservationService.Services;

[ServiceContract(Namespace = "http://flightreservation.example.com/")]
public interface IFlightReservationService
{
    [OperationContract]
    List<Flight> GetAllFlights();

    [OperationContract]
    List<Flight> SearchFlights(FlightSearchRequest request);

    [OperationContract]
    TicketPurchaseResponse BuyTicket(TicketPurchaseRequest request);

    [OperationContract]
    ReservationDetails CheckReservation(string reservationNumber);

    [OperationContract]
    PdfResponse GetReservationPdf(string reservationNumber);

    [OperationContract]
    FlightOperationResponse AddFlight(FlightAdminRequest request);

    [OperationContract]
    FlightOperationResponse UpdateFlight(FlightAdminRequest request);

    [OperationContract]
    FlightOperationResponse DeleteFlight(int flightId);

    [OperationContract]
    Flight? GetFlight(int flightId);

    [OperationContract]
    FlightPhotoResponse GetFlightPhoto(int flightId);
}
