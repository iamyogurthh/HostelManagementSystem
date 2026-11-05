using HostelManagementSystem.Models;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace HostelManagementSystem.Services;

public class LicenseService
{
    private const string ActivationApiUrl =
        "https://licensing-admin-dashboard.vercel.app/api/licenses/activate";

    private const string ValidationApiUrl =
        "https://licensing-admin-dashboard.vercel.app/api/licenses/validate";

    // =========================================================
    // SERVER VALIDATION INTERVAL
    // =========================================================

    private static readonly TimeSpan ValidationInterval =
        //TimeSpan.FromDays(30);
        TimeSpan.FromSeconds(10);

    private readonly MachineIdService _machineIdService;

    private readonly LicenseStorage _licenseStorage;

    private readonly string _validationApiUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LicenseService(
        LicenseStorage? licenseStorage = null,
        string? validationApiUrl = null)
    {
        _machineIdService =
            new MachineIdService();

        _licenseStorage =
            licenseStorage ?? new LicenseStorage();

        _validationApiUrl =
            string.IsNullOrWhiteSpace(validationApiUrl)
                ? ValidationApiUrl
                : validationApiUrl;
    }

    // =========================================================
    // IS ACTIVATED
    // =========================================================

    public bool IsActivated()
    {
        return GetLicenseState()
            == LicenseState.Active;
    }

    // =========================================================
    // GET LICENSE STATE
    // =========================================================

