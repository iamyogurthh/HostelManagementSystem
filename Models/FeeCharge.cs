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
        // STUDENT
        // =========================================================

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        // =========================================================
        // FEE RULE
        // =========================================================

        public int? FeeRuleId { get; set; }

        [ForeignKey(nameof(FeeRuleId))]
        public FeeRule? FeeRule { get; set; }

        // =========================================================
        // ACCOMMODATION
        // =========================================================

        public int? StudentAccommodationId { get; set; }

        [ForeignKey(nameof(StudentAccommodationId))]
        public StudentAccommodation? StudentAccommodation { get; set; }

        // =========================================================
        // BILLING PERIOD
        // =========================================================

        [Required]
        public DateOnly PeriodStart { get; set; }

        [Required]
        public DateOnly PeriodEnd { get; set; }

        // =========================================================
        // AMOUNT
        // =========================================================

        public decimal Amount { get; set; }

        [Required]
        public DateOnly DueDate { get; set; }

        // =========================================================
        // PAYMENT STATUS
        // =========================================================

        [Required]
        public FeeChargeStatus Status { get; set; } = FeeChargeStatus.Unpaid;

        // =========================================================
        // PRORATION
        // =========================================================

        /// <summary>
        /// Indicates whether this charge was calculated
        /// using a partial billing period.
        /// </summary>
        public bool IsProrated { get; set; }

        /// <summary>
        /// Number of days the student was actually charged for.
        /// </summary>
        public int ChargedDays { get; set; }

        /// <summary>
        /// Number of days in the complete billing period.
        /// </summary>
        public int PeriodDays { get; set; }

        // =========================================================
        // AUDIT
        // =========================================================

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================================================
        // PAYMENT ALLOCATIONS
        // =========================================================

        public List<PaymentAllocation> Allocations { get; set; } = new();
    }
}

public enum FeeChargeStatus
{
    Unpaid,
    Partial,
    Paid
}