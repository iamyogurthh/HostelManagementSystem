using CommunityToolkit.Mvvm.ComponentModel;
using HostelManagementSystem.Services;
using System;
using System.Diagnostics;

namespace HostelManagementSystem.ViewModels.SettingViewModels;

public partial class LicenseSettingsViewModel : ObservableObject
{
    private readonly LicenseStorage _licenseStorage;

    [ObservableProperty]
    private string _productName = "—";

    [ObservableProperty]
    private string _status = "—";

    [ObservableProperty]
    private string _licenseKey = "—";

    [ObservableProperty]
    private string _licenseType = "—";

    [ObservableProperty]
    private string _customerName = "—";

    [ObservableProperty]
    private string _expiration = "—";

    [ObservableProperty]
    private string _activatedDate = "—";

    [ObservableProperty]
    private string _lastValidated = "—";

    public LicenseSettingsViewModel()
    {
        _licenseStorage = new LicenseStorage();

        LoadLicense();
    }

    private void LoadLicense()
    {
        try
        {
            var license = _licenseStorage.Load();

            if (license == null)
            {
                Debug.WriteLine("[License] No readable license.dat was found.");
                return;
            }

            Debug.WriteLine("[License] Stored license data loaded:");
            Debug.WriteLine($"  LicenseKey: {MaskLicenseKey(license.LicenseKey)}");
            Debug.WriteLine($"  LicenseType: {license.LicenseType}");
            Debug.WriteLine($"  ProductName: {license.ProductName}");
            Debug.WriteLine($"  Status: {license.Status}");
            Debug.WriteLine($"  CustomerName: {license.CustomerName}");
            Debug.WriteLine($"  ExpiresAt: {license.ExpiresAt:O}");
            Debug.WriteLine($"  FirstActivatedUtc: {license.FirstActivatedUtc:O}");
            Debug.WriteLine($"  LastSeenUtc: {license.LastSeenUtc:O}");
            Debug.WriteLine($"  LastValidatedUtc: {license.LastValidatedUtc:O}");
            Debug.WriteLine($"  SavedAt: {license.SavedAt:O}");

            ProductName = string.IsNullOrWhiteSpace(license.ProductName)
                ? "Hostel Management System"
                : license.ProductName;

            Status = string.IsNullOrWhiteSpace(license.Status)
                ? "Unknown"
                : license.Status;

            LicenseKey = string.IsNullOrWhiteSpace(license.LicenseKey)
                ? "—"
                : license.LicenseKey;

            LicenseType = string.IsNullOrWhiteSpace(license.LicenseType)
                ? "—"
                : license.LicenseType;

            CustomerName = string.IsNullOrWhiteSpace(license.CustomerName)
                ? "—"
                : license.CustomerName;

            Expiration = license.ExpiresAt.HasValue
                ? license.ExpiresAt.Value.ToLocalTime().ToString("dd MMM yyyy")
                : "Perpetual";

            ActivatedDate = license.FirstActivatedUtc != default
                ? license.FirstActivatedUtc.ToLocalTime().ToString("dd MMM yyyy")
                : "—";

            LastValidated = license.LastValidatedUtc.HasValue
                ? license.LastValidatedUtc.Value.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt")
                : "Not yet validated";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[License] Failed to read license.dat: {ex}");

            ProductName = "—";
            Status = "Unable to load";
            LicenseKey = "—";
            LicenseType = "—";
            CustomerName = "—";
            Expiration = "—";
            ActivatedDate = "—";
            LastValidated = "—";
        }
    }

    private static string MaskLicenseKey(string? licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return "<empty>";
        }

        return licenseKey.Length <= 8
            ? "********"
            : $"{licenseKey[..4]}...{licenseKey[^4..]}";
    }
}