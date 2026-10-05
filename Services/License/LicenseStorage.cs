using HostelManagementSystem.Models;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Documents;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace HostelManagementSystem.Services;

public class LicenseStorage
{
    private const string ApplicationFolder =
        "HostelManagementSystem";

    private const string LicenseFileName =
        "license.dat";

    private const string DefaultRegistryPath =
        @"Software\HostelManagementSystem";

    private readonly string _registryPath;

    private const string RegistryMarkerName =
        "ActivationMarker";

    private const string RegistryLastSeenName =
        "LastSeenUtc";

    private readonly string _licenseFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LicenseStorage(
        string? licenseFilePath = null,
        string? registryPath = null)
    {
        _registryPath =
            string.IsNullOrWhiteSpace(registryPath)
                ? DefaultRegistryPath
                : registryPath;

        if (!string.IsNullOrWhiteSpace(licenseFilePath))
        {
            string? directory =
                Path.GetDirectoryName(licenseFilePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _licenseFilePath =
                licenseFilePath;

            return;
        }

        string appDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData
            );

        string applicationFolderPath =
            Path.Combine(
                appDataPath,
                ApplicationFolder
            );

        Directory.CreateDirectory(
            applicationFolderPath
        );

        _licenseFilePath =
            Path.Combine(
                applicationFolderPath,
                LicenseFileName
            );
    }


    // =========================================================
    // SAVE LICENSE
    // =========================================================

    public void Save(
        LicenseInfo license,
        string machineId)
    {
        DateTime nowUtc =
            DateTime.UtcNow;

        var storedLicense =
            new StoredLicense
            {
                LicenseKey =
                    license.LicenseKey,

                LicenseType =
                    license.LicenseType,

                MachineId =
                    machineId,

                Status =
                    license.Status,

                ExpiresAt =
                    license.ExpiresAt,

                ProductName =
                    license.ProductName,

                CustomerName =
                    license.CustomerName,

                FirstActivatedUtc =
                    nowUtc,

                LastSeenUtc =
                    nowUtc,

                LastValidatedUtc =
                    nowUtc,

                SavedAt =
                    nowUtc,

            };

        SaveEncrypted(
            storedLicense
        );

        CreateOrUpdateRegistryMarker(
            machineId,
            nowUtc
        );
    }


    // =========================================================
    // LOAD LICENSE
    // =========================================================

