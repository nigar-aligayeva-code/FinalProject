using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Controllers;

// "Attractions" tab-ı: şəhər bələdçiləri
public class GuideController : Controller
{
    private readonly ICityGuideService _cityGuideService;

    public GuideController(ICityGuideService cityGuideService)
    {
        _cityGuideService = cityGuideService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city)
    {
        var cities = await _cityGuideService.GetCitiesAsync();

        // Şəhər seçilməyibsə və ya yoxdursa, birincisi
        var selected = cities.FirstOrDefault(c =>
                string.Equals(c, city?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? cities.FirstOrDefault();

        ViewBag.Cities = cities;
        ViewBag.SelectedCity = selected;

        var places = selected == null
            ? new List<HotelTravel.Application.DTOs.CityGuide.CityPlaceDto>()
            : await _cityGuideService.GetByCityAsync(selected);

        return View(places);
    }
}
