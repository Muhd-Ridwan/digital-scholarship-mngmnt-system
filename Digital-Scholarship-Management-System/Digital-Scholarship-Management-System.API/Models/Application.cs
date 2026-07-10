using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum ApplicationStatus
    {
        Pending,
        UnderReview,
        Approved,
        Rejected
    }
    public class Application
    {
        [Key]
        public int Id { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; } = null;
        public int ScholarshipId { get; set; }
        public Scholarship Scholarship { get; set; } = null!;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public string? ReviewNotes { get; set; }
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedBy {  get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionAt { get; set; }
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
