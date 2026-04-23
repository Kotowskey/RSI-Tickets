using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class FlightPhotoResponse
{
    [DataMember]
    public bool Success { get; set; }

    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public byte[]? PhotoData { get; set; }

    [DataMember]
    public string? FileName { get; set; }

    [DataMember]
    public string? ContentType { get; set; }
}
