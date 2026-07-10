using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public class Scholarship
    {
        [Key]
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public User Sponsor { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EligibilityCriteria {  get; set; } = string.Empty;
        public string? University {  get; set; }
        public decimal FundingAmount { get; set; }
        public DateTime Deadline {  get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
