using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class FeeRule
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // FOREIGN KEYS
        // =========================================================
        [Required]
        public int StudentCategoryId { get; set; }

        [ForeignKey(nameof(StudentCategoryId))]
        public StudentCategory? StudentCategory { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [ForeignKey(nameof(RoomTypeId))]
        public RoomType? RoomType { get; set; }

        // =========================================================
        // PRICING DETAILS
        // =========================================================
        public decimal MonthlyAmount { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<FeeCharge> FeeCharges { get; set; } = new();
    }
}