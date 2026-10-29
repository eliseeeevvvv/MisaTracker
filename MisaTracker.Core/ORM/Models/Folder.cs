using System;
using System.Collections.Generic;
using System.Text;

namespace MisaTracker.Core.ORM.Models
{
    public class Folder
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }

        public ICollection<Tasks> Tasks { get; set; }
    }
}
