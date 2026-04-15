using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class Flight
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string CityFrom { get; set; } = string.Empty;

    [DataMember]
    public string CityTo { get; set; } = string.Empty;

    [DataMember]
    public DateTime DepartureDate { get; set; }

    [DataMember]
    public string DepartureTime { get; set; } = string.Empty;

    [DataMember]
    public decimal Price { get; set; }

    [DataMember]
    public int AvailableSeats { get; set; }

    [DataMember]
    public string FlightNumber { get; set; } = string.Empty;
}
