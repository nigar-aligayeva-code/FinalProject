using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.DTOs.Rooms;

namespace HotelTravel.Web.ViewModels;

public class OccupancyCalendarViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public List<GetRoomDto> Rooms { get; set; }
        = new List<GetRoomDto>();

    public List<OccupancyCalendarDto> Bookings { get; set; }
        = new List<OccupancyCalendarDto>();
}