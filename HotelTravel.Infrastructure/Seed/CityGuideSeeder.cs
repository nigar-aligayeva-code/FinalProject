using HotelTravel.Domain.Entities;
using HotelTravel.Domain.Enums;
using HotelTravel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelTravel.Infrastructure.Seed;

// Şəhər bələdçisi üçün başlanğıc məlumat.
// Cədvəl boşdursa bir dəfə doldurulur; sonra admin paneldən idarə olunur.
public static class CityGuideSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.CityPlaces.AnyAsync())
            return;

        var places = new List<CityPlace>();
        var order = 0;

        void Add(
            string city,
            PlaceCategory category,
            string name,
            string description,
            string? address = null,
            decimal? priceFrom = null,
            string? duration = null)
        {
            places.Add(new CityPlace
            {
                City = city,
                Category = category,
                Name = name,
                Description = description,
                Address = address,
                PriceFrom = priceFrom,
                Duration = duration,
                SortOrder = order++,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        // =========================
        // BAKU
        // =========================
        Add("Baku", PlaceCategory.Sight, "Icherisheher (Old City)",
            "UNESCO-listed walled old town with narrow streets, the Maiden Tower and the Palace of the Shirvanshahs.",
            "Icherisheher, Baku");
        Add("Baku", PlaceCategory.Sight, "Heydar Aliyev Center",
            "Iconic cultural centre designed by Zaha Hadid, known for its flowing white curves and exhibitions.",
            "1 Heydar Aliyev Ave, Baku");
        Add("Baku", PlaceCategory.Sight, "Highland Park & Flame Towers",
            "The best panoramic view of Baku Bay, especially at sunset when the Flame Towers light up.",
            "Highland Park, Baku");
        Add("Baku", PlaceCategory.Sight, "Baku Boulevard",
            "Long seaside promenade along the Caspian with parks, cafés and evening walks.",
            "Neftchilar Ave, Baku");
        Add("Baku", PlaceCategory.Tour, "Gobustan & Mud Volcanoes",
            "Day trip to ancient rock carvings at Gobustan National Park and the bubbling mud volcanoes nearby.",
            null, 70, "5 hours");
        Add("Baku", PlaceCategory.Tour, "Absheron Fire Tour",
            "Visit Yanar Dag, the hillside that burns non-stop, and the Ateshgah fire temple.",
            null, 50, "4 hours");
        Add("Baku", PlaceCategory.Food, "Old City tea houses",
            "Try plov, dolma and qutab, then finish with tea and jam in a traditional tea house inside the old walls.",
            "Icherisheher, Baku");
        Add("Baku", PlaceCategory.Shopping, "Nizami Street",
            "Pedestrian street in the city centre with fashion stores, souvenir shops and cafés.",
            "Nizami St, Baku");

        // =========================
        // GABALA
        // =========================
        Add("Gabala", PlaceCategory.Sight, "Tufandag Mountain Resort",
            "Ride the cable car up the Greater Caucasus for views over forests and valleys, all year round.",
            "Tufandag, Gabala");
        Add("Gabala", PlaceCategory.Sight, "Nohur Lake",
            "Calm lake surrounded by forest and mountains, perfect for a picnic or a boat ride.",
            "Nohur, Gabala");
        Add("Gabala", PlaceCategory.Sight, "Yeddi Gozel Waterfall",
            "The \"Seven Beauties\" waterfall: a short forest walk to seven cascading falls.",
            "Vandam, Gabala");
        Add("Gabala", PlaceCategory.Tour, "Sheki Day Trip",
            "Explore the Palace of the Shaki Khans and the old caravanserai in nearby Sheki.",
            null, 90, "8 hours");
        Add("Gabala", PlaceCategory.Tour, "Mountains & Lake Tour",
            "Cable car ride at Tufandag followed by a relaxed stop at Nohur Lake.",
            null, 60, "4 hours");
        Add("Gabala", PlaceCategory.Food, "Mountain cuisine",
            "Lamb kebab, fresh greens and local honey at riverside restaurants around the town.");
        Add("Gabala", PlaceCategory.Shopping, "Local farmers' market",
            "Seasonal fruit, nuts, jams and honey from the surrounding villages.");

        // =========================
        // ISTANBUL
        // =========================
        Add("Istanbul", PlaceCategory.Sight, "Hagia Sophia",
            "Nearly 1,500 years of history under one of the world's most famous domes.",
            "Sultanahmet, Fatih");
        Add("Istanbul", PlaceCategory.Sight, "Blue Mosque",
            "Sultan Ahmed Mosque, famous for its six minarets and blue Iznik tiles.",
            "Sultanahmet, Fatih");
        Add("Istanbul", PlaceCategory.Sight, "Topkapi Palace",
            "Former home of the Ottoman sultans with courtyards, treasury and Bosphorus views.",
            "Cankurtaran, Fatih");
        Add("Istanbul", PlaceCategory.Sight, "Galata Tower",
            "Medieval stone tower with a 360° view over the Golden Horn and the old city.",
            "Galata, Beyoglu");
        Add("Istanbul", PlaceCategory.Tour, "Bosphorus Cruise",
            "Sail between Europe and Asia past palaces, fortresses and waterside mansions.",
            null, 60, "2 hours");
        Add("Istanbul", PlaceCategory.Tour, "Old City Walking Tour",
            "Guided walk through Sultanahmet with the stories behind its mosques and palaces.",
            null, 50, "3 hours");
        Add("Istanbul", PlaceCategory.Food, "Karakoy & Kadikoy food streets",
            "Baklava, fish sandwiches, meze and Turkish coffee on both sides of the Bosphorus.");
        Add("Istanbul", PlaceCategory.Shopping, "Grand Bazaar",
            "One of the oldest covered markets in the world with thousands of shops.",
            "Beyazit, Fatih");
        Add("Istanbul", PlaceCategory.Shopping, "Istiklal Avenue",
            "Busy pedestrian avenue with shops, bookstores and the historic tram.",
            "Beyoglu");

        // =========================
        // ANTALYA
        // =========================
        Add("Antalya", PlaceCategory.Sight, "Kaleici Old Town",
            "Historic harbour quarter with Ottoman houses, Hadrian's Gate and narrow lanes.",
            "Kaleici, Muratpasa");
        Add("Antalya", PlaceCategory.Sight, "Duden Waterfalls",
            "Waterfalls that drop straight from the cliffs into the Mediterranean Sea.",
            "Lara, Antalya");
        Add("Antalya", PlaceCategory.Sight, "Konyaalti Beach",
            "Long pebble beach with mountain views and a seaside walking path.",
            "Konyaalti, Antalya");
        Add("Antalya", PlaceCategory.Tour, "Perge & Aspendos",
            "Ancient Greco-Roman city of Perge and the remarkably preserved Aspendos theatre.",
            null, 90, "6 hours");
        Add("Antalya", PlaceCategory.Tour, "Coastal Boat Trip",
            "Boat ride along the cliffs with swimming stops in clear water.",
            null, 70, "4 hours");
        Add("Antalya", PlaceCategory.Food, "Kaleici harbour restaurants",
            "Fresh seafood and Turkish dishes with a view over the old harbour.",
            "Kaleici, Muratpasa");
        Add("Antalya", PlaceCategory.Shopping, "Kaleici bazaar",
            "Small shops with textiles, ceramics, spices and souvenirs in the old town.",
            "Kaleici, Muratpasa");

        // =========================
        // TBILISI
        // =========================
        Add("Tbilisi", PlaceCategory.Sight, "Narikala Fortress",
            "Ancient fortress above the old town, reached on foot or by cable car.",
            "Old Tbilisi");
        Add("Tbilisi", PlaceCategory.Sight, "Abanotubani sulfur baths",
            "Historic brick-domed bath houses fed by natural hot springs.",
            "Abanotubani, Old Tbilisi");
        Add("Tbilisi", PlaceCategory.Sight, "Bridge of Peace",
            "Modern glass-and-steel pedestrian bridge over the Kura river.",
            "Old Tbilisi");
        Add("Tbilisi", PlaceCategory.Sight, "Mtatsminda Park",
            "Hilltop park with a funicular, rides and the best night view of the city.",
            "Mtatsminda");
        Add("Tbilisi", PlaceCategory.Tour, "Mtskheta Day Trip",
            "Georgia's ancient capital: Jvari Monastery and Svetitskhoveli Cathedral.",
            null, 70, "5 hours");
        Add("Tbilisi", PlaceCategory.Tour, "Kakheti Wine Tour",
            "Visit family wineries in Georgia's wine region and taste qvevri wine.",
            null, 120, "9 hours");
        Add("Tbilisi", PlaceCategory.Food, "Georgian cuisine",
            "Khachapuri, khinkali and churchkhela in the restaurants of the old town.",
            "Old Tbilisi");
        Add("Tbilisi", PlaceCategory.Shopping, "Dry Bridge market",
            "Open-air flea market with Soviet-era items, paintings and antiques.",
            "Dry Bridge, Tbilisi");
        Add("Tbilisi", PlaceCategory.Shopping, "Rustaveli Avenue",
            "Main avenue with shops, theatres and museums.",
            "Rustaveli Ave, Tbilisi");

        // =========================
        // BATUMI
        // =========================
        Add("Batumi", PlaceCategory.Sight, "Batumi Boulevard",
            "Seaside boulevard with gardens, fountains and bike paths along the Black Sea.",
            "Batumi Boulevard");
        Add("Batumi", PlaceCategory.Sight, "Alphabetic Tower",
            "Tower decorated with the letters of the Georgian alphabet, with a viewing deck on top.",
            "Batumi Seafront");
        Add("Batumi", PlaceCategory.Sight, "Ali and Nino statue",
            "Moving steel sculpture of two lovers that meet and pass through each other.",
            "Batumi Seafront");
        Add("Batumi", PlaceCategory.Sight, "Batumi Botanical Garden",
            "Huge garden on the hills above the sea with plants from around the world.",
            "Green Cape, Batumi");
        Add("Batumi", PlaceCategory.Tour, "Mountain Adjara & Waterfalls",
            "Drive into the green mountains of Adjara with stops at waterfalls and old stone bridges.",
            null, 80, "6 hours");
        Add("Batumi", PlaceCategory.Tour, "Gonio Fortress & Coast",
            "Ancient Roman fortress and a relaxed drive along the coast.",
            null, 50, "3 hours");
        Add("Batumi", PlaceCategory.Food, "Adjarian khachapuri",
            "Boat-shaped cheese bread with egg and butter, plus fresh fish near the seafront.");
        Add("Batumi", PlaceCategory.Shopping, "Old Batumi streets",
            "Small boutiques, wine shops and souvenir stores in the historic centre.",
            "Old Batumi");

        // =========================
        // DUBAI
        // =========================
        Add("Dubai", PlaceCategory.Sight, "Burj Khalifa",
            "The world's tallest building, with observation decks high above the city.",
            "Downtown Dubai");
        Add("Dubai", PlaceCategory.Sight, "Dubai Marina",
            "Waterfront district with skyscrapers, yachts and a long promenade.",
            "Dubai Marina");
        Add("Dubai", PlaceCategory.Sight, "Al Fahidi Historical District",
            "Old Dubai with wind-tower houses, galleries and the Dubai Museum area.",
            "Bur Dubai");
        Add("Dubai", PlaceCategory.Sight, "Palm Jumeirah",
            "Palm-shaped artificial island with beaches, resorts and views of the skyline.",
            "Palm Jumeirah");
        Add("Dubai", PlaceCategory.Tour, "Desert Safari",
            "Dune drive, sunset in the desert and dinner at a desert camp.",
            null, 220, "6 hours");
        Add("Dubai", PlaceCategory.Tour, "Creek & Souks Tour",
            "Cross Dubai Creek by traditional abra boat and explore the old markets.",
            null, 60, "3 hours");
        Add("Dubai", PlaceCategory.Food, "Marina waterfront dining",
            "Restaurants and cafés from around the world along the marina promenade.",
            "Dubai Marina");
        Add("Dubai", PlaceCategory.Shopping, "The Dubai Mall",
            "One of the largest malls in the world, next to the Burj Khalifa and the fountain show.",
            "Downtown Dubai");
        Add("Dubai", PlaceCategory.Shopping, "Gold & Spice Souks",
            "Traditional markets in Deira for gold jewellery, spices and perfumes.",
            "Deira, Dubai");

        await context.CityPlaces.AddRangeAsync(places);
        await context.SaveChangesAsync();
    }
}
