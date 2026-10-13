
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Rooms;

namespace HotelTravel.Web.Areas.Admin.Models;

public class OccupancyCalendarViewModel
{
    public List<OccupancyCalendarDto> Bookings { get; set; } = new();

    public List<GetRoomDto> Rooms { get; set; } = new();
}
