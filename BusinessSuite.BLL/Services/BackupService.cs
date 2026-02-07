using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;

namespace BusinessSuite.BLL.Services;

public class BackupService
{
    public Task BackupDatabaseAsync(string zipPath)
    {
        return Task.Run(() =>
        {
            var dbPath = DatabasePathProvider.GetDatabasePath();

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

            AddIfExists(zip, dbPath);
            AddIfExists(zip, dbPath + "-wal");
            AddIfExists(zip, dbPath + "-shm");
        });
    }

    private void AddIfExists(ZipArchive zip, string path)
    {
        if (File.Exists(path))
            zip.CreateEntryFromFile(path, Path.GetFileName(path));
    }

    public Task RestoreDatabaseAsync(string zipPath)
    {
        return Task.Run(() =>
        {
            var dbPath = DatabasePathProvider.GetDatabasePath();
            var dbDir = Path.GetDirectoryName(dbPath)!;

            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Backup file not found.");

            // 1. Ensure all DB connections are released
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 2. Delete existing DB files
            DeleteIfExists(dbPath);
            DeleteIfExists(dbPath + "-wal");
            DeleteIfExists(dbPath + "-shm");

            // 3. Extract ZIP into DB folder
            ZipFile.ExtractToDirectory(zipPath, dbDir, overwriteFiles: true);

            // 4. Force app restart
            throw new ApplicationException("RESTORE_SUCCESS_RESTART_REQUIRED");
        });
    }

    private void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

}
