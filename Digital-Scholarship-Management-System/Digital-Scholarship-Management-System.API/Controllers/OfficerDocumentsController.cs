using Amazon.S3;
using Amazon.S3.Model;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    [ApiController]
    [Route("api/officer/documents")]
    [Authorize(Roles = "officer")]
    public class OfficerDocumentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public OfficerDocumentsController(AppDbContext db, IAmazonS3 s3, IConfiguration config)
        {
            _db = db;
            _s3 = s3;
            _bucketName = config["S3:BucketName"]!;
        }

        // List all student documents
        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            var documents = await _db.Documents
                .Include(d => d.User)
                .OrderByDescending(d => d.UploadAt)
                .ToListAsync();

            return Ok(documents.Select(d => new {
                d.Id,
                d.FileName,
                d.FileType,
                d.UploadAt,
                d.DocumentType,
                d.Status,
                StudentName = d.User!.FullName,
                StudentEmail = d.User.Email
            }));
        }

        // Approve or Reject a document
        [HttpPost("{id}/review")]
        public async Task<IActionResult> ReviewDocument(int id, [FromQuery] ReviewStatus status)
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null) return NotFound();

            document.Status = status;
            document.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { document.Id, document.Status, document.ReviewedAt });
        }

        // Officer can download any student document
        [HttpGet("{id}/download")]
        public async Task<IActionResult> GetDownloadUrl(int id)
        {
            var document = await _db.Documents.FindAsync(id);
            if (document == null) return NotFound();

            var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = document.S3ObjectKey,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            return Ok(new { url });
        }
    }
}