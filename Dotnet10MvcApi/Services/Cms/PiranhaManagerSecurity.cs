using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Piranha.Manager.LocalAuth;

namespace Dotnet10MvcApi.Services.Cms
{
    /// <summary>
    /// Bridges the Piranha Manager LocalAuth security interface to our existing
    /// ASP.NET Core cookie authentication. This enables the Piranha Manager's
    /// save/publish/delete operations to authenticate against our custom login system.
    /// </summary>
    public class PiranhaManagerSecurity : ISecurity
    {
        private readonly IConfiguration _config;

        public PiranhaManagerSecurity(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Called by the Piranha Manager login page. We validate credentials against
        /// our own system (appsettings DevUsers or DB) and sign in via our cookie scheme.
        /// </summary>
        public async Task<LoginResult> SignIn(object context, string username, string password)
        {
            if (context is not HttpContext httpContext)
                return LoginResult.Failed;

            // Check against DevUsers in appsettings (covers the admin user)
            var devUsers = _config.GetSection("DevUsers").GetChildren();
            foreach (var user in devUsers)
            {
                var cfgUsername = user["Username"]?.Trim().ToLower();
                var cfgPassword = user["Password"];
                var cfgRole = user["Role"] ?? Dotnet10MvcApi.Models.Entities.UserAccount.DEFAULT_ADMIN_ROLENAME;

                if (cfgUsername == username.Trim().ToLower() && cfgPassword == password)
                {
                    var claims = UserAccountService.BuildClaims(cfgUsername, cfgRole);

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await httpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties { IsPersistent = false });

                    return LoginResult.Succeeded;
                }
            }

            return LoginResult.Failed;
        }

        /// <summary>
        /// Signs out by clearing our cookie auth session.
        /// </summary>
        public async Task SignOut(object context)
        {
            if (context is HttpContext httpContext)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
