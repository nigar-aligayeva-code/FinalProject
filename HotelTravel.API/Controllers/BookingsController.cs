using HotelTravel.API.Services;
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Hotel room bookings: create, price quote, find by code, modify, cancel, PDF voucher and admin management.</summary>
[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IBookingEmailService _bookingEmails;
    private readonly IVoucherPdfService _voucherPdf;
    private readonly IActivityNotifier _notifier;
    private readonly IPaymentService _payments;
    private readonly IHotelAvailabilityService _availability;
    private readonly PartnerScope _scope;
    private readonly IPhoneCallService _phoneCalls;

    public BookingsController(
        IBookingService bookingService,
        IBookingEmailService bookingEmails,
        IVoucherPdfService voucherPdf,
        IActivityNotifier notifier,
        IPaymentService payments,
        IHotelAvailabilityService availability,
        PartnerScope scope,
        IPhoneCallService phoneCalls)
    {
        _phoneCalls = phoneCalls;
        _availability = availability;
        _scope = scope;
        _bookingService = bookingService;
        _bookingEmails = bookingEmails;
        _voucherPdf = voucherPdf;
        _notifier = notifier;
        _payments = payments;
    }

    // Admin istənilən sorğuya, otel partnyoru yalnız öz otelinin sorğusuna cavab verir
    private async Task<bool> CanAnswerAsync(int bookingId)
    {
        if (User.IsInRole("Admin"))
            return true;

        var check = await _availability.CheckBookingAsync(bookingId);

        return check != null && await _scope.CanManageHotelAsync(User, check.HotelId);
    }

    // Ləğvdən sonra: onlayn ödənilibsə pulu (cərimə çıxılmaqla) qaytar, məktub göndər, bildiriş et.
    // Refund məktubdan əvvəl edilir ki, məktubda qaytarılan məbləğ görünsün.
    private async Task AfterCancelAsync(string confirmationCode, bool byHotel)
    {
        await _payments.RefundCancelledAsync(confirmationCode);
        await _bookingEmails.SendCancellationAsync(confirmationCode, byHotel);

        if (byHotel)
        {
            await _notifier.GuestBookingUpdateAsync(confirmationCode, "cancelled");
        }
        else
        {
            await _notifier.HotelCancelledAsync(confirmationCode);
            await _notifier.GuestBookingUpdateAsync(confirmationCode, "cancelled_by_you");
        }
    }

    // =========================
    // GUEST
    // =========================

    // POST: api/bookings  (token varsa rezervasiya istifadəçiyə bağlanır)
    /// <summary>Request a room. The booking starts as Pending until the hotel (or admin) confirms it. If a token is sent, the booking is linked to the user. Sends a "request received" email.</summary>
    [HttpPost]
    [HotelTravel.API.Filters.GuestOnly]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        var result = await _bookingService.CreateAsync(dto, CurrentUserId);

        // "Sorğu qəbul olundu" məktubu (alınmasa rezervasiya yenə uğurludur, EmailLog-da görünür)
        if (result.Success && !string.IsNullOrWhiteSpace(result.ConfirmationCode))
        {
            await _bookingEmails.SendConfirmationAsync(result.ConfirmationCode);
            await _notifier.HotelBookedAsync(result.ConfirmationCode);
            await _notifier.GuestBookingUpdateAsync(result.ConfirmationCode, "requested");
            await _notifier.PartnerNewRequestAsync(result.ConfirmationCode);
        }

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/bookings/price-quote?roomId=1&checkInDate=...&checkOutDate=...&email=...&promoCode=...
    /// <summary>Price for a room and dates, including promo code and loyalty discounts. Nothing is booked.</summary>
    [HttpGet("price-quote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PriceQuoteDto), 200)]
    public async Task<IActionResult> PriceQuote(
        [FromQuery] int roomId,
        [FromQuery] DateTime checkInDate,
        [FromQuery] DateTime checkOutDate,
        [FromQuery] string? email,
        [FromQuery] string? promoCode)
        => Ok(await _bookingService.GetPriceQuoteAsync(
            roomId, checkInDate, checkOutDate, email, CurrentUserId, promoCode));

    // GET: api/bookings/by-code/BK-1234ABCD
    /// <summary>Find a booking by its confirmation code.</summary>
    [HttpGet("by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetBookingDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(
            await _bookingService.GetByConfirmationCodeAsync(confirmationCode),
            "Booking not found.");

    // GET: api/bookings/by-code/BK-1234ABCD/voucher.pdf
    /// <summary>Download the booking voucher as a PDF with a QR code.</summary>
    [HttpGet("by-code/{confirmationCode}/voucher.pdf")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResultDto), 404)]
    public async Task<IActionResult> VoucherPdf(string confirmationCode)
    {
        var booking = await _bookingService.GetByConfirmationCodeAsync(confirmationCode);

        if (booking == null)
            return NotFound(ApiResultDto.Fail("Booking not found."));

        var pdf = _voucherPdf.Create(booking, _bookingEmails.GetBookingUrl(booking.ConfirmationCode));

        return File(pdf, "application/pdf", $"Tripio-{booking.ConfirmationCode}.pdf");
    }

    // POST: api/bookings/by-code/BK-1234ABCD/send-email  (məktub yalnız rezervasiyanın öz email-inə gedir)
    /// <summary>Send the booking email again (confirmation with PDF voucher, or "request received" while pending). It always goes to the email on the booking.</summary>
    [HttpPost("by-code/{confirmationCode}/send-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> SendEmail(string confirmationCode)
    {
        var result = await _bookingEmails.SendConfirmationAsync(confirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // POST: api/bookings/cancel  (kod + email ilə)
    /// <summary>Cancel a booking with confirmation code + email (guest without account).</summary>
    [HttpPost("cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelByGuest([FromBody] CancelBookingDto dto)
    {
        var result = await _bookingService.CancelByGuestAsync(dto.ConfirmationCode, dto.Email);

        if (result.Success)
            await AfterCancelAsync(dto.ConfirmationCode, byHotel: false);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // POST: api/bookings/modify
    /// <summary>Change the dates or guests of a booking.</summary>
    [HttpPost("modify")]
    [AllowAnonymous]
    public async Task<IActionResult> Modify([FromBody] ModifyBookingDto dto)
    {
        var result = await _bookingService.ModifyAsync(dto, CurrentUserId);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // =========================
    // MY BOOKINGS (token lazımdır)
    // =========================

    // GET: api/bookings/mine
    /// <summary>Bookings of the signed-in user.</summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<GetBookingDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _bookingService.GetByUserIdAsync(CurrentUserId!));

    // POST: api/bookings/5/cancel-mine
    /// <summary>Cancel one of the signed-in user's bookings.</summary>
    [HttpPost("{id:int}/cancel-mine")]
    [Authorize]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _bookingService.CancelByUserAsync(id, CurrentUserId!);

        if (result.Success)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking != null)
                await AfterCancelAsync(booking.ConfirmationCode, byHotel: false);
        }

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // =========================
    // ADMIN
    // =========================

    // GET: api/bookings
    /// <summary>All bookings (admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<GetBookingDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _bookingService.GetAllAsync());

    // GET: api/bookings/5
    /// <summary>One booking by id (admin).</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetBookingDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _bookingService.GetByIdAsync(id), "Booking not found.");

    // PUT: api/bookings/5/status
    /// <summary>Change booking status: Confirmed, CheckedIn, CheckedOut or Cancelled (admin). Pending requests should use confirm / reject so the guest gets an email.</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] StatusUpdateDto<BookingStatus> dto)
    {
        var updated = await _bookingService.UpdateStatusAsync(id, dto.Status);

        // Otel ləğv etdi: qonağa tam pul qaytarılır, məktub + canlı bildiriş
        if (updated && dto.Status == BookingStatus.Cancelled)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking != null)
                await AfterCancelAsync(booking.ConfirmationCode, byHotel: true);
        }

        return FromBool(
            updated,
            "Booking status updated.",
            "Booking status could not be changed.");
    }

    // POST: api/bookings/5/confirm
    /// <summary>The hotel has a free room: confirm a pending request (admin or the hotel's partner). The guest gets the confirmation email with the PDF voucher and can pay.</summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin," + PartnerScope.PartnerRole)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> ConfirmRequest(int id)
    {
        if (!await CanAnswerAsync(id))
            return Forbid();

        var result = await _bookingService.ConfirmRequestAsync(id);

        if (result.Success)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking != null)
            {
                await _bookingEmails.SendConfirmationAsync(booking.ConfirmationCode);
                await _notifier.GuestBookingUpdateAsync(booking.ConfirmationCode, "confirmed");
            }
        }

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // POST: api/bookings/5/call-hotel
    /// <summary>Admin: start a real phone call to the hotel (Twilio). When the phone is answered, the request details are read out. Needs the "Twilio" settings in User Secrets.</summary>
    [HttpPost("{id:int}/call-hotel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> CallHotel(int id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id);

        if (booking == null)
            return NotFound(ApiResultDto.Fail("Booking not found."));

        if (string.IsNullOrWhiteSpace(booking.HotelPhone))
            return BadRequest(ApiResultDto.Fail("The hotel has no phone number."));

        var (success, message) = await _phoneCalls.CallAsync(
            booking.HotelPhone, HotelTravel.Application.Services.HotelCallScript.For(booking), cancellationToken);

        return FromResult(success ? ApiResultDto.Ok(message) : ApiResultDto.Fail(message));
    }

    // POST: api/bookings/5/reject
    /// <summary>The hotel has no free room: reject a pending request (admin or the hotel's partner). The dates become free and the guest gets an email.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin," + PartnerScope.PartnerRole)]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> RejectRequest(int id)
    {
        if (!await CanAnswerAsync(id))
            return Forbid();

        var result = await _bookingService.RejectRequestAsync(id);

        if (result.Success)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking != null)
            {
                await _bookingEmails.SendRejectionAsync(booking.ConfirmationCode);
                await _notifier.GuestBookingUpdateAsync(booking.ConfirmationCode, "rejected");
            }
        }

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/bookings/pending-count
    /// <summary>How many booking requests are waiting for the hotel's answer (admin).</summary>
    [HttpGet("pending-count")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> PendingCount()
        => Ok(await _bookingService.CountPendingAsync());

    // GET: api/bookings/occupancy-calendar
    /// <summary>Bookings per room and day for the occupancy calendar (admin).</summary>
    [HttpGet("occupancy-calendar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<OccupancyCalendarDto>), 200)]
    public async Task<IActionResult> OccupancyCalendar()
        => Ok(await _bookingService.GetOccupancyCalendarAsync());
}
