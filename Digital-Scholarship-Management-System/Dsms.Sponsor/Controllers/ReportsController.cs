using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Read-only totals for the Reports screen and the dashboard tiles. All from SQL.
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        // Cap so the response does not grow with the table.
        private const int ApplicationRowLimit = 200;

        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db) => _db = db;

        // Pending: every number here is 0 until other roles add rows. Scholarships come from
        // the Sponsor side, Applications from the Student side, approvals from the Officer.
        // GET /api/reports/summary — everything the screen needs in one call.
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var now = DateTime.UtcNow;

            var statusCounts = await _db.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int CountOf(ApplicationStatus status) =>
                statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

            // Raw numbers only; labels and icons are set in the component.
            var totals = new
            {
                Scholarships = await _db.Scholarships.CountAsync(),
                Applications = statusCounts.Sum(x => x.Count),
                Pending = CountOf(ApplicationStatus.Pending),
                UnderReview = CountOf(ApplicationStatus.UnderReview),
                Approved = CountOf(ApplicationStatus.Approved),
                Rejected = CountOf(ApplicationStatus.Rejected),
            };

            // Pending: scholarship data from the Sponsor side.
            var scholarshipRows = await _db.Scholarships
                .OrderByDescending(s => s.Deadline)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    Sponsor = s.Sponsor.CompanyName ?? s.Sponsor.FullName,
                    s.FundType,
                    Applications = s.Applications.Count(),
                    s.Deadline,
                })
                .ToListAsync();

            // Pending: Withdrawn needs a Scholarship.RemovedAt column from the Sponsor side.
            // Without it a listing is only ever Open or Closed.
            var scholarships = scholarshipRows.Select(s => new
            {
                Id = s.Id.ToString(),
                s.Title,
                s.Sponsor,
                s.FundType,
                s.Applications,
                s.Deadline,
                Status = s.Deadline < now ? "Closed" : "Open",
            });

            // Pending: application data from the Student side.
            var applicationRows = await _db.Applications
                .OrderByDescending(a => a.SubmittedAt)
                .Take(ApplicationRowLimit)
                .Select(a => new
                {
                    a.Id,
                    Student = a.Student.FullName,
                    Scholarship = a.Scholarship.Title,
                    a.SubmittedAt,
                    a.Status,
                })
                .ToListAsync();

            var applications = applicationRows.Select(a => new
            {
                Id = a.Id.ToString(),
                a.Student,
                a.Scholarship,
                Submitted = a.SubmittedAt,
                Status = ToDisplayStatus(a.Status),
            });

            // Pending: needs the Officer to approve applications before this returns anything.
            // SQL grouping ignores case here, so "Full" and "full" count as one.
            var awardsByFundType = await _db.Applications
                .Where(a => a.Status == ApplicationStatus.Approved)
                .GroupBy(a => a.Scholarship.FundType)
                .Select(g => new { FundType = g.Key, Count = g.Count() })
                .OrderBy(x => x.FundType)
                .ToListAsync();

            var columns = await _db.Scholarships
                .Select(s => new { s.FundType, s.StudyLocation, s.OrganisationType })
                .ToListAsync();

            var valuesInUse = GroupOrdinal("FundType", columns.Select(c => c.FundType))
                .Concat(GroupOrdinal("StudyLocation", columns.Select(c => c.StudyLocation)))
                .Concat(GroupOrdinal("OrganisationType", columns.Select(c => c.OrganisationType)))
                .ToList();

            return Ok(new
            {
                totals,
                scholarships,
                applications,
                awardsByFundType,
                valuesInUse,
            });
        }


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


        // Grouped here instead of in SQL so different casings stay separate.
        private static IEnumerable<ReferenceValue> GroupOrdinal(string category, IEnumerable<string> values) =>
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value, StringComparer.Ordinal)
                .Select(group => new ReferenceValue(category, group.Key, group.Count()))
                .OrderBy(item => item.Value, StringComparer.Ordinal);

        // The frontend expects "Under Review" with a space.
        private static string ToDisplayStatus(ApplicationStatus status) => status switch
        {
            ApplicationStatus.Pending => "Pending",
            ApplicationStatus.UnderReview => "Under Review",
            ApplicationStatus.Approved => "Approved",
            ApplicationStatus.Rejected => "Rejected",
            _ => status.ToString(),
        };

        // The token has no role claim, so read the role from the database.
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

    public record ReferenceValue(string Category, string Value, int Count);
}
