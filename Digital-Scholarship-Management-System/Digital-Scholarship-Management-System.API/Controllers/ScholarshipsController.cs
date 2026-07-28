using Microsoft.AspNetCore.Mvc;
using Digital_Scholarship_Management_System.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]
    [Route("api/scholarships")]
    public class ScholarshipsController : ControllerBase // Because only use it as API, no view so thats why have Base
    {
        private readonly AppDbContext _db;
        public ScholarshipsController(AppDbContext db)
        {
            _db = db;
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
    }
}
