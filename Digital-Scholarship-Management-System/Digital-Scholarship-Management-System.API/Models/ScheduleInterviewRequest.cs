namespace Digital_Scholarship_Management_System.API.Models
{
    public class ScheduleInterviewRequest
    {
        public int ApplicationId { get; set; }

        public DateTime InterviewDate { get; set; }

        public string InterviewTime { get; set; } = string.Empty;

        public string InterviewMethod { get; set; } = string.Empty;

        public string? Location { get; set; }

        public string? MeetingLink { get; set; }

        public string? Notes { get; set; }
    }
}
