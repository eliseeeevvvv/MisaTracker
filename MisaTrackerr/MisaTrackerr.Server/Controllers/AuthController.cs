
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
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;

        public TasksController(IDbContextFactory<DbConnection> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(id, out var userId) ? userId : null;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var tasks = await db.Tasks
                .AsNoTracking()
                .Where(t => t.UserId == userId.Value)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    t.SubjectId,
                    t.FolderId
                })
                .ToListAsync();

            return Ok(tasks);
        }

        // GET: api/tasks/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var task = await db.Tasks
                .AsNoTracking()
                .Where(t => t.Id == id && t.UserId == userId.Value)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    t.SubjectId,
                    t.FolderId
                })
                .FirstOrDefaultAsync();

            if (task == null)
                return NotFound(new { message = "Задача не найдена." });

            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<IActionResult> CreateTask(
            [FromBody] CreateTaskRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Введите название задачи." });

            if (request.SubjectId <= 0 || request.FolderId <= 0)
                return BadRequest(new { message = "Выберите предмет и папку." });

            await using var db = await _contextFactory.CreateDbContextAsync();

            var subjectExists = await db.Subjects.AnyAsync(s =>
                s.Id == request.SubjectId &&
                s.UserId == userId.Value);

            if (!subjectExists)
                return BadRequest(new { message = "Предмет не найден." });

            var folderExists = await db.Folders.AnyAsync(f =>
                f.Id == request.FolderId &&
                f.UserId == userId.Value &&
                f.SubjectId == request.SubjectId);

            if (!folderExists)
                return BadRequest(new
                {
                    message = "Папка не найдена или не принадлежит этому предмету."
                });

            var task = new Tasks
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                DueDate = request.DueDate,
                IsCompleted = false,
                UserId = userId.Value,
                SubjectId = request.SubjectId,
                FolderId = request.FolderId
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.Name,
                task.Description,
                task.DueDate,
                task.IsCompleted,
                task.SubjectId,
                task.FolderId
            });
        }

        // PUT: api/tasks/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTask(
            int id,
            [FromBody] UpdateTaskRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Введите название задачи." });

            await using var db = await _contextFactory.CreateDbContextAsync();

            var task = await db.Tasks.FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId.Value);

            if (task == null)
                return NotFound(new { message = "Задача не найдена." });

            var subjectExists = await db.Subjects.AnyAsync(s =>
                s.Id == request.SubjectId &&
                s.UserId == userId.Value);

            if (!subjectExists)
                return BadRequest(new { message = "Предмет не найден." });

            var folderExists = await db.Folders.AnyAsync(f =>
                f.Id == request.FolderId &&
                f.UserId == userId.Value &&
                f.SubjectId == request.SubjectId);

            if (!folderExists)
                return BadRequest(new
                {
                    message = "Папка не найдена или не принадлежит этому предмету."
                });

            task.Name = request.Name.Trim();
            task.Description = request.Description?.Trim() ?? string.Empty;
            task.DueDate = request.DueDate;
            task.SubjectId = request.SubjectId;
            task.FolderId = request.FolderId;
            task.IsCompleted = request.IsCompleted;

            await db.SaveChangesAsync();

            return Ok(new { message = "Задача обновлена." });
        }

        // PATCH: api/tasks/5/completion
        [HttpPatch("{id:int}/completion")]
        public async Task<IActionResult> SetCompletion(
            int id,
            [FromBody] CompletionRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var task = await db.Tasks.FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId.Value);

            if (task == null)
                return NotFound(new { message = "Задача не найдена." });

            task.IsCompleted = request.IsCompleted;

            await db.SaveChangesAsync();

            return Ok(new
            {
                task.Id,
                task.IsCompleted
            });
        }

        // DELETE: api/tasks/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db = await _contextFactory.CreateDbContextAsync();

            var task = await db.Tasks.FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId.Value);

            if (task == null)
                return NotFound(new { message = "Задача не найдена." });

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();

            return Ok(new { message = "Задача удалена." });
        }
    }

    public class CreateTaskRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue)]
        public int FolderId { get; set; }
    }

    public class UpdateTaskRequest : CreateTaskRequest
    {
        public bool IsCompleted { get; set; }
    }

    public class CompletionRequest
    {
        public bool IsCompleted { get; set; }
    }
}
