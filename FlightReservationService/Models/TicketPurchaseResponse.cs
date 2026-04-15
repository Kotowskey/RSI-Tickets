using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class TicketPurchaseResponse
{
    [DataMember]
    public bool Success { get; set; }

    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public string? ReservationNumber { get; set; }
}
