using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class StudentAcademicRecord
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // FOREIGN KEY TO STUDENT
        // =========================================================
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student Student { get; set; } = null!;

        // =========================================================
        // ACADEMIC RECORD DETAILS
        // =========================================================
        [Required]
        public string AcademicYearLevel { get; set; } = string.Empty; // e.g., "First Year", "Second Year", "Honours"

        public int AttemptNumber { get; set; } = 1; // e.g., 1 for 1st try, 2 for 2nd try

        public string? RollNumber { get; set; }

        public string? AttendedYear { get; set; } // e.g., "2023-2024"

        public bool IsHonours { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;


    }
}