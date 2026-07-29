using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Models
{
    public enum Gender
    {
        male = 1,
        female,
    }

    public enum QualificationLevel
    {
        spm = 1,
        stpm,
        diploma,
        degree,
        masters,
        phd,
        other,
    }

    public class StudentProfile
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;


        // Personal Detail
        public string IcNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string Nationality { get; set; } = string.Empty;
        public string? Race { get; set; }
        public bool HasDisability { get; set; }
        public string? DisabilityDetails { get; set; }

        // Contact Details
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;

        // Family Details
        public string? FatherName { get; set; }
        public bool FatherDeceased { get; set; }
        public string? FatherOccupation { get; set; }
        public string? MotherName { get; set; }
        public bool MotherDeceased { get; set; }
        public string? MotherOccupation { get; set; }
        public bool ParentsDivorced { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        public decimal HouseholdIncome { get; set; }
        public int NumberOfSiblings { get; set; }

        // Academic Details
        public QualificationLevel HighestQualification { get; set; }
        public string? ExamResults { get; set; }
        public string? CurrentInstitution { get; set; }
        public string? FieldOfStudy { get; set; }

        // Disbursement Details
        public string BankName { get; set; } = string.Empty;
        public string BankAccNumber { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

