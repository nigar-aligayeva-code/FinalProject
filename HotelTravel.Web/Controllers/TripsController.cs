using HotelTravel.Application.DTOs.Trips;
using HotelTravel.Application.Interfaces;
using HotelTravel.Web.Services;
using HotelTravel.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// Trip paketi: uçuş + otel + taksi (hamısı API ilə)
public class TripsController : Controller
{
    private readonly ITripPackageService _tripService;
    private readonly CurrentUserAccessor _currentUser;
    private readonly IWeatherService _weatherService;

    public TripsController(
        ITripPackageService tripService,
        CurrentUserAccessor currentUser,
        IWeatherService weatherService)
    {
        _tripService = tripService;
        _currentUser = currentUser;
        _weatherService = weatherService;
    }

    // GET: /Trips?FromCity=Baku&ToCity=Istanbul&DepartDate=...&ReturnDate=...
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TripSearchDto search)
    {
        ViewData["Service"] = "Trips";

        var model = new TripsIndexViewModel
        {
            Search = search,
            Origins = await _tripService.GetOriginsAsync(),
            Destinations = await _tripService.GetDestinationsAsync()
        };

        // Axtarış formu boşdursa standart tarixlər
        search.DepartDate ??= DateTime.Today.AddDays(14);
        search.ReturnDate ??= search.DepartDate.Value.AddDays(4);

        if (!string.IsNullOrWhiteSpace(search.FromCity) && !string.IsNullOrWhiteSpace(search.ToCity))
        {
            model.Plan = await _tripService.PlanAsync(search);

            if (model.Plan.Error == null)
                model.Weather = await _weatherService.GetForCityAsync(search.ToCity, search.DepartDate, search.ReturnDate);
        }

        var user = await _currentUser.GetUserAsync(User);

        if (user != null)
        {
            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            model.Email = user.Email ?? string.Empty;
            model.Phone = user.PhoneNumber ?? string.Empty;
            model.MyTrips = await _tripService.GetByUserAsync(user.Id);
        }

        return View(model);
    }

    // POST: /Trips/Book
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(CreateTripPackageDto dto)
    {
        var search = new
        {
            dto.FromCity,
            dto.ToCity,
            DepartDate = dto.DepartDate.ToString("yyyy-MM-dd"),
            ReturnDate = dto.ReturnDate.ToString("yyyy-MM-dd"),
            dto.Adults,
            dto.Children,
            dto.Cabin
        };

        if (!ModelState.IsValid)
        {
            TempData["TripError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct());

            return RedirectToAction(nameof(Index), search);
        }

        var result = await _tripService.BookAsync(dto, _currentUser.GetUserId(User)!);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Code))
        {
            TempData["TripError"] = result.Message;
            return RedirectToAction(nameof(Index), search);
        }

        TempData["TripSuccess"] = result.Message;

        return RedirectToAction(nameof(Details), new { code = result.Code });
    }

    // GET: /Trips/Details?code=TP-1234ABCD
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Details(string code)
    {
        ViewData["Service"] = "Trips";

        var trip = await _tripService.GetByCodeAsync(code, _currentUser.GetUserId(User)!);

        if (trip == null)
            return NotFound();

        ViewBag.Weather = await _weatherService.GetForCityAsync(trip.ToCity, trip.DepartDate, trip.ReturnDate);

        return View(trip);
    }
}
