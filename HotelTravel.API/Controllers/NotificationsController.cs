using HotelTravel.Application.DTOs.Common;
using HotelTravel.Application.DTOs.Notifications;
using HotelTravel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelTravel.API.Controllers;

/// <summary>The signed-in user's notifications (🔔): booking confirmed / rejected / cancelled, stay and flight reminders.</summary>
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly IUserNotificationService _notifications;

    public NotificationsController(IUserNotificationService notifications)
    {
        _notifications = notifications;
    }

    // GET: api/notifications/mine?take=20
    /// <summary>Latest notifications of the signed-in user, newest first.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<GuestNotificationDto>), 200)]
    public async Task<IActionResult> Mine([FromQuery] int take = 20)
        => Ok(await _notifications.GetMineAsync(CurrentUserId!, take));

    // GET: api/notifications/unread-count
    /// <summary>How many notifications are not read yet (badge on the bell).</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> UnreadCount()
        => Ok(await _notifications.CountUnreadAsync(CurrentUserId!));

    // POST: api/notifications/read-all
    /// <summary>Mark all notifications as read (when the bell is opened).</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResultDto), 200)]
    public async Task<IActionResult> ReadAll()
    {
        await _notifications.MarkAllReadAsync(CurrentUserId!);
        return Ok(ApiResultDto.Ok("Notifications marked as read."));
    }
}
