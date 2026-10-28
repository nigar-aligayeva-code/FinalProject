using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.Interfaces;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Partner.Controllers;

// Partnyor: bağlı tarixlər (başqa yerdə satılan günlər), qiymətlər, "yoxladım"
[Area("Partner")]
[Authorize(Roles = "HotelPartner")]
public class AvailabilityController : Controller
{
    private readonly IPartnerApiClient _partner;
    private readonly IHotelAvailabilityService _availability;

    public AvailabilityController(IPartnerApiClient partner, IHotelAvailabilityService availability)
    {
        _partner = partner;
        _availability = availability;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await _partner.MeAsync();

        if (me is null)
            return View("~/Areas/Partner/Views/Dashboard/NotLinked.cshtml");

        var overview = await _availability.GetAsync(me.HotelId);

        if (overview is null)
            return NotFound();

        return View(overview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBlock(CreateRoomBlockDto dto)
        => await RunAsync(hotelId => _availability.AddBlockAsync(hotelId, dto));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBlock(int[] ids)
        => await RunAsync(async hotelId =>
        {
            // "Bütün otaqlar" bir sətirdə göstərilir -> bir neçə bağlama birlikdə açılır
            (bool Success, string Message) last = (false, "Closed dates were not found.");

            foreach (var id in ids.Distinct())
                last = await _availability.RemoveBlockAsync(hotelId, id);

            return last;
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(int roomId, decimal pricePerNight)
        => await RunAsync(hotelId => _availability.UpdateRoomPriceAsync(hotelId, roomId, pricePerNight));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUpToDate(string? returnTo = null)
    {
        var me = await _partner.MeAsync();

        if (me != null && await _availability.MarkUpToDateAsync(me.HotelId))
            TempData["SuccessMessage"] = "Availability marked as up to date.";

        return returnTo == "dashboard"
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction(nameof(Index));
    }

    // Otel həmişə partnyorun öz oteli
    private async Task<IActionResult> RunAsync(Func<int, Task<(bool Success, string Message)>> action)
    {
        var me = await _partner.MeAsync();

        if (me is null)
            return RedirectToAction("Index", "Dashboard");

        var result = await action(me.HotelId);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
