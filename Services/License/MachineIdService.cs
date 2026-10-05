using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace HostelManagementSystem.Services;

public class MachineIdService
{
    private const string MachineGuidRegistryPath =
        @"SOFTWARE\Microsoft\Cryptography";

    private const string MachineGuidRegistryName =
        "MachineGuid";

    public string GetMachineId()
    {
        string machineGuid = GetMachineGuid();

        if (string.IsNullOrWhiteSpace(machineGuid))
        {
            throw new InvalidOperationException(
                "Unable to generate a unique machine identifier."
            );
        }

        return CreateMachineId(machineGuid);
    }

    private static string GetMachineGuid()
    {
        using RegistryKey? key =
            Registry.LocalMachine.OpenSubKey(
                MachineGuidRegistryPath
            );

        return key?.GetValue(
            MachineGuidRegistryName
        )?.ToString() ?? string.Empty;
    }

    private static string CreateMachineId(string machineGuid)
    {
        byte[] inputBytes =
            Encoding.UTF8.GetBytes(machineGuid);

        byte[] hashBytes =
            SHA256.HashData(inputBytes);

        string hash =
            Convert.ToHexString(hashBytes);

        return string.Join(
            "-",
            hash[..4],
            hash[4..8],
            hash[8..12],
            hash[12..16]
        );
    }
}