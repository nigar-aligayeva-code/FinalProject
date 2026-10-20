using HotelTravel.Application.DTOs.Weather;

namespace HotelTravel.Application.Services;

public record WeatherWindow(DateTime Start, DateTime End, bool IsForecast, int YearShift);

// Hava qaydaları (unit test üçün ayrıca, şəbəkə yoxdur)
public static class WeatherRules
{
    // Open-Meteo proqnozu bu gün daxil 16 gündür
    public const int ForecastDays = 16;
    public const int MaxDays = 14;
    public const int DefaultDays = 7;

    // Hansı günlər üçün və hansı mənbədən (proqnoz / keçən il)
    public static WeatherWindow? GetWindow(DateTime? from, DateTime? to, DateTime today)
    {
        today = today.Date;

        var start = (from ?? today).Date;
        var end = (to ?? start.AddDays(DefaultDays - 1)).Date;

        if (end < start)
            (start, end) = (end, start);

        // Keçmiş səfər üçün hava göstərmirik
        if (end < today)
            return null;

        if (start < today)
            start = today;

        if ((end - start).Days >= MaxDays)
            end = start.AddDays(MaxDays - 1);

        var lastForecastDay = today.AddDays(ForecastDays - 1);

        if (start <= lastForecastDay)
        {
            if (end > lastForecastDay)
                end = lastForecastDay;

            return new WeatherWindow(start, end, true, 0);
        }

        // Çox uzaqdır: keçən ilin eyni günləri
        return new WeatherWindow(start.AddYears(-1), end.AddYears(-1), false, 1);
    }

    // WMO hava kodu -> mətn + ikon
    public static (string Summary, string Icon) Describe(int code) => code switch
    {
        0 => ("Clear sky", "☀️"),
        1 => ("Mainly clear", "🌤️"),
        2 => ("Partly cloudy", "⛅"),
        3 => ("Overcast", "☁️"),
        45 or 48 => ("Fog", "🌫️"),
        51 or 53 or 55 or 56 or 57 => ("Drizzle", "🌦️"),
        61 or 63 or 65 or 66 or 67 => ("Rain", "🌧️"),
        71 or 73 or 75 or 77 => ("Snow", "❄️"),
        80 or 81 or 82 => ("Rain showers", "🌦️"),
        85 or 86 => ("Snow showers", "🌨️"),
        95 or 96 or 99 => ("Thunderstorm", "⛈️"),
        _ => ("Mixed", "🌡️")
    };

    public static bool IsRainy(WeatherDayDto day)
        => day.Code is >= 51 and <= 67 or >= 80 and <= 82 or >= 95 ||
           day.RainChance >= 50 ||
           day.RainMm >= 2;

    public static string GetAdvice(IReadOnlyList<WeatherDayDto> days)
    {
        if (days.Count == 0)
            return string.Empty;

        var rainyDays = days.Count(IsRainy);
        var avgMax = days.Average(d => d.TempMax);
        var minTemp = days.Min(d => d.TempMin);

        if (days.Any(d => d.Code is >= 71 and <= 77 or 85 or 86))
            return "Snow is likely — pack warm, waterproof clothes.";

        if (rainyDays * 2 >= days.Count)
            return "A rainy stay — bring an umbrella and plan some indoor attractions.";

        if (avgMax >= 30)
            return "Hot days ahead — sunscreen, a hat and plenty of water.";

        if (minTemp <= 5)
            return "Cold evenings — pack a warm jacket.";

        if (rainyDays > 0)
            return "Mostly fine weather, but take an umbrella just in case.";

        return "Great weather for sightseeing!";
    }

    public static double Round(double value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
