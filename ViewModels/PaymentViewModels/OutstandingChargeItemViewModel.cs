using HostelManagementSystem.Models;
using System;
using System.Linq;

namespace HostelManagementSystem.ViewModels.PaymentViewModels
{
    public class OutstandingChargeItemViewModel
    {
        public FeeCharge Charge { get; }

        public int Id =>
            Charge.Id;

        public DateOnly PeriodStart =>
            Charge.PeriodStart;

        public DateOnly PeriodEnd =>
            Charge.PeriodEnd;

        public DateOnly DueDate =>
            Charge.DueDate;

        public decimal Amount =>
            Charge.Amount;

        public decimal PaidAmount =>
            Charge.Allocations?
                .Sum(a => a.Amount) ?? 0m;

        public decimal RemainingAmount =>
            Math.Max(
                0m,
                Amount - PaidAmount);

        public FeeChargeStatus Status
        {
            get
            {
                if (RemainingAmount <= 0)
                    return FeeChargeStatus.Paid;

                if (PaidAmount > 0)
                    return FeeChargeStatus.Partial;

                return FeeChargeStatus.Unpaid;
            }
        }

        public OutstandingChargeItemViewModel(
            FeeCharge charge)
        {
            Charge = charge;
        }
    }
}