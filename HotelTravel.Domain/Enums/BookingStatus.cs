namespace HotelTravel.Domain.Enums;

public enum BookingStatus
{
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled,

    // Otel hələ təsdiqləməyib (sorğu). Sonda durur ki, bazadakı köhnə rəqəmlər dəyişməsin.
    Pending
}