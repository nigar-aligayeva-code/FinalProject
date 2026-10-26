using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using HotelTravel.Web.Services;
using HotelTravel.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelTravel.Web.Controllers;

public class BookingsController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IRoomService _roomService;
    private readonly CurrentUserAccessor _currentUser;
    private readonly ICarRentalService _carRentalService;
    private readonly IVoucherApiClient _voucherApi;
    private readonly IWeatherService _weatherService;
    private readonly IPaymentApiClient _payments;

    public BookingsController(
        IBookingService bookingService,
        IRoomService roomService,
        CurrentUserAccessor currentUser,
        ICarRentalService carRentalService,
        IVoucherApiClient voucherApi,
        IWeatherService weatherService,
        IPaymentApiClient payments)
    {
        _weatherService = weatherService;
        _payments = payments;
        _bookingService = bookingService;
        _roomService = roomService;
        _currentUser = currentUser;
        _carRentalService = carRentalService;
        _voucherApi = voucherApi;
    }


    // =========================
    // ADMIN - ALL BOOKINGS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bookings =
            await _bookingService.GetAllAsync();

        return View(bookings);
    }


    // =========================
    // ADMIN - BOOKING DETAILS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var booking =
            await _bookingService.GetByIdAsync(id);

        if (booking is null)
            return NotFound();

        return View(booking);
    }


    // =========================
    // PUBLIC - FIND MY BOOKING
    // =========================


    [HttpGet]
    public async Task<IActionResult> Confirmation(
    string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction(nameof(Find));

        var booking =
            await _bookingService
                .GetByConfirmationCodeAsync(
                    confirmationCode);

        if (booking is null)
            return NotFound();

        ViewBag.PaymentsEnabled = await _payments.IsEnabledAsync();

        // Qalma günlərinin havası
        if (booking.HotelId > 0)
            ViewBag.Weather = await _weatherService.GetForHotelAsync(booking.HotelId, booking.CheckInDate, booking.CheckOutDate);

        return View(booking);
    }
    [HttpGet]
    public async Task<IActionResult> Find(
     string? confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return View();

        var booking =
            await _bookingService
                .GetByConfirmationCodeAsync(
                    confirmationCode);

        if (booking is null)
        {
            ViewBag.ErrorMessage =
                "Booking was not found. Please check your confirmation code.";

            return View();
        }

        ViewBag.PaymentsEnabled = await _payments.IsEnabledAsync();

        return View("FindResult", booking);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FindBooking(
     string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            ViewBag.ErrorMessage =
                "Please enter your confirmation code.";

            return View("Find");
        }

        var booking =
            await _bookingService
                .GetByConfirmationCodeAsync(
                    confirmationCode);

        if (booking is null)
        {
            ViewBag.ErrorMessage =
                "Booking was not found. Please check your confirmation code.";

            return View("Find");
        }

        ViewBag.PaymentsEnabled = await _payments.IsEnabledAsync();

        return View("FindResult", booking);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelByGuest(
    string confirmationCode,
    string email)
    {
        var result =
            await _bookingService.CancelByGuestAsync(
                confirmationCode,
                email);

        if (!result.Success)
        {
            TempData["ErrorMessage"] =
                result.Message;

            return RedirectToAction(
                nameof(Find),
                new
                {
                    confirmationCode
                });
        }

        TempData["SuccessMessage"] =
            result.Message;

        return RedirectToAction(
            nameof(Find),
            new
            {
                confirmationCode
            });
    }

    // =========================
    // ADMIN - OCCUPANCY CALENDAR
    // =========================
    [Authorize(Roles = "Admin")]

    [HttpGet]
    public async Task<IActionResult> Calendar()
    {
        var bookings =
            await _bookingService.GetOccupancyCalendarAsync();

        var rooms =
            await _roomService.GetAllAsync();

        var model = new OccupancyCalendarViewModel
        {
            Bookings = bookings,
            Rooms = rooms
        };

        return View(model);
    }


    // =========================
    // ADMIN - UPDATE STATUS
    // =========================
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        BookingStatus status)
    {
        var result =
            await _bookingService
                .UpdateStatusAsync(id, status);

        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }


    // =========================
    // PUBLIC - CREATE BOOKING
    // =========================
    [HttpGet]
    public async Task<IActionResult> Create(
        int? roomId,
        DateTime? checkInDate,
        DateTime? checkOutDate,
        int? adultCount,
        int? childrenCount)
    {
        // Search Rooms -> Book Now
        if (roomId.HasValue &&
            checkInDate.HasValue &&
            checkOutDate.HasValue)
        {
            await LoadRoomsAsync(roomId);

            var dto = new CreateBookingDto
            {
                RoomId = roomId.Value,
                CheckInDate = checkInDate.Value,
                CheckOutDate = checkOutDate.Value,
                AdultCount = adultCount ?? 1,
                ChildrenCount = childrenCount ?? 0
            };

            ViewBag.FromSearch = true;

            await LoadBookingRoomAsync(roomId.Value);

            // Daxil olmuş istifadəçinin məlumatlarını avtomatik doldur
            var user = await _currentUser.GetUserAsync(User);

            if (user != null)
            {
                dto.FirstName = user.FirstName ?? string.Empty;
                dto.LastName = user.LastName ?? string.Empty;
                dto.Email = user.Email ?? string.Empty;
                dto.Phone = user.PhoneNumber ?? string.Empty;
            }

            return View(dto);
        }

        // Otaq və tarix seçilməyibsə
        // əvvəl Search Rooms-a getsin
        return RedirectToAction(
            "Search",
            "Rooms");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadRoomsAsync(dto.RoomId);
            await LoadBookingRoomAsync(dto.RoomId);

            return View(dto);
        }

        // Daxil olubsa, rezervasiya "My Bookings"-də görünəcək
        var result =
            await _bookingService.CreateAsync(
                dto,
                _currentUser.GetUserId(User));

        if (!result.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            await LoadRoomsAsync(dto.RoomId);
            await LoadBookingRoomAsync(dto.RoomId);

            return View(dto);
        }

        // Rezervasiya "sorğu" kimi yaranır (Pending): otel təsdiqləyəndən sonra ödəniş açılır
        return RedirectToAction(
    nameof(Confirmation),
    new
    {
        confirmationCode =
            result.ConfirmationCode
    });
    }


    // =========================
    // F4 - MY BOOKINGS (GUEST DASHBOARD)
    // =========================
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var userId = _currentUser.GetUserId(User);

        var bookings =
            await _bookingService.GetByUserIdAsync(userId!);

        // Maşın icarələri də My Bookings-də görünsün
        ViewBag.CarRentals =
            await _carRentalService.GetByUserAsync(userId!);

        // Təsdiqlənmiş, ödənilməmiş rezervasiyada "Pay now" düyməsi üçün
        ViewBag.PaymentsEnabled = await _payments.IsEnabledAsync();

        return View(bookings);
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result =
            await _bookingService.CancelByUserAsync(
                id,
                _currentUser.GetUserId(User)!);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
            result.Message;

        return RedirectToAction(nameof(MyBookings));
    }


    // =========================
    // F4 - MODIFY BOOKING
    // =========================
    [HttpGet]
    public async Task<IActionResult> Modify(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction(nameof(Find));

        var booking =
            await _bookingService.GetByConfirmationCodeAsync(
                confirmationCode);

        if (booking is null)
            return NotFound();

        var error = HotelTravel.Application.Services.BookingRules
            .CanGuestChange(
                booking.Status,
                booking.CheckInDate,
                DateTime.Today);

        if (error != null)
        {
            TempData["ErrorMessage"] = error;

            return RedirectToAction(
                nameof(Find),
                new { confirmationCode });
        }

        var dto = new ModifyBookingDto
        {
            ConfirmationCode = booking.ConfirmationCode,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            AdultCount = booking.AdultCount,
            ChildrenCount = booking.ChildrenCount
        };

        await LoadModifyPageAsync(booking);

        return View(dto);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Modify(ModifyBookingDto dto)
    {
        var booking =
            await _bookingService.GetByConfirmationCodeAsync(
                dto.ConfirmationCode);

        if (booking is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadModifyPageAsync(booking);
            return View(dto);
        }

        var result =
            await _bookingService.ModifyAsync(
                dto,
                _currentUser.GetUserId(User));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            await LoadModifyPageAsync(booking);
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;

        return RedirectToAction(
            nameof(Find),
            new { confirmationCode = booking.ConfirmationCode });
    }


    // =========================
    // VOUCHER (çap / PDF + QR kod)
    // =========================
    [HttpGet]
    public async Task<IActionResult> Voucher(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction(nameof(Find));

        var booking =
            await _bookingService.GetByConfirmationCodeAsync(
                confirmationCode);

        if (booking is null)
            return NotFound();

        // QR kod skan edildikdə bu rezervasiya açılsın
        ViewBag.QrUrl = Url.Action(
            nameof(Find),
            "Bookings",
            new { confirmationCode = booking.ConfirmationCode },
            Request.Scheme);

        return View(booking);
    }


    // =========================
    // VOUCHER PDF (API-də yaradılır)
    // =========================
    [HttpGet]
    public async Task<IActionResult> VoucherPdf(string confirmationCode)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction(nameof(Find));

        var pdf = await _voucherApi.GetPdfAsync(confirmationCode);

        if (pdf is null)
            return NotFound();

        return File(pdf, "application/pdf", $"Tripio-{confirmationCode.Trim().ToUpperInvariant()}.pdf");
    }


    // =========================
    // ADD TO CALENDAR (.ics)
    // =========================
    [HttpGet]
    public async Task<IActionResult> CalendarFile(string confirmationCode)
    {
        var booking = await _bookingService.GetByConfirmationCodeAsync(confirmationCode);

        if (booking is null)
            return NotFound();

        var ics = CalendarRules.BuildIcs(CalendarRules.ForHotel(
            booking.ConfirmationCode, booking.HotelName ?? "Hotel", booking.HotelAddress, booking.HotelCity,
            booking.CheckInDate, booking.CheckOutDate, booking.RoomName));

        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", $"Tripio-{booking.ConfirmationCode}.ics");
    }


    // =========================
    // TƏSDİQ MƏKTUBUNU YENİDƏN GÖNDƏR
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendVoucherEmail(string confirmationCode, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
            return RedirectToAction(nameof(Find));

        var result = await _voucherApi.SendEmailAsync(confirmationCode);

        TempData[result.Success ? "VoucherEmailSuccess" : "VoucherEmailError"] = result.Message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Confirmation), new { confirmationCode });
    }


    // =========================
    // PRICE PREVIEW (AJAX)
    // Promo kod və Explorer reward-u formda göstərmək üçün
    // =========================
    [HttpGet]
    public async Task<IActionResult> PricePreview(
        int roomId,
        DateTime checkInDate,
        DateTime checkOutDate,
        string? email,
        string? promoCode)
    {
        var quote =
            await _bookingService.GetPriceQuoteAsync(
                roomId,
                checkInDate,
                checkOutDate,
                email,
                _currentUser.GetUserId(User),
                promoCode);

        return Json(new
        {
            success = quote.Success,
            error = quote.Error,
            nights = quote.Nights,
            pricePerNight = quote.PricePerNight,
            baseTotal = quote.BaseTotal,
            discountPercent = quote.DiscountPercent,
            discountAmount = quote.DiscountAmount,
            total = quote.Total,
            discountNote = quote.DiscountNote,
            promoError = quote.PromoError,
            appliedPromoCode = quote.AppliedPromoCode,
            loyaltyEligible = quote.LoyaltyEligible,
            loyaltyCity = quote.LoyaltyCity
        });
    }


    // =========================
    // PRIVATE HELPER
    // =========================
    private async Task LoadBookingRoomAsync(int roomId)
    {
        ViewBag.Room = await _roomService.GetByIdAsync(roomId);

        // Rezervasiya formunda "indi kartla ödə" seçimi yalnız Stripe aktivdirsə görünür
        ViewBag.PaymentsEnabled = await _payments.IsEnabledAsync();
    }

    private async Task LoadModifyPageAsync(GetBookingDto booking)
    {
        var room = await _roomService.GetByIdAsync(booking.RoomId);

        ViewBag.Booking = booking;
        ViewBag.PricePerNight = room?.PricePerNight ?? 0;
        ViewBag.Capacity = room?.Capacity ?? 0;

        // Sahib daxil olubsa, email soruşulmur
        var userId = _currentUser.GetUserId(User);

        var myBookings = userId == null
            ? new List<GetBookingDto>()
            : await _bookingService.GetByUserIdAsync(userId);

        ViewBag.IsOwner = myBookings.Any(b => b.Id == booking.Id);
    }

    private async Task LoadRoomsAsync(
        int? selectedRoomId = null)
    {
        var rooms =
            await _roomService.GetAllAsync();

        var roomItems =
            rooms.Select(r => new
            {
                r.Id,

                DisplayName =
                    $"{r.RoomNumber} - {r.Name} - {r.PricePerNight:0.00} AZN"
            });

        ViewBag.Rooms =
            new SelectList(
                roomItems,
                "Id",
                "DisplayName",
                selectedRoomId);
    }
}