    public LicenseState GetLicenseState()
    {
        try
        {
            StoredLicense? license =
                _licenseStorage.Load();

            // -------------------------------------------------
            // LICENSE FILE DOES NOT EXIST
            // -------------------------------------------------

            if (license == null)
            {
                if (_licenseStorage.HasRegistryMarker())
                {
                    return LicenseState.MissingLicenseFile;
                }

                return LicenseState.NotActivated;
            }

            // -------------------------------------------------
            // BASIC VALIDATION
            // -------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    license.LicenseKey))
            {
                return LicenseState.Invalid;
            }

            if (string.IsNullOrWhiteSpace(
                    license.MachineId))
            {
                return LicenseState.Invalid;
            }

            // -------------------------------------------------
            // MACHINE ID CHECK
            // -------------------------------------------------

            string currentMachineId =
                _machineIdService.GetMachineId();

            if (!string.Equals(
                    license.MachineId,
                    currentMachineId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseState.InvalidMachine;
            }

            // -------------------------------------------------
            // STATUS CHECK
            // -------------------------------------------------

            if (!IsActiveStatus(
                    license.Status))
            {
                if (IsExpiredStatus(license.Status))
                {
                    return LicenseState.Expired;
                }

                return LicenseState.Inactive;
            }

            DateTime nowUtc =
                DateTime.UtcNow;

            // -------------------------------------------------
            // CLOCK ROLLBACK CHECK
            // -------------------------------------------------

            if (IsClockRollbackDetected(
                    license,
                    nowUtc))
            {
                return LicenseState.ClockRollback;
            }

            // -------------------------------------------------
            // EXPIRATION CHECK
            // -------------------------------------------------

            if (license.ExpiresAt.HasValue)
            {
                if (nowUtc >=
                    license.ExpiresAt.Value)
                {
                    return LicenseState.Expired;
                }
            }

            // -------------------------------------------------
            // LICENSE IS VALID
            // -------------------------------------------------

            _licenseStorage.UpdateLastSeen(
                license,
                nowUtc
            );

            return LicenseState.Active;
        }
        catch
        {
            return LicenseState.Invalid;
        }
    }

    // =========================================================
    // GET STORED LICENSE
    // =========================================================

    public StoredLicense? GetStoredLicense()
    {
        return _licenseStorage.Load();
    }

    // =========================================================
    // GET MACHINE ID
    // =========================================================

    public string GetMachineId()
    {
        return _machineIdService.GetMachineId();
    }

    // =========================================================
    // SHOULD VALIDATE ONLINE
    // =========================================================

    public bool ShouldValidateOnline()
    {
        try
        {
            StoredLicense? license =
                _licenseStorage.Load();

            if (license == null)
            {
                return false;
            }

            // -------------------------------------------------
            // No successful server validation recorded.
            // -------------------------------------------------

            if (!license.LastValidatedUtc.HasValue)
            {
                return true;
            }

            DateTime nowUtc =
                DateTime.UtcNow;

            DateTime nextValidationTime =
                license.LastValidatedUtc.Value
                .Add(ValidationInterval);

            return nowUtc >=
                   nextValidationTime;
        }
        catch
        {
            // If we cannot determine the validation time,
            // do not make online validation mandatory.
            return false;
        }
    }

    // =========================================================
    // VALIDATE ONLINE
    // =========================================================

    public async Task<LicenseValidationResult> ValidateOnlineAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            StoredLicense? localLicense =
                _licenseStorage.Load();

            // -------------------------------------------------
            // NO LOCAL LICENSE
            // -------------------------------------------------

            if (localLicense == null)
            {
                return LicenseValidationResult.Failed(
                    "No local license was found."
                );
            }

            // -------------------------------------------------
            // MACHINE ID
            // -------------------------------------------------

            string machineId =
                _machineIdService.GetMachineId();

            // -------------------------------------------------
            // VERIFY LOCAL MACHINE BINDING
            // -------------------------------------------------

            if (!string.Equals(
                    localLicense.MachineId,
                    machineId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return LicenseValidationResult.Failed(
                    "This license belongs to another computer."
                );
            }

            // -------------------------------------------------
            // REQUEST
            // -------------------------------------------------

            var request =
                new LicenseValidationRequest
                {
                    LicenseKey =
                        localLicense.LicenseKey,

                    MachineId =
                        machineId
                };

            // -------------------------------------------------
            // HTTP REQUEST
            // -------------------------------------------------

            using HttpClient httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    _validationApiUrl,
                    request,
                    JsonOptions,
                    cancellationToken
                );

            // -------------------------------------------------
            // READ API RESPONSE
            // -------------------------------------------------

            LicenseApiResponse? apiResponse =
                null;

            try
            {
                apiResponse =
                    await response.Content
                        .ReadFromJsonAsync<LicenseApiResponse>(
                            JsonOptions,
                            cancellationToken
                        );

            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Invalid license API response: {ex.Message}");

                return LicenseValidationResult.Failed(
                    "The licensing server returned an invalid response.");
            }
            catch (NotSupportedException ex)
            {
                Debug.WriteLine($"Unsupported license API response format: {ex.Message}");

                return LicenseValidationResult.Failed(
                    "The licensing server returned an unsupported response.");
            }

            // -------------------------------------------------
            // SERVER REPLIED WITH INVALID RESPONSE
            // -------------------------------------------------

            if (apiResponse == null)
            {
                return LicenseValidationResult.Failed(
                    "The licensing server returned an invalid response."
                );
            }

            // =================================================
            // EXPLICIT SERVER REJECTION CODES
            // =================================================
            //
            // Some rejection responses such as
            // INSTALLATION_BLOCKED may not contain License data.
            //
            // Therefore we must check the Code separately,
            // BEFORE checking apiResponse.License.
            //
            // =================================================

            if (IsServerRejectionCode(
                apiResponse.Code))
            {
                // -------------------------------------------------
                // SERVER REJECTED THE LICENSE
                // -------------------------------------------------
                //
                // If the server returned license information,
                // synchronize the local license status with the
                // server before showing the Activation Form.
                //
                // This allows the UI to correctly show that the
                // license is suspended, expired, revoked, etc.
                //
                // -------------------------------------------------

                if (apiResponse.License != null)
                {
                    DateTime validatedUtc =
                        DateTime.UtcNow;

                    _licenseStorage.UpdateAfterValidation(
                        localLicense,
                        apiResponse.License,
                        validatedUtc
                    );
                }

                return LicenseValidationResult.ServerRejected(
                    apiResponse.Message,
                    apiResponse.License
                );
            }

            // -------------------------------------------------
            // SERVER RETURNED LICENSE INFORMATION
            // -------------------------------------------------

            if (apiResponse.License != null)
            {
                DateTime validatedUtc =
                    DateTime.UtcNow;

                _licenseStorage.UpdateAfterValidation(
                    localLicense,
                    apiResponse.License,
                    validatedUtc
                );

                Debug.WriteLine("[License API] Activation license data received:");
                Debug.WriteLine($"  LicenseType: '{apiResponse.License.LicenseType}'");
                Debug.WriteLine($"  Status: '{apiResponse.License.Status}'");
                Debug.WriteLine($"  ProductName: '{apiResponse.License.ProductName}'");
                Debug.WriteLine($"  CustomerName: '{apiResponse.License.CustomerName}'");

                // -------------------------------------------------
                // ACTIVE
                // -------------------------------------------------

                if (IsActiveStatus(
                        apiResponse.License.Status))
                {
                    return LicenseValidationResult.Success(
                        apiResponse.Message,
                        apiResponse.License
                    );
                }

                // -------------------------------------------------
                // EXPIRED
                // -------------------------------------------------

                if (IsExpiredStatus(
                        apiResponse.License.Status))
                {
                    return LicenseValidationResult.ServerRejected(
                        apiResponse.Message,
                        apiResponse.License
                    );
                }

                // -------------------------------------------------
                // SUSPENDED / REVOKED / OTHER INACTIVE STATUS
                // -------------------------------------------------

                return LicenseValidationResult.ServerRejected(
                    apiResponse.Message,
                    apiResponse.License
                );
            }

            // -------------------------------------------------
            // NO LICENSE DATA
            // -------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                return LicenseValidationResult.Failed(
                    apiResponse.Message
                );
            }

            return LicenseValidationResult.Failed(
                "The licensing server did not return license information."
            );

            


        }
        catch (HttpRequestException)
        {
            // -------------------------------------------------
            // OFFLINE
            // -------------------------------------------------
            //
            // Network failure does NOT invalidate the license.
            // HMS continues working offline.
            // -------------------------------------------------

            return LicenseValidationResult.Offline(
                "Unable to connect to the licensing server."
            );
        }
        catch (TaskCanceledException)
        {
            // -------------------------------------------------
            // TIMEOUT
            // -------------------------------------------------

            return LicenseValidationResult.Offline(
                "The licensing server request timed out."
            );
        }
        catch (InvalidOperationException ex)
        {
            return LicenseValidationResult.Failed(
                ex.Message
            );
        }
        catch (Exception ex)
        {
            return LicenseValidationResult.Failed(
                $"License validation failed: {ex.Message}"
            );
        }
    }

    // =========================================================
    // SERVER REJECTION CODES
    // =========================================================

    private static bool IsServerRejectionCode(
        string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return code.Equals(
                   "LICENSE_NOT_ACTIVE",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "LICENSE_EXPIRED",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "INSTALLATION_BLOCKED",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "INSTALLATION_NOT_ACTIVE",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "MACHINE_NOT_REGISTERED",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "PRODUCT_INACTIVE",
                   StringComparison.OrdinalIgnoreCase)
               ||
               code.Equals(
                   "LICENSE_NOT_FOUND",
                   StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // ACTIVATE
    // =========================================================

    public async Task<LicenseResult> ActivateAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                licenseKey))
        {
            return LicenseResult.Failed(
                "Please enter a license key."
            );
        }

  
        // -------------------------------------------------
        // ONLINE ACTIVATION
        // -------------------------------------------------

        try
        {
            string machineId =
                _machineIdService.GetMachineId();

            var request =
                new LicenseActivationRequest
                {
                    LicenseKey =
                        licenseKey.Trim(),

                    MachineId =
                        machineId,

                    MachineName =
                        Environment.MachineName,

                    AppVersion =
                        GetAppVersion()
                };

            using HttpClient httpClient =
                new HttpClient
                {
                    Timeout =
                        TimeSpan.FromSeconds(15)
                };

            HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    ActivationApiUrl,
                    request,
                    JsonOptions,
                    cancellationToken
                );

            LicenseApiResponse? apiResponse =
                null;

            try
            {
                apiResponse =
                    await response.Content
                        .ReadFromJsonAsync<LicenseApiResponse>(
                            JsonOptions,
                            cancellationToken
                        );
            }
            catch (JsonException ex)
            {
                Debug.WriteLine(
                    $"Invalid license API response: {ex.Message}");

                return LicenseResult.Failed(
                    "The licensing server returned an invalid response.");
            }
            catch (NotSupportedException ex)
            {
                Debug.WriteLine(
                    $"Unsupported license API response format: {ex.Message}");

                return LicenseResult.Failed(
                    "The licensing server returned an unsupported response.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            // -------------------------------------------------
            // HTTP ERROR
            // -------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                return LicenseResult.Failed(
                    apiResponse?.Message ??
                    "License activation failed."
                );
            }

            // -------------------------------------------------
            // INVALID RESPONSE
            // -------------------------------------------------

            if (apiResponse == null)
            {
                return LicenseResult.Failed(
                    "The licensing server returned an invalid response."
                );
            }

            // -------------------------------------------------
            // SERVER REJECTED
            // -------------------------------------------------

            if (!apiResponse.Success)
            {
                return LicenseResult.Failed(
                    apiResponse.Message
                );
            }

            // -------------------------------------------------
            // NO LICENSE DATA
            // -------------------------------------------------

            if (apiResponse.License == null)
            {
                return LicenseResult.Failed(
                    "The licensing server did not return license information."
                );
            }

            Debug.WriteLine("[License API] Validation license data received:"); 
            Debug.WriteLine($"  LicenseType: '{apiResponse.License.LicenseType}'"); 
            Debug.WriteLine($"  Status: '{apiResponse.License.Status}'"); 
            Debug.WriteLine($"  ProductName: '{apiResponse.License.ProductName}'"); 
            Debug.WriteLine($"  CustomerName: '{apiResponse.License.CustomerName}'");

            // -------------------------------------------------
            // SAVE LICENSE
            // -------------------------------------------------

            _licenseStorage.Save(
                apiResponse.License,
                machineId
            );

            return LicenseResult.Success(
                apiResponse.Message
            );
        }
        catch (HttpRequestException)
        {
            return LicenseResult.Failed(
                "Unable to connect to the licensing server. " +
                "Please check your internet connection."
            );
        }
        catch (TaskCanceledException)
        {
            return LicenseResult.Failed(
                "The activation request timed out."
            );
        }
        catch (InvalidOperationException ex)
        {
            return LicenseResult.Failed(
                ex.Message
            );
        }
        catch (Exception ex)
        {
            return LicenseResult.Failed(
                $"Activation failed: {ex.Message}"
            );
        }
    }

    // =========================================================
    // CLOCK ROLLBACK DETECTION
    // =========================================================

    private bool IsClockRollbackDetected(
        StoredLicense license,
        DateTime nowUtc)
    {
        // -------------------------------------------------
        // CHECK ENCRYPTED LICENSE
        // -------------------------------------------------

        if (license.LastSeenUtc != default)
        {
            if (nowUtc <
                license.LastSeenUtc)
            {
                return true;
            }
        }

        // -------------------------------------------------
        // CHECK WINDOWS REGISTRY
        // -------------------------------------------------

        DateTime? registryLastSeen =
            _licenseStorage.GetRegistryLastSeen();

        if (registryLastSeen.HasValue)
        {
            if (nowUtc <
                registryLastSeen.Value)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // ACTIVE STATUS
    // =========================================================

    private static bool IsActiveStatus(
        string? status)
    {
        if (string.IsNullOrWhiteSpace(
                status))
        {
            return false;
        }

        return status.Equals(
                   "Active",
                   StringComparison.OrdinalIgnoreCase
               )
               ||
               status.Equals(
                   "Activated",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    // =========================================================
    // EXPIRED STATUS
    // =========================================================

    private static bool IsExpiredStatus(
        string? status)
    {
        if (string.IsNullOrWhiteSpace(
                status))
        {
            return false;
        }

        return status.Equals(
            "Expired",
            StringComparison.OrdinalIgnoreCase
        );
    }

    // =========================================================
    // APP VERSION
    // =========================================================

    private static string GetAppVersion()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString()
            ??
            "-";
    }

    // =========================================================
    // CLEAR LOCAL LICENSE
    // =========================================================

    public void ClearLocalLicense()
    {
        _licenseStorage.Delete();
    }
}


// =============================================================
// LICENSE STATE
// =============================================================

public enum LicenseState
{
    NotActivated,

    Active,

    Expired,

    Inactive,

    InvalidMachine,

    ClockRollback,

    MissingLicenseFile,

    Invalid
}


// =============================================================
// LICENSE RESULT
// =============================================================

public class LicenseResult
{
    public bool IsSuccess { get; private set; }

    public string Message { get; private set; } =
        string.Empty;

    private LicenseResult()
    {
    }

    public static LicenseResult Success(
        string message)
    {
        return new LicenseResult
        {
            IsSuccess = true,
            Message = message
        };
    }

    public static LicenseResult Failed(
        string message)
    {
        return new LicenseResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}


// =============================================================
// LICENSE VALIDATION RESULT
// =============================================================

public class LicenseValidationResult
{
    public bool IsSuccess { get; private set; }

    public bool IsOffline { get; private set; }

    public bool IsServerRejected { get; private set; }

    public string Message { get; private set; } =
        string.Empty;

    public LicenseInfo? License { get; private set; }

    private LicenseValidationResult()
    {
    }

    public static LicenseValidationResult Success(
        string message,
        LicenseInfo license)
    {
        return new LicenseValidationResult
        {
            IsSuccess = true,
            IsOffline = false,
            IsServerRejected = false,
            Message = message,
            License = license
        };
    }

    public static LicenseValidationResult ServerRejected(
        string message,
        LicenseInfo? license)
    {
        return new LicenseValidationResult
        {
            IsSuccess = false,
            IsOffline = false,
            IsServerRejected = true,
            Message = message,
            License = license
        };
    }

    public static LicenseValidationResult Offline(
        string message)
    {
        return new LicenseValidationResult
        {
            IsSuccess = false,
            IsOffline = true,
            IsServerRejected = false,
            Message = message
        };
    }

    public static LicenseValidationResult Failed(
        string message)
    {
        return new LicenseValidationResult
        {
            IsSuccess = false,
            IsOffline = false,
            IsServerRejected = false,
            Message = message
        };
    }
}