using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public class Settings
    {
        private string apiKey;

        private string appClientId;

        private string appPassword;

        private string dataAccessConnectionString;

        private string webJobDashboardConnectionString;

        private string webJobStorageConnectionString;

        private static Configuration configuration;

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
            CreateSettings();
        }

        public string ApiKey
        {
            get { return GetActualSetting("env:ApiKey", apiKey); }
            set { apiKey = value; }
        }

        public string AppClientId
        {
            get { return GetActualSetting("env:AppRegId", appClientId); }
            set { appClientId = value; }
        }

        public string AppPassword
        {
            get { return GetActualSetting("env:AppRegPassword", appPassword); }
            set { appPassword = value; }
        }

        public string DataAccessConnectionString
        {
            get { return GetActualSetting("env:DataAccess", dataAccessConnectionString); }
            set { dataAccessConnectionString = value; }
        }

        public string WebJobDashboardConnectionString
        {
            get { return GetActualSetting("env:WebJobDashboardCs", webJobDashboardConnectionString); }
            set { webJobDashboardConnectionString = value; }
        }

        public string WebJobStorageConnectionString
        {
            get { return GetActualSetting("env:WebJobStorageCs", webJobStorageConnectionString); }
            set { webJobStorageConnectionString = value; }
        }
        
        private string GetActualSetting(string valueName, string defaultValue)
        {
            if (ConfigurationManager.AppSettings[valueName] != null)
            {
                return ConfigurationManager.AppSettings[valueName];
            }
            else
            {
                return defaultValue;
            }
        }

        public static bool Initialize()
        {
            Settings settings = Instance;
            if (settings.ApiKey != null &&
                settings.AppClientId != null &&
                settings.AppPassword != null &&
                settings.DataAccessConnectionString != null &&
                settings.WebJobDashboardConnectionString != null &&
                settings.WebJobStorageConnectionString != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetSetting(string valueName)
        {
            if (valueName == "env:DataAccess")
            {
                return DataAccessConnectionString;
            }
            else if (valueName == "env:ApiKey")
            {
                return ApiKey;
            }
            else if (valueName == "env:WebJobStorageCs")
            {
                return WebJobStorageConnectionString;
            }
            else if (valueName == "env:WebJobDashboardCs")
            {
                return WebJobDashboardConnectionString;
            }
            else if (valueName == "env:AppRegId")
            {
                return AppClientId;
            }
            else if (valueName == "env:AppRegPassword")
            {
                return AppPassword;
            }
            else
            {
                string defaultValue = GetConfigurationValue(valueName);
                return GetActualSetting(valueName, defaultValue);
            }
        }

        private void CreateSettings()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
            configuration = ConfigurationManager.OpenExeConfiguration(assemblyPath);

            string keyVaultName = GetConfigurationValue("env:KeyVault");
            if (keyVaultName == null)
            {
                return;
            }

            string dataAccessCs = GetConfigurationValue("env:DataAccess");
            if (dataAccessCs != null)
            {
                DataAccessConnectionString = Utilities.GetKeyVaultSecret(keyVaultName, dataAccessCs);
            }

            string apiKey = GetConfigurationValue("env:ApiKey");
            if (apiKey != null)
            {
                ApiKey = Utilities.GetKeyVaultSecret(keyVaultName, apiKey);
            }

            string webJobStorageCs = GetConfigurationValue("env:WebJobStorageCs");
            if (webJobStorageCs != null)
            {
                WebJobStorageConnectionString = Utilities.GetKeyVaultSecret(keyVaultName, webJobStorageCs);
            }

            string webJobDashboardCs = GetConfigurationValue("env:WebJobDashboardCs");
            if (webJobStorageCs != null)
            {
                WebJobDashboardConnectionString = Utilities.GetKeyVaultSecret(keyVaultName, webJobDashboardCs);
            }

            string appRegId = GetConfigurationValue("env:AppRegId");
            if (appRegId != null)
            {
                AppClientId = Utilities.GetKeyVaultSecret(keyVaultName, appRegId);
            }

            string appRegPwd = GetConfigurationValue("env:AppRegPassword");
            if (appRegPwd != null)
            {
                AppPassword = Utilities.GetKeyVaultSecret(keyVaultName, appRegPwd);
            }
        }

        private string GetConfigurationValue(string valueName)
        {
            var element = configuration.AppSettings.Settings[valueName];
            if (element == null)
            {
                System.Diagnostics.Trace.TraceInformation($"Value for {valueName} in App.config isn't defined");
                return null;
            }

            return element.Value;
        }
    }
}
