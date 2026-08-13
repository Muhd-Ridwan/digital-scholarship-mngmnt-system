using System.ComponentModel.DataAnnotations;
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
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var scholarships = await _db.Scholarships
                .Include(s => s.Sponsor)
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
                .Where(s => s.Id == id)
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateScholarshipRequest request)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor == null) {
                return errorResult!;
            }

            if (request.Deadline < DateTime.UtcNow) {
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

    }
}
