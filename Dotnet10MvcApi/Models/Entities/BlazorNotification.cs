using System;
using System.ComponentModel.DataAnnotations;

namespace Dotnet10MvcApi.Models.Entities
{
    public class BlazorNotification
    {
        public int Id { get; set; }

        [StringLength(255)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        public BlazorNotificationType NotificationType { get; set; } = BlazorNotificationType.Default;

        public string? NavigateToUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid? UserId { get; set; }

        public UserAccount? User { get; set; }
    }

    public enum BlazorNotificationType
    {
        Default = 0,
        Info = 4,
        Success = 5,
        Warning = 6,
        Error = 7
    }
}
