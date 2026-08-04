using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Read-only view of the values sponsors have actually used on their scholarships.
    // There is no reference_data table any more — the values are derived, so there is
    // nowhere to persist an add or an active/inactive flag.
    [Route("api/reference-data")]
    [ApiController]
    [Authorize]
    public class ReferenceDataController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReferenceDataController(AppDbContext db) => _db = db;

        // GET /api/reference-data — distinct values across all scholarships, expired included
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var fundTypes = await DistinctAsync(s => s.FundType);
            var studyLocations = await DistinctAsync(s => s.StudyLocation);
            var organisationTypes = await DistinctAsync(s => s.OrganisationType);

            var items = fundTypes.Select(v => new { Category = "FundType", Value = v })
                .Concat(studyLocations.Select(v => new { Category = "StudyLocation", Value = v }))
                .Concat(organisationTypes.Select(v => new { Category = "OrganisationType", Value = v }));

            return Ok(items);
        }

        private async Task<List<string>> DistinctAsync(
            System.Linq.Expressions.Expression<Func<Scholarship, string>> column)
        {
            return await _db.Scholarships
                .Select(column)
                .Where(value => value != null && value != "")
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync();
        }

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
