using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db) => _db = db;

        [HttpGet("sponsor-summary")]
        public async Task<IActionResult> GetSponsorSummary()
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var scholarshipRows = await _db.Scholarships
                .Where(s => s.SponsorId == sponsor.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.FundType,
                    s.Deadline,
                    s.Status,
                    Applications = s.Applications.Count(),
                    Disbursed = s.Applications
                        .Where(a => a.DisbursementStatus == DisbursementStatus.Disbursed)
                        .Sum(a => (decimal?)a.DisbursedAmount) ?? 0,
                })
                .ToListAsync();

            var totals = new
            {
                Scholarships = scholarshipRows.Count,
                OpenListings = scholarshipRows.Count(s => s.Status == ScholarshipStatus.Open),
                ClosedListings = scholarshipRows.Count(s => s.Status == ScholarshipStatus.Closed),
                TotalApplicants = scholarshipRows.Sum(s => s.Applications),
                TotalDisbursed = scholarshipRows.Sum(s => s.Disbursed),
            };

            var scholarships = scholarshipRows.Select(s => new
            {
                s.Id,
                s.Title,
                s.FundType,
                s.Deadline,
                Status = ToScholarshipStatusString(s.Status),
                s.Applications,
                s.Disbursed,
            });

            var disbursements = await _db.Applications
                .Where(a => a.Scholarship.SponsorId == sponsor.Id && a.DisbursementStatus ==
        DisbursementStatus.Disbursed)
                .OrderByDescending(a => a.DisbursedAt)
                .Select(a => new
                {
                    a.Id,
                    ScholarshipTitle = a.Scholarship.Title,
                    StudentName = a.Student.FullName,
                    a.DisbursedAmount,
                    a.DisbursedAt,
                })
                .ToListAsync();

            return Ok(new
            {
                totals,
                scholarships,
                disbursements,
            });
        }

        private static string ToScholarshipStatusString(ScholarshipStatus status) => status switch
        {
            ScholarshipStatus.Draft => "draft",
            ScholarshipStatus.Open => "open",
            ScholarshipStatus.Closed => "closed",
            _ => status.ToString().ToLowerInvariant(),
        };

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
