using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IBookingEmailService _bookingEmails;
    private readonly IVoucherPdfService _voucherPdf;

    public BookingsController(
        IBookingService bookingService,
        IBookingEmailService bookingEmails,
        IVoucherPdfService voucherPdf)
    {
        _bookingService = bookingService;
        _bookingEmails = bookingEmails;
        _voucherPdf = voucherPdf;
    }

    // =========================
    // GUEST
    // =========================

    // POST: api/bookings  (token varsa rezervasiya istifadəçiyə bağlanır)
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    [ProducesResponseType(typeof(ApiResultDto), 400)]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
    {
        var result = await _bookingService.CreateAsync(dto, CurrentUserId);

        // Təsdiq məktubu + PDF (alınmasa rezervasiya yenə uğurludur, EmailLog-da görünür)
        if (result.Success && !string.IsNullOrWhiteSpace(result.ConfirmationCode))
            await _bookingEmails.SendConfirmationAsync(result.ConfirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message, code: result.ConfirmationCode)
            : ApiResultDto.Fail(result.Message));
    }

    // GET: api/bookings/price-quote?roomId=1&checkInDate=...&checkOutDate=...&email=...&promoCode=...
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
    [HttpGet("by-code/{confirmationCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetBookingDto), 200)]
    public async Task<IActionResult> GetByCode(string confirmationCode)
        => OkOrNotFound(
            await _bookingService.GetByConfirmationCodeAsync(confirmationCode),
            "Booking not found.");

    // GET: api/bookings/by-code/BK-1234ABCD/voucher.pdf
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
    [HttpPost("cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelByGuest([FromBody] CancelBookingDto dto)
    {
        var result = await _bookingService.CancelByGuestAsync(dto.ConfirmationCode, dto.Email);

        if (result.Success)
            await _bookingEmails.SendCancellationAsync(dto.ConfirmationCode);

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // POST: api/bookings/modify
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
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<GetBookingDto>), 200)]
    public async Task<IActionResult> Mine()
        => Ok(await _bookingService.GetByUserIdAsync(CurrentUserId!));

    // POST: api/bookings/5/cancel-mine
    [HttpPost("{id:int}/cancel-mine")]
    [Authorize]
    public async Task<IActionResult> CancelMine(int id)
    {
        var result = await _bookingService.CancelByUserAsync(id, CurrentUserId!);

        if (result.Success)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking != null)
                await _bookingEmails.SendCancellationAsync(booking.ConfirmationCode);
        }

        return FromResult(result.Success
            ? ApiResultDto.Ok(result.Message)
            : ApiResultDto.Fail(result.Message));
    }

    // =========================
    // ADMIN
    // =========================

    // GET: api/bookings
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<GetBookingDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _bookingService.GetAllAsync());

    // GET: api/bookings/5
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetBookingDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _bookingService.GetByIdAsync(id), "Booking not found.");

    // PUT: api/bookings/5/status
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
    [HttpGet("occupancy-calendar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<OccupancyCalendarDto>), 200)]
    public async Task<IActionResult> OccupancyCalendar()
        => Ok(await _bookingService.GetOccupancyCalendarAsync());
}
