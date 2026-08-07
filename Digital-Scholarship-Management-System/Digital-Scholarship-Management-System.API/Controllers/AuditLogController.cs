using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Audit log review, admin-only. Reads DynamoDB — the SQL AuditLogs table was never
    // written to and has been dropped. Writes come from AuditLogService.LogAsync.
    [Route("api/audit-log")]
    [ApiController]
    [Authorize]
    public class AuditLogController : ControllerBase
    {
        // One partition Query per day in range, so an unbounded range would sweep every
        // day since launch. Default to a week and cap the span.
        private const int DefaultRangeDays = 7;
        private const int MaxRangeDays = 366;

        private readonly AppDbContext _db;
        private readonly AuditLogService _auditLog;

        public AuditLogController(AppDbContext db, AuditLogService auditLog)
        {
            _db = db;
            _auditLog = auditLog;
        }

        // GET /api/audit-log?role=officer&person=alice&from=2026-01-01&to=2026-12-31
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] UserRole? role,
            [FromQuery] string? person,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var toUtc = AsUtc(to) ?? DateTime.UtcNow;
            var fromUtc = AsUtc(from) ?? toUtc.AddDays(-DefaultRangeDays);

            if (fromUtc > toUtc)
            {
                return BadRequest(new { message = "'from' must not be after 'to'." });
            }

            if ((toUtc.Date - fromUtc.Date).TotalDays > MaxRangeDays)
            {
                return BadRequest(new { message = $"Date range cannot exceed {MaxRangeDays} days." });
            }

            var entries = await _auditLog.QueryAsync(fromUtc, toUtc, role, person);
            return Ok(entries);
        }

        // Query-string dates bind with varying DateTimeKind. Comparing a Local or Unspecified
        // value against a UTC timestamp silently shifts the window, so pin the kind here.
        private static DateTime? AsUtc(DateTime? value) => value?.Kind switch
        {
            null => null,
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };

        // Same pattern as UsersController.FindCurrentAdminAsync — the JWT carries no app-role
        // claim, so the role comes from the DB row the sub maps to.
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
    }
}
