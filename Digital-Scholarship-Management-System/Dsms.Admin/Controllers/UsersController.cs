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

            var temporaryPassword = GenerateTemporaryPassword();

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
                await SendOnboardingEmailAsync(user.Email, user.CognitoUsername, temporaryPassword, user.FullName);
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
        public record SponsorResponse(
        int Id,
        string CompanyName,
        DateTime RegisteredAt,
        SponsorApprovalStatus Status,
        DateTime? DecidedAt,
        string? DecidedBy
    );
}