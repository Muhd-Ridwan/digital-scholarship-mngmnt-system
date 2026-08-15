using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Platform announcements, stored in SQL. Lifecycle: Draft -> Published -> Archived.
    [Route("api/announcements")]
    [ApiController]
    [Authorize]
    public class AnnouncementsController : ControllerBase
    {
        // The bell fetches 10 and renders 10 — the badge and the list agree at that number.
        private const int FeedLimit = 10;

        private readonly AppDbContext _db;
        private readonly AnnouncementService _announcements;
        private readonly AuditLogService _auditLog;

        public AnnouncementsController(
            AppDbContext db,
            AnnouncementService announcements,
            AuditLogService auditLog)
        {
            _db = db;
            _announcements = announcements;
            _auditLog = auditLog;
        }

        // GET /api/announcements — all statuses (admin view)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            return Ok(await _announcements.ListAllAsync());
        }

        // GET /api/announcements/feed — published only, filtered to the caller's own audience.
        // The audience comes from the caller's DB role, never a query param — a param lets the
        // caller choose whose announcements to read.
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed()
        {
            var (user, errorResult) = await FindCurrentUserAsync();
            if (user is null)
            {
                return errorResult!;
            }

            var items = await _announcements.FeedAsync(ToAudience(user.Role), FeedLimit);
            // The read flag is resolved server-side so the client just counts !read rather than
            // receiving every marker and doing set maths.
            var readIds = await _announcements.ReadIdsAsync(user.Id);

            return Ok(items.Select(i => new AnnouncementFeedItem(
                i.Id, i.Title, i.Body, i.Audience, i.Status,
                i.PublishedAt, i.CreatedAt, i.CreatedBy, readIds.Contains(i.Id))));
        }

        // POST /api/announcements/read — mark one announcement read for the calling user.
        // Any signed-in role: the marker is per-user and affects only their own unread count.
        [HttpPost("read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest req)
        {
            var (user, errorResult) = await FindCurrentUserAsync();
            if (user is null)
            {
                return errorResult!;
            }

            if (await _announcements.GetAsync(req.AnnouncementId) is null)
            {
                return NotFound();
            }

            await _announcements.MarkReadAsync(user.Id, req.AnnouncementId);
            return NoContent();
        }

        // POST /api/announcements — create as Draft or Published
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest req)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Body))
            {
                return BadRequest(new { message = "Title and body are required." });
            }

            var status = req.Status == AnnouncementStatus.Archived ? AnnouncementStatus.Draft : req.Status;

            var item = await _announcements.CreateAsync(
                req.Title.Trim(), req.Body.Trim(), req.Audience, status, admin.FullName);

            if (status == AnnouncementStatus.Published)
            {
                await _auditLog.LogAsync(admin, $"Published announcement \"{item.Title}\"");
            }

            return Created($"/api/announcements/{item.Id}", item);
        }

        // PUT /api/announcements/{id} — status transition and/or edit
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAnnouncementRequest req)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var item = await _announcements.GetAsync(id);
            if (item is null) return NotFound();

            var title = req.Title is not null ? req.Title.Trim() : item.Title;
            var body = req.Body is not null ? req.Body.Trim() : item.Body;
            var status = item.Status;
            var publishedAt = item.PublishedAt;

            if (req.Status is not null && req.Status.Value != item.Status)
            {
                if (!IsAllowedTransition(item.Status, req.Status.Value))
                {
                    return Conflict(new
                    {
                        message = $"Cannot move an announcement from {item.Status} to {req.Status.Value}.",
                    });
                }

                status = req.Status.Value;

                // Publishing stamps the date only if it was never stamped; archiving keeps it
                if (status == AnnouncementStatus.Published && publishedAt is null)
                {
                    publishedAt = DateTime.UtcNow;
                }
            }

            var isUnarchive = item.Status == AnnouncementStatus.Archived &&
                              status == AnnouncementStatus.Published;

            var updated = await _announcements.UpdateAsync(id, title, body, status, publishedAt);
            if (updated is null) return NotFound();

            // Without this it returns already-read for everyone who saw it before archiving.
            if (isUnarchive)
            {
                await _announcements.ClearReadsAsync(id);
            }

            if (status != item.Status &&
                (status == AnnouncementStatus.Published || status == AnnouncementStatus.Archived))
            {
                var verb = isUnarchive ? "Unarchived" : status.ToString();
                await _auditLog.LogAsync(admin, $"{verb} announcement \"{updated.Title}\"");
            }

            return Ok(updated);
        }

        // DELETE /api/announcements/{id} — only a Draft may be hard-deleted.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (admin, errorResult) = await FindCurrentAdminAsync();
            if (admin is null)
            {
                return errorResult!;
            }

            var item = await _announcements.GetAsync(id);
            if (item is null) return NotFound();

            if (item.Status != AnnouncementStatus.Draft)
            {
                return Conflict(new { message = "Only draft announcements can be deleted; published ones must be archived." });
            }

            await _announcements.DeleteAsync(id);
            return NoContent();
        }

        // Draft -> Published -> Archived, plus Archived -> Published to undo a takedown.
        // A draft can never be reached again once published — there is no unpublish.
        private static bool IsAllowedTransition(AnnouncementStatus from, AnnouncementStatus to) =>
            (from, to) switch
            {
                (AnnouncementStatus.Draft, AnnouncementStatus.Published) => true,
                (AnnouncementStatus.Published, AnnouncementStatus.Archived) => true,
                (AnnouncementStatus.Archived, AnnouncementStatus.Published) => true,
                _ => false,
            };

        // Any signed-in account with a DB row — every role reads the feed.
        private async Task<(User? User, IActionResult? Error)> FindCurrentUserAsync()
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

            return (user, null);
        }

        // Same pattern as UsersController.FindCurrentAdminAsync — the JWT carries no app-role
        // claim, so the role comes from the DB row the sub maps to.
        private async Task<(User? User, IActionResult? Error)> FindCurrentAdminAsync()
        {
            var (user, error) = await FindCurrentUserAsync();
            if (user is null)
            {
                return (null, error);
            }

            if (user.Role != UserRole.admin)
            {
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Only admin accounts can access this feature."));
            }

            return (user, null);
        }

        // UserRole.user is a student but the audience value is Student — mapping the two is what
        // keeps a student's feed from coming back empty. Admin has no audience of its own, so an
        // admin sees the All items.
        private static AnnouncementAudience? ToAudience(UserRole role) => role switch
        {
            UserRole.user => AnnouncementAudience.Student,
            UserRole.officer => AnnouncementAudience.Officer,
            UserRole.sponsor => AnnouncementAudience.Sponsor,
            _ => null,
        };
    }

    // The feed's shape differs from the admin list's: read is per-caller, so it only makes
    // sense here.
    public record AnnouncementFeedItem(
        int Id,
        string Title,
        string Body,
        AnnouncementAudience Audience,
        AnnouncementStatus Status,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        string CreatedBy,
        bool Read);

    public record MarkReadRequest(int AnnouncementId);

    public record CreateAnnouncementRequest(string Title, string Body, AnnouncementAudience Audience, AnnouncementStatus Status);
    public record UpdateAnnouncementRequest(string? Title, string? Body, AnnouncementStatus? Status);
}
