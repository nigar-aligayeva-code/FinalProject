using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Hər otelin öz otaq dəsti olsun: məs. Dubayda Penthouse var, Tbilisidə yoxdur;
// Qəbələdə meşə villası var, Luxury Suite yoxdur.
//
// HotelDataSeeder hər otelə 5 baza otaq (01-05) yaradır. Bu seeder:
//   1) otelin profilində olmayan baza otaqları gizlədir (soft delete) — YALNIZ heç bir
//      rezervasiyası yoxdursa (keçmiş rezervasiyalar pozulmasın);
//   2) otelə xas "imza" otaqları (06+) əlavə edir;
//   3) baza otaqların qiymətini otelin səviyyəsinə uyğunlaşdırır (yalnız ilkin qiymətdədirsə).
// Təkrar işə salmaq təhlükəsizdir (idempotent).
public static class RoomVarietySeeder
{
    public record RoomSpec(
        string Suffix, string Name, string Description, decimal Price, int Capacity,
        RoomType Type, string Image, string[] Amenities);

    public record HotelProfile(decimal PriceFactor, string[] BaseRooms, RoomSpec[] Signature);

    // HotelDataSeeder-dəki ilkin qiymətlər (dəyişdirilməyibsə uyğunlaşdırırıq)
    private static readonly Dictionary<string, decimal> BasePrices = new()
    {
        ["01"] = 90m, ["02"] = 140m, ["03"] = 220m, ["04"] = 350m, ["05"] = 280m
    };

    private const string Img = "/assets/images/rooms/";

