using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class PaymentAllocation : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        // =========================================================
        // FOREIGN KEYS
        // =========================================================
        [Required]
        public int PaymentId { get; set; }

        [ForeignKey(nameof(PaymentId))]
        public Payment? Payment { get; set; }

        [Required]
        public int FeeChargeId { get; set; }

        [ForeignKey(nameof(FeeChargeId))]
        public FeeCharge? FeeCharge { get; set; }

        // =========================================================
        // ALLOCATION DETAILS
        // =========================================================
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (PaymentId <= 0)
            {
                yield return new ValidationResult(
                    "An allocation must reference a valid payment.",
                    new[] { nameof(PaymentId) });
            }

            if (FeeChargeId <= 0)
            {
                yield return new ValidationResult(
                    "An allocation must reference a valid fee charge.",
                    new[] { nameof(FeeChargeId) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Allocation amount must be greater than zero.",
                    new[] { nameof(Amount) });
            }
        }
    }
}