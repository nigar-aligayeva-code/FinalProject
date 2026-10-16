using HotelTravel.Application.Services;
using HotelTravel.Domain.Entities;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Otel şəhərləri arasında başlanğıc reys cədvəli.
// Cədvəl boşdursa bir dəfə doldurulur; sonra admin paneldən idarə olunur.
// Qiymətlər AZN, bir sərnişin üçün (Business 0 = bu reysdə yoxdur).
public static class FlightRouteSeeder
{
    private record Seed(
        string Number, string Airline, string From, string To,
        int Hour, int Minute, int Duration,
        decimal Economy, decimal Business, int EcoSeats, int BizSeats);

    private const string Azal = "Azerbaijan Airlines";
    private const string Turkish = "Turkish Airlines";
    private const string Pegasus = "Pegasus Airlines";
    private const string FlyDubai = "flydubai";
    private const string Georgian = "Georgian Airways";

    private static readonly Seed[] Routes =
    {
        // Baku <-> Istanbul
        new("J2 8011", Azal, "Baku", "Istanbul", 8, 0, 200, 260, 780, 150, 12),
        new("TK 333", Turkish, "Baku", "Istanbul", 14, 30, 205, 290, 850, 160, 16),
        new("J2 8012", Azal, "Istanbul", "Baku", 13, 30, 190, 260, 780, 150, 12),
        new("TK 334", Turkish, "Istanbul", "Baku", 19, 40, 195, 290, 850, 160, 16),

        // Baku <-> Dubai
        new("J2 8021", Azal, "Baku", "Dubai", 9, 15, 190, 320, 950, 150, 12),
        new("FZ 712", FlyDubai, "Baku", "Dubai", 23, 55, 200, 280, 820, 170, 12),
        new("J2 8022", Azal, "Dubai", "Baku", 14, 30, 200, 320, 950, 150, 12),
        new("FZ 711", FlyDubai, "Dubai", "Baku", 18, 10, 190, 280, 820, 170, 12),

        // Baku <-> Tbilisi
        new("J2 8031", Azal, "Baku", "Tbilisi", 7, 10, 65, 150, 420, 90, 8),
        new("A9 158", Georgian, "Baku", "Tbilisi", 18, 20, 65, 140, 0, 100, 0),
        new("J2 8032", Azal, "Tbilisi", "Baku", 9, 0, 65, 150, 420, 90, 8),
        new("A9 157", Georgian, "Tbilisi", "Baku", 16, 0, 65, 140, 0, 100, 0),

        // Baku <-> Batumi
        new("J2 8041", Azal, "Baku", "Batumi", 11, 20, 85, 180, 0, 120, 0),
        new("J2 8042", Azal, "Batumi", "Baku", 13, 40, 85, 180, 0, 120, 0),

        // Baku <-> Antalya
        new("J2 8051", Azal, "Baku", "Antalya", 10, 0, 240, 330, 900, 150, 12),
        new("J2 8052", Azal, "Antalya", "Baku", 15, 20, 230, 330, 900, 150, 12),

        // Baku <-> Gabala
        new("J2 8061", Azal, "Baku", "Gabala", 8, 40, 45, 90, 0, 70, 0),
        new("J2 8062", Azal, "Gabala", "Baku", 10, 20, 45, 90, 0, 70, 0),

        // Istanbul <-> Antalya
        new("TK 2410", Turkish, "Istanbul", "Antalya", 9, 30, 80, 120, 360, 160, 16),
        new("PC 2210", Pegasus, "Istanbul", "Antalya", 17, 15, 80, 95, 0, 180, 0),
        new("TK 2411", Turkish, "Antalya", "Istanbul", 12, 0, 80, 120, 360, 160, 16),
        new("PC 2211", Pegasus, "Antalya", "Istanbul", 20, 0, 80, 95, 0, 180, 0),

        // Istanbul <-> Dubai
        new("TK 760", Turkish, "Istanbul", "Dubai", 10, 20, 260, 350, 1100, 200, 24),
        new("FZ 754", FlyDubai, "Istanbul", "Dubai", 16, 40, 250, 310, 900, 170, 12),
        new("TK 761", Turkish, "Dubai", "Istanbul", 16, 30, 290, 350, 1100, 200, 24),
        new("FZ 753", FlyDubai, "Dubai", "Istanbul", 9, 30, 300, 310, 900, 170, 12),

        // Istanbul <-> Tbilisi
        new("TK 378", Turkish, "Istanbul", "Tbilisi", 11, 0, 140, 210, 620, 150, 12),
        new("TK 379", Turkish, "Tbilisi", "Istanbul", 15, 0, 140, 210, 620, 150, 12),

        // Istanbul <-> Batumi
        new("TK 2480", Turkish, "Istanbul", "Batumi", 8, 45, 125, 180, 520, 150, 12),
        new("TK 2481", Turkish, "Batumi", "Istanbul", 12, 10, 120, 180, 520, 150, 12),

        // Tbilisi <-> Dubai
        new("FZ 714", FlyDubai, "Tbilisi", "Dubai", 20, 40, 195, 300, 880, 170, 12),
        new("FZ 713", FlyDubai, "Dubai", "Tbilisi", 15, 50, 220, 300, 880, 170, 12),

        // Tbilisi <-> Batumi
        new("A9 211", Georgian, "Tbilisi", "Batumi", 10, 30, 50, 80, 0, 70, 0),
        new("A9 212", Georgian, "Batumi", "Tbilisi", 12, 30, 50, 80, 0, 70, 0),

        // Dubai <-> Antalya
        new("FZ 762", FlyDubai, "Dubai", "Antalya", 7, 45, 290, 380, 1050, 170, 12),
        new("FZ 763", FlyDubai, "Antalya", "Dubai", 13, 30, 270, 380, 1050, 170, 12)
    };

    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.FlightRoutes.AnyAsync())
            return;

        var routes = new List<FlightRoute>();

        foreach (var item in Routes)
        {
            var from = TaxiRules.GetAirport(item.From);
            var to = TaxiRules.GetAirport(item.To);

            if (from is null || to is null)
                continue;

            routes.Add(new FlightRoute
            {
                FlightNumber = item.Number,
                Airline = item.Airline,
                FromCity = from.City,
                FromCode = from.Code,
                ToCity = to.City,
                ToCode = to.Code,
                DepartureTime = new TimeSpan(item.Hour, item.Minute, 0),
                DurationMinutes = item.Duration,
                EconomyPrice = item.Economy,
                BusinessPrice = item.Business,
                EconomySeats = item.EcoSeats,
                BusinessSeats = item.BizSeats,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        await context.FlightRoutes.AddRangeAsync(routes);
        await context.SaveChangesAsync();
    }
}
