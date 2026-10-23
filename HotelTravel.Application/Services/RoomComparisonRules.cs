using HotelTravel.Application.DTOs.Rooms;

namespace HotelTravel.Application.Services;

/// <summary>
/// Otaq müqayisəsinin təmiz qaydaları (DB-dən asılı deyil, test olunur).
/// </summary>
public static class RoomComparisonRules
{
    /// <summary>Eyni anda ən çox neçə otaq müqayisə oluna bilər.</summary>
    public const int MaxRooms = 3;

    /// <summary>
    /// "3,7,7,abc,-1,12,15" kimi mətndən etibarlı, təkrarsız Id-ləri çıxarır (ən çox 3).
    /// </summary>
    public static List<int> ParseIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return new List<int>();

        return ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .Take(MaxRooms)
            .ToList();
    }

    /// <summary>
    /// Otaqları müqayisə edir. Bərabərlikdə heç kim "ən yaxşı" seçilmir,
    /// çünki bütün otaqlar eyni olanda nişan mənasızdır.
    /// </summary>
    public static RoomComparisonDto Build(IEnumerable<GetRoomDto> rooms)
    {
        var list = rooms.Where(r => r is not null).ToList();

        var result = new RoomComparisonDto
        {
            Rooms = list,
            AllAmenities = list
                .SelectMany(r => r.AmenityNames ?? new List<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        if (list.Count < 2)
            return result;

        result.CheapestRoomId = UniqueWinner(list, r => -r.PricePerNight);
        result.LargestRoomId = UniqueWinner(list, r => r.Capacity);
        result.MostAmenitiesRoomId = UniqueWinner(list, r => r.AmenityNames?.Count ?? 0);

        // Rəyi olmayan otaq "ən yüksək reytinq" ola bilməz
        var reviewed = list.Where(r => r.ReviewCount > 0).ToList();
        result.BestRatedRoomId = reviewed.Count switch
        {
            0 => null,
            1 => reviewed[0].Id,
            _ => UniqueWinner(reviewed, r => (decimal)r.AverageRating)
        };

        return result;
    }

    /// <summary>Ən böyük dəyəri yalnız bir otaq daşıyırsa onun Id-si, yoxsa null.</summary>
    private static int? UniqueWinner(List<GetRoomDto> rooms, Func<GetRoomDto, decimal> score)
    {
        var best = rooms.Max(score);
        var winners = rooms.Where(r => score(r) == best).ToList();

        return winners.Count == 1 ? winners[0].Id : null;
    }
}
