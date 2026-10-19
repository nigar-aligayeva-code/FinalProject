using HotelTravel.Application.DTOs.Trips;

namespace HotelTravel.Web.ViewModels;

public class TripsIndexViewModel
{
    public TripSearchDto Search { get; set; } = new();

    public List<string> Origins { get; set; } = new();
    public List<string> Destinations { get; set; } = new();

    // Axtarış edilibsə
    public TripPlanDto? Plan { get; set; }

    public List<TripPackageDto> MyTrips { get; set; } = new();

    // Əlaqə forması üçün (daxil olmuş istifadəçidən)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