    private static readonly Dictionary<string, HotelProfile> Profiles = new()
    {
        ["Caspian Grand Hotel"] = new(1.2m,
            new[] { "01", "02", "03", "04" },
            new[]
            {
                new RoomSpec("06", "Caspian Sea View King", "A king-size bed and floor-to-ceiling windows facing the Caspian boulevard.",
                    290m, 2, RoomType.King, Img + "deluxe-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Balcony" }),
                new RoomSpec("07", "Presidential Suite", "Two bedrooms, a private lounge and butler service on the top floor.",
                    780m, 4, RoomType.Presidential, Img + "suite-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Spa", "Gym", "Room Service", "Balcony" })
            }),

        ["Gabala Mountain Resort"] = new(1.0m,
            new[] { "02", "05" },
            new[]
            {
                new RoomSpec("06", "Mountain View Twin", "Two single beds and a balcony looking at the Caucasus mountains.",
                    150m, 2, RoomType.Twin, Img + "double-room.jpg", new[] { "Free Wi-Fi", "Breakfast", "TV", "Balcony", "Parking" }),
                new RoomSpec("07", "Forest Villa", "A private wooden villa with a fireplace and terrace in the forest.",
                    420m, 6, RoomType.Villa, Img + "family-room.jpg", new[] { "Free Wi-Fi", "Breakfast", "TV", "Parking", "Balcony", "Room Service" })
            }),

        ["Bosphorus Palace Hotel"] = new(1.3m,
            new[] { "01", "02", "03", "04" },
            new[]
            {
                new RoomSpec("06", "Bosphorus View King", "A king-size bed with a direct view of the Bosphorus strait.",
                    310m, 2, RoomType.King, Img + "deluxe-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Room Service" }),
                new RoomSpec("07", "Classic Twin Room", "Two comfortable single beds, ideal for friends or colleagues.",
                    160m, 2, RoomType.Twin, Img + "double-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "TV" })
            }),

        ["Antalya Blue Resort"] = new(1.1m,
            new[] { "02", "03", "05" },
            new[]
            {
                new RoomSpec("06", "Private Pool Villa", "A garden villa with its own pool, steps away from the beach.",
                    560m, 6, RoomType.Villa, Img + "family-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "Swimming Pool", "Balcony", "Room Service" })
            }),

        ["Tbilisi Heritage Hotel"] = new(0.8m,
            new[] { "01", "02", "05" },
            new[]
            {
                new RoomSpec("06", "Old Town Twin Room", "Two single beds in a restored 19th-century building in the Old Town.",
                    110m, 2, RoomType.Twin, Img + "double-room.jpg", new[] { "Free Wi-Fi", "Breakfast", "TV" }),
                new RoomSpec("07", "Heritage Studio", "A studio with a small kitchenette and a carved wooden balcony.",
                    170m, 3, RoomType.Studio, Img + "deluxe-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "TV", "Balcony" })
            }),

        ["Batumi Sea View Hotel"] = new(0.85m,
            new[] { "01", "02", "03", "05" },
            new[]
            {
                new RoomSpec("06", "Sea View King", "A king-size bed and a balcony over the Black Sea promenade.",
                    210m, 2, RoomType.King, Img + "deluxe-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Balcony" })
            }),

        ["Dubai Marina Luxury Hotel"] = new(1.6m,
            new[] { "03", "04" },
            new[]
            {
                new RoomSpec("06", "Marina King Room", "A king-size bed with views over Dubai Marina yachts.",
                    420m, 2, RoomType.King, Img + "deluxe-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Swimming Pool", "Gym" }),
                new RoomSpec("07", "Sky Penthouse", "A two-floor penthouse with a private terrace and 360° skyline views.",
                    1250m, 4, RoomType.Penthouse, Img + "suite-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Spa", "Gym", "Room Service", "Swimming Pool", "Balcony" }),
                new RoomSpec("08", "Royal Presidential Suite", "Three bedrooms, a private cinema and a dedicated butler.",
                    1900m, 6, RoomType.Presidential, Img + "suite-room.jpg", new[] { "Free Wi-Fi", "Air Conditioning", "Breakfast", "TV", "Spa", "Gym", "Room Service", "Swimming Pool", "Balcony" })
            })
    };

    /// <summary>Otelin profili (yoxdursa null: admin-in yaratdığı otellərə toxunulmur).</summary>
    public static HotelProfile? ProfileFor(string hotelName) =>
        Profiles.TryGetValue(hotelName, out var profile) ? profile : null;

    /// <summary>Profilə görə otelin otaq tipləri (baza + imza).</summary>
    public static IReadOnlyList<RoomType> RoomTypesFor(string hotelName)
    {
        var profile = ProfileFor(hotelName);
        if (profile is null)
            return Array.Empty<RoomType>();

        var baseTypes = profile.BaseRooms.Select(BaseType);
        return baseTypes.Concat(profile.Signature.Select(s => s.Type)).Distinct().ToList();
    }

    private static RoomType BaseType(string suffix) => suffix switch
    {
        "01" => RoomType.Single,
        "02" => RoomType.Double,
        "03" => RoomType.Deluxe,
        "04" => RoomType.Suite,
        _ => RoomType.Family
    };

    public static async Task SeedAsync(AppDbContext context)
    {
        var hotels = await context.Hotels
            .Where(h => !h.IsDeleted)
            .ToListAsync();

        var amenities = await context.Amenities
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        foreach (var hotel in hotels)
        {
            if (ProfileFor(hotel.Name) is not { } profile)
                continue;

            var rooms = await context.Rooms
                .Where(r => r.HotelId == hotel.Id)
                .ToListAsync();

            foreach (var (suffix, defaultPrice) in BasePrices)
            {
                var room = rooms.FirstOrDefault(r => r.RoomNumber == $"{hotel.Id}{suffix}" && !r.IsDeleted);
                if (room is null)
                    continue;

                if (!profile.BaseRooms.Contains(suffix))
                {
                    // Rezervasiyası olan otaq qalır, yoxdursa gizlədilir
                    var hasBookings = await context.Bookings.AnyAsync(b => b.RoomId == room.Id);
                    if (!hasBookings)
                    {
                        room.IsDeleted = true;
                        room.UpdatedAt = DateTime.UtcNow;
                    }

                    continue;
                }

                // Admin qiyməti dəyişməyibsə otelin səviyyəsinə uyğunlaşdır
                if (room.PricePerNight == defaultPrice && profile.PriceFactor != 1.0m)
                {
                    room.PricePerNight = Math.Round(defaultPrice * profile.PriceFactor / 5m) * 5m;
                    room.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var spec in profile.Signature)
            {
                var number = $"{hotel.Id}{spec.Suffix}";

                // Silinmiş olsa belə yenidən yaratmırıq (admin silibsə, qərarına hörmət)
                if (rooms.Any(r => r.RoomNumber == number))
                    continue;

                context.Rooms.Add(new Room
                {
                    RoomNumber = number,
                    Name = spec.Name,
                    Description = spec.Description,
                    PricePerNight = spec.Price,
                    Capacity = spec.Capacity,
                    RoomType = spec.Type,
                    MainImage = spec.Image,
                    HotelId = hotel.Id,
                    Amenities = amenities.Where(a => spec.Amenities.Contains(a.Name)).ToList(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
