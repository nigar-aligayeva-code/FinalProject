
using HotelTravel.Application.DTOs.Hotels;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public HotelsController(IHotelService hotelService)
        {
            _hotelService = hotelService;
        }

        // GET: api/Hotels
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var hotels = await _hotelService.GetAllAsync();
            return Ok(hotels);
        }

        // GET: api/Hotels/1
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var hotel = await _hotelService.GetByIdAsync(id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found." });

            return Ok(hotel);
        }

        // POST: api/Hotels
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateHotelDto dto)
        {
            await _hotelService.CreateAsync(dto);

            return Ok(new
            {
                message = "Hotel created successfully."
            });
        }

        // PUT: api/Hotels/1
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id, [FromBody] UpdateHotelDto dto)
        {
            var updated = await _hotelService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound(new { message = "Hotel not found." });

            return Ok(new
            {
                message = "Hotel updated successfully."
            });
        }

        // DELETE: api/Hotels/1
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _hotelService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Hotel not found." });

            return Ok(new
            {
                message = "Hotel deleted successfully."
            });
        }
    }
}
