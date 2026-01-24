using System;
using System.IO;

namespace BusinessSuite.DAL.Data
{
    public static class DatabasePathProvider
    {
        public static string GetDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            var businessSuiteFolder = Path.Combine(appDataPath, "BusinessSuite");

            if (!Directory.Exists(businessSuiteFolder))
            {
                Directory.CreateDirectory(businessSuiteFolder);
            }

            return Path.Combine(businessSuiteFolder, "businesssuite.db");
        }
    }
}
