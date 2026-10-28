using System.Globalization;
using HotelTravel.Application.DTOs.Taxis;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using HotelTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// "Airport taxis" tab-ı
public class TaxisController : Controller
{
    private readonly ITaxiService _taxiService;
    private readonly CurrentUserAccessor _currentUser;

    public TaxisController(
        ITaxiService taxiService,
        CurrentUserAccessor currentUser)
    {
        _taxiService = taxiService;
        _currentUser = currentUser;
    }


    // =========================
    // SEARCH
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index(
        int? hotelId,
        string? city,
        TaxiDirection direction = TaxiDirection.FromAirport,
        DateTime? date = null,
        string? time = null,
        int passengers = 2,
        int luggage = 2)
    {
        var hotels = await _taxiService.GetHotelsAsync();

        // Otel seçilməyibsə: şəhərə görə və ya ilk otel
        var hotel =
            hotels.FirstOrDefault(h => h.Id == hotelId)
            ?? hotels.FirstOrDefault(h =>
                string.Equals(h.City, city?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? hotels.FirstOrDefault();

        // Tarix seçilməyibsə: sabah saat 12:00
        var pickupDate = (date ?? DateTime.Today.AddDays(1)).Date;

        if (!TimeSpan.TryParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture, out var pickupClock))
            pickupClock = new TimeSpan(12, 0, 0);

        var pickupTime = pickupDate.Add(pickupClock);

        var error = TaxiRules.ValidateRequest(pickupTime, passengers, luggage, DateTime.Now);

        ViewBag.Hotels = hotels;
        ViewBag.Hotel = hotel;
        ViewBag.Direction = direction;
        ViewBag.PickupTime = pickupTime;
        ViewBag.Passengers = passengers;
        ViewBag.Luggage = luggage;
        ViewBag.Error = error;

        var options = hotel == null || error != null
            ? new List<TaxiOptionDto>()
            : _taxiService.GetOptions(hotel.DistanceKm, passengers, luggage);

        return View(options);
    }


    // =========================
    // BOOK
    // =========================
    [HttpGet]
    [HotelTravel.Web.Filters.GuestOnly]
    public async Task<IActionResult> Book(
        int hotelId,
        TaxiDirection direction,
        DateTime pickupTime,
        int passengers,
        int luggage,
        TaxiVehicleType vehicleType)
    {
        var hotel = await _taxiService.GetHotelAsync(hotelId);

        if (hotel is null)
            return NotFound();

        var dto = new CreateTaxiBookingDto
        {
            HotelId = hotelId,
            Direction = direction,
            PickupTime = pickupTime,
            Passengers = passengers,
            Luggage = luggage,
            VehicleType = vehicleType
        };

        // Daxil olmuş istifadəçinin məlumatları
        var user = await _currentUser.GetUserAsync(User);

        if (user != null)
        {
            dto.PassengerName = $"{user.FirstName} {user.LastName}".Trim();
            dto.Email = user.Email ?? string.Empty;
            dto.Phone = user.PhoneNumber ?? string.Empty;
        }

        FillBookViewBag(hotel, dto);

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HotelTravel.Web.Filters.GuestOnly]
    public async Task<IActionResult> Book(CreateTaxiBookingDto dto)
    {
        if (ModelState.IsValid)
        {
            var result = await _taxiService.BookAsync(
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

        var hotel = await _taxiService.GetHotelAsync(dto.HotelId);

        if (hotel is null)
            return NotFound();

        FillBookViewBag(hotel, dto);

        return View(dto);
    }


    // =========================
    // CONFIRMATION
    // =========================
    [HttpGet]
    public async Task<IActionResult> Confirmation(string confirmationCode)
    {
        var booking = await _taxiService.GetByCodeAsync(confirmationCode);

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
        var result = await _taxiService.CancelByUserAsync(
            id,
            _currentUser.GetUserId(User)!);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction("MyBookings", "Bookings");
    }


    private void FillBookViewBag(TaxiHotelDto hotel, CreateTaxiBookingDto dto)
    {
        var vehicleType = Enum.IsDefined(dto.VehicleType)
            ? dto.VehicleType
            : TaxiVehicleType.Standard;

        ViewBag.Hotel = hotel;
        ViewBag.Vehicle = TaxiRules.GetVehicle(vehicleType);
        ViewBag.Price = TaxiRules.CalculatePrice(vehicleType, hotel.DistanceKm);
        ViewBag.Fits = TaxiRules.Fits(vehicleType, dto.Passengers, dto.Luggage);
    }
}
