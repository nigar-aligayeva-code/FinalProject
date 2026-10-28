namespace HotelTravel.Application.Services;

/// <summary>Planlaşdırılmış gündəlik reys (bazaya FlightRoute kimi yazılır).</summary>
public record PlannedRoute(
    string FlightNumber, string Airline,
    string FromCity, string FromCode, string ToCity, string ToCode,
    TimeSpan Departure, int DurationMinutes,
    decimal EconomyPrice, decimal BusinessPrice,
    int EconomySeats, int BusinessSeats);

/// <summary>
/// Reys şəbəkəsini AVTOMATİK qurur — admin hər reysi əllə yaratmır.
/// Hər iki otel şəhəri arasında (hava limanı varsa):
///   • məsafə hava limanlarının koordinatlarından hesablanır (haversine);
///   • müddət = məsafə / 720 km/s + 40 dəq (qalxma/enmə);
///   • qiymət məsafəyə görə (AZN), uzun reyslərdə Business sinfi də olur;
///   • aviaşirkət ölkəyə görə (AZ → AZAL, TR → Turkish/Pegasus, GE → Georgian, AE → flydubai/Emirates);
///   • reys nömrəsi və saatı deterministikdir: hər dəfə eyni nəticə (testlər üçün vacib).
/// Kiçik hava limanları (Qəbələ) yalnız böyük mərkəzlərə (Bakı, İstanbul) uçur.
/// 250 km-dən qısa məsafəyə reys açılmır (Bakı–Qəbələ ~200 km): orada taksi / maşın təklif olunur.
/// </summary>
public static class FlightNetworkPlanner
{
    private record Airport(string City, string Code, string Country, double Lat, double Lng, bool IsSmall = false);

    private static readonly Airport[] Airports =
    {
        new("Baku", "GYD", "AZ", 40.4675, 50.0467),
        new("Gabala", "GBB", "AZ", 40.8267, 47.7125, IsSmall: true),
        new("Tbilisi", "TBS", "GE", 41.6692, 44.9547),
        new("Batumi", "BUS", "GE", 41.6103, 41.5997),
        new("Istanbul", "IST", "TR", 41.2753, 28.7519),
        new("Antalya", "AYT", "TR", 36.8987, 30.8005),
        new("Dubai", "DXB", "AE", 25.2532, 55.3657)
    };

    // Kiçik hava limanlarının uçduğu mərkəzlər
    private static readonly HashSet<string> Hubs = new(StringComparer.OrdinalIgnoreCase) { "GYD", "IST" };

    private static readonly Dictionary<string, (string Name, string Code)[]> Carriers = new()
    {
        ["AZ"] = new[] { ("Azerbaijan Airlines", "J2") },
        ["TR"] = new[] { ("Turkish Airlines", "TK"), ("Pegasus Airlines", "PC") },
        ["GE"] = new[] { ("Georgian Airways", "A9") },
        ["AE"] = new[] { ("flydubai", "FZ"), ("Emirates", "EK") }
    };

    public const double CruiseSpeedKmh = 720;
    public const int GroundMinutes = 40;

    // Bundan yaxın şəhərlər arasında reys olmur (Bakı–Qəbələ ~200 km, Tbilisi–Batumi ~280 km qalır)
    public const int MinFlightKm = 250;

    /// <summary>Məsafə reys üçün çox qısadırmı? (Dəstəklənməyən şəhərlər üçün false.)</summary>
    public static bool IsTooShortForFlight(string fromCity, string toCity) =>
        DistanceKm(fromCity, toCity) is int km && km < MinFlightKm;

    public static bool IsSupported(string city) => Find(city) is not null;

    /// <summary>İki şəhərin hava limanları arasındakı məsafə (km), dəstəklənmirsə null.</summary>
    public static int? DistanceKm(string fromCity, string toCity)
    {
        var a = Find(fromCity);
        var b = Find(toCity);

        return a is null || b is null ? null : (int)Math.Round(Haversine(a, b));
    }

    public static int DurationFor(int distanceKm) =>
        RoundTo5(distanceKm / CruiseSpeedKmh * 60 + GroundMinutes);

