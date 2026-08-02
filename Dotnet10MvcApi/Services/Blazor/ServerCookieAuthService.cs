using Dotnet10MvcApi.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Dotnet10MvcApi.Services.Blazor
{
    /// <summary>
    /// Cookie auth service for Blazor Server — shares the same cookie scheme as MVC.
    /// Login via this service is recognized by MVC controllers and vice versa.
    /// </summary>
    public class ServerCookieAuthService
    {
        private readonly IHttpContextAccessor _http;
        private readonly BlazorUserService _users;
        private ClaimsPrincipal _user = new(new ClaimsIdentity());

        public ServerCookieAuthService(IHttpContextAccessor http, BlazorUserService users)
        {
            _http = http;
            _users = users;

            if (_http.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _user = _http.HttpContext.User;
            }
        }

        public ClaimsPrincipal CurrentUser => _user.Identity?.IsAuthenticated == true ? _user : (_http.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity()));

        public async Task<bool> SignInAsync(string username, string password, string? returnUrl = null)
        {
            var (success, _) = await SignInUserAsync(username, password, returnUrl);
            return success;
        }

        public async Task<(bool Success, UserAccount? User)> SignInUserAsync(string username, string password, string? returnUrl = null)
        {
            var u = await _users.AuthenticateAsync(username, password);
            if (u is null) return (false, null);

            var claims = UserAccountService.BuildClaims(u);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            _user = new ClaimsPrincipal(identity);

            if (_http.HttpContext != null && !_http.HttpContext.Response.HasStarted)
            {
                await _http.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, _user);
            }
            return (true, u);
        }

        public async Task SignOutAsync()
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity());
            if (_http.HttpContext != null && !_http.HttpContext.Response.HasStarted)
            {
                await _http.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_http.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _user = _http.HttpContext.User;
            }
            return Task.FromResult(new AuthenticationState(_user));
        }
    }
}
