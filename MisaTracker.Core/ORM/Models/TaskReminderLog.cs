namespace MisaTracker.Core.ORM.Models
{
    public class TaskReminderLog
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        // "24h" или "1h"
        public string ReminderType { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }
    }
}