
using Microsoft.EntityFrameworkCore;
using MisaTracker.Core.ORM.Models;

namespace MisaTracker.Core.ORM.DataBase
{
    public class DbConnection : DbContext
    {
        public DbConnection(DbContextOptions<DbConnection> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Folder> Folders { get; set; } = null!;
        public DbSet<Tasks> Tasks { get; set; } = null!;
        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<TaskReminderLog> TaskReminderLogs { get; set; } = null!;
        public DbSet<PushSubscription> PushSubscriptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Folder>()
                .HasOne(f => f.User)
                .WithMany(u => u.Folders)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subjects)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Folder>()
                .HasOne(f => f.Subject)
                .WithMany(s => s.Folders)
                .HasForeignKey(f => f.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tasks>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tasks>()
                .HasOne(t => t.Subject)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PushSubscription>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PushSubscription>()
                .HasIndex(p => p.Endpoint)
                .IsUnique();

            modelBuilder.Entity<PushSubscription>()
                .Property(p => p.Endpoint)
                .HasMaxLength(2048)
                .IsRequired();

            modelBuilder.Entity<PushSubscription>()
                .Property(p => p.P256dh)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<PushSubscription>()
                .Property(p => p.Auth)
                .HasMaxLength(256)
                .IsRequired();
            modelBuilder.Entity<TaskReminderLog>()
    .HasIndex(r => new { r.TaskId, r.ReminderType })
    .IsUnique();

            modelBuilder.Entity<TaskReminderLog>()
                .Property(r => r.ReminderType)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<TaskReminderLog>()
                .HasOne<Tasks>()
                .WithMany()
                .HasForeignKey(r => r.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
