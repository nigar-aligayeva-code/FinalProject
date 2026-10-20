using System.Globalization;
using HotelTravel.Application.DTOs.Notifications;

namespace HotelTravel.Application.Services;

// Bildiriş mətnləri (unit test üçün ayrıca). Qonaqlara gedən mesajda şəxsi məlumat olmur.
public static class ActivityMessages
{
    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    public static string Money(decimal amount) => amount.ToString("N2", En) + " AZN";

    // Admin paneldə konkret sifarişi açan linklər
    public static string BookingUrl(int bookingId) => $"/Admin/Bookings/Details/{bookingId}";

    // Detal səhifəsi olmayanlar: siyahı açılır və həmin sətir vurğulanır
    public static string HighlightUrl(string page, string code) => $"{page}?highlight={Uri.EscapeDataString(code)}";

    public static LiveNotificationDto HotelBooked(int bookingId, string code, string guest, string hotel, string city, int nights, decimal total)
        => new()
        {
            Type = "hotel",
            Title = "New hotel booking",
            Message = $"{guest} booked {hotel} ({city}) for {nights} night{(nights == 1 ? "" : "s")} · {Money(total)}",
            Amount = total,
            Code = code,
            Url = BookingUrl(bookingId)
        };

    public static LiveNotificationDto HotelCancelled(int bookingId, string code, string guest, string hotel)
        => new()
        {
            Type = "cancel",
            Title = "Booking cancelled",
            Message = $"{guest} cancelled {code} at {hotel}",
            Code = code,
            Url = BookingUrl(bookingId)
        };

    public static LiveNotificationDto FlightBooked(string code, string from, string to, int passengers, decimal total)
        => new()
        {
            Type = "flight",
            Title = "New flight booking",
            Message = $"{from} → {to} · {passengers} passenger{(passengers == 1 ? "" : "s")} · {Money(total)}",
            Amount = total,
            Code = code,
            Url = HighlightUrl("/Admin/Flights", code)
        };

    public static LiveNotificationDto TaxiBooked(string code, string city, DateTime pickup, decimal price)
        => new()
        {
            Type = "taxi",
            Title = "New airport taxi",
            Message = $"{city} · pick-up {pickup.ToString("dd MMM HH:mm", En)} · {Money(price)}",
            Amount = price,
            Code = code,
            Url = HighlightUrl("/Admin/Taxis", code)
        };

    public static LiveNotificationDto CarRented(string code, string model, string city, int days, decimal total)
        => new()
        {
            Type = "car",
            Title = "New car rental",
            Message = $"{model} in {city} for {days} day{(days == 1 ? "" : "s")} · {Money(total)}",
            Amount = total,
            Code = code,
            Url = HighlightUrl("/Admin/Cars", code)
        };

    // Paketin otel hissəsinin səhifəsi açılır
    public static LiveNotificationDto TripBooked(int hotelBookingId, string code, string from, string to, int travelers, decimal total, int discountPercent)
        => new()
        {
            Type = "trip",
            Title = "New trip package",
            Message = $"{from} → {to} · {travelers} traveler{(travelers == 1 ? "" : "s")} · {Money(total)} (-{discountPercent}%)",
            Amount = total,
            Code = code,
            Url = BookingUrl(hotelBookingId)
        };

    // Qonaqlar üçün: ad, email, kod yoxdur
    public static PublicActivityDto PublicFor(string type, string? city)
    {
        var place = string.IsNullOrWhiteSpace(city) ? "" : $" in {city}";

        var message = type switch
        {
            "hotel" => $"Someone just booked a stay{place}",
            "flight" => $"Someone just booked a flight to {city}",
            "taxi" => $"Someone just booked an airport taxi{place}",
            "car" => $"Someone just rented a car{place}",
            "trip" => $"Someone just booked a trip package to {city}",
            _ => "Someone just made a booking"
        };

        return new PublicActivityDto { Type = type, Message = message, City = city };
    }

    // "Nigar Aliyeva" -> "Nigar A." (admin üçün qısaldılmış ad)
    public static string ShortName(string? first, string? last)
    {
        var f = (first ?? "").Trim();
        var l = (last ?? "").Trim();

        if (f.Length == 0 && l.Length == 0)
            return "A guest";

        return l.Length == 0 ? f : $"{f} {l[0]}.";
    }
}
