using HotelTravel.Application.DTOs.Exports;
using HotelTravel.Application.Interfaces;

namespace HotelTravel.Application.Services;

// Admin hesabatları: rezervasiyalar, uçuşlar, taksilər, maşın icarələri -> Excel / CSV
public class ExportService : IExportService
{
    public static readonly string[] Types = { "bookings", "flights", "taxis", "cars" };

    private readonly IBookingService _bookingService;
    private readonly IFlightService _flightService;
    private readonly ITaxiService _taxiService;
    private readonly ICarRentalService _carRentalService;
    private readonly IXlsxWriter _xlsxWriter;

    public ExportService(
        IBookingService bookingService,
        IFlightService flightService,
        ITaxiService taxiService,
        ICarRentalService carRentalService,
        IXlsxWriter xlsxWriter)
    {
        _bookingService = bookingService;
        _flightService = flightService;
        _taxiService = taxiService;
        _carRentalService = carRentalService;
        _xlsxWriter = xlsxWriter;
    }

    public async Task<ExportFile?> ExportAsync(string type, string format)
    {
        var table = (type ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "bookings" => await BookingsAsync(),
            "flights" => await FlightsAsync(),
            "taxis" => await TaxisAsync(),
            "cars" => await CarsAsync(),
            _ => null
        };

        if (table == null)
            return null;

        var isCsv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);
        var name = $"Tripio-{type!.Trim().ToLowerInvariant()}-{DateTime.Now:yyyy-MM-dd}";

        return isCsv
            ? new ExportFile { FileName = name + ".csv", ContentType = "text/csv", Content = CsvExport.Write(table) }
            : new ExportFile
            {
                FileName = name + ".xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Content = _xlsxWriter.Write(table)
            };
    }

    private async Task<ExportTable> BookingsAsync()
    {
        var rows = (await _bookingService.GetAllAsync()).OrderByDescending(b => b.CheckInDate);

        return new ExportTable
        {
            Title = "Hotel bookings",
            SheetName = "Bookings",
            Columns =
            {
                new("Code"), new("Guest"), new("Email"), new("Hotel"), new("City"), new("Room"),
                new("Check-in", ExportColumnKind.Date), new("Check-out", ExportColumnKind.Date),
                new("Nights", ExportColumnKind.Number, Sum: true),
                new("Adults", ExportColumnKind.Number), new("Children", ExportColumnKind.Number),
                new("Status"),
                new("Original price", ExportColumnKind.Money, Sum: true),
                new("Discount", ExportColumnKind.Money, Sum: true),
                new("Total", ExportColumnKind.Money, Sum: true)
            },
            Rows = rows.Select(b => new object?[]
            {
                b.ConfirmationCode, b.GuestName, b.GuestEmail, b.HotelName, b.HotelCity, $"{b.RoomNumber} · {b.RoomName}",
                b.CheckInDate, b.CheckOutDate, (b.CheckOutDate.Date - b.CheckInDate.Date).Days,
                b.AdultCount, b.ChildrenCount, b.Status.ToString(),
                b.OriginalPrice > 0 ? b.OriginalPrice : b.TotalPrice, b.DiscountAmount, b.TotalPrice
            }).ToList()
        };
    }

    private async Task<ExportTable> FlightsAsync()
    {
        var rows = (await _flightService.GetBookingsAsync(null)).OrderByDescending(f => f.DepartureTime);

        return new ExportTable
        {
            Title = "Flight bookings",
            SheetName = "Flights",
            Columns =
            {
                new("Code"), new("Contact"), new("Email"), new("Flight"), new("Airline"),
                new("From"), new("To"), new("Departure", ExportColumnKind.DateTime), new("Cabin"),
                new("Passengers", ExportColumnKind.Number, Sum: true), new("Status"),
                new("Total", ExportColumnKind.Money, Sum: true)
            },
            Rows = rows.Select(f => new object?[]
            {
                f.ConfirmationCode, f.ContactName, f.Email, f.FlightNumber, f.Airline,
                $"{f.FromCity} ({f.FromCode})", $"{f.ToCity} ({f.ToCode})", f.DepartureTime, f.Cabin.ToString(),
                f.Passengers, f.Status.ToString(), f.TotalPrice
            }).ToList()
        };
    }

    private async Task<ExportTable> TaxisAsync()
    {
        var rows = (await _taxiService.GetAllAsync(null)).OrderByDescending(t => t.PickupTime);

        return new ExportTable
        {
            Title = "Airport taxis",
            SheetName = "Taxis",
            Columns =
            {
                new("Code"), new("Passenger"), new("Email"), new("City"), new("From"), new("To"),
                new("Pick-up", ExportColumnKind.DateTime), new("Vehicle"),
                new("Passengers", ExportColumnKind.Number), new("Distance km", ExportColumnKind.Number),
                new("Status"), new("Price", ExportColumnKind.Money, Sum: true)
            },
            Rows = rows.Select(t => new object?[]
            {
                t.ConfirmationCode, t.PassengerName, t.Email, t.City, t.FromText, t.ToText,
                t.PickupTime, t.VehicleType.ToString(), t.Passengers, t.DistanceKm,
                t.Status.ToString(), t.Price
            }).ToList()
        };
    }

    private async Task<ExportTable> CarsAsync()
    {
        var rows = (await _carRentalService.GetRentalsAsync()).OrderByDescending(r => r.PickupDate);

        return new ExportTable
        {
            Title = "Car rentals",
            SheetName = "Car rentals",
            Columns =
            {
                new("Code"), new("Driver"), new("Email"), new("City"), new("Car"), new("Category"),
                new("Pick-up", ExportColumnKind.Date), new("Return", ExportColumnKind.Date),
                new("Days", ExportColumnKind.Number, Sum: true), new("Status"),
                new("Price / day", ExportColumnKind.Money), new("Total", ExportColumnKind.Money, Sum: true)
            },
            Rows = rows.Select(r => new object?[]
            {
                r.ConfirmationCode, r.DriverName, r.Email, r.City, r.CarModel, r.CarCategory.ToString(),
                r.PickupDate, r.ReturnDate, r.Days, r.Status.ToString(), r.PricePerDay, r.TotalPrice
            }).ToList()
        };
    }
}
