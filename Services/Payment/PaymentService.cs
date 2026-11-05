using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using PaymentModel = HostelManagementSystem.Models.Payment;

namespace HostelManagementSystem.Services.Payment
{
    public class PaymentService
    {
        // =========================================================
        // RECORD PAYMENT
        // =========================================================

        /// <summary>
        /// Records a payment and automatically allocates it to the
        /// student's oldest outstanding fee charges first.
        ///
        /// Payment allocation order:
        ///     Oldest charge -> Newest charge
        ///
        /// Supports:
        ///     - Full payment
        ///     - Partial payment
        ///     - One payment across multiple fee charges
        ///
        /// Does not allow:
        ///     - Zero/negative payments
        ///     - Overpayment
        ///     - Duplicate receipt numbers
        ///     - Payment for inactive students
        /// </summary>
        public async Task<PaymentModel> RecordPaymentAsync(
            int studentId,
            decimal amount,
            PaymentMethod paymentMethod,
            string receiptNumber,
            string? notes = null,
            DateTime? paymentDate = null,
            CancellationToken cancellationToken = default)
        {
            // -----------------------------------------------------
            // BASIC VALIDATION
            // -----------------------------------------------------

            if (studentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(studentId),
                    "Invalid student ID.");
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Payment amount must be greater than zero.");

            if (decimal.Round(amount, 2) != amount)
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Payment amount cannot have more than two decimal places.");
            }

            if (string.IsNullOrWhiteSpace(receiptNumber))
            {
                throw new ArgumentException(
                    "Receipt number is required.",
                    nameof(receiptNumber));
            }

            receiptNumber = receiptNumber.Trim();

            if (!Enum.IsDefined(paymentMethod))
            {
                throw new ArgumentException(
                    "Invalid payment method.",
                    nameof(paymentMethod));
            }

            var actualPaymentDate =
                paymentDate ?? DateTime.Now;

            if (actualPaymentDate > DateTime.Now)
            {
                throw new ArgumentException(
                    "Payment date cannot be in the future.",
                    nameof(paymentDate));
            }

            await using var db = new AppDbContext();

            // -----------------------------------------------------
            // STUDENT VALIDATION
            // -----------------------------------------------------

            var student = await db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == studentId,
                    cancellationToken);

            if (student == null)
            {
                throw new InvalidOperationException(
                    $"Student {studentId} does not exist.");
            }

            if (!student.IsActive)
            {
                throw new InvalidOperationException(
                    "Cannot record a new payment for an inactive student.");
            }

            // -----------------------------------------------------
            // RECEIPT NUMBER VALIDATION
            // -----------------------------------------------------

            var receiptExists = await db.Payments
                .AnyAsync(
                    p => p.ReceiptNumber == receiptNumber,
                    cancellationToken);

            if (receiptExists)
            {
                throw new InvalidOperationException(
                    $"Receipt number '{receiptNumber}' already exists.");
            }

            // -----------------------------------------------------
            // TRANSACTION
            // -----------------------------------------------------

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // -------------------------------------------------
                // LOAD FEE CHARGES
                //
                // We calculate the actual outstanding amount from
                // allocations instead of relying only on Status.
                // This protects us from stale/inconsistent status.
                // -------------------------------------------------

                var charges = await db.FeeCharges
                    .Where(fc =>
                        fc.StudentId == studentId)
                    .OrderBy(fc => fc.PeriodStart)
                    .ThenBy(fc => fc.Id)
                    .Include(fc => fc.Allocations)
                    .ToListAsync(cancellationToken);

                // -------------------------------------------------
                // CALCULATE OUTSTANDING AMOUNTS
                // -------------------------------------------------

                var outstandingCharges = charges
                    .Select(charge =>
                    {
                        var allocatedAmount =
                            charge.Allocations?
                                .Sum(a => a.Amount) ?? 0m;

                        if (charge.Allocations?.Any(a => a.Amount <= 0) == true)
                        {
                            throw new InvalidOperationException(
                                $"Fee charge {charge.Id} contains an invalid allocation.");
                        }

                        var outstandingAmount =
                            Math.Max(
                                0m,
                                charge.Amount - allocatedAmount);

                        return new
                        {
                            Charge = charge,
                            Outstanding = outstandingAmount
                        };
                    })
                    .Where(x => x.Outstanding > 0)
                    .ToList();

                // -------------------------------------------------
                // TOTAL OUTSTANDING
                // -------------------------------------------------

                var totalOutstanding =
                    outstandingCharges.Sum(
                        x => x.Outstanding);

                if (totalOutstanding <= 0)
                {
                    throw new InvalidOperationException(
                        "This student has no outstanding charges.");
                }

                // -------------------------------------------------
                // OVERPAYMENT CHECK
                // -------------------------------------------------

                if (amount > totalOutstanding)
                {
                    throw new InvalidOperationException(
                        $"Payment amount {amount:N0} MMK exceeds the " +
                        $"student's outstanding balance of " +
                        $"{totalOutstanding:N0} MMK.");
                }

                // -------------------------------------------------
                // CREATE PAYMENT
                // -------------------------------------------------

                var payment = new PaymentModel
                {
                    StudentId = studentId,
                    PaymentDate = actualPaymentDate,
                    TotalAmount = amount,
                    PaymentMethod = paymentMethod,
                    ReceiptNumber = receiptNumber,
                    Notes = string.IsNullOrWhiteSpace(notes)
                        ? null
                        : notes.Trim(),
                    CreatedAt = DateTime.Now
                };

                db.Payments.Add(payment);

                // -------------------------------------------------
                // ALLOCATE PAYMENT
                // -------------------------------------------------

                var remainingPayment = amount;

