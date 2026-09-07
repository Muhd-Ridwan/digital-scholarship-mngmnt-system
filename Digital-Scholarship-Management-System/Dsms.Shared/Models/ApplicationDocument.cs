namespace Digital_Scholarship_Management_System.API.Models
{
    public class ApplicationDocument
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;
        public DocumentType DocumentType { get; set; }
        public string S3ObjectKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadAt { get; set; } = DateTime.UtcNow;

        //officer review
        public DateTime? ReviewedAt { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    }
}
