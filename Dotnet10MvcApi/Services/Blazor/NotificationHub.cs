using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Dotnet10MvcApi.Services.Blazor
{
    /// <summary>
    /// SignalR hub — shared by MVC, Blazor Server, and API paradigms.
    /// Mapped at /notificationhub in Program.cs.
    /// </summary>
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

            await base.OnDisconnectedAsync(exception);
        }

        private string? GetUserId()
            => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
