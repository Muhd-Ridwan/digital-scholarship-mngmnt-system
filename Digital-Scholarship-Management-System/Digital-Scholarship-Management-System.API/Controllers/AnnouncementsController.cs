using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Platform announcements. Lifecycle: Draft -> Published -> Archived.
    [Route("api/announcements")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AnnouncementsController(AppDbContext db) => _db = db;

        // GET /api/announcements — all statuses (admin view)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.Announcements.OrderByDescending(a => a.Id).ToListAsync();
            return Ok(items);
        }

        // GET /api/announcements/feed?audience=Student — published only, audience-filtered
        // audience is a temporary query param; it should come from the JWT role claim once auth is in
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] AnnouncementAudience? audience)
        {
            var query = _db.Announcements.Where(a => a.Status == AnnouncementStatus.Published);
            if (audience is not null)
                query = query.Where(a => a.Audience == AnnouncementAudience.All || a.Audience == audience.Value);
            var items = await query.OrderByDescending(a => a.PublishedAt).ToListAsync();
            return Ok(items);
        }

        // POST /api/announcements — create as Draft or Published
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest req)
        {
            var status = req.Status == AnnouncementStatus.Archived ? AnnouncementStatus.Draft : req.Status;
            var item = new Announcement
            {
                Title = req.Title.Trim(),
                Body = req.Body.Trim(),
                Audience = req.Audience,
                Status = status,
                // Stamp only at publish; drafts keep it null
                PublishedAt = status == AnnouncementStatus.Published ? DateTime.UtcNow : null,
            };
            _db.Announcements.Add(item);
            await _db.SaveChangesAsync();
            return Created($"/api/announcements/{item.Id}", item);
        }

        // PUT /api/announcements/{id} — status transition and/or edit
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAnnouncementRequest req)
        {
            var item = await _db.Announcements.FindAsync(id);
            if (item is null) return NotFound();

            if (req.Title is not null) item.Title = req.Title.Trim();
            if (req.Body is not null) item.Body = req.Body.Trim();
            if (req.Audience is not null) item.Audience = req.Audience.Value;

            if (req.Status is not null && req.Status.Value != item.Status)
            {
                // Publishing stamps the date; archiving keeps it
                if (req.Status.Value == AnnouncementStatus.Published && item.PublishedAt is null)
                    item.PublishedAt = DateTime.UtcNow;
                item.Status = req.Status.Value;
            }

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE /api/announcements/{id} — only a Draft may be hard-deleted
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Announcements.FindAsync(id);
            if (item is null) return NotFound();
            if (item.Status != AnnouncementStatus.Draft)
                return Conflict(new { message = "Only draft announcements can be deleted; published ones must be archived." });
            _db.Announcements.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public record CreateAnnouncementRequest(string Title, string Body, AnnouncementAudience Audience, AnnouncementStatus Status);
    public record UpdateAnnouncementRequest(string? Title, string? Body, AnnouncementAudience? Audience, AnnouncementStatus? Status);
}
