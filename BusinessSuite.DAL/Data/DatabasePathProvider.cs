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

            // 1. Determine the folder name based on the build mode
            string folderName = "BusinessSuite";

            #if DEBUG
                        // This code only runs when you press "Run" in VS Code/Visual Studio
                        folderName = "BusinessSuite_Dev"; 
            #endif

            var businessSuiteFolder = Path.Combine(appDataPath, folderName);

            // 2. Ensure the directory exists
            if (!Directory.Exists(businessSuiteFolder))
            {
                Directory.CreateDirectory(businessSuiteFolder);
            }

            return Path.Combine(businessSuiteFolder, "businesssuite.db");
        }
    }
}
