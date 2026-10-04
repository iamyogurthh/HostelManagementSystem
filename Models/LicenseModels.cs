using System;

namespace HostelManagementSystem.Models;

public class LicenseActivationRequest
{
    public string LicenseKey { get; set; } = string.Empty;

    public string MachineId { get; set; } = string.Empty;

    public string? MachineName { get; set; }

    public string? AppVersion { get; set; }
}

public class LicenseValidationRequest
{
    public string LicenseKey { get; set; } = string.Empty;

    public string MachineId { get; set; } = string.Empty;
}

public class LicenseApiResponse
{
    public bool Success { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public LicenseInfo? License { get; set; }
}

public class LicenseInfo
{
    public string LicenseKey { get; set; } = string.Empty;

    public string LicenseType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public string? ProductName { get; set; }

    public string? CustomerName { get; set; }
}