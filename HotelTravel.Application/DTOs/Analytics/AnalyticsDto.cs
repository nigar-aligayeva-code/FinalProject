namespace HotelTravel.Application.DTOs.Analytics;

// Admin analitikası: bütün hesablamalar API-də aparılır
public class AnalyticsDto
{
    public DateTime GeneratedAt { get; set; }

    // GƏLİR (ləğv olunmayanlar)
    public decimal TotalRevenue { get; set; }
    public decimal HotelRevenue { get; set; }
    public decimal CarRevenue { get; set; }
    public decimal TaxiRevenue { get; set; }
    public decimal FlightRevenue { get; set; }

    // SİFARİŞLƏR
    public int TotalOrders { get; set; }
    public int HotelBookings { get; set; }
    public int CarRentals { get; set; }
    public int TaxiBookings { get; set; }
    public int FlightBookings { get; set; }

    // OTEL GÖSTƏRİCİLƏRİ
    public decimal CancellationRate { get; set; }
    public decimal AverageBookingValue { get; set; }
    public double AverageStayNights { get; set; }
    public double AverageLeadTimeDays { get; set; }
    public decimal OccupancyNext30Days { get; set; }
    public int UpcomingCheckIns7Days { get; set; }

    // ENDİRİMLƏR
    public decimal DiscountGiven { get; set; }
    public int DiscountedBookings { get; set; }

    public List<MonthlyRevenueDto> Monthly { get; set; } = new();
    public List<CityStatDto> TopCities { get; set; } = new();
    public List<RoomStatDto> TopRooms { get; set; } = new();
    public List<HotelRatingDto> HotelRatings { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Hotels { get; set; }
    public decimal Cars { get; set; }
    public decimal Taxis { get; set; }
    public decimal Flights { get; set; }
    public decimal Total => Hotels + Cars + Taxis + Flights;
}

public class CityStatDto
{
    public string City { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}

public class RoomStatDto
{
    public string RoomName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}

public class HotelRatingDto
{
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int Reviews { get; set; }
}
