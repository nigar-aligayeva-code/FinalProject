using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Otel və otaq şəkillərini qaydaya salır (hər açılışda işləyir):
//  1) Olmayan köhnə şəkil yollarını real fotolarla əvəz edir.
//  2) Bir oteldə iki otaq eyni şəkli göstərmir; foto çatmasa "şəkil tezliklə" qoyulur.
//  3) wwwroot/assets/images/rooms/extra qovluğuna atılan fotoları
//     avtomatik götürür, yəni yeni foto əlavə etmək üçün kod lazım deyil.
// Admin paneldən yüklənmiş şəkillərə (/uploads/...) toxunmur.
public static class MediaFixSeeder
{
    private const string Rooms = "/assets/images/rooms/";
    private const string Variants = "/assets/images/rooms/variants/";
    private const string Extra = "/assets/images/rooms/extra/";
    private const string Hotels = "/assets/images/hotels/";

    // Foto çatmayanda: "şəkil tezliklə" - admin özü yükləyir (Admin → Rooms → Edit)
    public const string NoPhoto = "/assets/images/rooms/no-photo.svg";

    // Şəkli olan şəhərlər (hotels qovluğunda {city}-exterior.jpg və s.)
    private static readonly HashSet<string> CitiesWithPhotos = new(StringComparer.OrdinalIgnoreCase)
    {
        "antalya", "baku", "batumi", "dubai", "gabala", "istanbul", "tbilisi"
    };

    // Mövcud olmayan köhnə otaq şəkilləri
    private static readonly HashSet<string> MissingRoomImages = new(StringComparer.OrdinalIgnoreCase)
    {
        Rooms + "single-room.jpg",
        Rooms + "double-room.jpg",
        Rooms + "deluxe-room.jpg",
        Rooms + "suite-room.jpg",
        Rooms + "family-room.jpg"
    };

    // Bütün real otaq fotoları (ad -> yol). Hər birinin variants qovluğunda
    // "-a" və "-b" variantı var (fərqli kadraj, güzgü əksi və işıq).
    private static readonly Dictionary<string, string> BasePhotos = new()
    {
        ["room201-bedroom-1"] = Rooms + "room201-bedroom-1.jpg",
        ["room201-bedroom-2"] = Rooms + "room201-bedroom-2.jpg",
        ["room202-bedroom"] = Rooms + "room202-bedroom.jpg",
        ["room202-balcony"] = Rooms + "room202-balcony.jpg",
        ["room203-bedroom-1"] = Rooms + "room203-bedroom-1.jpg",
        ["room203-bedroom-2"] = Rooms + "room203-bedroom-2.jpg",
        ["room204-bedroom"] = Rooms + "room204-bedroom.jpg",
        ["room204-living-room"] = Rooms + "room204-living-room.jpg",
        ["room205-bedroom"] = Rooms + "room205-bedroom.jpg",
        ["room205-family-area"] = Rooms + "room205-family-area.jpg",
        ["antalya-room"] = Hotels + "antalya-room.jpg",
        ["baku-room"] = Hotels + "baku-room.jpg",
        ["batumi-room"] = Hotels + "batumi-room.jpg",
        ["dubai-room"] = Hotels + "dubai-room.jpg",
        ["gabala-room"] = Hotels + "gabala-room.jpg",
        ["hotelhub-room"] = Hotels + "hotelhub-room.jpg",
        ["istanbul-room"] = Hotels + "istanbul-room.jpg",
        ["tbilisi-room"] = Hotels + "tbilisi-room.jpg"
    };

    // Otaq tipinə uyğun əsas fotolar
    private static readonly Dictionary<RoomType, string[]> Preferred = new()
    {
        [RoomType.Single] = new[] { "room201-bedroom-1", "room203-bedroom-2", "room201-bedroom-2", "tbilisi-room" },
        [RoomType.Double] = new[] { "room202-bedroom", "room203-bedroom-1", "batumi-room", "room202-balcony" },
        [RoomType.Twin] = new[] { "gabala-room", "room203-bedroom-1" },
        [RoomType.Deluxe] = new[] { "dubai-room", "hotelhub-room", "room201-bedroom-2" },
        [RoomType.Suite] = new[] { "room204-bedroom", "room204-living-room", "istanbul-room", "antalya-room" },
        [RoomType.Family] = new[] { "room205-bedroom", "room205-family-area", "baku-room" },

        // Sonradan əlavə olunan tiplər
        [RoomType.King] = new[] { "room202-bedroom", "dubai-room", "room201-bedroom-2" },
        [RoomType.Studio] = new[] { "room201-bedroom-2", "hotelhub-room", "room203-bedroom-2" },
        [RoomType.Villa] = new[] { "room205-family-area", "antalya-room", "gabala-room" },
        [RoomType.Presidential] = new[] { "room204-living-room", "istanbul-room", "room204-bedroom" },
        [RoomType.Penthouse] = new[] { "room204-living-room", "dubai-room", "room204-bedroom" }
    };

