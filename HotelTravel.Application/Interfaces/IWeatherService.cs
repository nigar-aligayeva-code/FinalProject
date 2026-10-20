using HotelTravel.Application.DTOs.Weather;

namespace HotelTravel.Application.Interfaces;

public interface IWeatherService
{
    // Tarix verilməsə: bu gündən 7 gün
    Task<TripWeatherDto?> GetForHotelAsync(int hotelId, DateTime? from, DateTime? to);

    Task<TripWeatherDto?> GetForCityAsync(string city, DateTime? from, DateTime? to);
}
