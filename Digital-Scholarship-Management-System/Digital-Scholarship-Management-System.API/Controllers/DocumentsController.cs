using Microsoft.AspNetCore.Mvc;
using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Digital_Scholarship_Management_System.API.Services;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]

    [Route("api/documents")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private static readonly Dictionary<string, byte[][]> FileSignatures = new()
        {
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        };

        private readonly AppDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;
        private readonly ILogger<DocumentsController> _logger;
        private readonly AuditLogService _auditLog;

        public DocumentsController(AppDbContext db, IAmazonS3 s3, IConfiguration configuration, ILogger<DocumentsController> logger, AuditLogService auditLog)
        {
            _db = db;
            _s3 = s3;
            _bucketName = configuration["S3:BucketName"]!;
            _logger = logger;
            _auditLog = auditLog;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDocuments()
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var documents = await _db.StudentDocuments
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.UploadAt)
                .ToListAsync();

            return Ok(documents.Select(ToResponse));
        }

        [HttpPost]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] DocumentType documentType)
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            if (file is null || file.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest("File exceeds the 10 MB size limit.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest("Unsupported file type. Allowed types; .pdf, .doc, .docx, .png, .jpg, .jpeg");
            }

            await using var stream = file.OpenReadStream();
            var header = new byte[8];
            await stream.ReadAsync(header, 0, header.Length);
            stream.Position = 0;

            if (!HasValidSignature(header, extension))
            {
                return BadRequest("File content does not match its extension.");
            }

            var key = $"studentprofile/{user.Id}/{Guid.NewGuid()}{extension}";

            try
            {
                await _s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload document to S3 for user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Could not upload the file. Please try again.");
            }

            // Creating Document
            var document = new StudentDocument
            {
                UserId = user.Id,
                DocumentType = documentType,
                S3ObjectKey = key,
                FileName = file.FileName,
                FileType = file.ContentType,
                UploadAt = DateTime.UtcNow,
            };
            try
            {
                _db.StudentDocuments.Add(document);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save document record for user {UserId}; rolling back S3 upload", user.Id);
                await _s3.DeleteObjectAsync(_bucketName, key);
                return StatusCode(StatusCodes.Status500InternalServerError, "Could not save the document. Please try again.");
            }

            await _auditLog.LogAsync(user, $"Uploaded document: {file.FileName}");
            return Ok(ToResponse(document));
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetDownloadUrl(int id)
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var document = await _db.StudentDocuments.FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);

            if (document is null)
            {
                return NotFound();
            }

            var url = await _s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = document.S3ObjectKey,
                Expires = DateTime.UtcNow.AddMinutes(15),
            });

            return Ok(new { url });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (user, errorResult) = await FindCurrentStudentAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var document = await _db.StudentDocuments.FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);

            if (document is null)
            {
                return NotFound();
            }

            await _s3.DeleteObjectAsync(_bucketName, document.S3ObjectKey);

            _db.StudentDocuments.Remove(document);
            await _db.SaveChangesAsync();

            return NoContent();
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

        private static bool HasValidSignature(byte[] header, string extension)
        {
            if (!FileSignatures.TryGetValue(extension, out var signatures))
            {
                return false;
            }

            return signatures.Any(sig => header.Length >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));
        }

        private static object ToResponse(StudentDocument document)
        {
            return new
            {
                document.Id,
                DocumentType = document.DocumentType.ToString().ToLowerInvariant(),
                document.FileName,
                document.FileType,
                document.UploadAt,
            };
        }
    }
}
