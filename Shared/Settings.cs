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
            DataAccessConnectionString = AzureDataUtils.GetKeyVaultSecret("subminimizer", "DataAccessCs");
            ApiKey = AzureDataUtils.GetKeyVaultSecret("subminimizer", "apikey");
            WebJobDashboardConnectionString = AzureDataUtils.GetKeyVaultSecret("subminimizer", "WJDashboardCs");
            WebJobStorageConnectionString = AzureDataUtils.GetKeyVaultSecret("subminimizer", "WJStorageCs");
            AppClientId = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregid");
            AppPassword = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregpassword");
        }

        public string ApiKey { get; set; }

        public string DataAccessConnectionString { get; set; }

        public string AppClientId { get; set; }

        public string AppPassword { get; set; }

        public string WebJobDashboardConnectionString { get; set; }
        
        public string WebJobStorageConnectionString { get; set; }
    }
}
