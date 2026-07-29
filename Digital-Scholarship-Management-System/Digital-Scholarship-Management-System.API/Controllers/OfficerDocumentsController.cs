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
    [Authorize]
    public class OfficerDocumentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public OfficerDocumentsController(
            AppDbContext db,
            IAmazonS3 s3,
            IConfiguration configuration)
        {
            _db = db;
            _s3 = s3;
            _bucketName = configuration["S3:BucketName"]!;
        }


        // Get documents uploaded by a specific student
        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetStudentDocuments(int studentId)
        {
            var officer = await GetCurrentOfficer();

            if (officer == null)
            {
                return Unauthorized();
            }

            var documents = await _db.Documents
                .Where(d => d.UserId == studentId)
                .OrderByDescending(d => d.UploadAt)
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.FileType,
                    d.DocumentType,
                    d.UploadAt
                })
                .ToListAsync();

            return Ok(documents);
        }


        // Generate download link
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var officer = await GetCurrentOfficer();

            if (officer == null)
            {
                return Unauthorized();
            }

            var document = await _db.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }


            var url = await _s3.GetPreSignedURLAsync(
                new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = document.S3ObjectKey,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                });

            return Ok(new
            {
                url
            });
        }

        private async Task<User?> GetCurrentOfficer()
        {
            var sub = User.FindFirst("sub")?.Value;

            if (sub == null)
                return null;

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.CognitoSub == sub);

            if (user == null || user.Role != UserRole.officer)
                return null;

            return user;
        }
    }
}