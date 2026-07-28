using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // User account & access management. DB-only for now; Cognito later.
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

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
            });
        }

        // POST /api/users — register an Officer (Officers cannot self-register)
        [HttpPost]
        public async Task<IActionResult> RegisterOfficer([FromBody] RegisterOfficerRequest req)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return Conflict(new { message = "A user with this email already exists." });

            var user = new User
            {
                Email = email,
                FullName = req.FullName.Trim(),
                Role = UserRole.Officer,
                Status = UserStatus.Active,
                CognitoSub = "", // set by Cognito later
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Created($"/api/users/{user.Id}", user);
        }

        // PATCH /api/users/{id}/status — lock/unlock
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role == UserRole.Admin)
                return BadRequest(new { message = "The Admin account cannot be locked." });
            user.Status = req.Status;
            await _db.SaveChangesAsync();
            return Ok(user);
        }

        // POST /api/users/{id}/approve-sponsor — approve a sponsor's onboarding (unlocks access)
        [HttpPost("{id:int}/approve-sponsor")]
        public async Task<IActionResult> ApproveSponsor(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (user.Role != UserRole.Sponsor)
                return BadRequest(new { message = "Only sponsor accounts can be approved." });
            user.Status = UserStatus.Active;
            await _db.SaveChangesAsync();
            return Ok(user);
        }
    }

    public record RegisterOfficerRequest(string Email, string FullName);
    public record SetStatusRequest(UserStatus Status);
}