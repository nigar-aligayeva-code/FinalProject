namespace HotelTravel.Domain.Entities;

// Gündəlik reys cədvəli (məs. J2 8011 Bakı -> İstanbul, hər gün 08:00).
// Konkret uçuş = marşrut + tarix.
public class FlightRoute : BaseEntity
{
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;

    public string FromCity { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;

    public string ToCity { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;

    // Yerli uçuş vaxtı (hər gün)
    public TimeSpan DepartureTime { get; set; }

    public int DurationMinutes { get; set; }

    // Bir sərnişin üçün baza qiymət (AZN)
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }

    // Hər uçuşda yer sayı (Business 0 = yoxdur)
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<FlightBooking> Bookings { get; set; }
        = new List<FlightBooking>();
}
