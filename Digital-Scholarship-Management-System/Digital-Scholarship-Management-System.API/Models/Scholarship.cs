using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum ScholarshipStatus { 
        Draft,
        Active,
        Closed
    }
    public class Scholarship
    {
        [Key]
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public User Sponsor { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EligibilityCriteria { get; set; } = string.Empty;
        public string FundType { get; set; } = string.Empty;
        public string StudyLocation { get; set; } = string.Empty;
        public string OrganisationType { get; set; } = string.Empty;
        public decimal FundingAmount { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ScholarshipStatus Status { get; set; } = ScholarshipStatus.Draft;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
