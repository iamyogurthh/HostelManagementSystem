using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HostelManagementSystem.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HostelManagementSystem.ViewModels;

public partial class LicenseActivationViewModel : ObservableObject
{
    private readonly LicenseService _licenseService;

    [ObservableProperty]
    private string licenseKey = string.Empty;

    [ObservableProperty]
    private string machineId = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string licenseInformation = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isExpired;

    public bool IsNotBusy => !IsBusy;

    public LicenseActivationViewModel()
    {
        _licenseService = new LicenseService();

        LoadLicenseInformation();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }

    private void LoadLicenseInformation()
    {
        try
        {
            MachineId =
                _licenseService.GetMachineId();

            LicenseState state =
                _licenseService.GetLicenseState();

            switch (state)
            {
                case LicenseState.NotActivated:

                    StatusMessage =
                        "Please enter your license key to activate HMS.";

                    break;

                case LicenseState.Expired:

                    IsExpired = true;

                    StoredLicense? expiredLicense =
                        _licenseService.GetStoredLicense();

                    if (expiredLicense?.ExpiresAt != null)
                    {
                        LicenseInformation =
                            $"Your license expired on " +
                            $"{expiredLicense.ExpiresAt.Value.ToLocalTime():dd MMM yyyy}.";
                    }

                    StatusMessage =
                        "Your license has expired. Please enter a new license key.";

                    break;

                case LicenseState.MissingLicenseFile:

                    StatusMessage =
                        "The local license file is missing. " +
                        "Please activate HMS again.";

                    break;

                case LicenseState.ClockRollback:

                    HasError = true;

                    StatusMessage =
                        "The system date or time appears to have been changed. " +
                        "Please correct your Windows date and time before continuing.";

                    break;

                case LicenseState.InvalidMachine:

                    HasError = true;

                    StatusMessage =
                        "This license belongs to another computer.";

                    break;

                case LicenseState.Inactive:

                    HasError = true;

                    StatusMessage =
                        "This license is inactive. " +
                        "Please contact the administrator.";

                    break;

                case LicenseState.Invalid:

                    HasError = true;

                    StatusMessage =
                        "The local license information is invalid. " +
                        "Please activate HMS again.";

                    break;

                case LicenseState.Active:

                    StatusMessage =
                        "This computer is already activated.";

                    break;
            }
        }
        catch (Exception ex)
        {
            HasError = true;

            StatusMessage =
                $"Unable to load license information: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            HasError = true;

            StatusMessage =
                "Please enter your license key.";

            return;
        }

        IsBusy = true;
        HasError = false;

        StatusMessage =
            "Activating license...";

        try
        {
            LicenseResult result =
                await _licenseService.ActivateAsync(
                    LicenseKey.Trim()
                );

            if (result.IsSuccess)
            {
                HasError = false;

                StatusMessage =
                    result.Message;

                MessageBox.Show(
                    result.Message,
                    "License Activation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                LicenseKey = string.Empty;

                ActivationSucceeded?.Invoke(
                    this,
                    EventArgs.Empty
                );
            }
            else
            {
                HasError = true;

                StatusMessage =
                    result.Message;
            }
        }
        catch (Exception ex)
        {
            HasError = true;

            StatusMessage =
                $"Activation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyMachineId()
    {
        if (string.IsNullOrWhiteSpace(MachineId))
        {
            return;
        }

        try
        {
            Clipboard.SetText(MachineId);

            HasError = false;

            StatusMessage =
                "Machine ID copied to clipboard.";
        }
        catch
        {
            HasError = true;

            StatusMessage =
                "Unable to copy Machine ID.";
        }
    }

    public event EventHandler? ActivationSucceeded;

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(
            this,
            EventArgs.Empty
        );
    }
}