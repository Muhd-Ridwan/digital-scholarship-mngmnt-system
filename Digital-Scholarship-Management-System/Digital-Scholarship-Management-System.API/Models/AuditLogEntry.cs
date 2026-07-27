using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public class AuditLogEntry
    {
        [Key]
        public int Id { get; set;  }
        public string User { get; set;  } = "";
        public UserRole Role {  get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set;  } = "";
    }
}
