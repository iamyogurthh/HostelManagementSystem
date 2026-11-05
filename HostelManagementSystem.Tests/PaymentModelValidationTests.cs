using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HostelManagementSystem.Database;
using HostelManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HostelManagementSystem.Tests;

public class PaymentModelValidationTests
{
    [Fact]
    public void Payment_WithInvalidValues_ReturnsValidationErrors()
    {
        var payment = new Payment
        {
            StudentId = 0,
            PaymentDate = default,
            TotalAmount = 0m,
            PaymentMethod = (PaymentMethod)999,
            ReceiptNumber = "valid-receipt"
        };

        var errors = Validate(payment);

        Assert.Contains(nameof(Payment.StudentId), errors.SelectMany(e => e.MemberNames));
        Assert.Contains(nameof(Payment.PaymentDate), errors.SelectMany(e => e.MemberNames));
        Assert.Contains(nameof(Payment.TotalAmount), errors.SelectMany(e => e.MemberNames));
        Assert.Contains(nameof(Payment.PaymentMethod), errors.SelectMany(e => e.MemberNames));
    }

    [Fact]
    public void ManualFeeCharge_WithoutDescription_ReturnsValidationError()
    {
        var charge = new FeeCharge
        {
            StudentId = 1,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 5),
            Status = FeeChargeStatus.Unpaid,
            Source = FeeChargeSource.Manual
        };

        var errors = Validate(charge);

        Assert.Contains(
            nameof(FeeCharge.Description),
            errors.SelectMany(error => error.MemberNames));
    }

    [Fact]
    public void ManualFeeCharge_WithDescription_PassesValidation()
    {
        var charge = new FeeCharge
        {
            StudentId = 1,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 5),
            Status = FeeChargeStatus.Unpaid,
            Source = FeeChargeSource.Manual,
            Description = "Manual accommodation charge"
        };

        Assert.Empty(Validate(charge));
    }

    [Fact]
    public void Payment_WithBlankReceiptNumber_ReturnsValidationError()
    {
        var payment = new Payment
        {
            StudentId = 1,
            PaymentDate = new DateTime(2026, 1, 15),
            TotalAmount = 100m,
            PaymentMethod = PaymentMethod.Cash,
            ReceiptNumber = "   "
        };

        var errors = Validate(payment);

        Assert.Contains(nameof(Payment.ReceiptNumber), errors.SelectMany(e => e.MemberNames));
    }

    [Fact]
    public void PaymentAllocation_WithNegativeAmount_ReturnsValidationError()
    {
        var allocation = new PaymentAllocation
        {
            PaymentId = 1,
            FeeChargeId = 1,
            Amount = -1m
        };

        var errors = Validate(allocation);

        Assert.Contains(
            nameof(PaymentAllocation.Amount),
            errors.SelectMany(error => error.MemberNames));
    }

    [Fact]
    public void PaymentAllocation_WithInvalidValues_ReturnsValidationErrors()
    {
        var allocation = new PaymentAllocation
        {
            PaymentId = 0,
            FeeChargeId = 0,
            Amount = 1m
        };

        var errors = Validate(allocation);

        Assert.Contains(nameof(PaymentAllocation.PaymentId), errors.SelectMany(e => e.MemberNames));
        Assert.Contains(nameof(PaymentAllocation.FeeChargeId), errors.SelectMany(e => e.MemberNames));
    }

    [Fact]
    public void FeeCharge_WithReversedPeriod_ReturnsValidationError()
    {
        var charge = new FeeCharge
        {
            StudentId = 1,
            PeriodStart = new DateOnly(2026, 2, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Amount = 100m,
            DueDate = new DateOnly(2026, 2, 5),
            Status = FeeChargeStatus.Unpaid
        };

        var errors = Validate(charge);

        Assert.Contains(
            errors,
            error => error.MemberNames.Contains(nameof(FeeCharge.PeriodStart)) &&
                     error.MemberNames.Contains(nameof(FeeCharge.PeriodEnd)));
    }

    [Fact]
    public void PaymentModels_WithValidValues_PassValidation()
    {
        var payment = new Payment
        {
            StudentId = 1,
            PaymentDate = new DateTime(2026, 1, 15),
            TotalAmount = 100m,
            PaymentMethod = PaymentMethod.Cash,
            ReceiptNumber = "R-1001"
        };
        var allocation = new PaymentAllocation
        {
            PaymentId = 1,
            FeeChargeId = 1,
            Amount = 100m
        };
        var charge = new FeeCharge
        {
            StudentId = 1,
            PeriodStart = new DateOnly(2026, 1, 1),
            PeriodEnd = new DateOnly(2026, 1, 31),
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 5),
            Status = FeeChargeStatus.Unpaid,
            Source = FeeChargeSource.Manual,
            Description = "Accommodation charge"
        };

        Assert.Empty(Validate(payment));
        Assert.Empty(Validate(allocation));
        Assert.Empty(Validate(charge));
    }

    [Fact]
    public void PaymentModel_ContainsExpectedMigrationMetadata()
    {
        using var db = new AppDbContext();

        var payment = db.Model.FindEntityType(typeof(Payment));
        var allocation = db.Model.FindEntityType(typeof(PaymentAllocation));
        var charge = db.Model.FindEntityType(typeof(FeeCharge));

        Assert.NotNull(payment);
        Assert.NotNull(allocation);
        Assert.NotNull(charge);
        Assert.Equal("Payments", payment!.GetTableName());
        Assert.Equal("PaymentAllocations", allocation!.GetTableName());
        Assert.Equal("FeeCharges", charge!.GetTableName());
        Assert.Equal(18, payment.FindProperty(nameof(Payment.TotalAmount))!.GetPrecision());
        Assert.Equal(2, payment.FindProperty(nameof(Payment.TotalAmount))!.GetScale());
        Assert.Equal(18, allocation.FindProperty(nameof(PaymentAllocation.Amount))!.GetPrecision());
        Assert.Equal(2, allocation.FindProperty(nameof(PaymentAllocation.Amount))!.GetScale());

        Assert.Contains(
            allocation.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Payment));
        Assert.Contains(
            allocation.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(FeeCharge));

        Assert.Contains(
            payment.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Any(property =>
                         property.Name == nameof(Payment.ReceiptNumber)));

        Assert.Contains(
            allocation.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(property => property.Name)
                         .SequenceEqual(new[]
                         {
                             nameof(PaymentAllocation.PaymentId),
                             nameof(PaymentAllocation.FeeChargeId)
                         }));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            errors,
            validateAllProperties: true);
        return errors;
    }
}