                foreach (var item in outstandingCharges)
                {
                    if (remainingPayment <= 0)
                    {
                        break;
                    }

                    var allocationAmount =
                        Math.Min(
                            remainingPayment,
                            item.Outstanding);

                    if (allocationAmount <= 0)
                    {
                        continue;
                    }

                    var allocation =
                        new PaymentAllocation
                        {
                            Payment = payment,
                            FeeCharge = item.Charge,
                            Amount = allocationAmount
                        };

                    db.PaymentAllocations.Add(
                        allocation);

                    // ---------------------------------------------
                    // UPDATE FEE CHARGE STATUS
                    // ---------------------------------------------

                    var previousAllocated =
                        item.Charge.Allocations?
                            .Sum(a => a.Amount) ?? 0m;

                    var newTotalAllocated =
                        previousAllocated +
                        allocationAmount;

                    if (newTotalAllocated >= item.Charge.Amount)
                    {
                        item.Charge.Status =
                            FeeChargeStatus.Paid;
                    }
                    else
                    {
                        item.Charge.Status =
                            FeeChargeStatus.Partial;
                    }

                    remainingPayment -= allocationAmount;
                }

                // -------------------------------------------------
                // SAFETY CHECK
                // -------------------------------------------------

                if (remainingPayment > 0)
                {
                    throw new InvalidOperationException(
                        "The payment could not be fully allocated.");
                }

                // -------------------------------------------------
                // SAVE EVERYTHING AT ONCE
                // -------------------------------------------------

                await db.SaveChangesAsync(
                    cancellationToken);

                // -------------------------------------------------
                // COMMIT
                // -------------------------------------------------

                await transaction.CommitAsync(
                    cancellationToken);

                return payment;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        // =========================================================
        // GET OUTSTANDING CHARGES
        // =========================================================

        /// <summary>
        /// Gets all fee charges that still have an outstanding
        /// balance for the specified student.
        ///
        /// The remaining amount is calculated from allocations.
        /// </summary>
        public async Task<List<FeeCharge>> GetOutstandingChargesAsync(
            int studentId,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                return new List<FeeCharge>();
            }

            await using var db = new AppDbContext();

            var charges = await db.FeeCharges
                .AsNoTracking()
                .Include(fc => fc.Allocations)
                .Where(fc =>
                    fc.StudentId == studentId)
                .OrderBy(fc => fc.PeriodStart)
                .ThenBy(fc => fc.Id)
                .ToListAsync(cancellationToken);

            return charges
                .Where(charge =>
                {
                    var allocated =
                        charge.Allocations?
                            .Sum(a => a.Amount) ?? 0m;

                    var outstanding =
                        charge.Amount - allocated;

                    return outstanding > 0;
                })
                .ToList();
        }

        // =========================================================
        // GET OUTSTANDING BALANCE
        // =========================================================

        /// <summary>
        /// Calculates the student's total outstanding balance
        /// from fee charges and payment allocations.
        /// </summary>
        public async Task<decimal> GetOutstandingBalanceAsync(
            int studentId,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                return 0m;
            }

            await using var db = new AppDbContext();

            var charges = await db.FeeCharges
                .AsNoTracking()
                .Include(fc => fc.Allocations)
                .Where(fc =>
                    fc.StudentId == studentId)
                .ToListAsync(cancellationToken);

            return charges.Sum(charge =>
            {
                var allocated =
                    charge.Allocations?
                        .Sum(a => a.Amount) ?? 0m;

                return Math.Max(
                    0m,
                    charge.Amount - allocated);
            });
        }

        // =========================================================
        // GET PAYMENT HISTORY
        // =========================================================

        /// <summary>
        /// Gets all payments made by a student.
        ///
        /// Includes the fee charges to which each payment
        /// was allocated.
        /// </summary>
        public async Task<List<PaymentModel>> GetStudentPaymentsAsync(
            int studentId,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                return new List<PaymentModel>();
            }

            await using var db = new AppDbContext();

            return await db.Payments
                .AsNoTracking()
                .Include(p => p.Allocations)
                    .ThenInclude(a => a.FeeCharge)
                .Where(p =>
                    p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        // =========================================================
        // GET MONTHLY COLLECTED TOTAL
        // =========================================================

        /// <summary>
        /// Gets the total amount collected during the specified
        /// calendar month.
        /// </summary>
        public async Task<decimal> GetMonthlyCollectedTotalAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            if (year < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year));
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month));
            }

            await using var db = new AppDbContext();

            var startDate =
                new DateTime(
                    year,
                    month,
                    1);

            var endDate =
                startDate.AddMonths(1);

            return await db.Payments
                .AsNoTracking()
                .Where(p =>
                    p.PaymentDate >= startDate &&
                    p.PaymentDate < endDate)
                .SumAsync(
                    p => (decimal?)p.TotalAmount,
                    cancellationToken) ?? 0m;
        }

        // =========================================================
        // GET SYSTEM-WIDE OUTSTANDING BALANCE
        // =========================================================

        /// <summary>
        /// Gets the total outstanding fee balance across all
        /// active students.
        /// </summary>
        public async Task<decimal> GetSystemOutstandingBalanceAsync(
            CancellationToken cancellationToken = default)
        {
            await using var db = new AppDbContext();

            var charges = await db.FeeCharges
                .AsNoTracking()
                .Include(fc => fc.Allocations)
                .Where(fc =>
                    fc.Student != null &&
                    fc.Student.IsActive)
                .ToListAsync(cancellationToken);

            return charges.Sum(charge =>
            {
                var allocated =
                    charge.Allocations?
                        .Sum(a => a.Amount) ?? 0m;

                return Math.Max(
                    0m,
                    charge.Amount - allocated);
            });
        }
    }
}