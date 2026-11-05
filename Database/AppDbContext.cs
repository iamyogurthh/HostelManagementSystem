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
            // BUILDING → FLOOR
            // =========================================================
            modelBuilder.Entity<Floor>()
                .HasOne(f => f.Building)
                .WithMany(b => b.Floors)
                .HasForeignKey(f => f.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // FLOOR → ROOM
            // =========================================================
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Floor)
                .WithMany(f => f.Rooms)
                .HasForeignKey(r => r.FloorId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // STUDENT → STUDENT ACADEMIC RECORD
            // =========================================================
            modelBuilder.Entity<StudentAcademicRecord>()
                .HasOne(ar => ar.Student)
                .WithMany(s => s.AcademicRecords)
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // STUDENT → STUDENT ACCOMMODATION
            //
            // Accommodation history must be preserved.
            // =========================================================
            modelBuilder.Entity<StudentAccommodation>()
                .HasOne(sa => sa.Student)
                .WithMany(s => s.Accommodations)
                .HasForeignKey(sa => sa.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // ROOM → STUDENT ACCOMMODATION
            //
            // A room cannot be deleted if accommodation history
            // references it.
            // =========================================================
            modelBuilder.Entity<StudentAccommodation>()
                .HasOne(sa => sa.Room)
                .WithMany(r => r.Accommodations)
                .HasForeignKey(sa => sa.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // STUDENT CATEGORY → FEE RULE
            // =========================================================
            modelBuilder.Entity<FeeRule>()
                .HasOne(fr => fr.StudentCategory)
                .WithMany(sc => sc.FeeRules)
                .HasForeignKey(fr => fr.StudentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // ROOM TYPE → FEE RULE
            // =========================================================
            modelBuilder.Entity<FeeRule>()
                .HasOne(fr => fr.RoomType)
                .WithMany(rt => rt.FeeRules)
                .HasForeignKey(fr => fr.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // STUDENT → FEE CHARGE
            //
            // Financial history must be protected.
            // =========================================================
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.Student)
                .WithMany(s => s.FeeCharges)
                .HasForeignKey(fc => fc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // FEE RULE → FEE CHARGE
            // =========================================================
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.FeeRule)
                .WithMany(fr => fr.FeeCharges)
                .HasForeignKey(fc => fc.FeeRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================================================
            // STUDENT ACCOMMODATION → FEE CHARGE
            // =========================================================
            modelBuilder.Entity<FeeCharge>()
                .HasOne(fc => fc.StudentAccommodation)
                .WithMany(sa => sa.FeeCharges)
                .HasForeignKey(fc => fc.StudentAccommodationId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================================================
            // STUDENT → PAYMENT
            // =========================================================
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Student)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================
            // PAYMENT → PAYMENT ALLOCATION
            // =========================================================
            modelBuilder.Entity<PaymentAllocation>()
                .HasOne(pa => pa.Payment)
                .WithMany(p => p.Allocations)
                .HasForeignKey(pa => pa.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================================================
            // FEE CHARGE → PAYMENT ALLOCATION
            // =========================================================
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

            // =========================================================
            // ACADEMIC RECORD INDEX
            // =========================================================
            modelBuilder.Entity<StudentAcademicRecord>()
                .HasIndex(ar => ar.StudentId);

            // =========================================================
            // ACCOMMODATION INDEXES
            // =========================================================

            // Fast lookup of a student's accommodation records.
            modelBuilder.Entity<StudentAccommodation>()
                .HasIndex(sa => new
                {
                    sa.StudentId,
                    sa.IsActive
                });

            // Fast lookup of room occupancy/history.
            modelBuilder.Entity<StudentAccommodation>()
                .HasIndex(sa => new
                {
                    sa.RoomId,
                    sa.IsActive
                });

            // =========================================================
            // ONE CURRENT ACCOMMODATION PER STUDENT
            //
            // IsActive = true means the record has not been soft-deleted.
            // EndDate IS NULL means the accommodation has not ended.
            //
            // Therefore, only one record can represent the student's
            // current accommodation.
            // =========================================================
            modelBuilder.Entity<StudentAccommodation>()
                .HasIndex(sa => sa.StudentId)
                .IsUnique()
                .HasFilter("\"IsActive\" = 1 AND \"EndDate\" IS NULL");

            // =========================================================
            // FEE RULE INDEX
            // =========================================================
            modelBuilder.Entity<FeeRule>()
                .HasIndex(fr => new
                {
                    fr.StudentCategoryId,
                    fr.RoomTypeId,
                    fr.IsActive
                });

            // =========================================================
            // FEE CHARGE INDEXES
            // =========================================================
            modelBuilder.Entity<FeeCharge>()
                .HasIndex(fc => new
                {
                    fc.StudentId,
                    fc.Status
                });

            modelBuilder.Entity<FeeCharge>()
                .HasIndex(fc => fc.DueDate);

            // Prevent duplicate charges for the same
            // accommodation segment and billing period.
            modelBuilder.Entity<FeeCharge>()
                .HasIndex(fc => new
                {
                    fc.StudentAccommodationId,
                    fc.PeriodStart
                })
                .IsUnique();

            // =========================================================
            // PAYMENT INDEXES
            // =========================================================
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.StudentId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentDate);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ReceiptNumber)
                .IsUnique();

            // =========================================================
            // PAYMENT ALLOCATION INDEXES
            // =========================================================
            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(pa => pa.PaymentId);

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(pa => pa.FeeChargeId);

            modelBuilder.Entity<PaymentAllocation>()
                .HasIndex(pa => new
                {
                    pa.PaymentId,
                    pa.FeeChargeId
                })
                .IsUnique();

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

            modelBuilder.Entity<FeeCharge>()
                .ToTable("FeeCharges", table => table.HasCheckConstraint(
                    "CK_FeeCharges_Amount_Positive",
                    "Amount > 0"));

            modelBuilder.Entity<Payment>()
                .ToTable("Payments", table => table.HasCheckConstraint(
                    "CK_Payments_TotalAmount_Positive",
                    "TotalAmount > 0"));

            modelBuilder.Entity<PaymentAllocation>()
                .ToTable("PaymentAllocations", table => table.HasCheckConstraint(
                    "CK_PaymentAllocations_Amount_Positive",
                    "Amount > 0"));
        }
    }
}