using HotelTravel.Application.DTOs.Profile;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

// İstifadəçinin bütün xidmətlərdəki sifarişlərindən statistika
public class ProfileService : IProfileService
{
    private readonly IAppDbContext _context;

    public ProfileService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileSummaryDto> GetSummaryAsync(string userId)
    {
        var now = DateTime.Now;

        var hotels = await _context.Bookings
            .Where(b => !b.IsDeleted && b.AppUserId == userId)
            .Select(b => new
            {
                b.TotalPrice,
                b.Status,
                b.CheckInDate,
                b.CheckOutDate,
                HotelName = b.Room.Hotel.Name,
                City = b.Room.Hotel.City
            })
            .ToListAsync();

        var cars = await _context.CarRentals
            .Where(r => !r.IsDeleted && r.AppUserId == userId)
            .Select(r => new { r.TotalPrice, r.Status, r.PickupDate })
            .ToListAsync();

        var taxis = await _context.TaxiBookings
            .Where(t => !t.IsDeleted && t.AppUserId == userId)
            .Select(t => new { t.Price, t.Status, t.PickupTime, t.City })
            .ToListAsync();

        var flights = await _context.FlightBookings
            .Where(f => !f.IsDeleted && f.AppUserId == userId)
            .Select(f => new { f.TotalPrice, f.Status, f.DepartureTime, f.FromCity, f.ToCity })
            .ToListAsync();

        var saved = await _context.WishlistItems
            .CountAsync(w => w.AppUserId == userId);

        var activeHotels = hotels.Where(b => b.Status != BookingStatus.Cancelled).ToList();
        var activeCars = cars.Where(r => r.Status != CarRentalStatus.Cancelled).ToList();
        var activeTaxis = taxis.Where(t => t.Status != TaxiBookingStatus.Cancelled).ToList();
        var activeFlights = flights.Where(f => f.Status != FlightBookingStatus.Cancelled).ToList();

        var summary = new ProfileSummaryDto
        {
            HotelBookings = hotels.Count,
            CarRentals = cars.Count,
            TaxiBookings = taxis.Count,
            FlightBookings = flights.Count,
            SavedRooms = saved,

            TotalSpent =
                activeHotels.Sum(b => b.TotalPrice) +
                activeCars.Sum(r => r.TotalPrice) +
                activeTaxis.Sum(t => t.Price) +
                activeFlights.Sum(f => f.TotalPrice),

            NightsBooked = activeHotels.Sum(b => Math.Max(0, (b.CheckOutDate.Date - b.CheckInDate.Date).Days)),

            CitiesVisited = activeHotels.Select(b => b.City)
                .Concat(activeFlights.Select(f => f.ToCity))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
        };

        // Gələcək səfərlər: hamısını bir siyahıya yığıb ən yaxınını tapırıq
        var upcoming = activeHotels
            .Where(b => b.CheckInDate.Date >= now.Date)
            .Select(b => (Date: b.CheckInDate, Title: $"{b.HotelName}, {b.City}"))
            .Concat(activeFlights
                .Where(f => f.DepartureTime >= now)
                .Select(f => (Date: f.DepartureTime, Title: $"Flight {f.FromCity} → {f.ToCity}")))
            .Concat(activeCars
                .Where(r => r.PickupDate.Date >= now.Date)
                .Select(r => (Date: r.PickupDate, Title: "Car rental pickup")))
            .Concat(activeTaxis
                .Where(t => t.PickupTime >= now)
                .Select(t => (Date: t.PickupTime, Title: $"Airport taxi in {t.City}")))
            .OrderBy(x => x.Date)
            .ToList();

        summary.UpcomingTrips = upcoming.Count;

        if (upcoming.Count > 0)
        {
            summary.NextTripTitle = upcoming[0].Title;
            summary.NextTripDate = upcoming[0].Date;
        }

        summary.Level = ProfileRules.GetLevel(activeHotels.Count + activeFlights.Count, summary.CitiesVisited);

        return summary;
    }
}
