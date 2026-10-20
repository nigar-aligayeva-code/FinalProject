namespace HotelTravel.Application.DTOs.Weather;

// Səfər tarixləri üçün hava (Open-Meteo)
public class TripWeatherDto
{
    public string Location { get; set; } = string.Empty;

    // true -> həqiqi proqnoz; false -> keçən ilin eyni günlərinə görə "tipik hava"
    public bool IsForecast { get; set; }

    public List<WeatherDayDto> Days { get; set; } = new();

    public double AverageMax { get; set; }
    public double AverageMin { get; set; }

    // Məs. "Pack an umbrella"
    public string Advice { get; set; } = string.Empty;
}

public class WeatherDayDto
{
    // Səfərin tarixi (tipik havada da səfər günü göstərilir)
    public DateTime Date { get; set; }

    public int Code { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    public double TempMax { get; set; }
    public double TempMin { get; set; }

    // Yalnız proqnozda (%)
    public int? RainChance { get; set; }

    // Yalnız tipik havada (mm)
    public double? RainMm { get; set; }
}
