using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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



        // ============================================================
        // CHECK OFFICER
        // ============================================================

        private async Task<User?> GetCurrentOfficerAsync()
        {
            var sub = User.FindFirst("sub")?.Value;

            if (sub is null)
                return null;

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.CognitoSub == sub);

            if (user?.Role != UserRole.officer)
                return null;

            return user;
        }

        // ============================================================
        // GET SHORTLISTED APPLICATIONS
        // ============================================================

        [HttpGet("shortlisted")]
        public async Task<IActionResult> GetShortlistedApplications()
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var applications = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.Scholarship)
                .Include(a => a.Interview)
                .Where(a =>
                    a.Status == ApplicationStatus.Shortlisted &&
                    a.Interview == null)
                .OrderByDescending(a => a.DecisionAt)
                .ToListAsync();

            return Ok(applications.Select(a => new
            {
                a.Id,

                StudentName = a.Student.FullName,
                StudentEmail = a.Student.Email,

                ScholarshipTitle = a.Scholarship.Title,

                Status = ToStatusString(a.Status),

                a.DecisionAt
            }));
        }

        // ============================================================
        // SCHEDULE INTERVIEW
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> ScheduleInterview(
            [FromBody] ScheduleInterviewRequest request)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var application = await _db.Applications
                .Include(a => a.Interview)
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

            if (application == null)
                return NotFound("Application not found.");

            if (application.Status != ApplicationStatus.Shortlisted)
                return BadRequest(
                    "Only shortlisted applicants can be scheduled for an interview.");

            if (application.Interview != null)
                return Conflict(
                    "An interview has already been scheduled.");

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

        // ============================================================
        // GET UPCOMING INTERVIEWS
        // ============================================================

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingInterviews()
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var interviews = await _db.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Student)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Scholarship)
                .Where(i => i.InterviewDate >= DateTime.Today)
                .OrderBy(i => i.InterviewDate)
                .ThenBy(i => i.InterviewTime)
                .ToListAsync();

            return Ok(interviews.Select(i => new
            {
                i.Id,
                ApplicationId = i.ApplicationId,

                StudentName = i.Application.Student.FullName,
                StudentEmail = i.Application.Student.Email,

                ScholarshipTitle = i.Application.Scholarship.Title,

                i.InterviewDate,
                i.InterviewTime,
                i.InterviewMethod,
                i.Location,
                i.MeetingLink,
                i.Notes
            }));
        }

        // ============================================================
        // STATUS STRING
        // ============================================================

        private static string ToStatusString(ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.Pending => "pending",
                ApplicationStatus.UnderReview => "under_review",
                ApplicationStatus.Shortlisted => "shortlisted",
                ApplicationStatus.Approved => "approved",
                ApplicationStatus.Rejected => "rejected",
                _ => status.ToString().ToLowerInvariant()
            };
        }
    }
}