using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum AnnouncementAudience
    {
        All,
        Student,
        Officer,
        Sponsor,
    }

    public enum AnnouncementStatus
    {
        Draft,
        Published,
        Archived,
    }

    public class Announcement
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public AnnouncementAudience Audience { get; set; }
        public AnnouncementStatus Status { get; set; }
        // Stamped at first publish and kept through archiving — the evidence of when a notice went out.
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        public ICollection<AnnouncementRead> Reads { get; set; } = new List<AnnouncementRead>();
    }

    // One row per (user, announcement); its existence means read. Re-marking is guarded by the
    // unique index rather than an overwrite, which is how the DynamoDB version stayed idempotent.
    public class AnnouncementRead
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int AnnouncementId { get; set; }
        public Announcement Announcement { get; set; } = null!;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
