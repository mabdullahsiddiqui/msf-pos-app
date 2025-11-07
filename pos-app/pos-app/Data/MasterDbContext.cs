using Microsoft.EntityFrameworkCore;
using pos_app.Models;

namespace pos_app.Data
{
    public class MasterDbContext : DbContext
    {
        public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SuperAdmin> SuperAdmins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            // Note: Username is SQL database username, not application username
            // Multiple users can share the same SQL credentials
            // Email is used for application user identification and should remain unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure SuperAdmin entity
            modelBuilder.Entity<SuperAdmin>()
                .HasIndex(sa => sa.Username)
                .IsUnique();

            modelBuilder.Entity<SuperAdmin>()
                .HasIndex(sa => sa.Email)
                .IsUnique();

            // Note: No seeding - system will start fresh for MS SQL-only setup
        }
    }
}
