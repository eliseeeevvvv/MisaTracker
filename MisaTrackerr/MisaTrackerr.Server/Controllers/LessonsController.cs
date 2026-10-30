
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
    [Route("api/lessons")]
    [Authorize]
    public class LessonsController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;

        public LessonsController(
            IDbContextFactory<DbConnection> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id) ? id : null;
        }

        // GET: api/lessons
        [HttpGet]
        public async Task<IActionResult> GetLessons()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var lessons = await db.Lessons
                .AsNoTracking()
                .Where(l => l.UserId == userId.Value)
                .OrderBy(l => l.StartDate)
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Description,
                    l.StartDate,
                    l.EndDate,
                    l.IsRecurring,
                    l.RecurrenceGroupId
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // GET: api/lessons/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLesson(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var lesson = await db.Lessons
                .AsNoTracking()
                .Where(l => l.Id == id && l.UserId == userId.Value)
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Description,
                    l.StartDate,
                    l.EndDate,
                    l.IsRecurring,
                    l.RecurrenceGroupId
                })
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound(new { message = "Урок не найден." });

            return Ok(lesson);
        }

        // POST: api/lessons
        [HttpPost]
        public async Task<IActionResult> CreateLesson(
            [FromBody] LessonRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var validationError = ValidateRequest(request);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var groupId = request.IsRecurring ? Guid.NewGuid() : (Guid?)null;
            var lessonsToCreate = new List<Lesson>();

            // Даты хранятся как локальное время без UTC-конвертации.
            // Поэтому 14:00 из datetime-local останется 14:00 в базе и интерфейсе.
            var start = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified);
            var end = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Unspecified);

            var repeatUntil = request.RecurrenceEndDate.HasValue
                ? DateTime.SpecifyKind(request.RecurrenceEndDate.Value, DateTimeKind.Unspecified).Date
                : start.Date;

            for (var occurrenceStart = start;
                 occurrenceStart.Date <= repeatUntil;
                 occurrenceStart = occurrenceStart.AddDays(7))
            {
                var occurrenceEnd = end.AddDays((occurrenceStart - start).Days);

                lessonsToCreate.Add(new Lesson
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    StartDate = occurrenceStart,
                    EndDate = occurrenceEnd,
                    UserId = userId.Value,
                    IsRecurring = request.IsRecurring,
                    RecurrenceGroupId = groupId,
                    RecurrenceEndDate = request.IsRecurring ? repeatUntil : null
                });
            }

            db.Lessons.AddRange(lessonsToCreate);
            await db.SaveChangesAsync();

            return Ok(lessonsToCreate.Select(lesson => new
            {
                lesson.Id,
                lesson.Name,
                lesson.Description,
                lesson.StartDate,
                lesson.EndDate,
                lesson.IsRecurring,
                lesson.RecurrenceGroupId
            }));
        }

        // PUT: api/lessons/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateLesson(
            int id,
            [FromBody] LessonRequest request)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            var validationError = ValidateRequest(request);

            if (validationError != null)
                return BadRequest(new { message = validationError });

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var lesson = await db.Lessons.FirstOrDefaultAsync(l =>
                l.Id == id && l.UserId == userId.Value);

            if (lesson == null)
                return NotFound(new { message = "Урок не найден." });

            lesson.Name = request.Name.Trim();
            lesson.Description = request.Description?.Trim() ?? "";
            lesson.StartDate = request.StartDate;
            lesson.EndDate = request.EndDate;

            await db.SaveChangesAsync();

            return Ok(new { message = "Урок обновлён." });
        }

        // DELETE: api/lessons/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
                return Unauthorized();

            await using var db =
                await _contextFactory.CreateDbContextAsync();

            var lesson = await db.Lessons.FirstOrDefaultAsync(l =>
                l.Id == id && l.UserId == userId.Value);

            if (lesson == null)
                return NotFound(new { message = "Урок не найден." });

            db.Lessons.Remove(lesson);
            await db.SaveChangesAsync();

            return Ok(new { message = "Урок удалён." });
        }

        private static string? ValidateRequest(LessonRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return "Укажи название урока.";

            if (request.Name.Trim().Length > 100)
                return "Название урока не должно превышать 100 символов.";

            if (request.Description?.Length > 2000)
                return "Описание не должно превышать 2000 символов.";

            if (request.StartDate == default ||
                request.EndDate == default)
                return "Укажи дату и время начала и окончания.";

            if (request.EndDate <= request.StartDate)
                return "Время окончания должно быть позже начала.";

            if (request.IsRecurring)
            {
                if (!request.RecurrenceEndDate.HasValue)
                    return "Укажи дату окончания повторения.";

                if (request.RecurrenceEndDate.Value.Date < request.StartDate.Date)
                    return "Дата окончания повторения не может быть раньше первого урока.";

                if ((request.RecurrenceEndDate.Value.Date - request.StartDate.Date).TotalDays > 364)
                    return "Можно создавать регулярные уроки максимум на год вперёд.";
            }

            return null;
        }
    }

    public class LessonRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsRecurring { get; set; }

        public DateTime? RecurrenceEndDate { get; set; }
    }
}
