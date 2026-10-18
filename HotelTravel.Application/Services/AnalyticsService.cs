using System.Globalization;
using HotelTravel.Application.DTOs.Analytics;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAppDbContext _context;

    public AnalyticsService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsDto> GetAsync()
    {
        var today = DateTime.Today;

        // =========================
        // MƏLUMAT (yalnız lazım olan sahələr)
        // =========================
        var hotel = await _context.Bookings
            .Where(b => !b.IsDeleted)
            .Select(b => new
            {
                b.TotalPrice,
                b.Status,
                b.CheckInDate,
                b.CheckOutDate,
                b.CreatedAt,
                b.DiscountAmount,
                b.RoomId,
                RoomName = b.Room.Name,
                HotelName = b.Room.Hotel.Name,
                City = b.Room.Hotel.City
            })
            .ToListAsync();

        var cars = await _context.CarRentals
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.TotalPrice, r.Status, r.PickupDate })
            .ToListAsync();

        var taxis = await _context.TaxiBookings
            .Where(t => !t.IsDeleted)
            .Select(t => new { t.Price, t.Status, t.PickupTime })
            .ToListAsync();

        var flights = await _context.FlightBookings
            .Where(f => !f.IsDeleted)
            .Select(f => new { f.TotalPrice, f.Status, f.DepartureTime })
            .ToListAsync();

        var reviews = await _context.Reviews
            .Where(r => !r.IsDeleted)
            .Select(r => new { r.Rating, HotelName = r.Room.Hotel.Name, City = r.Room.Hotel.City })
            .ToListAsync();

        var roomCount = await _context.Rooms
            .CountAsync(r => !r.IsDeleted && !r.Hotel.IsDeleted);

        // Ləğv olunmayanlar gəlirə daxildir
        var activeHotel = hotel.Where(b => b.Status != BookingStatus.Cancelled).ToList();
        var activeCars = cars.Where(r => r.Status != CarRentalStatus.Cancelled).ToList();
        var activeTaxis = taxis.Where(t => t.Status != TaxiBookingStatus.Cancelled).ToList();
        var activeFlights = flights.Where(f => f.Status != FlightBookingStatus.Cancelled).ToList();

        var dto = new AnalyticsDto
        {
            GeneratedAt = DateTime.Now,

            HotelRevenue = activeHotel.Sum(b => b.TotalPrice),
            CarRevenue = activeCars.Sum(r => r.TotalPrice),
            TaxiRevenue = activeTaxis.Sum(t => t.Price),
            FlightRevenue = activeFlights.Sum(f => f.TotalPrice),

            HotelBookings = hotel.Count,
            CarRentals = cars.Count,
            TaxiBookings = taxis.Count,
            FlightBookings = flights.Count,

            CancellationRate = AnalyticsCalculator.Percent(
                hotel.Count(b => b.Status == BookingStatus.Cancelled),
                hotel.Count),

            AverageBookingValue = activeHotel.Count == 0
                ? 0
                : Math.Round(activeHotel.Average(b => b.TotalPrice), 2),

            AverageStayNights = activeHotel.Count == 0
                ? 0
                : Math.Round(activeHotel.Average(b => (b.CheckOutDate.Date - b.CheckInDate.Date).TotalDays), 1),

            OccupancyNext30Days = AnalyticsCalculator.OccupancyPercent(
                activeHotel.Select(b => (b.CheckInDate, b.CheckOutDate)),
                roomCount,
                today,
                30),

            UpcomingCheckIns7Days = activeHotel.Count(b =>
                b.CheckInDate.Date >= today &&
                b.CheckInDate.Date < today.AddDays(7)),

            DiscountGiven = activeHotel.Sum(b => b.DiscountAmount),
            DiscountedBookings = activeHotel.Count(b => b.DiscountAmount > 0)
        };

        dto.TotalRevenue = dto.HotelRevenue + dto.CarRevenue + dto.TaxiRevenue + dto.FlightRevenue;
        dto.TotalOrders = dto.HotelBookings + dto.CarRentals + dto.TaxiBookings + dto.FlightBookings;

        // Rezerv edilmə ilə giriş arasındakı orta gün (köhnə məlumatda CreatedAt boş ola bilər)
        var withLead = activeHotel
            .Where(b => b.CreatedAt.Year > 2000 && b.CheckInDate.Date >= b.CreatedAt.Date)
            .ToList();

        dto.AverageLeadTimeDays = withLead.Count == 0
            ? 0
            : Math.Round(withLead.Average(b => (b.CheckInDate.Date - b.CreatedAt.Date).TotalDays), 1);

        // =========================
        // AYLIQ GƏLİR (5 ay əvvəl → 6 ay sonra)
        // =========================
        foreach (var month in AnalyticsCalculator.MonthRange(today, 5, 6))
        {
            dto.Monthly.Add(new MonthlyRevenueDto
            {
                Label = month.ToString("MMM yy", CultureInfo.InvariantCulture),
                Hotels = activeHotel.Where(b => AnalyticsCalculator.InMonth(b.CheckInDate, month)).Sum(b => b.TotalPrice),
                Cars = activeCars.Where(r => AnalyticsCalculator.InMonth(r.PickupDate, month)).Sum(r => r.TotalPrice),
                Taxis = activeTaxis.Where(t => AnalyticsCalculator.InMonth(t.PickupTime, month)).Sum(t => t.Price),
                Flights = activeFlights.Where(f => AnalyticsCalculator.InMonth(f.DepartureTime, month)).Sum(f => f.TotalPrice)
            });
        }

        // =========================
        // TOP ŞƏHƏRLƏR / OTAQLAR / REYTİNQ
        // =========================
        dto.TopCities = activeHotel
            .GroupBy(b => b.City)
            .Select(g => new CityStatDto
            {
                City = g.Key,
                Bookings = g.Count(),
                Revenue = g.Sum(b => b.TotalPrice)
            })
            .OrderByDescending(c => c.Revenue)
            .Take(7)
            .ToList();

        dto.TopRooms = activeHotel
            .GroupBy(b => new { b.RoomId, b.RoomName, b.HotelName })
            .Select(g => new RoomStatDto
            {
                RoomName = g.Key.RoomName,
                HotelName = g.Key.HotelName,
                Bookings = g.Count(),
                Revenue = g.Sum(b => b.TotalPrice)
            })
            .OrderByDescending(r => r.Bookings)
            .ThenByDescending(r => r.Revenue)
            .Take(5)
            .ToList();

        dto.HotelRatings = reviews
            .GroupBy(r => new { r.HotelName, r.City })
            .Select(g => new HotelRatingDto
            {
                HotelName = g.Key.HotelName,
                City = g.Key.City,
                AverageRating = Math.Round(g.Average(r => r.Rating), 1),
                Reviews = g.Count()
            })
            .OrderByDescending(h => h.AverageRating)
            .ThenByDescending(h => h.Reviews)
            .ToList();

        return dto;
    }
}
