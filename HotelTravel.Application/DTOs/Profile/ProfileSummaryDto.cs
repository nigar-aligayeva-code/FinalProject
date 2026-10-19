namespace HotelTravel.Application.DTOs.Profile;

// Profil səhifəsindəki "səyahət statistikası"
public class ProfileSummaryDto
{
    public int HotelBookings { get; set; }
    public int CarRentals { get; set; }
    public int TaxiBookings { get; set; }
    public int FlightBookings { get; set; }

    public int TotalTrips => HotelBookings + CarRentals + TaxiBookings + FlightBookings;

    // Gələcək (ləğv olunmayan) sifarişlər
    public int UpcomingTrips { get; set; }

    // Ləğv olunmayan sifarişlərin cəmi
    public decimal TotalSpent { get; set; }

    public int NightsBooked { get; set; }
    public int CitiesVisited { get; set; }
    public int SavedRooms { get; set; }

    // Ən yaxın səfər (yoxdursa null)
    public string? NextTripTitle { get; set; }
    public DateTime? NextTripDate { get; set; }

    // Statusa görə səviyyə (Explorer / Traveler / Globetrotter)
    public string Level { get; set; } = string.Empty;
}
