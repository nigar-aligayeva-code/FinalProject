using HotelTravel.Application.DTOs.Payments;
using HotelTravel.Application.Interfaces;
using HotelTravel.Application.Options;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Rezervasiyanın onlayn ödənişi (Stripe Checkout, test rejimi):
///   1) StartAsync: Stripe ödəniş səhifəsi yaradılır, session id rezervasiyada saxlanılır;
///   2) qonaq kartı Stripe-ın öz səhifəsində yazır (kart məlumatı bizə heç vaxt gəlmir);
///   3) ConfirmAsync: Stripe-dan "paid" olduğu YOXLANILIR (URL-ə etibar edilmir), sonra Paid qeyd olunur.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _context;
    private readonly IPaymentGateway _gateway;
    private readonly StripeOptions _options;

    public PaymentService(IAppDbContext context, IPaymentGateway gateway, StripeOptions options)
    {
        _context = context;
        _gateway = gateway;
        _options = options;
    }

    public bool IsEnabled => _gateway.IsEnabled;

    public async Task<PaymentResultDto> StartAsync(StartPaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (!_gateway.IsEnabled)
            return PaymentResultDto.Fail("Online payment is not available. You can pay at the hotel.");

        var code = (dto.ConfirmationCode ?? string.Empty).Trim().ToUpperInvariant();

        var booking = await _context.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .FirstOrDefaultAsync(b => !b.IsDeleted && b.ConfirmationCode.ToUpper() == code, cancellationToken);

        if (booking is null)
            return PaymentResultDto.Fail("Booking not found.");

        if (PaymentRules.WhyCannotPay(booking.Status, booking.PaymentStatus, booking.TotalPrice) is { } reason)
            return PaymentResultDto.Fail(reason);

        var nights = Math.Max(1, (booking.CheckOutDate.Date - booking.CheckInDate.Date).Days);

        var session = await _gateway.CreateCheckoutAsync(new PaymentCheckoutRequest(
            booking.ConfirmationCode,
            $"{booking.Room.Hotel.Name} · {booking.Room.Name}",
            $"{booking.CheckInDate:dd MMM yyyy} – {booking.CheckOutDate:dd MMM yyyy} · {nights} night(s) · {booking.ConfirmationCode}",
            PaymentRules.ToMinorUnits(booking.TotalPrice),
            _options.Currency,
            booking.Guest?.Email,
            dto.SuccessUrl,
            dto.CancelUrl), cancellationToken);

        booking.PaymentReference = session.SessionId;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentResultDto
        {
            Success = true,
            Message = "Redirecting to the secure payment page.",
            CheckoutUrl = session.Url,
            ConfirmationCode = booking.ConfirmationCode
        };
    }

    public async Task<PaymentResultDto> ConfirmAsync(ConfirmPaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (!_gateway.IsEnabled || string.IsNullOrWhiteSpace(dto.SessionId))
            return PaymentResultDto.Fail("Payment could not be verified.");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => !b.IsDeleted && b.PaymentReference == dto.SessionId, cancellationToken);

        if (booking is null)
            return PaymentResultDto.Fail("Payment could not be verified.");

        if (booking.PaymentStatus == PaymentStatus.Paid)
        {
            return new PaymentResultDto
            {
                Success = true,
                Message = "Payment received. Thank you!",
                ConfirmationCode = booking.ConfirmationCode
            };
        }

        // Stripe-dan soruşuruq: həqiqətən ödənilib, məbləğ və rezervasiya kodu uyğundur?
        var status = await _gateway.GetSessionAsync(dto.SessionId, cancellationToken);

        var matches =
            status.IsPaid &&
            string.Equals(status.ConfirmationCode, booking.ConfirmationCode, StringComparison.OrdinalIgnoreCase) &&
            status.AmountMinor == PaymentRules.ToMinorUnits(booking.TotalPrice);

        if (!matches)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "The payment was not completed.",
                ConfirmationCode = booking.ConfirmationCode
            };
        }

        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaidAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentResultDto
        {
            Success = true,
            Message = "Payment received. Thank you!",
            ConfirmationCode = booking.ConfirmationCode
        };
    }

    public async Task<PaymentResultDto> RefundCancelledAsync(string confirmationCode, CancellationToken cancellationToken = default)
    {
        var code = (confirmationCode ?? string.Empty).Trim().ToUpperInvariant();

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => !b.IsDeleted && b.ConfirmationCode.ToUpper() == code, cancellationToken);

        // Yalnız ləğv edilmiş və onlayn ödənilmiş rezervasiya
        if (booking is null ||
            booking.Status != BookingStatus.Cancelled ||
            booking.PaymentStatus != PaymentStatus.Paid ||
            string.IsNullOrWhiteSpace(booking.PaymentReference))
        {
            return PaymentResultDto.Fail("Nothing to refund.");
        }

        var amount = PaymentRules.RefundFor(booking.TotalPrice, booking.CancellationFee);

        if (amount > 0)
        {
            if (!_gateway.IsEnabled)
                return PaymentResultDto.Fail("The refund could not be processed automatically. The hotel will contact you.");

            var refund = await _gateway.RefundAsync(booking.PaymentReference, PaymentRules.ToMinorUnits(amount), cancellationToken);

            if (!refund.Success)
                return PaymentResultDto.Fail("The refund could not be processed automatically. The hotel will contact you.");
        }

        booking.PaymentStatus = PaymentStatus.Refunded;
        booking.RefundAmount = amount;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentResultDto
        {
            Success = true,
            Message = "The refund was sent to your card.",
            ConfirmationCode = booking.ConfirmationCode
        };
    }
}
