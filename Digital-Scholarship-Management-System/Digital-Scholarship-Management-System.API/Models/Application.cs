using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum ApplicationStatus
    {
        Pending,  //0
        UnderReview,//1
        Shortlisted,//2
        Approved,//3
        Rejected///4
    }

    public enum DisbursementStatus { 
        NotDisbursed, 
        Disbursed 
    }
    public class Application
    {
        [Key]
        public int Id { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; } = null!;
        public int ScholarshipId { get; set; }
        public Scholarship Scholarship { get; set; } = null!;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public string? ReviewNotes { get; set; }
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedBy { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionAt { get; set; }
        public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
        public DisbursementStatus DisbursementStatus { get; set; } = DisbursementStatus.NotDisbursed;
        public decimal? DisbursedAmount { get; set; }
        public DateTime? DisbursedAt { get; set; }
        public int? DisbursedByUserId { get; set; }
        public User? DisbursedBy { get; set;  }

        public Interview? Interview { get; set; }
    }
}
