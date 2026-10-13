
using HotelTravel.Application.DTOs.Bookings;
using HotelTravel.Application.Interfaces;
using HotelTravel.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _bookingService.GetAllAsync();
            return Ok(bookings);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _bookingService.GetByIdAsync(id);

            if (booking == null)
                return NotFound(new { message = "Booking not found." });

            return Ok(booking);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create(
            [FromBody] CreateBookingDto dto)
        {
            var result = await _bookingService.CreateAsync(dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                message = result.Message,
                confirmationCode = result.ConfirmationCode
            });
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateBookingStatusRequest request)
        {
            if (!Enum.IsDefined(typeof(BookingStatus), request.Status))
                return BadRequest(new { message = "Invalid booking status." });

            var updated = await _bookingService.UpdateStatusAsync(
                id, request.Status);

            if (!updated)
                return BadRequest(new
                {
                    message = "Booking not found or status transition is invalid."
                });

            return Ok(new { message = "Booking status updated successfully." });
        }

        [HttpGet("occupancy-calendar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOccupancyCalendar()
        {
            var calendar = await _bookingService.GetOccupancyCalendarAsync();
            return Ok(calendar);
        }

        [HttpPost("cancel")]
        [AllowAnonymous]
        public async Task<IActionResult> CancelBooking(
            [FromBody] CancelBookingDto dto)
        {
            var result = await _bookingService.CancelByGuestAsync(
                dto.ConfirmationCode,
                dto.Email
            );

            if (!result.Success)
            {
                return BadRequest(new
                {
                    message = result.Message
                });
            }

            return Ok(new
            {
                message = result.Message
            });
        }

    }

    public class UpdateBookingStatusRequest
    {
        public BookingStatus Status { get; set; }
    }

}
