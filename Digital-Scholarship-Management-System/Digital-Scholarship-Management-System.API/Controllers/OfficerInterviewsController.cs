using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]
    [Route("api/officer/interviews")]
    [Authorize]
    public class OfficerInterviewsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly AuditLogService _auditLogService;

        public OfficerInterviewsController(
             AppDbContext db,
             IHttpClientFactory httpClientFactory,
             IConfiguration config,
             AuditLogService auditLogService)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _auditLogService = auditLogService;
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
            .Include(a => a.Student)
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

            if (application == null)
                return NotFound("Application not found.");

            // Only shortlisted applicants can have interviews
            if (application.Status != ApplicationStatus.Shortlisted)
            {
                return BadRequest(
                    "Only shortlisted applicants can be scheduled for an interview.");
            }

            // Prevent duplicate interview
            if (application.Interview != null)
            {
                return Conflict(
                    "An interview has already been scheduled for this applicant.");
            }

            var interview = new Interview
            {
                ApplicationId = application.Id,
                InterviewDate = request.InterviewDate,
                InterviewTime = request.InterviewTime,
                InterviewMethod = request.InterviewMethod,
                Location = request.Location,
                MeetingLink = request.MeetingLink
            };

            _db.Interviews.Add(interview);

            await _db.SaveChangesAsync();


            // Send interview email
            try
            {
                await SendInterviewScheduledEmailAsync(
                    application.Student.Email,
                    application.Student.FullName,
                    application.Scholarship.Title,
                    interview.InterviewDate,
                    interview.InterviewTime,
                    interview.InterviewMethod,
                    interview.Location,
                    interview.MeetingLink
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to send interview email: {ex.Message}");
            }


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

        private async Task SendInterviewScheduledEmailAsync(
    string toEmail,
    string fullName,
    string scholarshipTitle,
    DateTime interviewDate,
    string interviewTime,
    string interviewMethod,
    string? location,
    string? meetingLink)
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri("https://api.resend.com/");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _config["Resend:ApiKey"]);

            var subject =
                "Your Scholarship Interview Has Been Scheduled";

            var locationSection = string.IsNullOrWhiteSpace(location)
                ? ""
                : $"<p><strong>Location:</strong> {location}</p>";

            var meetingLinkSection = string.IsNullOrWhiteSpace(meetingLink)
                ? ""
                : $"<p><strong>Meeting Link:</strong> " +
                  $"<a href=\"{meetingLink}\">Join Interview</a></p>";

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = subject,
                html =
                    $"<p>Hi {fullName},</p>" +

                    $"<p>We are pleased to inform you that your " +
                    $"scholarship interview has been scheduled.</p>" +

                    $"<p><strong>Scholarship:</strong> " +
                    $"{scholarshipTitle}</p>" +

                    $"<p><strong>Interview Date:</strong> " +
                    $"{interviewDate:dd/MM/yyyy}</p>" +

                    $"<p><strong>Interview Time:</strong> " +
                    $"{interviewTime}</p>" +

                    $"<p><strong>Interview Method:</strong> " +
                    $"{interviewMethod}</p>" +

                    locationSection +

                    meetingLinkSection +

                    $"<p>Please make sure you are available at the scheduled " +
                    $"date and time.</p>" +

                    $"<p>If you have any questions regarding your interview, " +
                    $"please contact the scholarship administration team.</p>" +

                    $"<p>Regards,<br>" +
                    $"Scholarship Management System</p>"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInterviews()
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var interviews = await _db.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Student)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Scholarship)
                .OrderBy(i => i.InterviewDate)
                .ThenBy(i => i.InterviewTime)
                .ToListAsync();

            return Ok(interviews.Select(i => new
            {
                i.Id,
                i.ApplicationId,

                StudentName = i.Application.Student.FullName,
                StudentEmail = i.Application.Student.Email,

                ScholarshipTitle = i.Application.Scholarship.Title,

                i.InterviewDate,
                i.InterviewTime,
                i.InterviewMethod,
                i.Location,
                i.MeetingLink,
                i.Notes,

                ApplicationStatus =
                    ToStatusString(i.Application.Status)
            }));
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
                .ToListAsync();

            var upcoming = interviews
            .Where(i =>
            {
                if (!TimeSpan.TryParse(i.InterviewTime, out var time))
                    return false;

                var interviewDateTime =
                    i.InterviewDate.Date.Add(time);

                return interviewDateTime > DateTime.Now;
            })
            .ToList();

            return Ok(upcoming.Select(i => new
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
                i.MeetingLink
            }));
        }

        // ============================================================
        // GET PAST INTERVIEWS
        // ============================================================

        [HttpGet("past")]
        public async Task<IActionResult> GetPastInterviews()
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var interviews = await _db.Interviews
               .Include(i => i.Application)
                   .ThenInclude(a => a.Student)
               .Include(i => i.Application)
                   .ThenInclude(a => a.Scholarship)
               .OrderByDescending(i => i.InterviewDate)
               .ThenByDescending(i => i.InterviewTime)
               .ToListAsync();

            var past = interviews
                .Where(i =>
                {
                    if (!TimeSpan.TryParse(i.InterviewTime, out var time))
                        return false;

                    var interviewDateTime =
                        i.InterviewDate.Date.Add(time);

                    return interviewDateTime <= DateTime.Now;
                })
                .OrderByDescending(i => i.InterviewDate)
                .ThenByDescending(i => i.InterviewTime)
                .ToList();

            return Ok(past.Select(i => new
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
                i.Notes,

                ApplicationStatus =
                    ToStatusString(i.Application.Status)
            }));
        }

        // ============================================================
        // UPDATE INTERVIEW NOTES
        // ============================================================

        [HttpPut("{id}/notes")]
        public async Task<IActionResult> UpdateInterviewNotes(
            int id,
            [FromBody] UpdateInterviewNotesRequest request)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var interview = await _db.Interviews
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview is null)
                return NotFound("Interview not found.");

            interview.Notes = request.Notes;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                interview.Id,
                interview.Notes
            });
        }

        // ============================================================
        // FINAL APPLICATION DECISION
        // APPROVE / REJECT AFTER INTERVIEW
        // ============================================================

        [HttpPost("{id}/decision")]
        public async Task<IActionResult> MakeFinalDecision(
            int id,
            [FromQuery] ApplicationStatus status)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var interview = await _db.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.Student)
                .Include(i => i.Application)
                    .ThenInclude(a => a.Scholarship)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview is null)
                return NotFound("Interview not found.");

            var application = interview.Application;

            if (!TimeSpan.TryParse(interview.InterviewTime, out var interviewTime))
            {
                return BadRequest("Invalid interview time format.");
            }

            var interviewDateTime =
                interview.InterviewDate.Date.Add(interviewTime);


            if (DateTime.Now < interviewDateTime)
            {
                return BadRequest(
                    "The interview has not taken place yet.");
            }

            // Only Approved or Rejected are allowed
            if (status != ApplicationStatus.Approved &&
                status != ApplicationStatus.Rejected)
            {
                return BadRequest(
                    "Application can only be Approved or Rejected.");
            }

            // Only shortlisted applications should reach this stage
            if (application.Status != ApplicationStatus.Shortlisted)
            {
                return BadRequest(
                    "Only shortlisted applications can receive a final decision.");
            }

            application.Status = status;
            await _auditLogService.LogAsync(
                officer,
                $"Application #{application.Id} status changed to {ToStatusString(status)}"
            );
            application.ReviewedByUserId = officer.Id;
            application.DecisionAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Send email
            try
            {
                await SendFinalDecisionEmailAsync(
                    application.Student.Email,
                    application.Student.FullName,
                    application.Scholarship.Title,
                    status);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Failed to send final decision email: {ex.Message}");
            }

            return Ok(new
            {
                application.Id,
                Status = ToStatusString(application.Status),
                application.DecisionAt
            });
        }

        private async Task SendFinalDecisionEmailAsync(
    string toEmail,
    string fullName,
    string scholarshipTitle,
    ApplicationStatus status)
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri("https://api.resend.com/");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _config["Resend:ApiKey"]);

            string subject;
            string message;

            if (status == ApplicationStatus.Approved)
            {
                subject = "Congratulations! Your Scholarship Application Has Been Approved";

                message =
                    $"We are pleased to inform you that your application for " +
                    $"<strong>{scholarshipTitle}</strong> has been approved.";
            }
            else
            {
                subject = "Scholarship Application Final Decision";

                message =
                    $"We regret to inform you that your application for " +
                    $"<strong>{scholarshipTitle}</strong> has not been successful.";
            }

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = subject,
                html =
                    $"<p>Hi {fullName},</p>" +
                    $"<p>{message}</p>" +
                    $"<p>Thank you for applying for the scholarship.</p>" +
                    $"<p>Please log in to the Scholarship Management System " +
                    $"to view your application details.</p>" +
                    $"<p>Regards,<br>Scholarship Management System</p>"
            });
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

    // ============================================================
    // UPDATE INTERVIEW NOTES REQUEST
    // ============================================================

    public class UpdateInterviewNotesRequest
    {
        public string? Notes { get; set; }
    }
}