using System.ComponentModel.DataAnnotations;

namespace HotelTravel.Application.DTOs.Rooms;

public class RoomSearchDto
{
    public string? Destination { get; set; }

    [Required(ErrorMessage = "Please select a check-in date.")]
    public DateTime? CheckInDate { get; set; }

    [Required(ErrorMessage = "Please select a check-out date.")]
    public DateTime? CheckOutDate { get; set; }

    [Range(1, 20, ErrorMessage = "Please select at least 1 adult.")]
    public int AdultCount { get; set; } = 1;

    [Range(0, 20, ErrorMessage = "Children count is not valid.")]
    public int ChildrenCount { get; set; } = 0;
}