using HotelTravel.Application.DTOs.Analytics;

namespace HotelTravel.Application.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAsync();
}
