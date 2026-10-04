using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class FeeCharge
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // FOREIGN KEYS
        // =========================================================
        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        public int? FeeRuleId { get; set; }

        [ForeignKey(nameof(FeeRuleId))]
        public FeeRule? FeeRule { get; set; }

        public int? StudentAccommodationId { get; set; }

        [ForeignKey(nameof(StudentAccommodationId))]
        public StudentAccommodation? StudentAccommodation { get; set; }

        // =========================================================
        // CHARGE DETAILS
        // =========================================================
        [Required]
        public DateOnly PeriodStart { get; set; }

        [Required]
        public DateOnly PeriodEnd { get; set; }

        public decimal Amount { get; set; }

        [Required]
        public DateOnly DueDate { get; set; }

        [Required]
        public FeeChargeStatus Status { get; set; } = FeeChargeStatus.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<PaymentAllocation> Allocations { get; set; } = new();
    }

}

// ENUM: FeeChargeStatus
public enum FeeChargeStatus
{
    Unpaid,
    Partial,
    Paid
}