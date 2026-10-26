using HotelTravel.API.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace HotelTravel.API.Hubs;

// Canlı bildirişlər: /hubs/notifications
// Hər kəs qoşula bilər (saytdakı "Someone just booked..." mesajları üçün),
// Admin rolu olanlar əlavə olaraq "admins" qrupuna düşür,
// daxil olmuş hər istifadəçi isə öz "user:{id}" qrupuna (rezervasiya təsdiqi, xatırlatmalar).
public class NotificationHub : Hub
{
    public const string AdminsGroup = "admins";

    public static string UserGroup(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);

        var userId = Context.User?.GetUserId();

        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }
}
