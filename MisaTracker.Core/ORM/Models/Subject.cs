using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace MisaTracker.Core.ORM.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public ICollection<Folder> Folders { get; set; }

        public ICollection<Tasks> Tasks { get; set; }
    }
}
