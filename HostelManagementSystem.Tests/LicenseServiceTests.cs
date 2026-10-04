using System;
using System.IO;
using System.Threading.Tasks;
using HostelManagementSystem.Models;
using HostelManagementSystem.Services;
using Xunit;

namespace HostelManagementSystem.Tests;

public class LicenseServiceTests
{
    private const string TestLicenseKey =
        "HMS-AQE7X-EYFQG-9ADDS-5XZZ9";

    [Fact]
    public async Task ActivateAsync_WithInvalidLicense_ShouldFail()
    {
        // Arrange
        string testDirectory =
            CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(
                    testDirectory,
                    "license.dat"
                );

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var service =
                new LicenseService(
                    storage
                );

            const string invalidLicenseKey =
                "HMS-INVALID-TEST-LICENSE-99999";

            // Act
            LicenseResult result =
                await service.ActivateAsync(
                    invalidLicenseKey
                );

            // Assert
            Assert.False(
                result.IsSuccess
            );

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Message
                )
            );

            Assert.False(
                storage.Exists()
            );
        }
        finally
        {
            DeleteTestDirectory(
                testDirectory
            );
        }
    }

    [Fact]
    public async Task ActivateAsync_WithEmptyLicense_ShouldFail()
    {
        // Arrange
        string testDirectory =
            CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(
                    testDirectory,
                    "license.dat"
                );

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var service =
                new LicenseService(
                    storage
                );

            // Act
            LicenseResult result =
                await service.ActivateAsync(
                    string.Empty
                );

            // Assert
            Assert.False(
                result.IsSuccess
            );

            Assert.Equal(
                "Please enter a license key.",
                result.Message
            );

            Assert.False(
                storage.Exists()
            );
        }
        finally
        {
            DeleteTestDirectory(
                testDirectory
            );
        }
    }

    [Fact]
    public async Task ActivateAsync_WithValidTestLicense_ShouldActivateAndStoreLicense()
    {
        // Arrange
        string testDirectory =
            CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(
                    testDirectory,
                    "license.dat"
                );

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var service =
                new LicenseService(
                    storage
                );

            // Act
            LicenseResult result =
                await service.ActivateAsync(
                    TestLicenseKey
                );

            // Assert
            Assert.True(
                result.IsSuccess,
                $"Activation failed: {result.Message}"
            );

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Message
                )
            );

            Assert.True(
                storage.Exists()
            );

            StoredLicense? storedLicense =
                storage.Load();

            Assert.NotNull(
                storedLicense
            );

            Assert.Equal(
                TestLicenseKey,
                storedLicense.LicenseKey
            );

            Assert.Equal(
                "ACTIVE",
                storedLicense.Status,
                ignoreCase: true
            );

            Assert.False(
                string.IsNullOrWhiteSpace(
                    storedLicense.MachineId
                )
            );

            Assert.NotEqual(
                default,
                storedLicense.FirstActivatedUtc
            );

            Assert.NotEqual(
                default,
                storedLicense.LastSeenUtc
            );

            Assert.NotNull(
                storedLicense.LastValidatedUtc
            );

            Assert.NotEqual(
                default,
                storedLicense.SavedAt
            );
        }
        finally
        {
            DeleteTestDirectory(
                testDirectory
            );
        }
    }

    [Fact]
    public async Task ValidateOnlineAsync_WithValidTestLicense_ShouldSucceed()
    {
        // Arrange
        string testDirectory =
            CreateTestDirectory();

        try
        {
            string licenseFile =
                Path.Combine(
                    testDirectory,
                    "license.dat"
                );

            string registryPath =
                CreateTestRegistryPath();

            var storage =
                new LicenseStorage(
                    licenseFile,
                    registryPath
                );

            var service =
                new LicenseService(
                    storage
                );

            // -------------------------------------------------
            // Create a local stored license.
            //
            // We use the actual machine ID so that the local
            // machine-binding check succeeds.
            // -------------------------------------------------

            string machineId =
                service.GetMachineId();

            var license =
                new LicenseInfo
                {
                    LicenseKey =
                        TestLicenseKey,

                    Status =
                        "ACTIVE",

                    ProductName =
                        "Hostel Management System"
                };

            storage.Save(
                license,
                machineId
            );

            StoredLicense? beforeValidation =
                storage.Load();

            Assert.NotNull(
                beforeValidation
            );

            DateTime? previousValidation =
                beforeValidation.LastValidatedUtc;

            // Act
            LicenseValidationResult result =
                await service.ValidateOnlineAsync();

            // Assert
            Assert.True(
                result.IsSuccess,
                $"Validation failed: {result.Message}"
            );

            Assert.False(
                result.IsOffline
            );

            Assert.False(
                result.IsServerRejected
            );

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Message
                )
            );

            Assert.NotNull(
                result.License
            );

            Assert.Equal(
                TestLicenseKey,
                result.License.LicenseKey
            );

            Assert.Equal(
                "ACTIVE",
                result.License.Status,
                ignoreCase: true
            );

            // -------------------------------------------------
            // Verify local validation timestamp was updated.
            // -------------------------------------------------

            StoredLicense? afterValidation =
                storage.Load();

            Assert.NotNull(
                afterValidation
            );

            Assert.NotNull(
                afterValidation.LastValidatedUtc
            );

            Assert.True(
                afterValidation.LastValidatedUtc >
                previousValidation
            );

            Assert.Equal(
                machineId,
                afterValidation.MachineId
            );

            Assert.Equal(
                TestLicenseKey,
                afterValidation.LicenseKey
            );

            Assert.Equal(
                "ACTIVE",
                afterValidation.Status,
                ignoreCase: true
            );
        }
        finally
        {
            DeleteTestDirectory(
                testDirectory
            );
        }
    }

    [Fact]
    public async Task ValidateOnlineAsync_WhenServerIsUnavailable_ShouldContinueOffline()
    {
        string testDirectory =
            CreateTestDirectory();

        string licenseFilePath =
            Path.Combine(
                testDirectory,
                "license.dat"
            );

        string registryPath =
            $@"Software\HostelManagementSystem\Tests\{Guid.NewGuid():N}";

        try
        {
            var storage =
                new LicenseStorage(
                    licenseFilePath,
                    registryPath
                );

            var machineIdService =
                new MachineIdService();

            string machineId =
                machineIdService.GetMachineId();

            var licenseInfo =
                new LicenseInfo
                {
                    LicenseKey =
                        TestLicenseKey,

                    Status =
                        "ACTIVE",

                    ExpiresAt =
                        null,

                    ProductName =
                        "Hostel Management System",

                    CustomerName =
                        "Test Customer"
                };

            storage.Save(
                licenseInfo,
                machineId
            );

            StoredLicense? beforeValidation =
                storage.Load();

            Assert.NotNull(beforeValidation);

            DateTime? lastValidatedBefore =
                beforeValidation!.LastValidatedUtc;

            // -------------------------------------------------
            // Use an unreachable local endpoint.
            // This simulates the licensing server being offline.
            // -------------------------------------------------

            var service =
                new LicenseService(
                    storage,
                    "http://127.0.0.1:1/api/licenses/validate"
                );

            LicenseValidationResult result =
                await service.ValidateOnlineAsync();

            // -------------------------------------------------
            // VALIDATE OFFLINE BEHAVIOR
            // -------------------------------------------------

            Assert.False(result.IsSuccess);

            Assert.True(result.IsOffline);

            Assert.False(result.IsServerRejected);

            Assert.NotEmpty(result.Message);

            // -------------------------------------------------
            // LastValidatedUtc MUST NOT change.
            //
            // A failed network request is not a successful
            // server validation.
            // -------------------------------------------------

            StoredLicense? afterValidation =
                storage.Load();

            Assert.NotNull(afterValidation);

            Assert.Equal(
                lastValidatedBefore,
                afterValidation!.LastValidatedUtc
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public void GetLicenseState_WhenLicenseIsExpired_ShouldReturnExpired()
    {
        string testDirectory = CreateTestDirectory();

        string licenseFilePath =
            Path.Combine(
                testDirectory,
                "license.dat"
            );

        string registryPath =
            $@"Software\HostelManagementSystem\Tests\{Guid.NewGuid():N}";

        try
        {
            var storage =
                new LicenseStorage(
                    licenseFilePath,
                    registryPath
                );

            var machineIdService =
                new MachineIdService();

            string machineId =
                machineIdService.GetMachineId();

            var licenseInfo = new LicenseInfo
            {
                LicenseKey = "HMS-TEST-EXPIRED",
                Status = "ACTIVE",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                ProductName = "Hostel Management System",
                CustomerName = "Test Customer"
            };

            storage.Save(
                licenseInfo,
                machineId
            );

            var service =
                new LicenseService(storage);

            LicenseState state =
                service.GetLicenseState();

            Assert.Equal(
                LicenseState.Expired,
                state
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [Fact]
    public async Task ValidateOnlineAsync_WhenServerLicenseIsExpired_ShouldReject()
    {
        string testDirectory = CreateTestDirectory();

        string licenseFilePath =
            Path.Combine(
                testDirectory,
                "license.dat"
            );

        string registryPath =
            $@"Software\HostelManagementSystem\Tests\{Guid.NewGuid():N}";

        try
        {
            var storage =
                new LicenseStorage(
                    licenseFilePath,
                    registryPath
                );

            var machineIdService =
                new MachineIdService();

            string machineId =
                machineIdService.GetMachineId();

            var licenseInfo = new LicenseInfo
            {
                LicenseKey = "HMS-GT5DX-SW2U3-B7E4L-RLFFM",
                Status = "ACTIVE",
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                ProductName = "Hostel Management System",
                CustomerName = "Expired Test Customer"
            };

            storage.Save(
                licenseInfo,
                machineId
            );

            var service =
                new LicenseService(storage);

            LicenseValidationResult result =
                await service.ValidateOnlineAsync();

            Assert.False(result.IsSuccess);
            Assert.False(result.IsOffline);
            Assert.True(result.IsServerRejected);

            Assert.Contains(
                "expired",
                result.Message,
                StringComparison.OrdinalIgnoreCase
            );

            Assert.Equal(
                "EXPIRED",
                result.License?.Status
            );
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }
    /// <summary>
    /// Helper method to create a unique temporary test directory for storing license files and registry keys during tests.
    /// </summary>
    /// <returns></returns>
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