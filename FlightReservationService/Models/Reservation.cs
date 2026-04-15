using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class Reservation
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string ReservationNumber { get; set; } = string.Empty;

    [DataMember]
    public int FlightId { get; set; }

    [DataMember]
    public string PassengerName { get; set; } = string.Empty;

    [DataMember]
    public string PassengerEmail { get; set; } = string.Empty;

    [DataMember]
    public DateTime PurchaseDate { get; set; }

    [DataMember]
    public string SeatNumber { get; set; } = string.Empty;

    public Flight? Flight { get; set; }
}