    // Əvvəl hansı tiplər şəkil seçsin (Deluxe şəhərin öz fotosunu alsın)
    private static readonly RoomType[] TypeOrder =
    {
        RoomType.Deluxe, RoomType.Presidential, RoomType.Penthouse, RoomType.Suite, RoomType.Villa,
        RoomType.Family, RoomType.King, RoomType.Double, RoomType.Studio, RoomType.Twin, RoomType.Single
    };

    private static readonly string[] Bathrooms =
    {
        Rooms + "room201-bathroom.jpg",
        Rooms + "room202-bathroom.jpg",
        Rooms + "room203-bathroom.jpg",
        Rooms + "room204-bathroom.jpg",
        Rooms + "room205-bathroom.jpg"
    };

    public static async Task SeedAsync(AppDbContext context, string? webRootPath = null)
    {
        await FixHotelsAsync(context);
        await FixRoomsAsync(context, GetExtraPhotos(webRootPath));
        await LinkPlacePhotosAsync(context, webRootPath);
        await LinkCarPhotosAsync(context, webRootPath);

        await context.SaveChangesAsync();
    }


    // =========================
    // HOTELS
    // =========================
    private static async Task FixHotelsAsync(AppDbContext context)
    {
        var hotels = await context.Hotels.ToListAsync();

        foreach (var hotel in hotels)
        {
            var city = (hotel.City ?? string.Empty).Trim().ToLower();

            if (!CitiesWithPhotos.Contains(city))
                continue;

            // Köhnə "/assets/images/hotels/baku-hotel.jpg" kimi yollar mövcud deyil
            var isMissing =
                string.IsNullOrWhiteSpace(hotel.MainImage) ||
                string.Equals(hotel.MainImage, $"{Hotels}{city}-hotel.jpg", StringComparison.OrdinalIgnoreCase);

            if (isMissing)
            {
                hotel.MainImage = $"{Hotels}{city}-exterior.jpg";
                hotel.UpdatedAt = DateTime.UtcNow;
            }
        }
    }


    // =========================
    // ROOMS
    // =========================
    private static async Task FixRoomsAsync(AppDbContext context, List<string> extraPhotos)
    {
        var rooms = await context.Rooms
            .Include(r => r.Hotel)
            .Include(r => r.Images)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Id)
            .ToListAsync();

