namespace Digital_Scholarship_Management_System.API.Models
{
    public class Interview
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public DateTime InterviewDate { get; set; }

        public string InterviewTime { get; set; } = string.Empty;
        public string InterviewMethod { get; set; } = string.Empty;
        // e.g. "Online", "In Person"

        public string? Location { get; set; }

        public string? MeetingLink { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
