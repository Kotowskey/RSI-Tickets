using System.Runtime.Serialization;

namespace FlightReservationService.Models;

[DataContract]
public class PdfResponse
{
    [DataMember]
    public bool Success { get; set; }

    [DataMember]
    public string Message { get; set; } = string.Empty;

    [DataMember]
    public byte[]? PdfData { get; set; }

    [DataMember]
    public string? FileName { get; set; }
}
