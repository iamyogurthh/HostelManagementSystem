using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class FeeCharge : IValidatableObject
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

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Identifies how the charge was entered.
        /// </summary>
        [Required]
        public FeeChargeSource Source { get; set; } = FeeChargeSource.Manual;

        [MaxLength(500)]
        public string? Description { get; set; }

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

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (StudentId <= 0)
            {
                yield return new ValidationResult(
                    "A fee charge must reference a valid student.",
                    new[] { nameof(StudentId) });
            }

            if (!Enum.IsDefined(Source))
            {
                yield return new ValidationResult(
                    "Charge source is invalid.",
                    new[] { nameof(Source) });
            }

            if (Source == FeeChargeSource.Manual &&
                string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult(
                    "A manual charge requires a description.",
                    new[] { nameof(Description) });
            }

            if (PeriodStart == default)
            {
                yield return new ValidationResult(
                    "Charge period start is required.",
                    new[] { nameof(PeriodStart) });
            }

            if (PeriodEnd == default)
            {
                yield return new ValidationResult(
                    "Charge period end is required.",
                    new[] { nameof(PeriodEnd) });
            }

            if (PeriodStart != default &&
                PeriodEnd != default &&
                PeriodEnd < PeriodStart)
            {
                yield return new ValidationResult(
                    "Charge period end cannot be before its start.",
                    new[] { nameof(PeriodStart), nameof(PeriodEnd) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Charge amount must be greater than zero.",
                    new[] { nameof(Amount) });
            }

            if (DueDate == default)
            {
                yield return new ValidationResult(
                    "Due date is required.",
                    new[] { nameof(DueDate) });
            }

            if (!Enum.IsDefined(Status))
            {
                yield return new ValidationResult(
                    "Charge status is invalid.",
                    new[] { nameof(Status) });
            }
        }
    }
}

public enum FeeChargeStatus
{
    Unpaid,
    Partial,
    Paid
}

public enum FeeChargeSource
{
    Manual
}