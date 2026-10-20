using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelTravel.Application.Services;

// Sifariş məlumatını bazadan oxuyub admin-lərə və qonaqlara canlı mesaj göndərir
public class ActivityNotifier : IActivityNotifier
{
    private readonly IAppDbContext _context;
    private readonly IRealtimePublisher _publisher;
    private readonly ILogger<ActivityNotifier> _logger;

    public ActivityNotifier(
        IAppDbContext context,
        IRealtimePublisher publisher,
        ILogger<ActivityNotifier> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public Task HotelBookedAsync(string confirmationCode) => SafeAsync(async () =>
    {
        var b = await _context.Bookings
            .Where(x => x.ConfirmationCode == confirmationCode)
            .Select(x => new
            {
                x.Id,
                x.Guest.FirstName,
                x.Guest.LastName,
                Hotel = x.Room.Hotel.Name,
                x.Room.Hotel.City,
                x.CheckInDate,
                x.CheckOutDate,
                x.TotalPrice
            })
            .FirstOrDefaultAsync();

        if (b == null) return;

        var nights = Math.Max(1, (b.CheckOutDate.Date - b.CheckInDate.Date).Days);

        await PublishAsync(
            ActivityMessages.HotelBooked(b.Id, confirmationCode, ActivityMessages.ShortName(b.FirstName, b.LastName), b.Hotel, b.City, nights, b.TotalPrice),
            b.City);
    });

    public Task HotelCancelledAsync(string confirmationCode) => SafeAsync(async () =>
    {
        var b = await _context.Bookings
            .Where(x => x.ConfirmationCode == confirmationCode)
            .Select(x => new { x.Id, x.Guest.FirstName, x.Guest.LastName, Hotel = x.Room.Hotel.Name })
            .FirstOrDefaultAsync();

        if (b == null) return;

        // Ləğv yalnız admin-lərə gedir
        await _publisher.ToAdminsAsync(
            ActivityMessages.HotelCancelled(b.Id, confirmationCode, ActivityMessages.ShortName(b.FirstName, b.LastName), b.Hotel));
    });

    public Task FlightBookedAsync(string confirmationCode) => SafeAsync(async () =>
    {
        var f = await _context.FlightBookings
            .Where(x => x.ConfirmationCode == confirmationCode)
            .Select(x => new { x.FromCity, x.ToCity, x.Passengers, x.TotalPrice })
            .FirstOrDefaultAsync();

        if (f == null) return;

        await PublishAsync(
            ActivityMessages.FlightBooked(confirmationCode, f.FromCity, f.ToCity, f.Passengers, f.TotalPrice),
            f.ToCity);
    });

    public Task TaxiBookedAsync(string confirmationCode) => SafeAsync(async () =>
    {
        var t = await _context.TaxiBookings
            .Where(x => x.ConfirmationCode == confirmationCode)
            .Select(x => new { x.City, x.PickupTime, x.Price })
            .FirstOrDefaultAsync();

        if (t == null) return;

        await PublishAsync(ActivityMessages.TaxiBooked(confirmationCode, t.City, t.PickupTime, t.Price), t.City);
    });

    public Task CarRentedAsync(string confirmationCode) => SafeAsync(async () =>
    {
        var c = await _context.CarRentals
            .Where(x => x.ConfirmationCode == confirmationCode)
            .Select(x => new { x.RentalCar.Model, x.RentalCar.City, x.Days, x.TotalPrice })
            .FirstOrDefaultAsync();

        if (c == null) return;

        await PublishAsync(ActivityMessages.CarRented(confirmationCode, c.Model, c.City, c.Days, c.TotalPrice), c.City);
    });

    public Task TripBookedAsync(string packageCode) => SafeAsync(async () =>
    {
        var p = await _context.TripPackages
            .Where(x => x.Code == packageCode)
            .Select(x => new { x.FromCity, x.ToCity, x.Travelers, x.Total, x.DiscountPercent, x.HotelBookingCode })
            .FirstOrDefaultAsync();

        if (p == null) return;

        var hotelBookingId = await _context.Bookings
            .Where(b => b.ConfirmationCode == p.HotelBookingCode)
            .Select(b => b.Id)
            .FirstOrDefaultAsync();

        await PublishAsync(
            ActivityMessages.TripBooked(hotelBookingId, packageCode, p.FromCity, p.ToCity, p.Travelers, p.Total, p.DiscountPercent),
            p.ToCity);
    });

    private async Task PublishAsync(LiveNotificationDto notification, string? city)
    {
        await _publisher.ToAdminsAsync(notification);
        await _publisher.ToEveryoneAsync(ActivityMessages.PublicFor(notification.Type, city));
    }

    private async Task SafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live notification could not be sent.");
        }
    }
}
