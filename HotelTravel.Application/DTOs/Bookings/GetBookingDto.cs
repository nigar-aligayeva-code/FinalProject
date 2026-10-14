using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Bookings;

public class GetBookingDto
{
    public int Id { get; set; }

    public string ConfirmationCode { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    public int AdultCount { get; set; }
    public int ChildrenCount { get; set; }

    public decimal TotalPrice { get; set; }

    // Endirim
    public decimal OriginalPrice { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountNote { get; set; }

    public BookingStatus Status { get; set; }

    public int RoomId { get; set; }
    public string RoomName { get; set; }
    public string RoomNumber { get; set; }
    public string? RoomImage { get; set; }

    // Voucher üçün otel məlumatı
    public string? HotelName { get; set; }
    public string? HotelAddress { get; set; }
    public string? HotelCity { get; set; }
    public string? HotelCountry { get; set; }
    public string? HotelPhone { get; set; }

    public int GuestId { get; set; }
    public string GuestName { get; set; }
    public string GuestEmail { get; set; }

    // Bu rezervasiya üçün rəy yazılıbmı?
    public bool HasReview { get; set; }
}
