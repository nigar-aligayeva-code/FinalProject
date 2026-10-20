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

    public BookingsController(
        IBookingService bookingService,
        IBookingEmailService bookingEmails,
        IVoucherPdfService voucherPdf,
        IActivityNotifier notifier)
    {
        _bookingService = bookingService;
        _bookingEmails = bookingEmails;
        _voucherPdf = voucherPdf;
        _notifier = notifier;
    }

    // =========================
    // GUEST
    // =========================

    // POST: api/bookings  (token varsa rezervasiya istifadəçiyə bağlanır)
    /// <summary>Book a room. If a token is sent, the booking is linked to the user. Sends a confirmation email with a PDF voucher.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        var result = await _bookingService.CreateAsync(dto, CurrentUserId);

        // Təsdiq məktubu + PDF (alınmasa rezervasiya yenə uğurludur, EmailLog-da görünür)
        if (result.Success && !string.IsNullOrWhiteSpace(result.ConfirmationCode))
        {
            await _bookingEmails.SendConfirmationAsync(result.ConfirmationCode);
            await _notifier.HotelBookedAsync(result.ConfirmationCode);
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
    /// <summary>Send the confirmation email (with PDF voucher) again. It always goes to the email on the booking.</summary>
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
        {
            await _bookingEmails.SendCancellationAsync(dto.ConfirmationCode);
            await _notifier.HotelCancelledAsync(dto.ConfirmationCode);
        }

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
            {
                await _bookingEmails.SendCancellationAsync(booking.ConfirmationCode);
                await _notifier.HotelCancelledAsync(booking.ConfirmationCode);
            }
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
    /// <summary>Change booking status: Confirmed, CheckedIn, CheckedOut or Cancelled (admin).</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] StatusUpdateDto<BookingStatus> dto)
        => FromBool(
            await _bookingService.UpdateStatusAsync(id, dto.Status),
            "Booking status updated.",
            "Booking status could not be changed.");

    // GET: api/bookings/occupancy-calendar
    /// <summary>Bookings per room and day for the occupancy calendar (admin).</summary>
    [HttpGet("occupancy-calendar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<OccupancyCalendarDto>), 200)]
    public async Task<IActionResult> OccupancyCalendar()
        => Ok(await _bookingService.GetOccupancyCalendarAsync());
}
