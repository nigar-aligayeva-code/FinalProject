namespace HotelTravel.Application.Interfaces;

// Biznes hadisələri -> canlı bildiriş. Heç vaxt xəta atmır (bildiriş alınmasa sifariş pozulmur).
public interface IActivityNotifier
{
    Task HotelBookedAsync(string confirmationCode);
    Task HotelCancelledAsync(string confirmationCode);
    Task FlightBookedAsync(string confirmationCode);
    Task TaxiBookedAsync(string confirmationCode);
    Task CarRentedAsync(string confirmationCode);
    Task TripBookedAsync(string packageCode);
}
