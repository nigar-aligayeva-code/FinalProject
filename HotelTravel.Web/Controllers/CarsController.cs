using HotelTravel.Application.DTOs.Cars;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using HotelTravel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// "Car rental" tab-ı
public class CarsController : Controller
{
    private readonly ICarRentalService _carRentalService;
    private readonly CurrentUserAccessor _currentUser;

    public CarsController(
        ICarRentalService carRentalService,
        CurrentUserAccessor currentUser)
    {
        _carRentalService = carRentalService;
        _currentUser = currentUser;
    }


    // =========================
    // SEARCH
    // =========================
    [HttpGet]
    public async Task<IActionResult> Index(
        string? city,
        DateTime? pickupDate,
        DateTime? returnDate)
    {
        var cities = await _carRentalService.GetCitiesAsync();

        var selected = cities.FirstOrDefault(c =>
                string.Equals(c, city?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? cities.FirstOrDefault();

        // Tarix seçilməyibsə: sabah -> 3 gün
        var pickup = (pickupDate ?? DateTime.Today.AddDays(1)).Date;
        var dropoff = (returnDate ?? pickup.AddDays(3)).Date;

        var error = CarRentalRules.ValidateDates(pickup, dropoff, DateTime.Today);

        ViewBag.Cities = cities;
        ViewBag.SelectedCity = selected;
        ViewBag.PickupDate = pickup;
        ViewBag.ReturnDate = dropoff;
        ViewBag.DateError = error;

        var cars = selected == null || error != null
            ? new List<CarDto>()
            : await _carRentalService.SearchAsync(selected, pickup, dropoff);

        return View(cars);
    }


    // =========================
    // BOOK
    // =========================
    [HttpGet]
    public async Task<IActionResult> Book(
        int carId,
        DateTime pickupDate,
        DateTime returnDate)
    {
        var car = await _carRentalService.GetCarAsync(carId, pickupDate, returnDate);

        if (car is null)
            return NotFound();

        var dto = new CreateCarRentalDto
        {
            CarId = carId,
            PickupDate = pickupDate.Date,
            ReturnDate = returnDate.Date
        };

        // Daxil olmuş istifadəçinin məlumatları
        var user = await _currentUser.GetUserAsync(User);

        if (user != null)
        {
            dto.DriverName = $"{user.FirstName} {user.LastName}".Trim();
            dto.Email = user.Email ?? string.Empty;
            dto.Phone = user.PhoneNumber ?? string.Empty;
        }

        ViewBag.Car = car;

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(CreateCarRentalDto dto)
    {
        if (ModelState.IsValid)
        {
            var result = await _carRentalService.BookAsync(
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

        var car = await _carRentalService.GetCarAsync(dto.CarId, dto.PickupDate, dto.ReturnDate);

        if (car is null)
            return NotFound();

        ViewBag.Car = car;

        return View(dto);
    }


    // =========================
    // CONFIRMATION
    // =========================
    [HttpGet]
    public async Task<IActionResult> Confirmation(string confirmationCode)
    {
        var rental = await _carRentalService.GetByCodeAsync(confirmationCode);

        if (rental is null)
            return NotFound();

        return View(rental);
    }


    // =========================
    // CANCEL (My Bookings)
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _carRentalService.CancelByUserAsync(
            id,
            _currentUser.GetUserId(User)!);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction("MyBookings", "Bookings");
    }
}
