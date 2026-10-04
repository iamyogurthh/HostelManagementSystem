using System;
using System.Linq;
using HostelManagementSystem.Services;
using Xunit;

namespace HostelManagementSystem.Tests;

public class MachineIdServiceTests
{
    [Fact]
    public void GetMachineId_ShouldReturnValidMachineIdFormat()
    {
        // Arrange
        var service = new MachineIdService();

        // Act
        string machineId = service.GetMachineId();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(machineId));

        string[] parts = machineId.Split('-');

        Assert.Equal(4, parts.Length);

        foreach (string part in parts)
        {
            Assert.Equal(4, part.Length);

            Assert.True(
                part.All(Uri.IsHexDigit),
                $"Machine ID contains an invalid hexadecimal part: {part}"
            );
        }
    }

    [Fact]
    public void GetMachineId_ShouldReturnSameId_WhenCalledMultipleTimes()
    {
        // Arrange
        var service = new MachineIdService();

        // Act
        string firstMachineId = service.GetMachineId();
        string secondMachineId = service.GetMachineId();

        // Assert
        Assert.Equal(firstMachineId, secondMachineId);
    }
}