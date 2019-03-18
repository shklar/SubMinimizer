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

        private string allowWebJobEmail;

        private string allowWebJobDelete;

        private string aRMAuthorizationPermissionsAPIVersion;

        private string aRMAuthorizationRoleAssignmentsAPIVersion;

        private string aRMAuthorizationRoleDefinitionsAPIVersion;
    
        private string azureResourceManagerIdentifier;

        private string azureResourceManagerUrl;

        private string azureResourceManagerAPIVersion;

        private string authority;

        private string devTeam;

        private string enableWebJob;

        private string graphAPIIdentifier;

        private string graphAPIVersion;

        private string microsoftAADID;

        private string serviceURL;

        private string telemetryInstrumentationKey;

        private string envDisplayName;

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

        public string AllowWebJobEmail
        {
            get { return GetActualSetting("env:AllowWebJobEmail", allowWebJobEmail); }
            set { allowWebJobEmail = value; }
        }

        public string AllowWebJobDelete
        {
            get { return GetActualSetting("env:AllowWebJobDelete", allowWebJobDelete); }
            set { allowWebJobDelete = value; }
        }

        public string ARMAuthorizationPermissionsAPIVersion
        {
            get { return GetActualSetting("env:ARMAuthorizationPermissionsAPIVersion", aRMAuthorizationPermissionsAPIVersion); }
            set { aRMAuthorizationPermissionsAPIVersion = value; }
        }

        public string ARMAuthorizationRoleDefinitionsAPIVersion
        {
            get { return GetActualSetting("env:ARMAuthorizationRoleDefinitionsAPIVersion", aRMAuthorizationRoleDefinitionsAPIVersion); }
            set { aRMAuthorizationRoleDefinitionsAPIVersion = value; }
        }

        public string ARMAuthorizationRoleAssignmentsAPIVersion
        {
            get { return GetActualSetting("env:ARMAuthorizationRoleAssignmentsAPIVersion", aRMAuthorizationRoleDefinitionsAPIVersion); }
            set { aRMAuthorizationRoleAssignmentsAPIVersion = value; }
        }

        public string AzureResourceManagerIdentifier
        {
            get { return GetActualSetting("env:AzureResourceManagerIdentifier", azureResourceManagerIdentifier); }
            set { azureResourceManagerIdentifier = value; }
        }
        public string AzureResourceManagerUrl
        {
            get { return GetActualSetting("env:AzureResourceManagerUrl", azureResourceManagerUrl); }
            set { azureResourceManagerUrl = value; }
        }
        public string AzureResourceManagerAPIVersion
        {
            get { return GetActualSetting("env:AzureResourceManagerAPIVersion", azureResourceManagerAPIVersion); }
            set { azureResourceManagerAPIVersion = value; }
        }
        public string Authority
        {
            get { return GetActualSetting("env:Authority", authority); }
            set { authority = value; }
        }

        public string DevTeam
        {
            get { return GetActualSetting("env:DevTeam", devTeam); }
            set { devTeam = value; }
        }

        public string GraphAPIIdentifier
        {
            get { return GetActualSetting("env:GraphAPIIdentifier", graphAPIIdentifier); }
            set { graphAPIIdentifier = value; }
        }

        public string GraphAPIVersion
        {
            get { return GetActualSetting("env:GraphAPIVersion", graphAPIVersion); }
            set { graphAPIVersion = value; }
        }

        public string MicrosoftAADID
        {
            get { return GetActualSetting("env:MicrosoftAADID", microsoftAADID); }
            set { microsoftAADID = value; }
        }
        public string ServiceURL
        {
            get { return GetActualSetting("env:ServiceURL", serviceURL); }
            set { serviceURL = value; }
        }

        public string  TelemetryInstrumentationKey
        {
            get { return GetActualSetting("env:TelemetryInstrumentationKey", telemetryInstrumentationKey); }
            set { telemetryInstrumentationKey = value; }
        }

        public string EnableWebJob
        {
            get { return GetActualSetting("env:EnableWebJob", enableWebJob); }
            set { enableWebJob = value; }
        }

        public string EnvDisplayName
        {
            get { return GetActualSetting("env:EnvDisplayName", envDisplayName); }
            set { envDisplayName = value; }
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
                settings.WebJobStorageConnectionString != null &&
                settings.AllowWebJobEmail != null &&
                settings.AllowWebJobDelete != null &&
                settings.ARMAuthorizationPermissionsAPIVersion != null &&
                settings.ARMAuthorizationRoleAssignmentsAPIVersion != null &&
                settings.ARMAuthorizationRoleDefinitionsAPIVersion != null &&
                settings.AzureResourceManagerIdentifier != null &&
                settings.AzureResourceManagerUrl != null &&
                settings.AzureResourceManagerAPIVersion != null &&
                settings.Authority != null &&
                settings.DevTeam != null &&
                settings.EnableWebJob != null &&
                settings.GraphAPIIdentifier != null &&
                settings.GraphAPIVersion != null &&
                settings.MicrosoftAADID != null &&
                settings.ServiceURL != null &&
                settings.TelemetryInstrumentationKey != null &&
                settings.EnvDisplayName != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string GetSetting(string valueName)
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

            AllowWebJobDelete = GetConfigurationValue("env:AllowWebJobDelete");
            AllowWebJobEmail = GetConfigurationValue("env:AllowWebJobEmail");
            ARMAuthorizationPermissionsAPIVersion = GetConfigurationValue("ida:ARMAuthorizationPermissionsAPIVersion");
            ARMAuthorizationRoleAssignmentsAPIVersion = GetConfigurationValue("ida:ARMAuthorizationRoleAssignmentsAPIVersion");
            ARMAuthorizationRoleDefinitionsAPIVersion = GetConfigurationValue("ida:ARMAuthorizationRoleDefinitionsAPIVersion");
            AzureResourceManagerAPIVersion = GetConfigurationValue("ida:AzureResourceManagerAPIVersion");
            AzureResourceManagerIdentifier = GetConfigurationValue("ida:AzureResourceManagerIdentifier");
            AzureResourceManagerUrl = GetConfigurationValue("ida:AzureResourceManagerUrl");
            Authority = GetConfigurationValue("ida:Authority");
            DevTeam = GetConfigurationValue("env:DevTeam");
            EnableWebJob = GetConfigurationValue("env:EnableWebJob");
            EnvDisplayName = GetConfigurationValue("env:EnvDisplayName");
            GraphAPIIdentifier = GetConfigurationValue("ida:GraphAPIIdentifier");
            GraphAPIVersion = GetConfigurationValue("ida:GraphAPIVersion");
            MicrosoftAADID = GetConfigurationValue("ida:MicrosoftAADID");
            ServiceURL = GetConfigurationValue("env:ServiceURL");
            TelemetryInstrumentationKey = GetConfigurationValue("env:TelemetryInstrumentationKey");
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
