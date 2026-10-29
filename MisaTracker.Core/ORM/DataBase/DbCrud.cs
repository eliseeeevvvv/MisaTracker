
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.Models;

namespace MisaTracker.Core.ORM.DataBase
{
    public class DbCrud
    {
        private readonly IDbContextFactory<DbConnection> _contextFactory;

        public DbCrud(IDbContextFactory<DbConnection> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // CRUD for Subject

        public async Task AddSubject(Subject subject)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();
        }

        public async Task<Subject?> ReadSubject(int subjectId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subjects.FindAsync(subjectId);
        }

        public async Task UpdateSubject(Subject subject)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Subjects.Update(subject);
            await context.SaveChangesAsync();
        }

        public async Task DeleteSubject(int subjectId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var subject = await context.Subjects.FindAsync(subjectId);

            if (subject != null)
            {
                context.Subjects.Remove(subject);
                await context.SaveChangesAsync();
            }
        }

        // CRUD for Folder

        public async Task AddFolder(Folder folder)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Folders.Add(folder);
            await context.SaveChangesAsync();
        }

        public async Task<Folder?> ReadFolder(int folderId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Folders.FindAsync(folderId);
        }

        public async Task UpdateFolder(Folder folder)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Folders.Update(folder);
            await context.SaveChangesAsync();
        }

        public async Task DeleteFolder(int folderId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var folder = await context.Folders.FindAsync(folderId);

            if (folder != null)
            {
                context.Folders.Remove(folder);
                await context.SaveChangesAsync();
            }
        }

        // CRUD for Tasks

        public async Task AddTask(Tasks task)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Tasks.Add(task);
            await context.SaveChangesAsync();
        }

        public async Task<Tasks?> ReadTask(int taskId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasks.FindAsync(taskId);
        }

        public async Task UpdateTask(Tasks task)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Tasks.Update(task);
            await context.SaveChangesAsync();
        }

        public async Task DeleteTask(int taskId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var task = await context.Tasks.FindAsync(taskId);

            if (task != null)
            {
                context.Tasks.Remove(task);
                await context.SaveChangesAsync();
            }
        }

        // CRUD for Lesson

        public async Task AddLesson(Lesson lesson)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Lessons.Add(lesson);
            await context.SaveChangesAsync();
        }

        public async Task<Lesson?> ReadLesson(int lessonId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Lessons.FindAsync(lessonId);
        }

        public async Task UpdateLesson(Lesson lesson)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Lessons.Update(lesson);
            await context.SaveChangesAsync();
        }

        public async Task DeleteLesson(int lessonId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var lesson = await context.Lessons.FindAsync(lessonId);

            if (lesson != null)
            {
                context.Lessons.Remove(lesson);
                await context.SaveChangesAsync();
            }
        }

        // CRUD for User

        public async Task AddUser(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public async Task<User?> ReadUser(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.FindAsync(userId);
        }

        public async Task UpdateUser(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        public async Task DeleteUser(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(userId);

            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }
    }
}
