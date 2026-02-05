using System;
using System.IO;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;

namespace BusinessSuite.BLL.Services;

public class BackupService
{
    public Task BackupDatabaseAsync(string destinationPath)
    {
        return Task.Run(() =>
        {
            var sourcePath = DatabasePathProvider.GetDatabasePath();
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Source database file not found.");

            File.Copy(sourcePath, destinationPath, true);
        });
    }

    public Task RestoreDatabaseAsync(string sourcePath)
    {
        return Task.Run(() =>
        {
            var destinationPath = DatabasePathProvider.GetDatabasePath();
            
            // It's safer to delete the current one before copying if needed, 
            // but File.Copy with overwrite true should work.
            // Note: Restoring while the app is running might be tricky if the DB is locked.
            // SQLite typically handles this but the user might need to restart.
            File.Copy(sourcePath, destinationPath, true);
        });
    }
}
