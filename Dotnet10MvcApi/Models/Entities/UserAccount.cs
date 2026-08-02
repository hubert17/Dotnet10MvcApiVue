using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace Dotnet10MvcApi.Models.Entities
{
    [Table("Users")]
    public class UserAccount
    {
        public const string DEFAULT_ADMIN_LOGIN = "admin"; // use as default login password
        public const string DEFAULT_ADMIN_ROLENAME = "admin";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreatedOn { get; set; }

        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; }

        public bool MustChangePassword { get; set; } = false;

        public string Roles { get; set; } = string.Empty; // Comma-separated roles

        // Hashing and verification helpers to keep DB compatibility
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public static bool VerifyPasswordHash(string password, byte[] passwordSalt, byte[] passwordHash)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
            }
        }
    }
}
