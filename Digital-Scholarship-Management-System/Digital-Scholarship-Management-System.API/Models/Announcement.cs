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
        public int Id { get; set;  }
        public string Title { get; set;  } = "";
        public string Body {  get; set; } = "";
        public AnnouncementAudience Audience {  get; set; }
        public AnnouncementStatus Status { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
