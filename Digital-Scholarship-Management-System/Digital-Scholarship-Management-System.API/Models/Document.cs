namespace Digital_Scholarship_Management_System.API.Models
{
    public enum DocumentType
    {
        bank_statement = 1,
        academic_result,
        certificate,
        other,
    }

    //for officer to review the document 
    public enum ReviewStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class Document
    {
        public int Id { get; set; }
        public int? ApplicationId { get; set; }
        public Application? Application { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public DocumentType DocumentType { get; set; }
        public string S3ObjectKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadAt { get; set; } = DateTime.UtcNow;

        // Officer review
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        public DateTime? ReviewedAt { get; set; }
    }
}
