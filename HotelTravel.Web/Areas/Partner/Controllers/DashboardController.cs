using HotelTravel.Application.DTOs.Availability;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using HotelTravel.Web.Models;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.Web.Areas.Partner.Controllers;

// Otel partnyoru (otel işçisi): öz otelinin sorğularını təsdiqləyir / rədd edir.
// Otel həmişə API-dən götürülür (formdan yox), ona görə başqa otelə toxunmaq olmur.
[Area("Partner")]
[Authorize(Roles = "HotelPartner")]
public class DashboardController : Controller
{
    private readonly IPartnerApiClient _partner;
    private readonly IHotelAvailabilityService _availability;
    private readonly IBookingService _bookings;

    public DashboardController(
        IPartnerApiClient partner,
        IHotelAvailabilityService availability,
        IBookingService bookings)
    {
        _partner = partner;
        _availability = availability;
        _bookings = bookings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(BookingStatus? status)
    {
        var me = await _partner.MeAsync();

        if (me is null)
            return View("NotLinked");

        var overview = await _availability.GetAsync(me.HotelId) ?? new HotelAvailabilityDto();
        var bookings = await _availability.GetBookingsAsync(me.HotelId, status);

        var model = new PartnerDashboardViewModel
        {
            Me = me,
            Overview = overview,
            Bookings = bookings,
            Status = status
        };

        // Gözləyən sorğular üçün yoxlama (üst-üstə düşən rezervasiya, bağlı günlər)
        foreach (var pending in bookings.Where(b => b.Status == BookingStatus.Pending).Take(30))
        {
            var check = await _availability.CheckBookingAsync(pending.Id);

            if (check != null)
                model.Checks[pending.Id] = check;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _bookings.ConfirmRequestAsync(id);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _bookings.RejectRequestAsync(id);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
