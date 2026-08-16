using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsersController> _logger;
        private readonly AuditLogService _auditLog;
        private readonly IAmazonCognitoIdentityProvider _cognito;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public UsersController(AppDbContext db, ILogger<UsersController> logger, AuditLogService auditLog, IAmazonCognitoIdentityProvider cognito, IConfiguration config, IHttpClientFactory httpClientFactory, IAmazonS3 s3)
        {
            _db = db;
            _logger = logger;
            _auditLog = auditLog;
            _cognito = cognito;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _s3 = s3;
            _bucketName = config["S3:BucketName"]!;
        }

        // GET /api/users — admin only. [Authorize] alone would let any signed-in student
        // read every account. Projected to the five fields the Users & Access screen reads;
        // the entity itself carries CognitoSub and SsmNumber.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var users = await _db.Users
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.Status,
                })
                .ToListAsync();

            return Ok(users);
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

        // Admin twin of FindCurrentStudentAsync. The JWT carries no app-role claim, so the
        // role has to come from the DB row the sub maps to.
        private async Task<(User? User, IActionResult? Error)> FindCurrentAdminAsync()
        {
            var sub = User.FindFirst("sub")?.Value;
            if (sub is null)
            {
                return (null, Unauthorized());
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.CognitoSub == sub);
            if (user is null)
            {
                return (null, NotFound());
            }

            if (user.Role != UserRole.admin)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Only admin accounts can access this feature."));
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

            var previousName = user.FullName;
            user.FullName = fullName;
            await _db.SaveChangesAsync();

            // Log both names so older entries can still be matched to this user.
            if (!string.Equals(previousName, fullName, StringComparison.Ordinal))
            {
                await _auditLog.LogAsync(user, $"Changed display name from \"{previousName}\" to \"{fullName}\"");
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

        // Officer registration moved to POST /api/auth/register (role: "officer", Admin-gated) —
        // it reuses the real Cognito + temp-password + email flow instead of duplicating it here.
        // See AuthController.Register.

        // PATCH /api/users/{id}/status — lock/unlock. Disables/enables the Cognito user so
        // sign-in itself is refused, not just a DB flag nothing else checks.
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest req)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null) return errorResult!;

            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role == UserRole.admin)
                return BadRequest(new { message = "The Admin account cannot be locked." });

            if (user.CognitoUsername is null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "This account has no Cognito username recorded, so it cannot be locked or unlocked." });
            }

            var userPoolId = _config["Cognito:UserPoolId"];
            try
            {
                if (req.Status == UserStatus.Locked)
                {
                    await _cognito.AdminDisableUserAsync(new AdminDisableUserRequest
                    {
                        UserPoolId = userPoolId,
                        Username = user.CognitoUsername,
                    });
                }
                else
                {
                    await _cognito.AdminEnableUserAsync(new AdminEnableUserRequest
                    {
                        UserPoolId = userPoolId,
                        Username = user.CognitoUsername,
                    });
                }
            }
            catch (UserNotFoundException)
            {
                _logger.LogError("No Cognito user named {Username} for user {UserId}", user.CognitoUsername, user.Id);
                return NotFound(new { message = "No Cognito user matches this account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cognito call failed while setting status for user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Could not reach Cognito. Please try again." });
            }

            user.Status = req.Status;
            await _db.SaveChangesAsync();

            var verb = req.Status == UserStatus.Locked ? "Locked" : "Unlocked";
            await _auditLog.LogAsync(admin, $"{verb} account {user.Email}");

            return Ok(user);
        }

        // POST /api/users/{id}/approve-sponsor — approve a sponsor's onboarding. Re-enables the
        // Cognito user that was disabled at registration, or approval would leave them still
        // unable to sign in.
        [HttpPost("{id:int}/approve-sponsor")]
        public async Task<IActionResult> ApproveSponsor(int id)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null) return errorResult!;

            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role != UserRole.sponsor)
                return BadRequest(new { message = "Only sponsor accounts can be approved." });

            if (user.CognitoUsername is null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "This account has no Cognito username recorded, so it cannot be approved." });
            }

            var temporaryPassword = AuthController.GenerateTemporaryPassword();

            try
            {
                await _cognito.AdminEnableUserAsync(new AdminEnableUserRequest
                {
                    UserPoolId = _config["Cognito:UserPoolId"],
                    Username = user.CognitoUsername,
                });

                await _cognito.AdminSetUserPasswordAsync(new AdminSetUserPasswordRequest
                {
                    UserPoolId = _config["Cognito:UserPoolId"],
                    Username = user.CognitoUsername,
                    Password = temporaryPassword,
                    Permanent = false,
                });
            }
            catch (UserNotFoundException)
            {
                _logger.LogError("No Cognito user named {Username} for user {UserId}", user.CognitoUsername, user.Id);
                return NotFound(new { message = "No Cognito user matches this account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cognito call failed while approving sponsor {UserId}", user.Id);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Could not reach Cognito. Please try again." });
            }

            user.Status = UserStatus.Active;
            user.SponsorStatus = SponsorApprovalStatus.Approved;
            user.DecidedAt = DateTime.UtcNow;
            user.DecidedBy = admin.FullName;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(admin, $"Approved sponsor {user.Email}");

            try
            {
                var httpClientFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                await AuthController.SendOnboardingEmailAsync(httpClientFactory, _config, user.Email, user.CognitoUsername, temporaryPassword, user.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Approved sponsor {UserId} but the credentials email failed", user.Id);
            }

            return Ok(Project(user));
        }

        [HttpGet("{id}/certificate")]
        public async Task<IActionResult> GetCertificateUrl(int id)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null) return errorResult!;

            var user = await _db.Users.FindAsync(id);
            if (user?.CertificateS3Key is null) return NotFound();

            var url = await _s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = user.CertificateS3Key,
                Expires = DateTime.UtcNow.AddMinutes(15),
            });
            return Ok(new { url });
        }

        // POST /api/users/{id}/reject-sponsor — refuse posting rights, keep the account.
        // Enables the Cognito user like approval does: registration disabled it only for the
        // pending window, and sign-in is governed by lock/unlock, not by this decision.
        // Pending from the Sponsor side: rejection is stored but nothing enforces it yet.
        // POST /api/scholarships must refuse callers whose SponsorStatus is not Approved.
        [HttpPost("{id:int}/reject-sponsor")]
        public async Task<IActionResult> RejectSponsor(int id)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null) return errorResult!;

            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role != UserRole.sponsor)
                return BadRequest(new { message = "Only sponsor accounts can be rejected." });

            if (user.CognitoUsername is null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "This account has no Cognito username recorded, so it cannot be rejected." });
            }

            try
            {
                await _cognito.AdminDeleteUserAsync(new AdminDeleteUserRequest
                {
                    UserPoolId = _config["Cognito:UserPoolId"],
                    Username = user.CognitoUsername,
                });
            }
            catch (UserNotFoundException)
            {
                _logger.LogError("No Cognito user named {Username} for user {UserId}", user.CognitoUsername, user.Id);
                return NotFound(new { message = "No Cognito user matches this account." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cognito call failed while rejecting sponsor {UserId}", user.Id);
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "Could not reach Cognito. Please try again." });
            }

            user.Status = UserStatus.Locked;
            user.SponsorStatus = SponsorApprovalStatus.Rejected;
            user.DecidedAt = DateTime.UtcNow;
            user.DecidedBy = admin.FullName;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(admin, $"Rejected sponsor {user.Email}");

            try
            {
                await SendSponsorRejectedEmailAsync(user.Email, user.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rejected sponsor {UserId} but the notification email failed", user.Id);
            }

            return Ok(Project(user));
        }

        // GET /api/users/sponsors — every sponsor with its decision, for both admin tables.
        [HttpGet("sponsors")]
        public async Task<IActionResult> GetSponsors()
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null) return errorResult!;

            var sponsors = await _db.Users
                .Where(u => u.Role == UserRole.sponsor)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new SponsorResponse(
                    u.Id,
                    u.CompanyName ?? u.FullName,
                    u.CreatedAt,
                    u.SponsorStatus ?? SponsorApprovalStatus.Pending,
                    u.DecidedAt,
                    u.DecidedBy))
                .ToListAsync();

            return Ok(sponsors);
        }

        private async Task SendSponsorRejectedEmailAsync(string toEmail, string fullName)
        {
            var client = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config["Resend:ApiKey"]);

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = "Your sponsor application was not approved",
                html = $"<p>Hi {fullName},</p><p>Your sponsor application has been reviewed and was not approved, " +
                $"so your account has been removed and you will not be able to sign in.</p>" +
                $"<p>If you believe this decision was made in error, or you would like to apply again with " +
                $"corrected details, please contact us.</p>"
            });
        }

        private static SponsorResponse Project(User user) => new(
            user.Id,
            user.CompanyName ?? user.FullName,
            user.CreatedAt,
            user.SponsorStatus ?? SponsorApprovalStatus.Pending,
            user.DecidedAt,
            user.DecidedBy
        );

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHIJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnpqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*";
            const string all = upper + lower + digits + symbols;

            var chars = new char[12];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }
            chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            chars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

            return new string(chars);
        }


        // For sponsor, to send email temp password at approval time from admin.
        private async Task SendOnboardingEmailAsync(string toEmail, string username, string temporaryPassword, string fullName)
        {
            var loginUrl = $"{_config["Frontend:BaseUrl"]}/login";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["Resend:ApiKey"]);

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = "Your Scholarship Management System account",
                html = $"<p>Hi {fullName},</p><p>Username: <strong>{username}</strong><p>Temporary password: <strong>{temporaryPassword}</strong></p>" +
                $"<p>You'll be asked to set a new password the first time you log in.</p>" +
                $"<p>You must signed in and changed password within 7 Days</p>" +
                $"<p>Click <a href=\"{loginUrl}\">here</a> to login</p>"
            });
        }
    }



    public record SetStatusRequest(UserStatus Status);
    public record UpdateProfileRequest(string FullName);
    public record SponsorResponse(
        int Id,
        string CompanyName,
        DateTime RegisteredAt,
        SponsorApprovalStatus Status,
        DateTime? DecidedAt,
        string? DecidedBy
    );
}