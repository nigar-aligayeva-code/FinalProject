using HotelTravel.Application.DTOs.Flights;

namespace HotelTravel.Application.Interfaces;

/// <summary>Real reys cədvəli provayderi (AviationStack). Açar yoxdursa IsEnabled = false.</summary>
public interface IRealFlightScheduleProvider
{
    bool IsEnabled { get; }

    /// <summary>Bu hava limanından bu günkü çıxışlar (bir sorğu — pulsuz limitə qənaət).</summary>
    Task<RealScheduleResult> GetDeparturesAsync(string fromIata, CancellationToken cancellationToken = default);
}

/// <summary>Reys şəbəkəsi: avtomatik yaratmaq və real cədvəldən idxal.</summary>
public interface IFlightNetworkService
{
    Task<bool> IsRealDataEnabledAsync();

    /// <summary>Otel şəhərləri arasında reysi olmayan istiqamətlər üçün avtomatik reys yaradır.</summary>
    Task<FlightNetworkResultDto> GenerateMissingAsync(CancellationToken cancellationToken = default);

    /// <summary>AviationStack-dan real reys nömrələri/saatları gətirib bazaya əlavə edir.</summary>
    Task<FlightNetworkResultDto> ImportRealAsync(CancellationToken cancellationToken = default);
}
