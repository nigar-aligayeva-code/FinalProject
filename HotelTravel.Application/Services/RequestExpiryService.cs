using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Application.Services;

/// <summary>
/// Otel sorğuya 24 saat ərzində cavab verməyibsə, sorğu avtomatik bağlanır:
/// tarixlər boşalır, qonaq məktub və 🔔 bildiriş alır, pul tutulmur.
/// Beləliklə qonaq sonsuz gözləmir.
/// </summary>
public class RequestExpiryService : IRequestExpiryService
{
    private readonly IAppDbContext _context;
    private readonly IBookingEmailService _emails;
    private readonly IActivityNotifier _notifier;

    public RequestExpiryService(
        IAppDbContext context,
        IBookingEmailService emails,
        IActivityNotifier notifier)
    {
        _context = context;
        _emails = emails;
        _notifier = notifier;
    }

    public async Task<int> ExpireOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var limit = utcNow.AddHours(-BookingRules.HotelAnswerHours);

        var overdue = await _context.Bookings
            .Where(b =>
                !b.IsDeleted &&
                b.Status == BookingStatus.Pending &&
                b.CreatedAt < limit)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
            return 0;

        foreach (var booking in overdue)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationFee = 0;
            booking.CancelledAt = utcNow;
            booking.UpdatedAt = utcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var booking in overdue)
        {
            await _emails.SendExpiredAsync(booking.ConfirmationCode);
            await _notifier.GuestBookingUpdateAsync(booking.ConfirmationCode, "expired");
        }

        return overdue.Count;
    }
}
