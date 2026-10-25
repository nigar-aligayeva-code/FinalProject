using HotelTravel.Domain.Enums;

namespace HotelTravel.Domain.Entities;

public class Booking : BaseEntity
{
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    public int AdultCount { get; set; }
    public int ChildrenCount { get; set; }

    public decimal TotalPrice { get; set; }

    // ENDIRİM (TotalPrice = OriginalPrice - DiscountAmount)
    public decimal OriginalPrice { get; set; }
    public int DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountNote { get; set; }
    public string? PromoCode { get; set; }
    public string ConfirmationCode { get; set; }

    public BookingStatus Status { get; set; }

    // Onlayn ödəniş (Stripe Checkout, test rejimi)
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }

    // Stripe Checkout session id (cs_test_...): ödənişi yoxlamaq üçün
    public string? PaymentReference { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; }

    public int GuestId { get; set; }
    public Guest Guest { get; set; }

    // REVIEW (CheckedOut olduqdan sonra)
    public Review? Review { get; set; }

    // Rezervasiyanı daxil olmuş istifadəçi edibsə (My Bookings üçün)
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
