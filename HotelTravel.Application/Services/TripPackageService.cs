using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.DTOs.Trips;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

// Trip paketi mövcud servislərdən qurulur (otel, uçuş, taksi).
// Sifariş addım-addım yaradılır; bir addım alınmasa əvvəlki hissələr ləğv edilir
// (kompensasiya / "saga" yanaşması), beləcə yarımçıq paket qalmır.
public class TripPackageService : ITripPackageService
{
    private readonly IAppDbContext _context;
    private readonly IFlightService _flightService;
    private readonly IRoomService _roomService;
    private readonly IBookingService _bookingService;
    private readonly ITaxiService _taxiService;

    public TripPackageService(
        IAppDbContext context,
        IFlightService flightService,
        IRoomService roomService,
        IBookingService bookingService,
        ITaxiService taxiService)
    {
        _context = context;
        _flightService = flightService;
        _roomService = roomService;
        _bookingService = bookingService;
        _taxiService = taxiService;
    }

    // =========================
    // ŞƏHƏRLƏR
    // =========================

    public Task<List<string>> GetOriginsAsync()
        => _flightService.GetCitiesAsync();

    public async Task<List<string>> GetDestinationsAsync()
    {
        var flightCities = await _flightService.GetCitiesAsync();

        var hotelCities = await _context.Hotels
            .Where(h => !h.IsDeleted && h.Rooms.Any(r => !r.IsDeleted))
            .Select(h => h.City)
            .Distinct()
            .ToListAsync();

        return hotelCities
            .Where(c => flightCities.Contains(c, StringComparer.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .ToList();
    }

    // =========================
    // PLAN (seçimlər)
    // =========================

    public async Task<TripPlanDto> PlanAsync(TripSearchDto search)
    {
        var plan = new TripPlanDto
        {
            BaseDiscount = TripPackageRules.BaseDiscount,
            ReturnFlightBonus = TripPackageRules.ReturnFlightBonus,
            TaxiBonus = TripPackageRules.TaxiBonus
        };

        plan.Error = TripPackageRules.ValidateSearch(
            search.FromCity, search.ToCity, search.DepartDate, search.ReturnDate, search.Travelers, DateTime.Today);

        if (plan.Error != null)
            return plan;

        var from = search.FromCity!.Trim();
        var to = search.ToCity!.Trim();
        var depart = search.DepartDate!.Value.Date;
        var back = search.ReturnDate!.Value.Date;

        plan.Nights = TripPackageRules.GetNights(depart, back);

        plan.OutboundFlights = (await _flightService.SearchAsync(from, to, depart, search.Travelers, search.Cabin))
            .Where(f => f.IsAvailable)
            .OrderBy(f => f.Departure)
            .ToList();

        plan.ReturnFlights = (await _flightService.SearchAsync(to, from, back, search.Travelers, search.Cabin))
            .Where(f => f.IsAvailable)
            .OrderBy(f => f.Departure)
            .ToList();

        var rooms = await _roomService.SearchAvailableRoomsAsync(new RoomSearchDto
        {
            Destination = to,
            CheckInDate = depart,
            CheckOutDate = back,
            AdultCount = search.Adults,
            ChildrenCount = search.Children
        });

        plan.Rooms = rooms
            .Where(r => string.Equals(r.City, to, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.PricePerNight)
            .Select(r => new TripRoomOptionDto
            {
                Room = r,
                StayPrice = r.PricePerNight * plan.Nights
            })
            .ToList();

        plan.Taxi = BuildTaxiOption(to, search.Travelers);

        if (plan.OutboundFlights.Count == 0)
            plan.Error = $"There are no available flights from {from} to {to} on {depart:dd MMM}. Try another date.";
        else if (plan.Rooms.Count == 0)
            plan.Error = $"No rooms are available in {to} for these dates and guests.";

        return plan;
    }

    // =========================
    // SİFARİŞ
    // =========================

    public async Task<(bool Success, string Message, string? Code)> BookAsync(
        CreateTripPackageDto dto,
        string appUserId)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
            return (false, "Please sign in to book a trip package.", null);

        var travelers = dto.Adults + dto.Children;

        var error = TripPackageRules.ValidateSearch(
            dto.FromCity, dto.ToCity, dto.DepartDate, dto.ReturnDate, travelers, DateTime.Today);

        if (error != null)
            return (false, error, null);

        var names = (dto.TravelerNames ?? new List<string>())
            .Select(n => (n ?? string.Empty).Trim())
            .Where(n => n.Length > 0)
            .ToList();

        if (names.Count != travelers)
            return (false, "Please enter the full name of every traveler.", null);

        var from = dto.FromCity.Trim();
        var to = dto.ToCity.Trim();
        var depart = dto.DepartDate.Date;
        var back = dto.ReturnDate.Date;

        // ---- 1. Qiymətləri yoxla (heç nə yaradılmadan) ----
        var outbound = await _flightService.GetOptionAsync(dto.OutboundRouteId, depart, travelers, dto.Cabin);

        if (outbound == null || !outbound.IsAvailable || !SameRoute(outbound, from, to))
            return (false, "The selected outbound flight is no longer available.", null);

        FlightOptionDto? inbound = null;

        if (dto.ReturnRouteId.HasValue)
        {
            inbound = await _flightService.GetOptionAsync(dto.ReturnRouteId.Value, back, travelers, dto.Cabin);

            if (inbound == null || !inbound.IsAvailable || !SameRoute(inbound, to, from))
                return (false, "The selected return flight is no longer available.", null);
        }

        var room = await _roomService.GetByIdAsync(dto.RoomId);

        if (room == null || !string.Equals(room.City, to, StringComparison.OrdinalIgnoreCase))
            return (false, $"Please choose a room in {to}.", null);

        var quote = await _bookingService.GetPriceQuoteAsync(room.Id, depart, back, dto.Email, appUserId, null);

        if (!quote.Success)
            return (false, quote.Error ?? "The selected room is not available for these dates.", null);

        TripTaxiOptionDto? taxi = null;

        if (dto.IncludeTaxi)
        {
            taxi = BuildTaxiOption(to, travelers);

            if (taxi == null)
                return (false, "Airport taxi is not available for this trip.", null);

            if (TripPackageRules.GetTaxiPickup(outbound.Arrival) < DateTime.Now.AddHours(TaxiRules.MinHoursAhead))
                return (false, $"Airport taxi needs at least {TaxiRules.MinHoursAhead} hours' notice.", null);
        }

        // ---- 2. Hissələri yarat (alınmasa geri qaytar) ----
        var created = new CreatedParts();

        var hotelResult = await _bookingService.CreateAsync(new CreateBookingDto
        {
            RoomId = room.Id,
            CheckInDate = depart,
            CheckOutDate = back,
            AdultCount = dto.Adults,
            ChildrenCount = dto.Children,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim()
        }, appUserId);

        if (!hotelResult.Success || hotelResult.ConfirmationCode == null)
            return (false, "Hotel: " + hotelResult.Message, null);

        created.HotelCode = hotelResult.ConfirmationCode;

        var outboundResult = await BookFlightAsync(outbound, dto, names, appUserId);

        if (!outboundResult.Success)
            return await FailAsync(created, "Outbound flight: " + outboundResult.Message);

        created.OutboundCode = outboundResult.ConfirmationCode;

        if (inbound != null)
        {
            var inboundResult = await BookFlightAsync(inbound, dto, names, appUserId);

            if (!inboundResult.Success)
                return await FailAsync(created, "Return flight: " + inboundResult.Message);

            created.ReturnCode = inboundResult.ConfirmationCode;
        }

        if (taxi != null)
        {
            var taxiResult = await _taxiService.BookAsync(new CreateTaxiBookingDto
            {
                HotelId = room.HotelId,
                Direction = TaxiDirection.FromAirport,
                PickupTime = TripPackageRules.GetTaxiPickup(outbound.Arrival),
                Passengers = travelers,
                Luggage = Math.Min(travelers, TaxiRules.MaxLuggage),
                VehicleType = taxi.Vehicle,
                FlightNumber = outbound.FlightNumber,
                PassengerName = $"{dto.FirstName} {dto.LastName}".Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Notes = $"Trip package · flight {outbound.FlightNumber}"
            }, appUserId);

            if (!taxiResult.Success)
                return await FailAsync(created, "Airport taxi: " + taxiResult.Message);

            created.TaxiCode = taxiResult.ConfirmationCode;
        }

        // ---- 3. Paket ----
        var flightsPrice = outbound.Total + (inbound?.Total ?? 0);
        var taxiPrice = taxi?.Price ?? 0;
        var subtotal = quote.Total + flightsPrice + taxiPrice;
        var percent = TripPackageRules.GetDiscountPercent(inbound != null, taxi != null);
        var discount = TripPackageRules.GetDiscountAmount(subtotal, percent);

        var package = new TripPackage
        {
            Code = $"TP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            AppUserId = appUserId,
            FromCity = from,
            ToCity = to,
            DepartDate = depart,
            ReturnDate = back,
            Travelers = travelers,
            HotelBookingCode = created.HotelCode!,
            OutboundFlightCode = created.OutboundCode!,
            ReturnFlightCode = created.ReturnCode,
            TaxiCode = created.TaxiCode,
            HotelPrice = quote.Total,
            FlightsPrice = flightsPrice,
            TaxiPrice = taxiPrice,
            Subtotal = subtotal,
            DiscountPercent = percent,
            DiscountAmount = discount,
            Total = subtotal - discount,
            CreatedAt = DateTime.Now
        };

        _context.TripPackages.Add(package);
        await _context.SaveChangesAsync();

        return (true, $"Your trip to {to} is booked. You saved {discount:N2} AZN.", package.Code);
    }

    // =========================
    // OXU
    // =========================

    public async Task<TripPackageDto?> GetByCodeAsync(string code, string appUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim().ToUpper();

        var package = await _context.TripPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == normalized && p.AppUserId == appUserId && !p.IsDeleted);

        if (package == null)
            return null;

        var dto = Map(package);

        dto.Hotel = await _bookingService.GetByConfirmationCodeAsync(package.HotelBookingCode);
        dto.HotelName = dto.Hotel?.HotelName;
        dto.OutboundFlight = await _flightService.GetByCodeAsync(package.OutboundFlightCode);

        if (!string.IsNullOrWhiteSpace(package.ReturnFlightCode))
            dto.ReturnFlight = await _flightService.GetByCodeAsync(package.ReturnFlightCode);

        if (!string.IsNullOrWhiteSpace(package.TaxiCode))
            dto.Taxi = await _taxiService.GetByCodeAsync(package.TaxiCode);

        return dto;
    }

    public async Task<List<TripPackageDto>> GetByUserAsync(string appUserId)
    {
        var packages = await _context.TripPackages
            .AsNoTracking()
            .Where(p => p.AppUserId == appUserId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var hotelCodes = packages.Select(p => p.HotelBookingCode).ToList();

        var hotelNames = await _context.Bookings
            .Where(b => hotelCodes.Contains(b.ConfirmationCode))
            .Select(b => new { b.ConfirmationCode, b.Room.Hotel.Name })
            .ToDictionaryAsync(x => x.ConfirmationCode, x => x.Name);

        return packages
            .Select(p =>
            {
                var dto = Map(p);
                dto.HotelName = hotelNames.GetValueOrDefault(p.HotelBookingCode);
                return dto;
            })
            .ToList();
    }

    // =========================
    // HELPERS
    // =========================

    private static TripTaxiOptionDto? BuildTaxiOption(string city, int travelers)
    {
        var airport = TaxiRules.GetAirport(city);
        var vehicle = TripPackageRules.ChooseVehicle(travelers);

        if (airport == null || vehicle == null)
            return null;

        return new TripTaxiOptionDto
        {
            Vehicle = vehicle.Value,
            VehicleName = TaxiRules.GetVehicle(vehicle.Value).Name,
            AirportName = $"{airport.Name} ({airport.Code})",
            DistanceKm = airport.DistanceKm,
            Price = TaxiRules.CalculatePrice(vehicle.Value, airport.DistanceKm)
        };
    }

    private Task<(bool Success, string Message, string? ConfirmationCode)> BookFlightAsync(
        FlightOptionDto flight,
        CreateTripPackageDto dto,
        List<string> names,
        string appUserId)
        => _flightService.BookAsync(new CreateFlightBookingDto
        {
            RouteId = flight.RouteId,
            FlightDate = flight.Departure.Date,
            Cabin = dto.Cabin,
            Passengers = names.Count,
            PassengerNames = names,
            ContactName = $"{dto.FirstName} {dto.LastName}".Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim()
        }, appUserId);

    private static bool SameRoute(FlightOptionDto flight, string from, string to)
        => string.Equals(flight.FromCity, from, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(flight.ToCity, to, StringComparison.OrdinalIgnoreCase);

    // Alınmayan sifarişin artıq yaradılmış hissələrini ləğv edir
    private async Task<(bool Success, string Message, string? Code)> FailAsync(CreatedParts parts, string message)
    {
        if (parts.HotelCode != null)
        {
            var hotel = await _context.Bookings.FirstOrDefaultAsync(b => b.ConfirmationCode == parts.HotelCode);
            if (hotel != null) hotel.Status = BookingStatus.Cancelled;
        }

        foreach (var code in new[] { parts.OutboundCode, parts.ReturnCode }.Where(c => c != null))
        {
            var flight = await _context.FlightBookings.FirstOrDefaultAsync(f => f.ConfirmationCode == code);
            if (flight != null) flight.Status = FlightBookingStatus.Cancelled;
        }

        if (parts.TaxiCode != null)
        {
            var taxi = await _context.TaxiBookings.FirstOrDefaultAsync(t => t.ConfirmationCode == parts.TaxiCode);
            if (taxi != null) taxi.Status = TaxiBookingStatus.Cancelled;
        }

        await _context.SaveChangesAsync();

        return (false, message + " Nothing was charged; please try another option.", null);
    }

    private static TripPackageDto Map(TripPackage p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        CreatedAt = p.CreatedAt,
        FromCity = p.FromCity,
        ToCity = p.ToCity,
        DepartDate = p.DepartDate,
        ReturnDate = p.ReturnDate,
        Nights = TripPackageRules.GetNights(p.DepartDate, p.ReturnDate),
        Travelers = p.Travelers,
        HotelPrice = p.HotelPrice,
        FlightsPrice = p.FlightsPrice,
        TaxiPrice = p.TaxiPrice,
        Subtotal = p.Subtotal,
        DiscountPercent = p.DiscountPercent,
        DiscountAmount = p.DiscountAmount,
        Total = p.Total,
        HotelBookingCode = p.HotelBookingCode,
        OutboundFlightCode = p.OutboundFlightCode,
        ReturnFlightCode = p.ReturnFlightCode,
        TaxiCode = p.TaxiCode
    };

    private sealed class CreatedParts
    {
        public string? HotelCode { get; set; }
        public string? OutboundCode { get; set; }
        public string? ReturnCode { get; set; }
        public string? TaxiCode { get; set; }
    }
}
