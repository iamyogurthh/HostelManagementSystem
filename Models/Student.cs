using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HostelManagementSystem.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // CATEGORY RELATIONSHIP
        // =========================================================
        [Required]
        public int StudentCategoryId { get; set; }

        [ForeignKey(nameof(StudentCategoryId))]
        public StudentCategory? StudentCategory { get; set; }

        // =========================================================
        // REQUIRED STUDENT INFO
        // =========================================================
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Img { get; set; } =
            "pack://application:,,,/Image/DefaultImages/default_student.png";

        [Required]
        public DateTime Birthday { get; set; }

        [Required]
        public string NRC { get; set; } = string.Empty;

        [Required]
        public string Student_ph { get; set; } = string.Empty;

        [Required]
        public string Parent_ph { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string Major { get; set; } = string.Empty;

        // =========================================================
        // REQUIRED PARENT INFO
        // =========================================================
        [Required]
        public string Father_name { get; set; } = string.Empty;

        [Required]
        public string Mother_name { get; set; } = string.Empty;

        // =========================================================
        // OPTIONAL FIELDS
        // =========================================================
        public string? NRC_img_front { get; set; }
        public string? NRC_img_back { get; set; }
        public string? Father_NRC { get; set; }
        public string? Mother_NRC { get; set; }

        // High School Details
        public string? High_school_certificate_img { get; set; }
        public string? High_school_roll_no { get; set; }
        public string? High_school_name { get; set; }
        public string? High_school_city { get; set; }

        // =========================================================
        // RELATIONSHIPS (1-to-Many)
        // =========================================================
        public List<StudentAcademicRecord> AcademicRecords { get; set; } = new();

        public List<StudentAccommodation> Accommodations { get; set; } = new();

        public List<FeeCharge> FeeCharges { get; set; } = new();

        public List<Payment> Payments { get; set; } = new();

        // =========================================================
        // STATUS & METADATA
        // =========================================================
        // Soft delete / active record status.
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // =========================================================
        // COMPUTED PROPERTIES
        // =========================================================
        [NotMapped]
        public string LatestRollNumber =>
            AcademicRecords?
                .OrderByDescending(r => r.Id)
                .FirstOrDefault()?
                .RollNumber ?? "-";

        [NotMapped]
        public StudentAccommodation? CurrentAccommodation =>
            Accommodations?
                .FirstOrDefault(a =>
                    a.IsActive &&
                    a.EndDate == null);
        [NotMapped]
        public bool IsUnpaid { get; set; }
    }
}