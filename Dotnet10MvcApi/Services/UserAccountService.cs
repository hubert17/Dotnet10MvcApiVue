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
                user.LastLogin = DateTime.UtcNow;
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
                    LastLogin = DateTime.UtcNow
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
                user.LastLogin = DateTime.UtcNow;
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
                    LastLogin = DateTime.UtcNow
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

        // ─── User Management (Admin) ─────────────────────────────────────────────

        public async Task<List<UserAccount>> GetAllUsersAsync()
        {
            await EnsureDefaultUsersExistAsync();
            try
            {
                return await _db.Users
                    .AsNoTracking()
                    .OrderByDescending(u => u.CreatedOn)
                    .ToListAsync();
            }
            catch
            {
                return new List<UserAccount>();
            }
        }

        public async Task<UserAccount?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _db.Users.FindAsync(id);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string? Error)> UpdateUserRolesAsync(Guid userId, string roles)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found.");

                var cleanRoles = Regex.Replace(roles ?? string.Empty, @"\s+", "");

                // Secondary Server Validation: Primary seeded admin account role protection
                if (user.UserName.Equals(UserAccount.DEFAULT_ADMIN_LOGIN, StringComparison.OrdinalIgnoreCase))
                {
                    var hasAdminRole = cleanRoles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Any(r => r.Equals(UserAccount.DEFAULT_ADMIN_ROLENAME, StringComparison.OrdinalIgnoreCase));

                    if (!hasAdminRole)
                    {
                        return (false, $"The primary '{UserAccount.DEFAULT_ADMIN_LOGIN}' account is protected and must retain the '{UserAccount.DEFAULT_ADMIN_ROLENAME}' role.");
                    }
                }

                user.Roles = cleanRoles;
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to update roles: {ex.Message}");
            }
        }

        public async Task<(bool Success, bool IsActive, string? Error)> ToggleUserStatusAsync(Guid userId, string currentAdminUserName)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, false, "User not found.");

                // Secondary Server Validation: Primary seeded admin status protection
                if (user.UserName.Equals(UserAccount.DEFAULT_ADMIN_LOGIN, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, user.IsActive, $"The primary '{UserAccount.DEFAULT_ADMIN_LOGIN}' account is permanent and cannot be deactivated.");
                }

                if (user.UserName.Equals(currentAdminUserName?.Trim(), StringComparison.OrdinalIgnoreCase) && user.IsActive)
                {
                    return (false, user.IsActive, "You cannot deactivate your own active admin account.");
                }

                user.IsActive = !user.IsActive;
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return (true, user.IsActive, null);
            }
            catch (Exception ex)
            {
                return (false, false, $"Failed to toggle status: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> AdminResetPasswordAsync(Guid userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return (false, "New password cannot be empty.");

            if (newPassword.Trim().Length < 4)
                return (false, "Password must be at least 4 characters long.");

            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found.");

                UserAccount.CreatePasswordHash(newPassword, out var hash, out var salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;

                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to reset password: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> DeleteUserAsync(Guid userId, string currentAdminUserName)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found.");

                // Secondary Server Validation: Primary seeded admin deletion protection
                if (user.UserName.Equals(UserAccount.DEFAULT_ADMIN_LOGIN, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"The primary '{UserAccount.DEFAULT_ADMIN_LOGIN}' account is system-critical and cannot be deleted.");
                }

                if (user.UserName.Equals(currentAdminUserName?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "You cannot delete your currently logged-in account.");
                }

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to delete user: {ex.Message}");
            }
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
            AddPiranhaRoleClaims(claims, user.Roles);
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
            AddPiranhaRoleClaims(claims, role);
            return claims;
        }

        // ─── Default Users Seeding ───────────────────────────────────────────────

        public async Task EnsureDefaultUsersExistAsync()
        {
            try
            {
                var seedUsers = new[]
                {
                    (Username: UserAccount.DEFAULT_ADMIN_LOGIN, Password: UserAccount.DEFAULT_ADMIN_LOGIN, Role: UserAccount.DEFAULT_ADMIN_ROLENAME)
                };

                bool addedAny = false;
                foreach (var su in seedUsers)
                {
                    var exists = await _db.Users.AnyAsync(u => u.UserName == su.Username);
                    if (!exists)
                    {
                        UserAccount.CreatePasswordHash(su.Password, out var hash, out var salt);
                        _db.Users.Add(new UserAccount
                        {
                            Id = Guid.NewGuid(),
                            UserName = su.Username,
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            Roles = su.Role,
                            CreatedOn = DateTime.UtcNow,
                            IsActive = true
                        });
                        addedAny = true;
                    }
                }

                if (addedAny)
                {
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnsureDefaultUsersExistAsync bypassed/failed: {ex.Message}");
            }
        }

        public void EnsureDefaultUsersExist()
        {
            try
            {
                var seedUsers = new[]
                {
                    (Username: UserAccount.DEFAULT_ADMIN_LOGIN, Password: UserAccount.DEFAULT_ADMIN_LOGIN, Role: UserAccount.DEFAULT_ADMIN_ROLENAME)
                };

                bool addedAny = false;
                foreach (var su in seedUsers)
                {
                    var exists = _db.Users.Any(u => u.UserName == su.Username);
                    if (!exists)
                    {
                        UserAccount.CreatePasswordHash(su.Password, out var hash, out var salt);
                        _db.Users.Add(new UserAccount
                        {
                            Id = Guid.NewGuid(),
                            UserName = su.Username,
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            Roles = su.Role,
                            CreatedOn = DateTime.UtcNow,
                            IsActive = true
                        });
                        addedAny = true;
                    }
                }

                if (addedAny)
                {
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnsureDefaultUsersExist bypassed/failed: {ex.Message}");
            }
        }

        public Task EnsureAdminExistsAsync() => EnsureDefaultUsersExistAsync();
        public void EnsureAdminExists() => EnsureDefaultUsersExist();

        // ─── Private helpers ─────────────────────────────────────────────────────

        private static void AddPiranhaRoleClaims(List<Claim> claims, string rolesString)
        {
            if (string.IsNullOrWhiteSpace(rolesString)) return;

            var roles = rolesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(r => r.Trim().ToLower())
                                   .ToHashSet();

            void AddPerms(params string[] perms)
            {
                foreach (var p in perms)
                {
                    if (!claims.Any(c => c.Type == p))
                        claims.Add(new Claim(p, p));
                }
            }

            // Admin role -> All Piranha Manager permissions + System Admin claim
            if (roles.Contains(UserAccount.DEFAULT_ADMIN_ROLENAME.ToLower()))
            {
                claims.Add(new Claim(ClaimTypes.Role, UserAccount.DEFAULT_ADMIN_ROLENAME));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                try
                {
                    foreach (var permission in Piranha.Manager.Permission.All())
                        AddPerms(permission);
                }
                catch { }
            }

            // CmsEditor role -> Full page, post, media, comment, & alias management
            if (roles.Contains("cmseditor") || roles.Contains("editor"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "CmsEditor"));
                claims.Add(new Claim(ClaimTypes.Role, "editor"));
                claims.Add(new Claim(ClaimTypes.Role, "Editor"));
                AddPerms(
                    Piranha.Manager.Permission.Admin,
                    Piranha.Manager.Permission.Pages, Piranha.Manager.Permission.PagesAdd, Piranha.Manager.Permission.PagesEdit, Piranha.Manager.Permission.PagesSave, Piranha.Manager.Permission.PagesPublish, Piranha.Manager.Permission.PagesDelete,
                    Piranha.Manager.Permission.Posts, Piranha.Manager.Permission.PostsAdd, Piranha.Manager.Permission.PostsEdit, Piranha.Manager.Permission.PostsSave, Piranha.Manager.Permission.PostsPublish, Piranha.Manager.Permission.PostsDelete,
                    Piranha.Manager.Permission.Media, Piranha.Manager.Permission.MediaAdd, Piranha.Manager.Permission.MediaEdit, Piranha.Manager.Permission.MediaDelete,
                    Piranha.Manager.Permission.Comments, Piranha.Manager.Permission.CommentsApprove, Piranha.Manager.Permission.CommentsDelete,
                    Piranha.Manager.Permission.Aliases
                );
            }

            // CmsWriter role -> Post & Media content creation, drafting, and self-publishing
            if (roles.Contains("cmswriter") || roles.Contains("writer") || roles.Contains("author"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "CmsWriter"));
                claims.Add(new Claim(ClaimTypes.Role, "writer"));
                claims.Add(new Claim(ClaimTypes.Role, "Writer"));
                AddPerms(
                    Piranha.Manager.Permission.Admin,
                    Piranha.Manager.Permission.Pages, Piranha.Manager.Permission.PagesEdit, Piranha.Manager.Permission.PagesSave,
                    Piranha.Manager.Permission.Posts, Piranha.Manager.Permission.PostsAdd, Piranha.Manager.Permission.PostsEdit, Piranha.Manager.Permission.PostsSave, Piranha.Manager.Permission.PostsPublish,
                    Piranha.Manager.Permission.Media, Piranha.Manager.Permission.MediaAdd, Piranha.Manager.Permission.MediaEdit
                );
            }

            // CmsModerator role -> Comment moderation & content review
            if (roles.Contains("cmsmoderator") || roles.Contains("moderator") || roles.Contains("comment moderator"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "CmsModerator"));
                claims.Add(new Claim(ClaimTypes.Role, "moderator"));
                claims.Add(new Claim(ClaimTypes.Role, "Moderator"));
                claims.Add(new Claim(ClaimTypes.Role, "comment moderator"));
                claims.Add(new Claim(ClaimTypes.Role, "Comment Moderator"));
                AddPerms(
                    Piranha.Manager.Permission.Admin,
                    Piranha.Manager.Permission.Pages, Piranha.Manager.Permission.PagesEdit, Piranha.Manager.Permission.PagesSave,
                    Piranha.Manager.Permission.Comments, Piranha.Manager.Permission.CommentsApprove, Piranha.Manager.Permission.CommentsDelete,
                    Piranha.Manager.Permission.Posts, Piranha.Manager.Permission.Media
                );
            }

            // User role -> Standard non-manager user
            if (roles.Contains("user"))
            {
                claims.Add(new Claim(ClaimTypes.Role, "user"));
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            }
        }
    }
}
