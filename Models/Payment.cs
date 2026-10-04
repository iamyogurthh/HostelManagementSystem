using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HostelManagementSystem.Models
{
    public class Payment
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
        public string ReceiptNumber { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================================================
        // RELATIONSHIPS
        // =========================================================
        public List<PaymentAllocation> Allocations { get; set; } = new();
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