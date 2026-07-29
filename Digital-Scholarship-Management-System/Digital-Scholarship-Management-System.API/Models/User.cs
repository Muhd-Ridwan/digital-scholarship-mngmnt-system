using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum UserRole
    {
        user = 1, // start with 1, so role int = 1 is user
        officer,
        sponsor,
        admin,
    }
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string CognitoSub { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsApproved { get; set; }
        public string? CompanyName { get; set; }
        public string? SsmNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<Scholarship> Scholarships { get; set; } = new List<Scholarship>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public StudentProfile? StudentProfile { get; set; }
    }
}
