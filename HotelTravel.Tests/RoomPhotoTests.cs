using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Seed;
using Xunit;
using Slot = HotelTravel.Infrastructure.Seed.MediaFixSeeder.RoomPhotoSlot;

namespace HotelTravel.Tests;

// Otaq şəkilləri: bir oteldə təkrar yoxdur, güzgü əksi yoxdur, çatmayanda "şəkil tezliklə"
public class RoomPhotoTests
{
    private const string Variant = "/assets/images/rooms/variants/room201-bedroom-1-a.jpg";
    private const string Real = "/assets/images/rooms/room201-bedroom-1.jpg";

    private static Dictionary<int, string> Final(List<Slot> rooms, List<string>? extras = null)
    {
        var map = rooms.ToDictionary(r => r.RoomId, r => r.Image ?? "");
        foreach (var (room, image) in MediaFixSeeder.PlanRoomImages(rooms, extras ?? new List<string>()))
            map[room.RoomId] = image;
        return map;
    }

    [Fact]
    public void SameHotel_ShouldNotShowSamePhoto_OrItsMirror()
    {
        var rooms = new List<Slot>
        {
            new(1, 5, "antalya", RoomType.Single, Real),
            new(2, 5, "antalya", RoomType.Twin, Variant),  // eyni fotonun güzgü əksi
            new(3, 5, "antalya", RoomType.Double, Real)    // eyni foto
        };

        var map = Final(rooms);

        Assert.Contains(Real, map.Values);                          // foto bir otaqda qalır
        Assert.Equal(3, map.Values.Distinct().Count());             // hamısı fərqli
        Assert.DoesNotContain(map.Values, v => v.Contains("/variants/"));
    }

    [Fact]
    public void OtherHotels_MayReuseAPhoto_AndAdminUploadsNeverChange()
    {
        var rooms = new List<Slot>
        {
            new(1, 1, "baku", RoomType.Single, Real),
            new(2, 2, "dubai", RoomType.Single, Real),
            new(3, 2, "dubai", RoomType.King, "/uploads/rooms/my-photo.jpg")
        };

        Assert.Empty(MediaFixSeeder.PlanRoomImages(rooms, new List<string>()));
    }

    [Fact]
    public void WhenPhotosRunOut_RoomGetsPlaceholder()
    {
        // Bir oteldə 30 otaq: real fotolar (18) çatmır
        var rooms = Enumerable.Range(1, 30)
            .Select(i => new Slot(i, 7, "batumi", RoomType.Double, null))
            .ToList();

        var map = Final(rooms);
        var real = map.Values.Where(v => v != MediaFixSeeder.NoPhoto).ToList();

        Assert.Equal(real.Count, real.Distinct().Count());
        Assert.Contains(MediaFixSeeder.NoPhoto, map.Values);
    }

    [Fact]
    public void NewExtraPhoto_ShouldReplacePlaceholderFirst()
    {
        var rooms = Enumerable.Range(1, 19)
            .Select(i => new Slot(i, 7, "batumi", RoomType.Double, null))
            .ToList();

        var withoutExtra = Final(rooms);
        Assert.Single(withoutExtra.Values, MediaFixSeeder.NoPhoto);

        var placeholderRoom = withoutExtra.First(x => x.Value == MediaFixSeeder.NoPhoto).Key;
        var current = rooms.Select(r => r with { Image = withoutExtra[r.RoomId] }).ToList();

        var map = Final(current, new List<string> { "/assets/images/rooms/extra/double-1.jpg" });

        Assert.Equal("/assets/images/rooms/extra/double-1.jpg", map[placeholderRoom]);
        Assert.DoesNotContain(MediaFixSeeder.NoPhoto, map.Values);
    }
}
