using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class FlightSearchRequest
{
    [DataMember]
    public string? CityFrom { get; set; }

    [DataMember]
    public string? CityTo { get; set; }

    [DataMember]
    public DateTime? Date { get; set; }
}
