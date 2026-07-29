using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dotnet10MvcApi.Data;
using Dotnet10MvcApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dotnet10MvcApi.Services
{
    /// <summary>
    /// Shared user account service used by all paradigms: MVC, Blazor Server, and Web API.
    /// Handles pure data operations: authenticate, create, change password, admin seeding, and claims building.
    /// Sign-in mechanics (cookie SignInAsync / JWT issue) remain in each paradigm's own controller/service.
    /// </summary>
    public class UserAccountService
    {
        private readonly ApplicationDbContext _db;
        private readonly DevUserService _devUserService;

        public UserAccountService(ApplicationDbContext db, DevUserService devUserService)
        {
            _db = db;
            _devUserService = devUserService;
        }

        // ─── Authenticate ────────────────────────────────────────────────────────

        /// <summary>
        /// Validates credentials against the database. Returns the UserAccount on success, null on failure.
        /// Also auto-seeds the default admin account on first call.
        /// Falls back to DevUsers (appsettings.Development.json) if not found in database.
        /// </summary>
        public async Task<UserAccount?> AuthenticateAsync(string? userName, string? password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            await EnsureAdminExistsAsync();

            var clean = userName.Trim().ToLower();
            UserAccount? user = null;
            try
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == clean);
            }
            catch { }

            if (user != null && user.IsActive && UserAccount.VerifyPasswordHash(password, user.PasswordSalt, user.PasswordHash))
            {
                user.LastLogin = DateTime.Now;
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return user;
            }

            // Fallback: DevUsers (appsettings.Development.json)
            var devUser = _devUserService.ValidateCredentials(clean, password);
            if (devUser != null)
            {
                return new UserAccount
                {
                    Id = Guid.Empty,
                    UserName = devUser.Username,
                    Roles = devUser.Role,
                    IsActive = true,
                    LastLogin = DateTime.Now
                };
            }

            return null;
        }

        /// <summary>
        /// Synchronous overload for Blazor Server (called from non-async service methods).
        /// </summary>
        public UserAccount? Authenticate(string? userName, string? password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            EnsureAdminExists();

            var clean = userName.Trim().ToLower();
            UserAccount? user = null;
            try
            {
                user = _db.Users.FirstOrDefault(u => u.UserName == clean);
            }
            catch { }

            if (user != null && user.IsActive && UserAccount.VerifyPasswordHash(password, user.PasswordSalt, user.PasswordHash))
            {
                user.LastLogin = DateTime.Now;
                _db.Entry(user).State = EntityState.Modified;
                _db.SaveChanges();
                return user;
            }

            // Fallback: DevUsers (appsettings.Development.json)
            var devUser = _devUserService.ValidateCredentials(clean, password);
            if (devUser != null)
            {
                return new UserAccount
                {
                    Id = Guid.Empty,
                    UserName = devUser.Username,
                    Roles = devUser.Role,
                    IsActive = true,
                    LastLogin = DateTime.Now
                };
            }

            return null;
        }

        // ─── Create ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new user account. Returns the new user's Id, or null if it already exists or input is invalid.
        /// Admin role is forbidden unless allowAdmin=true.
        /// </summary>
        public async Task<(Guid? Id, string? Error)> CreateUserAsync(
            string? userName, string? password, string roles = "", bool allowAdmin = false)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return (null, "Username and password are required.");

            userName = userName.Trim().ToLower();

            if (!Regex.IsMatch(userName, @"^[a-zA-Z0-9_.@]*$"))
                return (null, "Username contains invalid characters.");

            if (!allowAdmin)
            {
                foreach (var r in roles.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (r.Trim().Equals(UserAccount.DEFAULT_ADMIN_ROLENAME, StringComparison.OrdinalIgnoreCase))
                        return (null, "Creating an admin account is forbidden.");
                }
            }

            try
            {
                if (_devUserService.IsDevUser(userName))
                    return (null, "Account already exists.");

                var exists = await _db.Users.AnyAsync(x => x.UserName == userName);
                if (exists) return (null, "Account already exists.");

                UserAccount.CreatePasswordHash(password, out var hash, out var salt);

                var user = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    UserName = userName,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Roles = Regex.Replace(roles ?? string.Empty, @"\s+", ""),
                    CreatedOn = DateTime.Now,
                    IsActive = true
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                return (user.Id, null);
            }
            catch (Exception ex)
            {
                return (null, $"Database error: {ex.Message}");
            }
        }

        // ─── Change Password ─────────────────────────────────────────────────────

        /// <summary>
        /// Changes the password for a user after verifying the current password.
        /// If skipCurrentPasswordVerification=true (admin forced reset), verification is skipped.
        /// </summary>
        public async Task<bool> ChangePasswordAsync(
            string? userName, string? currentPassword, string? newPassword,
            bool skipCurrentPasswordVerification = false)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            var clean = userName.Trim().ToLower();

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == clean);
                if (user == null) return false;

                if (!skipCurrentPasswordVerification &&
                    !UserAccount.VerifyPasswordHash(currentPassword ?? "", user.PasswordSalt, user.PasswordHash))
                    return false;

                UserAccount.CreatePasswordHash(newPassword, out var hash, out var salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Synchronous overload for Blazor Server.</summary>
        public bool ChangePassword(string? userName, string? currentPassword, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            var clean = userName.Trim().ToLower();
            var user = _db.Users.SingleOrDefault(x => x.UserName == clean);
            if (user == null) return false;

            if (!UserAccount.VerifyPasswordHash(currentPassword ?? "", user.PasswordSalt, user.PasswordHash))
                return false;

            UserAccount.CreatePasswordHash(newPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            _db.Entry(user).State = EntityState.Modified;
            _db.SaveChanges();
            return true;
        }

        // ─── Get User ────────────────────────────────────────────────────────────

        public async Task<UserAccount?> GetByUserNameAsync(string? userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            var clean = userName.Trim().ToLower();
            try { return await _db.Users.FirstOrDefaultAsync(u => u.UserName == clean); }
            catch { return null; }
        }

        // ─── Claims Builder ──────────────────────────────────────────────────────

        /// <summary>
        /// Builds a claims list from a UserAccount, including Piranha admin claims if the role is admin.
        /// </summary>
        public static List<Claim> BuildClaims(UserAccount user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Roles)
            };
            AddPiranhaAdminClaims(claims, user.Roles);
            return claims;
        }

        /// <summary>Overload for DevUser (role string only).</summary>
        public static List<Claim> BuildClaims(string userName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role)
            };
            AddPiranhaAdminClaims(claims, role);
            return claims;
        }

        // ─── Admin Seeding ───────────────────────────────────────────────────────

        public async Task EnsureAdminExistsAsync()
        {
            try
            {
                var hasAdmin = await _db.Users.AnyAsync(x => x.Roles.Contains(UserAccount.DEFAULT_ADMIN_ROLENAME));
                if (!hasAdmin)
                {
                    UserAccount.CreatePasswordHash(UserAccount.DEFAULT_ADMIN_LOGIN, out var hash, out var salt);
                    _db.Users.Add(new UserAccount
                    {
                        Id = Guid.NewGuid(),
                        UserName = UserAccount.DEFAULT_ADMIN_LOGIN,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        Roles = UserAccount.DEFAULT_ADMIN_ROLENAME,
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    });
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnsureAdminExistsAsync bypassed/failed: {ex.Message}");
            }
        }

        public void EnsureAdminExists()
        {
            try
            {
                var hasAdmin = _db.Users.Any(x => x.Roles.Contains(UserAccount.DEFAULT_ADMIN_ROLENAME));
                if (!hasAdmin)
                {
                    UserAccount.CreatePasswordHash(UserAccount.DEFAULT_ADMIN_LOGIN, out var hash, out var salt);
                    _db.Users.Add(new UserAccount
                    {
                        Id = Guid.NewGuid(),
                        UserName = UserAccount.DEFAULT_ADMIN_LOGIN,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        Roles = UserAccount.DEFAULT_ADMIN_ROLENAME,
                        CreatedOn = DateTime.Now,
                        IsActive = true
                    });
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnsureAdminExists bypassed/failed: {ex.Message}");
            }
        }

        // ─── Private helpers ─────────────────────────────────────────────────────

        private static void AddPiranhaAdminClaims(List<Claim> claims, string role)
        {
            if (!string.IsNullOrWhiteSpace(role) && role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "admin"));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                try
                {
                    foreach (var permission in Piranha.Manager.Permission.All())
                        claims.Add(new Claim(permission, permission));
                }
                catch { }
            }
        }
    }
}
