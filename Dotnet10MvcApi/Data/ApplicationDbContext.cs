using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Dotnet10MvcApi.Models.Entities;

namespace Dotnet10MvcApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IConfiguration? _configuration;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration? configuration = null)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<UserAccount> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Song> Songs { get; set; }

        public DbSet<BlazorNotification> BlazorNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply default schema if configured (e.g. for PostgreSQL)
            var schema = _configuration?["DatabaseSchema"];
            if (!string.IsNullOrWhiteSpace(schema))
            {
                modelBuilder.HasDefaultSchema(schema);
            }

            // Configure mapping indexes or constraints if needed
            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.Token);

            modelBuilder.Entity<BlazorNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
