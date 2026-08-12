using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;

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
        public string? SsmNumber { get; set; }
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
        private readonly IAmazonCognitoIdentityProvider _cognito;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuditLogService _auditLog;

        public AuthController(IAmazonCognitoIdentityProvider cognito, AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory, AuditLogService auditLog)
        {
            _cognito = cognito;
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _auditLog = auditLog;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.Role != "user" && request.Role != "sponsor")
            {
                return BadRequest("Role must be 'user' or 'sponsor'.");
            }

            if (request.Role == "sponsor" && (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.SsmNumber)))
            {
                return BadRequest("Company name and SSM number are required for sponsor registration.");
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

            await _cognito.AdminSetUserPasswordAsync(new AdminSetUserPasswordRequest
            {
                UserPoolId = userPoolId,
                Username = request.Username,
                Password = temporaryPassword,
                Permanent = false,
            });

            var isSponsor = request.Role == "sponsor";

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
                Email = request.Email,
                FullName = request.FullName,
                Role = isSponsor ? UserRole.sponsor : UserRole.user,
                IsApproved = !isSponsor,
                CompanyName = isSponsor ? request.CompanyName : null,
                SsmNumber = isSponsor ? request.SsmNumber : null,
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
                throw;
            }

            if (!isSponsor)
            {
                await SendOnboardingEmailAsync(request.Email, request.Username, temporaryPassword, request.FullName);
            }
            return Ok(new { message = "Registration successful." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);

            if (user != null)
            {
                var userPoolId = _config["Cognito:UserPoolId"];
                var getUserResponse = await _cognito.AdminGetUserAsync(new AdminGetUserRequest
                {
                    UserPoolId = userPoolId,
                    Username = user.CognitoSub,
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
