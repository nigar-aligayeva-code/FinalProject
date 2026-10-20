using Microsoft.AspNetCore.SignalR;

namespace HotelTravel.API.Hubs;

// Canlı bildirişlər: /hubs/notifications
// Hər kəs qoşula bilər (saytdakı "Someone just booked..." mesajları üçün),
// Admin rolu olanlar əlavə olaraq "admins" qrupuna düşür.
public class NotificationHub : Hub
{
    public const string AdminsGroup = "admins";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);

        await base.OnConnectedAsync();
    }
}
