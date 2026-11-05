using HostelManagementSystem.Models;
using System;
using System.Linq;

namespace HostelManagementSystem.ViewModels.PaymentViewModels
{
    public class PaymentItemViewModel
    {
        public Payment Payment { get; }

        public int Id =>
            Payment.Id;

        public string ReceiptNumber =>
            Payment.ReceiptNumber;

        public DateTime PaymentDate =>
            Payment.PaymentDate;

        public decimal TotalAmount =>
            Payment.TotalAmount;

        public PaymentMethod PaymentMethod =>
            Payment.PaymentMethod;

        public string? Notes =>
            Payment.Notes;

        // =========================================================
        // DYNAMICALLY COMPUTED DATE RANGE FROM ALLOCATED CHARGES
        // =========================================================
        public string DateRangeText
        {
            get
            {
                if (Payment.Allocations == null || !Payment.Allocations.Any())
                    return string.Empty;

                var feeCharges = Payment.Allocations
                    .Where(a => a.FeeCharge != null)
                    .Select(a => a.FeeCharge!)
                    .ToList();

                if (!feeCharges.Any())
                    return string.Empty;

                var minStart = feeCharges.Min(fc => fc.PeriodStart);
                var maxEnd = feeCharges.Max(fc => fc.PeriodEnd);

                // Updated format: "From 08 Dec 2026 to 10 Dec 2026"
                return $"{minStart:dd MMM yyyy}မှ {maxEnd:dd MMM yyyy}ထိ";
            }
        }

        public bool HasDateRange =>
            !string.IsNullOrWhiteSpace(DateRangeText);

        // =========================================================
        // DYNAMIC MONTHS / DESCRIPTION COMPUTED FROM ALLOCATIONS
        // =========================================================
        public string DescriptionText
        {
            get
            {
                if (Payment.Allocations == null || !Payment.Allocations.Any())
                {
                    // Fallback to Notes if no allocations exist
                    return Payment.Notes ?? string.Empty;
                }

                var feeCharges = Payment.Allocations
                    .Where(a => a.FeeCharge != null)
                    .Select(a => a.FeeCharge!)
                    .ToList();

                if (!feeCharges.Any())
                {
                    return Payment.Notes ?? string.Empty;
                }

                // Count how many fee charges (months) are paid in this transaction
                int monthCount = feeCharges.Count;

                if (monthCount == 1)
                {
                    return "1 လစာ";
                }

                return $"{monthCount} လစာ";
            }
        }

        public bool HasDescription =>
            !string.IsNullOrWhiteSpace(DescriptionText);

        public PaymentItemViewModel(Payment payment)
        {
            Payment = payment;
        }
    }
}