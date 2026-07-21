using Microsoft.EntityFrameworkCore;
using Digital_Scholarship_Management_System.API.Models;

namespace Digital_Scholarship_Management_System.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Scholarship> Scholarships => Set<Scholarship>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<Document> Documents => Set<Document>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Model Builder = Tool for telling EF Core "here's exactly how this relationship/constraint
            // should work"
            modelBuilder.Entity<Application>()
                .HasOne(a => a.Student)
                .WithMany(u => u.Applications)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Application>()
                .HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Scholarship>()
                .HasOne(s => s.Sponsor)
                .WithMany(u => u.Scholarships)
                .HasForeignKey(s => s.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Scholarship>()
                .Property(s => s.FundingAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

        }
    }
}
