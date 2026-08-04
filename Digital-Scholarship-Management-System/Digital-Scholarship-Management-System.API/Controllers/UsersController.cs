using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Digital_Scholarship_Management_System.API.Services;

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
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsersController> _logger;
        private readonly AuditLogService _auditLog;

        public UsersController(AppDbContext db, ILogger<UsersController> logger, AuditLogService auditLog)
        {
            _db = db;
            _logger = logger;
            _auditLog = auditLog;
        }

        // GET /api/users/me
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
                user.CreatedAt,
            });
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
            await _auditLog.LogAsync(user, "Updated profile details");
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

        // PUT /api/users/me — update own profile. Full name only; email is the
        // Cognito identity + unique index, password stays with Cognito.
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req)
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

            var fullName = req.FullName?.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return BadRequest(new { message = "Full name is required." });
            }

            user.FullName = fullName;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                user.CognitoSub,
                user.Email,
                user.FullName,
                Role = user.Role.ToString().ToLowerInvariant(),
                user.CreatedAt,
            });
        }

        // Officer registration moved to POST /api/auth/register (role: "officer", Admin-gated) —
        // it reuses the real Cognito + temp-password + email flow instead of duplicating it here.
        // See AuthController.Register.

        // PATCH /api/users/{id}/status — lock/unlock. Disables/enables the Cognito user so
        // sign-in itself is refused, not just a DB flag nothing else checks.
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role == UserRole.admin)
                return BadRequest(new { message = "The Admin account cannot be locked." });

            var userPoolId = _config["Cognito:UserPoolId"];
            if (req.Status == UserStatus.Locked)
            {
                await _cognito.AdminDisableUserAsync(new AdminDisableUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = user.CognitoSub,
                });
            }
            else
            {
                await _cognito.AdminEnableUserAsync(new AdminEnableUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = user.CognitoSub,
                });
            }

            user.Status = req.Status;
            await _db.SaveChangesAsync();
            return Ok(user);
        }

        // POST /api/users/{id}/approve-sponsor — approve a sponsor's onboarding. Re-enables the
        // Cognito user that was disabled at registration, or approval would leave them still
        // unable to sign in.
        [HttpPost("{id:int}/approve-sponsor")]
        public async Task<IActionResult> ApproveSponsor(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role != UserRole.sponsor)
                return BadRequest(new { message = "Only sponsor accounts can be approved." });

            await _cognito.AdminEnableUserAsync(new AdminEnableUserRequest
            {
                UserPoolId = _config["Cognito:UserPoolId"],
                Username = user.CognitoSub,
            });

            user.Status = UserStatus.Active;
            await _db.SaveChangesAsync();
            return Ok(user);
        }
    }

    public record SetStatusRequest(UserStatus Status);
    public record UpdateProfileRequest(string FullName);
}