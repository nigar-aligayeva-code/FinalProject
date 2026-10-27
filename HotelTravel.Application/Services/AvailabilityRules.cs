namespace HotelTravel.Application.Services;

/// <summary>
/// Real boş yerlər üçün qaydalar (təmiz, test olunur).
/// Biz otelin bütün satışlarını görmürük, ona görə:
///   • otel (partnyor) başqa yerdə satdığı günləri "bağlı" edir;
///   • otel boş yerləri nə vaxt yoxladığını qeyd edir ("təzəlik");
///   • son qərarı otel verir (sorğunu təsdiqləyir / rədd edir).
/// </summary>
public static class AvailabilityRules
{
    // Bir dəfəyə ən çox 1 il bağlamaq olar
    public const int MaxBlockDays = 365;

    // Otel boş yerləri son 24 saatda yoxlayıbsa məlumat "təzədir"
    public const int FreshHours = 24;

    // İki tarix aralığı üst-üstə düşür? (son gün daxil deyil, rezervasiyalardakı kimi)
    public static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart.Date < bEnd.Date && aEnd.Date > bStart.Date;

    // Bağlama düzgündür? null -> hə, əks halda səbəb
    public static string? ValidateBlock(DateTime start, DateTime end, DateTime today)
    {
        if (start.Date < today.Date)
            return "Closed dates cannot start in the past.";

        if (end.Date <= start.Date)
            return "The end date must be after the start date.";

        if ((end.Date - start.Date).Days > MaxBlockDays)
            return $"You can close at most {MaxBlockDays} days at once.";

        return null;
    }

    public static bool IsFresh(DateTime? updatedAtUtc, DateTime nowUtc)
        => updatedAtUtc.HasValue &&
           nowUtc - updatedAtUtc.Value <= TimeSpan.FromHours(FreshHours);
}
