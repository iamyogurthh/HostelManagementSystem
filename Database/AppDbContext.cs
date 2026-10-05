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

        public DbSet<RoomType> RoomTypes { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<StudentCategory> StudentCategories { get; set; } = null!;
        public DbSet<StudentAcademicRecord> StudentAcademicRecords { get; set; } = null!;
        public DbSet<StudentAccommodation> StudentAccommodations { get; set; } = null!;
        public DbSet<FeeRule> FeeRules { get; set; } = null!;
        public DbSet<FeeCharge> FeeCharges { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentAllocation> PaymentAllocations { get; set; } = null!;

       
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
            // Keep student record if room is deleted.
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Room)
                .WithMany(r => r.Students)
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Student -> StudentAcademicRecord
            modelBuilder.Entity<StudentAcademicRecord>()
                .HasOne(ar => ar.Student)
                .WithMany(s => s.AcademicRecords)
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> StudentAccommodation
            // Preserve accommodation history when possible.
            modelBuilder.Entity<StudentAccommodation>()
                .HasOne(sa => sa.Student)
                .WithMany(s => s.Accommodations)
                .HasForeignKey(sa => sa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Room -> StudentAccommodation
            // Prevent deleting a room that has accommodation history.
            modelBuilder.Entity<StudentAccommodation>()
                .HasOne(sa => sa.Room)
                .WithMany(r => r.Accommodations)
                .HasForeignKey(sa => sa.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentCategory -> FeeRule
            modelBuilder.Entity<FeeRule>()
                .HasOne(fr => fr.StudentCategory)
                .WithMany(sc => sc.FeeRules)
                .HasForeignKey(fr => fr.StudentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // RoomType -> FeeRule
            modelBuilder.Entity<FeeRule>()
                .HasOne(fr => fr.RoomType)
                .WithMany(rt => rt.FeeRules)
                .HasForeignKey(fr => fr.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> FeeCharge
            // Protect financial history.
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.Student)
                .WithMany(s => s.FeeCharges)
                .HasForeignKey(fc => fc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // FeeRule -> FeeCharge
            // Keep the charge even if the original fee rule is removed.
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.FeeRule)
                .WithMany(fr => fr.FeeCharges)
                .HasForeignKey(fc => fc.FeeRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            // StudentAccommodation -> FeeCharge
            // Keep the charge even if the accommodation record is removed.
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.StudentAccommodation)
                .WithMany(sa => sa.FeeCharges)
                .HasForeignKey(fc => fc.StudentAccommodationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Student -> Payment
            // Protect payment history.
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment -> PaymentAllocation
            // Deleting a payment also deletes its allocations.
            modelBuilder.Entity<PaymentAllocation>()
                .HasOne(pa => pa.Payment)
                .WithMany(p => p.Allocations)
                .HasForeignKey(pa => pa.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // FeeCharge -> PaymentAllocation
            // Prevent deleting a fee charge that has payment history.
            modelBuilder.Entity<PaymentAllocation>()
                .HasOne(pa => pa.FeeCharge)
                .WithMany(fc => fc.Allocations)
                .HasForeignKey(pa => pa.FeeChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // PERFORMANCE INDEXES
            // =========================================================

            modelBuilder.Entity<Building>()
                .HasIndex(b => b.IsActive);

            modelBuilder.Entity<Floor>()
                .HasIndex(f => new
                {
                    f.BuildingId,
                    f.IsActive
                });

            modelBuilder.Entity<Room>()
                .HasIndex(r => new
                {
                    r.FloorId,
                    r.IsActive
                });

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.IsActive);

            modelBuilder.Entity<Student>()
                .HasIndex(s => new
                {
                    s.RoomId,
                    s.IsActive
                });

            // Fast lookup for a student's academic history.
            modelBuilder.Entity<StudentAcademicRecord>()
                .HasIndex(ar => ar.StudentId);

            // =========================================================
            // PAYMENT MANAGEMENT INDEXES
            // =========================================================

            modelBuilder.Entity<StudentAccommodation>()
                .HasIndex(sa => new
                {
                    sa.StudentId,
                    sa.IsActive
                });

            modelBuilder.Entity<StudentAccommodation>()
                .HasIndex(sa => new
                {
                    sa.RoomId,
                    sa.IsActive
                });

            modelBuilder.Entity<FeeRule>()
                .HasIndex(fr => new
                {
                    fr.StudentCategoryId,
                    fr.RoomTypeId,
                    fr.IsActive
                });

            modelBuilder.Entity<FeeCharge>()
                .HasIndex(fc => new
                {
                    fc.StudentId,
                    fc.Status
                });

            modelBuilder.Entity<FeeCharge>()
                .HasIndex(fc => fc.DueDate);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.StudentId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentDate);

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(pa => pa.PaymentId);

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(pa => pa.FeeChargeId);

            // =========================================================
            // MONEY PRECISION
            // =========================================================

            modelBuilder.Entity<FeeRule>()
                .Property(fr => fr.MonthlyAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FeeCharge>()
                .Property(fc => fc.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentAllocation>()
                .Property(pa => pa.Amount)
                .HasPrecision(18, 2);
        }
    }
}