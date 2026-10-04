using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using Xunit;

namespace HostelManagementSystem.Tests;

public class LicenseResultTests
{
    [Fact]
    public void LicenseResult_Success_ShouldSetSuccessTrue()
    {
        // Act
        LicenseResult result =
            LicenseResult.Success("Activation successful.");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            "Activation successful.",
            result.Message
        );
    }

    [Fact]
    public void LicenseResult_Failed_ShouldSetSuccessFalse()
    {
        // Act
        LicenseResult result =
            LicenseResult.Failed("Invalid license.");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Invalid license.",
            result.Message
        );
    }

    [Fact]
    public void ValidationResult_Success_ShouldSetCorrectState()
    {
        // Arrange
        var license = new LicenseInfo
        {
            LicenseKey = "TEST-LICENSE",
            Status = "ACTIVE",
            ProductName = "Hostel Management System"
        };

        // Act
        LicenseValidationResult result =
            LicenseValidationResult.Success(
                "License is valid.",
                license
            );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsOffline);
        Assert.False(result.IsServerRejected);
        Assert.Equal(
            "License is valid.",
            result.Message
        );
        Assert.Same(
            license,
            result.License
        );
    }

    [Fact]
    public void ValidationResult_Offline_ShouldSetOfflineTrue()
    {
        // Act
        LicenseValidationResult result =
            LicenseValidationResult.Offline(
                "Unable to connect."
            );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsOffline);
        Assert.False(result.IsServerRejected);
        Assert.Equal(
            "Unable to connect.",
            result.Message
        );
        Assert.Null(result.License);
    }

    [Fact]
    public void ValidationResult_ServerRejected_ShouldSetRejectedTrue()
    {
        // Arrange
        var license = new LicenseInfo
        {
            LicenseKey = "TEST-LICENSE",
            Status = "SUSPENDED"
        };

        // Act
        LicenseValidationResult result =
            LicenseValidationResult.ServerRejected(
                "License is suspended.",
                license
            );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOffline);
        Assert.True(result.IsServerRejected);
        Assert.Equal(
            "License is suspended.",
            result.Message
        );
        Assert.Same(
            license,
            result.License
        );
    }

    [Fact]
    public void ValidationResult_ServerRejected_ShouldAllowNullLicense()
    {
        // This specifically protects the INSTALLATION_BLOCKED
        // scenario that previously caused a problem.

        // Act
        LicenseValidationResult result =
            LicenseValidationResult.ServerRejected(
                "Installation is blocked.",
                null
            );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOffline);
        Assert.True(result.IsServerRejected);
        Assert.Equal(
            "Installation is blocked.",
            result.Message
        );
        Assert.Null(result.License);
    }

    [Fact]
    public void ValidationResult_Failed_ShouldSetAllFlagsFalse()
    {
        // Act
        LicenseValidationResult result =
            LicenseValidationResult.Failed(
                "Validation failed."
            );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOffline);
        Assert.False(result.IsServerRejected);
        Assert.Equal(
            "Validation failed.",
            result.Message
        );
        Assert.Null(result.License);
    }
}