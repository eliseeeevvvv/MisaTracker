
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.DataBase;
using MisaTracker.Core.ORM.Models;

namespace MisaTrackerr.Server.Services
{
    public class TaskReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskReminderBackgroundService> _logger;

        private static readonly TimeZoneInfo UkraineTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "FLE Standard Time"
                    : "Europe/Kyiv");

        public TaskReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TaskReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Сервис напоминаний запущен. Часовой пояс: {TimeZone}",
                UkraineTimeZone.Id);

            await CheckRemindersAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckRemindersAsync(stoppingToken);
            }
        }

        private async Task CheckRemindersAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var dbFactory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<DbConnection>>();

                var pushService = scope.ServiceProvider
                    .GetRequiredService<PushNotificationService>();

                await using var db =
                    await dbFactory.CreateDbContextAsync(cancellationToken);

                // Текущее время в Украине.
                var now = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    UkraineTimeZone);

                var tasks = await db.Tasks
                    .AsNoTracking()
                    .Where(t =>
                        !t.IsCompleted &&
                        t.DueDate > now &&
                        t.DueDate <= now.AddHours(24))
                    .ToListAsync(cancellationToken);

                foreach (var task in tasks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var remaining = task.DueDate - now;

                    string? reminderType = null;

                    if (remaining <= TimeSpan.FromHours(1))
                    {
                        reminderType = "1h";
                    }
                    else if (remaining <= TimeSpan.FromHours(24))
                    {
                        reminderType = "24h";
                    }

                    if (reminderType == null)
                        continue;

                    var alreadySent = await db.TaskReminderLogs
                        .AnyAsync(
                            r => r.TaskId == task.Id &&
                                 r.ReminderType == reminderType,
                            cancellationToken);

                    if (alreadySent)
                        continue;

                    var hasSubscription = await db.PushSubscriptions
                        .AnyAsync(
                            p => p.UserId == task.UserId,
                            cancellationToken);

                    if (!hasSubscription)
                        continue;

                    try
                    {
                        var title = reminderType == "24h"
                            ? "Дедлайн уже скоро 🍓"
                            : "До дедлайна остался час ⏰";

                        var body = string.IsNullOrWhiteSpace(task.Name)
                            ? "Проверь своё задание в MisaTracker."
                            : $"Задание: {task.Name}";

                        await pushService.SendToUserAsync(
                            task.UserId,
                            title,
                            body,
                            "/");

                        db.TaskReminderLogs.Add(new TaskReminderLog
                        {
                            TaskId = task.Id,
                            ReminderType = reminderType,
                            SentAt = now
                        });

                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Ошибка отправки напоминания для задания {TaskId}.",
                            task.Id);
                    }
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Нормальная остановка приложения.
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка проверки дедлайнов.");
            }
        }
    }
}
