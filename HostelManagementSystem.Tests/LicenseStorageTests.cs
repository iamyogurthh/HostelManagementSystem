using System;
using System.IO;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using Xunit;

namespace HostelManagementSystem.Tests;

public class LicenseStorageTests
{
    [Fact]
    public void NewStorage_ShouldNotThrow()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                $@"Software\HostelManagementSystem.Tests\{Guid.NewGuid()}";

            // Act
            var exception = Record.Exception(
                () => new LicenseStorage(
                    licenseFile,
                    registryPath
                )
            );

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void Load_WhenLicenseDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            // Act
            StoredLicense? license =
                storage.Load();

            // Assert
            Assert.Null(license);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void Save_ThenLoad_ShouldReturnSameLicenseInformation()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var license = new LicenseInfo
            {
                LicenseKey = "TEST-LICENSE-123",
                Status = "ACTIVE",
                ExpiresAt =
                    DateTime.UtcNow.AddDays(365),
                ProductName =
                    "Hostel Management System",
                CustomerName =
                    "Test Customer"
            };

            string machineId =
                "TEST-MACHINE-123";

            // Act
            storage.Save(
                license,
                machineId
            );

            StoredLicense? loaded =
                storage.Load();

            // Assert
            Assert.NotNull(loaded);

            Assert.Equal(
                license.LicenseKey,
                loaded.LicenseKey
            );

            Assert.Equal(
                machineId,
                loaded.MachineId
            );

            Assert.Equal(
                license.Status,
                loaded.Status
            );

            Assert.Equal(
                license.ExpiresAt,
                loaded.ExpiresAt
            );

            Assert.Equal(
                license.ProductName,
                loaded.ProductName
            );

            Assert.Equal(
                license.CustomerName,
                loaded.CustomerName
            );

            Assert.NotEqual(
                default,
                loaded.FirstActivatedUtc
            );

            Assert.NotEqual(
                default,
                loaded.LastSeenUtc
            );

            Assert.NotNull(
                loaded.LastValidatedUtc
            );

            Assert.NotEqual(
                default,
                loaded.SavedAt
            );

            Assert.True(
                storage.Exists()
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void UpdateAfterValidation_ShouldUpdateValidationInformation()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var storedLicense = new StoredLicense
            {
                LicenseKey =
                    "TEST-LICENSE-123",

                MachineId =
                    "TEST-MACHINE-123",

                Status =
                    "ACTIVE",

                ExpiresAt =
                    null,

                ProductName =
                    "Old Product",

                CustomerName =
                    "Old Customer",

                FirstActivatedUtc =
                    DateTime.UtcNow.AddDays(-10),

                LastSeenUtc =
                    DateTime.UtcNow.AddDays(-1),

                LastValidatedUtc =
                    DateTime.UtcNow.AddDays(-1),

                SavedAt =
                    DateTime.UtcNow.AddDays(-1)
            };

            var initialLicense =
                new LicenseInfo
                {
                    LicenseKey =
                        storedLicense.LicenseKey,

                    Status =
                        storedLicense.Status,

                    ExpiresAt =
                        storedLicense.ExpiresAt,

                    ProductName =
                        storedLicense.ProductName,

                    CustomerName =
                        storedLicense.CustomerName
                };

            storage.Save(
                initialLicense,
                storedLicense.MachineId
            );

            DateTime validationTime =
                DateTime.UtcNow;

            var serverLicense =
                new LicenseInfo
                {
                    LicenseKey =
                        "TEST-LICENSE-123",

                    Status =
                        "ACTIVE",

                    ExpiresAt =
                        validationTime.AddDays(365),

                    ProductName =
                        "Hostel Management System",

                    CustomerName =
                        "Test Customer"
                };

            StoredLicense? loaded =
                storage.Load();

            Assert.NotNull(loaded);

            // Act
            storage.UpdateAfterValidation(
                loaded,
                serverLicense,
                validationTime
            );

            StoredLicense? updated =
                storage.Load();

            // Assert
            Assert.NotNull(updated);

            Assert.Equal(
                "ACTIVE",
                updated.Status
            );

            Assert.Equal(
                serverLicense.ExpiresAt,
                updated.ExpiresAt
            );

            Assert.Equal(
                "Hostel Management System",
                updated.ProductName
            );

            Assert.Equal(
                "Test Customer",
                updated.CustomerName
            );

            Assert.Equal(
                validationTime,
                updated.LastValidatedUtc
            );

            Assert.Equal(
                validationTime,
                updated.LastSeenUtc
            );

            Assert.Equal(
                validationTime,
                updated.SavedAt
            );

            Assert.Equal(
                "TEST-LICENSE-123",
                updated.LicenseKey
            );

            Assert.Equal(
                "TEST-MACHINE-123",
                updated.MachineId
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void Delete_ShouldRemoveOnlyTestLicenseFile()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var license =
                new LicenseInfo
                {
                    LicenseKey =
                        "TEST-LICENSE-123",

                    Status =
                        "ACTIVE",

                    ProductName =
                        "Hostel Management System",

                    CustomerName =
                        "Test Customer"
                };

            storage.Save(
                license,
                "TEST-MACHINE-123"
            );

            Assert.True(
                storage.Exists()
            );

            // Act
            storage.Delete();

            // Assert
            Assert.False(
                storage.Exists()
            );

            Assert.Null(
                storage.Load()
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void Exists_ShouldMatchLicenseFileState()
    {
        // Arrange
        string testDirectory = CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(testDirectory, "license.dat");

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            // Initially no file
            Assert.False(
                storage.Exists()
            );

            var license =
                new LicenseInfo
                {
                    LicenseKey =
                        "TEST-LICENSE-123",

                    Status =
                        "ACTIVE"
                };

            // Save test license
            storage.Save(
                license,
                "TEST-MACHINE-123"
            );

            Assert.True(
                storage.Exists()
            );

            // Delete test license
            storage.Delete();

            Assert.False(
                storage.Exists()
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    private static string CreateTestDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "HostelManagementSystem.Tests",
                Guid.NewGuid().ToString()
            );

        Directory.CreateDirectory(
            directory
        );

        return directory;
    }

    private static string CreateTestRegistryPath()
    {
        return
            $@"Software\HostelManagementSystem.Tests\{Guid.NewGuid()}";
    }

    private static void DeleteTestDirectory(
        string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    recursive: true
                );
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }
}