using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class PaymentAllocation
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
        public decimal Amount { get; set; }
    }
}