using System;
using System.IO;
using System.Threading.Tasks;

namespace BusinessSuite.BLL.Services;

public class FileStorageService
{
    private readonly string _basePath;

    public FileStorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _basePath = Path.Combine(appData, "BusinessSuite", "Attachments", "Bills");

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> StoreBillAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Source file not found.");

        var extension = Path.GetExtension(sourceFilePath);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(_basePath, fileName);

        using (var sourceStream = File.OpenRead(sourceFilePath))
        using (var destinationStream = File.Create(destinationPath))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        return destinationPath;
    }

    public void OpenFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file: {ex.Message}");
            }
        }
    }
}
