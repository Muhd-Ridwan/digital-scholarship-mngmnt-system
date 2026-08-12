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
        public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
        public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

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

            modelBuilder.Entity<Application>()
                .HasOne(a => a.DisbursedBy)
                .WithMany()
                .HasForeignKey(a => a.DisbursedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Application>()
                .Property(a => a.DisbursedAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Scholarship>()
                .HasOne(s => s.Sponsor)
                .WithMany(u => u.Scholarships)
                .HasForeignKey(s => s.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Scholarship>()
                .Property(s => s.FundingAmount)
                .HasPrecision(18, 2);

            // Store enums as strings
            modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
            modelBuilder.Entity<User>().Property(u => u.Status).HasConversion<string>();
            modelBuilder.Entity<User>().Property(u => u.SponsorStatus).HasConversion<string>();

            // Unique email
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Application>()
                .HasIndex(a => new { a.StudentId, a.ScholarshipId })
                .IsUnique();

            modelBuilder.Entity<ApplicationDocument>()
                .HasOne(ad => ad.Application)
                .WithMany(a => a.Documents)
                .HasForeignKey(ad => ad.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentDocument>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<StudentProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            modelBuilder.Entity<StudentProfile>()
                .Property(p => p.HouseholdIncome)
                .HasPrecision(18, 2);

        }
    }
}
