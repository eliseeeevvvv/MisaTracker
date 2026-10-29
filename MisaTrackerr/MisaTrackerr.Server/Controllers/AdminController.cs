
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.DataBase;

namespace MisaTrackerr.Server.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IDbContextFactory<DbConnection> _factory;

        public AdminController(IDbContextFactory<DbConnection> factory)
        {
            _factory = factory;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            await using var db = await _factory.CreateDbContextAsync();

            return Ok(new
            {
                totalUsers = await db.Users.CountAsync(),
                totalSubjects = await db.Subjects.CountAsync(),
                totalFolders = await db.Folders.CountAsync(),
                totalTasks = await db.Tasks.CountAsync(),
                completedTasks = await db.Tasks.CountAsync(t => t.IsCompleted),
                unfinishedTasks = await db.Tasks.CountAsync(t => !t.IsCompleted),
                totalLessons = await db.Lessons.CountAsync()
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            await using var db = await _factory.CreateDbContextAsync();

            var users = await db.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.RoleId
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
