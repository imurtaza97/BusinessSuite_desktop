using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BusinessSuite.BLL.Services;

public static class HardwareService
{
    public static string GetHardwareId()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsHardwareId();
            }
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacHardwareId();
            }

            return "Unknown-OS";
        }
        catch (Exception)
        {
            return "Unknown-Error";
        }
    }

    private static string GetWindowsHardwareId()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "csproduct get uuid",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Output format is usually:
            // UUID
            // XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
            
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                return lines[1].Trim();
            }
            
            return "Unknown-Windows";
        }
        catch
        {
            return "Unknown-Windows-Error";
        }
    }

    private static string GetMacHardwareId()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "system_profiler",
                    Arguments = "SPHardwareDataType",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var serialLine = output.Split('\n')
                .FirstOrDefault(l => l.Contains("Serial Number"));
            var serial = serialLine?.Split(':').LastOrDefault()?.Trim();
            
            return serial ?? "Unknown-Mac";
        }
        catch
        {
            return "Unknown-Mac-Error";
        }
    }
}