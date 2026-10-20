using HotelTravel.Application.DTOs.Currency;

namespace HotelTravel.Application.Interfaces;

public interface ICurrencyService
{
    Task<CurrencyRatesDto> GetRatesAsync();
}
