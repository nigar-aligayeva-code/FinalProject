using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Hər şəhər üçün öz maşın parkı: ölkədə real olaraq icarəyə verilən modellər,
// yerli qiymətlər (AZN / gün). Məs. Gürcüstanda Toyota Prius, Dubayda Nissan Patrol.
// Cədvəl boşdursa doldurulur; köhnə "hər yerdə eyni 4 maşın" parkı varsa yenisi ilə əvəz olunur.
public static class RentalCarSeeder
{
    private record CarSpec(string Model, CarCategory Category, int Seats, bool Automatic, decimal Price);

    // Əvvəlki versiyada hər şəhərə qoyulan eyni park (yeniləmə üçün tanıyırıq)
    private static readonly string[] OldDefaultModels =
    {
        "Hyundai Accent", "Toyota Corolla", "Kia Sportage", "Mercedes-Benz V-Class"
    };

    private static readonly Dictionary<string, CarSpec[]> FleetByCity = new(StringComparer.OrdinalIgnoreCase)
    {
        // Azərbaycan
        ["Baku"] = new[]
        {
            new CarSpec("Chevrolet Cobalt", CarCategory.Economy, 5, true, 45m),
            new CarSpec("Hyundai Elantra", CarCategory.Compact, 5, true, 60m),
            new CarSpec("Toyota Camry", CarCategory.Sedan, 5, true, 95m),
            new CarSpec("Toyota Land Cruiser Prado", CarCategory.SUV, 7, true, 160m),
            new CarSpec("Mercedes-Benz E-Class", CarCategory.Premium, 5, true, 210m),
            new CarSpec("Hyundai Staria", CarCategory.Van, 9, true, 150m)
        },
        ["Gabala"] = new[]
        {
            new CarSpec("Kia Rio", CarCategory.Economy, 5, false, 50m),
            new CarSpec("Lada Niva Travel", CarCategory.SUV, 5, false, 70m),
            new CarSpec("Toyota RAV4", CarCategory.SUV, 5, true, 120m),
            new CarSpec("Mitsubishi Pajero", CarCategory.SUV, 7, true, 135m),
            new CarSpec("Hyundai H-1", CarCategory.Van, 8, false, 130m)
        },

        // Türkiyə
        ["Istanbul"] = new[]
        {
            new CarSpec("Fiat Egea", CarCategory.Economy, 5, false, 50m),
            new CarSpec("Renault Clio", CarCategory.Compact, 5, true, 55m),
            new CarSpec("Toyota Corolla", CarCategory.Sedan, 5, true, 70m),
            new CarSpec("Volkswagen Passat", CarCategory.Sedan, 5, true, 95m),
            new CarSpec("Peugeot 3008", CarCategory.SUV, 5, true, 110m),
            new CarSpec("Volkswagen Transporter", CarCategory.Van, 9, false, 150m)
        },
        ["Antalya"] = new[]
        {
            new CarSpec("Renault Clio", CarCategory.Compact, 5, false, 45m),
            new CarSpec("Fiat Egea Cross", CarCategory.Economy, 5, true, 55m),
            new CarSpec("Dacia Duster", CarCategory.SUV, 5, false, 80m),
            new CarSpec("BMW 4 Series Convertible", CarCategory.Premium, 4, true, 230m),
            new CarSpec("Fiat Doblo", CarCategory.Van, 7, false, 90m)
        },

        // Gürcüstan
        ["Tbilisi"] = new[]
        {
            new CarSpec("Toyota Prius", CarCategory.Economy, 5, true, 50m),
            new CarSpec("Toyota Camry", CarCategory.Sedan, 5, true, 85m),
            new CarSpec("Hyundai Tucson", CarCategory.SUV, 5, true, 95m),
            new CarSpec("Lexus RX", CarCategory.Premium, 5, true, 190m),
            new CarSpec("Mitsubishi Delica", CarCategory.Van, 8, true, 120m)
        },
        ["Batumi"] = new[]
        {
            new CarSpec("Suzuki Swift", CarCategory.Compact, 5, false, 45m),
            new CarSpec("Toyota Prius", CarCategory.Economy, 5, true, 50m),
            new CarSpec("Kia Sportage", CarCategory.SUV, 5, true, 95m),
            new CarSpec("Mercedes-Benz Vito", CarCategory.Van, 8, true, 140m)
        },

        // BƏƏ
        ["Dubai"] = new[]
        {
            new CarSpec("Nissan Sunny", CarCategory.Economy, 5, true, 60m),
            new CarSpec("Toyota Corolla", CarCategory.Sedan, 5, true, 75m),
            new CarSpec("Nissan Patrol", CarCategory.SUV, 7, true, 250m),
            new CarSpec("Range Rover Sport", CarCategory.Premium, 5, true, 380m),
            new CarSpec("Mercedes-Benz S-Class", CarCategory.Premium, 5, true, 450m),
            new CarSpec("Toyota Hiace", CarCategory.Van, 12, true, 160m)
        }
    };

    // Siyahıda olmayan yeni şəhər üçün (admin yeni otel əlavə edəndə)
    private static readonly CarSpec[] DefaultFleet =
    {
        new CarSpec("Hyundai Accent", CarCategory.Economy, 5, true, 55m),
        new CarSpec("Toyota Corolla", CarCategory.Sedan, 5, true, 75m),
        new CarSpec("Kia Sportage", CarCategory.SUV, 5, true, 110m),
        new CarSpec("Mercedes-Benz V-Class", CarCategory.Van, 7, true, 170m)
    };

    /// <summary>Şəhər üçün park (testlər və seeder istifadə edir).</summary>
    public static IReadOnlyList<(string Model, CarCategory Category)> FleetFor(string city) =>
        (FleetByCity.TryGetValue(city, out var fleet) ? fleet : DefaultFleet)
            .Select(c => (c.Model, c.Category))
            .ToList();

    public static async Task SeedAsync(AppDbContext context)
    {
        var cities = await context.Hotels
            .Where(h => !h.IsDeleted)
            .Select(h => h.City)
            .Distinct()
            .ToListAsync();

        var existing = await context.RentalCars
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        var cars = new List<RentalCar>();

        foreach (var city in cities)
        {
            var cityCars = existing
                .Where(c => string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cityCars.Count > 0)
            {
                // Admin parkı dəyişibsə toxunmuruq; yalnız köhnə "eyni park" yenilənir
                var isOldDefault =
                    FleetByCity.ContainsKey(city) &&
                    cityCars.All(c => OldDefaultModels.Contains(c.Model)) &&
                    cityCars.Count == OldDefaultModels.Length;

                if (!isOldDefault)
                    continue;

                // Soft delete: keçmiş icarələr maşına bağlı qalır, yeni axtarışda görünmür
                foreach (var old in cityCars)
                {
                    old.IsDeleted = true;
                    old.IsActive = false;
                    old.UpdatedAt = DateTime.UtcNow;
                }
            }

            var fleet = FleetByCity.TryGetValue(city, out var specific) ? specific : DefaultFleet;

            cars.AddRange(fleet.Select(item => new RentalCar
            {
                City = city,
                Model = item.Model,
                Category = item.Category,
                Seats = item.Seats,
                IsAutomatic = item.Automatic,
                HasAirConditioning = true,
                PricePerDay = item.Price,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }));
        }

        if (cars.Count == 0)
            return;

        await context.RentalCars.AddRangeAsync(cars);
        await context.SaveChangesAsync();
    }
}
