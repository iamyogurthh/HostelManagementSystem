using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class Payment : IValidatableObject
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

        // =========================================================
        // PAYMENT DETAILS
        // =========================================================
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Required]
        [MaxLength(100)]
        public string ReceiptNumber { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<PaymentAllocation> Allocations { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (StudentId <= 0)
            {
                yield return new ValidationResult(
                    "A payment must reference a valid student.",
                    new[] { nameof(StudentId) });
            }

            if (PaymentDate == default)
            {
                yield return new ValidationResult(
                    "Payment date is required.",
                    new[] { nameof(PaymentDate) });
            }

            if (TotalAmount <= 0)
            {
                yield return new ValidationResult(
                    "Payment amount must be greater than zero.",
                    new[] { nameof(TotalAmount) });
            }

            if (!Enum.IsDefined(PaymentMethod))
            {
                yield return new ValidationResult(
                    "Payment method is invalid.",
                    new[] { nameof(PaymentMethod) });
            }

            if (string.IsNullOrWhiteSpace(ReceiptNumber))
            {
                yield return new ValidationResult(
                    "Receipt number is required.",
                    new[] { nameof(ReceiptNumber) });
            }

            if (ReceiptNumber.Length > 100)
            {
                yield return new ValidationResult(
                    "Receipt number cannot exceed 100 characters.",
                    new[] { nameof(ReceiptNumber) });
            }
        }
    }
}

// ENUM: PaymentMethod
public enum PaymentMethod
{
    Cash,
    KPay,
    Wave,
    Other
}