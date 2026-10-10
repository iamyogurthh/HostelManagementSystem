using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class StudentAccommodation
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // FOREIGN KEYS
        // =========================================================
        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student Student { get; set; } = null!;

        [Required]
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room Room { get; set; } = null!;

        // =========================================================
        // STAY DETAILS
        // =========================================================
        [Required]
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Null means the student is currently staying in this room.
        /// </summary>
        public DateOnly? EndDate { get; set; }

        /// <summary>
        /// Soft-delete status.
        /// True = record is valid.
        /// False = record has been soft-deleted.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<FeeCharge> FeeCharges { get; set; } = new();
    }
}