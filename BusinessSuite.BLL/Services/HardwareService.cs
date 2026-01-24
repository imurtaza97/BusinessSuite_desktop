using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BusinessSuite.BLL.Services;

public static class HardwareService
{
    public static string GetHardwareId()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "system_profiler",
                Arguments = "SPHardwareDataType",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var serialLine = output.Split('\n')
            .FirstOrDefault(l => l.Contains("Serial Number"));
        var serial = serialLine?.Split(':').LastOrDefault()?.Trim() ?? "Unknown";

        return serial;
    }
}