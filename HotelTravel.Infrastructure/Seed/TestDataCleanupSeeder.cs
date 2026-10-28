using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

/// <summary>
/// Yoxlama zamanı (Swagger / admin panel) əllə yaradılmış "Test Room" kimi otaqları saytdan gizlədir.
/// Yalnız adı "test" sözü ilə başlayan otaqlar və yalnız aktiv rezervasiyası olmayanlar;
/// silinmir, IsDeleted = true olur (tarixçə qalır).
/// </summary>
public static class TestDataCleanupSeeder
{
    public static bool LooksLikeTestRoom(string? name)
    {
        var n = (name ?? string.Empty).Trim();

        return n.Equals("test", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("test ", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("test-", StringComparison.OrdinalIgnoreCase)
            || n.StartsWith("test_", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task SeedAsync(AppDbContext context)
    {
        var candidates = await context.Rooms
            .Where(r => !r.IsDeleted && r.Name.ToLower().StartsWith("test"))
            .Select(r => new
            {
                Room = r,
                HasActiveBooking = r.Bookings.Any(b => !b.IsDeleted &&
                    (b.Status == BookingStatus.Pending ||
                     b.Status == BookingStatus.Confirmed ||
                     b.Status == BookingStatus.CheckedIn))
            })
            .ToListAsync();

        var changed = 0;

        foreach (var c in candidates.Where(c => !c.HasActiveBooking && LooksLikeTestRoom(c.Room.Name)))
        {
            c.Room.IsDeleted = true;
            c.Room.UpdatedAt = DateTime.UtcNow;
            changed++;
        }

        if (changed > 0)
            await context.SaveChangesAsync();
    }
}
