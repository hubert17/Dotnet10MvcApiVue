using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppMvcOptions = Dotnet10MvcApi.Models.MvcOptions;
using Dotnet10MvcApi.Models;
using Dotnet10MvcApi.Models.Entities;
using Dotnet10MvcApi.Services;

namespace Dotnet10MvcApi.Controllers.Mvc
{
    public class AccountController : Controller
    {
        private readonly UserAccountService _userAccountService;
        private readonly DevUserService _devUserService;
        private readonly AppMvcOptions _mvcOptions;

        public AccountController(UserAccountService userAccountService, DevUserService devUserService, IOptions<AppMvcOptions> mvcOptions)
        {
            _userAccountService = userAccountService;
            _devUserService = devUserService;
            _mvcOptions = mvcOptions.Value;
        }

        [AllowAnonymous]
        [HttpGet("/Account/Login")]
        [HttpGet("/login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ViewBag.ReturnUrl = (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/") ? null : returnUrl;
            ViewBag.Username = TempData["Username"] as string ?? "";
            return View();
        }

        [AllowAnonymous]
        [HttpPost("/Account/Login")]
        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, bool rememberme = false, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["alert"] = "Username and password are required.";
                TempData["Username"] = username;
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            var cleanUsername = username.Trim().ToLower();
            string redirectTarget = (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/")
                ? returnUrl
                : _mvcOptions.HomePath;

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

                bool isAdmin = cleanUsername == UserAccount.DEFAULT_ADMIN_LOGIN ||
                               (user.Roles != null && System.Linq.Enumerable.Any(user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries), r => r.Trim().Equals(UserAccount.DEFAULT_ADMIN_ROLENAME, StringComparison.OrdinalIgnoreCase)));

                if (!isAdmin && user.MustChangePassword)
                    return RedirectToAction("ChangePassword", new { ReturnUrl = redirectTarget });

                if (cleanUsername == UserAccount.DEFAULT_ADMIN_LOGIN && password == UserAccount.DEFAULT_ADMIN_LOGIN)
                    return RedirectToAction("ChangePassword", new { ReturnUrl = redirectTarget });

                return Redirect(redirectTarget);
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

                return Redirect(redirectTarget);
            }

