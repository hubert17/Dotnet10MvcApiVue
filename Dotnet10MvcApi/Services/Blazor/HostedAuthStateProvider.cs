using Microsoft.AspNetCore.Components.Authorization;

namespace Dotnet10MvcApi.Services.Blazor
{
    /// <summary>
    /// Blazor AuthenticationStateProvider that delegates to ServerCookieAuthService (concrete, no interface).
    /// </summary>
    public class HostedAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ServerCookieAuthService _auth;

        public HostedAuthStateProvider(ServerCookieAuthService auth)
        {
            _auth = auth;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => _auth.GetAuthenticationStateAsync();

        public void Notify()
            => NotifyAuthenticationStateChanged(_auth.GetAuthenticationStateAsync());
    }
}
