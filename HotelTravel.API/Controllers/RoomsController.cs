
using HotelTravel.Application.DTOs.Rooms;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // GET: api/Rooms
        // Public
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _roomService.GetAllAsync();
            return Ok(rooms);
        }

        // GET: api/Rooms/1
        // JWT tələb olunur
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _roomService.GetByIdAsync(id);

            if (room == null)
            {
                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            return Ok(room);
        }

        // POST: api/Rooms
        // Yalnız Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateRoomDto dto)
        {
            await _roomService.CreateAsync(dto);

            return Ok(new
            {
                message = "Room created successfully."
            });
        }

        // PUT: api/Rooms/37
        // Yalnız Admin
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateRoomDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    message = "Room ID mismatch."
                });
            }

            var updated = await _roomService.UpdateAsync(id, dto);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            return Ok(new
            {
                message = "Room updated successfully."
            });
        }

        // DELETE: api/Rooms/37
        // Yalnız Admin
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roomService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Room not found."
                });
            }

            return Ok(new
            {
                message = "Room deleted successfully."
            });
        }
    }
}
