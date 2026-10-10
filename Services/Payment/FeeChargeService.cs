using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HostelManagementSystem.Services
{
    public class FeeChargeService
    {
        // =========================================================
        // PREVIEW MODEL
        // =========================================================

        public sealed class FeeChargePreview
        {
            public int StudentId { get; init; }
            public int StudentAccommodationId { get; init; }
            public int FeeRuleId { get; init; }

            public DateOnly PeriodStart { get; init; }
            public DateOnly PeriodEnd { get; init; }
            public DateOnly DueDate { get; init; }

            public decimal Amount { get; init; }

            public bool IsProrated { get; init; }
            public int ChargedDays { get; init; }
            public int PeriodDays { get; init; }
        }


        // =========================================================
        // ENSURE CHARGES THROUGH DATE
        //
        // This method persists charges.
        // Used for normal/current billing.
        // =========================================================

        public async Task EnsureChargesThroughAsync(
            int studentId,
            DateOnly through,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(studentId),
                    "Invalid student ID.");
            }

            await using var db = new AppDbContext();

            await EnsureChargesThroughAsync(
                db,
                studentId,
                through,
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        }


        // =========================================================
        // PREVIEW CHARGES
        //
        // IMPORTANT:
        // This method NEVER adds anything to the database.
        // It only calculates missing future charges.
        // =========================================================

        public async Task<List<FeeChargePreview>> PreviewChargesThroughAsync(
            int studentId,
            DateOnly through,
            CancellationToken cancellationToken = default)
        {
            if (studentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(studentId),
                    "Invalid student ID.");
            }

            await using var db = new AppDbContext();

            return await BuildChargePreviewAsync(
                db,
                studentId,
                through,
                cancellationToken);
        }


        // =========================================================
        // INTERNAL CHARGE GENERATION
        //
        // Adds FeeCharge entities to the supplied DbContext.
        // DOES NOT call SaveChanges().
        //
        // This is used by PaymentService so advance charges and
        // payment are saved atomically in one transaction.
        // =========================================================

        public async Task<List<FeeCharge>> EnsureChargesThroughAsync(
            AppDbContext db,
            int studentId,
            DateOnly through,
            CancellationToken cancellationToken = default)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (studentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(studentId),
                    "Invalid student ID.");
            }

            return await EnsureChargesThroughInternalAsync(
                db,
                studentId,
                through,
                cancellationToken);
        }


        // =========================================================
        // INTERNAL CHARGE GENERATION
        // =========================================================

        private async Task<List<FeeCharge>>
            EnsureChargesThroughInternalAsync(
                AppDbContext db,
                int studentId,
                DateOnly through,
                CancellationToken cancellationToken)
        {
            var student = await db.Students
                .Include(s => s.StudentCategory)
                .FirstOrDefaultAsync(
                    s => s.Id == studentId,
                    cancellationToken);

            if (student == null)
            {
                throw new InvalidOperationException(
                    "Student does not exist.");
            }

            if (!student.IsActive)
            {
                throw new InvalidOperationException(
                    "Cannot generate charges for an inactive student.");
            }

            var accommodations = await db.StudentAccommodations
                .Include(a => a.Room)
                    .ThenInclude(r => r.RoomType)
                .Where(a => a.StudentId == studentId)
                .OrderBy(a => a.StartDate)
                .ThenBy(a => a.Id)
                .ToListAsync(cancellationToken);

            if (accommodations.Count == 0)
                return new List<FeeCharge>();

            var billingAnchor = accommodations.Min(a => a.StartDate);

            var existingCharges = await db.FeeCharges
                .Where(fc => fc.StudentId == studentId)
                .ToListAsync(cancellationToken);

            var existingPeriodStarts = existingCharges
                .Select(fc => fc.PeriodStart)
                .ToHashSet();

            var createdCharges = new List<FeeCharge>();

            var currentPeriodStart = billingAnchor;

            while (currentPeriodStart <= through)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var periodEnd =
                    currentPeriodStart
                        .AddMonths(1)
                        .AddDays(-1);

                if (!existingPeriodStarts.Contains(currentPeriodStart))
                {
                    var accommodation =
                        FindAccommodationForBillingPeriod(
                            accommodations,
                            currentPeriodStart);

                    if (accommodation != null)
                    {
                        var feeRule =
                            await FindFeeRuleAsync(
                                db,
                                student.StudentCategoryId,
                                accommodation.Room.RoomTypeId,
                                currentPeriodStart,
                                cancellationToken);

                        if (feeRule != null)
                        {
                            var charge =
                                CreateFeeCharge(
                                    studentId,
                                    accommodation,
                                    feeRule,
                                    currentPeriodStart,
                                    periodEnd);

                            await db.FeeCharges.AddAsync(
                                charge,
                                cancellationToken);

                            createdCharges.Add(charge);

                            existingPeriodStarts.Add(
                                currentPeriodStart);
                        }
                    }
                }

                currentPeriodStart =
                    currentPeriodStart.AddMonths(1);
            }

            return createdCharges;
        }


        // =========================================================
        // BUILD PREVIEW
        //
        // This performs exactly the same billing calculation as
        // actual charge generation, but creates NO EF entities and
        // performs NO SaveChanges().
        // =========================================================

        private async Task<List<FeeChargePreview>>
            BuildChargePreviewAsync(
                AppDbContext db,
                int studentId,
                DateOnly through,
                CancellationToken cancellationToken)
        {
            var student = await db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Id == studentId,
                    cancellationToken);

            if (student == null)
            {
                throw new InvalidOperationException(
                    "Student does not exist.");
            }

            if (!student.IsActive)
            {
                throw new InvalidOperationException(
                    "Cannot calculate charges for an inactive student.");
            }

            var accommodations = await db.StudentAccommodations
                .AsNoTracking()
                .Include(a => a.Room)
                    .ThenInclude(r => r.RoomType)
                .Where(a => a.StudentId == studentId)
                .OrderBy(a => a.StartDate)
                .ThenBy(a => a.Id)
                .ToListAsync(cancellationToken);

            if (accommodations.Count == 0)
                return new List<FeeChargePreview>();

            var billingAnchor =
                accommodations.Min(a => a.StartDate);

            var existingPeriodStarts =
                await db.FeeCharges
                    .AsNoTracking()
                    .Where(fc => fc.StudentId == studentId)
                    .Select(fc => fc.PeriodStart)
                    .ToListAsync(cancellationToken);

            var existingPeriods =
                existingPeriodStarts.ToHashSet();

            var previews =
                new List<FeeChargePreview>();

            var currentPeriodStart =
                billingAnchor;

            while (currentPeriodStart <= through)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var periodEnd =
                    currentPeriodStart
                        .AddMonths(1)
                        .AddDays(-1);

                // Existing charge means there is nothing to preview
                // for this billing period.
                if (!existingPeriods.Contains(currentPeriodStart))
                {
                    var accommodation =
                        FindAccommodationForBillingPeriod(
                            accommodations,
                            currentPeriodStart);

                    if (accommodation != null)
                    {
                        var feeRule =
                            await FindFeeRuleAsync(
                                db,
                                student.StudentCategoryId,
                                accommodation.Room.RoomTypeId,
                                currentPeriodStart,
                                cancellationToken);

                        if (feeRule != null)
                        {
                            var calculation =
                                CalculateCharge(
                                    accommodation,
                                    feeRule,
                                    currentPeriodStart,
                                    periodEnd);

                            if (calculation != null)
                            {
                                previews.Add(
                                    new FeeChargePreview
                                    {
                                        StudentId =
                                            studentId,

                                        StudentAccommodationId =
                                            accommodation.Id,

                                        FeeRuleId =
                                            feeRule.Id,

                                        PeriodStart =
                                            currentPeriodStart,

                                        PeriodEnd =
                                            periodEnd,

                                        DueDate =
                                            currentPeriodStart,

                                        Amount =
                                            calculation.Amount,

                                        IsProrated =
                                            calculation.IsProrated,

                                        ChargedDays =
                                            calculation.ChargedDays,

                                        PeriodDays =
                                            calculation.PeriodDays
                                    });
                            }
                        }
                    }
                }

                currentPeriodStart =
                    currentPeriodStart.AddMonths(1);
            }

            return previews;
        }


        // =========================================================
        // FIND ACCOMMODATION
        // =========================================================

        private static StudentAccommodation?
            FindAccommodationForBillingPeriod(
                List<StudentAccommodation> accommodations,
                DateOnly periodStart)
        {
            return accommodations
                .Where(a =>
                    a.StartDate <= periodStart &&
                    (
                        a.EndDate == null ||
                        a.EndDate.Value >= periodStart
                    ))
                .OrderByDescending(a => a.StartDate)
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();
        }


        // =========================================================
        // FIND FEE RULE
        // =========================================================

        private static async Task<FeeRule?> FindFeeRuleAsync(
            AppDbContext db,
            int studentCategoryId,
            int roomTypeId,
            DateOnly periodStart,
            CancellationToken cancellationToken)
        {
            var periodDate =
                periodStart.ToDateTime(
                    TimeOnly.MinValue);

            return await db.FeeRules
                .Where(fr =>
                    fr.StudentCategoryId ==
                    studentCategoryId &&

                    fr.RoomTypeId ==
                    roomTypeId &&

                    fr.IsActive &&

                    fr.EffectiveFrom <=
                    periodDate &&

                    (
                        fr.EffectiveTo == null ||
                        fr.EffectiveTo >= periodDate
                    ))
                .OrderByDescending(fr => fr.EffectiveFrom)
                .ThenByDescending(fr => fr.Id)
                .FirstOrDefaultAsync(
                    cancellationToken);
        }


        // =========================================================
        // CHARGE CALCULATION
        // =========================================================

        private sealed class ChargeCalculation
        {
            public decimal Amount { get; init; }
            public bool IsProrated { get; init; }
            public int ChargedDays { get; init; }
            public int PeriodDays { get; init; }
        }


        private static ChargeCalculation?
            CalculateCharge(
                StudentAccommodation accommodation,
                FeeRule feeRule,
                DateOnly periodStart,
                DateOnly periodEnd)
        {
            var periodDays =
                periodEnd.DayNumber -
                periodStart.DayNumber +
                1;

            var chargedStart =
                periodStart > accommodation.StartDate
                    ? periodStart
                    : accommodation.StartDate;

            var chargedEnd =
                accommodation.EndDate.HasValue &&
                accommodation.EndDate.Value < periodEnd
                    ? accommodation.EndDate.Value
                    : periodEnd;

            if (chargedEnd < chargedStart)
                return null;

            var chargedDays =
                chargedEnd.DayNumber -
                chargedStart.DayNumber +
                1;

            var isProrated =
                chargedDays < periodDays;

            var amount =
                isProrated
                    ? Math.Round(
                        feeRule.MonthlyAmount *
                        chargedDays /
                        periodDays,
                        2,
                        MidpointRounding.AwayFromZero)
                    : feeRule.MonthlyAmount;

            return new ChargeCalculation
            {
                Amount = amount,
                IsProrated = isProrated,
                ChargedDays = chargedDays,
                PeriodDays = periodDays
            };
        }


        // =========================================================
        // CREATE FEE CHARGE
        // =========================================================

        private static FeeCharge CreateFeeCharge(
            int studentId,
            StudentAccommodation accommodation,
            FeeRule feeRule,
            DateOnly periodStart,
            DateOnly periodEnd)
        {
            var calculation =
                CalculateCharge(
                    accommodation,
                    feeRule,
                    periodStart,
                    periodEnd);

            if (calculation == null)
            {
                return new FeeCharge
                {
                    StudentId =
                        studentId,

                    StudentAccommodationId =
                        accommodation.Id,

                    FeeRuleId =
                        feeRule.Id,

                    PeriodStart =
                        periodStart,

                    PeriodEnd =
                        periodEnd,

                    Amount =
                        0m,

                    DueDate =
                        periodStart,

                    Status =
                        FeeChargeStatus.Paid,

                    IsProrated =
                        false,

                    ChargedDays =
                        0,

                    PeriodDays =
                        calculation?.PeriodDays ?? 0,

                    CreatedAt =
                        DateTime.Now
                };
            }

            return new FeeCharge
            {
                StudentId =
                    studentId,

                StudentAccommodationId =
                    accommodation.Id,

                FeeRuleId =
                    feeRule.Id,

                PeriodStart =
                    periodStart,

                PeriodEnd =
                    periodEnd,

                Amount =
                    calculation.Amount,

                DueDate =
                    periodStart,

                Status =
                    calculation.Amount <= 0
                        ? FeeChargeStatus.Paid
                        : FeeChargeStatus.Unpaid,

                IsProrated =
                    calculation.IsProrated,

                ChargedDays =
                    calculation.ChargedDays,

                PeriodDays =
                    calculation.PeriodDays,

                CreatedAt =
                    DateTime.Now
            };
        }
    }
}