            TempData["alert"] = "Invalid username or password";
            TempData["Username"] = username;
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
        [HttpGet("/logout")]
        [HttpPost("/logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logoff(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (string.IsNullOrEmpty(returnUrl) && Request.Path.StartsWithSegments("/manager"))
            {
                returnUrl = "/manager";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return RedirectToAction("Login", "Account", new { ReturnUrl = returnUrl });
            }
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        public async Task<IActionResult> ChangePassword(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            var userName = User.Identity?.Name;
            var user = await _userAccountService.GetUserByUsernameAsync(userName);

            bool isAdmin = User.IsInRole(UserAccount.DEFAULT_ADMIN_ROLENAME) ||
                           User.IsInRole("Admin") ||
                           string.Equals(userName, UserAccount.DEFAULT_ADMIN_LOGIN, StringComparison.OrdinalIgnoreCase) ||
                           (user != null && user.Roles != null && System.Linq.Enumerable.Any(user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries), r => r.Trim().Equals(UserAccount.DEFAULT_ADMIN_ROLENAME, StringComparison.OrdinalIgnoreCase)));

            ViewBag.IsForcedChange = !isAdmin && user != null && user.MustChangePassword;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["alertbox"] = "New password cannot be empty.";
                return RedirectToAction("ChangePassword", new { ReturnUrl = returnUrl });
            }

            var userName = User.Identity?.Name;
            var changed = await _userAccountService.ChangePasswordAsync(userName, currentPassword, newPassword);

            if (changed)
            {
                TempData["alertbox"] = "Password changed successfully.";
                return RedirectToAction("Logoff", new { ReturnUrl = returnUrl });
            }

            TempData["alertbox"] = "Failed to change password. Verification failed.";
            return RedirectToAction("ChangePassword", new { ReturnUrl = returnUrl });
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

        // ─── User Management (Admin Only) ──────────────────────────────────────

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpGet("/Account/Users")]
        public IActionResult Users()
        {
            return View();
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpGet("/Account/Users/List")]
        public async Task<IActionResult> GetUsersList()
        {
            var users = await _userAccountService.GetAllUsersAsync();
            var list = users.Select(u => new
            {
                id = u.Id,
                userName = u.UserName,
                roles = u.Roles,
                isActive = u.IsActive,
                createdOn = u.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                lastLogin = u.LastLogin.HasValue ? u.LastLogin.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never"
            }).ToList();

            var availableRoles = new[] { UserAccount.DEFAULT_ADMIN_ROLENAME, "CmsEditor", "CmsWriter", "CmsModerator", "user" };

            return Json(new { success = true, users = list, availableRoles });
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromForm] string username, [FromForm] string password, [FromForm] string roles, [FromForm] bool isActive = true, [FromForm] bool mustChangePassword = false)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Username and password are required." });
            }

            if (username.Trim().Equals(UserAccount.DEFAULT_ADMIN_LOGIN, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = $"Username '{UserAccount.DEFAULT_ADMIN_LOGIN}' is reserved for the primary system administrator." });
            }

            var cleanRole = string.IsNullOrWhiteSpace(roles) ? "user" : roles.Trim().ToLower();
            var (id, error) = await _userAccountService.CreateUserAsync(username, password, cleanRole, allowAdmin: true, mustChangePassword: mustChangePassword);

            if (id == null)
            {
                return Json(new { success = false, message = error ?? "Failed to create user." });
            }

            return Json(new { success = true, message = $"User '{username}' created successfully." });
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/Import")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImportUsers(
            [FromBody] List<UserImportRowDto> importRows,
            [FromQuery] string roles = "",
            [FromQuery] bool mustChangePassword = true)
        {
            if (importRows == null || importRows.Count == 0)
            {
                return Json(new UserImportResultDto { Success = false, Message = "No valid import data provided." });
            }

            var result = await _userAccountService.BulkImportUsersAsync(importRows, roles, mustChangePassword);
            return Json(result);
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/UpdateRoles")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles([FromForm] Guid id, [FromForm] string roles)
        {
            var (success, error) = await _userAccountService.UpdateUserRolesAsync(id, roles);
            if (!success)
            {
                return Json(new { success = false, message = error ?? "Failed to update roles." });
            }

            return Json(new { success = true, message = "User roles updated successfully." });
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus([FromForm] Guid id)
        {
            var currentAdmin = User.Identity?.Name ?? "";
            var (success, isActive, error) = await _userAccountService.ToggleUserStatusAsync(id, currentAdmin);

            if (!success)
            {
                return Json(new { success = false, message = error ?? "Failed to toggle user status." });
            }

            var statusStr = isActive ? "activated" : "deactivated";
            return Json(new { success = true, isActive = isActive, message = $"User successfully {statusStr}." });
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword([FromForm] Guid id, [FromForm] string newPassword)
        {
            var (success, error) = await _userAccountService.AdminResetPasswordAsync(id, newPassword);
            if (!success)
            {
                return Json(new { success = false, message = error ?? "Failed to reset password." });
            }

            return Json(new { success = true, message = "Password reset successfully." });
        }

        [Authorize(Roles = UserAccount.DEFAULT_ADMIN_ROLENAME)]
        [HttpPost("/Account/Users/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromForm] Guid id)
        {
            var currentAdmin = User.Identity?.Name ?? "";
            var (success, error) = await _userAccountService.DeleteUserAsync(id, currentAdmin);

            if (!success)
            {
                return Json(new { success = false, message = error ?? "Failed to delete user." });
            }

            return Json(new { success = true, message = "User account deleted successfully." });
        }
    }
}
