using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HostelManagementSystem.Services
{
    public class FeeChargeService
    {
        /// <summary>
        /// Creates one administrator-entered charge for the complete date range.
        /// Existing unallocated charges that overlap the range are replaced.
        /// Charges that already have payments are protected from replacement.
        /// </summary>
        public async Task<FeeCharge> CreateManualChargeAsync(
            int studentId,
            DateOnly periodStart,
            DateOnly periodEnd,
            decimal amount,
            DateOnly dueDate,
            string description,
            CancellationToken cancellationToken = default)
        {
            ValidateManualCharge(
                studentId,
                periodStart,
                periodEnd,
                amount,
                dueDate,
                description);

            await using var db = new AppDbContext();
            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var studentExists = await db.Students
                    .AnyAsync(
                        student => student.Id == studentId && student.IsActive,
                        cancellationToken);

                if (!studentExists)
                {
                    throw new InvalidOperationException(
                        "The selected student does not exist or is inactive.");
                }

                var overlappingCharges = await db.FeeCharges
                    .Include(charge => charge.Allocations)
                    .Where(charge =>
                        charge.StudentId == studentId &&
                        charge.PeriodStart <= periodEnd &&
                        charge.PeriodEnd >= periodStart)
                    .ToListAsync(cancellationToken);

                var hasAllocatedOverlap = overlappingCharges.Any(charge =>
                    charge.Allocations != null &&
                    charge.Allocations.Sum(allocation => allocation.Amount) > 0m);

                if (hasAllocatedOverlap)
                {
                    throw new InvalidOperationException(
                        "The selected range overlaps a charge that already has payments. " +
                        "Create a new range after the paid period instead.");
                }

                if (overlappingCharges.Count > 0)
                {
                    db.FeeCharges.RemoveRange(overlappingCharges);
                }

                var charge = new FeeCharge
                {
                    StudentId = studentId,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Amount = decimal.Round(
                        amount,
                        2,
                        MidpointRounding.AwayFromZero),
                    DueDate = dueDate,
                    Status = FeeChargeStatus.Unpaid,
                    Source = FeeChargeSource.Manual,
                    Description = description.Trim(),
                    FeeRuleId = null,
                    StudentAccommodationId = null,
                    IsProrated = false,
                    ChargedDays = periodEnd.DayNumber - periodStart.DayNumber + 1,
                    PeriodDays = periodEnd.DayNumber - periodStart.DayNumber + 1,
                    CreatedAt = DateTime.Now
                };

                db.FeeCharges.Add(charge);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return charge;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static void ValidateManualCharge(
            int studentId,
            DateOnly periodStart,
            DateOnly periodEnd,
            decimal amount,
            DateOnly dueDate,
            string description)
        {
            if (studentId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(studentId));
            }

            if (periodStart == default)
            {
                throw new ArgumentException(
                    "Charge period start is required.",
                    nameof(periodStart));
            }

            if (periodEnd < periodStart)
            {
                throw new ArgumentException(
                    "Charge period end cannot be before its start.",
                    nameof(periodEnd));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Charge amount must be greater than zero.");
            }

            if (decimal.Round(amount, 2) != amount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Charge amount cannot have more than two decimal places.");
            }

            if (dueDate == default)
            {
                throw new ArgumentException(
                    "Due date is required.",
                    nameof(dueDate));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException(
                    "A manual charge description is required.",
                    nameof(description));
            }
        }
    }
}