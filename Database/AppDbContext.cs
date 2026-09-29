using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace HostelManagementSystem.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Floor> Floors { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<StudentAcademicRecord> StudentAcademicRecords { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Set database path to %AppData%/HostelManagementSystem/hostel.db
            string folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HostelManagementSystem"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string dbPath = Path.Combine(folderPath, "hostel.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // FOREIGN KEY RELATIONSHIPS
            // =========================================================

            // Building -> Floor
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Building)
                .WithMany(b => b.Floors)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Floor -> Room
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room -> Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Room)
                .WithMany(r => r.Students)
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.SetNull); // Keeping student record intact if room is deleted

            // Student -> StudentAcademicRecord (NEW)
            modelBuilder.Entity<StudentAcademicRecord>()
                .HasOne(ar => ar.Student)
                .WithMany(s => s.AcademicRecords)
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a student deletes their academic history

            // =========================================================
            // PERFORMANCE INDEXES
            // =========================================================
            modelBuilder.Entity<Building>().HasIndex(b => b.IsActive);
            modelBuilder.Entity<Floor>().HasIndex(f => new { f.BuildingId, f.IsActive });
            modelBuilder.Entity<Room>().HasIndex(r => new { r.FloorId, r.IsActive });
            modelBuilder.Entity<Student>().HasIndex(s => s.IsActive);
            modelBuilder.Entity<Student>().HasIndex(s => new { s.RoomId, s.IsActive });

            // Fast lookup for a student's academic history (NEW)
            modelBuilder.Entity<StudentAcademicRecord>().HasIndex(ar => ar.StudentId);
        }
    }
}