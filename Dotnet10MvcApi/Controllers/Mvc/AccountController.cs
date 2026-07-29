using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dotnet10MvcApi.Models.Entities;
using Dotnet10MvcApi.Services;

namespace Dotnet10MvcApi.Controllers.Mvc
{
    public class AccountController : Controller
    {
        private readonly UserAccountService _userAccountService;
        private readonly DevUserService _devUserService;

        public AccountController(UserAccountService userAccountService, DevUserService devUserService)
        {
            _userAccountService = userAccountService;
            _devUserService = devUserService;
        }

        [AllowAnonymous]
        [HttpGet("/Account/Login")]
        [HttpGet("/login")]
        public async Task<IActionResult> Login(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost("/Account/Login")]
        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberme = false, string returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["alert"] = "Username and password are required.";
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            var cleanUsername = username.Trim().ToLower();

            // 1. Primary: Database Authentication
            var user = await _userAccountService.AuthenticateAsync(cleanUsername, password);
            if (user != null)
            {
                var claims = UserAccountService.BuildClaims(user);
                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberme,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (cleanUsername == UserAccount.DEFAULT_ADMIN_LOGIN && password == UserAccount.DEFAULT_ADMIN_LOGIN)
                    return RedirectToAction("ChangePassword");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            // 2. Fallback: DevUsers (appsettings.Development.json)
            var devUser = _devUserService.ValidateCredentials(cleanUsername, password);
            if (devUser != null)
            {
                var claims = UserAccountService.BuildClaims(devUser.Username, devUser.Role);
                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberme,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            TempData["alert"] = "Invalid username or password";
            return RedirectToAction("Login", new { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet("/manager/logout")]
        [HttpGet("/manager/login/logout")]
        [HttpPost("/manager/login/logout")]
        [HttpGet("/Account/Logout")]
        [HttpPost("/Account/Logout")]
        [HttpGet("/Account/Logoff")]
        [HttpPost("/Account/Logoff")]
        [HttpGet("/logout")]
        [HttpPost("/logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logoff()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["alertbox"] = "New password cannot be empty.";
                return RedirectToAction("ChangePassword");
            }

            var userName = User.Identity?.Name;
            var changed = await _userAccountService.ChangePasswordAsync(userName, currentPassword, newPassword);

            if (changed)
            {
                TempData["alertbox"] = "Password changed successfully.";
                return RedirectToAction("Logoff");
            }

            TempData["alertbox"] = "Failed to change password. Verification failed.";
            return RedirectToAction("ChangePassword");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string role = "")
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["alertbox"] = "Username and password are required.";
                return RedirectToAction("Register");
            }

            var cleanRole = string.IsNullOrWhiteSpace(role) ? "user" : role.Trim().ToLower();

            var (id, error) = await _userAccountService.CreateUserAsync(username, password, cleanRole);
            if (id == null)
            {
                TempData["alertbox"] = error ?? "Registration failed.";
                return RedirectToAction("Register");
            }

            TempData["alert"] = $"Account successfully created. Welcome {username}!";
            return RedirectToAction("Login");
        }
    }
}
