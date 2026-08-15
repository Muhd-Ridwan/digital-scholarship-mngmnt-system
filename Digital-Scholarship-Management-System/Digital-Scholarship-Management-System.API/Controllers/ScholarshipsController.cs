using System.ComponentModel.DataAnnotations;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    
    public class CreateScholarshipRequest
    {
        [Required] 
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public string EligibilityCriteria { get; set; } = string.Empty;
        [Required]
        public string FundType { get; set; } = string.Empty;
        [Required]
        public string StudyLocation { get; set; } = string.Empty;
        [Required]
        public string OrganisationType { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue, ErrorMessage = "Funding amount must be a positive value.")]
        public decimal FundingAmount { get; set; }
        public DateTime Deadline { get; set; }
    }


    [ApiController]
    [Route("api/scholarships")]
    public class ScholarshipsController : ControllerBase // Because only use it as API, no view so thats why have Base
    {
        private readonly AppDbContext _db;
        private readonly AuditLogService _auditLog;
        public ScholarshipsController(AppDbContext db, AuditLogService auditLog)
        {
            _db = db;
            _auditLog = auditLog;
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMine()
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var scholarships = await _db.Scholarships
                .Where(s => s.SponsorId == sponsor.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.FundType,
                    s.StudyLocation,
                    s.OrganisationType,
                    s.FundingAmount,
                    s.Deadline,
                    Status = ToStatusString(s.Status),
                    Applications = s.Applications.Count()
                })
                .ToListAsync();

            return Ok(scholarships);
        }

        // For students
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var scholarships = await _db.Scholarships
                .Include(s => s.Sponsor)
                .Where(s => s.Status == ScholarshipStatus.Open)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.FundType,
                    s.StudyLocation,
                    s.OrganisationType,
                    s.FundingAmount,
                    s.Deadline,
                    SponsorName = s.Sponsor.CompanyName ?? s.Sponsor.FullName,
                })
                .ToListAsync();

            return Ok(scholarships);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var scholarship = await _db.Scholarships
                .Include(s => s.Sponsor)
                .Where(s => s.Id == id && s.Status == ScholarshipStatus.Open)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.EligibilityCriteria,
                    s.FundType,
                    s.StudyLocation,
                    s.OrganisationType,
                    s.FundingAmount,
                    s.Deadline,
                    SponsorName = s.Sponsor.CompanyName ?? s.Sponsor.FullName,
                })
                .FirstOrDefaultAsync();

            if (scholarship == null)
            {
                return NotFound();
            }

            return Ok(scholarship);
        }

        // For sponsors
        [HttpGet("mine/{id}")]
        [Authorize]
        public async Task<IActionResult> GetMineById(int id)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var scholarship = await _db.Scholarships
                .Where(s => s.Id == id && s.SponsorId == sponsor.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.EligibilityCriteria,
                    s.FundType,
                    s.StudyLocation,
                    s.OrganisationType,
                    s.FundingAmount,
                    s.Deadline,
                    Status = ToStatusString(s.Status),
                    Applications = s.Applications.Count()
                })
                .FirstOrDefaultAsync();

            if (scholarship is null)
            {
                return NotFound("Scholarship not found");
            }

            return Ok(scholarship);
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

        private static string ToStatusString(ScholarshipStatus status) => status switch
        {
            ScholarshipStatus.Draft => "draft",
            ScholarshipStatus.Open => "open",
            ScholarshipStatus.Closed => "closed",
            _ => status.ToString().ToLowerInvariant(),
        };

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateScholarshipRequest request)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor == null)
            {
                return errorResult!;
            }

            if (request.Deadline < DateTime.UtcNow)
            {
                return BadRequest("Deadline cannot be in the past.");
            }

            var scholarship = new Scholarship
            {
                Title = request.Title,
                Description = request.Description,
                EligibilityCriteria = request.EligibilityCriteria,
                FundType = request.FundType,
                StudyLocation = request.StudyLocation,
                OrganisationType = request.OrganisationType,
                FundingAmount = request.FundingAmount,
                Deadline = request.Deadline,
                SponsorId = sponsor.Id,
                Status = ScholarshipStatus.Draft
            };

            _db.Scholarships.Add(scholarship);
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Create scholarship listing: {scholarship.Title} (ID: {scholarship.Id})");
            return Ok(new { scholarship.Id, message = "Scholarship created successfully." });
        }

        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<IActionResult> Publish(int id)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var scholarship = await _db.Scholarships.FirstOrDefaultAsync(s => s.Id == id && s.SponsorId == sponsor.Id);

            if (scholarship is null) {
                return NotFound("Scholarship not found");
            }

            if (scholarship.Status != ScholarshipStatus.Draft) {
                return BadRequest("Only draft listings can be published.");
            }

            scholarship.Status = ScholarshipStatus.Open;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Published scholarship listing '{scholarship.Title}' (ID {scholarship.Id})");

            return Ok(new { scholarship.Id, Status = ToStatusString(scholarship.Status) });
        }

        [HttpPost("{id}/close")]
        [Authorize]
        public async Task<IActionResult> Close(int id) {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null) {
                return errorResult!;
            }

            var scholarship = await _db.Scholarships.FirstOrDefaultAsync(s => s.Id == id && s.SponsorId == sponsor.Id);

            if (scholarship is null)
            {
                return NotFound("Scholarship not found");
            }

            if (scholarship.Status != ScholarshipStatus.Open)
            {
                return BadRequest("Only open listings can be closed.");
            }

            scholarship.Status = ScholarshipStatus.Closed;
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Closed scholarship listing '{scholarship.Title}' (ID {scholarship.Id})");

            return Ok(new { scholarship.Id, Status = ToStatusString(scholarship.Status) });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var scholarship = await _db.Scholarships.FirstOrDefaultAsync(s => s.Id == id && s.SponsorId == sponsor.Id);

            if (scholarship is null)
            {
                return NotFound("Scholarship not found");
            }

            if (scholarship.Status != ScholarshipStatus.Draft)
            {
                return BadRequest("Only draft listings can be deleted.");
            }

            _db.Scholarships.Remove(scholarship);
            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Delete scholarship listing '{scholarship.Title}' (ID {scholarship.Id})");

            return Ok(new { message = "Scholarship deleted." });
        }

    }
}
