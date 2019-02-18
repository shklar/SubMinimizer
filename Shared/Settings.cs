using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public class Settings
    {
        private static Settings settings;

        public static Settings Instance
        {
            get
            {
                if (settings == null)
                {
                    settings = new Settings();
                }

                return settings;
            }

        }

        private Settings()
        {
            DataAccessConnectionString = Utilities.GetKeyVaultSecret("subminimizer", "DataAccessCs");
            ApiKey = Utilities.GetKeyVaultSecret("subminimizer", "apikey");
            WebJobDashboardConnectionString = Utilities.GetKeyVaultSecret("subminimizer", "WJDashboardCs");
            WebJobStorageConnectionString = Utilities.GetKeyVaultSecret("subminimizer", "WJStorageCs");
            AppClientId = Utilities.GetKeyVaultSecret("subminimizer", "appregid");
            AppPassword = Utilities.GetKeyVaultSecret("subminimizer", "appregpassword");
        }

        public string ApiKey { get; set; }

        public string DataAccessConnectionString { get; set; }

        public string AppClientId { get; set; }

        public string AppPassword { get; set; }

        public string WebJobDashboardConnectionString { get; set; }
        
        public string WebJobStorageConnectionString { get; set; }

        public static bool Initialize()
        {
            try
            {
                Settings settings = Instance;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