    public static decimal EconomyFareFor(int distanceKm) =>
        Math.Max(60m, Math.Round((55m + distanceKm * 0.11m) / 5m) * 5m);

    // Qısa reyslərdə (500 km-dən az) Business yoxdur
    public static decimal BusinessFareFor(int distanceKm) =>
        distanceKm < 500 ? 0m : Math.Round(EconomyFareFor(distanceKm) * 2.8m / 5m) * 5m;

    /// <summary>Verilmiş şəhərlər üçün bütün reyslər (hər istiqamətdə 1-2 gündəlik reys).</summary>
    public static IReadOnlyList<PlannedRoute> Plan(IEnumerable<string> cities)
    {
        var airports = cities
            .Select(Find)
            .Where(a => a is not null)
            .Select(a => a!)
            .DistinctBy(a => a.Code)
            .ToList();

        var routes = new List<PlannedRoute>();
        var usedNumbers = new HashSet<string>();

        foreach (var from in airports)
        {
            foreach (var to in airports)
            {
                if (from.Code == to.Code)
                    continue;

                // Kiçik hava limanı yalnız mərkəzlə əlaqəlidir
                if ((from.IsSmall && !Hubs.Contains(to.Code)) || (to.IsSmall && !Hubs.Contains(from.Code)))
                    continue;

                var distance = (int)Math.Round(Haversine(from, to));

                // Çox yaxındır - avtomobillə getmək daha məntiqlidir
                if (distance < MinFlightKm)
                    continue;

                var duration = DurationFor(distance);
                var economy = EconomyFareFor(distance);
                var business = BusinessFareFor(distance);

                // Uçan şirkətlər: çıxış ölkəsinin şirkəti + (beynəlxalqdırsa) gəliş ölkəsinin şirkəti
                var operators = new List<(string Name, string Code)> { Carriers[from.Country][0] };

                if (from.Country != to.Country && !from.IsSmall && !to.IsSmall)
                    operators.Add(Carriers[to.Country][0]);
                else if (from.Country == to.Country && Carriers[from.Country].Length > 1)
                    operators.Add(Carriers[from.Country][1]); // daxili: aşağı büdcəli şirkət də

                for (var i = 0; i < operators.Count; i++)
                {
                    var (airline, code) = operators[i];
                    var seed = Hash($"{from.Code}-{to.Code}-{code}");
                    var lowCost = code is "PC" or "FZ";

                    // 1-ci reys səhər (06:00-11:55), 2-ci günorta/axşam (13:00-20:55)
                    var startHour = i == 0 ? 6 : 13;
                    var departure = TimeSpan.FromMinutes(startHour * 60 + seed % 72 * 5);

                    routes.Add(new PlannedRoute(
                        UniqueNumber(code, seed, usedNumbers),
                        airline,
                        from.City, from.Code, to.City, to.Code,
                        departure,
                        duration,
                        lowCost ? Math.Max(50m, economy - 25m) : economy,
                        lowCost || from.IsSmall || to.IsSmall ? 0m : business,
                        lowCost ? 186 : 150 + seed % 30,
                        lowCost || business == 0 ? 0 : 12 + seed % 3 * 4));
                }
            }
        }

        return routes;
    }

    private static Airport? Find(string? city) =>
        Airports.FirstOrDefault(a => string.Equals(a.City, city?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string UniqueNumber(string code, int seed, HashSet<string> used)
    {
        var number = 1000 + seed % 8000;

        while (!used.Add($"{code} {number}"))
            number++;

        return $"{code} {number}";
    }

    // Stabil hash (string.GetHashCode hər işə salmada fərqlidir)
    private static int Hash(string text)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in text)
                hash = hash * 31 + c;
            return Math.Abs(hash % 100_000);
        }
    }

    private static int RoundTo5(double minutes) => (int)(Math.Round(minutes / 5) * 5);

    private static double Haversine(Airport a, Airport b)
    {
        const double earthKm = 6371;
        double Rad(double degrees) => degrees * Math.PI / 180;

        var dLat = Rad(b.Lat - a.Lat);
        var dLng = Rad(b.Lng - a.Lng);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(a.Lat)) * Math.Cos(Rad(b.Lat)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return 2 * earthKm * Math.Asin(Math.Sqrt(h));
    }
}
