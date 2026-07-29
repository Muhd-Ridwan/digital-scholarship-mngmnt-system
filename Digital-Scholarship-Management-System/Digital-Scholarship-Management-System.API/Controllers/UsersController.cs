using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // User account & access management.
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAmazonCognitoIdentityProvider _cognito;
        private readonly IConfiguration _config;

        public UsersController(AppDbContext db, IAmazonCognitoIdentityProvider cognito, IConfiguration config)
        {
            _db = db;
            _cognito = cognito;
            _config = config;
        }

        // GET /api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
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