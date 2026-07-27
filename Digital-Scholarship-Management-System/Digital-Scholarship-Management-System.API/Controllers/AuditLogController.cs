using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Audit log review. Admin reads/queries; local EF table now, DynamoDB later.
    [Route("api/audit-log")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuditLogController(AppDbContext db) => _db = db;

        // GET /api/audit-log?role=Officer&person=alice&from=2026-01-01&to=2026-12-31
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] UserRole? role,
            [FromQuery] string? person,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _db.AuditLogs.AsQueryable();
            if (role is not null) query = query.Where(a => a.Role == role.Value);
            if (!string.IsNullOrWhiteSpace(person)) query = query.Where(a => a.User.Contains(person));
            if (from is not null) query = query.Where(a => a.Timestamp >= from.Value);
            if (to is not null) query = query.Where(a => a.Timestamp <= to.Value);
            var entries = await query.OrderByDescending(a => a.Timestamp).ToListAsync();
            return Ok(entries);
        }
    }
}
