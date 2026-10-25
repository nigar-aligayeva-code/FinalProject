namespace HotelTravel.Application.DTOs.Flights;

/// <summary>Real reys (AviationStack-dan): nömrə, şirkət, yerli çıxış saatı, gəliş hava limanı.</summary>
public record RealFlight(
    string FlightNumber,
    string Airline,
    string FromCode,
    string ToCode,
    TimeSpan DepartureLocal,
    TimeSpan ArrivalLocal);

/// <summary>Real cədvəl sorğusunun nəticəsi.</summary>
public record RealScheduleResult(bool Success, string? Error, IReadOnlyList<RealFlight> Flights)
{
    public static RealScheduleResult Fail(string error) => new(false, error, Array.Empty<RealFlight>());
}

/// <summary>Admin əməliyyatının nəticəsi: neçə reys əlavə olundu.</summary>
public class FlightNetworkResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Added { get; set; }
}
