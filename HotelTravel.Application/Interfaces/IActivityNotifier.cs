using HotelTravel.Application.DTOs.Notifications;

namespace HotelTravel.Application.Interfaces;

// Biznes hadisələri -> canlı bildiriş. Heç vaxt xəta atmır (bildiriş alınmasa sifariş pozulmur).
public interface IActivityNotifier
{
    Task HotelBookedAsync(string confirmationCode);
    Task HotelCancelledAsync(string confirmationCode);
    Task FlightBookedAsync(string confirmationCode);
    Task TaxiBookedAsync(string confirmationCode);
    Task CarRentedAsync(string confirmationCode);
    Task TripBookedAsync(string packageCode);

    // Qonağın özünə (🔔): "requested", "confirmed", "rejected", "cancelled" (otel), "cancelled_by_you"
    Task GuestBookingUpdateAsync(string confirmationCode, string kind);

    // Otel partnyorlarına: yeni rezervasiya sorğusu gəldi (🔔)
    Task PartnerNewRequestAsync(string confirmationCode);

    // Qonaq öz uçuşunu ləğv etdi (🔔-də qeyd, cərimə ilə)
    Task GuestFlightCancelledAsync(int flightBookingId);

    // Qonağın özünə: xatırlatma (giriş və ya uçuş yaxınlaşır)
    Task ToGuestAsync(string? appUserId, GuestNotificationDto notification);
}
