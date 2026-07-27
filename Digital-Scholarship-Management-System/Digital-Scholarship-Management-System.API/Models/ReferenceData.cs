using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum ReferenceCategory
    {
        University,
        Course,
        StudyLevel,
        Category,
    }

    public class ReferenceData
    {
        [Key]
        public int Id { get; set;  } 
        public ReferenceCategory Category {  get; set; }
        public string Code {  get; set; } = "";
        public string Label { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