        foreach (var (room, image) in PlanRoomImages(
                     rooms.Select(r => new RoomPhotoSlot(r.Id, r.HotelId, (r.Hotel?.City ?? string.Empty).Trim().ToLower(), r.RoomType, r.MainImage)).ToList(),
                     extraPhotos))
        {
            var entity = rooms.First(r => r.Id == room.RoomId);

            entity.MainImage = image;
            entity.UpdatedAt = DateTime.UtcNow;

            if (image != NoPhoto)
                AddGalleryIfEmpty(context, entity, room.City, image);
        }
    }

    /// <summary>Otaq haqqında şəkil seçmək üçün lazım olanlar (bazasız test oluna bilsin).</summary>
    public record RoomPhotoSlot(int RoomId, int HotelId, string City, RoomType Type, string? Image);

    /// <summary>
    /// Hansı otağa hansı şəkil? Qaydalar:
    ///   • eyni oteldə iki otaq eyni fotonu (və ya onun güzgü əksini) göstərmir;
    ///   • güzgü əksi (variants) şəkillər artıq istifadə olunmur;
    ///   • admin yükləməsi (/uploads/...) heç vaxt dəyişmir;
    ///   • foto çatmırsa otaq "Şəkil tezliklə" (NoPhoto) alır - admin özü yükləyə bilər;
    ///   • extra qovluğuna yeni foto atılanda əvvəl NoPhoto otaqlar, sonra başqa otellərdə də
    ///     görünən (təkrarlanan) şəkilli otaqlar onu alır.
    /// Yalnız dəyişməli olan otaqlar qaytarılır.
    /// </summary>
    public static List<(RoomPhotoSlot Room, string Image)> PlanRoomImages(List<RoomPhotoSlot> rooms, List<string> extraPhotos)
    {
        var usedInHotel = new Dictionary<int, HashSet<string>>();
        var globalUse = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var toAssign = new List<RoomPhotoSlot>();
        var kept = new List<RoomPhotoSlot>();

        HashSet<string> HotelSet(int hotelId) =>
            usedInHotel.TryGetValue(hotelId, out var set)
                ? set
                : usedInHotel[hotelId] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Saxlanılanlar: admin yükləməsi həmişə; layihənin real fotosu - oteldə ilk dəfədirsə
        foreach (var room in rooms.OrderBy(r => r.HotelId).ThenBy(r => TypeRank(r.Type)).ThenBy(r => r.RoomId))
        {
            var image = room.Image;

            if (!string.IsNullOrWhiteSpace(image) && !IsBuiltIn(image))
            {
                HotelSet(room.HotelId).Add(image);
                continue;
            }

            var keep =
                !string.IsNullOrWhiteSpace(image) &&
                image != NoPhoto &&
                !MissingRoomImages.Contains(image) &&
                !IsVariant(image) &&
                HotelSet(room.HotelId).Add(BaseOf(image));

            if (keep)
            {
                globalUse[BaseOf(image!)] = globalUse.GetValueOrDefault(BaseOf(image!)) + 1;
                kept.Add(room);
            }
            else
            {
                toAssign.Add(room);
            }
        }

        // 2) extra qovluğunda hələ işlənməyən fotolar varsa, NoPhoto-dan sonra
        //    başqa otellərdə də görünən şəkilli otaqlara verilir
        var freeExtras = extraPhotos.Count(p => !globalUse.ContainsKey(p)) -
                         toAssign.Count(r => string.IsNullOrWhiteSpace(r.Image) || r.Image == NoPhoto);

        if (freeExtras > 0)
        {
            foreach (var room in kept
                         .Where(r => globalUse.GetValueOrDefault(BaseOf(r.Image!)) > 1 && r.Image != CityRoomPhoto(r.City))
                         .OrderByDescending(r => globalUse[BaseOf(r.Image!)])
                         .ThenByDescending(r => r.RoomId)
                         .Take(freeExtras)
                         .ToList())
            {
                globalUse[BaseOf(room.Image!)]--;
                usedInHotel[room.HotelId].Remove(BaseOf(room.Image!));
                toAssign.Add(room);
            }
        }

        // 3) Yeni şəkil: oteldə olmayan, ümumilikdə ən az işlənən uyğun foto
        var result = new List<(RoomPhotoSlot, string)>();

        foreach (var room in toAssign.OrderBy(r => r.HotelId).ThenBy(r => TypeRank(r.Type)).ThenBy(r => r.RoomId))
        {
            var hotelSet = HotelSet(room.HotelId);

            var image = BuildCandidates(room.Type, room.City, extraPhotos)
                .Select((path, index) => (path, index))
                .Where(c => !hotelSet.Contains(c.path))
                .OrderBy(c => globalUse.GetValueOrDefault(c.path))
                .ThenBy(c => c.index)
                .Select(c => c.path)
                .FirstOrDefault() ?? NoPhoto;

            if (image != NoPhoto)
            {
                hotelSet.Add(image);
                globalUse[image] = globalUse.GetValueOrDefault(image) + 1;
            }

            if (!string.Equals(room.Image, image, StringComparison.OrdinalIgnoreCase))
                result.Add((room, image));
        }

        return result;
    }

    // Güzgü əksi -> əsl foto (eyni şəkil sayılır)
    private static string BaseOf(string path)
    {
        if (!IsVariant(path))
            return path;

        var name = Path.GetFileNameWithoutExtension(path);
        if (name.EndsWith("-a") || name.EndsWith("-b"))
            name = name[..^2];

        return BasePhotos.TryGetValue(name, out var real) ? real : path;
    }

    // Bu otaq üçün uyğun şəkillər, ən yaxşısı birinci
    private static List<string> BuildCandidates(RoomType type, string city, List<string> extraPhotos)
    {
        var list = new List<string>();

        var preferred = Preferred.TryGetValue(type, out var names)
            ? names
            : Preferred[RoomType.Double];

        // Deluxe: şəhərin öz otaq fotosu
        if (type == RoomType.Deluxe && CitiesWithPhotos.Contains(city))
            list.Add($"{Hotels}{city}-room.jpg");

        // Əvvəl uyğun real fotolar, sonra extra, sonra qalanları
        list.AddRange(preferred.Select(n => BasePhotos[n]));
        list.AddRange(extraPhotos.Where(p => MatchesType(p, type)));
        list.AddRange(extraPhotos);
        list.AddRange(BasePhotos.Values);

        // Güzgü əksi variantlar istifadə olunmur: eyni otaq iki dəfə görünürdü

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddGalleryIfEmpty(AppDbContext context, Room room, string city, string main)
    {
        if (room.Images.Any(i => !i.IsDeleted))
            return;

        var gallery = new List<string>
        {
            Bathrooms[Math.Abs(room.HotelId + (int)room.RoomType) % Bathrooms.Length]
        };

        if (CitiesWithPhotos.Contains(city))
        {
            gallery.Add($"{Hotels}{city}-lobby.jpg");
            gallery.Add($"{Hotels}{city}-pool.jpg");
        }

        foreach (var url in gallery.Where(u => u != main).Distinct())
        {
            context.RoomImages.Add(new RoomImage
            {
                RoomId = room.Id,
                ImageUrl = url,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }
    }

    // =========================
    // ATTRACTIONS & CARS
    // =========================

    // wwwroot/assets/images/guide/{şəhər}-{yer}.jpg varsa, yerə bağla.
    // Məs. "Hagia Sophia" (Istanbul) -> guide/istanbul-hagia-sophia.jpg
    private static async Task LinkPlacePhotosAsync(AppDbContext context, string? webRootPath)
    {
        var files = GetPhotoFiles(webRootPath, "guide");

        if (files.Count == 0)
            return;

        var places = await context.CityPlaces
            .Where(p => !p.IsDeleted && (p.ImageUrl == null || p.ImageUrl == ""))
            .ToListAsync();

        foreach (var place in places)
        {
            if (files.TryGetValue(Slug($"{place.City} {place.Name}"), out var url))
            {
                place.ImageUrl = url;
                place.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    // wwwroot/assets/images/cars/{model}.jpg varsa, maşına bağla.
    // Məs. "Toyota Corolla" -> cars/toyota-corolla.jpg
    private static async Task LinkCarPhotosAsync(AppDbContext context, string? webRootPath)
    {
        var files = GetPhotoFiles(webRootPath, "cars");

        if (files.Count == 0)
            return;

        var cars = await context.RentalCars
            .Where(c => !c.IsDeleted && (c.ImageUrl == null || c.ImageUrl == ""))
            .ToListAsync();

        foreach (var car in cars)
        {
            if (files.TryGetValue(Slug(car.Model), out var url))
            {
                car.ImageUrl = url;
                car.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    // Qovluqdakı fotolar: fayl adı (uzantısız) -> URL
    private static Dictionary<string, string> GetPhotoFiles(string? webRootPath, string folderName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(webRootPath))
            return result;

        var folder = Path.Combine(webRootPath, "assets", "images", folderName);

        if (!Directory.Exists(folder))
            return result;

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        foreach (var file in Directory.GetFiles(folder).OrderBy(f => f))
        {
            if (!allowed.Contains(Path.GetExtension(file).ToLowerInvariant()))
                continue;

            var key = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

            result.TryAdd(key, $"/assets/images/{folderName}/{Path.GetFileName(file)}");
        }

        return result;
    }

    // "Highland Park & Flame Towers" -> "highland-park-and-flame-towers"
    public static string Slug(string text)
    {
        var value = (text ?? string.Empty)
            .ToLowerInvariant()
            .Replace("&", " and ")
            .Replace("'", "")
            .Replace("’", "");

        var chars = value.Select(c => char.IsLetterOrDigit(c) && c < 128 ? c : '-').ToArray();
        var slug = new string(chars);

        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }

    private static string? CityRoomPhoto(string city)
        => CitiesWithPhotos.Contains(city) ? $"{Hotels}{city}-room.jpg" : null;

    private static int TypeRank(RoomType type)
    {
        var index = Array.IndexOf(TypeOrder, type);
        return index < 0 ? TypeOrder.Length : index;
    }

    // Güzgü əksi / kəsilmiş variant (real foto bitəndə istifadə olunur)
    private static bool IsVariant(string? path)
        => path != null && path.StartsWith(Variants, StringComparison.OrdinalIgnoreCase);

    // extra/king-01.jpg, extra/suite-sea.jpg -> adında otaq tipi varsa, əvvəl həmin tipə verilir
    private static bool MatchesType(string path, RoomType type)
        => Path.GetFileName(path).StartsWith(type.ToString(), StringComparison.OrdinalIgnoreCase);

    // Layihənin öz şəkilləri (admin yükləməsi deyil)
    private static bool IsBuiltIn(string? path)
        => path != null && path.StartsWith("/assets/images/", StringComparison.OrdinalIgnoreCase);

    // wwwroot/assets/images/rooms/extra içindəki fotolar
    private static List<string> GetExtraPhotos(string? webRootPath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath))
            return new List<string>();

        var folder = Path.Combine(webRootPath, "assets", "images", "rooms", "extra");

        if (!Directory.Exists(folder))
            return new List<string>();

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        return Directory.GetFiles(folder)
            .Where(f => allowed.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f)
            .Select(f => Extra + Path.GetFileName(f))
            .ToList();
    }
}
