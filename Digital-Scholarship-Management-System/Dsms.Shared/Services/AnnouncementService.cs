using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Services
{
    // Announcements live in SQL alongside the rest of the business data. DynamoDB keeps only the
    // audit log, whose append-only, query-by-date shape is what it is good at.
    public class AnnouncementService
    {
        private readonly AppDbContext _db;

        public AnnouncementService(AppDbContext db) => _db = db;

        public async Task<AnnouncementItem> CreateAsync(
            string title,
            string body,
            AnnouncementAudience audience,
            AnnouncementStatus status,
            string createdBy)
        {
            var now = DateTime.UtcNow;
            var announcement = new Announcement
            {
                Title = title,
                Body = body,
                Audience = audience,
                Status = status,
                // Stamp only at publish; drafts keep it null
                PublishedAt = status == AnnouncementStatus.Published ? now : null,
                CreatedAt = now,
                CreatedBy = createdBy,
            };

            _db.Announcements.Add(announcement);
            await _db.SaveChangesAsync();

            return Map(announcement);
        }

        // Admin view — every status, every audience.
        public async Task<List<AnnouncementItem>> ListAllAsync() =>
            (await _db.Announcements
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync())
            .Select(Map)
            .ToList();

        // Published items for one role: the All audience plus the caller's own. Admin has no
        // audience of its own, so it sees All only.
        public async Task<List<AnnouncementItem>> FeedAsync(AnnouncementAudience? audience, int limit) =>
            (await _db.Announcements
                .AsNoTracking()
                .Where(a => a.Status == AnnouncementStatus.Published)
                .Where(a => a.Audience == AnnouncementAudience.All ||
                            (audience != null && a.Audience == audience))
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync())
            .Select(Map)
            .ToList();

        public async Task<AnnouncementItem?> GetAsync(int id)
        {
            var announcement = await _db.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            return announcement is null ? null : Map(announcement);
        }

        public async Task<AnnouncementItem?> UpdateAsync(
            int id,
            string title,
            string body,
            AnnouncementStatus status,
            DateTime? publishedAt)
        {
            var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id);
            if (announcement is null)
            {
                return null;
            }

            announcement.Title = title;
            announcement.Body = body;
            announcement.Status = status;
            announcement.PublishedAt = publishedAt;
            await _db.SaveChangesAsync();

            return Map(announcement);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id);
            if (announcement is null)
            {
                return false;
            }

            _db.Announcements.Remove(announcement);
            await _db.SaveChangesAsync();
            return true;
        }

        // Idempotent: the unique index on (UserId, AnnouncementId) means a repeated click adds
        // nothing rather than duplicating the marker.
        public async Task MarkReadAsync(int userId, int announcementId)
        {
            var alreadyRead = await _db.AnnouncementReads
                .AnyAsync(r => r.UserId == userId && r.AnnouncementId == announcementId);
            if (alreadyRead)
            {
                return;
            }

            _db.AnnouncementReads.Add(new AnnouncementRead
            {
                UserId = userId,
                AnnouncementId = announcementId,
                ReadAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
        }

        // Used when an announcement is unarchived. Leaving the markers would bring it back
        // already-read for everyone who saw it before, so nobody would notice it returned.
        public async Task ClearReadsAsync(int announcementId)
        {
            await _db.AnnouncementReads
                .Where(r => r.AnnouncementId == announcementId)
                .ExecuteDeleteAsync();
        }

        // Everything this user has read, resolved server-side so the browser never receives
        // another user's markers.
        public async Task<HashSet<int>> ReadIdsAsync(int userId) =>
            (await _db.AnnouncementReads
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => r.AnnouncementId)
                .ToListAsync())
            .ToHashSet();

        private static AnnouncementItem Map(Announcement a) => new(
            a.Id,
            a.Title,
            a.Body,
            a.Audience,
            a.Status,
            a.PublishedAt,
            a.CreatedAt,
            a.CreatedBy);
    }

    public record AnnouncementItem(
        int Id,
        string Title,
        string Body,
        AnnouncementAudience Audience,
        AnnouncementStatus Status,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        string CreatedBy);
}
