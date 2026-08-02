using Microsoft.AspNetCore.SignalR;

namespace Dotnet10MvcApi.Services.Notifications
{
    public class ChatMessageDto
    {
        public string Sender { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Shared SignalR Chat Hub for real-time messaging across MVC, Vue SPA, and Blazor Server.
    /// </summary>
    public class ChatHub : Hub
    {
        public async Task SendMessage(string sender, string recipient, string message)
        {
            if (string.IsNullOrWhiteSpace(sender)) sender = "Anonymous";
            if (string.IsNullOrWhiteSpace(message)) return;

            var dto = new ChatMessageDto
            {
                Sender = sender.Trim(),
                Recipient = string.IsNullOrWhiteSpace(recipient) ? "Everyone" : recipient.Trim(),
                Message = message.Trim(),
                Timestamp = DateTime.UtcNow
            };

            // Broadcast message live to all connected clients across all paradigms
            await Clients.All.SendAsync("ReceiveChatMessage", dto);
        }
    }
}
