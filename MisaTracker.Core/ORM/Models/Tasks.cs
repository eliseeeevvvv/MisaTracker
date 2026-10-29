using System;
using System.Collections.Generic;
using System.Text;

namespace MisaTracker.Core.ORM.Models
{
    public class Tasks
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public string Description { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime DueDate { get; set; }
        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }

        public int FolderId { get; set; }

        public Folder? Folder { get; set; }
    }
}
