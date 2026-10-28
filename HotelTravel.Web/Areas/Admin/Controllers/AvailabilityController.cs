using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Admin.Controllers;

// Admin: partnyoru olmayan otellər üçün boş yerləri özü idarə edir (eyni panel)
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AvailabilityController : Controller
{
    private readonly IHotelAvailabilityService _availability;

    public AvailabilityController(IHotelAvailabilityService availability)
    {
        _availability = availability;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int hotelId)
    {
        var overview = await _availability.GetAsync(hotelId);

        if (overview is null)
            return NotFound();

        return View(overview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBlock(int hotelId, CreateRoomBlockDto dto)
        => Done(hotelId, await _availability.AddBlockAsync(hotelId, dto));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBlock(int hotelId, int[] ids)
    {
        // "Bütün otaqlar" bir sətirdə göstərilir -> bir neçə bağlama birlikdə açılır
        (bool Success, string Message) last = (false, "Closed dates were not found.");

        foreach (var id in ids.Distinct())
            last = await _availability.RemoveBlockAsync(hotelId, id);

        return Done(hotelId, last);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(int hotelId, int roomId, decimal pricePerNight)
        => Done(hotelId, await _availability.UpdateRoomPriceAsync(hotelId, roomId, pricePerNight));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUpToDate(int hotelId)
    {
        if (await _availability.MarkUpToDateAsync(hotelId))
            TempData["SuccessMessage"] = "Availability marked as up to date.";

        return RedirectToAction(nameof(Index), new { hotelId });
    }

    private IActionResult Done(int hotelId, (bool Success, string Message) result)
    {
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { hotelId });
    }
}
