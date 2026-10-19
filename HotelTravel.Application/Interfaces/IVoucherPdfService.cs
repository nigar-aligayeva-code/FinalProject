using HotelTravel.Application.DTOs.Bookings;

namespace HotelTravel.Application.Interfaces;

// Rezervasiya voucher-i PDF kimi (QR kod ilə) - Infrastructure-da
public interface IVoucherPdfService
{
    byte[] Create(GetBookingDto booking, string bookingUrl);
}
