using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Home;
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>Rooms: catalogue, availability search, fully booked dates and admin management.</summary>
[Route("api/rooms")]
public class RoomsController : ApiControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    // GET: api/rooms
    /// <summary>List all rooms.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetRoomDto>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _roomService.GetAllAsync());

    // GET: api/rooms/5
    /// <summary>One room with images, amenities and rating.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetRoomDto), 200)]
    public async Task<IActionResult> GetById(int id)
        => OkOrNotFound(await _roomService.GetByIdAsync(id), "Room not found.");

    // GET: api/rooms/featured?count=8
    /// <summary>Top-rated rooms for the home page.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<FeaturedRoomDto>), 200)]
    public async Task<IActionResult> Featured([FromQuery] int count = 8)
        => Ok(await _roomService.GetFeaturedRoomsAsync(count));

    // POST: api/rooms/search  (boş otaqları axtar)
    /// <summary>Rooms that are free for the given city, dates and guests.</summary>
    [HttpPost("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GetRoomDto>), 200)]
    public async Task<IActionResult> Search([FromBody] RoomSearchDto search)
        => Ok(await _roomService.SearchAvailableRoomsAsync(search));

    // GET: api/rooms/fully-booked-dates?startDate=2026-11-01&endDate=2027-02-01
    /// <summary>Days when every room is booked (disabled in the date picker).</summary>
    [HttpGet("fully-booked-dates")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<DateTime>), 200)]
    public async Task<IActionResult> FullyBookedDates(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
        => Ok(await _roomService.GetFullyBookedDatesAsync(startDate, endDate));

    // POST: api/rooms
    /// <summary>Create a room (admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
    {
        await _roomService.CreateAsync(dto);
        return Ok(ApiResultDto.Ok("Room created."));
    }

    // PUT: api/rooms/5
    /// <summary>Update a room (admin).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        dto.Id = id;
        return FromBool(await _roomService.UpdateAsync(id, dto), "Room updated.", "Room not found.");
    }

    // DELETE: api/rooms/5
    /// <summary>Delete a room (soft delete). Not allowed while it has upcoming bookings (admin).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
        => FromBool(await _roomService.DeleteAsync(id), "Room deleted.", "Room not found.");

    // GET: api/rooms/5/upcoming-bookings
    /// <summary>Number of upcoming bookings of a room (admin).</summary>
    [HttpGet("{id:int}/upcoming-bookings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> UpcomingBookings(int id)
        => Ok(await _roomService.CountUpcomingBookingsAsync(id));

    // GET: api/rooms/images/12
    /// <summary>One gallery image of a room (admin).</summary>
    [HttpGet("images/{imageId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GetRoomImageDto), 200)]
    public async Task<IActionResult> GetImage(int imageId)
        => OkOrNotFound(await _roomService.GetImageByIdAsync(imageId), "Image not found.");

    // DELETE: api/rooms/images/12
    /// <summary>Delete a gallery image (admin).</summary>
    [HttpDelete("images/{imageId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteImage(int imageId)
        => FromBool(await _roomService.DeleteImageAsync(imageId), "Image deleted.", "Image not found.");

    // DELETE: api/rooms/5/main-image
    /// <summary>Remove the main image of a room (admin).</summary>
    [HttpDelete("{id:int}/main-image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMainImage(int id)
        => FromBool(await _roomService.DeleteMainImageAsync(id), "Main image deleted.", "Room not found.");
}
