using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class FlightOperationResponse
{
    [DataMember]
    public bool Success { get; set; }

    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public int FlightId { get; set; }
}
