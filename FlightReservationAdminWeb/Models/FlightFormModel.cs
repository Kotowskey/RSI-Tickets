using System.ComponentModel.DataAnnotations;

namespace FlightReservationAdminWeb.Models;

public class FlightFormModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Numer lotu jest wymagany.")]
    [StringLength(10, ErrorMessage = "Numer lotu może mieć maksymalnie 10 znaków.")]
    [Display(Name = "Numer lotu")]
    public string FlightNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Miasto wylotu jest wymagane.")]
    [StringLength(100)]
    [Display(Name = "Miasto wylotu")]
    public string CityFrom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Miasto przylotu jest wymagane.")]
    [StringLength(100)]
    [Display(Name = "Miasto przylotu")]
    public string CityTo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data wylotu jest wymagana.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data wylotu")]
    public DateTime DepartureDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Godzina wylotu jest wymagana.")]
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Format godziny: HH:MM (np. 08:30).")]
    [Display(Name = "Godzina wylotu")]
    public string DepartureTime { get; set; } = "08:00";

    [Required]
    [Range(0, 100000, ErrorMessage = "Cena musi być między 0 a 100000.")]
    [Display(Name = "Cena (PLN)")]
    public decimal Price { get; set; }

    [Required]
    [Range(0, 1000, ErrorMessage = "Liczba miejsc musi być między 0 a 1000.")]
    [Display(Name = "Wolne miejsca")]
    public int AvailableSeats { get; set; }

    [Display(Name = "Zdjęcie (PNG / JPG)")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "Usuń obecne zdjęcie")]
    public bool RemovePhoto { get; set; }

    public bool HasExistingPhoto { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(CityFrom) && !string.IsNullOrWhiteSpace(CityTo)
            && string.Equals(CityFrom.Trim(), CityTo.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Miasto wylotu i przylotu muszą się różnić.",
                [nameof(CityFrom), nameof(CityTo)]);
        }

        if (DepartureDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Data wylotu nie może być w przeszłości.",
                [nameof(DepartureDate)]);
        }
    }
}
