using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Domain.Enums;

namespace HotelTravel.Web.Models;

// Otel partnyorunun ana səhifəsi
public class PartnerDashboardViewModel
{
    public PartnerMeDto Me { get; set; } = new();

    public HotelAvailabilityDto Overview { get; set; } = new();

    public List<GetBookingDto> Bookings { get; set; } = new();

    // Gözləyən sorğu -> yoxlama (üst-üstə düşmə, bağlı günlər)
    public Dictionary<int, AvailabilityCheckDto> Checks { get; set; } = new();

    public BookingStatus? Status { get; set; }
}
