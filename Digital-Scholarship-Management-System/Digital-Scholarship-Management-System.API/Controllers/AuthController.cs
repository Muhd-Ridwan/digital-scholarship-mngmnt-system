using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
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
    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        [RegularExpression(@"^\d{7}-[A-Z]$|^\d{12}$", ErrorMessage = "SSM number must be either 7 digits + a letter (e.g. 1456789 - A) or 12 digits.")]
        public string? SsmNumber { get; set; }

        public IFormFile? Certificate { get; set; }
    }



    public class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase // Because only use it as API, no view so thats why have Base
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        };

        private readonly IAmazonCognitoIdentityProvider _cognito;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuditLogService _auditLog;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public AuthController(IAmazonCognitoIdentityProvider cognito, AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory, AuditLogService auditLog, IAmazonS3 s3)
        {
            _cognito = cognito;
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _auditLog = auditLog;
            _s3 = s3;
            _bucketName = config["S3:BucketName"]!;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest request)
        {
            if (request.Role != "user" && request.Role != "sponsor" && request.Role != "officer")
            {
                return BadRequest("Role must be 'user', 'sponsor', or 'officer'.");
            }

            User? caller = null;

            var isOfficer = request.Role == "officer";
            if (isOfficer)
            {
                var callerSub = User.FindFirst("sub")?.Value;
                caller = callerSub is null
                    ? null
                    : await _db.Users.FirstOrDefaultAsync(u => u.CognitoSub == callerSub);
                if (caller is null || caller.Role != UserRole.admin)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Only an Admin can register an Officer.");
                }
            }

            var isSponsor = request.Role == "sponsor";

            if (isSponsor && (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.SsmNumber)))
            {
                return BadRequest("Company name and SSM number are required for sponsor registration.");
            }

            if (isSponsor && (request.Certificate is null || request.Certificate.Length == 0))
            {
                return BadRequest("A registration certificate is required for sponsor registration.");
            }

            string? certExtension = null;
            if (isSponsor)
            {
                var file = request.Certificate!;
                if (file.Length > MaxFileSizeBytes)
                {
                    return BadRequest("Certificate exceeds the 10 MB size limit.");
                }

                certExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(certExtension))
                {
                    return BadRequest("Unsupported file type. Allowed types: .pdf, .doc, .docx, .png, .jpg, .jpeg");
                }

                await using var stream = file.OpenReadStream();
                var header = new byte[8];
                await stream.ReadAsync(header, 0, header.Length);
                stream.Position = 0;

                if (!HasValidSignature(header, certExtension))
                {
                    return BadRequest("Certificate content does not match its extension.");
                }
            }

            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict("That email is already registered");
            }

            var userPoolId = _config["Cognito:UserPoolId"];
            var temporaryPassword = GenerateTemporaryPassword();

            AdminCreateUserResponse createUserResponse;
            try
            {
                createUserResponse = await _cognito.AdminCreateUserAsync(new AdminCreateUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = request.Username,
                    MessageAction = MessageActionType.SUPPRESS,
                    UserAttributes = new List<AttributeType>
              {
                  new() { Name = "email", Value = request.Email },
                  new() { Name = "email_verified", Value = "true" },
              },
                });
            }
            catch (UsernameExistsException)
            {
                return Conflict("That username is already taken.");
            }

            var sub = createUserResponse.User.Attributes.First(a => a.Name == "sub").Value;

            // S3 Key creation for certificate, add into folder sponsorcertificate in S3.
            string? certificateS3Key = null;
            if (isSponsor)
            {
                var file = request.Certificate!;
                certificateS3Key = $"sponsorcertificate/{sub}/{Guid.NewGuid()}{certExtension}";
                try
                {
                    await using var stream = file.OpenReadStream();
                    await _s3.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = certificateS3Key,
                        InputStream = stream,
                        ContentType = file.ContentType,
                    });
                }
                catch (Exception ex)
                {
                    await _cognito.AdminDeleteUserAsync(new AdminDeleteUserRequest
                    {
                        UserPoolId = userPoolId,
                        Username = request.Username,
                    });
                    return StatusCode(StatusCodes.Status500InternalServerError, "Could not upload the certificate. Please try again.");
                }
            }

            await _cognito.AdminSetUserPasswordAsync(new AdminSetUserPasswordRequest
            {
                UserPoolId = userPoolId,
                Username = request.Username,
                Password = temporaryPassword,
                Permanent = false,
            });

            if (isSponsor)
            {
                await _cognito.AdminDisableUserAsync(new AdminDisableUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = request.Username,
                });
            }

            var user = new User
            {
                CognitoSub = sub,
                CognitoUsername = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                Role = isSponsor ? UserRole.sponsor : isOfficer ? UserRole.officer : UserRole.user,
                SponsorStatus = isSponsor ? SponsorApprovalStatus.Pending : null,
                CompanyName = isSponsor ? request.CompanyName : null,
                SsmNumber = isSponsor ? request.SsmNumber : null,
                CertificateS3Key = certificateS3Key,
                CertificateFileName = isSponsor ? request.Certificate!.FileName : null,
                CreatedAt = DateTime.UtcNow,
            };

            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            catch
            {
                await _cognito.AdminDeleteUserAsync(new AdminDeleteUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = request.Username,
                });
                if (certificateS3Key is not null)
                {
                    await _s3.DeleteObjectAsync(_bucketName, certificateS3Key);
                }
                throw;
            }

            if (caller is not null)
            {
                await _auditLog.LogAsync(caller, $"Registered {request.Role} account for {request.Email}");
            }
            else
            {
                await _auditLog.LogAsync(user, "Registered account");
            }

            if (!isSponsor)
            {
                await SendOnboardingEmailAsync(_httpClientFactory, _config, request.Email, request.Username, temporaryPassword, request.FullName);
            }
            return Ok(new { message = "Registration successful." });
        }

        private static bool HasValidSignature(byte[] header, string extension)
        {
            if (!FileSignatures.TryGetValue(extension, out var signatures))
            {
                return false;
            }
            return signatures.Any(sig => header.Length >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

            if (user != null && user.CognitoUsername != null)
            {
                var userPoolId = _config["Cognito:UserPoolId"];
                var getUserResponse = await _cognito.AdminGetUserAsync(new AdminGetUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = user.CognitoUsername,
                });

                var temporaryPassword = GenerateTemporaryPassword();

                await _cognito.AdminSetUserPasswordAsync(new AdminSetUserPasswordRequest
                {
                    UserPoolId = userPoolId,
                    Username = getUserResponse.Username,
                    Password = temporaryPassword,
                    Permanent = false,
                });

                await SendPasswordResetEmailAsync(user.Email, getUserResponse.Username, temporaryPassword, user.FullName);
            }

            return Ok(new { message = "If that email is registered, we've sent a new password to it." });
        }

        [HttpPost("log-login")]
        [Authorize]
        public async Task<IActionResult> LogLogin()
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

            await _auditLog.LogAsync(user, "Logged In");
            return NoContent();
        }
        internal static string GenerateTemporaryPassword()
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

        internal static async Task SendOnboardingEmailAsync(IHttpClientFactory httpClientFactory, IConfiguration config, string toEmail, string username, string temporaryPassword, string fullName)
        {
            var loginUrl = $"{config["Frontend:BaseUrl"]}/login";
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config["Resend:ApiKey"]);

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

        private async Task SendPasswordResetEmailAsync(string toEmail, string username, string temporaryPassword, string fullName)
        {
            var loginUrl = $"{_config["Frontend:BaseUrl"]}/login";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["Resend:ApiKey"]);

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = "Reset Your Scholarship Management System Password",
                html = $"<p>Hi {fullName}, </p><p>We received a request to reset your password.</p>" +
                $"<p>Username: <strong>{username}</strong></p><p>Temporary Password: <strong>{temporaryPassword}</strong></p>" +
                $"<p>This temporary password expires in 7 days.</p>" +

                $"<p>If you didn't request this, your password has already been reset.</p>" +
                $"<p>For security - please contact support or use this temporary password to sign in and secure back your account.</p>" +
                $"<p>Click <a href=\"{loginUrl}\">here</a> to sign in</p>"
            });
        }
    }
}
