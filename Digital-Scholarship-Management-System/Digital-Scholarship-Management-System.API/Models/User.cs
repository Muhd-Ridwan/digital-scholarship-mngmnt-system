using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum UserRole
    {
        user,
        officer,
        sponsor,
        admin,
    }
    public enum UserStatus
    {
        Active,
        Locked,
    }
    // Whether a sponsor may post scholarships. Separate from UserStatus, which governs
    // sign-in — a rejected sponsor can still log in, they just cannot post.
    public enum SponsorApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
    }
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string CognitoSub { get; set; }
        // The Cognito username, needed separately from the sub: this pool has no sub-as-username
        // alias, so Admin API calls fail with UserNotFoundException if given the sub.
        public string? CognitoUsername { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        // Null for every role but sponsor — the concept does not apply to them.
        public SponsorApprovalStatus? SponsorStatus { get; set; }
        public DateTime? DecidedAt { get; set; }
        public string? DecidedBy { get; set; }
        public string? CompanyName { get; set; }
        public string? SsmNumber { get; set; }
        public string? CertificateS3Key { get; set; }
        public string? CertificateFileName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public UserStatus Status {  get; set; } = UserStatus.Active;

        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<Scholarship> Scholarships { get; set; } = new List<Scholarship>();
        public ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
        public StudentProfile? StudentProfile { get; set; }
    }
}
