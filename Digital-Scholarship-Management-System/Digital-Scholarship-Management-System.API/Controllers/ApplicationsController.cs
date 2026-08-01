using System.ComponentModel.DataAnnotations;
using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
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

        public ApplicationsController(AppDbContext db, IAmazonS3 s3, IConfiguration configuration, ILogger<ApplicationsController> logger)
        {
            _db = db;
            _s3 = s3;
            _bucketName = configuration["S3:BucketName"]!;
            _logger = logger;
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

        private static string ToStatusString(ApplicationStatus status) => status switch
        {
            ApplicationStatus.Pending => "pending",
            ApplicationStatus.UnderReview => "under_review",
            ApplicationStatus.Approved => "approved",
            ApplicationStatus.Rejected => "rejected",
            _ => status.ToString().ToLowerInvariant(),
        };

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
