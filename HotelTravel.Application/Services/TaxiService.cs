using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class TaxiService : ITaxiService
{
    private readonly IAppDbContext _context;

    public TaxiService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // HOTELS (hava limanı olan şəhərlər)
    // =========================
    public async Task<List<TaxiHotelDto>> GetHotelsAsync()
    {
        var hotels = await _context.Hotels
            .Where(h => !h.IsDeleted)
            .OrderBy(h => h.City)
            .ThenBy(h => h.Name)
            .Select(h => new { h.Id, h.Name, h.City, h.Address })
            .ToListAsync();

        var result = new List<TaxiHotelDto>();

        foreach (var hotel in hotels)
        {
            var airport = TaxiRules.GetAirport(hotel.City);

            if (airport is null)
                continue;

            result.Add(new TaxiHotelDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                Address = hotel.Address,
                AirportCode = airport.Code,
                AirportName = airport.Name,
                DistanceKm = airport.DistanceKm,
                EstimatedMinutes = TaxiRules.EstimateMinutes(airport.DistanceKm)
            });
        }

        return result;
    }

    public async Task<TaxiHotelDto?> GetHotelAsync(int hotelId)
    {
        var hotels = await GetHotelsAsync();

        return hotels.FirstOrDefault(h => h.Id == hotelId);
    }


    // =========================
    // OPTIONS (qiymətlər)
    // =========================
    public List<TaxiOptionDto> GetOptions(int distanceKm, int passengers, int luggage)
    {
        return TaxiRules.Vehicles
            .Select(v => new TaxiOptionDto
            {
                Type = v.Type,
                Name = v.Name,
                Example = v.Example,
                MaxPassengers = v.MaxPassengers,
                MaxLuggage = v.MaxLuggage,
                Price = TaxiRules.CalculatePrice(v.Type, distanceKm),
                Fits = TaxiRules.Fits(v.Type, passengers, luggage)
            })
            .OrderBy(o => o.Price)
            .ToList();
    }


    // =========================
    // BOOK
    // =========================
    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateTaxiBookingDto dto, string? appUserId)
    {
        var error = TaxiRules.ValidateRequest(
            dto.PickupTime,
            dto.Passengers,
            dto.Luggage,
            DateTime.Now);

        if (error != null)
            return (false, error, null);

        if (!Enum.IsDefined(dto.VehicleType) || !Enum.IsDefined(dto.Direction))
            return (false, "Please choose a valid option.", null);

        if (!TaxiRules.Fits(dto.VehicleType, dto.Passengers, dto.Luggage))
            return (false, "This vehicle is too small for your group. Please choose a bigger one.", null);

        var hotel = await _context.Hotels
            .Where(h => h.Id == dto.HotelId && !h.IsDeleted)
            .Select(h => new { h.Id, h.City })
            .FirstOrDefaultAsync();

        var airport = TaxiRules.GetAirport(hotel?.City);

        if (hotel is null || airport is null)
            return (false, "Airport taxis are not available for this hotel.", null);

        var code = $"TX-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        await _context.TaxiBookings.AddAsync(new TaxiBooking
        {
            ConfirmationCode = code,
            HotelId = hotel.Id,
            City = airport.City,
            AirportCode = airport.Code,
            AirportName = airport.Name,
            Direction = dto.Direction,
            PickupTime = dto.PickupTime,
            FlightNumber = string.IsNullOrWhiteSpace(dto.FlightNumber)
                ? null
                : dto.FlightNumber.Trim().ToUpper(),
            Passengers = dto.Passengers,
            Luggage = dto.Luggage,
            VehicleType = dto.VehicleType,
            DistanceKm = airport.DistanceKm,
            Price = TaxiRules.CalculatePrice(dto.VehicleType, airport.DistanceKm),
            Status = TaxiBookingStatus.Confirmed,
            PassengerName = dto.PassengerName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            AppUserId = appUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();

        return (true, "Airport taxi booked.", code);
    }


    // =========================
    // LOOKUP
    // =========================
    public async Task<TaxiBookingDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        var code = confirmationCode.Trim().ToUpper();

        var booking = await Project(
                _context.TaxiBookings.Where(t =>
                    !t.IsDeleted &&
                    t.ConfirmationCode == code))
            .FirstOrDefaultAsync();

        return FillCanCancel(booking);
    }

    public async Task<List<TaxiBookingDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<TaxiBookingDto>();

        var bookings = await Project(
                _context.TaxiBookings.Where(t =>
                    !t.IsDeleted &&
                    t.AppUserId == appUserId))
            .OrderByDescending(t => t.PickupTime)
            .ToListAsync();

        bookings.ForEach(t => FillCanCancel(t));

        return bookings;
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(
        int taxiBookingId,
        string appUserId)
    {
        var booking = await _context.TaxiBookings
            .FirstOrDefaultAsync(t =>
                t.Id == taxiBookingId &&
                !t.IsDeleted &&
                t.AppUserId == appUserId);

        if (booking is null)
            return (false, "Taxi booking was not found.");

        if (!TaxiRules.CanCancel(booking.Status, booking.PickupTime, DateTime.Now))
            return (false, $"Taxi bookings can be cancelled up to {TaxiRules.FreeCancelHours} hours before pick-up.");

        booking.Status = TaxiBookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Airport taxi cancelled.");
    }


    // =========================
    // ADMIN
    // =========================
    public async Task<List<TaxiBookingDto>> GetAllAsync(TaxiBookingStatus? status)
    {
        var query = _context.TaxiBookings.Where(t => !t.IsDeleted);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var bookings = await Project(query)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.PickupTime)
            .Take(300)
            .ToListAsync();

        bookings.ForEach(t => FillCanCancel(t));

        return bookings;
    }

    public async Task<bool> SetStatusAsync(int id, TaxiBookingStatus status)
    {
        // Yalnız Confirmed -> Completed / Cancelled
        if (status == TaxiBookingStatus.Confirmed)
            return false;

        var booking = await _context.TaxiBookings
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (booking is null || booking.Status != TaxiBookingStatus.Confirmed)
            return false;

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // HELPERS
    // =========================
    private static TaxiBookingDto? FillCanCancel(TaxiBookingDto? booking)
    {
        if (booking != null)
        {
            booking.CanCancel = TaxiRules.CanCancel(
                booking.Status,
                booking.PickupTime,
                DateTime.Now);
        }

        return booking;
    }

    private static IQueryable<TaxiBookingDto> Project(IQueryable<TaxiBooking> query)
    {
        return query.Select(t => new TaxiBookingDto
        {
            Id = t.Id,
            ConfirmationCode = t.ConfirmationCode,
            HotelId = t.HotelId,
            HotelName = t.Hotel.Name,
            HotelAddress = t.Hotel.Address,
            City = t.City,
            AirportCode = t.AirportCode,
            AirportName = t.AirportName,
            Direction = t.Direction,
            PickupTime = t.PickupTime,
            FlightNumber = t.FlightNumber,
            Passengers = t.Passengers,
            Luggage = t.Luggage,
            VehicleType = t.VehicleType,
            DistanceKm = t.DistanceKm,
            Price = t.Price,
            Status = t.Status,
            PassengerName = t.PassengerName,
            Email = t.Email,
            Phone = t.Phone,
            Notes = t.Notes
        });
    }
}
