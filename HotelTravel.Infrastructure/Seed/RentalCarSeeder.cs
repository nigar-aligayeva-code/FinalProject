using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Hər şəhər üçün başlanğıc maşın parkı.
// Cədvəl boşdursa bir dəfə doldurulur; sonra admin paneldən idarə olunur.
public static class RentalCarSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.RentalCars.AnyAsync())
            return;

        var cities = await context.Hotels
            .Where(h => !h.IsDeleted)
            .Select(h => h.City)
            .Distinct()
            .ToListAsync();

        // Hər şəhərdə eyni park (qiymətlər AZN / gün)
        var fleet = new[]
        {
            (Model: "Hyundai Accent", Category: CarCategory.Economy, Seats: 5, Automatic: true, Price: 55m),
            (Model: "Toyota Corolla", Category: CarCategory.Sedan, Seats: 5, Automatic: true, Price: 75m),
            (Model: "Kia Sportage", Category: CarCategory.SUV, Seats: 5, Automatic: true, Price: 110m),
            (Model: "Mercedes-Benz V-Class", Category: CarCategory.Van, Seats: 7, Automatic: true, Price: 170m)
        };

        var cars = new List<RentalCar>();

        foreach (var city in cities)
        {
            foreach (var item in fleet)
            {
                cars.Add(new RentalCar
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
                });
            }
        }

        await context.RentalCars.AddRangeAsync(cars);
        await context.SaveChangesAsync();
    }
}
