
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.DataBase;
using MisaTracker.Core.ORM.Models;

namespace MisaTrackerr.Server.Controllers
{
    [ApiController]
    [Route("api/subjects")]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;

        public SubjectsController(IDbContextFactory<DbConnection> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        // GET: api/subjects
        [HttpGet]
        public async Task<IActionResult> GetSubjects()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var subjects = await db.Subjects
                .AsNoTracking()
                .Where(s => s.UserId == userId.Value)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    FolderCount = s.Folders.Count,
                    TaskCount = s.Tasks.Count
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // GET: api/subjects/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSubject(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var subject = await db.Subjects
                .AsNoTracking()
                .Where(s => s.Id == id && s.UserId == userId.Value)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    Folders = s.Folders
                        .OrderBy(f => f.Name)
                        .Select(f => new
                        {
                            f.Id,
                            f.Name,
                            f.SubjectId
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (subject == null)
                return NotFound(new { message = "Предмет не найден." });

            return Ok(subject);
        }

        // POST: api/subjects
        [HttpPost]
        public async Task<IActionResult> CreateSubject(
            [FromBody] CreateSubjectRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var name = request.Name.Trim();
            await using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.Subjects.AnyAsync(s =>
                s.UserId == userId.Value && s.Name == name);

            if (exists)
                return Conflict(new { message = "Такой предмет уже существует." });

            var subject = new Subject
            {
                Name = name,
                UserId = userId.Value
            };

            db.Subjects.Add(subject);
            await db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetSubject),
                new { id = subject.Id },
                new { subject.Id, subject.Name });
        }

        // PUT: api/subjects/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSubject(
            int id,
            [FromBody] CreateSubjectRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var name = request.Name.Trim();
            await using var db = await _contextFactory.CreateDbContextAsync();

            var subject = await db.Subjects.FirstOrDefaultAsync(s =>
                s.Id == id && s.UserId == userId.Value);

            if (subject == null)
                return NotFound(new { message = "Предмет не найден." });

            var duplicate = await db.Subjects.AnyAsync(s =>
                s.UserId == userId.Value &&
                s.Id != id &&
                s.Name == name);

            if (duplicate)
                return Conflict(new { message = "Такой предмет уже существует." });

            subject.Name = name;
            await db.SaveChangesAsync();

            return Ok(new { subject.Id, subject.Name });
        }

        // DELETE: api/subjects/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var subject = await db.Subjects.FirstOrDefaultAsync(s =>
                s.Id == id && s.UserId == userId.Value);

            if (subject == null)
                return NotFound(new { message = "Предмет не найден." });

            // Не удаляем предмет, если у него есть задачи или папки.
            var hasTasks = await db.Tasks.AnyAsync(t => t.SubjectId == id);
            var hasFolders = await db.Folders.AnyAsync(f => f.SubjectId == id);

            if (hasTasks || hasFolders)
            {
                return Conflict(new
                {
                    message = "Сначала удали задачи и папки этого предмета."
                });
            }

            db.Subjects.Remove(subject);
            await db.SaveChangesAsync();

            return Ok(new { message = "Предмет удалён." });
        }
    }

    public class CreateSubjectRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}
