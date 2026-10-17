using HotelTravel.Application.DTOs.Flights;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using HotelTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// "Flights" tab-ı
public class FlightsController : Controller
{
    private readonly IFlightService _flightService;
    private readonly CurrentUserAccessor _currentUser;

    public FlightsController(
        IFlightService flightService,
        CurrentUserAccessor currentUser)
    {
        _flightService = flightService;
        _currentUser = currentUser;
    }


    // =========================
    // SEARCH
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index(
        string? from,
        string? to,
        DateTime? date,
        int passengers = 1,
        CabinClass cabin = CabinClass.Economy)
    {
        var cities = await _flightService.GetCitiesAsync();

        string? Match(string? city) => cities.FirstOrDefault(c =>
            string.Equals(c, city?.Trim(), StringComparison.OrdinalIgnoreCase));

        // Seçilməyibsə: Bakı -> İstanbul
        var fromCity = Match(from) ?? Match("Baku") ?? cities.FirstOrDefault();
        var toCity = Match(to)
            ?? cities.FirstOrDefault(c => c != fromCity && c == "Istanbul")
            ?? cities.FirstOrDefault(c => c != fromCity);

        var flightDate = (date ?? DateTime.Today.AddDays(7)).Date;

        if (!Enum.IsDefined(cabin))
            cabin = CabinClass.Economy;

        var error = FlightRules.ValidateSearch(
            fromCity, toCity, flightDate, passengers, DateTime.Today);

        ViewBag.Cities = cities;
        ViewBag.From = fromCity;
        ViewBag.To = toCity;
        ViewBag.Date = flightDate;
        ViewBag.Passengers = passengers;
        ViewBag.Cabin = cabin;
        ViewBag.Error = error;

        if (error != null || fromCity == null || toCity == null)
        {
            ViewBag.DayFares = new List<FlightDayFareDto>();
            return View(new List<FlightOptionDto>());
        }

        ViewBag.DayFares = await _flightService.GetDayFaresAsync(
            fromCity, toCity, flightDate, cabin);

        var flights = await _flightService.SearchAsync(
            fromCity, toCity, flightDate, passengers, cabin);

        return View(flights);
    }


    // =========================
    // BOOK
    // =========================
    [HttpGet]
    public async Task<IActionResult> Book(
        int routeId,
        DateTime date,
        int passengers = 1,
        CabinClass cabin = CabinClass.Economy)
    {
        passengers = Math.Clamp(passengers, 1, FlightRules.MaxPassengers);

        var flight = await _flightService.GetOptionAsync(routeId, date, passengers, cabin);

        if (flight is null)
            return NotFound();

        var dto = new CreateFlightBookingDto
        {
            RouteId = routeId,
            FlightDate = date.Date,
            Cabin = cabin,
            Passengers = passengers,
            PassengerNames = Enumerable.Repeat(string.Empty, passengers).ToList()
        };

        // Daxil olmuş istifadəçinin məlumatları
        var user = await _currentUser.GetUserAsync(User);

        if (user != null)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            dto.ContactName = fullName;
            dto.Email = user.Email ?? string.Empty;
            dto.Phone = user.PhoneNumber ?? string.Empty;
            dto.PassengerNames[0] = fullName;
        }

        ViewBag.Flight = flight;

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(CreateFlightBookingDto dto)
    {
        dto.Passengers = Math.Clamp(dto.Passengers, 1, FlightRules.MaxPassengers);

        // Formda sərnişin sayı qədər ad sahəsi olsun
        while (dto.PassengerNames.Count < dto.Passengers)
            dto.PassengerNames.Add(string.Empty);

        if (ModelState.IsValid)
        {
            var result = await _flightService.BookAsync(
                dto,
                _currentUser.GetUserId(User));

            if (result.Success)
            {
                return RedirectToAction(
                    nameof(Confirmation),
                    new { confirmationCode = result.ConfirmationCode });
            }

            ModelState.AddModelError(string.Empty, result.Message);
        }

        var flight = await _flightService.GetOptionAsync(
            dto.RouteId, dto.FlightDate, dto.Passengers, dto.Cabin);

        if (flight is null)
            return NotFound();

        ViewBag.Flight = flight;

        return View(dto);
    }


    // =========================
    // CONFIRMATION
    // =========================
    [HttpGet]
    public async Task<IActionResult> Confirmation(string confirmationCode)
    {
        var booking = await _flightService.GetByCodeAsync(confirmationCode);

        if (booking is null)
            return NotFound();

        return View(booking);
    }


    // =========================
    // CANCEL (My Bookings)
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _flightService.CancelByUserAsync(
            id,
            _currentUser.GetUserId(User)!);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction("MyBookings", "Bookings");
    }
}
