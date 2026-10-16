using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

public class FlightService : IFlightService
{
    private readonly IAppDbContext _context;

    public FlightService(IAppDbContext context)
    {
        _context = context;
    }


    // =========================
    // CITIES
    // =========================
    public async Task<List<string>> GetCitiesAsync()
    {
        var routes = _context.FlightRoutes.Where(r => !r.IsDeleted && r.IsActive);

        var from = await routes.Select(r => r.FromCity).Distinct().ToListAsync();
        var to = await routes.Select(r => r.ToCity).Distinct().ToListAsync();

        return from
            .Union(to, StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }


    // =========================
    // SEARCH
    // =========================
    public async Task<List<FlightOptionDto>> SearchAsync(
        string fromCity,
        string toCity,
        DateTime date,
        int passengers,
        CabinClass cabin)
    {
        var routes = await GetActiveRoutesAsync(fromCity, toCity);

        routes = routes.Where(r => HasCabin(r, cabin)).ToList();

        var taken = await GetTakenSeatsAsync(
            routes.Select(r => r.Id).ToList(),
            date.Date,
            cabin);

        var now = DateTime.Now;

        return routes
            .Select(r => ToOption(r, date.Date, passengers, cabin, taken.GetValueOrDefault(r.Id), now))
            .OrderBy(o => o.Departure)
            .ToList();
    }

    public async Task<List<FlightDayFareDto>> GetDayFaresAsync(
        string fromCity,
        string toCity,
        DateTime centerDate,
        CabinClass cabin)
    {
        var routes = (await GetActiveRoutesAsync(fromCity, toCity))
            .Where(r => HasCabin(r, cabin))
            .ToList();

        var now = DateTime.Now;
        var today = now.Date;

        // Seçilmiş tarixdən 3 gün əvvəl və 3 gün sonra (keçmiş günlər olmadan)
        var start = centerDate.Date.AddDays(-3);

        if (start < today)
            start = today;

        var days = new List<FlightDayFareDto>();

        for (var day = start; days.Count < 7; day = day.AddDays(1))
        {
            var fares = routes
                .Select(r => new
                {
                    Departure = FlightRules.GetDeparture(day, r.DepartureTime),
                    Base = BasePrice(r, cabin)
                })
                .Where(x => FlightRules.CanBook(x.Departure, now))
                .Select(x => FlightRules.CalculateFare(x.Base, x.Departure, now))
                .ToList();

            days.Add(new FlightDayFareDto
            {
                Date = day,
                LowestFare = fares.Any() ? fares.Min() : null
            });
        }

        return days;
    }

    public async Task<FlightOptionDto?> GetOptionAsync(
        int routeId,
        DateTime date,
        int passengers,
        CabinClass cabin)
    {
        var route = await _context.FlightRoutes
            .FirstOrDefaultAsync(r =>
                r.Id == routeId &&
                !r.IsDeleted &&
                r.IsActive);

        if (route is null || !HasCabin(route, cabin))
            return null;

        var taken = await GetTakenSeatsAsync(new List<int> { route.Id }, date.Date, cabin);

        return ToOption(
            route,
            date.Date,
            passengers,
            cabin,
            taken.GetValueOrDefault(route.Id),
            DateTime.Now);
    }


    // =========================
    // BOOK
    // =========================
    public async Task<(bool Success, string Message, string? ConfirmationCode)>
        BookAsync(CreateFlightBookingDto dto, string? appUserId)
    {
        var now = DateTime.Now;

        if (dto.Passengers < 1 || dto.Passengers > FlightRules.MaxPassengers)
            return (false, $"Passengers must be between 1 and {FlightRules.MaxPassengers}.", null);

        if (!Enum.IsDefined(dto.Cabin))
            return (false, "Please choose a valid cabin.", null);

        var names = (dto.PassengerNames ?? new List<string>())
            .Select(n => (n ?? string.Empty).Trim())
            .Where(n => n.Length > 0)
            .ToList();

        if (names.Count != dto.Passengers)
            return (false, "Please enter the full name of every passenger.", null);

        if (names.Any(n => n.Length > 100))
            return (false, "Passenger names can be up to 100 characters.", null);

        var route = await _context.FlightRoutes
            .FirstOrDefaultAsync(r =>
                r.Id == dto.RouteId &&
                !r.IsDeleted &&
                r.IsActive);

        if (route is null || !HasCabin(route, dto.Cabin))
            return (false, "This flight is no longer available.", null);

        var flightDate = dto.FlightDate.Date;

        if (flightDate > now.Date.AddDays(FlightRules.MaxDaysAhead))
            return (false, $"Flights can be booked up to {FlightRules.MaxDaysAhead} days ahead.", null);

        var departure = FlightRules.GetDeparture(flightDate, route.DepartureTime);

        if (!FlightRules.CanBook(departure, now))
            return (false, "This flight has already closed for booking.", null);

        // Yer qalıbmı?
        var taken = await GetTakenSeatsAsync(new List<int> { route.Id }, flightDate, dto.Cabin);
        var seatsLeft = FlightRules.SeatsLeft(Capacity(route, dto.Cabin), taken.GetValueOrDefault(route.Id));

        if (seatsLeft < dto.Passengers)
        {
            return (false, seatsLeft == 0
                ? "This flight is sold out in the selected cabin."
                : $"Only {seatsLeft} seat(s) left in this cabin.", null);
        }

        var fare = FlightRules.CalculateFare(BasePrice(route, dto.Cabin), departure, now);

        var code = $"FL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        await _context.FlightBookings.AddAsync(new FlightBooking
        {
            ConfirmationCode = code,
            FlightRouteId = route.Id,
            FlightDate = flightDate,
            FlightNumber = route.FlightNumber,
            Airline = route.Airline,
            FromCity = route.FromCity,
            FromCode = route.FromCode,
            ToCity = route.ToCity,
            ToCode = route.ToCode,
            DepartureTime = departure,
            ArrivalTime = FlightRules.GetArrival(departure, route.DurationMinutes, route.FromCity, route.ToCity),
            Cabin = dto.Cabin,
            Passengers = dto.Passengers,
            PassengerNames = string.Join("\n", names),
            FarePerPassenger = fare,
            TotalPrice = FlightRules.CalculateTotal(fare, dto.Passengers),
            Status = FlightBookingStatus.Confirmed,
            ContactName = dto.ContactName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            AppUserId = appUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();

        return (true, "Flight booked.", code);
    }


    // =========================
    // LOOKUP
    // =========================
    public async Task<FlightBookingDto?> GetByCodeAsync(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return null;

        var code = confirmationCode.Trim().ToUpper();

        var booking = await ProjectBookings(
                _context.FlightBookings.Where(b =>
                    !b.IsDeleted &&
                    b.ConfirmationCode == code))
            .FirstOrDefaultAsync();

        return FillCanCancel(booking);
    }

    public async Task<List<FlightBookingDto>> GetByUserAsync(string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return new List<FlightBookingDto>();

        var bookings = await ProjectBookings(
                _context.FlightBookings.Where(b =>
                    !b.IsDeleted &&
                    b.AppUserId == appUserId))
            .OrderByDescending(b => b.DepartureTime)
            .ToListAsync();

        bookings.ForEach(b => FillCanCancel(b));

        return bookings;
    }

    public async Task<(bool Success, string Message)> CancelByUserAsync(
        int flightBookingId,
        string appUserId)
    {
        var booking = await _context.FlightBookings
            .FirstOrDefaultAsync(b =>
                b.Id == flightBookingId &&
                !b.IsDeleted &&
                b.AppUserId == appUserId);

        if (booking is null)
            return (false, "Flight booking was not found.");

        if (!FlightRules.CanCancel(booking.Status, booking.DepartureTime, DateTime.Now))
            return (false, $"Flights can be cancelled up to {FlightRules.FreeCancelHours} hours before departure.");

        booking.Status = FlightBookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Flight cancelled.");
    }


    // =========================
    // ADMIN
    // =========================
    public async Task<List<FlightRouteDto>> GetRoutesAsync(string? city)
    {
        var query = _context.FlightRoutes.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(city))
        {
            var name = city.Trim().ToLower();

            query = query.Where(r =>
                r.FromCity.ToLower() == name ||
                r.ToCity.ToLower() == name);
        }

        return await query
            .OrderBy(r => r.FromCity)
            .ThenBy(r => r.ToCity)
            .ThenBy(r => r.DepartureTime)
            .Select(r => new FlightRouteDto
            {
                Id = r.Id,
                FlightNumber = r.FlightNumber,
                Airline = r.Airline,
                FromCity = r.FromCity,
                FromCode = r.FromCode,
                ToCity = r.ToCity,
                ToCode = r.ToCode,
                DepartureTime = r.DepartureTime,
                DurationMinutes = r.DurationMinutes,
                EconomyPrice = r.EconomyPrice,
                BusinessPrice = r.BusinessPrice,
                EconomySeats = r.EconomySeats,
                BusinessSeats = r.BusinessSeats,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    public async Task<SaveFlightRouteDto?> GetRouteForEditAsync(int id)
    {
        return await _context.FlightRoutes
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new SaveFlightRouteDto
            {
                Id = r.Id,
                FlightNumber = r.FlightNumber,
                Airline = r.Airline,
                FromCity = r.FromCity,
                ToCity = r.ToCity,
                DepartureTime = r.DepartureTime,
                DurationMinutes = r.DurationMinutes,
                EconomyPrice = r.EconomyPrice,
                BusinessPrice = r.BusinessPrice,
                EconomySeats = r.EconomySeats,
                BusinessSeats = r.BusinessSeats,
                IsActive = r.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> SaveRouteAsync(SaveFlightRouteDto dto)
    {
        var fromAirport = TaxiRules.GetAirport(dto.FromCity);
        var toAirport = TaxiRules.GetAirport(dto.ToCity);

        if (fromAirport is null || toAirport is null)
            return (false, "Both cities must have an airport in the list.");

        if (fromAirport.City == toAirport.City)
            return (false, "Departure and destination cannot be the same city.");

        if (dto.BusinessSeats > 0 && dto.BusinessPrice <= 0)
            return (false, "Set a business price or make business seats 0.");

        if (dto.DepartureTime < TimeSpan.Zero || dto.DepartureTime >= TimeSpan.FromDays(1))
            return (false, "Departure time is not valid.");

        FlightRoute? route = null;

        if (dto.Id.HasValue)
        {
            route = await _context.FlightRoutes
                .FirstOrDefaultAsync(r => r.Id == dto.Id.Value && !r.IsDeleted);

            if (route is null)
                return (false, "Flight was not found.");
        }

        if (route is null)
        {
            route = new FlightRoute
            {
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.FlightRoutes.AddAsync(route);
        }

        route.FlightNumber = dto.FlightNumber.Trim().ToUpper();
        route.Airline = dto.Airline.Trim();
        route.FromCity = fromAirport.City;
        route.FromCode = fromAirport.Code;
        route.ToCity = toAirport.City;
        route.ToCode = toAirport.Code;
        route.DepartureTime = new TimeSpan(dto.DepartureTime.Hours, dto.DepartureTime.Minutes, 0);
        route.DurationMinutes = dto.DurationMinutes;
        route.EconomyPrice = dto.EconomyPrice;
        route.BusinessPrice = dto.BusinessSeats > 0 ? dto.BusinessPrice : 0;
        route.EconomySeats = dto.EconomySeats;
        route.BusinessSeats = dto.BusinessSeats;
        route.IsActive = dto.IsActive;
        route.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, dto.Id.HasValue ? "Flight updated." : "Flight added.");
    }

    public async Task<bool> ToggleRouteAsync(int id)
    {
        var route = await _context.FlightRoutes
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (route is null)
            return false;

        route.IsActive = !route.IsActive;
        route.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<FlightBookingDto>> GetBookingsAsync(FlightBookingStatus? status)
    {
        var query = _context.FlightBookings.Where(b => !b.IsDeleted);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        var bookings = await ProjectBookings(query)
            .OrderBy(b => b.Status)
            .ThenBy(b => b.DepartureTime)
            .Take(300)
            .ToListAsync();

        bookings.ForEach(b => FillCanCancel(b));

        return bookings;
    }

    public async Task<bool> SetBookingStatusAsync(int id, FlightBookingStatus status)
    {
        // Yalnız Confirmed -> Completed / Cancelled
        if (status == FlightBookingStatus.Confirmed)
            return false;

        var booking = await _context.FlightBookings
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        if (booking is null || booking.Status != FlightBookingStatus.Confirmed)
            return false;

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // =========================
    // HELPERS
    // =========================
    private async Task<List<FlightRoute>> GetActiveRoutesAsync(string fromCity, string toCity)
    {
        if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            return new List<FlightRoute>();

        var from = fromCity.Trim().ToLower();
        var to = toCity.Trim().ToLower();

        return await _context.FlightRoutes
            .Where(r =>
                !r.IsDeleted &&
                r.IsActive &&
                r.FromCity.ToLower() == from &&
                r.ToCity.ToLower() == to)
            .ToListAsync();
    }

    // Marşrut -> həmin gün həmin salonda tutulmuş yer sayı
    private async Task<Dictionary<int, int>> GetTakenSeatsAsync(
        List<int> routeIds,
        DateTime flightDate,
        CabinClass cabin)
    {
        if (routeIds.Count == 0)
            return new Dictionary<int, int>();

        var date = flightDate.Date;

        return await _context.FlightBookings
            .Where(b =>
                routeIds.Contains(b.FlightRouteId) &&
                b.FlightDate == date &&
                b.Cabin == cabin &&
                !b.IsDeleted &&
                b.Status != FlightBookingStatus.Cancelled)
            .GroupBy(b => b.FlightRouteId)
            .Select(g => new { RouteId = g.Key, Seats = g.Sum(b => b.Passengers) })
            .ToDictionaryAsync(x => x.RouteId, x => x.Seats);
    }

    private static bool HasCabin(FlightRoute route, CabinClass cabin)
        => cabin == CabinClass.Economy
            ? route.EconomySeats > 0
            : route.BusinessSeats > 0 && route.BusinessPrice > 0;

    private static int Capacity(FlightRoute route, CabinClass cabin)
        => cabin == CabinClass.Business ? route.BusinessSeats : route.EconomySeats;

    private static decimal BasePrice(FlightRoute route, CabinClass cabin)
        => cabin == CabinClass.Business ? route.BusinessPrice : route.EconomyPrice;

    private static FlightOptionDto ToOption(
        FlightRoute route,
        DateTime flightDate,
        int passengers,
        CabinClass cabin,
        int takenSeats,
        DateTime now)
    {
        var departure = FlightRules.GetDeparture(flightDate, route.DepartureTime);
        var fare = FlightRules.CalculateFare(BasePrice(route, cabin), departure, now);
        var seatsLeft = FlightRules.SeatsLeft(Capacity(route, cabin), takenSeats);

        return new FlightOptionDto
        {
            RouteId = route.Id,
            FlightNumber = route.FlightNumber,
            Airline = route.Airline,
            FromCity = route.FromCity,
            FromCode = route.FromCode,
            FromAirport = FlightRules.AirportName(route.FromCity),
            ToCity = route.ToCity,
            ToCode = route.ToCode,
            ToAirport = FlightRules.AirportName(route.ToCity),
            Departure = departure,
            Arrival = FlightRules.GetArrival(departure, route.DurationMinutes, route.FromCity, route.ToCity),
            DurationMinutes = route.DurationMinutes,
            Cabin = cabin,
            Fare = fare,
            Total = FlightRules.CalculateTotal(fare, passengers),
            SeatsLeft = seatsLeft,
            IsAvailable = seatsLeft >= passengers && FlightRules.CanBook(departure, now)
        };
    }

    private static FlightBookingDto? FillCanCancel(FlightBookingDto? booking)
    {
        if (booking != null)
        {
            booking.CanCancel = FlightRules.CanCancel(
                booking.Status,
                booking.DepartureTime,
                DateTime.Now);
        }

        return booking;
    }

    private static IQueryable<FlightBookingDto> ProjectBookings(IQueryable<FlightBooking> query)
    {
        return query.Select(b => new FlightBookingDto
        {
            Id = b.Id,
            ConfirmationCode = b.ConfirmationCode,
            RouteId = b.FlightRouteId,
            FlightDate = b.FlightDate,
            FlightNumber = b.FlightNumber,
            Airline = b.Airline,
            FromCity = b.FromCity,
            FromCode = b.FromCode,
            ToCity = b.ToCity,
            ToCode = b.ToCode,
            DepartureTime = b.DepartureTime,
            ArrivalTime = b.ArrivalTime,
            Cabin = b.Cabin,
            Passengers = b.Passengers,
            PassengerNames = b.PassengerNames,
            FarePerPassenger = b.FarePerPassenger,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            ContactName = b.ContactName,
            Email = b.Email,
            Phone = b.Phone
        });
    }
}
