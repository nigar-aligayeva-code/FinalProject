using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

public static class HotelDataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // ==========================================
        // AMENITIES
        // ==========================================

        var amenitiesData = new[]
        {
            new { Name = "Free Wi-Fi", Icon = "bi bi-wifi" },
            new { Name = "Air Conditioning", Icon = "bi bi-snow" },
            new { Name = "Breakfast", Icon = "bi bi-cup-hot" },
            new { Name = "TV", Icon = "bi bi-tv" },
            new { Name = "Parking", Icon = "bi bi-car-front" },
            new { Name = "Swimming Pool", Icon = "bi bi-water" },
            new { Name = "Spa", Icon = "bi bi-heart" },
            new { Name = "Gym", Icon = "bi bi-activity" },
            new { Name = "Balcony", Icon = "bi bi-building" },
            new { Name = "Room Service", Icon = "bi bi-bell" }
        };

        foreach (var item in amenitiesData)
        {
            var exists = await context.Amenities
                .AnyAsync(a => a.Name == item.Name);

            if (!exists)
            {
                context.Amenities.Add(
                    new Amenity
                    {
                        Name = item.Name,
                        Icon = item.Icon
                    });
            }
        }

        await context.SaveChangesAsync();


        // ==========================================
        // HOTELS
        // ==========================================

        var hotels = new List<Hotel>
        {
            new Hotel
            {
                Name = "Caspian Grand Hotel",
                Description =
                    "A luxury hotel in the heart of Baku offering elegant rooms and modern facilities.",
                Address = "Neftchilar Avenue 10",
                City = "Baku",
                Country = "Azerbaijan",
                Phone = "+994 12 555 10 10",
                Email = "baku@hotelhub.com",
                MainImage = "/assets/images/hotels/baku-hotel.jpg",
                Latitude = 40.3667,
                Longitude = 49.8352,
                StarRating = 5
            },

            new Hotel
            {
                Name = "Gabala Mountain Resort",
                Description =
                    "A peaceful mountain resort surrounded by the beautiful nature of Gabala.",
                Address = "Heydar Aliyev Avenue 25",
                City = "Gabala",
                Country = "Azerbaijan",
                Phone = "+994 24 555 20 20",
                Email = "gabala@hotelhub.com",
                MainImage = "/assets/images/hotels/gabala-hotel.jpg",
                Latitude = 40.9814,
                Longitude = 47.8458,
                StarRating = 5
            },

            new Hotel
            {
                Name = "Bosphorus Palace Hotel",
                Description =
                    "A stylish Istanbul hotel offering comfortable rooms near the Bosphorus.",
                Address = "Besiktas Avenue 18",
                City = "Istanbul",
                Country = "Turkey",
                Phone = "+90 212 555 30 30",
                Email = "istanbul@hotelhub.com",
                MainImage = "/assets/images/hotels/istanbul-hotel.jpg",
                Latitude = 41.0422,
                Longitude = 29.0083,
                StarRating = 5
            },

            new Hotel
            {
                Name = "Antalya Blue Resort",
                Description =
                    "A Mediterranean resort offering relaxing stays, pools and seaside experiences.",
                Address = "Lara Beach Road 45",
                City = "Antalya",
                Country = "Turkey",
                Phone = "+90 242 555 40 40",
                Email = "antalya@hotelhub.com",
                MainImage = "/assets/images/hotels/antalya-hotel.jpg",
                Latitude = 36.8500,
                Longitude = 30.8500,
                StarRating = 5
            },

            new Hotel
            {
                Name = "Tbilisi Heritage Hotel",
                Description =
                    "A comfortable hotel combining Georgian character with modern hospitality.",
                Address = "Rustaveli Avenue 32",
                City = "Tbilisi",
                Country = "Georgia",
                Phone = "+995 32 555 50 50",
                Email = "tbilisi@hotelhub.com",
                MainImage = "/assets/images/hotels/tbilisi-hotel.jpg",
                Latitude = 41.7151,
                Longitude = 44.8271,
                StarRating = 4
            },

            new Hotel
            {
                Name = "Batumi Sea View Hotel",
                Description =
                    "A modern hotel near the Black Sea with beautiful views and spacious rooms.",
                Address = "Sherif Khimshiashvili Street 15",
                City = "Batumi",
                Country = "Georgia",
                Phone = "+995 422 555 60 60",
                Email = "batumi@hotelhub.com",
                MainImage = "/assets/images/hotels/batumi-hotel.jpg",
                Latitude = 41.6168,
                Longitude = 41.6367,
                StarRating = 4
            },

            new Hotel
            {
                Name = "Dubai Marina Luxury Hotel",
                Description =
                    "A premium hotel in Dubai Marina with luxury accommodation and modern amenities.",
                Address = "Dubai Marina Walk 50",
                City = "Dubai",
                Country = "United Arab Emirates",
                Phone = "+971 4 555 70 70",
                Email = "dubai@hotelhub.com",
                MainImage = "/assets/images/hotels/dubai-hotel.jpg",
                Latitude = 25.0800,
                Longitude = 55.1400,
                StarRating = 5
            }
        };


        // ==========================================
        // ADD HOTELS IF THEY DO NOT EXIST
        // ==========================================

        foreach (var hotel in hotels)
        {
            var exists = await context.Hotels
                .AnyAsync(h => h.Name == hotel.Name);

            if (!exists)
            {
                context.Hotels.Add(hotel);
            }
        }

        await context.SaveChangesAsync();


        // ==========================================
        // GET AMENITIES
        // ==========================================

        var allAmenities = await context.Amenities
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        var wifi = allAmenities
            .First(a => a.Name == "Free Wi-Fi");

        var airConditioning = allAmenities
            .First(a => a.Name == "Air Conditioning");

        var breakfast = allAmenities
            .First(a => a.Name == "Breakfast");

        var tv = allAmenities
            .First(a => a.Name == "TV");

        var parking = allAmenities
            .First(a => a.Name == "Parking");

        var pool = allAmenities
            .First(a => a.Name == "Swimming Pool");

        var spa = allAmenities
            .First(a => a.Name == "Spa");

        var gym = allAmenities
            .First(a => a.Name == "Gym");

        var balcony = allAmenities
            .First(a => a.Name == "Balcony");

        var roomService = allAmenities
            .First(a => a.Name == "Room Service");


        // ==========================================
        // GET SEEDED HOTELS
        // ==========================================

        var seededHotels = await context.Hotels
            .Where(h =>
                hotels.Select(x => x.Name)
                    .Contains(h.Name))
            .ToListAsync();


        // ==========================================
        // ROOMS
        // ==========================================

        foreach (var hotel in seededHotels)
        {
            // --------------------------------------
            // ROOM 1 - SINGLE
            // --------------------------------------

            await AddRoomIfNotExistsAsync(
                context,
                hotel,
                $"{hotel.Id}01",
                "Comfort Single Room",
                "A comfortable room ideal for one guest.",
                90m,
                1,
                RoomType.Single,
                "/assets/images/rooms/single-room.jpg",
                new List<Amenity>
                {
                    wifi,
                    airConditioning,
                    tv
                });


            // --------------------------------------
            // ROOM 2 - DOUBLE
            // --------------------------------------

            await AddRoomIfNotExistsAsync(
                context,
                hotel,
                $"{hotel.Id}02",
                "Classic Double Room",
                "A spacious double room perfect for couples.",
                140m,
                2,
                RoomType.Double,
                "/assets/images/rooms/double-room.jpg",
                new List<Amenity>
                {
                    wifi,
                    airConditioning,
                    breakfast,
                    tv
                });


            // --------------------------------------
            // ROOM 3 - DELUXE
            // --------------------------------------

            await AddRoomIfNotExistsAsync(
                context,
                hotel,
                $"{hotel.Id}03",
                "Deluxe Room",
                "An elegant deluxe room with premium comfort.",
                220m,
                2,
                RoomType.Deluxe,
                "/assets/images/rooms/deluxe-room.jpg",
                new List<Amenity>
                {
                    wifi,
                    airConditioning,
                    breakfast,
                    tv,
                    roomService
                });


            // --------------------------------------
            // ROOM 4 - SUITE
            // --------------------------------------

            await AddRoomIfNotExistsAsync(
                context,
                hotel,
                $"{hotel.Id}04",
                "Luxury Suite",
                "A luxurious suite with additional space and premium amenities.",
                350m,
                3,
                RoomType.Suite,
                "/assets/images/rooms/suite-room.jpg",
                new List<Amenity>
                {
                    wifi,
                    airConditioning,
                    breakfast,
                    tv,
                    spa,
                    gym,
                    roomService
                });


            // --------------------------------------
            // ROOM 5 - FAMILY
            // --------------------------------------

            await AddRoomIfNotExistsAsync(
                context,
                hotel,
                $"{hotel.Id}05",
                "Family Room",
                "A spacious family room designed for a comfortable family stay.",
                280m,
                5,
                RoomType.Family,
                "/assets/images/rooms/family-room.jpg",
                new List<Amenity>
                {
                    wifi,
                    airConditioning,
                    breakfast,
                    tv,
                    parking,
                    pool,
                    balcony
                });
        }

        await context.SaveChangesAsync();


        // ==========================================
        // HOTEL GALLERY IMAGES
        // ==========================================

        foreach (var hotel in seededHotels)
        {
            string prefix = hotel.City.ToLower();

            var hotelImages = new List<(string ImageUrl, string Category)>
    {
        ($"/assets/images/hotels/{prefix}-exterior.jpg", "Overview"),
        ($"/assets/images/hotels/{prefix}-lobby.jpg", "Overview"),
        ($"/assets/images/hotels/{prefix}-room.jpg", "Rooms"),
        ($"/assets/images/hotels/{prefix}-restaurant.jpg", "Dining"),
        ($"/assets/images/hotels/{prefix}-pool.jpg", "Wellness"),
        ($"/assets/images/hotels/{prefix}-spa.jpg", "Wellness")
    };


            // Bu hotel üçün artıq/köhnə gallery şəkillərini tap
            var existingImages = await context.HotelImages
                .Where(x => x.HotelId == hotel.Id)
                .ToListAsync();


            // Artıq siyahımızda olmayan köhnə şəkilləri sil
            var oldImages = existingImages
                .Where(x => !hotelImages.Any(i => i.ImageUrl == x.ImageUrl))
                .ToList();

            if (oldImages.Any())
            {
                context.HotelImages.RemoveRange(oldImages);
            }


            // Çatışmayan şəkilləri əlavə et
            foreach (var image in hotelImages)
            {
                var exists = existingImages
                    .Any(x => x.ImageUrl == image.ImageUrl);

                if (!exists)
                {
                    context.HotelImages.Add(new HotelImage
                    {
                        HotelId = hotel.Id,
                        ImageUrl = image.ImageUrl,
                        Category = image.Category
                    });
                }
            }
        }
        // ==========================================
        // HOTELHUB BAKU GALLERY
        // ==========================================

        var hotelHubBaku = await context.Hotels
            .FirstOrDefaultAsync(h =>
                h.Name == "HotelHub Baku" &&
                !h.IsDeleted);

        if (hotelHubBaku != null)
        {
            var hotelHubImages = new List<(string ImageUrl, string Category)>
    {
        ("/assets/images/hotels/hotelhub-exterior.jpg", "Overview"),
        ("/assets/images/hotels/hotelhub-lobby.jpg", "Overview"),
        ("/assets/images/hotels/hotelhub-room.jpg", "Rooms"),
        ("/assets/images/hotels/hotelhub-restaurant.jpg", "Dining"),
        ("/assets/images/hotels/hotelhub-pool.jpg", "Wellness"),
        ("/assets/images/hotels/hotelhub-spa.jpg", "Wellness")
    };

            foreach (var image in hotelHubImages)
            {
                var exists = await context.HotelImages
                    .AnyAsync(x =>
                        x.HotelId == hotelHubBaku.Id &&
                        x.ImageUrl == image.ImageUrl);

                if (!exists)
                {
                    context.HotelImages.Add(new HotelImage
                    {
                        HotelId = hotelHubBaku.Id,
                        ImageUrl = image.ImageUrl,
                        Category = image.Category
                    });
                }
            }

            // HotelHub-un əsas kart şəklini də düzəldirik
            hotelHubBaku.MainImage =
                "/assets/images/hotels/hotelhub-exterior.jpg";
        }
        // ==========================================
        // ROOM MEDIA - ROOMS 201-205
        // ==========================================

        var roomMediaData = new Dictionary<string, (string MainImage, string? PanoramaImage, List<string> GalleryImages)>
        {
            // Room 201 - Comfort Single
            ["201"] = (
                "/assets/images/rooms/room201-bedroom-1.jpg",
                "/assets/images/rooms/room201-360.jpg",
                new List<string>
                {
            "/assets/images/rooms/room201-bedroom-2.jpg",
            "/assets/images/rooms/room201-bathroom.jpg"
                }
            ),

            // Room 202 - Classic Double
            ["202"] = (
                "/assets/images/rooms/room202-bedroom.jpg",
                null,
                new List<string>
                {
            "/assets/images/rooms/room202-bathroom.jpg",
            "/assets/images/rooms/room202-balcony.jpg"
                }
            ),

            // Room 203 - Deluxe
            ["203"] = (
                "/assets/images/rooms/room203-bedroom-1.jpg",
                null,
                new List<string>
                {
            "/assets/images/rooms/room203-bedroom-2.jpg",
            "/assets/images/rooms/room203-bathroom.jpg"
                }
            ),

            // Room 204 - Luxury Suite
            ["204"] = (
                "/assets/images/rooms/room204-bedroom.jpg",
                null,
                new List<string>
                {
            "/assets/images/rooms/room204-living-room.jpg",
            "/assets/images/rooms/room204-bathroom.jpg"
                }
            ),

            // Room 205 - Family Room
            ["205"] = (
                "/assets/images/rooms/room205-bedroom.jpg",
                null,
                new List<string>
                {
            "/assets/images/rooms/room205-family-area.jpg",
            "/assets/images/rooms/room205-bathroom.jpg"
                }
            )
        };


        // ==========================================
        // APPLY ROOM MEDIA
        // ==========================================

        foreach (var roomMedia in roomMediaData)
        {
            var roomNumber = roomMedia.Key;
            var media = roomMedia.Value;

            var room = await context.Rooms
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r =>
                    r.RoomNumber == roomNumber &&
                    !r.IsDeleted);

            if (room == null)
                continue;


            // ==========================================
            // MAIN IMAGE
            // ==========================================

            room.MainImage = media.MainImage;


            // ==========================================
            // 360 PANORAMA
            // ==========================================

            room.PanoramaImage = media.PanoramaImage;


            // ==========================================
            // EXISTING GALLERY IMAGES
            // ==========================================

            var existingImages = room.Images
                .Where(i => !i.IsDeleted)
                .ToList();


            // ==========================================
            // REMOVE OLD GALLERY IMAGES
            // ==========================================

            var oldImages = existingImages
                .Where(i =>
                    !media.GalleryImages.Contains(i.ImageUrl))
                .ToList();

            if (oldImages.Any())
            {
                context.RoomImages.RemoveRange(oldImages);
            }


            // ==========================================
            // ADD MISSING GALLERY IMAGES
            // ==========================================

            foreach (var imageUrl in media.GalleryImages)
            {
                var exists = existingImages
                    .Any(i => i.ImageUrl == imageUrl);

                if (!exists)
                {
                    context.RoomImages.Add(new RoomImage
                    {
                        RoomId = room.Id,
                        ImageUrl = imageUrl,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
            }


            room.UpdatedAt = DateTime.UtcNow;
        }


        // ==========================================
        // SAVE ALL CHANGES
        // ==========================================

        await context.SaveChangesAsync();
      
      
    }

    // ==============================================
    // ADD ROOM IF IT DOES NOT EXIST
    // ==============================================

    private static async Task AddRoomIfNotExistsAsync(
        AppDbContext context,
        Hotel hotel,
        string roomNumber,
        string name,
        string description,
        decimal price,
        int capacity,
        RoomType roomType,
        string mainImage,
        List<Amenity> amenities)
    {
        var exists = await context.Rooms
            .AnyAsync(r =>
                r.HotelId == hotel.Id &&
                r.RoomNumber == roomNumber);

        if (exists)
        {
            return;
        }

        var room = new Room
        {
            RoomNumber = roomNumber,
            Name = name,
            Description = description,
            PricePerNight = price,
            Capacity = capacity,
            MainImage = mainImage,
            RoomType = roomType,
            HotelId = hotel.Id,
            Amenities = amenities
        };

        context.Rooms.Add(room);
    }
}