using Dotnet10MvcApi.Models.Entities;

namespace Dotnet10MvcApi.Services.Blazor
{
    /// <summary>
    /// Blazor Server user service — thin wrapper around the shared UserAccountService.
    /// Uses IDbContextFactory because Blazor Server components are long-lived circuits
    /// and cannot share the scoped ApplicationDbContext injected into controllers.
    /// </summary>
    public class BlazorUserService
    {
        private readonly UserAccountService _userAccountService;

        public BlazorUserService(UserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        public UserAccount? Authenticate(string? userName, string? password)
            => _userAccountService.Authenticate(userName, password);

        public Guid? Create(string? userName, string? password, string roles = "")
        {
            var (id, _) = _userAccountService.CreateUserAsync(userName, password, roles).GetAwaiter().GetResult();
            return id;
        }

        public bool ChangePassword(string? userName, string? currentPassword, string? newPassword)
            => _userAccountService.ChangePassword(userName, currentPassword, newPassword);
    }
}
