
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
    [Route("api/folders")]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;

        public FoldersController(IDbContextFactory<DbConnection> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        // GET: api/folders?subjectId=1
        [HttpGet]
        public async Task<IActionResult> GetFolders([FromQuery] int subjectId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var subjectExists = await db.Subjects.AnyAsync(s =>
                s.Id == subjectId && s.UserId == userId.Value);

            if (!subjectExists)
                return NotFound(new { message = "Предмет не найден." });

            var folders = await db.Folders
                .AsNoTracking()
                .Where(f =>
                    f.SubjectId == subjectId &&
                    f.UserId == userId.Value)
                .OrderBy(f => f.Name)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.SubjectId,
                    TaskCount = f.Tasks.Count
                })
                .ToListAsync();

            return Ok(folders);
        }

        // POST: api/folders
        [HttpPost]
        public async Task<IActionResult> CreateFolder(
            [FromBody] CreateFolderRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var name = request.Name.Trim();
            await using var db = await _contextFactory.CreateDbContextAsync();

            var subjectExists = await db.Subjects.AnyAsync(s =>
                s.Id == request.SubjectId &&
                s.UserId == userId.Value);

            if (!subjectExists)
                return BadRequest(new { message = "Предмет не найден." });

            var exists = await db.Folders.AnyAsync(f =>
                f.UserId == userId.Value &&
                f.SubjectId == request.SubjectId &&
                f.Name == name);

            if (exists)
                return Conflict(new { message = "Такая папка уже существует." });

            var folder = new Folder
            {
                Name = name,
                UserId = userId.Value,
                SubjectId = request.SubjectId
            };

            db.Folders.Add(folder);
            await db.SaveChangesAsync();

            return Ok(new
            {
                folder.Id,
                folder.Name,
                folder.SubjectId
            });
        }

        // PUT: api/folders/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateFolder(
            int id,
            [FromBody] UpdateFolderRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var name = request.Name.Trim();
            await using var db = await _contextFactory.CreateDbContextAsync();

            var folder = await db.Folders.FirstOrDefaultAsync(f =>
                f.Id == id && f.UserId == userId.Value);

            if (folder == null)
                return NotFound(new { message = "Папка не найдена." });

            var duplicate = await db.Folders.AnyAsync(f =>
                f.Id != id &&
                f.UserId == userId.Value &&
                f.SubjectId == folder.SubjectId &&
                f.Name == name);

            if (duplicate)
                return Conflict(new { message = "Такая папка уже существует." });

            folder.Name = name;
            await db.SaveChangesAsync();

            return Ok(new
            {
                folder.Id,
                folder.Name,
                folder.SubjectId
            });
        }

        // DELETE: api/folders/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var folder = await db.Folders.FirstOrDefaultAsync(f =>
                f.Id == id && f.UserId == userId.Value);

            if (folder == null)
                return NotFound(new { message = "Папка не найдена." });

            var hasTasks = await db.Tasks.AnyAsync(t => t.FolderId == id);

            if (hasTasks)
            {
                return Conflict(new
                {
                    message = "Сначала перемести или удали задачи из этой папки."
                });
            }

            db.Folders.Remove(folder);
            await db.SaveChangesAsync();

            return Ok(new { message = "Папка удалена." });
        }
    }

    public class CreateFolderRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }
    }

    public class UpdateFolderRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}
