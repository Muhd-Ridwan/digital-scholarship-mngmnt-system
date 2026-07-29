using Microsoft.AspNetCore.Mvc;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    public class UpdateStudentProfileRequest
    {
        [Required]
        public string IcNumber { get; set; } = string.Empty;
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public Gender Gender { get; set; }
        [Required]
        public string Nationality { get; set; } = string.Empty;
        public string? Race { get; set; }
        public bool HasDisability { get; set; }
        public string? DisabilityDetails { get; set; }
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        public string Address { get; set; } = string.Empty;
        [Required]
        public string EmergencyContactName { get; set; } = string.Empty;
        [Required]
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string? FatherName { get; set; }
        public bool FatherDeceased { get; set; }
        public string? FatherOccupation { get; set; }
        public string? MotherName { get; set; }

        public bool MotherDeceased { get; set; }
        public string? MotherOccupation { get; set; }
        public bool ParentsDivorced { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianPhone { get; set; }
        [Required, Range(0, double.MaxValue)]
        public decimal HouseholdIncome { get; set; }
        [Range(0, 20)]
        public int NumberOfSiblings { get; set; }
        [Required]
        public QualificationLevel HighestQualification { get; set; }
        public string? ExamResults { get; set; }
        public string? CurrentInstitution { get; set; }
        public string? FieldOfStudy { get; set; }
        [Required]
        public string BankName { get; set; } = string.Empty;
        [Required]
        public string BankAccNumber { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase // Using controller base because this is pure API with no views
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext db, ILogger<UsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var sub = User.FindFirst("sub")?.Value;
            if (sub is null)
            {
                return Unauthorized();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.CognitoSub == sub);
            if (user is null)
            {
                return NotFound();
            }

            return Ok(new
            {
                user.CognitoSub,
                user.Email,
                user.FullName,
                Role = user.Role.ToString().ToLowerInvariant(),
            });
        }

        [HttpGet("me/profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            if (user.StudentProfile is null)
            {
                return NotFound("Profile has not been created yet.");
            }

            return Ok(ToResponse(user.StudentProfile));
        }

        [HttpPut("me/profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateStudentProfileRequest request)
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }
            if (request.DateOfBirth > DateTime.UtcNow)
            {
                return BadRequest("Date of birth cannot be in future.");
            }

            if (request.FatherDeceased && request.MotherDeceased && string.IsNullOrWhiteSpace(request.GuardianName))
            {
                return BadRequest("Guardian name is required when both parents are deceased.");
            }

            if (request.FatherDeceased && request.MotherDeceased && string.IsNullOrWhiteSpace(request.GuardianPhone))
            {
                return BadRequest("Guardian phone number is required when both parents are deceased.");
            }

            var profile = user.StudentProfile ?? new StudentProfile { UserId = user.Id };
            var isNew = user.StudentProfile is null;

            profile.IcNumber = request.IcNumber;
            profile.DateOfBirth = request.DateOfBirth;
            profile.Gender = request.Gender;
            profile.Nationality = request.Nationality;
            profile.Race = request.Race;
            profile.HasDisability = request.HasDisability;
            profile.DisabilityDetails = request.HasDisability ? request.DisabilityDetails : null;
            profile.PhoneNumber = request.PhoneNumber;
            profile.Address = request.Address;
            profile.EmergencyContactName = request.EmergencyContactName;
            profile.EmergencyContactPhone = request.EmergencyContactPhone;
            profile.FatherName = request.FatherName;
            profile.FatherDeceased = request.FatherDeceased;
            profile.FatherOccupation = request.FatherDeceased ? null : request.FatherOccupation;
            profile.MotherName = request.MotherName;
            profile.MotherDeceased = request.MotherDeceased;
            profile.MotherOccupation = request.MotherDeceased ? null : request.MotherOccupation;
            profile.ParentsDivorced = request.ParentsDivorced;
            profile.GuardianName = request.GuardianName;
            profile.GuardianPhone = request.GuardianPhone;
            profile.HouseholdIncome = request.HouseholdIncome;
            profile.NumberOfSiblings = request.NumberOfSiblings;
            profile.HighestQualification = request.HighestQualification;
            profile.ExamResults = request.ExamResults;
            profile.CurrentInstitution = request.CurrentInstitution;
            profile.FieldOfStudy = request.FieldOfStudy;
            profile.BankName = request.BankName;
            profile.BankAccNumber = request.BankAccNumber;
            profile.UpdatedAt = DateTime.UtcNow;

            if (isNew)
            {
                _db.StudentProfiles.Add(profile);
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save student profile for user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Could not save your profile. Please try again.");
            }
            return Ok(ToResponse(profile));
        }

        private async Task<(User? User, IActionResult? Error)> FindCurrentStudentAsync()
        {
            var sub = User.FindFirst("sub")?.Value;
            if (sub is null)
            {
                return (null, Unauthorized());
            }

            var user = await _db.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.CognitoSub == sub);

            if (user is null)
            {
                return (null, NotFound());
            }

            if (user.Role != UserRole.user)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Only student accounts have a profile."));
            }

            return (user, null);
        }

        private static object ToResponse(StudentProfile profile)
        {
            return new
            {
                profile.IcNumber,
                profile.DateOfBirth,
                Gender = profile.Gender.ToString().ToLowerInvariant(),
                profile.Nationality,
                profile.Race,
                profile.HasDisability,
                profile.DisabilityDetails,
                profile.PhoneNumber,
                profile.Address,
                profile.EmergencyContactName,
                profile.EmergencyContactPhone,
                profile.FatherName,
                profile.FatherDeceased,
                profile.FatherOccupation,
                profile.MotherName,
                profile.MotherDeceased,
                profile.MotherOccupation,
                profile.ParentsDivorced,
                profile.GuardianName,
                profile.GuardianPhone,
                profile.HouseholdIncome,
                profile.NumberOfSiblings,
                HighestQualification = profile.HighestQualification.ToString().ToLowerInvariant(),
                profile.ExamResults,
                profile.CurrentInstitution,
                profile.FieldOfStudy,
                profile.BankName,
                profile.BankAccNumber,
                profile.UpdatedAt,
            };
        }
    }
}
