using System.ComponentModel.DataAnnotations;
using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Digital_Scholarship_Management_System.API.Services;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    public class DisburseRequest
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Disbursed amount must be a positive value.")]
        public decimal DisbursedAmount { get; set; }
    }
    public class CreateApplicationRequest
    {
        [Required]
        public int ScholarshipId { get; set; }
        public List<int> ExistingDocumentIds { get; set; } = new();
    }

    [ApiController]
    [Route("api/applications")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;
        private readonly ILogger<ApplicationsController> _logger;
        private readonly AuditLogService _auditLog;

        public ApplicationsController(AppDbContext db, IAmazonS3 s3, IConfiguration configuration, ILogger<ApplicationsController> logger, AuditLogService auditLog)
        {
            _db = db;
            _s3 = s3;
            _bucketName = configuration["S3:BucketName"]!;
            _logger = logger;
            _auditLog = auditLog;
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] CreateApplicationRequest request)
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var scholarship = await _db.Scholarships.FirstOrDefaultAsync(s => s.Id == request.ScholarshipId);
            if (scholarship is null)
            {
                return NotFound("Scholarship not found.");
            }

            if (scholarship.Deadline < DateTime.UtcNow)
            {
                return BadRequest("The application deadline for this scholarship has passed.");
            }

            var alreadyApplied = await _db.Applications.AnyAsync(a => a.StudentId == user.Id && a.ScholarshipId == request.ScholarshipId);
            if (alreadyApplied)
            {
                return Conflict("You have already applied to this scholarship.");
            }

            var selectedDocuments = await _db.StudentDocuments
                .Where(d => request.ExistingDocumentIds.Contains(d.Id) && d.UserId == user.Id)
                .ToListAsync();

            if (selectedDocuments.Count != request.ExistingDocumentIds.Count)
            {
                return BadRequest("One or more selected documents could not be found.");
            }

            var application = new Application
            {
                StudentId = user.Id,
                ScholarshipId = request.ScholarshipId,
                Status = ApplicationStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
            };

            _db.Applications.Add(application);
            await _db.SaveChangesAsync();

            var copiedKeys = new List<string>();
            var newApplicationDocuments = new List<ApplicationDocument>();

            try
            {
                foreach (var sourceDocument in selectedDocuments)
                {
                    var extension = Path.GetExtension(sourceDocument.S3ObjectKey);
                    var newKey = $"applications/{application.Id}/{Guid.NewGuid()}{extension}";

                    await _s3.CopyObjectAsync(new CopyObjectRequest
                    {
                        SourceBucket = _bucketName,
                        SourceKey = sourceDocument.S3ObjectKey,
                        DestinationBucket = _bucketName,
                        DestinationKey = newKey,
                    });
                    copiedKeys.Add(newKey);

                    newApplicationDocuments.Add(new ApplicationDocument
                    {
                        ApplicationId = application.Id,
                        DocumentType = sourceDocument.DocumentType,
                        S3ObjectKey = newKey,
                        FileName = sourceDocument.FileName,
                        FileType = sourceDocument.FileType,
                        UploadAt = DateTime.UtcNow,
                    });
                }

                _db.ApplicationDocuments.AddRange(newApplicationDocuments);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach documents to application {ApplicationId}; rolling back", application.Id);

                foreach (var key in copiedKeys)
                {
                    await _s3.DeleteObjectAsync(_bucketName, key);
                }
                _db.Applications.Remove(application);
                await _db.SaveChangesAsync();

                return StatusCode(StatusCodes.Status500InternalServerError, "Could not submit your application. Please try again.");
            }
            await _auditLog.LogAsync(user, $"Submitted application for '{scholarship.Title}'");
            return Ok(new { application.Id, message = "Application submitted successfully." });
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var applications = await _db.Applications
                .Where(a => a.StudentId == user.Id)
                .Include(a => a.Scholarship)
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            return Ok(applications.Select(a => new
            {
                a.Id,
                a.ScholarshipId,
                ScholarshipTitle = a.Scholarship.Title,
                Status = ToStatusString(a.Status),
                a.SubmittedAt,
                a.DecisionAt,
            }));
        }

        [HttpGet("sponsor/pending-disbursements")]
        [Authorize]
        public async Task<IActionResult> GetPendingDisbursements()
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var applications = await _db.Applications
                .Include(a => a.Scholarship)
                .Include(a => a.Student)
                .Where(a => a.Scholarship.SponsorId == sponsor.Id
                         && a.Status == ApplicationStatus.Approved
                         && a.DisbursementStatus == DisbursementStatus.NotDisbursed)
                .OrderBy(a => a.DecisionAt)
                .ToListAsync();

            return Ok(applications.Select(a => new
            {
                a.Id,
                a.ScholarshipId,
                ScholarshipTitle = a.Scholarship.Title,
                a.Scholarship.FundType,
                a.Scholarship.FundingAmount,
                a.StudentId,
                StudentName = a.Student.FullName,
                a.DecisionAt,
            }));
        }

        [HttpPost("{id}/disburse")]
        [Authorize]
        public async Task<IActionResult> Disburse(int id, [FromBody] DisburseRequest request)
        {
            var (sponsor, errorResult) = await FindCurrentSponsorAsync();
            if (sponsor is null)
            {
                return errorResult!;
            }

            var application = await _db.Applications
                .Include(a => a.Scholarship)
                .FirstOrDefaultAsync(a => a.Id == id && a.Scholarship.SponsorId == sponsor.Id);

            if (application is null)
            {
                return NotFound("Application not found");
            }

            if (application.Status != ApplicationStatus.Approved)
            {
                return BadRequest("Only approved applications can be disbursed.");
            }

            if (application.DisbursementStatus != DisbursementStatus.NotDisbursed)
            {
                return BadRequest("This application has already been disbursed.");
            }

            application.DisbursementStatus = DisbursementStatus.Disbursed;
            application.DisbursedAmount = request.DisbursedAmount;
            application.DisbursedAt = DateTime.UtcNow;
            application.DisbursedByUserId = sponsor.Id;

            if (application.Scholarship.FundType == "Full Scholarship")
            {
                application.Scholarship.FundingAmount = request.DisbursedAmount;
            }

            await _db.SaveChangesAsync();

            await _auditLog.LogAsync(sponsor, $"Disbursed RM{request.DisbursedAmount} to application ID {application.Id} for '{application.Scholarship.Title}'");

            return Ok(new { application.Id, message = "Disbursement recorded." });
        }

        private static string ToStatusString(ApplicationStatus status) => status switch
        {
            ApplicationStatus.Pending => "pending",
            ApplicationStatus.UnderReview => "under_review",
            ApplicationStatus.Approved => "approved",
            ApplicationStatus.Rejected => "rejected",
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


        private async Task<(User? User, IActionResult? Error)> FindCurrentStudentAsync()
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

            if (user.Role != UserRole.user)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Only student accounts can access this feature."));
            }
            return (user, null);
        }
    }
}
