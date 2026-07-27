using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    // Global reference data. Admin owns full CRUD; Sponsor rules reference it.
    [Route("api/reference-data")]
    [ApiController]
    public class ReferenceDataController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReferenceDataController(AppDbContext db) => _db = db;

        // GET /api/reference-data?category=University&activeOnly=true
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ReferenceCategory? category, [FromQuery] bool activeOnly = false)
        {
            var query = _db.ReferenceData.AsQueryable();
            if (category is not null) query = query.Where(r => r.Category == category.Value);
            if (activeOnly) query = query.Where(r => r.IsActive);
            var items = await query.OrderBy(r => r.Category).ThenBy(r => r.Label).ToListAsync();
            return Ok(items);
        }

        // POST /api/reference-data
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReferenceDataRequest req)
        {
            var item = new ReferenceData
            {
                Category = req.Category,
                Code = req.Code.Trim(),
                Label = req.Label.Trim(),
                IsActive = req.IsActive,
            };
            _db.ReferenceData.Add(item);
            await _db.SaveChangesAsync();
            return Created($"/api/reference-data/{item.Id}", item);
        }

        // PUT /api/reference-data/{id} — update fields / toggle active
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ReferenceDataRequest req)
        {
            var item = await _db.ReferenceData.FindAsync(id);
            if (item is null) return NotFound();
            item.Category = req.Category;
            item.Code = req.Code.Trim();
            item.Label = req.Label.Trim();
            item.IsActive = req.IsActive;
            await _db.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE /api/reference-data/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ReferenceData.FindAsync(id);
            if (item is null) return NotFound();
            _db.ReferenceData.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public record ReferenceDataRequest(ReferenceCategory Category, string Code, string Label, bool IsActive);
}
