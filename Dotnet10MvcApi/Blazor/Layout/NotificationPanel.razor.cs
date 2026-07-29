using Dotnet10MvcApi.Models.Entities;
using Dotnet10MvcApi.Services.Blazor;
using Dotnet10MvcApi.Services.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace Dotnet10MvcApi.Blazor.Layout
{
    public partial class NotificationPanel : IAsyncDisposable
    {
        [Parameter] public bool IncludeSample { get; set; } = false;

        [CascadingParameter] public Task<AuthenticationState>? AuthState { get; set; }

        [Inject] private NavigationManager NavManager { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;

        private Guid? _userId;
        private List<BlazorNotification> _notifications = new();
        private HubConnection? _hubConnection;
        private bool _isConnected = false;
        private bool _hasNewNotification = false;
        private bool _isMobile = true;
        private bool _isAllDialogOpen = false;

        private readonly DialogOptions _dialogOptions = new() { FullScreen = true, CloseOnEscapeKey = true };

        private void OpenAllDialog() => _isAllDialogOpen = true;
        private void CloseAllDialog() => _isAllDialogOpen = false;
        private void OnBreakpointChanged(MudBlazor.Breakpoint bp) => _isMobile = bp <= MudBlazor.Breakpoint.Sm;

        protected override async Task OnInitializedAsync()
        {
            if (AuthState is not null)
            {
                var state = await AuthState;
                var userIdClaim = state.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    _userId = Guid.Parse(userIdClaim);

                    var dbNotifications = await NotificationService.GetUserNotificationsAsync(_userId.Value);
                    _notifications = new List<BlazorNotification>(dbNotifications);

                    if (IncludeSample)
                        _notifications.AddRange(GetSampleNotifications());

                    await InitializeSignalRAsync();
                }
            }
        }

        private async Task InitializeSignalRAsync()
        {
            var hubUrl = new Uri(new Uri(NavManager.BaseUri), "/notificationhub").ToString();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<BlazorNotification>("ReceiveNotification", async (notification) =>
            {
                if (notification.UserId != _userId)
                {
                    await InvokeAsync(() =>
                    {
                        _notifications.Insert(0, notification);
                        _hasNewNotification = true;
                        StateHasChanged();
                    });
                }
            });

            const int maxRetries = 5;
            int retryCount = 0;

            while (!_isConnected && retryCount < maxRetries)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    _isConnected = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"SignalR connection failed: {ex.Message}. Retrying {retryCount}/{maxRetries}...");
                    await Task.Delay(1000);
                }
            }
        }

        private void NavigateTo(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                NavManager.NavigateTo(url);
        }

        private MudBlazor.Color GetColor(BlazorNotificationType type) => type switch
        {
            BlazorNotificationType.Info => MudBlazor.Color.Info,
            BlazorNotificationType.Warning => MudBlazor.Color.Warning,
            BlazorNotificationType.Error => MudBlazor.Color.Error,
            BlazorNotificationType.Success => MudBlazor.Color.Success,
            _ => MudBlazor.Color.Default
        };

        private string GetHumanizedTime(DateTime createdOn)
        {
            var ts = DateTime.Now - createdOn;
            if (ts.TotalSeconds < 60) return "Just now";
            if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
            if (ts.TotalHours < 24) return $"{(int)ts.TotalHours}h ago";
            if (ts.TotalDays < 7) return $"{(int)ts.TotalDays}d ago";
            if (ts.TotalDays < 30) return $"{(int)(ts.TotalDays / 7)}w ago";
            if (ts.TotalDays < 365) return $"{(int)(ts.TotalDays / 30)}mo ago";
            return $"{(int)(ts.TotalDays / 365)}y ago";
        }

        private List<BlazorNotification> GetSampleNotifications()
        {
            return new List<BlazorNotification>
            {
                new() { Id = -1, Title = "New Message", Content = "You received a new message 2 minutes ago.", CreatedOn = DateTime.Now.AddMinutes(-2), NotificationType = BlazorNotificationType.Info },
                new() { Id = -2, Title = "Activity Log", Content = "You logged in from a different browser about an hour ago.", CreatedOn = DateTime.Now.AddHours(-1), NotificationType = BlazorNotificationType.Warning },
                new() { Id = -3, Title = "Daily Report", Content = "Your daily report is now available.", CreatedOn = DateTime.Now.AddDays(-1), NotificationType = BlazorNotificationType.Success },
                new() { Id = -4, Title = "Weekly Roundup", Content = "Here's what you missed this week.", CreatedOn = DateTime.Now.AddDays(-7), NotificationType = BlazorNotificationType.Default },
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
