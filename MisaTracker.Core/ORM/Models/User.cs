using System;
using System.Collections.Generic;
using System.Text;

namespace MisaTracker.Core.ORM.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }

        public int RoleId { get; set; }

        public ICollection<Subject> Subjects { get; set; }

        public ICollection<Folder> Folders { get; set; }

        public ICollection<Tasks> Tasks { get; set; }

        public ICollection<Lesson> Lessons { get; set; }
    }
}
