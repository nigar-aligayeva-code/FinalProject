namespace HotelTravel.Application.Services;

// Profil səviyyəsi (unit test üçün ayrıca)
public static class ProfileRules
{
    public const string Explorer = "Explorer";
    public const string Traveler = "Traveler";
    public const string Globetrotter = "Globetrotter";

    // trips: ləğv olunmayan otel + uçuş sifarişləri; cities: fərqli şəhərlər
    public static string GetLevel(int trips, int cities)
    {
        if (trips >= 8 || cities >= 5)
            return Globetrotter;

        if (trips >= 3 || cities >= 2)
            return Traveler;

        return Explorer;
    }

    // Növbəti səviyyəyə qədər neçə səfər qalıb (sonuncu səviyyədə 0)
    public static int TripsToNextLevel(int trips, int cities)
        => GetLevel(trips, cities) switch
        {
            Explorer => Math.Max(0, 3 - trips),
            Traveler => Math.Max(0, 8 - trips),
            _ => 0
        };
}
