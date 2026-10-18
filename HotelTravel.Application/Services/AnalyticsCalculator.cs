namespace HotelTravel.Application.Services;

// Analitika hesablamaları (unit test üçün ayrıca, verilənlər bazası yoxdur)
public static class AnalyticsCalculator
{
    // part / total * 100, 1 rəqəm dəqiqliklə
    public static decimal Percent(decimal part, decimal total)
        => total <= 0 ? 0 : Math.Round(part / total * 100m, 1, MidpointRounding.AwayFromZero);

    // Verilən dövrdə doluluq faizi: tutulmuş otaq-gecələr / (otaq sayı × gün sayı)
    public static decimal OccupancyPercent(
        IEnumerable<(DateTime CheckIn, DateTime CheckOut)> stays,
        int roomCount,
        DateTime from,
        int days)
    {
        if (roomCount <= 0 || days <= 0)
            return 0;

        var start = from.Date;
        var end = start.AddDays(days);

        var bookedNights = stays.Sum(s =>
        {
            var overlapStart = s.CheckIn.Date > start ? s.CheckIn.Date : start;
            var overlapEnd = s.CheckOut.Date < end ? s.CheckOut.Date : end;

            return Math.Max(0, (overlapEnd - overlapStart).Days);
        });

        var percent = Percent(bookedNights, roomCount * (decimal)days);

        return Math.Min(100m, percent);
    }

    // Qrafik üçün aylar: bu aydan monthsBack ay əvvəl və monthsAhead ay sonra
    public static List<DateTime> MonthRange(DateTime today, int monthsBack, int monthsAhead)
    {
        var first = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsBack);

        return Enumerable
            .Range(0, monthsBack + monthsAhead + 1)
            .Select(i => first.AddMonths(i))
            .ToList();
    }

    public static bool InMonth(DateTime date, DateTime monthStart)
        => date.Year == monthStart.Year && date.Month == monthStart.Month;
}
