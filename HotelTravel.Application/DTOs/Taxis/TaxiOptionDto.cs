using HotelTravel.Domain.Enums;

namespace HotelTravel.Application.DTOs.Taxis;

// Axtarış nəticəsində bir maşın sinfi
public class TaxiOptionDto
{
    public TaxiVehicleType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;

    public int MaxPassengers { get; set; }
    public int MaxLuggage { get; set; }

    public decimal Price { get; set; }

    // Seçilmiş sərnişin və baqaj sayına uyğundur?
    public bool Fits { get; set; }
}
