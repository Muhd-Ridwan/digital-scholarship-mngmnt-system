using System.ComponentModel.DataAnnotations;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Digital_Scholarship_Management_System.API.Services;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    public class DisburseRequest
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Disbursed amount must be a positive value.")]
        public decimal DisbursedAmount { get; set; }
    }

    [ApiController]
    [Route("api/applications")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AuditLogService _auditLog;

        public ApplicationsController(AppDbContext db, AuditLogService auditLog)
        {
            _db = db;
            _auditLog = auditLog;
        }

        [HttpGet("sponsor/pending-disbursements")]
        public async Task<IActionResult> GetPendingDisbursements()
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var applications = await _db.Applications
                .Include(a => a.Scholarship)
                .Include(a => a.Student)
                .Where(a => a.Scholarship.SponsorId == sponsor.Id
                         && a.Status == ApplicationStatus.Approved
                         && a.DisbursementStatus == DisbursementStatus.NotDisbursed)
                .OrderBy(a => a.DecisionAt)
                .ToListAsync();

            return Ok(applications.Select(a => new
            {
                a.Id,
                a.ScholarshipId,
                ScholarshipTitle = a.Scholarship.Title,
                a.Scholarship.FundType,
                a.Scholarship.FundingAmount,
                a.StudentId,
                StudentName = a.Student.FullName,
                a.DecisionAt,
            }));
        }

        [HttpPost("{id}/disburse")]
        public async Task<IActionResult> Disburse(int id, [FromBody] DisburseRequest request)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var application = await _db.Applications
                .Include(a => a.Scholarship)
                .FirstOrDefaultAsync(a => a.Id == id && a.Scholarship.SponsorId == sponsor.Id);

            if (application is null)
            {
                return NotFound("Application not found");
            }

            if (application.Status != ApplicationStatus.Approved)
            {
                return BadRequest("Only approved applications can be disbursed.");
            }

            if (application.DisbursementStatus != DisbursementStatus.NotDisbursed)
            {
                return BadRequest("This application has already been disbursed.");
            }

            application.DisbursementStatus = DisbursementStatus.Disbursed;
            application.DisbursedAmount = request.DisbursedAmount;
            application.DisbursedAt = DateTime.UtcNow;
            application.DisbursedByUserId = sponsor.Id;

            if (application.Scholarship.FundType == "Full Scholarship")
            {
                application.Scholarship.FundingAmount = request.DisbursedAmount;
            }

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Disbursed RM{request.DisbursedAmount} to application ID {application.Id} for '{application.Scholarship.Title}'");

            return Ok(new { application.Id, message = "Disbursement recorded." });
        }

        private async Task<(User? User, IActionResult? Error)> FindCurrentSponsorAsync()
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

            if (user.Role != UserRole.sponsor)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Only sponsor accounts can access this feature."));
            }
            return (user, null);
        }
    }
}
