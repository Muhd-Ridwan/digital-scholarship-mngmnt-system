using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]
    [Route("api/officer/applications")]
    [Authorize]
    public class OfficerApplicationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public OfficerApplicationsController(
            AppDbContext db,
            IAmazonS3 s3,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _s3 = s3;
            _bucketName = config["S3:BucketName"]!;
            _config = config;
            _httpClientFactory = httpClientFactory;
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
        // GET ALL APPLICATIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetAllApplications()
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var applications = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.Scholarship)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return Ok(applications.Select(a => new
            {
                a.Id,

                StudentName = a.Student.FullName,
                StudentEmail = a.Student.Email,

                ScholarshipId = a.ScholarshipId,
                ScholarshipTitle = a.Scholarship.Title,

                Status = ToStatusString(a.Status),

                a.SubmittedAt,
                a.DecisionAt,
                a.ReviewNotes
            }));
        }

        // ============================================================
        // GET ONE APPLICATION + DOCUMENTS
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetApplication(int id)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var application = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.Scholarship)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application is null)
                return NotFound("Application not found.");

            return Ok(new
            {
                application.Id,

                Student = new
                {
                    application.Student.Id,
                    application.Student.FullName,
                    application.Student.Email
                },

                Scholarship = new
                {
                    application.Scholarship.Id,
                    application.Scholarship.Title
                },

                Status = ToStatusString(application.Status),

                application.ReviewNotes,
                application.SubmittedAt,
                application.DecisionAt,

                Documents = application.Documents.Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.FileType,
                    DocumentType = d.DocumentType.ToString(),
                    d.UploadAt
                })
            });
        }

        // ============================================================
        // START REVIEW
        // Pending → Under Review
        // ============================================================

        [HttpPost("{id}/start-review")]
        public async Task<IActionResult> StartReview(int id)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var application = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.Scholarship)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application is null)
                return NotFound("Application not found.");

            // Only pending applications can be moved to Under Review
            if (application.Status != ApplicationStatus.Pending)
            {
                return BadRequest(
                    "Only pending applications can be moved to under review.");
            }

            application.Status = ApplicationStatus.UnderReview;

            await _db.SaveChangesAsync();

            await SendApplicationStatusEmailAsync(
                application.Student.Email,
                application.Student.FullName,
                application.Scholarship.Title,
                "under_review");

            return Ok(new
            {
                application.Id,
                Status = ToStatusString(application.Status)
            });
        }

        // ============================================================
        // SHORTLIST / REJECT APPLICATION
        // Under Review → Shortlisted / Rejected
        // ============================================================

        [HttpPost("{id}/decision")]
        public async Task<IActionResult> MakeApplicationDecision(
            int id,
            [FromQuery] ApplicationStatus status)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var application = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.Scholarship)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application is null)
                return NotFound("Application not found.");

            // Only Under Review applications can be decided
            if (application.Status != ApplicationStatus.UnderReview)
            {
                return BadRequest(
                    "Only applications under review can be shortlisted or rejected.");
            }

            // Only Shortlisted or Rejected are allowed here
            if (status != ApplicationStatus.Shortlisted &&
                status != ApplicationStatus.Rejected)
            {
                return BadRequest(
                    "Application can only be Shortlisted or Rejected at this stage.");
            }

            application.Status = status;
            application.ReviewedByUserId = officer.Id;
            application.DecisionAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await SendApplicationStatusEmailAsync(
                application.Student.Email,
                application.Student.FullName,
                application.Scholarship.Title,
                ToStatusString(status));

            return Ok(new
            {
                application.Id,
                Status = ToStatusString(application.Status),
                application.ReviewedByUserId,
                application.DecisionAt
            });
        }

        // ============================================================
        // UNDO APPLICATION DECISION
        // Shortlisted / Rejected → Under Review
        // ============================================================

        [HttpPost("{id}/undo-decision")]
        public async Task<IActionResult> UndoApplicationDecision(int id)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var application = await _db.Applications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application is null)
                return NotFound("Application not found.");

            // Only Shortlisted or Rejected applications can be undone
            if (application.Status != ApplicationStatus.Shortlisted &&
                application.Status != ApplicationStatus.Rejected)
            {
                return BadRequest(
                    "There is no shortlist or rejection decision to undo.");
            }

            application.Status = ApplicationStatus.UnderReview;

            application.ReviewedByUserId = null;
            application.DecisionAt = null;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                application.Id,
                Status = ToStatusString(application.Status),
                application.ReviewedByUserId,
                application.DecisionAt
            });
        }

        // ============================================================
        // VIEW APPLICATION DOCUMENT
        // ============================================================

        [HttpGet("documents/{documentId}/download")]
        public async Task<IActionResult> GetDocumentDownloadUrl(int documentId)
        {
            var officer = await GetCurrentOfficerAsync();

            if (officer is null)
                return Forbid();

            var document = await _db.ApplicationDocuments
                .Include(d => d.Application)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document is null)
                return NotFound("Document not found.");

            var url = _s3.GetPreSignedURL(
                new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = document.S3ObjectKey,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                });

            return Ok(new { url });
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

        private async Task SendApplicationStatusEmailAsync(
            string toEmail,
            string fullName,
            string scholarshipTitle,
            string status)
        {
            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri("https://api.resend.com/");

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _config["Resend:ApiKey"]);

            string subject;
            string message;

            switch (status)
            {
                case "under_review":
                    subject = "Your Scholarship Application is Under Review";
                    message =
                        $"Your application for <strong>{scholarshipTitle}</strong> " +
                        "is now under review by our scholarship officer.";
                    break;

                case "shortlisted":
                    subject = "Congratulations! Your Scholarship Application Has Been Shortlisted";
                    message =
                        $"We are pleased to inform you that your application for " +
                        $"<strong>{scholarshipTitle}</strong> has been shortlisted.";
                    break;

                case "rejected":
                    subject = "Scholarship Application Status Update";
                    message =
                        $"We regret to inform you that your application for " +
                        $"<strong>{scholarshipTitle}</strong> has not been successful.";
                    break;

                case "approved":
                    subject = "Congratulations! Your Scholarship Application Has Been Approved";
                    message =
                        $"Congratulations! Your application for " +
                        $"<strong>{scholarshipTitle}</strong> has been approved.";
                    break;

                default:
                    return;
            }

            await client.PostAsJsonAsync("emails", new
            {
                from = "scholarship@dev-r.org",
                to = new[] { toEmail },
                subject = subject,
                html =
                    $"<p>Hi {fullName},</p>" +
                    $"<p>{message}</p>" +
                    $"<p>Please log in to the Scholarship Management System " +
                    $"to view more details.</p>" +
                    $"<p>Regards,<br>Scholarship Management System</p>"
            });
        }
    }
}