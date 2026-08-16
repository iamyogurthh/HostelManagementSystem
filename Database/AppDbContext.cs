using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HostelManagementSystem.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Floor> Floors { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Set database path to %AppData%/HostelManagementSystem/hostel.db
            string folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HostelManagementSystem"
            );

            if(!Directory.Exists( folderPath ) )
            {
                Directory.CreateDirectory( folderPath );
            }

            string dbPath = Path.Combine(folderPath, "hostel.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Foreign Key Cascades
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Building)
                .WithMany(b => b.Floors)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Performance Indexes for Fast Active/Inactive Filtering
            modelBuilder.Entity<Building>().HasIndex(b => b.IsActive);
            modelBuilder.Entity<Floor>().HasIndex(f => new { f.BuildingId, f.IsActive });
            modelBuilder.Entity<Room>().HasIndex(r => new { r.FloorId, r.IsActive });
        }
    }
}
