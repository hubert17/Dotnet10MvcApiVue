using Dotnet10MvcApi.Data;
using Dotnet10MvcApi.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Dotnet10MvcApi.Services.Notifications
{
    /// <summary>
    /// Application notification service — uses ApplicationDbContext + shared SignalR NotificationHub.
    /// </summary>
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _db;

        public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext db)
        {
            _hubContext = hubContext;
            _db = db;
        }

        public async Task PushNotificationAsync(BlazorNotification notification)
        {
            notification.CreatedOn = DateTime.Now;
            _db.BlazorNotifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }

        public async Task<List<BlazorNotification>> GetUserNotificationsAsync(Guid excludeSenderUserId)
        {
            return await _db.BlazorNotifications
                .Where(n => n.UserId != excludeSenderUserId)
                .OrderByDescending(n => n.CreatedOn)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
