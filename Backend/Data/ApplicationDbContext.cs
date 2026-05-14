using HeimdallBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HeimdallBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Agent> Agent { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Agent>()
                .HasKey(at => new { at.UserId, at.AgentId });

            modelBuilder.Entity<Agent>()
                .HasOne(at => at.User)
                .WithMany()
                .HasForeignKey(at => at.UserId);
        }
    }
}