    public StoredLicense? Load()
    {
        try
        {
            if (!File.Exists(
                    _licenseFilePath))
            {
                return null;
            }

            byte[] encryptedBytes =
                File.ReadAllBytes(
                    _licenseFilePath
                );

            byte[] plainBytes =
                ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser
                );

            string json =
                Encoding.UTF8.GetString(
                    plainBytes
                );

            return JsonSerializer.Deserialize<StoredLicense>(
                json,
                JsonOptions
            );
        }
        catch
        {
            return null;
        }
    }


    // =========================================================
    // UPDATE LAST SEEN
    // =========================================================

    public void UpdateLastSeen(
        StoredLicense license,
        DateTime currentUtc)
    {
        license.LastSeenUtc =
            currentUtc;

        license.SavedAt =
            currentUtc;

        SaveEncrypted(
            license
        );

        UpdateRegistryLastSeen(
            currentUtc
        );
    }


    // =========================================================
    // UPDATE AFTER SUCCESSFUL SERVER VALIDATION
    // =========================================================

    public void UpdateAfterValidation(
        StoredLicense license,
        LicenseInfo serverLicense,
        DateTime validatedUtc)
    {
        // -----------------------------------------------------
        // Always update the server-controlled license state.
        // -----------------------------------------------------

        license.Status =
            serverLicense.Status;

        license.ExpiresAt =
            serverLicense.ExpiresAt;

        if (!string.IsNullOrWhiteSpace(serverLicense.LicenseType))
        {
            license.LicenseType =
            serverLicense.LicenseType;
        }

        // -----------------------------------------------------
        // Update ProductName only when the server actually
        // provides a value.
        //
        // This prevents an accidental null/empty API response
        // from deleting existing local license information.
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                serverLicense.ProductName))
        {
            license.ProductName =
                serverLicense.ProductName;
        }


        // -----------------------------------------------------
        // Update CustomerName only when the server actually
        // provides a value.
        //
        // This is important for protecting the locally stored
        // customer information.
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                serverLicense.CustomerName))
        {
            license.CustomerName =
                serverLicense.CustomerName;
        }

        // -----------------------------------------------------
        // Update LicenseType only when the server actually
        // provides a value.
        //
        // This is important for protecting the locally stored
        // license information.
        // -----------------------------------------------------
        if (!string.IsNullOrWhiteSpace(serverLicense.LicenseType))
        {
            license.LicenseType =
                serverLicense.LicenseType;
        }

        // -----------------------------------------------------
        // Update validation timestamps.
        // -----------------------------------------------------

        license.LastValidatedUtc =
            validatedUtc;

        license.LastSeenUtc =
            validatedUtc;

        license.SavedAt =
            validatedUtc;


        // -----------------------------------------------------
        // Save updated license.
        // -----------------------------------------------------

        SaveEncrypted(
            license
        );


        // -----------------------------------------------------
        // Update registry last-seen timestamp.
        // -----------------------------------------------------

        UpdateRegistryLastSeen(
            validatedUtc
        );
    }


    // =========================================================
    // CHECK REGISTRY MARKER
    // =========================================================

    public bool HasRegistryMarker()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    _registryPath
                );

            if (key == null)
            {
                return false;
            }

            string? marker =
                key.GetValue(
                    RegistryMarkerName
                )?.ToString();

            return !string.IsNullOrWhiteSpace(
                marker
            );
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // GET REGISTRY LAST SEEN
    // =========================================================

    public DateTime? GetRegistryLastSeen()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    _registryPath
                );

            string? value =
                key?.GetValue(
                    RegistryLastSeenName
                )?.ToString();

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            if (DateTime.TryParse(
                    value,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime result))
            {
                return result.ToUniversalTime();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }


    // =========================================================
    // DELETE LICENSE FILE
    // =========================================================

    public void Delete()
    {
        try
        {
            if (File.Exists(
                    _licenseFilePath))
            {
                File.Delete(
                    _licenseFilePath
                );
            }
        }
        catch
        {
            // Ignore deletion errors.
        }
    }


    // =========================================================
    // CHECK LICENSE FILE
    // =========================================================

    public bool Exists()
    {
        return File.Exists(
            _licenseFilePath
        );
    }


    // =========================================================
    // ENCRYPT LICENSE
    // =========================================================

    private void SaveEncrypted(
        StoredLicense license)
    {
        string json =
            JsonSerializer.Serialize(
                license,
                JsonOptions
            );

        byte[] plainBytes =
            Encoding.UTF8.GetBytes(
                json
            );

        byte[] encryptedBytes =
            ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.CurrentUser
            );

        File.WriteAllBytes(
            _licenseFilePath,
            encryptedBytes
        );
    }


    // =========================================================
    // CREATE / UPDATE REGISTRY
    // =========================================================

    private void CreateOrUpdateRegistryMarker(
        string machineId,
        DateTime currentUtc)
    {
        try
        {
            using RegistryKey key =
                Registry.CurrentUser.CreateSubKey(
                    _registryPath
                );

            string marker =
                CreateRegistryMarker(
                    machineId
                );

            key.SetValue(
                RegistryMarkerName,
                marker,
                RegistryValueKind.String
            );

            key.SetValue(
                RegistryLastSeenName,
                currentUtc.ToString("O"),
                RegistryValueKind.String
            );
        }
        catch
        {
            // The encrypted license remains
            // the primary storage.
        }
    }


    // =========================================================
    // UPDATE REGISTRY TIME
    // =========================================================

    private void UpdateRegistryLastSeen(
        DateTime currentUtc)
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    _registryPath,
                    writable: true
                );

            key?.SetValue(
                RegistryLastSeenName,
                currentUtc.ToString("O"),
                RegistryValueKind.String
            );
        }
        catch
        {
            // Ignore registry errors.
        }
    }


    // =========================================================
    // CREATE REGISTRY MARKER
    // =========================================================

    private static string CreateRegistryMarker(
        string machineId)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(
                $"HMS-LICENSE-{machineId}"
            );

        byte[] hash =
            SHA256.HashData(
                bytes
            );

        return Convert.ToHexString(
            hash
        );
    }
}


// =============================================================
// STORED LICENSE
// =============================================================

public class StoredLicense
{
    public string LicenseKey { get; set; } =
        string.Empty;

    public string LicenseType { get; set; } =
        string.Empty;

    public string MachineId { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public string? ProductName { get; set; }

    public string? CustomerName { get; set; }

    public DateTime FirstActivatedUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public DateTime? LastValidatedUtc { get; set; }

    public DateTime SavedAt { get; set; }
}

