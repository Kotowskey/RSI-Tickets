using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class ReservationDetails
{
    [DataMember]
    public bool Found { get; set; }

    [DataMember]
    public string? ReservationNumber { get; set; }

    [DataMember]
    public string? PassengerName { get; set; }

    [DataMember]
    public string? PassengerEmail { get; set; }

    [DataMember]
    public string? FlightNumber { get; set; }

    [DataMember]
    public string? CityFrom { get; set; }

    [DataMember]
    public string? CityTo { get; set; }

    [DataMember]
    public DateTime? DepartureDate { get; set; }

    [DataMember]
    public string? DepartureTime { get; set; }

    [DataMember]
    public string? SeatNumber { get; set; }

    [DataMember]
    public DateTime? PurchaseDate { get; set; }
}
