using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class TicketPurchaseRequest
{
    [DataMember]
    public int FlightId { get; set; }

    [DataMember]
    public string PassengerName { get; set; } = string.Empty;

    [DataMember]
    public string PassengerEmail { get; set; } = string.Empty;
}
