using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]
    [Route("api/officer/interviews")]
    [Authorize]
    public class OfficerInterviewsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OfficerInterviewsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> ScheduleInterview(
            [FromBody] ScheduleInterviewRequest request)
        {
            var application = await _db.Applications
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

            if (application == null)
                return NotFound("Application not found.");

            if (application.Status != ApplicationStatus.Shortlisted)
                return BadRequest("Only shortlisted applicants can be scheduled for an interview.");

            if (application.Interview != null)
                return Conflict("An interview has already been scheduled.");

            var interview = new Interview
            {
                ApplicationId = application.Id,
                InterviewDate = request.InterviewDate,
                InterviewTime = request.InterviewTime,
                InterviewMethod = request.InterviewMethod,
                Location = request.Location,
                MeetingLink = request.MeetingLink,
                Notes = request.Notes
            };

            _db.Interviews.Add(interview);

            //application.Status = ApplicationStatus.UnderReview;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                interview.Id,
                interview.ApplicationId,
                interview.InterviewDate,
                interview.InterviewTime,
                interview.InterviewMethod,
                interview.Location,
                interview.MeetingLink
            });
        }
    }
}
